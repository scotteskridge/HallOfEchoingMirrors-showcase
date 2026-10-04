---
name: sync-state
description: Regenerate docs/BUILD-STATE.md, the plain-language snapshot of what the game build does right now. Run by /wrap-up after any change to gameplay, content or balance numbers, and when asked to sync state.
model: sonnet
effort: medium
---
Update `docs/BUILD-STATE.md` so it describes the game **as it is built right now**. It's the cheap way for the user, and for design sessions, to see what's built without reading code, so write for a game designer, not a programmer: plain language, class names only where they help, no code. Use a subagent to read the code and assets, so the reads stay out of this conversation.

**Sources of truth:** the code, the content assets in `Assets/Data/`, LoopSettings values, `git log`, and `DesignNotes/decisions-log.md`. Never copy claims from the GDD into this file; the point is to show where the build and the GDD differ.

Keep it under ~2,000 words so it can be read in one go. Rewrite sections in place; don't append history.

Use exactly this structure:

```
# Build state — Hall of Echoing Mirrors
Updated YYYY-MM-DD · commit <short hash> · compared against GDD v<x.y>

## A run right now
One paragraph: what a player actually does from New Game to the end of a run, in today's build.

## Systems
| System | Status | How it works now | GDD § | Differs from GDD? |
Status is one of: Built · Partial · Placeholder · Not started.
One row per system (vitality/drain, pools, stats, skills, mastery, action queue, travel/places, searching, pockets/containers, floor, carried items, switches/milestones, saving, story feed, meta currency, realms…).

## Current numbers
The main tuning values from LoopSettings and key content, as a table (name · value · what it does). Only numbers a designer would reason about.

## Content in the build
Places, tasks/verbs, items (note pocket use, containers and capacity), switches/milestones, story passages written vs placeholder. Names and counts, grouped; not every field.

## Where the build differs from the GDD
Numbered list. Each item: what the GDD says (with §), what the build does, and whether that's a deliberate change (cite the decisions-log date) or just not built yet.

## Open questions raised while building
Things implementation needed a design answer for and got a placeholder instead. Each: the question, the placeholder in use, where it lives.

## Recent changes
One line pointing at `docs/CHANGELOG.md`, which lists finished features; don't repeat them here.
```

After writing: show the diff summary (which sections changed). Commit only when the user says so.
