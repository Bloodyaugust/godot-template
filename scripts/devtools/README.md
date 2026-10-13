# scripts/devtools/

Debug-build-only tools for humans watching the game: views that may deliberately show what a
player isn't entitled to see, so they never exist in a release build. Namespace:
`godottemplate.DevTools` (not `Debug`, which would shadow `System.Diagnostics.Debug` across
the project).

| File | Role |
|------|------|
| `DebugView.cs` | The autoload holding the global debug-view flag (`DebugView.Enabled`) that every debug-only view reads. Polls the `debug_toggle` action (tilde by physical key) unless a text field has focus, so `/input/action` taps reach it too. In a release build the flag stays off and setting it refuses. Implements `IAgentDebug` (`scripts/server/`; group `agent_debug`, `/debug/state` and `/debug/set`). Covered by `tests/hurl/debug_view.hurl`. |

A debug view of your own (labels over agents, an overlay of hidden state) reads
`DebugView.Enabled` each frame and shows or hides itself; put its scene under
`scenes/devtools/`.
