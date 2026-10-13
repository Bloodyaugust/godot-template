# scenes/ui/

Shared UI pieces, each a `.tscn` with a thin script in `scripts/ui/` (see its README). Hosts
instance them and wire their signals; colours come from `Palette` in code.

| Scene | Role |
|-------|------|
| `top_bar.tscn` | A screen's top bar: title on the left, Menu button on the right (`TopBar.cs`). |
| `menu_overlay.tscn` | The modal menu: scrim, the menu root (Settings, Quit, Close) and the settings panel it swaps to (`MenuOverlay.cs`). |
| `transition.tscn` | The transition overlay, registered as the `Transition` autoload (`Transition.cs`): a `PaletteRect` background and the bottom-centred loading text, under a mouse-stopping root on canvas layer 10. |
| `settings_panel.tscn` | UI-scale and window-mode settings with a Back button (`SettingsPanel.cs`); its scale buttons carry `scale_factor` metadata. |

`scenes/main.tscn` is the reference host: the top bar and the menu overlay on a chrome canvas
layer (layer 20), exercised by `tests/hurl/menu.hurl`.
