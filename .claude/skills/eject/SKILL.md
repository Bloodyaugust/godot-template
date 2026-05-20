---
name: eject
description: Convert a fresh copy of the godot-template into a ready-to-go starter for a new Godot/C# game. Interviews the user about the game (name, pitch, genre, 2D/3D, core mechanics, key systems), renames the project across .csproj / .sln / project.godot / RootNamespace, rewrites the root README.md to describe the new game (human-facing), seeds a "Game Overview" section in CLAUDE.md, swaps the demo main scene for a minimal stub that keeps the agent REST verification surface working, deletes scripts/test/, and removes itself. Use when the user invokes /eject from a freshly-copied template directory, or asks to "eject", "rename this template", or "scaffold the project for my new game".
---

# Eject the template into a real project

You are converting a freshly-copied checkout of the `godot-template` into a
project-specific starter. The user has already cloned/copied the template into
a new directory and is running `/eject` from inside it. By the end of this
skill the project must be renamed, the root `README.md` and `CLAUDE.md` must
describe the user's game, the demo content must be replaced with a minimal
stub that keeps the agent REST surface verifiable, and this skill must remove
itself.

This is a destructive, one-shot operation. Work through the phases in order
and confirm before each batch of mutations.

## Inputs

- `PROJECT_NAME` — optional positional argument. If absent, default to the
  basename of `cwd`.
- `cwd` — the project being ejected. All file operations are relative to it.

## Phase 1 — Preconditions

Do not mutate anything until **all** of these pass. Surface a clear failure
message and stop if any check fails.

1. **Looks like the template.** `project.godot` exists and contains
   `config/name="godot-template"`. `godot-template.csproj` and
   `godot-template.sln` exist. `scripts/server/AgentRestServer.cs` exists.
2. **Not the template repo itself.** Run `git remote get-url origin` (if a
   remote exists). If the URL points at the upstream `godot-template`
   repository, refuse — ejecting the template itself would clobber it. If the
   remote is ambiguous, show it to the user and ask whether to proceed.
3. **Clean working tree.** `git status --porcelain` must be empty. Ask the
   user to commit or stash first so the eject lands as its own reviewable
   diff.
4. **Not already ejected.** If `config/name` in `project.godot` is anything
   other than `"godot-template"`, abort with "looks like this project has
   already been ejected."

## Phase 2 — Resolve project identity

1. Resolve `PROJECT_NAME`. Strip whitespace, reject empty.
2. Derive a default `<RootNamespace>` from `PROJECT_NAME`: lowercase, strip
   every non-`[A-Za-z0-9]` character, and ensure the result starts with a
   letter (prefix with `g` if it begins with a digit). Reject if the result
   is empty.
3. Show the resolved name and namespace to the user via `AskUserQuestion` and
   let them override the namespace if they want a different identifier.

## Phase 3 — Interview

Collect the inputs that will seed `README.md` and `CLAUDE.md`. Use
`AskUserQuestion` for the multi-choice items and a short free-form text
exchange (a normal assistant message asking the question) for the prose
items. Cache every answer; do not write to disk yet.

1. **Pitch** — one-paragraph elevator description. Free-form.
2. **Genre / format** — `AskUserQuestion` with options like Roguelite,
   Deckbuilder, RTS/Strategy, Platformer, Puzzle, Sim/Management, plus an
   open slot via "Other".
3. **2D vs 3D** — `AskUserQuestion` single-select.
4. **Core mechanics** — 3–5 bullets of what the player actually does.
   Free-form.
5. **Key systems** — top-level systems likely to need their own subdirectory
   (combat, inventory, dialogue, builder, world generation, etc.). Free-form.

## Phase 4 — Show the plan, get final confirmation

Print a concise summary block:

- New project name and namespace.
- Files to rename (csproj, sln, csproj.uid if present).
- Files to edit (`project.godot`, csproj, sln, every `.cs` file containing
  `godottemplate`, `README.md`, `CLAUDE.md`).
- Files to delete (`scripts/test/`, `.claude/skills/eject/`).
- Files to create (new `scenes/main.tscn`, plus any companion script for the
  QuitButton wiring).

Then ask one final yes/no via `AskUserQuestion` before mutating anything.

## Phase 5 — Mutations

Do these in order. Use the dedicated file tools (Edit/Write/Read) over shell
where possible. Use Bash/PowerShell for renames and deletions.

### 5.1 Rename project files

- `godot-template.csproj` → `<PROJECT_NAME>.csproj`
- `godot-template.sln` → `<PROJECT_NAME>.sln`
- `godot-template.csproj.uid` → `<PROJECT_NAME>.csproj.uid` (if it exists)

### 5.2 Edit the renamed `.csproj`

Replace `<RootNamespace>godottemplate</RootNamespace>` with
`<RootNamespace><CHOSEN_NAMESPACE></RootNamespace>`.

### 5.3 Edit the renamed `.sln`

Replace every occurrence of `godot-template` with `<PROJECT_NAME>` (project
references, project display name, and any other matches).

### 5.4 Edit `project.godot`

- `config/name="godot-template"` → `config/name="<PROJECT_NAME>"`
- `project/assembly_name="godot-template"` → `project/assembly_name="<PROJECT_NAME>"`

### 5.5 Namespace rewrite across `.cs` files

1. Use Grep to enumerate every `.cs` file containing `godottemplate`.
2. For each file, replace `namespace godottemplate` (and any
   `namespace godottemplate.<X>`) with `namespace <CHOSEN_NAMESPACE>`
   (preserving the `.<X>` suffix), and replace any other `godottemplate.`
   reference (using statements, fully-qualified type names) with
   `<CHOSEN_NAMESPACE>.`.

Known starting points (verify with Grep, do not assume exhaustive):

- `scripts/server/AgentRestServer.cs`
- `scripts/server/IAgentExample.cs`
- `scripts/server/IAgentInspectable.cs`

### 5.6 Swap the demo scene

Write a fresh `scenes/main.tscn` containing:

- A root `Node` named `Main`.
- A `Control` child anchored full-rect.
- A `Button` child of the Control named `QuitButton`, with `text = "Quit"`,
  `metadata/agent_id = "main.quit"`, and its `pressed` signal connected to a
  `_on_quit_pressed` handler on the root.

Implement the handler as a tiny C# script `scripts/main/Main.cs` (namespace
`<CHOSEN_NAMESPACE>.Main`) that calls `GetTree().Quit()` and is set as the
script on the root node of the scene. Add a `scripts/main/README.md`
describing the directory (one short paragraph).

The smoke-test bar: `GET /ui/controls` lists `main.quit` and
`POST /ui/press {"id":"main.quit"}` terminates the process.

### 5.7 Delete demo code

Remove `scripts/test/` recursively (`Player.cs`, `Player.cs.uid`,
`README.md`).

### 5.8 Rewrite `README.md`

Replace the entire file with a project-specific README seeded from the
interview. Required sections, in order:

- `# <PROJECT_NAME>` heading.
- One-paragraph pitch (from the interview).
- A bolded line summarizing genre / format and 2D-vs-3D.
- **Dependencies** — copy verbatim from the template README; still accurate.
- **Getting started** — trim to just the post-clone steps: open in Godot,
  build via the Godot hammer, then `dotnet build` from a shell. The rename
  step is no longer relevant.
- **Folder structure** — copy the table from the template README.
- **Agent REST interface** — copy the paragraph from the template README.
- **Tooling** — copy the entries from the template README *except* the
  `/eject` bullet (which is being deleted in this run). Keep the
  `extract-to-template` bullet — it's still useful in the spun-out project.

### 5.9 Edit `CLAUDE.md`

- Leave line 1 alone.
- Insert a new `## Game Overview` section immediately after line 1
  containing: pitch (one paragraph), a `**Genre / format**: …` line, a
  `**Dimension**: 2D | 3D` line, a `**Core mechanics**` bullet list, and a
  `**Key systems**` bullet list. All seeded from interview answers.
- In **Architecture → Main Scene**, replace the description of the demo
  scene (Sprite2D + QuitButton driven by `scripts/test/Player.cs`) with one
  line describing the new stub: a `Main` node with a `QuitButton` whose
  `agent_id = "main.quit"` triggers `GetTree().Quit()` via
  `scripts/main/Main.cs`. Drop the "Replace the scene with your own when
  starting real work and delete `scripts/test/`" sentence — already done.
- Under **Key Patterns → Namespaces**, replace the `godottemplate.Foo`
  example with `<CHOSEN_NAMESPACE>.Foo` and update the parenthetical example
  of `<RootNamespace>` to use the chosen namespace.

### 5.10 Self-delete

Remove the entire `.claude/skills/eject/` directory. Do this as the **last**
mutation, after every other change has been written to disk.

## Phase 6 — Verification

1. `dotnet build` — must succeed. If it fails, surface the error verbatim and
   stop. Do not try to fix compilation errors inline; the user will read the
   diff and decide.
2. Boot the game briefly with `godot-mono --path . --headless` (or windowed,
   in the background) and confirm `GET /status` returns 200 and
   `GET /ui/controls` lists `main.quit`. Then `POST /ui/press` with
   `{"id":"main.quit"}` (or `GET /quit` as fallback) to tear down the
   instance. Per the project's CLAUDE.md cleanup rule, never leave a
   `godot-mono` process running after the skill exits — including on error
   paths.

## Phase 7 — Report

End with a short summary listing:

- New project name and namespace.
- File renames / edits / deletes performed.
- Build + REST verification results.
- Next steps for the user: review `git status` / `git diff`, populate
  `docs/` per `CLAUDE.md`, commit when satisfied.

Do **not** commit the changes automatically — the user reviews and commits
themselves.

## Constraints

- **One reviewable commit's worth of changes.** Never commit.
- **Confirm before each destructive batch.** Renames, the namespace rewrite,
  and the self-delete each get a one-line "proceed?" checkpoint with
  `AskUserQuestion`.
- **Stop on first build failure.** Don't iterate on fixes inline.
- **Do not touch** `docs/`, `.godot/`, `.idea/`, `bin/`, `obj/`, or any build
  output.
- **Do not change `.claude/settings.json`.** Its permissions remain valid
  under the new project name.
- **Do not modify `.claude/skills/extract-to-template/`.** It is still useful
  in the ejected project.
