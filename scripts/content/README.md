# scripts/content/

The **JSON content load path**: reads authored definition files under
`resources/definitions/` at runtime and turns them into the immutable definition
objects the rest of the game looks up by id. Use it when game content (unit types,
item catalogs, generation recipes) is better authored as plain JSON than as `.tres`
resources or hardcoded C#.

## The split: core parses, Godot reads

Parsing and validation are **engine-free** (`scripts/core/content/`, unit-tested in
`tests/unit/Content/`): `JsonDefReader` works on injected text. This directory does only
what needs Godot — reads `res://` files, loads textures and other resources a
definition references (checking they exist), and latches failures. A domain whose files
cross-reference each other can expose an engine-free `Parse(Func<string, string> read)`
taking `ContentLoader.ReadText` as the reader, so the unit tests feed it the same files
from disk.

| File | Role |
|------|------|
| `ContentLoader.cs` | Reads a definitions file (`res://resources/definitions/<name>.json`) via `FileAccess`: `ReadText` returns the raw text (for an injected-reader `Parse`), `LoadArray<T>(file, each)` hands it to `JsonDefReader.ParseArray` and maps each element. A missing file or any engine-free `ContentError` surfaces as a logged `ContentException`. |
| `ContentBootstrap.cs` | The **autoload** that loads every definitions file at boot (`_EnterTree`): reads each through `ContentLoader.ReadText`, parses it engine-free, and latches the first failure into `Error` (logged once by `ContentException`) so the game still boots and the failure is readable. Joins `agent_content` and implements `IAgentContent` (`/content/state`: `loaded`, `error`, `files`, the placeholder `examples`). Add one line per definitions file to `LoadAll`. |
| `ContentException.cs` | The Godot-side content-error type. Constructing one also emits `GD.PushError` so the failure shows as one clean console line before it propagates — content errors are hard, loud failures, never a silent empty world. `From(ContentError)` bridges an engine-free error. |

The reader itself (`scripts/core/content/JsonDefReader.cs`): typed required/optional
getters for non-empty strings, **integers only** (a fractional number is rejected),
bools, string lists, `[x, y]` integer pairs, string/int maps, nested arrays, and
snake_case enums (`ReqEnum<T>`); `RejectUnknownKeys` catches typoed keys. Every failure
is a `ContentError` naming the file and entry.

Namespace: `godottemplate.Content`.

## The registry pattern (add per game)

The template ships the load path only; each game adds its own definition classes and
registries on top. The shape that has worked well (proven in spun-out projects):

1. **One registry per definition type** (`StructureRegistry`, `EnemyRegistry`, …): a
   static class that lazily calls `ContentLoader.LoadArray("<file>.json", ...)` on first
   access, parses each entry with a `JsonDefReader`, and stores it by id behind
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
6. **Keep validation engine-free where you can.** A load-time check that needs no
   Godot (duplicates, references, derived sets) belongs in a `scripts/core/` parser
   throwing `ContentError` — then it is unit-tested; the registry only bridges it with
   `ContentException.From`. Checks that need Godot (a `sprite` path resolves to a
   texture) stay here.
7. **Validate cross-file references once at startup**: a single
   `ValidateReferences()` pass (called from `ContentBootstrap.LoadAll`) checks that
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
        var defs = ContentLoader.LoadArray(FileName, (el, i) =>
        {
            var r = new JsonDefReader(el, FileName, $"thing [{i}]");
            r.RejectUnknownKeys("id", "name", "cost");
            return new ThingDef(r.ReqString("id"), r.ReqString("name"), r.ReqInt("cost"));
        });
        foreach (var def in defs)
        {
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
