# boot_scene: res://scenes/devtools/dev_lab.tscn
# Dev lab — the worked dev-only scene (CLAUDE.md, "Dev-only scenes"), booted by path:
# its state, Reroll through the button and the route, a reseed that replays the same first
# draw, the 400s on a bad seed, then quit through its menu. The scene's absence elsewhere
# (the scene-hinting 503) is devlab_absent.hurl.

# ── 0. Wait for the server and the lab ────────────────────────────────────────
GET http://{{host}}/devlab/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.seed" isString
jsonpath "$.seed_source" == "default"
jsonpath "$.rolls" == 0
jsonpath "$.last_roll" == null

# ── 1. The chrome and the Reroll button are agent-tagged ──────────────────────
GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'devlab.reroll' && @.visible == true && @.disabled == false)]" exists
jsonpath "$[?(@.id == 'devlab.menu' && @.visible == true)]" exists

# ── 2. Reroll through the route draws a value ─────────────────────────────────
GET http://{{host}}/devlab/reroll
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.rolls" == 1
jsonpath "$.state.last_roll" isInteger

# ── 3. ...and through the button ──────────────────────────────────────────────
POST http://{{host}}/ui/press
{ "id": "devlab.reroll" }
HTTP 200

GET http://{{host}}/devlab/state
HTTP 200
[Asserts]
jsonpath "$.rolls" == 2

# ── 4. A reseed resets the draws; capture its first roll ─────────────────────
GET http://{{host}}/devlab/seed?seed=18446744073709551615
HTTP 200
[Asserts]
jsonpath "$.ok" == true
jsonpath "$.state.seed" == "18446744073709551615"
jsonpath "$.state.seed_source" == "agent"
jsonpath "$.state.rolls" == 0
jsonpath "$.state.last_roll" == null

GET http://{{host}}/devlab/reroll
HTTP 200
[Captures]
first_roll: jsonpath "$.state.last_roll"
[Asserts]
jsonpath "$.state.rolls" == 1

GET http://{{host}}/devlab/reroll
HTTP 200
[Asserts]
jsonpath "$.state.rolls" == 2

# ── 5. The same seed replays the same first draw ─────────────────────────────
GET http://{{host}}/devlab/seed?seed=18446744073709551615
HTTP 200
[Asserts]
jsonpath "$.state.rolls" == 0

GET http://{{host}}/devlab/reroll
HTTP 200
[Asserts]
jsonpath "$.state.rolls" == 1
jsonpath "$.state.last_roll" == {{first_roll}}

# ── 6. A bad seed is a 400 and leaves the lab as it was ──────────────────────
GET http://{{host}}/devlab/seed?seed=abc
HTTP 400
[Asserts]
jsonpath "$.ok" == false
jsonpath "$.error" contains "uint64"
jsonpath "$.state.rolls" == 1

GET http://{{host}}/devlab/seed?seed=-1
HTTP 400
[Asserts]
jsonpath "$.ok" == false

GET http://{{host}}/devlab/seed
HTTP 400
[Asserts]
jsonpath "$.error" contains "missing"

# ── 7. Teardown through the lab's menu ────────────────────────────────────────
POST http://{{host}}/ui/press
{ "id": "devlab.menu" }
HTTP 200

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'devlab.menu.quit' && @.visible == true)]" exists
jsonpath "$[?(@.id == 'devlab.reroll' && @.disabled == true)]" exists

POST http://{{host}}/ui/press
{ "id": "devlab.menu.quit" }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
