---
name: eject
description: Convert a fresh copy of the godot-template into a ready-to-go starter for a new Godot/C# game. Interviews the user about the game (name, pitch, genre, 2D/3D, core mechanics, key systems, architectural considerations), renames the project across .csproj / .sln / project.godot / RootNamespace, rewrites the root README.md to describe the new game (human-facing), seeds a "Game Overview" section in CLAUDE.md, strips the demo pieces (scripts/test/, the scene-handoff demo, optionally the dev lab) down to a minimal main-scene stub that keeps the chrome and the agent REST verification surface working, flags the placeholders a real game replaces, and removes itself. Use when the user invokes /eject from a freshly-copied template directory, or asks to "eject", "rename this template", or "scaffold the project for my new game".
---

# Eject the template into a real project

You are converting a freshly-copied checkout of the `godot-template` into a
project-specific starter. The user has already cloned/copied the template into
a new directory and is running `/eject` from inside it. By the end of this
skill the project must be renamed, the root `README.md` and `CLAUDE.md` must
describe the user's game, the demo content must be replaced with a minimal
stub that keeps the agent REST surface verifiable, the placeholders a real
game replaces must be listed for the user, and this skill must remove itself.

This is a destructive, one-shot operation. Work through the phases in order
and confirm before each batch of mutations.

## Inputs

- `PROJECT_NAME` — optional positional argument. If absent, default to the
  basename of `cwd`.
- `cwd` — the project being ejected. All file operations are relative to it.

## The template surface

What a fresh template ships, and what this skill does with each piece. Verify
against the checkout (Grep/Glob) before acting — the template moves faster
than this list.

**Demo pieces** — throwaway, there only to exercise the surface out of the box:

- `scripts/test/Player.cs` — the WASD `Sprite2D` in `scenes/main.tscn` (group
  `player`), with the hub's `ZoomCamera` as its child; the `QuitButton`'s
  `pressed` is connected to it. **Always stripped.**
- The **scene-handoff demo pair** — the hub's Travel button (`main.travel`,
  in `scenes/main.tscn`, driven by `scripts/ui/MainScreen.cs`), the
  destination (`scenes/demo/`, `scripts/demo/`), `GameScenes.Destination`,
  `IAgentDemo` + `/demo/state` (group `agent_demo`). **Stripped by default**;
  the user may keep it as a working reference for `Transition` +
  `GameSession` + `AsyncLoader` until the first real screen pair exists.
- The **dev lab** — `scenes/devtools/dev_lab.tscn`, `scripts/devtools/DevLab.cs`,
  `GameScenes.DevOnlyLab`, `IAgentDevLab` + `/devlab/*` (group
  `agent_devlab`), `GODOT_DEVLAB_SEED`. The worked example of a dev-only
  scene. **Kept by default** (it is the thing to copy for a generator lab or
  an observer); stripped on request. `scripts/devtools/DebugView.cs` stays
  either way.

**Placeholders** — kept, but listed for the user in Phase 7: each is
scaffolding the game's first real system replaces, not something to delete
blind.

- `scripts/server/IAgentExample.cs` + `/example/state` (group
  `agent_example`) — the typed-interface stub; delete once a real domain
  surface exists.
- `ExampleRecord` + its codec (`scripts/core/persistence/ExampleRecord.cs`) —
  the reference record for `RecordStore<T>`; the persistence unit tests use
  it, so retarget them when a real record replaces it.
- `GameSession`'s `SessionLaunch` / `SessionResult` records
  (`scripts/session/GameSession.cs`) — placeholder-typed: replace them with
  the game's launch (the accepted mission, the chosen loadout) and result.
- The example definitions under `resources/definitions/` — with their
  definition type in `scripts/core/content/`, the content loader's wiring and
  the Hurl/unit coverage that reads them; replace them with the game's first
  real content.

**Autoloads** (`project.godot` `[autoload]`) — `DisplayScale`,
`ContentBootstrap`, `AgentRestServer`, `DebugView`, `Transition`,
`GameSession`. All infrastructure, all kept; the eject never edits
`[autoload]`.

**Hurl files** (`tests/hurl/`):

- Keepers (pass against the stub): `smoke.hurl` (after the trim in 5.6),
  `menu.hurl`, `input.hurl` and `display_input.hurl` (they press, drag and
  click the kept `main.quit`), `debug_view.hurl`, `transition.hurl` (after
  the trim in 5.6), `content.hurl` (until the example definitions are
  replaced — then it is retargeted, not deleted).
- Demo-bound: `handoff.hurl` and `handoff_direct.hurl` (go with the
  scene-handoff demo), `camera.hurl` (the camera follows the demo Player; goes
  with it), `devlab.hurl` and `devlab_absent.hurl` (go with the dev lab, only
  if it is stripped).

**Design docs** — `design/_system-template.md` is the outline every system doc
starts from; it stays, and the Phase 7 next steps point the user at it.

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
6. **Architectural considerations** — what must be designed for *now*, even
   if it is built much later, because retrofitting it would mean rewriting
   systems. Free-form; ask it as an open prompt and offer, as examples only,
   the usual candidates: AI-drivable systems (can an AI player observe and
   control everything a human can?), multiplayer with a partial view (does a
   client only ever receive what the rules entitle it to see?), determinism
   (must identical inputs give identical outcomes on every machine — replays,
   lockstep, server authority?), save/mod compatibility. These are the
   human's answers to give — do not suggest which apply to their game. "None
   yet" is a valid answer.
7. **Demo pieces to keep** — `AskUserQuestion` multi-select over the two
   optional pieces in *The template surface*: keep the scene-handoff demo
   (default: no), keep the dev lab (default: yes).

## Phase 4 — Show the plan, get final confirmation

Print a concise summary block:

- New project name and namespace.
- Files to rename (csproj, sln, csproj.uid if present).
- Files to edit (`project.godot`, csproj, sln, `export_presets.cfg`, the
  `.github/workflows/` files, every `.cs` and `.md` file containing
  `godottemplate`, `README.md`, `CLAUDE.md`, `scenes/main.tscn`,
  `scripts/ui/MainScreen.cs`, `scripts/ui/GameScenes.cs`,
  `scripts/server/AgentRestServer.cs`, `tests/hurl/smoke.hurl`,
  `tests/hurl/transition.hurl`, the READMEs that index deleted files).
- Files to delete (`scripts/test/`, `tests/hurl/camera.hurl`,
  `.claude/skills/eject/`, plus the scene-handoff demo and/or the dev lab
  per interview answer 7 — list their files from *The template surface*).
- Placeholders left in place for the user to replace (from *The template
  surface*).

Then ask one final yes/no via `AskUserQuestion` before mutating anything.

## Phase 5 — Mutations

Do these in order. Use the dedicated file tools (Edit/Write/Read) over shell
where possible. Use Bash/PowerShell for renames and deletions.

### 5.1 Rename project files

- `godot-template.csproj` → `<PROJECT_NAME>.csproj`
- `godot-template.sln` → `<PROJECT_NAME>.sln`
- `godot-template.csproj.uid` → `<PROJECT_NAME>.csproj.uid` (if it exists)
- `tests/unit/godottemplate.Tests.csproj` → `tests/unit/<CHOSEN_NAMESPACE>.Tests.csproj`
  (also update its `<RootNamespace>` to `<CHOSEN_NAMESPACE>.Tests`, and the
  `dotnet test` path named in `CLAUDE.md`'s verification workflow,
  `tests/unit/README.md` and `.github/workflows/unit-tests.yml`)

### 5.2 Edit the renamed `.csproj`

Replace `<RootNamespace>godottemplate</RootNamespace>` with
`<RootNamespace><CHOSEN_NAMESPACE></RootNamespace>`.

### 5.3 Edit the renamed `.sln`

Replace every occurrence of `godot-template` with `<PROJECT_NAME>` (project
references, project display name, and any other matches).

### 5.4 Edit `project.godot`

- `config/name="godot-template"` → `config/name="<PROJECT_NAME>"`
- `project/assembly_name="godot-template"` → `project/assembly_name="<PROJECT_NAME>"`
- Leave `[autoload]` alone — every autoload is infrastructure the stub keeps.

### 5.4b Edit `export_presets.cfg` and `.github/workflows/publish-itch.yml`

- In `export_presets.cfg`, replace the `godot-template` stem in both
  `export_path` values (`build/windows/godot-template.exe`,
  `build/linux/godot-template.x86_64`) with `<PROJECT_NAME>`.
- In `.github/workflows/publish-itch.yml`, set `BINARY_NAME: godot-template` to
  `BINARY_NAME: <PROJECT_NAME>` (it must match the export paths).

### 5.5 Namespace rewrite across `.cs` and `.md` files

1. Use Grep to enumerate every `.cs` file containing `godottemplate` — and
   every `.md` (the READMEs name their namespaces), skipping `docs/`.
2. For each file, replace `namespace godottemplate` (and any
   `namespace godottemplate.<X>`) with `namespace <CHOSEN_NAMESPACE>`
   (preserving the `.<X>` suffix), and replace any other `godottemplate.`
   reference (using statements, fully-qualified type names) with
   `<CHOSEN_NAMESPACE>.`.

Known starting points (verify with Grep, do not assume exhaustive):

- `scripts/server/` — `AgentRestServer.cs` and every `IAgent*.cs`
- `scripts/ui/` — `DisplayScale.cs`, `GameScenes.cs`, `Palette.cs`,
  `MainScreen.cs`, `Transition.cs`, the chrome scripts
- `scripts/content/` — `ContentLoader.cs`, `ContentException.cs`,
  `ContentBootstrap.cs`
- `scripts/core/` — `GameVersion.cs` and every subdirectory (`content/`,
  `loading/`, `persistence/`, `snapshots/`, `validation/`)
- `scripts/session/`, `scripts/camera/`, `scripts/devtools/`, `scripts/util/`
- `tests/unit/` — every test class

### 5.6 Swap the demo scene

Rewrite `scenes/main.tscn` as a stub that keeps the chrome:

- The root `Node2D` named `Main`, still scripted by `scripts/ui/MainScreen.cs`.
- A `UI` `CanvasLayer` (layer 1) holding the `QuitButton` with
  `text = "Quit"` and `metadata/agent_id = "main.quit"`, its `pressed` signal
  connected to an `OnQuitPressed` handler on the root.
- The `Chrome` `CanvasLayer` (layer 20) exactly as shipped: the shared
  `TopBar` (`MenuAgentId = "main.menu"`) and `MenuOverlay`
  (`AgentIdPrefix = "main.menu"`).
- No `Player`, no `Camera`, and — unless the scene-handoff demo is kept — no
  `TravelButton`.

Trim `scripts/ui/MainScreen.cs` to match: keep the chrome wiring (Menu and
Esc open the overlay, Esc closes it, the overlay's Quit quits) and its
controls-lock reason, add `OnQuitPressed` (`GetTree().Quit()`), and drop the
`Player`, `ZoomCamera` and `godottemplate.Test` references. Unless the
scene-handoff demo is kept, also drop Travel, the arrival fade-in,
`IAgentDemo` and the `godottemplate.Demo` using. Rewrite its summary to
describe the stub.

`scripts/ui/GameScenes.cs` already points its `MainMenu` constant at
`res://scenes/main.tscn`; remove `Destination` with the scene-handoff demo,
and `DevOnlyLab` with the dev lab.

Also trim the Hurl files bound to the demo scene: in `tests/hurl/smoke.hurl`,
remove the `/nodes?group=player` entry (keep everything else — the boot-wait
`/status` entry, the `/ui/controls` asserts on `main.quit` and `main.menu`,
and the closing `/ui/press` teardown); in `tests/hurl/transition.hurl`,
remove the `/demo/state` entries unless the scene-handoff demo is kept (the
transition asserts stand without them); delete `tests/hurl/camera.hurl`.

The smoke-test bar: `GET /ui/controls` lists `main.quit` and `main.menu`, and
`POST /ui/press {"id":"main.quit"}` terminates the process.

### 5.7 Delete demo code

Remove `scripts/test/` recursively (`Player.cs`, `Player.cs.uid`,
`README.md`).

Unless kept (interview answer 7), remove the scene-handoff demo:
`scenes/demo/`, `scripts/demo/`, `scripts/server/IAgentDemo.cs` (and its
`.uid`), the `Demo` `Domain<T>` and the `/demo/state` route in
`scripts/server/AgentRestServer.cs`, and `tests/hurl/handoff.hurl` /
`handoff_direct.hurl`.

If the user chose to strip the dev lab: `scenes/devtools/` (its README too),
`scripts/devtools/DevLab.cs` (and `.uid`), `scripts/server/IAgentDevLab.cs`
(and `.uid`), the `DevLab` `Domain<T>`, its routes and `HandleDevLabSeed` in
`AgentRestServer.cs`, and `tests/hurl/devlab.hurl` / `devlab_absent.hurl`.
Keep the rest of `scripts/devtools/`.

Then fix every index that named a deleted file: `scenes/README.md`,
`scripts/devtools/README.md`, `scripts/server/README.md` (its route sections
and the shipped-users list), `CLAUDE.md` (5.9). Leave the placeholders
(*The template surface*) in place.

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
- **Folder structure** — copy the table from the template README (it includes
  the `tests/` row).
- **Agent REST interface** — copy the section from the template README,
  including the Hurl-scenarios paragraph.
- **Tooling** — copy the entries from the template README *except* the
  `/eject` bullet (which is being deleted in this run). Keep the
  `extract-to-template` bullet — it's still useful in the spun-out project —
  and the `HANDOFF.md` bullet.

### 5.9 Edit `CLAUDE.md`

- Leave line 1 alone.
- Insert a new `## Game Overview` section immediately after line 1
  containing: pitch (one paragraph), a `**Genre / format**: …` line, a
  `**Dimension**: 2D | 3D` line, a `**Core mechanics**` bullet list, a
  `**Key systems**` bullet list, and an `**Architectural considerations**`
  bullet list headed "(design for these now, even before they're built)" —
  omit it if the answer was "none yet". All seeded from interview answers.
- In **Architecture → Main Scene**, replace the description of the demo
  scene (Sprite2D driven by `scripts/test/Player.cs`, the camera, Travel)
  with one line describing the new stub: a `Main` node whose `QuitButton`
  (`agent_id = "main.quit"`) quits via `scripts/ui/MainScreen.cs`, plus the
  shared top bar and menu overlay (`main.menu.*`) on layer 20. Drop the
  "Replace the scene with your own when starting real work and delete
  `scripts/test/`" sentence — already done.
- For each stripped demo piece, remove what `CLAUDE.md` says about it: the
  `player` group (always); for the scene-handoff demo, the `/demo/state` row,
  the `agent_demo` group, the *Scene handoff* paragraph's "shipped demo pair"
  sentences (keep the flow itself), the *Layered scene snapshot* and
  *Controls-lock reason* "shipped in" pointers (keep the patterns), and the
  demo buttons in *Canvas layers*; for the dev lab, the `/devlab/*` rows, the
  `agent_devlab` group and the *Dev-only scenes* paragraph's worked-example
  sentences (keep the convention).
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
2. `dotnet test tests/unit/<CHOSEN_NAMESPACE>.Tests.csproj` — must stay
   green (same stop rule).
3. **Ask the user to open the project in the Godot editor once, and wait.**
   A freshly cloned checkout has no `.godot/` import cache, and the editor is
   what builds it (and the renamed C# assembly) on first open; until that has
   happened, any `godot-mono --path .` boot — the Hurl wrapper's included —
   errors out instead of starting the game, so the scenarios fail for a
   reason that has nothing to do with the eject. Tell the user: open the
   ejected project in the Godot editor, let the import finish, press the
   Build (hammer) button, then confirm here. Do not continue until they
   confirm; then check that `.godot/` now exists before booting anything.
4. Run the remaining Hurl files through the wrapper — `tests/hurl/run-tests.ps1`
   (or `run-tests.sh`) with no file list runs them all, each in a fresh
   headless instance, and guarantees teardown; `smoke.hurl` drives `/status`
   → `/ui/controls` → `/ui/press main.quit`. A keeper that fails is a stub
   bug to report, not to patch around. If `hurl` isn't installed, fall back
   to booting with `godot-mono --path . --headless` and hand-driving the
   smoke's three calls with
   `curl` (`GET /quit` as the fallback teardown); the editor-first rule in
   step 3 applies to that boot too. Per the project's CLAUDE.md
   cleanup rule, never leave a game instance you launched running after the
   skill exits — including on error paths — and never kill `godot-mono` by
   name.

## Phase 7 — Report

End with a short summary listing:

- New project name and namespace.
- File renames / edits / deletes performed.
- Build + REST verification results.
- The placeholders left in place (*The template surface*), each with what
  replaces it.
- Next steps for the user: review `git status` / `git diff`, populate
  `docs/` per `CLAUDE.md`, start the first system's design doc by copying
  `design/_system-template.md` to `design/game/<system>.md` (and create
  `design/game/README.md` with it), commit when satisfied.

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
- **Do not modify `HANDOFF.md`.** It ships generic with empty placeholder
  sections; the new project uses it as-is.
- **Do not modify `.claude/skills/extract-to-template/`.** It is still useful
  in the ejected project.
- **Do not modify `design/_system-template.md`** or the autoload list. Both
  stay as the project's scaffolding.
