# scripts/core/snapshots/

The agent-state snapshot convention, enforced (`CLAUDE.md`, *Typed domain interfaces*).

| File | Role |
|------|------|
| `Snapshot.cs` | `Snapshot.Of((key, value), ...)` builds a `GetXState()` snapshot and throws on a non-snake_case or duplicate key, a raw 64-bit integer (use `Snapshot.Wide`, a string), an enum (use `Snapshot.Name`, its snake_case name) or any other non-JSON-plain value; `Snapshot.List` maps a sequence the same way. Used by `GameSession.GetState()`, the demo pair's `/demo/state` and `ConfirmDialog.GetState()`. |

Namespace: `godottemplate.Core.Snapshots`. Tests: `tests/unit/Snapshots/`.
