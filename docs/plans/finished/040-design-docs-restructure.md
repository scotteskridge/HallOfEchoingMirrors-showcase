# 040 — Design docs restructure: vision, living GDD, changelog

**Status:** Done 2026-10-01
**Design:** decisions-log 2026-09-29 *No design chat* (superseded in part, see step 6); the user's answers in the planning session, 2026-10-01
**Left to do:** nothing.

## Goal
Three documents, one job each, so a Claude Code session checks one place per question:
- **`DesignNotes/VISION.md`**: what the game is for (pillars). Rarely changes.
- **`DesignNotes/GDD.md`**: the living design, kept current in the session a point is settled.
- **`docs/CHANGELOG.md`**: finished features, one line each, in player language.

`decisions-log.md` becomes the *why* (history); `docs/BUILD-STATE.md` stays the plain-English *what's built*.

## Out of scope
- Slimming the GDD's `[BUILT]` sections to pointers (≈41 of them): plan 040b, after this one (backlog line added).
- Splitting the GDD into one file per system: wait until after the stage 1 playtest.
- Any change to game code, assets or tests.

## Settled with the user (2026-10-01)
1. **BUILD-STATE stays**, revived as the "what the game does now" summary, kept by a project `/sync-state` skill that `/wrap-up` runs. The claude.ai-synced user-level `sync-state` skill is removed by the user (Claude can't). Logged as a decision superseding the BUILD-STATE part of *No design chat*.
2. **The GDD is edited when a point is settled:** Claude shows the § edit, applies it on the user's OK (commit starts `GDD:`), and adds a short log entry.
3. **`opening-brief.md` merges into the GDD**; the original moves to `DesignNotes/Archive/`.
4. **Pillars:** Claude drafts 3–5 from GDD §2, §3, §14 and the brief; the user approves each before it's saved.
5. **Changelog lines are player-facing** (what the player can now do or see; no class names), so they can become Steam patch notes. Plan link in brackets at the end.

## Design assumptions
- Where the opening brief and later decisions disagree (e.g. the brief says drain is action-driven; 2026-09-25 made it time-driven), the merge **does not resolve it**: it lists each conflict for the user and uses the decisions log's rule in the meantime, marked `[OPEN]`.

## Changes
| File | New / Edit | What |
| --- | --- | --- |
| `DesignNotes/VISION.md` | New | Pillars, player fantasy, audience, comparables, non-goals, what Act I should prove. ≤1 page |
| `docs/CHANGELOG.md` | New | Header + backfill: one player-facing line per finished plan (44 in `docs/plans/finished/`), grouped by date from `git log` |
| `DesignNotes/GDD.md` | Edit | New § from the opening brief; *Order of precedence* rewritten (vision → GDD for intent; code/plans → BUILD-STATE for built; log = why); drop the dead `BUILD-STATE-UI.md` link; drop *Changes from v0.4* (git has it); "update labels only when asked" → per decision 2 |
| `DesignNotes/opening-brief.md` | Move | → `DesignNotes/Archive/` (`git mv`) |
| `DesignNotes/README.md` | Edit | List VISION, GDD, log roles |
| `.claude/rules/design-docs.md` | Edit | Exact text below; **user OK first** |
| `CLAUDE.md` | Edit | Exact lines below; **user OK first**; must end ≤1,100 words (it's 1,107 now) |
| `.claude/skills/design/SKILL.md` | Edit | Step 1: read `VISION.md` first. Step 4: show and apply the GDD edit on OK, then the log entry |
| `.claude/skills/plan-feature/SKILL.md` | Edit | Step 1: name the pillar(s) the feature serves; flag one it fights |
| `.claude/skills/wrap-up/SKILL.md` | Edit | Step 3: changelog line (player language); run `/sync-state` if gameplay, content or numbers changed; propose GDD label changes; move log entries over two weeks old to `decisions-log-older.md` |
| `.claude/skills/sync-state/SKILL.md` | New | Copy of the user-level skill, description fixed (no "design chat"), one file only (no BUILD-STATE-UI) |
| `docs/plans/_TEMPLATE.md` | Edit | *Design:* line names the pillar; *Done when* adds "changelog line" |
| Other files naming `opening-brief.md` or BUILD-STATE-UI | Edit | Point at the new GDD § (grep `DesignNotes/`, `docs/`, `.claude/`; skip `plans/finished/` and the logs, which are history) |
| `.claude/skills/workflow/NOTES.md` | Edit | Dated line for the change |

### Proposed `CLAUDE.md` lines (replace)
- Line 13 → ``- `DesignNotes/`: `VISION.md` (pillars), `GDD.md` (the living design), `decisions-log.md` (why). If it's missing, say so and stop on design questions; don't reconstruct the design from code.``
- Line 27, last two sentences → ``The code and `docs/plans/` win over the GDD on what is built. When the user settles a point, show the GDD edit and apply it on OK; spreadsheets only when asked.``
- Backlog line: append ``` `docs/CHANGELOG.md`: finished features.``` and trim elsewhere to stay ≤1,100.

### Proposed `.claude/rules/design-docs.md` body
```
- **`VISION.md`**: the pillars every feature serves; only the user changes it. Read it in `/design` and `/plan-feature`; flag a feature that fights a pillar.
- **`GDD.md`** is the living design for build stages 1 and 2 and wins on what the design intends. Grep for the §; never read it whole. When the user settles a point, show the § edit, apply it on OK (commit message starting `GDD:`), then log it.
- **`decisions-log.md`**: why things were decided, newest first (date, choice, why, GDD § changed). History, not the current rule: if it disagrees with the GDD, a GDD edit was missed; flag it. Older entries: `decisions-log-older.md`, grep by topic.
- What's built: the code and `docs/plans/` win; `docs/BUILD-STATE.md` summarises them in plain English (`/sync-state`). Finished features: `docs/CHANGELOG.md`.
- `future-sketches.md`: Act II+ sketches, read only for those. `act-II-design.md`: the Act II reference; update it when an Act II decision is logged.
- Also *Balancing Formulas v0.1*, *Narrative Document v0.1* and the spreadsheets (read `.xlsx` by unzipping its XML).
- GDD labels: [BUILT] · [BUILT — differs] · [DIRECTION] · [PROPOSED] · [OPEN]; `~` marks a number awaiting playtest; nothing is decided. Labels change when the user settles a point or approves `/wrap-up`'s proposal.
- Flag conflicts between code, GDD and decisions. A placeholder rule is labelled `Placeholder rule:` and listed under "Placeholder rules to revisit" in `PROJECT_NOTES.md`.
```

## Steps
1. **Vision:** draft `VISION.md`; show each pillar; save on the user's OK.
2. **Rules and CLAUDE.md:** show the exact lines above (adjusted for the word budget); apply on OK.
3. **Skills and template:** edit `/design`, `/plan-feature`, `/wrap-up`, `_TEMPLATE.md`; add project `/sync-state`.
4. **Merge the brief:** new GDD §, conflict list to the user, `git mv` the brief to Archive, fix references.
5. **GDD header:** precedence, dead link, *Changes from v0.4*, labels rule. Show the diff; apply on OK.
6. **Log and changelog:** decisions-log entry (this restructure; supersedes the BUILD-STATE part of *No design chat*); create `docs/CHANGELOG.md` with the backfill.
7. **Check:** grep for `opening-brief`, `BUILD-STATE-UI`, `unless asked`, `design chat` outside history; `wc -w CLAUDE.md` ≤1,100; each rules file <200 lines.

## Tests
No code changes, so no EditMode tests. Checks: step 7's greps come back clean (outside `plans/finished/` and the logs); a fresh `/design` session on any topic reads VISION + one GDD § + the log without needing the brief.

## Done when
- [x] The three documents exist and each says its job in its first line
- [x] Rules, `CLAUDE.md` and skills updated with the user's OK; budgets kept
- [x] The user has removed the claude.ai-synced `sync-state` skill (in claude.ai's skill settings, where synced skills are managed)
- [x] decisions-log entry added; backlog line for 040b added

## Notes after implementation
- **Pillars:** the user approved 2–5 and added pillar 1 in their own words (agency: meaningful choices, never an obviously optimal one). Pillar 6 (prose) was approved as drafted after a reread.
- **`CLAUDE.md`:** besides the planned lines, it was trimmed to 1,098 words: the *Tabled* list moved to `VISION.md`'s non-goals; the rules-file names, "Any other chat is a coding or general session" and "say so and" went.
- **The opening brief** became GDD §12a, not a section inside §12. Its parts already in the GDD or `future-sketches.md` (hub split, one-way valves, memory nodes, node types) became pointers. Two conflicts are marked [OPEN]: drain in the prologue, and Act I's length (\~7–12 runs against 10–15). The original is in `DesignNotes/Archive/`, untracked like the rest of that folder.
- **GDD:** §1 *Status* became a short pointer to BUILD-STATE and the changelog (it was stale: it said the lab's end wasn't built). The v0.4 change list went; its one live question (the ring's bleed) is now §17 question 28.
- **`docs/BUILD-STATE.md`:** *Recent changes* now points at the changelog. The rest is still dated 2026-09-30 and needs a `/sync-state` run.
- **Changelog backfill** skips plans the player can't see (refactors, the jump-ahead dev tool, the cost curve, the colour roles asset, design-only and superseded plans).
- **`Remove Me/`** was deleted at the user's request instead of adding a deny rule (the auto-mode safety check refused Claude editing its own permissions). Everything in it was in git history (`21fe0aa` and earlier) or superseded by `.claude/skills/sync-state/`.
