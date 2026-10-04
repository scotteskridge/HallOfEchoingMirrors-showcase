# Hall of Echoing Mirrors — Future Sketches

Design sketches for Act II and beyond. Not read during greybox. Nothing here is built, and most of it will change once pools exist and the first realm has been played.

Moved verbatim from `GDD.md` v0.5. Section numbers match the GDD, where a stub marks each moved section. The Act II summary that was here is now `act-II-design.md`.

## Act II: the Amber realm

Moved to `act-II-design.md` (2026-09-30), the single reference for Act II.

## 7. Build Archetypes

Four shapes a run can take. **They only stay distinct if the content contains walls matching each.** A build system does not create variety on its own; the walls do.

| Archetype | Pool shape | Route it implies | What it buys | Costs |
| --- | --- | --- | --- | --- |
| **Spike** | One hue enormous, others near zero | One traversal, one realm | Raw magnitude — a threshold nothing else reaches | Six pools near empty on every traversal; cannot tour |
| **Balanced** | All seven even | Multi-stop tour | Cheap travel, many realms per run | No threshold high enough for a magnitude wall |
| **Burst** | Any shape, paid in vitality | Short and direct | A deep reach very early | Collapse partway through |
| **Lopsided** | Two or three high, rest at a floor | Two or three realms | Practical middle ground | Master of nothing |

**Burst is enabled by Maroon** (§5): certain workings can be paid in vitality rather than pathos, which is the Codex's sustaining-gift. Without a mechanic like this, "overspend early" is just a worse balanced run.

### &#91;OPEN\] The missing control surface

Every archetype above is a statement about how pathos is distributed across pools, and nothing in the design yet says how the player *sets* that distribution.

**Deferred deliberately.** This is a UI and economy question, and playtesting has not reached the point where meta currency exists, so there is nothing to decide against yet. Three candidates, kept on the shelf:

- **Starting capacity per hue.** Meta currency buys per-hue pool capacity, distributed in the Hub each run. Most legible, and matches §4's claim that reallocation is the core verb of the Hub.
- **Gather priority.** Pools start near empty and the shape emerges from what the queue chooses to gather. Makes pool shape a routing consequence rather than a menu decision — which is what §13a keeps asking for elsewhere.
- **Anchor facets.** The seven-facet gem becomes the control surface; slot gems to bias capacity.

The first two are not exclusive.

### The tuning rule

Each archetype should be **best at one wall type and merely adequate at the rest** — never useless. A player punished for a legitimate build choice stops experimenting. Rough target: the right build clears a wall in about 3 runs, the wrong build in 12 or not at all.

### The dominant strategy to watch

**Balanced will quietly win unless magnitude and race walls are common.** In almost any system the longest run is worth the most, so the design has to actively pay the player for choosing short. Rough target: about a third of currency-bearing milestones should be unreachable by a balanced build.

## 8. Content Vocabulary: Walls

Walls gate progress and force rebuilds. Each answers *what kind of mage is Clara right now?* Every wall authored should be tagged with a type, so the content can be audited for variety.

Almost none exist in the build yet. The one that does: the door into The Mirror's Laboratory opens only for someone carrying Roland's ring, an item wall.

| Type | Requirement | Which build passes |
| --- | --- | --- |
| **Magnitude** | One hue at or above a threshold | Spike |
| **Breadth** | Four or more hues above a floor *simultaneously* | Balanced only |
| **Race** | Reached before tick N, or before the reflection | Burst |
| **Endurance** | Deep enough that only a long build arrives with anything left | Balanced / long |
| **Sustained** | Continuous drain over many ticks with no pool bottoming out | Tests pool shape, not peak |
| **Attrition** | Passable at any level, but cost scales — a weak build passes with nothing left | Any, at a price |
| **Precision** | A hue inside a *band*; too much fails as surely as too little | Careful tuning |
| **Dual-hue** | Two specific hues together | Forces cross-realm travel |
| **Stat wall** | Non-magical threshold: Scholarship 40 or the text stays gibberish | Keeps the five stats relevant |
| **Item wall** | Needs a tool, not a number | Lets a weak build pass where a strong one cannot |
| **Knowledge wall** | Needs a fact learned elsewhere | Free to implement; makes the *player* smarter |

**Precision and Breadth are the signature types.** Neither exists in the reference games, and both are only possible because of seven pools with an empty-pool penalty. If two wall types are built well, build these.

**The trap:** stat walls and knowledge walls are the cheapest to author, so they multiply quietly, and neither exercises the pool system at all. If most gates read "Scholarship 40," the result is *Idle Loops* with extra steps.

**\[OPEN\] A target distribution is premature.** Setting quotas before any wall has been built and played would be authoring against a spreadsheet. Revisit once the first realm exists and it is clear which types are actually fun to hit.

## 10. Content Vocabulary: Challenges

Walls test the build. **Challenges test judgement — this is where agency actually lives.** They should be rare as individual instances but frequent as a category: every run should contain two or three.

| Challenge | The decision |
| --- | --- |
| **Fork** | Two routes, one resource pool. Both viable, only one this run |
| **Bargain** | The hall offers power now for Shadow later |
| **Sacrifice** | Spend vitality instead of pathos. The Burst enabler |
| **Gamble** | An unknown cost behind a door. Rewards knowledge from prior runs |
| **Ratchet** | A one-way change to the world. Drink the fountain now or save it for a deeper run |
| **Pursuit** | The reflection is closing. Push on or retreat |
| **Interrupt** | A timed decision that pauses the run. Which hue, right now? |
| **Trade-off pair** | Two switches, one run; flipping either closes the other until next loop |

**Ratchet is the sharpest of these,** because it is the only one with a *negative* persistent consequence. A fountain that gives a large one-time pool and is then spent forever creates a real decision: take it now to push deeper this run, or leave it for a run that can go further? Use one or two per realm, no more.

**[RETIRED 2026-09-30]** The permanent Ratchet is retired; run-long choices and an escalating harvest replace it. See `act-II-design.md` §7 and the decisions log.

Act I already contains the shape of a Fork without calling it one: the left and right corridors, where early runs can afford only one.

### The test every challenge must pass

1. **It is a trade-off.** Picking one thing costs another.
2. **It is informed.** The player can reason about it, not guess blindly.
3. **It has consequences.** The result shows up later, not just in a number.
4. **No option dominates.** If one choice is always best, it is not a choice.

Test 4 is the one *Idle Loops* fails, and the one most easily failed here.

## 12. World Structure & the Lock-and-Key Graph (moved parts)

### From Zones

**\[PROPOSED\]** Realms 1–3 are Clara's memories, 4–6 Roland's, 7 shared.

**\[OPEN\] The realm sketches are stale.** They were written against an old colour order with Red first and Violet last. The gem order now runs Amber → Ruby, so Ruby is the final realm and the burned Evercrest that was Realm 1 is now Realm 7. Rewrite before any realm is authored.

### Memory nodes

Memories are not recordings. The hall has twisted them: stopped, repeating, decaying or falsified. Four verb classes inside a memory:

- **Against the room:** the hue's effect on matter (Amber = bind, unstick, separate).
- **Against reflections:** Listen, Console, Ask, Refuse, Take — the Codex's three transfer modes.
- **Against the memory's accuracy:** correct what the hall has altered. Requires Clara to have been there.
- **Against herself:** gather honestly (more yield, costs more) or hold back.

Reflections can see her, but mistake her for who she was then. Roland-in-the-memory sees his student, not the woman who came back, and she must play the part she had then to get what she needs.

**\[PROPOSED\] A memory only perceives her if she was in it.** Roland's memories cannot see her, so she can only watch and gather — a different verb set, not just different flavour.

**\[DIRECTION\] Roughly a third of nodes should not be memories.** Hall nodes (junctions, dead ends, doors, no content) make routing legible; constructed nodes are the hall's own architecture.

### The switch graph

Modelled in the companion workbook. Current shape: **63 switches** across Hub, Hall and seven realms, with **97 dependency edges, 39 of them cross-realm** — the key for a switch is generally not in the same realm as the switch, which is what forces the back-and-forth. Only majors pay currency. Early game opens realms in wheel order with Hub and Hall always available; late game opens six or seven options at once.

Reachability is verified: zero steps with fewer than two open options before the finale funnel. Pacing at current defaults is \~289 runs and \~39.7 hours, a reward every \~4.6 runs — which should be read against §3's open question on target length.

**The workbook's real job** is not the specific 63 switches, which are placeholders. It is the reachability check: any change to requirements should be re-verified against the rule that two to five options stay open.
