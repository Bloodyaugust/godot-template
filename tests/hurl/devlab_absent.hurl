# boot_scene: default
# Dev lab, absent — the main scene's boot never reaches the dev-only lab, so /devlab/state
# is a 503 whose message names the scene to boot (the dev-only scenes' scene-hinting 503).
# The lab itself: devlab.hurl.

GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.running" == true

GET http://{{host}}/devlab/state
HTTP 503
[Asserts]
jsonpath "$.error" contains "res://scenes/devtools/dev_lab.tscn"

GET http://{{host}}/devlab/reroll
HTTP 503
[Asserts]
jsonpath "$.error" contains "res://scenes/devtools/dev_lab.tscn"

GET http://{{host}}/quit
HTTP 200
