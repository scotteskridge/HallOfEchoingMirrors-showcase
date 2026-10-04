# 041 — Stats rework: Act I without stats, the first exit wakes them, mastery uncapped

**Status:** Done 2026-10-01 (built, tested, playtested by the user)
**Left to do:** nothing. The user's check (2026-10-01, with the *Gem in hand* jump stage, setup Step 103): stats do nothing in Act I; after the first walk out they wake. A run can't reach the exit on its own at current balance: backlog *Act I balance pass*.
**Design:** GDD §4 (*Mastery has no cap*), §6 (*Stats & Mastery*), §12 *The three run endings*; decisions log 2026-09-30 *Features come in layers*, 2026-09-30 *Stats pass* and *Stats pass built*, 2026-10-01 *Endurance banks*; the user's answers 2026-10-01 (this plan's session, listed under *Design assumptions*)
**Pillar:** 3 *Every run teaches something* and the GDD §14 layering (no choices → one choice → a choice that persists): Act I teaches skills, switches and items; walking out is the first choice that persists. Fights none.

## Goal
In Act I Clara has no stats: nothing earns stat XP, no stat effect applies, Endurance banks nothing. Her first walk out through the glowing mirror wakes all five stats at zero; from then on a walk out settles all of a run's stat XP into mastery and a collapse or *End run* settles 50%. Mastery has no cap. Backlog items: *Act I without stats*, Next 8 (*Walking out, and stats waking*), *Remove the mastery cap*, *Retire PoolSettings.name / vitalityName*.

## Out of scope
- All UI-lane work (listed under *Follow-ups*), except the compile-forced edit to `ClaraTips.cs` in step 2 (the user's OK, 2026-10-01).
- The Hub, Act II content, meta currency, the gem giving the empty Amber pool.
- A one-off starting amount when stats arrive (stays [PROPOSED]; the user chose zero).
- Changing any balance number (the user tunes; see *Act I rebalance*).
- Keeping old saves playable (the user, 2026-10-01: start a new save).

## Design assumptions
- **Settled by the user today** (log at wrap-up): stats arrive at zero; the Endurance bank is off until stats wake (max vitality stays 50 all Act I); *End run* settles like a collapse; stat mastery is held during a run and settles at its end (skill mastery unchanged, still live); after the first exit runs carry on at the Smoky Mirror with stats awake and the exit offered every run (already built: the gem is kept forever); old saves needn't upgrade cleanly.
- **Placeholder rule:** a collapse or *End run* settles `collapseStatXpShare` = 0.5 of the run's stat XP into mastery (new `LoopSettings` field, Balance Sheet *Rules*).
- **Reuse "asleep" for "no stats":** plan 031's asleep stats already switch effects off until a switch's `wakesAttributes` flips. This plan makes an asleep stat also earn **no XP** (supersedes plan 031's "an asleep stat still gains XP": the user chose no stats over asleep-but-earning, 2026-09-30). Data: `asleepAttributes` = all five; the *Home* switch (the first walk out) wakes all five; *Roland Takes the Ring* wakes nothing. Awake stays derived from flipped switches, so no new save field.
- **Order at a run's end** (Claude's call): bank vitality with the Endurance the run had, then settle stat mastery. A dev restart (no `EndLoop`) settles nothing, as it banks nothing.
- **Settling uses `Loop.AttributeXp`** (already saved mid-run) × `masteryShare` × the ending's share. No new run field.
- **No mastery ceiling** beyond `AttributeMath.MaxLevel` (999), which only bounds the curve's loop and can't be reached. Mastery XP is a `float`: past ~16 million XP (mastery ~140) small gains round away. The user expects numbers never to get that large (2026-10-01), so `float` stays; step 2's test still checks it, and only if it fails, widen mastery XP to `double` (inside the same save bump).
- **A pool setting with no hue** now fails loudly at simulation start (it would have no name). All seven shipped pools have hues.

## Reuse
- `Simulation.IsAwake` / `EffectStrengthOf` (`Simulation.Attributes.cs:42–58`): the one choke point for effects, already 0 while asleep. Bank, Composure, Attunement, overflow and the skills' learning bonus all go through it.
- `LoopSettings.asleepAttributes`, `SwitchDefinition.wakesAttributes`: the sleep/wake data (plan 031).
- `Simulation.EndLoop` (`Simulation.cs:250`): every ending passes here; `BankVitality` already runs per ending.
- `XpTrack` (`_masteryShare`, `Gain`): stat and skill XP; gains a deferred-mastery mode for stats.
- `LoopState.AttributeXp` (saved as `attributeXp`): the run's stat XP to settle.
- `SaveSerializer.Upgrades.cs` ladder (`if (data.version < N)`); `CurrentVersion` 22.
- Balance Sheet *Rules* draws every `LoopSettings` field generically, so the new field gets its row with no column code.
- Stat gates are already skill gates (Craft the gem: Crafting 8; earrings ungated; no task uses `requiresAttributes` or `needsPerception`): nothing to convert. The gate code stays for Act II.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `LoopSettings.cs` | Edit | remove `PoolSettings.name`, `vitalityName`, `masteryMaxLevel`; add `collapseStatXpShare` (tooltip, `[Range(0,1)]`) |
| `Simulation.cs` | Edit | pools built from hue only (fail on `Hue.None`); `EndLoop` settles stat mastery after banking |
| `Pool.cs` | Edit | name from hue only (keep a constructor for code-made test pools if tests need it) |
| `XpTrack.cs` | Edit | no cap (`Mastery`, `MasteryProgress`, `Gain` lose the cap); deferred mastery for stats plus `SettleMastery(key, xp)` |
| `Simulation.Attributes.cs` | Edit | drop `MasteryCapOf`; `GainXp(stat)` does nothing while asleep; `BankVitality` returns 0 while Endurance is asleep; `SettleStatMastery(reason)` |
| `Simulation.Report.cs` | Edit | mastery before/after without a cap; report built after settling |
| `PersistentState.cs` | Edit | remove `AttributeMasteryCapBonus` |
| `SaveData.cs`, `SaveSerializer*.cs` | Edit | drop `expertiseCapBonus`; version 22 → 23 |
| `UI/ClaraTips.cs` | Edit (compile-forced) | delete the two "at max" branches and `AtMaxNote`; keys left for the UI lane |
| `Editor/GreyboxSetup.cs` | Edit | *Setup → Step NN: Act I without stats* (next free number): `asleepAttributes` all five; *Home* wakes all five; *Roland Takes the Ring* wakes none |
| `Data/LoopSettings.asset`, `Switches/Home.asset`, `Switches/RolandTakesTheRing.asset` | Edit (by the setup step) | as above; old `name`/`vitalityName`/`masteryMaxLevel` lines drop on the next save |

New content fields: `LoopSettings.collapseStatXpShare` (0.5) appears in the Balance Sheet's *Rules* section automatically. Removed: `masteryMaxLevel`.

Save format change? **Yes** → `CurrentVersion` 22 → 23. Upgrade step: drop `expertiseCapBonus` (and, if step 2 needs it, read float mastery XP as double). Nothing else is migrated: **start a new save** after this lands. Old saves load, but keep the Act I stat XP, stat mastery and banked vitality they earned before.

## Steps
1. **Pool names.** Edit `DrainTests.APoolsName_…` / `VitalitysName_…` and `SimulationTestBase.HuePool` (drop the settings-name setup; assertions stay); add `DrainTests.APoolSettingWithNoHue_FailsLoudly` (failing). Remove the two fields and `ReplacePoolsWithSevenHues`' name line; build pools from the hue.
2. **Mastery uncapped.** Failing tests `AttributeTests.Mastery_KeepsRisingPastTheOldCap` (stat and skill past 150) and `XpTrackTests.SmallGains_StillCount_AtHighMastery`. Retire `AttributeTests.Mastery_StopsAtItsMaximum`, `AttributeTests.RaisingAStatsMasteryMaximum_LetsItGrowAgain`, `ClaraTipsTests.AtMaxMastery_TheTipSaysSo`; edit `AttributeTests.TheCodeDefaults_MatchTheAgreedNumbers` (drop the cap assertion) (all approved 2026-10-01). Remove the cap from `XpTrack`, `Simulation.Attributes.cs`, `Simulation.Report.cs`, `LoopSettings`, `ClaraTips.cs`. Widen to `double` only if the precision test fails.
3. **Save 22 → 23.** Edit `SaveTests.UnlockedTasks_AndTheCapBonuses_SurviveSavingAndLoading` (keeps the unlocked-tasks check; rename `UnlockedTasks_SurviveSavingAndLoading`) and `SaveTests.SavedHuesAndAttributes_ThatNoLongerExist_…` (drop its cap-bonus entry) (approved). Failing test `SaveTests.Upgrade22To23_LoadsWithoutTheCapBonus`. Remove `AttributeMasteryCapBonus` and `expertiseCapBonus`; bump; add the step.
4. **Asleep stats earn nothing; no bank while Endurance sleeps.** Failing tests in `AttributeTests`: `AsleepStat_GainsNoXp` (task XP, Endurance from loss, Composure per second), `AsleepEndurance_BanksNothing`, `AsleepStats_GiveSkillsNoLearningBonus`. Replace `AsleepStat_GainsXpButHasNoEffect` with `AsleepStat_HasNoEffect` (its XP half is the rule changing; approved by the user 2026-10-01). Gate `GainXp(stat)` and `BankVitality`.
5. **Stat mastery settles at the run's end.** Failing tests: `StatMastery_HeldDuringTheRun` (mastery unchanged mid-run, run level rises), `WalkOut_SettlesAllStatXp`, `Exhausted_SettlesTheCollapseShare`, `EndRun_SettlesTheCollapseShare`, `DevRestart_SettlesNothing`, `SkillMastery_StillRisesLive`, `RunReport_ShowsSettledStatMastery`, `MidRunSave_ThenLoad_StillSettles` (`RunSaveTests`). Add the deferred mode, `SettleStatMastery`, `collapseStatXpShare`.
6. **The first exit wakes the stats.** Failing tests in `LabRulesTests`: `FirstWalkOut_WakesAllFiveStats_AtZero` (that run settles nothing: asleep all run), `NextRun_AfterTheExit_StatsEarnAndTheExitIsOffered`, `SecondWalkOut_IsANormalEnding` (no second *Home* passage). `ShippedRulesTests.ActI_StartsWithEveryStatAsleep_AndHomeWakesThem` checks the assets. Write the setup step; run it.
7. **Full EditMode pass;** check `DevJumpTests` and the `Assets/Data/Dev/` jump stages (a stage that sets stat levels now sets them on asleep stats: harmless, but say so) and `ShippedLabTests` (the lab's pacing without the Attunement wake).

## Tests
The tests named in each step, plus the whole EditMode suite after each step (26 test files touch stats).

## Act I rebalance (for the user, in *Hall of Echoing Mirrors → Balance Sheet*; no numbers changed here)
What Act I loses: max vitality no longer grows run to run (bank off); Composure no longer slows the drain's growth, Feed's extra drain or the ring's bleed; no skill gets a stat learning bonus (Wayfinding, Gathering, Instantiate, Invoking, Studying, Convincing learn at base rate); Endurance's overflow tolerance is gone; Attunement's +2% restore after the talk is gone.
- **Rules:** `vitalityMax` (50), `vitalityDrainPerSecond` (1), `drainGrowthPerMinute` (0.5625): the run's length now comes from these alone. `statSkillXpPerLevel` matters only after the exit. New `collapseStatXpShare` (0.5).
- **Tasks:** `Time ×` and `XP ×` per task, the escalating charges (Travel 3 × 1.08, Practise the cut 2 × 1.15), `requiresSkills` on Craft the gem (Crafting 8): the gem gate was sized with stats' learning bonuses on.
- **Items:** restore amounts of wisp, phial of memory, dense wisp; the ring's carry cost (1 a second, now with no Composure softening).
- **Skills:** `learnsFasterWith` has no effect in Act I; Crafting is unchanged (it had no stat).
A cold run to the exit is the check (the 7–12 runs target, 027c).

## Done when
- [x] Tests above pass; compile and Console clean; tests that ran listed with results
- [x] *Setup → Step NN: Act I without stats* run; a new save: the stats earn nothing and max vitality stays 50 across runs; after the first walk out, the next run's stats rise and the Summary shows stat mastery settled (all on a walk out, half on a collapse)
- [x] decisions-log entry (today's answers; supersedes plan 031's "asleep still gains XP"); GDD edits proposed: §12 *Walking out and meta currency unlock together* (stale since 2026-09-30), §6 stat table (stats exist from Act II); `PROJECT_NOTES.md` placeholder list gains `collapseStatXpShare`; `docs/BACKLOG-later.md` *Remove the mastery cap: soft caps instead* removed (done here); changelog line

## Risks
- **The UI shows asleep stats as zeros** until the UI lane hides the row: the game is playable but looks odd in Act I.
- `ClaraTips.cs` edit collides with UI-lane tooltip work (ui-036): build when the UI lane isn't editing tooltips.
- If `GameTextTests` checks every key is used, the two orphaned "at max" keys fail it: then remove them from `game_text.txt` in this batch (UI-lane text; say so).
- Act I may become too hard without the bank and Composure until the user rebalances.

## Follow-ups (other lanes and sessions)
- **UI lane:** hide the stats row (and the Summary's stat lines, "+N vitality kept") while every stat is asleep, using `IsAwake`; remove keys `tips.xp_at_max`, `tips.mastery_at_max`; the Summary shows settled stat mastery and which share applied. Then the separate *stats arrive* moment (backlog Next).
- **Balance pass (the user):** the rows above.
- **Clean-up ideas:** both done in the same session (the stale Perception comments, and `SeesWell` now reads `EffectStrengthOf`).

## Notes after implementation
- Built as planned. `XpTrack` got a `defersMastery` flag (stats) and `SettleMastery`; `Simulation.SettleStatMastery` runs in `EndLoop` after `BankVitality`.
- Existing tests that read a stat's mastery rising live now check it is held until the run ends (`FinishingATask_…AndTheSkillsMastery`, `Levels_ResetEachLoop_ButMasteryIsKept`); no test was weakened.
- `AsleepStats_GiveSkillsNoLearningBonus` already existed as `AnAsleepStat_HasNoLearningBonusForItsSkills`, so it was kept rather than duplicated.
- Float XP precision held (`SmallGains_StillCount_AtHighMastery`), so mastery XP stays `float`.
- Review fixes: the first walk out no longer banks vitality (Endurance counts only if a switch from an earlier run woke it, `WasAwakeAtRunStart`), and settled stat mastery re-checks stat switches.
- Setup step 102 was run and then removed from `GreyboxSetup.cs`; `LoopSettings`, `Home` and `RolandTakesTheRing` assets changed (the shipped `masteryMaxLevel` 150 and the pool/vitality names dropped out).
- Added for the user's check: the dev jump stage *Gem in hand* (`Assets/Data/Dev/GemInHand.asset`, setup Step 103, test `ShippedLabTests.GemInHandStage_PutsTheExitOneActionAway_AndWalkingOutWakesTheStats`), because Act I's balance keeps the exit out of reach in play.
