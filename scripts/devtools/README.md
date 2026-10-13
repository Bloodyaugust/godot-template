# scripts/devtools/

Debug-build-only tools for humans watching the game: views that may deliberately show what a
player isn't entitled to see, so they never exist in a release build. Namespace:
`godottemplate.DevTools` (not `Debug`, which would shadow `System.Diagnostics.Debug` across
the project).

| File | Role |
|------|------|
| `DebugView.cs` | The autoload holding the global debug-view flag (`DebugView.Enabled`) that every debug-only view reads. Polls the `debug_toggle` action (tilde by physical key) unless a text field has focus, so `/input/action` taps reach it too. In a release build the flag stays off and setting it refuses. Implements `IAgentDebug` (`scripts/server/`; group `agent_debug`, `/debug/state` and `/debug/set`). Covered by `tests/hurl/debug_view.hurl`. |
| `DevLab.cs` | Controller for the dev-only lab `scenes/devtools/dev_lab.tscn` — the worked example of a dev-only scene (`CLAUDE.md`, *Dev-only scenes*): booted by path, never reached from the game (`GameScenes.DevOnlyLab` names the path, nothing navigates to it). Seeds a `System.Random` from `GODOT_DEVLAB_SEED` (invariant-culture uint64; a bad value warns and falls back to the default) and draws from it on Reroll (`devlab.reroll`). Shared chrome (`devlab.menu.*`). Implements `IAgentDevLab` (`scripts/server/`; group `agent_devlab`, `/devlab/state`, `/devlab/reroll`, `/devlab/seed`; elsewhere a 503 naming the scene). Covered by `tests/hurl/devlab.hurl` and `devlab_absent.hurl`. |

A debug view of your own (labels over agents, an overlay of hidden state) reads
`DebugView.Enabled` each frame and shows or hides itself; put its scene under
`scenes/devtools/` (see `scenes/devtools/README.md`).
