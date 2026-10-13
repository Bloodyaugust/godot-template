using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the player's camera zoom (the <see cref="IAgentExample"/>
/// pattern). <c>godottemplate.Camera.ZoomCamera</c> implements this and joins the
/// <c>agent_camera</c> group so <see cref="AgentRestServer"/> can expose <c>/camera/*</c>.
/// Scene-backed: it exists while a scene with a zoom camera is up, and the routes 503
/// otherwise. Zoom changes go through the same easing as the mouse wheel.
/// </summary>
public interface IAgentCamera
{
    /// <summary>Applied zoom, eased and target user zoom, bounds, whether the wheel is live, and the view centre.</summary>
    Dictionary<string, object> GetCameraState();

    /// <summary>
    /// Set the target user zoom to <paramref name="level"/> (which must lie within the
    /// camera's bounds), then step it by <paramref name="steps"/> wheel steps (positive zooms
    /// in, clamped to the bounds). At least one is required. Works even while the wheel is
    /// disabled; whether the level applies is up to the camera's owner.
    /// </summary>
    (bool ok, string error) SetZoom(float? level, int? steps);
}
