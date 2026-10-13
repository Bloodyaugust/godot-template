using System;
using System.Collections.Generic;
using Godot;
using godottemplate.Core.Snapshots;

namespace godottemplate.UI;

/// <summary>
/// Script for the shared <c>scenes/ui/confirm_dialog.tscn</c>: a modal question over a
/// <see cref="PaletteRect"/> scrim — a title, a body, an optional list of items and a row of
/// choices. The host fills it with <see cref="Setup"/>, shows it with <see cref="Open"/> and
/// handles <see cref="Chosen"/> (the index of the button pressed) and <see cref="Cancelled"/>
/// (Esc, or a click on the scrim). The dialog closes itself before raising either.
/// <para>Each choice is a <c>scenes/ui/confirm_button.tscn</c> instanced from the exported
/// <see cref="ButtonScene"/>, so the row's look is scene-authored and the count is the host's.
/// While open the dialog blocks the mouse beneath it (the scrim stops it); hosts lock their own
/// keyboard controls on <see cref="IsOpen"/> and skip their own Esc handling while it is up (the
/// dialog's <see cref="_Process"/> runs after its host's and takes the Esc).</para>
/// </summary>
public partial class ConfirmDialog : Control
{
    /// <summary>The scene each choice is instanced from (<c>scenes/ui/confirm_button.tscn</c>).</summary>
    [Export] public PackedScene ButtonScene { get; set; }

    /// <summary>A choice was pressed: its index in the list given to <see cref="Setup"/>. The dialog has already closed.</summary>
    public event Action<int> Chosen;

    /// <summary>The player backed out (Esc or the scrim). The dialog has already closed.</summary>
    public event Action Cancelled;

    private Label _title;
    private Label _body;
    private Label _items;
    private Container _buttons;
    private readonly List<(string Label, string AgentId)> _choices = new();
    private IReadOnlyList<string> _itemList = Array.Empty<string>();

    /// <summary>Whether the dialog is up.</summary>
    public bool IsOpen => Visible;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _body = GetNode<Label>("%Body");
        _items = GetNode<Label>("%Items");
        _buttons = GetNode<Container>("%Buttons");
        _items.AddThemeColorOverride("font_color", Palette.TextDim);
        // Opaque surface from the palette: the default theme's panel is translucent over the world.
        GetNode<Control>("%Panel").AddThemeStyleboxOverride("panel", Frames.Outline(Palette.LineSolid, 1, 4, Palette.Panel));
    }

    /// <summary>
    /// Fill the dialog: its <paramref name="title"/>, <paramref name="body"/>, one button per
    /// entry of <paramref name="buttons"/> (label and <c>agent_id</c>, left to right) and, when
    /// given, the <paramref name="items"/> listed under the body. Replaces any previous content;
    /// <see cref="Open"/> shows it.
    /// </summary>
    public void Setup(string title, string body, IReadOnlyList<(string label, string agentId)> buttons, IReadOnlyList<string> items = null)
    {
        _title.Text = title;
        _body.Text = body;
        _body.Visible = !string.IsNullOrEmpty(body);
        _itemList = items ?? Array.Empty<string>();
        _items.Text = string.Join("\n", _itemList);
        _items.Visible = _itemList.Count > 0;

        // Detach before freeing, so the old row is gone from /ui/controls at once.
        foreach (var old in _buttons.GetChildren())
        {
            _buttons.RemoveChild(old);
            old.QueueFree();
        }
        _choices.Clear();
        for (int i = 0; i < buttons.Count; i++)
        {
            var button = ButtonScene.Instantiate<ConfirmButton>();
            _buttons.AddChild(button);
            button.Setup(buttons[i].label, buttons[i].agentId);
            int index = i;
            button.Pressed += () => Choose(index);
            _choices.Add((buttons[i].label, buttons[i].agentId));
        }
    }

    /// <summary>Show the dialog as last set up.</summary>
    public void Open() => Visible = true;

    /// <summary>Close without raising anything (the host withdrew the question).</summary>
    public void Close() => Visible = false;

    /// <summary>Back out, as Esc or the scrim does: close and raise <see cref="Cancelled"/>.</summary>
    public void Cancel()
    {
        if (!Visible) return;
        Close();
        Cancelled?.Invoke();
    }

    // Polled like the hosts' Esc, so the agent /input/action route drives it like a real key.
    public override void _Process(double delta)
    {
        if (Visible && Input.IsActionJustPressed("ui_cancel")) Cancel();
    }

    /// <summary>The scrim's <c>gui_input</c>: a left click outside the panel backs out.</summary>
    public void OnScrimInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            AcceptEvent();
            Cancel();
        }
    }

    private void Choose(int index)
    {
        if (!Visible) return;
        Close();
        Chosen?.Invoke(index);
    }

    /// <summary>The dialog for agent states: <c>open</c>, the texts, the <c>items</c> and the <c>buttons</c> (label and <c>agent_id</c>).</summary>
    public Dictionary<string, object> GetState() => Snapshot.Of(
        ("open", Visible),
        ("title", _title?.Text),
        ("body", _body?.Text),
        ("items", Snapshot.List(_itemList, item => item)),
        ("buttons", Snapshot.List(_choices, c => Snapshot.Of(("label", c.Label), ("agent_id", c.AgentId)))));
}
