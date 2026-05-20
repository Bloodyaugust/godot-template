# godot-template

An opinionated Godot 4.6 Mono (C#) project template. Use as a starting point for game jams, prototypes, or small projects.

## Dependencies

- Godot 4.6 mono
- `dotnet` CLI (.NET 8 SDK; .NET 9 if targeting Android)

## Getting started

1. Clone or copy this template into a new directory.
2. Rename the project. Either:
   - **Recommended (Claude Code):** run `/eject [project-name]` from inside the new directory. The skill interviews you about the game, renames `.csproj` / `.sln` / `project.godot` / `<RootNamespace>`, rewrites `README.md` and `CLAUDE.md` to describe your project, swaps the demo scene for a minimal stub, and removes itself. See `.claude/skills/eject/`.
   - **Manual:** find-replace every instance of `godot-template` with your project name across `godot-template.csproj` (rename + `<RootNamespace>`), `godot-template.sln` (rename), and `project.godot` (`config/name`, `project/assembly_name`).
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
| `sprites/` | Sprite images and their `.import` sidecars (individual sprites and packed atlases). |
| `ideas/` | Markdown design notes for in-flight features; move completed ones to `ideas/done/`. |

Each non-gitignored subdirectory should carry its own `README.md` describing its contents.

## Agent REST interface

A debug-build-only HTTP server (`scripts/server/AgentRestServer.cs`, registered as an autoload) lets external agents drive a running game — trigger input actions and query nodes by group. Defaults to `http://127.0.0.1:8080/`, override with the `GODOT_AGENT_REST_PREFIX` env var. See `scripts/server/README.md` for endpoint details and `curl` examples. The shipped `scenes/main.tscn` is a small demo scene wired up to verify the surface end-to-end.

## Tooling

- `CLAUDE.md` — instructions for [Claude Code](https://claude.com/claude-code).
- `AGENTS.md` — symlink-style pointer for agentic CLI tools that look up `AGENTS.md` instead of `CLAUDE.md`.
- `opencode.json` — [opencode](https://opencode.ai) configuration; primarily used to hand off Godot docs exploration tasks (see `CLAUDE.md`).
- `.editorconfig` — editor-agnostic formatting rules.
- `.vscode/settings.json` — points VSCode's Godot Tools extension at the local Godot mono binary. Update the path to match your environment.
- `.claude/settings.json` — checked-in Claude Code permissions for common Godot / `dotnet` / debug-REST commands so first-run permission prompts stay out of the way. Extend in PRs when a useful command is missing.
- `.claude/settings.local.json` — per-user overrides on top of the checked-in settings (gitignored).
- `.claude/skills/extract-to-template/` — project-local Claude Code skill that audits the current project for reusable patterns and produces a prioritized proposal for contributing them back to this template. Invoke as `/extract-to-template <path-to-template-checkout>` from a project that was spun out of this template.
- `.claude/skills/eject/` — one-shot Claude Code skill that converts a fresh copy of this template into a starter for a new project: interviews you about the game, renames `.csproj` / `.sln` / `project.godot` / `<RootNamespace>`, rewrites `README.md` and `CLAUDE.md`, swaps the demo scene for a minimal stub, and deletes itself. Invoke as `/eject [project-name]` from inside the newly-copied project directory.
