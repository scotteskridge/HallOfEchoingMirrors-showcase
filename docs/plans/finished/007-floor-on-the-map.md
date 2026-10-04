# 007 — Floor on the map

**Status:** Done (2026-09-28)
**Design:** GDD v0.5 §13a *Floor piles are map data, not inventory data* (every room's pile drawn on the map, only when non-empty; pips coloured by kind); decisions-log 2026-09-28 *Floor on the map: kinds and dots*; roadmap row 5
**Mockup:** `DesignNotes/mockups/Act I — the hall, monochrome@1x.png` (A Dark Hall: the dots sit just after the progress bar under the room's name): read it before step 5

## Goal
Any room with something on its floor shows a small row of coloured dots on the map, one per kind of item lying there, so the player can see what's cached in rooms Clara isn't in. Hover a dot for the item and its count. A dot swells briefly when more is put down.

## Out of scope
- Removing the inventory's Floor rows (the user's call: kept until plan 6 replaces the panel).
- A Pathos kind (no pathos items yet; its dot would need the item's hue): backlog line added.
- Colouring the popover's floor chips to match (small; a follow-up if wanted).
- The room card's action count, "by heart" and mastery badges (not built; the decisions log's open question about fitting them all waits for them).
- Piles kept between runs (backlog *Floor stashes kept between runs*).

## Design assumptions
- **One dot per kind of item on the floor** (6 candles + 2 wisps = 2 dots). The user called the dots a placeholder: the easiest version.
- **Four kinds** (decisions-log 2026-09-28): Restorative (lavender): Wisp, Phial of memory. Light (gold): Hanging candle. Tool (grey): Flint and steel. Keepsake (pale rose): Roland's ring, The tome. **Tool is the default** for any item not given a kind.
- Her own room shows its dots too (the same rule everywhere).
- Dot order: by kind (Restorative, Light, Tool, Keepsake), then in the floor's own order, so dots don't shuffle as counts change.
- Swell on a rise only (the rule: numbers that rise glow where shown); a dot that appears doesn't swell; nothing happens when items are taken.
- Kind colours, dot size, spacing and offset from the room go in the Map Style asset: look, not balance, so no Balance Sheet column.

## Reuse
- `Simulation.FloorAt(room, into)` (`Core/Simulation.Floor.cs`) and `Loop.Floor` (`Core/LoopState.cs`): what lies where; polled each frame as `RoomPopover` does (there's no floor-changed event)
- `MapRoute` (`UI/MapRoute.cs`): the pattern for a map layer (`_map.AddLayer` in `Start`, `TemplateList<Image>`, `ToolTip.On`, re-subscribe on `SimulationChanged`)
- `MapView.PositionOf`, `MapView.RoomRect`, `MapStyle`, `Glow.Flash(image, tint: false)`, `UiStyle`, `UiText`
- `RouteLayout` / `RouteLayoutTests`: the pattern for pure, tested UI maths
- `ItemSections` (Editor): the new item field goes in one section (`ItemSectionsTests` checks)
- `GreyboxSetup` + `EditorUiFactory` (Step 81 in `git show 6271b55^:Assets/Editor/GreyboxSetup.cs` as the model)

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/ItemKind.cs` | New | `enum ItemKind { Tool, Restorative, Light, Keepsake }`: how an item reads on the map |
| `Core/ResourceDefinition.cs` | Edit | `[SerializeField] kind` field with tooltip, and `Kind` |
| `Editor/ItemSections.cs` | Edit | `kind` in the section that holds display settings |
| `UI/FloorDots.cs` | New | Pure static maths: which dots a room's floor gives, in order; which counts rose since last frame |
| `UI/MapFloor.cs` | New | The map layer: one row of dots per room with a pile, tooltips, swell on a rise |
| `UI/MapStyle.cs` | Edit | Floor dots: one colour per kind, `KindColour(kind)` (fails loudly on an unknown kind), dot size, spacing, offset |
| `game_text.txt` | Edit | new key `map.floor_dot_tip` ("{amount} {item} on the floor") |
| `Editor/GreyboxSetup.cs` | Edit | Step 82: floor layer, dot template, wire `MapFloor`, set the six items' kinds |

Save format change? No (the kind lives on the item asset).

## Steps
1. Failing EditMode tests for `FloorDots` (below).
2. `ItemKind`, the item field, `ItemSections`; `FloorDots` until the tests pass.
3. `MapStyle` fields and `KindColour`.
4. `MapFloor`: dots per room from the floor, placed by the room, with tooltips; hidden when the floor's empty.
5. Swell on a rise; check against the mockup.
6. Setup step: *Hall of Echoing Mirrors → Setup → Step 82: Floor on the map*.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `FloorDotsTests.EmptyFloor_NoDots` | a room with nothing on its floor draws nothing |
| `FloorDotsTests.OneDotPerKindOfItem` | 6 candles + 2 wisps give two dots |
| `FloorDotsTests.Dots_OrderedByKind` | a wisp sorts before a candle before flint, whatever order they were put down in |
| `FloorDotsTests.CountRose_Swells_CountFell_DoesNot` | only a rise is reported; a new item isn't |
| `MapStyleTests.EveryKind_HasAColour` | each `ItemKind` value gets a colour (a new kind can't be forgotten) |

## Done when
- [x] Tests above pass (and the existing suite); compile and Console clean
- [x] In Unity: run Step 82; play; fill her pockets and put things down in A Dark Hall, walk on → gold and lavender dots stay under A Dark Hall; hover → "6 Hanging candle on the floor"; put down more → the dot swells; pick it all up → the dots go; a new run → no dots
- [x] Map in play compared with the mockup; differences listed for the user (look only)
- [x] Roadmap, BACKLOG updated; `ui.md` rules line proposed for `MapFloor`

## Notes after implementation
- **`kind` is a public field** (`item.kind`), not a private field with a `Kind` property: the class's other Inspector settings are public fields. It sits in ItemSections' "An object in her pockets" section (only pocketed items can lie on a floor), so the Balance Sheet shows it as a *Kind* column there by itself.
- **Two extra tests:** `Dots_OfTheSameKind_KeepTheFloorsOrder`, and `EveryKind_GetsADot` (reviewer: a kind missing from `FloorDots.Order` would otherwise only fail at runtime). The placement test is `Row_StartsAtTheRoomsLeftEdge_AndGrowsRight`.
- **Playtest fix, placement:** the row first hung from the room's bottom-right, where Clara's token and its CLARA label rest; the label covered the last dot (keepsakes, and tools when no keepsake), so the ring and the flint seemed not to show in her room. It now hangs from the **bottom-left** and grows right. The mockup puts the dots after the search bar on the right; the build's bar is inside the room, so they can't sit there. *Floor Dot Offset* in the Map Style moves them.
- **Playtest fix, tooltips:** `MapFloor` showed each room's dots as it went, which switched later rooms' dots off and on every frame and cancelled their tooltips (only the first room's worked). It now gathers every room's dots, then shows them once. No EditMode test covers this (it needs a scene).
- **Known, cosmetic:** dots share one list of copies across rooms, so a change in an earlier room can shift a running swell or hover to the neighbouring dot (backlog).
- **Same playtest batch, outside this plan:** Roland's ring now comes out of a mirror (Step 83: *Instantiate Roland's ring*, placeholder hover text for the five mirror actions, new story-feed lines, drafts beside the user's ring prose). Decisions log 2026-09-28 *Roland's ring comes out of a mirror*.
- **Not done here:** the mockup comparison beyond placement, and the `ui.md` line for `MapFloor` (proposed to the user; rules files need their OK).
