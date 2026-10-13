# scenes/ui/

Shared UI pieces, each a `.tscn` with a thin script in `scripts/ui/` (see its README). Hosts
instance them and wire their signals; colours come from `Palette` in code.

| Scene | Role |
|-------|------|
| `top_bar.tscn` | A screen's top bar: title on the left, Menu button on the right (`TopBar.cs`). |
| `menu_overlay.tscn` | The modal menu: scrim, the menu root (Settings, Quit, Close) and the settings panel it swaps to (`MenuOverlay.cs`). |
| `transition.tscn` | The transition overlay, registered as the `Transition` autoload (`Transition.cs`): a `PaletteRect` background and the bottom-centred loading text, under a mouse-stopping root on canvas layer 10. |
| `confirm_dialog.tscn` | A modal question: `PaletteRect` scrim (its click cancels), a centred panel with title, body, items label and a `%Buttons` row the script fills from `confirm_button.tscn` (`ConfirmDialog.cs`). |
| `confirm_button.tscn` | One choice in the dialog's row (`ConfirmButton.cs`). |
| `floating_label.tscn` | A `Node2D` with a centred, outlined `%Text` label that rises, fades and frees itself (`FloatingLabel.cs`). |
| `settings_panel.tscn` | UI-scale and window-mode settings with a Back button (`SettingsPanel.cs`); its scale buttons carry `scale_factor` metadata. |

`scenes/main.tscn` is the reference host: the top bar, the menu overlay and the quit dialog on a
chrome canvas layer (layer 20), exercised by `tests/hurl/menu.hurl` and `tests/hurl/dialog.hurl`.
