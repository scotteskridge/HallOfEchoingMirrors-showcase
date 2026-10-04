# ui-036a — Tooltip shape

**Status:** Done (2026-10-02)
**Left to do:** nothing. Hand check done 2026-10-02: the tip colours look fine; the scene's `_delay` stays 0.1 for now (the user thinks it's likely too fast; revisit in ui-036b)
**Design:** GDD v0.5 §13a, §14.12, §14.19; decisions log 2026-10-01 *Tooltips: the open points settled* and *Colour as the payoff*; `docs/plans/ui-backlog-roadmap.md` *Tooltip design pass*; mock-up `docs/mockups/tooltips/`
**Pillar:** 3, *Every run teaches something*: a failed run can be diagnosed (§14.12), and a chain is visibly faster (§14.19, actual number large, base small).

## Goal
The tooltip panel takes its new shape: a kicker, a serif title, fact rows, and a footer strip. It is fixed at 340 px (260 for the compact form), sits 8 px beside what it describes and never covers it, opens after 350 ms and then instantly between neighbours. Action tooltips use the new shape: the full form, and the compact form for Pick up and Put down. Every other tooltip keeps today's text, shown inside the new panel.

## Out of scope
- States 3–6 (run-ender, skill, item, locked) and moving the other ~20 tooltip callers to the new shape: **ui-036b**.
- Filling "since last run": plan 042 and **ui-044**. 036a leaves an empty slot for it.
- Dropping or rewording facts. The action tooltip shows the same facts as today, rearranged into rows.
- Hue colours, which are parked to stage 2.

## Design assumptions
- The compact form is for `Simulation.PickUpVerb` and `PutDownVerb` only. The decisions log says "the other one-per-item verbs", but no other verb fits that today.
- **Placeholder rule:** the task's `description` stays as one muted line above the footer. The design says no prose on action tips; 036b decides whether to drop it.
- **Placeholder rule:** "flat · skill cannot reduce it" shows on the Costs row whenever the price is a fixed charge (`PriceOf`), because no skill reduces it.
- **Placeholder rule:** "0 ms between neighbours" means a tip opens at once if another tip closed less than 0.25 s ago (an Inspector field).
- **Placeholder rule:** the colour shades are time amber #E0A650 (today's `Milestone`), vitality rose ~#D9867A, met green ~#8FB27A (different from `SearchedBar`) and unmet orange #D9894A (today's `Warning`). All are set in the `UiColours` asset, for the user's look check.
- Second cues for colour-blind players: met shows ✓ (and "kept" where it applies), unmet shows ○, and times and vitality always carry their unit word ("s", "vitality"). The cue is added in one place, so no builder can forget it.
- XP chips use one dot colour, the gilt `Accent`. A colour per skill would need hue colours, which are kept off the chrome.

## Reuse
- `ToolTip`, `ToolTipPanel` (the 0.35 s delay, the innermost-tip rule, `KeepClearOfToast`), `ToolTipPlacement.ClearOf`, and `ToolTip.prefab` with its scene instance.
- `ActionText` (`NameOf`, `CostText`, `RepeatRule`, `XpTargets`, the price and speed calls), which already gathers every fact.
- `UiColours`, `ColourRole`, `ColourRoleTag`; `UiFonts`, `TextRole`, `FontRole` (EB Garamond and Inter SDF assets exist).
- The kind prefabs `Chip` and `ChipFaint` through `EditorUiFactory.MakeChip`, and `EditorUiFactory.MakeFrame`.
- Tests: `ToolTipPlacementTests`, `UiColoursTests`, `UiFontsTests`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `UI/TipLayout.cs` | New | A plain C# description of one tip: kicker, title (with an optional value on the right), compact flag, rows (label, large value, small base, an empty *since* slot, a note), chips, a rule line, and a footer (text, "n / m", progress) |
| `UI/TipMeaning.cs` | New | The meanings Plain, Time, Vitality, Met and Unmet, each mapped to its `ColourRole` and its second cue |
| `ColourRole.cs`, `Data/UI/UiColours.asset` | Edit | New roles **appended**, so saved numbers don't shift: `TipTime`, `TipVitality`, `TipMet`, `TipUnmet`, `TipFooter`, `TipBorder` |
| `ToolTipPlacement.cs` | Edit | `Beside(target, tipSize, screen, gap 8)`: try right, then left, then below, then above, and finally clamp to the screen |
| `UI/TipTiming.cs` | New | The rule for the first tip and its neighbours, as plain C# that is passed the unscaled time (UI only, never gameplay) |
| `ToolTip.cs`, `ToolTipPanel.cs` | Edit | `ToolTip.On(component, Func<TipLayout>)` overload; the panel shows either a layout or a plain string (as the body); fixed widths; anchors to the target's rect |
| `ToolTip.prefab` | Edit (by setup step) | Header, rows area, chip row and footer strip, built by *Hall of Echoing Mirrors → Setup → Rebuild Tooltip Panel* |
| `ActionText.cs` | Edit | `Layout(sim, task, to, room)` builds the full or the compact form; `Tip` stays for its other callers until 036b |
| `RoomPopover.cs`, `ActionRow.cs` | Edit | Action tips use the layout overload |
| `game_text.txt` | Edit | Row labels (`tips.row_takes`, `row_costs`, `row_trains`, `row_faster`, `row_gives`, `row_needs`), kicker verb classes, `tips.from_base`, `tips.flat_cost`, `tips.mastery_rule`, and the compact line |

No balance fields. Save format change? No.

## Steps
1. **Check: one TMP box or real rows.** In a scratch scene that isn't committed, render state 1 from the mock-up as a single rich-text TMP box, and check:
   - (a) a label column with `<pos>`, and a 22 px value beside 11 px small text on one baseline, with wrapping;
   - (b) chip backgrounds (`<mark>` is square, with no padding or rounded ends);
   - (c) the footer strip's darker background and its bar;
   - (d) the dividers;
   - (e) the glyphs ✓ ○ · → × ⟳ in the Inter and EB Garamond SDF atlases;
   - (f) the right-aligned since slot.

   Write the result under *Notes*. **Expected:** a hybrid. The kicker and title are one TMP box, each row is a real row (label and value TMPs), the chips come from the Chip kind, and the footer is a real strip with an Image bar. If one box handles (a), (b) and (f) cleanly, step 5 uses it instead. Any missing glyphs get added to the font atlas, or a small sprite is used.
2. Tests first: `TipLayoutTests` (the meaning → role → cue mapping, and that the since slot is empty unless set), `TipTimingTests` (350 ms first, 0 within the window, 350 ms again after it), and new `ToolTipPlacementTests` cases for `Beside` (right; flip left at the right edge; below or above when neither side fits; clamp; never overlapping the target when it fits). Then make them pass.
3. Colour roles: append the six roles, set their colours in `UiColours.asset` through the existing apply path, and extend `UiColoursTests` so every new role has a colour.
4. Panel behaviour: anchor to the target's rect with `Beside`, keep `KeepClearOfToast`, add the fixed 340/260 widths, use `TipTiming`, and add the `ToolTip.On` layout overload. Plain-string tips render as body text in the new panel.
5. Setup step *Rebuild Tooltip Panel*: rebuilds `ToolTip.prefab` with the structure that step 1 chose. Kicker: small caps, letter-spaced, `TextSecondary`. Title: EB Garamond, through `FontRole`. Rows: the label column in `TextSecondary`. Chips: from `ChipFaint`. The footer strip uses `TipFooter`, and the border uses `TipBorder`. The step is one click, never overwrites by hand, and keeps the scene instance's `_keepClearOf`.
6. `ActionText.Layout`, tests first in a new `ActionTextTests`. The full form has:
   - a kicker (verb class · room);
   - a Takes row (actual, Time; "from X base" only when a skill speeds it);
   - a Costs row (Vitality; the flat note; charge-rise and extra-drain notes);
   - Needs (skill gates as Met or Unmet), Gives;
   - Trains (chips plus the mastery rule line);
   - Faster with (skill ×speed now);
   - the description line;
   - a footer (the repeat rule, with "n / m" and a bar where the rule has a count).

   The compact form, for Pick up and Put down, is the title plus one line ("1.0s · no cost · trains nothing" and the destination on the right).
7. Point `RoomPopover` and `ActionRow` at `Layout`. Compile, Console, all EditMode tests. In Play mode, check the full form on a search, the compact form on Put down, the placement at all four screen edges, and scanning a list (one 350 ms wait, then instant). Then add `PROJECT_NOTES.md` placeholder lines and a changelog line.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `TipLayoutTests.Meaning_MapsToRoleAndCue` | Met has ✓ and `TipMet`, Unmet has ○ and `TipUnmet`: never colour alone |
| `TipLayoutTests.Since_EmptyUnlessSet` | The slot for ui-044 renders nothing by default (never "−0.0s") |
| `TipTimingTests.*` | 350 ms for the first tip, 0 between neighbours, 350 ms again after the window |
| `ToolTipPlacementTests.Beside_*` | 8 px gap, the flip order, the clamp, and no overlap with the target |
| `UiColoursTests` (extended) | Every new role has a colour |
| `ActionTextTests.Full_*` / `Compact_PutDown` | Kicker, the Takes row's meaning and base, the flat note, the chips per XP target, the footer count; Put down is compact |

## Done when
- [ ] Tests above pass; compile and Console clean
- [ ] In Unity: hovering a search action in a room popover shows the full form beside the row, not over it. Put down shows the compact 260 px form. Moving down the action list opens each tip at once after the first.
- [ ] Layout changes listed in the report; placeholders in `PROJECT_NOTES.md`; changelog line; the decisions log only if the user settles a placeholder

## Notes after implementation
**Step 1 (the check).** Not run as a scratch scene; settled by measuring the font assets and then by building the hybrid and looking at it in Play mode.
- (e) Glyphs: ✓ and ○ are missing from the Inter and EB Garamond SDF atlases (· → × − • — are there). ⟳ is missing too and Inter's own TTF lacks it, so the footer has no repeat icon. ✓ and ○ come from a new dynamic fallback, `Assets/Fonts/Inter-Symbols SDF.asset` (made from Inter Regular), added to the fallback list of the four Inter and EB Garamond Regular/SemiBold SDF assets.
- (a), (b), (f): the hybrid was built as the plan expected. Rows are real rows (label, body, right note); the value is one rich-text line (22 px value, 11 px unit and base on one baseline, with `<size>` tags); chips are the `ChipFaint` kind inside a `FlowLayout`; the since slot is the right-hand note. (c), (d): the footer is a real strip with an Image bar; dividers are 1 px Images.
- Seen in Play mode (a real search and a real Put down): the full form, the compact form, the flip to the left at the right-hand edge, wrapping of the footer and the right note.

**Differences from the plan**
- Only layout tips (action tips) use `Beside`; plain-text tips keep following the mouse inside the new frame. Moving them is ui-036b's job.
- The compact form shows no repeat rule (title and one line only, as step 6 says).
- The scene's tooltip instance has `_delay` 0.1 (a hand tweak in the scene), so the first tip opens after 100 ms there, not 350 ms; the prefab's default is 0.35.
- Two chips don't fit side by side beside a right-hand note (230 px of body), so they stack; with no note they sit on one line only if they fit.
