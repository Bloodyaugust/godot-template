# scripts/core/content/

The **engine-free** JSON definition reader — no Godot types (compiled straight
into the `tests/unit/` rig). It parses injected text; the Godot side
(`scripts/content/`) reads `res://` files, bridges errors to the logged
`ContentException`, and latches failures. The load path and registry pattern are
documented in `scripts/content/README.md`; file conventions in
`resources/definitions/README.md`.

- `JsonDefReader.cs` — typed, fail-loud accessor over one `System.Text.Json`
  object: required/optional strings, integers (fractional numbers rejected),
  bools, string lists, `[x, y]` int pairs, string/int maps, arrays, snake_case
  enums (`ReqEnum<T>`), and `RejectUnknownKeys`. `ParseArray` parses a whole
  file's top-level array.
- `ContentError.cs` — the engine-free content failure, formatted as one
  `[Content] file (context): reason` line.

Namespace: `godottemplate.Core.Content`. Tests: `tests/unit/Content/`.
