# tests/unit/Content/

Unit tests for the engine-free JSON definition reader and the placeholder parser in `scripts/core/content/`.

| File | Covers |
|------|--------|
| `JsonDefReaderTests.cs` | `JsonDefReader`: typed fields in file order (strings, ints, snake_case enums, string lists, int pairs), and the fail-loud paths — unknown key, non-integer number, unknown enum (listing the valid spellings), missing key, lax JSON (trailing comma, comment), non-array root — each a `ContentError` naming file and entry. |
| `ExampleContentTests.cs` | `ExampleContent.Parse` (the placeholder `ExampleDef`; delete with it): records in file order, duplicate id, unknown `kind` (listing the valid spellings), and the shipped `resources/definitions/examples.json` parsing. |
