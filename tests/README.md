# tests/

Automated tests for the project.

| Subdir | Contents |
|--------|----------|
| `hurl/` | Scripted agentic tests — [Hurl](https://hurl.dev) scenarios that drive a booted game through the debug REST interface for fast, stable end-to-end verification. See `hurl/README.md`. |
| `unit/` | Engine-free C# unit tests — an xUnit project over the pure-logic `scripts/core/` layer, run with plain `dotnet test`. See `unit/README.md`. |
