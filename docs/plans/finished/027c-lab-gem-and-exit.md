# 027c — The lab, the gem and the exit: Act I's finish

**Status:** Done 2026-10-01 (built 2026-09-30, setup step 101 run and retired; the user played it and signed it off (2026-10-01))
**Left to do:** nothing.
**Design:** as 027a; GDD §12 *The three run endings* (walk out). **Content tables:** `027-lab-redesign.md` (*Tasks: phase 2*, *Task: the exit*, *Switches*).

> **Design change 2026-09-30 (decisions log *Features come in layers*): Act I has no stats.** This plan changes in three places; the rest stands:
> - **The gate is a skill gate on Crafting, not Attunement 26.** No skill gate exists yet: `TaskDefinition` only has `requiresAttributes` (stats). The Core change is a **skill requirement** alongside it, following the same pattern (a `CanStart` reason, shown before she tries it on the room offer and in the tooltip, for example "Crafting N (has M)", with text keys in `game_text.txt`), plus EditMode tests. **Placeholder rule:** the number is set so Practise the cut is needed first and the first attempt still runs her dry. Leave the existing stat-gate code alone; it's for Act II.
> - **The walk out is the first exit.** Stats waking on the first exit, and stat XP settling on a walk out (all of it on a walk out, ~50% on a collapse), are **out of scope here**; they're backlog Next 8. This plan still ends at the story beat and the Summary.
> - **The 027d *Before the gem* jump** sets a Crafting level instead of Attunement 26.

## Goal
She practises the cut and, over one or more runs, **crafts the gem** (a long task behind a visible Attunement gate that first runs her dry). The gem unlocks *Exit through the glowing mirror* at the starting mirror: a story beat, then the Summary. A friend can now play Act I to the end.

## Out of scope
- The meta-currency screen and the Summary rework; the Amber pool (the gem is a kept item and a switch only).
- Any change to how walking out keeps carried items (already built).

## Design assumptions
- The spec's 1–7. The gate (Attunement 26), 60 s, practice's XP × 3 and charge 2 × 1.15 are placeholders.
- A stat gate shows on a task **before** she tries it: in the tooltip and as the greyed reason on the room offer, never hidden.

## Reuse
- `requiresAttributes` and the `needs_attribute` reason in `Simulation.CanStart` (`CantStartForAttribute`, `Simulation.Tasks.cs:514`); `AddRoomOffer` (`Simulation.Offers.cs:67`) checks only done-reason and needs today; `ActionText.Tip` shows needs as counts but not stat gates.
- Walk-out: `Simulation.Paying.cs:143` (`walksOut` → `LoopEndReason.WalkedOut`; also `Simulation.Carrying.cs:58`); only `CarriedItemsTests` uses it; no content yet.
- `LoopEndedDuringTask` switch trigger for the first failure.
- `BlurbImporter.FixKnownRules` (`Editor/BlurbImporter.cs:106`, the note at 147–157); `reaching.asset` has `endsWhenFlipped: []`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.Offers.cs` | Edit | a room offer with an unmet stat gate shows greyed with the gate as its reason |
| `UI/ActionText.cs` | Edit | the tooltip lists stat gates ("Attunement 26 (has 23)"), in the needs style; text keys in `game_text.txt` |
| `Editor/BlurbImporter.cs` | Edit | `reaching` ends when *The Gem Is Made* flips (by asset) |
| `Editor/GreyboxSetup.cs` | Edit | *Setup/Step NN: Lab, the gem and the exit* (next free number) |

UI change: one tooltip line and one greyed reason; no layout change.
Save format change? No.

## Steps
1. Failing tests: an offer with an unmet `requiresAttributes` is greyed with the gate as its reason; met, it's offered.
2. Implement in `AddRoomOffer`; add the tooltip line. Compile, console, tests.
3. Failing test: a task with `walksOut` at the start room ends the run `WalkedOut` and flips its switch before the Summary (extend `CarriedItemsTests` or `LabRulesTests`).
4. Setup step (the gem and the exit): Crafting skill; item The gem; tasks Practise the cut (XP and escalating charge per the refreshed spec), Craft the gem (Instantiate large × 1.8215 = 60 s, Attunement 26), Exit through the glowing mirror (at `HallMirror`, `walksOut`, locked); switches *The Gem Cracks*, *The Gem Is Made* (locks craft and practice, unlocks the exit), *Home*; *Roland Takes the Ring* unlocks practise and craft; lab finds `afterSwitch` it at 25%.
5. Text keys and placeholder passages *Not Yet*, *The Gem*, *Through the Glass, Home*; the `reaching` rule; Import Blurbs.
6. If 027d is built: add the *Before the gem* jump stage to this setup step.
7. Run the setup step; compile, tests; the in-Unity check. Add the placeholders to `PROJECT_NOTES.md`; update backlog Next 7 and 8.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `LabRulesTests.StatGate_Unmet_OfferGreyedWithReason` | the gate is visible, not secret |
| `LabRulesTests.StatGate_Met_Offered` | and then usable |
| `LabRulesTests.WalkOutTask_EndsRunWalkedOut_FlipsSwitch` | the exit ends Act I's run |
| `LabRulesTests.LoopEndedDuringTask_FlipsOnFirstFailure` (if not already covered in `SwitchTests`) | *Not Yet* fires |

## Done when
- [x] Tests pass (744 EditMode); compile and Console clean.
- [ ] In Unity: after the reopened search reaches 25%, Craft the gem shows greyed with "needs Crafting 8 (has N)" until met; a first attempt runs her dry partway and shows *Not Yet*, keeping the XP; a later one completes and shows *The Gem*; next run, only with the gem, *Exit through the glowing mirror* is at the starting mirror; it ends the run as walked out, shows *Through the Glass, Home*, then the Summary.
- [ ] A cold play from a new save reaches the exit (the user's pacing check: about 7–12 runs).
- [ ] Report as 027a.

## Notes after implementation
- **The gate is `TaskDefinition.requiresSkills`** (`SkillRequirement`: skill + level), read by `Simulation.StrengthOf(skill)` (this run's level plus mastery) in `CantStartForSkill`, in both start chains after the stat gate. Text: `reasons.needs_skill` ("needs Crafting 8 (has 5)"), `actions.tip_needs_skill`. Balance Sheet: *Needs skill level* column (add or remove a gate with Select).
- **Crafting 8** (the user, at build time; I'd suggested 5).
- **Unmet, the room offer is greyed but not blocked:** Craft the gem can be queued behind practice and starts once the gate is met (`SkillGate_QueuedBehindPractice_StartsOnceMet`); pressed on its own it's refused with the reason.
- **Steps 1 and 2 changed** with the design note: no stat-gate offer or tooltip work (the stat code is untouched). The walk-out test needed no Core change. `LoopEndedDuringTask` was already covered (`SwitchTests`); added `ARunEndingMidCraft_KeepsTheXpItEarned` for "keeping the XP".
- **The exit** lives in `Tasks/Hall/` and on the Smoky Mirror's own tasks; it stays offered every run while she holds the gem (what follows the first exit is backlog Next 8).
- **Practise and Craft start unlocked** (the reopened search shows them), not unlocked by *Roland Takes the Ring*: see the spec's notes. *The Gem Is Made* is new, so its unlock of the exit reaches old saves.
- `reaching` now ends at *The Gem Is Made* (*Fix Known Blurb Rules*, run).
