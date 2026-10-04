# ui-054 — Mirror shapes and per-room size

**Status:** Approved
**Design:** GDD §13a *A room's shape says what kind of room it is*; decisions log 2026-10-02 *Mirror shapes carry meaning*; UI-BACKLOG Next 15 (second half of *Map Planner and mirror shapes*; first half was ui-053)
**Pillar:** 4, *The hall remembers* (the player reads the map by heart); 3, *Every run teaches something* (learning to read the shapes is part of learning the hall)

## Goal
Every room on the game map and in the editor's Map Layout preview is drawn as its kind's mirror shape: Hall a rectangle, Constructed an octagon, Clara memory an oval, Roland memory an arch. Each room has its own size, which you set by dragging its edge in the preview.

## Out of scope
- The three frame PNGs in `Assets/Art/MirrorFrames/` and real mirror art (stage 3).
- A per-room shape override (decided 2026-10-02: the kind decides).
- An outline-only look for planned rooms (decided: the 45% fade is enough).
- New room kinds; colour per pool (UI-BACKLOG Next 6); reflections in the rooms (UI-BACKLOG-later); the "connect to…" button (Next, *Map planner follow-ups*).

## Design assumptions (settled with the user 2026-10-02 unless marked)
- **Shapes:** curves mean memories, corners mean the Hall's own rooms. Hall = rectangle with small rounded corners (today's look); Constructed = octagon (cut corners); Clara memory = oval; Roland memory = arch (flat bottom, round top).
- **Size** is one number per room, `mapScale`. At 1 a Hall room is today's 150×48. Each shape asset has its own base size, so a kind keeps its proportions. *Placeholder rule:* base sizes for the oval, arch and octagon (start at about 170×60, so a name fits inside the curve); set on the assets.
- **Size range** *(placeholder rule)*: 0.75 to 2, in steps of 0.05, as fields on `MapStyle`.
- **Tags** stay centred under the room's lowest edge (`MapRoom.PlaceTag` already does this) and move down as the room grows.
- **Lines** need no change: they already run centre to centre under the rooms, so any shape hides its own line ends.
- **The by-heart glow matches the room's shape and size** (the user, 2026-10-02): it is the room's own outline (same `MirrorShape`), drawn `byHeartGlowMargin` pixels larger on every side with a soft edge, replacing the rectangular sliced sprite. It is stretched to the room's box, so whenever the room is resized (a new `mapScale`, a drag in the preview, Undo) the glow resizes with it; the margin stays the same in pixels, so a big room doesn't get a thicker glow.
- **Clicks** only count inside the shape, not in an oval's empty corners.

## Reuse
- `NodeKind` and `NodeDefinition.kind` (`Assets/Scripts/Core/NodeDefinition.cs`): the four kinds already exist. No new rule data.
- `MapStyle` (`Assets/Scripts/UI/MapStyle.cs`, asset `Assets/Data/UI/MapStyle.asset`): holds the per-kind shapes and the size range.
- `MapRoom` (`Assets/Scripts/UI/MapRoom.cs`): builds the label, explore bar, glow and tag; gets one `ApplyShape` that both the game and the preview call.
- `MapView.LayOut` / `PositionOf`: where each room's size gets set.
- `MapLayoutEditing.Tick` (`Assets/Editor/MapLayoutEditing.cs`): already turns a dragged position into `mapPosition` with Undo; a resize works the same way.
- `BalanceSheetWindow.Places.cs`: the rooms table gets a "Map size" column next to "Map position".
- Nothing procedural exists yet (searched: no custom `Graphic`, `OnPopulateMesh` or generated sprite helper).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Scripts/UI/MirrorShape.cs` | New | ScriptableObject: outline type (`Rectangle`, `Octagon`, `Oval`, `Arch`), base size, corner amount, content inset (where the name and explore bar fit) |
| `Assets/Scripts/UI/MirrorOutline.cs` | New | Plain C#: the outline points for a shape in a box, and `Contains(point)` |
| `Assets/Scripts/UI/MirrorShapeImage.cs` | New | An `Image` subclass that draws the outline as a mesh, with an optional soft edge (for the glow); click test uses `Contains` |
| `NodeDefinition.cs` | Edit | `[Min(0.1f)] public float mapScale = 1f;` under `[Header("Map")]`, with a tooltip |
| `MapStyle.cs` | Edit | `[Header("Room shapes")]`: one `MirrorShape` per kind, `ShapeFor(NodeKind)` (throws on a missing shape), `minRoomScale`, `maxRoomScale`, `roomScaleStep`, `SnapRoomScale(float)` |
| `MapRoom.cs` | Edit | `ApplyShape(MirrorShape, float scale)`: size, outline, label and explore bar inside the content inset; the glow becomes a `MirrorShapeImage` with the same shape and a soft edge, stretched to the room plus the margin, so it follows every resize |
| `MapView.cs` | Edit | `LayOut` calls `ApplyShape(Style.ShapeFor(node.kind), node.mapScale)` |
| `MapLayoutEditing.cs` | Edit | Preview calls the same `ApplyShape`; `Tick` reads a resize, snaps it, writes `mapScale` with Undo and resets the box to the kind's proportions |
| `BalanceSheetWindow.Places.cs` | Edit | "Map size" column |
| `Assets/Editor/MirrorShapeSetup.cs` | New | One-click setup (below) |
| `Assets/Data/UI/MirrorShapes/*.asset` | New (by setup) | Hall, Constructed, Clara Memory, Roland Memory |
| `MapPanel.prefab` | Edit (by setup) | the node template's `Image` becomes a `MirrorShapeImage` (same colour; the Button's target kept) |

New content fields: `NodeDefinition.mapScale` (Balance Sheet column "Map size"); `MirrorShape` assets (look data, not balance: no column).

Save format change? **No.** `mapPosition` and `mapScale` live only in the content assets; saves store room `Id`s, never layout.

## Steps
1. **Failing tests first** (table below): `MirrorOutlineTests`, `MapStyle` scale snapping, every kind has a shape on the real `MapStyle` asset, every room's `mapScale` within range.
2. `MirrorOutline` and `MirrorShape` until the outline tests pass.
3. `MirrorShapeImage`: mesh from the outline (a fan of triangles from the centre), an optional soft edge ring that fades to clear, and the click test.
4. `NodeDefinition.mapScale`, `MapStyle` room shapes and size range, Balance Sheet column. Existing rooms read 1 (the field's starting value), so nothing moves.
5. **Setup:** *Hall of Echoing Mirrors → Setup → Mirror Shapes* creates the four shape assets, assigns them on `MapStyle` and swaps the prefab's node `Image` for `MirrorShapeImage`. Safe to run twice.
6. `MapRoom.ApplyShape` and `MapView.LayOut`: the game map shows the shapes. Check tags, floor dots, explore bars and the by-heart glow on each shape.
7. Preview: `MapLayoutEditing` uses `ApplyShape`; dragging an edge with the Rect Tool resizes, snapped and kept in proportion, with Undo.
8. Docs: list the placeholder rules in `PROJECT_NOTES.md`; changelog line; `/sync-state`; GDD §13a and decisions log (proposed below).

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `MirrorOutlineTests.Oval_ContainsCentre_NotCorners` | the oval's empty corners don't count as the room |
| `MirrorOutlineTests.Octagon_CutsCorners_KeepsEdgeMidpoints` | the octagon is cut at the corners, full at the sides |
| `MirrorOutlineTests.Arch_FlatBottom_RoundTop` | bottom corners inside, top corners outside |
| `MirrorOutlineTests.Rectangle_FillsItsBox` | the Hall shape still fills its box |
| `MirrorOutlineTests.Outline_StaysInsideItsBox` | every shape's points lie within the box (nothing draws past it) |
| `MapStyleTests.SnapRoomScale_ClampsAndSteps` | 0.5 → 0.75, 3 → 2, 1.03 → 1.05 |
| `MapStyleTests.Asset_HasAShapeForEveryKind` | each `NodeKind` value has a shape on the real asset; a missing one fails loudly |
| `MapStyleTests.Rooms_MapScaleInRange` | every room in `GameContent` sits within the size range |
| `MapRoomShapeTests.Glow_MatchesShapeAndSize` | after `ApplyShape(oval, 1.5)` the glow uses the oval and its box is the room's box plus the margin on each side; after a second `ApplyShape(oval, 0.75)` it shrinks with the room |

## Done when
- [ ] Tests above and all other EditMode tests pass; compile and Console clean
- [ ] In Unity, Play: the four Hall rooms (Smoky Mirror, Junction, Left and Right Corridors) are rectangles, The Dark Corridor with Hanging Mirrors an octagon, The Mirror's Laboratory (Clara memory) an oval; names, explore bars, tags and the by-heart glow sit right on each; clicking an oval's empty corner does nothing
- [ ] *Tools → Map Layout*: the same shapes, planned rooms faded; dragging a room's edge resizes it in steps, kept in proportion, and a by-heart room's glow keeps hugging its outline as it grows or shrinks; Ctrl+Z undoes it; the Balance Sheet shows the new size
- [ ] Layout changes listed in the report (node template's graphic, label and bar positions per shape)
- [ ] decisions log and GDD §13a updated; changelog line; placeholder rules in `PROJECT_NOTES.md`

## Notes after implementation
<!-- filled in at wrap-up -->
