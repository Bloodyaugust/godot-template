using System.Collections.Generic;
using Godot;
using godottemplate.Server;

namespace godottemplate.DevTools;

/// <summary>
/// The global <b>debug view</b> flag: one switch every debug-only view for humans reads
/// (<see cref="Enabled"/>) — labels over agents, overlays of hidden state, anything that shows
/// what a player isn't entitled to see. Because those views may ignore the game's information
/// rules, the flag exists only in debug builds: in a release build it stays off, the toggle is
/// ignored and <see cref="SetDebug"/> refuses. Toggled by the <see cref="ToggleAction"/> (tilde,
/// by physical key) unless a text field has focus; agents read and set it over <c>/debug/*</c>.
/// </summary>
/// <remarks>Registered as an autoload, so it is always present (group <c>agent_debug</c>). The
/// action is polled rather than handled as an event, so <c>/input/action</c> taps reach it too.</remarks>
public partial class DebugView : Node, IAgentDebug
{
    public const string AgentGroup = "agent_debug";

    /// <summary>The Godot action that toggles the view.</summary>
    public const string ToggleAction = "debug_toggle";

    private static DebugView _instance;

    /// <summary>Whether the debug view is on (always false in a release build).</summary>
    public static bool Enabled => _instance?._enabled ?? false;

    private bool _enabled;

    public override void _Ready()
    {
        _instance = this;
        AddToGroup(AgentGroup);
        if (!OS.IsDebugBuild()) SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    public override void _Process(double delta)
    {
        if (!Input.IsActionJustPressed(ToggleAction)) return;
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus is LineEdit or TextEdit) return;   // a tilde typed into a text field
        _enabled = !_enabled;
    }

    public Dictionary<string, object> GetDebugState() => new()
    {
        ["enabled"] = _enabled,
        ["available"] = OS.IsDebugBuild(),
        ["toggle_action"] = ToggleAction,
    };

    public (bool ok, string error) SetDebug(bool enabled)
    {
        if (!OS.IsDebugBuild()) return (false, "the debug view exists only in debug builds");
        _enabled = enabled;
        return (true, null);
    }
}
