# ui-024a — The planning screen: design and mock-up

**Status:** Done 2026-09-30 (mock-up approved; answers in `docs/mockups/planning/notes.md`)
**Design:** decisions-log 2026-09-29 *Faster base speed; rooms known by heart…* (Repeat and Plan) and 2026-09-30 *Known-by-heart details*; roadmap Part 2 (`ui-rework-roadmap.md`). Backlog Next 1b (UI half). Part A of two: this plan designs the screen, **ui-024b** builds it. Needs 023 (built).

## Goal
After the Summary the player picks **Repeat** (start the next run at once with the carried plan, skipping the planning screen) or **Plan** (go to a planning screen to edit the carried plan before the run). No planning screen is designed yet, so this plan designs one and draws it: an HTML mock-up in `docs/mockups/planning/` the user can open in a browser, and the design decisions written down, so ui-024b can build it without guessing.

No code, no Unity changes, no tests in this part.

## Design questions to settle (with the user, one at a time)
The 2026-09-29 decision said *Plan* opens the main screen paused, with a separate screen only if playtests want one. The user decided on 2026-09-30 that a planning screen needs designing first; these are the questions that decision opens:
1. **What it is:** its own screen (like the Summary), or the main screen in a "planning" mode (map and queue column, run not started, some panels swapped)? The mock-up shows both so the user can compare.
2. **What it shows:** the carried by-heart blocks per room; which rooms are by heart and how close the others are ("worked here in N runs, by heart at 4"); the map or a room list to add actions from; anything about the run ahead (vitality, stats, an estimate of time)?
3. **What the player can do there:** remove, reorder, unfold blocks, add actions (from where: map popovers, a list?); whether ways and scheduling-ahead work there as on the main screen.
4. **Leaving it:** one *Begin* button; can the player go back to the Summary; does it remember edits if the game is quit? Today the between-runs queue (`_nextQueue`) isn't saved, so quitting empties it (code-health 2026-09-28, which says to decide this with the planning screen). If the answer is "remember", ui-024b gets a save format change; mark the code-health row as settled either way.
5. **Blocks that can't start** (open since 2026-09-29): does the planning screen warn ahead of time, e.g. a block needing an item she won't have?
6. **Room speed (plan 025):** does the planning screen show each room's speed and the runs to the next step (overlaps UI-BACKLOG Next 8 (d))? Probably a slot for later. Whatever is answered here, trim Next 8 (d) to match so the two don't drift.
7. **Look:** it should match the gilt-and-dark direction of `ui-026` and the Increlution reference (clean rows, bold headings), and be screenshot-worthy.

## Reuse
- `docs/mockups/main-screen/2026-09-29-queue-column-layout.html`: the queue column mock-up's style and toggle pattern.
- `docs/mockups/summary/` (the Summary proposal) and `docs/mockups/increlution/` for layout references.
- The built queue column's cards and fold controls (ui-019), so the mock-up draws blocks the way the game already draws stops.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `docs/mockups/planning/YYYY-MM-DD-planning-screen.html` | New | The mock-up, with switches for the options in question 1 and for states (nothing carried; two blocks; a block unfolded). |
| `docs/mockups/planning/notes.md` | New | What the mock-up shows, the user's feedback, the answers. |
| `docs/mockups/README.md` | Edit | A row for `planning/`. |
| `DesignNotes/decisions-log.md` | Edit | One entry with the answers, naming the 2026-09-29 line it supersedes (*Plan opens the main screen paused*). |
| `docs/plans/finished/ui-024b-repeat-and-plan-build.md` | Edit | Fill in its planning-screen rows, steps and tests from the answers. |
| `docs/plans/ui-rework-roadmap.md` | Edit | Tick "Part 2 designed" (planning screen part). |
| `docs/UI-BACKLOG.md` | Edit | Next 8 (d): trim to what question 6 left open. |
| `docs/code-health/2026-09-28.md` | Edit | The `_nextQueue` row: settled by question 4 (point to ui-024b if it's built there). |

## Steps
1. Ask the design questions above, one at a time; write the answers into `notes.md` as they come.
2. Draw the HTML mock-up (both options for question 1 if still open), placeholder wording in British English.
3. The user looks at it in a browser; adjust until they're happy.
4. Log the decision; update ui-024b and the roadmap; trim UI-BACKLOG Next 8 (d) if question 6 was answered; mark the code-health 2026-09-28 `_nextQueue` row as settled (question 4).
5. Offer one commit for both plans (the ui-024 → ui-024b rename, the new ui-024a, the mock-up and the docs), so the split lands as one change. Commit only when the user asks.

## Done when
- [x] The user has approved the mock-up.
- [x] Every question above has an answer or is marked as left for playtest.
- [x] The decisions log entry is written and ui-024b has no "decided in ui-024a" gaps left.

## Notes after implementation
- All seven questions answered; the warning rule's detail (how far ahead it can tell) is left for ui-024b's plan, to settle with the user.
- New ideas from the session: doing an action N times (BACKLOG-later Ideas), dragging mirror nodes (UI-BACKLOG-later Ideas), real candle art (UI-BACKLOG-later Ideas).
- Correction: question 6 was asked as if plan 025 weren't built; it is, so ui-024b shows real room speed.
