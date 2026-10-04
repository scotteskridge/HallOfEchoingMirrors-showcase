# Planning screen: notes (plan ui-024a)

Answers to the design questions in `docs/plans/finished/ui-024a-planning-screen-design.md`, in the order they were settled. Mock-up: `2026-09-30-planning-screen.html` (three queue states; switches for the popover and the candles). **Approved by the user 2026-09-30.**

## Feedback on the mock-up (2026-09-30)
- Looks great. The candles are **placeholder art**; better candles come in the art pass (UI-BACKLOG-later Ideas).
- New idea, out of scope: **drag the mirror nodes** to lay out the map as the player likes. Affects both screens and the save, so it went to UI-BACKLOG-later Ideas.

## 1. What it is (2026-09-30)
Its own screen, but **very similar to the main game screen**. The top bars and story bars are removed so the map and the queue take up 100% of the screen; leave room for other features we find are needed.

## 2. What it shows (2026-09-30)
- The carried plan as folded by-heart blocks in the queue column.
- By-heart progress on the map (each room: "Known by heart", or "worked here in N runs (by heart at 4)").
- Not chosen for now: a run-ahead strip (vitality, stats, estimate) and room speed. Leave a spare area for them.

## 3. What the player can do (2026-09-30)
- Remove, reorder, unfold blocks; add actions from the map popovers; ways and scheduling ahead work as on the main screen; a *Clear plan* button.
- **Easy count increment on actions** (Idle Loops style: do this action N times, then move on). Not in Act I today (verbs are "once until complete" or "loop until full"), but new verbs will need it soon. The mock-up draws a `− N +` stepper on an entry as a **labelled placeholder**; the rule itself (what counts as one, how it interacts with ways and by-heart blocks) is an open design question, and the feature belongs in BACKLOG, not in ui-024b.

## 4. Leaving it (2026-09-30)
One **Begin** button; no way back to the Summary. The between-runs plan (`_nextQueue`) is **saved**, so quitting and returning keeps the edits. ui-024b gets a save format change (`SaveData.CurrentVersion` bump, upgrade step, round-trip test) and closes the code-health 2026-09-28 row.

## 5. Blocks that can't start (2026-09-30)
**Warn ahead.** A block or entry that would be refused (e.g. needs an item she won't have by then) gets a warning mark and a tooltip with the reason. Needs a Core check that walks the plan; because earlier entries change what she holds, it may be an approximation. Where it can't tell, it stays silent rather than crying wolf. The exact rule is for ui-024b's plan to settle with the user before building.

## 6. Room speed (2026-09-30)
Show a speed line on each room. The mock-up's numbers are made up, but plan 025 is already built (Claude wrongly said it wasn't when asking), so ui-024b shows the real values: `Simulation.RoomSpeed(room)` and the runs to the next step from `LoopSettings.RoomSpeedAfter(runs)`. UI-BACKLOG Next 8 (d) is trimmed to the Summary part.

## 6b. Crowded room tags and the empty stop (2026-10-01, plan ui-033 follow-ups)
- Room tags now draw on their own layer above every room frame, and the speed line is smaller (`MapStyle.speedLineSize`, 80% of the tag). Before/after: `2026-10-01-map-tags-*-before-a-b.png`. Rooms still sit close (`MapStyle.pixelsPerUnit` 150): a tag can still cross a neighbour's frame or name.
- **An empty stop isn't marked as won't-carry** (the user): nothing in it can be lost. The Queue column keeps its grey "by heart in N of 4 runs" note; neither the queue nor the map shows a ⚠. Decided in one place, `StopCard.MarksNotCarried`.

## 7. Look (2026-09-30)
Same palette, fonts, tooltips and built features as the main screen, so assets are reused. The difference is atmosphere: with the extra screen space, **two flickering candles, one on each side**, casting light and flickering shadows over the map. (Decorative only; needs to be cheap: a sprite or two and a light/shader flicker, no simulation.)
