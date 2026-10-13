# The shared confirm dialog (scenes/ui/confirm_dialog.tscn) as the hub's quit question: the
# menu's Quit opens it instead of quitting; its Cancel button, Esc and a click on the scrim
# each back out with the game still up (a click aimed at the hub's own Quit button lands on
# the scrim — the dialog blocks the mouse beneath it); its Quit button quits (the teardown).
# The destination asks the same question: handoff.hurl.

# ── 0. Boot: the hub, free, no dialog up ─────────────────────────────────────────
GET http://{{host}}/demo/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.lock" == null
jsonpath "$.dialog.open" == false

# ── 1. Menu → Quit asks instead of quitting ──────────────────────────────────────
POST http://{{host}}/ui/press
{"id": "main.menu"}
HTTP 200

POST http://{{host}}/ui/press
{"id": "main.menu.quit"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == true
jsonpath "$.dialog.title" == "Quit?"
jsonpath "$.dialog.buttons[0].agent_id" == "main.quit.cancel"
jsonpath "$.dialog.buttons[1].agent_id" == "main.quit.confirm"
jsonpath "$.lock" == "menu"

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.quit.confirm' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'main.quit.cancel' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'main.menu.quit' && @.visible == false)]" exists
jsonpath "$[?(@.id == 'main.travel' && @.disabled == true)]" exists

# ── 2. Cancel keeps the game up ──────────────────────────────────────────────────
POST http://{{host}}/ui/press
{"id": "main.quit.cancel"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == false
jsonpath "$.lock" == null

GET http://{{host}}/status
HTTP 200
[Asserts]
jsonpath "$.scene" == "/root/Main"

# ── 3. Esc backs out too (and doesn't reopen the menu) ───────────────────────────
POST http://{{host}}/ui/press
{"id": "main.menu"}
HTTP 200

POST http://{{host}}/ui/press
{"id": "main.menu.quit"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == true

POST http://{{host}}/input/action
{"action": "ui_cancel", "mode": "tap"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == false
jsonpath "$.lock" == null

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.menu.close' && @.visible == false)]" exists

# ── 4. The scrim blocks the hub beneath it, and a click on it backs out ──────────
GET http://{{host}}/ui/controls
HTTP 200
[Captures]
quit_x: jsonpath "$[?(@.id == 'main.quit')].rect.center_x"
quit_y: jsonpath "$[?(@.id == 'main.quit')].rect.center_y"

POST http://{{host}}/ui/press
{"id": "main.menu"}
HTTP 200

POST http://{{host}}/ui/press
{"id": "main.menu.quit"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == true

# Aimed at the hub's own (immediate) Quit button: the scrim takes it, so the game stays up.
POST http://{{host}}/input/click
{"x": {{quit_x}}, "y": {{quit_y}}}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.dialog.open" == false
jsonpath "$.lock" == null

GET http://{{host}}/status
HTTP 200
[Asserts]
jsonpath "$.scene" == "/root/Main"

# ── 5. Teardown through the dialog's Quit (ends the instance) ────────────────────
POST http://{{host}}/ui/press
{"id": "main.menu"}
HTTP 200

POST http://{{host}}/ui/press
{"id": "main.menu.quit"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.quit.confirm' && @.visible == true)]" exists

POST http://{{host}}/ui/press
{"id": "main.quit.confirm"}
HTTP 200
[Asserts]
jsonpath "$.ok" == true
