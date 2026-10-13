using System;
using Godot;

namespace godottemplate.UI;

/// <summary>
/// Script for the shared <c>scenes/ui/menu_overlay.tscn</c>: a full-screen modal over a
/// <see cref="PaletteRect"/> scrim, holding the menu root (Settings, Quit, Close) and the
/// <see cref="SettingsPanel"/> it swaps to. The overlay owns its own navigation (root ↔
/// settings); the host opens it (from its <see cref="TopBar"/> or Esc) and decides what Quit
/// means by handling <see cref="QuitPressed"/> — e.g. an unsaved-changes guard before quitting.
/// <para>The menu buttons' <c>agent_id</c>s are <see cref="AgentIdPrefix"/> plus
/// <c>.settings</c> / <c>.quit</c> / <c>.close</c> (the main scene uses <c>main.menu</c>); the
/// settings panel keeps its own <c>settings.*</c> ids.</para>
/// </summary>
public partial class MenuOverlay : Control
{
    /// <summary>Prefix for the menu buttons' <c>agent_id</c>s; empty leaves them untagged.</summary>
    [Export] public string AgentIdPrefix { get; set; } = "";

    /// <summary>The player chose Quit. The overlay has already closed.</summary>
    public event Action QuitPressed;

    private Control _menuPanel;
    private SettingsPanel _settings;

    /// <summary>Whether the overlay is up.</summary>
    public bool IsOpen => Visible;

    /// <summary>Whether the overlay is up on its settings page.</summary>
    public bool SettingsOpen => Visible && _settings.Visible;

    public override void _Ready()
    {
        _menuPanel = GetNode<Control>("%MenuPanel");
        _settings = GetNode<SettingsPanel>("%SettingsPanel");
        Tag("%SettingsButton", "settings");
        Tag("%QuitButton", "quit");
        Tag("%CloseButton", "close");
        _settings.Back += ShowRoot;
        // Opaque surfaces from the palette: the default theme's panel is translucent over the world.
        foreach (var panel in new Control[] { _menuPanel, _settings })
            panel.AddThemeStyleboxOverride("panel", Frames.Outline(Palette.LineSolid, 1, 4, Palette.Panel));
    }

    private void Tag(string path, string tail)
    {
        if (string.IsNullOrEmpty(AgentIdPrefix)) return;
        GetNode<Button>(path).SetMeta("agent_id", $"{AgentIdPrefix}.{tail}");
    }

    /// <summary>Show the overlay on its menu root.</summary>
    public void Open()
    {
        Visible = true;
        ShowRoot();
    }

    public void Close() => Visible = false;

    public void OnSettingsPressed()
    {
        _menuPanel.Visible = false;
        _settings.Visible = true;
    }

    public void OnQuitPressed()
    {
        Close();
        QuitPressed?.Invoke();
    }

    private void ShowRoot()
    {
        _settings.Visible = false;
        _menuPanel.Visible = true;
    }
}
