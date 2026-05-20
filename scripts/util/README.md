# scripts/util/

Stateless cross-system helpers — pure functions and small UI utilities that do not own scene state of their own. Things that grow state, signals, or scene references belong in their feature directory (e.g. `units/`, `world/`, `ui/`), not here.

Namespace: `<RootNamespace>.Util` (e.g. `godottemplate.Util`).

## Promotion rule

If a helper is used by exactly one scene or system, keep it private to that scene. **Promote it here only on the second consumer.** Premature extraction makes refactors harder and pollutes the cross-cutting namespace with one-shot helpers that should have stayed local.
