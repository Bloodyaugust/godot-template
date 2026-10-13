# tests/unit/Persistence/

Unit tests for the one-file-per-record persistence core in `scripts/core/persistence/`
(the engine-free half of `scripts/session/RecordStore.cs`).

| File | Covers |
|------|--------|
| `RecordStoreTests.cs` | `ExampleRecordCodec` round-trip and `version` stamp; bad files (wrong version, no version, unknown key, malformed, empty) read as null with a reason; `RecordFolder<T>` over `MemoryRecordFiles` — save, load in file-name order, delete; bad and id-mismatched files skipped with a warning and left in place; unsafe ids refused. |
