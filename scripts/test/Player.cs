using Godot;
using System.Collections.Generic;
using godottemplate.Server;

namespace godottemplate.Test;

public partial class Player : Sprite2D, IAgentInspectable
{
    [Export] public float Speed { get; set; } = 200f;

    /// <summary>Set by the scene while its controls are locked (a transition, the menu): the sprite ignores the move actions.</summary>
    public bool Locked { get; set; }

    public override void _Ready()
    {
        AddToGroup("player");
    }

    public override void _Process(double delta)
    {
        if (Locked) return;
        var dir = new Vector2(
            (Input.IsActionPressed("move_right") ? 1 : 0) - (Input.IsActionPressed("move_left") ? 1 : 0),
            (Input.IsActionPressed("move_down") ? 1 : 0) - (Input.IsActionPressed("move_up") ? 1 : 0)
        );

        if (dir != Vector2.Zero)
            Position += dir.Normalized() * Speed * (float)delta;
    }

    public Dictionary<string, object> GetAgentProperties() => new()
    {
        ["speed"] = Speed,
        ["locked"] = Locked,
    };

    public void OnQuitPressed() => GetTree().Quit();
}
