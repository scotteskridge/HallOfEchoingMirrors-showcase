# ui-024b — Repeat and Plan after the Summary; by-heart blocks in the queue

**Status:** Done (built and playtested 2026-09-30; setup Steps 97 and 98 run, committed and retired)
**Left to do:** nothing. The won't-carry mark and Plan's target carry on in ui-024c/d.
**Design:** decisions-log 2026-09-29 *Faster base speed; rooms known by heart…* (the two buttons), 2026-09-30 *Known-by-heart details*, *Rooms known by heart built (plan 023)* and *The planning screen (plan ui-024a)*; GDD §13a (the word *by heart*). Mock-up: `docs/mockups/planning/2026-09-30-planning-screen.html` (block cards) and `notes.md`. Backlog Next 1b (UI half). **First of three:** ui-024b (this) → `ui-024c-planning-screen.md` → `ui-024d-plan-warnings.md`.

## Goal
After the Summary the player picks **Repeat** (start the next run now) or **Plan** (look at and edit the carried plan first). In the queue column, the actions carried from rooms known by heart show as one folded block per room visit, and a room's popover says how well she knows it.

## Out of scope
- The planning screen itself, map tags, the room speed line, Clear plan, candles, ▶ between runs: **ui-024c**.
- Warnings ahead (⚠): **ui-024d**.
- Saving the between-runs plan: **already built** (save v16, plan 023; `ByHeartTests.NextQueue_AndRoomRuns_RoundTrip`). No save format change here.
- Doing an action N times; dragging map nodes; real candle art (all in the later backlogs).

## Design assumptions
- **Repeat** is today's *Next run* button renamed (`GameController.BeginLoop()`); with nothing carried it just starts the run.
- **Plan always shows**, even in runs 1–3 when nothing is carried (the user, 2026-09-30).
- **Interim, until ui-024c:** Plan opens the main screen paused between runs (`ScreenManager.ShowMain()`, no `BeginLoop`). The column there already shows the next run's queue (`Simulation.Queue` is `_nextQueue` between runs), and the header's Begin button starts it. ui-024c points Plan at the planning screen instead.
- **A block = a stop whose entries are all by heart** (`QueueEntry.ByHeart`), matching the mock-up: the stop card's head gets a violet "by heart" badge, "N actions" and a fold button. A stop that mixes carried and player-added entries is **not** a block: it shows unfolded as today, and its carried rows get a small "by heart" mark.
- **Folded by default.** Unfolding is per block, remembered while the game runs (keyed by the block's first `QueueEntry` object, so it survives the column redrawing), not saved.
- **Folded:** ✕ removes the whole stop's entries; dragging moves the stop (the existing `MoveStop`). **Unfolded:** rows behave as today (remove, drag, to top one by one).
- **During a run** a folded block that's running stays folded and reads "A Dark Hall · by heart · 2 of 5 · Search the hall", with the running bar (the user, 2026-09-30).
- **Popover line** under the room name: "Known by heart", "Worked here in N runs · by heart at 4" (4 = `LoopSettings.byHeartRuns`), or "Not worked yet", with a tooltip.

## Reuse
- `RunResultsPanel._nextRunButton` → `GameController.BeginLoop()`; `ScreenManager.ShowMain()`; `RunHeader._beginButton` (Begin between runs).
- `Simulation.QueueStops` / `QueueStop` (Core/QueueStop.cs): the stop grouping blocks are built on.
- `QueueDrawer`, `StopCard`, `QueueRow`, `QueueDragController` (`CanMoveStop` / `MoveStop` in `Simulation.Reorder.cs`); `Simulation.RemoveFromQueue` (Simulation.Tasks.cs:209) as the model for removing a stop.
- `Simulation.IsKnownByHeart`, `RunsWorkedIn` (Simulation.ByHeart.cs); `LoopSettings.byHeartRuns`.
- `RoomPopover`, `ToolTip`, `UiText`, `UiStyle`; `EditorUiFactory.MakeButton` / `Wire` for the setup step.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/QueueStop.cs`, `Simulation.Route.cs` | Edit | `QueueStop.ByHeart`: every entry in the stop is by heart. |
| `Simulation.Tasks.cs` | Edit | `RemoveStop(int stopIndex)`: removes a stop's entries, with the same care as `RemoveFromQueue` (fail loudly on a bad index). |
| `RunResultsPanel.cs` | Edit | Repeat (renamed) and a new `_planButton` → `ScreenManager.ShowMain()`; tooltips. |
| `Editor/GreyboxSetup.cs` (or a new `RepeatPlanSetup.cs`) | Edit / New | *Setup → Step 97: Repeat and Plan*: adds the Plan button beside Repeat and wires it. |
| `StopCard.cs`, `QueueRow.cs`, `QueueDrawer.cs` | Edit | Block head (badge, count, fold button, running line), fold state, ✕ and drag as a unit; by-heart mark on rows. Layout via Step 97 (Step 94, the queue-column step, was retired once run; no setup steps are left, so 97 is the next number and is one-shot too: retire it once run and committed). |
| `RoomPopover.cs` | Edit | The by-heart line and its tooltip. |
| `game_text.txt` | Edit | `## results`: `repeat_button`, `repeat_tip`, `plan_button`, `plan_tip` (rename the old next-run key). `## queue`: `by_heart`, `by_heart_actions`, `by_heart_running`, `by_heart_tip`, `block_fold_tip`, `block_unfold_tip`, `remove_block_tip`. `## popover`: `known_by_heart`, `runs_worked`, `not_worked`, `tip.by_heart`. |

New content fields: none. Save format change: **no**.

**Layout changes to list in the report:** the Plan button on the Summary; the block card head (badge, count, fold); the by-heart row mark; the popover line.

## Steps
1. Failing EditMode tests for `QueueStop.ByHeart` and `RemoveStop`; then build them.
2. Summary: rename Repeat, add Plan (interim target), tooltips, text keys; setup step 97.
3. Block cards: head, fold/unfold with remembered state, ✕ and drag as a unit, row mark.
4. The running line on a folded block.
5. Popover by-heart line and tooltip.
6. Docs: code-health 2026-09-28 `_nextQueue` row → built in plan 023 (save v16); UI-BACKLOG *Now*; plans README.

**Follow-up from commit 524c9fd (save version 17), check before step 1:** in Play mode, load an existing save slot made before 17 (ideally one saved mid-run). It should load with no warnings in the Console, and an unfinished run's Summary should still list its actions. The upgrade has a test (`ARunLoggedBeforeVersion17_KeepsItsReport_ButCarriesNothing`) but hasn't been tried on a real save.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ByHeartTests.Stop_AllCarriedEntries_IsByHeart` | A carried stop is a block |
| `ByHeartTests.Stop_WithPlayerEntry_IsNotByHeart` | A mixed stop is not |
| `ByHeartTests.Stop_ByHeartEntriesInTwoRooms_AreTwoBlocks` | One block per room visit |
| `ByHeartTests.RemoveStop_RemovesAllItsEntries_AndNothingElse` | Remove as a unit |
| `ByHeartTests.RemoveStop_BadIndex_Throws` | Fails loudly |

## Done when
- [ ] Tests pass; compile and Console clean.
- [ ] In Unity (Balance Sheet: *Runs to by heart* lowered to 1 for the check): end a run; the Summary shows **Repeat** and **Plan**. Repeat starts the run and it does the carried actions. Next Summary: Plan opens the main screen paused with a folded "by heart" block; unfold it, remove one action, fold it, drag it below another stop, press Begin. While the block runs it stays folded and shows "2 of N · …". A room's popover shows "Known by heart"; an unworked one "Not worked yet".
- [ ] Every layout change listed in the report.

## Notes after implementation
- Built as planned, with these choices: the block's running count ("2 of 5") is keyed by the block's **last** entry (the first leaves the queue as it runs); fold state is per **room**, kept in PlayerPrefs (`QueueDrawer.OpenBlocksKey`), so it survives runs and sessions (the plan said per first entry, not saved); an entry can't be dropped into a folded block (`StopCard.EntryGapAt` refuses); the carried rows of a mixed stop show a small violet "by heart" after their name (no new layout); the fold and remove buttons use `-`/`+` and `x` (no new font characters); `RemoveStop(stopIndex)` throws `ArgumentOutOfRangeException` on a bad index; `QueueStop` gained a `byHeart` constructor argument (default false).
- The Summary button's field is now `_repeatButton` (`FormerlySerializedAs` keeps the scene wiring); its scene object was renamed RepeatButton and the old `scene.runscreen_plannextbutton` text key removed.
- Follow-ups built on request: a *Fold all / Open all* button at the top right of the Queue column (setup Step 98); a "Nothing carried over yet" line in the first stop while planning with an empty queue (`queue.stop_empty_planning`); a violet feed line when a run makes a room known by heart (`feed.known_by_heart`). Retire Steps 97 and 98 once run and committed.
- Playtest finding, built on request: stops that won't carry into the next run are marked in the Queue column ("· by heart in 2 of 4 runs", or "won't carry: an earlier room isn't by heart", with a tooltip); `QueueStop.CarriesOver`, tests `Stop_CarriesOver_OnlyWhileEveryRoomSoFarIsKnownByHeart` and `Stop_AfterARoomThatDoesntCarry_DoesntCarryEither`. Decisions log 2026-09-30 (*Planning in a room not known by heart is allowed…*). ui-024c must carry the same mark to the planning screen's map tags and rows, and ui-024d's warnings should cover it.
- Tests: the five in the table, plus `Stop_WithNoEntries_IsNotByHeart`, all in `ByHeartTests`.
- Version 17 check (2026-09-30): no save from before 17 exists (the only file, slot 1, is version 17, current). Instead the real slot 1 was rewritten as a version 16 save (plain log, no steps) and loaded against the real content: 0 warnings. Its run had no actions yet, so the mid-run case rests on `ARunLoggedBeforeVersion17_KeepsItsReport_ButCarriesNothing`.
- Wrap-up review fixes: `CarriesOver` now judges a room the way the carry does when the run ends (this run's work adds one to the count; mid-run, a room already worked in that won't be known breaks the carry after it), with tests `Stop_InARoomOneRunShort_CarriesOnceWorkIsQueuedThere` and `Stop_AfterWorkingInARoomThatWontBeKnown_DoesntCarry_MidRun`; the by-heart feed and popover texts now say the carry needs every earlier room known too; the block's Remove button targets by entry (`Simulation.RemoveStopOf`) and `RemoveStop` fires `QueueChanged` once; "1 action" singular; the duplicate two-rooms test now checks a return visit. Left for `docs/UI-BACKLOG.md` Next 9: stale row indices, per-frame strings, the running block's total drifting.
