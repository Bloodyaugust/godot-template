# Smoke scenario — the minimal end-to-end agentic flow against the demo scene:
# wait for boot, list the agent-tagged buttons, query a node group, then quit
# through the tagged button. See tests/hurl/README.md for the idiom and authoring
# rules. Run via tests/hurl/run-tests.ps1 / .sh, or:
#   hurl --test --jobs 1 --variable host=127.0.0.1:8080 tests/hurl/smoke.hurl

# ── 0. Wait for the server to come up (retry absorbs the boot delay) ──────────
GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.running" == true

# ── 1. The demo scene exposes its quit button via agent_id metadata ───────────
GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$" count == 1
jsonpath "$[0].id" == "main.quit"
jsonpath "$[0].visible" == true
jsonpath "$[0].disabled" == false

# ── 2. Group query: the demo Player registers in the 'player' group ───────────
GET http://{{host}}/nodes?group=player
HTTP 200
[Asserts]
jsonpath "$" count == 1
jsonpath "$[0].position.x" exists

# ── 3. Teardown: quit through the button under test (ends the instance) ───────
POST http://{{host}}/ui/press
{ "id": "main.quit" }
HTTP 200
[Asserts]
jsonpath "$.ok" == true
