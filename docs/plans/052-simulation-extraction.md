# 052 — Simulation extraction (backlog 16(c), second half)

**Status:** Approved; #1, #2 and #3 done
**Left to do:** #4 `Stats` (Act I playtest done 2026-10-02, plan 055; judge first whether it pays, per the stop rule)
**Design:** none: a code-structure plan. Builds against `docs/CODE-STANDARDS.md` §1 (one reason to change, size signals), backlog item 16(c), `docs/code-health/2026-09-30.md` and `2026-09-28.md`.
**Pillar:** none: tooling.

**Priority: low until the Act I playtests have judged the loop.** Backlog 16 says to wait: refactoring code the design may still change is wasted. Nothing is broken. This plan is a roadmap; each extraction is its own `/refactor` session.

## Goal
`Simulation` (about 5,100 lines over 23 partial files) hands a few self-contained jobs to small classes that own their own state and have an enumerated surface, so a change in one can't reach into another's private fields. No behaviour changes; the player sees nothing different.

## Out of scope
- Splitting the queue engine (see *Not worth extracting*), and any further split of `Simulation.cs`.
- Removing the six events with no subscriber outside `Simulation` (`RoomExplored`, `AttributeLevelledUp`, `AttributeMasteryGained`, `SkillLevelledUp`, `SkillMasteryGained`, `RestoreStarted`). That changes the public surface: listed under *Follow-ups*.
- Demoting public methods with no outside callers (the 2026-09-30 audit named about 14). Same reason.
- The remaining size findings that aren't `Simulation` (`SaveSerializer`, `BalanceSheetWindow`, `AttributeTests`).

## Design assumptions
None. No game-design question is settled or depends on this. If a design change lands first (for example the Summary/Plan rework, backlog 1b, or the stats rework, plan 041), re-check the extraction it touches before starting.

## Reuse
- Method used: Grep and a regex scan over `Assets/Scripts/Core/Simulation*.cs` (LSP was not used), so counts are text matches and approximate. Step 1 of each session re-checks with LSP find-references before moving anything.
- `RunReportTests` (613 lines), `PlanWarningTests`/`ByHeartTests`, `AttributeTests` and the other existing suites are the proof. No assertion changes.
- `SimulationTestBase`: uses only `Tick`, `Loop`, `Phase`, `BeginLoop`, `Schedule`, `QueueStops`, `OffersAt`, the events `TaskSkipped`/`ActionRefused`, and the 2–4 argument constructor. All stay on `Simulation`.
- `Simulation.RestoreLastRun` (`internal`, `Simulation.Report.cs`) is used by `SaveSerializer.Run.cs`: keep it `internal` on `Simulation`.

## Coupling map (summary)
Every partial reads the shared state in `Simulation.cs` (`Loop`, `Persistent`, `Phase`, `_settings`, `_content`, `_ticksPerSecond`, `_nextQueue`); `Loop` is the dominant coupling. Beyond that:

| Partial (lines) | Owns | Writes | Calls into other partials | Called from other files |
| --- | --- | --- | --- | --- |
| `Simulation.cs` (382) | shared state, 26 events, tick, run begin/end | everything | about 47 (Attributes 13, Report 7, Exploring 6, ByHeart 5) | every file |
| `Attributes` (420) | no fields | `Loop.DrainGrown`, XP | about 10 | about 44 (`Simulation.cs` 13, Report 12) |
| `Resources` (376) | 3 container caches | pockets, `Persistent.Resources` | Floor 8, Unlocks 4 | about 55 |
| `Floor` (273) | 2 verb caches | `Loop.Floor` | Resources 18 | about 29 |
| `Places` (374) | 2 delegates | none (trip scheduling goes via the queue) | Tasks 7, Exploring 3 | about 70 |
| `Exploring` (253) | `_knownWays` | explore progress | Attributes 5, Places 5, Unlocks 3 | about 47 |
| `Offers` (116) | 1 scratch list | none | about 28 reads | 2 |
| `Carrying` (127) | none | `Stash`, `Packed` | about 29 (22 plain state) | 6 |
| `Restoring` (140) | none | `Loop.Restorings`, `DrainHeldOff` | Carrying, Floor, Resources | 3 |
| `Unlocks` (318) | none | 10 lists | Exploring, Resources, Tasks | about 12 files |
| `Report` (198) | `LastRun` | `LastRun` only | 10 read-only helpers | `EndLoop`, `LastRunTimeOf` |
| `PlanWarnings` (316) | 8 cache/scratch fields | none | about 30, 8 partials | 3 |
| `ByHeart` (241) | 2 fields (speed hold) | `RoomRuns`, `_nextQueue` | Places, Unlocks | 3 + `WhyNotPlannable` |
| `Route` (125), `Reorder` (160) | none | queue order (Reorder) | Places, Tasks | about 10 / none |
| `Tasks`, `QueueInRoom`, `Starting`, `CanStart`, `Repeating`, `Supplying`, `Paying` | `_scheduleRooms`, delegate caches | queue, `Loop.Current*` (Starting about 33 members) | each other, cyclically | each other |
| `DevJump` (78) | none | many (dev only) | 6 | none |

Events: most are raised from one partial; `QueueChanged` from 7, `ActionRefused` from 3. Subscribers (UI, `GameController`, tests) subscribe on the `Simulation` instance and re-subscribe on `SimulationChanged`, so **every event stays declared and raised on `Simulation`**.

## Not worth extracting (and why)
- **`Simulation.cs`**: the hub (state, events, tick). Splitting it moves the coupling, not removes it.
- **The queue engine** (`Tasks`, `QueueInRoom`, `Starting`, `CanStart`, `Repeating`, `Supplying`, `Paying`): they call each other in a cycle and `Starting` writes about 33 `Loop` members. Any seam would be all forwarding methods.
- **`Places`, `Route`, `Reorder`, `Exploring`, `Unlocks`**: 70, 47 and about 12-file inbound use, shared writes, and `Places`–`Exploring`–`Unlocks` form one cluster. The seam would cost more than it saves.
- **`Carrying` (127), `Restoring` (140), `Offers` (116), `DevJump` (78)**: already small; no size gain. `DevJump` has no callers, so no coupling to cut.
- **`Resources` + `Floor`**: a two-way cycle (18 and 8 calls each way). Only worth moving as one inventory unit, and only if a feature makes it pay.

## Order and what each extraction is
Smallest and least connected first. Each is its own `/refactor` session, under 6 steps, full EditMode run after every step.

| # | Extract | Owns / needs passed in | Surface | About lines out |
| --- | --- | --- | --- | --- |
| 1 | **`RunReportBuilder`** from `Report`: `MakeRunReport`, `AddTimeSlices`, `AddMilestoneRows`, `RowFor` | `Loop`, `Persistent`, `_content`, `_ticksPerSecond`, and the `Simulation` for 10 read-only helpers: `LevelOf`, `MasteryOf`, `MasteryProgressOf`, `MasteryFor`, `MasteryProgressFor`, `StrengthOf`, `Charges`, `MovesThisRun`, `CanBeReachedAgain`, `ExploredFraction` (any private ones widened to `internal`, each named in the session) | one `Build()` called by `EndLoop`; `LastRun` and `RestoreLastRun` stay on `Simulation` | 150 |
| 2 | **`RoomSpeedHold`** from `ByHeart`: `_speedHeld`, `_heldActionStarted`, `WatchRoomSpeedHold`, `HoldSpeed` | the two fields; subscribes to 5 events via the `Simulation` it is given (same lifetime) | what `GameController` and `PauseControl` read today | 60 |
| 3 | **`PlanWarningsTracker`** from `PlanWarnings` | its 8 cache fields; reads `Queue`, `Loop.CompletedTasks`, `Loop.Floor`, `RunUnderWay`, `_settings`, `_content`; about 30 helper calls on `CanStart`, `Places`, `Exploring`, `Floor`, `Route`, `Unlocks`, `Supplying`, `Paying` (the session lists them, then widens to `internal` only what's needed); subscribes to 7 events on the `Simulation` | `PlanWarnings()` and `MarkPlanWarningsStale()` stay on `Simulation` as one-line forwards (`Carrying` and `Supplying` call them) | 280 |
| 4 | **`Stats`** from `Attributes` (XP, speed, mastery, drain rate and growth) | reads `Loop.AttributeXp/SkillXp`, `Persistent` mastery and `_settings`; writes `Loop.DrainGrown`; the 4 level/mastery events are raised **on `Simulation`** through callbacks passed in, keeping their order | every public `Attributes` member stays on `Simulation` as a one-line forward (UI uses it about 65 times, tests about 110) | 350 |

Honest expectation: about 800 of about 5,100 lines move, and `Simulation` stays one large partial class. The gain is owned state moving out (about 10 private fields) and the read surface of each piece being written down. If 1 and 2 don't make the later code visibly easier to follow, stop there and re-judge 3 and 4.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Scripts/Core/RunReportBuilder.cs` (and one file per later extraction) | New | the extracted class, one public type per file, `///` summary |
| `Simulation.Report.cs`, `.ByHeart.cs`, `.PlanWarnings.cs`, `.Attributes.cs` | Edit | the moved code replaced by a field and forwarding methods |
| `Simulation.cs` file map and `.claude/rules/simulation.md` table | Edit | list each new class (proposed lines, user's OK first) |
| `docs/CODE-STANDARDS.md`, `CHANGELOG.md` | none | no player-visible change; no changelog line |

New content fields: none. Save format change? **No**: `SaveData`, content `Id`s and `CurrentVersion` are untouched.

## Steps (per extraction session; `/refactor`)
1. Re-count the extraction's coupling with LSP find-references; list every private member the new class needs and widen to `internal` only those (name them in the report). Run the full EditMode suite as the baseline.
2. Create the class with the moved code and an explicit constructor (references passed in, never `Find*`, no new static state).
3. Make `Simulation` build it once (in the constructor) and replace the old partial body with a field and one-line forwards; keep every public signature and event as is.
4. `refresh_unity`, `read_console`, full EditMode run: same count passing as the baseline, no test changed.
5. Update the `Simulation.cs` file map and propose the `simulation.md` table line.

## Tests
No new behaviour, so no new tests; the proof is that the existing suite is unchanged and passes after every step.

| Suite | Proves |
| --- | --- |
| `RunReportTests` | step 1 |
| `RoomSpeedTests`, `ByHeartTests`, `RunSaveTests` | step 2 |
| `PlanWarningTests` (and the plan-warning cases in `ByHeartTests`/`SupplyTests`) | step 3 |
| `AttributeTests`, `ExploringTests`, `RunSaveTests` (tick-for-tick resume) | step 4 |
| `TutorialContentTests` | content still loads |

## Risks
- **Save format:** unchanged; `SaveSerializer` reaches `Simulation` only through `Persistent`, `Loop`, `Phase`, `NextLoopNumber`, `Queue`, `LastRun`, the pick-up/put-down verb members, `ResumeRun`, `RestoreNextQueue` and the `internal` `RestoreLastRun`. All stay on `Simulation`. `RunSaveTests` catches any drift.
- **Determinism:** 1 and 3 only read. 2 and 4 must keep the order in which events fire within a tick (`GainXp` → level-up → mastery); check by running `RunSaveTests` and the attribute event tests.
- **Test fixtures:** `SimulationTestBase` and about 48 test files call `new Simulation(settings, ticksPerSecond, content)` and read `Loop`/`Persistent`; that constructor and those members must not change.
- **Events:** `QueueChanged`, `ActionRefused`, `TaskSkipped` and the rest stay declared on `Simulation`; extracted classes raise them through the `Simulation` they were given. UI and `GameController` keep subscribing to `Simulation` and rebinding on `SimulationChanged`.
- **Dead code temptation:** don't remove unsubscribed events or demote public methods in these sessions (public surface; see *Follow-ups*).

## Done when
- [ ] Each extraction: all EditMode tests pass, count unchanged, no test assertion changed; compile and Console clean
- [ ] `Simulation.cs` file map and `simulation.md` table list the new class (user's OK on the rules-file line)
- [ ] Backlog 16(c) updated with what was extracted and what was judged not worth it

## Follow-ups (backlog candidates, not done here)
- Remove the six events with no outside subscriber, or confirm they're wanted for the Summary/stats UI.
- Demote the about 14 public methods with no outside callers (`BaseSecondsOf`, `CopiesThisRun`, `EffectStrengthOf`, `FindWay`, `IsUsable`, `TripModifier`, `InPockets`, `IsPacked`…) from the 2026-09-30 audit.
- `SaveSerializer`, `BalanceSheetWindow` and `AttributeTests` size splits (separate backlog lines already in the 2026-09-30 report).

## Notes after implementation
<!-- filled in at wrap-up: what changed from the plan and why -->
**#1 `RunReportBuilder` — done 2026-10-01** (commits ebf4845, 43f4861, 76a00c2; 886/886 EditMode tests before and after, no test touched).
- The report reads 7 Simulation helpers, not 10: `StrengthOf` and `Charges` aren't used by it, and `LongestRun` (public) is read too. Widened to `internal`: `MasteryFor`, `MasteryProgressFor` (`.Attributes.cs`) and `CanBeReachedAgain` (`.Unlocks.cs`).
- The class is `internal`: only `Simulation` builds it. It reads `sim.Loop`/`sim.Persistent` on each `Build()` rather than keeping them, because each run gets a new `LoopState`.
- `LongestRun` stayed on `Simulation` (the dev panel reads it). `Simulation.Report.cs` went from 198 to 29 lines.

**#2 `RoomSpeedHold` — done 2026-10-01** (commits aca0fe2, 29cc39b, ea5cd64; 892/892 EditMode tests before and after, no assertion changed).
- Corrections to the table: about 25 lines moved, not 60. The readers are `GameController` (`RoomSpeedNow`, via `TickEngine.RoomSpeed`) and `QueueRibbon` (`SpeedHeld`, `RoomSpeedNow`); `PauseControl` reads neither. The proof suite is `RoomSpeedTests` (plus `ByHeartTests`, `RunSaveTests`), not `PauseControlTests`.
- `StartNewLoop` also reset both fields, so the class has a `Reset()` called at that spot. It's built in the constructor where `WatchRoomSpeedHold()` was (before `WatchPlanWarnings`), so event handlers keep their order.
- Two characterisation tests added first (`RoomSpeedTests`): a new run starts unheld, and a resumed run isn't held (the hold isn't saved).
- `RoomSpeedNow` and `SpeedHeld` stayed on `Simulation` (`RoomSpeedNow` reads `RoomSpeed`, `Loop` and `RoomSpeedUnlocked`); the class is `internal`.
- Judgement for the stop rule: worth it but small. The gain is encapsulation (the run start no longer writes another topic's fields), not size.

**#3 `PlanWarningsTracker` — done 2026-10-02** (commits 4036e97, 226a11e, d5abb60, c19f5f8, 158e458; 923 tests (922 pass, 1 explicit skip) before and after; one characterisation test added, no assertion changed).
- Added first: `TheRunEnding_RefreshesTheWarnings` (`PlanWarningTests`), because nothing pinned the run-end refresh whose wiring moved.
- 14 helpers widened to `internal` (access only): `PlanStart`, `NodeAfterEntry`, `CantUseWay`, `IsSearching`, `BarCountsFor`, `IsExploredTo`, `HeldWhenPlanStarts`, `TotalCostOf`, `CantStartForUnknownHue`, `NeedsPoolReason`, `NotHereReason`, `NeedsReason`, `OpensAtRunStart`, and `Gives`. `Gives` stayed on `Simulation` as `internal static` because `Simulation.Supplying.cs` uses it too.
- Steps 3 and 4 of the session went in one commit (a class nothing used would have been dead code). `Simulation.PlanWarnings.cs` went from 316 to 47 lines; `PlanWarningsTracker.cs` is about 305 lines, a third of them doc comments.
- Built in the constructor after `RoomSpeedHold`, so event handler order is unchanged. `MarkPlanWarningsStale` stays on `Simulation`; correction to the table: its callers are `Carrying` (`SetPacked`) and `Simulation.cs` (`RestoreNextQueue`), not `Supplying`. The walk reads `Loop` through the `Simulation` on each call, as `RunReportBuilder` does.
- The `simulation.md` row and the `Simulation.cs` file map list the new class.
