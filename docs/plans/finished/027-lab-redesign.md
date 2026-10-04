# 027 — The Mirror's Laboratory, redesigned: talk to Roland, craft the gem, exit

**Status:** Spec (the shared content tables), **refreshed 2026-09-30** against the cost curve (plan 030a/b) and the stats pass (plan 031). **Built by three plans, in order:** `027a-lab-phase-1.md` (talk to Roland), `027b-lab-earrings.md` (the search reopens, earrings, dense wisps), `027c-lab-gem-and-exit.md` (the gem and the exit). Each ends playable. Every number `~`, to tune in play.
**Status: Done 2026-10-01** (built 2026-09-30 through 027a–d; the user played it and signed it off (2026-10-01)). **Left to do:** nothing.
**Settled 2026-09-30, at build time (the user):** the reopened search finds the earrings at **15%** and the gem's tasks at **25%** (not 50% and 25%); the gem's gate is **Crafting 8**; 027d built too (decisions log *The lab's end is built*).
**Settled 2026-09-30 (the user):** the insight's display name is **"Insight"**; **Practise the cut gets an escalating charge** (plan 030b's `escalatingCharge` / `chargeGrowth`, placeholder 2 × 1.15 `~`); Draw a dense wisp does not. **Numbers are labelled placeholders** (the user, 2026-09-30: no playtest reading first); see *Where the numbers come from*.
**Design:** GDD v0.5 §12 *The memory lab: Act I's finish*; decisions-log 2026-09-29 *The Mirror's Laboratory, redesign in progress* and 2026-09-30 *Lab plan: search reopens, phase 1 locks*; opening brief *The rest of Act I: the lab memory*

> **Design change 2026-09-30 (decisions log *Features come in layers*): Act I has no stats.** Stats arrive when she first exits. This spec stands except for the following:
> 1. **No new stat dependencies.** **Craft the gem's gate becomes a skill gate on Crafting** (this run's level plus mastery), replacing Attunement 26. **Placeholder rule:** set the number so Practise the cut is needed first and the first attempt still runs her dry. **The earrings drop `needsPerception` 15.** **Placeholder rule:** they become a plain find at a later threshold of the reopened search. New tasks set no `trainsAttribute` overrides.
> 2. **What's already built stays for now:** the *Kind → stat* column, rule 8 (Attunement wakes at the talk), and the skills' *learns faster with* stat. Backlog *Act I without stats* removes them later, together with the Act I rebalance. They aren't part of 027b/c/d.
> 3. **Stats waking on the first exit, and stat XP settling on a walk out, are not in 027c.** They belong to backlog Next 8.

> **Confirmed 2026-09-30 (decisions log *Act I's exit stays the Smoky Mirror*): the exit is the Smoky Mirror, not a mirror in the lab.** The gem opens the mirror she came in through, and Act I's last run is the walk back through the hall with the gem. Build *Exit through the glowing mirror* at the starting room (`HallMirror`) as this spec and `027c` say; the lab gets no exit mirror. Also from that session: Act I takes **10–15 runs** at minimum, depending on how efficient the player is (30 minutes to 2 hours, still being tuned), so the *Goal*'s "7–12 runs" is the low end of that.

This file holds what the room contains: items, tasks, switches, text. The Core changes, steps, tests and Done-when checks are in the three build plans. Read the decisions-log entries first: they are the authority where this is unclear.

## Goal

The lab room becomes Act I's last 3–5 loops, in two phases:
1. **Put the room to rights.** Over 2–3 visits she builds a kept insight by watching Roland, and each visit studies the tome and attends; when she has enough, **Talk to Roland** (once ever) convinces his memory to take the ring. The ring is never fetched or carried again, and the lab opens without it.
2. **The gem.** She finds the white sapphire earrings (kept), which hold dense wisps (a stronger restorative), practises the cut, and over one or more runs **crafts the gem**: a long task her first attempts cannot finish.
3. **The exit.** The gem opens the mirror she started at; she walks home through the hall and **exits through the glowing mirror**: story, then the Summary. (The meta-currency screen is not designed: out of scope.)

A friend playing cold should reach the exit in about 7–12 runs in total.

## Out of scope

- **The meta-currency screen** (not designed, UI or balance). The exit ends the run with `WalkedOut` and shows the Summary; nothing else.
- **Pools arriving empty / Amber as a working pool.** The build starts all seven pools full (`LoopSettings`, all `startsUnlocked: 1`). The gem gives a kept item and a switch; wiring it to the Amber pool waits for *Pools taken in the prologue* (BACKLOG-later).
- **Amber binding** (Act II direction only).
- **Draw on the mana stone**: parked unchanged (BACKLOG Next 9). Leave its asset, its switch and its place in the room exactly as they are.
- **The plot**: what Watch him reveals and why Roland is convinced. Story passages are written as `[PLACEHOLDER]` in a writing session; do not invent reveals.
- **Rebalancing the hall.** Only the lab's numbers are set here.
- **Switching action or carry costs on.** See *Design assumptions*.

## Design assumptions

State these in the report; flag any the code contradicts.

1. **Costs are off.** `LoopSettings.chargeActionCosts: 0` and `chargeCarryCosts: 0`, so a task's `costs` and the ring's `carryCostPerSecond` do nothing today. The **escalating charges** are paid even with costs off (Travel: 3 × 1.08 a move; Practise the cut: 2 × 1.15 a go). Otherwise time is the cost (vitality drains at 1/s, growing 56.25% a minute). The cost column is filled in for when costs are switched on; don't switch them on in this plan. **Flag to the user:** the ring's "major vitality drain" the talk removes is only felt as the ring's time cost to fetch (Instantiate Roland's ring, 15 s at depth 2, plus the trip); if the user expects its carry cost to bite, that is the separate costs toggle.
2. **Durations come from the cost curve** (plan 030a): base = family coefficient × 15 s × 1.1^depth × the task's *Time ×*; the lab is depth 4 (×1.4641, so one "lab unit" = 21.96 s × the family), the starting room depth 0. XP = ceil(1.5 × base × *XP ×*). Durations below are at skill level 0; the *Time ×* is what the setup step writes. Base vitality 50 plus the Endurance bank; a phial restores 20 over 3.5 s.
3. **"Once ever" = a switch that locks the task on completion** (as `TheManaStone` locks its task), not a new field.
4. **Searching is do-once and kept** (plan 020), so the room's search reveals its tasks in stages, once.
5. **The starting room is `Places/HallMirror.asset`** (confirmed 2026-09-30: `GameContent.startNode`).
6. **Pacing target** (to check in play, not to prove in tests): on the first visit she can afford Watch him, Attend and a little study; the talk is possible from the third visit (insight 3) and takes most of that run; the gem is first attempted the run after, and fails at least once.
7. A task trains its kind's stat unless its `trainsAttribute` names another (built in plan 031; Watch him → Perception and Attend → Composure are already set). The lab's pairings are the decisions log's (2026-09-30, the stats pass): Talk → Attunement (override), Practise and Craft → Attunement (Instantiate default), Take the earrings and Draw a dense wisp → none.
8. **Attunement wakes at the talk:** *Roland Takes the Ring* lists `wakesAttributes: [Attunement]` (decisions log 2026-09-30; the chip's glow is UI backlog 10).

## Where the numbers come from (placeholders, 2026-09-30)

The user chose labelled placeholders over a playtest reading. The one reading available is the user's own save after 16 runs (dev-speed play, so a guide only): **Attunement mastery ≈ 21, Perception ≈ 11, Endurance ≈ 10, Composure ≈ 8**; Instantiate skill mastery ≈ 24; her long runs last 330–390 s with the Dark Corridor with Hanging Mirrors reached at 300–365 s. Skill speed is 1.05^level × 1.01^mastery.
- **Vitality-seconds in the lab** (`~`): she is assumed to have **about 40 s of work in the lab** on the visit the talk first fits (arriving with ~25 s and filling one phial there). Room speed (by heart, ×1.8–×5) is what makes later visits arrive with more; the first lab visits arrive late and short.
- **Talk to Roland, the first wall:** on the talk's run she must also make its needs in that run (Watch 7.5 + Attend 5 + two studies 10 = 22.5 s), so the wall is the rest: **24 s base, 17.5 s at Insight 3** (×0.73), about 0.9 of the ~18 s left. Each extra visit (Insight 4, 5) shortens it (15.7 s, 14.2 s). A new skill (Convincing) starts at speed 1.
- **Craft the gem, the second wall:** **60 s base**, sized so the first attempt (Crafting 0, ~40 s in the lab, less the reopened search and a few practices) cannot finish; about Crafting 10 plus two dense wisps (+80 vitality) fit it.
- **The gem's gate:** Attunement **26** (`requiresAttributes`: this run's level plus mastery). The save's mastery ~21, plus ~2 run levels from the talk run's Instantiate work, plus ~3 from a few practices. Meant to be just out of reach on arrival and met by practising; **the number to check first in play.**
- **The earrings' hidden find:** `needsPerception` **15** (Perception mastery ~11, plus what the lab's searches and Watch him add). Not before 027b.

## Reuse

- `TaskDefinition` in `Assets/Scripts/Core/TaskDefinition.cs`: `oncePerRun`, `needs`, `takes`, `easierWith` (`HeldModifier` time/cost per one held), `requiresAttributes` (stat gate: this run's level plus mastery), `xpReward` (explicit XP; 0 = duration × 1.5), `walksOut`, `startsUnlocked`.
- `ResourceDefinition` (`ResourceDefinition.cs`): `lasts` (ThisRun / Forever / Carried), `startingMax`, containers (`holds`, `holdsHowMany`), restoratives (`restoreVitality`, `restoreSeconds`).
- `SwitchDefinition` (`SwitchDefinition.cs`): triggers `TasksCompletedInOneRun`, `ResourceReached`, `RoomExplored`, `LoopEndedDuringTask`; effects `unlocksTasks`, `locksTasks`, `opensWays`, `closesWays`; story pop-ups.
- Place `foundBySearching` with `atSearched` thresholds (reveal in stages instead of all at 100%).
- `LoopEndReason.WalkedOut` (`Core/LoopState.cs`) and the walk-out path (`Simulation.Tasks.cs` ~890, `Simulation.Carrying.cs`).
- `SkillDefinition` assets (`Assets/Data/Skills/`), the shared XP curve (`XpCurve.From`).
- Existing items: `Whathedidthatnight` (kept, max 5) becomes the insight; `Understanding` (per run, max 3); `Steadyhands` (per run, max 1); `Rolandsring`; `Emptyphial` / `Bottledwell` (pouch and phials).
- Existing tasks: `WatchHim`, `StudyTheTome`, `Attend` (reworked); `FillABottledWell` (added to the lab's list: the well of memories).
- The setup-step pattern in `Assets/Editor/GreyboxSetup.cs` for content changes (never hand-edit `.asset` YAML).
- The Balance Sheet: every number below is edited there; new fields get a column.

## Changes

**Core changes** (details in the build plans): `waivesWayNeeds` on switches (027a; two ways between one pair of rooms is not an option, as `FindWay`, `WayRef` and found ways are keyed by the pair), `reopensSearch` on switches with `RoomFind.afterSwitch` (027b), kept containers (027b; today a kept container holds nothing), the stat gate in the tooltip and room offers (027c).

Save format change? **No.** Unknown Ids are skipped with a warning on load (`SaveSerializer.Find`), and the unlisted assets keep theirs anyway.

### Content

**New content fields** (Balance Sheet columns): `waivesWayNeeds` on switches. (`trainsAttribute`, *Time ×*, *XP ×*, *Charge*, *Charge ×* and `requiresAttributes` already exist.)

#### New skills (assets in `Assets/Data/Skills/`)

| Skill (placeholder name) | `learnsFasterWith` | `description` (placeholder) |
| --- | --- | --- |
| **Convincing** | Attunement | `[PLACEHOLDER] Getting a memory to hear me. It sees who I was then, so I have to be her.` |
| **Crafting** (name `~`) | Attunement | `[PLACEHOLDER] Shaping something that holds: cutting, setting, making it true.` |

Icons: reuse an existing trait icon until art (list it in the report).

#### Items

| Item | New / Edit | Fields |
| --- | --- | --- |
| **Insight** (`Whathedidthatnight`) | Edit | Display name **"Insight"** (the user, 2026-09-30; the asset and its Id keep their name). Kept, max 5. `description`: `[PLACEHOLDER] What I see when I watch him instead of the stone. It stays with me.` |
| `Understanding` | Edit | Per run, max 3 (as now). `description`: `[PLACEHOLDER] The tome's words hold still for a while. Not for long.` |
| `Steadyhands` | Edit | Per run, max 1 (as now). `description`: `[PLACEHOLDER] I am here, in this room, and I can stay.` |
| **White sapphire earrings** | New | Kind Keepsake; `lasts: Forever`; max 1; **not** in a pocket; container: `holds: [Densewisp]`, `holdsHowMany: 2` (one per earring, `~`). `description`: `[PLACEHOLDER] The earrings Roland gave me, back where they belong. Each holds one thick wisp of memory.` |
| **Dense wisp** | New | Kind Restorative; `lasts: ThisRun`; max 0 (unlimited, bounded by the earrings); pocketed like phials (follow `Bottledwell`); `restoreVitality: 40`, `restoreSeconds: 6` (`~`; a phial is 20 over 3.5). `description`: `[PLACEHOLDER] A thick, slow wisp. It takes its time, and gives more back.` |
| **The gem** | New | Kind Keepsake; `lasts: Forever`; max 1; not pocketed. `description`: `[PLACEHOLDER] A copy of the gem she took from me. Amber stirs in it, faint.` (No pool effect in this plan.) |
| `Rolandsring` | Edit | None to its fields. The talk `takes` it. |
| `Benchcleared`, `Thetome` | Unlist | No longer used by any lab task (see *Removed*). |

#### Tasks: phase 1 (unlocked by the room's search)

Numbers are game seconds at skill level 0, at the lab (depth 4). **Family × Time ×** is what the asset holds; **XP** follows (1.5 × base, × *XP ×*). **Cost** applies only when action costs are on.

| Task | New / Edit | Kind → stat | Skill | Family × Time × | Dur | XP | Cost | Needs / takes → gives | Flags | Easier with |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **Watch him** | Edit `WatchHim` | Study → **Perception** (set) | Studying | Study × 0.4269 (as now) | **7.5** | 12 | 30 (as now) | → Insight 1 | once per run | Insight: time ×0.8 each (as now) |
| **Study the tome** | Edit `StudyTheTome` | Study → Scholarship | Studying | Study × 0.2846 (as now) | **5** | 8 | 10 (as now) | *no longer needs The tome* → Understanding 1 (Scholarship's extra-yield rule stays) | repeatable | — (drop Bench cleared) |
| **Attend** | Edit `Attend` | Other → **Composure** (set) | Invoking | Work × 0.2277 (as now) | **5** | 8 | 6 (as now) | → Steady hands 1 | once per run | — |
| **Fill a phial** | Reuse `FillABottledWell` | as is | Gathering | Gather × 2.0661 (as now) | 7.5 at depth 2; **9.1 in the lab** | 14 | 0 | as is | as is | — |
| **Talk to Roland** | New | Other → **Attunement** | **Convincing** | Correct a memory × **0.9108** | **24** | 36 | 25 | needs Insight 3; takes Understanding 2, Steady hands 1, Roland's ring 1 → nothing (the switch does the work) | once per run; **once ever** by switch | Insight: time ×0.9 each (×0.73 at 3, ×0.59 at 5) |

Why these numbers: insight is once a run and the talk needs 3, so **the talk cannot happen before the third visit**, whatever her stats: the 2–3 visit ladder is guaranteed, and extra visits (Insight 4–5) make it shorter. The third visit's lab budget is Watch 7.5 + Attend 5 + two studies 10 + the talk ~17.5 = **about 40 s of work** (*Where the numbers come from*). The existing three verbs keep their times: the cost curve's migration already set them (decisions log 2026-09-30). Filling a phial in the lab is slower than in the corridors (depth 4, the curve's own rule).

**Reveal order** (the room's `foundBySearching`, replacing all-at-100%):

| Searched | Revealed |
| --- | --- |
| 20% | Fill a phial (the well), Watch him |
| 34% | Attend, Study the tome (and Draw on the mana stone, unchanged) — `TheMemoryIsWrong` fires here as now |
| 67% | Talk to Roland (shown locked until its needs are met, never secret) |

**Mechanism (chosen 2026-09-30):** Watch him, Study the tome, Attend and Talk to Roland get `startsUnlocked: 1`, so the lab's search alone decides when they appear (they're offered nowhere else), and *Roland Takes the Ring* locks them after (locking wins over unlocked). `TheMemoryIsWrong` keeps its trigger and story and unlocks **only Draw on the mana stone** (unchanged). Why not a switch's unlock list: `Flip` writes unlocks into the save once, when the switch flips, so a task added to an already-flipped switch would never unlock in an existing save (the user's slot has `TheMemoryIsWrong` flipped).

#### Tasks: phase 2 (found by the reopened search)

*Roland Takes the Ring* **reopens the lab's search** (settled 2026-09-30): the bar starts again at 0%, everything found before stays found, and a second round of finds appears (`RoomFind.afterSwitch` = *Roland Takes the Ring*). Placeholder stages `~`: **25%** Practise the cut, Craft the gem · **50%** Take the earrings, Draw a dense wisp. The tasks below are unlocked by the switch; the search makes them visible.

| Task | New / Edit | Kind → stat | Skill | Family × Time × | Dur | XP | Cost | Needs / takes → gives | Flags | Easier with |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **Take the earrings** | New | Take → none | none | Take × 0.3416 | **3** | 5 | 0 | → White sapphire earrings 1 | once ever (switch *The Earrings* locks it) | — |
| **Draw a dense wisp** | New | Gather → none | Gathering | Gather × 2.2769 | **10** | 15 | 0 | needs Earrings 1 → Dense wisp 1 (into the earrings) | repeatable (stops when the earrings are full) | — |
| **Practise the cut** | New | Instantiate → Attunement (default) | **Crafting** | Instantiate small × 0.3643; **XP × 3** | **4** | 18 | 8 | → nothing | repeatable; **Charge 2, Charge × 1.15** | — |
| **Craft the gem** | New | Instantiate → Attunement (default) | **Crafting** | Instantiate large × 1.8215 | **60** | 90 | 60 | **requires Attunement 26** (`requiresAttributes`) → The gem 1 | once per run; once ever by switch | — |

- **The earrings are a hidden find** (decisions log 2026-09-30, the stats pass): the reopened search shows *Take the earrings* only at Perception **15** (`needsPerception`, *Where the numbers come from*), the game's first. *Draw a dense wisp* is found with it.
- **Practise the cut:** XP × 3 (18 XP for 4 s, the old `xpReward: 18` expressed the cost curve's way; `xpReward` stays 0). **Escalating charge** 2 vitality × 1.15 per repeat this run (the user 2026-09-30; Balance Sheet *Charge* and *Charge ×*): the soft cap on farming. It is always worse than advancing because it gives no item and no story, only XP. It is the training verb for the gate (18 XP ≈ one or two run levels of Attunement early in a run) and for the gem's speed (Crafting 1.05× per level). Check in play that practising doesn't beat just attempting the gem; if it does, lower the multiple, don't cap it.
- **Craft the gem, 60 s:** at Crafting 0 with the gate just met, the first attempt should run her dry partway (she keeps the XP; the task's progress is lost). By about Crafting 10 (×1.63) it takes ~37 s, and with dense wisps it fits. **Gate:** Attunement 26 (`~`), just above the reading's ~23 on arrival, so a few practices meet it. Instantiate trains Attunement all through Act I, so a low gate (the old 3) would stop nothing. The tooltip must show the gate while it's unmet (locked, never secret).

#### Task: the exit

| Task | Place | Kind | Skill | Family × Time × | Dur | Needs → gives | Flags |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Exit through the glowing mirror** | the starting room (`HallMirror`, depth 0) | Other | none | Handle × 2 | **3** | needs The gem 1 → — | `walksOut: 1`; `startsUnlocked: 0`; unlocked by *The Gem Is Made* |

#### Switches

| Switch | New / Edit | Trigger | Effects | Story pop-up (passage, placeholder) |
| --- | --- | --- | --- | --- |
| `TheMemoryIsWrong` | Edit | RoomExplored OtherLaboratory 34% (as now) | unlocks Draw on the mana stone only (see *Mechanism*) | existing `TheMemoryIsWrong` |
| `WhatHeDidThatNight` | Keep | ResourceReached Insight ≥1 | — | existing (the first observation) |
| **Roland Takes the Ring** | New (replaces `TheStoneIsCut`'s role) | TasksCompletedInOneRun [Talk to Roland] | locks Talk to Roland, **Instantiate Roland's ring** (`TakeTheRing`), Watch him, Attend, Study the tome; unlocks Take the earrings, Draw a dense wisp, Practise the cut, Craft the gem; **waives the ring's need on the HangingMirrors → OtherLaboratory way**; **wakes Attunement** (`wakesAttributes`) | new passage: *He Takes the Ring* |
| **The Earrings** | New | ResourceReached Earrings ≥1 | locks Take the earrings | new passage: *The Earrings* |
| **The Gem Cracks** | New | LoopEndedDuringTask [Craft the gem] | — (story only, first failure) | new passage: *Not Yet* |
| **The Gem Is Made** | New | ResourceReached The gem ≥1 | locks Craft the gem, Practise the cut; unlocks Exit through the glowing mirror | new passage: *The Gem* |
| **Home** | New | TasksCompletedInOneRun [Exit through the glowing mirror] | — | new passage: *Through the Glass, Home* (she is back in the lab) |
| `TheStoneIsCut` | Unlist | — | no longer referenced | (passage 12 retired or rewritten by the writer) |

Watch him, Attend and Study the tome **lock after the talk** (the user, 2026-09-30): phase 1 is done and the room reads as changed; the insight stays kept. *Roland Takes the Ring* also has `reopensSearch: OtherLaboratory` (027b adds it; in 027a it has none).

The `reaching` blurb's `endsWhenFlipped` (BACKLOG Next 7 note) points at **The Gem Is Made**.

#### Removed from the room (unlisted, not deleted)

`ClearTheBench`, `TakeTheTome`, `CutTheStone`, the orphaned `SearchTheLaboratory`; items `Benchcleared`, `Thetome`. Remove them from `OtherLaboratory.foundBySearching` and every switch list. **Don't delete the assets** in this plan (saves may hold their Ids); list them for the user under BACKLOG-later *Remove leftover assets*.

#### Tooltip asides (`description`, Clara's voice, first person, present tense, all `[PLACEHOLDER]`)

The UI already builds the mechanical lines (time, gives, needs, skill, XP); `description` is the aside (`UI/ActionText.cs`). Keep each ≤20 words; no plot reveals.

| Task | `description` |
| --- | --- |
| Watch him | `[PLACEHOLDER] I watch him, not the stone. At nineteen I only saw the stone.` |
| Study the tome | `[PLACEHOLDER] The words shift as I read. I hold on to what I can.` |
| Attend | `[PLACEHOLDER] I stay. The memory wants me gone, and I stay.` |
| Fill a phial | (as now) |
| Talk to Roland | `[PLACEHOLDER] He sees his student. I have to be her long enough for him to listen.` |
| Take the earrings | `[PLACEHOLDER] They're on the bench again. They were never meant to be missing.` |
| Draw a dense wisp | `[PLACEHOLDER] The memory is thick here. I draw it slowly into the sapphires.` |
| Practise the cut | `[PLACEHOLDER] Again. My hands remember before I do.` |
| Craft the gem | `[PLACEHOLDER] The gem she took from me. I make it again, facet by facet.` |
| Exit through the glowing mirror | `[PLACEHOLDER] The glass is warm. On the other side, the lab, and home.` |

Locked-state reasons (the UI's existing "needs" text) must read clearly for: Talk to Roland (Insight 3; show the count), Craft the gem (Attunement 26), Exit (The gem).

#### Story passages (for the writing session; titles are placeholders)

New, all `[PLACEHOLDER]` until the user writes them: *He Takes the Ring*, *The Earrings*, *Not Yet*, *The Gem*, *Through the Glass, Home*. Watch him's kept insight may want one short line per level (1–5): **the user's plot, not to be invented**; until then one placeholder line. Passage 12 (*The Stone Is Cut*) is retired or rewritten. Follow `DesignNotes/Writing Guides/README.md`; the opening brief's rule (Roland's memory "sees his student, not the woman who came back") is the talk's flavour.

## Build order

027a → 027b → 027c. Steps, tests and Done-when checks live in each. `PROJECT_NOTES.md` *Placeholder rules to revisit* gets each plan's placeholders as it lands.

## Notes after implementation

- **Phase 2's tasks start unlocked** (not unlocked by *Roland Takes the Ring*), for phase 1's reason: the reopened search's `afterSwitch` finds alone show them. *Roland Takes the Ring* gained only `reopensSearch`.
- **Dense wisp:** max 0 as specified, with a new flag `onlyInContainers` so it never goes loose in a pocket or on the floor; Draw a dense wisp stops at two.
- **Crafting** learns faster with no stat, and has Instantiate's icon until art. Practise the cut and Craft the gem keep kind Instantiate, so they still train Attunement by kind until backlog *Act I without stats*.
- **No stat gate or stat tooltip line** was added (the gate is a skill gate, `requiresSkills`); the stat-gate code is untouched.
- Story passages 23–26 are `[PLACEHOLDER]`; descriptions are on the assets. Details per plan in 027b, 027c and 027d.
