# 030a — Cost curve: families, room depth, duration and XP multipliers

**Status:** Done (2026-09-30; the playtest check passed)
**Design:** decisions-log 2026-09-30 *A cost curve that new verbs default to*; Balancing Formulas doc v0.2 §2 (anchor, room depth), §4 (family table, default curve), §5 (XP); GDD §12 *Tier is baked*. Part 1 of 2; the escalating charge is `030b-escalating-charge.md`.

## Goal
Every task's time comes from one curve, `family multiplier × standard trip × roomStep^depth × the task's own duration multiplier`, instead of a typed-in number. A new verb only needs a family and a room to get a sensible time and XP. Nothing changes in play: the migration sets each task's multiplier so its time is today's.

## Out of scope
- The escalating charge (travel, training verbs): plan 030b.
- Moving multipliers toward ×1 (the balance pass), the lab's new numbers (plans 027a–c take theirs from this curve).
- XP rounding (`ceil` → `round`, formulas doc bad number 3); the `xpReward` override stays as it is.
- Realm tiers (`tierMult`): Act I has none.
- Explore's Always Charged 5 vitality a step (flagged in the formulas doc §13).

## Design assumptions
- **Placeholder rule:** `roomStep` 1.1 until a playtest reading replaces it; `standardTripSeconds` 15 (today's Travel).
- **Placeholder rule:** Gather's family multiplier 0.2 (formulas doc §8: gather ratio 1.2 × a wisp's 2.5 s restore ÷ 15). Handle (pick up, put down) 0.1: the doc has no such family.
- Depths: Smoky Mirror 0, A Dark Hall 1, both corridors 2, Hanging Mirrors 3, the lab and the Other Laboratory 4 (the user, 2026-09-30).
- **A task's depth is the room she does it in**, fixed on that room (never from the live map). Travel uses the room she leaves. No room (a game without rooms) = depth 0.
- **XP follows the final time** (the user, 2026-09-30): `ceil(xpPerSecondOfTask × baseSeconds × xpMultiplier)`, so a ×2 duration pays ×2 XP and the XP multiplier changes XP per second.
- The migration keeps today's time **in the task's shallowest room**. Shared tasks get slightly longer deeper in the hall, as the curve intends: a wisp in the right corridor 1.21 s (was 1), Travel from the lab 22 s (was 15), a lab Search step ×1.46. **This is the one change in play.**
- A task with no family is an impossible state: fail loudly (Console error naming the task; the task can't start).

## Reuse
- `TaskDefinition` (`Scripts/Core/TaskDefinition.cs`): gains the fields; `durationSeconds` is removed after migration.
- `Simulation.PriceOf` / `WorkNeededFor` (`Simulation.Tasks.cs`), explore pricing (`Simulation.Exploring.cs`), `XpRewardOf` (`Simulation.Attributes.cs:190`): the only readers of `durationSeconds`; they get the room.
- `HeldModifier` and the room's `ActionModifier` multiply on top, unchanged.
- `ContentAsset` for the new family asset; `NodeDefinition` for depth.
- `BalanceSheetWindow` (`DrawRules` shows new `LoopSettings` fields by itself; `DrawTasks`/`DrawPlaces` need columns).
- `SimulationTestBase.MakeTask`: keeps its seconds argument by making a test family (×1) and setting the multiplier, so existing tests keep their times.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Scripts/Core/CostFamily.cs` | New | `CostFamily : ContentAsset`, `durationCoefficient` (tooltip says what it multiplies) |
| `Scripts/Core/CostCurve.cs` | New | pure maths: `BaseSeconds(coeff, tripSeconds, roomStep, depth, multiplier)` and `MultiplierKeeping(seconds, ...)` for the migration |
| `LoopSettings.cs` | Edit | `standardTripSeconds` 15, `roomStep` 1.1 (placeholder comments) |
| `NodeDefinition.cs` | Edit | `depth` (int, Min 0) |
| `TaskDefinition.cs` | Edit | `family`, `durationMultiplier` 1, `xpMultiplier` 1; remove `durationSeconds` in step 5 |
| `Simulation.Tasks.cs`, `.Exploring.cs`, `.Attributes.cs` | Edit | one `BaseSecondsOf(task, room)`; price, work and XP use it |
| `Data/CostFamilies/*.asset` | New | Traverse 1.0, Explore 0.3, Search 0.7, Study 0.8, Gather 0.2, Handle 0.1, Instantiate small 0.5, Instantiate large 1.5, Work 1.0, Sacrifice 1.0, Take 0.4, Correct a memory 1.2 |
| `Editor/CostCurveSetup.cs` | New | *Hall of Echoing Mirrors → Setup → Put Tasks on the Cost Curve*: creates families, sets room depths, gives each task its family (table below) and the multiplier that keeps today's time; safe to run twice |
| `BalanceSheetWindow.cs` | Edit | Tasks: Family, ×Time, ×XP, and a read-only "Seconds (in its room)"; Places: Depth; new Families section |
| `GameTimeRescale*.cs` + its tests | Done | the one-shot rescale tool reads `durationSeconds`; already deleted (with `LoopSettings.gameTimeRescaled`) on 2026-09-30 at the user's OK, so nothing to do here |

Family per task: Chase Roland, Travel → Traverse · Search (explore) → Explore · Search the laboratory, the two unplaced corridor searches → Search · Study the tome, Watch him → Study · Gather a wisp, Fill a phial → Gather · Pick up, Put down → Handle · candle → Instantiate small · flint, satchel, empty phial, ring, Cut the stone → Instantiate large · Light a candle (both), Clear the bench, Attend, Draw on the mana stone → Work · Feed your hours → Sacrifice · Take the tome → Take.

Save format change? No (families and depth are content, not saved).

## Steps
1. Failing EditMode tests for `CostCurve` and for `BaseSecondsOf` (table below).
2. `CostFamily`, `CostCurve`, the new fields; `BaseSecondsOf` used by price, work and XP. `durationSeconds` still exists (read only by the migration). Update `MakeTask`.
3. `CostCurveSetup` menu item; run it; check in the Balance Sheet that every task's seconds match today's in its shallowest room.
4. Balance Sheet columns and Families section.
5. Remove `durationSeconds` (the rescale tool is already gone); update `ShippedRulesTests`.
6. Playtest check: a run plays as before; the deeper shared tasks are a little longer.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `CostCurveTests.BaseSeconds_IsCoefficientTimesTripTimesStepToDepth` | the formula |
| `CostCurveTests.MultiplierKeeping_ReturnsTodaysSeconds` | the migration keeps times |
| `CostCurveTests.DurationMultiplier_ScalesTimeAndXpTogether` | XP follows final time |
| `CostCurveTests.XpMultiplier_ChangesXpNotTime` | XP per second |
| `CostCurveTests.SharedTask_UsesDepthOfRoomWhereDone` | depth by room, Travel by the room left |
| `CostCurveTests.NoRooms_UsesDepthZero` | games without rooms |
| `CostCurveTests.TaskWithoutFamily_FailsLoudly` | impossible state |
| `ShippedRulesTests.EveryTaskHasAFamily_EveryRoomADepth` | shipped content |

## Done when
- [ ] Tests above pass; compile and Console clean
- [ ] In Unity: run the setup menu item; Balance Sheet shows Family, ×Time, ×XP, Seconds and Depth; changing `roomStep` changes every deeper task's seconds
- [ ] Play a run: same times as before in the first rooms
- [ ] `PROJECT_NOTES.md` placeholder list gets `roomStep`, Gather 0.2, Handle 0.1

## Notes after implementation
- Built on 2026-09-30; 622 EditMode tests pass (8 new in `CostCurveTests`, 1 in `ShippedRulesTests`). Step 6 (the playtest check) is the user's.
- `XpRewardOf` now takes the room (`XpRewardOf(task, room)`); `BaseSecondsOf(task, room)` is in `Simulation.Tasks.cs`. A task with no family logs one Console error and is refused ("not finished yet", `reasons.no_cost_family`).
- The migration ran from a one-shot menu item and was deleted with `durationSeconds`. Search the laboratory and both corridor searches are in no room's list (assets only), so they were kept at depth 0.
- Room depths set: A Dark Hall 1, both corridors 2, Hanging Mirrors 3, the lab 4 (the Smoky Mirror stays 0). Families are in `Assets/Data/CostFamilies/`.
