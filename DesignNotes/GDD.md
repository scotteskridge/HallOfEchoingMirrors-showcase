# Hall of Echoing Mirrors — Game Design Document v0.5

> Design talk: use `/design` in Claude Code; read only the § at hand (grep the headings), never this whole file. The pillars every section serves are in `VISION.md`.

Sep 27, 2026 · @Scott

## About this document

This revision of GDD v0.4 was rebuilt section by section against the build and against Scott's corrections in the design session of 27 Sept 2026, then checked against the code (decisions log 2026-09-27, *GDD v0.5 draft checked against the build*). It supersedes v0.4.

**Nothing here is decided.** The project is in greybox. Every system is a hypothesis to be proved or discarded in playtesting, including the ones marked BUILT. Where this document and play disagree, play wins.

### Two labels, not one

v0.4 used a single set of labels for *build state*, which left the reader unable to tell a number that had been tested from a number that was invented on a Tuesday. This revision separates the two axes.

**Build state** — where an idea stands against the code:

| Label | Means |
| --- | --- |
| **\[BUILT\]** | The build does this as written |
| **\[BUILT — differs\]** | The build does this differently; `docs/BUILD-STATE.md` says how |
| **\[DIRECTION\]** | Where the design is heading; not built |
| **\[PROPOSED\]** | Recommended, not adopted |
| **\[OPEN\]** | Unresolved; needs a decision |

**Confidence in a number** — a tilde marks any figure that is a guess awaiting playtest data. `~0.5/s` is a placeholder. `0.5/s` has been played and held. At a glance, a table of five numbers then shows which two are load-bearing and which three are scaffolding.

Never use "DECIDED". Nothing is promoted to \[DIRECTION\] or \[BUILT\] without Scott saying so in conversation.

### Order of precedence

- **What the game is for:** `VISION.md` (the pillars). Only Scott changes it.
- **What the design intends:** this document. It is kept current: when Scott settles a point in a design session, the § is edited in that session (commit messages start `GDD:`).
- **What the build does:** the code and `docs/plans/`; `docs/BUILD-STATE.md` summarises them in plain English, and can drift (it once made "strength" look like a sixth stat), so re-sync it with `/sync-state` when it does.
- **Act II:** `act-II-design.md` wins where it and this document disagree.
- **What has been finished:** `docs/CHANGELOG.md`.
- **Why something was decided:** `decisions-log.md`. It is history, not a layer of corrections: if it disagrees with this document, a GDD edit was missed, so flag it.

## 1. Status

Act I is playable in greybox from the first run to the walk home through the starting mirror (plans 027a–d). What the build does, system by system, is in `docs/BUILD-STATE.md`; finished features are in `docs/CHANGELOG.md`. Not built: pathos as a working resource, meta currency, the Hub, realms, standing orders and realm mastery.

## 2. The Differentiation Problem

The genre skeleton — a clock runs out, you reset, some progress persists — is shared and not worth avoiding. What separates games inside it is narrower: **the clock, the planning language, and what the player is choosing between.** *Increlution* is not a reskin of *Idle Loops* because it swapped mana for a lifespan and a sequential list for parallel priorities. *Stuck in Time* moved the plan onto a map.

**Honest assessment:** this design sits roughly 40% away from *Idle Loops*. A player who finished that game will recognise the engine within five minutes and should find the differences by hour three. That is a well-differentiated entry in a small genre, not a reinvention of it, and that is an acceptable target.

**The five things carrying the differentiation, in order of strength:**

1. **No grind path to victory.** Milestone-gated currency means a patient player with a bad build does not eventually win. Skill means allocation and routing, not hours.
2. **Pool balance as the clock.** Seven pools with an empty-pool penalty makes the *shape* of a build matter, not just its total. This is what makes Breadth and Precision walls possible, and neither exists in the reference games.
3. **A hall that remembers.** Persistent world switches mean the player is building the world across runs, not only the character.
4. **Recovery, not acquisition.** The seven pools are taken from her on screen and won back one at a time. The same unlock ladder every game in the genre has, carrying a plot.
5. **Prose.** The genre's writing is uniformly functional. A working novelist writing the journals and realms is an asset competitors cannot easily copy.

**\[OPEN\] The rival reflection.** A reflection that replays the previous loop's route and beats Clara to things. It is the single most original mechanic discussed and is currently integrated with nothing. It pairs naturally with a no-grind economy, because it punishes running the same script twice. Currently tabled. Leaving it out is a defensible scope decision; leaving it undecided is not.

## 3. Project Overview & Premise

A loop-based incremental game in Unity, played actively rather than idly, in which Clara re-enters the Hall of Echoing Mirrors to rescue Roland and is emptied a little more each time.

- **\[BUILT\]** Inspired by *Idle Loops*, *Increlution* and *Stuck in Time*; fun at default speed, with no speed hacks needed (`VISION.md`, "Not idle").
- **\[DIRECTION\]** Puzzle and discovery driven rather than combat driven, but resolved through power thresholds and build checks, not adventure-game verb puzzles.
- **\[BUILT\]** Clara is a practised eromancer, not a novice — twenty-one years past her training, at the height of her craft, stripped of it by the hall. The player learns alongside her because *she* has lost the vocabulary, not because she never had it.
- **\[DIRECTION\]** Setting is 1928 Evercrest from *Colours of Longing* (formerly *Whispers of Want*), twenty-one years after the events of the book. The fae built the portal centuries ago; the hall holds reflections of all possible worlds.
- **\[BUILT\]** A run ends when Clara's vitality runs out. This replaces the older idea that an emptying anchor gem pulls her back. Gems that store pathos now belong to the meta currency (§4). **\[OPEN\]** Exactly what role the gem plays in the meta currency.
- **\[BUILT\]** Narrative is first person, present tense, in Clara's voice (delivery: `docs/BUILD-STATE.md`, "Story feed"). Roland's journal scraps are \[DIRECTION\].
- **\[DIRECTION\]** Roland's core wound is guilt over teaching others, and the harm they have done by harvesting eros.
- **\[DIRECTION\]** Tone: longing and devotion. Romance implied, not explicit. Lust is not a mechanic.
- **\[OPEN\]** Target length. `CLAUDE.md` targets 15–25 tight hours; v0.4 targeted 40–60 with New Game+. The intent is a **longer experience if and only if the features earn it** — length is an output of how much genuinely fun content exists, never a target to pad toward. Hold the number lightly and revisit after the vertical slice.
- **\[OPEN\]** Platform. Recommendation: PC first, with a free web demo. End-game screen promotes the book and Patreon.

## 4. The Loop Economy

The spine of the design: three layers, each doing a job the others cannot. v0.4 had four — the mastery cap layer is to be removed (decisions log 2026-09-27); meta currency buys mastery levels instead.

| Layer | What it does | Earned by | Resets? |
| --- | --- | --- | --- |
| **Stat / skill level** | How fast Clara acts this run | Doing actions | Yes |
| **Mastery** | A head start every run: skills start faster, stats start stronger | Doing actions (rising XP per level) and meta currency (rising price per level) | No |
| **Meta currency** | Changes what a run *looks like* | First-time milestones only | No |

**\[BUILT\] Mastery makes runs faster, and speed is what buys depth.** Repetition buys efficiency, never reach directly: a faster early game leaves more of the run for the actions at its far end. (How stat mastery behaves in the build: `docs/BUILD-STATE.md`.)

**\[BUILT\] Mastery has no cap** (plan 041; a stat's mastery settles when a run ends, a skill's rises as it is earned). The brakes are the rising XP requirement and the rising price in meta currency, not a ceiling. A cap is a wall with no number on it; a rising price is visible and can be reasoned about. XP cost growth stays steep on purpose (decisions log 2026-09-27): progress slows, and meta currency is how the player pushes past it. What that means for deep content is §6's open question.

**\[DIRECTION\] Milestones pay once.** A switch flipped for the first time awards currency; flipping it again awards nothing. There is no grind path to victory.

**\[DIRECTION\] Respec is free; the total is finite.** The player may redistribute everything between runs at no cost but can never exceed the total earned. Reallocation becomes the core verb of the Hub, and no purchase is ever a mistake the player is stuck with.

### The intended rhythm

1. Grind a little to raise mastery and shorten the chains she already knows.
2. Hit a benchmark the current build can reach.
3. Spend the currency on something that changes how a run plays.
4. Rebuild, and go after a wall the old build could not touch.

**The condition that makes this work:** benchmarks must be capability checks, not time checks. If a patient player can grind past a wall with the wrong build, step 3 becomes optional and the economy unravels.

**The condition that keeps it fair:** at any moment there must be two to five reachable walls of at least two different types. One is a forced corridor; zero is a dead end with no way to earn. This is what the lock-and-key workbook exists to verify.

### How meta currency is earned

**\[DIRECTION\] The gem is a physical object she carries home.** Flipping a milestone crystallises pathos into a gem, which goes into her inventory and takes a pocket. Carried out and banked, it becomes currency. On collapse she drops it at the node where she fell.

Four things fall out of that one rule:

- **Walking out becomes the decision it is meant to be.** Three milestones deep, three gems in hand, the drain accelerating — push for a fourth or bank what you have.
- **Deep runs are priced honestly.** Gems in pockets are pockets not holding wisps, so a successful run gets progressively harder to survive. The reward is its own cost. At \~5 starting pockets a gem is a fifth of her capacity, which is steep; this is the first thing to tune once currency exists.
- **Nothing is permanently lost.** "Milestones pay once" plus "collapse forfeits currency" would let a player permanently lose an unrepeatable payout. A gem dropped where she fell means she loses the *trip*, not the reward.
- **It reuses what exists.** A dropped gem is a floor pile on that node, already drawn on the map and already retrievable.

### Harvesting is an action with an escalating cost

**\[DIRECTION\] Harvesting a gem is a high-cost action requiring a stat check**, often a build-specific one, and it demands a route back out. Both a puzzle and a capability test.

**\[DIRECTION\] Repeat harvests of the same milestone cost +\~25% each, compounding.** Technically grindable, in practice soft-capped, because cost grows exponentially while capability grows roughly logarithmically with play.

| Harvest | at +25% | at +50% |
| --- | --- | --- |
| 1 | 1.0× | 1.0× |
| 3 | 1.6× | 2.3× |
| 5 | 2.4× | 5.1× |
| 8 | 4.8× | 17× |
| 10 | 7.5× | 38× |

Use 25%, not 50%. At 25% a milestone pays four to six times across a playthrough before it stops being worth it; at 50% it pays two or three, which teaches "never repeat" and the player stops coming back.

**\[DIRECTION\] The escalation decays with runs since last harvest** — roughly one cost step back every \~5 runs. Without this, compounding fights the goal: cost only rises, capability growth slows, and every milestone is dead content by hour four. With it, a realm untouched for twenty runs has partly recovered, and checking in on old realms becomes literally correct rather than aspirational. Thematically exact: the hall is a reservoir, and a place she drained reaccumulates feeling.

**The test that keeps it honest: farming must be possible but always worse than advancing.** Impossible, and a stuck player has no way out. Equally good, and players will farm, because farming is safe and frontiers are not.

**\[DIRECTION\] Show the next cost, not only the current one** — *Harvest: 156 (next: 195)*. That turns a diminishing return into a decision. Hidden escalation reads as the game arbitrarily punishing repetition.

The build-specific stat check is only safe because respec is free. Otherwise a player who respecs to afford a harvest could lose the build that got them to it.

### Rejected earning models

All four pay for **effort** rather than **achievement**, which is what the milestone rule exists to prevent.

| Model | Why not |
| --- | --- |
| Max run length | Rewards surviving while doing nothing useful |
| Sum of stats earned in a run | Grindable, and rewards many cheap actions |
| Doing high-cost actions | Grindable too, just slower |
| Paying for personal bests | A slow infinite source, and infinite sources are what this economy exists to avoid |

**\[PROPOSED\] Two secondary sources.** A small trickle for first sight of a node, memory or reflection, which rewards exploring over beelining and is naturally finite. And refusing the dark pole — completing something the light way when the fast way was available — which makes the moral axis pay in something other than endings.

### What meta currency buys

With the mastery cap layer gone, currency's old sink (raising the cap) is gone with it; it buys mastery levels directly instead. **\[OPEN\] What the largest sink now is.** The test for any lever: does it change what a run *looks like*, or only what its numbers are?

| Lever | Status | Notes |
| --- | --- | --- |
| **Mastery levels** | \[DIRECTION\] | A rising price per level: the soft cap (decisions log 2026-09-27) |
| **Per-hue pool capacity** | \[PROPOSED\] — the likeliest largest sink | The archetype control surface. Without it, Spike, Balanced and Lopsided are descriptions of outcomes, not builds. Hue-tag it so pushing Amber deep funds the Citrine needed next |
| **Carry capacity** | \[DIRECTION\] | Now shares the job with in-run containers (§5). Whole steps, off the level curve entirely |
| **Starting position** | \[PROPOSED\] | Begin at the mirror lab rather than the entrance. Saves a traversal *every run forever*; the most felt purchase available |
| **Standing-order slots** | \[PROPOSED\] | Stages automation, and thematically it is how much she holds in mind |
| **Faster learning** | \[PROPOSED\] | Rooms need fewer repeats. Attacks the fact that re-searching is most of an act's content |
| **Starting level vs mastery** | \[PROPOSED\] | Levels reset, so a head start is worth much more in a short run than a long one, while mastery is worth the same in any run. Front-loaded versus sustained |
| **Foresight** | \[PROPOSED\] | See a wall's requirement before attempting it. Makes information a resource |
| **A flat drain reduction** | \[PROPOSED\] | Boring, always useful, never wrong. The safe default, so a new player is not paralysed by a menu of situational options |
| **Game speed** | Not sold — see below | Charging for relief from designed-in tedium forces "stronger, or less bored", which is a miserable choice |

**\[OPEN\] Possible redundancy.** Endurance/Composure already splits short-run against long-run (§6). If starting-level-versus-mastery does too, a "short build" is Endurance plus levels every time, and there are two builds rather than many.

### Game speed is earned, in tiers, and never bought

**\[BUILT\] The speed control is earned, and it unlocks room speed, not a global multiplier.** *Feed your hours to the flames* (once ever) is deliberately costly and not offered on the first run: the player must have felt the pace before being handed a lever for it. The puzzle grants a capability, not a number; the multiplier itself comes from play, per room (§13a). Costs: plan 029, decisions log 2026-09-30.

This is the difference between a speed slider and an adaptive system. A global slider means the late game is mostly watching, and the player manages a control that exists because the game is boring. Realm mastery points the speed-up exactly where attention is not needed — the corridor she has crossed thirty times — and leaves the frontier at 1×.

## 5. Pathos Pools & the Vitality Clock

**\[BUILT\] Vitality drains with time, not with actions.** The *Increlution* model, and the one in the build. Every skill that speeds an action is therefore directly buying vitality, which is what makes mastery the economy rather than a convenience.

|  |  |
| --- | --- |
| **Max vitality** | \~50, +1 per Endurance level |
| **Base drain** | \~0.5 per second |
| **Growth** | \~+25% per minute, compounding |
| **Unmitigated run length** | \~85 seconds |
| **Target run length** | \~3–7 minutes, rising gradually |
| **Endurance** | Raises max vitality — a flat buffer |
| **Composure** | Slows the growth rate — a flatter curve |

Every number above is a placeholder awaiting playtest. The gap between 85 seconds unassisted and a 3–7 minute target is the space the candles and the wisp economy are meant to fill; whether they fill it, or whether max vitality is simply too low, is a playtest question and not a design one.

### Growth rate is the master dial

Because growth compounds, **run length is logarithmic in resources.** Hoarding cannot buy a long run; only a faster gather rate and a flatter growth curve can. Runs inch up rather than exploding once the player finds a good item.

It also means the growth percentage, not item strength, is the lever for act pacing. **\[PROPOSED\]** Make growth a per-act value rather than a constant, so runs lengthen across the game by flattening the curve rather than by piling on stronger items.

### The sustainability crossover

A restoration item sustains her only while its restore rate exceeds the current drain. A wisp restoring 10 over 5 s gives 2/s; at 2 s to gather that is the ceiling, and the drain passes it at about six minutes.

**The ceiling is not fixed — mastery raises it continuously.** The wisp economy does not die at a set drain level; it dies wherever drain exceeds *current* gather rate.

Each crossover is a point where the shape of a run changes: first she gathers to survive, then she gathers to buy time to build drain reducers, then she builds reducers to buy time to reach things. **Place these transitions deliberately.** Each is a moment the player must notice and adapt, and they are the best available defence against the pattern going stale.

### The seven pools are taken from her in the prologue

**\[DIRECTION\] Clara enters the hall with all seven pools full and leaves the prologue with none.** She is a practised eromancer; the hall's reflection takes the anchor gem off her as she collapses. Acts I and most of II are played with vitality alone, and the seven pools are recovered one at a time, in wheel order, as the game's central progression.

**Why this is load-bearing rather than flavour:**

- It turns the whole progression into a **recovery** rather than an acquisition. "You unlock Amber in realm 1" is generic; "you get back the first of the seven things that were taken off your neck" is a plot, for the same code.
- It gives the antagonist a crime the player watches happen, so nothing has to be explained.
- It is Koster's scarcity rule exactly: choices come from scarcity, and scarcity needs the player to know what they are short of. Seven full bars shown and then removed teaches what the game is about before the player has any vocabulary for it.
- The prose already assumes it. *Through the Glass* has "All seven facets are lit at once," and the entire `reaching` blurb bucket — a woman's hand closing on nothing out of habit — is dead text without the mechanic.

**The build currently has all seven unlocked and full every run, doing nothing but pay for travel.** That is scaffolding for the gem bar, not the design.

**\[OPEN\] The prologue is \~3 seconds long, and the brief asks for \~90.** The player cannot see seven bars drain in three seconds, which is the one thing the prologue exists to do. (In the build the chase costs vitality only; the pools don't drain at all.) The chase needs to run long enough to watch, with story text delivered in small readable beats as it goes rather than as a wall of prose — which is also the better way to feed the opening exposition. Fix before a friend plays it cold.

### Pools as dual-purpose

Once she has them, each pool is simultaneously **a shield** — absorbing drain that would otherwise hit vitality — and **a resource**, spent on workings to pass or bypass walls. Every point of pathos is therefore contested between staying alive and getting something done, and that tension needs no extra system to create it.

**\[OPEN\] The empty-pool penalty, and which form it takes.** This is the mechanical heart of the archetype system: a player who dumps everything into one hue leaves six pools near empty, and those empty pools cut the run short. Depth in one colour is paid for in time everywhere else. Two shapes:

- **The cliff.** One empty pool flips the vitality penalty fully on. Simple and brutal; makes running any pool dry catastrophic, which forces the balanced spread and effectively kills Spike.
- **Partial.** Each empty pool *adds* to the drain rate. Running two dry becomes a priced choice rather than a cliff, and a Spike build pays for six empty pools in run length.

§15 already predicts "balanced always wins" as the likeliest failure mode, and partial is the listed countermeasure — but this is unanswerable until pools do something, so it stays open.

### Holding off the darkness

**\[BUILT\]** Lighting candles lowers the *current* drain in steps for the rest of the run. Growth then continues from the lower value, so an early reducer is worth far more than a late one. It is a two-part puzzle: light the hall, then carry more candles on to open the dark corridor. Numbers: `docs/BUILD-STATE.md` "Vitality and drain".

**Make light required to explore the dark, not merely a drain reducer.** It is a room with no light in it; she cannot search what she cannot see. That converts the flint → candles → light chain from optional padding into the actual key, and it is thematically free.

**\[PROPOSED\] Make the chain's duration visibly shrink with mastery.** If the number moves on screen, grinding reads as progress; if it silently improves by 4%, it reads as nothing happening (§14.19).

**Thematically** this is Sapphire — concealment, not shelter. The darkness is the hall *noticing her*, so the counter is not being seen. Later tiers: a veiled frame, a fogged room, and finally hiding inside a memory the hall has no copy of because nobody else ever saw it.

### Restoration items

**\[BUILT\] Items restore vitality over a clock and are then consumed.** Clara uses one automatically once what she's missing, plus what the steady drain will take while it gives back, covers what it gives (one of each kind at a time). Waiting for a large item to fit the bar left it unused when she needed it most (decisions log 2026-10-02).

**\[BUILT\] One of each kind at a time.** Stacking several at once was rejected: because consumption is capped and gather speed rises with mastery, she gathers *fewer* restoration items over a run as she improves, not more — the binding constraint moves from gather rate to consumption rate, which is the intended shape.

**Capacity progression is by density, not only slot count.** Value-per-slot should rise faster than slot count, so finding a better *form* matters as much as a bigger bag.

| Form | Restores | Source |
| --- | --- | --- |
| Motes / wisps | Very little | Drift, everywhere |
| Chips | Moderate | Cut from seams, fixed nodes |
| Bottled wells / phials | A lot | Requires a phial and a source |
| Cut gems | Full | Crafted, or freely given |

### Pockets and carry capacity

**\[BUILT\] Every object takes one pocket.** Not one limit per kind: separate limits never bind against each other, so the player never chooses. Knowledge takes no space.

**\[BUILT\] Capacity expands inside a run, through actions.** Containers are instantiated out of memories — there is no leather in the hall (the satchel she carried to Evercrest, her mother's workbasket). Sizes and times: `docs/BUILD-STATE.md` "Pockets, containers".

**\[OPEN\] Making a container may need two steps, not one.** Playtesting suggests that today's one-step craft (a single action yields the satchel or pouch) is not an interesting puzzle. The alternative: gather a resource first, then combine it into the finished container, so the player plans two actions and where each happens. Not decided; nothing is built.

**\[DIRECTION\] An expansion becomes permanent only if she walks out carrying it.** This is the rule that matters most in this section, because it gives walking out (§12) a job from the moment it arrives, when the lab is completed, alongside meta currency. A run's gains convert into permanent gains only by choosing to leave with them — the press-your-luck decision.

The corollary, from the opening brief (§12a): anything carried out can later be carried back in, but anything not carried out through an exit is lost and has to be re-found.

**\[OPEN\] Whether the meta layer buys capacity at all**, now that in-run containers do the job. If both, they need different shapes — currency buying the *floor* she starts each run with, containers buying the *ceiling* she can reach within one.

**Tuning requirements:**

- **Sizes must eventually differ**, or a bigger bag is just a bigger number. Everything is size 1 in the build.
- **A full light chain plus a full vitality chain must not fit.** The failure mode to avoid is "carries everything, never chooses."
- **\[OPEN\] Can she gather while holding a full load of candles?** If not, sequencing becomes the puzzle — light first then gather, or gather first then light — and the growth curve makes that a real calculation.

### The seven hues

**\[BUILT\] Hues are named for their gems, not their colours**, matching the Eromancy Codex. The wheel and the poles are the Codex's; the game enters at station two and ends at Ruby, so the return to love is the destination rather than the starting point.

| # | Gem | Light / Dark | Effect on matter |
| --- | --- | --- | --- |
| 1 | **Amber** | Attraction / Revulsion | Adhesion / Repulsion |
| 2 | **Citrine** | Joy / Grief | Vitality / Decay |
| 3 | **Emerald** | Generosity / Envy | Growth / Withering |
| 4 | **Sapphire** | Humble pride / Shame | Concealment / Exposure |
| 5 | **Iolite** | Honest longing / Dread | Approach / Avoidance |
| 6 | **Amethyst** | Tenderness / Lust | Communion / Consumption |
| 7 | **Ruby** | Love / Hate | Repair / Fracture |

**\[DIRECTION\] The wheel order is the unlock order.** "You cannot skip a station" is the Codex's Law of the Wheel and the game's progression rule. **\[PROPOSED\]** Taking a realm out of order is a dark shortcut that costs Shadow.

These pairs are power sources with a direction, not adventure-game verbs. "Ruby 40 repairs a fracture of severity 3" is the register; "use ruby on the bridge" is not.

**Iolite is the mechanically load-bearing hue.** Approach and Avoidance — the way eases, the way resists. Since hall travel is the dominant cost (§12), Iolite operates directly on the game's central cost structure, and it is also honest longing, the emotion the story runs on. Do not cut it.

**Amethyst is the antagonist's hue.** The Codex describes parasitic eromancy — Consumption — as the seed of black magic, and the hall is a parasitic pathomancer. Clara using it rarely while the hall runs on it is thematically exact, not a gap.

### Boundary hues: Maroon and Golden-bronze

**\[DIRECTION\] Neither is a pathos pool.** They are mechanics.

**Maroon — sustaining-gift and binding.** Not an eighth bar; the name for two systems the design already has. Light pole (selfless sacrifice): paying a working in vitality instead of pathos, which is the Burst enabler (§7). Dark pole (coercion): the Take action, and Shadow. This keeps maroon prominent — it sits behind two systems rather than competing for a slot — and Clara's whole quest is a sacrifice.

**Golden-bronze — shelter and reinforcement.** Held in reserve, available if a defensive mechanic is ever wanted.

## 6. Stats & Mastery

**\[BUILT\] Two layers doing different jobs.** Skills govern the **speed** of their verb. Stats govern **everything that is not speed**. If both were speed multipliers they would be one layer with two names.

There are five stats and seven skills, and more will be added as new verbs reveal the need for them. There is no Strength stat. (In the build, a stat's *strength* is its level this run plus its mastery: the value its effects use.)

**\[BUILT\] Act I has no stats.** All five start asleep (no effects, no XP) until the first walk out wakes them at zero (plan 041; decisions log 2026-10-01).

### Each stat's primary job is a different kind of thing

This is the test for whether a stat earns its place. If two stats begin answering the same question, one is redundant — which is what happened when Endurance and Composure both touched the drain.

| Stat | Primary job is a… | The three jobs |
| --- | --- | --- |
| **Endurance** | quantity | Banks a share of each run's lost vitality, paid as extra max vitality next run (unbounded) · overflow tolerance on auto-use · XP rate for the legwork skills |
| **Composure** | rate | Slows the drain's growth rate · reduces the bleed from carried items · later, resistance to the reflection |
| **Perception** | information | Search yield per pass, not search speed · reveals hidden things below a threshold · XP rate for Instantiate |
| **Scholarship** | permission | Hard gates on tomes and inscriptions · Understanding yield per study · XP rate for Studying and Invoking |
| **Attunement** | efficiency | Pathos cost per effect · instantiation stability · craft quality |

**\[BUILT\] Endurance trains from vitality lost, and banks it.** When a run ends, part of the vitality lost goes into a bank, paid as extra max vitality next run (numbers on the assets; BUILD-STATE "Endurance bank"). Surviving is what teaches endurance, and it fixes the gap where the two most-repeated Act I actions fed no stat. The formula is provisional. It is off until Endurance wakes, so Act I banks nothing.

**\[OPEN\] Attunement currently does nothing.** Its one built job is cheapening hue costs, and hues cost nothing until pools work. Its other two jobs are unbuilt. Accepted for now: one of the five stats is dormant until Act II.

### Why Endurance and Composure had to be separated

**Max vitality and drain resistance are the same lever.** Both scale the drain integral linearly: halving the drain and doubling max vitality give identical run lengths. Giving one stat both is not two benefits, it is one benefit at double rate, and it made the two stats interchangeable.

Endurance now buys a **flat buffer**; Composure buys a **flatter curve**. Short aggressive runs want Endurance. Long deep runs want Composure. That is the first archetype split, and it arrives in Act I with no new systems.

### Curve shapes depend on whether the effect is bounded

Levels reach the high hundreds or low thousands late in the game, which breaks any stat whose effect has a small useful range.

- **Unbounded effects** (max vitality, speed multipliers): linear or near-linear is fine at 1,000 levels.
- **Bounded effects** (carry slots, percentage reductions toward zero): either logarithmic with accepted dead zones, or moved off levels entirely.

**Carry capacity is not a level-scaled stat.** At level 900 it would need \~100 more levels for +1 slot — the opposite of visible progress. Capacity comes from in-run containers and possibly the meta layer (§5), where each increase is an event rather than a rounding.

### Skills

Stats govern capacity and cost; skills govern the speed of a class of action.

| Skill | Covers |
| --- | --- |
| **Wayfinding** | Travel and searching — covering ground and reading unfamiliar territory |
| **Gathering** | Collecting drift, wisps, chips |
| **Instantiate** | Pulling objects out of reflections |
| **Invoking** | Casting workings |
| **Studying** | Reading, deciphering, translating |
| **Crafting** | Making things from parts (the gem's gate). *Placeholder wording.* |
| **Convincing** | Talking to reflections and memories. *Placeholder name.* |

**\[BUILT\] Stats influence skills through XP rate, never through speed.** This is always a stat's second or third job: a stat whose identity is "makes other numbers rise faster" cannot be felt or reasoned about.

**\[BUILT\] One stat per action, not *Idle Loops*' two to five.** Requiring several stats per action is what makes that game opaque — the player cannot tell why an action is slow without a wiki. With five stats, one per action stays legible, and the multi-input richness comes from the pathos pools instead.

**Avoid a generic Speed stat.** Speed belongs to skills; a stat that granted speed would make the skill layer redundant.

### Levels, mastery and the treadmill

|  | Resets? | Job | XP cost growth | Speed per level |
| --- | --- | --- | --- | --- |
| **Level** | Yes | The within-run speed ramp | \~1.085 | \~×1.05 |
| **Mastery** | No | A head start: skills start faster, stats start stronger | \~1.085 (stays steep: the soft cap) | \~×1.01 (skills) |

**Levels supply the large multipliers, and they self-scale with content depth.** Level XP cost never grows across the game, because levels reset and always climb the same bottom stretch of the curve. XP income per action, however, scales with base duration, which scales with realm tier. So the same level is far cheaper in deep content:

|  | Level 40 | Level 60 | Level 100 |
| --- | --- | --- | --- |
| Speed | ×7.0 | ×18.7 | ×131 |
| Actions to reach, tier 0 | 269 | 1,418 | 36,499 |
| Actions to reach, tier 7 | 2 | 11 | 285 |

That is where ×100–×1000 comes from, and it works without intervention.

**Mastery's job is the floor she does not have to re-earn** — for skills, the speed at which a run's *first* action resolves; for stats, how strong they start (max vitality, search yield and so on). This is why mastery has to track content tier. At tier 7 a base action is \~1,920 s; at level 1 with mastery at ×2, the first action of the run takes \~960 game-seconds, which is longer than the whole run. The run cannot start.

**Ruling (decisions log 2026-09-27): mastery's XP cost growth stays steep.** The slowdown is the intended soft cap, and meta currency buys mastery levels at rising prices to push past it. The consequence, which this table shows: from XP alone, at \~8.5% cost growth against \~1% speed growth, skill mastery tops out near ×2–×3:

| Mastery | Cumulative XP | Speed |
| --- | --- | --- |
| 40 | 5,914 | ×1.49 |
| 60 | 31,198 | ×1.82 |
| 80 | 160,455 | ×2.22 |
| 100 | 821,222 | ×2.70 |
| 150 | 48,536,638 | ×4.45 |

**\[OPEN\] How deep content stays startable.** Flattening cost growth to \~1.01 (so the treadmill self-maintains) was considered and rejected. The tier-7 problem above still needs an answer: meta-bought mastery levels, shorter base durations deep in the game, or a faster early level ramp. Note for tuning: the build has no separate mastery growth field; mastery reuses the level curve's 1.085, and only its first level (20 XP) is separate. A different value needs a new field.

**\[BUILT\] Show the multiplier, not the level.** Each stat and skill displays its current effect — "×1.16 speed" — with levels in the tooltip. Because duration is cost (§5), this is the number that tells the player their runs are getting longer.

### Open questions in this section

**\[OPEN\] Where does raw power live?** With no meta currency and mastery bounded, Clara's Act I power is run levels (which reset) and switches (which are binary). Nothing gets permanently bigger except mastery. Is magnitude a stat she levels, or purely a build choice?

**\[PROPOSED\] Stat opposition.** Composure resists the drain by not feeling; Attunement channels pathos by feeling keenly. High Composure could reduce pathos harvest rate; high Attunement could make the drain bite harder. This turns a build from "where do I spend" into "what kind of mage is Clara this run." Least fleshed-out idea in the document, and not implemented for a long while — it needs pools to exist before it means anything.

## 7. Build Archetypes
Moved to `future-sketches.md`. Not built; revisit when pathos pools work.

## 8. Content Vocabulary: Walls
Moved to `future-sketches.md`. Not built; revisit when pathos pools work.

## 9. Content Vocabulary: Tasks

Tasks are the connective tissue — what fills the action queue between walls, and the bulk of the content.

| Task | What it does | Stat |
| --- | --- | --- |
| **Travel** | Move between nodes. Costs time and a flat charge (5, split across the pools, which pay it while they are full; halved by the mana stone's warmth) | — |
| **Search** | Fill a room's bar to reveal its actions and ways on | Perception |
| **Study** | Translate, decipher. Yields knowledge flags | Scholarship |
| **Gather** | Collect drifting pathos and restoration items. The refuel action | — |
| **Instantiate** | Pull an object out of a reflection | Attunement (Perception speeds the skill's XP) |
| **Pick up / Put down** | Move items between pockets and a room's floor | — |
| **Assist** | Help a reflection; slower than taking, yields stronger freely-given pathos. Not built | Varies |
| **Take** | Fast pathos from an unwilling reflection. Adds Shadow. Built only as *Take the tome*, with no Shadow | — |
| **Work** | Cast. Spends pathos to change the world. The build has the kind but no Work tasks, and it trains no stat | Attunement (intended) |

Act I actions outside this vocabulary so far: Chase Roland, Light a candle, Attend, Clear the bench, Draw on the mana stone, Feed your hours to the flames.

**\[BUILT\] Explore and Search are one verb.** One bar fills and reveals a room's actions and ways; the merge is the design, and Explore is not in the vocabulary.

**\[OPEN\] Are rooms searched once, ever, or until they are known by heart?** Playtests found re-searching every room every run, in several passes, unintuitive (decisions log 2026-09-29). The build searches each room once, ever, and keeps the bar between runs (plan 020). The other option is to search afresh each run until the room is known by heart (the "after N searches" idea). This is still being playtested and neither is settled.

**\[PROPOSED\] Further exploring costs more, steeply.** Explore's cost should ramp heavily, so that in Act I only one exploring of a room is affordable, yet the player gets the idea that exploring further may be necessary later in the game. The alternative: exploring further is locked until the Amber pool is unlocked. Not decided between these, and not built.

**\[BUILT\] There is no Rest or Wait action, and no idle state.** An empty queue pauses the game, and an action repeats until it completes (BUILD-STATE "Action queue"; reasoning in decisions-log-older 2026-09-26 *No resting*).

**\[BUILT\] Gather needs no artificial ceiling.** With time-based drain, an infinite gather loop cannot happen: growth compounds until it outpaces any gather rate, so the darkness is the ceiling. The only tuning question is *where* each crossover lands (§5).

**People and objects are entries in the action list**, revealed as a room's search bar fills. Example: *The Weeping Governess* — Listen (Perception), Console (spends a hue, returns freely-given pathos), Take (fast pathos, adds Shadow).

## 10. Content Vocabulary: Challenges
Moved to `future-sketches.md`. Not built; revisit when pathos pools work.

## 11. Persistent Items & World Switches

Two different things persist, by two different rules.

| What | How it persists |
| --- | --- |
| **Physical items** | Only by being carried out through an exit |
| **Knowledge-like rewards** (Mirrors Found, memories, Quickened Hours, the mana stone's warmth) | By being obtained |
| **World switches** | By being flipped, once, for good |

### Items persist by being carried out, not by being obtained

**\[BUILT — differs\] Tools reset every run.** Only a handful of switch-like rewards persist on being obtained. Once Act I is complete, the player will be able to carry tools out of the Hall with her to keep them for the next run (walking out, §12). The rule for everything else:

**\[DIRECTION\] No physical item persists by being obtained; it persists by being carried out.** Knowledge-like rewards are the exception and are kept on obtaining (decisions log 2026-09-27). Anything carried out can later be carried back in, but anything not carried out through an exit is lost and has to be re-found. This is what gives walking out (§12) its job, and it is why the satchel matters: an expansion earned inside a run becomes permanent only if she leaves with it.

Items do something stats cannot: stats are numbers going up, **items are new verbs.** They are what make the hall feel like it is opening up rather than the character simply getting bigger. Two rules:

- **An item changes what Clara can do, not how fast.** If it would be a percentage bonus, make it a stat instead.
- **Items should sometimes substitute for stats.** A lens that reveals hidden things lets a low-Perception build pass a Perception wall. This is how off-meta builds stay viable instead of funnelling toward one correct allocation.

**\[BUILT\] Carried items with an ongoing cost** are a third category, distinct from permanent free upgrades. Roland's ring is the first: while held it drains vitality during actions, a hard cost that bypasses the carry-cost switch (the build has it on; numbers in `docs/BUILD-STATE.md`, "Carried items and the ring"), and it trains Composure. Its rate is a placeholder to balance from playtest feedback: it should make carrying, dropping and picking the ring up a real decision. She is expected to put it down. That is the point — and the aha arrives later, when the lab door will only open for someone carrying it.

**\[OPEN\] The ring's rate needs balancing.** It is likely too low now; too high and, carried from the Right Corridor all the way to the lab, it makes the run uncompletable rather than merely expensive. Verify it reads as *a cost she resents* and not *a run she cannot finish*.

**\[BUILT\] Carried items are lost when a run ends** by exhaustion or by the player; only walking out (§12) keeps them. Stored items, once the mirror lab has storage, are kept. Detail: `docs/BUILD-STATE.md`, "Carried items and the ring".

### World switches

**\[BUILT\] Certain checkpoints do not reset.** A repaired mirror stays repaired; an unlocked door stays unlocked; a drained fountain stays empty. Each is a milestone with a story passage attached; the count is in `docs/BUILD-STATE.md`.

This is a limited set by design — not everything the player achieves becomes permanent, or the carry-out rule above has nothing to bite on.

**Why this survives the scope objection to branching paths:** a branch needs two authored worlds. A persistent flag needs one world with a door in two states — a boolean and an `if`. Ten of these is ten conditionals, not ten storylines.

Two rules that keep it honest:

- **Gate every switch behind capability, not patience.** Flipping a switch is then *proof of a build*, never proof of persistence — otherwise persistent unlocks quietly become the grind path the economy is designed to prevent.
- **Show the state of the hall.** If the world changes across runs, the player needs a record. The node map becomes essential rather than optional, and is the natural Hub screen: look at the hall, decide what to open next, build for it.

**The payoff:** the player is building the world, not only the character. A run that ends badly still leaves a door open — real progress without stakes, which also softens the frustration risk inherent in a no-grind economy.

## 12. World Structure & the Lock-and-Key Graph

### Zones

**\[DIRECTION\] Ten zones**, across two sides of the glass. The build has six rooms: four Hall rooms, one constructed (The Dark Corridor with Hanging Mirrors) and one memory. **Name clash:** the build's *The Mirror's Laboratory* is Clara's memory of a laboratory, not the mirror lab below.

| Zone | Side | Drain | Role |
| --- | --- | --- | --- |
| **The real lab** | Outside | None | Between-run Hub. Meta currency, allocation, Roland's actual notes. **Reaching it is an achievement, not a default** |
| **The mirror lab** | Inside | None | In-run safe node. Crafting, storage, the way home |
| **The Hall of Mirrors** | Inside | **Fastest** | The connecting corridor. Draws from all seven pools |
| **Seven realms** | Inside | Slower | Puzzle zones, entered through mirror portals |

**Rule of thumb:** the real world holds what he wrote; the mirror holds what happened.

**\[DIRECTION\]** The hall drains faster than the realms, so travelling back and forth is the most costly activity. A good run does as much as possible per realm visit.

**\[DIRECTION\]** Memory supplies the setting; the hue supplies the physics. Each realm is built from someone's memory and twisted by the hall into a place governed by one emotion.

**\[DIRECTION\] Entries and exits are one-way valves.** Clara either enters through the real lab and exits through the mirror lab, or enters through the mirror lab and exits through the hall mirror. This is what stops extraction becoming free once the lab is a starting point.

**\[OPEN\] Conflict with Act I's finish.** Act I's exit is the starting mirror, the one she came in through (below), which is an entry and an exit at once. Settle how this fits the one-way valves, and how the starting mirror relates to the mirror lab, when Act II's map is designed.

**\[OPEN\] The realm sketches are stale.** Moved to `future-sketches.md`.

### \[BUILT — differs\] The memory lab: Act I's finish

*The Mirror's Laboratory* in the build: Clara's memory of the night the stone was cut, reached through the ring-gated mirror. Redesigned 2026-09-29/30; verbs, pacing, stat pairings and numbers are in the decisions log (2026-09-29 *The Mirror's Laboratory*, 2026-09-30 *Stats pass*), plans `027a`–`027d` and `docs/BUILD-STATE.md` ("The memory lab"). The plot of what the memories reveal is unwritten.

- **Shape.** The first search finds the earrings (10 dense wisps), three ways to earn kept insight (*Watch him*, *Study the tome*, *Attend*, each once a run) and *Talk to Roland*: one long push needing only the ring, shorter for every insight held. First tries run her dry but train Convincing: a deliberate grind against the drain. The talk hands him the ring and unlocks phase 2: practising the cut (always worse than advancing), then Craft the gem, so long and costly that first attempts run her dry but keep the XP. Earlier rooms are run through fast by then (room mastery).
- **The exit.** The gem is the memory's replica of the one her reflection stole; it opens the starting mirror and gives the Amber pool, empty (no Amber magic in Act I). The last run is the walk home through the whole hall, then *Exit through the glowing mirror*: a story beat (she is back in the lab), the Summary, then the meta-currency screen (not designed).
- **Direction for Act II:** Amber binds things to a place (a stash, a lit candle, a way held open) across runs, at a pathos cost and in a few slots; exiting binds things to her. This gives the Act I rooms a job in Act II.

### The three run endings

The prologue leaves Clara trapped on the mirror side. The real lab is visible through the glass — her bench, her chair, her life going on without her — and unreachable. Getting home is not free.

| Ending | Condition | Keeps | Loses |
| --- | --- | --- | --- |
| **Collapse** | Vitality hits zero | Knowledge, stored items, world state; from Act II, \~half of the run's stat XP | Carried items and any gems, dropped where she fell |
| **Walk out** | *Exit through the glowing mirror* at the Smoky Mirror (it needs the crafted gem), with vitality still in hand | Everything she carries; from Act II, all of the run's stat XP; from the end of Act II, meta currency | The depth she could have reached with the vitality left |
| **Plan fulfilled** | The queue empties with its objectives met, and she goes home | As walking out | Nothing — the clean win |

**\[DIRECTION\] Only one of the three is failure.** The structural flaw otherwise: every run ends in "she ran out," and the success is deferred to a summary screen. Roguelikes prove that pattern can work, but the ones that work give the player an ending they *chose*.

**\[BUILT — differs\] Walking out arrives at the end of Act I; meta currency at the end of Act II** (decisions log 2026-09-30 *Features come in layers*). Crafting the gem opens the exit at the Smoky Mirror, which takes her home to the real 1928 lab. She chooses to step back in each run, for Roland. The exit is built (plan 027c). **\[BUILT\]** The first walk out wakes her five stats at zero (Act I has none). Until currency arrives, stat XP is walking out's reason to exist: a walk out keeps all of the run's stat XP, and a collapse or End run keeps \~half (plan 041; decisions log 2026-10-01).

**Every run is then a live calculation: how deep can I go and still afford the way home?** Push too far and the knowledge is kept but the build is forfeit.

**Plan fulfilled costs almost nothing to add.** The game already knows whether the authored route completed. "You did what you came to do" is a far better run-end line than "you ran out."

### Narrativising the collapse

**\[PROPOSED\] Collapse is a transition with a scene, not a fail state.** *Hades*' most-praised structural choice was making death a narrated transition rather than a loss screen.

**Collapse text should vary by where and how she went down.** Running dry in the dark reads differently from failing at the lab door with the ring in hand. This is the ambient-bucket system pointed at the moment with the least writing and the most emotional weight.

**\[PROPOSED\] Sequence the run-end so the last thing on screen is the win.** Collapse → summary → next run reads as loss-then-consolation. Give the achievements the visual weight and the final position.

**\[DIRECTION\] Do not over-soften it.** *Hades* slightly suffers from death being so pleasant it stops stinging, and this is a game about a woman being consumed. The distinction to hold: **the run ending should have weight; the session should be able to end on a win.**

**\[BUILT\] No wall clock shown to the player.** The between-run Summary page is the beat that prevents an engineered next-run pull: it costs nothing for a player who wants to continue and gives a natural exit to one who does not. Milestones should land at legible stopping points, so a player who leaves remembers the unlock rather than a wall.

**\[DIRECTION\] No vitality recovery in the mirror lab.** Safe means *no drain*, never *recovery*. If resting there restores anything, the optimal play is ping-ponging back whenever low, and the run-length puzzle disappears.

**\[PROPOSED\] The two-sided mirror is a later switch.** Once earned, Clara may enter from either side, saving the inbound traversal. Gate it *after* the mirror lab itself — find the lab, then earn the shortcut.

### Verbs in the mirror lab

| Verb | Does | Available from |
| --- | --- | --- |
| **Craft** | Recut the gem and add facets as hues unlock. The gem itself is made in the memory lab at the end of Act I (*The memory lab: Act I's finish*, above) | Act II |
| **Study** | Read the hall's copy of the book on the bench. Not Roland's real notes — those are through the glass. **The copy may be wrong, and she would know** | Act II |
| **Store / Retrieve** | Leave items here. The lab does not reset, so stored items survive a collapse | Act II |
| **Look through** | Watch the real lab. Her chair. Whether anyone has come looking for her | Act I |
| **Cross** | Spend vitality to go home. Ends the run and banks meta currency | Act II |

**Store is the other half of carrying out** (§11). Items cannot be carried out through a collapse, but items left in the lab survive one. It is available earlier than a crossing, and it makes the lab feel like somewhere she is making hers.

**Look through is where the family lives.** Clara's children never appear on screen. They appear as a chair pushed back, a lamp left burning, someone having tidied the bench. It costs nothing and it is the best available pressure against the rescue.

### &#91;OPEN\] The Hub needs stakes

A no-drain zone has no decisions in it, so the lab risks becoming a settings screen with prose attached. Two candidate fixes: **the lab decays** — it is a reflection held together by her attention, and neglected it degrades, which ties directly to the memory-correction verb; or **the lab is watched** — something visits, and what she stores is not perfectly safe, which is cheaper to build. Decide once the lab exists and it is clear whether it feels inert.

### Memory nodes
Moved to `future-sketches.md`. Not built; revisit when pathos pools work.

### Position and travel

**\[BUILT\] Nodes from day one**, displayed on a visual map. Hall drain is the dominant cost and hall shortcuts are switch rewards, so routing is part of the puzzle. Retrofitting nodes later would mean rewriting movement and every action cost.

### Traversal economy

**\[DIRECTION\] A traversal has three costs, and they behave differently on purpose.** The build currently charges a flat 5 split across the pools, with no escalation and no tiers.

```
duration(edge)      = baseDuration(tier) / wayfindingSpeed      ← scales with skill
drain incurred      = duration × current drain rate             ← follows from duration
vitalityCharge(n)   = chargeBase × chargeGrowth^(n-1)           ← skill-INdependent
```

`n` is the traversal index **this run**, counted across all edge types and reset each run.

**Why the flat charge exists.** Anything skill reduces eventually trivialises: at 100× Wayfinding a trip home takes 0.3 seconds and costs nothing, and with it goes the walk-out decision, the value of shortcuts, and any reason the early map still exists. A charge skill cannot touch is the only cost that stays meaningful at every scale. Thematically it is not distance but **exposure** — the hall notices her moving, and each crossing draws more attention than the last.

**\[PROPOSED\] Starting parameters:**

| Parameter | Value | Notes |
| --- | --- | --- |
| `chargeBase` | \~5 | Vitality for the first traversal of a run |
| `chargeGrowth` | \~1.2 | Compounding per traversal |
| `hallMultiplier` | \~×2 | Applied to hall edges, preserving "the hall is the expensive place" |
| `D0` | \~30 s | Base duration of a tier-0 traversal |
| `tierMult` | \~2.0 | Base durations double per realm tier |

**The charge does not scale by tier.** It stays on the same numbers all game, so maximum route length is set by the escalation against the vitality pool — which grows only with Endurance. "How far can she range" becomes a direct, legible consequence of one stat: \~8 traversals at 100 vitality, \~16 at 500, \~24 at 2,000.

At a six-traversal round trip per realm, the second realm of a run costs about three times the first and the third about nine times. **Act II's "travel back and out a different branch" structure is therefore gated by economy rather than by locks** — it opens as vitality grows, and "I can do two realms in a run now" becomes a felt milestone rather than an announced unlock.

**Emergent tension worth protecting:** the return legs sit at the top of the escalation curve, so a wide tour often cannot afford to walk out. Tour widely and probably collapse, dropping your gems where you fell — or go focused, gather less, and bank it. *Verify in the balance model that a two-realm tour can sometimes still walk out; if touring always ends in collapse, the choice collapses back to one option.*

**Shortcuts scale with when they are used.** Skipping one traversal saves \~6 vitality as traversal 2 and \~64 as traversal 15, so shortcut placement is a routing decision rather than a flat discount — and the two-sided mirror is a much larger purchase than it looks.

**\[DIRECTION\] Tier is baked, never computed from live graph distance.** Base durations are a property of the realm tier, assigned once and inherited by the edges into and within it. Computing depth from the live graph looks equivalent and is not: the moment a shortcut lands, a deep realm sits two hops from the lab and its base durations collapse. The content did not get easier, the route did.

### The switch graph
Moved to `future-sketches.md`. Not built; revisit when pathos pools work.

## 12a. The Prologue and Act I's Beats

*Merged from `opening-brief.md` (Scott, 2026-09-24, revised 2026-09-30) on 2026-10-01; the original is in `Archive/`. Run numbers are pacing targets: beats are triggered by switches, not by run count. The lab's finish is in §12 *The memory lab: Act I's finish*; run endings in §12 *The three run endings*; zones and the one-way valves in §12 *Zones*; carrying in and out in §11; memory nodes in `future-sketches.md`. Older log entries citing "§12a *Mid-run editing*" mean §13.*

### \[DIRECTION\] The prologue (run 1): scripted, \~90 seconds

Clara enters the hall with her anchor gem full: all seven pools. She chases Roland, who is being dragged by a reflection wearing his face. Every action drains all seven pools visibly and fast. When they empty she collapses; her last sight is her own reflection taking the gem off her.

She wakes on the mirror side of the lab, looking out, with no way back.

Purpose: the player must see seven full bars before they are taken. That is what makes wanting them back legible.

**\[OPEN\] Drain in the prologue.** The brief has *every action* drain the pools; since 2026-09-25 vitality drains with time, not actions (§5). Settle which the prologue uses when it is built, together with its length (§17 question 5).

### \[BUILT — differs\] Runs 2–3: exploration, no pools

Only vitality remains, and it falls as she explores (drain is by time, §5).

- Exploration must present choices, not a single bar. At minimum, two corridors she cannot both search in one run.
- Each run must end with a discovery, not a counter increment. Run 2: the dark with the hanging mirrors. Run 3: some mirrors show her life, some show his.
- Roland's ring lies in plain sight, in a fixed node. It never moves and is always re-findable.

### \[BUILT — differs\] The ring: a carried item with an ongoing cost

A carried item that taxes while held; every other item is a permanent free upgrade. The cost, its balance and the aha are in §11 *Carried items with an ongoing cost*. The ring is his instrument: an empty gem, and empty vessels draw on whatever feeling is nearest. The player is expected to drop it. That is the point.

**\[OPEN\] since 2026-09-30:** once Clara has pathos pools, the ring was to draw from those instead of vitality, becoming cheap, and later useful rather than costly. She now gives the ring to Roland's memory at the end of the lab's first phase, so this later use needs a new home, or is dropped.

### \[BUILT — differs\] Act I's arc to the lab

What each stage teaches. All numbers are `~` placeholders. The room itself is §12 *The memory lab: Act I's finish* (plans 027a–d).

| Runs | Beat | Teaches |
| --- | --- | --- |
| \~5–7 | The lab memory is visible but sealed. Entry requires the ring; she carries it there. | Mirrors are doors with conditions |
| \~8–10 | Inside, the memory is wrong. Over a few visits she earns insight (watching him, studying the tome, attending) and pushes one long **Talk to Roland** until she convinces his memory to take the ring. The ring is never needed again. | Correcting a memory |
| \~10–12 | The earrings, found on the first search, hold dense wisps. She practises and then crafts the gem: the memory's replica of the one her reflection stole. One pool (Amber), empty. | The first bar appears · a stat check you train for over runs |
| End | The gem opens the mirror she came in through. She walks home through the whole hall and exits; Summary, then meta currency. | Extraction |

**\[OPEN\] Act I's length.** This table puts Act I at \~7–12 runs; the decisions log (2026-09-30, *One act per pool*) says 10–15 runs at minimum and 30 minutes to 2 hours. Playtest to settle.

Moved to Act II: the first Gather into Amber (the memory of a meeting) and the first working (adhesion on something in the hall). The mirror lab as a separate exit is also Act II (§12 *\[OPEN\] Conflict with Act I's finish*).

The lab's gate is the aha: the player refused the ring earlier as a cost, and now needs it. It must read as a choice they made, never as something they missed. Giving the ring to Roland is its release: the cost she carried is gone for good.

### Run endings, as the brief framed them

Both endings must exist: **yanked out** (vitality hits zero: keeps knowledge, loses carried items) and **walked out** (keeps carried items, costs the way home). Rule to surface plainly: **knowledge survives because it is in her head; items do not, because the anchor pulls only her.** This makes the node graph a round trip: every run needs a return-cost calculation. The full table, with *Plan fulfilled*, is §12 *The three run endings*.

## 13. The Action Queue & Routing

**\[BUILT\] The queue is built during the run and empties every run.** How it works now: `docs/BUILD-STATE.md`, "Action queue"; the rework that replaces it is §13a.

**\[DIRECTION\] The queue and the interface are due a large rework**, and §13a is that rework. Enough features now exist that the current shape is the limiting factor. Read §13 as a statement of the problems the rework has to solve, not as a description of the destination.

### The problem this section exists to solve

An assumed queue is not automatically an interesting one. In *Idle Loops* the queue is a genuine puzzle because it is constrained — mana is a shared budget across every action, prerequisites must land in order, and adding one thing means cutting another. As currently specified, none of that pressure exists. Walls are allocation checks, so the queue becomes a delivery mechanism: get Clara to the wall, cast the thing. The interesting decision already happened in the Hub.

The question to keep asking is not *is there a queue* but **what makes one queue better than another?**

### Sources of queue pressure

Four candidates. At least two are needed; none is currently in the design.

| Source | What it does | Status |
| --- | --- | --- |
| **Consumption clock** | Every action added spends something | Resolved against: drain is time-based, so *time* is the budget, not actions |
| **Queue length limit** | Clara can only hold so much in mind. Short early, growing with Composure or Scholarship | \[PROPOSED\] — see the collision below |
| **Order-dependent value** | Actions worth more after others — gather before a big cast, study before a search reveals more | \[PROPOSED\] |
| **Timing windows** | Something available only for a stretch of ticks. Race walls do this; nothing else does | \[PROPOSED\] |

With drain time-based, the real budget is **elapsed game time**, and every action competes for it. That is a genuine constraint, but it is a flat one: it makes the queue shorter without making any ordering better than another. Order-dependent value is the cheapest of the three remaining candidates and the one that most directly attacks §14.11.

### The constraint collision

Vitality-as-budget and a queue length cap both limit how much Clara does in a run, so **one will bind and the other will be decoration.** If time allows 40 actions and the cap is 12, the cap is the real constraint. If reversed, the cap never binds and the player forgets it exists.

**\[PROPOSED\] Make them constrain different things:** time limits total *work*; the queue cap limits total *planning*. That requires the queue to fold or carry repeat counts, so a 12-slot queue can produce 40 actions. The cap then becomes about expressing an efficient plan compactly — a genuinely different puzzle from doing less.

### &#91;OPEN\] End of queue

Pause and loop-the-last are different games. Pause makes the queue a complete plan; running out means you planned short. Loop-the-last makes it a plan plus a default — a soft landing that keeps doing something useful. If both are always available, players will end every queue with whatever action is cheapest-per-benefit and let it ride, and the loop becomes the real mechanic.

**Recommendation:** a per-queue flag on the final slot, so ending on a gather loop versus stopping to reassess is a deliberate choice each run. The build currently pauses only.

*Koster's warning applies directly: players will optimise the fun out of a game given the chance. A stable "best filler action" is exactly that hole.*

### &#91;OPEN\] Mid-run editing

The build allows it, and it is how the queue is built at all. But if the player can pause and add actions at any time with full knowledge of the current state, planning ahead loses value — the game can be played reactively, one action at a time. That is not automatically wrong (*Stuck in Time* works this way), but it makes any between-run planning layer less central than §4 assumes. Decide as part of the rework whether mid-run editing is free, limited, or costs something.

**Direction (decisions log 2026-09-27): both halves.** A planning screen before the run sets up automation for content Clara has mastered; unmastered content is played live, so mid-run editing stays. Whether it costs anything is still open.

### The structural issue: two puzzles, not one

As currently designed, the build decides whether Clara *can* pass a wall and the queue decides whether she *gets there*. Those are separate puzzles stacked on each other, not one puzzle.

They should interact. A spike build should require a **different route** — fewer stops, less gathering, a direct line because there is no time — not merely a different number. If the build changes the shape of the optimal queue, then reallocating forces rewriting, and §4's intended rhythm actually contains a re-route step. Without this, step 4 of that rhythm is "change one number in the Hub and run the same script."

## 13a. Interface — the Map Is the Queue

**\[DIRECTION\] This section is the proposed UI and queue rework**, and it is the next large piece of work. It is planned in `docs/plans/finished/ui-rework-roadmap.md` (decisions log 2026-09-27), which takes this section as a starting point, not a spec. Where the plan departs from it: the ribbon shows only the current action (with its progress) and the next; a tab at the bottom slides up a drawer with the whole queue laid out horizontally, for adjusting; story beats and ambient text share one panel for now (to playtest); and the planning screen for mastered content comes after the main screen. A route drawn between nodes and a list of stops carry identical information, so the map holds the plan rather than sitting beside it.

**The problem it solves.** The current greybox has the map at roughly 10% of the screen, with buttons, queue and story filling the rest. A map that small makes routing impossible to *feel* — and routing is the thing the design most needs to be evaluated on. Bad layout does not merely look worse; it corrupts the read on whether the game works.

### How it works

- **Click nodes in order** to build the route. A line draws between them, numbered 1, 2, 3.
- **Each node carries a badge** — action count, or *by heart* once folded.
- **Clicking a node opens its popover** over the map: available actions, floor pile, learn progress. One popover meaning *everything about this place*.
- **Clara's token walks the drawn line** during the run.

This removes two of the three large panels: the button column moves into node popovers, and the queue becomes the drawn route.

**The division of labour:** the map is what the player *interacts with* — verbs are chosen there and dumped into the queue. The queue is what *shows the result*.

### Proposed layout

```
┌──────────────────────────────────────────────────────┐
│ ♦ vitality ████████░░░  −0.8/s   2:14   ▶3×   ⏸       │  8%
├─────────────────────────────────────────┬──────────────┤
│                                         │              │
│        ○──①────────②                    │  ambient     │
│                ╲                        │  text        │
│                 ╲      ╭─────────╮      │  scrolls     │
│          ③───────④     │ the dark│      │  here        │
│                  ╲     │  ▓▓▓░░  │ ←learn             │ 65%
│                   ⑤    ╰──●●●────╯ ←pile              │
│                     ◆Clara              │              │
│                                         │              │
├─────────────────────────────────────────┴──────────────┤
│ ① Dark Hall · ② left corr · ③ the dark · ④ … ⑤ …       │  6%
├───────────────────────────────────────────────────────┤
│ 🕯🕯🕯 ✦✦ ⬤  9/12   │ standing: top up <20%  │ Wayf ×2.1 ▾ │  8%
└───────────────────────────────────────────────────────┘
```

**The route ribbon** (the strip under the map) exists because order is trivially readable in a list and genuinely hard on a map — a route that returns to a node crosses itself, and that node has to show *1, 3*. The ribbon restores linear legibility for one thin strip and doubles as the drag target for reordering. Map for space, ribbon for sequence.

**Space recovered elsewhere:**

- **Stats collapse to one row of multipliers.** During a run the player needs "Wayfinding ×2.1," not the level. Expand on click.
- **Ambient lines render on the map**, fading in near Clara's token as she moves. Only story beats open a panel. This also separates the two text types visually, since they do different jobs.

### \[DIRECTION\] A room's shape says what kind of room it is

Each room on the map is a mirror, and its frame shape tells the player its kind (Hall junction, memory, constructed, and any kinds later acts add), so the map can be read at a glance and learning the hall includes learning to read its shapes. Size can vary room to room within a kind. **Curves are memories, corners are the Hall's own rooms:** Hall junction a rectangle, constructed an octagon, Clara's memory an oval, Roland's memory an arch (decisions log 2026-10-02 *Mirror shapes: curves are memories*). Stage 1 draws them as plain shapes (plan ui-054), real mirror art waits for stage 3 (decisions log 2026-10-01 *Colour as the payoff*).

### Floor piles are map data, not inventory data

**\[BUILT — differs\]** Rooms have floor space. A pile belongs to a node, so it draws *on* the node — which solves the hard problem (seeing what is cached in rooms Clara is not standing in) for free and costs no panel space. Whether piles persist between runs is an Act II question tied to the carry-out rule (§11); how the build shows them now: `docs/BUILD-STATE.md`, "Floor".

| What | Where |
| --- | --- |
| Clara's pockets | Persistent bottom strip, with "9/12" — the ratio packing decisions turn on |
| Every node's pile | A badge on the map node, drawn **only when non-empty** |
| The current node's contents | Inside that node's popover, beside its actions |

**Colour the pile pips by kind** — lavender for restoratives, hue-coloured for pathos, grey for tools. The map then reads as a fuel map at a glance, which is what makes a late-game supply network legible instead of requiring a spreadsheet.

**\[BUILT\] Transfers are queue actions, not drag-and-drop.** *Put down 5 candles* and *Pick up 3 wisps* are verbs like any other. With a folding queue and automatic top-ups, per-item dragging every run would be miserable; as actions they are planned rather than fiddled, and they survive into a learned room's routine. A supply run then becomes a legible thing: a route whose stops are mostly drops.

### Standing orders — priorities, scoped correctly

**\[DIRECTION\] Two systems, neither doing the other's job.**

|  | Answers | Nature |
| --- | --- | --- |
| **The route** | *Where* | Spatial, planned |
| **Standing orders** | *What overrides what, wherever she is* | Non-spatial, persistent, condition-triggered |

*Increlution*'s 1–5 priorities carry everything because location is nearly free there. Here travel is the dominant cost, so the route must own placement and priorities own interruption and opportunism.

**A standing order has to be a verb that takes queue time.** That is the test. Consumption is automatic and parallel, so *consume a wisp* is not a candidate — it displaces nothing and so contains no decision. *Gather a wisp* is.

Candidate orders: gather wisps when below N; fill a phial when empty and there is a source here; drop surplus when pockets are full and this node is a depot; take from a pile when passing one with space.

**\[DIRECTION\] An ordered list, not numbered tiers.** Position is priority; cap the list around five or six. A player reads "these three things, in this order, whenever they apply" instantly, and cannot read "this is a 3 and that is a 4" without checking what else is a 3.

**The trigger threshold is the real knob**, and the player should set the number. *Whenever pockets have room* is greedy — steady time cost everywhere, rarely dies, never goes deep. *Below N items* is a buffer, the middle. *Below X% vitality* is reactive — free until it is not, and it fires at the worst moment because that is when the drain is steepest.

**\[DIRECTION\] Standing orders never reroute.** If Clara runs dry at a node with no source, she does *not* detour — the moment a standing order can send her elsewhere it is making routing decisions and will wreck plans the player built. She runs out, the run ends, and the report says *ran dry in the left corridor — no source here*. Diagnosable, the player's fault, fixable by putting a gathering stop on the route.

**\[BUILT — differs\] Auto-inserted actions must be visibly distinct and counted in the run report.** The build marks auto-supply as "supplying…" in the queue but does not count it in the report. A plan that does not run as authored, with no visible reason, is the opacity failure in §14.12.

**\[PROPOSED\] Standing-order slots are a progression.** Clara starts with none and earns them one at a time. A currency sink that changes how a run *plays* rather than its numbers; it stages automation instead of dropping a policy editor on the player; and thematically it is how much she can hold in mind without attending to it. **The teaching moment writes itself:** grant the first slot the first time a run ends with unconsumed items in her pocket.

### Two ways to gather, both valid

Opportunistic gathering (a standing order, a little time at every node) and a deliberate gathering stop (a route entry, a lot of time once) are different strategies with different shapes. Topping up as she goes suits a long tour; one big fill suits a spike run heading straight somewhere deep. Which wins depends on the map and the build, which is exactly what the pairing should produce.

### Realm mastery and adaptive speed

**\[BUILT — differs\] Fast-forward is earned, not bought globally:** the unlock and the capability-not-a-number idea are in §4; the multiplier comes from play, per room rather than per realm. How it is shown to the player: decisions log 2026-10-01 *Room speed made visible* (not built yet).

**Why this shape.** Speed-up belongs exactly where attention is not needed. A realm known by heart is what the player has watched thirty times; the frontier deserves 1×. Tying fast-forward to the visit counter makes the game **direct the player's attention at whatever is new**, with no speed control to manage. It is also the same fiction as the folding queue — rote corridors blur, new ones do not.

**\[BUILT — differs\] Count visits from the start, so the unlock is retroactive.** By the time the concept is unlocked, several rooms will already have four or five visits, and they should jump immediately. That turns the unlock from *a new thing to grind* into *all that walking was learning something*, and it accelerates Act I into its climax. (The build counts runs worked per room, not visits: `docs/BUILD-STATE.md`.)

**Framing:** she does not acquire it, she *notices* it — catching herself crossing a corridor without looking at it. That fits the stripped-expertise premise: not a new trick, a way of being the hall took from her.

**\[BUILT — differs\] The curve is a power law, not logarithmic.** *Built as a linear ramp, a placeholder: ×1.8 at 4 runs to a ×5 cap at 8 (`LoopSettings.RoomSpeedAfter`, plan 025), because Act I's 7–12 runs would never reach this curve's range.* A log curve spanning 1× to 20× is either too flat mid-range or too steep at the start.

```latex
\text{realmSpeed} = \min\left(\text{cap},\; \text{visits}^{\,p}\right), \qquad p = \frac{\ln(\text{anchorSpeed})}{\ln(\text{anchorVisits})}
```

Anchored at 3× for 6 visits, `p` = \~0.613:

| Visits | 1 | 2 | 4 | 6 | 10 | 25 | 50 | 100 | 133+ |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Speed | 1.0× | 1.5× | 2.3× | **3.0×** | 4.1× | 7.2× | 11× | 16.8× | 20× cap |

Two parameters to tune — the anchor and the cap — with `p` falling out of them. Diminishing per visit throughout.

**Graduating the curve rather than flipping at the end also fixes the repetition problem.** Re-searching is most of Act I's content by time; making the fifth pass visibly faster than the second is the difference between grinding and progressing, and it costs nothing but a formula.

**\[BUILT — differs\] Track visits per node, average into a realm-level speed.** The counter must be per-node because that is where searching happens and where the queue folds, but if speed also changed per-node the pace would flicker room to room and read as a bug. (The build uses per-room speed with no averaging; decisions log 2026-09-29.)

**\[PROPOSED\] The Hall of Mirrors should be masterable too.** It takes the most travel time and is the most repeated space in the game, so a hall that speeds up as she learns it is the largest quality-of-life gain available — and thematically it is the point: she stops being lost in it.

### Three implementation traps

1. **Drain must scale with realm speed.** Everything in game time scales together; only the wall clock changes. If a mastered realm runs at 3× while vitality drains per *real* second, mastered realms become nearly free and the economy tips.
2. **Keep realm speed visually separate from skill speed.** Skill levels make actions take fewer *game* seconds — a balance change. Realm mastery makes those seconds pass faster in *real* time — no balance change. Both read as "faster." Realm speed belongs on the map or the queue block, never in the stats panel.
3. **Anything needing a decision must break the speed-up.** At 15× the player physically cannot react to an interrupt. Build the speed system with a *hold* any event can assert, rather than bolting it on later.

**\[PROPOSED\] Past roughly 10×, stop animating.** A sixty-second sequence resolving in three seconds is unreadable, and a blur of flickering bars looks broken rather than fast. Above the threshold, resolve the block instantly and print one summary line — *The dark: 10 searches, 4 wisps, 38 s*. That is the honest representation of doing something without thinking, and it is less work to render.

### Pacing targets per run

**\[PROPOSED\] The planning-to-watching ratio should invert across the game rather than being fixed.**

|  | Planning | Watching | Why |
| --- | --- | --- | --- |
| **Act I** | \~1 min | \~3–5 min | Few options; nothing mastered, so it all plays at 1× |
| **Act II** | \~3–4 min | \~3–4 min | The classic loop-game balance |
| **Late** | \~6–10 min | \~1–3 min | Large graph to route; mastered content blinks past |

**The risk to watch:** planning-dominant pacing plus 10–20× mastery means a late run can become eight minutes of queue editing and twenty seconds of resolution. That is not a loop, it is a spreadsheet with a submit button, and it loses the dominoes pleasure entirely. Keeping an unmastered frontier in every run is what prevents it.

**\[PROPOSED\] A projected run.** If planning is to carry the late game, the queue editor needs live feedback — estimated run length, where vitality runs out, which walls are reachable. Eight minutes of staring at a static queue is homework; eight minutes of iterating against a readout is absorbing. **Keep the projection imperfect**: it must not know about interrupts or anything undiscovered, or running the plan becomes a formality and the whole game moves into the editor.

### &#91;BUILT\] The run timer shows game time

Late game, wall-clock run time becomes almost entirely the unmastered frontier, so real-time run length stops growing and may shrink. Showing **game time** means the number climbs as the player invests.

**The second reason matters more: benchmarks measured in wall clock would show fake improvement.** A realm at 3× makes the player "30 seconds faster" with no skill gain at all. In game time, a benchmark improving means she genuinely got better. A dev-side wall clock would help balance testing; none exists yet.

### Benchmarks

**\[BUILT\]** On reaching a milestone, the time it took this run against last run. This is §14.19 implemented — mastery is only satisfying if the number is visible and moving, and "you got here 30 seconds sooner" is that, concretely, at the moment it happens.

Four extensions worth building:

- **Show best-ever alongside last-run.** *This run 1:20 · last 1:50 · best 1:15.* Comparing only against the last run makes a deliberately different build read as regression, which punishes exactly the experimentation the design wants. Built for run length (the Summary page's Longest and "Longest yet"); not yet for milestone times.
- **Surface the negative ones.** *Ran dry at 4:20, last run 5:10.* The diagnostic half.
- **Auto-designate rather than authoring a list.** A benchmark is any node she has reached before, fired on first entry this run. Self-limiting, always relevant, no content to maintain.
- **A sparkline, late game.** Time-to-reach-X plotted across runs is the progression made visual, and the one place a chart earns its keep here.

### Legibility: the run report

**\[PROPOSED\]** Diagnosis is the loop. A player who cannot tell *why* a run failed is not being challenged, only confused — and opacity reads as unfairness, which is the failure mode that kills games in this genre.

After every run, show at minimum: which resource bottomed out first and when; drain split between travel, workings and idle time; what the wall required versus what Clara brought; and where the queue stalled or idled.

This is not polish. It is the feedback channel the entire plan-watch-diagnose-revise cycle runs on, and it should exist in the greybox before any art.

## 14. Design Principles

Apply these to every new system and every authored piece of content.

1. **Power levels, not verb puzzles.** Hues are schools of magic with thresholds, not King's Quest verbs applied to objects. A puzzle with one right answer has no growth curve — the player either knows it or does not, and levelling up changes nothing.
2. **Capability, never patience.** Any gate passable by repetition alone breaks the milestone economy. If a wall can be ground past with the wrong build, it is not a wall.
3. **Two to five open options, always.** One is a corridor; zero is a dead end with no way to earn.
4. **Optimisation must not collapse to one answer.** The fun is optimising; the agency is choosing *what* to optimise.
5. **Three timescales of decision.** Moment to moment: which hue, how to answer interrupts. Per run: route, when to enter a realm. Across runs: allocation, which walls to chase. If any layer is empty, the game thins out.
6. **Match the wall to the archetype.** Before authoring a wall, name which build shape it is *for*. Unlabelled walls drift toward stat checks.
7. **Always leave a way to think, not grind.** Where a player might get stuck with nothing earnable, the escape should be knowledge — a translation that reveals how to pass — not repetition.
8. **Prose is a feature.** The genre's writing is uniformly functional. Journal entries, realm text and Roland's scraps are where this project can be unambiguously better than its competitors.
9. **Every build archetype should imply a different route, not just a different number.** If reaching a wall requires the same queue whoever Clara is, the queue is decoration and the build system is a spreadsheet.
10. **The queue must be constrained enough that adding one action means cutting another.** At least two of the four pressure sources in §13 should ship.
11. **Order should matter.** If a queue can be shuffled with no change in outcome, sequencing is not a puzzle.
12. **The player must be able to diagnose a failed run.** Difficulty comes from a hard problem, not a hidden one. Opacity reads as unfairness.
13. **Author the aha.** The genre's strongest pleasure is the moment a player realises something. This does not emerge from numbers. One per realm at minimum.

### Principles from *A Theory of Fun* (Koster)

Koster's thesis: **fun is the feeling of learning a pattern; boredom is what happens when the pattern is exhausted but the game keeps demanding you perform it.**

14. **Each realm must teach a new rule, not just demand a different number.** The largest unaddressed risk in the design. Sixty-three switches do not create sixty-three patterns if they are all the same pattern with different values. If a player groks "check which stat the wall wants, allocate there, route directly" in hour three, then hours 4–50 are executing a solved pattern — Koster's definition of grinding, however elegant the economy.
15. **Expect players to optimise the fun out of the game.** Any stable dominant option — a best filler action, a best build, a best route — will be found and repeated until the pattern is dead. Design against it rather than hoping players self-restrain.
16. **Introduce systems one at a time.** Seven pools, five stats, mastery, currency and persistent switches presented together read as noise, and players quit noise. The wheel-order early game is the right shape; stage the *systems* alongside the realms.
17. **Story is a parallel pleasure, not a fun source.** §14.8 stands as a *differentiator*, not as a substitute for a working loop. **Test the greybox with placeholder text.** If it is not fun with lorem ipsum, the journals will not save it.
18. **Boredom is a signal to stop.** A game should end when its patterns are exhausted. Stretching past the point where the player has learned everything is where people leave with a bad taste. Length is an output, never a target.
19. **Make mastery's effect visible and moving.** Because duration is cost (§5), skill levels *are* the economy — but only if the player can see them working. A chain that visibly goes 85 s → 70 s → 50 s across runs reads as progress; the same improvement applied silently reads as nothing happening.
20. **Every run must deliver at least one of four things.** A new action or room; a permanent unlock that changes the next run; a visible shortening of a chain the player already knows; or a story passage. One dead run is survivable. **Two consecutive dead runs is where players quit** — the most reliable boredom predictor available. Budget an act's length by counting its distinct payoffs and mapping them one per run.
21. **Derive costs from formulas; never hand-tune them.** At the scales this game reaches — base durations spanning six orders of magnitude — hand-set numbers become unmaintainable within an act. **Author the intent, derive the number:** the designer says *this is a tier-4 realm traversal* and the formula yields the duration, the XP and the threshold.

| Hand-tune | Derive |
| --- | --- |
| The ten to fifteen global parameters | Every action's base duration |
| Tier assignment per realm | Every XP value |
| Deliberate exceptions — a boss wall, a story beat | Wall thresholds and traversal charges |

A deliberate exception is fine and sometimes necessary; an *accidental* one is a number nobody can explain six months later.

22. **The scale-invariant rule: base duration tracks the speed multiplier.** Hold `base ÷ speed` roughly constant — an action should feel like 5–20 actual seconds at any point in the game — and three things stay fixed for free: actual durations, so runs stay minutes long and queues stay readable; vitality, drain and run length, which never need to scale at all; and XP per mastery level.

**The original condition was that speed-per-mastery-level and cost-per-mastery-level stay equal**, or mastery stalls or runs away. **Ruling (decisions log 2026-09-27): cost stays steeper on purpose**, \~8.5% against \~1% speed. The slowdown is the soft cap, and meta currency buys mastery levels past it. The risk it leaves is the stall: mastery from XP alone tops out near ×2–×3, and deep content must stay startable (§6, \[OPEN\]).

**Note the asymmetry with run levels:** they reset every run, so they always climb the same bottom stretch of the curve and their XP requirement never grows. Reaching level 30 costs the same in hour one and hour forty. Only *mastery* needs inflating XP, which is why late content must carry inflated base durations — and it is also why levels, not mastery, supply the large multipliers.

### Act I: what it should ask of the player

In the first fifteen minutes the player has almost nothing to choose between, and that is correct. **Choices come from scarcity, and scarcity only exists once the player knows what they are short of.** Act I's job is to make them want things. The arc is: no choices → one choice → a choice that persists.

| Minutes | What is happening | The decision |
| --- | --- | --- |
| **0–5** | Chased, robbed, dropped. Search is the only verb | None. Only revelations |
| **5–15** | Wisps arrive. A rate to beat | **The candle chain**: a large share of a short run spent on something that does not advance her. Invest or push? |
| **15–30** | Left and right corridors, and early runs cannot do both | **Build capacity, or go get the key.** Both correct, neither obviously better |
| **30–45** | Mastery growth visibly slows | Grinding stops working. The meta layer now has a reason to exist |

**The first run must end before the player understands why.** Confusion is fine at 90 seconds and fatal at 10 minutes. This is also why the prologue needs to be long enough to *watch* — see §5.

**Split the branches so they compete.** Right corridor: the ring (needed for the lab, and it bleeds her the whole way) and the phial chain. Left corridor: the dark, the Mirrors count, and the satchel. Early runs can afford one.

**What Act I must not ask:**

- **No build decision before the lab.** No allocation, no pool shape, no loadout. Those need meta currency, which needs the real lab. Inventing a second economy for Act I means deleting it later.
- **No irreversible choices.** The corridor branch is about *this run*, recoverable next run.

**The test for whether Act I works:** at the end of it, the player should be able to answer *what do I wish I had more of?* If the answer is **time**, it is built right. If the answer is *nothing in particular*, the branch is not biting and the candle chain is too cheap.

## 15. Known Risks

**Scope is the real risk, not originality.** 63 switches, seven authored realms and a layered stat economy is a large first project for someone learning to program. The thing most likely to kill this game is not being derivative — it is not shipping.

| Risk | Symptom | Countermeasure |
| --- | --- | --- |
| Balanced always wins | Player never spikes; long runs strictly better | More magnitude and race walls; vitality-spend workings |
| Even spread is mandatory but boring | Optimal play is "keep all seven level" — a chore, not a puzzle | Partial empty-pool penalty instead of a cliff |
| Stat walls multiply | Most gates read "Scholarship 40"; pools barely matter | Tag every wall by type; audit the distribution |
| Persistent switches become the grind | Patience alone opens the hall | Gate every switch behind a capability threshold |
| Frustration with no way forward | Player stuck, earning nothing | Two to five open walls; translations as the knowledge escape |
| Act I outstays its welcome | 45–90 minutes before the first build decision | Move the first pathos pool earlier — give her Amber at the lab rather than after it. \[OPEN\] This contradicts §5's pools taken in the prologue and recovered later; not yet ruled on |
| The mastery treadmill stalls | Runs stop getting faster; grinding pays nothing | Meta currency buys mastery levels; keep deep content startable (§6, open). Flattening the XP curve was rejected (2026-09-27) |

**The unresolved mechanic:** the rival reflection (§2) has no current integration. It is the strongest differentiator discussed and the natural partner to a no-grind economy, because it punishes running the same optimised script twice. Currently tabled.

## 16. Build Approach

**Engine decisions settled:**

- **Ticks, not real seconds.** Drain and events resolve per tick; tick rate is a speed multiplier. Decouples game time from frame rate and makes speed controls and saves trivial. Pacing comes from durations, stats and mastery, never the tick rate.
- **The run ends when vitality hits zero**, or when the player ends it.
- **Nodes as data from day one.**
- **Pools can be added at runtime.** Never assume seven.
- **Balance numbers live in the assets**, edited in the Balance Sheet, never in code.
- **Greybox before anything else.** Text and bars, to prove the loop. Art and any visual polish layer on afterwards; the underlying code should barely change.

### Production stages

A production plan, not the in-game milestones.

| Stage | Scope | Done when |
| --- | --- | --- |
| **1. Greybox Act I** — *current* | A text-and-bars Act I someone else can play start to finish | A friend has played it cold and the loop has been judged fun |
| **2. Vertical slice** | Prologue, Hub and the first realm (Amber) | The milestone economy and persistent switches are interesting in plain text |
| **3. Demo** | 1–2 hours with first-pass art and sound | Steam page up, wishlists accumulating |
| **4. Full game** | See §3 on length | — |

**Stage 2 is a pass/fail test, not a demo.** If the milestone economy and the persistent switches are not interesting with no art, the design changes then — not after seven realms are built.

**Currently tabled:** Shadow, Clara's reflection, combination magic, multiple endings.

**\[PROPOSED\]** Plan for Steam's AI-content disclosure if generated art is used.

## 17. Open Questions

Ordered by what they block.

### Blocking the next piece of work

1. **The UI and queue rework** (§13, §13a). Shape settled in `docs/plans/finished/ui-rework-roadmap.md`; open details (the inventory, whether the combined story panel works) are settled as each part is planned.
2. **Is mid-run editing free, limited, or costly?** (§13) It stays (live play for unmastered content); whether it costs anything is open.
3. **End of queue: pause, loop-the-last, or a per-queue flag?** (§13) Cheap while the queue code is being rewritten anyway.
4. **How deep content stays startable with steep mastery growth** (§6, §14.22). Growth stays steep (ruled 2026-09-27); candidates: meta-bought levels, shorter deep base durations, a faster early level ramp.
5. **The prologue's length** (§5). Three seconds cannot show seven bars draining, which is the one thing it exists to do.

### Needed before pools are built

6. **The empty-pool penalty: cliff or partial?** (§5) The mechanical heart of the archetype system.
7. **How does the player set pool shape?** (§7) Deferred until playtesting reaches meta currency, but it blocks the whole archetype layer. *Material now in `future-sketches.md`.*
8. **Where does raw magnitude live** — a stat Clara levels, or purely a build choice? (§6)
9. **Does stat opposition ship** (§6), or do all five stats stay strictly good? Not for a long while.
10. **Does the meta layer buy carry capacity at all**, now that in-run containers do the job? (§5)

### Needed before content authoring

11. **Does each realm teach a new rule?** (§14.14) The largest risk in the design. Nothing else matters if the pattern is solved in hour three. *Material now in `future-sketches.md`.*
12. Rewrite the realm sketches against the Amber → Ruby order. (§12)
13. What new rule does each of the seven realms teach? (§14.14) *Material now in `future-sketches.md`.*
14. Where is the authored *aha* in each realm? (§14.13) *Material now in `future-sketches.md`.*
15. In what order do the systems themselves unlock? (§14.16)
16. How many persistent world switches per realm? Working target: two or three. (§11)
17. **Is the rival reflection in or out?** (§2, §15) Strongest differentiator; currently tabled, which is a decision but not a recorded one.
18. What proportion of walls should be of each type? (§8) Premature until the first realm is played. *Material now in `future-sketches.md`.*
19. Which events are interrupts, how often, and can defaults be pre-set?

### Deferrable to playtesting

20. Every number in §5, all of them placeholders: max vitality, drain, growth rate, and whether 85 seconds unassisted is the right floor.
21. Traversal charge parameters and overflow severity. (§12)
22. What goes in the run report beyond the minimum. (§13a)
23. Name of the meta currency. (Codex candidate: vitalium gems.)
24. Target length. (§3)
25. What New Game+ changes.
26. Platform.
27. Character names: keep the book's or make them adjacent.
28. **Is the ring's bleed survivable end to end?** (§11) Only play answers it.
