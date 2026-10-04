# 032b — The Summary survives a load; a dev control for runs worked

**Status:** Done (play check passed, 2026-10-01)
**Design:** no GDD change; follow-ups to plan 032a, added by the user, 2026-09-30. Build after 032a.

## Goal
After loading a game between runs, Menu → Summary shows the last run's report again. In the editor, the dev panel can set how many runs she has worked in a room, so "known by heart" (the glow, planning, carrying) can be tested without playing 4 runs.

## Out of scope
- Changing what Load opens: it keeps 032a's rule (before *Feed your hours*, Load starts the run; after it, Load opens the planning screen).
- Saving older reports: `RunHistory` stays as it is.
- Any dev control for the start room, whose count comes from runs finished.

## Design assumptions
- **The saved report:** `Simulation.LastRun` is saved and loaded, with content stored by `Id`, as every save must. Older saves load with no report, as now.
- **Open for the user at wrap-up:** now that the report survives, should Load before the unlock show the Summary instead of starting the run?
- **The dev control (editor only):** in `DevToolsPanel`, − / + / "by heart" buttons next to each room's runs worked, calling a Core `DevSetRoomRuns(room, runs)`. The start room's row has no buttons. The count never goes below 0.

## Reuse
- `Simulation.LastRun` (`Simulation.Report.cs:10`), set when a run ends (`Simulation.cs:249`); `RunResultsPanel` reads it.
- `SaveSerializer` (`Saving/SaveSerializer.cs`): the existing pattern for storing content by `Id` and for upgrade steps.
- `PersistentState.RoomRuns`, `DevToolsPanel.DescribeRoomRuns` (l.191).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Saving/SaveData.cs`, `SaveSerializer.cs`, `Simulation.Report.cs` | Edit | save and load `LastRun` by content `Id`; bump `SaveData.CurrentVersion`; upgrade step: no report |
| `Simulation.ByHeart.cs` | Edit | `DevSetRoomRuns(room, runs)` (refuses the start room loudly) |
| `DevToolsPanel.cs` | Edit | the runs-worked buttons |

Save format change? **Yes.** Bump `SaveData.CurrentVersion`, with an upgrade step: older saves have no last run.

## Steps
1. Failing `RunSaveTests` for the saved report, then the save, load and upgrade step.
2. `DevSetRoomRuns` with a test, then the panel's buttons.
3. Docs: decisions-log line (the report is saved; save version N); answer the open Load question with the user.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RunSaveTests.LastRun_KeptThroughSave` | the Summary's report survives a load |
| `RunSaveTests.OldSave_LoadsWithNoLastRun` | the upgrade step |
| `ByHeartTests.DevSetRoomRuns_ChangesKnownByHeart` | the dev control's Core call |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] Finish a run, save, quit to the menu, load: Menu → Summary shows that run
- [x] Dev panel: the + / "by heart" buttons make a room glow and let it be planned; the start room's row has none
- [x] decisions-log updated

## Notes after implementation
- The report's save code is its own pair of files (`SavedRunReport.cs`, `RunReportSaving.cs`) because `SaveSerializer` is past 600 lines; its lookup helpers (`Find`, `TaskFor`, `FindMilestone`, `IdOf`, `ItemIdOf`) became `internal`.
- The two history records the report compares with (`RunBefore`, `Longest`) are saved as loop numbers and found again in the run history.
- The dev panel lists every room (not just ones with a count), so a room at 0 can be raised.
- Tests: `RunSaveTests.LastRun_KeptThroughSave`, `NoRunYet_SavesNoLastRun`, `OldSave_LoadsWithNoLastRun`; `ByHeartTests.DevSetRoomRuns_ChangesKnownByHeart`, `DevSetRoomRuns_RefusesTheStartRoom`.
- The open Load question (should Load before the unlock show the Summary?) was not answered at wrap-up: it is in `docs/BACKLOG.md`.
- Unplanned: `QueueRow` got a lazy `Group` property (a row inside a folded block refreshed before its `Awake`, so `_group` was null).
