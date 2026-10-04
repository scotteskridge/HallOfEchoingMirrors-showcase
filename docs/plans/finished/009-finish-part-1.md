# 009 — Finish Part 1

**Status:** Done (2026-09-28)
**Design:** GDD v0.5 §13a (*Proposed layout*: "Stats collapse to one row of multipliers. Expand on click"); decisions-log 2026-09-27 *Main screen UI rework* (story and ambient in one panel, to playtest), 2026-09-24 *The story feed* (follows new lines unless scrolled up, cleared each run), 2026-09-28 *Pockets and stats: the choices*; roadmap `docs/plans/ui-rework-roadmap.md`; backlog Next 8a, 8b, 8d

## Goal
Opening the queue drawer pushes the map up instead of covering it. The stats row under the vitality bar does everything the Stats tab did (blue level bar, yellow mastery bar, same tooltips) and wraps as new skills are learnt, so the Stats tab goes. Story milestones and *Around her* lines share one scrollable box. The retired Actions list, inventory panel and Pockets tab are deleted.

## Out of scope
- Backlog 8c (auto pause/resume), 8e (craft or pick up replaces a pocket item), 8f (knowledge items), 8g (Roland's ring).
- Keeping story lines between runs (Journal screen, backlog). The user's layout pass. Moving any other existing UI.

## Design assumptions (settled with the user 2026-09-28)
- **Drawer:** the map's bottom edge follows the drawer's top as it slides (same 0.25 s). Zoom is unchanged, so rooms, text and icons keep their size; the map just shows less. Follow mode keeps Clara in the smaller view. The overlays and popover sit inside the map and ride along.
- **Stats chips:** the text stays short ("Wayfinding ×2.10"). A thin blue level bar (`UiStyle.LevelBar`) and a yellow mastery bar (`MasteryBar`) sit under it. Hover shows the Stats tab's tooltip (`ClaraTips.StatTip`/`SkillTip`), plus the level and mastery if the tip lacks them. A click no longer opens anything. The −/+ toggle stays.
- **Wrapping:** chips flow left to right and wrap onto a new line when the row is full, so the row grows downward over the map.
- **Combined box:** one scroll list, oldest at the top. It follows new lines unless scrolled up, and is cleared each run (this run only). Milestones keep their card (time vs last run, paragraph, Read more) and never fade. Only *Around her* lines fade (beyond the newest 4 of them). The 150-line cap drops only the oldest ambient lines. The heading reads *Story* (`scene.mainscreen_story`), and `scene.mainscreen_ambient` goes. Placeholder: the box takes the space both panels had; the user adjusts it afterwards.
- **Stats tab deleted outright**, with `StatsSkillsPanel` and `TraitRow` (script and prefab).

## Reuse
- `SlideDrawer` (+ `SlideDrawerTests`): its slide offset drives the map's inset.
- `MapView`/`MapWindow`: fixed-scale layout, so shrinking the map rect doesn't shrink content.
- `StatsRow`, `ClaraTips` (`StatTip`, `SkillTip`, `StatBrief`, `SkillBrief`), `UiStyle.LevelBar`/`MasteryBar`, `Simulation.KnownSkills`, `LevelOf`.
- `StoryFeed` (scroll, stick-to-bottom, fade, typewriter, cap) is extended. `StoryPanel` and `MilestoneRow` post milestones into it instead of owning a list. `FeedNotes` and `BlurbTeller` are unchanged.
- `EditorUiFactory` (`MakeDisplayBar`, `SetTip`, `Wire`, `Stretch`) for the setup step.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `UI/SlideDrawer.cs` | Edit | `CoveredInset(openness)`; sets the map rect's inset on its edge while sliding |
| `UI/FlowLayout.cs` | New | wrapping layout group (pure `Place(widths, maxWidth, spacing)` + thin LayoutGroup) |
| `UI/StatsRow.cs` | Edit | two bars per chip, Stats-tab tooltips, no click-to-open |
| `UI/StoryFeed.cs`, `UI/StoryPanel.cs` | Edit | milestones as feed entries; fade/cap only ambient (pure `FeedTrim` helper) |
| `Editor/GreyboxSetup.cs` | Edit | Step 85: remove the Actions, Inventory and Stats pages and tabs from the scene, wire the map inset, add bars and FlowLayout to the stats row, merge the right panel into one box |
| `ActionList.cs`, `InventoryPanel.cs`, `InventoryRow.cs` (+ prefab), `StatsSkillsPanel.cs`, `TraitRow.cs` (+ prefab) | Delete | after Step 85 is run and the scene saved (else missing scripts); deleted through Unity with their `.meta` |
| `game_text.txt` | Edit | remove keys used only by the deleted pieces (the `actions.where*`, `play_elsewhere`, `inventory.*` list from the search, the retired scene labels, `scene.mainscreen_ambient`, Stats-page-only keys), each checked by grep first; fix the stale `## inventory` comment |

New content fields: none. Save format change? No.

## Steps
1. Failing tests for `SlideDrawer.CoveredInset`, then the drawer pushes the map.
2. Failing tests for `FlowLayout.Place`, then the layout group.
3. `StatsRow`: bars, tooltips, no click.
4. Failing tests for `FeedTrim`, then the combined feed in `StoryFeed`/`StoryPanel`.
5. Step 85 (*Hall of Echoing Mirrors → Setup → Step 85: Finish Part 1*). The user runs it and saves the scene (Ctrl+S).
6. Delete the retired scripts and prefabs, and their now-unused text keys. Compile and check the Console for missing-script warnings.
7. Play through. Compile, Console and all EditMode tests clean.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `SlideDrawerTests.CoveredInset_ClosedIsZero` / `_OpenIsPanelHeight` / `_HalfOpenIsHalf` | the map's bottom edge tracks the drawer |
| `FlowLayoutTests.Place_FitsOnOneLine` / `_WrapsWhenFull` / `_TooWideGetsOwnLine` / `_HeightGrowsWithLines` | chips wrap instead of overflowing |
| `FeedTrimTests.Trim_DropsOldestAmbientFirst` / `_NeverDropsMilestones` | old story stays scrollable |
| `FeedTrimTests.Faded_OnlyAmbientBeyondNewestFour` | milestones never fade |

## Done when
- [x] Tests above pass; compile and Console clean (no missing scripts)
- [x] In Unity: open the drawer. The map's bottom rises with it and nothing on the map gets smaller. Close it and the map returns.
- [x] Each stat and skill chip has a blue and a yellow bar that fill as she trains. Hovering shows the old Stats tooltip with the level. Learning a new skill adds a chip that wraps to a second line when needed. The drawer has only the Queue tab.
- [x] One *Story* box: ambient lines and milestone cards interleave in order, scroll back to the run's start, and empty at a new run
- [x] Roadmap progress updated; decisions-log entry for the choices above; backlog 8a, 8b, 8d and the two retired-panel lines removed; `/sync-state`

## Notes after implementation
- **`FlowLayout.Place` takes sizes, not widths** (`Place(sizes, maxWidth, spacing)`, plus `Height(sizes, placed)`): line height depends on the items' heights too.
- **`FeedTrim` is `Dropped`, `AmbientAges` and `Fades`** rather than one trim call; the tests are as planned.
- **`StoryPanel` catches up rather than listening for `MilestoneReached`:** each frame it posts a card for any milestone of this run not yet in the box. So it doesn't matter whether a new run or a loaded game clears the box before or after the panel hears of it. The box's `Cleared` event tells it the old cards are gone.
- **The "Milestones reached this run appear here" line was removed** (`story.empty`): in one box it would sit among the ambient lines.
- **Also removed as dead code:** `ClaraTips.StatEffect`/`SkillEffect` and the `stats.effect_*` keys, used only by the Stats page. 32 text keys went in all.
- **The stats chips stay Buttons** (the serialized field is typed `Button`), with no click listener; backlog clean-up line added.
- **After Step 85:** a stray milestone card (probably left by undoing and re-running the step) sat visible in the Story box showing *The Chase Ends* at every run; deleted from the scene. Everything else the step built was checked and correct.
- **Review fix:** if a new run begins and the box wasn't cleared (only `FeedNotes` clears it), `StoryPanel` logs an error and clears it itself, so old cards can't linger.
- The play-through (step 7) is the user's.
