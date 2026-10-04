# Design Notes

Game design docs for Hall of Echoing Mirrors, tracked in git. Design work happens in Claude Code (`/design`).

- This folder sits at the project root (outside `Assets/`), so Unity never imports it.
- **One question, one place:**
  - `VISION.md`: what the game is for. One page of design pillars; only Scott changes it.
  - `GDD.md`: the living design (its version is in the title line, and git holds the history). Kept current: a settled point is edited in the session it's settled.
  - `decisions-log.md`: why things were decided, newest first; older entries in `decisions-log-older.md`.
  - What the build does: the code and `docs/plans/`, summarised in `docs/BUILD-STATE.md`. Finished features: `docs/CHANGELOG.md`.
- `act-II-design.md` is the single reference for Act II (the Amber realm), to read when Act II is planned. `future-sketches.md` holds the older sketches for Act II and beyond.
- Not committed: `Archive/` (versions from before git tracking, and retired docs such as the opening brief, now GDD §12a) and the novel background files (see `.gitignore`).
