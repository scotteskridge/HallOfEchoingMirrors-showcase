# 025 — Rooms known by heart speed up

**Status:** Done (in-Unity check passed, 2026-09-30)
**Left to do:** nothing. Its flat ×2 Feed tier is retired by plan 029.
**Superseded in part (decisions log 2026-09-30, *Feed your hours unlocks room speed*):** the placeholder below that says Feed keeps its flat ×2 tier and room speed needs no unlock no longer holds. Feed will unlock room speed and the ×2 tier is retired; backlog Next 14.
**Design:** GDD v0.5 §13a *Realm mastery and adaptive speed* (superseded in part: per room, not per realm; linear ramp over runs, not the visit power law) and *Three implementation traps* (kept); decisions-log 2026-09-29 *Faster base speed; rooms known by heart…* and 2026-09-30 *Known-by-heart details*. Backlog Next 6. Needs 022 and 023.

## Goal
While Clara works in a room she knows by heart, the clock runs faster, reaching full speed at ~8 runs worked there. The frontier stays at the player's chosen speed, and anything needing a decision drops back to it.

## Out of scope
- The >10× one-line summary (backlog Ideas). ×5 tier and meta currency. What *Feed your hours* finally unlocks (placeholder below).

## Design assumptions
- **Placeholder rule: linear ramp.** Room speed = 1 below `byHeartRuns`; from there it rises linearly to `roomSpeedCap` at `fullSpeedRuns`: `1 + (cap − 1) × min(1, (runs − byHeart + 1) / (fullSpeed − byHeart + 1))`. With `~`4, `~`8, cap `~`5: 1.8× at 4 runs, 5× at 8. Replaces the GDD's visit power law (Act I is 7–12 runs, so visits would never reach its range).
- **Placeholder rule: Feed your hours keeps its flat 2× tier**; room speed needs no unlock and stacks on it (user, 2026-09-30: decide after playtesting). Listed in `PROJECT_NOTES.md`.
- **Which room:** the current action's room; a trip runs at the **slower** of its two rooms (settled 2026-09-30).
- **The hold:** room speed drops to 1 from a skipped action, a refused action or a milestone until the first action that starts after it is done (changed at wrap-up: releasing on the next start did nothing for skips and arrivals, where the next action starts in the same tick). Game-made pauses already stop the clock.
- **Traps kept:** Core runs in ticks, so drain and XP per game second are untouched (trap 1). Room speed shows on the map and ribbon, never in the stats panel (trap 2). The hold is built now (trap 3).
- Clock = player tier × room speed (plan 022's base speed was replaced by halving game time, 022b: no base multiplier). Core computes room speed as a number; it never reads real time.

## Reuse
- `TickEngine.Speed` / `Advance` and the `GameController.UseSimulation` speed wiring (022's `BaseSpeed` is gone); 023's `RoomRuns`, `IsKnownByHeart`.
- `LoopState.CurrentNode` / `CurrentDestination`; events `TaskSkipped`, `ActionRefused`, `MilestoneReached`, task started.
- `MapView` room labels; the ribbon (plan 004).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Simulation.ByHeart.cs` | Edit | `RoomSpeed(room)`, `RoomSpeedNow` (slower of trip ends), `SpeedHeld` set and released by events. |
| `LoopSettings.cs` / `.asset` | Edit | `fullSpeedRuns` (`~`8), `roomSpeedCap` (`~`5). |
| `TickEngine.cs`, `GameController.cs` | Edit | Multiply by `RoomSpeedNow` each update. |
| `MapView.cs` | Edit | "×1.8" badge on by-heart rooms (tooltip). |
| Ribbon | Edit | Shows room speed while it applies, "held" when held. |
| `BalanceSheetWindow.cs` | Edit | Two new fields; read-only room-speed column in Places. |
| `game_text.txt` | Edit | `map.room_speed`, tip; `ribbon.room_speed`, `ribbon.speed_held`. |
| `PROJECT_NOTES.md` | Edit | The two placeholder rules. |

New content fields: `fullSpeedRuns`, `roomSpeedCap` (Balance Sheet). Save format change? No (`SpeedHeld` is per moment; saves happen at action boundaries, check).

## Steps
1. Failing tests for `RoomSpeed` (curve points) and trips at the slower end.
2. Failing tests for the hold; implement both in Core.
3. Engine wiring; a test that ticks per task and drain per game second don't change with room speed.
4. Map badge, ribbon text, tooltips.
5. Balance Sheet, PROJECT_NOTES, backlog.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RoomSpeedTests.BelowByHeart_IsOne` | Frontier at 1 |
| `RoomSpeedTests.Ramp_HitsCap_AtFullSpeedRuns` | Curve ends |
| `RoomSpeedTests.Trip_UsesSlowerRoom` | Settled rule |
| `RoomSpeedTests.SkippedAction_HoldsSpeed_UntilNextStarts` | Trap 3 |
| `RoomSpeedTests.RoomSpeed_DoesNotChangeDrainPerGameSecond` | Trap 1 |
| `TickEngineTests.RoomSpeed_Multiplies` | Clock wiring |

## Done when
- [x] Tests pass; compile and Console clean (597 EditMode tests, 2026-09-30).
- [x] In Unity (lower the by-heart and full-speed runs for the check): a by-heart room shows a speed badge on the map; its actions visibly race; the trip into an unknown room runs at normal speed; a skipped action shows "held".
- [x] Placeholders listed in PROJECT_NOTES.

## Notes after implementation
- The curve lives in `LoopSettings.RoomSpeedAfter(runs)` (one formula for Core and the Balance Sheet). The engine takes a `TickEngine.RoomSpeed` function that `GameController` points at `Simulation.RoomSpeedNow`, read once per frame.
- The hold is set by subscribing to the simulation's own events (skipped, refused, milestone) and released by `TaskCompleted` of the first action started after it; it resets with each new run.
- Balance Sheet: the two fields appear in Rules automatically; the Places "column" is a read-only line above the table (the curve isn't per room).
- Map: "×1.8" after the room name, with a hover tip. Ribbon: "×1.8" or "held at your speed" beside the time left.
- `TickEngineTests.RoomSpeed_Multiplies` and `RoomSpeedTests` added; 593 EditMode tests pass.
