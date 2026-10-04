# 002 — Layout shell (UI rework plan 1)

**Status:** Done (2026-09-28)
**Design:** GDD v0.5 §13a *Interface — the Map Is the Queue* (starting point, not a spec), decisions-log 2026-09-27 *Main screen UI rework* and *UI rework: the mockup and plan 1's layout*, `docs/plans/ui-rework-roadmap.md`

## Goal
The map fills the centre of the main page. Around it: a top bar (vitality, drain, clock, speed tiers, pause, End run, page tabs); a right panel holding the ambient feed and the story milestones; and reserved bands for the ribbon and the bottom strip. The old Actions, Queue, Inventory and Stats panels are pulled up over the map with bookmark-style tabs, so everything still works while plans 2–6 replace them.

## Out of scope
- Room popovers (plan 2), the ribbon's content and the queue chip row (plan 3), the route line and token (plan 4), floor dots (plan 5), pockets and stats strip content (plan 6).
- Standing orders, visit counts, "by heart", the queue forecast (backlog).
- Ambient lines on the map (kept in the right panel, decisions-log 2026-09-27).
- How the Hall plus seven realms fit on the map (open; the map keeps follow and wheel zoom).
- Art: bookmark tabs are coloured rectangles; the book-page look is stage 3.

## Design assumptions
- **Proportions** from the mockup (a proportion study): top bar ~7%, ribbon ~6%, strip ~9% of height, the rest the middle band; right panel ~21% of width. Set as anchor fractions so the 1920×1080 canvas scales; the user tunes by hand after.
- **Right panel:** ambient feed on top (~65%), story milestones below (both components unchanged, reparented into one frame titled with the existing *Around her* / *Story* headings).
- **Drawer:** one page open at a time; clicking the open tab closes it, another tab switches. It covers the lower ~55% of the map band. It doesn't pause the game. Its edge (Bottom/Top/Left/Right) is an Inspector setting, bottom by default.
- **Speed tiers:** one button per earned speed from `RunHeader._speeds`, the current one highlighted, plus the next unearned speed greyed with a tooltip. Hidden while only ×1 is earned (as now); no locked tier once all are earned. A dev-panel speed outside the list highlights none.
- **Map info text** ("Click a place…") stays with the map until plan 2's popovers.
- **Hard rule set aside with approval:** "never move or resize existing UI". The user approved the setup steps moving the listed objects (roadmap, decisions-log). Rows, prefabs and their styling are not touched; the old `SpeedButton` is deactivated, not deleted.

## Reuse
- `RunHeader` (`UI/RunHeader.cs`): the top bar; `Simulation.FastestSpeedUnlocked` (`Core/Simulation.Unlocks.cs:30`) for the earned speed; `TickEngine.Speed`.
- `StoryPanel`, `StoryFeed`: no size assumptions; the feed was already reparented once (c588b4a).
- `MapView`: sizes itself from its `_area`, re-centres on resize, no clamping (safe to enlarge).
- `ScreenManager`: only moves page roots; its tab buttons keep working wherever they sit.
- `TemplateList<T>` for the tier buttons; `UiStyle` for colours; `ToolTip`, `TextKey`, `UiText`.
- `EditorUiFactory` (`Place`, `Stretch`, `MakeButton`, `MakeText`, `Wire`, `SetTextKey`); reparent pattern `Undo.SetTransformParent` + `Stretch` (c9da169).
- `MapView.ZoomAfterScroll` + `MapZoomTests`: the pattern for testing UI maths as a static method.
- `Simulation.MilestoneReached` (tested in `MilestoneTests`).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `UI/StoryPanel.cs` | Edit | Step 0: listen to `MilestoneReached` (rebuild from `Loop.Milestones` on `SimulationChanged`) instead of polling each frame; behaviour unchanged |
| `UI/SpeedTiers.cs` | New | Static: which tiers to show and which is locked, from speeds + earned max |
| `UI/RunHeader.cs` | Edit | Tier buttons (template + `TemplateList`) replace the cycling Speed button |
| `UI/SlideDrawer.cs` | New | Tabs, one open page, slide in/out from a chosen edge (`Time.unscaledDeltaTime`: UI motion, like `Glow`) |
| `UI/UiStyle.cs` | Edit | `DrawerBackground`, `BookmarkTab`, `BookmarkTabOpen`, `SpeedTierCurrent`, `SpeedTierLocked`, `BandBackground` |
| `Editor/GreyboxSetup.cs` | Edit | Step 76: layout frames; Step 77: drawer and speed tiers |
| `game_text.txt` | Edit | tab labels (scene keys), `tips.drawer_*`, `tips.speed_locked` |
| `SampleScene.unity` | Edit | by the setup steps only |

New content fields: none. Save format change? No.

## Steps
1. **Step 0:** refactor `StoryPanel` to the event. Existing tests stay green; check milestones still show after loading a save mid-run.
2. Write failing `SpeedTiersTests`, then `SpeedTiers`; switch `RunHeader` to tier buttons.
3. Write failing `SlideDrawerTests`, then `SlideDrawer`.
4. **Setup Step 76: Main page layout.** Builds the top bar, middle (map | right panel), ribbon and strip frames. Reparents Header, Gem, Map, StoryFeed, Story and ScreenTabs (tabs to the top bar's right end). Safe to run twice, undoable, logs each move.
5. **Setup Step 77: Drawer and speed tiers.** Builds the drawer over the map band with bookmark tabs in the strip's left end. Moves Actions, Queue, Inventory and Stats into its pages. Builds the tier template and deactivates `SpeedButton`. Wires fields, adds tooltips and TextKeys.
6. User runs both steps, adjusts by hand, plays one run; then the steps are retired.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `SpeedTiersTests.OnlyX1Earned_ShowsNothing` | hidden until the first speed is earned |
| `SpeedTiersTests.SomeEarned_ShowsEarnedPlusOneLocked` | e.g. earned 2 of {1,2,5} → 1, 2, [5 locked] |
| `SpeedTiersTests.AllEarned_NoLockedTier` | nothing left to tease |
| `SlideDrawerTests.ClickClosedTab_Opens` / `ClickOpenTab_Closes` / `ClickOtherTab_Switches` | tab state rules |
| `SlideDrawerTests.Offset_ClosedIsPastEdge_OpenIsHome` (each side) | the slide works from any edge |

## Done when
- [x] Tests above pass, all EditMode tests pass; compile and Console clean
- [x] In Unity: run Steps 76 and 77, press Play: the map fills the centre and follows Clara; clicking a neighbouring room queues the trip; the right panel shows *Around her* above *Story*; Menu/Summary/Main work from the top bar; each bookmark tab slides its old panel up over the map and Play/Schedule still work there; the speed tiers show earned speeds plus one locked tier (once one is earned)
- [x] Roadmap progress ticked; decisions-log already updated; BUILD-STATE synced

## Notes after implementation
- **Page tabs stay on the Canvas**, pinned top-right, not moved into MainScreen's top bar (they'd slide away with the page, leaving Menu and Summary without tabs).
- **BeginButton re-anchored** by Step 76 over the Pause/End run area (not in the approved move list): it was placed in pixels from the Header's centre and hung under the page tabs once the Header narrowed.
- The whole *Around her* panel (title and feed) moved, not StoryFeed alone; the Gem moved with the Header it's inside.
- StoryPanel also rebuilds on `LoopStarted` (the milestone list empties at a new run), and only unsubscribes in `OnDestroy`.
- `tips.speed_locked` is neutral ("not earned yet"): nothing in Act I grants ×5.
- Extra tests beyond the plan: a speed earned between listed speeds, a half-open drawer, refilling the tier list.
- Helpers added to `EditorUiFactory`: `MakeFrame`, `MoveInto`, `SetTip`.
- The user ran both steps and is holding layout tuning until Part 1 is built (decisions-log 2026-09-28).
