# scripts/server/

Agent-facing REST interface used by external CLIs (claude code, scripts, smoke tests) to drive a running Godot instance.

## Activation

Registered as an autoload in `project.godot`. The server:

- starts only when `OS.IsDebugBuild()` is true — release/exported builds `QueueFree` the autoload immediately
- binds `http://127.0.0.1:8080/` by default
- binds the URL in the `GODOT_AGENT_REST_PREFIX` env var instead, when set (e.g. `http://127.0.0.1:9090/`)

If the bind fails (port in use, ACL), the failure is logged via `GD.PushError` and the game continues without a listener.

On a **headless** boot the server also resizes the root window to the project's configured viewport size (`display/window/size/viewport_width`/`_height`): the headless display server starts it at a 64×64 placeholder and ignores `--resolution`, which collapses anchored layouts and pushes `/ui/controls` rects off-screen. `/status` reports the resulting `window` size.

## Threading

`HttpListener` runs on a background `Task`. Each request is enqueued and handled on the main thread inside `_Process`, then the response is written back from the listener thread. Handlers may freely touch the scene tree.

## Response envelope

All JSON responses follow one of two shapes:

- **State / query routes** (`GET`s): return the resource payload directly (`/status`, `/nodes`, `/example/state`, etc.).
- **Mutation routes** (`POST`s on a domain group): return `{"ok": true|false, "state": { ... }, "error": "..." }`. `state` is the full domain snapshot after the operation; `error` is present only when `ok` is `false`. The HTTP status mirrors `ok` (200 / 400).

Generic errors (route not found, body too large, bad JSON, missing field) use `{"error": "..."}` with the appropriate 4xx / 5xx status.

Keep new routes consistent with these shapes so a single client helper can handle every response.

**Bodyless mutations accept `GET`.** Windows' HTTP.sys rejects a bodyless `POST` with `411 Length Required` before the application sees it. Any mutation route callable without a body therefore also accepts `GET`, mirroring its body fields as query parameters (`/quit` / `/quit?code=N` is the shipped example). Follow the idiom for every bodyless route you add — register the route for both methods and read its fields through `TryReadFields`, which merges the JSON body and the query string into one string map (the query wins) so the route doesn't care which form the caller used. `ParseBool` / `ParseInt` / `ParseFloat` (and `TryRequireFloat` for required numbers) turn those strings into values with uniform `400` messages (`missing 'y'`, `invalid 'code' value 'abc' (expected an integer)`).

## Typed-domain helpers

A typed surface is declared once in `AgentRestServer.cs` as a `Domain<T>(Group, Missing, Snapshot)` — the group its implementer joins, the `503` text when none is up, and its state snapshot — and each route is one line on the generic helpers:

- `State(domain)` — `GET` snapshot, or `503 {"error": Missing}`.
- `Route(req, domain, keys, (target, fields) => (ok, error))` — find the implementer (`503` when absent), read `keys` through `TryReadFields`, run the op, and wrap the post-operation snapshot in `{ok, state, error?}`. An op that rejects a field value returns `(false, message)` like any other failure.
- `Find<T>(group)` and `Envelope(state, result)` — the building blocks, for a route that has to step outside that shape.

`/example/state`, `/display/*`, `/debug/*`, `/camera/*`, `/transition/*`, `/session/state`, `/content/state`, `/demo/state` and `/devlab/*` are the shipped users.

## Endpoints

### `GET /status`

Sanity check.

```json
{ "running": true, "scene": "/root/Main", "godot_version": "4.7.x",
  "game_version": "0.1.0-dev", "window": { "width": 1280, "height": 720 } }
```

`window` is the root window's size in pixels (see *Activation* for the headless resize).

`game_version` is `GameVersion.Current` (`scripts/core/GameVersion.cs`), suffixed
`-dev` in debug builds so a dev build never claims to be the release of the same
number.

### `POST /input/action` (or `GET /input/action?action=&mode=`)

Triggers a project input-mapped action. The fields come from a JSON POST body or the
query string (the query wins), read through the same parser as every other route.

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

```sh
curl 'http://127.0.0.1:8080/input/action?action=ui_cancel&mode=tap'
```

### `POST /input/click`

Injects a synthetic mouse click (or one wheel step) at a canvas pixel — for exercising the **human
input path** that bypasses `/ui/*` and the typed domain routes (e.g. click-to-select
an actor, committing a placement, right-click to cancel).

```json
{ "x": 687.6, "y": 324, "button": "left" }
```

`button` is `left` (default), `right`, `middle`, `wheel_up`, or `wheel_down` (one wheel step, `Factor = 1`). The handler pushes a mouse-motion
event (establishing GUI hover state, as real clicks do) followed by a press + release
through the viewport's input pipeline (`Viewport.PushInput`, local coords), so the
click flows through the normal GUI → `_unhandled_input` path exactly like a real one —
`Control`s with `mouse_filter = Stop` still block clicks from reaching the world
behind them. It works **headless** and at any `ContentScaleFactor`; when windowed, the
OS cursor is also warped to `(x, y)` so handlers reading `GetGlobalMousePosition()`
stay in sync.

Coordinates are **canvas pixels** — the `/ui/controls` rect space, which is
`/screenshot` pixels divided by the `DisplayScale` factor (identical at factor 1.0, so
pixel positions read off a screenshot can be clicked directly there). To click a *world*
position in a game with a moving camera, expose the camera position, zoom, and
viewport size through a typed domain route and map world → screen on the client:

```
screen = viewport_size / 2 + (world - camera_pos) * zoom
```

Fields may also come as query parameters (`TryReadFields`). Responses:
`200 {"ok": true, "x", "y", "button"}`; `400` for a missing/non-numeric coordinate or an
unknown button.

### `POST /input/move`

Pointer motion only — no button — at a canvas pixel, through the same pipeline as
`/input/click`. Builds GUI hover state exactly as a real pointer does (highlights at
once, tooltips after their delay); `/ui/controls` reports it as `hovered`.

```json
{ "x": 56, "y": 32 }
```

Responses: `200 {"ok": true, "x", "y"}`; `400` for a missing/non-numeric coordinate.

### `POST /input/drag`

Press, drag, release. The end is given absolutely or as an offset from the start (so a
scenario can drag from a captured rect centre without arithmetic):

```json
{ "from_x": 400, "from_y": 300, "to_x": 500, "to_y": 320 }
{ "from_x": 400, "from_y": 300, "dx": -100, "dy": 20, "button": "right", "steps": 3 }
```

`button` is `left` (default), `right`, or `middle`; `steps` is 1–100 (default 8). The
handler pushes a motion to the start, the press, `steps` motion events interpolated
along the straight line (each carrying the held button in its `ButtonMask` and its
`Relative` travel, as a real drag does), then the release at the end — same space and
pipeline as `/input/click`. Responses: `200 {"ok": true, "from_x", "from_y", "to_x",
"to_y", "button", "steps"}`; `400` for a missing/non-numeric coordinate, both end forms
at once, an unknown button, or `steps` out of range.

### `GET /screenshot`

Captures the current main viewport as an image and returns the raw encoded bytes — nothing is written to the game's filesystem. Optional query params:

- `format` — `png` (default), `jpg`/`jpeg`, or `webp`.
- `quality` — float in `[0.01, 1.0]`, only honored for `jpg`. Defaults to `0.75`.

Responses:

- `200` with body bytes and `Content-Type: image/png|image/jpeg|image/webp` on success.
- `400 {"error": "..."}` for an unknown `format` or unparseable `quality`.
- `503 {"error": "headless: no viewport image ..."}` on a headless boot (see below).
- `500 {"error": "..."}` if the viewport image is unavailable or encoding fails.

**Headless has no image.** `--headless` (or `--display-driver headless`) always runs the
dummy rendering server, whatever `--rendering-driver` / `--rendering-method` say — nothing
is drawn, so neither the root viewport nor a `SubViewport` has pixels to read (checked on
4.7: every combination returns the dummy texture). Boot windowed for screenshots
(`WINDOWED=1` / `-Windowed` with the Hurl runners).

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
    "disabled": false, "visible": true, "hovered": false,
    "rect": { "x": 16.0, "y": 16.0, "width": 80.0, "height": 34.0,
              "center_x": 56.0, "center_y": 33.0 } }
]
```

- `pressed` is included for toggle buttons (`ToggleMode = true`).
- `hovered` is the button's GUI hover state (`BaseButton.IsHovered()`), as `/input/move` or a real pointer builds it.
- Disabled / hidden controls are still listed so agents can see *why* a button isn't actionable (`/ui/press` rejects them with `400` — walk the real navigation flow instead).
- `rect` is the button's on-screen rectangle in the `/input/click` coordinate
  space (canvas/content space — identical to window pixels at `DisplayScale`
  factor 1.0), with a ready-made `center_x`/`center_y` click target so Hurl
  scenarios — which can't do arithmetic in captures — aim without hardcoding
  layout (`tests/hurl/display_input.hurl`).

#### `POST /ui/press` (or `GET /ui/press?id=`)

```json
{ "id": "main.quit" }
```

The `id` comes from a JSON POST body or the query string (the query wins).

Emits the button's `Pressed` signal (for toggles, flips `ButtonPressed` first so `Toggled` fires too). Responses:

- `200 {"ok": true, "id": "..."}`
- `400` for missing/empty `id`, disabled control, hidden control, ambiguous id, or invalid JSON
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

Its route is `State(Example)` on the typed-domain helpers above. See `IAgentExample.cs` for step-by-step instructions on copying the pattern into a real domain surface (inventory, quest log, level editor, NPC dialogue, etc.) — the comments there are the canonical reference. Delete the stub once you've added at least one real domain interface.

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
  "factor": 1.0,          // effective ContentScaleFactor (override if set, else auto)
  "auto_factor": 1.0,     // monitor-derived value (2.0 at 254 DPI), capped by pixel_cap; re-derived on resize
  "has_override": false,  // whether an explicit override is active
  "window_mode": "windowed", // the persisted setting: "windowed" | "fullscreen"
  "applied": 1.0,         // the value actually on the window (sanity check)
  "screen": 0,
  "screen_scale": 1.0,    // DisplayServer.ScreenGetScale (often 1.0 on X11)
  "screen_dpi": 254,      // DisplayServer.ScreenGetDpi (drives the fallback)
  "min_factor": 1.0,
  "max_factor": 3.0,
  "max_auto_factor": 2.0,
  "window_size": { "width": 1280, "height": 720 },  // the live window, in pixels
  "pixel_cap": 1.0,       // the largest quarter step leaving min_canvas UI pixels (never below min_factor)
  "min_canvas": { "width": 1280, "height": 720 }    // the canvas floor (design/visual/ui-scale.md)
}
```

#### `POST /display/set` (also accepts `GET`)

Set the UI scale. Supply **either** an explicit `factor` **or** `auto`, in a JSON body
or as query parameters:

```sh
curl -X POST http://127.0.0.1:8080/display/set -d '{"factor": 2.0}'   # explicit override (snapped to 0.25, clamped 1.0–3.0)
curl -X POST http://127.0.0.1:8080/display/set -d '{"auto": true}'    # clear override, follow the monitor
curl 'http://127.0.0.1:8080/display/set?factor=1.5'                   # GET form
```

Returns the `{ok, state, error?}` envelope. `400` when neither field is supplied, a
value doesn't parse (`factor` not a number, `auto` not true|false), or `factor` is out
of the `[min_factor, max_factor]` range. The change applies to the live
window immediately (scales GUI + 2D canvas together) and persists to the player profile.

#### `POST /display/window` (also accepts `GET`)

Set the window-mode setting — `"windowed"` or `"fullscreen"` (borderless; no
exclusive mode):

```sh
curl -X POST http://127.0.0.1:8080/display/window -d '{"mode": "fullscreen"}'
curl 'http://127.0.0.1:8080/display/window?mode=windowed'             # GET form
```

Returns the `{ok, state, error?}` envelope; `400` on any other mode. The setting
persists to the player profile and applies to the live window immediately; the
reported `window_mode` is the *setting* — headless runs have no real window to
reflect it.

### `/debug/*` — the debug view (typed domain interface)

The global debug-view flag, backed by the `DebugView` autoload (group `agent_debug`,
`IAgentDebug` — see `scripts/devtools/README.md`). Always present, like `/display/*`.

- `GET /debug/state` → `{"enabled": false, "available": true, "toggle_action": "debug_toggle"}`
  (`available` is whether this build honours the flag — a debug build).
- `GET` or `POST /debug/set` — `?enabled=true|false` (or `{"enabled": bool}`). `{ok, state,
  error?}`; `400` without a boolean, or in a release build.

The `debug_toggle` action (tilde by physical key) flips the same flag for humans, ignored
while a `LineEdit`/`TextEdit` has focus; an `/input/action` tap reaches it too.

### `/camera/*` — the zoom camera (typed domain interface)

The current scene's `ZoomCamera` (group `agent_camera`, `IAgentCamera` — see
`scripts/camera/README.md`). Scene-backed: `503` when no scene has one up.

- `GET /camera/state` → `zoom` (applied), `user_zoom` (eased), `target_user_zoom`,
  `min_zoom`/`max_zoom`, `step_factor`, `framed`, `input_enabled`, and the view centre `x`/`y`.
- `GET` or `POST /camera/zoom` — `?level=<zoom>` (within the bounds) and/or `?steps=<int>`
  (wheel steps, positive zooms in, clamped to the bounds), or a JSON body with the same
  fields. The target is set at once and eased like the wheel, so poll `user_zoom`. `{ok,
  state, error?}`; `400` on no field, a value that doesn't parse, or an out-of-bounds level.

```sh
curl 'http://127.0.0.1:8080/camera/zoom?level=2'
curl -X POST http://127.0.0.1:8080/camera/zoom -d '{"steps": -1}'
```

### `/transition/*` — the transition overlay (typed domain interface)

The loading-screen overlay, backed by the `Transition` autoload (group `agent_transition`,
`IAgentTransition` — see `scripts/ui/README.md`; the flow: `CLAUDE.md`, *Scene handoff*).
Always present, like `/display/*`.

#### `GET /transition/state`

```json
{ "stage": "hold",            // none | out | hold | in
  "text": "Travelling..",     // the loading text as shown, ellipsis included
  "held_seconds": 0.8, "min_hold_seconds": 2, "min_hold_elapsed": false,
  "fade_pending": true,       // a FadeIn waiting for the minimum
  "view_size": { "x": 1280, "y": 720 },
  "blocking_mouse": true, "layer": 10 }
```

#### `GET` or `POST /transition/skip`

End the loading screen's minimum now; a pending fade-in starts at once. `{ok, state, error?}`;
`400` outside `hold` (succeeds inside it even when the minimum has passed).

#### `GET` or `POST /transition/demo`

`?text=<loading text>&seconds=<0-60>` (both optional; `seconds` defaults to the loading
minimum): hold the loading screen over the current scene, with no scene swap, then fade in.
`{ok, state, error?}`; `400` while a transition runs or on a duration that doesn't parse or is
outside 0-60.

### `GET /session/state` — the session carrier (typed domain interface)

The `GameSession` autoload (group `agent_session`, `IAgentSession` — see
`scripts/session/README.md`). Always present; read-only (the scenes write the carrier).

```json
{ "launch": { "destination": "destination", "seed": "9051350816506544889" },
  "result": null,             // or { "kind": "complete", "seed": "..." }
  "next_seed": null }
```

**Seeds are strings**: a 64-bit seed doesn't survive a JSON number (clients parse doubles,
exact only to 2^53), so every seed goes out as its decimal text — and a route that takes one
should parse a string.

### `GET /content/state` — the content pipeline (typed domain interface)

The `ContentBootstrap` autoload (group `agent_content`, `IAgentContent` — see
`scripts/content/README.md`), which loads every definitions file at boot. Always present;
read-only. A failed load is reported in the body (`loaded: false`, the latched `error` line),
never as a 503, so a scenario can tell a bad file from a missing server.

```json
{ "loaded": true, "error": null, "files": ["examples.json"], "example_count": 3,
  "examples": [ { "id": "example_alpha", "name": "Alpha", "cost": 10, "kind": "basic" }, ... ] }
```

`examples` is the placeholder definition type — replace it with the real types' summaries.

### `GET /demo/state` — the scene-handoff demo (layered scene snapshot)

`IAgentDemo`, implemented by **both** demo scenes — the hub (`scenes/main.tscn`) and the
destination (`scenes/demo/destination.tscn`) — in group `agent_demo`; `503` in any other scene.
The shared blocks come from one helper (`scripts/demo/HandoffState.cs`); the scene adds a block
under its own name:

```json
{ "scene": "destination", "controls_locked": false,
  "lock": null,               // loading | transition | menu, null when the controls are free
  "session": { ... },         // /session/state's snapshot
  "transition": "none",       // the overlay's stage
  "dialog": { "open": false, "title": "", "body": "", "items": [], "buttons": [] },  // buttons: [{label, agent_id}]
  "destination": { "seed": "...", "payload": "Payload 44889", "returning": false, "arrived": true,
                   "loader": { "stage": "done", "attempts": 1, "failures": 0, "elapsed_ms": 171, "error": null } } }
```

The hub's block is `hub: { travelling, arriving, floating_labels }` (`floating_labels` counts the demo's live floating labels, spawned by the `demo_ping` action). The buttons that drive the flow are
`main.travel` and `destination.return` (`/ui/press`; refused with `400` while locked).

### `/devlab/*` — the dev-only lab (typed domain interface)

`IAgentDevLab` on the dev lab's controller (`scripts/devtools/DevLab.cs`, group `agent_devlab`)
— the worked **dev-only scene** (`CLAUDE.md`, *Dev-only scenes*). Present only while
`scenes/devtools/dev_lab.tscn` is the current scene, which nothing in the game navigates to: boot
it by path (`godot-mono --path . res://scenes/devtools/dev_lab.tscn`, or `# boot_scene:` in a
Hurl file). Anywhere else every `/devlab/*` route is a **scene-hinting 503** — its `error` names
the scene to boot:

```json
{ "error": "no dev lab; boot res://scenes/devtools/dev_lab.tscn (godot-mono --path . res://scenes/devtools/dev_lab.tscn) — a dev-only scene (group 'agent_devlab', IAgentDevLab)" }
```

#### `GET /devlab/state`

```json
{ "seed": "42",             // a string, like every 64-bit seed
  "seed_source": "env",     // env (GODOT_DEVLAB_SEED) | default (unset or unparseable) | agent (/devlab/seed)
  "rolls": 1,               // draws since the seed was set
  "last_roll": 1434747710 } // null before the first draw
```

#### `GET` or `POST /devlab/reroll`

Draw the next value from the seeded generator (the `devlab.reroll` button does the same).
`{ok, state}`.

#### `GET` or `POST /devlab/seed`

`?seed=<uint64>` (or `{"seed": "<uint64>"}`): reseed; `rolls` resets to 0 and `last_roll` to
null, so the same seed replays the same draws. `{ok, state, error?}`; `400` on a missing seed or
one that isn't a decimal uint64.

### `POST /quit` (also accepts `GET`)

Stops the running game. Body and query are both optional:

```sh
curl -X POST http://127.0.0.1:8080/quit                    # exit code 0
curl http://127.0.0.1:8080/quit?code=2                     # exit code 2 (GET form)
curl -X POST http://127.0.0.1:8080/quit \
     -H 'Content-Type: application/json' \
     -d '{"code": 1}'                                      # exit code 1 (POST + JSON)
```

`code` is the process exit code passed to `GetTree().Quit(code)`. Defaults to `0`. Query-string `?code=N` takes precedence over a JSON body (`TryReadFields`); a non-integer `code` is a `400`.

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

curl -X POST http://127.0.0.1:8080/input/move -d '{"x":56,"y":32}'
curl -X POST http://127.0.0.1:8080/input/click -d '{"x":400,"y":300,"button":"wheel_up"}'
curl -X POST http://127.0.0.1:8080/input/drag \
     -d '{"from_x":400,"from_y":300,"dx":-100,"dy":0,"steps":4}'

curl 'http://127.0.0.1:8080/display/set?factor=1.0'

curl -s -o /tmp/shot.png http://127.0.0.1:8080/screenshot

curl http://127.0.0.1:8080/ui/controls
curl -X POST http://127.0.0.1:8080/ui/press \
     -H 'Content-Type: application/json' \
     -d '{"id":"main.quit"}'
curl 'http://127.0.0.1:8080/ui/press?id=main.quit'   # the GET alias

curl -X POST http://127.0.0.1:8080/quit
```
