using System.Collections.Generic;
using Godot;
using godottemplate.Server;

namespace godottemplate.UI;

/// <summary>
/// Global hi-dpi UI scale. Sets the main window's <see cref="Window.ContentScaleFactor"/>
/// so the entire GUI + 2D canvas renders larger on high-density displays, where the
/// baked-in pixel sizes (font sizes, panel/button dimensions, margins) are otherwise too
/// small to read. Pairs with <c>display/window/stretch/mode = canvas_items</c> set in
/// <c>project.godot</c>; the content scale re-rasterizes vector content at the larger size
/// rather than bitmap-stretching, so text stays crisp.
///
/// On startup it derives an automatic factor from the monitor (<see cref="DisplayServer.ScreenGetScale"/>,
/// falling back to <see cref="DisplayServer.ScreenGetDpi"/> / 96), snapped to quarter steps
/// and clamped to a sane range. The player may set an explicit override; clearing it returns
/// to auto. The override is in-memory; wire it into the game's settings persistence (and a
/// settings screen) once one exists.
///
/// Registered as an autoload (runs in debug and release alike); the agent-facing REST
/// surface that drives it (<c>/display/*</c>) is the only debug-only part.
/// </summary>
public partial class DisplayScale : Node, IAgentDisplay
{
    /// <summary>The running instance. Set in <see cref="_Ready"/>; never null after autoload.</summary>
    public static DisplayScale Instance { get; private set; }

    private const string AgentGroup = "agent_display";

    /// <summary>Reference DPI that maps to a 1.0 scale (Godot's assumed baseline density).</summary>
    private const float BaselineDpi = 96f;

    /// <summary>Lower clamp — below 1.0 would shrink the UI, which defeats the purpose.</summary>
    public const float MinFactor = 1.0f;

    /// <summary>Upper clamp — beyond this, fixed-size panels start to overflow the viewport.</summary>
    public const float MaxFactor = 3.0f;

    /// <summary>
    /// Ceiling for the <em>auto-derived</em> factor specifically. Very dense panels report
    /// a DPI that snaps higher than is comfortable (a 254-DPI screen → ~2.75), so auto-detect
    /// stops here; a player can still push past it with an explicit override up to <see cref="MaxFactor"/>.
    /// </summary>
    public const float MaxAutoFactor = 2.0f;

    /// <summary>Explicit player override, or null to follow <see cref="AutoFactor"/>.</summary>
    private float? _override;

    /// <summary>The monitor-derived factor computed at startup.</summary>
    public float AutoFactor { get; private set; } = 1.0f;

    /// <summary>The factor currently applied to the window (override if set, else auto).</summary>
    public float Factor => _override ?? AutoFactor;

    /// <summary>Whether an explicit override is active (vs. following the monitor).</summary>
    public bool HasOverride => _override.HasValue;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup(AgentGroup);
        AutoFactor = ComputeAutoFactor();
        Apply();
    }

    /// <summary>
    /// Derive a scale from the current monitor: prefer the OS-reported display scale,
    /// fall back to reported DPI / 96 where the scale is unavailable (commonly X11, which
    /// reports 1.0), then snap to quarter steps and clamp.
    /// </summary>
    public float ComputeAutoFactor()
    {
        // Default screen arg resolves to the main window's screen.
        float scale = DisplayServer.ScreenGetScale();
        if (scale < MinFactor + 0.001f)
        {
            int dpi = DisplayServer.ScreenGetDpi();
            if (dpi > 0) scale = dpi / BaselineDpi;
        }
        // Snap/clamp to the supported range, then hold auto-detect to its lower ceiling.
        return Mathf.Min(Snap(scale), MaxAutoFactor);
    }

    /// <summary>Snap to quarter steps and clamp into the supported range.</summary>
    private static float Snap(float raw)
    {
        float snapped = Mathf.Round(raw * 4f) / 4f;
        return Mathf.Clamp(snapped, MinFactor, MaxFactor);
    }

    /// <summary>Apply <see cref="Factor"/> to the main window's content scale.</summary>
    public void Apply()
    {
        var window = GetWindow();
        if (window != null) window.ContentScaleFactor = Factor;
    }

    /// <summary>Set an explicit override factor (snapped + clamped) and apply it immediately.</summary>
    public void SetOverride(float factor)
    {
        _override = Snap(factor);
        Apply();
    }

    /// <summary>Clear any override and return to the monitor-derived auto factor.</summary>
    public void ClearOverride()
    {
        _override = null;
        Apply();
    }

    // =====================================================================
    //  IAgentDisplay  (/display/*)
    // =====================================================================

    public Dictionary<string, object> GetDisplayState()
    {
        var window = GetWindow();
        return new Dictionary<string, object>
        {
            ["factor"] = Factor,
            ["auto_factor"] = AutoFactor,
            ["has_override"] = HasOverride,
            ["applied"] = window?.ContentScaleFactor ?? Factor,
            ["screen"] = DisplayServer.WindowGetCurrentScreen(),
            ["screen_scale"] = DisplayServer.ScreenGetScale(),
            ["screen_dpi"] = DisplayServer.ScreenGetDpi(),
            ["min_factor"] = MinFactor,
            ["max_factor"] = MaxFactor,
            ["max_auto_factor"] = MaxAutoFactor,
        };
    }

    public (bool ok, string error) SetScale(float? factor, bool auto)
    {
        if (auto)
        {
            ClearOverride();
            return (true, null);
        }
        if (!factor.HasValue)
            return (false, "expected 'factor' (number) or 'auto': true");
        if (factor.Value < MinFactor || factor.Value > MaxFactor)
            return (false, $"factor {factor.Value} out of range [{MinFactor}, {MaxFactor}]");
        SetOverride(factor.Value);
        return (true, null);
    }
}
