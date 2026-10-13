# scripts/core/

**Engine-free** pure-C# game logic — no Godot types allowed anywhere in this
directory. The xUnit project in `tests/unit/` compiles these sources **directly**
(not via the Godot assembly), so any Godot dependency that sneaks in becomes a
compile error there: the test rig doubles as the boundary check. Put logic here
when it benefits from fast deterministic unit tests (rules engines, formats,
protocols, version/state math); keep anything that touches nodes, resources, or
the scene tree in the regular `scripts/` subdirectories.

- `GameVersion.cs` — the single authoritative game version (`Current`) plus
  dotted-numeric `Compare`/`IsValid`. The release pipeline
  (`.github/workflows/publish-itch.yml`) verifies release tags against it and
  stamps `project.godot` `config/version` from it; the `/status` REST route
  reports it (`-dev`-suffixed in debug builds).
- `content/` — the engine-free JSON definition reader. `JsonDefReader` is a
  typed, fail-loud accessor over `System.Text.Json` (unknown keys rejected,
  snake_case enums, integers only); `ContentError` is its single-line failure.
  It parses injected text — `scripts/content/` reads `res://` and bridges errors
  to the logged `ContentException`. Tests: `tests/unit/Content/`.
- `loading/` — `AsyncLoader<TIn, TOut>`, the background loader a scene runs under
  the loading screen: work on `Task.Run`, `Poll()` each frame publishes the result and
  delivers queued warnings through injected log sinks. Tests: `tests/unit/Loading/`.
- `persistence/` — the format and folder logic behind
  `scripts/session/RecordStore.cs` (one JSON file per record): `IRecordCodec<T>`
  (a record's versioned file format), `RecordJson` (the `Utf8JsonWriter` writer
  with the `version` stamp and the non-fatal reader), `IRecordFiles` with the
  in-memory backend `MemoryRecordFiles`, and `RecordFolder<T>` (id = file name,
  bad files skipped with a warning). `ExampleRecord` + `ExampleRecordCodec` are
  the deletable reference pair. Tests: `tests/unit/Persistence/`.
- `validation/` — `BuildResult<T>` / `BuildIssue`: a build that returns the
  frozen value or *every* issue found, each with a stable string code and a
  location path. Tests: `tests/unit/Validation/`.
