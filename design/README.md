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
| `visual/` | Visual-design notes: the visual language, palette tokens, typography, component conventions. |
| `mocks/` | Self-contained HTML mocks of in-game screens. Open directly in a browser. Each mock is a throwaway exploration, not a spec — promote decisions that survive into `visual/`. |

> Subdirectories are created as content arrives — don't pre-create empty folders.
> When a mock settles into a real decision, capture the *why* in `visual/` or
> `game/` so the rationale outlives the throwaway HTML.

## Relationship to `ideas/`

`ideas/` holds in-flight, exploratory notes for features being worked on;
`design/` holds the settled, authoritative result. When an idea ships and its
design stabilizes, fold the durable decisions into the relevant `design/` doc
(and move the idea file to `ideas/done/`).
