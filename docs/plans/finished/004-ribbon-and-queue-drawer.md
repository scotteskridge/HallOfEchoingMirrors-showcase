# 004 — Ribbon and queue drawer (UI rework plan 3a)

**Status:** Done (2026-09-28)
**Design:** GDD v0.5 §13a *Proposed layout* (the route ribbon: "map for space, ribbon for sequence"), §13 [BUILT] queue; decisions-log 2026-09-27 *Main screen UI rework* (ribbon = current + progress + next; full queue in a slide-up drawer) and *UI rework: the mockup and plan 1's layout* (the drawer uses the route chip row: numbered stops, *return* marker), 2026-09-28 *Ribbon and queue drawer: the four choices*; mockups `Control strips — detail@1x.png`, `Run screen — map central@1x.png`; `docs/plans/ui-rework-roadmap.md` Part 1 row 3

## Goal
The empty ribbon band under the map shows what Clara is doing now (name, progress bar, time left) and what comes next. The drawer's Queue page shows the whole queue left to right as numbered **stop cards** (one per visit to a room, with a *return* marker for a room visited again). Each card lists the actions queued there, with To top and Remove.

## Out of scope
- **Drag to reorder:** plan 3b (backlog *Now*). It needs a Core move rule that refuses moves breaking the route.
- The queue forecast ("projected 4:48 · dry at 4:31"): stays in backlog *Later*.
- Editing repeat counts, and a Clear button (`ClearQueue` exists; no UI asked for).
- The route drawn on the map (plan 4); closing the drawer with Escape (backlog).
- Deleting `QueuePanel`, `QueueRow`'s old layout or the old page: switched off here, removed later (as with `ActionList`).

## Design assumptions
- **A stop** starts at each trip that will actually be made. The first stop is the room she's in (between runs, the start room), holding whatever is queued before the first trip. A trip `PlannedNodeAfter` would skip (not reachable from where she'd be) doesn't start a stop. It shows greyed in the card it falls in, with `reasons.cant_get_there` (new, display only; Core already skips it).
- **Return marker:** a stop whose room already appeared earlier in the queue (not counting where she started).
- **Ribbon, next:** the queue entry after the current one (a trip reads as its travel name, as in the queue today).
- **Ribbon, empty:** mid-run, a hint (`ribbon.empty`: click a room to plan her next step). Between runs it shows the queue planned for the next run (first entry, no progress), or the hint.
- **Clicking the ribbon opens the drawer on its Queue page** (tooltip says so).
- The drawer footer shows the action count only (no times until the forecast).
- **Time left** on the current action moves into Core (`CurrentTimeLeftSeconds`). This is a slice of backlog *Next* 1, taken because the ribbon needs it; the rest of that refactor stays there.

## Reuse
- Core: `ActionQueue`/`QueueEntry`, `Queue` (the next run's queue between runs), `PlannedNodeAfter`, `CurrentSpeed`, `LoopState.RunningEntry`/`CurrentTaskWorkDone`/`CurrentTaskWorkNeeded`/`CurrentTaskProgress`; queue calls `PlayNow`, `PlayTripNow`, `RemoveFromQueue`; event `QueueChanged`.
- UI: `QueuePanel`'s row details and tooltip (moved, not copied); `QueueRow` + prefab (rows inside a stop card); `TemplateList<T>`, `UiStyle`, `UiText` (`Countdown`, `ShowEmpty`), `ActionText.NameOf(sim, index, entry)`/`Tip`, `ToolTip`, `TextKey`, `SlideDrawer.Open`.
- Editor: `EditorUiFactory` (`MakeFrame`, `MakeText`, `MakeDisplayBar`, `MakeChipRow`, `MakeScrollList`, `AddStack`, `SwitchOff`, `SetTip`, `SetTextKey`, `Wire`); scene objects `Ribbon`, `Drawer`, `QueueTab`, `Queue` page.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/QueueStop.cs` | New | struct: `Room`, `Number`, `IsReturn`, `FirstEntry`, `EntryCount` |
| `Core/Simulation.Route.cs` | New | `QueueStops(List<QueueStop> into)`, `WillTravel(index)`, `CurrentTimeLeftSeconds` (Tasks.cs is already 743 lines) |
| `UI/QueueEntryText.cs` | New | the row detail and tooltip text lifted out of `QueuePanel`, shared by ribbon and cards |
| `UI/QueuePanel.cs` | Edit | uses `QueueEntryText` and `CurrentTimeLeftSeconds` (stays working until removed) |
| `UI/QueueRibbon.cs` | New | current name, bar, time left, "then: next"; hint when empty; click opens the Queue page |
| `UI/StopCard.cs` | New | number, room name, *return* marker, `TemplateList<QueueRow>` of its entries |
| `UI/QueueDrawer.cs` | New | `TemplateList<StopCard>` in a horizontal scroll; footer with the action count; rebuilds on `QueueChanged` |
| `UI/UiStyle.cs` | Edit | `StopCard`, `StopCardCurrent`, `ReturnMarker` |
| `Editor/GreyboxSetup.cs` | Edit | Step 79 (below) |
| `game_text.txt` | Edit | `## ribbon` (`then`, `empty`, `empty_between_runs`, `tip`), `queue.stop_tip`, `queue.return`, `queue.actions_count`, `reasons.cant_get_there` |
| `SampleScene.unity` | Edit | by Step 79 only |

New content fields: none. Save format change? No.

## Steps
1. Failing `QueueStopsTests`, then `QueueStop` and `Simulation.QueueStops`/`WillTravel`.
2. Failing `TimeLeftTests`, then `CurrentTimeLeftSeconds`; `QueuePanel` uses it.
3. `QueueEntryText` lifted out of `QueuePanel` (no behaviour change; the old list still works).
4. `QueueRibbon`.
5. `StopCard` and `QueueDrawer`.
6. **Setup Step 79: Ribbon and queue drawer.** Builds the ribbon inside `Ribbon`; builds the stop-card scroll inside the drawer's `Queue` page and switches off the old list (nothing deleted); wires both; tooltips and TextKeys. Safe to run twice, undoable, logs each change.
7. You run Step 79 and play a run; the step is then retired.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `QueueStopsTests.EmptyQueue_OneStopHere` | the room she's in is stop 1 |
| `QueueStopsTests.ActionsBeforeFirstTrip_BelongToFirstStop` | grouping |
| `QueueStopsTests.EachTrip_StartsNumberedStop` | numbering and entry ranges |
| `QueueStopsTests.RoomVisitedAgain_IsReturn` / `StartRoomRevisited_IsReturn` | the return marker |
| `QueueStopsTests.UnreachableTrip_StartsNoStop` | matches `PlannedNodeAfter`'s skip; `WillTravel` false |
| `QueueStopsTests.BetweenRuns_UsesNextRunsQueueFromStart` | between runs |
| `TimeLeftTests.HalfDone_HalfTheTimeLeft` / `FasterSpeed_LessTimeLeft` / `NothingRunning_Zero` | time left in Core |

## Done when
- [x] Tests above pass, all EditMode tests pass; compile and Console clean
- [x] In Unity: run Step 79, press Play. Queue a search in A Dark Hall, a trip, and two actions there. The ribbon shows the search with its bar filling and time counting down, and "then: …". Click the ribbon: the drawer opens on Queue, showing ① A Dark Hall (1 action) › ② the next room (2 actions); To top and Remove work in the cards; going back to A Dark Hall shows *return*. Empty the queue: the ribbon shows the hint.
- [x] Roadmap row 3 split into 3a (done) and 3b; decisions-log entry kept; `ui.md` line proposed for `QueueRibbon`/`QueueDrawer`; BUILD-STATE synced

## Notes after implementation
- **A trip is its stop's first entry** (`FirstEntry` points at it), so it shows as the card's first row with To top and Remove. The plan didn't say where a trip's row goes.
- **`reasons.cant_get_there` was dropped** for `Simulation.SkippedTripReason(index)`, the words the run itself uses (`CantTravel`), after review: the new line was wrong for a trip to where she'd already be and for a hidden way still to be found this run. Test `QueueStopsTests.TripToWhereSheWillBe_SaysAlreadyThere` added.
- **`SlideDrawer.OpenPage(page)`** added: the drawer had no way to be opened from code.
- **`QueueDrawer` recomputes the stops every frame**, not on `QueueChanged`: stops also change when she arrives, a way opens or a run begins.
- **Extra keys:** `queue.stop_empty`, `queue.actions_count_tip` (the footer's tooltip), `ribbon.empty_between_runs`; `queue.empty` removed with the old list.
- **The old queue list was removed, not just switched off** (the user asked after playtesting): `QueuePanel.cs` deleted; Step 79 removes its component, List and EmptyLabel.
- **Step 79's first version showed no cards**: the cards' scroll was pinned to a point (no size) and the side scroll ignored the cards' width. Fixed in the step and in `EditorUiFactory.MakeSideScroll`; the step checks each part, so re-running it repaired the scene.
- **New:** `NestedScroll` (the wheel over a card's rows scrolls the cards when the rows don't need it); `EditorUiFactory.MakeSideScroll` and `MainPage` (moved from the setup step so later steps share it).
- **Stop numbers are plain digits:** no font has ①②. Bold and italic card labels cut off rather than end in "…" (the font has no bold or italic "…").
