using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace godottemplate.Server;

public partial class AgentRestServer : Node
{
    private const string DefaultPrefix = "http://127.0.0.1:8080/";
    private const long MaxBodyBytes = 65536;
    private const string AgentIdMeta = "agent_id";
    private const string AgentExampleGroup = "agent_example";
    private const string AgentDisplayGroup = "agent_display";

    private HttpListener _listener;
    private CancellationTokenSource _cts;
    private Task _listenerTask;
    private readonly ConcurrentQueue<PendingRequest> _queue = new();
    private readonly HashSet<StringName> _pendingReleases = new();
    private int _quitCountdown = -1;
    private int _quitExitCode;

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            QueueFree();
            return;
        }
        StartListener();
    }

    public override void _ExitTree()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Close(); } catch { }
    }

    public override void _Process(double delta)
    {
        if (_pendingReleases.Count > 0)
        {
            foreach (var action in _pendingReleases)
            {
                if (InputMap.HasAction(action))
                    Input.ActionRelease(action);
            }
            _pendingReleases.Clear();
        }

        while (_queue.TryDequeue(out var req))
        {
            try
            {
                req.Result = HandleRequest(req.Context);
            }
            catch (Exception ex)
            {
                req.Result = ResponseData.Error(500, ex.Message);
            }
            req.Done.Set();
        }

        if (_quitCountdown > 0)
        {
            _quitCountdown--;
            if (_quitCountdown == 0)
                GetTree().Quit(_quitExitCode);
        }
    }

    private void StartListener()
    {
        var prefix = OS.GetEnvironment("GODOT_AGENT_REST_PREFIX");
        if (string.IsNullOrEmpty(prefix)) prefix = DefaultPrefix;

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
        }
        catch (Exception ex)
        {
            GD.PushError($"[AgentRestServer] failed to start on {prefix}: {ex.Message}");
            _listener = null;
            return;
        }

        _cts = new CancellationTokenSource();
        _listenerTask = Task.Run(() => ListenLoop(_cts.Token));
        GD.Print($"[AgentRestServer] Listening on {prefix}");
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch
            {
                break;
            }

            var pending = new PendingRequest(ctx);
            _queue.Enqueue(pending);

            try { pending.Done.Wait(ct); }
            catch (OperationCanceledException) { break; }

            try
            {
                var resp = ctx.Response;
                resp.StatusCode = pending.Result.StatusCode;
                resp.ContentType = pending.Result.ContentType;
                var bytes = pending.Result.Body;
                resp.ContentLength64 = bytes.Length;
                await resp.OutputStream.WriteAsync(bytes, ct);
            }
            catch (Exception ex)
            {
                GD.PushError("[AgentRestServer] response write failed: " + ex.Message);
            }
            finally
            {
                try { ctx.Response.Close(); } catch { }
            }
        }
    }

    private ResponseData HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "";
        var method = ctx.Request.HttpMethod;

        if (method == "GET" && path == "/status") return HandleStatus();
        if (method == "POST" && path == "/input/action") return HandleInputAction(ctx.Request);
        if (method == "POST" && path == "/input/click") return HandleInputClick(ctx.Request);
        if (method == "GET" && path == "/nodes") return HandleNodes(ctx.Request);
        if (method == "GET" && path == "/screenshot") return HandleScreenshot(ctx.Request);
        if (method == "GET" && path == "/ui/controls") return HandleUiControls();
        if (method == "POST" && path == "/ui/press") return HandleUiPress(ctx.Request);
        if (method == "GET" && path == "/example/state") return HandleExampleState();
        if (method == "GET" && path == "/display/state") return HandleDisplayState();
        if (method == "POST" && path == "/display/set") return HandleDisplaySet(ctx.Request);
        if ((method == "POST" || method == "GET") && path == "/quit") return HandleQuit(ctx.Request);

        return ResponseData.Error(404, $"unknown route {method} {path}");
    }

    private ResponseData HandleStatus()
    {
        var tree = GetTree();
        var sceneName = "";
        if (tree?.CurrentScene != null && IsInstanceValid(tree.CurrentScene))
            sceneName = tree.CurrentScene.GetPath().ToString();

        var versionString = Engine.GetVersionInfo()["string"].AsString();

        var payload = new Dictionary<string, object>
        {
            ["running"] = true,
            ["scene"] = sceneName,
            ["godot_version"] = versionString,
        };
        return ResponseData.Json(200, payload);
    }

    private ResponseData HandleInputAction(HttpListenerRequest req)
    {
        if (!TryReadJson(req, out var doc, out var err)) return err;
        string action;
        string mode;
        using (doc)
        {
            var root = doc.RootElement;
            action = root.TryGetProperty("action", out var a) && a.ValueKind == JsonValueKind.String ? a.GetString() : null;
            mode = root.TryGetProperty("mode", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
        }

        if (string.IsNullOrEmpty(action))
            return ResponseData.Error(400, "missing 'action'");
        if (string.IsNullOrEmpty(mode))
            return ResponseData.Error(400, "missing 'mode'");

        var actionName = new StringName(action);
        if (!InputMap.HasAction(actionName))
            return ResponseData.Error(400, $"unknown action '{action}'");

        switch (mode)
        {
            case "press":
                Input.ActionPress(actionName);
                _pendingReleases.Remove(actionName);
                break;
            case "release":
                Input.ActionRelease(actionName);
                _pendingReleases.Remove(actionName);
                break;
            case "tap":
                Input.ActionPress(actionName);
                _pendingReleases.Add(actionName);
                break;
            default:
                return ResponseData.Error(400, $"unknown mode '{mode}' (expected press|release|tap)");
        }

        return ResponseData.Json(200, new Dictionary<string, object> { ["ok"] = true });
    }

    // Injects a synthetic mouse click at a viewport pixel so agents can exercise the
    // human input path (click-to-select, placement commit) that bypasses /ui and the
    // typed domain routes. Warps the mouse first so handlers reading
    // GetGlobalMousePosition() see the clicked point, then parses a press+release that
    // flows through the normal GUI -> _unhandled_input pipeline like a real click.
    private ResponseData HandleInputClick(HttpListenerRequest req)
    {
        if (!TryReadJson(req, out var doc, out var err)) return err;

        float x, y;
        string button = "left";
        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("x", out var xe) || xe.ValueKind != JsonValueKind.Number)
                return ResponseData.Error(400, "missing or non-numeric 'x'");
            if (!root.TryGetProperty("y", out var ye) || ye.ValueKind != JsonValueKind.Number)
                return ResponseData.Error(400, "missing or non-numeric 'y'");
            x = xe.GetSingle();
            y = ye.GetSingle();
            if (root.TryGetProperty("button", out var be) && be.ValueKind == JsonValueKind.String)
                button = be.GetString();
        }

        MouseButton index = button switch
        {
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            "left" => MouseButton.Left,
            _ => MouseButton.None,
        };
        if (index == MouseButton.None)
            return ResponseData.Error(400, $"unknown button '{button}' (expected left|right|middle)");

        var viewport = GetViewport();
        if (viewport == null) return ResponseData.Error(500, "no viewport");

        var pos = new Vector2(x, y);
        // Coordinates are VIEWPORT pixels — the same space /screenshot captures.
        // Events go through Viewport.PushInput with inLocalCoords: true rather than
        // Input.ParseInputEvent: parsed OS events need a real window and a valid
        // window->content stretch transform, neither of which exists headless, while
        // local-coords PushInput enters the same GUI -> _unhandled_input pipeline
        // directly in both modes and at any content scale factor. The leading motion
        // event establishes GUI hover state, which BaseButton requires before a
        // release emits `pressed` — real clicks are always preceded by motion. The
        // warp keeps the OS cursor (and GetGlobalMousePosition readers) roughly in
        // sync when windowed; it is a no-op headless.
        Input.WarpMouse(pos);
        viewport.PushInput(new InputEventMouseMotion
        {
            Position = pos,
            GlobalPosition = pos,
        }, true);
        viewport.PushInput(new InputEventMouseButton
        {
            ButtonIndex = index,
            Pressed = true,
            Position = pos,
            GlobalPosition = pos,
        }, true);
        viewport.PushInput(new InputEventMouseButton
        {
            ButtonIndex = index,
            Pressed = false,
            Position = pos,
            GlobalPosition = pos,
        }, true);

        return ResponseData.Json(200, new Dictionary<string, object>
        {
            ["ok"] = true,
            ["x"] = x,
            ["y"] = y,
            ["button"] = button,
        });
    }

    private ResponseData HandleQuit(HttpListenerRequest req)
    {
        int code = 0;
        if (req.ContentLength64 > MaxBodyBytes)
            return ResponseData.Error(413, "body too large");

        var codeQuery = req.QueryString["code"];
        if (!string.IsNullOrEmpty(codeQuery))
        {
            if (!int.TryParse(codeQuery, out code))
                return ResponseData.Error(400, $"invalid 'code' query value '{codeQuery}'");
        }
        else if (req.ContentLength64 > 0)
        {
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
                body = reader.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number)
                        code = c.GetInt32();
                }
                catch (JsonException ex)
                {
                    return ResponseData.Error(400, "invalid json: " + ex.Message);
                }
            }
        }

        _quitExitCode = code;
        _quitCountdown = 5;
        GD.Print($"[AgentRestServer] /quit received; shutting down with exit code {code}");
        return ResponseData.Json(200, new Dictionary<string, object>
        {
            ["ok"] = true,
            ["exit_code"] = code,
        });
    }

    private ResponseData HandleNodes(HttpListenerRequest req)
    {
        var group = req.QueryString["group"];
        if (string.IsNullOrEmpty(group))
            return ResponseData.Error(400, "missing 'group' query parameter");

        var tree = GetTree();
        if (tree == null) return ResponseData.Json(200, new List<object>());

        var nodes = tree.GetNodesInGroup(group);
        var result = new List<object>(nodes.Count);
        foreach (var node in nodes)
        {
            if (!IsInstanceValid(node)) continue;
            result.Add(SerializeNode(node));
        }
        return ResponseData.Json(200, result);
    }

    private ResponseData HandleScreenshot(HttpListenerRequest req)
    {
        var viewport = GetViewport();
        if (viewport == null) return ResponseData.Error(500, "no viewport");

        var texture = viewport.GetTexture();
        if (texture == null) return ResponseData.Error(500, "no viewport texture");

        var image = texture.GetImage();
        if (image == null || image.IsEmpty())
            return ResponseData.Error(500, "failed to read viewport image");

        var format = (req.QueryString["format"] ?? "png").ToLowerInvariant();
        byte[] bytes;
        string contentType;
        switch (format)
        {
            case "png":
                bytes = image.SavePngToBuffer();
                contentType = "image/png";
                break;
            case "jpg":
            case "jpeg":
            {
                float quality = 0.75f;
                var qStr = req.QueryString["quality"];
                if (!string.IsNullOrEmpty(qStr))
                {
                    if (!float.TryParse(qStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out quality))
                        return ResponseData.Error(400, $"invalid 'quality' value '{qStr}'");
                    quality = Math.Clamp(quality, 0.01f, 1.0f);
                }
                bytes = image.SaveJpgToBuffer(quality);
                contentType = "image/jpeg";
                break;
            }
            case "webp":
                bytes = image.SaveWebpToBuffer();
                contentType = "image/webp";
                break;
            default:
                return ResponseData.Error(400, $"unknown format '{format}' (expected png|jpg|webp)");
        }

        if (bytes == null || bytes.Length == 0)
            return ResponseData.Error(500, "failed to encode screenshot");

        return ResponseData.Binary(200, contentType, bytes);
    }

    // ---- UI agent-id routes -------------------------------------------------

    private void CollectAgentControlsRecursive(Node node, List<BaseButton> result)
    {
        if (node == null || !IsInstanceValid(node)) return;
        if (node is BaseButton btn && node.HasMeta(AgentIdMeta))
            result.Add(btn);
        foreach (var child in node.GetChildren())
            CollectAgentControlsRecursive(child, result);
    }

    private List<BaseButton> CollectAgentControls()
    {
        var list = new List<BaseButton>();
        var scene = GetTree()?.CurrentScene;
        if (scene == null || !IsInstanceValid(scene)) return list;
        CollectAgentControlsRecursive(scene, list);
        return list;
    }

    private static Dictionary<string, object> SerializeAgentControl(BaseButton btn)
    {
        var entry = new Dictionary<string, object>
        {
            ["id"] = btn.GetMeta(AgentIdMeta).AsString(),
            ["type"] = btn.GetType().Name,
            ["disabled"] = btn.Disabled,
            ["visible"] = btn.IsVisibleInTree(),
        };
        if (btn is Button b) entry["text"] = b.Text;
        if (btn.ToggleMode) entry["pressed"] = btn.ButtonPressed;
        return entry;
    }

    private ResponseData HandleUiControls()
    {
        var nodes = CollectAgentControls();
        var result = new List<object>(nodes.Count);
        foreach (var n in nodes) result.Add(SerializeAgentControl(n));
        return ResponseData.Json(200, result);
    }

    private ResponseData HandleUiPress(HttpListenerRequest req)
    {
        if (!TryReadJson(req, out var doc, out var err)) return err;
        using (doc)
        {
            string id = null;
            if (doc.RootElement.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                id = idEl.GetString();
            if (string.IsNullOrEmpty(id))
                return ResponseData.Error(400, "missing or empty 'id'");

            var nodes = CollectAgentControls();
            var matches = new List<BaseButton>();
            foreach (var n in nodes)
            {
                if (n.GetMeta(AgentIdMeta).AsString() == id) matches.Add(n);
            }
            if (matches.Count == 0)
                return ResponseData.Error(404, $"no control with id '{id}'");
            if (matches.Count > 1)
                return ResponseData.Error(400, $"ambiguous id '{id}' matches {matches.Count} controls");

            var btn = matches[0];
            if (btn.Disabled) return ResponseData.Error(400, $"control '{id}' is disabled");
            if (!btn.IsVisibleInTree()) return ResponseData.Error(400, $"control '{id}' is not visible");

            if (btn.ToggleMode) btn.ButtonPressed = !btn.ButtonPressed;
            btn.EmitSignal(BaseButton.SignalName.Pressed);

            return ResponseData.Json(200, new Dictionary<string, object>
            {
                ["ok"] = true,
                ["id"] = id,
            });
        }
    }

    // ---- Typed domain interface — Example stub -----------------------------
    //
    // Pattern for adding a typed domain surface (inventory, quest log, NPC dialogue,
    // a level editor, etc.). To wire one up:
    //
    //   1. Copy `IAgentExample.cs` to `IAgent<YourDomain>.cs` and rename `GetExampleState`.
    //   2. Implement it on the active scene controller; that controller calls
    //      `AddToGroup("agent_<your_domain>")` in `_Ready`.
    //   3. Add a `FindYourDomain()` helper + route group below, mirroring `FindExample` /
    //      `HandleExampleState`. Mutation routes should return `{ok, state, error?}`
    //      where `state` is the full snapshot after the operation.
    //
    // The /example/state route stays in the template as a self-documenting stub; you can
    // delete it once you've added real domain routes.

    private IAgentExample FindExample()
    {
        var tree = GetTree();
        if (tree == null) return null;
        foreach (var node in tree.GetNodesInGroup(AgentExampleGroup))
        {
            if (node is IAgentExample e && IsInstanceValid(node)) return e;
        }
        return null;
    }

    private ResponseData HandleExampleState()
    {
        var example = FindExample();
        if (example == null)
            return ResponseData.Error(503, $"no active example; expected a node in group '{AgentExampleGroup}' implementing IAgentExample");
        return ResponseData.Json(200, example.GetExampleState());
    }

    // ---- Typed domain interface — Display (global UI scale) ----------------
    //
    // Unlike the scene-controller-backed /example pattern, this surface is backed by
    // the DisplayScale AUTOLOAD, so it is always present for the whole process and
    // never 503s in practice — the canonical example of an always-present typed surface.

    private IAgentDisplay FindDisplay()
    {
        var tree = GetTree();
        if (tree == null) return null;
        foreach (var node in tree.GetNodesInGroup(AgentDisplayGroup))
        {
            if (node is IAgentDisplay d && IsInstanceValid(node)) return d;
        }
        return null;
    }

    private const string NoDisplayError = "no display surface; expected the DisplayScale autoload in group 'agent_display' implementing IAgentDisplay";

    private ResponseData HandleDisplayState()
    {
        var display = FindDisplay();
        if (display == null) return ResponseData.Error(503, NoDisplayError);
        return ResponseData.Json(200, display.GetDisplayState());
    }

    private ResponseData HandleDisplaySet(HttpListenerRequest req)
    {
        var display = FindDisplay();
        if (display == null) return ResponseData.Error(503, NoDisplayError);
        if (!TryReadJson(req, out var doc, out var err)) return err;

        float? factor = null;
        bool auto = false;
        using (doc)
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("factor", out var fe) && fe.ValueKind == JsonValueKind.Number)
                factor = fe.GetSingle();
            if (root.TryGetProperty("auto", out var ae) &&
                (ae.ValueKind == JsonValueKind.True || ae.ValueKind == JsonValueKind.False))
                auto = ae.GetBoolean();
        }

        var result = display.SetScale(factor, auto);
        var payload = new Dictionary<string, object>
        {
            ["ok"] = result.ok,
            ["state"] = display.GetDisplayState(),
        };
        if (!result.ok) payload["error"] = result.error;
        return ResponseData.Json(result.ok ? 200 : 400, payload);
    }

    // -------------------------------------------------------------------------

    private static Dictionary<string, object> SerializeNode(Node node)
    {
        var entry = new Dictionary<string, object>
        {
            ["path"] = node.GetPath().ToString(),
            ["name"] = node.Name.ToString(),
            ["type"] = node.GetType().Name,
        };

        switch (node)
        {
            case Node2D n2d:
                entry["position"] = new Dictionary<string, object>
                {
                    ["x"] = n2d.Position.X,
                    ["y"] = n2d.Position.Y,
                };
                break;
            case Node3D n3d:
                entry["position"] = new Dictionary<string, object>
                {
                    ["x"] = n3d.Position.X,
                    ["y"] = n3d.Position.Y,
                    ["z"] = n3d.Position.Z,
                };
                break;
        }

        if (node is IAgentInspectable inspectable)
        {
            var props = inspectable.GetAgentProperties();
            if (props != null && props.Count > 0)
                entry["properties"] = props;
        }

        return entry;
    }

    private static bool TryReadJson(HttpListenerRequest req, out JsonDocument doc, out ResponseData errorResponse)
    {
        doc = null;
        errorResponse = default;
        if (req.ContentLength64 > MaxBodyBytes)
        {
            errorResponse = ResponseData.Error(413, "body too large");
            return false;
        }
        string body;
        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
            body = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(body)) body = "{}";
        try
        {
            doc = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException ex)
        {
            errorResponse = ResponseData.Error(400, "invalid json: " + ex.Message);
            return false;
        }
    }

    private sealed class PendingRequest
    {
        public HttpListenerContext Context { get; }
        public ManualResetEventSlim Done { get; } = new(false);
        public ResponseData Result { get; set; } = ResponseData.Error(500, "no handler");

        public PendingRequest(HttpListenerContext ctx) { Context = ctx; }
    }

    private readonly struct ResponseData
    {
        public int StatusCode { get; }
        public string ContentType { get; }
        public byte[] Body { get; }

        private ResponseData(int code, string contentType, byte[] body)
        {
            StatusCode = code;
            ContentType = contentType;
            Body = body;
        }

        public static ResponseData Json(int code, object payload)
            => new(code, "application/json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, payload.GetType())));

        public static ResponseData Error(int code, string message)
            => Json(code, new Dictionary<string, object> { ["error"] = message });

        public static ResponseData Binary(int code, string contentType, byte[] body)
            => new(code, contentType, body);
    }
}
