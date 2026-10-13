using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the human debug view (the <see cref="IAgentExample"/> pattern). The
/// <c>DebugView</c> autoload (<c>scripts/devtools/</c>) implements it and joins the
/// <c>agent_debug</c> group so <see cref="AgentRestServer"/> can expose <c>/debug/*</c>. Always
/// present, so it never 503s.
/// </summary>
public interface IAgentDebug
{
    /// <summary>The flag, whether this build honours it, and the toggle's action name.</summary>
    Dictionary<string, object> GetDebugState();

    /// <summary>Turn the debug view on or off. Fails in a release build, where it stays off.</summary>
    (bool ok, string error) SetDebug(bool enabled);
}
