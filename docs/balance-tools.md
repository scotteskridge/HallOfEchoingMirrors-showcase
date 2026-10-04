# Balance tools

For balance passes: measure how many runs a fresh save needs, try changes without touching the assets, and keep a record of every pass. The workflow is the `/balance` command (`.claude/skills/balance/SKILL.md`). Built 2026-10-02 (plan 055's balance pass).

## What's where
| Piece | Where | What it does |
| --- | --- | --- |
| The probe | `Assets/Tests/EditMode/Balance/BalanceProbe.cs` | Plays a fresh save, run after run, for every way of playing × every scenario; writes the reports |
| A route | `RouteBot.cs` (shared moves), `ActIRoute.cs` (Act I) | A "sensible player": decides one action at a time when the queue is empty |
| Ways of playing | `ProbePolicy.cs`, listed in each route | Tidy to loose (e.g. how many Dark Hall candles, how many phials) |
| What-ifs | `WhatIf.cs`, reading `docs/balance/scenarios.txt` | Changes numbers in memory only, and always puts them back |
| Snapshot | `BalanceSnapshot.cs` | Every number on the balance assets, as one table |
| Tests | `BalanceProbeTests.cs` | `Probe_ActI` (hand-run, category **Balance**); two suite tests: what-ifs put values back, and `scenarios.txt` names only real fields |

## Running it
- **Unity:** *Window → General → Test Runner → EditMode*, open `HallOfEchoingMirrors.Tests.Balance`, select `Probe_ActI`, *Run Selected* (it's Explicit, so *Run All* skips it).
- **Claude (MCP):** `run_tests` with `category_names: ["Balance"]`.
- Takes about 10 seconds for 3 scenarios × 4 ways of playing.

## Reports (`docs/balance/latest/`, in git)
| File | Open in | Holds |
| --- | --- | --- |
| `summary.md` | any viewer | the run each way of playing walks out on, per scenario, and each scenario's changes |
| `probe_summary.csv` | Excel / Sheets | the same as a table, with the route's counters (Act I: lab visits, talk attempts) |
| `probe_runs.csv` | Excel / Sheets | every run: seconds, how it ended, how far she got, where she died doing what, vitality lost, travel paid, switches flipped (notes), skills as level+mastery |
| `balance_snapshot.csv` | Excel / Sheets | every balance field: folder, asset, field path, value. The field paths are what `scenarios.txt` uses |

Each pass's reports are committed, so `git log -p docs/balance/latest/` compares passes. To keep one on its own, copy the folder to `docs/balance/YYYY-MM-DD-name/`.

## Writing scenarios (`docs/balance/scenarios.txt`)
```
shipped
talk longer | Assets/Data/Tasks/Lab/TalkToRoland.asset | durationMultiplier | 8
talk longer | Assets/Data/LoopSettings.asset | xpPerSecondOfTask | 1.4
```
- Same name on several lines = one scenario with several changes. A name alone = the shipped numbers.
- Numbers, `true`/`false` and enums (by index) only. Nested fields use Unity's paths: `exploring.time`, `easierWith.Array.data[0].timeEach` (find them in `balance_snapshot.csv`).
- A suite test fails if a line names a missing asset or field, so a typo can't hide.

## Extending
- **New content in an act:** update that act's route so the bot uses it the way a player would. Read gates and counts from the assets (as `ActIRoute` does for the corridor's candles and Crafting 8), never copy the numbers in.
- **A new act:** add `<Act>Route : RouteBot` with its ways of playing, `Frontier`, `IsFinished`, `Decide` and counters; add a `Probe_<Act>` test.
- **A new report column:** add it to `BalanceProbe` (every run) or to the route's `Counters` (per save).
- **The bot vs the game:** the bot plays the same route every time and never explores. A cold player is slower; expect a few more runs than the tidy way of playing. If the bot does something no human would (retry a task that kills it every run), fix the bot, not the balance.
