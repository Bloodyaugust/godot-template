# scripts/session/

Cross-scene state — the CLAUDE.md *cross-scene state & persistence* trio's
durable leg, shipped as working reference code. (Add the transient session
carrier — a `GameSession` autoload — alongside these when the game grows
screen-to-screen handoff.)

| File | Role |
|------|------|
| `PlayerProfile.cs` | The durable player profile: lazy-loading static class over the permanent player state (today the display settings — UI-scale override + window mode). Saves through `ProfileStore` after every mutation; the `GODOT_PROFILE_MEMORY` env var makes the whole profile memory-only for the process (the Hurl runners pin it so test runs never touch the real `user://profile.json`). |
| `ProfileStore.cs` | The profile's companion disk store + the `ProfileSnapshot` DTO: one version-stamped `user://profile.json` record, every failure path non-fatal (fresh defaults apply). |

Namespace: `godottemplate.Session`.
