using Godot;

namespace godottemplate.UI;

/// <summary>
/// Reusable settings panel (<c>scenes/ui/settings_panel.tscn</c>): UI scale and window mode,
/// applied and persisted through the <see cref="DisplayScale"/> autoload. The scale choices
/// are the buttons under <c>%ScaleButtons</c>; each carries a <c>scale_factor</c> metadata
/// value (<c>0</c> = follow the monitor), so adding a choice is a scene edit. Emits
/// <see cref="BackEventHandler"/> when the player leaves; the host decides what that means.
/// </summary>
public partial class SettingsPanel : PanelContainer
{
    [Signal] public delegate void BackEventHandler();

    private const string ScaleFactorMeta = "scale_factor";

    private Label _currentScale;
    private Label _currentWindow;

    public override void _Ready()
    {
        _currentScale = GetNode<Label>("%CurrentScale");
        _currentWindow = GetNode<Label>("%CurrentWindow");
        _currentScale.Modulate = Palette.TextDim;
        _currentWindow.Modulate = Palette.TextDim;

        foreach (var child in GetNode("%ScaleButtons").GetChildren())
        {
            if (child is not Button b || !b.HasMeta(ScaleFactorMeta)) continue;
            var factor = (float)b.GetMeta(ScaleFactorMeta).AsDouble();
            b.Pressed += () => OnScalePressed(factor);
        }

        VisibilityChanged += Refresh;
        Refresh();
    }

    private void OnScalePressed(float factor)
    {
        var display = DisplayScale.Instance;
        if (display == null) return;
        if (factor <= 0) display.ClearOverride();
        else display.SetOverride(factor);
        Refresh();
    }

    public void OnWindowedPressed() => SetFullscreen(false);

    public void OnFullscreenPressed() => SetFullscreen(true);

    public void OnBackPressed() => EmitSignal(SignalName.Back);

    private void SetFullscreen(bool fullscreen)
    {
        DisplayScale.Instance?.SetFullscreen(fullscreen);
        Refresh();
    }

    private void Refresh()
    {
        var display = DisplayScale.Instance;
        if (display == null || _currentScale == null) return;
        _currentScale.Text = $"{display.Factor:0.##}×" + (display.HasOverride ? "" : " (auto)");
        _currentWindow.Text = display.Fullscreen ? "Fullscreen" : "Windowed";
    }
}
