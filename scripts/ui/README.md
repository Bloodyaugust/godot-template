# scripts/ui/

Shared UI scaffolding: cross-screen constants and utilities, plus (as the project
grows) top-level menu/navigation screen controllers.

| File | Role |
|------|------|
| `GameScenes.cs` | Registry of top-level scene paths for `ChangeSceneToFile` navigation. Add new screens here rather than scattering `res://` literals across controllers. |
| `Palette.cs` | Single source of truth for the UI colour tokens, grouped by role (surfaces / borders / text / accents / affordances / overlays). Ships neutral placeholders — swap in your game's palette, keep the grouping. |
| `DisplayScale.cs` | Autoload that sets the main window's `ContentScaleFactor` for hi-dpi legibility / accessibility — scales the whole GUI + 2D canvas uniformly so the baked-in pixel sizes aren't tiny on dense displays, while the viewport keeps tracking the **real window size** (default `disabled` stretch mode + `dpi/allow_hidpi`; layout stays anchor/container-responsive). Derives an auto factor from the monitor (`ScreenGetScale`, falling back to `ScreenGetDpi`/96, snapped to quarter steps), capped at **2.0** for auto-detect, with an explicit override allowed up to 3.0. Also owns the window-mode setting (windowed vs. borderless fullscreen). Both settings persist through `PlayerProfile` (`scripts/session/`) and are re-applied at boot. Exposes the `agent_display` REST surface (`/display/*`) via `IAgentDisplay`. **It is an accessibility knob, not a responsive-layout mechanism** — don't reach for a fixed-design-size stretch mode instead of designing UI for arbitrary window sizes. |

Namespace: `godottemplate.UI`.

Per the project's UI-panel convention, panel **structure** (anchors, containers) lives
in the `.tscn`; controllers only wire signals and build dynamic content in code.
