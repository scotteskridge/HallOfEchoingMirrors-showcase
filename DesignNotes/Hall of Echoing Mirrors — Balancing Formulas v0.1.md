# Hall of Echoing Mirrors — Balancing Formulas v0.2

Sep 27, 2026 · @Scott · **numbers updated to the build 2026-09-30** (the file name keeps "v0.1" so links still work)

*Companion to GDD v0.5. Every number in the game derives from something on this page. Greybox — all values are starting points, not measurements.*

**What changed in v0.2.** The build rescaled game time on 2026-09-29 (durations ×0.5, per-second rates ×2, drain growth 25% → 56.25% a minute): a run holds the same actions and XP in half the seconds. Every number below is the build's, from `LoopSettings.asset` and the task and item assets. Where the build differs from v0.1's intent, the v0.1 value is kept as *(v0.1: …)* so the reasoning still reads. Sections for systems not built yet (traversal escalation, meta currency) keep their v0.1 maths, rescaled where it touches time. The cost curve for new verbs (decisions log 2026-09-30) is added to §2 and §4.

## 1. Purpose

**Author the intent, derive the number.** A designer says *this is a tier-4 realm traversal* and the formulas here produce its duration, its XP, and its threshold. Nobody types a duration.

This exists because the game spans roughly six orders of magnitude in base numbers. Hand-set values become unmaintainable inside a single act, one parameter change should rebalance everything consistently, and the balance model can only predict what a formula produced.

### How to use it

| Hand-tune | Derive |
| --- | --- |
| The globals in §3 | Every action's base duration |
| Tier assignment per realm | Every XP value |
| Deliberate exceptions | Wall thresholds |
|  | Traversal charges, restore amounts, harvest costs |

A deliberate exception is fine and occasionally necessary. An *accidental* one is a number nobody can explain six months later — so when a value is set by hand, record why here.

**Every number below is a starting point, not a measurement.** They are internally consistent, which is the point; whether they are *fun* is a question only playtesting answers.

**Companion documents:** GDD v0.3 for what the systems are and why; the Act I balance model spreadsheet for run-by-run projection; the narrative document for story and pacing.

## 2. The Anchor

One number generates every duration in the game:

```
B(t) = D0 x tierMult^t

  D0        base duration of a tier-0 traversal   = 15s   (the build's Travel; v0.1: 30s before the rescale)
  tierMult  growth per realm tier                 = 2.0   (not built: Act I has no realm tiers yet)
  t         realm tier, 0-6 (Amber through Ruby)
```

| Tier | Realm | B(t) |
| --- | --- | --- |
| 0 | Amber | 15s |
| 1 | Citrine | 30s |
| 2 | Emerald | 60s |
| 3 | Sapphire | 120s |
| 4 | Iolite | 240s |
| 5 | Amethyst | 480s |
| 6 | Ruby | 960s |

**Within a tier: room depth (decisions log 2026-09-30, not built).** `tierMult` only steps between realms, so all of Act I (the hall and the lab) sits inside one step. New verbs default to

```
baseDuration = familyCoeff x B x roomStep^depth

  roomStep  ~1.1 per room   Placeholder rule: from an estimated x1.3-1.5 skill speed on first reaching
                            the lab; replace with a playtest reading (skill chips on first entering it)
  depth     a fixed number set on each room (Smoky Mirror 0 ... lab ~4), never read from the live map (§9)
```

`roomStep` tracks her speed, so ordinary verbs deeper in the hall are only modestly longer (the lab ~1.5x the corridors). **Walls** (heavy once-ever verbs) are sized in runs instead, see §4.

Every action's base duration is a coefficient on `B(t)` (§4). Every XP value follows from the base duration (§5). Every threshold scales by the same `tierMult` (§12).

### Why tierMult = 2.0

It has to track the speed multiplier, and the two must stay in step or everything downstream drifts. If late-game speed reaches roughly 145x from mastery and there are seven tiers, then `tierMult = 145^(1/7) = 2.06`. Two is close enough, and memorable.

**The rule that makes this work: `base / speed` stays roughly constant.** An action should feel like 5-20 actual seconds at any point in the game. A tier-0 traversal at 1.5x speed is 10 actual seconds; a tier-6 traversal at 145x is 6.6. Both fine. *(v0.1, before the rescale: 20 and 13.)*

### What this buys

Because actual durations stay constant, **vitality, the drain curve, and run length never need to scale.** The numbers tuned for Act I remain valid at tier 6. Only the base numbers inflate, and they inflate in lockstep with the speed that divides them.

It also keeps **XP per mastery level constant**, which is what stops the treadmill stalling. See §5.

## 3. Master Parameters

Every tunable global. **If a number in the game is not on this list and not derived from it, that is a bug.** This should map one-to-one onto the balance screen.

*Build* is the value in `LoopSettings.asset` (field name in brackets) or the asset named. *Not built* means the parameter has no field yet.

### Scale

| Name | Build | Effect |
| --- | --- | --- |
| `D0` | 15s (Travel's duration) | Tier-0 base duration *(v0.1: 30s)* |
| `tierMult` | not built | Duration growth per realm tier. **Must track speed growth** |
| `roomStep` | not built, ~1.1 | Duration growth per room depth inside a tier (§2, decided 2026-09-30) |

### The clock

| Name | Build | Effect |
| --- | --- | --- |
| `vitalityMax` | 50 (`vitalityMax`) | Before Endurance (+1 per level, `enduranceVitalityPerLevel`) *(v0.1: 100)* |
| `drainBase` | 1/s (`vitalityDrainPerSecond`) | At t=0 in a run *(v0.1: 0.5/s)* |
| `drainGrowth` | 0.5625 (`drainGrowthPerMinute`) | Per minute, compounding (= 1.25² after the rescale). **The master pacing dial**. Composure multiplies it by 0.97 per level (`composureDrainGrowthPerLevel`) *(v0.1: 0.25)* |

### XP and levels

| Name | Build | Effect |
| --- | --- | --- |
| `xpRate` | 1.5 (`xpPerSecondOfTask`) | XP per base second *(v0.1: 0.75)* |
| `xpRounding` | **ceil** | v0.1 wanted `round` — still open, see §13 |
| `runLevelFirst` | 10 (`xpForFirstLevel`) | XP for run level 1 |
| `runLevelGrowth` | 0.085 (`xpGrowthPerLevel` 1.085, `xpMinIncreasePerLevel` 0) | **Must be >= `runLevelSpeed`** or the XP loop runs away *(v0.1: 0.08)* |
| `runLevelSpeed` | 0.05 (`skillSpeedPerLevel` 1.05) | Speed per run level |
| `masteryFirst` | 20 (`masteryXpForFirstLevel`) | XP for mastery level 1 |
| `masteryGrowth` | 0.085 (reuses `xpGrowthPerLevel`) | **Differs from v0.1 (0.01) by ruling** (decisions log 2026-09-27, GDD §6): steep on purpose, the soft cap. Breaks I2, see §12 |
| `masterySpeed` | 0.01 (`skillMasterySpeedPerLevel` 1.01) | Speed per mastery level |
| `masteryShare` | 1 | Mastery gets this share of every XP earned |
| `masteryMaxLevel` | 150 | A hard cap v0.1 did not have; decided to be removed for soft caps (decisions log, older), not built |
| `statSkillXpPerLevel` | 0.05 | Each level of a skill's paired stat adds 5% to its XP |

### Traversal

| Name | Build | Effect |
| --- | --- | --- |
| `chargeBase` | 5, split across the pools, no growth (Travel is Always Charged) | Vitality for the first traversal of a run. **Decided 2026-09-30:** a flat vitality charge growing per move this run; not built |
| `chargeGrowth` | not built, ~1.2 | Compounding per traversal, resets each run |
| `hallMultiplier` | not built, ~2.0 | Applied to hall edges (open) |

### Restoration

| Name | Build | Effect |
| --- | --- | --- |
| `restoreTierMult` | 2.0 (intent; two items built) | Restore *rate* doubling per item tier. Wisp 10 over 2.5s (4/s); bottled well 20 over 3.5s (5.7/s) |
| `gatherRatio` | varies: wisp 0.4, bottled well 2.1 | Gather seconds per restore second *(v0.1: 1.2)*, §8 |

### Meta currency

Not built (GDD §4; the meta screen is not designed).

| Name | v0.1 value | Effect |
| --- | --- | --- |
| `harvestGrowth` | 1.25 | Cost escalation per repeat harvest |
| `harvestDecay` | 5 runs | Runs per cost-step recovered |
| `respecCost` | 0 | Free, by design |

### Room speed (replaces realm mastery speed)

Superseded 2026-09-29/30: speed is per room, counted in runs, and unlocked by *Feed your hours to the flames*. §11 keeps v0.1's realm version for reference.

| Name | Build | Effect |
| --- | --- | --- |
| `byHeartRuns` | 4 | Runs worked in a room before it speeds up (x1.8 at 4) |
| `fullSpeedRuns` | 8 | Runs to reach the cap |
| `roomSpeedCap` | 5.0 | Maximum room fast-forward *(v0.1 realm cap: 20)* |

### Capacity

| Name | Build | Effect |
| --- | --- | --- |
| `pocketSlots` | 5 | Shared across all item kinds; the satchel adds more *(v0.1: 15)* |
| `floorSpace` | 10 | Items a room's floor holds; lit candles add 5 |
| `pocketStep` | not built, 5 | Per meta purchase |

### Stats (not in v0.1)

| Name | Build | Effect |
| --- | --- | --- |
| Endurance | +1 max vitality per level; per level she may waste 0.5 of a restoration item and still use it | Also trains from vitality lost (0.05 XP each) |
| Composure | drain growth x0.97 per level | Trains 0.1 XP/s (0.2 while carrying) |
| Perception | each search/explore step counts +10% more per level | Placeholder rule (decisions log 2026-09-29) |
| Scholarship | one extra study per 5 levels | |
| Attunement | every hue costs 2% less per level | Hues aren't spent in Act I, so it does nothing yet (the lab wakes it) |

**About thirty numbers.** That is the whole tuning surface; the stats pass (backlog Next 13) will change the last table.

## 4. Action Costs

**Every verb can carry two costs, and they behave in opposite ways.**

```
baseDuration(family, t) = durationCoeff(family) x B(t)
actualDuration          = baseDuration / skillSpeed
drain incurred          = actualDuration x current drain rate

flatCost(family)        = flatCoeff(family)          <- NOT divided by speed
```

### The principle

**Flat costs live on the vitality scale, which never inflates. Time costs live on the `B(t)` scale, which does.**

That is the whole mechanism. A time cost divides by a growing speed multiplier and eventually vanishes; a flat cost does not. So **each verb's flat-to-time ratio is a deliberate statement about what mastery is allowed to erase.**

- Verbs that *should* become free as she learns the hall get **time only**.
- Verbs that must stay meaningful forever get a **flat component**.

Getting this wrong is invisible until mastery is high, which is the worst moment to discover it. See invariant I7.

**In the build, flat costs are switched off** (`chargeActionCosts` 0, decisions log 2026-09-25): duration is the cost, paid through the drain. Only tasks marked *Always Charged* pay their cost: Chase Roland (300 vitality), Explore (5 vitality a step) and Travel (5, split across the pools). Items can be Always Charged too: Roland's ring drains 1/s while carried.

### The table

*x B* is the intended coefficient; *Build* is what the build's tasks work out to at B = 15s (placeholders, hand-set before this curve existed).

| Family | x B(t) | Build | Flat vitality | Skill | Rationale |
| --- | --- | --- | --- | --- | --- |
| **Traverse** | 1.0 | Travel 15s (1.0) | `5 x 1.2^n` (build: 5 on pools, flat) | Wayfinding | Escalating per traversal — the routing constraint (§9) |
| **Explore** | 0.3 | Search 7.5s a step (0.5) | 0 (build: 5 a step) | Wayfinding | *Should* trivialise; that is the mastery reward |
| **Search** | 0.7 | folded into Explore (done once, ever) | 0 | Wayfinding | Same — a solved room should stop costing |
| **Study** | 0.8 | Study the tome 5s (0.33) | 0 | Studying | Same |
| **Gather** | derived (§8) | wisp 1s, fill a phial 7.5s | 0 | Gathering | Should get cheap; that is the point of §8's curve |
| **Instantiate (small)** | 0.5 | candle 2s (0.13), ring 15s (1.0) | 1 | Instantiate | She lends substance to hold the reflection |
| **Instantiate (large)** | 1.5 | flint 22.5s (1.5), satchel and empty phial 30s (2.0) | 2 | Instantiate | A container, a tool |
| **Work / cast** | 1.0 | Attend 5s, Draw on the mana stone 2.5s | 0 | Invoking | Pathos is already its non-time cost |
| **Sacrifice working** | 1.0 | Feed your hours 40s (2.7) + 2 vitality/s | large | none | Maroon — paying in vitality *is* the mechanic |
| **Assist** | 0.9 | — | 0 | Varies | The light path is slower |
| **Take** | 0.4 | Take the tome 15s (1.0) | 0 (+Shadow) | Varies | The cost is moral, not physical |
| **Correct a memory** | 1.2 | lab placeholders 4-10s (0.3-0.7) | 5-10 | Studying | It costs her something to insist |
| **Harvest a gem** | 1.0 | — | large, escalating | Varies | Never trivial by design (§10) |

**The lab's placeholders are shorter than the corridors' tasks**, the wrong way round; the lab plans (027a-c) should take their numbers from the default curve below.

### Default curve for new verbs (decisions log 2026-09-30, not built)

Three layers:

1. **Ordinary verbs** default to `familyCoeff x B x roomStep^depth` (§2).
2. **Walls** (heavy once-ever verbs: Talk to Roland, Craft the gem) are sized in runs, not by the curve:
   `base ~ 0.6-0.9 x (vitality-seconds left on arriving, on the visit it should first fit) x (skill speed on that visit)`.
   Example: 70s left on visit 3 at x1.5 gives 60-95 base seconds (90-140 XP).
3. **XP stays derived** (§5): a longer verb pays more by itself.

**Per-verb adjustments on top of the default:** a duration multiplier and an XP multiplier, set independently (a verb can cost more and pay less); the existing extra drain per second (as on Feed your hours); and an **escalating charge**.

**The escalating charge** is one rule used twice: a flat vitality charge, Always Charged, that grows with each use this run and resets each run.

- **Travel:** counts every move between rooms (§9).
- **Training verbs** (high XP multiplier): count repeats of that verb. Duration and XP stay the same, so once or twice is worth it and dozens of times costs more than it gives. Skill can't erase it (I7). Start and growth are open (`~`).

### Worked example — the same verb at both ends

|  | Tier 0, skill 1.5x | Tier 6, skill 145x |
| --- | --- | --- |
| Traverse: base | 15s | 960s |
| Traverse: actual | 10.0s | 6.6s |
| Traverse: flat cost | 5-160 vitality | 5-160 vitality |
| Explore: base (at 0.3) | 4.5s | 288s |
| Explore: actual | 3.0s | 2.0s |
| Explore: flat cost | 0 | 0 |

Actual durations barely move across the whole game — that is invariant I3 working. **And the traversal's flat cost is identical at both ends**, which is why routing still matters at 145x while exploring a known room does not.

### Two notes

**Flat costs never scale by tier.** If they did, they would inflate alongside everything else and stay proportionally constant, which defeats the purpose. They stay on the vitality scale all game.

**The Take/Assist gap is a balance requirement, not flavour.** The Codex is explicit that taken eros is faster and dirtier. At 0.4 versus 0.9 the dark path is better than twice as fast, which has to be a real temptation or the Shadow track means nothing.

## 5. XP, Levels and Mastery

```
xp(action) = round(xpRate x baseDuration)          build: ceil(1.5 x baseDuration), unless the task sets its own XP
```

In the build XP is paid as the bar fills (each tick's share), goes in full to mastery too (`masteryShare` 1), and the skill's paired stat adds 5% a level.

XP is earned against the **base** duration, not the actual one. A faster Clara therefore completes more base-seconds per real second and earns XP faster — speed feeds XP feeds speed. That loop is intended, and §12 gives the condition that keeps it from exploding.

### Level cost curves

```
cost(level L)   = first x (1 + growth)^(L-1)
totalXP(L)      = first x ((1+growth)^L - 1) / growth
level(XP)       = ln(1 + XP x growth / first) / ln(1 + growth)
multiplier(L)   = (1 + speed)^L
```

### Run levels — reset every run

`runLevelFirst 10 · runLevelGrowth 0.085 · runLevelSpeed 0.05` (build)

**Run levels never need inflating XP.** They reset, so she always climbs the same bottom stretch: reaching run level 30 costs 1,242 XP in hour one and in hour forty.

| Run level | Total XP to reach | Next level costs | Speed |
| --- | --- | --- | --- |
| 10 | 148 | 21 | 1.63x |
| 20 | 484 | 47 | 2.65x |
| 30 | 1,242 | 107 | 4.32x |
| 40 | 2,957 | 241 | 7.04x |

The growth rate matters enormously though — see the runaway condition in §12. The build's 8.5% against 5% speed is safe (alpha 0.60). The original 1% growth gave **11.5x** in a 300-second run (v0.1 seconds), which was the bug.

### Mastery — persists forever

Build: `masteryFirst 20 · masteryGrowth 0.085 · masterySpeed 0.01 · cap 150`. **This is not v0.1's design** (0.01 growth, no cap): the 2026-09-27 ruling keeps growth steep as the soft cap, and meta currency buys levels past it (GDD §6).

| Mastery | Total XP to reach | Next level costs | Speed |
| --- | --- | --- | --- |
| 20 | 968 | 94 | 1.22x |
| 40 | 5,914 | 482 | 1.49x |
| 60 | 31,198 | 2,463 | 1.82x |
| 80 | 160,455 | 12,589 | 2.22x |
| 100 | 821,222 | 64,354 | 2.70x |
| 150 (cap) | 48.5 million | — | 4.45x |

From XP alone, skill mastery tops out near x2-x3. *(v0.1, at 0.01 growth: mastery 500 for 287,600 XP and 144.8x, no hard cap.)*

### Why mastery pacing stays constant

**Superseded in the build.** This section is v0.1's reasoning for 0.01 growth. At 0.085 growth the ratio below is not fixed: a level costs 8.5% more each time while speed rises 1%, so levels per action fall steeply. How deep content stays startable is **[OPEN]** in GDD §6 (meta-bought mastery, shorter deep durations, or a faster early ramp).

XP per action scales with `B(t)`, which scales with speed. Mastery's level cost also scales with speed, since both grow at 1% per level. The ratio is therefore fixed:

|  | XP per action | Cost of next level | Levels per action |
| --- | --- | --- | --- |
| Mastery 0, tier 0 | 22 | 20 | 1.10 |
| Mastery 500, tier 6 | 3,262 | 2,896 | 1.13 |

That identity is the single most important thing on this page, and §12 states the condition it depends on.

## 6. The Drain

```
drain(T)          = drainBase x (1 + drainGrowth)^(T/60) x reducers
cumulative(0..T)  = drainBase x 60/ln(1+g) x ((1+g)^(T/60) - 1)

runLength(V, I)   solves   cumulative(T) = V + I x T
                  (income I per second; iterate, it converges in 3 passes)
```

Time-based, not action-based. Nothing drains while the game is paused — **time is a resource for the player, not for the character.** She can stand still and think for an hour at no cost.

**But time is no longer the only cost.** Many verbs also carry a flat vitality charge, paid however fast the action completes (§4). The drain is what makes *duration* expensive; flat costs are what keep an action expensive once mastery has made its duration negligible.

Build: `drainBase 1/s · drainGrowth 0.5625 a minute` (v0.1: 0.5/s, 0.25).

| Minute | Drain/s |
| --- | --- |
| 0 | 1.00 |
| 1 | 1.56 |
| 2 | 2.44 |
| 3 | 3.81 |
| 4 | 5.96 |
| 6 | 14.6 |
| 8 | 35.5 |

With no income, 50 vitality lasts **42 seconds**; with all 15 hall candles lit from the start (x0.5 drain), **75 seconds**. Wisps and phials stretch that (§7). *(v0.1's minutes 2-15 correspond to the build's minutes 1-7.5.)*

**Growth compounds, so run length is logarithmic in resources.** Hoarding cannot buy a long run; only a faster gather rate, better item tiers, and reducers can. That is the desired shape — runs inch up rather than exploding the moment a player finds a good item.

### Reducers

Multiplicative, applied to the current drain, permanent for the rest of the run. Growth then continues from the lowered value, so **an early reducer is worth far more than a late one.**

```
candles:  x(1 - (1 - drainAtFull) x min(candlesLit, countsUpTo)/countsUpTo)
build:    A Dark Hall's candles  drainAtFull 0.5, countsUpTo 15  -> x0.5 at 15    (v0.1: x0.8)
```

Per-candle rather than a 15-candle threshold (built): the effect scales continuously, partial progress works, and the batch-completion bug disappears. The corridor's candles don't reduce the drain; ten of them are the need for *Feed your hours*. Later tiers are Sapphire concealment (§5 GDD), stacking multiplicatively.

**Growth rate is the master pacing dial, not item strength.** Making a per-act value is the cleanest way to lengthen runs across the game — flatten the curve rather than piling on stronger restoratives.

## 7. Restoration — the Rate Ladder

**Only one item is consumed at a time.** Income is therefore capped by the consumption rate, no matter how fast she gathers:

```
income = MIN( restore / restoreSeconds ,  restore / (gatherSeconds / speed) )
            consumption cap              gather throughput
```

The left term is a hard ceiling. **An item tier's restore rate sets a maximum run length that no amount of skill can exceed.** That makes restoration tiers — not the drain curve — the thing that paces act length.

### The ladder

Choose the ceiling, derive the item. Rate doubles per tier:

Rescaled to the build's clock (rates x2, times x0.5 from v0.1):

| Tier | Rate | Sustains to | Built item |
| --- | --- | --- | --- |
| 1 | 2.0/s | 93s | — |
| 2 | 4.0/s | 186s (3.1 min) | **Wisp**: 10 over 2.5s |
| 2-3 | 5.7/s | 234s (3.9 min) | **Bottled well** (phial of memory): 20 over 3.5s |
| 3 | 8.0/s | 280s (4.7 min) | — (the lab's dense wisp, decisions log 2026-09-29) |
| 4 | 16.0/s | 373s (6.2 min) | — |
| 5 | 32.0/s | 466s (7.8 min) | — |

```
sustainsTo(rate) = 60 x ln(rate / drainBase) / ln(1 + drainGrowth)     seconds
R                = rate x S                  (pick S for readability, 2-6s)
```

Lit candles halve the drain, which adds another 93s to every row.

**Each doubling buys about a minute and a half more run** (v0.1: three minutes, before the rescale). That is the whole restoration ladder in one sentence, and it is the cleanest lever for pacing an act: decide how long late-act runs should be, and the tier follows.

### Consumption rule

Clara uses an item automatically as soon as none of it would be wasted. **This rule is correct and needs no refinement** — with exponential drain, total run length depends only on total vitality gathered, not on when it is spent. Only overflow waste matters, and the rule prevents exactly that.

Consumption is parallel and costs no queue time, which is why *consume* is not a verb and cannot be a standing order (§13a GDD).

## 8. Gathering

Gather time is not chosen independently — it follows from the restore duration it has to keep pace with:

```
G = S x gatherRatio                          gatherRatio = 1.2
gatherShareNeeded(speed) = gatherRatio / speed
```

**The build does not follow this ratio.** A wisp takes 1s to gather against 2.5s to restore (ratio 0.4), so she is never gather-limited on wisps even at x1. A phial takes 7.5s to fill against 3.5s (ratio 2.1), after a one-off 30s to make the empty phial. The table below is v0.1's intent at 1.2; whether the wisp's 0.4 is too generous is a playtest question.

| Speed | Share of run needed to hold the cap |
| --- | --- |
| 1.0x | 120% — impossible, she is gather-limited |
| 1.2x | 100% |
| 2.4x | 50% |
| 4.0x | 30% |
| 8.0x | 15% |

**This is the "gathering shrinks as she gets faster" behaviour, and it falls out of one ratio.** Early she cannot keep the pipeline full at all; later she tops up in a fraction of the run and everything freed goes to content.

Each crossing point is a moment the shape of a run changes — first she gathers to survive, then to buy time for reducers, then reducers buy time to reach things. **Place those transitions deliberately** (GDD §5); they are the best defence against the pattern going stale.

### Two gathering strategies, both valid

|  | Mechanism | Suits |
| --- | --- | --- |
| **Opportunistic** | A standing order; a little time at every node | A long tour |
| **Deliberate** | A route entry; a lot of time in one place | A spike run heading somewhere deep |

The two systems express the same goal differently, and which wins depends on the map and the build. That is the pairing working as intended.

### Pathos gathering

Same formulas, different currency. **Gathering pathos costs vitality-time** — that is the first real opportunity cost in the game, and the thing that makes Act II's economy different from Act I's. Yield per gather should follow `B(t)` so that deep realms pay proportionally.

## 9. Traversal Economy

**Status:** decided 2026-09-30 (a flat vitality charge growing per move this run, the same escalating-charge rule as training verbs, §4); **not built**. The build's Travel takes 15s and charges a flat 5 split across the pools. The tables below assume `vitalityMax` 100; the build has 50 plus Endurance, which halves the reachable route (about 5-6 moves at 50).

Traversal follows the general two-cost rule in §4. **What is specific to it is the escalation:**

```
vitalityCharge(n) = chargeBase x chargeGrowth^(n-1) x edgeMult
```

`n` counts traversals **this run**, globally across all edge types, reset each run. Duration and drain work exactly as §4 describes.

### Why traversal needs a flat cost at all

Of all the verbs, this is the one that would hurt most if mastery erased it. At 100x Wayfinding the trip home takes 0.3 seconds — and with it goes the walk-out decision, the value of shortcuts, and any reason the early map still exists.

Thematically the charge is not distance but **exposure**: the hall notices her moving, and each crossing draws more attention than the last.

### Charge by traversal

| n | 1 | 3 | 5 | 10 | 15 | 20 |
| --- | --- | --- | --- | --- | --- | --- |
| Cost | 5 | 7 | 10 | 26 | 64 | 159 |

### Maximum route length

Set by the escalation against the vitality pool, which grows only with Endurance:

| Vitality | Max traversals |
| --- | --- |
| 100 | \~8 |
| 500 | \~16 |
| 2,000 | \~24 |

Logarithmic — doubling vitality buys about four more stops. **This makes "how far can she range" a direct consequence of one stat.**

### What it produces, at 6 traversals per realm round trip

|  | Traversals | Charge | Cumulative |
| --- | --- | --- | --- |
| First realm | 1-6 | 50 | 50 |
| Second realm | 7-12 | 148 | 198 |
| Third realm | 13-18 | 443 | 641 |

The second realm costs three times the first; the third nine times. **Act II's "go back to the start and out a different branch" structure is therefore gated by economy rather than by locks**, and opens naturally as vitality grows.

**Emergent tension to protect:** the return legs sit at the top of the curve, so a wide tour often cannot afford to walk out. *Tour widely and probably collapse, dropping gems where you fell — or go focused, gather less, and bank it.* Verify in the model that a two-realm tour can **sometimes** still walk out; if touring always ends in collapse, the choice collapses to one option.

**Shortcuts scale with when they are used** — skipping one traversal saves 6 vitality as traversal 2 and 64 as traversal 15. Placement is a routing decision, not a flat discount.

### Tier is baked, never computed live

Base durations belong to the **realm tier**, assigned once, inherited by edges into and within it. Computing depth from the live graph looks equivalent and is not: the moment a shortcut lands, a deep realm sits two hops from the lab and its durations collapse. The content did not get easier, the route did.

## 10. Meta Currency

```
harvestCost(m, r) = baseCost(tier) x harvestGrowth^(m - floor(r / harvestDecay))

  m  times this milestone has been harvested
  r  runs since it was last harvested
  harvestGrowth 1.25 · harvestDecay 5 runs per step recovered
```

The exponent floors at zero — a milestone never gets cheaper than its first harvest.

| Harvest | at 1.25 | at 1.50 |
| --- | --- | --- |
| 1 | 1.0x | 1.0x |
| 3 | 1.6x | 2.3x |
| 5 | 2.4x | 5.1x |
| 8 | 4.8x | 17x |
| 10 | 7.5x | 38x |

**Use 1.25.** Cost grows exponentially while capability grows roughly logarithmically with play, so the soft cap is real: expect four to six harvests per milestone across a playthrough. At 1.50 it is two or three, which teaches "never repeat" and the player stops coming back.

### Why the decay matters

Without it, compounding fights the goal. Cost only rises, capability growth slows, and every milestone is dead content by hour four. With one cost-step recovered every five runs, a realm untouched for twenty runs has partly recovered and **"check in on the old realms" becomes literally correct rather than aspirational.**

Thematically: the hall is a reservoir, and a place she drained reaccumulates feeling.

### The test

**Farming must be possible but always worse than advancing.** Impossible, and a stuck player has no way out and the no-grind economy is a wall. Equally good, and players will farm, because farming is safe and frontiers are not.

> Set the recharge so that currency-per-run from old realms sits clearly below what a new milestone pays. Check this in the model whenever `harvestDecay` or milestone payouts move.

### Banking

A flipped milestone yields a **gem that occupies pockets** and converts to currency only when carried home. On collapse it drops at that node as a floor stash, recoverable by returning. The player loses the trip, not the reward — which is what prevents permanent, unrecoverable loss under "milestones pay once."

**Show the next cost, not only the current one:** *Harvest: 156 (next: 195)*. Hidden escalation reads as arbitrary punishment; visible escalation is a decision.

## 11. Realm Mastery Speed

**Superseded in the build by room speed** (decisions log 2026-09-29 and 2026-09-30; plans 025 and 029). Each room counts the runs in which she worked in it; from 4 runs it speeds up (x1.8), rising in equal steps to x5 at 8 runs. Nothing speeds up until *Feed your hours to the flames* is done. **Placeholder rule:** linear ramp, because the power law below would never reach its range in Act I's 7-12 runs. Trap 1 still holds: room speed is a clock speed-up, so drain, XP and durations per game second are untouched. The rest of this section is v0.1's realm version, kept for reference.

Wall-clock fast-forward, earned per realm. **Not a balance change** — everything in game time scales together, only the real clock moves.

```
p          = ln(anchorSpeed) / ln(anchorVisits)      = ln(3)/ln(6) = 0.613
realmSpeed = MIN(speedCap, avgVisits ^ p)
```

Two parameters to tune — the anchor and the cap — with `p` falling out of them.

| Visits | 1 | 2 | 4 | 6 | 10 | 25 | 50 | 100 | 133+ |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Speed | 1.0x | 1.5x | 2.3x | **3.0x** | 4.1x | 7.2x | 11x | 16.8x | 20x cap |

**A power law, not a logarithm.** A log curve spanning 1x to 20x is either too flat mid-range or too steep at the start — to reach 20x at 500 visits you need a coefficient that already puts visit 6 above 6x.

### Why it graduates rather than flipping

Re-exploring is most of an act's content by time. Making the fifth pass visibly faster than the second is the difference between grinding and progressing, **and it costs nothing but a formula.**

### Granularity

**Count visits per node; average into a realm-level speed.** The learn counter must be per-node because that is where exploring happens and where the queue folds, but per-node speed would flicker room to room and read as a bug.

### Three implementation traps

1. **Drain must scale with realm speed.** If a mastered realm runs at 3x while vitality drains per *real* second, mastered realms become nearly free and the economy tips.
2. **Keep it visually separate from skill speed.** Skill reduces *game* seconds (a balance change); realm mastery makes those seconds pass faster in *real* time (not one). Realm speed belongs on the map, never in the stats panel.
3. **Any event needing a decision must assert a hold.** At 15x the player cannot react to an interrupt. Build the hold in from the start.

**Above `resolveThreshold` (10x), stop animating** — resolve the block instantly and print one summary line. A sixty-second sequence resolving in three seconds is unreadable, and flickering bars look broken rather than fast.

## 12. Invariants

The relationships that must not drift. **Assert these in code and fail loudly.** Every one of them has a failure mode that is silent, arrives hours later, and is expensive to diagnose.

### I1 — The runaway condition

```
alpha = ln(1 + speedPerLevel) / ln(1 + costGrowthPerLevel)     must be <= 1.0
```

With XP earned against base duration, speed feeds XP feeds speed. Whether that converges depends entirely on this ratio.

| speed/lvl | cost growth | alpha | Speed after a 300s run |
| --- | --- | --- | --- |
| 5% | 1% | 4.90 | **14.1x** — runaway |
| 5% | 3% | 1.65 | 4.3x |
| 5% | 5% | 1.00 | 3.1x |
| **5%** | **8%** | **0.63** | **2.4x** |

**Level costs must grow at least as fast as the speed they buy.** The build's original "+1%, minimum +0.5" made this worse than it looks: the floor gives \~5% growth early, decaying to 1% by level 80 — safe at Act I run lengths, and it **overflows at 900 seconds**. **Fixed in the build:** +8.5%, no minimum; alpha = 0.60.

### I2 — The treadmill condition

```
masterySpeed == masteryGrowth          v0.1: both 0.01
```

**Deliberately broken in the build** (ruling 2026-09-27): mastery growth is 0.085 against 0.01 speed, so XP per mastery level rises steeply and mastery slows to a soft cap near x2-x3 from XP alone. Meta currency is meant to push past it (not built). The consequence below is the reason v0.1 wanted the equality; GDD §6 holds the open question it leaves.

This is what keeps XP-per-mastery-level constant across the whole game. Break the equality and mastery either stalls or accelerates without limit. **If someone later raises mastery speed to 2% "to make progression feel better," every tuned number downstream goes with it.**

### I3 — Scale invariance

```
B(t) / speed(t)  ~ constant       target 5-20 actual seconds
tierMult         ~ speed growth across the same span
```

Holding this fixes actual durations, and therefore fixes run length, drain and vitality — none of which then need to scale at all.

### I4 — Bounded effects stay off level curves

Anything with a small useful range — carry slots, percentage reductions toward zero — must not scale with four-figure levels, or it spends hundreds of levels doing nothing visible. Those belong in the meta layer in whole steps.

### I5 — Farming below advancing

```
currencyPerRun(farming old realms)  <  currencyPerRun(new milestone)
```

Re-check whenever `harvestDecay`, `harvestGrowth` or milestone payouts move.

### I6 — The walk-out stays reachable

A two-realm tour must **sometimes** be able to afford the crossing home. If the traversal charge always exhausts vitality before she gets back, the three-endings design collapses to one.

### I7 — Every verb's flat-to-time ratio is deliberate

```
flatCoeff(family) == 0    means    this verb WILL become free
```

A verb with no flat component *will* be erased by mastery. If that is intended — exploring a room she has learned, searching a solved corridor — good. If it is not, it is a bug, and it will not surface until mastery is high, which is the worst moment to find it.

**When adding any new verb, state which side it is on and why.** The question is not "what should this cost" but "should mastery eventually make this free?"

| Erased by mastery | Permanently relevant |
| --- | --- |
| Explore, Search, Study, Gather | Traverse, Harvest, Sacrifice workings, training repeats (decided 2026-09-30) |
| The overhead of solved content | The decisions that shape a run |

The second column is, in effect, the list of things the late game is *about*.

## 13. Known Bad Numbers in the Build

Found by working the formulas backwards (v0.1, 2026-09-27). Each is a real defect, not a preference. **Status** is the build on 2026-09-30.

| # | Was | Should be | Why | Status |
| --- | --- | --- | --- | --- |
| 1 | Run-level cost "+1%, min +0.5" | **"+8%, min +0.5"** | alpha = 4.9. Safe at 300s only because the +0.5 floor gives \~5% growth early; **overflows at 900s** | **Fixed**: +8.5%, min 0 |
| 2 | Bottled well: 50 over 3s = **16.7/s** | **50 over 25s = 2.0/s** (4.0/s after the rescale) | Tier-5 restore rate sitting in Act I. Permits 24-minute runs against a 3-7 minute target | **Mostly fixed**: 20 over 3.5s = 5.7/s, a little above target |
| 3 | `xpRounding = ceil` | **`round`** | ceil overpays short actions per second — a bias back toward the short-action spam it was meant to remove. At 1.5 XP/s a 1s wisp pays 2 (2.0/s) | **Open**: still ceil |
| 4 | Endurance reduces drain 3%/level | **Raises max vitality** | Max vitality and drain reduction are the same lever; having both made Endurance and Composure interchangeable | **Fixed**: +1 max vitality a level |
| 5 | Composure softens costs | **Slows drain *growth*** | A flat buffer and a flatter curve are different maths. This is what produces the short-run vs long-run build split | **Fixed**: growth x0.97 a level (its old cost-softening fields remain, unused while costs are off) |
| 6 | 15 candles required, 15 pockets, flint takes 1 | **Per-candle lighting, effect scales** | 16 items into 15 slots — the chain cannot complete. Scaling also turns a gate into a dial | **Fixed**: per candle, lit candles don't take pockets |
| 7 | Travel priced as duration only | **Add the flat vitality charge** | Without it, the return trip trivialises at high Wayfinding and the walk-out decision dies | **Decided, not built**: flat 5 on the pools today (§9) |

### Also worth checking

**Roland's ring at 1/s doubles base drain** (0.5/s in v0.1 seconds). Carried from the right corridor to the lab, verify it reads as *a cost she resents* rather than *a run she cannot finish*.

**Explore is Always Charged at 5 vitality a step** on top of its 7.5s: flat cost on a verb §4 says should become free. Intended as a brake while costs are off, or should it go when exploring is done once, ever? *(Flagged 2026-09-30.)*

**Overhead is \~64% of run 1 and falls to \~36% by run 12.** The trend is right; the level matters less than the slope. A flat line would be the real failure.

**Instantiate takes \~47% of Act I XP**, almost all from the 15-candle chain, so it levels roughly twice as fast as Wayfinding — the overhead gets cheap while the content stays expensive. Re-check the split after the candle rework.

## 14. Needs Playtest Data

Everything here is internally consistent. Whether it is *fun* is a separate question, and these are the values no amount of algebra will settle.

### Cannot be derived — must be measured

| Parameter | Current guess | What decides it |
| --- | --- | --- |
| `drainGrowth` | 0.5625 | Whether early runs feel tense or rushed. The master dial |
| `tierMult` | 2.0 | Must track observed speed growth, which depends on how much people actually play |
| `roomStep` | ~1.1 | Skill chips and vitality left on first walking into the lab |
| `gatherRatio` | 1.2 (build: wisp 0.4, phial 2.1) | Whether gathering feels like a chore or a rhythm |
| `chargeGrowth` | 1.2 | Whether a two-realm tour is a stretch or impossible |
| `byHeartRuns` | 4 (replaces `anchorVisits`) | Whether re-running a room reads as learning or grinding |
| Explore coefficient | 0.3 (build 0.5, done once ever) | Explore is now done once per room, ever, so it matters less than v0.1 thought |

### Structural questions still open

**Can she gather while holding a full load of candles?** Decides whether the light chain and the vitality chain are sequential or parallel, and therefore what the corridor branch is actually asking.

**Does the escalation counter reset at the mirror lab?** Would make routing through it mid-run a real reward, but it undercuts the escalation. Default no; possibly a late upgrade.

**Do gems scale in pocket cost by tier?** A tier-6 gem taking 15 slots is a very different run from one taking 1.

**Does `drainGrowth` vary per act?** Flattening the curve is the cleanest way to lengthen late runs without inflating item strength — but it breaks the "one number, all game" simplicity.

### The measurements worth instrumenting now

The dev clock already exists. These four would answer most of the above:

- **Actual duration distribution per action family.** Invariant I3 holds or it does not — this is how you find out.
- **Overhead share per run**, and its slope across runs.
- **Gather share of run time**, plotted against speed. Should fall as `gatherRatio / speed`.
- **Runs per milestone**, which is the real pacing number and the one the GDD's two-dead-runs rule depends on.
