# tests/hurl/

Scripted agentic tests: plain-text [Hurl](https://hurl.dev) scenarios that drive a
running game through the debug REST interface (`scripts/server/AgentRestServer.cs`).

They exist to make agentic verification **fast and stable**. The game is a live
`_Process` simulation, so when an agent hand-drives `curl` calls, wall-clock passes
between requests and the world moves out from under it — every step races the sim. A
Hurl scenario moves that waiting out of the agent loop: it's authored once and run
with a single command, and its retry mechanism **waits on state** instead of guessing
at timing.

## Prerequisites

- `hurl` on `PATH` (see **Install** below).
- `godot-mono` on `PATH` (the debug REST server is autoloaded only in debug builds).

## Running

The runner boots a **fresh headless instance per file**, runs it, and guarantees
teardown even on a mid-file failure. By default each file boots the configured
main scene; a file can override this with a directive comment in its header —
`# boot_scene: res://scenes/foo.tscn` passes that scene to `godot-mono` as the
scene argument (`# boot_scene: default` is the explicit form of the default), so
files exercising a non-default scene never have to walk the menu flow to reach
it.

```sh
tests/hurl/run-tests.ps1                 # PowerShell: all *.hurl, headless
tests/hurl/run-tests.ps1 -Windowed       #   watch it run in a window
tests/hurl/run-tests.ps1 -Files smoke.hurl   # a single file — bare positional
                                             # misbinds to -Port under PS 5.1

tests/hurl/run-tests.sh                  # bash equivalent
WINDOWED=1 tests/hurl/run-tests.sh
```

Both runners pin `GODOT_PROFILE_MEMORY=1` on every instance they boot: the whole
**player profile** (`scripts/session/PlayerProfile.cs`) goes memory-only, so test
runs never touch the developer's real `user://profile.json` — display-settings
mutations included (`display_input.hurl`). A file run **by hand** against a
manually-booted game doesn't get the pin — boot with the env var set if profile
writes would interfere.

Or run one file by hand against an **already-booted** game (handy while iterating):

```sh
godot-mono --path . --headless &
hurl --test --jobs 1 --variable host=127.0.0.1:8080 --error-format long tests/hurl/smoke.hurl
```

`--jobs 1` is required: there is a single game instance on one port, so files must
not run in parallel against it. `--error-format long` prints the full response body
on a failed assert — the state snapshot that tells you *why* it failed.

## The idiom

1. **Retry IS the wait.** Hurl re-runs an entry until all its `[Asserts]` pass or the
   `retry` budget is spent. That's the auto-wait — no `sleep`, no polling loop in the
   agent.
2. **Separate mutations from polls.** Because retry replays the *whole* entry, a
   mutation (a build, a purchase, a spawn) lives in its own entry with **no** retry;
   the "wait until it settled" lives in the **next** entry — a `GET .../state` with
   `[Options] retry`. Never put a mutation in a retried entry, or it fires repeatedly.
3. **Capture, don't hardcode.** Pull ids and coordinates out of responses with
   `[Captures]` and reuse them as `{{var}}`. Anything the game generates at runtime
   (rolled content, generated maps, session ids) differs per boot — a hardcoded value
   is a latent flake. For clicking buttons, `/ui/controls` reports each control's
   on-screen `rect` with a ready-made `center_x`/`center_y` (Hurl captures can't do
   arithmetic) — capture those instead of pinning layout coordinates
   (`display_input.hurl`).

## Authoring rules (learned the hard way)

**Rule 1 — assert the positive form of a JSONPath count.** Hurl's `count` filter
errors (`missing value to apply filter`) on a JSONPath filter that matches **zero**
nodes. So `jsonpath "$.items[?(@.done == false)]" count == 0` is unusable. Assert a
filter that always matches at least one node instead, e.g.
`jsonpath "$.items[?(@.done == true)]" count >= 3`.

**Rule 1b — a filter matching exactly ONE node unwraps to an object, breaking
`count` too.** Hurl's JSONPath returns a bare object (not a one-element list) when a
filter matches a single node, and `count` then errors with `invalid filter input
type`. So `count` is only safe when the match is guaranteed to be ≥ 2. For
"at least one exists" (including inside a retry that waits for the first match), use
`exists` instead: `jsonpath "$.items[?(@.kind == 'foo')]" exists`. For "exactly
one was created", `exists` after a mutation guaranteed to add it is the
practical assert.

**Rule 1c — a filter predicate cannot reach into a nested array.**
`$.items[?(@.tags[0].id == 'x')]` matches **nothing**, and so does
`$.items[?(@.kind == 'x')].tags[0].field`. What works is a filter that matches
exactly one node followed by a wildcard hop:
`$.items[?(@.uid == 12)].tags[*].weight == 1.3` (a nested *object* under the
wildcard hop is fine — only arrays inside filters break). For "does any element
have a second tag?", the global sweep `$.items[*].tags[1]` **not exists** does the
job, and `$.items[*].tags[*].id` `count == N` counts carriers across the board.

**Rule 2 — use the GET aliases for bodyless calls.** Windows' HTTP.sys rejects a
bodyless `POST` with `411` before the app sees it. Routes that take no body (`/quit`,
and any bodyless mutation route you add) should accept `GET` — use it.

**Rule 3 — best-effort steps get no asserts.** If a step *can* legitimately be
rejected by game rules (e.g. a placement that may collide with generated content),
omit the `HTTP 200` line and asserts so a rejection is tolerated instead of failing
the run. Reserve hard asserts for steps that are guaranteed valid.

**Rule 4 — assert phases and identity, never timings or display names.** Anything
that runs on wall-clock (async connects, timeouts, grace windows) is noise under
test load — assert the *state transitions* and let retry do the waiting; never
assert how long a transition took. Likewise assert stable identifiers (ids, keys)
rather than rendered display text, which is presentation and may change or collide.

**Teardown.** Every file ends by quitting the game (`GET /quit`, or pressing a quit
button through the surface under test) so a successful run leaves nothing behind. The
runner additionally force-quits (and, only as a last resort, kills the **specific PID
it launched** — never `godot-mono` by name) so a failed run can't leave an instance
holding port 8080.

## Adding a test

Copy `smoke.hurl` as a starting point. If a flow you want to test isn't reachable via
the existing endpoints, extending `AgentRestServer` (a new route, a new `agent_id` on
a button, a typed domain interface) is part of the implementation — see the
verification workflow in `CLAUDE.md`. Cheap, deterministic test hooks (grant-resource,
reset-state, force-event routes) beat farming gameplay for setup. The full route list,
response envelopes, and the `agent_id` button surface are in `scripts/server/README.md`
— keep that file authoritative when you add or change routes.

## Install

**Requires hurl ≥ 8.0.1.** Combined JSONPath filters
(`$.items[?(@.kind == 'foo' && @.team == 'blue')]`) are the workhorse of these
scenarios, and hurl 4.x–6.x reject the `&&` and fail every such file with
`Invalid JSONPath`. Also note the two JSONPath quirks Rules 1b/1c above work
around: a filter matching **exactly one** element returns that object (not a
1-element list), so `count == 1` errors — assert `exists` (or count a set that is
always ≥ 2) instead; and chained filters (`[?(...)][?(...)]`) don't compose for
value extraction, so combine predicates with `&&` in a single filter.

- **Windows:** `winget install --id Orange-OpenSource.Hurl` (binary lands at
  `C:\Program Files\Hurl\hurl.exe`; may not be on the current shell's `PATH` until a
  new shell — the runners fall back to that path automatically).
- **macOS:** `brew install hurl`
- **Linux / other:** https://hurl.dev/docs/installation.html
