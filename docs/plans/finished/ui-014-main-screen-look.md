# UI 014 — Main screen look: composition, backdrop, one material, capture clean-up

**Status:** Superseded by `ui-026-look-pass.md` (2026-09-30). Not built as written: the queue column, colour roles and popover placement landed first, so only the backdrop, vignette, top strip and capture clean-up remain.
**Design:** GDD §13a; `docs/mockups/main-screen/` (esp. `composition-critique.md`); `docs/UI-BACKLOG.md` *Now* (3 items) and *Next* 1; decisions-log 2026-09-29 *Warm colours*

## Goal
A screenshot of the main screen shows a hall of mirrors: a big map on a rich backdrop, one slim top bar, one consistent material across every panel, and nothing overlapping. Nothing about how the game plays changes. 

## Out of scope
- **Colour as the payoff, Clara on screen, juice, the ghost of last run, vitality-as-star-bar** — see `docs/UI-BACKLOG.md` *Next* and *Later*.
- **The pathos display** (rainbow bar or seven facets): open, not settled. The top bar keeps the current bar until the user judges both.
- **Tooltip redesign** and **Button/Chip prefabs**: separate plans. Chips and buttons are restyled in place here, so if the prefab work is done first this plan gets smaller.
- **Final painted art**: this plan builds a placeholder backdrop and the vignette. Real art is a later, licensed asset.
- **Per-node art rework** (`docs/BACKLOG.md` *Later*): nodes keep their shared master.

## Design assumptions
- **Placeholder rule:** the backdrop is a dark gradient with a vignette and a faint texture, made from assets we can legally ship, to prove the composition. It is swapped for real art later, so it is one image slot, not baked into the map.
- The map takes 60–70% of the frame as the design chat proposed. The design chat's other changes (popover beside its node, ribbon slot for the Queue bar) are included; its pathos and stat-row changes are not.
- "One material" needs the user's choice before Part C. Options: **(a)** drop the wood-grain bars and let the dark panels carry gilt borders; **(b)** keep the wood and give every panel the same wood/gilt frame. Recommendation: (a), because the wood clashes with the flat black and is the cheaper one to unify. **Not decided.**
- Only one of popover and tooltip is visible at a time. This changes what the player sees, so it is confirmed here by approving the plan.

## Reuse
To be filled in at approval by reading `Assets/Scripts/UI/`, `Assets/Prefabs/UI/`, `Assets/Data/UI/` and `.claude/rules/ui.md`. Already known from earlier plans:
- `UiColours` / `ColourRole` (plan 012): colours for panels and buttons live in the asset, so a material change is an asset edit plus *Apply UI Colours*.
- `UiFonts` / `TextRole` (plan 011): text sizes per role.
- `MapStyle` asset: map, floor and explore-bar colours.
- `PopoverPlacement`, `DragToMove`: existing popover placement to change for "beside its node".

## Parts (each is its own small tested step; commit between)
**A. Composition.** Resize the frame layout so the map takes 60–70%; collapse the top area into one slim bar; the popover opens beside its node. (The Queue bar is now plan ui-019's job: a right-hand column that folds to a strip, and no bottom strip.)
**B. Backdrop and vignette.** One backdrop image slot behind the map (placeholder), plus a vignette that darkens the edges towards Clara.
**C. One material.** Apply the user's choice to the top and bottom bars, Story panel, popover, tooltip and the dev panel's frame (dev panel restyle is optional and only for the editor).
**D. Capture clean-up.** No overlaps (popover / tooltip / pockets overlay / node labels); dev panel off by default in a build and hidden by F1; no placeholder text on screen; pockets and pouch lines anchored to a panel.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| *to be listed at approval* | | |
| `game_text.txt` | Edit | only if new labels appear; list every key added or changed for a `/writing` check |

Layout changes are listed in the report for every step.

New content fields: none expected. Save format change? No.

## Steps
1. Screenshot the current screen at 1920×1080 into `docs/mockups/main-screen/` as `YYYY-MM-DD-before-look-pass.png` (the baseline).
2. **Part A.** Test only what is code (placement rules); the layout itself is checked by eye. Screenshot.
3. **Part B.** Add the backdrop slot and vignette. Screenshot.
4. **Ask the user for the material choice**, then **Part C.** Screenshot.
5. **Part D.** Write a failing EditMode test first for "one of popover/tooltip" if a rule class is involved. Screenshot.
6. Play at 1920×1080 and 1280×800 (Steam Deck) and check nothing is clipped or overlapping.
7. Update `docs/mockups/README.md`, the decisions log (the material choice, the composition), and move finished items out of `docs/UI-BACKLOG.md`.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| *only where a C# rule changes* (e.g. popover placement beside a node, the one-at-a-time rule) | filled in at approval |

## Done when
- [ ] Console clean; EditMode tests pass if any C# file changed
- [ ] In Unity, at 1920×1080: the map is 60–70% of the frame; the backdrop and vignette show behind it; the top bar is one slim strip
- [ ] Opening a room popover and hovering a chip never leaves two panels overlapping, and no label is covered
- [ ] With the dev panel hidden there is no default-Unity look and no placeholder text on screen
- [ ] The user has judged a fresh screenshot against the before shot
- [ ] decisions-log updated; rules-file line proposed if a new system was added

## Notes after implementation
<!-- filled in at wrap-up -->
