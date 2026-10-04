# 029 — Feed your hours unlocks room speed

**Status:** Done
**Design:** GDD §4 (*Feed your hours* earns the speed control), §13a (room speed), decisions-log 2026-09-30 *Feed your hours unlocks room speed*; open points settled by the user 2026-09-30 in the planning chat (below)

## Goal
Rooms known by heart only speed up once Clara has fed her hours to the flames. The flat ×2 speed buttons disappear. Feed now takes 40 s with no up-front vitality cost, but drains an extra 2 vitality a second while she works it, and the top bar's drain readout shows the jump.

## Out of scope
- The feedback half: badge, ribbon, tooltip and first-time explanation when room speed unlocks (`docs/UI-BACKLOG.md` item 8, with the planning screen).
- Deleting the speed-tier code or scene objects (`SpeedTiers`, the RunHeader row, `unlocksSpeed`): kept so the buttons can come back.
- Already done while planning (2026-09-30, at the user's request): GDD §4, §13a and the open-questions line updated; story passage `16_all_twenty_five_alight.txt` reworded to the new cost; Quickened Hours' description reworded. At wrap-up, only flip the GDD's "the build keeps the ×2 until plan 029 is built" / *Differs* notes to built.

## Design assumptions (settled 2026-09-30 by the user)
- Header speed buttons: hidden; code and assets kept.
- Feed: 40 s~, no direct vitality cost, a **flat** extra drain of 2~/s while it's being worked. It is added on top of the normal drain after candles, growth and Stamina (none of them change it), and it's included in the top bar's drain per second.
- Feed's needs are unchanged (the corridor's candles).
- The map hides room speed (×N label and tooltip line) until room speed is unlocked; so does the ribbon's ×N / "held" note.
- Clara pays the extra drain only while Feed is the running action and progressing, per game second. Faster skill finishes it sooner, so it costs less in total.

## Reuse
- `ResourceDefinition.unlocksSpeed` + `Simulation.FastestSpeedUnlocked` (`Simulation.Unlocks.cs:94`): same "a kept item grants a capability" pattern for the new flag. With no item granting ×2, `SpeedTiers.Fill` returns nothing and `RunHeader` hides the row by itself.
- `Simulation.RoomSpeedNow` / `RoomSpeed(room)` (`Simulation.ByHeart.cs`): the gate goes in `RoomSpeedNow`; `RoomSpeed(room)` stays the "would be" value.
- `VitalityDrainPerSecond` (`Simulation.Attributes.cs:227`): the tick already drains by it (`Simulation.cs:282`), so adding the extra here charges it *and* shows it in `RunHeader`'s `_drainLabel`.
- `RunHeader.DrainTip()`, `MapView` (lines 240, 263), `QueueRibbon.RoomSpeedNote`.
- **Save format:** Quickened Hours is already a kept item saved by `Id`. Old saves that hold it now mean "room speed unlocked", so no upgrade step is needed.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/ResourceDefinition.cs` | Edit | `bool unlocksRoomSpeed` (tooltip) |
| `Core/Simulation.Unlocks.cs` | Edit | `RoomSpeedUnlocked`: any kept or run item with `unlocksRoomSpeed` |
| `Core/Simulation.ByHeart.cs` | Edit | `RoomSpeedNow` returns 1 until `RoomSpeedUnlocked` |
| `Core/TaskDefinition.cs` | Edit | `float extraDrainPerSecond` ([Min 0], tooltip) |
| `Core/Simulation.Attributes.cs` | Edit | `ExtraDrainNow` (running task's extra, else 0), added flat into `VitalityDrainPerSecond` |
| `UI/MapView.cs` | Edit | room-speed label and tooltip line only when `RoomSpeedUnlocked` |
| `UI/QueueRibbon.cs` | Edit | `RoomSpeedNote` empty until unlocked |
| `UI/RunHeader.cs` | Edit | `DrainTip` adds a line while `ExtraDrainNow > 0` |
| `game_text.txt` | Edit | new key `tips.drain_extra`: "{action}: +{rate} a second until it's done." |
| `Editor/BalanceSheetWindow.cs`, `Editor/ItemSections.cs` | Edit | task column "Drain +/s"; item column/section for `unlocksRoomSpeed` |
| `Data/Tasks/Hall/FeedYourHoursToTheFlames.asset` | Edit (Balance Sheet / MCP, not YAML) | duration 40, cost 0, cost shares cleared, `alwaysCharged` off, `extraDrainPerSecond` 2 |
| `Data/Items/QuickenedHours.asset` | Edit (same) | `unlocksSpeed` 0, `unlocksRoomSpeed` on (description already reworded) |

New content fields: `ResourceDefinition.unlocksRoomSpeed`, `TaskDefinition.extraDrainPerSecond` (both get Balance Sheet columns).

Save format change? **No**. The unlock is the Quickened Hours item, already saved by `Id`.

## Steps
1. Failing tests for the unlock (table below); then add `unlocksRoomSpeed`, `RoomSpeedUnlocked` and the gate in `RoomSpeedNow`. Existing `RoomSpeedTests` that expect a speed-up get the unlock item in their setup (a new precondition, not a weakened test).
2. Failing tests for the extra drain; then add `extraDrainPerSecond`, `ExtraDrainNow` and the flat addition to `VitalityDrainPerSecond`.
3. Rewrite `RewardTests`' ×2 test as "Feed unlocks room speed, kept through save" (the old ×2 promise is retired by the decision, not skipped).
4. Balance Sheet columns and `ItemSections` entry (`ItemSectionsTests` must stay green).
5. Data: Feed and Quickened Hours as in the table.
6. UI: map, ribbon, drain tooltip, text key. Check the header speed row stays hidden after Feed.
7. In-Unity check; decisions-log entry for the 2026-09-30 answers; backlog updated.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RoomSpeedTests.RoomSpeedNow_IsOne_InByHeartRoom_UntilUnlocked` | no speed-up before Feed |
| `RoomSpeedTests.RoomSpeedNow_ByHeartRoom_AfterUnlock_IsCapped` | the unlock turns room speed on |
| `RoomSpeedTests.RoomSpeedUnlocked_KeptThroughSave` | an old-format save holding Quickened Hours stays unlocked (no upgrade step needed) |
| `RewardTests.FeedingYourHours_UnlocksRoomSpeed_NotSpeedTwo` | Feed gives the unlock; `FastestSpeedUnlocked` stays 1 |
| `ExtraDrainTests.DrainPerSecond_RisesByExtra_WhileTaskRuns_AndDropsAfter` | the readout's number jumps and returns |
| `ExtraDrainTests.ExtraDrain_IsFlat_NotHeldOffOrGrown` | candles and growth don't touch it |
| `ExtraDrainTests.ExtraDrain_NotCharged_WhenTaskNotRunning` | queued or paused costs nothing |
| `ExtraDrainTests.VitalityLost_OverTask_IsBasePlusExtraTimesSeconds` | charged per game second, tick for tick |

## Done when
- [x] Tests above and all EditMode tests pass; compile and Console clean (608 of 608)
- [x] In Unity: new run → header shows no ×1/×2 buttons, map shows no ×N. Do Feed → drain readout jumps by 2 for its 40 s and drops back; no 50-vitality charge. Next run, a by-heart room shows ×N on the map and runs faster.
- [x] decisions-log entry (header hidden; Feed 40 s, flat +2/s, no direct cost; map hides room speed until unlocked); backlog Next 14 removed at wrap-up

## Notes after implementation
- Built as planned. Additions beyond the Changes table (player-facing wording, for the user's approval):
  - Feed's task description reworded ("Burn her hours hard, for as long as it takes, and they will burn to her rhythm ever after: rooms she knows by heart will pass faster. It can only ever be done once, and it drains her while she does it.").
  - A new action-tooltip line (`actions.tip_extra_drain`): "Burns {rate} extra vitality a second while she does it."
  - Also: Feed's leftover cost share cleared, and a `TutorialContentTests` check of the real Feed and Quickened Hours assets.
- Commit 84184fb also carried another session's ui-024 docs (the ui-024b/c/d plans, the UI-BACKLOG and BACKLOG 1b lines, the plans README row) and the Candles art folder; none of it belongs to plan 029. Keep separate sessions' files in separate commits.
- `unlocksSpeed`, `SpeedTiers` and the header buttons stay (the user's call): the buttons are simply hidden while no item grants ×2.
- The in-Unity play check (header hidden, drain jump, by-heart room faster next run) is the user's to run.
