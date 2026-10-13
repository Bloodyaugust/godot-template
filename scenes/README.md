# scenes/

Godot scene files (`.tscn`).

Mirror the structure of `scripts/` (e.g. `scenes/units/`, `scenes/ui/`). The main scene is set via `run/main_scene` in `project.godot`.

| Path | Role |
|------|------|
| `main.tscn` | The template's demo main scene (controller `scripts/ui/MainScreen.cs`): the demo Player (`scripts/test/`) with a `ZoomCamera` following it, the demo `main.quit` button, the scene-handoff demo's `main.travel` button, and the chrome — the shared top bar and menu overlay on canvas layer 20. Replace it with your own when starting real work, keeping the chrome pattern. |
| `ui/` | Shared UI pieces (top bar, menu overlay, settings panel, the transition overlay); see `ui/README.md`. |
| `demo/` | The scene-handoff demo's destination (`destination.tscn`), reached from `main.tscn`'s Travel; see `demo/README.md`. |
