# tests/unit/Validation/

Unit tests for `scripts/core/validation/` (`BuildResult<T>` / `BuildIssue`).

| File | Covers |
|------|--------|
| `BuildResultTests.cs` | A clean build succeeds with no issues; a failing one collects every issue in order with its code and location; a failure without issues and a success without a value are refused. |
