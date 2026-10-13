# scenes/devtools/

Debug-build and dev-only scenes for developers (scripts in `scripts/devtools/`; see
`scripts/devtools/README.md`). Nothing here is reachable from the game.

| Scene | Role |
|-------|------|
| `dev_lab.tscn` | The worked **dev-only scene** (`CLAUDE.md`, *Dev-only scenes*), controller `scripts/devtools/DevLab.cs`. Booted by path only: `godot-mono --path . res://scenes/devtools/dev_lab.tscn` (or a Hurl file's `# boot_scene:`). A `PaletteRect` background and a centred column (title, the seed and its source, the last roll, the `devlab.reroll` button) on layer 1, and the chrome — top bar `devlab.menu` and the menu overlay (`devlab.menu.*`) — on layer 20. Copy it as the starting point for a real tool scene (a generator lab, an all-AI observer). |
