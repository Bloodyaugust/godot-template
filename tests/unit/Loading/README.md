# tests/unit/Loading/

Unit tests for the engine-free background loader in `scripts/core/loading/`.

| File | Covers |
|------|--------|
| `AsyncLoaderTests.cs` | `AsyncLoader<TIn, TOut>`: idle until started; the result published only by `Poll`, never by the worker; a second `Start` refused while running; attempts and failures counted and warnings delivered in order on `Poll`; a faulted job staying `Loading` with its error logged once, then retried; `Elapsed` frozen once done; `Clear` back to idle. |
