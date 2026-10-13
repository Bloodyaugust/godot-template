# resources/definitions/

Authored game content as **plain JSON** — distinct from the `.tres` resources in the
rest of `resources/`. These are the definition files loaded at runtime by
`scripts/content/` (the loader), parsed into the immutable definition objects your
registries serve by id. The directory starts empty; add one file per definition type
(`structures.json`, `enemies.json`, …) as the game grows content; the
`ContentBootstrap` autoload (`scripts/content/`) loads every file at boot.

| File | Holds |
|------|-------|
| `examples.json` | **Placeholder — delete it** with its parser (`scripts/core/content/ExampleDef.cs`), tests and `ContentBootstrap` line once a real definition file lands. Three `ExampleDef` records (id, name, an int `cost`, a snake_case `kind`) so the pipeline runs at boot and `/content/state` has something to report. |

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
build automatically. Every preset in `export_presets.cfg` carries

```
include_filter="resources/definitions/*.json"
```

in its `[preset.N]` section (Export dialog → *Resources* tab → "Filters to export
non-resource files/folders"). Keep it on any preset you add (merge, comma-separated,
with any other filter it needs), or the loader fails at startup in the exported build
while working fine in the editor. The release workflow's smoke check boots the export
but only checks the exit code, so a missing file there shows as a `[Content]` error in
its log, not a failed job.
