using System.Collections.Generic;
using godottemplate.Session;
using godottemplate.UI;

namespace godottemplate.Demo;

/// <summary>
/// The shared blocks of the demo pair's layered snapshot (<see cref="Server.IAgentDemo"/>): built
/// once here so the hub and the destination can't drift, each adding its own block under its
/// scene's name.
/// </summary>
public static class HandoffState
{
    public const string AgentGroup = "agent_demo";

    /// <summary>
    /// <c>scene</c>, <c>controls_locked</c> and its <c>lock</c> reason (null when the player is
    /// free), the <c>session</c> carrier, the <c>transition</c> stage, and the scene's own block
    /// under <c>scene</c>'s name.
    /// </summary>
    public static Dictionary<string, object> Build(string scene, string lockReason, Dictionary<string, object> own) => new()
    {
        ["scene"] = scene,
        ["controls_locked"] = lockReason != null,
        ["lock"] = lockReason,
        ["session"] = GameSession.GetState(),
        ["transition"] = (Transition.Instance?.Stage ?? TransitionStage.None).ToString().ToLowerInvariant(),
        [scene] = own,
    };
}
