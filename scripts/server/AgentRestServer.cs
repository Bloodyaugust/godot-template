using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
        SizeHeadlessWindow();
        StartListener();
    }

    // The headless display server starts the root window at a 64x64 placeholder (and
    // ignores --resolution), which collapses any real layout: anchored panels overflow and
    // /ui/controls rects land off-screen. Headless runs are agent test runs, so give the
    // window the project's configured viewport size — the same layout a windowed boot
    // gets — keeping click/rect coordinates meaningful.
    private void SizeHeadlessWindow()
    {
        if (DisplayServer.GetName() != "headless") return;
        var size = new Vector2I(
            (int)ProjectSettings.GetSetting("display/window/size/viewport_width"),
            (int)ProjectSettings.GetSetting("display/window/size/viewport_height"));
        GetWindow().Size = size;
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
        if (method == "POST" && path == "/input/click") return HandleInputClick(ctx.Request);
        if (method == "POST" && path == "/input/move") return HandleInputMove(ctx.Request);
        if (method == "POST" && path == "/input/drag") return HandleInputDrag(ctx.Request);
        if (method == "GET" && path == "/nodes") return HandleNodes(ctx.Request);
        if (method == "GET" && path == "/screenshot") return HandleScreenshot(ctx.Request);
        if (method == "GET" && path == "/ui/controls") return HandleUiControls();
        if (method == "GET" && path == "/example/state") return State(Example);
        if (method == "GET" && path == "/display/state") return State(Display);
        if (method == "GET" && path == "/debug/state") return State(Debug);
        if (method == "GET" && path == "/camera/state") return State(Camera);
        if (method == "GET" && path == "/transition/state") return State(TransitionSurface);
        if (method == "GET" && path == "/session/state") return State(Session);
        if (method == "GET" && path == "/demo/state") return State(Demo);
        if (method == "GET" && path == "/devlab/state") return State(DevLab);
        if (method == "GET" && path == "/content/state") return State(ContentSurface);
        bool getOrPost = method == "POST" || method == "GET";
        if (getOrPost && path == "/input/action") return HandleInputAction(ctx.Request);
        if (getOrPost && path == "/ui/press") return HandleUiPress(ctx.Request);
        if (getOrPost && path == "/display/set") return HandleDisplaySet(ctx.Request);
        if (getOrPost && path == "/display/window") return HandleDisplayWindow(ctx.Request);
        if (getOrPost && path == "/debug/set") return HandleDebugSet(ctx.Request);
        if (getOrPost && path == "/camera/zoom") return HandleCameraZoom(ctx.Request);
        if (getOrPost && path == "/transition/skip") return Route(ctx.Request, TransitionSurface, Array.Empty<string>(), (t, _) => t.SkipHold());
        if (getOrPost && path == "/transition/demo") return HandleTransitionDemo(ctx.Request);
        if (getOrPost && path == "/devlab/reroll") return Route(ctx.Request, DevLab, Array.Empty<string>(), (l, _) => l.Reroll());
        if (getOrPost && path == "/devlab/seed") return HandleDevLabSeed(ctx.Request);
        if (getOrPost && path == "/quit") return HandleQuit(ctx.Request);

        return ResponseData.Error(404, $"unknown route {method} {path}");
    }

    private ResponseData HandleStatus()
    {
        var tree = GetTree();
        var sceneName = "";
        if (tree?.CurrentScene != null && IsInstanceValid(tree.CurrentScene))
            sceneName = tree.CurrentScene.GetPath().ToString();

        var versionString = Engine.GetVersionInfo()["string"].AsString();

        // GameVersion is engine-free, so the debug `-dev` suffix (a dev build must
        // never claim to be the release of the same number) is applied here.
        var gameVersion = godottemplate.Core.GameVersion.Current + (OS.IsDebugBuild() ? "-dev" : "");

        var window = GetWindow();
        var payload = new Dictionary<string, object>
        {
            ["running"] = true,
            ["scene"] = sceneName,
            ["godot_version"] = versionString,
            ["game_version"] = gameVersion,
            // Window pixels (/ui/controls rects and click coords are these divided by the
            // DisplayScale factor) — lets a headless scenario confirm the boot sized it.
            ["window"] = new Dictionary<string, object>
            {
                ["width"] = window?.Size.X ?? 0,
                ["height"] = window?.Size.Y ?? 0,
            },
        };
        return ResponseData.Json(200, payload);
    }

    private ResponseData HandleInputAction(HttpListenerRequest req)
    {
        // `GET /input/action?action=&mode=`, or the same fields in a POST body (the query wins).
        if (!TryReadFields(req, new[] { "action", "mode" }, out var f, out var err)) return err;
        var action = f.GetValueOrDefault("action");
        var mode = f.GetValueOrDefault("mode");

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

    // ---- Synthetic pointer input (/input/click, /input/move, /input/drag) ----
    //
    // Coordinates are canvas pixels — the /ui/controls rect space (/screenshot pixels
    // divided by the DisplayScale factor). Events go through Viewport.PushInput with
    // inLocalCoords: true rather than Input.ParseInputEvent: parsed OS events need a real
    // window and a valid window->content stretch transform, neither of which exists
    // headless, while local-coords PushInput enters the same GUI -> _unhandled_input
    // pipeline directly in both modes and at any content scale factor. A leading motion
    // event establishes GUI hover state, which BaseButton requires before a release emits
    // `pressed` — real clicks are always preceded by motion. The warp keeps the OS cursor
    // (and GetGlobalMousePosition readers) roughly in sync when windowed; it is a no-op
    // headless.

    private void PushMotion(Vector2 at, Vector2 relative = default, MouseButtonMask mask = 0)
        => GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = at,
            GlobalPosition = at,
            Relative = relative,
            ButtonMask = mask,
        }, true);

    private void PushButton(Vector2 at, MouseButton index, bool pressed, MouseButtonMask mask = 0)
        => GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = index,
            Pressed = pressed,
            Position = at,
            GlobalPosition = at,
            ButtonMask = mask,
            Factor = 1f, // a wheel step's strength; ignored for real buttons
        }, true);

    // Synthetic click (or one wheel step) at a canvas pixel: motion, press, release.
    private ResponseData HandleInputClick(HttpListenerRequest req)
    {
        if (!TryReadFields(req, new[] { "x", "y", "button" }, out var f, out var err)) return err;
        if (!TryRequireFloat(f, "x", out var x, out err) || !TryRequireFloat(f, "y", out var y, out err)) return err;
        var button = f.GetValueOrDefault("button", "left");
        MouseButton index = button switch
        {
            "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            "wheel_up" => MouseButton.WheelUp,
            "wheel_down" => MouseButton.WheelDown,
            _ => MouseButton.None,
        };
        if (index == MouseButton.None)
            return ResponseData.Error(400, $"unknown button '{button}' (expected left|right|middle|wheel_up|wheel_down)");
        if (GetViewport() == null) return ResponseData.Error(500, "no viewport");

        var pos = new Vector2(x, y);
        Input.WarpMouse(pos);
        PushMotion(pos);
        PushButton(pos, index, true);
        PushButton(pos, index, false);
        return ResponseData.Json(200, new Dictionary<string, object>
        {
            ["ok"] = true,
            ["x"] = x,
            ["y"] = y,
            ["button"] = button,
        });
    }

    // Pointer motion only (no button), so hover state — highlights at once, tooltips after
    // their delay — builds exactly as for a real pointer.
    private ResponseData HandleInputMove(HttpListenerRequest req)
    {
        if (!TryReadFields(req, new[] { "x", "y" }, out var f, out var err)) return err;
        if (!TryRequireFloat(f, "x", out var x, out err) || !TryRequireFloat(f, "y", out var y, out err)) return err;
        if (GetViewport() == null) return ResponseData.Error(500, "no viewport");
        var pos = new Vector2(x, y);
        Input.WarpMouse(pos);
        PushMotion(pos);
        return ResponseData.Json(200, new Dictionary<string, object> { ["ok"] = true, ["x"] = x, ["y"] = y });
    }

    // Press-drag-release from one canvas pixel to another, the end given as to_x/to_y or as
    // an offset dx/dy from the start (so scenarios can drag from a captured rect without
    // arithmetic): motion to the start, a press, `steps` motion events along the straight
    // line carrying the held button in their mask and their relative travel, the release.
    private ResponseData HandleInputDrag(HttpListenerRequest req)
    {
        var keys = new[] { "from_x", "from_y", "to_x", "to_y", "dx", "dy", "button", "steps" };
        if (!TryReadFields(req, keys, out var f, out var err)) return err;
        bool relative = f.ContainsKey("dx") || f.ContainsKey("dy");
        if (relative && (f.ContainsKey("to_x") || f.ContainsKey("to_y")))
            return ResponseData.Error(400, "give the end as 'to_x'+'to_y' or as 'dx'+'dy', not both");
        var names = relative ? new[] { "from_x", "from_y", "dx", "dy" } : new[] { "from_x", "from_y", "to_x", "to_y" };
        var p = new float[4];
        for (int i = 0; i < names.Length; i++)
        {
            if (!TryRequireFloat(f, names[i], out p[i], out err)) return err;
        }
        if (relative)
        {
            p[2] += p[0];
            p[3] += p[1];
        }

        var button = f.GetValueOrDefault("button", "left");
        (MouseButton index, MouseButtonMask mask) = button switch
        {
            "left" => (MouseButton.Left, MouseButtonMask.Left),
            "right" => (MouseButton.Right, MouseButtonMask.Right),
            "middle" => (MouseButton.Middle, MouseButtonMask.Middle),
            _ => (MouseButton.None, (MouseButtonMask)0),
        };
        if (index == MouseButton.None)
            return ResponseData.Error(400, $"unknown button '{button}' (expected left|right|middle)");
        var steps = ParseInt(f, "steps", out var stepsError) ?? 8;
        if (stepsError != null) return ResponseData.Error(400, stepsError);
        if (steps < 1 || steps > 100) return ResponseData.Error(400, "'steps' must be 1-100");
        if (GetViewport() == null) return ResponseData.Error(500, "no viewport");

        var from = new Vector2(p[0], p[1]);
        var to = new Vector2(p[2], p[3]);
        Input.WarpMouse(from);
        PushMotion(from);
        PushButton(from, index, true, mask);
        var last = from;
        for (int i = 1; i <= steps; i++)
        {
            var at = from.Lerp(to, (float)i / steps);
            PushMotion(at, at - last, mask);
            last = at;
        }
        Input.WarpMouse(to);
        PushButton(to, index, false);
        return ResponseData.Json(200, new Dictionary<string, object>
        {
            ["ok"] = true,
            ["from_x"] = from.X,
            ["from_y"] = from.Y,
            ["to_x"] = to.X,
            ["to_y"] = to.Y,
            ["button"] = button,
            ["steps"] = steps,
        });
    }

    // `GET /quit?code=N`, or `{"code": N}` in a POST body (the query wins).
    private ResponseData HandleQuit(HttpListenerRequest req)
    {
        if (!TryReadFields(req, new[] { "code" }, out var f, out var err)) return err;
        var code = ParseInt(f, "code", out var codeError) ?? 0;
        if (codeError != null) return ResponseData.Error(400, codeError);

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
        // A headless boot (`--headless`, or `--display-driver headless`) always runs the
        // dummy renderer, whatever --rendering-driver/--rendering-method say: nothing is
        // drawn, so there is no image to read (SubViewports included). Say so plainly.
        if (DisplayServer.GetName() == "headless")
            return ResponseData.Error(503, "headless: no viewport image (the dummy renderer draws nothing); boot windowed");

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
        // The on-screen rectangle, in the same coordinate space /input/click consumes
        // (canvas/content space — identical to window pixels at DisplayScale 1.0), so
        // click-driven scenarios can aim at a button's center without hardcoding layout.
        var rect = btn.GetGlobalRect();
        var entry = new Dictionary<string, object>
        {
            ["id"] = btn.GetMeta(AgentIdMeta).AsString(),
            ["type"] = btn.GetType().Name,
            ["disabled"] = btn.Disabled,
            ["visible"] = btn.IsVisibleInTree(),
            // GUI hover state, as /input/move (or a real pointer) builds it.
            ["hovered"] = btn.IsHovered(),
            ["rect"] = new Dictionary<string, object>
            {
                ["x"] = rect.Position.X,
                ["y"] = rect.Position.Y,
                ["width"] = rect.Size.X,
                ["height"] = rect.Size.Y,
                // Ready-made click target: Hurl captures can't do arithmetic.
                ["center_x"] = rect.GetCenter().X,
                ["center_y"] = rect.GetCenter().Y,
            },
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
        // `GET /ui/press?id=`, or `{"id": ...}` in a POST body (the query wins).
        if (!TryReadFields(req, new[] { "id" }, out var f, out var err)) return err;
        var id = f.GetValueOrDefault("id");
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

    // ---- Typed domain interfaces --------------------------------------------
    //
    // The server stays domain-agnostic: a typed surface (inventory, quest log, level
    // editor, display settings) is an IAgent<X> interface implemented by a node that joins
    // a scene-tree group. Each one is declared once as a Domain<T> — its group, its 503
    // text and its state snapshot — and its routes are one line each on the generic
    // helpers below:
    //
    //   State(domain)                          GET  -> the snapshot, or 503 when absent
    //   Route(req, domain, keys, (x, f) => ...) GET or POST -> find (503) -> read the
    //                                          fields -> run the op -> {ok, state, error?}
    //
    // Find<T>(group) and Envelope(state, result) are there for routes that need to step
    // outside that shape. To add a surface: copy IAgentExample.cs, implement it on the
    // owning node (AddToGroup in _Ready), declare its Domain<T> here, and register its
    // routes in HandleRequest. /example/state is a self-documenting stub — delete it once
    // a real domain exists.

    /// <summary>One typed surface: the group its implementer joins, the 503 text when none is up, and its snapshot.</summary>
    private sealed record Domain<T>(string Group, string Missing, Func<T, object> Snapshot) where T : class;

    private static readonly Domain<IAgentExample> Example = new(
        "agent_example",
        "no active example; expected a node in group 'agent_example' implementing IAgentExample",
        e => e.GetExampleState());

    // Backed by the DisplayScale AUTOLOAD, so unlike scene-controller-backed surfaces
    // (/example) it is present for the whole process and never 503s in practice.
    private static readonly Domain<IAgentDisplay> Display = new(
        "agent_display",
        "no display surface; expected the DisplayScale autoload in group 'agent_display' implementing IAgentDisplay",
        d => d.GetDisplayState());

    /// <summary>The live node in <paramref name="group"/> implementing <typeparamref name="T"/>, or null.</summary>
    private T Find<T>(string group) where T : class
    {
        var tree = GetTree();
        if (tree == null) return null;
        foreach (var node in tree.GetNodesInGroup(group))
        {
            if (node is T found && IsInstanceValid(node)) return found;
        }
        return null;
    }

    /// <summary>The mutation envelope: <c>{ok, state, error?}</c>, 200 on success and 400 otherwise.</summary>
    private static ResponseData Envelope(object state, (bool ok, string error) result)
    {
        var payload = new Dictionary<string, object>
        {
            ["ok"] = result.ok,
            ["state"] = state,
        };
        if (!result.ok) payload["error"] = result.error;
        return ResponseData.Json(result.ok ? 200 : 400, payload);
    }

    /// <summary>A state route: the domain's snapshot, or 503 when no implementer is up.</summary>
    private ResponseData State<T>(Domain<T> domain) where T : class
    {
        var target = Find<T>(domain.Group);
        return target == null ? ResponseData.Error(503, domain.Missing) : ResponseData.Json(200, domain.Snapshot(target));
    }

    /// <summary>
    /// A mutation route: find the implementer (503 when absent), read <paramref name="keys"/>
    /// through <see cref="TryReadFields"/> (query and body alike), run <paramref name="op"/>,
    /// and wrap the post-operation snapshot in the envelope. An op that rejects a field
    /// returns <c>(false, message)</c> like any other failure.
    /// </summary>
    private ResponseData Route<T>(HttpListenerRequest req, Domain<T> domain, string[] keys,
        Func<T, Dictionary<string, string>, (bool ok, string error)> op) where T : class
    {
        var target = Find<T>(domain.Group);
        if (target == null) return ResponseData.Error(503, domain.Missing);
        if (!TryReadFields(req, keys, out var fields, out var err)) return err;
        var result = op(target, fields);
        return Envelope(domain.Snapshot(target), result);
    }

    // `GET ?factor=2.0` / `?auto=true`, or the same fields in a POST body.
    private ResponseData HandleDisplaySet(HttpListenerRequest req)
        => Route(req, Display, new[] { "factor", "auto" }, (d, f) =>
        {
            var factor = ParseFloat(f, "factor", out var error);
            if (error != null) return (false, error);
            var auto = ParseBool(f, "auto", out error);
            if (error != null) return (false, error);
            return d.SetScale(factor, auto == true);
        });

    // `GET ?mode=windowed|fullscreen`, or the same field in a POST body.
    private ResponseData HandleDisplayWindow(HttpListenerRequest req)
        => Route(req, Display, new[] { "mode" }, (d, f) => d.SetWindowMode(f.GetValueOrDefault("mode")));

    // Backed by the DebugView AUTOLOAD (always present, like /display). In a release build the
    // flag stays off and setting it is a 400 — though this server is debug-only anyway.
    private static readonly Domain<IAgentDebug> Debug = new(
        "agent_debug",
        "no debug surface; expected the DebugView autoload in group 'agent_debug' implementing IAgentDebug",
        d => d.GetDebugState());

    // `GET ?enabled=true|false`, or the same field in a POST body.
    private ResponseData HandleDebugSet(HttpListenerRequest req)
        => Route(req, Debug, new[] { "enabled" }, (d, f) =>
        {
            var enabled = ParseBool(f, "enabled", out var error);
            if (error != null) return (false, error);
            if (enabled == null) return (false, "expected 'enabled' (true|false)");
            return d.SetDebug(enabled.Value);
        });

    // Backed by a ZoomCamera in the current scene, so it 503s when none is up.
    private static readonly Domain<IAgentCamera> Camera = new(
        "agent_camera",
        "no zoom camera; expected a node in group 'agent_camera' implementing IAgentCamera",
        c => c.GetCameraState());

    // `GET ?level=<zoom>&steps=<int>` (either or both), or the same fields in a POST body.
    private ResponseData HandleCameraZoom(HttpListenerRequest req)
        => Route(req, Camera, new[] { "level", "steps" }, (c, f) =>
        {
            var level = ParseFloat(f, "level", out var error);
            if (error != null) return (false, error);
            var steps = ParseInt(f, "steps", out error);
            if (error != null) return (false, error);
            return c.SetZoom(level, steps);
        });

    // Backed by the Transition AUTOLOAD (always present, like /display).
    private static readonly Domain<IAgentTransition> TransitionSurface = new(
        "agent_transition",
        "no transition overlay; expected the Transition autoload in group 'agent_transition' implementing IAgentTransition",
        t => t.GetTransitionState());

    // `GET ?text=<loading text>&seconds=<0-60>` (either or neither), or the same fields in a POST body.
    private ResponseData HandleTransitionDemo(HttpListenerRequest req)
        => Route(req, TransitionSurface, new[] { "text", "seconds" }, (t, f) =>
        {
            var seconds = ParseFloat(f, "seconds", out var error);
            if (error != null) return (false, error);
            return t.Demo(f.GetValueOrDefault("text"), seconds);
        });

    // Backed by the GameSession AUTOLOAD (always present); read-only — the scenes write the carrier.
    private static readonly Domain<IAgentSession> Session = new(
        "agent_session",
        "no session carrier; expected the GameSession autoload in group 'agent_session' implementing IAgentSession",
        s => s.GetSessionState());

    // Scene-backed by EITHER demo scene (the layered snapshot): 503 elsewhere.
    private static readonly Domain<IAgentDemo> Demo = new(
        "agent_demo",
        "no scene-handoff demo scene; expected scenes/main.tscn or scenes/demo/destination.tscn (group 'agent_demo', IAgentDemo)",
        d => d.GetDemoState());

    // Backed by the ContentBootstrap AUTOLOAD (always present, like /display); read-only. A
    // failed boot load is in the state (`loaded`, `error`), never a 503.
    private static readonly Domain<IAgentContent> ContentSurface = new(
        "agent_content",
        "no content surface; expected the ContentBootstrap autoload in group 'agent_content' implementing IAgentContent",
        c => c.GetContentState());

    // Scene-backed by the DEV-ONLY lab, booted by path and never reached from the game: its 503
    // names the scene to boot (the dev-only scenes' "scene-hinting 503").
    private static readonly Domain<IAgentDevLab> DevLab = new(
        "agent_devlab",
        $"no dev lab; boot {godottemplate.UI.GameScenes.DevOnlyLab} (godot-mono --path . {godottemplate.UI.GameScenes.DevOnlyLab}) — a dev-only scene (group 'agent_devlab', IAgentDevLab)",
        l => l.GetDevLabState());

    // `GET ?seed=<uint64>`, or the same field in a POST body (a string, like every 64-bit seed).
    private ResponseData HandleDevLabSeed(HttpListenerRequest req)
        => Route(req, DevLab, new[] { "seed" }, (l, f) =>
        {
            if (!f.TryGetValue("seed", out var raw)) return (false, "missing 'seed' (a decimal uint64)");
            if (!ulong.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
                return (false, $"invalid 'seed' value '{raw}' (expected a decimal uint64)");
            return l.SetSeed(seed);
        });

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

    // ---- Request fields (the GET-or-POST idiom) -------------------------------

    /// <summary>
    /// Merges a JSON POST body and the query string into one string map (the query wins),
    /// so a route reads its fields the same way whichever form the caller used — the
    /// helper behind "bodyless mutations accept GET". Only <paramref name="keys"/> are
    /// read; JSON strings come through as-is, booleans as <c>true</c>/<c>false</c>, and
    /// anything else as its raw JSON text. Parse the values with
    /// <see cref="ParseBool"/> / <see cref="ParseInt"/> / <see cref="ParseFloat"/>.
    /// </summary>
    private static bool TryReadFields(HttpListenerRequest req, string[] keys, out Dictionary<string, string> fields, out ResponseData err)
    {
        fields = new Dictionary<string, string>();
        err = default;
        if (req.HttpMethod == "POST" && req.ContentLength64 != 0)
        {
            if (!TryReadJson(req, out var doc, out err)) return false;
            using (doc)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var k in keys)
                    {
                        if (!doc.RootElement.TryGetProperty(k, out var el) || el.ValueKind == JsonValueKind.Null) continue;
                        fields[k] = el.ValueKind switch
                        {
                            JsonValueKind.String => el.GetString(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => el.GetRawText(),
                        };
                    }
                }
            }
        }
        foreach (var k in keys)
        {
            var v = req.QueryString[k];
            if (!string.IsNullOrEmpty(v)) fields[k] = v;
        }
        return true;
    }

    /// <summary>An optional boolean field: null when absent, an error when it isn't true|false.</summary>
    private static bool? ParseBool(Dictionary<string, string> f, string key, out string error)
    {
        error = null;
        if (!f.TryGetValue(key, out var v)) return null;
        if (bool.TryParse(v, out var b)) return b;
        error = $"invalid '{key}' value '{v}' (expected true|false)";
        return null;
    }

    /// <summary>An optional integer field: null when absent, an error when it doesn't parse.</summary>
    private static int? ParseInt(Dictionary<string, string> f, string key, out string error)
    {
        error = null;
        if (!f.TryGetValue(key, out var v)) return null;
        if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) return n;
        error = $"invalid '{key}' value '{v}' (expected an integer)";
        return null;
    }

    /// <summary>An optional number field: null when absent, an error when it doesn't parse.</summary>
    private static float? ParseFloat(Dictionary<string, string> f, string key, out string error)
    {
        error = null;
        if (!f.TryGetValue(key, out var v)) return null;
        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && float.IsFinite(n)) return n;
        error = $"invalid '{key}' value '{v}' (expected a number)";
        return null;
    }

    /// <summary>A required number field; false with a 400 when it is missing or doesn't parse.</summary>
    private static bool TryRequireFloat(Dictionary<string, string> f, string key, out float value, out ResponseData err)
    {
        value = 0f;
        var parsed = ParseFloat(f, key, out var error);
        if (error == null && parsed == null) error = $"missing '{key}'";
        if (error != null) return Fail(out err, error);
        value = parsed.Value;
        err = default;
        return true;
    }

    /// <summary>A plain <c>{error}</c> 400, for the bool-returning parse helpers.</summary>
    private static bool Fail(out ResponseData err, string message)
    {
        err = ResponseData.Error(400, message);
        return false;
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
