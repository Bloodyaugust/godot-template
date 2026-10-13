# design/

Home for **design decisions and documentation** — both *game design* (mechanics,
systems, economy, progression, mission/level structure) and *visual design* (UI
layout, visual language, palette, typography, screen mocks).

This is documentation, not shipped code. Nothing here is loaded by the game; it
exists so decisions are recorded, debated, and iterated on before they cost
engine time.

Once a system settles, its doc here is **authoritative**: implementation reads
against it, not the other way around. When code and a design doc disagree,
either the code is wrong or the doc needs a deliberate revision — never let
them silently drift.

## Layout

| Path | Purpose |
|------|---------|
| `game/` | Game-design notes: systems, economy, mission/level structure, progression, balance reasoning. Once it exists, `game/README.md` indexes each system doc and carries the project glossary and global constants — start there. |
| `visual/` | Visual-design notes: the visual language, palette tokens, typography, component conventions. `visual/README.md` indexes them; it ships with `ui-scale.md` (the 1280×720 canvas floor and the window-capped auto UI scale). |
| `mocks/` | Throwaway mocks of in-game screens: self-contained HTML (open in a browser) or Godot `.tscn` scenes that instance the real component scenes (open in the editor). Each mock is an exploration, not a spec. Promote decisions that survive into `visual/`. |
| `_system-template.md` | Outline for a system design doc (see *Writing a system doc* below). Copy and rename; not itself a system doc. |

> Subdirectories are created as content arrives — don't pre-create empty folders.
> When a mock settles into a real decision, capture the *why* in `visual/` or
> `game/` so the rationale outlives the throwaway HTML.

## Writing a system doc

Start every system doc from `_system-template.md`: a scope paragraph, the pointer to the
glossary and to the code that states the mechanics, `## Goals`, topic sections carrying
their reasoning in inline markers (*Why:*, *Rejected:*, *Balance intent:*, *Revised
YYYY-MM-DD*, *Settled while building (date)*), `## Agent surface`, an optional `## Built-ins
and the amendment`, and `## Deferred` last. Revisions are made in place, beside the rule
they change, with a dated marker — the doc is never a changelog. Postponed work lives only
in the owning doc's *Deferred*; other docs cross-reference it.

## The `game/` index

Create `game/README.md` with the first system doc. It carries three sections:

- **Index** — a table of every system doc (`| Doc | System |`), one or two sentences each
  on what the doc owns.
- **Glossary** — one bold term per bullet with a short definition. Every system doc points
  here instead of defining shared terms itself.
- **Global constants** — opened with the line "Named here; the code holds the values." Each
  bullet names a constant and where it lives (`Class.Member`, `scripts/<path>.cs`), never
  its number, so the value is stated once, in code.

## Mocks

A `.tscn` mock instances the real component scenes (with editable children) and fills them
with a worked example, so it stays honest about the real layout. Once `mocks/` exists, its
`README.md` lists each mock and what it shows. A `.tscn` mock can be rendered to PNGs
without the editor (after a `dotnet build`) by running it briefly under Godot's movie
writer; the frames land in the gitignored `tmp/`:

```sh
godot-mono --path . --write-movie tmp/mock/frame.png --fixed-fps 10 --quit-after 6 \
  --resolution 1920x1080 res://design/mocks/<mock>.tscn
```

Pin `GODOT_PROFILE_MEMORY=1` so the render never writes the player's profile, and point
`GODOT_AGENT_REST_PREFIX` at a spare port so the debug REST server doesn't collide with
another running instance.

## Relationship to `ideas/`

`ideas/` holds in-flight, exploratory notes for features being worked on;
`design/` holds the settled, authoritative result. When an idea ships and its
design stabilizes, fold the durable decisions into the relevant `design/` doc
(and move the idea file to `ideas/done/`).
