using Godot;

namespace godottemplate.UI;

/// <summary>
/// Script for the shared <c>scenes/ui/floating_label.tscn</c>: a short world-space readout (a hit,
/// a pickup, a ping) that rises, fades and frees itself. The spawner instances it from an
/// exported <see cref="PackedScene"/>, adds it to the world at the spot, and calls
/// <see cref="Setup"/>. It frees itself, so a spawner that keeps references must check them with
/// <c>IsInstanceValid</c> (and <c>IsQueuedForDeletion</c>) before use — a C# null check misses a
/// freed node.
/// </summary>
public partial class FloatingLabel : Node2D
{
    /// <summary>How long the label lives, in seconds.</summary>
    [Export] public float Seconds { get; set; } = 1.2f;

    /// <summary>How far it rises over its life, in world units.</summary>
    [Export] public float Rise { get; set; } = 24f;

    private Label _label;
    private float _age;

    public override void _Ready()
    {
        _label = GetNode<Label>("%Text");
        _label.AddThemeColorOverride("font_outline_color", Palette.Bg);
    }

    /// <summary>The text and its tint (a <see cref="Palette"/> colour). Call once it is in the tree.</summary>
    public void Setup(string text, Color color)
    {
        _label.Text = text;
        Modulate = color;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        Position += new Vector2(0, -Rise * (float)delta / Seconds);
        Modulate = Modulate with { A = Mathf.Clamp(1f - _age / Seconds, 0f, 1f) };
        if (_age >= Seconds) QueueFree();
    }
}
