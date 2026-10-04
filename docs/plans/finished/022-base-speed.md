# 022 — Base speed ×2

**Status:** Built, play-tested, then rolled back (2026-09-30). The ×2 pace felt right, but the game clock no longer matched real time; the user chose one time scale everywhere, so the durations get halved instead (see the halving plan). Kept from this work: `TickEngine.Advance(realSeconds)` and two tick-rate tests. `BaseSpeed` and `baseClockSpeed` are gone.
**Design:** GDD v0.5 §4 *Game speed is earned*, §13a *[BUILT] The run timer shows game time*; decisions-log 2026-09-29 *Faster base speed; rooms known by heart carry over and speed up*. Backlog Next 1. First of four: 022 → 023 → ui-024 → 025.

## Goal
The game plays twice as fast to watch. The player's "1×" runs two game seconds per real second. Balance, XP, drain, benchmarks and the run timer (all in game time) are unchanged.

## Out of scope
- Room speed from mastery (plan 025). ×5 and meta currency.
- Any change to task durations or other balance numbers.

## Design assumptions
- **Base speed is data**, a `LoopSettings` field with a Balance Sheet column, so the user can tune it. It is presentation, not balance: Core never reads it.
- **The speed buttons keep their labels** (1×, 2×). They multiply on top of the base. *Feed your hours* still unlocks 2× (4 game s per real s).
- `TicksPerSecond` stays 10 (ticks per game second). `MaxSpeed` (50) still clamps the tier the player or dev tools pick, not base × tier.
- **CLAUDE.md wording:** the user approved bending "never the tick rate". The exact line change has been proposed; edit CLAUDE.md only once the user says yes to the text.

## Reuse
- `TickEngine` (`Assets/Scripts/TickEngine.cs`): `Speed`, `LimitSpeedTo`, the `Update` accumulator.
- `GameController.UseSimulation()` (l.198): already hands the engine its speed limit, so it hands over the base too.
- `LoopSettings` (`Core/LoopSettings.cs`) and `BalanceSheetWindow` loop-settings section.
- `TickEngineTests`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `LoopSettings.cs` / `LoopSettings.asset` | Edit | `baseClockSpeed` (game seconds per real second at 1×), default 2, min 0.1, tooltip. |
| `TickEngine.cs` | Edit | `BaseSpeed` (set from outside); the accumulator adds `deltaTime × BaseSpeed × Speed`. Comment: why this isn't balance. |
| `GameController.cs` | Edit | Set `BaseSpeed` from `LoopSettings` in `UseSimulation`. |
| `BalanceSheetWindow.cs` | Edit | "Base speed" field in the loop-settings section. |
| `CLAUDE.md` | Edit (after OK) | The proposed tick-rate line. |

New content fields: `LoopSettings.baseClockSpeed` (Balance Sheet column).

Save format change? No.

## Steps
1. Failing tests in `TickEngineTests`: base 2 at 1× runs 20 ticks per real second; the tier multiplies it; `LimitSpeedTo` still limits the tier only.
2. Add the field, the engine change and the wiring.
3. Balance Sheet field.
4. Docs: backlog; CLAUDE.md only after the user's OK.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `TickEngineTests.BaseSpeed_DoublesTicksPerRealSecond` | Base ×2 at 1× |
| `TickEngineTests.Tier_MultipliesBaseSpeed` | 2× tier gives ×4 |
| `TickEngineTests.LimitSpeedTo_LimitsTierNotBase` | Earned limit unchanged in meaning |
| Existing suite | No gameplay result changes (Core untouched) |

## Done when
- [ ] Tests pass; compile and Console clean.
- [ ] In Unity: start a new game; a 2 s action (Gather a wisp) finishes in about 1 real second, and the run timer still shows 2 s of game time for it.
- [ ] Balance Sheet shows "Base speed" = 2; setting it to 1 restores the old pace.

## Notes after implementation
- Built: `LoopSettings.baseClockSpeed` (default 2), `TickEngine.BaseSpeed`, set in `GameController.UseSimulation`. The Balance Sheet's loop-settings section draws every `LoopSettings` field automatically, so "Base speed" appears without an edit to `BalanceSheetWindow`.
- `TickEngine.Update` now calls a public `Advance(realSeconds)` so tests can drive the clock.
- `LoopSettings.asset` wasn't edited: it picks up the field's default (2) when Unity reloads it.
- Tests: 537/537 EditMode pass. CLAUDE.md not edited (waiting for the user's OK on the text). Play-check still to do.
