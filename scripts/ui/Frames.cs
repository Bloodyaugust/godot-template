using Godot;

namespace godottemplate.UI;

/// <summary>
/// Style boxes built in code, so outline and fill colours come from <see cref="Palette"/>
/// rather than being baked into scenes.
/// </summary>
public static class Frames
{
    /// <summary>A rounded border with an optional fill (transparent by default).</summary>
    public static StyleBoxFlat Outline(Color border, int width = 2, int radius = 4, Color? fill = null)
    {
        var box = new StyleBoxFlat { BgColor = fill ?? Colors.Transparent, BorderColor = border };
        box.SetBorderWidthAll(width);
        box.SetCornerRadiusAll(radius);
        return box;
    }
}
