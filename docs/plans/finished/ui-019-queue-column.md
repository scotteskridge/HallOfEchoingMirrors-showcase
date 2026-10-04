# UI 019 — The queue as a column that folds to a strip

**Status:** Done (2026-09-29; user playtested)
**Design:** GDD §13a; decisions-log 2026-09-29 *Queue column and Schedule anywhere*; mock-up `docs/mockups/main-screen/2026-09-29-queue-column-layout.html`; `docs/UI-BACKLOG.md` *Now* 1 (second half)
**Why:** playtesting (user, 2026-09-29). The queue pops up from the bottom, is hard to read and is barely used, and the bottom strip takes space from the map.

## Goal
The queue sits on the right, top to bottom, beside the Story box. The player can fold it to a slim strip, which widens the map, or open it again, which pushes the map left. The full-width bottom strip is gone.

## Out of scope
- Where Schedule puts things: plan ui-015 (build that first).
- The slim top bar, backdrop, one material, capture clean-up: plan ui-026.
- A whole-queue forecast: the mock-up's "2:41 left" and "vitality left ≈ 11" aren't built.
- Tooltip redesign, Button/Chip prefabs, juice.

## Design assumptions
- **Settled by the user (2026-09-29):** a right-hand column; fold to a strip (current action and its bar); unfold pushes the map left; the player chooses; the choice is a player setting, not run save.
- **Claude's assumptions (say if wrong):** folded is not the default (the column starts open, so new players see the queue); the Story box narrows to about 400 px (from about 480) with its text size unchanged; the pockets overlay stays on the map's left.

## Reuse
- `StopCard`, `QueueRow`, `QueueEntryText`, `NestedScroll`: the cards and rows as they are.
- `QueueDrawer` (`EntryGapAt`, `StopGapAt`, `CardsScroll`): `StopGapAt` is horizontal and becomes vertical. Rows within a card are already vertical.
- `QueueDragController.FindGap`: unchanged apart from the vertical stop gap; Core's `CanMoveStop` / `CanMoveEntry` still decide.
- `SlideDrawer` (`Edge.Right`, `_pushed`, `Push`): the column is a one-page drawer on the right with the map as `_pushed`.
- `QueueRibbon`: becomes the folded strip; its `_drawer.OpenPage` click is replaced by the fold button.
- `Glow.Flash`: the strip glows when a Schedule lands while folded.
- Settings: raw `PlayerPrefs`, as `StatsRow.ShownKey` does (there's no Settings class).
- Setup: `GreyboxSetup.cs` (empty now), `EditorUiFactory`; next step number **94**; follow `.claude/rules/editor-tools.md`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `QueueDrawer.cs` | Edit | vertical `StopGapAt`; cards top to bottom |
| `QueueRibbon.cs` | Edit | folded strip; no drawer click |
| `QueueFold.cs` | New | fold/unfold button; remembers the choice in PlayerPrefs |
| `SlideDrawer.cs` | Edit only if needed | right edge and push already exist |
| `GreyboxSetup.cs` Step 94 "Queue column" | New | builds the column, strip and fold button; removes the bottom drawer, its tab and ribbon slot |
| `game_text.txt` | Edit | keys below |

**Text keys:** `queue.header`, `queue.footer`, `queue.fold`, `queue.unfold`, `queue.fold_tip`, `queue.unfold_tip`, `ribbon.next` (new or reworded); `tips.drawer_queue` (removed).

New content fields: none. Save format change: **no**.

## Steps
1. Screenshot the current screen at 1920×1080 into `docs/mockups/main-screen/` as `YYYY-MM-DD-before-queue-column.png`.
2. Vertical `StopGapAt` (EditMode test if it can be pulled out as plain maths, else a Play check).
3. Step 94 builds the column; Play, drag rows and stops, and check nothing overlaps. Screenshot.
4. `QueueFold` and the strip: fold, unfold, restart, and check the choice is kept. Screenshot both.
5. Play at 1920×1080 and 1280×800 (Steam Deck): nothing clipped. Judge the 1280 layout by eye with the user.
6. Retire Step 94 once it has run, update `docs/mockups/README.md`, and move the item out of `docs/UI-BACKLOG.md`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `SlideDrawerTests` (existing) | push and offset still right |
| `QueueGapTests.StopGap_Vertical` (if the maths can be pulled out) | stop drops land between the right cards |

## Done when
- [x] Console clean; all EditMode tests pass
- [x] The column lists stops top to bottom; drag and drop works; the current action shows its bar
- [x] Fold gives a strip with the current action and bar, and the map widens. Unfold pushes the map left. The choice survives a restart
- [x] No bottom strip; the map runs to the bottom edge
- [x] Checked at 1920×1080 and 1280×800
- [x] The user has played it and judged it against the before shot

## Notes after implementation
- Built as planned. Step 94 (`GreyboxSetup.cs`) made the layout; run twice (the second run added the scrollbar and two wire-ups).
- **Changed from the plan:** the card list uses a layout-driven height (Rows → Viewport → Content each pass their height up), so a card grows with its rows instead of its rows scrolling on their own. `SlideDrawer` gained `Close()` and an `instant` option; `QueueFold` needed `[DefaultExecutionOrder(100)]` so it runs after `SlideDrawer.Start`. No `QueueDrawer` test could cover the card layout, so `QueueGapTests` covers only the vertical drop maths (`StopGapBefore`).
- **Added beyond the plan (small, user-approved):** a vertical scrollbar; the strip glows when the player queues something while folded; Q folds and unfolds (main page only); the column opens instantly at startup; backtick (`) shows/hides the dev panel like F1.
- The plan's `queue.header`, `queue.footer` and `ribbon.next` were not needed: the existing Queue title, `queue.actions_count` and `ribbon.then` serve.
- Keys: added `queue.fold`, `queue.unfold`, `queue.fold_tip`, `queue.unfold_tip`; removed `ribbon.tip`, `tips.drawer_queue`, `scene.mainscreen_queuetab`; reworded `queue.actions_count_tip`.
- The Story box's bottom edge lifts instantly when folded (the map slides): see UI-BACKLOG-later.
- Not built (out of scope): the whole-queue forecast ("2:41 left"). The 1280×800 check was covered by the user's playtest.
- **Step 94 is still in `GreyboxSetup.cs`:** retire it after the commit (`git show 7a0dc41:Assets/Editor/GreyboxSetup.cs > Assets/Editor/GreyboxSetup.cs`, separate commit).
