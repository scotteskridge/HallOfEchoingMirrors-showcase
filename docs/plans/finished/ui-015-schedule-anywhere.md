# UI 015 — Schedule in any room she can reach

**Status:** Done
**Design:** GDD §13 (Play/Schedule/Carry), §13a (numbered stops on the map); decisions-log 2026-09-29 *Queue column and Schedule anywhere*; `docs/UI-BACKLOG.md` *Now* 1 (first half; the column is plan ui-019)
**Why:** playtesting (user, 2026-09-29). Schedule is greyed in every room except the one where the queue ends, so nothing can be queued ahead, which defeats the point of a queue.

## Goal
Schedule works in any room Clara can reach. In a room on her route, the action joins that room's last visit. In a room off it, the queue adds the shortest walk there, then the action. Clicking a far room on the map adds the shortest walk too. The map's numbered badges show where it landed.

## Out of scope
- The queue column, fold and strip, and removing the bottom drawer: plan ui-019.
- Play and Carry: as drafted, unchanged. **Changed during the build:** Play now also works in other rooms (see Notes after implementation); Carry is unchanged.
- Queue length limit, end-of-queue flag, mid-run editing cost (§13 [OPEN]): unchanged.
- A whole-queue forecast (time left, vitality at the end): not built.

## Design assumptions
- **Settled by the user (2026-09-29):**
  - On her route: the action goes in at the end of the room's **last** visit, before she walks on. Example: *Room 1: a, b → Room 2 → Room 1: a, c*, where c is new.
  - Off her route: the shortest walk from where the queue ends, then the action.
  - Shortest = fewest seconds of walking. Ties go to the fewest rooms, then to the order the ways are listed.
  - **The walk uses only ways found this run** (`IsUsable`), so a queued walk never gets skipped. A room reachable only through ways known from past runs stays greyed, with a reason.
  - **Map clicks** on a far room use the same search and add the whole walk.
- **Claude's assumptions (say if wrong):**
  - Stop 1 (where she is now) follows the same rule: if it's her last visit there, Schedule goes after that stop's actions.
  - A map click always extends the route from where the queue ends, even if the room is already on the route (a click means "go there next").
  - The walk is saved as ordinary trip entries, one per way, so the save format and drag and drop are unchanged.
  - Between runs, planning from the start room uses the same rules.

## Reuse
- `Simulation.Schedule` / private `Enqueue(entry, atTop)` in `Simulation.Tasks.cs`: the one place entries go in. It gains an insert-at-index path.
- `Simulation.Places.cs`: `FindWay`, `IsUsable`, `DestinationsFrom` (single hop, the search's neighbours), `PlannedNodeAfter`, `PlannedEndNode`, private `CantTravel` (reason strings), `CanScheduleTripTo` / `ScheduleTrip` (map clicks).
- `Simulation.QueueStops` and `QueueStop` (`Simulation.Route.cs`): find a room's last visit and the index after its entries.
- A trip is a `QueueEntry(TravelVerb, destination)`; no new entry type.
- `RoomPopover.CanScheduleHere` (line 204), the one place the grey rule lives in the UI; `MapView` line 261 (map clicks).
- `MapRoute`: stop badges and the route line already exist (setup Step 81), and badges `Glow.Flash` when they change, which shows where an action landed.
- Tests: `SimulationTestBase.MakePlaces`; neighbours `PlacesTests`, `QueueStopsTests`, `TaskTests`.
- No existing shortest-path code: `RoomsOnMap` is a flood fill, not a route search.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/RouteSearch.cs` | New | fewest-seconds route over usable ways; fixed tie-break |
| `Core/ScheduleTarget.cs` | New | the result: insert index, walk to add first, or can't plus reason |
| `Simulation.Tasks.cs` | Edit | `Schedule` uses the target; `WhereScheduleLands(task)` query |
| `Simulation.Places.cs` | Edit | `CanScheduleTripTo` / `ScheduleTrip` use `RouteSearch` for far rooms |
| `RoomPopover.cs` | Edit | ask Core; the one-line route note under the title |
| `game_text.txt` | Edit | keys below |
| `Assets/Tests/EditMode/` | New | `RouteSearchTests`, `ScheduleTargetTests`; new cases in `PlacesTests` |

**Text keys (for a `/writing` check):** `popover.on_route` (*On her route: stop {0}*), `popover.adds_walk` (*Not on her route yet: she walks here first ({time})*), `reasons.no_found_way` (new); `actions.schedule_tip` (reworded); `popover.schedule_elsewhere` (removed); `popover.play_elsewhere` (reworded) and `popover.carry_elsewhere` (new), added when Play went anywhere.

New content fields: none. Balance fields: none. Save format change: **no**.

## Steps
1. Failing tests for `RouteSearch`, then build it.
2. Failing tests for where a Schedule lands, then `ScheduleTarget` and `Simulation.Schedule`. Existing tests pass unchanged (Scheduling where the queue ends behaves as now).
3. Map clicks: failing tests in `PlacesTests`, then `CanScheduleTripTo` / `ScheduleTrip` add the whole walk.
4. `RoomPopover`: ask Core, add the route note, and use the new text keys. Run all EditMode tests.
5. Play: Schedule in a room on the route, in a room off it, and in one reachable only by a way known from a past run. Click a far room on the map. Check the badges glow where it landed.
6. Decisions log (the found-ways and map-click answers are already recorded), move the item in `docs/UI-BACKLOG.md`, then `/sync-state`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RouteSearchTests.PicksFewestSeconds` | a longer path of shorter walks beats one slow way |
| `RouteSearchTests.TieBreaksByRoomsThenListOrder` | the result never depends on luck |
| `RouteSearchTests.IgnoresWaysNotFoundThisRun` | known-only ways aren't used |
| `ScheduleTargetTests.EndRoom_AppendsAtEnd` | the old behaviour is unchanged where the queue ends |
| `ScheduleTargetTests.RoomOnRoute_InsertsAtEndOfThatVisit` | after that visit's actions, before the walk on |
| `ScheduleTargetTests.CurrentRoom_InsertsBeforeWalkingOn` | stop 1 works like any on-route room |
| `ScheduleTargetTests.RoomVisitedTwice_AddsToLastVisit` | the action follows the last visit |
| `ScheduleTargetTests.RoomOffRoute_AddsShortestWalkThenAction` | walk and action are appended in order |
| `ScheduleTargetTests.NoFoundWay_CantWithReason` | unreachable rooms stay greyed |
| `PlacesTests.ScheduleTrip_FarRoom_AddsWholeWalk` | map clicks use the route search |

## Done when
- [x] Console clean; all EditMode tests pass
- [x] In a room on her route, Schedule isn't greyed, the action joins that room's last visit, and that badge glows
- [x] In a room off her route, Schedule adds the walk and then the action; the note says how far
- [x] A room reachable only by a way from a past run stays greyed with a reason
- [x] Clicking a far room on the map adds the whole walk
- [x] The user has played it

## Notes after implementation
- **Added beyond the plan (user request while playtesting):** Play in a room she isn't in goes at the start of that room's visit, or after the walk for an off-route room. Carry stays where she is. Decisions log 2026-09-29 *Play works in any room she can reach*.
- `Simulation.PlayIn` / `ScheduleIn` share a private `AddIn`; `WhereScheduleLands` returns a `ScheduleTarget` (with `VisitStartIndex`). `Enqueue` gained an `insertAt` argument.
- `ActionRow.Refresh` (popover overload) now takes `canCarry` separately.
- Two old `PlacesTests` cases asserted "next door only" and were updated to the new rule.
- The route note is a second line in the popover title text (no separate element); it no longer shows a room count.
- Tooltips were written by Claude, not the writing session (user's call).
