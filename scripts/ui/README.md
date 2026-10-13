# scripts/ui/

Shared UI scaffolding: cross-screen constants and utilities, plus (as the project
grows) top-level menu/navigation screen controllers.

| File | Role |
|------|------|
| `GameScenes.cs` | Registry of top-level scene paths for `ChangeSceneToFile` navigation. Add new screens here rather than scattering `res://` literals across controllers. |
| `Palette.cs` | Single source of truth for the UI colour tokens, grouped by role (surfaces / borders / text / accents / affordances / overlays). Ships neutral placeholders — swap in your game's palette, keep the grouping. |
| `DisplayScale.cs` | Autoload that sets the main window's `ContentScaleFactor` for hi-dpi legibility / accessibility — scales the whole GUI + 2D canvas uniformly so the baked-in pixel sizes aren't tiny on dense displays, while the viewport keeps tracking the **real window size** (default `disabled` stretch mode + `dpi/allow_hidpi`; layout stays anchor/container-responsive). Derives an auto factor from the monitor (`ScreenGetScale`, falling back to `ScreenGetDpi`/96, snapped to quarter steps), capped at **2.0** for auto-detect and by `PixelCap` so the window keeps the `MinCanvas` floor of UI pixels (re-derived on every resize; design: `design/visual/ui-scale.md`), with an explicit override allowed up to 3.0. Also owns the window-mode setting (windowed vs. borderless fullscreen). Both settings persist through `PlayerProfile` (`scripts/session/`) and are re-applied at boot. Exposes the `agent_display` REST surface (`/display/*`) via `IAgentDisplay`. **It is an accessibility knob, not a responsive-layout mechanism** — don't reach for a fixed-design-size stretch mode instead of designing UI for arbitrary window sizes. |

| `TopBar.cs` | Script for `scenes/ui/top_bar.tscn`: a title and a Menu button. Each host sets the `Title` and `MenuAgentId` exports and handles the `MenuPressed` event. Chrome: host it on a canvas layer at or above the chrome layer (`CLAUDE.md`, *Canvas layers*). |
| `MenuOverlay.cs` | Script for `scenes/ui/menu_overlay.tscn`: the full-screen modal menu (Settings, Quit, Close) over a scrim, swapping to the `SettingsPanel` and back on its own. `AgentIdPrefix` names its buttons (`<prefix>.settings` / `.quit` / `.close`); the host opens it and handles `QuitPressed`. |
| `SettingsPanel.cs` | Script for `scenes/ui/settings_panel.tscn`: UI scale and window mode through `DisplayScale`. The scale choices are the buttons under `%ScaleButtons`, each carrying a `scale_factor` metadata value (`0` = auto), so adding a choice is a scene edit. Emits `Back`; ids `settings.*`. |
| `PaletteRect.cs` | A `[Tool]` `ColorRect` filled from a `Palette` surface token (`Bg`, `Panel`, `Panel2`, `Scrim`), so a scene can place a coloured surface without an inline colour. |
| `Frames.cs` | `StyleBoxFlat` builders (`Outline`) so borders and fills come from `Palette` in code, never baked into a scene. |
| `MainScreen.cs` | Controller for the template's `scenes/main.tscn`, the hub of the scene-handoff demo: wires the top bar's Menu and Esc to the menu overlay, its Quit to quitting, and the Travel button (`main.travel`) to the handoff out to `scenes/demo/destination.tscn`; arriving back under a held loading screen it fades in. Owns the hub's controls-lock reason (`transition`/`menu`) and implements `IAgentDemo` (`/demo/state`, block `hub`). The structure is the scene's. |
| `Transition.cs` | The transition overlay, an autoload `CanvasLayer` (`scenes/ui/transition.tscn`, layer `OverlayLayer` = 10, below the chrome's `ChromeLayer` = 20) that survives scene swaps: `BeginOut` (fade to opaque, then a callback) → `Hold(text)` (the loading screen, cycling ellipsis, a minimum hold) → `FadeIn` (queued as `FadePending` until the minimum passes) → `Finished`; `SkipMinHold`, `SetLoadingText`, `Cancel`. Blocks the mouse while up; the scenes lock their own keyboard controls. Implements `IAgentTransition` (group `agent_transition`, `/transition/*`). The flow: `CLAUDE.md`, *Scene handoff*. |

Namespace: `godottemplate.UI`.

Per the project's UI-panel convention, panel **structure** (anchors, containers) lives
in the `.tscn`, and a repeated piece is its own `.tscn` with a thin script; controllers
only wire signals and fill content (`CLAUDE.md`, *UI Panels*).
