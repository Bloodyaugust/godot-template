# Pointer-input scenario against the demo scene: the headless boot sized its window
# (so /ui/controls rects are on-screen), the GET-or-POST display mutations, pointer
# motion building hover state, wheel steps, drags in both end forms with their
# 400s, and teardown by a short DRAG inside the QuitButton — press and release land
# on the button, so the instance exiting on its own is the proof the drag's events
# went through the real GUI pipeline.

# ── 0. Wait for the server; headless boots start at 64x64 unless resized ──────
GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.running" == true
jsonpath "$.window.width" >= 640
jsonpath "$.window.height" >= 360

# ── 1. Display mutations accept GET with query fields (bodyless idiom) ────────
GET http://{{host}}/display/set?factor=1.0
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.applied" == 1.0

GET http://{{host}}/display/window?mode=windowed
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.window_mode" == "windowed"

GET http://{{host}}/display/set?factor=big
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "factor"
jsonpath "$.state.applied" == 1.0

# ── 2. The QuitButton's rect sits inside the window; nothing hovers it yet ───
GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.quit')]" exists
jsonpath "$[0].rect.x" >= 0
jsonpath "$[0].rect.y" >= 0
jsonpath "$[0].rect.center_x" <= 640
jsonpath "$[0].rect.center_y" <= 360
[Captures]
quit_x: jsonpath "$[?(@.id == 'main.quit')].rect.center_x"
quit_y: jsonpath "$[?(@.id == 'main.quit')].rect.center_y"

# ── 3. Pointer motion onto the button builds hover; off it clears ────────────
POST http://{{host}}/input/move
{ "x": {{quit_x}}, "y": {{quit_y}} }
HTTP 200
[Asserts]
jsonpath "$.ok" == true

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[0].id" == "main.quit"
jsonpath "$[0].hovered" == true

POST http://{{host}}/input/move
{ "x": 400, "y": 300 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$[0].hovered" == false

POST http://{{host}}/input/move
{ "x": 400 }
HTTP 400
[Asserts]
jsonpath "$.error" contains "'y'"

# ── 4. Wheel steps through /input/click ──────────────────────────────────────
POST http://{{host}}/input/click
{ "x": 400, "y": 300, "button": "wheel_up" }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.button" == "wheel_up"

POST http://{{host}}/input/click
{ "x": 400, "y": 300, "button": "wheel_down" }
HTTP 200
[Asserts]
jsonpath "$.button" == "wheel_down"

POST http://{{host}}/input/click
{ "x": 400, "y": 300, "button": "wheel_left" }
HTTP 400

# ── 5. Drags: absolute end, offset end, and the rejected shapes ──────────────
POST http://{{host}}/input/drag
{ "from_x": 400, "from_y": 300, "to_x": 500, "to_y": 320 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.button" == "left"
jsonpath "$.steps" == 8

POST http://{{host}}/input/drag
{ "from_x": 400, "from_y": 300, "dx": -100, "dy": 20, "button": "right", "steps": 3 }
HTTP 200
[Asserts]
jsonpath "$.to_x" == 300
jsonpath "$.to_y" == 320
jsonpath "$.button" == "right"
jsonpath "$.steps" == 3

POST http://{{host}}/input/drag
{ "from_x": 400, "from_y": 300, "dx": 10, "dy": 0, "steps": 0 }
HTTP 400

POST http://{{host}}/input/drag
{ "from_x": 400, "from_y": 300, "dx": 10, "dy": 0, "button": "wheel_up" }
HTTP 400

POST http://{{host}}/input/drag
{ "from_x": 400, "from_y": 300, "to_x": 10, "to_y": 0, "dx": 10, "dy": 0 }
HTTP 400

POST http://{{host}}/input/drag
{ "from_x": 400, "to_x": 10, "to_y": 0 }
HTTP 400
[Asserts]
jsonpath "$.error" contains "from_y"

# ── 6. Teardown: a short drag inside the QuitButton presses and releases on it,
#      so the button emits `pressed` and the game quits ────────────────────────
POST http://{{host}}/input/drag
{ "from_x": {{quit_x}}, "from_y": {{quit_y}}, "dx": 2, "dy": 1, "steps": 2 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
