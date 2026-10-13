# scripts/core/validation/

**Engine-free** build/validation results for anything authored by players or in
content: a validator collects *every* problem instead of stopping at the first.

- `BuildResult.cs` — `BuildIssue` (a stable snake_case string `Code` that UI,
  tests and agents key on, a `Location` path, and a `Message`) and
  `BuildResult<T>` (`Success`, `Failure` — which needs at least one issue — and
  `Of`, the usual end of a validator). Codes are strings so the type stays
  domain-neutral: each domain keeps its closed set of codes as `const string`s in
  one class.

Namespace: `godottemplate.Core.Validation`. Tests: `tests/unit/Validation/`.
