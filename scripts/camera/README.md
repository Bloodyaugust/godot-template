# scripts/camera/

Cameras usable by any 2D world scene. Namespace: `godottemplate.Camera`.

| File | Role |
|------|------|
| `ZoomCamera.cs` | A `Camera2D` with scroll-to-zoom. The `zoom_in` / `zoom_out` actions (the wheel, over the world only — UI that takes the wheel keeps it) step a user zoom multiplier geometrically within its exported bounds, and it eases toward that target. On its own it applies that zoom. A `Framed` camera leaves the zoom to an owner that follows or frames something, which reads `UserZoom` into its own target and switches `ZoomInputEnabled` off when the player's zoom shouldn't apply. Implements `IAgentCamera` (`scripts/server/`; group `agent_camera`, `/camera/state` and `/camera/zoom`). |

The main scene puts one under the demo Player (`scenes/main.tscn`), so `/camera/*` is live at
boot; `tests/hurl/camera.hurl` covers it.
