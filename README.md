# godot-template

An opinionated Godot 4.7 Mono (C#) project template. Use as a starting point for game jams, prototypes, or small projects.

## Dependencies

- Godot 4.7 mono
- `dotnet` CLI (.NET 8 SDK; .NET 9 if targeting Android)
- [Hurl](https://hurl.dev) — runs the scripted agentic REST tests in `tests/hurl/` (verification only; not needed to build or play)

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
| `resources/` | Game content data: `.tres` resources (`[GlobalClass]` Resource subclasses) plus `definitions/`, plain-JSON content loaded at runtime by `scripts/content/`. |
| `shaders/` | `.gdshader` files. |
| `sprites/` | Sprite images and their `.import` sidecars (individual sprites and packed atlases). |
| `ideas/` | Markdown design notes for in-flight features; move completed ones to `ideas/done/`. |
| `design/` | Authoritative, settled design documentation — game-system specs (`game/`), visual language (`visual/`), throwaway HTML screen mocks (`mocks/`). See `design/README.md`. |
| `tests/` | Automated tests. `tests/hurl/` holds [Hurl](https://hurl.dev) REST scenarios that drive a booted game for agentic verification; `tests/unit/` is the engine-free xUnit project (`dotnet test`) over `scripts/core/`. |

Each non-gitignored subdirectory should carry its own `README.md` describing its contents.

## Releasing

The authoritative game version is `GameVersion.Current` (`scripts/core/GameVersion.cs`).
Cutting a release: bump the constant in a commit, then push the matching `v*` tag
(e.g. `v0.2.0`) — that runs `.github/workflows/publish-itch.yml`: it fails fast on a
tag-vs-constant mismatch, exports the `export_presets.cfg` Windows and Linux release
presets headless on CI, stamps `project.godot` `config/version` from the constant,
smoke-checks that the exported Linux binary boots, and — when an itch.io project is
configured in the workflow's `ITCH_PROJECT` env — publishes both builds with butler
(requires the `BUTLER_API_KEY` repository secret, an itch.io API key). A manual run
from the Actions tab is a dry run — build and smoke check only, no publish. Details:
`.github/workflows/README.md`.

## Agent REST interface

A debug-build-only HTTP server (`scripts/server/AgentRestServer.cs`, registered as an autoload) lets external agents drive a running game — trigger input actions and query nodes by group. Defaults to `http://127.0.0.1:8080/`, override with the `GODOT_AGENT_REST_PREFIX` env var. See `scripts/server/README.md` for endpoint details and `curl` examples. The shipped `scenes/main.tscn` is a small demo scene wired up to verify the surface end-to-end.

Multi-step flows are scripted as [Hurl](https://hurl.dev) scenarios in `tests/hurl/`, which drive this interface and wait on game state rather than racing the live simulation — see `tests/hurl/README.md`.

## Tooling

- `CLAUDE.md` — instructions for [Claude Code](https://claude.com/claude-code).
- `export_presets.cfg` — Godot export presets ("Windows Release", "Linux Release"), built locally via `godot-mono --headless --export-release "<preset>" build/<platform>/<binary>` (output lands in the gitignored `build/`) and by the release pipeline above.
- `HANDOFF.md` — opt-in session-to-session handoff brief: what the last work session shipped and what the next one should pick up. Only used when a human explicitly asks for the handoff flow; see the header inside the file.
- `milestones.md` — the milestone checklist grounding "what should the next session target" conversations; updated as items complete.
- `AGENTS.md` — symlink-style pointer for agentic CLI tools that look up `AGENTS.md` instead of `CLAUDE.md`.
- `opencode.json` — [opencode](https://opencode.ai) configuration; primarily used to hand off Godot docs exploration tasks (see `CLAUDE.md`).
- `.editorconfig` — editor-agnostic formatting rules.
- `.vscode/settings.json` — points VSCode's Godot Tools extension at the local Godot mono binary. Update the path to match your environment.
- `.claude/settings.json` — checked-in Claude Code permissions for common Godot / `dotnet` / debug-REST commands so first-run permission prompts stay out of the way. Extend in PRs when a useful command is missing.
- `.claude/settings.local.json` — per-user overrides on top of the checked-in settings (gitignored).
- `.claude/skills/extract-to-template/` — project-local Claude Code skill that audits the current project for reusable patterns and produces a prioritized proposal for contributing them back to this template. Invoke as `/extract-to-template <path-to-template-checkout>` from a project that was spun out of this template.
- `.claude/skills/eject/` — one-shot Claude Code skill that converts a fresh copy of this template into a starter for a new project: interviews you about the game, renames `.csproj` / `.sln` / `project.godot` / `<RootNamespace>`, rewrites `README.md` and `CLAUDE.md`, swaps the demo scene for a minimal stub, and deletes itself. Invoke as `/eject [project-name]` from inside the newly-copied project directory.
