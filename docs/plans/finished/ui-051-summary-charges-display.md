# ui-051 — Summary lists every escalating charge (display half)

**Status:** Done 2026-10-02
**Left to do:** nothing
**Design:** GDD v0.5 §13a *Legibility: the run report*, §4 *Show the next cost*; decisions-log 2026-10-01 *The Summary counts every escalating charge, per task*; `docs/BACKLOG.md` *Now* (the item moves to `docs/UI-BACKLOG.md` *Now* with this plan, as the backlog roadmap asks)
**Pillar:** 3, *Every run teaches something* ("a failed run can be diagnosed")

## Goal
After a run, the Summary names every task whose rising charge took vitality, beside the existing Moves figure: "· Moves: 9, costing 31 vitality · Practise the cut: 4, costing 9.4 vitality". A rising cost the player couldn't see no longer reads as the game punishing them.

## Out of scope
- Any layout change: the entries join the existing one-line detail (the user, 2026-10-01); the Summary layout pass is ui-039.
- Core: `RunReport.Charges` is built and saved (save version 22).
- Per-task wording in Clara's voice: entries use the task's own display name.
- Changing Moves or the by-heart and kept-vitality parts of the line.

## Design assumptions
- Placement: same line, after Moves, before "kept" (the user, 2026-10-01). Wording: the task's own name, mirroring Moves (the user, 2026-10-01).
- **Placeholder rule:** an entry is skipped when its vitality shows as 0 (`UiText.Number` rounds to one place), the way "+0 vitality kept" is skipped; the test is on vitality, not goes, because a go cut short by the run's end has 0 goes but did cost vitality.
- A cut-short go therefore reads "Practise the cut: 0, costing 1.2 vitality": honest, slightly odd; the user can reword when ui-039 passes over the Summary.
- Entries appear in the order each task was first finished (the order `Charges` holds them).

## Reuse
- `RunReport.Charges` (`Core/RunReport.cs`): a list of (task, goes, vitality); trips are separate (`Moves`, `MoveVitality`).
- `RunResultsPanel.ShowDetail` (`UI/RunResultsPanel.cs`, 144 lines): builds the one-line detail; the Moves suffix there is the pattern.
- `GameText.Get(key, (name, value))`, `UiText.Number` (one-decimal rounding), `ActionText.NameOf(task)` (`UI/ActionText.cs`): task display name.
- `results.moves_suffix` in `Assets/Text/game_text.txt` (`## results`): the wording to mirror.
- `QueueEntryText` / `ActionText`: the pattern for a small pure static helper, testable with no scene.
- `GameTextTests`: fails if code uses a key the text file lacks.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Scripts/UI/RunSummaryText.cs` | New | pure static `ChargeSuffixes(RunReport) → string` (empty when none); so the rule is testable without a scene |
| `Assets/Scripts/UI/RunResultsPanel.cs` | Edit | `ShowDetail` appends `RunSummaryText.ChargeSuffixes(run)` after the Moves suffix |
| `Assets/Text/game_text.txt` | Edit | `results.charge_suffix: · {task}: {goes}, costing {vitality} vitality` (British spelling, beside `moves_suffix`) |
| `Assets/Tests/EditMode/RunSummaryTextTests.cs` | New | the tests below |

Layout changes: none. New UI element: none (text in an existing label, so no new tooltip).

New content fields: none. Save format change? No.

## Steps
1. Write failing EditMode tests (below), building `RunReport`s directly.
2. Add the text key.
3. Add `RunSummaryText.ChargeSuffixes`.
4. Wire it into `ShowDetail`.
5. Run all EditMode tests; fix causes, never weaken a test.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RunSummaryTextTests.NoCharges_GivesNothing` | an empty `Charges` adds no text |
| `RunSummaryTextTests.OneChargedTask_ReadsLikeMoves` | name, goes and one-decimal vitality appear in the expected wording |
| `RunSummaryTextTests.EveryChargedTask_GetsAnEntry_InOrder` | two tasks give two entries in `Charges` order |
| `RunSummaryTextTests.AGoCutShort_StillShows_WhenItCostVitality` | goes 0, vitality > 0 is shown |
| `RunSummaryTextTests.AnEntryThatRoundsToZero_IsSkipped` | vitality 0.04 adds nothing |
| `RunSummaryTextTests.TripsAreNotListedHere` | `Moves` / `MoveVitality` never appear in the charge text |
| `GameTextTests` (existing) | the new key exists |

## Done when
- [ ] Tests above pass; whole EditMode suite green; compile and Console clean
- [ ] In Unity (UI editor): play a run, spend a few goes on Practise the cut and walk some trips, end the run: the Summary detail shows "· Moves: N, costing X vitality · Practise the cut: N, costing X vitality"; a run with no Practise shows no such entry; check the line still fits at 1920×1080 and 1280×800
- [ ] decisions-log updated only if something changes; GDD no edit; changelog line (player language: the Summary now lists what each rising charge cost you)

## Notes after implementation
Built as planned, tests green (896 of 896). Differences: the task name is `task.displayName` (`ActionText.NameOf` needs a `Simulation`, so isn't pure); `TripsAreNotListedHere` sets `Moves`/`MoveVitality` by reflection (internal setters, separate test assembly). The user checked it in Play mode on 2026-10-02 and signed it off; line-fit at 1920×1080 and 1280×800 was part of that check.
