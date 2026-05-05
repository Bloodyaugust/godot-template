This is a Godot 4.6 Mono (C#) game project. Read the root @README.md for a human-centric description of the game.

Update this file whenever major architectural changes occur.

Observe the README.md file in a subdirectory when making changes there. If you create or find a subdirectory that is not gitignored when looking for relevant source or making changes, create one. If you meaningfully change functionality in a directory, update its README.md.

For Godot-specific documentation searches, there is a `docs` directory in the root of the project that contains all of the Godot documentation, downloaded locally. Prefer that to searching the internet for Godot documentation. (`docs/` is gitignored — populate it locally if missing.)

After making code changes, always run `dotnet build` to verify the project compiles before reporting the work as done.

After you make changes, provide a summary of what was changed, and prompt the user to test those changes.

## Architecture

### Main Scene
The main scene is set via `run/main_scene` in `project.godot`. This template ships without one — add your scene under `scenes/` and point `run/main_scene` at it.

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
