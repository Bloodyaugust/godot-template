# Display-scale + synthetic-click scenario against the demo scene: round-trip the
# /display/* typed surface (set an override, return to auto), then quit the game by
# CLICKING the demo QuitButton — /input/click flows through the real GUI pipeline, so the instance
# exiting on its own (the runner finds /status gone without force-quitting) is the
# end-to-end proof the click landed.

# ── 0. Wait for the server to come up (retry absorbs the boot delay) ──────────
GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.running" == true

# ── 1. Fresh boot follows the monitor (no override), constants sane ───────────
GET http://{{host}}/display/state
HTTP 200
[Asserts]
jsonpath "$.has_override" == false
jsonpath "$.min_factor" == 1.0
jsonpath "$.max_factor" == 3.0
jsonpath "$.factor" >= 1.0

# ── 2. Explicit override (mutation -> {ok, state} envelope) ───────────────────
POST http://{{host}}/display/set
{ "factor": 2.0 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.factor" == 2.0
jsonpath "$.state.has_override" == true

# ── 3. Back to auto (clears the override) ─────────────────────────────────────
POST http://{{host}}/display/set
{ "auto": true }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.has_override" == false

# ── 4. An out-of-range factor is rejected with the error envelope ─────────────
POST http://{{host}}/display/set
{ "factor": 9.0 }
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" exists

# ── 5. Pin an explicit 1.0 (click coords are content-space and scale-invariant;
#       the pin is belt-and-braces determinism + one more set-path exercise) ───
POST http://{{host}}/display/set
{ "factor": 1.0 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.applied" == 1.0

# ── 6. Teardown: click the QuitButton (rect 16..96 x 16..48 -> center 56,32) ──
POST http://{{host}}/input/click
{ "x": 56, "y": 32 }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.button" == "left"
