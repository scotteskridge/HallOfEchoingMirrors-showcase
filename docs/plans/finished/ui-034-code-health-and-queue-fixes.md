# ui-034 — Code health and queue fixes

**Status:** Done (2026-10-01); the user's five editor checks passed
**Left to do:** nothing.
**Design:** GDD v0.5 §13 (the queue), §13a (interface); `docs/CODE-STANDARDS.md` §1 (size, duplication), §3 (hot paths), §4 (errors); `docs/code-health/2026-09-30.md`; roadmap `ui-backlog-roadmap.md` row 2. No design change.

## Goal
The player sees three small fixes: a refused trip says where it was going ("Can't travel to the Long Gallery: …"); a click on a queue row always hits that row, even as a trip finishes; a running block's "2 of 5" drops when an action is removed from it. Everything else is tidier code under it: no queue text rebuilt every frame, `RoomPopover` and `MapView` under ~300 lines, and one helper for the orange reason line.

## Out of scope
- The milestone that shows three times (feed switch note, card, pop-up): parked to `docs/UI-BACKLOG-later.md` as a design call.
- New warning rules, the tooltip redesign (ui-036), button/chip prefabs (ui-035), room speed cues (ui-038).
- Other code-health 🟡s not named here (`StoryPopup` comment, `PoolBars` unknown hue, `ScreenManager`, fill-parent copies, `FeedNotes` flag).

## Design assumptions
- **Toast wording (the user, 2026-10-01):** a refused trip reads `Can't travel to {room}: {reason}`; other refusals keep `notices.cant`.
- **Stale click (Claude's call):** rows hold their `QueueEntry`, not an index. If that entry has left the queue when the click lands, nothing happens: the row is a frame from vanishing, so this is a race, not an impossible state. Core's index-based removes still throw on a bad index.
- **"N of M" (Claude's call):** M = actions finished or skipped in this block + actions still queued in it. Removing one lowers M; finishing one raises N.
- **Refresh rate (Claude's call):** queue, pockets and plan text rebuild at most 5×/s (the 0.2 s pattern `MapView` and `RoomPopover` use) and at once on `QueueChanged` or `GameText.Changed`. Bars still move every frame.

## Reuse
- `Simulation.RemoveStopOf(QueueEntry)` in `Simulation.Tasks.cs`: the entry-keyed remove that rows will copy.
- `Simulation.TaskCompleted`, `TaskSkipped` events in `Simulation.cs`: count a block's finished actions.
- `MapView._refreshSeconds` / `RoomPopover.TextSeconds`: the existing 0.2 s throttle.
- `GameText.Changed` (`Core/GameText.cs`), `TextKey`: text-reload pattern for "Read more".
- `PopoverPlacement` (pure rect maths, tested) and `DragToMove`: where the popover's placing and drag set-up go.
- `RoomKnowledgeText`: already shares the "worked N of M / by heart" text (that 🟠 is fixed).
- `StopCard.WarningMark`: becomes part of the shared reason helper.
- `SimulationTestBase.RecordRefusals`: refusal test helper.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.cs`, `Simulation.Places.cs`, `Simulation.Tasks.cs`, `Simulation.ByHeart.cs` | Edit | `ActionRefused` becomes `Action<TaskDefinition, NodeDefinition, string>`: destination for a trip, null otherwise |
| `Core/Simulation.Tasks.cs` | Edit | `bool RemoveFromQueue(QueueEntry)`: false if the entry is gone |
| `UI/NoticeToast.cs` | Edit | uses `notices.cant_travel` when a destination comes with the refusal |
| `UI/StopCard.cs`, `QueueRow.cs`, `QueueDrawer.cs`, `QueueDragController.cs` | Edit | rows keep their `QueueEntry`; index looked up when clicked or dragged |
| `UI/QueueDrawer.cs`, `StopCard.cs` | Edit | block count from events, not a growing max; text throttled |
| `UI/PocketsOverlay.cs`, `PlanPage.cs`, `QueueEntryText.cs` | Edit | text throttled, not every frame |
| `UI/WarningText.cs` | New | static: `Mark`, `ReasonLine(reason)`, `ActionWithReason(action, reason)`; used by `StopCard`, `QueueDrawer.RowTip`, `MapTagText` |
| `UI/MapTagText.cs` | New | static: `TagText`, `WarnMark`, `NotCarriedLine`, `RoomTip`, `RoomSpeedTip` moved out of `MapView` |
| `UI/PopoverDocker.cs` | New | plain C# class owned by `RoomPopover`: `PlaceBeside`, `RectInArea`, drag set-up. No new component, no scene change |
| `UI/MilestoneRow.cs`, `StoryPanel.cs` | Edit | "Read more" caption re-set on `GameText.Changed` |
| tests (`ActionRefused` subscribers) | Edit | new parameter |
| `game_text.txt` | Edit | new keys: `cant_travel: Can't travel to {room}: {reason}` (beside `cant`); `action_with_reason: {action}: {reason}` (queue section) |

Save format change? No.

## Steps
1. **Refused trip names its room (Core).** Failing tests first: a refused trip raises `ActionRefused` with its destination; a refused non-trip passes null. Change the event and its raisers and subscribers. Add the ui-033 follow-up: one `PlanWarningTests` case per kind of refused trip that `RoomWarnings` reports.
2. **Remove by entry (Core).** Failing tests first for `RemoveFromQueue(QueueEntry)`: removes that entry, returns false for one already gone, fires `QueueChanged` once.
3. **Rows target by entry.** `StopCard.Setup` gives each row its `QueueEntry`; To top, Remove, tip and drag look up the index when used and do nothing if it's -1. `QueueDrawer.RemoveEntry` takes the entry.
4. **"N of M" that can drop.** Replace `_blockSizes`' growing max with a per-block finished count (from `TaskCompleted`/`TaskSkipped` while that block runs); total = finished + `EntryCount`. Keep `ForgetGoneBlocks`.
5. **No per-frame text.** Throttle text in `QueueDrawer.Update`, `StopCard.Refresh`/`ShowBlock`, `PocketsOverlay.Update`, `PlanPage.Update`; rebuild at once on `QueueChanged`/`GameText.Changed`. Folded cards skip text; step 4 has already decoupled block sizes from it.
6. **Text fixes.** `NoticeToast` uses `cant_travel`; new `WarningText` replaces the four hand-built orange lines and the hard-coded ": "; "Read more" follows a reload.
7. **Splits.** `MapTagText` out of `MapView`, `PopoverDocker` out of `RoomPopover`. Aim for under ~300 lines each; if `MapView` is still over, name the next split in the report rather than forcing it. Pure moves, no behaviour change.
8. **Check and report.** Compile, Console, all EditMode tests; editor steps for the user (below).

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `PlacesTests.RefusedTrip_ReportsItsDestination` | a refused trip carries its room |
| `TaskTests.RefusedAction_ReportsNoDestination` | non-trips pass null |
| `PlanWarningTests.RoomWarning_<kind>` (one per refused-trip kind) | each kind is reported |
| `QueueGuardTests.RemoveByEntry_RemovesThatEntry` | the right row goes, even after the queue shifted |
| `QueueGuardTests.RemoveByEntry_GoneEntry_ReturnsFalse` | a stale click is harmless |
| `QueueGuardTests.RemoveByEntry_FiresQueueChangedOnce` | the UI redraws once |
| existing `PopoverPlacementTests`, `QueueReorderTests`, `ByHeartTests` | the moves broke nothing |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity (the user): (1) queue a trip she can't make → toast names the room; (2) open a running block of 5, remove one → "1 of 4"; (3) click Remove on a row just as the trip ahead finishes → that row goes, not its neighbour; (4) while playing, change `read_more` in `Assets/Text/game_text.txt` and save → within a second the Console says "Game text reloaded." and the milestone card's button shows the new words; (5) watch the queue, pockets and plan for a minute: timers and floor amounts may lag up to 0.2 s, never longer
- [x] `docs/UI-BACKLOG.md` Now 4 and the Next 9 item cleared at wrap-up

## Notes after implementation
Built in the UI lane on 2026-10-01 while the user was away. 832 EditMode tests ran: all pass except the three test-first tests the Features lane owns (`FloorTests.Carry_IsNeverOfferedForAContainer_EvenWithoutAMaximumOfOne`, `LabRulesTests.SkillGate_AndAMissingItem_TheRefusalNamesTheSkillFirst`, `OffersAttributeGateTests.AnAttributeGate_AndAMissingItem_TheOfferNamesTheAttributeFirst`), which fail until those rules are built. A play-mode smoke run (SampleScene, started a run, queued an action, asked for a trip with no way) showed the toast "Can't travel to a Dark Hall: …", cards with their words, and a clean Console.

Changes from the plan:
- **`RemoveFromQueue(QueueEntry)`** is an overload beside the index one (which still throws), as planned. Rows also look up their entry for To top, the tooltip and drag (`QueueRow.Entry`, set every frame); `QueueDrawer`'s block-remove click no longer indexes the queue with a stale stop (`BlockKey` returns null if the stop is out of date).
- **"N of M"**: instead of a per-block dictionary keyed by the block's last entry, only the first stop can be a block with finished actions, so the drawer keeps one record (`_activeRoom`, `_activeDone`, `_activeCount`). Done goes up by how far the queued count dropped, capped by the actions that finished or were skipped since last frame, so removing an action lowers the total and a repeating action doesn't inflate it. Known gap: a removal in the very frame an action finishes counts as a finish.
- **Throttle**: one small helper, `TextThrottle` (0.2 s, or at once after `MarkDirty`), used by `QueueDrawer`, `PocketsOverlay` and `PlanPage` (it is the plan's "0.2 s pattern" made once). `QueueDrawer` rebuilds at once on `QueueChanged`, `GameText.Changed` and a new simulation; `PocketsOverlay` words a chip at once when it appears, goes or changes count (so the glow and the numbers don't lag); `PlanPage` rebuilds on `GameText.Changed` and when the page turns in, not on `QueueChanged` (its words are slow-changing). `StopCard` rebuilds when its stop changes, folds or unfolds; a folded card skips row text. `MapView` and `RoomPopover` still keep their own copies of the 0.2 s timer.
- **`WarningText`** has four helpers (`Mark`, `ReasonLine`, `MarkedReason`, `ActionWithReason`); the new text key is `queue.action_with_reason`, not a bare `action_with_reason`.
- **Splits**: `MapTagText` out of `MapView` (449 → 389 lines) and `PopoverDocker` out of `RoomPopover` (404 → 361): both still over ~300. Next splits: `MapView`'s known-rooms/lines gathering and layout (`GatherKnownRooms`, `SameAsShown`, `LayOut`, ~80 lines) into a plain class; `RoomPopover`'s title, route note and the Play/Schedule/Carry tips (~70 lines) into a static `RoomPopoverText`.
- Added `PlanWarningTests.RoomWarnings_ATripTheWayCantMake_CountsInTheRoomSheWouldLeave` and `..._ATripRefusedForAnItem_CountsInTheRoomItEnters` for the ui-033 follow-up, plus `QueueGuardTests.RemoveByEntry_*` (3), `PlacesTests.RefusedTrip_ReportsItsDestination` and `TaskTests.RefusedAction_ReportsNoDestination`.
- After the reviewer's pass: a trip refused as it would start passes no destination (its reason already reads "to {room}, …", so the toast wouldn't repeat the room); only the click-time and between-runs refusals carry it. The block's Remove button now acts on its last row's entry too; "Read more" subscribes in `OnEnable`/`OnDisable`; `PlanPage` marks its text dirty on `QueueChanged`; fold clicks rebuild the drawer's words at once.
- The five editor checks passed (the user, 2026-10-01).
- **Second split pass (same day):** `MapContents` (known rooms and ways, next stops, has-way-in; plain class) out of `MapView` (389 → 305 lines), and static `RoomPopoverText` (title, route note, Play/Schedule/Carry tips) out of `RoomPopover` (361 → 307). Pure moves, text keys and serialized fields unchanged; 860 EditMode tests pass; Play check by the user. `FindNextStops` moved too (not named in the plan) to bring `MapView` near 300. Both files are still a few lines over; leftovers are in `docs/UI-BACKLOG.md` Now.
