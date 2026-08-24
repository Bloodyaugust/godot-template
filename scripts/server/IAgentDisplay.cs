using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the global hi-dpi UI scale (the <see cref="IAgentExample"/>
/// pattern). The <c>DisplayScale</c> autoload implements this and joins the
/// <c>agent_display</c> group so <see cref="AgentRestServer"/> can expose
/// <c>/display/*</c>: a snapshot of the effective/auto factors, the window-mode setting,
/// and the monitor's reported scale/DPI, plus setters for the scale (explicit factor or
/// back to auto) and the window mode — returning the <c>{ok, state, error?}</c> envelope.
/// Always present (the autoload runs for the whole process), so unlike
/// scene-controller-backed surfaces it never 503s.
/// </summary>
public interface IAgentDisplay
{
    /// <summary>Effective + auto factors, override flag, window mode, and the monitor's reported scale/DPI.</summary>
    Dictionary<string, object> GetDisplayState();

    /// <summary>
    /// Set the UI scale. When <paramref name="auto"/> is true, clear any override and
    /// follow the monitor; otherwise apply <paramref name="factor"/> (clamped to the
    /// supported range). Fails if neither is supplied or the factor is out of range.
    /// </summary>
    (bool ok, string error) SetScale(float? factor, bool auto);

    /// <summary>Set the window-mode setting: "windowed" or "fullscreen". Fails on anything else.</summary>
    (bool ok, string error) SetWindowMode(string mode);
}
