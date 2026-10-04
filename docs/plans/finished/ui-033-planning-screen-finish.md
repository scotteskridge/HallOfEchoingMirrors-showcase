# ui-033 — Planning screen finish: sizes, map warnings, capture clean-up

**Status:** Done 2026-10-01 (the user judged the play checks done at wrap-up)
**Design:** GDD §13a (*the Map Is the Queue*); decisions log 2026-09-30 *Warnings ahead: the rule (plan ui-024d)* and *built*, *The planning screen is built (plan ui-024c)*, *Planning in a room not known by heart is allowed, but it must say it won't carry over*; decisions log 2026-10-01 *Colour as the payoff* (the map's warm palette only). Roadmap: `ui-backlog-roadmap.md` batch 1.

## Goal
The planning screen looks finished: sensible sizes, and a warned or won't-carry stop shows ⚠ on its room's map tag as well as in the queue. A folded block's ⚠ tip lists the reasons instead of pointing at rows the player can't see. The pockets heading and pouch line sit anchored, and nothing overlaps at 1920×1080 or 1280×800.

## Out of scope
- New warning rules (the walk in `Simulation.PlanWarnings.cs` is unchanged).
- Room speed display gaps (UI-BACKLOG Next 8), the tooltip redesign (Next 3), prefabs (Next 2).
- Splitting `MapView.cs` (ui-034). Keep this plan's additions to `MapView` small; if it grows, put the tag text in a new `MapTagText` helper now and say so.

## Design assumptions
- **Map tag (the user, 2026-10-01):** a ⚠ after the room's tag; hovering lists the reasons, one line each, using the same reason text as the queue rows. A stop that won't carry over gets its own mark, with the existing `queue.not_carried_tip` wording.
- The tag already has one tooltip (`MapView.RoomSpeedTip`: speed line or `queue.not_carried_tip`). The warning reasons are **added to that tip**, above its speed line; don't add a second tooltip. ui-038 later adds the ramp line to the same tip (decisions log 2026-10-01 *Room speed made visible*), so keep the tip built in one place.
- **Menu → Summary while planning (the user, 2026-10-01):** allowed. The Summary's **Plan** button (`RunResultsPanel`) is the way back; only check that it shows.
- Folded block's tip: lists the reasons itself (Claude's choice of wording; the key text is new, so the user may reword it).
- Sizes are Claude's to set; the user tweaks afterwards (CLAUDE.md, Unity rules).
- **Map colours in the warm palette (added 2026-10-01):** the one stage-1 piece of *Colour as the payoff* (decisions log 2026-10-01): the map's rooms, ways and reachable marks move from the cold greybox greys to the gilt-and-dark palette, with **no hue colour** on the map in Act I. Colours stay edit-time values in `MapStyle`/`UiStyle`; no runtime recolour system (that's stage 2). Shades are Claude's call; the user checks by eye.

## Reuse
- `Simulation.PlanWarnings()` / `PlanWarningCount` in `Core/Simulation.PlanWarnings.cs`: the per-queue-entry reasons, cached.
- `StopCard` (`Refresh`, `ShowBlock`, `AnyWarned`, `WarningMark`; `queue.warning_tip` in `Setup`) and `QueueDrawer.RowTip`: the queue column's ⚠, copied for the map.
- `MapView.TagText` → `RoomKnowledgeText.ByHeart`; `MapView.RoomSpeedTip` (already shows `queue.not_carried_tip`).
- `MapRoom.MakeTag()`: the typed-in `fontSize 15`, offset `(0,-16)`, size `(280,36)`.
- `MapStyle` (ScriptableObject), `TextRole`/`FontRole`/`UiFonts`, `UiStyle`.
- `PocketsOverlay` (`_heading`, `pockets.heading`, `ContainerText`).
- `ScreenManager.UpdateTabs`, `RunResultsPanel` Plan button.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.PlanWarnings.cs` | Edit | a query: the warned reasons for one room's stops (from the same cached walk, or grouped the way the queue column groups stops); keep the file under ~300 lines or put it in a new partial |
| `UI/MapRoom.cs`, `UI/MapStyle.cs` | Edit | tag offset and size become `MapStyle` fields (defaults = today's numbers); the tag's text gets a `FontRole` (`Small`, or a new role if no size fits) |
| `UI/UiStyle.cs`, `Data/UI/MapStyle.asset` | Edit | map node, sealed, way and reachable colours in the warm palette (`UiStyle` map colours ~l.42–47 and the asset's matching fields) |
| `UI/MapView.cs` (or new `UI/MapTagText.cs`) | Edit | ⚠ and won't-carry mark on the tag, reasons in its tooltip; planning screen only, while the plan has warnings |
| `UI/StopCard.cs` | Edit | folded block's tip lists the reasons |
| `UI/PocketsOverlay.cs` + scene | Edit | heading and pouch line anchored to the overlay (layout) |
| Planning screen in the scene | Edit | sizes and spacing of head, foot, candles and map slot |
| `game_text.txt` | Edit | new keys: `map.warning_tip`, `queue.warning_tip_folded` (names may change) |

Save format change? No.

## Steps
1. Failing EditMode tests for the per-room warnings query, then build it.
2. Move `MapRoom`'s numbers into `MapStyle` and the font role; check the tags look the same as before. Then recolour the map to the warm palette (rooms, ways, reachable; no hue colour) and take a before/after screenshot for the user.
3. ⚠ and won't-carry mark on the map tags, with the tooltip; new text keys.
4. Folded block's tip lists its reasons.
5. Planning screen sizes: head, foot, candles, map slot (read `docs/mockups/planning/` first). List every layout change in the report.
6. Anchor the pockets heading and pouch line.
7. Check: Menu → Summary from the planning screen, then the Plan button brings you back.
8. Play check at 1920×1080 and 1280×800: the popover, pockets overlay and node labels don't cover each other; no placeholder text on screen. Note anything left for the user.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `PlanWarningsTests.RoomWarnings_ListsReasonsOfStopsInThatRoom` | a warned entry's reason shows under its room only |
| `PlanWarningsTests.RoomWarnings_EmptyWhenPlanClean` | no warnings → empty, no ⚠ |
| `PlanWarningsTests.RoomWarnings_FollowsQueueEdit` | removing the warned entry clears the room's list (cache goes stale) |

## Done when
- [x] Tests above and all EditMode tests pass; compile and Console clean
- [x] In Unity, between runs with planning unlocked: queue an action in a room where it isn't offered → the planning screen's map tag for that room shows ⚠, and hovering lists the reason; fold the block → its ⚠ tip lists the reason; queue a stop in a room not known by heart → its tag shows the won't-carry mark
- [x] Menu → Summary → Plan returns to the planning screen
- [x] Both resolutions checked; layout changes listed
- [x] UI-BACKLOG Now 1–3 updated; `PROJECT_NOTES.md` placeholder list gets the new tip wording

## Notes after implementation
- Code half built 2026-10-01. Changed from the plan: the map tag's size is `MapStyle.tagFontSize` (default 15), not a `FontRole`, because `Small` is the sans font and would change the tags' look; the tag keeps the room label's serif. The user may prefer a role later.
- `MapView.cs` grew to ~425 lines; the tag text (`TagText`, `WarnMark`, `NotCarriedLine`, `RoomTip`) moves to a `MapTagText` helper in ui-034.
- Review follow-ups (not done): one shared helper for the warning-coloured reason line (`StopCard`, `QueueDrawer.RowTip`, `MapView`); a key for the ": " between an action and its reason; a `RoomWarnings` test per kind of refused trip.
- `MapStyle.asset` colours were typed into the YAML; the new tag fields appear in it once Unity saves the asset (open it in the Inspector).
- **Layout half (2026-10-01, UI session).** Steps 5 and 6 turned out to be already in the scene: the planning screen's head, foot, candles and map slot match the approved mock-up at both resolutions, and `PocketsOverlay` already hangs from the bottom of the stats chips (`_below` is set to `StatsRow/Chips`, 16 px gap) with its heading, pouch line and knowledge chips in one vertical layout, so nothing needed anchoring. The only layout change made: `NoticeToast` (Canvas) `anchoredPosition` from `(-24, 24)` to `(-24, 104)`, so a refusal notice (3.6 s) no longer covers the Begin button and the "Ends in" label on the planning screen. (Saving the scene also wrote three layout-driven `StatsRow` y values: noise, recomputed every frame.)
- **Step 7 (checked in Play):** Plan → Summary → the Summary's Plan button returns to the planning screen with the map and the queue back in their slots.
- **Step 8 (checked in Play at 1920×1080 and 1280×800):** no placeholder text on the planning screen; popover and Queue column and Begin don't cover each other; the pockets overlay isn't shown on the planning screen. Two things left for the user, both because the rooms sit close together on the map (`MapStyle.pixelsPerUnit` 220, the map can be zoomed): (a) a room's two-line tag (its "Known by heart" line plus the speed line) runs under a neighbour's frame (A Dark Hall's under The Right Corridor), and two neighbouring speed lines nearly touch; (b) a room's popover covers the tags of rooms below it, as popovers do. Options: spread the rooms (`pixelsPerUnit`), shorten the speed line, or draw tags above neighbouring frames.
- **Seen, not in scope:** the Queue column marks an *empty* first stop (the start room, not known by heart) with ⚠, while the map tag shows its won't-carry line only for a stop with actions in it. Worth a decision on whether an empty stop should be marked.
- Not tried by hand: a ⚠ reason on a map tag (the test game's plan raised none, since the walk stays silent where it can't tell); the unit tests for the per-room query cover it.
- **Follow-ups resolved (2026-10-01, UI session).**
  - *Overlapping tags:* (a) the tags moved out of each room into a `RoomTags` layer made in code just above the Nodes layer (`MapView.MakeTagLayer`, `MapRoom.PlaceTag`; the tag shows, hides and is destroyed with its room). A Dark Hall's speed line now reads in full over The Right Corridor's frame. (a) alone left neighbouring speed lines touching, so (b): the speed line is drawn at `MapStyle.speedLineSize` (new, 0.8 of the tag size; helper `UiStyle.Sized`), which opens a clear gap at both resolutions. Before/after: `docs/mockups/planning/2026-10-01-map-tags-{1920,1280}-before-a-b.png`. (c) not done: at 1280×800 the 80% line is about 8 px high, and a tag can still cross a neighbour's frame or name (the Left Corridor's "Known by heart ⚠" runs into A Dark Hall's "×4.2"). `MapStyle.pixelsPerUnit` is **150** in the asset (220 is only the code default): raising it is the proposal, waiting for the user.
  - *Empty-stop ⚠ (the user, 2026-10-01):* an empty stop is marked nowhere. `StopCard.MarksNotCarried` decides it for both the Queue column and the map tag (tests: `StopCardTests`, 3 cases). In practice only stop 1 (the start room) can be empty. Decisions log 2026-10-01 *An empty stop isn't marked as won't-carry*.
  - Side effect: a room's tag no longer swells with it after a search step (it's on another layer). Hovering a warned folded block was left to a later play test (the user, 2026-10-01).
  - *⚠ on a map tag, by hand:* checked in Play (no save slot). Dev jump to *Before the gem*, four runs ended, The Left Corridor set to 4 runs; plan Dark Hall → Left Corridor → *Light a candle* without making the flint. The tag reads "Known by heart ⚠", and the room's hover tip lists "needs 1 Flint and steel" under "⚠ Will be refused here", above the speed text. Folded block: a run (vitality topped up from code) carried Dark Hall and Left Corridor as blocks; after removing the flint row, the folded Left Corridor block shows ⚠ on its name and badge line, and its tip text is "⚠ Some actions here won't start: Light a candle: needs 1 Flint and steel". That tip was read from the card, not seen in the tooltip panel (a simulated hover doesn't open it while the editor is in the background): the user's hand check.

- **Wrap-up (2026-10-01):** the user considered the remaining hand checks (map colours, tag fix, hovering a warned folded block) done. Not decided: spreading the rooms (`MapStyle.pixelsPerUnit` 150 → ~220); carried to `UI-BACKLOG.md` Now.
