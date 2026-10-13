# scripts/core/persistence/

The **engine-free** half of the one-file-per-record store: the record format
and folder logic behind `scripts/session/RecordStore.cs`, which picks the
backend and does the Godot file IO. It follows the four rules in CLAUDE.md's
*Cross-scene state & persistence* section; when to use it beside
`PlayerProfile` is in `scripts/session/README.md`.

- `IRecordCodec.cs` — a record type's file format: its id (= file name), its
  version-stamped JSON, and a non-fatal read returning an error string.
- `RecordJson.cs` — codec helpers: `WriteObject` (a `Utf8JsonWriter` object with
  a leading `version`) and `Read` (version check, then fields through
  `JsonDefReader`, every failure turned into an error string).
- `IRecordFiles.cs` — the storage backend interface, plus `MemoryRecordFiles`,
  the in-memory backend used under `GODOT_PROFILE_MEMORY` and in tests (the data
  still goes through the codec's JSON).
- `RecordFolder.cs` — `RecordFolder<T>`: load (bad or id-mismatched files skipped
  with a warning), save, delete, plus the filename-safe id rule
  (`RecordFolder.IsValidId`).
- `ExampleRecord.cs` — the reference record and codec, to delete once a real one
  exists (like `scripts/server/IAgentExample.cs`).

Namespace: `godottemplate.Core.Persistence`. Tests: `tests/unit/Persistence/`.
