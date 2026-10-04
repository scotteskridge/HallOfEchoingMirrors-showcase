# 011 — Book fonts: EB Garamond and Inter

**Status:** Done (2026-09-29)
**Design:** GDD §13a (legibility; no font rule in the GDD), backlog *Now* "Fonts: EB Garamond and Inter", decisions-log 2026-09-29 *Warm colours* ("the fonts of the mock-ups aren't adopted yet"), the design chat's tooltip mock-up (`docs/mockups/tooltips/`, for its fonts and sizes only)

## Before you start
- The font files are in place (done by the user 2026-09-29): EB Garamond in `Assets/Fonts/EB_Garamond/` (the `.ttf` files in `static/`), and Inter in `Assets/Fonts/Inter/`. Each folder has its own OFL.
- **Unity must import them first**, so their `.meta` files exist. Check that they're committed before starting.

## Goal
Tooltips, story text and small print read cleanly at 1920×1080 and 1280×720. Every text uses one of a few named *roles* (Heading, Story, Body…). Each role's font and size are set in one asset, so the whole UI can be retuned from the Inspector.

## Out of scope
- **The tooltip redesign** (the mock-up's six states, kicker line, fact columns, footer, locked and warning forms): the next tooltip plan (backlog *Now*). This plan only changes the tooltips' fonts, sizes and padding.
- A player text-size setting (backlog *Ideas*).
- Map colours, and circled stop numbers.
- Boecklins' licence (stays flagged in ASSET-LOG).
- Unused font files: already cleared (2026-09-29). Only the 8 static fonts this plan uses are left. Cormorant and the other weights are in `/Remove Me/Fonts/`. ASSET-LOG still lists Cormorant as proposed: change it to EB Garamond in step 8.

## Design assumptions (settled with the user 2026-09-29)
- **EB Garamond** (the mock-up's serif): headings, room and action names, story feed lines, the story pop-up body, and tooltip titles. It's a text face, so story text needs no size boost.
- **Inter:** tooltip bodies and tables, numbers, buttons, chips, the feed's small notes, and small print.
- **Boecklins Universe:** the menu title only.
- **Claude manages the UI layout** (CLAUDE.md, 2026-09-29). The report lists every box that was resized, moved or restyled.
- **Starting sizes** at the 1920×1080 reference (Canvas Scaler, match 0.5): Title 48 · Heading 24 · Name 19 · Story 22 · Body 17 · Small 14. They're a little above the mock-up (tip title ~26, body ~14) for Steam Deck. All are `~` values, to tune in `UiFonts.asset` after looking.
- **Static weights, not the variable `.ttf` files:** TMP (TextMeshPro) doesn't support variable fonts. Inter uses its 18pt optical size, which is drawn for small text. The weight tables map `<b>` to the SemiBold weight and `<i>` to the true italic.

## Reuse
- `UiStyle.Heading` / `Small` in `Scripts/UI/UiStyle.cs`: the one place tip headings and small print get their markup. Heading switches to EB Garamond with TMP's `<font>` tag.
- `ToolTipPanel` (`_maxWidth`, `_padding`) and `ToolTip.prefab`: tooltip box sizing.
- `StoryFeed` (`_noteSize`, the line template) and `StoryPopup.prefab`: story text.
- `EditorUiFactory.MakeText` / `MakeButton`: get a role so new UI is born right.
- `UiTools.UseSelectedFontForAllText`: replaced by *Apply UI Fonts*.
- `TipTable` (em-based columns) and `TipTableTests`: re-checked, because the width of an em changes with the font.
- `GreyboxSetup`: the new setup step.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Scripts/UI/TextRole.cs`, `UiFonts.cs`, `FontRole.cs` | New | role enum; ScriptableObject (font + size per role); a component that tags a text with its role |
| `Data/UI/UiFonts.asset` | New | the fonts and the size scale |
| `Fonts/*SDF.asset` | New | EB Garamond Regular/SemiBold/Italic and Inter Regular/SemiBold/Italic (static atlases, so they don't churn in git on every Play) |
| `TMP Settings.asset` | Edit | default font becomes Inter; the fallbacks become Inter, then LiberationSans |
| `Editor/GreyboxSetup.cs` | Edit | Step 89 *Book fonts* |
| `Editor/UiTools.cs`, `EditorUiFactory.cs` | Edit | *Apply UI Fonts* menu; `MakeText` takes a role |
| `UiStyle.cs`, `ToolTipPanel.cs`, prefabs, scene | Edit | heading font; box sizes from the readability pass |
| `.claude/rules/ui.md`, `editor-tools.md`, `ASSET-LOG.md` | Edit | Fonts gotcha, the new tool, which fonts are used |

Save format change? No. Balance fields? No (these are UI, not balance).

## Steps
1. ~~Apply the CLAUDE.md rule change~~ (done 2026-09-29, along with PROJECT_NOTES). Check that the EB Garamond files are in place.
2. **Failing tests first** (below).
3. `TextRole`, `UiFonts`, `FontRole`.
4. **Step 89, part 1:** build the SDF assets from the characters in `game_text.txt`, plus ASCII, Latin-1 and `… × → · − – — ‘ ’ “ ”`. Set the weight tables and fallbacks, and TMP Settings. Create `UiFonts.asset`.
5. **Step 89, part 2:** tag each of the 76 texts with a role, using a name-and-path rule table that is listed in the step's code. *Apply UI Fonts* then sets each text's font and size from `UiFonts`. Update `MakeText`.
6. Tip headings in EB Garamond through `UiStyle.Heading`. If TMP can't find the font by name without a `Resources` folder, fall back to Inter SemiBold headings and say so.
7. **Readability pass**, checked in Game-view screenshots at 1920×1080 and 1280×720. It fixes these problems, seen in the user's screenshot of 2026-09-29:
   - **Map room labels** break inside words ("Laborat / ory") and run to 4 lines. Fix: wrap only between words, Name role at a smaller size, and a wider label box.
   - **Tooltips:** the text sits right against the edge and the lines are tight. Fix: padding 10 → ~16, a little extra line spacing, a max width that fits the TipTable columns, and the note below the table in Small. The structure stays as it is.
   - **Stat and skill chips** (top bar) and **pockets** (left side) use text that is too small (~12 px). Fix: Body/Small size, with taller chips if needed.
   - **Popover rows** get cut off ("Put down all phials of memo…"). Fix: widen the popover or let the row labels shrink to fit, ending in "…".
   - **Story cards:** the body is too small and cramped, and the *Read more* buttons take too much space. Fix: Story role with more line spacing, and a slimmer *Read more*.
   - The action rows' auto-size minimums (14/11) are too small. Also check the story pop-up (auto-size off) and the TipTable columns.
   - **Refusal notice** (`NoticeToast`, bottom left): Body role, with room for two clean lines.
   - Leave the dev tools panel as it is (editor only).

   Aim for the Increlution feel (see PROJECT_NOTES *UI reference*), not its layout. List every change.
8. Update the docs (rules lines proposed first), then `/sync-state`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `UiFontsTests.EveryRoleHasFontAndSize` | no role is left unset |
| `UiFontsTests.FontsCoverGameText` | every character in `game_text.txt`, plus `… × → · −`, exists in the role's font or its fallbacks, so no more missing-glyph warnings |
| `UiFontsTests.EveryTextHasARole` | every TMP text in `SampleScene` and `Prefabs/UI` has a `FontRole` and uses that role's font |
| `TipTableTests` (existing) | still pass unchanged |

## Done when
- [x] Tests pass; compile and Console clean (no missing-glyph warnings)
- [x] In Unity: run *Setup → Step 89*, press Play. Story lines are in EB Garamond; hovering a stat shows an Inter table with an EB Garamond heading; the menu title is still Boecklins; no text is cut off
- [x] decisions-log entry (font roles and EB Garamond over Cormorant; layout managed by Claude); the backlog's *Fonts* and *Ellipsis missing* items removed; ASSET-LOG lists EB Garamond (OFL)

## Notes after implementation
- **All eight static weights** were built (SemiBoldItalic too), so `<b><i>` has a real face. Each atlas holds 200 characters; EB Garamond's italics lack ²³¹, which Inter supplies.
- **Heading font by name works without Resources:** `UiFonts.RegisterForMarkup` (called by `ToolTipPanel`) adds the fonts to TMP's `MaterialReferenceManager`, which the `<font>` tag checks first. No Inter fallback was needed. `UiFontsTests.HeadingMarkupNamesTheHeadingFont` guards the name. Tip headings are 130% of the pop-up's text (`UiStyle.HeadingSize`), not the Heading role's size.
- **Added beyond the plan:** *UI → Update UI Font Atlases* (a reusable tool, since static atlases need rebuilding for new characters); `FontRole` has a per-text size scale (map names 0.85, the story pop-up title 1.35); `UiFonts` has line spacing per role and an auto-size floor (85%); Boecklins and EB Garamond fall back to Inter first.
- **Readability pass differences:** the popover wasn't widened (its floor and way chips went to Small instead); chips needed no extra height; the notice already fits two lines; TipTable's character width went 0.55 → 0.62 em for Inter's digits.
- **Known leftovers** (backlog *Later*): map lines run through room names; the pockets overlay draws over the popover; the popover's close button is a white box; table column headings look unevenly spaced.
- Review fixes: Step 89 only changes values still at their old ones (TMP Settings, overflow, wrapping), checks all eight fonts before changing anything, wires UiFonts into the ToolTip prefab, and no longer records unrelated prefab overrides.
