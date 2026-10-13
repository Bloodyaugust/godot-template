# boot_scene: res://scenes/demo/destination.tscn
# Direct boot of the scene-handoff destination (no launch in flight): it launches itself on a
# fresh seed, puts its own loading screen up, runs its job, fades in and frees the controls.
# The full round trip from the hub: handoff.hurl.

GET http://{{host}}/status
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "/root/Destination"

GET http://{{host}}/session/state
HTTP 200
[Asserts]
jsonpath "$.launch.destination" == "destination"
jsonpath "$.launch.seed" isString

# Best effort: the loading screen may already be fading by now (Rule 3).
GET http://{{host}}/transition/skip

GET http://{{host}}/demo/state
[Options]
retry: 40
retry-interval: 100ms
HTTP 200
[Asserts]
jsonpath "$.scene" == "destination"
jsonpath "$.transition" == "none"
jsonpath "$.lock" == null
jsonpath "$.destination.loader.stage" == "done"
jsonpath "$.destination.payload" startsWith "Payload "

GET http://{{host}}/ui/controls
HTTP 200
[Asserts]
jsonpath "$[?(@.id == 'destination.return' && @.visible == true && @.disabled == false)]" exists

GET http://{{host}}/quit
HTTP 200
