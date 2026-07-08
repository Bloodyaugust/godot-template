# resources/definitions/

Authored game content as **plain JSON** — distinct from the `.tres` resources in the
rest of `resources/`. These are the definition files loaded at runtime by
`scripts/content/` (the loader), parsed into the immutable definition objects your
registries serve by id. The directory starts empty; add one file per definition type
(`structures.json`, `enemies.json`, …) as the game grows content.

## Conventions

- **One top-level JSON array per file**, one object per definition, each with an `id`.
- **Snake-case field names.** Enum-valued fields use the snake-case spelling
  (`category: "collector"`, `effect.op: "add"`); unknown values fail loudly at load.
- **Ids are the cross-file glue.** Definitions reference each other by id string only.
  Catch dangling references with a single startup validation pass — see
  `scripts/content/README.md`.
- **Fail loudly, never silently.** A missing file, malformed JSON, unknown enum, bad
  type, or dangling reference throws a `ContentException` (logged via `GD.PushError`)
  at startup.

## Export inclusion

These are non-resource files (`*.json`), so they are **not** pulled into an exported
build automatically. When an export preset is added, include
`res://resources/definitions/*.json` in its non-resource export filter (Export dialog
→ *Resources* tab → "Filters to export non-resource files/folders") — or the loader
will fail at startup in the exported build while working fine in the editor.
