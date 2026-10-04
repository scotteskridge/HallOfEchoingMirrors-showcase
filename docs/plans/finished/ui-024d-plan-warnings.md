# ui-024d — Warnings ahead on the plan

**Status:** Done 2026-09-30 (built, reviewed, in-Unity check passed by the user)
**Left to do:** nothing.
**Design:** decisions-log 2026-09-30 *The planning screen (plan ui-024a)* (warn ahead; silent where it can't tell), *Planning in a room not known by heart is allowed…* (same marking for won't-carry), *Warnings ahead: the rule (plan ui-024d)*; mock-up `docs/mockups/planning/` (note 5; the ⚠ and orange reason line in the "block unfolded" state). No GDD § (the GDD has nothing on this). Third of three: ui-024b → ui-024c → **ui-024d**. UI-BACKLOG *Now*; BACKLOG Next 1b.

## Goal
In the queue column, on the planning screen and during a run, an entry that will be refused when it's reached gets an orange ⚠ with the reason under it ("needs 1 candle"), its stop and folded block get a ⚠, and the foot says "· 1 warning". Stops that won't carry over get the ⚠ too, but aren't counted.

## Out of scope
- Simulating pools, vitality, pocket space or items used up (see assumptions).
- A run-ahead strip (vitality, estimate): the planning screen's spare slot stays empty.
- Warnings on the map's room tags.
- The per-frame string clean-up of `StopCard.Refresh` (UI-BACKLOG Next 9b), except that the walk itself must not run every frame.

## Design assumptions (the rule: **Placeholder rule**, list it in `PROJECT_NOTES.md`)
- **The walk** starts from the queue's start: between runs, the start room, her packed items (`Persistent.Packed`) and kept switches/ways; during a run, where she is now, her pockets and every floor now. It steps through entries in order, moving her as `NodeAfterEntry` does. The top entry during a run is skipped (the real check handles it).
- **Warned (sure):** a trip that can't be made (`CantTravel` from the planned room, as `SkippedTripReason` does today); a task a switch has locked; a task not offered in the planned room (`not_here`); a hue she hasn't learned; a second copy of a once-a-run task after one earlier in the walk (or, during a run, one already done this run).
- **Warned (items):** a need (an action's or a way's) for an item that nothing can give her in time: she doesn't hold it or have it packed, it's on no floor, no earlier entry in the walk gives it, and (for actions, not trips) no action in that room gives it (auto-supply could make it). Items used up by earlier entries are **not** subtracted, so it errs towards silence.
- **Silent:** stat gates (her level rises during the run); pools, vitality, full pockets or floors; everything after an entry that can flip a switch (a switch may open ways, unlock hues or tasks).
- **"Can flip a switch"** (switches are flipped by triggers, not by tasks directly; `SwitchDefinition`): for a switch not yet flipped (`Persistent.FlippedSwitches`), the entry's task is its `triggerTask` or in its `requiredTasks`, or the task `gives` its `resourceToHold`, or the entry is a search of its `roomToExplore`. `AttributeLevelReached` switches are ignored (they could fire at any point; placeholder).
- Between runs, `Loop.CompletedTasks` and `Loop.Floor` belong to the last run: the walk must not read them.

## Reuse
- `CantTravel` (`Simulation.Places.cs`), `NodeAfterEntry`/`PlanStart`, `SkippedTripReason` (`Simulation.Route.cs`): the location half of the walk.
- `NeedsReason`, `MissingFrom` (`Simulation.Supplying.cs`); `IsUnlocked` (`Unlocks.cs`); `CantStartHere`, `CantStartForUnknownHue` (`Simulation.Tasks.cs`): the reasons and their `reasons.*` wording.
- `QueueStop`/`MarkStopsThatCarry` (`Simulation.ByHeart.cs`): won't-carry already known.
- `StopCard` (row details, `NotCarriedNote`, `ShowBlock`), `QueueDrawer` foot (`queue.actions_count`), `UiStyle.Warning`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.Tasks.cs` | Edit | One `WhyEntryCantStart(entry)` (carry-full, `CanStart`, trip wrap) used by `Enqueue` and `TryStartNextTask` (BACKLOG follow-up, code-health 2026-09-28). |
| `Core/Simulation.PlanWarnings.cs` | New | `PlanWarnings()`: the walk; returns a reason per queue index (null = none). Cached; marked stale on the existing `QueueChanged` event and when a switch flips (`Flip`, `Simulation.Unlocks.cs`), recomputed on the next read. |
| `UI/StopCard.cs` | Edit | ⚠ after a warned row's name, reason in the row's existing details text coloured with `UiStyle.Warning` (no prefab change; if it doesn't fit, stop and report); ⚠ on the stop heading and on a folded block if any entry in it is warned, or the stop won't carry. Tooltip on the ⚠. |
| `UI/QueueDrawer.cs` | Edit | Foot: "· N warnings" (won't-carry not counted). |
| `game_text.txt` | Edit | `queue.warnings.one/many`, `queue.warning_tip` (stop ⚠: "Some actions here won't start"). |

Save format change? No. New balance fields? No.

## Steps
1. Tests first for `WhyEntryCantStart`; fold the two copies into it. All existing queue tests stay green.
2. Failing `PlanWarningTests` (below), then `PlanWarnings()` for the sure reasons.
3. Add the item rule and the switch cut-off.
4. Cache it; check the planning screen and a run with a long queue don't stutter.
5. UI: row ⚠ and reason, stop and block ⚠, won't-carry ⚠, foot count. Check the book font has ⚠ (U+26A0); if not, add it as a fallback glyph or use a sprite, and say which.
6. Placeholder rule in `PROJECT_NOTES.md`; strike the BACKLOG follow-up line.

## Tests
| Test (`PlanWarningTests.*`) | Proves |
| --- | --- |
| `ATripThroughAShutWay_IsWarned` | location walk warns |
| `AnEntryNeedingAnItemNothingGives_IsWarned` / `…ThatAnEarlierEntryGives_IsNot` / `…ThatThisRoomCanSupply_IsNot` / `…ThatIsPacked_IsNot` | the item rule |
| `ASecondOncePerRunTask_IsWarned` | doubles |
| `ALockedTask_IsWarned`; `AnUnlearnedHue_IsWarned` | sure reasons |
| `AfterAnEntryThatFlipsASwitch_ItIsSilent`; `AStatGate_IsSilent` | silence |
| `BetweenRuns_LastRunsFloorAndDoneTasks_AreIgnored` | no stale state |
| `DuringARun_TheTopEntry_IsNotWarned` | the real check owns it |
| `WhyEntryCantStart_*` (in `TaskTests`) | one reason builder, same answers as before |

## Done when
- [x] Tests above pass; all EditMode green; compile and Console clean.
- [x] In Unity: end a run, press **Plan**, queue an action that needs an item nothing in the plan or that room makes (the implementer finds a real one in `Assets/Data/Tasks` and names it in the report) → orange ⚠, reason under it, ⚠ on the stop, "· 1 warning" in the foot. Remove it → the warning goes. Queue in a room not known by heart → ⚠ on the stop, not counted. Begin → the same marks show during the run.
- [x] Decisions log entry written (done at planning); placeholder rule listed.

## Notes after implementation
- Built as planned, plus two cases the plan didn't name, both towards silence: a search earlier in the plan (or Perception rising, when the bar is already far enough) may find a hidden way or a found-by-searching action, so a trip through such a way is silent and cuts off the walk, and such an action isn't warned `not_here`. Pick-ups and put-downs are never warned (they depend on floors and pockets then). What an entry "gives" includes what a pick-up takes and what a search of the room gives.
- Stale marking: besides `QueueChanged` and a switch flipping, also an action done, a way found, a run beginning or ending, packing (`SetPacked`) and a restored queue.
- Refactors for reuse: `CantUseWay` split out of `CantTravel` (the walk checks a way's needs itself); `NotHereReason`, `NeedsPoolReason`, `OpensAtRunStart` extracted so the walk reads the same wording and pool rule.
- ⚠ (U+26A0): in every source font; *Update UI Font Atlases* added it to the four Inter atlases; EB Garamond draws it through its Inter fallback. No sprite.
- Tests: 21 `PlanWarningTests` (the plan's list, plus the added silences, the not-here and floor cases, and the review's fixes) and 3 `WhyEntryCantStart_*` in `TaskTests`.
- Review fixes: after a trip warned for a missing item the walk goes silent (she stays put though the stops show her arriving); a warned entry gives nothing and uses up no once-a-run task; an unknown switch trigger fails loudly; items used up mid-action mark the warnings stale.
- Real content for the editor check: *Light a candle* in The Left Corridor needs *Flint and steel*, made only in A Dark Hall.
