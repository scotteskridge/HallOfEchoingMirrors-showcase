# 030b — The escalating charge: travel and training verbs

**Status:** Done (2026-09-30; the playtest check passed). Numbers changed from the draft: see the notes at the end.
**Design:** decisions-log 2026-09-30 *A cost curve that new verbs default to*; Balancing Formulas doc v0.2 §4 (*The escalating charge*), §9, invariant I7; GDD §12 *Traversal economy*. Part 2 of 2.

## Goal
A verb can carry a flat vitality charge that grows with each use this run and resets each run, paid even while action costs are off. Travel uses it: every move between rooms this run costs a little more, and skill can't erase that. Training verbs (none shipped yet; the lab's practise verb will be first) get the same rule.

## Out of scope
- A "next: X" line in tooltips (UI later list); the planning screen's projections (ui-024d).
- The practise verb itself (plan 027b/c).
- Hall edges costing ×2 (`hallMultiplier`, open).

## Design assumptions
- **Placeholder rule:** Travel's charge starts at 1 vitality, growing ×1.1 a move (the user, 2026-09-30). It replaces Travel's flat 5 on the pools (Travel's `cost` becomes 0).
- The count is **completed uses of that verb this run**. Every move uses the one Travel asset (`GameContent.travelVerb`), so "all moves" and "this verb" are the same count; confirm no other task moves her. An interrupted use doesn't count.
- The price is fixed when the use starts (charge for use n = start × growth^(n−1)) and paid per tick like other costs.
- The charge always comes out of **vitality**, and Composure doesn't soften it: I7 says it must stay whatever her stats.
- Derived from `LoopState.Steps`, which is already saved: **no save format change**.

## Reuse
- `Simulation.Paying.cs`: `Charges`, `EffectiveCosts`, `SpendCurrentTaskCost`; the charge joins the price there.
- `Simulation.PriceOf`: tooltips (`ActionText.Tip`, `RoomPopover`, `StopCard`) read it, so the growing charge shows with no UI code.
- `LoopState.Steps` / `CompletedStep` for the count.
- `SimulationTestBase` (`MakeTask`, `RunLoop`, `SaveAndLoad`).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `TaskDefinition.cs` | Edit | `escalatingCharge` (vitality, 0 = none), `chargeGrowth` (×, default 1) |
| `Simulation.Paying.cs` | Edit | `EscalatingChargeOf(task)`; added to the price, always charged, to vitality |
| `LoopState.cs` (or `Simulation`) | Edit | `UsesThisRun(task)` from `Steps` |
| `BalanceSheetWindow.cs` | Edit | Tasks: Charge, ×Growth columns |
| `Data/Tasks/Common/Travel.asset` | Edit (via a setup step) | cost 0, charge 1, growth 1.1 |

Save format change? No.

## Steps
1. Failing tests (below).
2. Fields, `UsesThisRun`, the charge in pricing and paying.
3. Setup step: *Hall of Echoing Mirrors → Setup → Travel Escalating Charge*; run it.
4. Balance Sheet columns; playtest: hover Travel after a few moves and see the cost rise.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `EscalatingChargeTests.FirstUse_ChargesStart` | n = 1 |
| `EscalatingChargeTests.NthUse_ChargesStartTimesGrowthToNMinus1` | growth |
| `EscalatingChargeTests.ResetsNextRun` | per run |
| `EscalatingChargeTests.CountsEachVerbSeparately` | training verbs don't share travel's count |
| `EscalatingChargeTests.ChargedWhileActionCostsOff` | always charged |
| `EscalatingChargeTests.GoesToVitality_NotSoftenedByComposure` | I7 |
| `EscalatingChargeTests.InterruptedUse_DoesNotCount` | completed uses only |
| `EscalatingChargeTests.CountSurvivesSaveAndLoad` | no format change needed |
| `EscalatingChargeTests.PriceOf_ShowsCurrentCharge` | tooltips see it |

## Done when
- [ ] Tests above pass; compile and Console clean
- [ ] In Unity: run the setup item; in play, Travel's tooltip cost is 1, then 1.1, 1.21 ... and resets next run
- [ ] `PROJECT_NOTES.md` placeholder list gets Travel's 1 × 1.1

## Notes after implementation
- Built 2026-09-30 (status: built, playtest check outstanding). The numbers changed from the draft at the user's word: Travel's charge **3 vitality, growing ×1.08 a move** (1 × 1.1 was too small beside the drain; 5 × 1.2 is far too steep, about 160 by move 20), still placeholders. Travel's flat `cost` is 0 and the charge is set on `Travel.asset` through Unity (no setup step needed).
- The user asked for travel *time* to grow instead; chose to keep the vitality charge (skill speed would cancel a growing time).
- One difference from the draft: the room's and held items' cost multipliers (the mana stone's warmth halves Travel's cost) still scale the charge; stats (Composure, Attunement) never do.
- Feedback: the tooltip cost line shows the current charge, and a new line says "Each go this run costs 20% more than the last. Done N so far." (`actions.tip_charge_rises`). Nothing else on screen yet (the "next: X" line and planning projections are still out of scope).
- Follow-ups built the same day (the user's ask): queued trips show their projected charge ("· costs 3.5 Vitality", `Simulation.PlannedTripCharge`, counting the trips queued before them); the run header's clock reads "Run time 1:23 · Moves 7" (tooltip: "Moves this run: 7…"); the Summary's detail line ends "· Moves: 9, costing 41 vitality". The vitality tally is saved with a run under way (**save version 18**, `moveVitalityPaid` and `SavedCost.charge`). No scene layout changed: the counter shares the clock label.
- 637 EditMode tests pass (13 new in `EscalatingChargeTests`).
