# tests/unit/

The engine-free C# unit-test rig: an xUnit project run with plain
`dotnet test tests/unit/godottemplate.Tests.csproj` — no Godot, no booted game.
It covers the pure logic in `scripts/core/`; whole-game behavior stays with the
Hurl scenarios in `tests/hurl/`.

The project does **not** reference the game project. Instead its `.csproj`
compiles `scripts/core/**/*.cs` directly, which is the engine-free boundary's
enforcement: a Godot type reaching `scripts/core/` breaks this project's
compile. If an engine-free type can't be tested here, it's on the wrong side of
the boundary — restructure it, don't mock the engine.

Tests are grouped per area: each `tests/unit/<Area>/` directory mirrors the
`scripts/core/<area>/` it covers and carries its own `README.md` listing its test
files, with a row in the table below pointing at it.

| File | Covers |
|------|--------|
| `godottemplate.Tests.csproj` | The rig: xUnit + the `scripts/core/` source include. Deliberately not in `godot-template.sln`, so Godot's editor build never compiles it. |
| `GameVersionTests.cs` | `GameVersion`: `Current` well-formedness, numeric ordering, empty-sorts-first, malformed rejection — and the proof the wiring works. |
| `Content/` | `scripts/core/content/`: the JSON definition reader's typed accessors and fail-loud paths. |
| `Loading/` | `scripts/core/loading/`: the background loader's publish-on-poll, counting, warnings and fault handling. |
| `Persistence/` | `scripts/core/persistence/`: the record codec round-trip, bad-file reads and the one-file-per-record folder. |
| `Validation/` | `scripts/core/validation/`: `BuildResult<T>` collecting every `BuildIssue`. |
