using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the session carrier (the <see cref="IAgentExample"/> pattern). The
/// <c>GameSession</c> autoload (<c>scripts/session/GameSession.cs</c>) implements it and joins the
/// <c>agent_session</c> group so <see cref="AgentRestServer"/> can expose <c>/session/state</c>.
/// Always present, so it never 503s. Read-only: the scenes write the carrier.
/// </summary>
public interface IAgentSession
{
    /// <summary>The launch in flight, the last result and the next seed (seeds as strings).</summary>
    Dictionary<string, object> GetSessionState();
}
