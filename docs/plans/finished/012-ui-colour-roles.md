# 012 — UI colour roles

**Status:** Done. The user accepted the Play check at 1920×1080 (2026-09-30), including the `MapPanel` background note below.
**Left to do:** Nothing.
**Design:** GDD §13a (legibility, look settings live in assets); decisions-log 2026-09-29 *Warm colours* (the mock-up palette, Claude's judgement); backlog *Next* "UI colour roles"

## Goal
A palette change (panel, row, button and accent colours) is a `UiColours` asset edit and one menu command — no code change, no recolour pass through every prefab.

## Out of scope
- **Map, floor and explore-bar colours** — already own an asset (`MapStyle`), untouched by this plan.
- **Hue colours** (`UiStyle.TryGetHueColour`) — per-hue content, not a palette role.
- **Rich-text hex strings** (`Warning`, `Milestone`, `Levelling`, `Muted`, `ReturnMarker`) — already in one place in `UiStyle`; stay as code constants.
- **State-dependent runtime colour pairs** — `SpeedTierLocked`/`Current`, `BookmarkTab`/`TabOpen`, `StopCard`/`StopCardCurrent`, `Glow`, `DraggedAlpha`/`UnavailableAlpha`. These stay as `UiStyle` constants; the scripts that already choose between them (`RunHeader`, `SlideDrawer`, `StopCard`, `Glow`, `ActionRow`, `QueueRow`) are untouched.
- Button and Chip prefabs (backlog *Next* item 2) — a separate plan; this one only recolours what exists today.
- The Button/Chip ColorBlock highlighted/pressed/disabled *ratios* aren't redesigned — the same tints Step 88 uses now, just derived from role colours instead of hard-coded.

## Design assumptions
- "Role" = a colour that is set once and doesn't change at runtime (panel backgrounds, row backgrounds, bars, static button faces, tab/chip/card backgrounds, main and secondary text, the accent). State-dependent pairs and rich text are out of scope (above) — narrower than the font system, since UiStyle mixes static and stateful colours in one place and fonts didn't.
- Following plan 011's pattern one-for-one: `UiColours : ScriptableObject` (role → Color) mirrors `UiFonts`; `ColourRole` enum mirrors `TextRole`; a `ColourRole` tag component (`DisallowMultiple`, `RequireComponent(typeof(Graphic))`) mirrors `FontRole`; *Apply UI Colours* mirrors *Apply UI Fonts*, looping prefabs then the scene.
- ColorBlock tinting moves into *Apply UI Colours*: for a tagged Button/Selectable, it derives highlighted/pressed/disabled from the tagged Graphic's role colour using Step 88's existing ratios (highlighted → white blend, pressed → 0.65, disabled → 0.4 alpha), replacing Step 88's hard-coded ColorBlock block.
- Step 88 retires once every currently-recoloured Graphic is tagged and *Apply UI Colours* reproduces its result on the prefabs and scene (checked in Play, side by side with the current build) — same bar plan 011 used to retire the old font tool.

## Reuse
- `UiFonts.cs` / `TextRole.cs` / `FontRole.cs` (`Assets/Scripts/UI/`): the class shapes to mirror.
- `UiTools.cs` (`Assets/Editor/`): where *Apply UI Fonts* lives; *Apply UI Colours* joins it, same load/confirm/prefab-loop/scene-loop structure (`UiFontsPath` → a new `UiColoursPath` const).
- `EditorUiFactory.MakeText`/`MakeButton`/`MakeDisplayBar`/`MakeFrame`/`MakeChip` (`Assets/Editor/`): take a `ColourRole` alongside the existing `TextRole`, so new UI is born tagged and correctly coloured (also fixes the backlog's *Setup helpers build in the warm palette* item).
- `GreyboxSetup.cs` Step 88 (l.503-640, `WarmColours`/`Recolour`/`NewColourFor`/the ColorBlock block): the mapping and ratios to carry into the new step and the Apply command, then retire.
- `MapStyle.cs` / `MapStyleTests.cs`: the asset-plus-test pattern for a look-settings ScriptableObject with no code needed to retune.
- `UiFontsTests.cs` (`EveryRoleHasFontAndSize`, `EveryTextHasARole`): the test shapes to mirror for colours.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Scripts/UI/ColourRole.cs`, `UiColours.cs`, `ColourRoleTag.cs` | New | role enum; ScriptableObject (Color per role); a component that tags a Graphic with its role |
| `Data/UI/UiColours.asset` | New | the palette, starting from the current `UiStyle` static values |
| `Editor/GreyboxSetup.cs` | Edit | Step 90 *UI colours*: creates `UiColours.asset`, tags every currently-recoloured Graphic per Step 88's mapping |
| `Editor/UiTools.cs` | Edit | *Apply UI Colours* menu: sets tagged Graphics' colour from `UiColours`, then derives each tagged Button's ColorBlock from its role colour |
| `Editor/EditorUiFactory.cs` | Edit | `MakeText`/`MakeButton`/`MakeDisplayBar`/`MakeFrame`/`MakeChip` take a `ColourRole`; tag on creation |
| `Editor/GreyboxSetup.cs` | Edit | retire Step 88 once Step 90 + Apply UI Colours reproduce its result |
| `.claude/rules/ui.md`, `editor-tools.md` | Edit | the new asset/tag/Apply-step, replacing the Step 88 reference |

No new content fields, no Balance Sheet column (look setting, not balance). Save format change? No.

## Steps
1. Failing EditMode tests first (below).
2. `ColourRole`, `UiColours`, `ColourRoleTag`.
3. **Step 90:** build `UiColours.asset` from `UiStyle`'s current static values; tag every Graphic Step 88 currently recolours, using Step 88's own mapping as the rule table (same pattern as Step 89's `RoleRules`).
4. *Apply UI Colours* in `UiTools.cs`: sets `Graphic.color` from the tagged role; then, for a tagged Button whose ColorBlock is still `ColorBlock.defaultColorBlock`, derives highlighted/pressed/disabled from the role colour with Step 88's ratios.
5. `EditorUiFactory` changes; update its callers to pass a role.
6. Run Step 90, then *Apply UI Colours*, and compare against the current (Step 88) look in Play at 1920×1080 — same panels, buttons, bars. Fix any mismatch.
7. Retire Step 88 (git history keeps it, per the existing retirement convention).
8. Update the docs, then `/sync-state`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `UiColoursTests.EveryRoleHasAColour` | no role is left unset in `UiColours.asset` |
| `UiColoursTests.EveryTaggedGraphicUsesItsRoleColour` | every `ColourRoleTag` in `SampleScene` and `Prefabs/UI` has its Graphic's colour matching `UiColours` for that role |
| `UiColoursTests.TaggedButtonsHaveDerivedColorBlock` | a tagged Button's ColorBlock states are derived from its role colour, not left at `ColorBlock.defaultColorBlock` |

## Done when
- [x] Tests pass; compile and Console clean (467/467 EditMode, confirmed live via Unity MCP)
- [x] In Unity: press Play at 1920×1080 — panels, rows, buttons and bars look the same as before (Step 88's result); the one flagged exception below is worth a specific look. Editing `UiColours.asset` and re-running *Apply UI Colours* changing them with no code touched is confirmed already (that's how the MapPanel fix below was verified)
- [x] Step 88 removed; decisions-log entry added; backlog's *UI colour roles* and *Setup helpers build in the warm palette* items removed

## Notes after implementation

**Built and verified except the Play look-check**, which needs the user's eyes (no screenshot capability over Unity MCP). Everything else — Step 90, *Apply UI Colours*, the factory changes, the tests, retiring Step 88, the decisions-log entry and the backlog trim — is done and confirmed live through Unity MCP (compile clean, 467/467 EditMode tests, checked twice more after further edits).

Two real things turned up while debugging live through MCP, both fixed in code (not the design):
1. **A latent prefab-override bug, now fixed in `UiTools.ApplyColoursUnder`:** setting `Graphic.color`/`Selectable.colors` directly on a Graphic that's part of a prefab instance in the scene updates the live field but doesn't register the change in the PrefabInstance's override cache, so the scene keeps re-serialising the *old* override on save even though the live value is right. Added `PrefabUtility.RecordPrefabInstancePropertyModifications(...)` after each set. Found via `MapPanel`, which had a pre-existing scene-only alpha override that predates this plan; confirmed fixed by reading the raw `.unity` file before and after. `UiFonts.Apply` (plan 011) likely has the same gap for a prefab-instance text with an existing override — not fixed here, logged as a backlog *Later* item instead (out of scope for this plan).
2. **A test that was stricter than Step 88 itself, now corrected, not a UI bug:** `UiColoursTests.TaggedButtonsHaveDerivedColorBlock` originally required every tagged Selectable to get a derived ColorBlock. A Scrollbar's `Selectable` lives on a different GameObject than its `targetGraphic` (the Handle), so neither the retired Step 88 nor the new *Apply UI Colours* ever derives it (both use `graphic.GetComponent<Selectable>()`, not `GetComponentInParent`) — true before this plan too. Narrowed the test to only require derivation when the Selectable and its targetGraphic share a GameObject.

**Flagged for the user's Play check, not resolved unilaterally:** fixing (1) above also normalised `MapPanel`'s background to `BandBackground`'s exact colour (what's baked into the prefab), which is about 12% more opaque than its previous scene-only override (0.824 → 0.941 alpha). That override's origin is ambiguous — its RGB matched `BandBackground` but its alpha matched `MapBackground` (whose doc comment says "Behind the map"), a mismatch that predates this plan. Left as `BandBackground` since that's what Step 90's tagging (matching the prefab's own value) and the "reproduce Step 88's result" goal point to, but call it out specifically when checking Play — happy to retag it `MapBackground` instead if the old, more transparent look was intentional.

**Reviewer pass (before commit) found three real issues, all fixed:**
1. `StopCard` and `BookmarkTab` were tagged roles, but `StopCard.cs`/`SlideDrawer.cs` overwrite that Graphic's colour at runtime from `UiStyle` directly (they're state pairs, explicitly out of scope per this plan) — editing those two entries in `UiColours.asset` would have changed nothing in Play. Untagged the two live instances via Unity MCP, removed both from `RoleForCurrentColour`'s matching (so Step 90 won't retag them again), and commented the two enum entries as unused (kept, not deleted, so the stored indices don't shift).
2. `EditorUiFactory`'s `MakeButton`, `MakeDisplayBar` and `MakeBarLabel` (unused by any current step, so latent, not visible in the build) didn't colour a button's label, a bar's track, or used a hard-coded colour instead of a role — exactly the *Setup helpers build in the warm palette* problem this plan's backlog item claimed was fixed. Fixed all three.
3. The backlog *Next* list's renumbering was double-checked against the pre-edit diff: items 5 and 11's cross-references were already correct once the shift is accounted for (the reviewer's read of those two didn't account for the shift). Item 12's reference was a genuine pre-existing bug — it named the wrong item even before this session's renumbering — fixed to point at the intended item.

Also, while adding `MapBackground`/`SearchedBar` doc comments (raised as polish, not required): confirmed via the reviewer's scan that neither role is reached by anything in the current scene/prefabs — both stay reserved for future use.
