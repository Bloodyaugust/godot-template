# The main scene's chrome: the top bar's Menu button opens the menu overlay, Settings swaps
# to the shared settings panel (scenes/ui/settings_panel.tscn) whose buttons drive
# /display/*, Back returns to the menu root, Close and Esc close it, and the menu's Quit
# asks through the confirm dialog, whose Quit quits (the dialog itself: dialog.hurl). Hidden buttons report visible:false and refuse /ui/press, so every step walks the
# real navigation. See tests/hurl/README.md for the idiom.

# ── 0. Boot: the top bar is up, the menu is closed ────────────────────────────────
GET http://{{host}}/ui/controls
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'main.menu.settings' && @.visible == false)]" exists
jsonpath "$[?(@.id == 'settings.back' && @.visible == false)]" exists

# A hidden button refuses a press.
POST http://{{host}}/ui/press
{"id": "main.menu.settings"}
HTTP 400

# The display surface reports the canvas floor and the window-derived cap.
GET http://{{host}}/display/state
HTTP 200
[Asserts]
jsonpath "$.min_canvas.width" == 1280
jsonpath "$.min_canvas.height" == 720
jsonpath "$.pixel_cap" >= 1.0
jsonpath "$.window_size.width" >= 1280
jsonpath "$.window_size.height" >= 720

# ── 1. Menu → Settings ────────────────────────────────────────────────────────────
GET http://{{host}}/ui/press?id=main.menu
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.id" == "main.menu"

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.settings' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'main.menu.quit' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'settings.back' && @.visible == false)]" exists

POST http://{{host}}/ui/press
{"id": "main.menu.settings"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'settings.back' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'settings.scale.150' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'main.menu.settings' && @.visible == false)]" exists

# ── 2. The settings buttons drive the display settings ───────────────────────────
POST http://{{host}}/ui/press
{"id": "settings.scale.150"}
HTTP 200

GET http://{{host}}/display/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.has_override" == true
jsonpath "$.factor" == 1.5

POST http://{{host}}/ui/press
{"id": "settings.scale.auto"}
HTTP 200

GET http://{{host}}/display/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.has_override" == false

POST http://{{host}}/ui/press
{"id": "settings.window.fullscreen"}
HTTP 200

GET http://{{host}}/display/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.window_mode" == "fullscreen"

POST http://{{host}}/ui/press
{"id": "settings.window.windowed"}
HTTP 200

GET http://{{host}}/display/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.window_mode" == "windowed"

# ── 3. Back to the menu root, then Close ──────────────────────────────────────────
POST http://{{host}}/ui/press
{"id": "settings.back"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.settings' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'settings.back' && @.visible == false)]" exists

POST http://{{host}}/ui/press
{"id": "main.menu.close"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.close' && @.visible == false)]" exists

# ── 4. Esc opens it, Esc closes it ────────────────────────────────────────────────
# A bad mode is a 400 (GET alias, query fields) and presses nothing.
GET http://{{host}}/input/action?action=ui_cancel&mode=hold
HTTP 400
[Asserts]
jsonpath "$.error" contains "mode"

POST http://{{host}}/input/action
{"action": "ui_cancel", "mode": "tap"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.close' && @.visible == true)]" exists

GET http://{{host}}/input/action?action=ui_cancel&mode=tap
HTTP 200
[Asserts]
jsonpath "$.ok" == true

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.close' && @.visible == false)]" exists

# ── 5. Teardown through the menu's Quit and the dialog's (ends the instance) ─────
POST http://{{host}}/ui/press
{"id": "main.menu"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.quit' && @.visible == true)]" exists

POST http://{{host}}/ui/press
{"id": "main.menu.quit"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.quit.confirm' && @.visible == true)]" exists

POST http://{{host}}/ui/press
{"id": "main.quit.confirm"}
HTTP 200
[Asserts]
jsonpath "$.ok" == true
