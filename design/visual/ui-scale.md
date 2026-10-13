# UI scale and the canvas floor

How every screen copes with real window sizes and pixel densities. The mechanics (the
constant, the snapping, the cap) live in `scripts/ui/DisplayScale.cs`; this doc says what
they promise and why.

## The canvas floor

Every screen is laid out for a canvas of **at least 1280×720 UI pixels** (`MinCanvas`).
UI pixels are what's left after the UI scale: a 1920×1080 window at 1.5× is exactly the
floor. At or above the floor, everything fits without scrolling. Below it, screens degrade
gracefully (see *Bars are fixed*) rather than promising a layout. The project's default
window size is the floor itself.

## The auto scale respects the floor

The automatic UI scale starts from the monitor's density (its reported scale, else
DPI/96), capped at 2×. It is then **capped by the window's real pixels**: the largest
quarter step that still leaves the floor (never below 1×). The cap follows the live
window, so auto re-derives when the window resizes or goes fullscreen. An explicit player
override is not capped (up to 3×); a player who asks for more scale than the window holds
gets the below-floor fallback.

*Why:* density alone over-scales a small window on a dense screen. A 1920×1080 window on a
254-DPI laptop panel auto-scales to 2× — a 960×540 canvas — and a screen's top and bottom
bars get pushed off it. *Rejected:* a fixed design resolution through the
`canvas_items`/`viewport` stretch modes (see `CLAUDE.md`, *UI Panels*); the floor is a
lower bound for responsive layouts, not a design size.

## Bars are fixed

A screen's top and bottom bars (the shared top bar, `scenes/ui/top_bar.tscn`, and any
bottom bar a screen adds) are never crowded out, at any size. Only the body between them
flexes, and the body never pushes the bars: when the canvas is below the floor and the
body can't fit, the body scrolls instead. Panels inside the body that hold long content
(stat lists, logs) scroll on their own before it comes to that, keeping their actions in
view.
