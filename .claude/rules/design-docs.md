---
# The design docs, decisions log and plans.
paths:
  - "DesignNotes/**"
  - "docs/plans/**"
  - "docs/BACKLOG.md"
---
# Design docs

- **`VISION.md`**: the pillars every feature serves; only the user changes it. Read it in `/design` and `/plan-feature`; flag a feature that fights a pillar.
- **`GDD.md`** is the living design for build stages 1 and 2 and wins on what the design intends. Grep for the §; never read it whole. When the user settles a point, show the § edit, apply it on OK (commit message starting `GDD:`), then log it.
- **`decisions-log.md`**: why things were decided, newest first (date, choice, why, GDD § changed). History, not the current rule: if it disagrees with the GDD, a GDD edit was missed; flag it. Older entries: `decisions-log-older.md`, grep by topic.
- What's built: the code and `docs/plans/` win; `docs/BUILD-STATE.md` summarises them in plain English (`/sync-state`). Finished features: `docs/CHANGELOG.md`.
- `future-sketches.md`: Act II+ sketches (archetypes, wall types, challenges, memory nodes, the switch graph), read only for those. `act-II-design.md`: the Act II reference; update it when an Act II decision is logged; it wins over the GDD for Act II.
- Also *Balancing Formulas v0.1*, *Narrative Document v0.1* and the spreadsheets (read `.xlsx` by unzipping its XML).
- GDD labels: **[BUILT]** as built · **[BUILT — differs]** built differently · **[DIRECTION]** not built yet · [PROPOSED] · [OPEN]; a `~` marks a number awaiting playtest; nothing is decided. Labels change when the user settles a point or approves `/wrap-up`'s proposal.
- Flag conflicts between code, GDD and decisions. A placeholder rule is labelled `Placeholder rule:` in its comment and listed under "Placeholder rules to revisit" in `PROJECT_NOTES.md`.
