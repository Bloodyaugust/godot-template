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
                resp.ContentType = "application/json";
                var bytes = Encoding.UTF8.GetBytes(pending.Result.Body);
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
        if (method == "GET" && path == "/nodes") return HandleNodes(ctx.Request);
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
        if (req.ContentLength64 > MaxBodyBytes)
            return ResponseData.Error(413, "body too large");

        string body;
        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
            body = reader.ReadToEnd();

        string action;
        string mode;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            action = root.TryGetProperty("action", out var a) ? a.GetString() : null;
            mode = root.TryGetProperty("mode", out var m) ? m.GetString() : null;
        }
        catch (JsonException ex)
        {
            return ResponseData.Error(400, "invalid json: " + ex.Message);
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
        public string Body { get; }

        private ResponseData(int code, string body)
        {
            StatusCode = code;
            Body = body;
        }

        public static ResponseData Json(int code, object payload)
            => new(code, JsonSerializer.Serialize(payload, payload.GetType()));

        public static ResponseData Error(int code, string message)
            => new(code, JsonSerializer.Serialize(new Dictionary<string, object> { ["error"] = message }));
    }
}
