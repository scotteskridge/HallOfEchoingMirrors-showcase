# 016 — Run report, part A: time by skill

**Status:** Done
**Design:** GDD §13a *Legibility: the run report* [PROPOSED], §13a *Benchmarks*, §14.12 and §14.19; the user's answers while planning (2026-09-29); mock-up `docs/mockups/summary/` (read its `notes.md`)

## Goal
When a run ends, the Summary page opens with:
- a headline that says how the run ended and how long it lasted, compared with the last run;
- a strip showing which skills Clara spent her time on, one slice per skill, each with its icon;
- directly under the strip, a row of those skills showing what she learned (×before › ×after, also with icons).

## Out of scope
Part B, `docs/BACKLOG.md` *Next* 9:
- the milestones table (improvement ± · this run · last run · previous run);
- best-ever milestone times;
- restyling *Changed for good* and *What she carried*;
- stats with icons;
- the page's final look.

Also out of scope:
- walls ("needed X, she brought Y"): parked as *The first wall, in the lab*;
- a flavour line under the headline: parked in `UI-BACKLOG-later` Ideas;
- the sparkline;
- the end-of-game screen;
- main-screen layout fixes.

## Design assumptions
- **The run clock only runs while Clara is doing an action** (the user). So every tick belongs to exactly one action, and there are no idle or stalled slices. A test checks this. If it fails, stop and ask.
- Each action has one skill (`TaskDefinition`'s skill field). Travel and Explore use theirs (Wayfinding).
- `Placeholder rule:` if an action has no skill, its time goes into one unlabelled "Other" slice.
- The headline has one wording per ending: vitality ran out, ended early, stepped through the mirror (`LoopEndReason` Exhausted / EndedByPlayer / WalkedOut). The comparison is with the last run only, because "best" belongs to milestone times (part B). Ending early is compared like any other run.
- `Placeholder rule:` in the strip, the widest slice comes first. The skill name only shows if the slice is wide enough (about 90 px); the icon needs about 22 px. The colours are shades of the gilt colour role, darker for smaller slices.
- For a save made in the middle of a run with the old format, the tallies start empty. The strip then shows shares of the time recorded since the upgrade.

## Reuse
- `RunReport` (`Core/RunReport.cs`), `MakeRunReport()` (`Core/Simulation.Report.cs`): add the time split here. `Seconds`, `EndReason`, `RunBefore` and `Skills` (`SkillGain`) already exist.
- `LoopState` (`Core/LoopState.cs`): the per-run tally goes here, beside `CompletionLog` and `SkillXp`.
- The per-tick XP grant in `Core/Simulation.Attributes.cs`: tally ticks at the same place, so time and XP can't disagree.
- `SkillDefinition.Icon`, `TraitIcon` / `TraitIcons` (`UI/`): the slice and chip icons, with their hover text.
- `RunResultsPanel` (`UI/RunResultsPanel.cs`): gains the headline and hosts the two new pieces. The skill lines leave its text block.
- `UiText` for time formatting; `UiStyle` / the plan 012 colour roles; `UiFonts` roles.
- `SaveData` / `SaveSerializer` (`Core/Saving/`), `CurrentVersion` = 10.
- `GreyboxSetup.cs`: the one-click setup step pattern (last step: 90).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/LoopState.cs` | Edit | `SkillTicks` (skill → ticks), plus the "Other" count; cleared at the start of each run |
| `Core/Simulation.Attributes.cs` (or wherever the running action advances) | Edit | +1 tick to the action's skill each tick it runs |
| `Core/RunReport.cs`, `Simulation.Report.cs` | Edit | `TimeBySkill`: (skill, ticks, seconds, share), longest first, "Other" last |
| `Core/Saving/SaveData.cs`, `SaveSerializer.cs` | Edit | save the run's `skillTicks` by skill `Id`; version 11 |
| `UI/RunTimeStrip.cs` | New | builds one slice per entry from a template (`LayoutElement` flexible width = share); hides the label and then the icon when the slice is too narrow; tooltip on each slice |
| `UI/SkillGainRow.cs` | New | one chip per skill, in the strip's order: icon, name, ×before › ×after, progress bar; tooltip: level and XP this run |
| `UI/RunResultsPanel.cs` | Edit | headline and "vs last run" line as their own TMP texts; feeds the strip and the row; drops the skill lines from the text block |
| `Editor/GreyboxSetup.cs` (or a new partial file, if the class is partial and the file is too long) | Edit | Step 91: *Summary: time strip and skills*. Builds the headline, the strip with its slice template and the skill row, in that order above the existing text |
| `Assets/Text/game_text.txt` | Edit | new keys: `results.headline.exhausted`, `results.headline.ended_early`, `results.headline.walked_out` (with `{time}`), `results.vs_last` (`{time}`, `{delta}`), `results.time_heading`, `results.learned_heading`, `results.slice_tip` (`{skill}`, `{time}`, `{percent}`), `results.other`; retire any `results.ended.*` keys left unused |

New content fields: none. The slice widths (90 / 22 px) are UI layout, not balance, and live on `RunTimeStrip` as Inspector fields.

Save format change? **Yes.** Bump `SaveData.CurrentVersion` to 11. Upgrade step: a run under way gets empty `skillTicks`; history is unaffected.

## Steps
1. Failing tests: the time tallies, and the report's `TimeBySkill` (see Tests).
2. Tally in Core: `LoopState` and the tick. Build `RunReport.TimeBySkill`. Tests pass.
3. Save: failing round-trip and upgrade tests, then the version 11 fields and the upgrade step.
4. Text keys in `game_text.txt`.
5. `RunTimeStrip` and `SkillGainRow`. Slices and chips come from templates, refreshed on each report.
6. `RunResultsPanel`: headline, the vs-last line, wiring, and the skill lines taken out of the text.
7. Setup step 91 in *Hall of Echoing Mirrors → Setup*. Run it, check the layout, then play one run of each ending.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RunReportTests.TimeBySkill_SumsToRunTicks` | every tick is counted once (the "clock only runs during actions" assumption) |
| `RunReportTests.TimeBySkill_TravelCountsForItsSkill` | travel time goes to the travel action's skill |
| `RunReportTests.TimeBySkill_LongestFirst_OtherLast` | slice order |
| `RunReportTests.TimeBySkill_NoSkillAction_GoesToOther` | the placeholder bucket |
| `RunReportTests.TimeBySkill_ResetsEachRun` | per-run, not kept |
| `RunReportTests.TimeBySkill_SharesSumToOne` | widths fill the strip |
| `SaveTests.SkillTicks_RoundTripMidRun` | a reload mid-run keeps the split |
| `SaveTests.Version10Save_LoadsWithEmptySkillTicks` | upgrade step |

## Done when
- [x] Tests above pass; all EditMode tests pass; compile and Console clean
- [x] In Unity: run Step 91, then play a run to the end. The Summary page shows the headline, e.g. "Her strength gave out after 4:20", with "last run 5:10 (−0:50)" under it. Below that is the strip: one slice per skill with its icon, and a tooltip on hover. Under the strip is the skills row, in the same order, with icons and ×before › ×after.
- [x] End a run early: the headline wording changes and the strip still shows correctly.
- [x] Every layout change listed in the report; the keys added or changed listed for a `/writing` pass
- [x] decisions-log entry: the run clock only runs during actions (no idle time); the Summary shows time by skill; "best" means milestone times; a wall is a stat or pool check (the user, 2026-09-29)
- [x] `PROJECT_NOTES.md` placeholder rules: the "Other" slice, and the slice widths and order

## Notes after implementation
Built 2026-09-29, alongside plan 017. A layout bug surfaced once both parts were on screen together with real content: `SkillRow` (the skills-learned chips under the time strip) had no `ContentSizeFitter`, unlike its sibling `StatStrip`, so it never grew past its saved (empty) height and everything below it in `ReportScroll` — the milestones table, the chip columns — overlapped it. Fixed 2026-09-29 by adding a `ContentSizeFitter` to `SkillRow` (Horizontal: Unconstrained, Vertical: Preferred Size), matching `StatStrip`. Confirmed fixed by playtest.
