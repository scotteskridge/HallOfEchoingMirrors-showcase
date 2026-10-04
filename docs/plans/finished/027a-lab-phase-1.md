# 027a — The lab, phase 1: watch, study, attend, talk to Roland

**Status:** Done 2026-10-01 (built 2026-09-30, setup step 100 run and retired; the user played it and signed it off (2026-10-01))
**Left to do:** nothing.
**Design:** GDD v0.5 §12 *The memory lab: Act I's finish*; decisions-log 2026-09-29 *The Mirror's Laboratory*, 2026-09-30 *Lab plan: search reopens, phase 1 locks* and the stats pass entry (the lab's pairings, Attunement asleep until the talk). **Content tables:** `027-lab-redesign.md` (read it first; this plan says what to build and in what order).

> **Design change 2026-09-30 (decisions log *Features come in layers*): Act I has no stats.** This plan is built and stays as it is. Its stat pairings (Watch him → Perception, Attend → Composure, *Roland Takes the Ring* waking Attunement, Convincing learning faster with Attunement) come out later with backlog *Act I without stats*, not in a rework of this plan. For the in-Unity check, the stat behaviour can be ignored.

## Goal
The lab's search reveals its tasks in three stages. Over 2–3 visits she builds a kept insight, and on the third or later she can **Talk to Roland** (once ever): he takes the ring, the ring's way opens without it, and phase 1's tasks lock.

## Out of scope
- Phase 2 and the exit (027b, 027c). After the talk, the room holds only Fill a phial and Draw on the mana stone until 027b.
- The plot of what Watch him reveals; all text is `[PLACEHOLDER]`.
- Switching action or carry costs on; rebalancing the hall; deleting the unlisted assets.

## Design assumptions
The spec's list, 1–8, and its *Where the numbers come from* (every number a labelled placeholder). **Placeholder rule:** the talk's 24 s base and the ~40 s lab budget it assumes.

## Reuse
- `TaskDefinition.trainsAttribute` and `Simulation.StatTrainedBy` are **already built** (plan 031): Watch him → Perception and Attend → Composure are set; the talk just sets Attunement.
- `SwitchDefinition.wakesAttributes` (plan 031) wakes Attunement; nothing lists it yet.
- `Simulation.CantTravel` (`Simulation.Places.cs:121`) is the only place a way's `needs` **stop** a trip (`MissingFrom`); `Simulation.PlanWarnings.cs:106` warns on them; `Simulation.Unlocks.StillUsed` (`:63`) keeps an item listed while a way needs it; `ContentIndex` indexes them. `IsOpen` / `Names` (`Simulation.Places.cs:39–59`) show how a `WayRef` matches a way (both-ways aware): reuse `Names`.
- `Simulation.Flip` (`Simulation.Unlocks.cs:276`) writes unlocks into `Persistent` once, at the flip: why the lab's tasks start unlocked (spec *Mechanism*).
- `RoomFind.atSearched` for staged reveals; room offers show a found task with unmet needs greyed, with the reason (`Simulation.Offers.AddRoomOffer`).
- Test classes: `PlacesTests`, `SwitchTests`, `LabRulesTests`, `SaveTests`, `PlanWarningTests` (if it exists), `SimulationTestBase`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/SwitchDefinition.cs` | Edit | `waivesWayNeeds` (list of `WayRef`), tooltip |
| `Core/Simulation.Places.cs` | Edit | `NeedsOf(way, owner)`: the way's needs, or none once a flipped switch waives it (read from `Persistent.FlippedSwitches`, so no save field); `CantTravel` uses it |
| `Core/Simulation.PlanWarnings.cs`, `Simulation.Unlocks.cs` (`StillUsed`) | Edit | read a way's needs through `NeedsOf`, so the plan doesn't warn about the ring and the ring can be let go once waived |
| `Editor/BalanceSheetWindow.cs` | Edit | switch *Waives* column if switches have a table (else say so) |
| `Editor/GreyboxSetup.cs` | Edit | *Setup/Step 100: Lab phase 1* (content below) |
| Lab assets, `game_text.txt` | Edit / New | per the spec's phase 1 tables |

Save format change? No.

## Steps
1. Failing tests: the ring way needs the ring until the waiving switch flips; after, it doesn't, across runs and a save round-trip; the plan stops warning about it.
2. Add `waivesWayNeeds` and `NeedsOf`; route `CantTravel`, the plan warning and `StillUsed` through it; Balance Sheet column. Compile, console, all EditMode tests.
3. Setup step 100 (Lab phase 1): Convincing skill (learns faster with Attunement; reuse an icon); Watch him, Study the tome (drop The tome and Bench cleared), Attend: `startsUnlocked`; new Talk to Roland (spec numbers: Correct a memory × 0.9108, trains Attunement, needs/takes, Insight ×0.9); Insight's display name; `foundBySearching` at 20/34/67 with Fill a phial added; unlist `ClearTheBench`, `TakeTheTome`, `CutTheStone` from the room and every switch; `TheMemoryIsWrong` unlocks only Draw on the mana stone; new *Roland Takes the Ring* (locks Talk, `TakeTheRing`, Watch, Attend, Study; waives the HangingMirrors → OtherLaboratory way; wakes Attunement); unlist `TheStoneIsCut` from `GameContent`.
4. Descriptions and the placeholder passage *He Takes the Ring* (keys in `game_text.txt`).
5. Run the setup step; compile, tests; the in-Unity check. Placeholders to `PROJECT_NOTES.md`; unlisted assets to BACKLOG-later *Remove leftover assets*.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `PlacesTests.WaivedWay_NeedsItem_UntilSwitchFlips` | the gate before the switch |
| `PlacesTests.WaivedWay_StaysOpen_AcrossRunsAndSave` | the waiver is kept |
| `PlacesTests.WaivedWay_OnlyWaivesThatWay` | other ways' needs still count |
| `PlanWarningTests.WaivedWay_NoMissingItemWarning` (or the class that tests ui-024d) | the plan agrees |
| `LabRulesTests.OnceEverTask_LockedBySwitch_AfterCompletion` | the talk can't be done twice, even though it starts unlocked |

## Done when
- [x] Tests pass (694 EditMode); compile and Console clean.
- [ ] In Unity: searching the lab reveals its tasks at 20, 34 and 67%; Watch him adds one kept Insight a run; Talk to Roland shows greyed with "Insight 3" until the third visit; completing it shows *He Takes the Ring*, Attunement's chip wakes, Watch/Attend/Study and Instantiate Roland's ring are gone, and next run she walks into the lab without the ring. Draw on the mana stone is unchanged.
- [ ] Report: tests run, placeholders, unlisted assets, which assumptions held.

## Notes after implementation
- **`trainsAttribute` was already built** (plan 031); steps 1–2 of the old draft were dropped.
- **`NeedsOf(way, owner)`** (`Simulation.Places.cs`) is the one reader of a way's needs; `CantUseWay` now also hands back the room that lists the way, so a waiver can be matched with the same `Names` rule as opening and closing. `MissingFrom` and `NothingGives` take `IReadOnlyList` so a waived way can return an empty array rather than a shared list. `ContentIndex` still indexes the raw needs (it's about which content a save can name).
- **Balance Sheet:** switches have a table; the waiver shows in its *Changes* summary ("waives needs on …"), not a separate column (it's a list of ways, like *opens*).
- **Removed from `GameContent.tasks` too,** not only from the room and switches: a task listed at no room can be done anywhere, so an unlocked *Cut the stone* would have appeared everywhere.
- **Watch him's Insight discount stays ×0.8** (the asset's value), not the spec's ×0.9; the step only fills what's new or still at its old value.
- The new tests passed at their first run: they were written first, but they couldn't compile until `waivesWayNeeds` existed, so the red step was a compile failure rather than a failing assertion.
- Descriptions are placeholder text in the assets (`description` is shown as written, not a `game_text.txt` key), set by the step only where empty.
- **Review fixes:** skill chips now show once a skill is found or trained (`Simulation.KnownSkills`, the user's choice; two `AttributeTests`); the ring's three blurb buckets end at *Roland Takes the Ring* (`BlurbImporter.FixKnownRules`, run); step 100 removes only the lab's own tasks from *The Memory Is Wrong*; `PlanWarningTests.WaivedWay_NoMissingItemWarning` also checks the warning before the waiver; `PlacesTests.WaivedWay_NoLongerKeepsItsItemInUse` covers `StillUsed`.
