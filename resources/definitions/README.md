# resources/definitions/

Authored game content as **plain JSON** — distinct from the `.tres` resources in the
rest of `resources/`. These are the definition files loaded at runtime by
`scripts/content/` (the loader), parsed into the immutable definition objects your
registries serve by id. The directory starts empty; add one file per definition type
(`structures.json`, `enemies.json`, …) as the game grows content.

## Conventions

- **Strict JSON: no comments, no trailing commas.** The Godot editor also parses
  `*.json` under `res://` (and complains about anything laxer), and the engine-free
  parser (`scripts/core/content/JsonDefReader.cs`) is strict to match.
- **Integers only.** The reader rejects fractional numbers, keeping content off
  floating point; scale units instead (e.g. milliseconds, hundredths). Relax it
  deliberately in the reader if a game needs floats.
- **Unknown keys fail.** Parsers list their known keys (`RejectUnknownKeys`), so a
  typoed field name is a load error, not a silently ignored default.
- **One top-level JSON array per file**, one object per definition, each with an `id`.
- **Snake-case field names.** Enum-valued fields use the snake-case spelling
  (`category: "collector"`, `effect.op: "add"`); unknown values fail loudly at load.
- **Ids are the cross-file glue.** Definitions reference each other by id string only.
  Catch dangling references with a single startup validation pass — see
  `scripts/content/README.md`.
- **Fail loudly, never silently.** A missing file, malformed JSON, unknown enum, bad
  type, or dangling reference throws (an engine-free `ContentError`, surfaced as a
  `ContentException` logged via `GD.PushError`) at startup.

## Export inclusion

These are non-resource files (`*.json`), so they are **not** pulled into an exported
build automatically. Every preset in `export_presets.cfg` needs

```
include_filter="resources/definitions/*.json"
```

in its `[preset.N]` section (Export dialog → *Resources* tab → "Filters to export
non-resource files/folders"). The template's presets ship with `include_filter=""`
because the directory starts empty — set it on each preset when the first definition
file lands, or the loader will fail at startup in the exported build while working
fine in the editor.
