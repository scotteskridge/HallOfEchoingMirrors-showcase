# Main screen composition: design chat critique (2026-09-29)

This is the design chat's review of the in-game screenshot (Mirror's Laboratory popover, dev panel open). It's a proposal, **not settled**. It's the starting point for a main-screen layout plan (backlog *Next* 1, "Main screen re-composition").

## What already works
- **The map nodes are framed mirrors with ornate borders:** the most important visual decision in the project.
- **The warm parchment ground and candlelit palette.** Beside Increlution, it looks like a game rather than an admin panel.

## The problem
The best thing on screen is the smallest. The map fills about 18% of the frame, bottom left, while the top third is eleven progress bars. That's the inversion §13a exists to fix. The fix is re-composition, not art.

## Ranked changes (most gained for least cost first)
1. **The map takes 60–70% of the frame.** Everything above it collapses into one ~64 px bar.
2. **Remove the pathos rainbow bar.** Seven colours in one strip reads as a test pattern. Seven small dark facets say "she has none of these" in a tenth of the space.
3. **The nine stat and skill chips become one row of multipliers:** "Wayf ×3.54 · Gath ×6.01 · …". The rest moves into the tooltip.
4. **Vitality bar:** amber when full, shifting towards red as it drains. Flat full-width red reads as an error banner.
5. **The empty "Queue" bar** (~9% of the screen for one word) becomes the route ribbon's slot.
6. **The room popover opens beside its node,** not floating over the middle of the map.
7. **Take screenshots with F1 pressed** (the dev panel hides).

Their summary: "Two layout sessions from a screenshot that would stop me scrolling, and zero art away from it."
