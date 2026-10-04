# 042 — Since last run and best-ever milestone times (Core half)

**Status:** Done 2026-10-02
**Left to do:** nothing (the display is plan ui-044)
**Design:** GDD v0.5 §13a *Benchmarks* (extension 1, "show best-ever alongside last-run"), §14.19; decisions-log 2026-10-01 *Tooltips: the open points settled* ("since last run waits for its own plan"); `docs/mockups/tooltips/design-notes.md` (one kept value per skill); `docs/BACKLOG.md` Next 5 (Core half)
**Pillar:** 3, *Every run teaches something* (a visibly faster chain). Also 4, *The hall remembers* (the kept value persists).

## Goal
Core can answer two questions for the UI to show later (plan ui-044): "how many seconds faster is this action than at the start of last run?" and "what is the best time ever for this milestone?". Nothing changes on screen.

## Out of scope
- Any display: tooltips, the Summary, text keys. That is plan ui-044 (UI lane, after ui-036); its need is recorded as `docs/UI-BACKLOG.md` *Next* 14, added when this plan was written, so it isn't lost.
- Changing the Summary's milestone rows (`MilestoneRow`, `RunReport`); the display plan reads the new helper instead.
- Level speed (a skill's in-run level): only mastery is compared (the user, 2026-10-01).
- Per-stat or per-room-speed deltas; a sparkline across runs (§13a, late game).

## Design assumptions
- **"Since last run" means mastery only:** this run's starting mastery speed against last run's starting mastery speed, so the number is steady during a run (the user, 2026-10-01).
- **Best-ever counts every run**, including collapsed, ended-early and dev-restarted ones: a time she reached is a real time (the user, 2026-10-01). This differs from *Longest run*, which skips early-ended runs because it measures length.
- **Deviation from the design notes, for the user to OK:** the kept value is the skill's *mastery XP* at the start of the last run (the same number `LoopState.SkillMasteryAtStart` already holds), not its speed multiplier. Speed is worked out from it on demand, so retuning `skillMasterySpeedPerLevel` in the Balance Sheet never shows a false change. Still one kept float per skill.
- Existing saves have no kept value: a skill shows no change until a run has ended after the upgrade.
- Whether a "since last run" figure of exactly zero is hidden is a display rule (ui-044); Core returns the exact figure, or nothing when no value is kept.

## Reuse
- `PersistentState.SkillMasteryXp` / `SkillMasteryXpOf` (`Core/PersistentState.cs`): the pattern for a per-skill kept dictionary.
- `LoopState.SkillMasteryAtStart`, filled by `Simulation.NoteWhatTheRunStartsWith()` (`Simulation.cs`): already the start-of-run snapshot, and already saved with a run under way.
- `Simulation.EndLoop(LoopEndReason)` (`Simulation.cs`): the one place a run ends; plus the dev-restart branch in `StartNewLoop`, which adds an `EndedByPlayer` record and skips `EndLoop`.
- `SpeedMultiplierFor(TaskDefinition)`, `MasterySpeedFor`, `LoopSettings.MasterySpeedAt` (`Simulation.Attributes.cs`, `LoopSettings.cs`): how a task's speed comes from its skills.
- `RunRecord.TimeOf(ContentAsset)` and `Simulation.LongestRun` (`Simulation.Report.cs`): the pattern for a best-over-history helper.
- `SaveSerializer.Upgrades.cs`: inline `if (data.version < N)` upgrade steps; latest is 22 → 23.
- Tests: `SaveTests`, `AttributeTests`, `MilestoneTests`, `SimulationTestBase`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `PersistentState.cs` | Edit | `SkillMasteryXpAtLastRunStart` dictionary + `…Of(skill)` returning `float?` |
| `Simulation.cs` | Edit | at the end of a run (one shared helper used by `EndLoop` and the dev-restart branch) copy the run's `SkillMasteryAtStart` into the kept dictionary |
| `Simulation.Attributes.cs` (or a new small partial if it nears 300 lines) | Edit | `float? SecondsFasterSinceLastRun(TaskDefinition)`: base time ÷ mastery speed now minus base time ÷ mastery speed at last run's start, using the task's governing skills; null if no kept value |
| `Simulation.Report.cs` | Edit | `float? BestTimeOf(ContentAsset milestone)`: lowest `RunRecord.TimeOf` over `RunHistory`, every run counts; null if never reached |
| `SaveData.cs`, `SaveSerializer.cs`, `SaveSerializer.Upgrades.cs` | Edit | new list `skillMasteryXpAtLastRunStart`; version 23 → 24; upgrade step leaves it empty |

New content fields: none (no balance numbers).

Save format change? **Yes** → bump `SaveData.CurrentVersion` 23 → 24; upgrade step: empty list (no comparison until a run ends).

## Steps
1. Write failing EditMode tests (below).
2. Add the kept dictionary to `PersistentState`, and its save/load by skill `Id` (save version 24, upgrade step, round-trip test).
3. Copy the run's start snapshot into it when a run ends, from `EndLoop` and the dev-restart branch via one helper.
4. `SecondsFasterSinceLastRun(TaskDefinition)`.
5. `BestTimeOf(ContentAsset)`.
6. Full EditMode run; fix the cause of any failure, never weaken a test.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `SaveTests.Upgrade23To24_LoadsWithNoKeptValues` | an old save loads, version 24, no kept values |
| `SaveTests.KeptSkillValues_RoundTrip` | save then load keeps the values by skill `Id` |
| `AttributeTests.EndingARun_KeepsItsStartingMastery` | after any run end (walk out, collapse, ended early) the kept value is that run's start snapshot |
| `AttributeTests.ADevRestart_KeepsTheCutRunsStart` | the restart branch that skips `EndLoop` also keeps it |
| `AttributeTests.SinceLastRun_IsNull_BeforeAnyRunEnded` | no kept value → null, not zero |
| `AttributeTests.SinceLastRun_ShowsTheMasteryGain` | a skill that gained mastery last run makes its tasks faster by the expected seconds; mid-run level gains don't change it |
| `AttributeTests.SinceLastRun_IsZero_WhenMasteryDidNotMove` | exact zero, not null |
| `MilestoneTests.BestTime_IsTheLowestOverEveryRun` | lowest of several runs, including a collapsed and an ended-early one |
| `MilestoneTests.BestTime_IsNull_ForAMilestoneNeverReached` | null |

## Done when
- [x] Tests above pass; whole EditMode suite green (932 run: 931 passed, 1 explicit probe skipped); compile and Console clean
- [x] In Unity (Features editor): nothing looks different; older save loads (the user OK'd the play check, 2026-10-02)
- [x] decisions-log updated (the mastery-XP deviation, mastery-only, every run counts); GDD §13a *Benchmarks* line "not yet for milestone times" edit proposed once ui-044 shows it; changelog line only when ui-044 makes it visible

## Notes after implementation
Differs from the plan:
- **A skill with no entry counts as 0 once a run has ended** (the user, 2026-10-02), so a new flag `PersistentState.HasLastRunStart` (saved as `hasLastRunStart`) separates "no run has ended yet" (null) from "ended, skill had none" (0). The plan's single list stayed, plus that one field.
- **`SecondsFasterSinceLastRun(task, room)`** takes the room, because a task's base time depends on the room's depth.
- **New file `Simulation.SinceLastRun.cs`** holds the helper and `KeepThisRunsStart`, since `Simulation.Attributes.cs` was already 420 lines (the plan allowed it).
- `SaveTests.Upgrade22To23_LoadsWithoutTheCapBonus` now checks the current version instead of exactly 23 (every save upgrades to 24 now); what it proves is unchanged.
- `BestTime_IsTheLowestOverEveryRun` builds run records directly rather than playing a collapse and an early end.

