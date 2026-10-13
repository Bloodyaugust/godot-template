# scripts/

C# source files for all game logic.

Organize by system (e.g. `units/`, `world/`, `projectiles/`, `ui/`, `camera/`). Each subdirectory should mirror its corresponding folder under `scenes/` where applicable, and carry its own `README.md`.

Pure data classes — `[GlobalClass]` Resource subclasses backing `.tres` files — typically live under `scripts/data/`.

| Directory | Holds |
|-----------|-------|
| `core/` | The engine-free layer (no Godot types), compiled into `tests/unit/`. |
| `content/` | The Godot side of content loading and the `ContentBootstrap` autoload that loads it at boot. |
| `session/` | Cross-scene state: the `GameSession` carrier, persistence (`PlayerProfile`, record stores). |
| `server/` | The debug-only agent REST server and its typed `IAgent*` surfaces. |
| `ui/` | Shared UI scaffolding: palette, display scale, scene registry, the transition overlay, top bar, menu overlay, settings panel, the main scene's controller. |
| `demo/` | The scene-handoff demo's destination and the pair's shared agent state; delete with the demo. |
| `camera/` | Cameras for any 2D world scene (`ZoomCamera`). |
| `devtools/` | Debug-build-only tools for humans (the `DebugView` flag). |
| `util/` | Stateless cross-system helpers. |
| `test/` | Throwaway demo content for the main scene; delete when real work starts. |
