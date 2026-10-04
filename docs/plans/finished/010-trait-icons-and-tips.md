# 010 — Stat and skill icons, and tidy stat and skill tooltips

**Status:** Done (2026-09-29)
**Design:** GDD v0.5 §13a *Show the multiplier, not the level*; decisions-log 2026-09-28 (the stats row replaces the Stats tab); playtest 2026-09-29 (this plan's choices, to be logged)

## Goal
Every stat and skill has an icon. It shows on its chip in the stats row, and at the far left of each action in the room popover (skill first, then the stat it trains), each with a small tooltip. The stat and skill tooltips become a neat table in smaller text, like Increlution's.

## Visual references
Open these before building; they're what the user is aiming for.
- [icon-mockup.png](010-refs/icon-mockup.png): the user's mockup with the actual icons. Top: every icon at 48/24/16 px. Bottom left: **action rows as this plan builds them** (skill icon, then stat icon, then the name, time on the right). Bottom right: an action tooltip, which is for the next pass, not this plan.
- *(The three Increlution screenshots below are third-party and not in this public copy.)*
- increlution-skill-chips-and-table-tooltip.png: **the model for the chips and the stat/skill tooltip**: an icon before each name, and a table of Level / Experience / Multiplier over short small-print explanation.
- increlution-rows-with-skill-icons.png: the skill icon in a column at the far left of each action row.
- increlution-action-tooltip.png: an action tooltip with inline icons and small print; for the content pass, not this plan.

The stat and skill tooltip this plan builds (the user chose columns lined up, not centred):
```
<b>Wayfinding</b>  ×2.10 speed

             Level      XP        Speed
  This run      12    40 / 120    ×1.80
  Mastery        3    15 / 60     ×1.17
  Total                           ×2.10

  (small, muted:) Makes these faster: Walk, Search the room.
  Levels reset every run; mastery is kept. Each level ×1.05 speed, each mastery ×1.05.
```

## Out of scope
- What the action tooltips and the other tooltips say (the content pass comes next, with icons inside the text via a TMP Sprite Asset). Its starting point is the user's mockup, [icon-mockup.png](010-refs/icon-mockup.png) (bottom right): one line per icon ("×1.30 speed · Lv 6 · mastery 12"), then the description.
- Icons on queue rows, the ribbon, stop cards or the pockets overlay.
- The vitality heart icon (`res_vitality`): imported but not used yet.
- A colour for each stat or skill.

## Design assumptions
- **Icons:** the user's `Assets/Art/hall_icons_v1.zip` (made by a script in another session: no licence question, but note it for Steam's AI disclosure). The 64 px PNGs are used, shown at ~20 px.
- **An icon's tooltip** is the chip's short text, e.g. "Wayfinding ×2.10". The full tooltip stays on the chip.
- **Stat icons are dimmer than skill icons** on action rows (tinted `UiStyle`'s muted colour, as in the mockup), so the skill, which sets the speed, reads first. On the chips all icons are full strength. (The user's call, 2026-09-29.)
- **An action with no skill or no trained stat** shows an empty slot, so names stay lined up.
- **The table** (the user chose "columns, justified"): the heading, then the rows *This run*, *Mastery* and *Total* under the columns *Level*, *XP* and *Speed* (skills only). The columns sit at fixed positions (TMP `<pos>`) and the numbers are fixed-width (`<mspace>`), padded so they line up on the right. A stat's *Total* row shows what it does now (its short text). The explanation follows in small, muted print.
- **Text size:** the tooltip body goes from 30 to 20; small print is `UiStyle.Small` (80%, so 16). The maximum width goes from 360 to 420. The user OK'd this change to the ToolTip prefab.

## Reuse
- `ClaraTips` (`Scripts/UI/ClaraTips.cs`): `StatTip`, `SkillTip`, `StatBrief`, `SkillBrief`. The table replaces the tip templates; the briefs become the icons' tooltips.
- `StatsRow`: the chip template gets an icon; `Refresh` sets it.
- `ActionRow` and `RoomPopover.Fill`: `task` is in scope there, which gives `task.skill` and `sim.StatTrainedBy(task)`.
- `ToolTip.On`: `ToolTipPanel` already shows the innermost tooltip, so an icon's tooltip wins over its row's.
- `UiStyle.Small`, `Aside` and `Heading`; `UiText.Number` and `Rate`; `EditorUiFactory` for the setup step.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Assets/Art/Icons/*.png` | New | the 11 PNGs at 64 px from the zip (imported as Sprites) |
| `SkillDefinition.cs` | Edit | `[SerializeField] Sprite icon` plus an `Icon` property, with a tooltip |
| `Scripts/UI/TraitIcons.cs` + `Data/UI/TraitIcons.asset` | New | a UI asset holding one Sprite per stat; `IconOf(ClaraAttribute)` fails loudly if one is missing |
| `Scripts/UI/TraitIcon.cs` | New | one icon Image plus its tooltip; `Show(sprite, tip)`, blank when there's none |
| `Scripts/UI/TipTable.cs` | New | builds the aligned rows (pure strings, testable) |
| `ClaraTips.cs` | Edit | `StatTip` and `SkillTip` build the table |
| `StatsRow.cs`, `ActionRow.cs`, `RoomPopover.cs` | Edit | set the icons |
| `game_text.txt` | Edit | `tips.col_level`/`col_xp`/`col_speed`, `row_run`/`row_mastery`/`row_total`, the small-print lines; retire the old `tips.stat*`/`tips.skill*` templates |
| `Editor/GreyboxSetup.cs` | Edit | Step 86 (below) |

No new balance fields. Save format change? No (icons aren't saved).

## Steps
1. Unzip the 64 px PNGs to `Assets/Art/Icons/` (named `stat_endurance_64.png` etc.); Unity imports them. Ask the user whether the zip and preview stay in `Assets/`.
2. Failing tests for `TipTable`, then build it.
3. `SkillDefinition.icon`, `TraitIcons`, `TraitIcon`.
4. Tests for the new `SkillTip`/`StatTip` (rows and numbers present), then rewrite them in `ClaraTips` with the new text keys.
5. `StatsRow`, `ActionRow` and `RoomPopover` show the icons.
6. Setup **Step 86: Stat and skill icons**. It creates `TraitIcons.asset` and fills it by file name (a one-time match; the game uses the asset references). It sets each skill's icon, adds a `TraitIcon` at the left of the chip template and two at the left of the `ActionRow` prefab (added, not moving anything else), and sets the ToolTip prefab to font 20 and width 420.
7. Compile, check the Console, run all EditMode tests; the user runs Step 86 and plays.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `TipTableTests.RowsLineUpOnTheRight` | numbers of different lengths are padded to the same width |
| `TipTableTests.EmptyCellLeavesColumnBlank` | a stat row with no Speed still lines up |
| `ClaraTipsTests.SkillTipShowsLevelMasteryAndTotal` | the three rows with this run's level, mastery and ×speed |
| `ClaraTipsTests.StatTipTotalShowsWhatItDoes` | a stat's Total row carries its short text |
| `ClaraTipsTests.AtMaxMasterySaysSo` | a capped mastery reads "the most it can be" instead of XP |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity: *Hall of Echoing Mirrors → Setup → Step 86* runs; in Play every chip shows its icon, each action in a room popover shows its skill and stat icons at the far left, and hovering an icon names it; a chip's tooltip is the aligned table in smaller text
- [x] decisions-log entry: icons for stats and skills; action rows show skill, then stat; the table tooltips

## Notes after implementation
- **Built as planned**, with these differences (the user was told):
  - The ActionRow's name and details moved right to make room for the icons (they started 10 px from the edge).
  - The chip's Label moved into a new **Line** (icon, then label); the bars stay under both.
  - Hovering a **chip's** icon shows the chip's full table, not a short tip of its own; action-row icons have their short tips.
  - The ToolTip prefab was **720** px wide, not 360; it's now 420 as planned.
  - At the most mastery, the XP cell reads "max" and the small print says "Mastery is the most it can be for now" (the full phrase didn't fit the column).
  - Each stat's brief split into name + `stats.effect_*`, so the heading and the Total row reuse the effect. The stat table's Total row shows its strength (level + mastery) under Level.
  - Core gained `LevelSpeedFor`/`MasterySpeedFor` (a skill's speed split in two) for the table's Speed column.
- **The same session's playtest batch then changed things this plan built:** the ActionRow became one line (Step 87), the colours warm (Step 88), and the action tooltip was reordered (see decisions log 2026-09-29).
- Step 1's question (keep the zip and preview in `Assets/`?): no; the icon sources and mock-ups now live outside the project.
