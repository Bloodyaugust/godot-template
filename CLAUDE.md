This is a Godot 4.6 Mono (C#) game project. Read the root @README.md for a human-centric description of the game.

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

After making code changes, always run `dotnet build` to verify the project compiles before reporting the work as done.

After you make changes, provide a summary of what was changed, and prompt the user to test those changes.

## Architecture

### Main Scene
The main scene is set via `run/main_scene` in `project.godot`. The template ships with `scenes/main.tscn`, a demo scene containing a `Sprite2D` driven by `scripts/test/Player.cs` — useful for verifying the agent REST surface end-to-end. Replace it with your own scene when starting real work and delete `scripts/test/`.

### Agent REST Server
`scripts/server/AgentRestServer.cs` is a debug-build-only autoload that exposes a small HTTP API for external agents (claude code, scripts) to drive a running game. Default bind is `http://127.0.0.1:8080/`; override with the `GODOT_AGENT_REST_PREFIX` env var. Disabled in release builds via `OS.IsDebugBuild()`.

Endpoints:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/status` | Sanity check; returns running flag, current scene path, Godot version. |
| `POST` | `/input/action` | Body `{"action": "<name>", "mode": "press"\|"release"\|"tap"}`. `tap` auto-releases after one `_Process` frame. |
| `GET` | `/nodes?group=<name>` | Returns `[ {path, name, type, position?, properties?}, ... ]` for nodes in the named group. |
| `POST` or `GET` | `/quit` | Optional `?code=<int>` query (or JSON body `{"code": <int>}` on POST). Calls `GetTree().Quit(code)` after a 5-frame delay so the response flushes first. `GET` is accepted because Windows HTTP.sys rejects bodyless POSTs with 411. |

Per-node JSON includes `position` for `Node2D`/`Node3D` and a `properties` object for nodes that implement `godottemplate.Server.IAgentInspectable` (`scripts/server/IAgentInspectable.cs`). Implementers return a `Dictionary<string, object>` of extra fields. The HTTP listener runs on a background task; all handlers execute on the main thread inside `_Process` so they may freely touch the scene tree. See `scripts/server/README.md` for examples.

### Directory Layout
- `scripts/` — C# game logic. Add subdirectories per system (e.g. `units/`, `world/`, `ui/`) as the project grows; the template keeps it flat.
- `scenes/` — `.tscn` scene files, mirroring the structure of `scripts/`.
- `resources/` — `.tres` content files (`[GlobalClass]` Resource subclasses). See `resources/README.md` for the C# `.tres` format requirements.
- `shaders/` — `.gdshader` files.
- `sprites/` — sprite images and their `.import` sidecars (both individual sprites and packed atlases).
- `ideas/` — markdown design notes for in-flight features. Move completed ideas into `ideas/done/`.

### UI Panels
Always define UI panel structure in a `.tscn` file, not entirely in C#. Control anchor/offset layout is baked into the scene before nodes enter the tree; setting anchors in `_Ready()` runs after the first layout pass and produces incorrect positioning (e.g., centered panels appear in the top-left corner). The C# controller should only wire up signals and build dynamic content (cards, rows) via `GetNode<T>("%UniqueName")`.

### Key Patterns
- **IsInstanceValid**: always use to check freed Godot objects; C# null checks are insufficient.
- **Z-index registry**: when introducing layered 2D objects, document the z-index assignments in this file so future code follows the same registry.
- **Collision layers and groups**: when introducing physics interactions, document the layer/group assignments here.
- **Resource references**: prefer exported `PackedScene`/resource references on scene roots, or a small content registry resource, over hardcoding `GD.Load<T>(...)` paths in runtime classes. See `resources/README.md` for the C# `.tres` script-binding requirements.

## Local Godot Docs

Explore tasks against the local copy of Godot docs should be handed off to the `opencode` CLI harness instead of using the `Explore` tool or even directly `grep`, `glob`, or other shell-based commands while running in the `claude` CLI harness.

When invoking `opencode run` for doc exploration, frame the prompt so it:
1. Asks opencode to **explore** the `docs` directory (not search for a string).
2. Specifies that only a **concise summary** should be returned as output — no raw file contents or tool call results.

Example: `opencode run "Explore the Godot 4 documentation at <project-root>/docs to understand how <Topic> works. Return only a concise summary of key properties, signals, methods, and usage patterns."`
