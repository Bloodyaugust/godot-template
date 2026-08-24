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

## Response envelope

All JSON responses follow one of two shapes:

- **State / query routes** (`GET`s): return the resource payload directly (`/status`, `/nodes`, `/example/state`, etc.).
- **Mutation routes** (`POST`s on a domain group): return `{"ok": true|false, "state": { ... }, "error": "..." }`. `state` is the full domain snapshot after the operation; `error` is present only when `ok` is `false`. The HTTP status mirrors `ok` (200 / 400).

Generic errors (route not found, body too large, bad JSON, missing field) use `{"error": "..."}` with the appropriate 4xx / 5xx status.

Keep new routes consistent with these shapes so a single client helper can handle every response.

**Bodyless mutations accept `GET`.** Windows' HTTP.sys rejects a bodyless `POST` with `411 Length Required` before the application sees it. Any mutation route callable without a body therefore also accepts `GET`, mirroring its body fields as query parameters (`/quit` / `/quit?code=N` is the shipped example). Follow the idiom for every bodyless route you add — register the route for both methods and read query values first, body fields second.

## Endpoints

### `GET /status`

Sanity check.

```json
{ "running": true, "scene": "/root/Main", "godot_version": "4.7.x",
  "game_version": "0.1.0-dev" }
```

`game_version` is `GameVersion.Current` (`scripts/core/GameVersion.cs`), suffixed
`-dev` in debug builds so a dev build never claims to be the release of the same
number.

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

### `POST /input/click`

Injects a synthetic mouse click at a viewport pixel — for exercising the **human
input path** that bypasses `/ui/*` and the typed domain routes (e.g. click-to-select
an actor, committing a placement, right-click to cancel).

```json
{ "x": 687.6, "y": 324, "button": "left" }
```

`button` is `left` (default), `right`, or `middle`. The handler pushes a mouse-motion
event (establishing GUI hover state, as real clicks do) followed by a press + release
through the viewport's input pipeline (`Viewport.PushInput`, local coords), so the
click flows through the normal GUI → `_unhandled_input` path exactly like a real one —
`Control`s with `mouse_filter = Stop` still block clicks from reaching the world
behind them. It works **headless** and at any `ContentScaleFactor`; when windowed, the
OS cursor is also warped to `(x, y)` so handlers reading `GetGlobalMousePosition()`
stay in sync.

Coordinates are **viewport pixels** — the same space `GET /screenshot` captures, so
pixel positions read off a screenshot can be clicked directly. To click a *world*
position in a game with a moving camera, expose the camera position, zoom, and
viewport size through a typed domain route and map world → screen on the client:

```
screen = viewport_size / 2 + (world - camera_pos) * zoom
```

Responses: `200 {"ok": true, "x", "y", "button"}`; `400` for a missing/non-numeric
coordinate or an unknown button.

### `GET /screenshot`

Captures the current main viewport as an image and returns the raw encoded bytes — nothing is written to the game's filesystem. Optional query params:

- `format` — `png` (default), `jpg`/`jpeg`, or `webp`.
- `quality` — float in `[0.01, 1.0]`, only honored for `jpg`. Defaults to `0.75`.

Responses:

- `200` with body bytes and `Content-Type: image/png|image/jpeg|image/webp` on success.
- `400 {"error": "..."}` for an unknown `format` or unparseable `quality`.
- `500 {"error": "..."}` if the viewport image is unavailable or encoding fails.

```sh
# save a PNG to a temp file (no in-game persistence)
curl -s -o /tmp/shot.png http://127.0.0.1:8080/screenshot

# smaller JPG with custom quality
curl -s -o /tmp/shot.jpg 'http://127.0.0.1:8080/screenshot?format=jpg&quality=0.5'
```

The capture is performed on the main thread inside `_Process`, so it sees the most recently rendered frame.

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

### `/ui/*` — generic button interaction by `agent_id`

Scene-agnostic surface for agents to operate top-level buttons (main menu Play, builder Continue, future settings / credits / post-combat). Each opt-in button gets an `agent_id` node metadata entry; the server walks the **current scene** and surfaces every `BaseButton` with that meta set.

**Tagging a button in `.tscn`:**

```
[node name="QuitButton" type="Button" parent="UI"]
text = "Quit"
metadata/agent_id = "main.quit"
```

**Tagging a button in C#:**

```csharp
button.SetMeta("agent_id", "post_combat.continue");
```

#### `GET /ui/controls`

Returns the list of agent-tagged buttons in the active scene:

```json
[
  { "id": "main.quit", "type": "Button", "text": "Quit",
    "disabled": false, "visible": true,
    "rect": { "x": 16.0, "y": 16.0, "width": 80.0, "height": 34.0,
              "center_x": 56.0, "center_y": 33.0 } }
]
```

- `pressed` is included for toggle buttons (`ToggleMode = true`).
- Disabled / hidden controls are still listed so agents can see *why* a button isn't actionable (`/ui/press` rejects them with `400` — walk the real navigation flow instead).
- `rect` is the button's on-screen rectangle in the `/input/click` coordinate
  space (canvas/content space — identical to window pixels at `DisplayScale`
  factor 1.0), with a ready-made `center_x`/`center_y` click target so Hurl
  scenarios — which can't do arithmetic in captures — aim without hardcoding
  layout (`tests/hurl/display_input.hurl`).

#### `POST /ui/press`

```json
{ "id": "main.quit" }
```

Emits the button's `Pressed` signal (for toggles, flips `ButtonPressed` first so `Toggled` fires too). Responses:

- `200 {"ok": true, "id": "..."}`
- `400` for missing/empty `id`, disabled control, hidden control, or ambiguous id
- `404` if no control matches

#### Conventions

- IDs are scene-prefixed and dot-delimited (`main.quit`, `main_menu.play`, `settings.apply`); they are the API contract, so refactor freely as long as the meta value is preserved.
- Dynamically built buttons (list rows, tabs, cards) follow the same convention with a data-derived tail — `settings.tab.<name>`, `list.select.<key>` — set in code via `button.SetMeta("agent_id", ...)`, so data-driven UIs stay agent-drivable with zero per-item code.
- Only `BaseButton` nodes participate. For richer controls, add a typed interface following the `IAgentInspectable` / `IAgentExample` patterns.
- Don't tag inner UI used by bespoke endpoints; reserve `agent_id` for top-level navigation and one-off actions.

The bundled `scenes/main.tscn` ships a `QuitButton` with `metadata/agent_id = "main.quit"` to verify this surface out of the box:

```sh
curl http://127.0.0.1:8080/ui/controls
curl -X POST http://127.0.0.1:8080/ui/press \
     -H 'Content-Type: application/json' \
     -d '{"id":"main.quit"}'
```

### `/example/*` — typed domain interface stub

`/example/state` is a self-documenting stub that demonstrates the typed-domain-interface pattern. The server resolves an active `IAgentExample` implementer via the `agent_example` scene-tree group; if none is present it returns `503`.

```json
{ "your": "domain", "state": "here" }
```

See `IAgentExample.cs` for step-by-step instructions on copying the pattern into a real domain surface (inventory, quest log, level editor, NPC dialogue, etc.) — the comments there are the canonical reference. Delete the stub once you've added at least one real domain interface.

### `/display/*` — display settings (typed domain interface)

The hi-dpi UI scale and window mode, backed by the `DisplayScale` autoload (group
`agent_display`, `IAgentDisplay` — see `scripts/ui/README.md`). Both settings persist
through `PlayerProfile` (`scripts/session/`). Because the autoload runs for the whole
process, these routes are **always available** — the canonical example of an
*always-present* typed surface, in contrast to scene-controller-backed surfaces (like
`/example/state`) that return `503` when their scene isn't active.

#### `GET /display/state`

```json
{
  "factor": 2.75,         // effective ContentScaleFactor (override if set, else auto)
  "auto_factor": 2.75,    // monitor-derived value computed at startup
  "has_override": false,  // whether an explicit override is active
  "window_mode": "windowed", // the persisted setting: "windowed" | "fullscreen"
  "applied": 2.75,        // the value actually on the window (sanity check)
  "screen": 0,
  "screen_scale": 1.0,    // DisplayServer.ScreenGetScale (often 1.0 on X11)
  "screen_dpi": 254,      // DisplayServer.ScreenGetDpi (drives the fallback)
  "min_factor": 1.0,
  "max_factor": 3.0,
  "max_auto_factor": 2.0
}
```

#### `POST /display/set`

Set the UI scale. Supply **either** an explicit `factor` **or** `auto`:

```sh
curl -X POST http://127.0.0.1:8080/display/set -d '{"factor": 2.0}'   # explicit override (snapped to 0.25, clamped 1.0–3.0)
curl -X POST http://127.0.0.1:8080/display/set -d '{"auto": true}'    # clear override, follow the monitor
```

Returns the `{ok, state, error?}` envelope. `400` when neither field is supplied or
`factor` is out of the `[min_factor, max_factor]` range. The change applies to the live
window immediately (scales GUI + 2D canvas together) and persists to the player profile.

#### `POST /display/window`

Set the window-mode setting — `"windowed"` or `"fullscreen"` (borderless; no
exclusive mode):

```sh
curl -X POST http://127.0.0.1:8080/display/window -d '{"mode": "fullscreen"}'
```

Returns the `{ok, state, error?}` envelope; `400` on any other mode. The setting
persists to the player profile and applies to the live window immediately; the
reported `window_mode` is the *setting* — headless runs have no real window to
reflect it.

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

## Extending node payloads

Implement `IAgentInspectable` on any C# node script to contribute extra fields to `/nodes`:

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

For larger domain surfaces — anything with operations beyond "read a field" — use the typed-interface pattern via `IAgentExample` rather than packing the world into `IAgentInspectable`.

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

curl -s -o /tmp/shot.png http://127.0.0.1:8080/screenshot

curl http://127.0.0.1:8080/ui/controls
curl -X POST http://127.0.0.1:8080/ui/press \
     -H 'Content-Type: application/json' \
     -d '{"id":"main.quit"}'

curl -X POST http://127.0.0.1:8080/quit
```
