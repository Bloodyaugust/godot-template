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
