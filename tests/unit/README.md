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

| File | Covers |
|------|--------|
| `godottemplate.Tests.csproj` | The rig: xUnit + the `scripts/core/` source include. Deliberately not in `godot-template.sln`, so Godot's editor build never compiles it. |
| `GameVersionTests.cs` | `GameVersion`: `Current` well-formedness, numeric ordering, empty-sorts-first, malformed rejection — and the proof the wiring works. |
