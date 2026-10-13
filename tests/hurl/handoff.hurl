# Scene handoff (CLAUDE.md, "Scene handoff"): the hub's Travel button fades out, holds the
# loading screen, changes to scenes/demo/destination.tscn, whose AsyncLoader job runs under it,
# fades in and frees the controls; the destination's Return finishes the launch on GameSession
# and goes back the same way. Direct boot of the destination: handoff_direct.hurl.

# ── 0. Boot: the hub, free, nothing in flight ────────────────────────────────
GET http://{{host}}/demo/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.controls_locked" == false
jsonpath "$.lock" == null
jsonpath "$.session.launch" == null
jsonpath "$.hub.travelling" == false

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.travel' && @.visible == true && @.disabled == false)]" exists

# ── 1. Travel: the controls lock and the launch is on the session ────────────
POST http://{{host}}/ui/press
{"id": "main.travel"}
HTTP 200

GET http://{{host}}/session/state
HTTP 200
[Asserts]
jsonpath "$.launch.destination" == "destination"
jsonpath "$.launch.seed" isString
jsonpath "$.result" == null
[Captures]
seed: jsonpath "$.launch.seed"

# Out: fading to opaque, the mouse already blocked.
GET http://{{host}}/transition/state
[Options]
retry: 25
retry-interval: 20ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "out"
jsonpath "$.blocking_mouse" == true

# Hold: the loading screen is up and the scene swap happens under it.
GET http://{{host}}/transition/state
[Options]
retry: 50
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "hold"
jsonpath "$.text" startsWith "Travelling"

GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "/root/Destination"

# The destination's job finished; its fade-in waits out the minimum, controls still locked.
GET http://{{host}}/transition/state
[Options]
retry: 40
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "hold"
jsonpath "$.fade_pending" == true
jsonpath "$.min_hold_elapsed" == false

GET http://{{host}}/demo/state
HTTP 200
[Asserts]
jsonpath "$.scene" == "destination"
jsonpath "$.lock" == "transition"
jsonpath "$.destination.seed" == "{{seed}}"
jsonpath "$.destination.loader.stage" == "done"
jsonpath "$.destination.loader.attempts" >= 1
jsonpath "$.destination.loader.error" == null
jsonpath "$.destination.payload" startsWith "Payload "

# The Return button refuses while locked.
POST http://{{host}}/ui/press
{"id": "destination.return"}
HTTP 400

# ── 2. Skip the minimum: the fade-in runs and the controls free ──────────────
GET http://{{host}}/transition/skip
HTTP 200
[Asserts]
jsonpath "$.ok" == true

GET http://{{host}}/demo/state
[Options]
retry: 30
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.transition" == "none"
jsonpath "$.lock" == null
jsonpath "$.destination.arrived" == true

# ── 3. The menu locks the controls too ───────────────────────────────────────
POST http://{{host}}/ui/press
{"id": "destination.menu"}
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.lock" == "menu"

POST http://{{host}}/ui/press
{"id": "destination.menu.close"}
HTTP 200

GET http://{{host}}/ui/controls
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'destination.return' && @.visible == true && @.disabled == false)]" exists

# ── 4. Return: the result goes on the session, and back to the hub ───────────
POST http://{{host}}/ui/press
{"id": "destination.return"}
HTTP 200

GET http://{{host}}/session/state
HTTP 200
[Asserts]
jsonpath "$.launch" == null
jsonpath "$.result.kind" == "complete"
jsonpath "$.result.seed" == "{{seed}}"

GET http://{{host}}/transition/state
[Options]
retry: 50
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.stage" == "hold"
jsonpath "$.text" startsWith "Returning"

GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "/root/Main"

# The hub arrived under the loading screen and queued its fade-in, locked meanwhile.
GET http://{{host}}/demo/state
[Options]
retry: 20
retry-interval: 50ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.lock" == "transition"
jsonpath "$.hub.arriving" == true

GET http://{{host}}/transition/skip
HTTP 200

GET http://{{host}}/demo/state
[Options]
retry: 30
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "hub"
jsonpath "$.transition" == "none"
jsonpath "$.lock" == null
jsonpath "$.session.result.kind" == "complete"

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'main.travel' && @.visible == true && @.disabled == false)]" exists

# ── Teardown ─────────────────────────────────────────────────────────────────
GET http://{{host}}/quit
HTTP 200
