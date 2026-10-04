---
# Editor-only code: setup steps, the Balance Sheet, custom Inspectors and tools.
paths:
  - "Assets/Editor/**"
---
# Editor tools

Namespace `HallOfEchoingMirrors.EditorTools`, assembly `HallOfEchoingMirrors.Editor` (the tests reference it). None of it ships in the game.

## Setup steps (`GreyboxSetup.cs`)
- The user prefers Claude to do scene and asset setup (creating assets, building UI, wiring Inspector fields) as a one-click menu command: *Hall of Echoing Mirrors → Setup → Step N: …* in `GreyboxSetup.cs` (empty between steps).
- Build them from the helpers in `EditorUiFactory.cs` (`MakeText`, `MakeButton`, `MakeDisplayBar`, `MakeFrame`, `MoveInto`, `AddStack`, `FixHeight`, `MakeChipRow`, `MakeChip`, `MakeScrollList`, `MakeSideScroll`, `MainPage`, `SwitchOff`, `SetTip`, `Wire`, `GetOrCreateAsset`, `FindOrCreateResource`, `MakePrefab`…). Add a helper there rather than duplicating one.
- Each step is **safe to run twice**, **undoable** (`Undo.RecordObject`), and **never overwrites user-tuned values**: change a value only if it still has its old one. Log what it did.
- **Retire a step** once the user has run it and committed the result: restore the empty file with `git show 7a0dc41:Assets/Editor/GreyboxSetup.cs > Assets/Editor/GreyboxSetup.cs` and commit that separately.
- **Retire one-shot tools too:** a menu command that is meant to run once (a rescale, a migration, a bulk fix) says so in its summary comment, and is deleted, with its `.meta`, once the user has run it and committed the result. Delete its marker field and its tests with it, and commit that separately. Repeatable tools (*Import Blurbs*, *Stamp Content IDs*, *Apply UI Fonts*) stay.
- Afterwards, explain briefly what it built and where to look in the Hierarchy and Inspector. Save by-hand steps for when a new Unity concept is worth learning, and say so.
- A copied asset (`AssetDatabase.CopyAsset`) keeps the original's `Id`: call `StampId()` on the copy.

## The Balance Sheet (`BalanceSheetWindow.cs`)
- *Hall of Echoing Mirrors → Balance Sheet*: LoopSettings (every field, drawn automatically), then rows per task (common verbs first), switch, skill, place, way and search find, and the items: one table of every item, then a table per ability.
- Deriving the numbers from *Balancing Formulas v0.1* is the goal; not built.

## Item sections (`ItemSections.cs`, `ResourceDefinitionEditor.cs`)
- `ItemSections.All` is the one list grouping item settings; the item Inspector opens only the sections an item uses, and the Balance Sheet's item tables come from the same list.
- **A new `ResourceDefinition` setting must go in exactly one section** (`ItemSectionsTests` fails otherwise).

## Other tools
- **Map layout** (`MapLayoutEditing.cs`; *Edit map layout* on the Map View, also *Stop editing* and *Frame map*): an unsaved Scene-view preview built from the room assets; dragging a room writes its `mapPosition`. `MapViewEditor` also shows the Map Style inline.
- **Import Blurbs** (`BlurbImporter.cs`): see `content.md`.
- **Scene Labels** (*Hall of Echoing Mirrors → Text → Scene Labels*, `SceneLabelsWindow.cs`).
- **Pages:** `ScreenManagerEditor` (*Lay pages out side by side*, *Frame*, *Preview in Game view*) and `PageLayout` (*Hall of Echoing Mirrors → UI → Lay Out Pages Side by Side*).
- **Content Ids:** `ContentIds` (*Hall of Echoing Mirrors → Tools → Stamp Content IDs*).
- **Fonts:** `UiTools` (*Hall of Echoing Mirrors → UI → Apply UI Fonts*, *Update UI Font Atlases*); `MakeText` takes a `TextRole`.
- **Colours:** `UiTools` (*Hall of Echoing Mirrors → UI → Apply UI Colours*); `MakeText`/`MakeButton`/`MakeDisplayBar`/`MakeFrame`/`MakeChip` take a `ColourRole`.
- **Scenes and git:** `SceneGitGuard` (*Hall of Echoing Mirrors → Tools → Close Scenes Before Git* / *Reopen Scenes After Git*); used by `docs/parallel-lanes.md` → *Git commands that change a scene*.
