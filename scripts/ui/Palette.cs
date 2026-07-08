using Godot;

namespace godottemplate.UI;

/// <summary>
/// The shared UI palette. Kept as one source of truth so every screen tints from the
/// same tokens — when a token changes, change it here, never inline in a controller
/// or scene. The shipped values are neutral placeholders; replace them with your
/// game's palette (keep the role grouping).
/// </summary>
public static class Palette
{
    // Surfaces (backgrounds, back to front).
    public static readonly Color Bg = Color.FromHtml("141414");        // screen background
    public static readonly Color Panel = Color.FromHtml("1c1c1c");     // card / bar background
    public static readonly Color Panel2 = Color.FromHtml("242424");    // hover / raised surface

    // Borders.
    public static readonly Color Line = Color.FromHtml("3a3a3a94");    // low-contrast inner border (has alpha)
    public static readonly Color LineSolid = Color.FromHtml("3a3a3a"); // hard outer border

    // Text.
    public static readonly Color Text = Color.FromHtml("c8c8c8");
    public static readonly Color TextDim = Color.FromHtml("888888");
    public static readonly Color TextFaint = Color.FromHtml("585858");

    // Accents.
    public static readonly Color Primary = Color.FromHtml("c08040");
    public static readonly Color Positive = Color.FromHtml("80a45a");
    public static readonly Color Info = Color.FromHtml("9a7bb0");
    public static readonly Color Negative = Color.FromHtml("c25b4b");

    // Affordances.
    public static readonly Color AcceptFg = Color.FromHtml("bcd49a");  // confirm-action text/border

    // Overlays.
    public static readonly Color Scrim = Color.FromHtml("080808a8");   // modal / spotlight dim
}
