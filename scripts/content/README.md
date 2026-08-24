# scripts/content/

The **JSON content load path**: reads authored definition files under
`resources/definitions/` at runtime and turns them into the immutable definition
objects the rest of the game looks up by id. Use it when game content (unit types,
item catalogs, generation recipes) is better authored as plain JSON than as `.tres`
resources or hardcoded C#.

| File | Role |
|------|------|
| `ContentLoader.cs` | Reads a definitions file (`res://resources/definitions/<name>.json`) via `FileAccess` + Godot `Json` and returns its top-level array; throws a `ContentException` on a missing file, malformed JSON, or non-array root. |
| `DefReader.cs` | A typed, fail-loud accessor over one parsed JSON object: required/optional getters for strings, ints, floats, bools, string arrays, `[min, max]` int/float **ranges** (`IntRange`/`FloatRange` from `scripts/util/Ranges.cs`), an `[x, y]` `Vector2`, and nested objects/arrays — plus `IsObject` (for fields that accept a string or an object form) and `Keys` (for map-shaped blocks whose keys are data, validated against a closed set by the caller). Throws a contextual `ContentException` (naming the file + entry) on a missing required key or wrong type. |
| `ContentException.cs` | The content-error type. Constructing one also emits `GD.PushError` so the failure shows as one clean console line before it propagates — content errors are hard, loud failures, never a silent empty world. |

Namespace: `godottemplate.Content`.

## The registry pattern (add per game)

The template ships the load path only; each game adds its own definition classes and
registries on top. The shape that has worked well (proven in spun-out projects):

1. **One registry per definition type** (`StructureRegistry`, `EnemyRegistry`, …): a
   static class that lazily calls `ContentLoader.LoadArray("<file>.json")` on first
   access, parses each entry with a `DefReader`, and stores it by id behind
   `Get(id)` / `Has(id)` / `All()` accessors. Reject duplicate ids at load.
2. **A failed load latches.** Constructing a `ContentException` also logs it, so
   re-parsing on every call (an editor `_Draw` hits a registry 60×/s) would flood
   the console with the same line forever. Cache the first failure and rethrow it
   on later accesses — logged once, like a successful parse is only performed once.
   Both are re-tried on assembly reload (static state resets).
3. **Parsed state publishes only after full validation.** Build into locals, run
   every load-time check (duplicates, in-file reference edges, derived sets), and
   assign the static fields last — a throw partway must leave the registry unloaded,
   not half-loaded.
4. **Retain file order** (an ordered list beside the id map): definition files are
   authored in display/priority order, and consumers (build menus, rosters) should
   not re-sort.
5. **Keep loads independent** — registries store only id *strings* for
   cross-references, never objects from another registry, so init order doesn't matter.
6. **Validate cross-file references once at startup**: a single
   `ValidateReferences()` pass (called from an autoload's `_EnterTree`) checks that
   every cross-registry id resolves, throwing a `ContentException` on a dangling
   reference. A bad file then fails at boot with a clear
   `[Content] <file> (<entry>): <reason>` message rather than booting an empty world.
   (In-file edges — e.g. an `upgrades_to` chain — validate inside the owning
   registry's load instead, cycle checks included.)

The abstract shape of a registry:

```csharp
public static class ThingRegistry
{
    private const string FileName = "things.json";
    private static Dictionary<string, ThingDef> _byId;   // null = not loaded yet
    private static List<ThingDef> _ordered;              // file order
    private static ContentException _loadFailure;        // the latch

    private static void EnsureLoaded()
    {
        if (_byId != null) return;
        if (_loadFailure != null) throw _loadFailure;
        try { Load(); }
        catch (ContentException e) { _loadFailure = e; throw; }
    }

    private static void Load()
    {
        var byId = new Dictionary<string, ThingDef>();
        var ordered = new List<ThingDef>();
        foreach (var entry in ContentLoader.LoadArray(FileName))
        {
            var dict = entry.AsGodotDictionary();
            var id = new DefReader(dict, FileName, "thing").OptString("id", "?");
            var def = new ThingDef(new DefReader(dict, FileName, $"thing '{id}'"));
            if (byId.ContainsKey(def.Id))
                throw ContentException.For(FileName, $"duplicate thing id '{def.Id}'");
            byId[def.Id] = def;
            ordered.Add(def);
        }
        // ... in-file reference validation here, before publishing ...
        _byId = byId;      // publish last — all or nothing
        _ordered = ordered;
    }
}
```
