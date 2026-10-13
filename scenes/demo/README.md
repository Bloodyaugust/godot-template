# scenes/demo/

The scene-handoff demo's destination (controller `scripts/demo/DestinationScreen.cs`; see
`scripts/demo/README.md`). Delete with the demo.

| Scene | Role |
|-------|------|
| `destination.tscn` | Reached from the hub's Travel (`main.travel`) through the transition overlay, or booted directly (`godot-mono --path . res://scenes/demo/destination.tscn`). A `PaletteRect` background and a centred panel (title, Lorem Ipsum body, the job's payload, the `destination.return` button) on layer 1, and the chrome — top bar `destination.menu` and the menu overlay (`destination.menu.*`) — on layer 20. |
