using Godot;
using System.Collections.Generic;
using godottemplate.Server;

namespace godottemplate.Test;

public partial class Player : Sprite2D, IAgentInspectable
{
    [Export] public float Speed { get; set; } = 200f;

    public override void _Ready()
    {
        AddToGroup("player");
    }

    public override void _Process(double delta)
    {
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
    };

    public void OnQuitPressed() => GetTree().Quit();
}
