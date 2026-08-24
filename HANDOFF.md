# HANDOFF

## What this file is

A **handoff prompt** between work sessions on this project. It captures what the last
session accomplished and what the next session should pick up, so a fresh agent (or
a returning human) can get to work without re-deriving context. It is a living
document, not a historical log — each handoff **replaces** the previous one.

> **Use this workflow only when a human explicitly asks for it.** The handoff
> process (reading this at session start, regenerating it at session end) is not a
> default mode of operation — don't read, follow, or update this file unless a human
> specifically requests the handoff flow. When it isn't requested, ignore this file.

It complements, and does not duplicate, the standing docs:

- `CLAUDE.md` / `README.md` — how the project is built and what conventions hold.
- `design/` — the authoritative design docs: settled system specs and visual
  language (the *what* and *why*).

This file is the *current* connective tissue: "here's where we are, here's what's
next."

### How to use it

1. **At the start of a session**, read this file top to bottom, then read the docs it
   points at. Treat the "Next session" section as your brief.
2. **As you work**, follow the project's verification workflow in `CLAUDE.md`
   (compile, agentically test via the REST surface, update READMEs).
3. **At the end of the session**, regenerate this file (below).

### How to regenerate it

Ask the agent to **rewrite `HANDOFF.md` for the next phase**. A good regeneration:

- Keeps this top "What this file is" section intact.
- Replaces "What we did this session" with a concise summary of the session that's
  ending (what shipped, what was tested, what's deferred).
- Replaces "Next session" with the agreed next objective, scoped tightly, with
  in/out-of-scope boundaries and a concrete done-criteria checklist.
- References exact file paths and the relevant docs.

### What must NOT accumulate here

This file holds only **transient** connective tissue — knowledge with no other
home *yet*. Everything else moves to its owning doc at the moment it's learned,
never parked here:

- **Gotchas, invariants, quirks** → the owning `design/` doc, the directory
  README, or a `tests/hurl/README.md` authoring rule. If it can't be settled
  yet, it's an `ideas/` note.
- **"Where the machinery lives" maps** → the directory READMEs are the index;
  never mirror them here.
- **Rot-prone literals** — file counts, member names, constant values — point
  at the owning file instead of restating.

**The litmus test**: a line that would survive the *next* regeneration unchanged
is standing knowledge and belongs elsewhere. Past handoffs that accumulated
"standing wrinkles" and machinery maps drifted into stale names and outright
wrong claims — duplicated facts rot. When regenerating, migrate anything
standing *first*, then write the brief.

Keep it skimmable. It's a brief, not a novel.

---

## What we did this session

*(Nothing yet — this is a fresh project. Replace this section at the end of the
first session that uses the handoff flow.)*

---

## Next session

*(No objective queued. When a session ends with agreed next steps, capture them here:
the objective, what's in and out of scope, a done-criteria checklist, and the exact
files/docs to read first.)*
