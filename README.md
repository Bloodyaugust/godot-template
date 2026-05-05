# godot-template

An opinionated Godot 4.6 Mono (C#) project template. Use as a starting point for game jams, prototypes, or small projects.

## Dependencies

- Godot 4.6 mono
- `dotnet` CLI (.NET 8 SDK; .NET 9 if targeting Android)

## Getting started

1. Clone or copy this template into a new directory.
2. Find-replace every instance of `godot-template` with your project name. Files affected:
   - `godot-template.csproj` → rename to `<your-name>.csproj`
   - `godot-template.sln` → rename to `<your-name>.sln`
   - `project.godot` (`config/name`, `project/assembly_name`)
   - `godot-template.csproj` `<RootNamespace>` (use a valid C# identifier, e.g. `mygame`)
3. Open the project in Godot.
4. Build the dotnet project from inside Godot (top-right hammer, left of the Play button) to populate `.godot/` and verify the toolchain is wired up.
5. `dotnet build` from a shell to confirm a clean compile outside of Godot.

## Folder structure

| Folder | Purpose |
|--------|---------|
| `scripts/` | C# source files for game logic. Add subdirectories per system as the project grows. |
| `scenes/` | `.tscn` files. Mirror the structure of `scripts/`. |
| `resources/` | `.tres` files defining game content (`[GlobalClass]` Resource subclasses). |
| `shaders/` | `.gdshader` files. |
| `sprites/` | Individual sprite images and their `.import` sidecars. |
| `spritesheets/` | Packed spritesheet images. |
| `ideas/` | Markdown design notes for in-flight features; move completed ones to `ideas/done/`. |

Each non-gitignored subdirectory should carry its own `README.md` describing its contents.

## Tooling

- `CLAUDE.md` — instructions for [Claude Code](https://claude.com/claude-code).
- `AGENTS.md` — symlink-style pointer for agentic CLI tools that look up `AGENTS.md` instead of `CLAUDE.md`.
- `opencode.json` — [opencode](https://opencode.ai) configuration; primarily used to hand off Godot docs exploration tasks (see `CLAUDE.md`).
- `.editorconfig` — editor-agnostic formatting rules.
- `.vscode/settings.json` — points VSCode's Godot Tools extension at the local Godot mono binary. Update the path to match your environment.
- `.claude/settings.local.json` — local Claude Code permissions and plugin enablement (gitignored).
