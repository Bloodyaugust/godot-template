This is a Godot 4.7 Mono (C#) game project. Read the root @README.md for a human-centric description of the game.

Update this file whenever major architectural changes occur.

Observe the README.md file in a subdirectory when making changes there. If you create or find a subdirectory that is not gitignored when looking for relevant source or making changes, create one. If you meaningfully change functionality in a directory, update its README.md.

For Godot-specific documentation searches, there is a `docs` directory in the root of the project that contains all of the Godot documentation, downloaded locally. Prefer that to searching the internet for Godot documentation.

`docs/` is gitignored. To populate it, download the latest stable HTML build and unzip it into `docs/`:

```sh
mkdir -p docs
curl -L -o /tmp/godot-docs.zip https://nightly.link/godotengine/godot-docs/workflows/build_offline_docs/master/godot-docs-html-stable.zip
unzip -q /tmp/godot-docs.zip -d docs
rm /tmp/godot-docs.zip
```

## Verification & testing workflow

After making code changes, before reporting the work as done:

1. **Compile.** Run `dotnet build` and resolve any errors/warnings introduced by the change.
2. **Agentic functional test.** Boot the game with `godot-mono` and exercise the changed behavior through the `AgentRestServer` REST interface (default `http://127.0.0.1:8080/`; endpoints documented below and in `scripts/server/README.md`). The goal is to confirm that the basic flow works end-to-end and that no regressions were introduced in adjacent features — not to evaluate balance or feel.
3. **Extend the REST surface when needed.** If a new feature isn't reachable via the existing endpoints, extending `AgentRestServer` (new route, new `agent_id` on a button, new `IAgentInspectable` properties, new typed domain interface, etc.) is part of the implementation, not a follow-up. The bar is: any functionality that doesn't inherently require human eyes (visuals, feel, balance) must be agent-testable. Update the endpoint table in this file when you add or change routes.
4. **Identify human-only verification.** Anything the agent genuinely can't assess — visual polish, animation/timing feel, audio, balance/difficulty tuning, subjective gameplay interactions — gets handed back to the user.
5. **Always clean up game instances.** Any `godot-mono` process spawned for agentic testing must be shut down (`POST /quit`, or `GET /quit` on Windows HTTP.sys) once testing is complete. Leaving an instance running ties up port 8080, blocks the next boot's `AgentRestServer` registration, and leaves stale background tasks in the harness. Tear down before reporting work as done — including failure paths, where you should still quit the instance before moving on.

The `godot-mono` binary is expected to be on `PATH`. Use it directly for headless or windowed runs (e.g. `godot-mono --path . scenes/main.tscn`).

### End-of-session report

When reporting work as done, include:

- **What was built / changed** — a concise summary of the code, scenes, and resources touched.
- **How it was tested agentically** — which REST endpoints were exercised, which scenarios were covered, and the observed results. Note any new endpoints / `agent_id`s / inspectable properties added to support testing.
- **What still needs human verification** — a focused checklist of balance, visual, and feel checks the user should run, plus anything the agent couldn't reach.

## Architecture

### Main Scene
The main scene is set via `run/main_scene` in `project.godot`. The template ships with `scenes/main.tscn`, a demo scene containing a `Sprite2D` driven by `scripts/test/Player.cs` and a `QuitButton` carrying `metadata/agent_id = "main.quit"` — together they verify the agent REST surface (input actions, group queries, UI control, screenshot, quit) end-to-end. Replace the scene with your own when starting real work and delete `scripts/test/`.

### Agent REST Server
`scripts/server/AgentRestServer.cs` is a debug-build-only autoload that exposes a small HTTP API for external agents (claude code, scripts) to drive a running game. Default bind is `http://127.0.0.1:8080/`; override with the `GODOT_AGENT_REST_PREFIX` env var. Disabled in release builds via `OS.IsDebugBuild()`.

Endpoints:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/status` | Sanity check; returns running flag, current scene path, Godot version. |
| `POST` | `/input/action` | Body `{"action": "<name>", "mode": "press"\|"release"\|"tap"}`. `tap` auto-releases after one `_Process` frame. |
| `GET` | `/nodes?group=<name>` | Returns `[ {path, name, type, position?, properties?}, ... ]` for nodes in the named group. |
| `GET` | `/screenshot` | Captures the main viewport and returns raw image bytes (`image/png` by default). Optional `?format=png\|jpg\|webp` and `?quality=<0.01-1.0>` (jpg only). No filesystem write on the game side. |
| `GET` | `/ui/controls` | Lists every `BaseButton` in the current scene whose `agent_id` metadata is set: `[{id, type, text?, disabled, visible, pressed?}]`. |
| `POST` | `/ui/press` | Body `{"id": "<agent_id>"}`. Emits the button's `Pressed` signal (toggles flip `ButtonPressed` first). `400` if disabled / hidden / ambiguous; `404` if not found. |
| `GET` | `/example/state` | Stub demonstrating the typed domain-interface pattern: looks up an `IAgentExample` implementer in the `agent_example` group and returns its state. See `scripts/server/IAgentExample.cs` for the pattern to copy when adding real domain surfaces (inventory, quest log, NPCs, etc.). |
| `POST` or `GET` | `/quit` | Optional `?code=<int>` query (or JSON body `{"code": <int>}` on POST). Calls `GetTree().Quit(code)` after a 5-frame delay so the response flushes first. `GET` is accepted because Windows HTTP.sys rejects bodyless POSTs with 411. |

Per-node JSON includes `position` for `Node2D`/`Node3D` and a `properties` object for nodes that implement `godottemplate.Server.IAgentInspectable` (`scripts/server/IAgentInspectable.cs`). Implementers return a `Dictionary<string, object>` of extra fields. The HTTP listener runs on a background task; all handlers execute on the main thread inside `_Process` so they may freely touch the scene tree. See `scripts/server/README.md` for examples.

Every domain-route mutation (the `/example/*`-style routes you'll add) should return `{ok, state, error?}` where `state` is the full domain snapshot after the operation. Keep error envelopes consistent so a single client helper can handle them all.

### Generic UI control pattern

`/ui/controls` and `/ui/press` provide a scene-agnostic way for agents to operate top-level buttons (main menu Play, builder Continue, future settings/credits/etc.). To opt a button in, give it an `agent_id` node metadata entry — e.g. in a `.tscn`:

```
[node name="PlayButton" type="Button" parent="..."]
text = "Play"
metadata/agent_id = "main_menu.play"
```

Conventions:

- **IDs are scene-prefixed and dot-delimited** (`main_menu.play`, `main.quit`, future `settings.apply`). Globally unique.
- **Stable across refactors** — the ID is the API contract; node names / paths can change freely.
- Only `BaseButton` nodes are picked up. For richer affordances (sliders, dropdowns, drag targets) add a typed interface following the `IAgentInspectable` pattern when needed.
- For systems that already have bespoke endpoints (typed domain interfaces — see below), keep using those — `agent_id` is for top-level navigation and one-off actions, not for high-frequency authoring loops.

Agents should prefer `/ui/press` over forging input events whenever a button exists for the action.

### Typed domain interfaces

For richer surfaces than buttons — an inventory, a shop, a quest journal, a level editor — add a typed interface following `IAgentExample` (`scripts/server/IAgentExample.cs`):

1. Define an `IAgent<X>` interface that exposes the operations and a `GetXState()` snapshot.
2. Implement it on the active scene controller; have that controller `AddToGroup("agent_<x>")` in `_Ready`.
3. Add an `AgentRestServer` route group that uses `tree.GetNodesInGroup("agent_<x>")` to find the implementer and returns `503` if none is present.

This keeps domain logic out of the server file: the server only knows how to look the implementer up and serialize its state.

### Directory Layout
- `scripts/` — C# game logic. Add subdirectories per system (e.g. `units/`, `world/`, `ui/`) as the project grows; the template keeps it flat. Stateless cross-system helpers go in `scripts/util/`.
- `scenes/` — `.tscn` scene files, mirroring the structure of `scripts/`.
- `resources/` — `.tres` content files (`[GlobalClass]` Resource subclasses). See `resources/README.md` for the C# `.tres` format requirements.
- `shaders/` — `.gdshader` files.
- `sprites/` — sprite images and their `.import` sidecars (both individual sprites and packed atlases).
- `ideas/` — markdown design notes for in-flight features. Move completed ideas into `ideas/done/`.

### UI Panels
Always define UI panel structure in a `.tscn` file, not entirely in C#. Control anchor/offset layout is baked into the scene before nodes enter the tree; setting anchors in `_Ready()` runs after the first layout pass and produces incorrect positioning (e.g., centered panels appear in the top-left corner). The C# controller should only wire up signals and build dynamic content (cards, rows) via `GetNode<T>("%UniqueName")`.

### Key Patterns
- **IsInstanceValid**: always use to check freed Godot objects; C# null checks are insufficient.
- **Namespaces mirror directory structure**: a script in `scripts/foo/` belongs in namespace `<RootNamespace>.Foo` (where `<RootNamespace>` is set in the `.csproj`, e.g. `godottemplate.Foo`). Keeps code navigation predictable.
- **Z-index registry**: when introducing layered 2D objects, document the z-index assignments here so future code follows the same registry.
- **Collision layers**: when introducing physics interactions, document the layer/mask assignments here.
- **Groups**: when introducing scene-tree groups (gameplay membership, REST domain-interface lookup, etc.), document the names here. The template uses `player` (demo) and `agent_example` (typed-interface stub) out of the box.
- **Resource references**: prefer exported `PackedScene`/resource references on scene roots, or a small content registry resource, over hardcoding `GD.Load<T>(...)` paths in runtime classes. See `resources/README.md` for the C# `.tres` script-binding requirements.

## Local Godot Docs

Explore tasks against the local copy of Godot docs should be handed off to the `opencode` CLI harness instead of using the `Explore` tool or even directly `grep`, `glob`, or other shell-based commands while running in the `claude` CLI harness.

When invoking `opencode run` for doc exploration, frame the prompt so it:
1. Asks opencode to **explore** the `docs` directory (not search for a string).
2. Specifies that only a **concise summary** should be returned as output — no raw file contents or tool call results.

Example: `opencode run "Explore the Godot 4 documentation at <project-root>/docs to understand how <Topic> works. Return only a concise summary of key properties, signals, methods, and usage patterns."`
