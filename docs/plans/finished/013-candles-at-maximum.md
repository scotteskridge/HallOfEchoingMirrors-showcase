# 013 — Candles at their maximum

**Status:** Done
**Design:** decisions-log 2026-09-28 *The room popover (playtest)* ("Actions that can't be done this run leave the room popover"); 2026-09-28 *Playtest batch: pausing, full pockets…*. No GDD § changes.

## Goal
Once every candle in A Dark Hall is lit (15/15, or 10/10 in The Left Corridor), *Light a candle* leaves the room popover like any action done this run. It no longer stays on offer and then refuses with a message that sounds like her pockets are full. *Instantiate a candle* stays available so she can still make candles to carry on to the next room.

## Cause (found by the investigation, reproduced in memory from save slot 1)
- `CanStart` (`Simulation.Tasks.cs` ~516) refuses a task whose gives are full with `reasons.cant_hold_more` ("she can't hold any more … in her pockets or on the floor here"). That wording is for objects. *Candlelight* is a lit-candle count (not pocketed, lasts this run, max 15), so at 15 lit the player is told about pockets.
- `GivesOnlyWhatExists` (`Simulation.Tasks.cs` ~218) treats only one-of-a-kind or kept gives as done, so a this-run count at its maximum is still offered. The row even shows a stale "needs 1 Flint and steel".
- With 0–14 lit, lighting worked in every state tried (full pockets, full floor or both). **The player didn't know if the hall was fully lit**, so step 1 checks for another cause.

## Out of scope
- Renaming or rewording *Light a candle* / *Instantiate a candle* (a writing session, if wanted).
- `SupplierFor` checking only pocket room, not floor (logged in BACKLOG *Later*).
- The "needs 1 Flint and steel" reason showing when the queue would supply it automatically (existing behaviour).

## Design assumptions
- **Hidden at maximum, not greyed.** This turned out to be the existing convention already used for a one-time kept reward: `GivesOnlyWhatExists` returning true hides a task from the offers list entirely (the `hidden` branch in `DoneReason`), so there's no separate "greyed with the Show Unavailable switch" state to build for this case — extending that same check was simpler than adding one.
- Applies to **any** non-pocketed this-run count at its maximum, not just candles (so the corridor's candles too). One-of-a-kind and kept gives behave as now; a pocketed object (e.g. Wisp) blocked only by full pockets is unaffected — it still shows, refused with `cant_hold_more`, because emptying pockets can free it up again.
- New refusal text for counts, for when it's refused anyway (e.g. a queued entry after the last candle is lit): `reasons.count_at_max` = "all {max} {item} are already done this run" `[PLACEHOLDER]`.
- **Confirmed with the user:** *Light a candle* and *Instantiate a candle* are distinct actions; this bug and fix are about the former only. The latter must keep working past 15/15 lit so candles can be carried to the next room — confirmed unaffected (see Notes).

## Reuse
- `GivesOnlyWhatExists` in `Simulation.Tasks.cs` and `DoneReason` / `AddRoomOffer` in `Simulation.Offers.cs`: extend them, don't add a new check.
- `RoomFor` / `SpaceFor` / `GivesAreFull` / `FirstGive` in `Simulation.Resources.cs` and `Simulation.Tasks.cs`.
- Test fixtures: `SupplyTests` (already has *Light a candle* with max 3 in `_hall`), `OffersTests.Find`, `SimulationTestBase.MakeResource/MakeTask/Reason`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.Tasks.cs` | Edit | `GivesOnlyWhatExists`: a give's "exists" check now also covers a non-pocketed this-run count at `RoomFor <= 0`, not just a kept resource. `CanStart`: the `GivesAreFull` refusal now checks `give.IsPocketed` and uses `count_at_max` for a count, `cant_hold_more` for an object |
| `Assets/Text/game_text.txt` | Edit | new key `reasons.count_at_max` (placeholder, marked in the file) |
| `Assets/Tests/EditMode/SupplyTests.cs` | Edit | 3 new cases (below) |

No new content fields. Save format change: **No**.

## Steps
1. ~~Reproduce first~~ — done via Unity MCP against the player's real save (see Notes): the save had moved past the incident (0 lit), so the state was forced via reflection instead. Confirmed directly against the real `LightTheCandles`/`Candlelight` content, not just the test fixture.
2. Write failing tests (below) — done.
3. Change `GivesOnlyWhatExists` so a maxed this-run count hides the offer — done.
4. Change the `CanStart` refusal and add the text key — done.
5. Run all EditMode tests — done, 470/470 passed, `FloorTests.AnActionWithNoRoomForWhatItGives_…` unchanged.
6. Play test in the running scene (not just a fresh sim) — done via Unity MCP, see Notes.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `SupplyTests.LightingACandle_WhenAllAreLit_IsRefusedWithTheCountReason` | refusal names the count, not pockets |
| `SupplyTests.LightingACandle_WhenAllAreLit_LeavesThePopover` | row hidden from `TasksAt` at the maximum |
| `SupplyTests.MakingACandle_StillWorks_WhenAllAreLit` | *Instantiate a candle* (`_makeCandle`) still offered at the maximum |
| `FloorTests.AnActionWithNoRoomForWhatItGives_…` (existing) | object wording unchanged |

(Dropped the separate `OffersTests` cases from the draft: `SupplyTests`' own `_light`/`_lit` fixture already covers "at max" vs "below max" without a second fixture.)

## Done when
- [x] Tests above pass; compile and Console clean
- [x] Confirmed against the real save/content via Unity MCP: `IsDoneForThisRun(LightTheCandles)` is `True` at 15/15, `TasksAt(Junction)` no longer lists it, `InstantiateACandle` still does
- [x] In Unity (the user, in a normal play session): light all 15 candles in A Dark Hall → *Light a candle* disappears from the popover, *Instantiate a candle* is still there and puts a candle in her pockets; same in The Left Corridor at 10
- [x] decisions-log entry added
- [x] The user rewrites `reasons.count_at_max`

## Notes after implementation
- The player's save (slot 1) had already moved past the incident by the time this was investigated — 0 candles lit, in a different room. Step 1's reproduction instead forced `Candlelight` to 15 via reflection on a `Simulation` built from that same save's real content (same `GameContent`/`LoopSettings` assets), confirming the exact refusal text the player must have seen: "she can't hold any more Candles lit in A Dark Hall, in her pockets or on the floor here" — matching "no room in inventory" closely enough to be confident this was the bug, even without an exact step-by-step repro.
- Turned out to be simpler than planned: no new "greyed, Show Unavailable" UI state was needed. `GivesOnlyWhatExists` already had a "hide once earned" path (for one-time kept rewards); extending its exists-check to cover a non-pocketed this-run count reused that path exactly, so `Simulation.Offers.cs` needed no changes at all.
- User clarified mid-plan that *Light a candle* and *Instantiate a candle* are distinct actions (the latter must keep working past 15/15, to carry candles to the next room) — confirmed via Unity MCP against the real content that `InstantiateACandle` is unaffected.
- **Reviewer caught a regression:** the first pass used `!give.resource.IsPocketed` to spot a "plain count", but `IsPocketed` is also false for a pocket-adder (Satchel) and a container (Empty phial) — neither is a candle-like count, but both would have started hiding themselves once held. Fixed with a narrower `IsPlainCount` helper (`!IsPocketed && !IsContainer && addsPockets == 0`), used in both `GivesOnlyWhatExists` and `CanStart`'s wording choice; re-confirmed via Unity MCP against the real Satchel and Empty phial assets that they're unaffected, and the candle fix still holds. Also renamed/strengthened `MakingACandle_StillWorks_WhenAllAreLit` to actually play the action, and updated two doc comments (`IsDoneForThisRun`, `DoneReason`) the reviewer found now understated.
