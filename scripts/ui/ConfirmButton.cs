using Godot;

namespace godottemplate.UI;

/// <summary>
/// Script for <c>scenes/ui/confirm_button.tscn</c>, one choice in a <see cref="ConfirmDialog"/>'s
/// button row: the dialog instances one per choice from its exported scene and calls
/// <see cref="Setup"/> — the shipped reference for a repeated piece being its own <c>.tscn</c>
/// with a thin <c>Setup(...)</c> (<c>CLAUDE.md</c>, <i>UI Panels</i>).
/// </summary>
public partial class ConfirmButton : Button
{
    /// <summary>The choice's label and its <c>agent_id</c> (empty leaves it untagged).</summary>
    public void Setup(string label, string agentId)
    {
        Text = label;
        if (string.IsNullOrEmpty(agentId))
        {
            if (HasMeta("agent_id")) RemoveMeta("agent_id");
        }
        else
        {
            SetMeta("agent_id", agentId);
        }
    }
}
