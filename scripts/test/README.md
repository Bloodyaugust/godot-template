# scripts/test/

Throwaway demo content used to verify the agent REST surface end-to-end out of the box.

- `Player.cs` — `Sprite2D` controller. Polls `move_<up|down|left|right>` actions in `_Process` and slides the sprite WASD-style. Adds itself to the `"player"` group on `_Ready` and implements `IAgentInspectable` to expose `Speed`. Also handles the demo `QuitButton`'s `pressed` signal via `OnQuitPressed`. Used by `scenes/main.tscn`.

The bundled `scenes/main.tscn` also contains a `QuitButton` carrying `metadata/agent_id = "main.quit"` — wired to `Player.OnQuitPressed` — that demonstrates the generic UI-control pattern documented in `scripts/server/README.md` (`/ui/controls`, `/ui/press`).

Safe to delete once you start building real game systems — drop the `Sprite2D` and `QuitButton` from `scenes/main.tscn`, remove this directory, and replace the main scene with your own.
