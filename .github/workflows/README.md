# .github/workflows

CI/CD workflows. (`.github/` itself deliberately carries no `README.md` — GitHub
displays `.github/README.md` *instead of* the root `README.md` on the repo front
page, so the per-directory-README convention stops here.)

- `publish-itch.yml` — the Windows + Linux → itch.io release pipeline. Pushing
  a `v*` tag builds both `export_presets.cfg` release presets on an
  `ubuntu-latest` runner (Godot mono + export templates, cached; Godot
  cross-exports the Windows build from Linux), resolves the version from
  `GameVersion.Current` (`scripts/core/GameVersion.cs` — the single authority;
  tag builds fail fast on a tag-vs-constant mismatch, and `project.godot`
  `config/version` is stamped *from* the constant as derived metadata),
  smoke-checks that the exported **Linux** binary boots headless
  (`--quit-after`, exit 0 — the exit code is the whole check because release
  builds disable `AgentRestServer`, so there is no REST surface to probe;
  Linux because everything project-specific is identical across the two
  exports, the Windows exe is an upstream template binary, and the Windows
  Godot editor detaches from the console so it can't report exit codes
  anyway), and — when the workflow's `ITCH_PROJECT` env is set to
  `<user>/<game>` — pushes `build/windows` and `build/linux` to that itch.io
  project's `windows` / `linux` channels via butler with the tag as
  `--userversion` (requires the `BUTLER_API_KEY` repository secret, an itch.io
  API key). Local exports land in the same gitignored `build/` directory; its
  committed `build/.gdignore` must stay — without it the Godot editor tries to
  import exported binaries as assets (same rationale as `docs/.gdignore`), and
  the exporter packs earlier exports' loose resource-typed files (e.g.
  `deps.json`) into later pcks — which is why `.gitignore` whitelists that one
  file. A manual `workflow_dispatch` run is the dry-run path: build + smoke
  check + workflow artifacts, no publish.
