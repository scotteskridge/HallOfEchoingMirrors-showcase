# 055 — Act I playtest batch: candles every run, the lab's talk, the Summary's first button

**Status:** Done 2026-10-02 (built and play-checked).
**Design:** GDD v0.5 §12 *The memory lab: Act I's finish*, §12a *Act I's arc to the lab*, §13a; decisions log 2026-09-29 *The Mirror's Laboratory*, 2026-09-30 *Planning is earned by Feed your hours*; the user's answers 2026-10-02 (this session, after the cold playtest)
**Pillar:** 1 (agency: invest in insight or push the talk), 3 (every lab visit moves her forward), 2 (the corridor is a routing cost every run, not a one-off)
**Lane:** Features, as one batch (the user's call, 2026-10-02: no other lane runs meanwhile). Builds on this session's uncommitted balance work (drain growth 0.4, XP 1.8, Time × rounding, Feed trains Invoking, earrings in the first lab search with 10 slots, the drain-aware restore rule); one commit at the end.

## Goal
The corridor's 10 candles must be lit again every run to go deeper; the lab's first half stops being a checklist: *Talk to Roland* is one long push made shorter by kept insight, which three actions earn; and before planning is earned the Summary offers one honest button.

## Out of scope
- The lab's second half (earrings, practice, the gem) and the exit: unchanged.
- A story beat when she collapses mid-talk (like *The Gem Cracks*): backlog idea.
- Retiring the now-unused Understanding and Steady hands items, and the orphaned Clear the bench, Take the tome, Cut the stone: clean-up list.
- Showing max restore per second, and the talk's insight speed-up on screen beyond what tooltips already show: `docs/UI-BACKLOG.md`.
- Rewriting story text; the earrings' "two at most" lines (the user or `/writing`).

## Design assumptions (settled by the user 2026-10-02 unless marked)
- **Candles every run:** the Left Corridor's way to the Dark Corridor needs 10 *Candles lit in the Left Corridor* this run. *All twenty-five alight* still fires once (story, unlocks Feed, opens the way for good); after that the way is open but needs the candles each run. Refusal reads as any missing need ("needs 10 Candles lit…").
- **The talk resets:** it's one long task; a run that ends mid-talk loses its progress (how every task already works). Insight is what carries over.
- **Insight from three sources:** *Watch him*, *Study the tome* and *Attend* each give 1 kept insight, once a run (Study becomes once a run). *Talk to Roland* needs only the ring (it still takes it); its Understanding, Steady hands and 3-insight needs go.
- **Insight speeds the talk** through its existing `easierWith` (time × per insight held). **Placeholder numbers**, set in step 5 from the probe: talk Time × 0.911 → ~2.7 (about 24 s → ~72 s), time per insight 0.9 → ~0.85, insight cap 5 → ~6.
- **Claude's call:** *What He Did That Night* (the story switch) fires on finishing *Watch him*, not on holding the first insight, so its passage still follows watching him whichever source gives insight first.
- **Summary before planning is earned:** one button, **"Go back in"**, with the line **"I wake at the Smoky Mirror. Again."** above it (placeholder wording, Clara's voice); tooltip says it starts the next run. Once *Feed your hours* is done: Repeat and Plan as now, no line.

## Reuse
- `Way.needs` + `Simulation.NeedsOf`/`CantTravel`/`MissingFrom` (`Simulation.Places.cs`, `.Supplying.cs`): a count item (lasts this run) already satisfies a way's needs; `PlanWarnings` already warns on them.
- `TaskDefinition.easierWith`, `oncePerRun`; `SwitchTrigger.TasksCompletedInOneRun`.
- `RunResultsPanel` (`UI/RunResultsPanel.cs:78` already switches the Plan button on `Sim.PlanningUnlocked`); `results.*` keys in `game_text.txt`.
- `ActIColdRunTests` (the probe) for the rebalance.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Editor/GreyboxSetup.cs` | Edit | *Setup → Step NN: Act I playtest batch* (one click, then removed as plan 041 did): applies the data rows below |
| `Places/LeftCorridor.asset` | Edit (step) | way to HangingMirrors: needs 10 CorridorCandlelight |
| `Tasks/Lab/TalkToRoland.asset` | Edit (step) | needs only the ring (takes it); Time ×, `easierWith` insight time (step 5 numbers) |
| `Tasks/Lab/StudyTheTome.asset`, `Attend.asset` | Edit (step) | give 1 Insight instead of Understanding / Steady hands; Study once a run |
| `Items/Whathedidthatnight.asset` | Edit (step) | cap (step 5 number) |
| `Switches/WhatHeDidThatNight.asset` | Edit (step) | trigger: Watch him completed |
| `UI/RunResultsPanel.cs` | Edit | label and line by `PlanningUnlocked`; new TMP line (layout change, listed in the report, with tooltip) |
| `game_text.txt` | Edit | `results.go_back_button`, `results.go_back_line`, `results.go_back_tip` |
| `Tests/EditMode/ActIColdRunTests.cs` | Edit | bot relights the corridor every deep run; lab policy: earn insight, then talk |

Save format change? **No** (data only; old saves' Understanding/Steady hands are simply unused).

## Steps
1. **Candles:** edit `TutorialContentTests.TheDark_OpensForGood…` (its "the way needs nothing" line becomes "needs 10 corridor candles, every run"; approved with this plan) and add `ShippedRulesTests.DarkCorridor_NeedsTheCandlesLitThisRun` (failing); setup step part 1; run it.
2. **The lab's talk:** failing tests in `ShippedLabTests`: `TalkToRoland_NeedsOnlyTheRing`, `ThreeActions_EachGiveOneKeptInsight_OnceARun`, `Insight_MakesTheTalkShorter`, `WhatHeDidThatNight_FollowsWatchingHim`; setup step part 2; run it.
3. **Summary button:** keys and `RunResultsPanel` change; `GameTextTests` covers the keys.
4. **Probe:** update the bot (step 1 and 2 rules); run `ColdRun_Report`.
5. **Rebalance (stop for the user's OK):** a table of field / asset / old → new / why from the probe (talk length, insight speed and cap first; drain growth, XP only if needed). Target: best route ~9–10 runs, a lighter route ≤12. Apply on OK.
6. **Full EditMode run;** check the dev jump stages (stages past the corridor now need it relit before going deeper; say so) and remove the setup step.
7. **Play check by the user**, then `/wrap-up` (one commit for the whole batch).

## Tests
| Test | Proves |
| --- | --- |
| `ShippedRulesTests.DarkCorridor_NeedsTheCandlesLitThisRun` | next run, after the switch, the trip is refused until 10 are lit again |
| `TutorialContentTests.TheDark_OpensForGood…` (edited) | the switch still opens the way for good |
| `ShippedLabTests.TalkToRoland_NeedsOnlyTheRing` | no Understanding, Steady hands or insight count needed |
| `ShippedLabTests.ThreeActions_EachGiveOneKeptInsight_OnceARun` | Watch, Study, Attend: +1 kept each, once a run |
| `ShippedLabTests.Insight_MakesTheTalkShorter` | more insight, fewer seconds |
| `ShippedLabTests.WhatHeDidThatNight_FollowsWatchingHim` | the passage fires on watching him, not on studying first |

## Done when
- [x] Tests above pass; full EditMode run; compile and Console clean
- [x] In Unity, new save: a second deep run refuses the Dark Corridor until the candles are relit; the lab's talk is offered at once, slower with no insight; before Feed the Summary shows "Go back in" and the line, after Feed Repeat and Plan
- [x] decisions-log entry (today's answers, incl. the restore rule and earrings); GDD §12 / §12a edits shown for OK; changelog lines; `PROJECT_NOTES.md` placeholders (talk length, insight speed and cap, the Summary line); `docs/UI-BACKLOG.md` (max restore per second)

## Notes after implementation
- Built as planned in one session; Setup Step 105 applied the data rows and the Summary's line, then was removed from `GreyboxSetup.cs`.
- **Step 5 numbers, from the user's answer** ("grindy: repeat runs to level up, drain overtakes her until mastery is high enough"): talk Time × 0.911 → 6, insight time each 0.9 → 0.85, insight cap 5 → 6, **and** XP per second of task 1.8 → 1.5 (back to the original; 1.8 was set earlier the same day). Probe: tidy play walks out on run 9 (3 lab visits, one failed talk), loose play on 15 (over the plan's ≤12, accepted for the grind).
- The Summary's button is centred while it's alone (the Repeat button moved to x 0 until planning is earned): a small layout change not in the plan. `TextKey.SetKey` added so a label can change its line.
- The probe moved out of one test file into reusable balance tools (`Assets/Tests/EditMode/Balance/`, `docs/balance-tools.md`, `/balance`), at the user's request; its bot waits 3 runs after a failed *Feed your hours* (a human wouldn't retry a run-killer every time).
- Dev jump stages past the corridor now need the candles lit again before going deeper (unchanged assets).
