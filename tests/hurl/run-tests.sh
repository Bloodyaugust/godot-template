#!/usr/bin/env bash
# Run the Hurl agentic scenarios. Boots a FRESH game instance per .hurl file
# (each file is self-contained: starts at the main scene, ends by quitting the
# game), and GUARANTEES teardown even when a file fails partway. See
# tests/hurl/README.md.
#
#   ./run-tests.sh                 # all *.hurl, headless
#   WINDOWED=1 ./run-tests.sh      # watch it run
#   ./run-tests.sh smoke.hurl      # a single file
#   PORT=9090 ./run-tests.sh       # non-default REST port
set -uo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/../.." && pwd)"
port="${PORT:-8080}"
hostport="127.0.0.1:$port"
windowed="${WINDOWED:-0}"

if ! command -v hurl >/dev/null 2>&1; then
    echo "hurl not found on PATH. Install it (see tests/hurl/README.md), e.g.:"
    echo "  macOS: brew install hurl   |   Linux: https://hurl.dev/docs/installation.html"
    exit 127
fi

files=("$@")
if [ "${#files[@]}" -eq 0 ]; then files=("$here"/*.hurl); fi

# Pin the player profile to memory-only on every instance we boot: test runs
# must never touch the developer's real user://profile.json (display-settings
# mutations included — display_input.hurl).
export GODOT_PROFILE_MEMORY=1
# The instances we boot listen on the port the scenarios are aimed at, so PORT can run
# beside a game already holding the default one.
export GODOT_AGENT_REST_PREFIX="http://$hostport/"
# Per-tool env overrides go here too: a dev tool that writes files (a save path, an
# export dir) gets an env var pointing it under tmp/hurl/, never over tracked content.

failed=0
for f in "${files[@]}"; do
    [ -f "$f" ] || f="$here/$f"
    echo "=== $(basename "$f") ==="
    # Boot scene: the configured main scene unless the file carries a
    # "# boot_scene:" directive in its header. "default" means no scene
    # argument — the configured main scene; any other value is passed to
    # godot-mono as the scene path (e.g. res://scenes/foo.tscn), so files
    # exercising a non-default scene never have to walk the menu flow.
    scene="$(sed -n 's/^#[[:space:]]*boot_scene:[[:space:]]*\([^[:space:]]*\).*/\1/p' "$f" | head -n1)"
    scene="${scene:-default}"
    gargs=(--path "$root")
    [ "$scene" != "default" ] && gargs+=("$scene")
    [ "$windowed" != "1" ] && gargs+=(--headless)
    godot-mono "${gargs[@]}" >/dev/null 2>&1 &
    pid=$!
    hurl --test --jobs 1 --variable host="$hostport" --error-format long "$f" || failed=$((failed + 1))
    # The file quits itself on success; force teardown on any failure path.
    curl -fsS "http://$hostport/quit" >/dev/null 2>&1 || true
    # Wait for the instance we launched to exit (up to ~10 s), so the next file's
    # status wait can't reach this one while it shuts down. Only ever kill the
    # specific PID we launched — never godot-mono by name.
    for _ in $(seq 50); do kill -0 "$pid" 2>/dev/null || break; sleep 0.2; done
    if kill -0 "$pid" 2>/dev/null; then kill "$pid" 2>/dev/null || true; fi
    wait "$pid" 2>/dev/null || true
done

if [ "$failed" -gt 0 ]; then echo -e "\nFAILED: $failed file(s)"; exit 1; fi
echo -e "\nAll Hurl tests passed"
exit 0
