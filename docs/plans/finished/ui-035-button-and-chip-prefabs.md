# ui-035 — Button and chip prefabs

**Status:** Done (2026-10-01)
**Left to do:** nothing (the reference screenshots were not taken; see the notes)
**Design:** GDD §13a (interface); `docs/UI-BACKLOG.md` *Next* 2; decisions log *Every recoloured Graphic has a role* (plan 012)
**Pillar:** none: tooling. It serves *Who it's for* (players who bounced off the genre's bare look) by making later restyles (ui-036 tooltips, ui-037 star bar and juice) one edit, not thirty.

## Goal
Every button and chip comes from a small set of prefab kinds, so restyling a kind is one prefab edit. The player sees the same screen: sizes and places don't change, and buttons of one kind become identical where they differ slightly today. One visible fix: the room popover's close button loses its white box. Folded in because this plan makes most texts prefab instances: *Apply UI Fonts* must persist on them (`UI-BACKLOG-later.md`).

## Out of scope
- A new look for buttons or chips (no mock-up; a later plan restyles the kinds).
- Map room nodes (`MapNode`, styled by `MapStyle`), the tooltip panel (ui-036), the vitality bar (ui-037).
- Buttons made at runtime other than from templates (there are none today).

## Design assumptions (Claude's calls, the user agreed 2026-10-01)
- **Kinds:** `Button` (command: Begin, Plan, End run, Save, Repeat…), variants `IconButton` (small: close ×, fold, To top, Remove, Delete) and `TabButton` (Main/Menu/Plan tabs, speed tiers); `Chip`, variants `ChipFaint` and `IconChip` (stats-row chip with its icon). The implementer may merge or add a kind if the inventory says so, and lists the final mapping in the notes.
- **Per place vs per kind:** RectTransform, `LayoutElement`, label text, `TextKey`, `ToolTip`, view scripts (`StatChipView`, `SkillChipView`, `SummaryChipView`) stay on each instance. Face sprite, colour role, `ColorBlock`/transition, label font role, size and alignment come from the kind (overrides of these are reverted).
- Nobody else edits `SampleScene.unity` until ui-035 reaches `backlog-tasks` (the user, 2026-10-01).

## Reuse
- `EditorUiFactory.MakeButton`, `MakeChip`, `MakeChipRow`, `MakePrefab` (`Assets/Editor/EditorUiFactory.cs`): build the base prefabs; afterwards `MakeButton`/`MakeChip` instantiate the kinds instead of building from parts.
- `UiTools` *Apply UI Colours* / *Apply UI Fonts* (`Assets/Editor/UiTools.cs`), `DerivedColorBlock`: already handle prefabs first, then the scene.
- `ColourRole` (`ButtonFace`, `ChipBackground`, `ChipBackgroundFaint`), `ColourRoleTag`, `FontRole`, `UiColours`, `UiFonts`.
- `TemplateList<T>`: runtime copies of templates keep working unchanged (a template that is a prefab instance clones like any object).
- Unity's `PrefabUtility.ConvertToPrefabInstance` (2022.3+): turns an existing object into a prefab instance, matching children by name and keeping every reference to it.
- Inventory (2026-10-01): 23 Buttons in the scene, 11 in the row prefabs (ActionRow 3, QueueRow 2, SlotRow 3, MilestoneRow 1, StoryPopup 1, MapPanel's MapNode excluded); ~10 chip templates owned by `StatsRow`, `RoomPopover` (floor, way), `PocketsOverlay` (pocket, floor, knowledge), `StatStrip`, `SkillGainRow`, `ChipColumns` (changed, carried).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Prefabs/UI/Kinds/` (Button, IconButton, TabButton, Chip, ChipFaint, IconChip) | New | base prefabs and variants, tagged with colour and font roles |
| `Assets/Editor/UiKinds.cs` | New | *Hall of Echoing Mirrors → UI → Convert to Kinds* (one-shot, retired after use) and the kind mapping list; *Build Kind Prefabs* if the factory can't do it |
| `Assets/Editor/UiTools.cs` | Edit | `ApplyUiFonts` calls `PrefabUtility.RecordPrefabInstancePropertyModifications` after setting a prefab instance's text, as `ApplyUiColours` already does (plan 012) |
| `Assets/Editor/EditorUiFactory.cs` | Edit | `MakeButton`/`MakeChip` instantiate the kind prefabs |
| `Assets/Prefabs/UI/*.prefab`, `SampleScene.unity` | Edit (by the tool only) | buttons and chip templates become kind instances; the popover's close button becomes an `IconButton` |
| `Assets/Tests/EditMode/UiKindsTests.cs` | New | the guard tests below |
| `docs/mockups/main-screen/` and its README | Edit | the after screenshots as the new reference shots |
| `docs/plans/ui-backlog-roadmap.md` | Edit | ui-036 and ui-037 restyle through the kinds |
| `.claude/rules/ui.md` | Edit (propose lines) | Prefabs section: "new buttons and chips are instances of a kind" |

Save format change? No. Content or balance fields? No.

## Steps
1. Failing tests: every `Button` in the scene and UI prefabs (except `MapNode`) and every chip template named in the mapping is an instance of a kind prefab.
2. Build the six kind prefabs (step tool, safe to run twice), styled from today's most common look of each kind; *Apply UI Colours* and *Apply UI Fonts* cover them.
3. Failing test, then fix: *Apply UI Fonts* persists on a prefab-instance text that already has a font/size override (record the modification, as colours do). Write *Convert to Kinds*: for each mapped object, `ConvertToPrefabInstance` (match by hierarchy, record overrides), then revert the style overrides listed above; keep layout and per-place components. Undoable; logs each conversion and every style difference it flattened. Tests for the revert list on a throwaway object.
4. Before screenshots (the user, or Claude via the editor): main screen, popover open, queue column open, planning screen, menu, Summary, at 1920×1080.
5. Run the tool on the row prefabs first (ActionRow, QueueRow, SlotRow, MilestoneRow, StoryPopup), then the scene. Re-run *Apply UI Colours* and *Apply UI Fonts*. All tests green.
6. Point `MakeButton`/`MakeChip` at the kinds.
7. After screenshots; compare with step 4. Report every visible difference (expected: flattened one-offs and the close button only). Save the after shots in `docs/mockups/main-screen/` (dated names) and index them in `docs/mockups/README.md` as the current reference.
8. Retire *Convert to Kinds* once committed (editor-tools rule); propose the `ui.md` lines; move the close-button and *ApplyUiFonts* override lines out of `UI-BACKLOG-later.md`; add to `ui-backlog-roadmap.md` (ui-036, ui-037 *Before starting*): restyle buttons and chips through the kind prefabs, not per object.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `UiKindsTests.EverySceneButtonIsAKind` | no scene button is styled on its own (MapNode excepted) |
| `UiKindsTests.EveryPrefabButtonIsAKind` | same for the UI prefabs |
| `UiKindsTests.ChipTemplatesAreKinds` | every mapped chip template is a kind instance |
| `UiKindsTests.KindsHaveRoles` | each kind's face has a `ColourRoleTag` and its label a `FontRole` |
| `UiKindsTests.ConvertRevertsStyleKeepsLayout` | conversion drops face/font overrides, keeps size, text and tooltip |
| `UiFontsTests.ApplyPersistsOnPrefabInstanceOverride` | *Apply UI Fonts* records its change on a prefab instance whose text already overrides font or size |
| existing `UiColoursTests`, `UiFontsTests`, `NoGameUiTests` | still pass |

## Done when
- [ ] Tests above pass; compile and Console clean
- [ ] In Unity: open `Assets/Prefabs/UI/Kinds/Button`, change its face colour role's colour in `UiColours` or the prefab's label size: every command button in Play mode changes; revert
- [ ] Play: every button still clicks (Begin, Plan, End run, tabs, speed tiers, popover close, To top, Remove, slot Load/New/Delete, Read more, Continue); chips still show their text and tooltips
- [ ] The popover's close button is warm, not white; before/after screenshots show no other change than the listed flattened one-offs
- [ ] After shots saved in `docs/mockups/main-screen/` and indexed; roadmap lines for ui-036/037 added
- [ ] decisions-log line (buttons and chips come from kinds); changelog line (close button); `ui.md` lines proposed

## Notes after implementation
**Kinds built (7):** Button, AccentButton (Play, Begin), IconButton (one-symbol captions: ×, −, +, ^, x), TabButton (Menu/Plan/Main, speed tiers), SmallButton (Read more), Chip, ChipFaint (stat strip, pockets). Variants of Button/Chip.
**Changed from the plan:** `IconChip` dropped (chips keep their icons and bars per place; a chip kind is only the face: sprite + colour role). Added `AccentButton` and `SmallButton` (the inventory has accent buttons and a Small-role caption). Chip labels keep their own font role. Icon buttons are picked by caption length, since a prefab's size is often set by a layout.
**Flattened differences (compared by dumping every button and chip's colour, sprite, font, size, alignment, transition and colours before and after):** only the speed button and the popover close button, white to the warm button face. Nothing else changed.
***Apply UI Fonts* gap:** real. `Apply` alone leaves a prefab-instance text's old size override, and `Undo.RecordObject` does not help; recording the modification fixes it. `UiFontsTests.ApplyPersistsOnPrefabInstanceOverride` fails without the fix (checked) and passes with it.
**Not done:** screenshots (step 4 and 7) and the Play check; asked of the user.
**Finished 2026-10-01:** the user's Play check passed. *Convert to Kinds* and `ConvertRevertsStyleKeepsLayout` were retired in a separate commit (the plan's step 8); *Build Kind Prefabs* stays. **Differs from the plan:** the before/after screenshots (steps 4 and 7) were not taken; the property dump comparison stood in. A fresh set of reference shots of the main screen is a backlog line.
