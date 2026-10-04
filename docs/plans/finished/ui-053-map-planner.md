# ui-053 — Map Planner

**Status:** Done (built and signed off 2026-10-02)
**Design:** GDD §13a *A room's shape says what kind of room it is* (context only); decisions log 2026-10-02 *Mirror shapes carry meaning* ("next acts are laid out in Unity"); UI-BACKLOG *Next* 15, first half
**Pillar:** none: tooling (it lets Act II be laid out so the map can be learned by heart, pillar 4, later)

## Goal
In Unity, *Hall of Echoing Mirrors → Tools → Map Layout* opens the map preview, and *Add Room* creates a room ticked **Planned**: drawn faded in the preview with a one-line planning note, joined by ways, dragged into place, and completely ignored by the game until the tick is cleared.

## Out of scope
- Mirror shapes per kind, per-room size, the game map using them: plan **ui-054** (UI-BACKLOG *Next* 15, second half).
- Any change to what the player sees. Planned rooms never reach the game, the save or the Summary.
- Validating a room when it leaves Planned (tasks, depth, reachability); the existing tests catch the depth.

## Design assumptions
- A planned room is a normal `NodeDefinition` asset in `GameContent.nodes` with a `planned` tick, so unticking it is the whole "promote to the game" step (no moving assets between lists).
- A way from a real room to a planned room is ignored by the game and drawn only in the preview (the user, 2026-10-02). A planned room's own ways, including *both ways*, give real rooms nothing.
- The planning note is editor-only text for the author, never shown to players, so it stays in the asset, not `game_text.txt`.
- **Test scope change for the user to OK:** `ShippedRulesTests`, `ShippedSearchRoundsTests` and `TutorialContentTests` walk the shipped rooms; they will walk only *playable* rooms. This is not a weakened test: planned rooms aren't game rooms. A room that loses its tick is checked as before (the depth test then asks for its depth).

## Reuse
- `MapLayoutEditing` in `Assets/Editor/MapLayoutEditing.cs`: the preview; dragging already saves `mapPosition` with Undo; its own known-at-start search.
- `MapViewEditor` in `Assets/Editor/MapViewEditor.cs`: the *Edit map layout* / *Stop editing* / *Frame map* buttons.
- `MapPreviewRoom` + `MapPreviewRoomEditor`: selecting a preview room shows its settings (Planned and Note appear there for free).
- `NodeDefinition` (`Assets/Scripts/Core/NodeDefinition.cs`), `GameContent` (`GameContent.cs`), `Simulation.AllNodes` / `RoomsOnMap` (`Simulation.Places.cs`).
- `ContentIds` (`Assets/Editor/ContentIds.cs`): stamps the new room's `Id`.
- `BalanceSheetWindow.Places.cs`: the room rows.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `NodeDefinition.cs` | Edit | `planned` (bool) and `planningNote` (string), with tooltips |
| `GameContent.cs` | Edit | `PlayableNodes`: `nodes` without planned rooms; the one place the filter lives |
| `Simulation.Places.cs`, `.Unlocks.cs`, `.Attributes.cs`, `ContentIndex.cs` | Edit | read `PlayableNodes`; skip any way whose `to` is planned |
| `MapLayoutEditing.cs` | Edit | planned rooms and their ways drawn faded, labelled "(planned)" with the note beneath; known-at-start search skips them |
| `MapPlannerMenu.cs` (Editor) | New | *Tools/Map Layout* (ticked while open) and *Tools/Add Planned Room* |
| `MapViewEditor.cs` | Edit | *Add Room* button beside *Edit map layout* |
| `BalanceSheetWindow.Places.cs` | Edit | Planned and Note columns |
| `ShippedRulesTests`, `ShippedSearchRoundsTests`, `TutorialContentTests` | Edit | walk `PlayableNodes` (see assumptions) |

Save format change? No. Saves name rooms by `Id` through `ContentIndex`, which won't index planned rooms; a save naming a room later re-ticked Planned warns and skips it, as for any missing content.

## Steps
1. Failing EditMode tests (below) for planned rooms in Core.
2. Core: the two fields, `GameContent.PlayableNodes`, every game read moved to it, ways into planned rooms skipped. Shipped-content tests switched.
3. Preview: faded planned rooms and ways, "(planned)" label and note, known-at-start skips them.
4. *Tools → Map Layout* menu entry toggling the preview (editor scripts may use `Find*` to reach the Map View; warn in the Console if the scene has none).
5. *Add Room*: menu entry and Inspector button create `Assets/Data/Places/New Room N.asset` (Planned ticked, kind Hall), stamp its `Id`, add it to `GameContent`, place it at the preview's centre, select it; one Undo step.
6. Balance Sheet: Planned and Note columns on the Places tab.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `PlannedRoomTests.PlayableNodes_SkipsPlannedRooms` | the filter |
| `PlannedRoomTests.PlannedRoom_NeverOnTheMap` | `AllNodes`/`RoomsOnMap` leave it out, even if `alwaysOnMap` |
| `PlannedRoomTests.WayIntoPlannedRoom_IsNeverOffered` | a real room's way to it can't be found, opened or travelled |
| `PlannedRoomTests.BothWaysFromPlannedRoom_GivesNoWayBack` | a planned room's ways don't reach real rooms |
| `PlannedRoomTests.ContentIndex_SkipsPlannedRooms` | saves can't name it |
| `PlannedRoomTests.UntickedRoom_JoinsTheGame` | clearing the tick is enough |

## Done when
- [ ] Tests above pass, with the full EditMode suite; compile and Console clean
- [ ] In Unity: *Hall of Echoing Mirrors → Tools → Map Layout* shows the preview (menu ticked); *Add Planned Room* adds a faded "New Room 1 (planned)"; type a note, add a way from the Junction to it, drag it; press Play: the game map, ways and Summary are exactly as before; untick Planned: the depth test fails until it has a depth (expected)
- [ ] decisions-log line; no GDD change (tooling); no changelog line (players see nothing); rules line proposed for `.claude/rules/` if wanted ("game code reads `PlayableNodes`, never `nodes`")

## Notes after implementation
Built 2026-10-02 in the UI lane; the user signed it off the same day with the new rooms still Planned (the verbs that unlock them aren't made yet).
- **Differs from the plan:** `TutorialContentTests` loads fixed assets and never walks the node list, so only `ShippedRulesTests` and `ShippedSearchRoundsTests` switched to `PlayableNodes`. Added `Way.IntoPlannedRoom` and skips in `FindWay`, `NeighboursOf`, `LearnKnownWays`, `CheckForNewWays`, `RememberFoundWays`, `SaveSerializer` and `ContentIndex`. `PlannedRoomTests` has 8 tests (the plan's 6, plus `PlayableNodes_FollowsLaterChanges` and `WayIntoPlannedRoom_IsNeverAnnounced`).
- **Added after play-testing by the user:** the Planning section is first in the room's Inspector (under a new *Room* header); the Tools menu and later rebuilds never move the Scene view; the preview room's script is the first component under the Rect Transform; adding a way keeps the preview room selected.
- **Test renames:** the user renamed the Hall Mirror asset to *The Smoky Mirror* (`TheSmokyMirror.asset`); `ShippedLabTests`, `TutorialContentTests`, `ShippedRulesTests` and `Balance/ActIRoute` follow it.
- **Left for later:** a "connect to…" button for ways; a test that fails if game code under `Assets/Scripts` reads `.nodes`.
- **Tests:** full EditMode suite 921 total, 920 passed, 1 skipped (the explicit balance probe), 0 failed.
- **Found in review and fixed:** an action listed only at a planned room would have read as "listed at no room" and been offered everywhere (`IsAvailableAt`); now `GameContent.IsListedInAPlannedRoom` makes it not doable anywhere (test `TaskListedOnlyInAPlannedRoom_IsNotOfferedAnywhere`). *Add Room* calls `GUIUtility.ExitGUI()`; the preview's component reorder was checked to record no Undo step. Tests after the fix: 922 total, 921 passed, 1 skipped, 0 failed.
- **Not covered by a test (reviewer's polish):** an item needed only by a way into a planned room still counting as in use; a hidden way into a planned room never entering `FoundWays` on load.
