using System;
using Godot;

namespace godottemplate.UI;

/// <summary>
/// Script for the shared <c>scenes/ui/top_bar.tscn</c>: a title on the left and a Menu button on
/// the right — a screen's chrome, meant to sit on a canvas layer at or above the chrome layer so
/// it stays up through a transition (see <c>CLAUDE.md</c>, <i>Canvas layers</i>). Each host sets
/// its own <see cref="Title"/> and <see cref="MenuAgentId"/> (the main scene uses
/// <c>main.menu</c>) and handles <see cref="MenuPressed"/>.
/// </summary>
public partial class TopBar : PanelContainer
{
    private string _title = "Title";
    private Label _titleLabel;

    /// <summary>The title text on the left.</summary>
    [Export]
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_titleLabel != null) _titleLabel.Text = value;
        }
    }

    /// <summary>The Menu button's <c>agent_id</c> (applied as metadata in <see cref="_Ready"/>); empty leaves it untagged.</summary>
    [Export] public string MenuAgentId { get; set; } = "";

    /// <summary>The Menu button, for owners that need its focus or disabled state.</summary>
    public Button MenuButton { get; private set; }

    /// <summary>The Menu button was pressed.</summary>
    public event Action MenuPressed;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("%Title");
        _titleLabel.Text = _title;
        MenuButton = GetNode<Button>("%MenuButton");
        if (!string.IsNullOrEmpty(MenuAgentId)) MenuButton.SetMeta("agent_id", MenuAgentId);
        MenuButton.Pressed += () => MenuPressed?.Invoke();
    }
}
