# scripts/server/

Agent-facing REST interface used by external CLIs (claude code, scripts, smoke tests) to drive a running Godot instance.

## Activation

Registered as an autoload in `project.godot`. The server:

- starts only when `OS.IsDebugBuild()` is true — release/exported builds `QueueFree` the autoload immediately
- binds `http://127.0.0.1:8080/` by default
- binds the URL in the `GODOT_AGENT_REST_PREFIX` env var instead, when set (e.g. `http://127.0.0.1:9090/`)

If the bind fails (port in use, ACL), the failure is logged via `GD.PushError` and the game continues without a listener.

## Threading

`HttpListener` runs on a background `Task`. Each request is enqueued and handled on the main thread inside `_Process`, then the response is written back from the listener thread. Handlers may freely touch the scene tree.

## Endpoints

### `GET /status`

Sanity check.

```json
{ "running": true, "scene": "/root/Main", "godot_version": "4.6.x" }
```

### `POST /input/action`

Triggers a project input-mapped action.

```json
{ "action": "move_up", "mode": "press" }
```

`mode` is one of:

- `press` — calls `Input.ActionPress` and leaves the action held until a matching `release` (or another `tap`).
- `release` — calls `Input.ActionRelease`.
- `tap` — calls `Input.ActionPress` now and `Input.ActionRelease` at the start of the next `_Process` frame, so polling-based scripts (`Input.IsActionPressed` in `_Process`) observe the press for at least one frame.

Responses:

- `200 {"ok": true}` on success
- `400 {"error": "..."}` for unknown action, unknown mode, missing field, or invalid JSON
- `413 {"error": "body too large"}` if the body exceeds 64 KB

### `POST /quit` (also accepts `GET`)

Stops the running game. Body and query are both optional:

```sh
curl -X POST http://127.0.0.1:8080/quit                    # exit code 0
curl http://127.0.0.1:8080/quit?code=2                     # exit code 2 (GET form)
curl -X POST http://127.0.0.1:8080/quit \
     -H 'Content-Type: application/json' \
     -d '{"code": 1}'                                      # exit code 1 (POST + JSON)
```

`code` is the process exit code passed to `GetTree().Quit(code)`. Defaults to `0`. Query-string `?code=N` takes precedence over a JSON body.

`GET` is supported because Windows' HTTP.sys rejects bodyless `POST` requests with `411 Length Required` before the application sees them — `GET` sidesteps this so a single `curl http://.../quit` works without flags.

The response is sent before the engine shuts down — the server holds the actual `Quit()` call back by 5 process frames so the response has time to flush. Returns `200 {"ok": true, "exit_code": 0}`.

### `GET /nodes?group=<name>`

Returns every node currently in the named group.

```json
[
  {
    "path": "/root/Main/Player",
    "name": "Player",
    "type": "Player",
    "position": { "x": 576.0, "y": 324.0 },
    "properties": { "speed": 200.0 }
  }
]
```

Field notes:

- `type` is `node.GetType().Name`. For scripted nodes that's the C# class; for plain engine nodes it's the engine class.
- `position` is `{x, y}` for `Node2D`, `{x, y, z}` for `Node3D`, omitted otherwise.
- `properties` is present only when the node implements `IAgentInspectable` (see below).

`400` if `group` is missing. An empty result is `200 []`.

## Extending node payloads

Implement `IAgentInspectable` on any C# node script to contribute extra fields:

```csharp
using godottemplate.Server;

public partial class Enemy : CharacterBody2D, IAgentInspectable
{
    [Export] public float Health { get; set; } = 100f;

    public Dictionary<string, object> GetAgentProperties() => new()
    {
        ["health"] = Health,
        ["state"] = _stateMachine.CurrentState.ToString(),
    };
}
```

Return primitive values, strings, arrays, and nested `Dictionary<string, object>` — they round-trip through `System.Text.Json` without configuration. Return `null` or an empty dict to omit the `properties` key entirely.

## Examples

```sh
curl http://127.0.0.1:8080/status

curl -X POST http://127.0.0.1:8080/input/action \
     -H 'Content-Type: application/json' \
     -d '{"action":"move_right","mode":"press"}'

curl -X POST http://127.0.0.1:8080/input/action \
     -H 'Content-Type: application/json' \
     -d '{"action":"move_right","mode":"release"}'

curl -X POST http://127.0.0.1:8080/input/action \
     -H 'Content-Type: application/json' \
     -d '{"action":"interact","mode":"tap"}'

curl 'http://127.0.0.1:8080/nodes?group=player'

curl -X POST http://127.0.0.1:8080/quit
```
