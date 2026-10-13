using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the content pipeline (the <see cref="IAgentExample"/> pattern). The
/// <c>ContentBootstrap</c> autoload (<c>scripts/content/</c>) implements it and joins the
/// <c>agent_content</c> group so <see cref="AgentRestServer"/> can expose <c>/content/state</c>.
/// Always present, so it never 503s — a failed load is reported in the state, not as an error.
/// </summary>
public interface IAgentContent
{
    /// <summary>Whether every definitions file loaded, the latched error, the files read, and the loaded definitions.</summary>
    Dictionary<string, object> GetContentState();
}
