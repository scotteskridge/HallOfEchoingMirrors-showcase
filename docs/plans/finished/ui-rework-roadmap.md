# UI rework — roadmap

**Status:** Done 2026-09-30. Part 1 done 2026-09-28; Part 2 (the planning screen) designed in ui-024a and built in ui-024b, c, d. Further UI work lives in `docs/UI-BACKLOG.md`; the open questions below were moved there.
**Left to do:** nothing.
**Design:** GDD v0.4 §13a *Interface — the Map Is the Queue* (a starting point, not a spec), decisions-log 2026-09-27 *Main screen UI rework*

This file holds the overall plan so it survives between sessions. It is not a build plan: each row below gets a numbered plan (`docs/plans/NNN-….md`) when its turn comes, and this file is updated as each one finishes.

## Goal
A main screen that looks good and is built around the map and routing, so the game reads as distinct from *Idle Loops* and *Increlution* (as *Stuck in Time*'s map set it apart). Today the map is about 10% of the screen; a map that small makes routing impossible to judge in a playtest.

## Settled (decisions log 2026-09-27)
- **Two halves of planning:** a **planning screen before the run** for automating mastered content (Part 2), and **live play on the map** for unmastered content (Part 1). May change after playtesting.
- **Visual level: structure and motion, no art.** Layout, hierarchy, type, a palette in `UiStyle` and the Map Style asset; motion such as the route drawing in, Clara's token walking, glows. Art waits for stage 3.
- **Layout work:** a one-click setup step builds each new layout piece; the user then adjusts it by hand, and from then on it's theirs.
- **The queue keeps a full view.** The **ribbon** under the map shows only the current action with its progress bar and the next scheduled action (if any). A small **tab at the bottom of the screen slides up a queue drawer**: the whole queue laid out horizontally, with more information, overlaying part of the map, where the queue is adjusted.
- **Story and ambient text in one panel** (the user's guess, to test once the new UI is in): the current Story panel (story beats with benchmark times) combined with the ambient feed. So ambient lines stay in the panel rather than on the map, for now.
- **The Summary page rework is separate**, after this.

## Part 1 — the main screen (live play)
Built on existing systems. Each step leaves the game playable; an old panel stays until its replacement works.

| # | Plan | What it does | Replaces |
|---|---|---|---|
| 1 | **Layout shell** | The map fills the centre; top bar (vitality, drain, clock, speed, pause); the combined story and ambient panel on the right; space for the ribbon and bottom strip; the one-click setup step | The current arrangement |
| 2 | **Room popovers** | Click a room: its actions, floor and search bar in one popover over the map; Play and Schedule from there | The task list |
| 3 | **Ribbon and queue drawer** | Ribbon: current action + progress + next. Bottom tab slides up the full horizontal queue for adjusting | The queue list |
| 4 | **Route on the map** | The queued trips draw as a numbered line (a room visited twice shows *1, 3*); Clara's token walks it | — (new) |
| 5 | **Floor on the map** | Each room's floor drawn as coloured dots, only when non-empty (restoratives, pathos, tools) | Floor rows in the inventory |
| 6 | **Pockets and stats strip** | Pockets and stats in the bottom strip (see *Inventory*, below); stats as one row of multipliers, click to expand | The inventory panel (with its Floor rows, kept by plan 5); the stats and skills block |

**From the design chat's mockups (decisions log 2026-09-27 *UI rework: the mockup and plan 1's layout*):** the four bands (top bar, map and right rail, ribbon, bottom strip) are a proportion study, not pixel values. Plan 1: the old Actions, Queue, Inventory and Stats panels live as tabs in the slide-up drawer until replaced; speed becomes segmented tiers plus one locked tier; the map keeps follow and zoom. Plan 2 follows the popover mockup (greyed unavailable actions with reasons; visits and "by heart" wait for visit counts). Plan 3's drawer uses the route chip row (numbered stops, *return* marker, drag to reorder); the queue forecast is a backlog item to consider then.

**Motion** goes into the plan it belongs to (route drawing in and the walking token in 4, glows in 5 and 6), not a separate polish pass.

## Inventory: undecided (recommendation, to settle in plan 6)
The aim: always easy to see, never cluttered. Recommended: **three layers, detail on demand.**
1. **Always visible:** a compact pocket strip in the bottom bar: one small entry per item with its count, and "3/5 pockets" (the ratio packing decisions turn on). Hover for the tooltip.
2. **Where it's relevant:** the current room's floor inside its popover (plan 2); other rooms' floors as dots on the map (plan 5). So the floor needs no panel.
3. **On demand:** a *Pockets* tab in the same slide-up drawer as the queue, with the full list, containers (pouch, satchel) and Put down / Pick up.

## Part 2 — the planning screen (after Part 1)
A screen before the run where the player sets up automation for content Clara has **mastered**; unmastered content is still played live. It needs a **"mastered" rule** first. Candidates to design: the 8 searches rule (decisions log 2026-09-26 *Ways, later*), and the GDD's visit counts, "by heart" folding and realm mastery (§13a). Design questions to settle before planning: what counts as mastered, what automation can do (a saved route? standing orders?), and how the planned part hands over to live play.

## Questions for later plans
- The story and ambient panel: does the combination work? (Playtest after plan 1.)
- Standing orders and realm speed have slots in the GDD's layout: leave them out until built.
- The Summary page: its own rework, after Part 1.

## Progress
- [x] 1 Layout shell (plan 002, 2026-09-28)
- [x] 2 Room popovers (plan 003, 2026-09-28)
- [x] 3a Ribbon and queue drawer (plan 004, 2026-09-28)
- [x] 3b Drag to reorder in the drawer (plan 005, 2026-09-28)
- [x] 4 Route on the map (plan 006, 2026-09-28)
- [x] 5 Floor on the map (plan 007, 2026-09-28; the inventory's Floor rows stay until plan 6)
- [x] 6 Pockets and stats (plan 008, 2026-09-28; a pockets overlay and a stats row on the map, not a strip)
- [x] Finish Part 1 (plan 009, 2026-09-28; the drawer pushes the map up, the stats row replaces the Stats tab, one Story box, retired panels deleted)
- [x] Part 2 designed: the planning screen (plan ui-024a, 2026-09-30; mock-up in `docs/mockups/planning/`)
- [x] Part 2 built (plans ui-024b, ui-024c, ui-024d, 2026-09-30)
