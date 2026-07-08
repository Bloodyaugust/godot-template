# scripts/ui/

Shared UI scaffolding: cross-screen constants and utilities, plus (as the project
grows) top-level menu/navigation screen controllers.

| File | Role |
|------|------|
| `GameScenes.cs` | Registry of top-level scene paths for `ChangeSceneToFile` navigation. Add new screens here rather than scattering `res://` literals across controllers. |
| `Palette.cs` | Single source of truth for the UI colour tokens, grouped by role (surfaces / borders / text / accents / affordances / overlays). Ships neutral placeholders — swap in your game's palette, keep the grouping. |
| `DisplayScale.cs` | Autoload that sets the main window's `ContentScaleFactor` for hi-dpi legibility — scales the whole GUI + 2D canvas uniformly so the baked-in pixel sizes aren't tiny on dense displays. Derives an auto factor from the monitor (`ScreenGetScale`, falling back to `ScreenGetDpi`/96, snapped to quarter steps), capped at **2.0** for auto-detect, with an explicit override allowed up to 3.0. Pairs with `display/window/stretch/mode = canvas_items` + `dpi/allow_hidpi` in `project.godot`. Exposes the `agent_display` REST surface (`/display/*`) via `IAgentDisplay`. The override is in-memory; wire it into settings persistence once the game grows one. |

Namespace: `godottemplate.UI`.

Per the project's UI-panel convention, panel **structure** (anchors, containers) lives
in the `.tscn`; controllers only wire signals and build dynamic content in code.
