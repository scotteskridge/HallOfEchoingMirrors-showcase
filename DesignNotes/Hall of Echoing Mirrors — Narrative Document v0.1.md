# Hall of Echoing Mirrors — Narrative Document v0.1

Sep 24, 2026 · @Scott

*Companion to GDD v0.3. Story only — mechanics live in the GDD. Nothing here is firmly decided; the project is in greyboxing.*

## 1. Status & Scope

**Nothing here is firmly decided.** The project is in greyboxing. Act I is worked out in detail because it is being built now; Act II is deliberately left open; Act III and the endings are sketched to the level needed to aim at them.

**Relationship to the other documents:**

| Document | Holds |
| --- | --- |
| **GDD v0.3** | Mechanics, economy, principles, open design questions |
| **This document** | Story, characters, beats, endings |
| **Eromancy Codex** | The magic system as established in the books. Canonical for hues and their meanings |
| **Story text files** | The passages served to the player when in-game conditions are met |

Where this document and the GDD disagree, flag it rather than working around it. Where either disagrees with the Codex on how the magic behaves, the Codex wins.

**Target length revised:** 10–20 hours per playthrough, down from the 40–60 in GDD §3. This is more honest about scope and about when the game's patterns run out.

## 2. Premise & Timeframe

**\[DECIDED 2026-09-29\] Clara is 41.** She was 20 in the novel; the game is set twenty-one years later. She and Roland married and have children, and she is a practised eromancer. He is taken, and she goes in after him.

### Why not young Clara

A Clara in her twenties, a year or two after the book, wants one thing and never wavers. That is a vector, not an arc. She rescues him because she loves him, and nothing in the story can pull against it.

**What age buys:**

- **She has something to lose.** A marriage already tested, children who need a mother more than they need a father returned, and twenty years of her own work. She has a life that does not require him, and she risks it anyway. That is a choice, and choices are what stories are made of.
- **Her reflection gets a devastating argument.** To a girl, "you are lonely" is thin. To a woman twenty-one years into a marriage: *you are not saving him, you are refusing to lose him. You have had your life together. Go home to your children.* That is hard to answer.
- **The memory mechanic becomes the emotional centre.** The realms are a young woman's life seen from twenty-one years on. Walking into the night they cut the gem, as a woman of 41, meeting a Roland who sees his student — that is devastating in a way it simply cannot be if the memory is from last spring.
- **It resolves the book's frame.** The mad narrator writes to a wife he has loved "from before I was born," signed R. Wardmont, with a postscript about a man who walked too deep and lost his name. If she is his wife, the letter is to her.

### The line the game is built on

> She has spent twenty-one years with this man. She goes into a hall that shows her the year they met — and he does not know her.

### What it costs

- **The romance is a marriage, not a courtship.** Readers who came for the courtship will not get a second one directly. Mitigation: the memories *are* the courtship. The realms let the player walk back through it, which may serve that audience better than repeating it would.
- **A protagonist of 41 is rarer in the genre** than a young one. Frame it as experience: she is not weaker. She knows exactly what everything costs.

## 3. What Clara Wants

**The problem this section solves:** in the book she wanted something she could not have, and the engine was watching her get it. In the game she already has it. A protagonist who wants only one thing, and has already got it, cannot be torn — and a story where nobody is torn stays an outline.

She needs a second want that **conflicts with rescuing him.** Three candidates, drawn from the book:

**1. To be his equal, not his student.** *(Strongest.)* The book ends with her still "Miss Brytwell" outside the walls, on five shillings a week, in his household — while possessing direct sight no text records and the ability to read pages he cannot see. Going into the hall alone, doing what he could not, is her seizing what the arrangement never gave her.

This conflicts productively: if she saves him, is she the student again? If she becomes the greater practitioner, what happens to them?

**2. To be the one who gives.** She spent the book being provided for — room, board, wardrobe, and wages her own father was secretly paying. Maroon is selfless sacrifice, and it is the mechanic behind spending vitality (GDD §5). She may want to repay him badly enough to overspend. **A character flaw with a health bar attached.**

**3. To refuse the arithmetic.** Every man of her generation was taken by the war. He was the one who was not. There is a Clara who will not accept losing him that is less about love than about refusing the sum that took everyone else's.

### The flaw

**\[OPEN\]** Does Clara enter as a rescuer, or as someone with something to prove?

The first is simpler and safer. The second gives her a flaw, gives her reflection ammunition, and produces an ending where she must choose between the man and the mastery. **Recommend the second.**

### Where the wanting lives mechanically

The design already contains the places where character could live, and they are currently empty:

- The two run endings — taken, or walking out (GDD §5, §11)
- Gathering honestly versus holding back (GDD §10)
- The Shadow track

Each is a choice with a cost. Each should be a place where what she wants beyond Roland shows.

## 4. Roland and the Reflections

### Roland

**\[OPEN\] The current weakness: he is cargo.** Taken in minute one, then a destination. Every beat is Clara pursuing an absence.

He needs to be *doing* something in there, and she needs to keep finding evidence of it. The book supplies the material: **his letter taught Harlow to harvest, and people in almshouses died of it.** That is not abstract guilt. It is a specific thing he did, and he has carried it for twenty-one years.

**\[PROPOSED\] He may not have been taken at all.** A man carrying that, who finds a place offering to take his feelings away, might have gone willingly. What Clara saw in the prologue could be a man being escorted somewhere he had agreed to go. This turns the rescue from a task into a question, and the ending into a negotiation with a man who does not want saving.

### Roland's dark reflection

The guilt given form. Per GDD §11 it cannot be destroyed without destroying him; the resolution is integration, not defeat.

Its material is concrete: the letter, Harlow, the almshouse dead, and every student he taught who went on to harvest.

### Clara's reflection

**\[DECIDED\] It steals her gem in the prologue.** This is the antagonist established in the first ninety seconds rather than revealed late, and it explains the empty pools with an event rather than a rule.

**\[PROPOSED\] It must have an argument, not just a grudge.** A thief with her face is an obstacle; a reflection that is *partly right* is a scene. Its case:

> You already got everything you wanted, and it was not enough — so here you are, burning yourself for more. You are not saving him. You are refusing to lose him.

And, from the book's own facts:

> You are here to save a man who killed people by being helpful.

If the player cannot immediately dismiss it, the story is working.

### Voice notes

- **Clara:** first person, journal. Older now — dry where she was earnest, and precise about cost.
- **Roland:** clipped, audit-toned, self-address as "Wardmont." Per GDD §10.
- **The hall:** never speaks in its own voice. It speaks through reflections and through what it chooses to show.

## 5. Prologue and Acts I–II — Beat by Beat

The most worked-out part of the story, because it is being built now. **One lesson per loop** — the systems stage in one at a time (GDD §14.16).

**\[DECIDED\] Act boundaries.** These are the labels the code should use for triggers, so that act breaks are not guessed at:

|  | Loops | What it is | Target time |
| --- | --- | --- | --- |
| **Prologue** | 1 | Click start, enter, fail, get served story. The only part that is not a game | \~90 seconds |
| **Act I** | 2–4 | Learning the interface: that exploration exists, that verbs exist, that objects exist. **No magic at all** | \~5 minutes played optimally |
| **Act II** | 5 onward | The game proper unlocks: first pool, meta currency, the hub | — |

**Two consequences of this structure.**

**Act I must stay short.** Everything distinctive about this game — pools, allocation, walls, builds — is gated behind Act II. A long Act I asks players to sit through the least distinctive part before reaching the distinctive part. Four loops, and out.

**Act II carries six systems** — first pool, first gather, first working, meta currency, the hub, extraction. That is too many to open at once. Act II needs internal beats of its own, staged in the same one-at-a-time way.

### Prologue — loop 1 (scripted)

Clara enters with her anchor gem **full: all seven pools**. She chases Roland, who is being dragged by a reflection wearing his face. Every action drains all seven pools visibly and fast.

When they empty she collapses. Her last sight is **her own reflection bending over her to take the gem.**

She wakes on the **mirror side** of the lab, looking out, with no way back.

**Purpose:** the player must *see* seven full bars before they are taken. That is what makes wanting them back legible. The prologue must be short — ninety seconds, not ten minutes.

### Act I, loops 2–3 — exploration, no pools

Only vitality remains. Drain is action-driven, so vitality falls as she explores.

**Two requirements, or these loops are filler:**

- **Exploration must present choices.** At minimum two corridors she cannot both search in one run. Without this the only verb is Explore and more is strictly better.
- **Each loop ends with a discovery, not a counter.** Loop 2: the dark with the hanging mirrors. Loop 3: some mirrors show her life, some show his.

**Roland's ring lies in plain sight at a fixed node.** It never moves. It is always re-findable.

### Act I, loop 4 — the lab she recognises

She finds a reflection of the lab and briefly fears she has walked in a circle. Through it: herself and Roland bent over the magic book. **Her memory of crafting her own gem.**

### Act II, loops 5–10 — the game proper

| Loop | Beat | Teaches |
| --- | --- | --- |
| **5** | The lab memory is visible but sealed. Entry requires the ring. | Mirrors are doors with conditions |
| **6** | Inside, the memory is *wrong* — a detail missing or stopped. She corrects it. | The correction verb |
| **7** | She crafts the mirror gem. **One pool (Amber), empty.** | The first bar appears |
| **8** | The empty pool sends her to a memory of a meeting. | Gather |
| **9** | With Amber, her first working — adhesion, used on something in the hall itself. | Work |
| **10** | The mirror lab becomes usable as an exit. | Extraction, and the first item |

### The ring — Act I's authored aha

The player is *expected to drop it.* It costs vitality to carry — an empty gem draws on whatever feeling is nearest, and in loops 2–3 the only thing available is Clara herself. There is a second layer available: it is the instrument he took off in her presence as a courtesy. Carrying it keeps her feeling him, which is exactly what the hall eats.

**The design requirement:** the refusal must be *a choice the player made*, never something they missed. Put it directly in the path, show the cost plainly, and let them decide it is not worth the vitality. At loop 5 they get to be wrong about it.

Tuning target: a run that would cover ten corridors covers about seven.

**Later inversion (design intent, not needed yet):** once Clara has pools, the ring draws from those instead of vitality and becomes cheap. Eventually it becomes useful rather than costly. The object the player resented in hour one becomes something they rely on.

## 6. The Memory Rules

**A memory is not a recording.** If Clara only watches, there is one verb and it is dead. The hall has twisted every memory: stopped it, set it repeating, let it decay, or falsified it. She is not reviewing. She is fixing something that resists.

### Verbs inside a memory

| Against | Verbs | Notes |
| --- | --- | --- |
| **The room** | The hue's effect on matter | Amber: bind, unstick, separate. Per the Codex |
| **Reflections** | Listen, Console, Ask, Refuse, Take | The Codex's three transfer modes, as three different costs |
| **The memory's accuracy** | Correct what the hall altered | Requires Clara to have been there |
| **Herself** | Gather honestly or hold back | Honest yields more and hurts more |

**The third is the richest.** A hall of reflections that lies, and a woman whose only weapon is having actually been there, is a premise with a thesis in it. It is also mechanically cheap: a memory with three details, one of them wrong, is a data structure, not a scripted scene.

### Who can see her

**\[PROPOSED\] Reflections can see her, but mistake her for who she was then.** Roland-in-the-memory sees his student, not the woman who came back twenty-one years later to pull him out. He answers — but he answers the wrong question, because he does not know what has happened since.

To get what she needs, **she has to play the part she had then.** That is painful, which is exactly what should power a magic system that runs on feeling. Gathering attraction from a memory of being drawn to him means being drawn to him again, as a woman pretending to be a girl who did not know yet.

**Late-game escalation available for free:** somewhere deep in the game, one of them notices she is wrong.

**\[PROPOSED\] The rule: a memory only perceives her if she was in it.** Roland's memories cannot see her. She can only watch and gather — a different verb set, not just different flavour. The player learns this the first time they walk into one of his and nobody looks up.

### Node types — roughly a third should not be memories

If every node is a memory the form flattens and the per-realm *aha* gets harder to find.

- **Hall nodes:** junctions, dead ends, doors. No content. These make routing legible — a graph where every node has a scene is one the player cannot read at a glance.
- **Constructed nodes:** the hall's own architecture. The mirror lab. Where it stores things. Where it feeds.
- **Clara's memories:** full verb set, including correction.
- **Roland's memories:** watch and gather only. She learns things he never told her.

## 7. Act II — NEEDS FLESHING OUT

**This section is deliberately empty.** The middle cannot be written until the node graph exists and the realms have their rules. What follows is the set of constraints any Act II must satisfy, not the content itself.

### Constraints

**Structure.** Six realms between the mirror lab and the heart. Wheel order: Amber, Citrine, Emerald, Sapphire, Iolite, Amethyst, with Ruby last. Clara's memories in the first three, Roland's in the next three, per GDD §12.

**Each realm must teach a new rule, not just demand a different number.** GDD §14.14, and the largest risk in the whole design. The realm's hue supplies the rule; the memory supplies the setting. Until each realm has a rule, Act II should not be written.

**The turn from Clara's memories to Roland's is the act's hinge.** Her realms let her correct. His only let her watch. She stops being an author of the past and becomes a witness to a life she was not in — and what she witnesses is the letter, Harlow, and what followed.

**Something must pull against the rescue.** Per §3, she needs a second want. Act II is where it should cost her something. If every beat is "go deeper, get closer," the middle is a corridor.

**Her reflection must appear and must have a case.** Not as an obstacle first and an argument later — it should be arguing from the start, and getting better at it.

### The realm sketches in GDD v0.2 are stale

They were written against a Red-first order. Ruby is now realm seven. They must be reassigned before any realm is authored.

### The twist to build Act II around

See §10. **The memories are getting more accurate, not less.** If that is the load-bearing twist, Act II is where the player's confidence in the correction verb is built and then broken.

## 8. Act III — The Confrontation

**\[PROPOSED\] Three things must resolve, in this order.**

**1. Clara faces her reflection.** It has her gem, her face, and it has been in the hall longer. Its case is not a threat, it is an argument — that she has had her life, that she is not saving him but refusing to lose him, that she is burning herself because what she got was not enough.

Resolution is **rejection**, per GDD §11. But rejection only means something if the argument was good.

**2. She reaches Roland, and he may not want to be reached.** Per §4, he may have gone willingly. A man who has carried the almshouse dead for twenty-one years, offered a place that takes feeling away, is not obviously a victim. The rescue becomes a negotiation.

**3. She helps him face his dark reflection.** Resolution is **integration**, not defeat — it cannot be destroyed without destroying him. The thematic contrast holds: her arc is rejection, his is integration. Some parts of yourself you leave behind; some you must take back.

### The two Rolands

**\[OPEN, and the strongest available late twist.\]** The book's frame describes a man writing from inside the Halls to a wife he has loved "from before I was born," and a postscript about someone who walked too deep and lost his name. That is not ambiguous: **some version of Roland has been in there a long time.**

If both exist, the ending is not *save him or don't*. It is **which one.**

- The one taken last week is hers — the husband, with the life attached.
- The one who has been in there long enough to write that letter knows her better. He has had nothing to do but remember her.

They cannot both come out. That is a genuinely hard choice, and it makes the frame of the book pay off rather than sit as decoration.

**\[OPEN\]** Whether to build this. It roughly doubles the weight of Act III and requires seeding across Act II.

## 9. Endings

**\[DECIDED\] Build one ending well.** Four half-written endings do not ship; one good one does. Structure the code so the others slot in later.

### The one to build first: The Keeper

Clara stays so that Roland goes. **The prologue inverted** — he gave up his escape for her; she gives up hers for him. The mad narrator's letter from the book's frame turns out to be hers.

Why this one:

- It completes a structure the book already started.
- It is the only ending that makes the frame pay off.
- If the game ships with exactly one ending, this is the one that means something.

**Important distinction.** The *What Dreams May Come* reference is worth being precise about: Chris chooses to stay, and **the choice itself is what breaks Annie out.** The sacrifice works. That is very different from two people dissolving together, which makes forty hours of striving pointless and tends to read as cruelty mistaken for depth.

The Keeper should be a sacrifice that *works*, not a shared fade.

### The others, for later

| Ending | Shape | Gate |
| --- | --- | --- |
| **Communion** (light) | Both out, both intact | Low Shadow only. Earned and narrow |
| **Diminished** | He returns, but something is missing. They have years, and they are not the years she wanted | Middle Shadow |
| **Taken** (fail) | Shadow maxed. Her reflection walks out into her life. The last thing the player sees is it meeting her children | High Shadow |

**Taken is what makes Shadow matter.** It is also the only genuinely horrifying ending, and it should not be reachable by accident.

### Position on tone

**Do not make the bleak ending the "true" one.** The audience came for a love story, the tone note is longing and devotion, and a game that punishes completion with despair reads as the designer mistaking cruelty for depth.

The happy ending should be **real but narrow** — reachable only without taking, without dark shortcuts, without hollowing herself. That she *could* have lost everything is what makes the win land.

**\[OPEN\]** Should every ending cost something, or can the player get everything? A romance audience wants the latter; a literary instinct wants the former. Decide before Act III is written.

### New Game+

**\[OPEN, and the current plan has a flaw.\]** Replaying faster with extra meta currency means the second run is the first run with less friction — the pattern is solved and the player is being charged hours to re-execute it for a different cutscene. Worse, more currency raises mastery caps, so fewer walls block: NG+ makes the game *easier*, removing the very thing that made run one interesting.

Two better shapes:

- **Carry knowledge, not power.** She remembers. Realms open in a different order, corrected memories stay corrected, the ring is already understood. The replay is shorter because the player knows things, not because the numbers are bigger.
- **NG+ is a different question.** Run one: *can I get him out.* Run two: *which one do I get out*, or *what will I spend to keep him whole.* The same hall with a different problem in it.

**Recommendation: defer entirely.** NG+ is a post-launch question best answered with player data rather than speculation.

## 10. Twists

Six available. They are not a single chain — take what fits.

### The one to build around

**The memories are getting more accurate, not less.** She assumes the hall is falsifying her past. Then she corrects a detail — and is wrong. The hall's version was accurate and hers was not.

This is the load-bearing twist because **it is the only one that changes how the game plays rather than how the story reads.** It attacks the verb: her whole power is having been there, and memory twenty-one years on is not a reliable instrument. Once the correction mechanic cannot be trusted, every subsequent memory is tense.

It also arms her reflection: *you do not remember him. You remember what you have told yourself about him for twenty-one years.*

### The rest, roughly in order of when they would land

**Early — the ring is a leash, not a key.** The instrument that reads hue also lets the hall read *her*. Every time she carries it, it learns what she feels and shapes the next memory accordingly. The memories get more precisely painful because she has been telling it what hurts. Mechanically this is already half-built; making the tax informational costs nothing.

**Mid — what the reflection did with the gem.** It took a full anchor and walked out. So: did it? Twenty-one years together, and a wife who could see threads no text records. There is a version where the prologue's theft was not the first one. **Handle with care** — done badly this retroactively cheapens the book.

**Late — Roland was not taken, he went.** See §4.

**Late — the two Rolands.** See §8.

**Last — the letter is hers to write.** If Clara stays (the Keeper ending), the mad narrator writing from the Halls to a beloved was never Roland, or was both of them, or the hall has held whoever chose to stay and the letter has been rewritten many times. This reframes the book's frame as a trap that has caught more than one person — a stronger horror than *a man got lost.*

## 11. Narrative Delivery

**\[DECIDED\]** Primarily first person: Clara's journal entries, and scraps of Roland's journals found in the hub and the realms.

### Story is the pacing layer

**\[DECIDED\] Text must be served continually during exploration, not only at story beats.** While bars fill passively the player needs something to do, and in this game that something is reading. This is a structural requirement, not a flourish.

What it implies for how the passages are written:

- **Many short passages, not a few long ones.** A steady drip that matches the rate at which bars move. A wall of text at a milestone leaves the minutes between milestones empty.
- **Most of them are small observations,** not plot. What a corridor smells like. What she notices about a frame. What she remembers without meaning to. Plot beats are the rare punctuation.
- **The rate should scale with the action.** A long traversal earns more text than a quick search. If the bar is moving, something should be arriving.
- **They must not block.** Reading is optional; the run continues. A player who ignores the text is not punished, and a player who reads all of it never has to pause.
- **Act I depends on this most.** Loops 2–3 have almost no mechanics. If exploration is silent there, it is a progress bar with no game attached.

This makes the text volume much larger than a conventional narrative game — closer to ambient writing than to cutscenes. Worth knowing before the files are written, because it changes what kind of writing job this is.

### Story text files

Passages live in separate `.txt` files and are served when in-game conditions are met. Conventions to settle before many are written:

**Each file needs a trigger, and the trigger needs to be data.** A passage fires on a condition — loop count, a switch flipped, a node first entered, an item carried, a Shadow threshold. That condition belongs in the file's metadata or an index, not in code.

**One passage, one beat.** A file that covers three things cannot be re-ordered when the design shifts, and the design will shift.

**Fire-once versus repeatable.** Most passages fire once. Some should be re-readable from the journal. Decide which at write time.

**Suggested metadata header** for each file: an id, the trigger condition, whether it fires once, whose voice it is (Clara / Roland / neither), and which act it belongs to. Whatever the shape, keep it machine-readable — this is what lets the passages be audited as a set.

### Prose notes

- **Placeholder names calcify.** "Ember Hearth" and the rest of the v0.2 switch names were invented as structure fillers; they will survive to ship if nothing replaces them. The vocabulary layer — action names, node names, UI labels, what Clara calls things in her journal — is a pass worth taking deliberately.
- **The prose is the differentiator** (GDD §14.8). It is also *not a fun source* (GDD §14.17). Both are true: test the greybox with placeholder text, and if it is not fun with lorem ipsum, the journals will not save it.
- **Do not explain the magic.** The Codex is explicit: agency is shown, never lectured. Freely given eros reads luminous; taken eros reads corrupt. No character pauses to explain the ethics.

## 12. Open Narrative Questions

**Blocking — decide before Act II is written**

1. ~~Is Clara old?~~ **Settled 2026-09-29:** she is 41, twenty-one years after the novel (§2).
2. **What does she want besides Roland?** (§3) Recommend: to be his equal, not his student.
3. **Does she enter as a rescuer, or with something to prove?** (§3)
4. **What rule does each realm teach?** (§7) The largest risk in the design.
5. **Was Roland taken, or did he go?** (§4)

**Needed before Act III**

6. Are there two Rolands? (§8) Roughly doubles Act III's weight and needs seeding through Act II.
7. Does the memory-accuracy twist ship? (§10) If yes, Act II must build and then break the player's trust in correction.
8. Should every ending cost something, or can the player get everything? (§9)
9. How far does the reflection's theft go? (§10) The "twenty-one years" version is powerful and risky.

**Deferrable**

10. NG+ shape, or whether to have one at all. (§9)
11. Whether Harlow or his successors appear, or are held for a sequel.
12. The vocabulary pass: action names, node names, journal voice. (§11)
13. Story text file metadata format. (§11)

---

**Decided so far:** Clara's reflection steals the gem in the prologue (§5); one lesson per loop through Act I (§5); the ring is a carried item with an ongoing cost, and refusing it must be a choice rather than an oversight (§5); memories are twisted, not recordings (§6); reflections see her as who she was then (§6); build one ending, and make it the Keeper (§9).
