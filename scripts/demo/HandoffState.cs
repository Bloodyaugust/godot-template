using System.Collections.Generic;
using godottemplate.Core.Snapshots;
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
    /// free), the <c>session</c> carrier, the <c>transition</c> stage, the scene's quit
    /// <c>dialog</c> (<see cref="ConfirmDialog.GetState"/>), and the scene's own block under
    /// <c>scene</c>'s name.
    /// </summary>
    public static Dictionary<string, object> Build(string scene, string lockReason, ConfirmDialog dialog, Dictionary<string, object> own) => Snapshot.Of(
        ("scene", scene),
        ("controls_locked", lockReason != null),
        ("lock", lockReason),
        ("session", GameSession.GetState()),
        ("transition", Snapshot.Name(Transition.Instance?.Stage ?? TransitionStage.None)),
        ("dialog", dialog?.GetState()),
        (scene, own));

    /// <summary>The quit question both demo scenes ask through their <see cref="ConfirmDialog"/>: index <see cref="QuitConfirm"/> quits.</summary>
    public static void AskQuit(ConfirmDialog dialog, string agentPrefix, IReadOnlyList<string> items = null)
    {
        dialog.Setup("Quit?", "Lorem ipsum dolor sit amet, consectetur adipiscing elit.", new (string, string)[]
        {
            ("Cancel", $"{agentPrefix}.quit.cancel"),
            ("Quit", $"{agentPrefix}.quit.confirm"),
        }, items);
        dialog.Open();
    }

    /// <summary>The Quit choice's index in <see cref="AskQuit"/>'s row (0 is Cancel).</summary>
    public const int QuitConfirm = 1;
}
