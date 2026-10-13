# The debug view: a global, debug-build-only flag on the DebugView autoload
# (scripts/devtools/DebugView.cs), toggled by the debug_toggle action (tilde by physical
# key) or over /debug/set. The flag is what debug-only views for humans read; this file
# covers the flag itself. See tests/hurl/README.md for the idiom.

# ── 0. Boot: the flag is off, and this is a debug build ──────────────────────────
GET http://{{host}}/debug/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.enabled" == false
jsonpath "$.available" == true
jsonpath "$.toggle_action" == "debug_toggle"

# Setting it needs a boolean.
GET http://{{host}}/debug/set
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "enabled"
jsonpath "$.state.enabled" == false

GET http://{{host}}/debug/set?enabled=maybe
HTTP 400
[Asserts]
jsonpath "$.error" contains "enabled"

# ── 1. On over the route (GET form, then the POST body form) ────────────────────
GET http://{{host}}/debug/set?enabled=true
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.enabled" == true

POST http://{{host}}/debug/set
{"enabled": true}
HTTP 200
[Asserts]
jsonpath "$.state.enabled" == true

# ── 2. The toggle action turns it off… ───────────────────────────────────────────
POST http://{{host}}/input/action
{"action": "debug_toggle", "mode": "tap"}
HTTP 200

GET http://{{host}}/debug/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.enabled" == false

# …and on again.
POST http://{{host}}/input/action
{"action": "debug_toggle", "mode": "tap"}
HTTP 200

GET http://{{host}}/debug/state
[Options]
retry: 20
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.enabled" == true

# ── 3. Off, and quit ──────────────────────────────────────────────────────────────
GET http://{{host}}/debug/set?enabled=false
HTTP 200
[Asserts]
jsonpath "$.state.enabled" == false

GET http://{{host}}/quit
HTTP 200
