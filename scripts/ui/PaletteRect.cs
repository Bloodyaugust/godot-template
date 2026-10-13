using Godot;

namespace godottemplate.UI;

/// <summary>
/// A <see cref="ColorRect"/> filled from a <see cref="Palette"/> surface token (screen
/// background, modal scrim, …), so scenes can place a coloured surface without an inline
/// colour. A tool script, so the editor shows the real colour.
/// </summary>
[Tool]
public partial class PaletteRect : ColorRect
{
    public enum Token { Bg, Panel, Panel2, Scrim }

    private Token _fill;

    [Export]
    public Token Fill
    {
        get => _fill;
        set
        {
            _fill = value;
            Color = Resolve(value);
        }
    }

    public override void _Ready() => Color = Resolve(_fill);

    private static Color Resolve(Token token) => token switch
    {
        Token.Bg => Palette.Bg,
        Token.Panel => Palette.Panel,
        Token.Panel2 => Palette.Panel2,
        _ => Palette.Scrim,
    };
}
