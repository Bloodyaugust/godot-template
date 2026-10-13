# _system-template

Outline for a system design doc in `design/game/` (or `design/visual/`). Copy it to
`design/game/<system>.md`, rename the heading, and delete this paragraph and every
italic guidance line. The leading underscore keeps this stub sorted to the top; it is not
a system doc and is not listed in any index.

---

# <System>

One paragraph of scope: what this system is, where the player first meets it, and which
other systems build on it. Terms are defined in `README.md`'s glossary.

Mechanics (the units, the constants, the validation rules, the algorithm) are stated by the
code in `<path>`. This doc states intent. *Settled YYYY-MM-DD from `ideas/done/<note>.md`.*

## Goals

*Three to six bullets, each a bold one-line lead plus a sentence or two of why. These are
what every later decision in the doc is judged against.*

- **<Goal in one line.>** Why it matters, and what it rules out.
- **<Goal in one line.>** Why it matters, and what it rules out.

## <Topic>

*One section per topic, in the order a reader needs them. State the decision in plain
prose, then attach the reasoning with inline markers, each opening its own paragraph:*

The decision, stated so a reader can predict the system's behaviour without the code.

*Why:* the reason the decision is this and not the obvious alternative.

*Rejected:* the alternative considered, and why it lost — so nobody re-litigates it.

*Balance intent:* the feel or balance the first numbers aim for (the numbers themselves
live in code).

*Revised YYYY-MM-DD:* what changed, what it used to be, and why — kept beside the rule it
revises rather than in a changelog.

*Settled while building (YYYY-MM-DD):* a decision the build made that the plan left open,
or a plan item the build changed.

### <Subtopic>

*Use subsections when a topic has distinct parts; keep each focused.*

## Agent surface

How agents observe and drive the system: the typed domain interface or the existing surface
it extends, what its state reports, which operations it offers, and the Hurl scenarios that
cover it. In intent only — the routes are the contract in `CLAUDE.md`'s endpoint table.

## Built-ins and the amendment

*Optional: include only when the system departs from a built-in.* Which Godot built-ins the
system still uses, and which part is hand-rolled under the dated amendment in `CLAUDE.md`,
restating the concrete reason the built-in doesn't fit.

## Deferred

*Always last.* Work deliberately postponed: features, open questions, known limits. This is
the single home for postponed work on the system; other docs point here (`<system>.md`,
*Deferred*) instead of restating it, and a session with no objective set picks from these.

- <Postponed item, with the trigger that would bring it back.>
