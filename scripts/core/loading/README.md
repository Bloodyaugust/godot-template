# scripts/core/loading/

The **engine-free** background loader a scene runs under the loading screen (the flow:
`CLAUDE.md`, *Scene handoff*).

- `AsyncLoader.cs` — `AsyncLoader<TIn, TOut>`: the caller gathers the input on the main
  thread (every engine read happens there), `Start` runs the work on `Task.Run`, and the
  scene's `Poll()` each frame publishes the result and delivers the warnings the work queued
  (`ILoadProgress.Warn` / `Fail`) through injected sinks (`GD.PushWarning` / `GD.PushError`
  from a Godot caller). `Stage` (idle/loading/done), `Attempts`/`Failures` (interlocked, live
  mid-load), an `Elapsed` stopwatch, and `Error` from a faulted job. The shipped user is
  `scripts/demo/DestinationScreen.cs`.

Namespace: `godottemplate.Core.Loading`. Tests: `tests/unit/Loading/`.
