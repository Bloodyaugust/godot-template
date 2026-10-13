using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the scene-handoff demo pair (the <see cref="IAgentExample"/> pattern),
/// and the shipped example of a <b>layered scene snapshot</b>: one interface implemented by two
/// scenes — the hub (<c>scripts/ui/MainScreen.cs</c>) and the destination
/// (<c>scripts/demo/DestinationScreen.cs</c>) — whose state carries the same shared blocks
/// (<c>scene</c>, <c>controls_locked</c>, <c>lock</c>, <c>session</c>, <c>transition</c>) plus one
/// block named after the scene (<c>hub</c> / <c>destination</c>). Each joins the
/// <c>agent_demo</c> group, so <c>/demo/state</c> answers in either and 503s elsewhere.
/// </summary>
public interface IAgentDemo
{
    /// <summary>The shared blocks plus the scene's own block (see <c>scripts/demo/HandoffState.cs</c>).</summary>
    Dictionary<string, object> GetDemoState();
}
