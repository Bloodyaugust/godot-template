# scripts/session/

Cross-scene state — the CLAUDE.md *cross-scene state & persistence* trio's
transient leg (`GameSession`) and durable leg, shipped as working reference code.

| File | Role |
|------|------|
| `GameSession.cs` | The session carrier, an autoload with static state: the `Launch` in flight (`BeginLaunch(destination)`, which consumes an agent's `NextSeed` override or draws a fresh 64-bit seed, and clears the last result), the `Result` coming back (`Finish(result)`, which clears the launch), and `GetState()` for agent snapshots — **seeds serialised as strings**, since a 64-bit seed doesn't survive a JSON number (exact only to 2^53). Never persisted. `SessionLaunch`/`SessionResult` are placeholder records a real game replaces. Implements `IAgentSession` (group `agent_session`, `/session/state`). Used by the scene-handoff demo (`CLAUDE.md`, *Scene handoff*). |
| `PlayerProfile.cs` | The durable player profile: lazy-loading static class over the permanent player state (today the display settings — UI-scale override + window mode). Saves through `ProfileStore` after every mutation; the `GODOT_PROFILE_MEMORY` env var makes the whole profile memory-only for the process (`MemoryOnly` exposes it, so other stores follow it) (the Hurl runners pin it so test runs never touch the real `user://profile.json`). |
| `ProfileStore.cs` | The profile's companion disk store + the `ProfileSnapshot` DTO: one version-stamped `user://profile.json` record, every failure path non-fatal (fresh defaults apply). New fields are additive and nullable ("absent in older files", null = default) with no version bump. |
| `RecordStore.cs` | `RecordStore<T>`: a durable folder of records, one version-stamped `<id>.json` per record, through an engine-free `IRecordCodec<T>` (`scripts/core/persistence/`). Bad files are skipped with a warning and left in place; under `PlayerProfile.MemoryOnly` the files live in memory, still through the codec's JSON. |

## Profile or record store?

`PlayerProfile` is **one** record of scalar settings and small maps, loaded whole.
When the player owns a growing *collection* of things (saved designs, loadouts,
replays), give it a `RecordStore<T>` beside the profile instead: one file per record,
so a corrupt file costs one record, not the profile, and a write touches only what
changed. Both follow the four persistence rules — version stamp, DTO snapshot (the
record type is the DTO; the codec is the format), non-fatal reads, save after every
mutation (`Save`/`Delete` per change) — and both obey `GODOT_PROFILE_MEMORY`
(`PlayerProfile.MemoryOnly`). The profile can hold *pointers* into a store (e.g. the
id of the active record), resolved — and tolerated when dangling — at use.

Namespace: `godottemplate.Session`.
