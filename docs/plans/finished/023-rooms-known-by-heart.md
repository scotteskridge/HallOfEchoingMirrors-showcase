# 023 — Rooms known by heart carry into the next run

**Status:** Done (Core)
**Left to do:** nothing. The carried-plan UI is ui-024b.
**Design:** GDD v0.5 §13a *Realm mastery* (superseded in part), roadmap Part 2 (`ui-rework-roadmap.md`); decisions-log 2026-09-29 *Faster base speed; rooms known by heart…* and 2026-09-30 *Known-by-heart details*. Backlog Next 1b (Core half; the UI half is ui-024). Needs 021 built first if 021 also bumps the save version.

## Goal
Each room remembers how many runs she has worked in it. Once a room is known by heart (~4 runs), what she did there last run is already in the next run's queue when the run ends, up to the first room she doesn't know by heart. From run 4–5 the opening of a run needs no requeuing.

## Out of scope
- Summary buttons, block display in the queue column, popover text: **ui-024**.
- Room speed: **025**. The >10× one-line summary: backlog Ideas.
- A separate Planning screen.

## Design assumptions
- **Worked in a room** = she completed at least one action there this run that isn't a trip or an auto-supply. Counted once per run at run end into kept state. Old saves start every room at 0 (no per-room history exists).
- **By heart** = run count ≥ `LoopSettings.byHeartRuns` (`~`4). Counted at the end of the run, so the 4th run in a room carries into the 5th.
- **The carried plan** is built at run end from the run's record of what she did, in order: trips and actions, **without** auto-supply steps (the supplier adds them again when needed). It stops at the first step in, or trip into, a room not known by heart (settled 2026-09-30). If the start room isn't known by heart, nothing carries.
- **What drops out:** tasks now in `LockedTasks` (once-ever work already done). Everything else is carried, even if it may not start; at run time the queue's existing skip-with-reason handles it (settled 2026-09-30).
- **Repeats merge:** consecutive completions of the same action become one entry with `TimesLeft` = the count.
- **Carried entries are marked** (`QueueEntry.ByHeart`) so ui-024 can draw them as blocks and 025 can find them.
- **The between-runs queue is saved** (it now holds the carried plan; this also fixes code-health 2026-09-28's "`_nextQueue` isn't saved").

## Reuse
- `LoopState.CompletionLog` (filled in `Simulation.Tasks.cs` ~l.817): extend it to record room, item or destination and a supply flag, instead of adding a second list (check its other users).
- `Simulation.StartNewLoop` / `_nextQueue` (`Simulation.cs` l.52, l.153); `Queue => Loop.IsOver ? _nextQueue : Loop.Queue`.
- `QueueEntry` supply fields (`SuppliesFor`, `Supplies`) to tell supply apart; `Destination` for trips.
- `PersistentState.Explored` (Dictionary keyed by `NodeDefinition`) as the model for `RoomRuns`.
- `CanStart` / `TryStartNextTask` / `TaskSkipped`: unchanged skip behaviour.
- The queue's verb-plus-item rebuild for saving entries (code-health 2026-09-28), `SavedEntry`, `SaveSerializer.FromJson` upgrade chain.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `PersistentState.cs` | Edit | `RoomRuns` (room → runs worked). |
| `LoopState.cs` | Edit | `CompletionLog` entries gain room, item/destination, supply flag. |
| `Simulation.Tasks.cs` | Edit | Record the richer log entry on completion. |
| `Simulation.ByHeart.cs` | New (partial) | `IsKnownByHeart(room)`, `RunsWorkedIn(room)`, counting at run end, `BuildCarriedPlan()` into `_nextQueue`. |
| `Simulation.cs` | Edit | Call the count and the plan build when a run ends. |
| `ActionQueue.cs` | Edit | `QueueEntry.ByHeart`. |
| `LoopSettings.cs` / `.asset` | Edit | `byHeartRuns` (`~`4). |
| `BalanceSheetWindow.cs` | Edit | "Runs to by heart" field; read-only runs-worked column in Places. |
| `SaveData.cs`, `SaveSerializer.cs` | Edit | Next version: `roomRuns` kept; `nextQueue` saved; `SavedEntry.byHeart`; richer log in `SavedRun`. Upgrade: rooms at 0, empty next queue. |
| `.claude/rules/simulation.md` | Propose | One line on the carried plan (user's OK first). |

New content fields: `LoopSettings.byHeartRuns` (Balance Sheet).

Save format change? **Yes** → bump `SaveData.CurrentVersion`; upgrade: `roomRuns` empty, `nextQueue` empty, old logs without rooms read as not carryable.

## Steps
1. Failing tests: a room's count rises once per run worked, not per entry; trips and supply don't count.
2. Richer completion log; `RoomRuns`; counting at run end.
3. Failing tests for the carried plan (below); then `BuildCarriedPlan`.
4. `ByHeart` flag; carried entries skip with a reason when they can't start.
5. Save: next version, `nextQueue` and `roomRuns`, upgrade step and tests.
6. `LoopSettings` field and Balance Sheet.
7. Docs: backlog, proposed rules line.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ByHeartTests.RoomRuns_CountOncePerRun_NotPerEntry` | Corridors crossed often count once |
| `ByHeartTests.TripsAndSupply_DontCountAsWork` | Definition of "worked" |
| `ByHeartTests.FourthRun_MakesRoomKnownByHeart` | Threshold from `LoopSettings` |
| `ByHeartTests.CarriedPlan_StopsAtFirstUnknownRoom` | The cut |
| `ByHeartTests.CarriedPlan_EmptyWhenStartRoomUnknown` | Runs 1–3 carry nothing |
| `ByHeartTests.CarriedPlan_LeavesOutSupplyAndLockedTasks` | Clean plan |
| `ByHeartTests.CarriedPlan_MergesRepeats` | One entry with a count |
| `ByHeartTests.CarriedEntry_ThatCantStart_IsSkippedWithReason` | Settled 2026-09-30 |
| `SaveTests.NextQueue_AndRoomRuns_RoundTrip` | Saved between runs |
| `SaveTests.OldSave_Upgrades_WithNoRoomRuns` | Upgrade step |

## Done when
- [x] Tests pass; compile and Console clean (606 EditMode tests, 0 failed after the follow-ups below).
- [x] In Unity (dev tools may lower "Runs to by heart" to 1 for the check): end a run; the queue between runs already holds the start room's actions from last run, stopping before the first room not known by heart. Quit and reload between runs: the queue is still there.
- [x] decisions-log already records the rule; rules line added to `simulation.md` (the user approved it).

## Notes after implementation
- The record is `LoopState.Steps` (`CompletedStep`: task, room, trip destination, supply flag), saved in `SavedRun.steps`. `CompletionLog` is a view over it (made fresh on each read). An older save's plain log becomes steps with no room: the report keeps it, nothing carries.
- Saved version is 17 (16 added the rooms and queue; 17 dropped the duplicate `actionLog`). The between-runs queue is captured whenever no run is under way (including a run begun but at tick 0) and put back by `SaveSerializer.RestoreNextQueue` on loading. Queue entries are saved and restored by shared code (`SavedQueue`/`FillQueue`). `SaveSerializer.Load` is the one load entry point (`GameController.LoadGame` and the tests' `Reopen` both use it); `ResumeRun` and `RestoreNextQueue` are private to it.
- Tests are all in `ByHeartTests` (including the save ones), not `SaveTests`: that fixture has no rooms.
- **Not done: the read-only runs-worked column in Balance Sheet Places.** Runs worked live in the save, not in an asset, so the sheet has nothing to show outside play. `byHeartRuns` appears in the Rules section by itself (it draws every LoopSettings field). Suggest showing it in the dev panel instead.
- A run restarted mid-way (dev tools) or a run in an old save carries nothing into the next; only a run that ends counts.
- Placeholder: none beyond the ~4 in `byHeartRuns`. A carried step whose room isn't known by heart ends the plan, even if later steps were in known rooms.
