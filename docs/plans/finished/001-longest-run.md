# 001 — Longest run on the Summary page, and a run history

**Status:** Done
**Design:** GDD v0.4 §13a *The run timer shows game time* and *Benchmarks* ("Show best-ever alongside last-run"; "A sparkline, late game"), decisions-log 2026-09-27 *Run length: last and longest*

## Goal
The Summary page compares this run's length (game time) with the last run **and the longest run ever**, and marks a new record. Every finished run is recorded in the save, so a future sparkline has data from today.

## Out of scope
- The Summary page rework (layout, look, what it shows): a separate task, noted in `PROJECT_NOTES.md`. This plan adds one column to the existing text and nothing else.
- Any chart or sparkline, averages, or a history list on screen.
- Best-ever times for milestones (the other half of the §13a *Benchmarks* suggestion).

## Design assumptions
- **Every run goes in the history**: the prologue, runs ended with *End run*, and runs where she walks out. **Runs ended with *End run* don't count for Longest** (changed during implementation at the user's request; see the decisions log).
- **"Longest yet" only when beating an earlier run**, strictly: not on the very first run, not on a tie.
- **"Longest" shows the record *before* this run**, so the player sees what she beat.
- **Old saves start with an empty history** (they hold only a total of game time, not per-run lengths): the first Summary after the upgrade shows "none" under Longest.
- Each record holds loop number, length in ticks (like `ticksPlayed`), actions completed and how the run ended. Enough for a first sparkline; more fields can be added with a later save version.
- Placeholder layout: third column at about 80% across; the Actions row shows a dash there. Will be replaced by the rework.

## Reuse
- `RunReport` / `Simulation.MakeRunReport` (`Core/RunReport.cs`, `Core/Simulation.Report.cs`): the report the Summary page reads; gains the last and longest earlier runs.
- `Simulation.EndLoop` (`Core/Simulation.cs`): where the run is added to the history. *(`LastRunMilestones`, alongside it at the time, was removed by BACKLOG item 13 once the history covered the same data.)*
- `PersistentState` (`Core/PersistentState.cs`): kept state; holds the history.
- `SaveData` / `SaveSerializer` (`Core/Saving/`): save and upgrade, following the existing `lastRunMilestones` pattern. *(That field is gone too — see above.)*
- `RunResultsPanel.AppendComparison` (`UI/RunResultsPanel.cs`): the "This run | Last run" table; `UiText.Clock`, `UiStyle.Aside`, `Difference` for formatting.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/RunRecord.cs` | New | One finished run: loop number, ticks, actions completed, how it ended |
| `Core/PersistentState.cs` | Edit | `RunHistory` list, oldest first |
| `Core/Simulation.cs` | Edit | `EndLoop` makes the report, then adds the run to the history |
| `Core/RunReport.cs`, `Simulation.Report.cs` | Edit | `RunBefore` and `Longest` earlier records (or null), `IsNewLongest`; `Simulation.LongestRun` |
| `Core/Saving/SaveData.cs`, `SaveSerializer.cs` | Edit | `runHistory` list; version 9 |
| `UI/RunResultsPanel.cs` | Edit | Third column; "last run" read from the report, so it survives a load |
| `Text/game_text.txt` | Edit | new keys: `results.longest` (Longest), `results.new_longest` (Longest yet) |

New content fields: none (no balance numbers).

Save format change? **Yes** → `SaveData.CurrentVersion` 8 → 9; upgrade step: saves before 9 get an empty `runHistory` (nothing to recover).

`Simulation.PreviousRun` is then unused by the Summary page; remove it if nothing else reads it.

## Steps
1. Failing EditMode tests (below).
2. `RunRecord`, `PersistentState.RunHistory`, record the run in `EndLoop`; report fields in `MakeRunReport`.
3. Save and load `runHistory`; bump to version 9 with the upgrade step.
4. `RunResultsPanel`: third column, "Longest yet" marker; text keys.
5. Check: compile, Console, all EditMode tests; play three runs in the editor.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RunReportTests.EndingARun_AddsItToTheHistory` | Loop number, ticks, actions and end reason recorded, oldest first |
| `RunReportTests.Report_LongestIsTheLongestEarlierRun` | Longest picks the max of earlier runs, not this one |
| `RunReportTests.Report_NewLongest_OnlyWhenBeatingAnEarlierRun` | False on the first run and on a tie; true when longer |
| `RunReportTests.Report_LastRunSurvivesSaveAndLoad` | After a load, the next report still has the last run |
| `RunReportTests.Report_RunsEndedEarly_DontCountForLongest` | Added: an early-ended run is skipped for Longest but is still the last run |
| `RunReportTests.ARunRestartedBeforeItEnds_IsInTheHistory_AsEndedEarly` | Added: a dev-panel restart is recorded under its own loop number |
| `SaveTests.RunHistory_RoundTrips` | History saved and loaded intact |
| `SaveTests.Version8Save_LoadsWithEmptyHistory` | Old saves upgrade cleanly |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity: play three runs of different lengths. The Summary page shows This run | Last run | Longest; "Longest yet" appears only when a run beats every earlier one. Save, load, end a run: Last run and Longest are still filled in.
- [x] decisions-log entry checked; BUILD-STATE synced (`/wrap-up`)

## Notes after implementation
- **Runs ended with *End run* don't count for Longest** (the user's ruling during implementation; decisions log). So each record also stores how the run ended, and the tests make runs collapse rather than end early.
- **Tests are in `RunReportTests`**, not a new `RunHistoryTests`: the simulation rules say to extend the existing suite, and its helpers were there.
- **`Simulation.PreviousRun` removed**; its test now checks `RunReport.RunBefore`. The report's field is `RunBefore`, not `LastRun`, so it doesn't read as `sim.LastRun.LastRun` (reviewer's suggestion).
- **Extras the user asked for:** a tooltip over "Longest" / "Longest yet" (a TMP link tag in the Summary text, `results.longest_tip`); a dev panel line (runs recorded, longest), using the new `Simulation.LongestRun`; a run restarted mid-way from the dev panel now goes in the history as ended early; two unused `using` lines removed.
- `endReason` went into version 9 without another bump: version 9 was never committed.
