# 031 — Stats pass: each stat gets a job skills can't do

**Status:** Done
**Design:** GDD v0.4 §6 *Stats & Mastery* (the rows for Endurance, Perception and Attunement, [OPEN] *Attunement currently does nothing* and [BUILT] *Stats influence skills through XP rate* are superseded); decisions log 2026-09-30 *Stats pass: each stat gets a job skills can't do*; backlog Next 13.

## Goal
Every stat does something a skill can't. Endurance banks kept max vitality after each run. Composure also softens a task's own extra drain. Perception reveals hidden finds and no longer speeds Explore. Attunement makes restoratives restore more, but stays asleep until a switch wakes it. A stat's learning bonus for its skills goes to this run's levels only, not mastery. A task can name the stat it trains.

## Out of scope
- How the stats row *looks* asleep, the wake moment, and the Summary's "+N vitality kept" line: UI-BACKLOG Next 10 (a later ui plan). This plan changes only the effect **text**, so it isn't wrong.
- The lab: the talk's switch that wakes Attunement, the earrings as a hidden find, the gem's gate (027 refresh).
- Retuning Explore times (playtest first), GDD edits, a dev-panel wake button (the user: tests only).
- Meta currency buying starting levels (BACKLOG-later Ideas).

## Design assumptions (all numbers `~`, Placeholder rules, list in `PROJECT_NOTES.md`)
1. **Bank gain** at every `EndLoop` (any ending) = vitality lost this run × `enduranceBankShare` 0.03 × (1 + `enduranceBankPerLevel` 0.01 × Endurance strength). Using Endurance strength *at the run's end*. Dev restarts (no `EndLoop`) bank nothing, like room counts. No cap.
2. **Vitality lost = every loss, any source:** drain, a task's extra drain, travel and training charges, action costs when on, the ring's carry cost. **One counter** feeds both the bank and Endurance XP (so charges now train Endurance too). Restoring is not negative loss.
3. **Max vitality = `vitalityMax` + kept bank**, fixed for the run. `enduranceVitalityPerLevel` is removed.
4. **Composure on extra drain:** a task's `extraDrainPerSecond` × (1 − `composureExtraDrainPerLevel` 0.03)^strength, like its growth effect.
5. **Perception:** `SearchYield` is removed (Explore speed from Wayfinding only); `perceptionSearchPerLevel` is removed; `needsPerception` gates unchanged.
6. **Attunement:** a restorative's `restoreVitality` × (1 + `attunementRestorePerLevel` 0.02 × strength), before Endurance's overflow is added. Its hue-cost job stays (dead while costs are off).
7. **Asleep:** `LoopSettings.asleepAttributes` (Attunement) lists stats that start asleep; a switch with that stat in its new `wakesAttributes` wakes it once flipped. Awake is **derived from flipped switches**, so no new save data. An asleep stat still gains XP and mastery; **all its effects use strength 0** (including its skills' learning bonus), through one choke point.
8. **Learning bonus to this run's level only:** skill mastery gets the plain XP × `masteryShare`; the run track gets XP × the stat multiplier.
9. **`trainsAttribute`:** `None` = use the kind (`StatForKind`, and Explore's special case). No task needs "override to nothing": the lab's no-stat tasks already have no-stat kinds.

## Reuse
- `Simulation.Attributes.cs`: `StrengthOf`, `MaxVitality`/`MaxVitalityAt` (:252), `GainXp` (:112), `SkillXpMultiplierFor` (:132), `ExtraDrainNow` (:235), `SearchYield` (:262), `TrainEnduranceAndComposure` (:66), `StatTrainedBy` (:57).
- `XpTrack.cs` `Gain` (split at :62). `Simulation.cs` `StartNewLoop` (:182), the tick's `vitalityLost` (:296), `EndLoop` (:234, where `CountRoomsWorked` updates kept state). `Simulation.Paying.cs` (:88 `MoveVitalityPaid`), `Simulation.Carrying.cs` (`CarryCostOf` :123), `Simulation.Restoring.cs` (`StartRestoring` :69–73), `Simulation.Exploring.cs` (:57, :83).
- `SwitchDefinition.cs` effect lists; `PersistentState.FlippedSwitches`. `SaveSerializer` upgrade chain (:500–595; 17→18 is the pattern). `RunReport` / `Simulation.Report.cs`.
- `ClaraTips.StatEffect` (:82) and `stats.effect_*` / `tips.*` keys. `BalanceSheetWindow.DrawTasks` (:356 header, :381 fields). LoopSettings fields show in the Balance Sheet automatically.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `XpTrack.cs`, `Simulation.Attributes.cs` | Edit | `Gain` takes run XP and mastery XP separately; `GainXp` boosts run XP only |
| `TaskDefinition.cs`, `Simulation.Attributes.cs`, `BalanceSheetWindow.cs` | Edit | `trainsAttribute` (default `None`); `StatTrainedBy` honours it; Tasks column *Trains* |
| `Simulation.Exploring.cs`, `LoopSettings.cs` | Edit | remove `SearchYield` and `perceptionSearchPerLevel` |
| `Simulation.Attributes.cs`, `LoopSettings.cs` | Edit | `composureExtraDrainPerLevel`; `ExtraDrainNow` softened |
| `LoopState.cs`, `PersistentState.cs`, `Simulation*.cs`, `SaveData`/`SaveSerializer`, `RunReport` | Edit | `VitalityLostThisRun` counter via one `LoseVitality` path; `KeptVitality` bank; banking in `EndLoop`; `RunReport.KeptVitalityGained`; max vitality from the bank |
| `LoopSettings.cs` | Edit | `enduranceBankShare`, `enduranceBankPerLevel`, `attunementRestorePerLevel`, `asleepAttributes`; remove `enduranceVitalityPerLevel` |
| `SwitchDefinition.cs`, `Simulation.Attributes.cs`, `Simulation.Restoring.cs` | Edit | `wakesAttributes`; `IsAwake(stat)`; effect strength 0 while asleep; restore multiplier |
| `game_text.txt`, `ClaraTips.cs` | Edit | effect and tip text for Endurance (bank), Perception (hidden finds), Composure (extra drain), Attunement (restoratives; "Asleep" while asleep) |
| `ClaraAttribute.cs` | Edit | stale doc comments ("Stamina", "Search speed") |
| `Data/LoopSettings.asset` | Edit (setup step or Inspector) | `asleepAttributes: [Attunement]`, new numbers |

Save format change? **Yes** → `SaveData.CurrentVersion` 18 → 19; upgrade step: `KeptVitality` starts at 0 (older saves had no bank; the user doesn't need old saves to play the same).

## Steps
1. **Learning bonus to run level only** — failing test, then split `XpTrack.Gain`.
2. **`trainsAttribute`** — failing test, field, `StatTrainedBy`, Balance Sheet column.
3. **Perception off Explore** — change `ExploringTests` :122/:138 to the new rule (a design change, not a weakened test: say so in the report), remove `SearchYield`.
4. **Composure softens extra drain** — failing test, `ExtraDrainNow`.
5. **One vitality-lost path** — failing tests (charge, carry cost and drain all count; Endurance XP matches); route every loss through it; `VitalityLostThisRun`.
6. **The bank** — failing tests (gain formula at each ending; next run's max; dev restart banks nothing; save round-trip and 18→19); `KeptVitality`, `EndLoop`, `StartNewLoop`, `RunReport`, remove the per-level max vitality and the per-tick `SetMax`.
7. **Asleep and Attunement's job** — failing tests; `asleepAttributes`, `wakesAttributes`, `IsAwake`, the strength choke point, the restore multiplier; set the asset (one-click setup step if the Inspector can't).
8. **Text** — effect/tip keys and `ClaraTips`; `ClaraTipsTests`; stale comments; `PROJECT_NOTES.md` placeholder list; delete the BACKLOG *Now* follow-up "plan 020 review: … if Perception shortens it partway" (moot).

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `AttributeTests.StatXpBonus_BoostsRunLevelOnly` | skill run XP × multiplier; mastery XP unboosted |
| `AttributeTests.TrainsAttribute_OverridesKind` / `_NoneUsesKind` | a Study task set to Perception trains Perception; `None` keeps the kind's stat |
| `ExploringTests.Explore_SpeedIgnoresPerception` | Explore takes the same time at Perception 0 and 20 |
| `ExtraDrainTests.Composure_SoftensExtraDrain` | extra drain × 0.97^strength; background drain unchanged by it |
| `AttributeTests.VitalityLost_CountsEverySource` | drain, a travel charge and carry cost add to `VitalityLostThisRun` and Endurance XP |
| `AttributeTests.Bank_GainsShareOfLossAtEveryEnding` | exhausted, ended early, walked out all bank the formula's amount |
| `AttributeTests.Bank_RaisesNextRunsMaxVitality` / `_DevRestartBanksNothing` | next run's max = 50 + bank; restart without `EndLoop` adds nothing |
| `SaveTests.KeptVitality_RoundTrips` / `Upgrade18To19_StartsBankAtZero` | save keeps the bank; old saves load with 0 |
| `AttributeTests.AsleepStat_GainsXpButHasNoEffect` | asleep Attunement levels up, restoratives unchanged |
| `AttributeTests.WakingSwitch_TurnsEffectsOnAtFullStrength` | after the switch flips, restore × (1 + 0.02 × strength) at once |
| `ClaraTipsTests.StatEffect_ShowsNewJobs` | effect lines for the four changed stats; "Asleep" while asleep |

## Done when
- [ ] Tests above pass, all EditMode tests pass; compile and Console clean
- [ ] In Unity: play a run to exhaustion; the next run's top bar starts above 50 by the banked amount; Attunement's chip says it is asleep; Perception's tooltip talks about hidden things; Balance Sheet shows the *Trains* column and the new LoopSettings fields
- [ ] decisions-log entry added noting what changed from this plan; `PROJECT_NOTES.md` placeholders listed; `/sync-state` run

## Numbers behind it (why the shapes, so they aren't "fixed" by accident)
- **Vitality has logarithmic returns.** The drain grows 56.25% a minute, so time to drain V ≈ 60 × ln(1 + V/134) ÷ ln 1.5625 s: 50 → ~43 s, 100 → ~75 s, 300 → ~2.6 min, 1,000 → ~4.8 min. A flat +1 per level can't be felt; the bank grows by multiples, and a big bank is safe (each doubling buys less).
- **The bank can't be farmed by dying fast:** gain scales with vitality *lost*, which a long run with restoratives makes large and a quick death keeps near max vitality. Early ≈ 60 × 3% ≈ +2; mid (≈1,000 lost, strength ~150) ≈ +75. Target from the user: +2–3 early, +30–50 mid, +100–250 late.
- **An XP multiplier adds levels, not multiples:** on the 1.085 curve, ×m XP ≈ +ln(m) ÷ 0.082 levels (strength 10 → ×1.5 → +5; 150 → ×8.5 → +26; 800 → ×41 → +46). Into mastery, and with stat mastery feeding the multiplier back, that bypasses the mastery soft cap (ruling 2026-09-27), hence run level only.
- **Why Attunement sleeps:** Instantiate trains it all through Act I (likely 10–20+ mastery by the lab). Awake, it would quietly buff phials from run 1; asleep, it arrives in the lab as a banked payoff. The same fact makes a gem gate of 3 meaningless (the 027 refresh sizes it from a playtest reading).

## Notes after implementation
- **One vitality-lost path** was already there: every loss (drain, extra drain, charges, costs, the ring) goes through the tick's single `Loop.Vitality.Drain`, so `NoteVitalityLost` just counts what it took. It is counted right after the drain (not with Endurance's XP) so a run that ends that very tick by walking out still banks it. Travel/training charges already trained Endurance.
- **`EffectStrengthOf`** is the choke point (asleep = 0). `StrengthOf` stays plain: switches, task gates and `needsPerception` read it, since an asleep stat still levels.
- **Restoratives:** `StartRestoring` checks "is enough missing" against the boosted amount, so Attunement doesn't waste a wisp; Endurance's overflow is added on top as planned.
- **Test base:** `MakeLoopSettings` sets `enduranceBankShare` to 0 (like the drain and XP rates) so tests that start several runs aren't lengthened by the default bank; the bank tests set their own. Three tests (`DrainTests`, `RunReportTests`) failed on the default until then: a design change, not a weakened test.
- **`ExploringTests`:** `Perception_ShortensSearch` became `Explore_SpeedIgnoresPerception`, and `WithPerception_AFullBar_StillGivesOneForEveryStep` became `AFullBar_GivesOneForEveryStep` (a design change: Perception no longer speeds a search).
- **`Endurance_LengthensTheVitalityBar_AndItsMasteryStartsARunLonger`** became `Endurance_DoesNotLengthenTheBarMidRun` (the per-level max is removed).
- **Also saved:** a run under way keeps `vitalityLostThisRun` (`SavedRun`), so a resumed run banks correctly.
- **Text:** chips read "+N vitality kept", "sees hidden things", "×N restoring" / "asleep"; `effect_attunement_off` is gone.
- **Pulled in from UI-BACKLOG Next 10:** the Summary's "+N vitality kept" line (`results.kept_suffix`) and an asleep stat's dimmed chip (`StatsRow`, `UiStyle.UnavailableAlpha`). The wake moment is still to do. Also set `trainsAttribute` on the lab's *Watch him* (Perception) and *Attend* (Composure), by editor tooling, as the decisions log settled; the other lab pairings wait for the 027 build.
- **Asset:** `LoopSettings.asset` has `asleepAttributes: [Attunement]` (set through the editor tooling). No switch lists `wakesAttributes` yet: the lab's talk switch (027) will, so Attunement stays asleep until then.
