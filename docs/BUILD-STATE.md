# Build state — Hall of Echoing Mirrors
Updated 2026-10-02 · commit 13e465a · compared against GDD v0.5

## A run right now
From the main menu (three save slots) a new game opens on a short scripted chase after Roland, then the first real run: Clara stands at the Smoky Mirror with 50 vitality and a bar that drains faster every minute. The player builds a queue (travel, search, gather, make, study, talk), can add, reorder or remove entries as it runs, and the game pauses when a way is found. She searches each room once ever, makes wisps, phials, candles and a satchel to carry them, drinks restoratives to push vitality back up (each one used as soon as the drain would take back what it gives), lights candles to hold the darkness off (the Left Corridor's ten must be lit again every run to go into the Dark Corridor), and may carry Roland's ring, which costs vitality every second she holds it. Milestone cards and story passages appear as she reaches new rooms and finds; a short ambient feed comments as she works. Most runs end when vitality hits zero, or when the player ends one. A Summary then shows time by skill, stats and mastery gained, milestones against earlier runs and what was kept; until *Feed your hours* earns planning it offers one "Go back in" button. Her five stats sleep and earn nothing all through Act I, so max vitality stays 50. Switches, skill mastery, knowledge and the rooms she knows by heart persist. Late in Act I, in the lab, she finds the earrings, earns kept insight (watching Roland, studying the tome, attending), and pushes one long talk that gives him the ring (first tries usually fail, training Convincing), then practises and crafts the gem, and walks home through the Smoky Mirror: the first run that ends by walking out, which wakes her five stats at zero. After that she steps back in at the Smoky Mirror each run (the exit stays offered while she holds the gem); stats now earn XP, a walk out settles all of a run's stat XP into mastery and a collapse or ending the run settles half. The Hub, meta currency and Act II are not built.

## Systems
| System | Status | How it works now | GDD § | Differs from GDD? |
| --- | --- | --- | --- | --- |
| Vitality and drain | Built | 50 max; drains 1 a second, growth 40% a minute (compounding). Composure (once awake) slows the growth; lit candles hold the darkness off. Some tasks add their own extra drain (Feed your hours +2 a second). Restoratives give vitality back over a few seconds; one starts when what she's missing plus what the steady drain will take meanwhile covers it (one of each kind at a time). | 5 | Drain by time, not by action (2026-09-25). |
| Endurance bank | Built (placeholder numbers) | When any run ends, 3% of the vitality lost (+1% per Endurance level) is banked as kept max vitality. Off while Endurance is asleep, so nothing is banked in Act I. | 6 | Yes: replaces "+max vitality per level" (2026-09-30). |
| Pools (pathos) | Partial | Seven hue pools exist and are open and full from run 1, but action costs and carry costs are switched off, and the hard-charged tasks (trips, searches) have zero pool cost. Pools do nothing yet. The gem's capacity is the pools' maximums added up. | 5 | Yes: GDD makes pathos central and has the pools taken in the prologue (not built). |
| Stats | Built | Five: Endurance (bank, restorative waste), Perception (reveals hidden finds; nothing hidden is placed yet), Scholarship (study yield, gates), Attunement (restoratives give +2% per level), Composure (slows drain growth, extra drain, carry cost). All five start asleep: no effect and no XP until the first walk out wakes them at zero. Strength is the run's level plus mastery. A stat's mastery is held during a run and settled when it ends: all of the run's stat XP on a walk out, half (`collapseStatXpShare`) on a collapse or End run. | 6 | Mostly no: Act I has no stats, as designed (2026-09-30, 2026-10-01). Perception and Attunement jobs differ (2026-09-30). |
| Skills | Built | Seven: Wayfinding, Gathering, Studying, Invoking, Instantiate, Crafting, Convincing. Each makes its tasks ×1.05 faster per level; a stat's learning bonus speeds the run level, never mastery. Crafting (the gem's gate) learns faster since Act I has no stat for it. | 6 | GDD names five skills; two added by the lab. |
| Mastery | Built | Kept XP per stat and skill; ×1.01 task speed per skill mastery level; no cap. Skill mastery rises as it is earned; stat mastery settles at a run's end. | 4, 6 | No. |
| Action queue | Built | Play (now) and Schedule (anywhere, the walk is added); repeats; drag to reorder; supply-blocked entries queue their supplier; a task that cannot start is dropped; the refusal names a missing stat or skill level before a missing item (runs to earn it, against a trip), and Carry is not offered for containers. | 13 | Queue empties each run; known-by-heart rooms carry over. |
| Planning screen | Built | After the Summary: Repeat (start at once) or Plan (map plus queue, then Begin); before planning is earned, one "Go back in" button with a line in Clara's voice. Planning is earned by Feed your hours and covers only rooms known by heart. Warnings (⚠ with a reason) mark plan entries that will be refused. | 13a | Planning gate is new (2026-09-30). |
| Travel and places | Built | Six rooms; ways can be hidden or shut until a condition is met; Travel costs vitality that rises with every trip in a run (3, ×1.08). Task time is family × 15 s × 1.1 per room depth. | 12, 4 | Travel charge is new (2026-09-30). |
| Searching | Built | Each room is searched once, ever; the bar is kept even partway; finds appear at set percentages. A switch can reopen a room's search at 0% (the lab, once); a reopened round pays XP and gives again. | 9 | Explore and Search are one verb. |
| Room speed / by heart | Built | A room worked in 4 runs is "known by heart" (×1.8), rising linearly to ×5 at 8 runs. Speed is unlocked by Feed your hours; by-heart rooms glow gold. Speed holds when attention is needed. | 4, 13a | Yes: linear ramp, not a power law (plan 025). |
| Pockets, containers | Built | 5 pockets; the satchel adds 7. The pouch of phials holds 10 phials and the earrings hold 10 dense wisps, outside the pockets. Pushing something in can push the most-held kind out. | 5, 11 | Placeholder rules; see open questions. |
| Floor | Built | 10 spaces per room; put down and pick up; one-of-a-kind things never stay missing (the most-floored item is lost if all is full). | 11 | Placeholder rules. |
| Carried items and the ring | Built | The ring is hard-charged: it takes 1 vitality a second of action while carried, even with costs off (Composure softens it). Walking out keeps what she carries, packed for next run; other endings lose it. She hands the ring to Roland's memory in the lab, after which it is gone. | 5, 11, 12a | Yes: the GDD note says the ring is switched off; it is on. |
| Switches and milestones | Built | Sixteen switches fire story passages, unlock tasks, wake all five stats (the first walk out), reopen the lab's search. Entering a room is a milestone card comparing times to earlier runs. | 11 | No. |
| The memory lab | Built | First search: the earrings and dense wisps (15%), Watch him, Study the tome, Attend (each 1 kept insight, once a run, cap 6), Draw on the mana stone, and Talk to Roland: one long push (~160 s, ×0.85 per insight) needing only the ring; a run that ends mid-talk loses it but keeps the Convincing XP. The talk gives him the ring and reopens the search: practise the cut and craft the gem at 25%. Craft the gem needs Crafting 8; practice charges vitality that grows each go. | 12, 12a | Yes: skill gates (2026-09-30); the talk as a long push with insight, earrings first (2026-10-02, plan 055). |
| Run endings | Partial | Exhausted and ended-by-player work; walking out works through Exit through the glowing mirror (needs the gem, at the Smoky Mirror). The Summary lists what was carried out. Walking out grants nothing beyond that yet. | 12 | Yes: GDD ties walking out to the Hub and currency (not built). |
| Saving | Built | Three slots, autosave; a run in progress resumes exactly; the last Summary is saved. Format version 23. | 16 | No. |
| Story feed | Partial | 26 passages, all written except passage 13's last line and the unreachable passage 12; a feed of ambient lines from 21 buckets with cooldowns, priorities and chances; milestone cards stay for the run. | 3 | Placeholder text. |
| Dev tools | Built | A dev panel with Jump to (before the talk, after it, earrings found, before the gem, gem in hand) and run counts. Balance probe and what-ifs for balance passes (test-side, `docs/balance-tools.md`). | — | Not in the GDD. |
| Meta currency | Not started | Nothing earns, spends or shows it. | 4 | Moved to the end of Act II (2026-09-30). |
| Hub, realms, standing orders | Not started | Walking home ends the build. No Hub, no Act II realm, no standing orders. | 4, 12, 13a | Not built. |

## Current numbers
| Name | Value | What it does |
| --- | --- | --- |
| Vitality max / drain | 50 / 1 per s | Starting bar and base drain |
| Drain growth | 40% a minute | Compounds all run |
| Standard trip / room step | 15 s / ×1.1 per depth | Base time of tasks |
| Skill speed per level / mastery | ×1.05 / ×1.01 | Faster tasks |
| XP curve | first level 10 (mastery 20), ×1.085 a level; 1.5 XP per second of task | Levelling |
| Share of stat XP kept on a collapse or End run | 0.5 (placeholder) | Stat mastery settled at a run's end; a walk out keeps all |
| Pockets / satchel / floor / earrings | 5 / +7 / 10 per room / 10 dense wisps | Carrying |
| By heart / full speed / speed cap | 4 runs / 8 runs / ×5 | Room speed |
| Travel charge | 3 vitality, ×1.08 per trip this run | Escalating cost; playtest says it may be steep |
| Roland's ring | 1 vitality per second of action | Cost of carrying it |
| Endurance bank | 3% of loss, +1% per level; waste allowed 0.5 per level | Kept max vitality |
| Composure (all stats asleep in Act I) | growth ×0.97, extra drain ×0.97, carry cost −10% per level | Softens drains |
| Attunement | restoratives +2% per level; hue costs −2% (dormant) | Economy |
| Scholarship | +1 per study every 5 levels | Study yield |
| Wisp / phial of memory / dense wisp | 10 in 2.5 s / 20 in 3.5 s / 40 in 6 s | Restoratives |
| Feed your hours | 40 s, +2 drain a second | Unlocks room speed and planning |
| Talk to Roland | ~160 s, ×0.85 per insight (cap 6) | The lab's grind: first tries fail |
| Lab gem | Crafting 8; 60 s; practice 4 s, charge 2 ×1.15 | The Act I finish |
| Pools | Amber 150, Citrine 100, Emerald 25, Sapphire 75, Iolite 100, Amethyst 90, Ruby 75 | Inert while costs are off |
| Action / carry costs | Off | Pools inert |

## Content in the build
- **Places (6):** The Smoky Mirror (start), A Dark Hall, The Left Corridor, The Right Corridor, The Dark Corridor with Hanging Mirrors, The Mirror's Laboratory.
- **Verbs (4):** Travel, Search, Pick up, Put down.
- **Tasks (30 assets):** Common 4; Prologue 1 (chase Roland); Hall 13 (candles, flint and steel, pouch of phials, satchel, gather a wisp, fill a phial of memory, Feed your hours, two corridor searches, take the ring, exit through the glowing mirror); Lab 12 (clear the bench, take and study the tome, draw on the mana stone, cut the stone, watch him, attend, talk to Roland, take the earrings, draw a dense wisp, practise the cut, craft the gem).
- **Items (22):** restoratives (wisp, phial of memory, dense wisp), containers (pouch of phials, satchel, earrings), tools (flint and steel, candles), the ring, the tome, the gem, and counts or states (candlelight, mirrors found, understanding, bench cleared, quickened hours, steady hands, focus, what he did that night, mana stone's warmth).
- **Switches (16)** and **cost families (12).** Seven skills.
- **Story:** 26 passages, all written (2026-10-01) except passage 13's last line (waits on the mana stone's redesign) and passage 12 (*The Stone Is Cut*, which can no longer fire).
- **Blurbs:** 21 ambient buckets, imported from text files, with starting rules.
- **Dev Jump stages:** 5.

## Where the build differs from the GDD
1. Act I has no stats: all five sleep until the first walk out (2026-09-30, 2026-10-01); the GDD v0.5 predates this and its §6 stat table reads as if they exist from the start.
2. Mastery has no cap, as §4 says (built 2026-10-01); a stat's mastery settles at a run's end rather than live.
3. Endurance banks vitality after each run; GDD §6's "+vitality per level" is gone (2026-09-30).
4. Perception reveals hidden finds rather than speeding search (§6); nothing hidden exists in Act I and the earrings are ungated (2026-10-01).
5. Attunement boosts restoratives instead of cutting hue costs (§5, §6; 2026-09-30).
6. Room speed rises linearly, not by power law (§4; plan 025).
7. Pathos pools exist but nothing costs them; §5 treats them as core. Not built.
8. The ring takes vitality, and the GDD's §12a note that the build has it switched off is wrong: only pool costs are off. It is also given away in the lab, not reused (§12a OPEN).
9. Walking out arrives at the end of Act I and currency at the end of Act II (§12, §4; 2026-09-30); neither the Hub nor currency is built.
10. Travel has an escalating charge, and practice has one too (2026-09-30, plans 030b, 027c).
11. The lab uses skill gates (Crafting 8), not stat gates (§12; 2026-09-30).
12. Game time was halved, not doubled (2026-09-30).
13. The exit is the Smoky Mirror (the start), not a separate lab exit (2026-09-30). The gem does not yet give the empty Amber pool.
14. Planning between runs is earned and limited to known-by-heart rooms (§13a; 2026-09-30).

## Open questions raised while building
- **Push-out with full pockets:** the most-held kind goes, carried items included; which item is lost when the floor is also full is unchosen. `Simulation.Floor.cs`, `Simulation.Resources.cs`.
- **Hidden finds shown or not:** deferred to Act II (2026-10-01); lean is to show it.
- **Stat starting amounts at first exit:** stats arrive at zero (the user, 2026-10-01); a one-off starting amount stays [PROPOSED].
- **Lab numbers:** earrings and dense wisps at 15% of the first search, the gem's tasks at 25% of the second; the talk ~160 s, ×0.85 per insight, cap 6; Crafting 8, practice charge 2 ×1.15, gem time 60 s. Tuned 2026-10-02 with the balance probe (tidy play walks out on run 9, loose on 15). The lab's task assets.
- **The Summary's "Go back in"** wording and its centred button (plan 055), placeholders. `results.go_back_*` in the text file.
- **Travel charge (3, ×1.08)** and room step 1.1: placeholders; the 2026-10-02 pass left them (they barely change the run count).
- **Share of stat XP kept on a collapse or End run:** 0.5, a placeholder (`LoopSettings.collapseStatXpShare`, Balance Sheet Rules). Act I's balance without the bank and Composure needs a playtest.
- **Summary details:** action count, strike-through of left-behind items, slice order. `RunResultsPanel`, `RunReport`.
- **Summary lines for escalating charges:** the run report now counts each charged task's goes and vitality (`RunReport.Charges`), but the Summary only shows trips (Moves); showing the rest is waiting on the UI lane.
- **Planning and by-heart looks:** glow, candle light, speed-line wording; the shut Dark Corridor message. `UiStyle`, `MapStyle`.
- **Blurb topics for Cut the Stone and Fill a Phial** left to the writer.
- **Gem capacity** as the sum of all pools' maximums. `Simulation.GemCapacity`.
- **Blurb starting rules** (priorities, chances, loop ranges). Blurb assets.

## Recent changes
Finished features are listed in `docs/CHANGELOG.md`.
