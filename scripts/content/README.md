# scripts/content/

The **JSON content load path**: reads authored definition files under
`resources/definitions/` at runtime and turns them into the immutable definition
objects the rest of the game looks up by id. Use it when game content (unit types,
item catalogs, generation recipes) is better authored as plain JSON than as `.tres`
resources or hardcoded C#.

| File | Role |
|------|------|
| `ContentLoader.cs` | Reads a definitions file (`res://resources/definitions/<name>.json`) via `FileAccess` + Godot `Json` and returns its top-level array; throws a `ContentException` on a missing file, malformed JSON, or non-array root. |
| `DefReader.cs` | A typed, fail-loud accessor over one parsed JSON object: required/optional getters for strings, ints, floats, bools, string arrays, `[min, max]` int/float **ranges** (`IntRange`/`FloatRange` from `scripts/util/Ranges.cs`), an `[x, y]` `Vector2`, and nested objects/arrays. Throws a contextual `ContentException` (naming the file + entry) on a missing required key or wrong type. |
| `ContentException.cs` | The content-error type. Constructing one also emits `GD.PushError` so the failure shows as one clean console line before it propagates — content errors are hard, loud failures, never a silent empty world. |

Namespace: `godottemplate.Content`.

## The consuming pattern (add per game)

The template ships the load path only; each game adds its own definition classes and
registries on top. The shape that has worked well:

1. **One registry per definition type** (`StructureRegistry`, `EnemyRegistry`, …): a
   static class whose static constructor calls `ContentLoader.LoadArray("<file>.json")`
   on first access (lazy), parses each entry with a `DefReader`, and stores it by id
   behind `Get(id)` / `All()` accessors.
2. **Keep loads independent** — registries store only id *strings* for
   cross-references, never objects from another registry, so init order doesn't matter.
3. **Validate references once at startup**: after the registries exist, a single
   `ValidateReferences()` pass (called from an autoload's `_EnterTree`) checks that
   every cross-file id resolves, throwing a `ContentException` on a dangling
   reference. A bad file then fails at boot with a clear
   `[Content] <file> (<entry>): <reason>` message rather than booting an empty world.
