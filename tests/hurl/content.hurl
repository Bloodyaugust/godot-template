# The content pipeline at boot: the ContentBootstrap autoload (scripts/content/) loads every
# definitions file in _EnterTree and latches the first failure; /content/state reports it.
# Covers the placeholder examples.json — replace the example asserts with a real definition
# type's when it lands. See tests/hurl/README.md for the idiom.

# ── 0. Boot: everything loaded, no latched error ────────────────────────────────
GET http://{{host}}/content/state
[Options]
retry: 50
retry-interval: 200ms
HTTP 200
[Asserts]
jsonpath "$.loaded" == true
jsonpath "$.error" == null
jsonpath "$.files[0]" == "examples.json"
jsonpath "$.example_count" == 3
jsonpath "$.examples" count == 3

# ── 1. A record comes through typed, its enum back in snake_case ────────────────
GET http://{{host}}/content/state
HTTP 200
[Asserts]
jsonpath "$.examples[?(@.id == 'example_beta')].kind" == "heavy_support"
jsonpath "$.examples[?(@.id == 'example_beta')].cost" == 25

GET http://{{host}}/quit
HTTP 200
