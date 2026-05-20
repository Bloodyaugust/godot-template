using System.Collections.Generic;

namespace godottemplate.Server;

// Template/stub for the typed domain-interface pattern used by AgentRestServer.
//
// The pattern lets the server stay domain-agnostic: it only knows how to find an
// implementer (by scene-tree group) and serialize its state. The implementer —
// which lives in a feature directory like `scripts/inventory/` or
// `scripts/quests/` — owns the actual operations and decides what its state
// snapshot looks like.
//
// To add a real domain surface (inventory, quest log, level editor, etc.):
//
//   1. Copy this file to `IAgent<YourDomain>.cs`, rename the interface, and
//      rename `GetExampleState` to `Get<YourDomain>State`. Add any mutation
//      methods you need (e.g. `BuyItem(string file)`, `EquipSlot(int slot)`),
//      returning `(bool ok, string error)` tuples so the server can build the
//      `{ok, state, error?}` response envelope.
//
//   2. Implement the interface on the active scene controller. In its `_Ready`
//      method, call `AddToGroup("agent_<your_domain>")` so the server can find
//      it (and ideally also `RemoveFromGroup` on free, though Godot handles
//      group cleanup automatically when nodes are freed).
//
//   3. In `AgentRestServer.cs`, add a route group following `FindExample` /
//      `HandleExampleState`: a `Find<YourDomain>()` helper that scans the
//      group, and one `Handle<Action>` per route. State routes (`GET`) just
//      return the snapshot; mutation routes (`POST`) call into the interface
//      and return `{ok, state, error?}`.
//
// You can delete this file (and the `/example/state` route) once you have at
// least one real domain interface — it's only here as a self-documenting
// reference.
public interface IAgentExample
{
    Dictionary<string, object> GetExampleState();
}
