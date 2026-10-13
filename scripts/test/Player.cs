using Godot;
using System.Collections.Generic;
using godottemplate.Server;
using godottemplate.UI;

namespace godottemplate.Test;

public partial class Player : Sprite2D, IAgentInspectable
{
    [Export] public float Speed { get; set; } = 200f;

    /// <summary>The label a <c>demo_ping</c> tap floats above the sprite (<c>scenes/ui/floating_label.tscn</c>).</summary>
    [Export] public PackedScene PingLabel { get; set; }

    /// <summary>The labels this sprite spawned; they free themselves, so entries are checked with <c>IsInstanceValid</c>.</summary>
    private readonly List<FloatingLabel> _labels = new();

    /// <summary>Set by the scene while its controls are locked (a transition, the menu): the sprite ignores the move actions.</summary>
    public bool Locked { get; set; }

    public override void _Ready()
    {
        AddToGroup("player");
    }

    public override void _Process(double delta)
    {
        if (Locked) return;
        if (Input.IsActionJustPressed("demo_ping")) Ping();
        var dir = new Vector2(
            (Input.IsActionPressed("move_right") ? 1 : 0) - (Input.IsActionPressed("move_left") ? 1 : 0),
            (Input.IsActionPressed("move_down") ? 1 : 0) - (Input.IsActionPressed("move_up") ? 1 : 0)
        );

        if (dir != Vector2.Zero)
            Position += dir.Normalized() * Speed * (float)delta;
    }

    /// <summary>Float a placeholder label above the sprite, into the world (the sprite's parent), so it doesn't follow the sprite.</summary>
    private void Ping()
    {
        if (PingLabel == null) return;
        var label = PingLabel.Instantiate<FloatingLabel>();
        label.Position = Position + new Vector2(0, -72);
        GetParent().AddChild(label);
        label.Setup("Lorem ipsum", Palette.Primary);
        _labels.Add(label);
    }

    /// <summary>How many of the spawned labels are still alive (not freed, not queued for freeing).</summary>
    public int LiveLabels
    {
        get
        {
            _labels.RemoveAll(l => !IsInstanceValid(l) || l.IsQueuedForDeletion());
            return _labels.Count;
        }
    }

    public Dictionary<string, object> GetAgentProperties() => new()
    {
        ["speed"] = Speed,
        ["locked"] = Locked,
        ["floating_labels"] = LiveLabels,
    };

    public void OnQuitPressed() => GetTree().Quit();
}
