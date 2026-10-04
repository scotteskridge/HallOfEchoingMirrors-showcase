# 008 — Pockets and stats on the main screen

**Status:** Done (2026-09-28; changed during building: see Notes after implementation)
**Design:** GDD v0.5 §5 *Pockets and carry capacity*, §6 (*Show the multiplier, not the level*), §13a *Proposed layout* and *Floor piles are map data*; roadmap step 6 (`docs/plans/ui-rework-roadmap.md`); decisions-log 2026-09-28 *Pockets and stats: the choices (plan 6)*

## Goal
While routing on the map, the player can see what Clara carries at a glance (a small overlay on the map's left side, with "3/5 pockets") and her stats and skills as one row of effects under the vitality bar, which they can switch off. The last step of the UI rework's Part 1.

## Out of scope
- Put down or Pick up from the overlay or the Pockets tab (stays in the room popover; backlog).
- Anything in the bottom strip besides the bookmark tabs; standing orders; realm speed.
- Layout polish or moving existing UI (the user's layout pass comes after Part 1).
- The wider refactor *Move UI rules into Core* (backlog Next 1): only the pocket list query moves here.
- Item sizes other than 1; carry-out persistence.

## Design assumptions
- **Overlay contents:** only what's in pockets and containers (the pouch's phials on their own line, "Pouch: Phial 4/10"). Knowledge and other non-pocketed items stay in the Pockets tab only.
- **Chip order:** the order the inventory panel uses today.
- **Stat chips:** a short effect per stat (new `stats.brief_*` keys, e.g. "Endurance 120 vitality"); skills drop the word "speed" ("Wayfinding ×2.10"), since every skill is a speed.
- **Toggle:** a small button at the vitality bar's right end shows and hides the row; on by default; kept in PlayerPrefs for every save slot (as the popover's place).
- **Placement:** both are overlays on the map (they don't move the top bar or the map). Only the chips catch the mouse, so clicks between them reach the rooms beneath.
- **Motion:** a chip glows when its count rises (pockets) or its level rises (stats), reusing `Glow.Flash` and `UiStyle.Levelling`.

## Reuse
- `Simulation.PocketsUsed`, `PocketSlots`, `ContentsOf`, `AmountOf`, `InPockets` (`Simulation.Resources.cs`, `Simulation.Floor.cs`): the numbers.
- `InventoryPanel.Collect` / `TipFor` (`UI/InventoryPanel.cs`): the ordering and tooltip text, moved behind a shared query.
- `ClaraTips.StatEffect` / `SkillEffect` / `StatTip` / `SkillTip`, `Simulation.KnownSkills`, `LevelOf` (`UI/ClaraTips.cs`, `Simulation.Attributes.cs`).
- `MapStyle.KindColour` for chip edges; `UiStyle` (ChipBackground, Small); `TemplateList`, `ToolTip.On`, `Glow`.
- `SlideDrawer.OpenPage` to open the Stats tab on a chip click.
- `EditorUiFactory.MakeChipRow` / `MakeChip` / `MakeText` / `SetTip` / `Wire` for the setup step.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.Resources.cs` | Edit | `CarriedItems(List<CarriedEntry>)`: pocketed items and container contents in display order |
| `Core/CarriedEntry.cs` | New | struct: item, amount, container (or none) |
| `UI/PocketsOverlay.cs` | New | the left overlay: heading, chips, pouch line, glow, tooltips |
| `UI/StatsRow.cs` | New | the row of chips; click opens the Stats page; toggle with PlayerPrefs |
| `UI/ClaraTips.cs` | Edit | `StatBrief`, `SkillBrief` |
| `UI/InventoryPanel.cs` | Edit | drop the Floor section; use `CarriedItems` |
| `Editor/GreyboxSetup.cs` | Edit | Step 84: build overlay, row and toggle; rename the Inventory tab's label key |
| `game_text.txt` | Edit | `pockets.heading`, `pockets.pouch_line`, `stats.brief_*`, `tips.stats_toggle`, `tips.pockets_overlay`, `tips.drawer_pockets` |

New content fields: none (look settings only, in `UiStyle`). Save format change? No.

## Steps
1. Failing EditMode tests for `CarriedItems`, then implement it; point `InventoryPanel` at it and remove its Floor rows.
2. `ClaraTips.StatBrief` / `SkillBrief` and their text keys.
3. `PocketsOverlay`: chips edged by kind, "n/m pockets", pouch line, glow on a rise, tooltips.
4. `StatsRow`: chips, glow on level-up, click → Stats page, toggle button with PlayerPrefs.
5. Setup Step 84 (*Hall of Echoing Mirrors → Setup → Step 84: Pockets and stats*): builds both overlays and the toggle, wires them, relabels the Inventory tab *Pockets*.
6. Play through; compile, Console and all EditMode tests clean.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `PocketTests.CarriedItems_ListsPocketedItemsWithAmounts` | the overlay's rows match what's in pockets |
| `PocketTests.CarriedItems_PouchContentsNamedWithContainer` | phials in the pouch are listed under it, not as pockets |
| `PocketTests.CarriedItems_SkipsKnowledgeAndFloor` | knowledge and floor piles aren't in the list |
| `PocketTests.CarriedItems_EmptyAtRunStart` | nothing carried → empty list, heading still reads 0/5 |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity: after Step 84, press Play; the map's left edge shows "0/5 pockets"; make a candle and "Candle 1" appears edged in gold and glows; take the satchel and the heading's total grows by 7 (5 → 12; the satchel itself fills one)
- [x] A row of stat and skill chips sits under the vitality bar; its toggle hides and shows it, and the choice survives a restart; clicking a chip opens the drawer's Stats tab
- [x] The drawer's tab reads *Pockets* and has no Floor rows; clicks between overlay chips still reach rooms
- [x] Roadmap step 6 ticked; BUILD-STATE line 34 updated via `/sync-state`

## Notes after implementation
- **After the first look (user, 2026-09-28; decisions-log *The pockets overlay replaces the Pockets tab*):** the satchel shows on the overlay (`CarriedItems` lists objects that add pockets after the pocketed items; it still takes no pocket, so the heading goes 0/5 → 0/12). The drawer's Pockets tab and page are switched off (Step 84 does it), and the overlay shows everything the tab did: a *Knowledge and progress* part and in-use countdowns. It can be dragged by any chip or heading and stays where it's left (PlayerPrefs `HallOfEchoingMirrors.PocketsOverlay.*`); dragged, it may hang partly off the map (see DragToMove below). A ContentSizeFitter keeps its box the size of its contents. The floor where she is came back too (user, later the same day), as an *On the floor here* part between what she carries and knowledge: chips edged by kind, no glow (as the map's dots), tooltips from the old floor wording (`tips.pockets_floor`, `tips.pockets_floor_chip`, pointing to the room popover for Pick up).
- **Moved out of InventoryPanel:** the item tooltips (`ItemTips`, a static class like `ClaraTips`), and "which container holds this kind" as Core's `Simulation.ContainerFor` (tested in `ContainerTests`). `InventoryPanel` still works but is switched off; deleting it is in the backlog.
- **`CarriedItems` lists a container as its own entry** (`CarriedEntry.IsContainer`), followed by what's in it. That's so an empty pouch still gets its line and heading.
- **The Pockets label:** the scene label keys stay as they were (`scene.mainscreen_inventorytab`, `scene.mainscreen_inventory`). They're named after the objects by the Scene Labels tool, so only their words changed to "Pockets" (the drawer page's heading too, to match the tab). Step 84 also moves the tab's hover text to `tips.drawer_pockets` (moot now the tab is off). The unused floor keys (`inventory.section.floor`, `floor_limit`, `tip.section_floor`, `tip.floor`) and `tips.drawer_inventory` were removed.
- **Pouch line:** "Pouch of phials: Phial of memory 4/10", "…: Phial 3, Wisp 1 (4/10)" if it ever holds two kinds, or "…: empty 0/10". It's one chip, edged in the colour of what the container holds. The wording is in `pockets.pouch_line` for tuning.
- **Stat chips:** Endurance shows her whole vitality ("Endurance 120 vitality"). Attunement reads "(costs off)" while action costs are off. Scholarship shows "+0 per study" until it earns one. A chip glows (`Glow.Flash`) on a level or mastery rise. `UiStyle.Levelling` wasn't needed.
- **Toggle:** a "−"/"+" button inside the vitality bar at its right end (the bar's words are centred). PlayerPrefs key `HallOfEchoingMirrors.StatsRow.shown`.
- **Layering:** both overlays sit in `Middle → Map` just after MapPanel (a prefab, so not inside it). The room popover lives inside MapPanel, so if it's dragged over the stats row or the pockets column, they draw on top of it.
- **DragToMove (user's request):** dragging is one reusable component, `UI/DragToMove.cs`: drag anywhere on the panel that catches the mouse, even partly off the parent to get it out of the way (at least *Keep Visible*, 80 px, stays in view each way, and its top never goes above the parent's: `PopoverPlacement.KeepGrabbable`, tested), place remembered under an Inspector key (PlayerPrefs), and a double-click puts it back where it started. Its place is its top-left corner from an origin (the parent's top-left corner by default; the room popover sets its room's centre and places itself, `OwnerPlaces`). The popover and the overlay both use it, keeping their old keys (`HallOfEchoingMirrors.RoomPopover`, `…PocketsOverlay`), so places already set survive. The placement sums from any origin are in `PopoverPlacement` (`LowerLeftAt`, `TopLeftFrom`), tested. Step 84 adds the component to both.
