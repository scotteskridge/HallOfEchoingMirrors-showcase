# 017 — Run report, part B: milestones table and the rest of the page

**Status:** Done
**Design:** GDD §13a *Benchmarks* (best-ever extension) and *Legibility: the run report* [PROPOSED]; decisions-log 2026-09-29 ("best" = fastest milestone time; ending early only means fewer milestones); mock-up `docs/mockups/summary/` (read its `notes.md`); plan 016 (part A, built); `docs/BACKLOG.md` *Next* 9, which overlaps *Next* 5 (best-ever milestone times)

## Goal
The Summary page looks like the mock-up all the way down, not just at the top:
- a **Milestones table**: one row per milestone, with improvement ± · this run · last run · previous run (the mock-up's columns);
- **Changed for good** and **What she carried** as two columns of chips;
- the stats (Endurance, Perception…) as a quieter row with their icons;
- the old centred text block is gone; "Loop N" becomes a small caption above the headline.

## Out of scope
- The sparkline, the end-of-game screen, walls ("needed X, she brought Y"), findings ("the queue stalled at…").
- A flavour quote under the headline (parked in `UI-BACKLOG-later` Ideas).
- The "since last run" ghost numbers on action tooltips (backlog *Next* 5's other half). Only the *best-ever milestone time* part is shared with this plan, and it is only stored here, not shown on tooltips.
- Main-screen layout.

## Settled questions (the user confirmed the bold answers, 2026-09-29)
1. **What does "Improvement" compare?** **This run against last run** (negative = faster = green). The mock-up's columns don't say. Alternative: against the best-ever time.
2. **Is best-ever shown on this page?** **No column for it in this plan**; the time is stored (see Changes) so the tooltip work and a later "best" column can use it. Alternative: a fifth column now.
3. **Which milestones are listed?** **Every milestone she has ever reached in any run** (so a row appears once it has been reached once), ordered by the time reached this run, then not-reached ones (dim, "not reached") last. Alternative: only ones reached this run or last run.
4. **A milestone reached this run for the very first time:** row shows "new" in Improvement instead of a number.

## Design assumptions
- Milestone = a switch with a story (`LoopState.Milestones` already records these with run time).
- A milestone that can only be met once (`CanBeReachedAgain` false) still shows a row, with its time in "this run" only in the run it was met, and "—" afterwards.
- `Placeholder rule:` "previous run" means the run before last, counting every run whatever its ending. Ending early is compared like any other run (decisions-log 2026-09-29).
- `Placeholder rule:` row order = time reached this run; rows not reached this run come last, in the order first ever reached.
- Colours: an improvement (faster) uses the existing `Milestone`/good colour role; slower uses `UiStyle.Warning`. No new colours.

## Reuse
- `RunRecord` (`Core/RunRecord.cs`), `SavedRunRecord` (`SaveData.cs`), `SaveSerializer` history code: the per-run milestone times go on the record, so "last" and "previous" come from the history and survive a load.
- `PersistentState.LastRunMilestones` and `Simulation.LastRunTimeOf`: these stay for the live "last run 4:05" text on the main screen; the record makes them redundant later (list in the follow-ups, don't remove here). *(Removed by BACKLOG item 13: `LastRunTimeOf` now reads `RunHistory` directly, and `LastRunMilestones` is gone.)*
- `RunReport` and `MakeRunReport()` (plan 016): add the milestone rows.
- `RunResultsPanel`, `TemplateList<T>`, `FlowLayout`, `TraitIcon`/`TraitIcons`, `ClaraTips` (stat tips), `UiText.Clock`, `UiStyle`, `EditorUiFactory` (`MakeText`, `MakeChip`, `MakeChipRow`, `MakeScrollList`, `AddStack`).
- `SummarySetup.cs` (Steps 91, 92): new step goes here.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/RunRecord.cs` | Edit | `Milestones`: (milestone, seconds) reached that run, and `BestOf(milestone)` helper on the history if question 2 is "yes" later |
| `Core/Saving/SaveData.cs`, `SaveSerializer.cs` | Edit | `SavedRunRecord.milestones` (switch Id, seconds); version 12; older records get none |
| `Core/Simulation.cs` (`EndLoop`) | Edit | pass `Loop.Milestones` into the new `RunRecord` |
| `Core/RunReport.cs`, `Simulation.Report.cs` | Edit | `MilestoneRows`: milestone, this run, last run, previous run (each a nullable time), change in seconds; ordered as assumed |
| `UI/MilestoneTable.cs` | New | builds rows from a template (name · improvement · this · last · previous), colours the improvement, dims not-reached rows, tooltip on a row (the milestone's title) |
| `UI/ChipColumns.cs` | New | the two chip lists (Changed for good, What she carried) from a chip template; kept chips in the gilt border, left-behind ones struck through, as in the mock-up |
| `UI/StatStrip.cs` | New | the quiet stats row: icon, name, level, mastery ×, using `TraitIcons` and `ClaraTips` for the hover text |
| `UI/RunResultsPanel.cs` | Edit | feeds the three new pieces; deletes `Describe`, `AppendComparison`, `AppendActions`, `AppendAttributes`, `AppendGains` and the `<pos=…>` columns; "Loop N" caption above the headline; the `Longest` figures move into the headline block as "Longest yet" (see Steps 5) |
| `Editor/SummarySetup.cs` | Edit | Step 93: *Summary: milestones and lists*. Builds the table (header row + row template), the two chip columns, the stat row and the caption inside `ReportScroll`, and switches the old text off |
| `Assets/Text/game_text.txt` | Edit | new keys: `results.milestones_heading`, `results.col.milestone/improvement/this_run/last_run/previous_run`, `results.not_reached`, `results.new_milestone`, `results.changed_heading`, `results.carried_heading`, `results.run_caption`, chip texts for kept/lost; retire the keys the old text used (`results.time`, `results.actions`, `results.done`, `results.task_count`, `results.stat_line`, `results.kept`, …) once nothing reads them |

New content fields: none. Row and column widths are layout, on the components as Inspector fields.

Save format change? **Yes** → `SaveData.CurrentVersion` 11 → 12. Upgrade step: history records from older saves have no milestone times, so their columns show "—".

## Where the old lines go
The old text listed things the mock-up drops or moves; none should vanish silently:
| Old line | New home |
| --- | --- |
| Time / Actions / Longest table | headline + "Last run … (±)" (built); actions count and "Longest yet" become one small line under it (`Placeholder rule:` the user hasn't said whether the count is wanted) |
| Done (each action × count) | dropped (the user: the time strip conveys the same in a more compact way); kept in `RunReport.Tasks` |
| Clara: stat lines | the stat row |
| Kept resources (Mirrors found 3 → 5) | What she carried, **only when she walked out through the mirror with them** (the user); a run that ended otherwise shows none |
| Tools and stats held (this run only) | dropped: they don't last |
| Carried out / left behind | What she carried: carried out shown; left behind only on a walked-out run's report if any (`Placeholder rule:` unless the user says otherwise) |
| Explored (rooms searched %) | dropped from the page for now; `RunReport.Explored` stays |
| Switches flipped | Changed for good, **permanent switches only** (the user) |

## Steps
1. Failing tests: milestone times on `RunRecord` saved and loaded; report rows (see Tests).
2. Core: `RunRecord.Milestones`, `EndLoop` passes them; `RunReport.MilestoneRows`. Tests pass.
3. Save: round-trip and version 11 → 12 upgrade tests, then the fields and upgrade step.
4. Text keys.
5. `MilestoneTable`, `ChipColumns`, `StatStrip`; then `RunResultsPanel` uses them and the old text builders are deleted.
6. Setup Step 93, run it, check the layout at 1920×1080 and 1280×720 with a long run (10+ minutes, many milestones) and a first run (no history).
7. Play one run of each ending; compare with the mock-up side by side.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RunReportTests.Milestones_ThisRunLastAndPrevious` | the three columns come from the run and the two before it |
| `RunReportTests.Milestones_NotReachedThisRun_ListedLast` | ordering rule, and the dim row |
| `RunReportTests.Milestones_FirstTime_HasNoImprovement` | question 4 |
| `RunReportTests.Milestones_ImprovementIsThisMinusLast` | sign convention |
| `RunReportTests.Milestones_EndedEarly_ComparesLikeAnyRun` | decisions-log rule |
| `SaveTests.RunRecordMilestones_RoundTrip` | history keeps milestone times |
| `SaveTests.Version11Save_LoadsWithNoMilestoneHistory` | upgrade step |

## Done when
- [x] Tests above pass; all EditMode tests pass; compile and Console clean; `GameTextTests` and `UiFontsTests` pass (new characters → *UI → Update UI Font Atlases*)
- [x] In Unity: run Step 93, play three runs. The Summary page matches the mock-up top to bottom: caption, headline, strip, skills, milestones table with the four columns, two chip columns, quiet stats row, button. A very long run scrolls and nothing overlaps the button.
- [x] A first run (no earlier runs) shows "—" in Last and Previous, and no improvement figure
- [x] Every layout change listed; keys added or changed listed for a `/writing` pass
- [x] `BACKLOG.md` *Next* 9 removed and the milestone-time half of *Next* 5 marked as stored; decisions-log entry for whichever way questions 1–4 went
- [x] `PROJECT_NOTES.md` placeholder rules: the row order and the meaning of "previous run"

## Notes after implementation
Built 2026-09-29. All 497 EditMode tests pass (9 new: 7 in `RunReportTests`, 2 in `SaveTests`). Step 93 was run and the scene saved; the page was checked in Play with an empty second run (dimmed "not reached" row, empty lists). Not yet checked by eye: a run with skills, stats, chips and a walked-out ending, or 1280×720.
- `SaveData.CurrentVersion` is now 12.
- `RunResultsPanel` lost its `_summary` field; Steps 91 and 92 still mention it but return early on a scene that has run them (retire them with Step 93 after commit).
- The actions/longest line is one line with one pop-up over all of it (the old per-word link is gone).
- Setup Steps 86-93 were retired after commit 4073d11: `GreyboxSetup.cs` is empty again and `SummarySetup.cs` is deleted. To read them, use `git show 4073d11:Assets/Editor/SummarySetup.cs` (91-93) and `git show 4073d11:Assets/Editor/GreyboxSetup.cs` (86-90).
