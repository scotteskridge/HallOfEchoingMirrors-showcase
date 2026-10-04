# 003 — Room popovers (UI rework plan 2)

**Status:** Done (2026-09-28)
**Design:** GDD v0.5 §13a *How it works* ("clicking a node opens its popover … everything about this place"), *Floor piles are map data*; decisions-log 2026-09-27 *UI rework: the mockup and plan 1's layout* (the popover follows `DesignNotes/mockups/Node popover@1x.png`), 2026-09-28 *Room popovers: the four choices* and *Plan 002 as built* (no layout polish); `docs/plans/ui-rework-roadmap.md` Part 1 row 2

## Goal
Clicking a room on the map opens one popover beside it: the room's name and search bar, its actions (Play, Schedule, Carry; unavailable ones greyed with the reason), its floor pile with Pick up / Put down, and the ways on. It replaces the Actions list, which is hidden.

## Out of scope
- Visits and "by heart" in the header (need visit counts: backlog *Later*).
- Moving the adjacency rule into Core (backlog *Next* 1); the popover uses `DestinationsFrom(PlannedEndNode)` as the map does now.
- Floor dots on the map (plan 5), the ribbon and queue drawer (plan 3).
- Closing with Escape (backlog, with the drawer's); layout polish and proportions (decisions-log 2026-09-28).
- Deleting `ActionList` and its prefab: at wrap-up, once confirmed unused.

## Design assumptions
- **Click** (your choice): as today, clicking a room next to where the queue ends queues the trip; every click also opens that room's popover. Clicking a *way on* chip does the same as clicking that room on the map. So the mockup's separate "Add stop to route" button isn't needed.
- **Greyed, with a reason** (your choice): done for this run; one of a kind already held this run (`reasons.already_have`); a need missing (existing `reasons.needs*`); room fully searched (`reasons.fully_explored`). **Still hidden:** actions not yet found by searching, those locked behind a switch, and kept rewards already owned (*Feed your hours*). Needs are judged against what she holds now plus that room's floor (as `TasksAt` does today).
- **Play** is only live for the room she's in now (as the Actions list does, tip `actions.play_elsewhere`). **Schedule** is only live for the room where the queue ends; elsewhere it's greyed with "add a stop here first". This keeps today's behaviour (the list only offered the end room's actions); nothing new is refused by Core.
- **Floor:** per-item chips ("6 candles · 6 of 15"), since floor space is per item kind, not a total; the Pick up / Put down rows sit under *On the floor*, not *Here*.
- **Times:** base and actual seconds, as `ActionText.Details` works them out.
- **Position** (your choice): beside the room, flipped to the other side if it wouldn't fit, kept inside the map; follows the room when the map pans. Closed by its × button or a click on empty map; opening another room switches.
- The map's "Click a place…" info text is retired (the popover says all of it).

## Reuse
- `TemplateList<T>`, `UiStyle`, `UiText`, `ToolTip`, `TextKey`, `ActionText` (`NameOf`, `Details`, `Tip`, `RepeatRule`).
- `ActionRow` (+ prefab): extended with a greyed state and a Schedule `interactable`, not replaced.
- Core: `TasksAt`, `IsAvailableAt`, `IsDoneForThisRun`, `MissingNeed`, `IsFoundHere`, `IsFullyExplored`, `ExploresDoneIn`, `LiveExploredFraction`, `FloorAt`, `FloorSpace`, `PickUpActionFor`/`PutDownActionFor`, `DestinationsFrom`, `PriceOf(TravelVerb…)`, `PlannedEndNode`, `PlannedNodeAfter(0)`; queue calls `PlayNow`, `Schedule`, `CarryNow`, `ScheduleTrip`.
- `MapStyle.ExploreColour` for the search bar; `MapView.ZoomAfterScroll`/`MapZoomTests` as the pattern for testable static UI maths.
- `EditorUiFactory` (`MakeFrame`, `MakeText`, `MakeButton`, `MakeDisplayBar`, `SetTip`, `SetTextKey`, `Wire`).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `UI/MapLines.cs` | New | Step 0: `Place(rect, a, b, thickness)`, the one copy of the line maths |
| `Editor/MapLayoutEditing.cs` | Edit | Step 0: uses `MapLines.Place` |
| `UI/MapRoom.cs` | New | Step 0: a room on the map (Image, Label, search bar); replaces MapView's parallel lists |
| `UI/MapView.cs` | Edit | Step 0: `TemplateList<MapRoom>`; one `SimulationChanged` handler replaces `_exploredShownFor`/`_cameraFor`/`_refreshedFor`. Then: click opens the popover; `Describe`/`_infoText` removed |
| `Core/Simulation.Places.cs` (or `.Tasks.cs`) | Edit | `OffersAt(room, into)`: each action shown at a room, with a reason when it can't be done (null if it can) |
| `UI/PopoverPlacement.cs` | New | Static: where the popover goes beside a room, inside the map |
| `UI/RoomPopover.cs` | New | The popover: header, *Here*, *On the floor*, *Ways on*; rebuilds when its offers change |
| `UI/ActionRow.cs` | Edit | Greyed state with reason; Schedule `interactable` |
| `UI/UiStyle.cs` | Edit | `PopoverBackground`, `RowUnavailable`, `Chip` |
| `Editor/GreyboxSetup.cs` | Edit | Step 78 (below) |
| `game_text.txt` | Edit | `popover.*` (headings, `searched`, chips, tips), `reasons.already_have`, `reasons.done_this_run`, `actions.schedule_elsewhere` |
| `SampleScene.unity`, `MapPanel` prefab | Edit | by Step 78 only |

New content fields: none. Save format change? No.

## Steps
1. **Commit the waiting changes first**, on their own: "Remove the x5 speed until the speed rework" (RunHeader, rules files, docs, scene).
2. **Step 0a:** failing `MapLinesTests`, then `MapLines`; MapView and MapLayoutEditing use it.
3. **Step 0b:** `MapRoom` and MapView's single `SimulationChanged` handler; all tests green, the map still draws, follows Clara and shows search bars after loading a save.
4. Failing `OffersTests`, then `Simulation.OffersAt`.
5. Failing `PopoverPlacementTests`, then `PopoverPlacement`.
6. `RoomPopover`, `ActionRow`'s greyed state, MapView opening the popover.
7. **Setup Step 78: Room popover.** Adds `MapRoom` to the map's room template (prefab); builds the popover inside the map panel; wires it; deactivates the drawer's Actions tab and page and the map's info text (nothing deleted); tooltips and TextKeys. Safe to run twice, undoable, logs each change.
8. You run Step 78 and play a run; then the step is retired and `ActionList` removed at wrap-up.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `MapLinesTests.Place_CentresRotatesAndSizes` | midpoint, length and angle for a diagonal line |
| `OffersTests.Available_HasNoReason` | a doable action is offered with no reason |
| `OffersTests.DoneThisRun_GreyedWithReason` / `OneOfAKindHeld_…` / `MissingNeed_…` / `FullySearched_…` | each greyed case and its reason |
| `OffersTests.NotFound_Hidden` / `Locked_Hidden` / `KeptRewardOwned_Hidden` | nothing is spoiled |
| `OffersTests.OtherRoom_ListsThatRoomsActions` | works for a room she isn't in |
| `PopoverPlacementTests.FitsRight_GoesRight` / `NoRoomRight_FlipsLeft` / `NearTopOrBottom_StaysInside` | placement |

## Done when
- [x] Tests above pass, all EditMode tests pass; compile and Console clean
- [x] In Unity: run Step 78, press Play. Click A Dark Hall: its popover opens beside it with the search bar, actions with base/actual times, greyed ones with reasons, the floor and the ways on. Play and Schedule work; clicking a neighbouring room queues the trip and opens its popover; clicking a way chip does the same; × or empty map closes it. The drawer no longer has an Actions tab.
- [x] Roadmap row 2 ticked; decisions-log entry added (2026-09-28 *Room popovers: the four choices*); `ui.md` line proposed for `RoomPopover` and `MapRoom`; BUILD-STATE synced

## Notes after implementation
<!-- filled in at wrap-up: what changed from the plan and why -->
- **A missing need shows its reason but isn't greyed** (`ActionOffer.Blocked` false): the room's own actions were always listed with a missing need, because the queue can supply it or she may pick it up on the way. Greying them would stop *Light a candle* being scheduled before a candle exists. Only done this run, one of a kind held and fully searched are greyed.
- **`MapRoom` is added to the room template at runtime** (MapView.Start), so the MapPanel prefab isn't touched; Step 78 doesn't add it.
- **`OffersAt` and `TasksAt` live in a new `Simulation.Offers.cs`** (Places.cs would have passed 300 lines); `TasksAt` is now "the offers that aren't blocked", so the two can't drift apart. The anywhere-tasks' need check now counts the listed room's floor, not the current room's.
- Step 78's reusable builders went into `EditorUiFactory`: `AddStack`, `FixHeight`, `MakeChipRow`, `MakeChip`, `SwitchOff`.
- **Carry follows Play**, not Schedule (it starts the action now, on top of the queue): found in review.
- A row missing a need shows its times *and* the reason; a blocked row shows only the reason.
- Floor chips read "6 Candle · 15 fit" (the per-item floor space), not "6 of 15".
- Loading a game closes the popover (its rows were built from the old game). The search bar got a tooltip.
- The map's `## map` text section was removed with `Describe`. **`ActionList` is not deleted yet:** its component still sits on the switched-off Actions page, and deleting the script would leave a missing-script component in the scene (backlog).
- Fixed after the first play: a `NullReferenceException` in `ActionRow.Refresh`: rows inside a switched-off section hadn't run `Awake`. `ActionRow` now finds its CanvasGroup when first needed.
- Extra test beyond the plan: `OffersTests.AnywhereTask_ListedOnlyWhereItsNeedLiesOnTheFloor`.
