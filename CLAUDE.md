This is a Godot 4.7 Mono (C#) game project. Read the root @README.md for a human-centric description of the game.

**Engineering constraint**: lean hard on built-in Godot systems — do not write custom replacements until the built-in ones demonstrably don't fit. When a built-in genuinely doesn't fit, record a **dated, scoped amendment** here stating exactly which subsystem the constraint is lifted for and why, and that it stands in full everywhere else. (Amendment shape: "**Amended YYYY-MM-DD**: the built-ins demonstrably don't fit `<subsystem>` — `<concrete reason>`. That subsystem is deliberately hand-rolled; the constraint stands in full everywhere else.")

A worked example (hypothetical, for shape only — delete it once the project records a real amendment):

> **Amended 2026-01-10, widened 2026-02-03**: the built-ins demonstrably don't fit **the replay timeline** (`scripts/core/replay/`) — Godot's `Timer` and physics bodies are float-based and engine-bound, so a recorded run couldn't be guaranteed to play back bit-identically on every machine, nor run under the engine-free unit tests (widened on 2026-02-03 to cover the scrubbing cursor, once it proved to need the same exact ticks). That subsystem is deliberately hand-rolled in integer ticks (design: `design/game/replay.md`); the constraint stands in full everywhere else — the playback controls stay plain Controls and the scrub animation stays a Tween.

Each amendment names the design doc of the subsystem it covers, and that doc carries a matching *Built-ins and the amendment* section (see `design/_system-template.md`): which built-ins the system still uses, and the concrete reason the rest is hand-rolled.

## Documentation convention

Three layers, each stated **once** — never mirror one layer's content into another:

- **`design/` docs capture intent** — what a system is and *why* (decisions, rejected alternatives, balance intents, revisions). `design/game/*.md` and `design/visual/*.md` are the single authority for every system's semantics; `design/game/README.md` (create it with the first system doc) is the design-side index and carries the project glossary and global constants; start each system doc from `design/_system-template.md`. When implementing a new system or making major functional changes, read against the relevant design doc first; when a change touches semantics, revise the doc deliberately — never let code and doc silently drift. In-flight, unsettled notes live in `ideas/` instead (see `design/README.md` for the workflow).
- **Code self-documents the exact mechanics** — signatures, XML docs, validation rules, and the constants themselves. Do not restate a number, a validation rule, or an algorithm the code already states in a README or in this file. Each unit-test class's XML doc comment cites the design doc and section it verifies (e.g. `<c>design/game/<system>.md</c>, <i><Section></i>`), so a test traces back to the intent it pins.
- **READMEs are a searchable index** — every non-gitignored subdirectory carries a `README.md` listing what lives there and which design doc owns it: a few sentences per file, plus pointers. Create one when you create a directory; keep it pointing at the right places when functionality moves. Two deliberate exceptions own reference content of their niche: `scripts/server/README.md` (the full REST endpoint/schema reference) and `tests/hurl/README.md` (the scenario-authoring rules).

This file stays a lean **operating manual**: engineering constraints, workflows, cross-cutting patterns and registries, the REST endpoint table (the API contract), and pointers into `design/` and the READMEs. Update it whenever major architectural changes occur, but do not inflate it with per-system semantics — those belong to `design/`.

## Version control

This project uses **trunk-based development**: commit directly to `main`. Do **not** create feature branches for changes — branch only if a human explicitly asks. Commit (and push) only when asked. *(Policy block — a team that works differently should rewrite this section to match its flow.)*

For Godot-specific documentation searches, there is a `docs` directory in the root of the project that contains all of the Godot documentation, downloaded locally. Prefer that to searching the internet for Godot documentation.

`docs/` is gitignored except for its tracked `docs/.gdignore`, which is **required** — without it the Godot editor tries to import the thousands of images inside the docs as game assets (the download recipe below leaves the marker in place). To populate it, download the latest stable HTML build and unzip it into `docs/`:

```sh
touch docs/.gdignore   # already tracked; harmless if present
curl -L -o /tmp/godot-docs.zip https://nightly.link/godotengine/godot-docs/workflows/build_offline_docs/master/godot-docs-html-stable.zip
unzip -q /tmp/godot-docs.zip -d docs
rm /tmp/godot-docs.zip
```

## Verification & testing workflow

After making code changes, before reporting the work as done:

1. **Compile.** Run `dotnet build` and resolve any errors/warnings introduced by the change.
2. **Unit-test the engine-free layers.** Run `dotnet test tests/unit/godottemplate.Tests.csproj` — the xUnit rig over the pure-C# `scripts/core/` layer; it must stay green, and changes to engine-free logic get their coverage here, not in Hurl. The rig compiles the `scripts/core/` sources directly, so it doubles as the no-Godot-types boundary check.
3. **Agentic functional test.** Boot the game with `godot-mono` and exercise the changed behavior through the `AgentRestServer` REST interface (default `http://127.0.0.1:8080/`; endpoints documented below and in `scripts/server/README.md`). The goal is to confirm that the basic flow works end-to-end and that no regressions were introduced in adjacent features — not to evaluate balance or feel. **For anything beyond a single request, write or extend a Hurl test in `tests/hurl/` instead of hand-driving `curl`** — see *Scripted agentic tests (Hurl)* below.
4. **Extend the REST surface when needed.** If a new feature isn't reachable via the existing endpoints, extending `AgentRestServer` (new route, new `agent_id` on a button, new `IAgentInspectable` properties, new typed domain interface, etc.) is part of the implementation, not a follow-up. The bar is: any functionality that doesn't inherently require human eyes (visuals, feel, balance) must be agent-testable. Update the endpoint table in this file when you add or change routes.
5. **Identify human-only verification.** Anything the agent genuinely can't assess — visual polish, animation/timing feel, audio, balance/difficulty tuning, subjective gameplay interactions — gets handed back to the user.
6. **Clean up only the game instance you launched — never a blanket process kill.** Shut the test instance down with `POST /quit` (or `GET /quit` on Windows HTTP.sys). That is the *only* approved teardown: `/quit` is reachable solely through the running game's `AgentRestServer`, so it can never touch anything else. Confirm teardown by checking the **server is gone** (`/status` no longer responds), not by listing processes. Leaving an instance up ties up port 8080, blocks the next boot's `AgentRestServer` registration, and leaves stale background tasks. Tear down before reporting work done — including on failure paths.

   **Do NOT kill `godot-mono` processes by name.** The developer normally has the Godot **editor** open, which is *also* a `godot-mono` process; `Get-Process godot-mono | Stop-Process`, `taskkill /im godot-mono`, `pkill godot-mono`, and the like will destroy their editor session. Seeing a leftover `godot-mono` in the task list after `/quit` returns `200` almost always means you're looking at the editor — leave it alone. If (and only if) a *game instance you launched* genuinely refuses to quit via `/quit`, kill that one **specific PID** — the one captured when you spawned it (`echo $!` / the launch command's reported pid) — and nothing else.

The `godot-mono` binary is expected to be on `PATH`. Use it directly for headless or windowed runs (e.g. `godot-mono --path .` to boot the configured main scene). Note that this command **launches a fresh game process** independent of any editor the developer has open — it does not attach to or disturb their editor.

**Scratch files go in the project's `tmp/`** (gitignored; its tracked `.gdignore` keeps Godot from importing anything there): screenshots, game logs, captured PIDs, throwaway scripts. Use a subdirectory per task (e.g. `tmp/<topic>/`), never the system temp directory or a session scratchpad, so the developer can find and inspect them. The `.gdignore` doesn't reach the C# build: the game's csproj compiles every `.cs` under the project, `tmp/` included, so a throwaway C# program (and its `obj/`) breaks `dotnet build` — park its sources as `.txt` and delete its `bin/` and `obj/` when done.

### Scripted agentic tests (Hurl)

`tests/hurl/` holds [Hurl](https://hurl.dev) scenarios — plain-text HTTP scripts that drive a booted game through the REST interface. **Prefer writing a Hurl test over manually issuing a sequence of REST calls.** The game is a live `_Process` simulation: when you hand-drive `curl`, the world moves between your calls and you race it. A Hurl scenario is authored once, run with one command, and its `retry`/`retry-interval` options **wait on game state** (poll until the asserts pass) instead of guessing timing — faster, re-runnable, and stable.

Run them with the wrapper (boots a fresh headless instance per file, guarantees teardown even on failure):

```sh
tests/hurl/run-tests.ps1                 # all files, headless   (-Windowed to watch)
tests/hurl/run-tests.ps1 -Files smoke.hurl   # one file (bare positional binds to -Port under PS 5.1 — always pass -Files)
tests/hurl/run-tests.sh                  # bash equivalent (WINDOWED=1 to watch)
```

`-Port <n>` / `PORT=<n>` boots each instance's REST server on that port (via `GODOT_AGENT_REST_PREFIX`) and aims the scenarios there, so a run can go beside a game already holding 8080.

The runner boots each file into the configured main scene by default; a file that needs a different boot carries a `# boot_scene:` directive in its header (`default` = the configured main scene) — see `tests/hurl/README.md`.

Or iterate against an already-booted game: `hurl --test --jobs 1 --variable host=127.0.0.1:8080 --error-format long tests/hurl/<file>.hurl`.

The authoring idiom and the non-obvious rules (retry *is* the wait; keep mutations and polls in separate entries; capture ids/coordinates — never hardcode runtime-generated values; assert the *positive* form of a JSONPath `count`; use the `GET` aliases for bodyless calls) live in `tests/hurl/README.md`. Read it before adding a scenario.

**If `hurl` is not detected** when you go to run a test, install it, then retry — Windows: `winget install --id Orange-OpenSource.Hurl` (binary lands at `C:\Program Files\Hurl\hurl.exe`; may not be on the current shell's `PATH` until a new shell — the runners fall back to that path); macOS: `brew install hurl`; Linux/other: https://hurl.dev/docs/installation.html.

### End-of-session report

When reporting work as done, include:

- **What was built / changed** — a concise summary of the code, scenes, and resources touched.
- **How it was tested agentically** — which REST endpoints were exercised, which scenarios were covered, and the observed results. Note any new endpoints / `agent_id`s / inspectable properties added to support testing.
- **What still needs human verification** — a focused checklist of balance, visual, and feel checks the user should run, plus anything the agent couldn't reach.

## Architecture

### Main Scene
The main scene is set via `run/main_scene` in `project.godot`. The template ships with `scenes/main.tscn` (controller `scripts/ui/MainScreen.cs`), a demo scene containing a `Sprite2D` driven by `scripts/test/Player.cs` with a `ZoomCamera` following it, a `QuitButton` carrying `metadata/agent_id = "main.quit"`, a `TravelButton` (`main.travel`, the scene-handoff demo below), and the chrome: the shared top bar (`main.menu`) and menu overlay (`main.menu.settings` → the settings panel's `settings.*`, `main.menu.quit`, `main.menu.close`; Esc toggles it) on canvas layer 20 — together they verify the agent REST surface (input actions, group queries, UI control, camera, screenshot, quit) end-to-end. Replace the scene with your own when starting real work and delete `scripts/test/`.

**Scene handoff.** Moving between top-level screens goes through the `Transition` autoload (`scripts/ui/Transition.cs`, `scenes/ui/transition.tscn`), a CanvasLayer that survives `ChangeSceneToFile`: the leaving scene locks its controls, writes the launch on `GameSession`, and calls `BeginOut` (fade to opaque), whose callback calls `Hold(text)` (the loading screen) and changes scene under it; the arriving scene reads the launch, gathers its inputs on the main thread and runs its load on an `AsyncLoader<TIn,TOut>` (`scripts/core/loading/`), polled each frame, then calls `FadeIn` — which waits out the hold's minimum (`fade_pending`) — and frees its controls on `Finished`. The way back is the same, with `GameSession.Finish(result)` before leaving. The shipped demo pair is the hub `scenes/main.tscn` (Travel) and `scenes/demo/destination.tscn` (Return, `destination.return`; `scripts/demo/`), driven end-to-end by `tests/hurl/handoff.hurl`. An arriving scene booted directly (no launch in flight) launches itself with a default and puts its own loading screen up, so a direct boot works (`handoff_direct.hurl`).

**Dev-only scenes.** A tool scene for developers (a generator lab, an all-AI observer) is never reachable from the game: no button, no `GameScenes` route; it is booted directly with `godot-mono --path . res://<path>.tscn` (and a Hurl file's `# boot_scene:`). It gets its own agent group and typed interface, whose `503` names the scene to boot (`"… boot res://scenes/x/lab.tscn"`). Its knobs for reproducible runs are `GODOT_<X>_SEED`-style env overrides, parsed with the invariant culture; a value that doesn't parse logs a warning and falls back to the default — never a crash. Document each one under *Main Scene* here.

### Agent REST Server
`scripts/server/AgentRestServer.cs` is a debug-build-only autoload that exposes a small HTTP API for external agents (claude code, scripts) to drive a running game. Default bind is `http://127.0.0.1:8080/`; override with the `GODOT_AGENT_REST_PREFIX` env var. Disabled in release builds via `OS.IsDebugBuild()`.

Endpoints:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/status` | Sanity check; returns running flag, current scene path, Godot version, `game_version` (`GameVersion.Current` — `-dev`-suffixed in debug builds), `window` (`width`/`height` in pixels — a headless boot is resized from its 64×64 placeholder to the configured viewport size). |
| `POST` | `/input/action` | Body `{"action": "<name>", "mode": "press"\|"release"\|"tap"}`. `tap` auto-releases after one `_Process` frame. |
| `POST` | `/input/click` | Body `{"x": <px>, "y": <px>, "button": "left"\|"right"\|"middle"\|"wheel_up"\|"wheel_down"}`. Synthetic mouse click (or one wheel step) at a canvas pixel (the `/ui/controls` rect space: `/screenshot` pixels ÷ UI scale) — motion + press + release through the real GUI/`_unhandled_input` pipeline; works headless. |
| `POST` | `/input/drag` | Body `{"from_x", "from_y", "to_x", "to_y"}` or `{"from_x", "from_y", "dx", "dy"}`, optional `"button"` (`left`/`right`/`middle`) and `"steps"` (1-100, default 8). Press, motion along the line with the button held, release — same space and pipeline as `/input/click`. |
| `POST` | `/input/move` | Body `{"x": <px>, "y": <px>}`. Pointer motion only (no press), same space and pipeline as `/input/click`: builds hover state — highlights at once, tooltips after their delay. |
| `GET` | `/nodes?group=<name>` | Returns `[ {path, name, type, position?, properties?}, ... ]` for nodes in the named group. |
| `GET` | `/screenshot` | Captures the main viewport and returns raw image bytes (`image/png` by default). Optional `?format=png\|jpg\|webp` and `?quality=<0.01-1.0>` (jpg only). No filesystem write on the game side. |
| `GET` | `/ui/controls` | Lists every `BaseButton` in the current scene whose `agent_id` metadata is set: `[{id, type, text?, disabled, visible, hovered, pressed?, rect}]` — `rect` is the on-screen rectangle (plus `center_x`/`center_y`) in the `/input/click` coordinate space, so click scenarios never hardcode layout. |
| `POST` | `/ui/press` | Body `{"id": "<agent_id>"}`. Emits the button's `Pressed` signal (toggles flip `ButtonPressed` first). `400` if disabled / hidden / ambiguous; `404` if not found. |
| `GET` | `/example/state` | Stub demonstrating the typed domain-interface pattern: looks up an `IAgentExample` implementer in the `agent_example` group and returns its state. See `scripts/server/IAgentExample.cs` for the pattern to copy when adding real domain surfaces (inventory, quest log, NPCs, etc.). |
| `GET` | `/display/state` | Snapshot of the display settings (`DisplayScale` autoload, group `agent_display`): effective/auto scale factors, override flag, `window_size`, `pixel_cap` and `min_canvas` (the 1280×720 floor the auto scale respects), `window_mode` (the persisted setting), monitor scale/DPI. Always present — never 503s. |
| `GET` or `POST` | `/display/set` | `?factor=<1.0-3.0>` or `?auto=true` (or a JSON body with the same fields). Sets or clears the UI-scale override (persisted to the player profile); returns the `{ok, state, error?}` envelope. |
| `GET` or `POST` | `/display/window` | `?mode=windowed\|fullscreen` (or `{"mode": ...}`). Sets the persisted window-mode setting and applies it to the live window (borderless fullscreen — no exclusive mode). `400` on any other mode. `{ok, state, error?}` envelope. |
| `GET` | `/debug/state` | The debug view (`DebugView` autoload, group `agent_debug`): `enabled`, `available` (a debug build), `toggle_action`. Always present — never 503s. |
| `GET` or `POST` | `/debug/set` | `?enabled=true\|false` (or `{"enabled": bool}`): turn the debug view on or off (the `debug_toggle` action, tilde by physical key, does the same for humans). `{ok, state, error?}`; `400` without a boolean, or in a release build. |
| `GET` | `/camera/state` | The current scene's zoom camera (`ZoomCamera`, group `agent_camera`): applied zoom, eased and target user zoom, bounds, `step_factor`, `framed`, `input_enabled`, view centre. `503` when no scene has one. |
| `GET` or `POST` | `/camera/zoom` | `?level=<zoom>` (within the bounds) and/or `?steps=<int>` (wheel steps, positive zooms in, clamped), or a JSON body with the same fields: sets the player's target zoom, eased like the wheel. `{ok, state, error?}`; `400` on no field, a bad value, or an out-of-bounds level. |
| `GET` | `/transition/state` | The transition overlay (`Transition` autoload, group `agent_transition`): `stage` (`none`/`out`/`hold`/`in`), the loading `text` as shown (ellipsis included), `held_seconds`, `min_hold_seconds`, `min_hold_elapsed`, `fade_pending` (a fade-in waiting for the minimum), `view_size`, `blocking_mouse`, `layer`. Always present — never 503s. |
| `GET` or `POST` | `/transition/skip` | End the loading screen's minimum now (a pending fade-in starts at once). `{ok, state, error?}`; `400` outside `hold`. |
| `GET` or `POST` | `/transition/demo` | `?text=&seconds=` (both optional; `seconds` 0-60, default the loading minimum): dev check of the loading screen over the current scene, then the fade-in. `{ok, state, error?}`; `400` while a transition runs or on a bad duration. |
| `GET` | `/session/state` | The session carrier (`GameSession` autoload, group `agent_session`): the `launch` in flight (`destination`, `seed`), the last `result` (`kind`, `seed`), `next_seed` — seeds as strings (64-bit). Always present — never 503s. |
| `GET` | `/demo/state` | The scene-handoff demo pair (`IAgentDemo`, group `agent_demo`; `503` unless the hub or the destination is up), a layered snapshot: shared `scene` (`hub`/`destination`), `controls_locked` and its `lock` reason (`loading`/`transition`/`menu`, null when free), `session`, `transition` stage, plus the scene's own block — `hub` (`travelling`, `arriving`) or `destination` (`seed`, `payload`, `returning`, `arrived`, `loader` with stage, attempts, failures, `elapsed_ms`, error). |
| `POST` or `GET` | `/quit` | Optional `?code=<int>` query (or JSON body `{"code": <int>}` on POST). Calls `GetTree().Quit(code)` after a 5-frame delay so the response flushes first. `GET` is accepted because Windows HTTP.sys rejects bodyless POSTs with 411. |

Per-node JSON includes `position` for `Node2D`/`Node3D` and a `properties` object for nodes that implement `godottemplate.Server.IAgentInspectable` (`scripts/server/IAgentInspectable.cs`). Implementers return a `Dictionary<string, object>` of extra fields. The HTTP listener runs on a background task; all handlers execute on the main thread inside `_Process` so they may freely touch the scene tree. See `scripts/server/README.md` for examples.

Every domain-route mutation (the `/example/*`-style routes you'll add) should return `{ok, state, error?}` where `state` is the full domain snapshot after the operation. Keep error envelopes consistent so a single client helper can handle them all.

**Bodyless mutations accept `GET`.** Windows HTTP.sys rejects a bodyless `POST` with `411 Length Required` before the app ever sees it, so any mutation route that can be called without a body also accepts `GET`, mirroring its body fields as query parameters (`/quit?code=N` is the shipped example). Follow the idiom for every bodyless route you add, and use the `GET` form in scripts.

### Generic UI control pattern

`/ui/controls` and `/ui/press` provide a scene-agnostic way for agents to operate top-level buttons (main menu Play, builder Continue, future settings/credits/etc.). To opt a button in, give it an `agent_id` node metadata entry — e.g. in a `.tscn`:

```
[node name="PlayButton" type="Button" parent="..."]
text = "Play"
metadata/agent_id = "main_menu.play"
```

Conventions:

- **IDs are scene-prefixed and dot-delimited** (`main_menu.play`, `main.quit`, future `settings.apply`). Globally unique. Dynamically built buttons (list rows, tabs, cards) follow the same convention with a data-derived tail (`settings.tab.<name>`, `list.select.<key>` — set via `button.SetMeta("agent_id", ...)`), so data-driven UIs stay agent-drivable with zero per-item code.
- Buttons on a hidden panel are listed with `visible: false` and reject `/ui/press` with `400` — scenarios must walk the real navigation flow to reach them, so a broken navigation path fails observably instead of silently pressing invisible buttons.
- **Stable across refactors** — the ID is the API contract; node names / paths can change freely.
- Only `BaseButton` nodes are picked up. For richer affordances (sliders, dropdowns, drag targets) add a typed interface following the `IAgentInspectable` pattern when needed.
- For systems that already have bespoke endpoints (typed domain interfaces — see below), keep using those — `agent_id` is for top-level navigation and one-off actions, not for high-frequency authoring loops.

Agents should prefer `/ui/press` over forging input events whenever a button exists for the action.

### Typed domain interfaces

For richer surfaces than buttons — an inventory, a shop, a quest journal, a level editor — add a typed interface following `IAgentExample` (`scripts/server/IAgentExample.cs`):

1. Define an `IAgent<X>` interface that exposes the operations and a `GetXState()` snapshot.
2. Implement it on the active scene controller; have that controller `AddToGroup("agent_<x>")` in `_Ready`.
3. In `AgentRestServer`, declare the surface once as a `Domain<T>` (its group, its `503` text, its snapshot) and register one line per route on the generic helpers: `State(domain)` for the `GET` snapshot, `Route(req, domain, keys, (x, fields) => ...)` for mutations — find the implementer (`503` when absent), read the fields, run the op, wrap `{ok, state, error?}`. Underneath sit `Find<T>(group)` and `Envelope(state, result)`, and `TryReadFields` (query and JSON body merged into one field map, the query winning) with `ParseBool`/`ParseInt`/`ParseFloat` for uniform `400` messages — the helper behind the GET-or-POST idiom.

This keeps domain logic out of the server file: the server only knows how to look the implementer up and serialize its state.

Two variants of the pattern ship as working examples:

- **Scene-controller-backed** (`/example/state` + `IAgentExample`): the implementer is the active scene's controller, so the surface exists only while that scene is up — routes return `503` otherwise. Use for per-screen domains (a mission, a shop, an editor).
- **Autoload-backed, always present** (`/display/*` + `IAgentDisplay` on the `DisplayScale` autoload): the implementer lives for the whole process, so the surface never 503s. Use for global, process-lifetime domains (display settings, audio, save slots).

**Layered scene snapshot.** When two scenes share a domain (the player's match and a dev-only observer of the same match), one `IAgentX` is implemented by both: the state carries the shared blocks, built by one helper so they can't drift, plus a block named after the scene with what only it has; an operation one scene doesn't support is a `400` with the reason (`"no player in the observer"`), never a `404` or a silent no-op. The shipped example is `/demo/state` (`IAgentDemo` on both demo scenes, the shared blocks from `scripts/demo/HandoffState.cs`).

### Cross-scene state & persistence (pattern to copy when needed)

Top-level screens swapped with `GetTree().ChangeSceneToFile(...)` can't pass arguments, so anything that must survive a swap lives on an autoload. The shape that has worked well is a **trio**:

- **A session carrier** (`GameSession` — `scripts/session/GameSession.cs`, an autoload with static state, shipped as a placeholder-typed skeleton): the *transient* state handed from one screen to the next — the launch in flight, the result coming back, an agent's `NextSeed` override consumed by the next launch. Process-lifetime, but conceptually scoped to "the current run"; it is not persisted. Its `SessionLaunch`/`SessionResult` records are the thing a real game replaces (the accepted mission, the chosen loadout); `/session/state` reads it, 64-bit seeds as strings.
- **A durable profile** (`PlayerProfile` — `scripts/session/PlayerProfile.cs`, a lazy-loading static class shipped as working reference code): the *permanent* player state — today the display settings; later currency, unlocks, progression. It owns its mutation rules and persists to disk through a companion store class (`ProfileStore`). The `GODOT_PROFILE_MEMORY` env var makes it memory-only for a process (the Hurl runners pin it).
- **A constants class** (e.g. `GameConstants`): game-wide tunables that are neither per-scene nor per-content-definition. Plain `static` class, not an autoload.

Register the autoloads in `project.godot` `[autoload]`. Keep per-scene state on the scene's own controller — it should die with the scene.

When the durable profile grows disk persistence, follow these four rules (proven out in spun-out projects):

1. **Version-stamped `user://` JSON.** One record (e.g. `user://profile.json`) written via `FileAccess` + `System.Text.Json`, carrying a `version` int. On load, a version mismatch discards the file and starts fresh (migrate later if it ever matters).
2. **A DTO snapshot type decouples the disk format from live state.** The profile hands a plain `ProfileSnapshot` DTO to the store and rebuilds itself from one on load — the store never touches the profile's private fields, and the disk schema can evolve independently.
3. **Every failure path is non-fatal.** A missing file, unreadable file, malformed JSON, or version mismatch logs a warning and returns `false`; the caller falls back to fresh defaults. A bad save must never crash the game.
4. **Save after every durable mutation.** Call `Save()` at the end of each mutating operation (purchase, reward, settings change) rather than on quit — quit paths are unreliable (crashes, task kills), and the writes are tiny.

### Directory Layout
- `scripts/` — C# game logic. Add subdirectories per system (e.g. `units/`, `world/`) as the project grows. `scripts/core/` is the **engine-free** layer — pure C# with no Godot types, compiled directly into the `tests/unit/` xUnit project as the boundary check; it holds the JSON definition reader (`content/`), the background loader `AsyncLoader<TIn,TOut>` (`loading/`), the record codec and folder logic (`persistence/`) and `BuildResult<T>` (`validation/`). `scripts/content/` is the Godot side of content loading (reads `res://` text for the core reader, latches failures). `scripts/session/` holds the cross-scene trio's transient leg (`GameSession`) and durable leg (`PlayerProfile`/`ProfileStore`) and `RecordStore<T>`, a one-file-per-record store. Shared UI scaffolding (scene-path registry, palette, display scale, the transition overlay, the top bar, menu overlay and settings panel) lives in `scripts/ui/`; the scene-handoff demo's destination in `scripts/demo/` (scene in `scenes/demo/`); the generic scroll-zoom camera in `scripts/camera/`; debug-build-only views for humans (the debug view's flag) in `scripts/devtools/`; stateless cross-system helpers go in `scripts/util/`.
- `scenes/` — `.tscn` scene files, mirroring the structure of `scripts/`.
- `resources/` — `.tres` content files (`[GlobalClass]` Resource subclasses). See `resources/README.md` for the C# `.tres` format requirements.
- `shaders/` — `.gdshader` files.
- `sprites/` — sprite images and their `.import` sidecars (both individual sprites and packed atlases).
- `ideas/` — markdown design notes for in-flight features. Move completed ideas into `ideas/done/`; fold settled designs into `design/`.
- `design/` — authoritative, settled design documentation (`game/`, `visual/`, `mocks/`); documentation only, nothing here is loaded by the game. See `design/README.md`.

### UI Panels
Always define UI panel structure in a `.tscn` file, not entirely in C#. Control anchor/offset layout is baked into the scene before nodes enter the tree; setting anchors in `_Ready()` runs after the first layout pass and produces incorrect positioning (e.g., centered panels appear in the top-left corner). The C# controller should only wire up signals and fill dynamic content via `GetNode<T>("%UniqueName")`.

**Never build UI node trees in code — everything must be editable in the Godot editor.** A repeated piece (a card, a list row, a stat line, a marker) gets its own `.tscn` with a thin script (`Setup(...)` + properties). The controller instances it from an exported `PackedScene` and fills it; it never `new`s Controls and assembles them. The shared `scenes/ui/` pieces are the reference examples: `top_bar.tscn` (`Title`/`MenuAgentId` exports, a `MenuPressed` event), `settings_panel.tscn` (its choices are scene-authored buttons carrying `scale_factor` metadata) and `menu_overlay.tscn`, all hosted by `scenes/main.tscn`. Colours are the exception: they come from `Palette` (`scripts/ui/Palette.cs`) in code — or a `PaletteRect` token in a scene — never inline in a scene.

**Design layouts responsive to the real window size** — anchors, containers, and size flags against whatever space actually exists, not a fixed design resolution. The project deliberately keeps the default `disabled` stretch mode: the viewport always matches the window, and hi-dpi/accessibility sizing is handled by the `DisplayScale` autoload scaling the canvas (`ContentScaleFactor`) on top of responsive layout. Do not switch to the `canvas_items`/`viewport` stretch modes to sidestep multi-resolution layout — a fixed design size is a crutch that breaks down across aspect ratios and compounds badly with DPI scaling.

### Key Patterns
- **IsInstanceValid**: always use to check freed Godot objects; C# null checks are insufficient.
- **Namespaces mirror directory structure**: a script in `scripts/foo/` belongs in namespace `<RootNamespace>.Foo` (where `<RootNamespace>` is set in the `.csproj`, e.g. `godottemplate.Foo`). Keeps code navigation predictable.
- **Z-index registry**: when introducing layered 2D objects, document the z-index assignments here so future code follows the same registry.
- **Canvas layers**: the world draws on the default canvas (`0`); plain screen UI that may be covered (the demo `QuitButton`/`TravelButton`, the destination's panel) sits on a default `CanvasLayer` (`1`); the `Transition` autoload's overlay is layer `10` (`Transition.OverlayLayer`); a scene's chrome that must stay up through a transition (the top bar, menus) sits on layer `20` or above (`Transition.ChromeLayer`; the demo scenes' `Chrome` layers). Register new layers here.
- **Controls-lock reason**: a scene whose player controls can be locked computes *why* as one property — a string reason or null when free (`ControlsLock`: `loading` while its load runs, `transition` while a transition or its own arrival/exit runs, `menu` while the menu is up, in that priority order) — applies it each frame (input ignored, buttons disabled, zoom input off), and exposes `controls_locked` + `lock` in its agent state. One derived reason, never a set of booleans toggled at each call site, so a scenario can assert *which* lock holds and a stuck lock names its cause. Shipped in both demo scenes (`/demo/state`).
- **Collision layers**: when introducing physics interactions, document the layer/mask assignments here.
- **Groups**: when introducing scene-tree groups (gameplay membership, REST domain-interface lookup, etc.), document the names here. The template uses `player` (demo), `agent_example` (typed-interface stub), `agent_display` (the `DisplayScale` autoload, `/display/*` — always present), `agent_debug` (the `DebugView` autoload, `/debug/*` — always present), `agent_transition` (the `Transition` autoload, `/transition/*` — always present), `agent_session` (the `GameSession` autoload, `/session/state` — always present), `agent_camera` (a scene's `ZoomCamera`, `/camera/*` — present only while such a scene is up), and `agent_demo` (either scene-handoff demo scene, `/demo/state` — present only while the hub or the destination is up) out of the box.
- **Resource references**: prefer exported `PackedScene`/resource references on scene roots, or a small content registry resource, over hardcoding `GD.Load<T>(...)` paths in runtime classes. See `resources/README.md` for the C# `.tres` script-binding requirements.
- **Node-typed `[Export]`s in hand-authored `.tscn`s**: a `Node`-derived export serializes as `Prop = NodePath("...")` **plus** a `node_paths=PackedStringArray("Prop")` attribute on the `[node]` header. Omitting the attribute makes the reference silently load as null (the editor writes it automatically; hand edits must include it).
- **`[Tool]` scripts**: editor-only paths guard with `Engine.IsEditorHint()`; the editor only sees tool-script changes after `dotnet build`.

## Local Godot Docs

Explore tasks against the local copy of Godot docs should be handed off to the `opencode` CLI harness instead of using the `Explore` tool or even directly `grep`, `glob`, or other shell-based commands while running in the `claude` CLI harness.

When invoking `opencode run` for doc exploration, frame the prompt so it:
1. Asks opencode to **explore** the `docs` directory (not search for a string).
2. Specifies that only a **concise summary** should be returned as output — no raw file contents or tool call results.

Example: `opencode run "Explore the Godot 4 documentation at <project-root>/docs to understand how <Topic> works. Return only a concise summary of key properties, signals, methods, and usage patterns."`

## Misc Agent direction

*(Opt-in content-boundary policy — keep, adjust, or delete per project.)*

- Sprites should only ever be human-authored. **NEVER** author sprites for any reason. If you need a placeholder sprite, use `icon.svg` in the project root.
- ANY text that qualifies as "prose", IE is not strictly technical and will meet with player eyes, should be generated as Lorem Ipsum. Headers, button values, and other one-or-two-word verbs are fine. Guide entries and item/spell descriptions are examples of what should be left to the human. In general, a human is responsible for all prose that involves more than a glance, and we should make it obvious what needs their attention by using obvious placeholder text.
- If the human asks about what the current or next session should target, offer suggestions based on unchecked items in `milestones.md`.
