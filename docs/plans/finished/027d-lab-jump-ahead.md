# 027d — Jump ahead to a lab stage (developer tool)

**Status:** Done 2026-10-01 (built 2026-09-30 with 027b and 027c; the user played it and signed it off (2026-10-01))
**Left to do:** nothing.
**Design:** none; a testing tool, not player-facing. Follows `DevToolsPanel` (stripped from release builds).

> **Design change 2026-09-30 (decisions log *Features come in layers*): Act I has no stats.** The jumps set no stats. *Earrings found* needs no Perception; *Before the gem* sets the Crafting skill to the gem's gate instead of Attunement 26 (see the note at the top of 027c).

## Goal
In the F1 developer overlay, a **Jump to** row of buttons (*Before the talk*, *After the talk*, *Earrings found*, *Before the gem*) puts the kept state where that point in the lab would leave it and begins a new run, so 027b and 027c can be checked in minutes instead of replaying Act I.

## Out of scope
- Anything the player sees; release builds (the panel is `#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- Undoing a jump: it only **adds** to the current game. Use a spare save slot.
- The other 027 follow-ups (writing brief, deleting leftover assets, pacing record): parked in `docs/BACKLOG-later.md`.

## Design assumptions
- A stage is **content as data**: a `DevJumpStage` asset listing what to apply, so the stages change with the content rather than with code.
- Flipping a stage's switches applies their real effects (unlocks, locks, ways, reopened search), but their story pop-ups are marked read, so a jump doesn't bury the screen in passages.

## Reuse
- `DevToolsPanel.DrawWindow` (`Assets/Scripts/DevToolsPanel.cs:133`): button rows, `_game.BeginLoop()`, `_game.Simulation`.
- `Simulation.Flip` (`Simulation.Unlocks.cs:276`, private; returns early if already flipped): applies a switch's effects into `Persistent`; the jump goes through it, never around it.
- `Simulation.MarkRead` (`Simulation.Unlocks.cs:138`), `Persistent.Explored`, `Persistent.AttributeMasteryXp` / skill mastery XP, the kept-resource grant path (`Grant` for `lasts: Forever`).
- `SimulationTestBase` for tests; the `GreyboxSetup` step pattern to make the assets and wire the panel.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/DevJumpStage.cs` | New | ScriptableObject: `switches` (flipped in order), `keptItems` (resource + amount), `roomsExplored` (room + percent), `attributeLevels` / `skillLevels` (mastery set to at least this level). Tooltips on each. |
| `Core/Simulation.DevJump.cs` | New | `JumpTo(DevJumpStage)`: only while between runs or at a run's start; raises, never lowers; flips through `Flip`, marks those stories read, grants kept items, sets explored and mastery; fails loudly on a null entry. |
| `DevToolsPanel.cs` | Edit | `[SerializeField] List<DevJumpStage> _jumpStages`; a *Jump to* row: end the run, `JumpTo`, `BeginLoop`, autosave. |
| `Assets/Data/Dev/` | New | the stage assets (made by the setup step) |
| `Editor/GreyboxSetup.cs` | Edit | the next free *Setup/Step NN: Lab jump stages*: creates *Before the talk* and *After the talk* and assigns them on the panel |

UI change: one button row in the developer overlay only. Save format change? No (writes existing kept state).

## Steps
1. Failing tests: a stage flips its switches with their effects and marks their stories read; grants kept items; sets explored percent and mastery; never lowers anything already higher; a second jump to the same stage changes nothing.
2. Add `DevJumpStage` and `Simulation.JumpTo`. Compile, console, all EditMode tests.
3. Panel row; setup step making *Before the talk* (the hall's rooms explored, the ring way's switches, lab explored 67%, Insight 3) and *After the talk* (+ *Roland Takes the Ring*). Run it; in-Unity check.
4. 027b and 027c already carry a step to add *Earrings found* and *Before the gem* (Attunement 26, the earrings, the reopened search at 25%) to their own setup steps; if 027d is built after them, add both here instead.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `DevJumpTests.JumpTo_FlipsSwitches_WithEffects_StoriesRead` | effects real, no pop-up flood |
| `DevJumpTests.JumpTo_GrantsKeptItems_SetsExploredAndMastery` | the rest of the stage |
| `DevJumpTests.JumpTo_NeverLowers` | a later game isn't set back |
| `DevJumpTests.JumpTo_Twice_ChangesNothing` | safe to press again |

## Done when
- [x] Tests pass (744 EditMode); compile and Console clean.
- [ ] In Unity, on a new save slot: F1 → *Jump to: Before the talk* starts a run with the lab reachable, Insight 3 and Talk to Roland available; *After the talk* starts one without the ring task and with the ring way open. No story pop-ups appear.
- [ ] A release-style build (Development Build off) has no *Jump to* row.

## Notes after implementation
- **`DevJumpStage`** is a plain ScriptableObject, not `ContentAsset`: saves never name it. Fields: `switches`, `keptItems`, `roomsSearched` (room + percent), `skillMastery` (no stat levels: Act I has no stats).
- **`Simulation.JumpTo`** (`Simulation.DevJump.cs`) refuses mid-run (allowed between runs or before a run's first tick), flips through `Flip`, raises kept items, searches and mastery, then checks `ResourceReached` and `RoomExplored` switches as play would, and marks **every** story the jump revealed as read (not only the stage's switches'). A searched room counts as entered, its first-entry story in the journal, read, so walking in later shows no pop-up. A stage listing a per-run item fails loudly.
- **The panel** ends a run under way (only if any time has passed), jumps, begins a run and saves. Buttons stacked, one per line.
- **Stages** (`Assets/Data/Dev/`): every hall room 100% searched, the hall's switches up to the lab's first finds (not *The Mana Stone*: parked), Insight 3, Mirrors found 10, Quickened Hours (so room speed and planning are on); the lab at 67% / 0% / 15% / 25%; *After the talk* on adds *Roland Takes the Ring*; *Earrings found* and *Before the gem* add the earrings; *Before the gem* adds Crafting mastery 8 (the gate met at once: to see the greyed gate, use *Earrings found* and search to 25%).
- **Review fix:** a jump onto a run not yet begun (straight after loading) re-notes what the run starts with and clears its flipped switches and milestones, so the next Summary doesn't credit that run with the jump (`Simulation.NoteWhatTheRunStartsWith`, shared with `StartNewLoop`).
- Extra tests: `JumpTo_MidRun_IsRefused`, `JumpTo_ANullEntry_IsAMistake`, `JumpTo_ARunNotYetBegun_StartsFromTheStage`.
