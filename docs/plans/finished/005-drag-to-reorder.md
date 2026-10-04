# 005 — Drag to reorder in the queue drawer (UI rework plan 3b)

**Status:** Done (2026-09-28; setup Step 80 run and retired)
**Design:** GDD v0.5 §13a *Proposed layout* (the ribbon "doubles as the drag target for reordering"; moved to the drawer's stop cards); decisions-log 2026-09-28 *Ribbon and queue drawer: the four choices* (drag split off; "a Core rule refusing moves that break the route") and *Plan 004 as built* (a trip is its stop's first row; skipped trips greyed with the run's reason); `docs/plans/ui-rework-roadmap.md` row 3b; backlog *Now*

## Goal
In the queue drawer the player can drag an action row to a new place, or drag a whole stop card (its trip and the actions queued there) to a new place in the route. While dragging, a line shows only at gaps where the drop is allowed; hovering a refused gap shows why. A refused drop snaps back.

## Out of scope
- Dragging on the ribbon or the map (the route on the map is plan 4).
- Auto-scrolling the drawer when dragging near its edge (backlog, if playtesting wants it).
- Merging a moved entry into a neighbouring entry of the same action; keyboard reordering; the queue forecast.

## Design assumptions (settled with the user 2026-09-28 unless marked *chosen*)
- **Both rows and stops drag.** A stop's header drags the whole stop. Its trip row drags the whole stop too (*chosen*: a trip moved alone would leave its actions in another room). Stop ① has no trip and doesn't drag as a stop.
- **The move rule (Core).** A move is refused if, after it, (a) a trip that would have been made is now skipped, or (b) an action that could be done in its stop's room now sits in a stop whose room doesn't offer it (`IsAvailableAt`). The reason is the first break found: the run's own trip reason (`CantTravel`) or `reasons.not_here`. Entries that were already broken before the move don't count against it (*chosen*).
- **The running entry can be dragged, like To top.** If something is dropped above it, she stops and starts the dropped one, and the stopped action keeps its progress (`SuspendCurrentTask`). If it's dragged down, it's suspended and the new top starts.
- **Between runs** the same rule applies to the next run's queue, planned from the start room.
- **Supplier links** (`SuppliesFor`) survive a move. A step checks whether they're held by reference or by index (*chosen*: fix them if they're held by index).
- **A move never merges entries** (*chosen*).

## Reuse
- Core: `ActionQueue.Entries`, `Simulation.Queue`, `QueueStops`, `WillTravel`, `SkippedTripReason`, `PlannedNodeAfter`/`NodeAfterEntry`, `IsAvailableAt`, `CantTravel`, `SuspendCurrentTask`, event `QueueChanged`; To top's move in `Enqueue(atTop)`.
- UI: `QueueDrawer`, `StopCard` (`_firstEntry + local` index mapping), `QueueRow` + prefab, `NestedScroll` (pattern for passing events to the side scroll), `MapView`'s drag handlers (pattern), `ToolTip`, `UiStyle`, `TextKey`.
- Editor: `EditorUiFactory` (`MakeFrame`, `MakeText`, `SetTip`, `SetTextKey`, `Wire`, `MainPage`), the drawer's `Queue` page from Step 79.
- Tests: `SimulationTestBase` (`MakePlaces`, `Reason`), `QueueStopsTests` fixtures.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.Reorder.cs` | New | `CanMoveEntry(from, to, out reason)`, `MoveEntry`, `CanMoveStop(stop, beforeStop, out reason)`, `MoveStop`; one private `MoveBreaksRoute` check over a trial copy of the order |
| `Core/Simulation.Tasks.cs` | Edit | only if needed: expose the suspend/restart path To top uses, so moves share it |
| `UI/QueueDragController.cs` | New | on the drawer: ghost, insertion line, gap finding under the pointer, asks Core `CanMove…`, calls `Move…` on drop, shows the refusal reason near the ghost |
| `UI/QueueRow.cs`, `UI/StopCard.cs` | Edit | begin/drag/end handlers that hand off to the controller (row: entry index; header or trip row: stop) |
| `UI/UiStyle.cs` | Edit | `DropLine`, `DragGhost` |
| `Editor/GreyboxSetup.cs` | Edit | Step 80 (below) |
| `game_text.txt` | Edit | `queue.drag_tip`, `queue.cant_move` ("can't go here: {0}") |
| `.claude/rules/simulation.md`, `ui.md` | Propose | one line each (moves in `.Reorder.cs`; `QueueDragController`) |

New content fields: none. Save format change? No (the order is saved as the list; check the running-entry index after a move in step 2).

## Steps
1. Failing `QueueReorderTests` for entry moves, then `CanMoveEntry`/`MoveEntry` with the route rule.
2. Failing tests for the running entry and supplier links, then make them pass (suspend, restart, links, the saved running index).
3. Failing tests for stop moves, then `CanMoveStop`/`MoveStop` (built on the entry range from `QueueStops`).
4. `QueueDragController`, plus the drag handlers on `QueueRow` and `StopCard`: gaps, the line, the ghost, the refusal label.
5. **Setup Step 80: Queue drag.** Adds the controller, drop line and ghost to the drawer's `Queue` page, wires it, sets tooltips and TextKeys. Safe to run twice, undoable, logs each change.
6. You run Step 80 and play a run. The step is then retired.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `QueueReorderTests.MoveActionWithinStop_Reorders` | basic move, `QueueChanged` fires |
| `QueueReorderTests.MoveActionToStopForSameRoom_Allowed` | return visits accept the room's actions |
| `QueueReorderTests.MoveActionToOtherRoom_RefusedNotHere` | rule (b) and its reason |
| `QueueReorderTests.MoveThatSkipsATrip_RefusedWithTripReason` | rule (a) and its reason |
| `QueueReorderTests.AlreadyBrokenEntries_DontBlockAMove` | only new breaks count |
| `QueueReorderTests.DropAboveRunning_SuspendsItKeepingProgress` / `DragRunningDown_SuspendsIt` | running entry like To top |
| `QueueReorderTests.MoveKeepsSupplierLink` | `SuppliesFor` intact |
| `QueueReorderTests.MoveStop_MovesTripAndItsActions` / `MoveStopWhereNoWay_Refused` / `FirstStop_CantMove` | stop moves |
| `QueueReorderTests.BetweenRuns_MovesNextRunsQueue` | between runs |
| `QueueReorderTests.RefusedMove_ChangesNothing` | a refusal leaves the queue as it was |

## Done when
- [x] Tests above pass, all EditMode tests pass; compile and Console clean
- [x] In Unity: run Step 80, press Play. Queue a search, a trip to a neighbouring room, two actions there, and a trip back. Open the drawer. Drag the second action above the first: they swap. Drag an action into a card for another room: no line there, and "can't go here: …" by the ghost; letting go snaps it back. Drag stop ③ before ②: refused if there's no way, and moved (with its actions) if there is. Drag a row above the running action: she switches at once, and the old action keeps its bar.
- [x] Roadmap row 3b ticked; decisions-log entry; rules-file lines proposed; BUILD-STATE synced

## Notes after implementation
- **Nothing can be dropped in front of stop ①** (`CanMoveStop(s, 0)` is refused, no line is shown there). Found in review: a trip dropped there would quietly move stop ①'s anywhere-actions to the other room. Test `StopBeforeFirstStop_Refused`. To be confirmed by the user.
- **Four follow-ups added at the user's request:** the dragged row or stop fades (`UiStyle.DraggedAlpha`); no tooltips open during any drag, anywhere in the game (`ToolTip` checks `eventData.dragging`, `ToolTipPanel.HideAny`); Esc cancels a drag; the cards and a card's rows scroll while dragging near their edge (was out of scope; Inspector settings *Edge Zone* and *Scroll Speed*).
- Only the left mouse button drags. Loading a game mid-drag drops the drag (`SimulationChanged`).
- `Simulation.Tasks.cs` needed no change: moves reuse `SuspendCurrentTask`. Supplier links are held by reference and the save stores the running entry's index at save time, so neither needed work.
- Step 80 sets no tooltips or TextKeys: the ghost and line can't be clicked and are filled by code. It wires only empty fields (new helper `EditorUiFactory.WireEmpty`).
- New text key `queue.drag_stop` (the ghost's label for a stop), beyond the two planned.
- `StopsOf` moved into `SimulationTestBase` (shared with `QueueStopsTests`).
- Side effect: a click-drag on a card or row now reorders, so those lists no longer scroll by dragging them (backlog, Later).
