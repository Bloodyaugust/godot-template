# scripts/demo/

The scene-handoff demo's destination half and the pair's shared agent state (the flow:
`CLAUDE.md`, *Scene handoff*; the hub half is `scripts/ui/MainScreen.cs`). Throwaway, like
`scripts/test/`: delete it (with `scenes/demo/`, `GameScenes.Destination`, the hub's Travel
button and `IAgentDemo`) once the game has its own screens, keeping the pattern.

| File | Role |
|------|------|
| `DestinationScreen.cs` | Controller for `scenes/demo/destination.tscn`: reads the launch from `GameSession` (a direct boot launches itself and puts its own loading screen up), runs a seeded placeholder job on an `AsyncLoader` (`scripts/core/loading/`), fades in, frees the controls on `Transition.Finished`; Return (`destination.return`) records a `complete` result and goes back the same way; the menu's Quit asks through the quit `ConfirmDialog` (`destination.quit.confirm` / `.cancel`), listing the loaded payload. Owns its controls-lock reason (`loading`/`transition`/`menu`) and implements `IAgentDemo` (block `destination`). |
| `HandoffState.cs` | The layered snapshot's shared blocks (`scene`, `controls_locked`, `lock`, `session`, `transition`, the quit `dialog`) both demo scenes build their `/demo/state` from (with `Snapshot.Of`, `scripts/core/snapshots/`), the quit question both ask (`AskQuit`), and the `agent_demo` group name. |

Namespace: `godottemplate.Demo`. Tests: `tests/hurl/handoff.hurl`, `tests/hurl/handoff_direct.hurl`, `tests/hurl/dialog.hurl`.
