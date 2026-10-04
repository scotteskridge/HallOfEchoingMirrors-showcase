# 027b — The lab, phase 2: the search reopens, the earrings, dense wisps

**Status:** Done 2026-10-01 (built 2026-09-30, setup step 101 run and retired; the user played it and signed it off (2026-10-01))
**Left to do:** nothing.
**Design:** as 027a. **Content tables:** `027-lab-redesign.md` (*Items*, *Tasks: phase 2*, *Switches*).

> **Design change 2026-09-30 (decisions log *Features come in layers*): Act I has no stats.** **Take the earrings drops `needsPerception` 15.** **Placeholder rule:** it's a plain find at a later threshold of the reopened search, after phase 1's finds and before the gem's tasks show at 25%. So this plan needs no Perception training, and the 027d *Earrings found* jump needs no Perception either. New tasks set no `trainsAttribute` overrides. Leave Attunement's bonus to restoratives alone: backlog *Act I without stats* removes it. Everything else stands.

## Goal
After the talk the lab's search starts again and finds the white sapphire earrings: kept once taken, and each run she can fill them with dense wisps, a slower, stronger restorative than a phial.

## Out of scope
- Practise the cut, Craft the gem, the exit (027c); the Amber pool.
- Search rounds in any other room (only the fields; the lab is the one user).

## Design assumptions
- **Placeholder rule:** a reopened search restarts the bar at 0% and **every earlier find stays found**, whatever its threshold (all phase 1 finds are at ≤67%, reached before the talk).
- A dense wisp goes in the earrings only, never loose in a pocket (as a filled phial goes in the pouch).

## Reuse
- `Simulation.Flip` applies switch effects once into `Persistent`, so a one-off reset of the search belongs there.
- `Persistent.Explored` (kept search percent per room), `Simulation.IsFoundHere` (checks each `RoomFind`), `RoomFind` (`NodeDefinition.cs:56`).
- `Simulation.FillContainers` / `ContentsOf` (`Simulation.Resources.cs:~87`): contents are worked out from `Loop.ToolsAndStats`; it walks every container in `Loop.ToolsAndStats` (`:94`) with no kept/per-run check; **first find out** whether a kept item reaches `ToolsAndStats` at all and what an empty kept container does today, then write the tests. `Emptyphial` / `Bottledwell` are the pattern.
- Test classes: `ExploringTests`, `ContainerTests`, `PocketTests`, `SwitchTests`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/SwitchDefinition.cs` | Edit | `reopensSearch` (list of `NodeDefinition`) |
| `Core/NodeDefinition.cs` | Edit | `RoomFind.afterSwitch` (`SwitchDefinition`, null = first round); also fix the stale "found again every run" comments here and on `SwitchTrigger.RoomExplored` |
| `Core/Simulation.Unlocks.cs` / `Simulation.Places.cs` | Edit | on flip, reopened rooms' `Explored` goes to 0; `IsFoundHere`: an `afterSwitch` find needs that switch flipped; an earlier find counts as found once its room is reopened |
| `Core/Simulation.Resources.cs` | Edit | `FillContainers` also counts kept containers (from `Persistent`); contents stay per run |
| `Editor/BalanceSheetWindow.cs` | Edit | *After switch* column on room finds; fix the "Found at % this run" header |
| `Editor/GreyboxSetup.cs` | Edit | *Setup/Step NN: Lab phase 2, earrings* (next free number) |

Save format change? No new fields, but **version 21** with an upgrade step (see the notes).

## Steps
1. Failing tests: a reopening switch resets the room's search to 0 once; earlier finds stay found; an `afterSwitch` find appears only after the flip, at its threshold; survives a save round-trip.
2. Implement `reopensSearch` and `afterSwitch`. Check (and report) what the map, room-entry benchmarks and "known by heart" show for a room back at 0%.
3. Failing tests: a kept container is kept across runs, its contents empty each run, `holdsHowMany` holds, it isn't counted as a pocket, its contents aren't counted twice.
4. Extend `FillContainers` for kept containers. Compile, console, all EditMode tests.
5. Setup step (Lab phase 2): items Earrings (kept container, holds 2 Dense wisps) and Dense wisp (restores 40 over 6 s); tasks Take the earrings, Draw a dense wisp; *Roland Takes the Ring* gains `reopensSearch: OtherLaboratory` and unlocks the two tasks; lab finds `afterSwitch` it at 50%, Take the earrings with `needsPerception` 15 (the game's first hidden find; neither task trains a stat); switch *The Earrings* (locks Take the earrings). Text keys and the placeholder passage *The Earrings*.
6. Run the setup step; compile, tests; the in-Unity check. Add the placeholders to `PROJECT_NOTES.md`.
7. If 027d is built: add the *Earrings found* jump stage to this setup step.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ExploringTests.ReopenedSearch_StartsAtZero_KeepsEarlierFinds` | the reset and what stays |
| `ExploringTests.AfterSwitchFind_HiddenUntilFlip_ThenAtThreshold` | round two |
| `ExploringTests.ReopenedSearch_SurvivesSave` | kept correctly |
| `ContainerTests.KeptContainer_KeptAcrossRuns_EmptyEachRun` | earrings kept, wisps not |
| `ContainerTests.KeptContainer_RespectsHoldsHowMany` | two at most |
| `PocketTests.KeptContainer_NotCountedAsPocket` | pockets unaffected |

## Done when
- [x] Tests pass (744 EditMode); compile and Console clean.
- [ ] In Unity (a save past 027a's talk, or F1 → *Jump to: After the talk*): the lab's search bar reads 0%; Fill a phial and Draw on the mana stone are still offered; at 15% Take the earrings appears; taking them shows *The Earrings* and it never appears again; next run she still has them, empty; Draw a dense wisp fills two and stops; a dense wisp restores more, slower, than a phial.
- [ ] Report as 027a.

## Notes after implementation
- **Thresholds (the user, at build time):** the earrings and Draw a dense wisp at **15%**, before the gem's tasks at 25%. Take the earrings has no `needsPerception`.
- **Rounds:** `RoomFind.afterSwitch` names the round; `Simulation.SearchRoundOf` finds the flipped switch that reopened the room (two would be ambiguous, as flipped switches have no order, so it throws). A find of the current round needs the bar; an earlier round's counts as found; a later round's waits. `ReopenSearch` also zeroes the run's `ExploredAtStart`, so the Summary counts the run's search from the reset (`ExploringTests.ReopenedSearch_TheRunsReport_CountsFromZero`).
- **Save version 21** (found in review): the reset happens once, at the flip, so a save that flipped *Roland Takes the Ring* before this build would have kept its full bar and shown every phase 2 find at once. Loading a save older than 21 now starts the search again in every room a flipped switch reopens (`SaveSerializer.RestoreExplored`; `ExploringTests.AVersion20Save_PastTheReopeningSwitch_StartsItsRoomsSearchAgain`). A run saved under way keeps its own starting bar, so that one run's Summary may not list the lab.
- **What a room at 0% shows (step 2's check):** the map's and popover's search bars read the new round; Search is offered again; known-by-heart and room-entry benchmarks don't read the bar, so they're unchanged; the plan's warnings treat a queued search as possibly finding things, as before.
- **Kept containers:** `FillContainers`, `ContainerFor` and `CarriedItems` read kept items too (this run's first). What's inside stays per run. The pockets overlay lists a kept container with what she carries, not under what she knows.
- **Dense wisps go only in the earrings:** a new item flag, `onlyInContainers` (*An object in her pockets* section): no pocket room and no floor room, so drawing stops at two. Not spelled out in the spec; it's this plan's assumption made real.
- **The phase 2 tasks start unlocked** (the search shows them), not unlocked by the switch: see the spec's notes.
- Balance Sheet: *After switch* column on finds (header now "Found at %"); the switch summary says "reopens the search of …".
- Extra tests: `ReopenedSearch_KeepsAnEarlierFind_TheFirstRoundNeverReached`, `ReopenedSearch_ResetsOnce_NotEveryRun`, `TwoSwitchesReopeningOneRoom_IsAContentMistake`, `ContainerTests.AnItemOnlyForContainers_HasNoRoom_WithoutOne`, `ContainerTests.CarriedItems_ListsAKeptContainer_AndWhatsInIt`.
