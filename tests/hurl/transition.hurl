# Transition overlay (scripts/ui/Transition.cs) — the loading screen's dev demo over the hub:
# hold with the cycling ellipsis, the queued fade-in, the skip, the refusals, and the hub's
# controls locked ("transition") while it runs. See tests/hurl/README.md for the idiom.
#   hurl --test --jobs 1 --variable host=127.0.0.1:8080 tests/hurl/transition.hurl

# ── 0. Boot ──────────────────────────────────────────────────────────────────
GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.running" == true

GET http://{{host}}/demo/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.lock" == null

# ── 1. The overlay is always present and idle ────────────────────────────────
GET http://{{host}}/transition/state
HTTP 200
[Asserts]
jsonpath "$.stage" == "none"
jsonpath "$.blocking_mouse" == false
jsonpath "$.fade_pending" == false
jsonpath "$.layer" == 10
jsonpath "$.view_size.x" > 0

# Bad durations are refused, and nothing starts.
GET http://{{host}}/transition/demo?seconds=-1
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.state.stage" == "none"

GET http://{{host}}/transition/demo?seconds=abc
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "seconds"

# Skipping with no loading screen up is refused.
GET http://{{host}}/transition/skip
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "none"

# ── 2. Hold for 2 seconds, then fade in ──────────────────────────────────────
GET http://{{host}}/transition/demo?seconds=2&text=Generating%20layout
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.stage" == "hold"
jsonpath "$.state.fade_pending" == true
jsonpath "$.state.min_hold_elapsed" == false
jsonpath "$.state.min_hold_seconds" == 2
jsonpath "$.state.blocking_mouse" == true
jsonpath "$.state.text" startsWith "Generating layout."

# A second demo while one runs is refused.
GET http://{{host}}/transition/demo
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "hold"

# The hub's controls lock for the transition.
GET http://{{host}}/demo/state
HTTP 200
[Asserts]
jsonpath "$.controls_locked" == true
jsonpath "$.lock" == "transition"
jsonpath "$.transition" == "hold"

GET http://{{host}}/transition/state
HTTP 200
[Captures]
first_text: jsonpath "$.text"

# The ellipsis cycles while the loading screen holds.
GET http://{{host}}/transition/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "hold"
jsonpath "$.text" != "{{first_text}}"
jsonpath "$.text" startsWith "Generating layout"

# The queued fade starts once the minimum passes, then the overlay finishes.
GET http://{{host}}/transition/state
[Options]
retry: 40
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "in"
jsonpath "$.min_hold_elapsed" == false
jsonpath "$.fade_pending" == false

GET http://{{host}}/transition/state
[Options]
retry: 40
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "none"
jsonpath "$.blocking_mouse" == false

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.lock" == null

# ── 3. A long hold, ended by the skip ────────────────────────────────────────
GET http://{{host}}/transition/demo?seconds=30
HTTP 200
[Asserts]
jsonpath "$.state.stage" == "hold"
jsonpath "$.state.fade_pending" == true

GET http://{{host}}/transition/skip
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.stage" == "in"

GET http://{{host}}/transition/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "none"

# ── 4. The hub underneath still answers ──────────────────────────────────────
GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.lock" == null

# ── Teardown ─────────────────────────────────────────────────────────────────
GET http://{{host}}/quit
HTTP 200
