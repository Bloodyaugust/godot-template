---
name: extract-to-template
description: Audit the current project for reusable patterns, infrastructure, conventions, and agentic-workflow guidance that should be contributed back to its parent template project. Use when the user asks to "harvest", "extract", "find reusable code", or "contribute back to the template" — or otherwise wants to identify what in this project is generic enough to belong upstream. Spawns parallel subagents to survey distinct slices, then consolidates findings into a prioritized proposal for the human to approve. Does not modify either repo; output is a plan, not a port.
---

# Extract template contributions

You are auditing **the current project** to identify generally-useful patterns, interfaces, infrastructure, scaffolding, and agentic-development guidance that should be contributed back to its **parent template project**. The downstream goal is to make future projects spun out of the template inherit these improvements by default.

## Inputs

Two paths are required for this run. Cache them as variables:

- `SOURCE_ROOT` — the project being harvested (default: cwd).
- `TEMPLATE_ROOT` — the template repo that will receive contributions. **Required.** Always ask the user where they have the template checked out before doing anything else, even when operating in no-clarifying-questions mode — the whole skill is a comparison against the template, so guessing here invalidates the output. The one exception: a sibling directory or recent shell history makes the path unambiguous; in that case, state the inferred path and proceed unless the user objects.

Before spawning subagents, verify both paths exist and are readable. If `TEMPLATE_ROOT` is missing or empty, stop and surface the problem to the user instead of continuing.

## Goal

Produce a single, prioritized list of candidate contributions to the template. Each candidate must be **project-agnostic** (or trivially generalizable) and valuable to *any* project of the same kind that the template scaffolds. Domain-specific code (game mechanics, business logic, product features) is out of scope unless it demonstrates a reusable *pattern* worth lifting in abstract form.

## Method: use agent teams

**Do not do the survey yourself.** Spawn multiple `Explore` (or `general-purpose`) agents **in parallel** — single message, multiple `Agent` tool calls — each scoped to one slice of `SOURCE_ROOT`.

### Choosing slices

Slices should be picked based on what actually exists in `SOURCE_ROOT`. Before spawning, do a quick top-level inventory of the source project (root files, top-level directories, autoloads/entry points, any `CLAUDE.md` / `AGENTS.md` / per-subdir `README.md`s, `.claude/`, `.vscode/`, editor configs, CI files, devcontainer/docker, scripts dir). Use this to decide which of the following slice categories are present and worth a subagent. Merge or split as appropriate; aim for 4–8 subagents, not 20.

Common slice categories (adapt to the project at hand):

1. **Agentic / automation infrastructure** — anything that lets external agents, scripts, or CI drive the running app: debug HTTP/IPC servers, inspectable interfaces, scene/UI affordances tagged for automation, screenshot/quit/input hooks, generic UI control conventions.
2. **Agentic-workflow guidance** — `CLAUDE.md`, `AGENTS.md`, per-subdir `README.md` discipline, verification/testing workflows, end-of-session report formats, cleanup rules, docs-exploration handoffs, local-docs conventions.
3. **UI / scene / view conventions** — separation of declarative structure (scene/markup/template files) from imperative wiring (code), unique-name access patterns, metadata conventions for automation, lifecycle/validity rules.
4. **Data / resource / content layer** — content-as-data conventions, resource binding/loading patterns, registries vs. hardcoded paths, export-references vs. dynamic loads.
5. **Application state / flow** — top-level state machines, scene/route directors, strategy interfaces with default implementations, fallback-for-direct-boot patterns.
6. **Project scaffolding** — `.editorconfig`, IDE/editor config (`.vscode/`, JetBrains, etc.), `.claude/settings*.json`, dependency-tool configs, getting-started README sections, offline-docs download recipes, `AGENTS.md` ↔ `CLAUDE.md` linking tricks.
7. **Code conventions** — namespace ↔ directory rules, per-subdir `README.md` discipline, ideas/design-notes flow, registries (z-index, layers, groups, etc.) as a *pattern*.
8. **Build / test / CI** — build scripts, pre-commit hooks, CI workflows, test harness conventions, lint config.

For each slice, the subagent should:

- Read the relevant files in `SOURCE_ROOT` (including any in-scope `README.md`s).
- Report (≤300 words) a bulleted list of candidate items, each with:
  - **what it is** (one line)
  - **source path(s)** in `SOURCE_ROOT`
  - **why it's generally useful** for projects of this kind
  - **what would need to change to generalize** it (e.g. strip domain refs, parameterize names)
  - **rough effort**: trivial copy / light edit / significant rework
- Flag anything that *looks* general but is actually entangled with domain-specific concepts so the consolidator doesn't pollute the template.

## Before spawning the team

1. Inspect `TEMPLATE_ROOT` enough to know **what's already there** (top-level layout, existing `CLAUDE.md`/`README.md`, existing equivalents of likely candidates, autoloads/entry points, settings). Subagents should not re-recommend things the template already has. Pass a short summary of the template's current state into each subagent's prompt.
2. Read `SOURCE_ROOT`'s root `CLAUDE.md`, `AGENTS.md`, and `README.md` (whichever exist) in full so you can brief the subagents accurately on conventions in play.
3. Brief each subagent as a cold-start colleague: source root path, template root path, short summary of what already exists in the template, the slice it owns, and the report format above.

## Consolidation

After all subagents return:

1. Merge into one deduped table with columns:
   - **Candidate**
   - **Source path(s)**
   - **Why useful for the template**
   - **Generalization work**
   - **Effort** (S/M/L)
   - **Priority** (High/Med/Low)
   - **Already in template?** (Y/N/Partial)
2. Group rows by category (matching the slice categories actually used).
3. Call out any **cross-cutting decisions** the human needs to make first — questions whose answers gate multiple candidates (e.g. "Does the template want an automation/debug server at all?", "Standardize on a metadata convention for automation hooks?", "Per-subdir READMEs as a hard rule?").
4. End with a short **Recommended first batch** — the 5–8 highest-leverage items to land first, ordered by dependency. Note any items that depend on a cross-cutting decision.

## Constraints

- **Do not modify either repo.** Output is a proposal for the human to approve. Once approved, a separate session does the porting.
- Be specific: file paths, function/class names, concrete excerpts where helpful. Vague suggestions ("improve the docs") are not actionable.
- Keep the final consolidated report under ~800 words plus the table.
- Prefer lifting *patterns* over copying code verbatim when the code is entangled with domain types.
