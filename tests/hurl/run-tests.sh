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
    sleep 0.8
    # Only ever kill the specific PID we launched — never godot-mono by name.
    if curl -fsS "http://$hostport/status" >/dev/null 2>&1; then kill "$pid" 2>/dev/null || true; fi
done

if [ "$failed" -gt 0 ]; then echo -e "\nFAILED: $failed file(s)"; exit 1; fi
echo -e "\nAll Hurl tests passed"
exit 0
