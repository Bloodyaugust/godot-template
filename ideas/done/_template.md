# _template

*Shipped YYYY-MM-DD; archived. The settled design is `design/game/<system>.md`. Changed in the build: <what the build did differently from this note>, with the design docs carrying the revisions.*

Outline for archived design notes. Copy and rename when archiving a shipped idea. The leading underscore keeps this stub sorted to the top.

**The archive stamp.** The italic line above opens every archived note, right under its heading, written when the note moves to `done/`: *Shipped* for a built feature, *Settled* for a design interview folded into `design/` (add "**Built** YYYY-MM-DD" once it ships). It names the doc that now holds the settled design, and lists what changed between the note and the build, so a reader knows where the note is no longer true. The body below it is left as it was.

**`(proposed)` markers.** While a note is in flight, any detail the agent filled in that the human hasn't decided is marked **(proposed)** inline. The human accepts, edits or rejects each one; on archiving, the stamp records the outcome (e.g. "the human accepted every **(proposed)** item as written"), and the markers stay only to show which details were the agent's.

**Two shapes.** A design note (the outline below) works out *what* to build. A build-plan note, written once the design is settled in `design/`, is the build order instead, and uses these sections: *Relationship to* (the design doc it implements, the docs it touches, what it supersedes or unblocks), *Approach* (per layer, the concrete first-cut values), *Steps* (numbered, each with the files and tests it lands), *Out of scope*, *Human verification once built* (the feel, visual and balance checks the agent can't make), and *Later questions*.

## Goal

One-sentence problem statement. What was this idea trying to solve?

## Relationship To

- Link to prior ideas this builds on or supersedes (`other-idea.md`).
- Note ideas this enables or blocks.

Delete the section if the idea stands alone.

## Approach

The core design — typically 1-4 subsections walking through the structure of the solution. Use pseudocode blocks where it clarifies. Keep each subsection focused; if a design alternative was considered and rejected, capture the tradeoff in its own subsection rather than burying it in prose.

### Alternative considered (optional)

Briefly: what the alternative was, why it lost. Saves the next reader from re-litigating settled questions.

## Settled Decisions

Bullet list of decisions baked in during implementation. Future readers should treat these as load-bearing — if a settled decision needs to change, that's a new idea, not a tweak.

- Decision 1.
- Decision 2.

## Later Questions

Open follow-ups deferred past this idea's ship date. These are the seeds of future `ideas/` entries.

- Question 1.
- Question 2.
