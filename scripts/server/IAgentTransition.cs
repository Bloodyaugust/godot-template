using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the transition overlay (the <see cref="IAgentExample"/> pattern). The
/// <c>Transition</c> autoload (<c>scripts/ui/Transition.cs</c>) implements it and joins the
/// <c>agent_transition</c> group so <see cref="AgentRestServer"/> can expose <c>/transition/*</c>.
/// Always present, so it never 503s.
/// </summary>
public interface IAgentTransition
{
    /// <summary>The stage, the loading text as shown, the hold's progress, and the mouse block.</summary>
    Dictionary<string, object> GetTransitionState();

    /// <summary>
    /// Dev check of the loading screen without a scene swap: hold over the current scene with
    /// <paramref name="text"/> for <paramref name="seconds"/> (default the loading minimum), then
    /// fade in. Fails while another transition runs or on a duration outside 0-60.
    /// </summary>
    (bool ok, string error) Demo(string text, double? seconds);

    /// <summary>
    /// End the loading screen's minimum now (<c>/transition/skip</c>): a pending fade-in starts at
    /// once. Fails outside <c>hold</c>; inside it, succeeds even when the minimum has passed.
    /// </summary>
    (bool ok, string error) SkipHold();
}
