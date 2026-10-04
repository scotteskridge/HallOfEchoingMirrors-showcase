# 020 — Explore once, kept between runs

**Status:** Done (2026-09-30)
**Design:** GDD v0.5 §9 (supersedes *[BUILT] Rooms are searched afresh every run* and its [OPEN] hidden-ways placeholder), decisions-log 2026-09-29 *Exploring a room is done once, ever*. Backlog Next 12 (part A; part B is plan 021).

## Goal
Searching a room is one long action whose bar is kept for good, including partway. What it finds (actions, hidden ways) stays found in every later run, so on run 2 she walks straight through rooms she has explored.

## Out of scope
- Room-entry benchmarks and first-entry story passages: **plan 021**.
- What Perception really does: the stats pass (backlog Next 13). This plan keeps a placeholder.
- Tuning explore times: the balance pass.
- Any "hall scrambled again" story event that resets rooms.

## Design assumptions
- **The player-facing verb stays "Search"** (GDD term; existing text keys). Code keeps "Explore".
- **Placeholder rule:** Perception makes the bar fill faster. Progress per tick = `SearchYield` ÷ ticks per step, where `SearchYield` is today's 1 + 0.1 × Perception. At Perception 0, the full search takes exactly as long as today's full set of passes.
- **Steps stay the unit.** `exploresToFill` still counts steps. Each whole step crossed gives `eachExploreGives` (e.g. Mirrors Found) and the XP of one old pass, so XP and rewards are unchanged. A run that ends mid-step keeps the bar but not that step's XP.
- **One queue entry, no repeat.** Search runs until the room is full. Play or pushing it down simply stops, because progress lives in kept state, not in `QueueEntry.SavedWork`.
- **Save upgrade:** progress saved in a run in progress is merged in (the larger value wins). A room with any of its hidden ways already in `FoundWays` counts as fully searched.
- **The Hall Has Shifted is unwired from `GameContent`.** Its asset and story `.txt` file stay (the user's prose).

## Reuse
- `Simulation.Exploring.cs`: `FinishExploring`, `FindWaysNowSeen`, `IsExploredTo`, `LiveExploredFraction`. They switch from per-run to kept progress.
- `Simulation.Places.cs` `IsFound` / `IsUsable`: route search (`RouteSearch`, `ScheduleTarget`) follows automatically.
- `Simulation.Unlocks.cs` `CanBeReachedAgain`: add the `RoomExplored` trigger as "not again".
- `SaveSerializer.FromJson` upgrade chain and the `Restore` content-aware step (the v4→5 pattern).
- `BalanceSheetWindow.DrawPlaces`: a read-only "full search (s)" column.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `PersistentState.cs` | Edit | `Explored` (room → steps, kept). `HasSearched` removed. |
| `LoopState.cs` | Edit | `ExploresDone` removed. Keep `ExploredAtStart` (a snapshot) for the report. |
| `Simulation.Exploring.cs` | Edit | Progress applied every tick while searching. Step crossings grant rewards and XP, then check finds, ways and switches. Header comment rewritten. |
| `Simulation.Tasks.cs` | Edit | Search duration = remaining steps ÷ `SearchYield` × step time. `IsDone` when full. No per-pass `FinishExploring` call. |
| `Simulation.Places.cs` | Edit | `IsFound` reads kept progress. The `way_not_found_this_run` branch is removed. |
| `Simulation.Unlocks.cs` | Edit | `RoomExplored` reads kept progress and never fires again. |
| `Simulation.cs`, `GameContent.cs/.asset` | Edit | Remove the hall-shift beat and `hallShiftsStory`. |
| `Simulation.Report.cs`, `RunReport.cs` | Edit | "Explored" = rooms whose bar rose this run. |
| `SaveData.cs`, `SaveSerializer.cs` | Edit | v14: `explored` kept. `SavedRun.explored` and `hasSearched` become read-only legacy fields. |
| `RoomPopover.cs`, `MapView.cs`, `ActionText.cs` | Edit | The bar reads kept progress. The action shows remaining time, with no repeat marker. |
| `game_text.txt` | Edit | Reword `popover.tip.searched`, `reasons.fully_explored` and `actions.repeat.explore`. Remove `reasons.way_not_found_this_run`. |
| `BalanceSheetWindow.cs` | Edit | Read-only "Full search (s)" column in Places. |
| `PROJECT_NOTES.md`, `.claude/rules/simulation.md` | Edit | Swap the "hidden ways re-found every run" placeholder for the Perception one. The rules-file line is proposed to the user, not edited. |

New content fields: none (the explore time is `exploresToFill` × step time, both existing).

Save format change? **Yes** → `SaveData.CurrentVersion` 14. Upgrade: merge the in-progress run's `explored` into kept progress. In `Restore`, rooms with a hidden way in `FoundWays` count as full. `hasSearched` is dropped.

## Steps
1. Failing tests: progress kept across `BeginLoop`, including partway; finds and ways usable next run without searching.
2. Move progress to `PersistentState`, apply it per tick, grant step rewards and XP. Make Places and Unlocks read it.
3. Failing test, then fix: a `RoomExplored` switch milestone is not reached again next run.
4. Remove the hall-shift story, `HasSearched` and the "search again" reason. Update or retire their tests (see below).
5. Save v14 and the upgrade step, with tests for a v13 save (mid-run, and with found ways).
6. UI: bar, popover, action text and text keys. Add the Balance Sheet column.
7. Docs: the PROJECT_NOTES placeholder, the proposed rules-file line, and the backlog.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ExploringTests.Progress_IsKept_IntoNextRun` | The bar survives a new run |
| `ExploringTests.PartialProgress_IsKept_WhenRunEndsMidSearch` | Partway progress isn't lost |
| `ExploringTests.Finds_AppearMidAction_AtTheirPercent` | Along-the-bar reveals within one action |
| `ExploringTests.Perception_ShortensSearch` | Placeholder rate |
| `ExploringTests.StepRewards_AndXp_OncePerStep` | Mirrors Found and XP unchanged from passes |
| `WaysFoundTests.HiddenWay_UsableNextRun_WithoutSearching` | Replaces the `way_not_found_this_run` test |
| `MilestoneTests.RoomExploredSwitch_NotReachedAgain` | Fires once, ever |
| `SaveTests.V13Save_Upgrades_KeptExploreProgress` | Upgrade merges and fills rooms with found ways |
| `RunSaveTests` (existing) | Mid-run save/load still round-trips |

Existing tests to change, not weaken: `ExploringTests.TheHallShifting_IsToldOnce…` and `SaveTests.SavesReach_TheHallShiftStory_AndFinds` test a removed rule. **The user approved retiring them (2026-09-29).** Keep the "finds" half of the save test as a kept-progress round-trip. `OffersTests:147` and `TutorialContentTests:139` are rewritten to kept progress.

## Done when
- [x] Tests above pass; compile and Console clean.
- [x] In Unity, new save: search The Smoky Mirror; the bar fills in one action and the wisp appears at a third. Let vitality run out mid-search in A Dark Hall; the next run, its bar is where it was. Walk into an explored room: its actions and ways are there without searching.
- [x] Old save loads with explored rooms full.
- [x] decisions-log already records the rule; rules-file line proposed.

## Notes after implementation
- Built as planned: 534 EditMode tests pass. `LiveExploredFraction` is gone (the bar is filled every tick, so `ExploredFraction` is always live). The Balance Sheet column is "Full search (s)" beside a renamed "Steps to fill".
- A search's price (`PriceOf`) and length (`SecondsAtSpeedNow`) are now for the steps still to fill, so the queue and tooltips show the time left, not one step. A pushed-down search keeps no `SavedWork`: it resumes from the room's bar.
- Save v14 adds `exploredRooms` (kept bar) and `SavedRun.exploredAtStart`. `SaveData.loadedVersion` (not saved) lets `Restore` fill rooms with found ways only for pre-14 files.
- Old saves whose journal holds "The Hall Has Shifted" will log one "story beat no longer exists" load warning: that asset is no longer listed in GameContent.
- Finds that need Perception (`needsPerception`) are still checked against her Perception now (per run), not remembered: a question for the stats pass.
