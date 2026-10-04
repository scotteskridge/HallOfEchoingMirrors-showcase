# Backlog roadmap: clearing `docs/BACKLOG.md` (*Now* and *Next*) in batches

**Status:** Approved (order and grouping 2026-10-01; the user also asked for plans for the writing and UI needs, added as batches 049 to 051)
**Left to do:** plan each batch below in its own session. Each batch gets its own `/plan-feature` session, in the order below. Batches 042 and 043 can start now; 045 to 047 wait on a `/design` session or a playtest; 048 waits on the loop being judged fun.

One plan per batch, each ≤8 steps. UI items are not here: they live in `ui-backlog-roadmap.md`. Where a game-rules item pairs with a UI item, the pairing is named. Plan numbers 042 to 051 are reserved by this roadmap; the next free number after it is 052.

## Batches in build order

| Order | Plan | Backlog items (`docs/BACKLOG.md`) | Size | Lane | Before starting |
| --- | --- | --- | --- | --- | --- |
| 1 | `042-since-last-run-core` (**done 2026-10-02**, `finished/`) | *Next 5*, Core half: one kept float per skill (its speed at the start of the last run), a save version bump with an upgrade step, best-ever milestone times read from the run records, and tests | M | Features | Done (the kept value is mastery XP, not speed: decisions log 2026-10-02) |
| 2 | `ui-043-first-exit-moment` | *The first exit's moment: her stats arrive*, with **UI-BACKLOG Next 13** (hide the stats row and the Summary's stat lines while every stat is asleep; show settled mastery and which share applied; remove the orphaned keys `tips.xp_at_max`, `tips.mastery_at_max`) | M | UI | Plan 041 built and checked by you in Unity. Should land before ui-036 and ui-037 so they design for "skills only until the first exit". Its story beat is a `[PLACEHOLDER]` key for `/writing` |
| 3 | `ui-044-since-last-run-display` | *Next 5*, display half: "since last run" on action tooltips and the best-ever milestone time. **This is the UI roadmap's batch 4b** | S–M | UI | 042 built, and ui-036 (tooltip shape) built; it fills the place ui-036 leaves in the full form |
| 4 | `045-container-crafting-two-step` | *Two-step container crafting* (gather a resource, then combine it into the satchel, pouch and the lab's earrings) | M | Features | `/design` session first (questions below). Touches the satchel and pouch tasks, the lab's earrings, §5 *Pockets* `[OPEN]` |
| 5 | `046-draw-and-room-speed-up` | *Next 9*: rework "Draw on the mana stone" for room speed-up | M | Features, plus a UI follow-up if the design adds a screen | `/design` session first (questions below), with ui-038 (room speed visible) and ui-039 (Summary part C) built so the design starts from the real screens. Not the top priority (the user, 2026-09-29) |
| 6 | `047-balance-followups-code` | **Only if the balance pass needs code** (a formula or a new Balance Sheet column), from *Travel's escalating charge*, *round the tasks' Time × values* and *Feed Your Hours at 20 s* | S | Features | The balance pass below. If you can do everything in the Balance Sheet, this plan is never written and 047 is skipped |
| 7 | `048-code-quality-pass` | *Next 16c*: oversized files, `Simulation.Tasks.cs` is done (218 lines, 2026-10-01); what's left is `Simulation` as one ~4,800-line class over 25 partial files. Opus plans which cohesive pieces to extract, and in what order, then `/refactor` builds it; may split in two | M–L | Features | **Deliberately waiting:** the loop has been judged fun (stage 1 done), and no feature work in flight. Don't mix it with feature work |
| 8 | `049-writing-pass` (done, in `finished/`) | *Writing pass (032a)*: reword `feed.planning_unlocked`, `reasons.not_by_heart_plan` and the Dark Corridor's shut message (on `LeftCorridor.asset`) | S | Design (`/writing`) | Nothing; can run any time. Its `/plan-feature` session only scopes it: your prose is never overwritten (`.claude/rules/content.md`) |
| 9 | `050-writing-pass-placeholder-keys` | The `[PLACEHOLDER]` keys the UI plans leave for you: ui-043's story beat, ui-036's item line in Clara's voice, ui-038's "first speed-up ever" note | S | Design (`/writing`) | ui-043, ui-036 and ui-038 built, so the keys exist and show where they appear on screen |
| 10 | `ui-051-summary-charges-display` | *Summary lists every escalating charge*, display half: one entry per charged task beside "Moves: N, costing X vitality" in `RunResultsPanel.cs`, with new text keys | S | UI | Nothing (Core half is built, `RunReport.Charges`). Do it before ui-039 so the Summary layout pass starts from it; the balance pass's Travel check is easier with it |

Batches 8 to 10 are numbered after the gated ones, but 049 and ui-051 have no gate and can be planned any time.

**Overlaps with the UI roadmap**
- *Next 5* is the UI roadmap's **batch 4b**, now two plans: 042 (Features) and ui-044 (UI). The UI roadmap's 4b row should point here when this roadmap is approved.
- The first exit's moment pairs with **UI-BACKLOG Next 13** in one UI-lane batch, ui-043.
- The *Summary lists every escalating charge* display half (*Now*) is UI work but has its own plan here, ui-051, so it isn't lost. It edits `RunResultsPanel`, as ui-039 (Summary part C) will, so it goes first. When it is planned, move the backlog line to `UI-BACKLOG.md`.

## Sorted: every *Now* and *Next* item

| Item | Bucket | Where it goes |
| --- | --- | --- |
| Writing pass (032a): reword `feed.planning_unlocked`, `reasons.not_by_heart_plan`, the Dark Corridor's shut message | Ready now | Batch 8 (049) |
| GDD stale-and-duplicate pass (040c) | Ready now | *Other sessions* (`/design`) |
| *Next 5* since last run, Core | Ready to plan now | Batch 1 (042) |
| First exit's moment | Ready to plan now | Batch 2 (ui-043) |
| *Next 5* since last run, display | Ready once 042 and ui-036 are built | Batch 3 (ui-044) |
| Two-step container crafting (`[OPEN]`) | Needs a `/design` session first | Batch 4 (045) |
| Next 9, Draw on the mana stone | Needs a `/design` session first | Batch 5 (046) |
| Follow-up: Travel's escalating charge too steep | Needs your playtest first | *Balance pass* below; batch 6 only if code |
| Follow-up: tasks' Time × raw floats (0.3333333) | Needs your playtest first | *Balance pass* below; batch 6 only if code |
| Follow-up (022/022b): Feed Your Hours 20 s | Needs your playtest first | *Balance pass* below |
| Before Steam: AI disclosure, Grok licence email | Needs you outside the game | *Outside the game* below |
| Next 16c, code-quality pass | Deliberately waiting | Batch 7 (048) |
| Summary lists every escalating charge (display half) | Done 2026-10-02 | Batch 10 (ui-051) |
| Stats rework (041), *Remove the mastery cap*, *Retire PoolSettings.name / vitalityName*, Next 8 (*Walking out*), *Act I without stats* | Already planned and built | Plan 041, waiting on your in-Unity check (*Open plans*) |
| Next 10, 11, 12, 13, 15 and 2 to 4 | Moved or superseded (the backlog says so) | Nothing to do |

## Other sessions (not `/plan-feature` plans)

| Session | What | Lane | When |
| --- | --- | --- | --- |
| `/writing` | Builds batches 049 and 050 once planned: your prose in your words, never overwritten | Design | 049 any time; 050 after ui-043, ui-036 and ui-038 |
| `/design` | GDD stale-and-duplicate pass (040c): one short session, each § edit shown for your OK, aim ~1,000 words saved | Design | Any time; its backlog note says "after ui-033", which is built |
| `/design` | The two design sessions batches 4 and 5 need | Design | Before their `/plan-feature` sessions (questions below) |

## Needs your playtest: the balance pass

One session, then you tune in *Hall of Echoing Mirrors → Balance Sheet*. I don't change the numbers (`PROJECT_NOTES.md`). Do it after a full Act I through to the first exit has been played at roughly ×1, not the dev panel's ×10 to ×20, because pace is the point.

| Follow-up | What to look for while playing |
| --- | --- |
| Travel's escalating charge (3, ×1.08 a move; `roomStep` 1.1) | Does back-and-forth pathing feel like the puzzle or like a tax? Watch the Summary's "Moves: N, costing X vitality": is walking the main thing that ends runs, instead of the tasks she chose? Does a deliberate three-room round trip cost about what you'd expect by the end of the run? Does vitality left at the lab leave room to try the talk, earrings and gem? |
| Tasks' Time × raw floats (0.3333333) | Open each task's tooltip and the Balance Sheet: which durations show an ugly number, and which of those would you want as a round time? Tune Travel, `roomStep`, Gather (0.2) and Handle (0.1) first; round after they settle, or the rounding is redone |
| Feed Your Hours at 20 s | At the first Feed, is 20 s a wait where you stop and read, or a wall? Compare against the tasks around it: does it feel twice as long as them? Do you reach for pause, or skip it? Does room speed (unlocked by it) then pay it back quickly enough? |

If a finding needs code, for example a formula instead of a number, note it for batch 6 (047).

## Needs you outside the game

- **Grok licence:** email support@x.ai to confirm Grok Imagine output may be used in a commercial game (its Terms want "permission" and attribution for Output); keep the reply in `Assets/Art`; note the account type and date for each Grok image (`Assets/Art/GROK-IMAGINE-LICENCE.md`). Before stage 3, when the Steam page goes up.
- **Steam page:** add the AI-content disclosure. Stage 3.

## Questions for you (design is yours; I haven't settled any)

**Two-step container crafting (batch 4, `/design`)**
1. Which containers become two-step: the satchel and pouch only, or the lab's earrings too?
2. Where does the first step's resource come from (found by searching, a pick-up in a room, a task's output), and may it be missed or lost, which would make it a puzzle across runs?
3. Does the one-step version stay as a fallback, or is it replaced?
4. Does the player see the combine step as its own action, or does it happen automatically when she holds both?
5. §5 *Pockets* is marked `[OPEN]`: does this settle it or only part of it?

**"Draw on the mana stone" (batch 5, `/design`)**
1. Room speed already rises on its own from runs by heart (plan 025, `byHeartRuns` to `fullSpeedRuns`). What does Draw still add: nothing (remove it), a way to choose which rooms speed up, a spend that speeds them faster, or something else?
2. Does it stay a task in the lab, or move to the Summary and Plan screens (§13a, §12)?
3. Should it cost mana, vitality or a once-a-run item, and what does the player lose by not using it?
4. Does Draw come before or after the Hub exists (stage 2)? The backlog says it is parked as a placeholder in the lab.

**The first exit's moment (batch 2: the look is yours; Claude only builds it)**
1. Plan 041's session settled that stats arrive at zero, but this backlog line still says `[PROPOSED]`. May the tag be cleared when 043 is planned?
2. Where does the moment happen: on the Summary after the walk out, on the main screen the next run, or in the Story box?
3. Do the five stats appear together or one at a time, and do they show at level 0 or stay hidden until the first XP?

**Order**
1. Batch 2 (ui-043) is placed before ui-036 and ui-037 so their tooltips and star bar design for "skills only until the first exit". Is that the order you want, or should it follow them?
2. Batch 1 (042) can run in the Features lane while the UI lane builds ui-036. Their only shared files are `ClaraTips.cs` and the tooltip text keys, and 042 doesn't touch those, so I expect no clash. Do you want it started now?
