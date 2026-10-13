# tests/unit/Snapshots/

Unit tests for `scripts/core/snapshots/` (`Snapshot`, the agent-state snapshot builders).

| File | Covers |
|------|--------|
| `SnapshotTests.cs` | Fields keep their order; non-snake_case and duplicate keys are refused; 64-bit integers and enums are refused as values and have their string forms (`Wide`, `Name`); other types are refused; `List` maps and checks each item. |
