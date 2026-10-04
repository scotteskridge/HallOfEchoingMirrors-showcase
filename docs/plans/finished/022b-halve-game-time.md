# 022b — Halve every game-time number

**Status:** Done (the tool was run 2026-09-30; 557 EditMode tests pass on the halved content)
**Design:** decisions-log 2026-09-29 *Faster base speed*; replaces plan 022's ×2 clock (rolled back 2026-09-30: the ×2 pace felt right, but the game clock no longer matched real time). GDD §4 *Game speed is earned*.

## Goal
Everything plays at the pace the user liked at ×2, but game seconds are real seconds again at 1×: one time scale for balancing, the run timer and the notes.

## Out of scope
- Any change to the tick rate, `TickEngine` or speed tiers (022's rollback already restored them).
- Real-time presentation numbers (UI timings, blurb gaps in real seconds): they never scaled with the game clock.
- Re-balancing. This is a pure rescale: every ratio stays the same.

## Design assumptions
- **Factor 0.5 on everything that counts in game seconds**, so a run lasts half as long in seconds, with the same number of actions, levels and XP.
- **Per-second rates double, per-task amounts stay.** Halving a task's time while keeping "XP per second of task" would halve its XP, so the XP-per-second setting doubles to keep XP per task identical. Likewise Composure XP per second, carry cost per second and restoration speed. The drain is adjusted too: see below.
- **The drain is adjusted (user confirmed 2026-09-30):** `vitalityDrainPerSecond` 0.5 doubles and `drainGrowthPerMinute` 25% compounds twice as fast (1.25² − 1 = 56.25%), so a run still holds the same actions.
- **Blurbs run on real seconds (user decided 2026-09-30),** so they stay readable at any speed. Today the timer (`BlurbTeller`, counted in ticks) and the bucket cooldowns (`BlurbPicker.OnCooldown`, simulation seconds) follow game time, so they'd fire S× as often at ×S; only `minRealSecondsApart` is already real. Their numbers (3–8 s, 20 s cooldown) don't change; the clock does:
  - `BlurbTeller` counts the wait in real time (`Time.unscaledTime`), paused while the game is paused.
  - `Pick` gets the real-time "now" passed in, so Core stays plain C# and `BlurbPicker`'s cooldowns compare real seconds.
  - Balance Sheet labels and tooltips say "real seconds". `minRealSecondsApart` stays as a guard.
  - Blurbs are presentation, so this may differ run to run; it never affects the simulation or saves.
- Anything in seconds is a candidate, including things I can't see from code alone (benchmark thresholds, blurb timings in game seconds). The audit step lists them before anything changes.

## Reuse
- *Hall of Echoing Mirrors → Balance Sheet* (`BalanceSheetWindow`): where the user balances; shows every field below.
- `SimulationTestBase` helpers and `TutorialContentTests` (check real content numbers).
- `SaveSerializer` / `SaveData`: to confirm nothing saved is in seconds.

## Audit result (step 1, 2026-09-30)
Searched every seconds/per-second field in code and in `Assets/Data`. **Scaled (game time):**
- **Exception (user, 2026-09-30): Feed Your Hours keeps its 20 s**, because its story text says twenty seconds. The tool skips it.
- `TaskDefinition.durationSeconds`: 32 tasks (Explore 15, Travel 30, PickUp 2, PutDown 1, Feed Your Hours 20, Fill a Bottled Well 15, Gather a Wisp 2, Instantiate Candle 4 / Phial 60 / Satchel 60 / Flint and Steel 45, Light a Candle 6 ×2, Search corridors 20 ×2, Take the Ring 30, Attend 10, Clear the Bench 8, Cut the Stone 20, Draw on the Mana Stone 5, Search the Laboratory 8, Study the Tome 10, Take the Tome 30, Watch Him 15, Chase Roland 20). Halved: Put Down 0.5, Gather a Wisp 1, Pick Up 1, Flint and Steel 22.5. All above the 0.1 s minimum.
- `ResourceDefinition.restoreSeconds`: 20 items (5 → 2.5; Bottled Well 7 → 3.5). The vitality given back stays the same, so it comes back twice as fast.
- `ResourceDefinition.carryCostPerSecond`: Roland's ring 0.5 → 1. (Story text `09_the_ring.txt` reads it through `{cost per second}`, so it updates itself.)
- `LoopSettings`: `vitalityDrainPerSecond` 0.5 → 1, `drainGrowthPerMinute` 0.25 → 0.5625, `xpPerSecondOfTask` 0.75 → 1.5, `composureXpPerSecond` 0.05 → 0.1, `composureXpPerSecondCarrying` 0.1 → 0.2.

**Not scaled:** task `cost` (a total spent over the task, not a rate; and action costs are switched off), room `Way`/modifier `time` and `cost` (multipliers around 1, not seconds), `exploresToFill` (a count), skill and mastery speed multipliers, `xpReward` (all 0, so XP follows the formula), the `MapStyle` UI timings (real time already), and the switch and skill assets (no seconds fields). Save files hold ticks and progress, not balance seconds, so no save change is expected (the rescale test and a save round-trip will confirm).

**Found in text, for the user:** `Assets/Story/16_all_twenty_five_alight.txt` says *"Feed your hours to the flames costs fifty vitality and twenty seconds"*. After halving it takes ten. This is your prose, so I won't edit it; decide the wording. The UI's own time text is computed, so it follows.

## Changes (the audit list, as applied)
| Field (asset) | What happens |
| --- | --- |
| `TaskDefinition.durationSeconds` (every task) | × 0.5 |
| `ResourceDefinition.restoreSeconds` | × 0.5 |
| `ResourceDefinition.carryCostPerSecond` | × 2 |
| `LoopSettings.vitalityDrainPerSecond` | × 2 |
| `LoopSettings.drainGrowthPerMinute` | 0.25 → 0.5625 (compounds twice as fast) |
| `LoopSettings.xpPerSecondOfTask` | × 2 |
| `LoopSettings.composureXpPerSecond`, `…Carrying` | × 2 |
| `BlurbLibrary` min/max/cooldown, `BlurbBucket.cooldownSeconds` | numbers unchanged; clock becomes real seconds (see assumptions). Edit `BlurbTeller.cs`, `BlurbPicker.cs`, tooltips, Balance Sheet labels; update `BlurbTests` |
| Milestone times | no data to scale: they're timed live from the run; plan 021's benchmark figures (draft) to be updated |
| Notes, decisions log, plan text quoting seconds | update the quoted numbers |

New content fields: none. Save format change? No, unless the audit finds a saved value in seconds (then bump `SaveData.CurrentVersion`).

## Steps
1. **Audit:** list every field above and any I've missed in `Assets/Data`; show the user the before/after list before applying.
2. **Test first:** a test that a scaled fixture produces the same number of actions, XP and run length per run as the unscaled one (proves it's a pure rescale).
3. **One-click tool:** *Hall of Echoing Mirrors → Tools → Halve Game Time* (asks to confirm, is undoable, refuses to run twice).
4. **Run it**, then run all EditMode tests; fix tests that quote real content times (never weaken them: update the expected numbers with the reason).
5. **Docs:** decisions-log entry, update 021 and quoted times in `PROJECT_NOTES.md` and `docs/`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| New rescale test | Same actions, XP and run length at half the times |
| `TutorialContentTests` | Real content still consistent |
| Existing suite | Nothing else moved |

## Done when
- [x] Tests pass; compile and Console clean.
- [x] In Unity: a new game at 1× feels like the old ×2; a 2 s action before is now 1 s on the run timer.
- [x] A full run lasts about half as many seconds as before, with the same actions, levels and XP.
- [x] decisions-log updated.

## Notes after implementation
- Built: `GameTimeRescale` (logic) and `GameTimeRescaleCommand` (menu, Feed Your Hours kept by asset path, one-shot: delete once run and committed), marker `LoopSettings.gameTimeRescaled`; `GameTimeRescaleTests` (pure rescale: same actions and XP in half the seconds, refuses to run twice). Blurbs: `BlurbPicker.Pick(..., nowRealSeconds)` with cooldowns keyed by Simulation and loop; `BlurbTeller` has a real-time clock that stops while paused; tooltips and Balance Sheet labels say real seconds.
- Composure multiplies the drain's growth *rate*, so the rescale is exact only at Composure 0; at high Composure it differs by about 1%.
