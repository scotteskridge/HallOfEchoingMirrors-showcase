# 040b — Slim the GDD to what's in flux

**Status:** Done 2026-10-01
**Design:** `.claude/rules/design-docs.md`; plan 040 (GDD = living design; what's built = code, plans, `docs/BUILD-STATE.md`); GDD v0.5 §3–§13a
**Pillar:** none directly: tooling for the design docs. It protects pillar 3 (*every run teaches something*) and the whole loop by keeping one trustworthy place for what is still being designed. It must not shrink any reasoning that supports pillars 1, 2 or 4.
**Left to do:** nothing.

## Goal
`GDD.md` holds what is still being designed. Where a section only describes what the build does, it shrinks to 2–3 lines and a pointer. Where it carries the *why*, the why stays. Where it disagrees with the build, you decide which is right.

## Out of scope
- No code, assets, tests or spreadsheets. Docs only.
- Unlabelled prose, `[DIRECTION]`, `[PROPOSED]`, `[OPEN]` sections and the tables around them (only a `[BUILT]` / `[BUILT — differs]` label qualifies).
- Re-labelling anything, or settling the conflicts myself: each is yours to decide (below).
- Splitting the GDD into files (parked until after the stage 1 playtest, plan 040).
- Editing `BUILD-STATE.md` (just re-synced). If a shrink needs a fact it lacks, I list it for `/sync-state` instead.

## Design assumptions (change any of these)
1. **The why stays in the GDD**, as one sentence inside the stub. A longer why that has no home yet moves to `decisions-log.md` as one new entry (*GDD slimmed: reasoning kept from §X*), with the GDD stub linking to it.
2. **History goes** ("v0.4 had…", "superseded"): git has it. The *reason* the old way lost is design reasoning and stays.
3. **Numbers marked `~` and "currently" figures go**: they are in the assets and BUILD-STATE and go stale here.
4. **Pointers name a stable home**: `docs/BUILD-STATE.md` (what it does now), a plan (how it was built), or a type name (the rule in code), never a line number.
5. Labels stay as they are on any block I keep; a shrunk block keeps its label.

## The inventory
43 labelled lines in 40 blocks. **S** = shrink to 2–3 lines + pointer · **K** = keep (carries intent or reasoning) · **K−** = keep, but trim build detail or old-version history · **C** = conflicts with the build, you decide.

### Batch 1 — §3 Premise and §4 Loop Economy (lines 60–177, 7 blocks)
| Line | Block | Do | Why / where the reasoning lives |
| --- | --- | --- | --- |
| 64 | Inspired by *Idle Loops*…; fun at default speed | S | Already `VISION.md` ("Not idle… no speed hacks"); stub points there |
| 66 | Clara a practised eromancer | K | Premise; the *why* (she lost the vocabulary, so the player learns alongside her) is the design |
| 68 | Anchor gem vs vitality ends the run | **C1** | Intent says the gem empties → pulled back; the build ends on vitality. Which is the design? |
| 69 | First person, present tense; pop-ups + feed | K− | One line of intent; delivery detail → BUILD-STATE "Story feed" |
| 85 | Mastery faster not deeper (differs) | K− | Keep the principle ("repetition buys efficiency, never reach"); the build's stat-mastery behaviour moves to BUILD-STATE (already there). **C2** (cap 150 vs soft cap) is the neighbouring `[DIRECTION]` and stays as is |
| 173 | Speed control earned: Feed your hours | S | Cost numbers → plan 029 / decisions log 2026-09-30. Keep: "must have felt the pace before being given a lever" |
| 174 | It unlocks room speed, not a global multiplier | S | Merge with 173; the *why* ("a puzzle grants a capability, not a number") is also in the kept paragraph below it and in §13a |

### Batch 2 — §5 Pathos Pools & the Vitality Clock (178–313, 7 blocks)
| Line | Block | Do | Why / where |
| --- | --- | --- | --- |
| 180 | Vitality drains with time, not actions | K | Short; the reason (every speed-up skill buys vitality, so mastery *is* the economy) is core |
| 236 | Candles lower current drain (+ the two-part table) | S | The mechanism is built (BUILD-STATE "Vitality and drain"); keep the one-line why: an early reducer is worth far more than a late one. The Sapphire/concealment paragraph below is unlabelled and stays |
| 253 | Restoratives restore over a clock, auto-use | K | The proof that auto-use needs no refinement is reasoning |
| 255 | One of each kind at a time | K− | Drop "v0.4 proposed…"; keep the consumption-rate argument |
| 268 | ~5 pockets, every object takes one | K− | Keep "separate limits never bind, so the player never chooses"; drop the starting number |
| 270 | Capacity expands in a run through actions | S | "Currently: a satchel +7…" is BUILD-STATE ("Pockets, containers"); keep the memory/instantiation idea in one line |
| 286 | Hues named for gems; the wheel's stations | K | Design intent about the Codex and the ending at Ruby |

### Batch 3 — §6 Stats & Mastery and §9 Tasks (314–441, 9 blocks)
| Line | Block | Do | Why / where |
| --- | --- | --- | --- |
| 316 | Two layers: skills speed, stats everything else | K | The core test for any new stat |
| 332 | Endurance trains from vitality lost | **C3** | BUILD-STATE: the Endurance **bank** (3% of lost vitality + 1% per level) replaced "+max vitality per level" (2026-09-30). The GDD paragraph and the stats table (§6) describe the older job |
| 363 | Stats influence skills through XP rate | K | Reasoning |
| 365 | One stat per action | K | Reasoning (opacity of *Idle Loops*) |
| 400 | Show the multiplier, not the level | K | Reasoning (duration is cost) |
| 432 | Explore and Search are one verb | K− | "The merge is the design"; drop the v0.4 history |
| 434 | Rooms searched afresh every run | **C4** | Build: "searches each room once, ever" (plan 020). The `[OPEN]` hidden-ways remark may be moot too |
| 436 | No Rest or Wait, no idle state | S | Rule in code (queue empties → pause); one line + BUILD-STATE |
| 438 | Gather needs no artificial ceiling | K | The reasoning (compounding drain is the ceiling) |

Also in this batch, unlabelled but stale: §6's "five stats and five skills" and the *Five skills* heading; BUILD-STATE says seven skills. Listed under **C3** for you, not edited unprompted.

### Batch 4 — §11 Persistent Items and §12 World Structure (445–617, 7 blocks)
| Line | Block | Do | Why / where |
| --- | --- | --- | --- |
| 457 | Tools reset every run (differs) | K− | Lead-in to the `[DIRECTION]` carry-out rule; drop "v0.4 said…" |
| 466 | Carried items with ongoing cost: the ring (~0.5/s) | **C5** | Build: 1 vitality per second of action, hard-charged (BUILD-STATE). Keep the three-category idea and the aha (the lab door needs the ring) |
| 470 | Carried items lost when a run ends | S | Rule in code; BUILD-STATE "Carried items" |
| 474 | Checkpoints that do not reset: "eleven exist" | **C6** | BUILD-STATE says sixteen switches. Fix: drop the number (it goes stale), keep "limited by design, or carry-out has nothing to bite on" |
| 512 | The memory lab: Act I's finish (differs) | S | 10 bullets → ~5 lines: keep the design intent (the exit = the gem; the phase 1/2 shape as one line; the Amber direction for Act II) and point to plans 027a–d, decisions log 2026-09-29/30 and BUILD-STATE. Pacing and the full verb list are in the log |
| 550 | No wall clock shown to the player | K | Reasoning (the Summary as a natural exit) |
| 579 | Nodes from day one | K | Reasoning (retrofitting would rewrite movement) |

### Batch 5 — §12a Prologue and Act I beats, and §13 Queue (618–720, 4 blocks)
| Line | Block | Do | Why / where |
| --- | --- | --- | --- |
| 632 | Runs 2–3: exploration, no pools (differs) | S | Keep the design rules (choices not a single bar; each run ends with a discovery); drop the "brief said…" aside |
| 640 | The ring: a carried item with an ongoing cost (differs) | S | Merge with §11 (466): one home for the ring. Keep "the player is expected to drop it" and the `[OPEN]` on its later use |
| 649 | Act I's arc to the lab (differs) | K− | The beat/teaches table is design intent (what each stage teaches); trim the built-by notes, keep the `[OPEN]` Act I length |
| 672 | The queue is built during the run (§13) | S | 2 lines + pointer to BUILD-STATE "Action queue" and §13a, which the section already says is the rework |

### Batch 6 — §13a Interface: the Map Is the Queue (721–886, 9 blocks)
| Line | Block | Do | Why / where |
| --- | --- | --- | --- |
| 769 | Floor piles are map data (differs) | K− | Keep the reasoning (a pile drawn on its node shows what is cached elsewhere); the "in the build piles vanish" sentence moves to BUILD-STATE ("Floor") |
| 779 | Transfers are queue actions, not drag-and-drop | K | Reasoning |
| 802 | Auto-inserted actions visible and counted (differs) | K | It is a **gap**, not history: the report still does not count auto-supply (§14.12). Keep as an open design obligation |
| 812 | Fast-forward earned (differs) | S | Duplicates §4 (173–174); one home, the other points to it |
| 816 | Count visits from the start, so the unlock is retroactive (differs) | K− | Keep the reasoning (rooms already known jump on unlock); drop plan numbers |
| 820 | The curve is a power law (differs) | **C7** | Not a conflict with the build, a **disagreement of intent**: build is a linear ramp placeholder (plan 025). Keep, since the power-law intent is the design, and decide below whether it still is |
| 836 | Visits per node, average into realm speed (differs) | K− | Keep why per-node counting; drop the build note (decisions log 2026-09-29) |
| 862 | The run timer shows game time | K | Reasoning (benchmarks in wall clock show fake improvement) |
| 870 | Benchmarks | K− | The §14.19 link is reasoning; the "four extensions" list under it is unlabelled and stays |

## Decided by the user (2026-10-01)
- **C1:** empty vitality ends the run; that is a redesign, not a gap. Gems storing pathos moved to the meta currency. §3's anchor-gem line is rewritten to say so (the wording of the meta-currency role is left as `[OPEN]` where the GDD hasn't settled it).
- **C2:** mastery is uncapped. Every reference to a cap (including the "build still has a hard cap (150)" sentence in §4) is old and is removed; the cap in the build is a code gap to log in the backlog, not GDD text.
- **C3:** Endurance's bonus banks per run and is awarded as extra max vitality next run. §6's Endurance paragraph and table row are rewritten to that; the skill count is corrected to seven (fix lands with batch 3; order doesn't matter as long as it's done).
- **C4:** once-ever versus until-known-by-heart searching is still being playtested. The GDD marks it `[OPEN]` with both options; no label of `[BUILT]` claims either way.
- **C5:** ring drain stays on, and is likely too low: it should make carrying, dropping and picking up the ring a real decision. §11 and §12a drop the "0.5/s" and "switched off" text, say the drain is on and needs balance from playtest feedback, and keep the aha.
- **C6:** "Eleven exist" is a count of already-built switches (the build now has sixteen). Dropping the number only stops it going stale; no feature is removed. Applied as a normal edit in batch 4, shown for your OK.
- **C7:** keep the power-law text as intent.
- **Rule for the why:** a one-sentence why stays in the stub; longer reasoning goes to one `decisions-log.md` entry (the usual decision-record practice: current intent in the design doc, the reasoning history in the log).

Backlog line to add when applied: the build's mastery cap of 150 contradicts the uncapped design.

## Conflicts as first listed (now decided above)
| # | GDD says | Build says | Options |
| --- | --- | --- | --- |
| C1 | §3: an empty gem pulls Clara back | A run ends when vitality runs out; the gem does not end it | (a) keep the GDD as the intent for later acts, mark it `[DIRECTION]`; (b) change the GDD to the build |
| C2 | §4: mastery has no hard cap (`[DIRECTION]`) | Cap 150 | Already labelled correctly; no action unless you want it dropped |
| C3 | §6: stats table, Endurance trains from vitality lost, five skills | Endurance bank replaced "+max vitality"; seven skills; "Act I has no stats" (2026-09-30, not yet removed) | Update the §6 table and counts, or keep as intent and label `[BUILT — differs]` |
| C4 | §9: rooms searched afresh each run | Searched once ever (plan 020) | Fix the GDD to the build, or keep as a deliberate future change |
| C5 | §11: ring ~0.5 vitality/s; §12a: its cost "switched off" | 1 vitality/s, hard-charged, on | Fix both to the build, or say the 0.5 is the target |
| C6 | §11: eleven switches | Sixteen | Drop the number (recommended) |
| C7 | §13a: the power-law curve | Linear ramp placeholder | Keep as intent (recommended): labelled `differs` already |

## Changes
| File | New / Edit | What |
| --- | --- | --- |
| `DesignNotes/GDD.md` | Edit | The slim, one batch at a time (below); each § edit shown to you and applied on your OK |
| `DesignNotes/decisions-log.md` | Edit | One entry per batch only where reasoning had no home and moved there |
| `docs/BACKLOG.md` | Edit | Item 17 moved to *Now* (done); removed when the plan is finished |
| `docs/plans/README.md` | Edit | Add 040b to the Game rules row |

Save format change? No. No code, assets or tests: nothing to compile or run.

## Steps
1. **You answer the conflicts and the questions below.**
2. **Run one batch per session** (so you review one diff at a time): for each block I show the current text beside the proposed one; you OK, change or skip; I apply the approved ones with a commit starting `GDD:`. Order: 1 (§3–§4, ~100 lines), 2 (§5), 3 (§6 + §9), 4 (§11 + §12, the lab is the biggest edit), 5 (§12a + §13), 6 (§13a).
3. After each batch: grep the GDD for every `§` and plan number the batch mentions so no pointer is dead; make sure no kept `[OPEN]` was lost.
4. After batch 6: a last grep of `[BUILT` to confirm what remains is deliberately kept; set this plan Done and move it to `finished/`.

## Tests
None: docs only. The check is a grep for dead pointers and for lost `[OPEN]`, `[DIRECTION]` and `[PROPOSED]` labels after each batch, and `git diff --stat` showing only `GDD.md`, the log and the plan.

## Done when
- [ ] Each of the 40 blocks is shrunk, kept, or decided; no `[OPEN]`/`[DIRECTION]`/`[PROPOSED]` text was removed
- [ ] C1–C6 are settled and logged (C7 as you choose)
- [ ] Every shrunk block points to BUILD-STATE, a plan or a type that exists
- [ ] `GDD:` commits, one per batch

## Notes after implementation
- All six batches committed (e71b5d0, 966e3c3, 05c6a23, 860e962, 402c687, 6a0889a); conflicts C1–C7 applied as decided.
- Final check: 43 `[BUILT` labels remain (45 before). Each is a short design rule kept with its reasoning, or the label key in *About this document*; none is a build description.
- The GDD shrank less than the goal suggested: 16,720 → 16,384 words. Most of the bulk is design reasoning, kept on purpose. The remaining stale and duplicated text (§16, §17, *About this document*, §13a's built layout, §5's numbers table) is plan 040c on the backlog.
