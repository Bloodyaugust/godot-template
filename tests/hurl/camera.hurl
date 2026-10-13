# The main scene's zoom camera (scripts/camera/ZoomCamera.cs, following the demo Player):
# /camera/state and /camera/zoom by level and by wheel steps, the bounds 400s, and a real
# wheel step over the world through /input/click. Zoom eases, so every change is polled.
# See tests/hurl/README.md for the idiom.

# ── 0. Boot: an unframed camera at 1×, the wheel live ────────────────────────────
GET http://{{host}}/camera/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.framed" == false
jsonpath "$.input_enabled" == true
jsonpath "$.zoom" == 1.0
jsonpath "$.target_user_zoom" == 1.0
jsonpath "$.min_zoom" == 0.5
jsonpath "$.max_zoom" == 2.5
[Captures]
min_zoom: jsonpath "$.min_zoom"
max_zoom: jsonpath "$.max_zoom"

# ── 1. By level: the target is set at once, the applied zoom eases to it ───────────
GET http://{{host}}/camera/zoom?level=2
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.target_user_zoom" == 2.0

GET http://{{host}}/camera/state
[Options]
retry: 30
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.user_zoom" == 2.0
jsonpath "$.zoom" == 2.0

# ── 2. By steps: one step out divides by the step factor (POST body form) ─────────
POST http://{{host}}/camera/zoom
{"steps": -1}
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.target_user_zoom" < 2.0
jsonpath "$.state.target_user_zoom" > 1.7

# Many steps clamp to the bounds instead of failing.
GET http://{{host}}/camera/zoom?steps=100
HTTP 200
[Asserts]
jsonpath "$.state.target_user_zoom" == {{max_zoom}}

GET http://{{host}}/camera/zoom?steps=-100
HTTP 200
[Asserts]
jsonpath "$.state.target_user_zoom" == {{min_zoom}}

# ── 3. The rejected shapes: out of bounds, no field, not a number ─────────────────
GET http://{{host}}/camera/zoom?level=9
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "level"
jsonpath "$.state.target_user_zoom" == {{min_zoom}}

GET http://{{host}}/camera/zoom?level=0.1
HTTP 400

GET http://{{host}}/camera/zoom
HTTP 400
[Asserts]
jsonpath "$.error" contains "level"

GET http://{{host}}/camera/zoom?steps=lots
HTTP 400
[Asserts]
jsonpath "$.error" contains "steps"

# ── 4. A real wheel step over the world zooms in ──────────────────────────────────
GET http://{{host}}/camera/zoom?level=1
HTTP 200

POST http://{{host}}/input/click
{"x": 640, "y": 400, "button": "wheel_up"}
HTTP 200

GET http://{{host}}/camera/state
[Options]
retry: 30
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.target_user_zoom" > 1.0
jsonpath "$.user_zoom" > 1.0

GET http://{{host}}/quit
HTTP 200
