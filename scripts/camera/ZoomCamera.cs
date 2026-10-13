using System;
using System.Collections.Generic;
using Godot;
using godottemplate.Server;

namespace godottemplate.Camera;

/// <summary>
/// A <see cref="Camera2D"/> with scroll-to-zoom, for any 2D world view. The wheel (the
/// <c>zoom_in</c> / <c>zoom_out</c> actions, over the world only: UI that takes the wheel
/// keeps it) steps a <b>user zoom</b> multiplier geometrically between
/// <see cref="MinZoom"/> and <see cref="MaxZoom"/>, and <see cref="UserZoom"/> eases toward
/// that target.
/// <para>A camera nothing else frames applies <see cref="UserZoom"/> as its zoom. A
/// <see cref="Framed"/> camera leaves its zoom to an owner (e.g. a scene that follows a
/// target or eases between framings); the owner reads <see cref="UserZoom"/> into its own
/// target and turns <see cref="ZoomInputEnabled"/> off while the player's zoom shouldn't
/// apply.</para>
/// Serves <c>/camera/*</c> via <see cref="IAgentCamera"/> (group <c>agent_camera</c>).
/// </summary>
public partial class ZoomCamera : Camera2D, IAgentCamera
{
    private const string AgentGroup = "agent_camera";

    /// <summary>Within this of its target, <see cref="UserZoom"/> snaps to it.</summary>
    private const float SnapEpsilon = 0.001f;

    [Export] public float MinZoom { get; set; } = 0.5f;
    [Export] public float MaxZoom { get; set; } = 2.5f;

    /// <summary>User zoom is multiplied (in) or divided (out) by this per wheel step.</summary>
    [Export] public float StepFactor { get; set; } = 1.15f;

    /// <summary>Exponential ease rate toward the target, per second (higher is snappier).</summary>
    [Export] public float EaseRate { get; set; } = 14f;

    /// <summary>An owner applies the zoom; the camera only tracks <see cref="UserZoom"/>.</summary>
    [Export] public bool Framed { get; set; }

    /// <summary>Whether the wheel changes the user zoom. The level is kept while off.</summary>
    public bool ZoomInputEnabled { get; set; } = true;

    /// <summary>The player's zoom multiplier as currently eased (1 = normal scale).</summary>
    public float UserZoom { get; private set; } = 1f;

    /// <summary>Where <see cref="UserZoom"/> is easing to.</summary>
    public float TargetUserZoom { get; private set; } = 1f;

    public override void _Ready() => AddToGroup(AgentGroup);

    public override void _UnhandledInput(InputEvent e)
    {
        if (!ZoomInputEnabled) return;
        int steps = e.IsActionPressed("zoom_in") ? 1 : e.IsActionPressed("zoom_out") ? -1 : 0;
        if (steps == 0) return;
        StepZoom(steps);
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (UserZoom != TargetUserZoom)
        {
            UserZoom = Mathf.Lerp(UserZoom, TargetUserZoom, 1f - Mathf.Exp(-EaseRate * (float)delta));
            if (Mathf.Abs(UserZoom - TargetUserZoom) < SnapEpsilon) UserZoom = TargetUserZoom;
        }
        if (!Framed) Zoom = new Vector2(UserZoom, UserZoom);
    }

    /// <summary>Step the target user zoom by whole wheel steps (positive zooms in).</summary>
    public void StepZoom(int steps) => SetTargetUserZoom(TargetUserZoom * Mathf.Pow(StepFactor, steps));

    private void SetTargetUserZoom(float level) => TargetUserZoom = Mathf.Clamp(level, MinZoom, MaxZoom);

    // ---- IAgentCamera (/camera/*) -----------------------------------------------------

    public Dictionary<string, object> GetCameraState() => new()
    {
        ["zoom"] = Zoom.X,
        ["user_zoom"] = UserZoom,
        ["target_user_zoom"] = TargetUserZoom,
        ["min_zoom"] = MinZoom,
        ["max_zoom"] = MaxZoom,
        ["step_factor"] = StepFactor,
        ["framed"] = Framed,
        ["input_enabled"] = ZoomInputEnabled,
        ["x"] = GetScreenCenterPosition().X,
        ["y"] = GetScreenCenterPosition().Y,
    };

    public (bool ok, string error) SetZoom(float? level, int? steps)
    {
        if (level == null && steps == null) return (false, "expected 'level' or 'steps'");
        if (level is { } l)
        {
            if (!float.IsFinite(l) || l < MinZoom || l > MaxZoom)
                return (false, $"level must be within [{MinZoom}, {MaxZoom}]");
            SetTargetUserZoom(l);
        }
        if (steps is { } s) StepZoom(Math.Clamp(s, -100, 100));
        return (true, null);
    }
}
