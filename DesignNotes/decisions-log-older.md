# Design Decisions Log (older entries)

Entries from 2026-09-28 and earlier. Newer ones are in `decisions-log.md`; grep this file by topic, do not read it whole.

---

## 2026-09-28: Playtest batch: pausing, full pockets, used-up knowledge (backlog 8c, 8e, 8f, 8g)

- **The queue drives the pause** (8c, the user's call). With nothing queued the game is always paused: Resume stays paused and says why ("Clara needs to know what to do next"), and queuing something ends any pause the game made. The player's own Pause stays until they resume. The game's own stops when a single action is done or a new way is found stay, and queuing ends them. A loaded run stays paused until Resume.
- **With nothing queued, no time passes at all:** no drain, no run clock, no training, no restoring. Refines *vitality drains with time, not actions* (2026-09-25, GDD §5): time only passes while she has something to do. Before, Resume with an empty queue let vitality drain for ever, and every run's first tick drained before the pause.
- **What she makes goes in her pockets, even when full** (8e and 8g, placeholder rule). An action that makes something (kind Instantiate, e.g. pulling Roland's ring from its mirror, or crafting from what it takes) and any one-of-a-kind object push something else out: one of what she has most of, put down on the floor here. Never the same kind or a one-of-a-kind thing. **Carried items can be pushed out** (the user's call): left on a floor they go back to the mirror realm with the run and must be found again. **A repeating make-action may push out one thing per go** until her pockets hold only what it makes: fine for now (the pouch keeps restoratives safe); revisit if playtesting shows a problem. **Gathering (wisps) still overflows to the floor**, so Carry still stops when she's full. Crafting uses up what it takes before giving what it makes, so the freed pocket is used first. Supersedes *Picking up … pushes something out (simple version)* in *One-of-a-kind objects* (2026-09-27): the ring couldn't be pulled from its mirror at all with full pockets.
- **Picking up with full pockets pushes out as many as the whole pile needs** (8e, the user's call), up to the item's own maximum. Supersedes "picking up ordinary items (wisps) pushes nothing out" (2026-09-27).
- **Knowledge that has done its job leaves the pockets overlay** (8f, the user's call). A *Hide Once Used* tick-box on the item (ticked for Mirrors Found): once every switch watching it has flipped, and no action or way needs it, it leaves *Knowledge and progress* and shows beside the title of the room where it's found ("Hanging Mirrors · Mirrors Found 10"). Still kept and saved.
- Settled with the user 2026-09-28 before building.

---

## 2026-09-28: Finishing Part 1: drawer, stats row, one Story box (plan 009)

- **The queue drawer pushes the map up instead of covering it** (backlog 8a, the user's call). The map's bottom edge follows the drawer's top as it slides; the zoom doesn't change, so rooms, text and icons keep their size and the map just shows less. The overlays and the room popover sit inside the map and ride along. Supersedes the roadmap's "overlaying part of the map" (*Settled*, the queue drawer).
- **The stats row replaces the Stats tab** (8b). Each chip keeps its short text ("Wayfinding ×2.10") with a thin blue level bar and a yellow mastery bar under it, and hovering shows the Stats tab's old tooltip (levels and XP). Clicking a chip no longer opens anything; the −/+ toggle stays. Chips wrap onto a new line when the row is full, so the row grows down over the map. The Stats tab and its page are deleted. Supersedes *Clicking a chip opens the drawer's Stats tab* (2026-09-28, *Pockets and stats: the choices*) and GDD §13a's "Expand on click".
- **Story milestones and *Around her* share one scrolling box, headed *Story*** (8d). Oldest at the top; it follows new lines unless scrolled up, and it's cleared at each new run (this run only, as in *The story feed*, 2026-09-24). Milestone cards keep their look (time against last run, opening paragraph, Read more), never fade and are never trimmed; only *Around her* lines fade (beyond the newest 4) and only they are dropped past 150 entries. The "Milestones reached this run appear here" line went, since the box is rarely empty. Supersedes the separate Story and Around her panels (2026-09-27, *Main screen UI rework*: "story and ambient in one panel, to playtest" is now built).
- **Placeholder:** the Story box takes the space both panels had; the user adjusts it in the layout pass.
- **Deleted, not switched off:** the old Actions list, the inventory (Pockets) page and the Stats page, with their tabs, scripts and prefabs. The drawer has only the Queue tab.
- Settled with the user 2026-09-28 before the plan was built.

---

## 2026-09-28: BUILD-STATE split into gameplay and UI

- **`docs/BUILD-STATE.md` covers gameplay** (rules, systems, numbers, content, story; under ~2,000 words) and **`docs/BUILD-STATE-UI.md` covers the screen and controls** (layout, panels, map drawings, popovers, the queue display; under ~1,200 words). The user's call: the single file had grown past 3,000 words, mostly UI rework detail. `/sync-state` writes both; the design chat reads the gameplay file every time and the UI file when designing the interface.
- Both files win over the GDD on what's built, as in the 2026-09-27 entry below. The GDD's label table and *Order of precedence* now name both (supersedes their single-file wording).
- **Claude Code keeps the GDD** (the user's call): the design chat reads it but can't write to it, so its handoff notes are applied by Claude Code.

---

## 2026-09-28: The pockets overlay replaces the Pockets tab (plan 6, first look)

- **The drawer's Pockets tab goes; the overlay on the map shows everything she has** (the user's call, after seeing both). Below what she carries, the overlay has a *Knowledge and progress* part (what isn't an object, this run's and kept), and items in use show their countdowns there. Supersedes *The drawer's Inventory tab becomes Pockets* in the entry below. The tab and page are switched off in the scene, not deleted.
- **The overlay also shows what's on the floor where she is** (the user's call), between what she carries and knowledge: one chip per kind, edged in its kind's colour, under "On the floor here · up to 10 of each". Supersedes *without its Floor rows* in the entry below. Pick up stays in the room popover.
- **The satchel shows on the overlay**, among what she carries, though it takes no pocket (it adds them).
- **The overlay can be dragged anywhere on the map**, like the room popover, and stays where it was left (kept for every save slot). Supersedes the backlog's *switch it off too, or move it*.

---

## 2026-09-28: Pockets and stats: the choices (plan 6)

- **The pockets show as a small-text overlay on the map's left side**, always visible while she's routing, not in the bottom strip (the user's call; supersedes the roadmap's *Inventory* layer 1 and GDD §13a's *Proposed layout* "pockets in the bottom strip"). One chip per kind of item she carries, "Candle 6", edged in the item kind's colour (the map's floor-dot colours), under a "3/5 pockets" heading; the pouch's phials get their own line. Hover a chip for its tooltip.
- **The stats and skills show as one row directly under the vitality bar**, which can be switched on and off. One chip each with its current effect (skills as "Wayfinding ×2.1"; stats as their effect, e.g. max vitality). Clicking a chip opens the drawer's Stats tab, which keeps the level and mastery bars as the expanded view.
- **The drawer's Inventory tab becomes *Pockets***: the full read-only list, without its Floor rows (the floor is on the map and in the room popover). Put down and Pick up stay in the room popover as queued actions.
- **The bottom strip keeps only the bookmark tabs** for now.
- Recommended by Claude and accepted by the user so it can be playtested; to revisit after playing.

---

## 2026-09-28: The room popover (playtest)

- **Actions that can't be done this run leave the room popover** instead of showing greyed: done this run, one of a kind already held, a fully searched room's Search. Actions only missing something they need ("needs 1 Candle") still show, since the queue may make or pick up the thing first. Supersedes *Unavailable actions are shown greyed, with the reason* (2026-09-27). Built as a switch on the popover (*Show Unavailable*) so greying can come back.
- **The popover can be dragged**, and keeps that place relative to the room it's open for, whichever room that is, so it doesn't cover the next room. The place is a player preference, kept on the computer for every save slot. Not in the mockup; the user asked for it after playing.
- **Searching stays every run for now** (2026-09-26 *Searching every run*). The user found it unintuitive; keeping searches for good raises questions (what a search gives each step, once-per-run finds like the satchel, the hall-shifts story), left open for later.

---

## 2026-09-28: Plan 007 (floor on the map) as built

- **A room's floor shows as a row of coloured dots hanging under the room's bottom-left corner**, one per kind of item, ordered Restorative, Light, Tool, Keepsake. Not the bottom-right as first built: Clara's token and name rest there and hid the last dot. Where they sit is a look setting in the Map Style.
- **Hover a dot for "6 Hanging candle on the floor"; a dot swells briefly when its pile grows.** The inventory's Floor rows stay until plan 6, as settled.
- Plan: `docs/plans/finished/007-floor-on-the-map.md`.

---

## 2026-09-28: Roland's ring comes out of a mirror (playtest)

- **Every object in the hall comes out of a mirror.** The rooms are hung with dozens of mirrors; a few hold something close enough to the glass for Clara to reach in and bring out (Instantiate: "pull an object out of a reflection", GDD §9). Nothing starts a run lying on a floor: the user's first report asked for the ring to start on the floor, then settled that this didn't match the design.
- **"Take Roland's ring" becomes "Instantiate Roland's ring"**: an Instantiate action (it trains Attunement and gets quicker with the Instantiate skill), still once per run, still 30 s before the skill. Found the Ring still flips when searching the Right Corridor reaches 66%, and only once ever.
- **Hover text and story-feed lines say so**: placeholder descriptions on the five mirror actions and new `start_instantiate` / `instantiating` lines. The ring's existing prose (story pop-up *The Ring*, `ring_present`) still places it on the floor; drafts that place it behind the glass sit beside it for the user to accept or rewrite.

---

## 2026-09-28: Floor on the map: kinds and dots

- **Each item gets a kind that sets its dot colour on the map** (GDD §13a *Colour the pile pips by kind*). Four kinds for now: **Restorative** (lavender): Wisp, Phial of memory; **Light** (gold): Hanging candle; **Tool** (grey): Flint and steel; **Keepsake** (pale rose): Roland's ring, The tome. Light and Keepsake are additions to the GDD's three; **Pathos** waits until pathos items exist (its dot would take the item's hue). Colours are look settings in the Map Style asset.
- **One dot per kind of item on a room's floor**, the easiest version: the user calls the dots a placeholder.
- **Dots swell briefly when their pile grows.**
- **The inventory's Floor rows stay until plan 6** replaces the inventory panel (the roadmap had them replaced by plan 5).
- Plan: `docs/plans/finished/007-floor-on-the-map.md`.

---

## 2026-09-28: Plan 006 (route on the map) as built

- **The queued trips draw as a gold line from room to room**, one straight leg per trip; a way walked both ways draws once (the badges carry the order). The walked part of the line disappears behind Clara; a newly scheduled leg grows in over ~0.3 s.
- **Stop badges count from where she is now**, as the drawer's stop cards do: stop 1 is her room, and the numbers move down as she goes. A room she comes back to lists each visit ("1, 3").
- **No badges when nothing is queued** (the user's call after the first build): a lone "1" on her room said nothing the token doesn't.
- **Badges swell briefly when their numbers change** (a trip added, or she arrived). Added at the user's request.
- **Clara's token rests by her room's bottom-right corner** and walks a path alongside the line, not on it, so it never jumps when a trip starts or ends. Whether it should sit on the room instead is a playtest question.
- **Colours, thickness, draw-in time, token size and easing are look settings in the Map Style asset**, not balance (no Balance Sheet column).
- Plan: `docs/plans/finished/006-route-on-the-map.md`.

---

## 2026-09-28: GDD split into a working doc and future sketches

- **`GDD.md` now describes build stages 1 and 2 only.** §7 (Build Archetypes), §8 (Walls), §10 (Challenges) and four parts of §12 (Memory nodes, The switch graph, the stale-realm-sketches \[OPEN\], and the realms 1–3 / 4–6 / 7 \[PROPOSED\] line) moved verbatim to `future-sketches.md`. Nothing reworded; nothing renumbered: each moved section leaves a one-line stub, so every "§N" reference still resolves.
- **§17 stays whole** in the GDD, as the index of what is unsettled; items 7, 11, 13, 14 and 18 point to the new file. *Changes from v0.4* stays until v0.6.
- **Convention from now on:** an entry that supersedes something in the GDD names the section, e.g. "Supersedes §5's stacking proposal."
- Why: half the GDD described systems that can't be built until pathos pools work, which every design session paid for in context.

---

## 2026-09-28: Plan 005 (drag to reorder) as built

- **Rows and whole stops drag in the queue drawer**; a stop by its heading or its trip row. A move is refused if a trip that would have been made is skipped, or an action ends up in a room that doesn't offer it; the reason is the run's own words. Entries already broken before the move don't count against it.
- **Dragging above the running action switches to the dropped one at once**; the old one waits with its progress kept, as To top.
- **Nothing goes in front of stop ①** (where the queue starts): a trip dropped there would quietly move the actions queued there to another room. (Chosen at review; to confirm.)
- **Moves never merge entries.** Supplier links survive moves.
- **Added at the user's request:** the dragged row or stop fades, no tooltips open during a drag (anywhere in the game), Esc cancels a drag, and the lists scroll while dragging near their edge.
- Plan: `docs/plans/finished/005-drag-to-reorder.md`.

---

## 2026-09-28: Plan 004 (ribbon and queue drawer) as built

- **A trip is the first row of the stop card it starts**, so it can be sent to the top or removed like any action. Stop ② reads "Travel to …" then the actions there.
- **A queued trip the run will skip is greyed with the run's own reason** ("she's already there", "the hall has shifted: she must search this room again…"), not a new "no way there" line: a hidden way searched for earlier in the queue may still open.
- **The old queue list is removed** (component, list and script) after the user's playtest, rather than kept switched off as the plan said.
- **Stop numbers are plain digits**, not the mockup's ①②: no font in the project has circled numbers (backlog).
- Plan: `docs/plans/finished/004-ribbon-and-queue-drawer.md`.

---

## 2026-09-28: Ribbon and queue drawer: the four choices

- **The queue drawer shows stop cards:** one card per visit to a room, left to right, numbered, with a *return* marker for a room visited again; each card lists the actions queued there (as the route chip mockup, with more detail).
- **Drag to reorder is split off** into plan 3b: it needs a Core rule refusing moves that break the route. Plan 3a keeps To top and Remove.
- **The queue forecast ("projected · dry at") stays in the backlog**, not in plan 3.
- **An empty ribbon shows a hint** (click a room to plan her next step); between runs it shows the queue planned for the next run.
- Plan: `docs/plans/finished/004-ribbon-and-queue-drawer.md`.

---

## 2026-09-28: Plan 003 (room popovers) as built

- **An action missing something it needs isn't greyed:** it shows the reason ("needs 1 Candle") beside its times and can still be played or scheduled, because the queue can make the thing first or she may pick it up on the way. Only actions done this run, one of a kind already held, and fully searched rooms are greyed. (Refines *Room popovers: the four choices*, below; to revisit after playtesting.)
- **Play and Carry work only for the room she's in; Schedule only for the room where the queue ends** (as the old Actions list behaved). Elsewhere they're greyed with a tooltip saying why.
- **Floor chips show each item's count and its floor space** ("6 Candle · 15 fit"): floor space is per kind of item in the build, not one total as in the mockup.

---

## 2026-09-28: Room popovers: the four choices

- **Clicking a room still queues the trip there** when it's next to where the queue ends (as built), **and also opens that room's popover**. So the mockup's separate "Add stop to route" button isn't needed.
- **Unavailable actions are shown greyed, with the reason**, when she could get to them: done for this run, one of a kind already held, a need missing, room fully searched. **Still hidden:** actions not yet found by searching, those locked behind a switch, and kept rewards already owned, so nothing is spoiled. This replaces "an action that only gives one-of-a-kind things … leaves the action list" (2026-09-27 *One-of-a-kind objects*) for the popover: it stays, greyed.
- **The popover opens beside the room**, kept inside the map, and moves with it.
- **The old Actions list is hidden** (its drawer tab and page deactivated) once the popover works, so the popover is playtested on its own.
- Plan: `docs/plans/finished/003-room-popovers.md`.

---

## 2026-09-28: No ×5 speed until the speed rework

- **×5 is removed from the speed tiers for now**: nothing in Act I grants it, so a locked ×5 would tease a speed the player can't earn. It comes back with the speed rework (realm mastery) and meta currency. The tiers show ×1 and ×2 once ×2 is earned; the locked-tier display stays in the code for when a further speed is added.
- This settles the conflict in *UI rework: the mockup and plan 1's layout* (2026-09-27) between "the locked speed tier [is] left out until built" and "one locked tier visible": no locked tier while there's nothing more to earn.

---

## 2026-09-28: Plan 002 (layout shell) as built

- **The page tabs (Menu, Summary, Main) stay above every page**, pinned to the top-right corner where the top bar ends, rather than inside the main page's top bar: inside it they'd slide away with the page and leave Menu and Summary without tabs.
- **Begin run takes the place of Pause and End run** in the top bar (it only shows between runs, when they're hidden).
- **The locked speed tier's tooltip only says it isn't earned yet**, not where it comes from: nothing in Act I grants ×5, and where later speeds come from (realm mastery) isn't built.
- **Layout tuning waits until the UI rework's Part 1 is built** (plans 2–6): the user isn't fully happy with plan 002's proportions, but the popovers, ribbon and strip will change them anyway.

---

## 2026-09-27: UI rework: the mockup and plan 1's layout

- **The design chat's mockup (1440×900, four bands: top bar, map and right rail, ribbon, bottom strip) is a proportion study**, not pixel values. Where it disagrees with *Main screen UI rework* (below), the earlier decisions stand, confirmed by the user:
  - **The ribbon** shows only the current action (with progress) and the next; the full queue is in the slide-up drawer, not a row of chips.
  - **Ambient lines stay in the right panel** with the story milestones and the benchmark card, not on the map (still to playtest).
- **Until plans 2, 3 and 6 replace them, the old Actions, Queue, Inventory and Stats panels live as tabs in the slide-up drawer**, so the map gets the centre straight away.
- **The map keeps following Clara and zooming with the wheel.** **Open:** how the Hall plus seven realms fit (whole map, or one realm at a time): decide when Amber is built.
- **Also open from the mockup:** what a room card shows when it needs its action count, floor pips and mastery at once (plan 5), and what leaves the bottom strip first when it overflows (plan 6). Standing orders and the locked speed tier are left out until built.
- Clicking a room next to where the queue ends already queues the trip there (built); the popover (plan 2) builds on that.
- **Further mockups (room popover, route chips, speed tiers):**
  - **The route chip row** (numbered stops, a *return* marker for a room visited again, drag to reorder) **is the queue drawer's design** (plan 3); the ribbon stays current + next.
  - **Speed becomes segmented tiers with one locked tier visible** (plan 1's top bar), so the player sees there is another to earn. The speeds themselves don't change.
  - **The room popover** (plan 2) follows the mockup: header with search progress; actions with base and actual times and Play, Schedule, Carry; unavailable actions greyed with their reason; the floor with its space used, Pick up and Put down; the ways on, with Add stop. Visits and "by heart" wait until visits are counted.
  - **The queue forecast** ("projected 4:48 · dry at 4:31") goes in the backlog, to consider with plan 3. So do visit counts and the one-line summary for by-heart rooms past ~10×.
- **Plan 1 details (plan 002):**
  - **Empty ribbon and strip bands are reserved now**, so the whole layout is tuned once.
  - **The page tabs (Menu, Summary, Main) move to the top bar**; the bottom belongs to the drawer's tabs.
  - **The whole UI is meant to feel like a page in a book, and the drawer tabs like bookmark ribbons** pulled to bring information onto the page. Which edge they pull from (bottom, side or top) is for playtesting, so the drawer's side is an Inspector setting; bottom for now.
  - **The speed tiers stay hidden until the first speed is earned**, as now.

---

---

## 2026-09-27: Game speed belongs to the save that earned it

- **Starting, loading or deleting a game drops the game speed to what that save has earned** (×1 if nothing). Before, a speed earned in one slot carried into another with the Speed button hidden, so the player couldn't get back to ×1 (code-health 2026-09-27 🔴 3).
- **The dev panel is dropped too**, so the editor behaves as players will see it; its slider can be set to ×20 again straight after. This replaces the earlier backlog note "DevTools stays unlimited".

---

## 2026-09-27: GDD v0.5 draft checked against the build: rulings

Rulings where the v0.5 draft disagreed with earlier decisions:
- **Mastery XP growth stays steep** (the soft cap). The draft's recommendation (1.085 → 1.01) is rejected. Meta currency buys mastery levels (the entry below); the draft's removal of currency from mastery is corrected.
- **Game speed moves to realm mastery** (the draft is mostly right): *Feed your hours* unlocks the concept, and the speed per realm grows with visits (GDD §13a). This replaces "later speeds are new items and actions" (2026-09-27 *Game speed is a reward*). The global ×2 stays until then. A rework, **after the UI rework**.
- **Both kinds of persistence exist:** knowledge-like rewards (switches, memories, Mirrors Found, Quickened Hours, the mana stone's warmth) are kept on obtaining them; physical items are kept only if carried out.
- **Where Act II starts is narrative, not a rule:** in the author's mind, **Act II begins when the lab is completed**, the benchmark for exiting through the mirror and the first time meta currency appears. So walking out and meta currency arrive together.
- **GDD v0.5 applied** to `DesignNotes/GDD.md` with these rulings and the build check's corrections. Still open there: how deep content stays startable with steep mastery growth (§6), and the draft's pools-taken-in-the-prologue proposal against §15's "Amber at the lab".

---

## 2026-09-27: No mastery cap: soft caps from rising costs

- **The mastery cap is to be removed** (the build caps at 150, the Mastery Max Level setting). In its place: a **levelling system and meta currency with soft caps**: each level costs more to buy and needs progressively more XP, so progress slows without a hard stop. Matches GDD §4 (hard caps dropped for rising costs).
- Supersedes the 25 (2026-09-26) and the 150 in the asset. Not built yet.
- **Open:** whether the cap goes before meta currency exists (the XP curve alone as the soft cap) or together with it; the cost curve and the currency's name (GDD §17).

---

## 2026-09-27: Main screen UI rework: the map at the centre

- **Goal:** a screen that looks good and is built around the map and routing, so the game reads as distinct from *Idle Loops* and *Increlution* (as *Stuck in Time*'s map set it apart). Top of the backlog.
- **GDD §13a *Proposed layout* is the starting point**, not a spec: keep the big ideas (a large map, room popovers instead of the task list, a drawn route with a ribbon instead of the queue list, a compact status strip) and adapt the details to what's built. Slots for unbuilt systems (standing orders, realm speed) are labelled placeholders or left out.
- **Planning has two halves** (the user's intention; may change after playtesting): a **planning screen before the run**, where automation is set up for content that's been **mastered**, and **live play during the run** for unmastered content. This is the "route-planning screen with automation" of 2026-09-26 *Ways, later*.
- **Order:** Part 1 is the main screen (live play), built on existing systems. Part 2, the planning screen and automation, comes after and needs a "mastered" rule first.
- **Visual level for Part 1: structure and motion**, no art assets: layout, hierarchy, type, a palette kept in `UiStyle` and the Map Style asset, and motion (Clara's token walking the route, the route drawing in, glows). Art stays at stage 3.
- **Layout work:** a one-click setup step builds the new layout; the user then adjusts it by hand, and from then on it's theirs.
- **The Summary page rework stays separate**, after this one.
- **The queue keeps a full view:** the ribbon under the map shows only the current action with its progress bar and the next scheduled action. A small tab at the bottom of the screen slides up a drawer with the whole queue laid out horizontally, with more information, overlaying part of the map, for adjusting it.
- **Story and ambient text in one panel** (the user's guess, to playtest once the new UI is in): the Story panel's beats and benchmark times combined with the ambient feed.
- **Inventory: undecided.** It must be easy to see without clutter. Recommendation in `docs/plans/ui-rework-roadmap.md`, to settle when that part is planned.
- The whole plan is in `docs/plans/ui-rework-roadmap.md`.

---

## 2026-09-27: Run length: last and longest

- **The Summary page compares this run's game time with the last run and the longest run ever** (GDD §13a *Benchmarks*, "show best-ever alongside last-run"; for run length, best means longest). A run that beats every earlier run is marked "Longest yet".
- **Runs the player ends early (*End run*) don't count for the longest-run record**: they can't hold it or beat it. They still show as "Last run" and are still in the history.
- **Every finished run is recorded** (loop number, length, actions completed, how it ended) and kept in the save, so a future sparkline has the data. How the stats are shown will change with the Summary page rework (planned, not designed yet).
- For now the longest run is a third column in the existing table: a placeholder until the rework.

## 2026-09-27: The GDD's labels describe the build; BUILD-STATE wins

- **`docs/BUILD-STATE.md`** is the plain-language snapshot of what the build does, for the design chat (which can't see the code). `/sync-state` regenerates it; `/wrap-up` runs it whenever gameplay, content or balance numbers changed.
- **GDD v0.4** replaces v0.3 (moved to `Archive/`). Its [DECIDED] / [RESOLVED] labels became **[BUILT]** (the build does it as written), **[BUILT — differs, see BUILD-STATE]** (the build does it differently) or **[DIRECTION]** (not built yet), beside [PROPOSED] and [OPEN]. Nothing in it is decided.
- **Where the GDD and BUILD-STATE disagree about the build, BUILD-STATE wins.**

## 2026-09-27: The candle puzzle: 15 here, 10 there

- **A Dark Hall:** light **15** candles (its own count, *Candles lit in A Dark Hall*, max 15). All 15 push the darkness back: the drain **halved** and **+5 floor space** (both in step as they're lit), and the milestone *The darkness pushed back*.
- **The Left Corridor:** candles are only made in the hall, so **10 more must be carried there** (5 pockets: the satchel's +5 carries all 10 at once, or two trips) and lit with the corridor's own *Light a candle* (*Candles lit in the Left Corridor*, max 10). Candles on the hall's floor don't count in the corridor: that's the puzzle.
- **The first time 10 are lit in the corridor**, *All twenty-five alight* flips: it **opens the way into the dark with the hanging mirrors for good** (the puzzle is solved once; the way no longer needs candles) and offers **Feed your hours to the flames** (the ×2 speed), now in the corridor, needing the corridor's 10 lit in that run.
- **Every run:** the Hall's 15 (pushing the darkness back). **Once:** the corridor's 10 (the way into the dark). The way, like other hidden ways, is still found each run by searching the corridor (for now).

## 2026-09-27: Game speed is a reward

- **Faster game speeds are earned, one at a time**, as an optional reward for players who want them (the default pace suits reading the story and the ambient notes). The Speed button is hidden until one is earned, and only offers speeds earned so far.
- **×2 (placeholder numbers):** the first time all 25 candles are lit, the switch *All twenty-five alight* (placeholder story 16) offers **Feed your hours to the flames** in A Dark Hall: 20 s, **50 vitality** (Always Charged), needs 25 candles lit. It gives **Quickened Hours** (kept forever, one only, Unlocks Speed 2). Once earned it's never offered again.
- How it works in general: any item with **Unlocks Speed** lets the Speed button reach that speed while held (kept items included). Later speeds (×3…) are new items and actions, plus adding the speed to the Run Header's Speeds list in the scene.
- General rule added: an action that only gives kept things she already has all of isn't offered.

## 2026-09-27: Room names are titles

- **Naming convention for rooms (places):** title case: major words capitalised, small words (of, with, the, a, and) lower case unless first, e.g. "The Dark Corridor with Hanging Mirrors", "The Mirror's Laboratory", "A Dark Hall". A name may start with "The", "A" or "An" or have no article at all ("Dark Hall").
- **Mid-sentence** ("Travel to the Dark Corridor"), only a leading "The", "A" or "An" is lowercased (`GameText.TitleInSentence`); the rest keeps its capitals. Action names (not titles) still lowercase their first letter in notices ("Can't light a candle").

## 2026-09-27: One-of-a-kind objects

- **An object whose maximum is 1 (Roland's ring, the tome, the flint and steel) is one of a kind:** never a second this run, counting one put down on any floor. It never overflows to the floor as a duplicate, and an action that only gives such things stops (and leaves the action list) once one exists this run. Put down and Pick up still move the one she has.
- Found in playtesting: Take Roland's ring could be repeated, piling extra rings on the floor.
- **Picking up a one-of-a-kind object with full pockets pushes something out** (simple version, placeholder): one of what she has most of in her pockets (never another one-of-a-kind thing) is put down on the floor here to make way. Picking up ordinary items (wisps) pushes nothing out, so they can't push out the ring; how swapping should work more generally is to be tuned later.
- **The old "Put the ring down"** (from before the floor; it used the ring up) is retired: "Put down Roland's ring" puts it on the floor.

## 2026-09-26: Playtest fixes: Carry, searching with Perception

- **Carry is only for restoration items** (things that give vitality back: wisps, phials of memory). Other gathering actions just have Play and Schedule.
- **Carry goes on top of the queue**, like Play (it was the bottom, like Schedule).
- **Candles lit add +5 floor space in all, not +5 each** (the user's intent; the old per-candle reading made the floor 135 with 25 lit). In step as she lights them: +1 for every 5 lit, so 25 lit = 15 of each object per room. An item's Adds Floor Space is now its total at its maximum.
- **A room's search gives what it gives once per step of its bar**, not once per search. Perception makes a search count for more than one step, so a full bar always gives the whole amount (10 mirrors from the hanging mirrors), just in fewer searches.

## 2026-09-26: Pick up and Put down are two verbs, not an action per object

- The 12 "Pick up ..." / "Put down ..." task assets are replaced by two common verbs in Game Content, like Travel: the game makes each object's action when it's needed. New objects get both for free; the Make Pick-up and Put-down Actions tool is gone.
- One time for every pick-up and one for every put-down (Balance Sheet: the Pick up and Put down rows). If some object should be slower to handle, add a per-item "handling" multiplier then (like a room's leaving/entering), not before.
- Each object has a "Name in actions" (e.g. "all wisps", "the tome") for how it reads in these actions.

## 2026-09-26: Carry, putting things down, a second fountain

- **Carry:** a third queue option beside Play and Schedule, for actions that gather things she carries. It repeats only until her pockets and containers are full, never gathering just to leave things on the floor. Play and Schedule keep going until the floor here is full too. Both are useful.
- **Putting things down:** every object gets a "Put down ..." action (all of it in her pockets onto the floor where she is), e.g. to drop the flint and steel.
- **A memory fountain in the left corridor:** phials can be filled there too (found on its first search), as well as in the right corridor.

---

## 2026-09-26: Containers: the pouch of phials

- **The empty phial becomes a pouch of phials:** carried outside her pockets, it holds up to **10 phials of memory** (placeholder). Instantiated once a run, as before; Fill a phial needs it.
- **Containers in general:** an item can hold a list of other items, up to a number, outside her pockets: a separate carrying capacity for a class of items, where pockets hold almost anything. New items go in the container first, then her pockets, then the floor; using one frees her pockets first. (Later: other containers; perhaps some kept between runs.)

---

## 2026-09-26: Playtest fixes (ways re-found each run for now; no floating notices)

- **Ways must be found again every run to be taken** (until the planned 8-searches feature): the hall shifts. A way found before stays **known** and on the map, and finding it again isn't announced; trying it early says she must search the room again.
- **The floating notices (top left) are gone:** they got in the way, and what they told of already shows where it is. **Items glow when they go up**, like stats.
- **Actions offered everywhere** (listed in no room, e.g. *Put the ring down*) appear only when she has what they need.

---

## 2026-09-26: Actions check first; the floor where she is counts

- **Items on the floor of the room she's in count for what an action needs, and are used up first** (then her pockets). Found in playtesting: with full pockets, flint, candles and the empty phial landed on the floor and Light a candle / Fill a phial did nothing.
- **An action checks it can do something before any time is spent:** with no room anywhere for what it gives, it can't start; and an action asked for that can't be done now (Play, or Schedule onto an empty queue) is **refused on the spot** with its reason, not queued, with no time passing. Queued behind other actions, it's accepted (it may be possible by its turn).
- **A "can't do that" notice:** a small red-edged box in the bottom right says why ("Can't light a candle: needs 1 Hanging candle"), then fades.
- **Once-a-run actions disappear from the list once done** that run (e.g. Instantiate flint and steel).

---

## 2026-09-26: Closing the game mid-run keeps the run

- **A run under way is saved whole and resumes where it was**, paused, when the game is loaded. **Quitting mid-run no longer ends the run** (this replaces the placeholder reading that quitting counts as being yanked back and loses carried items). Ending a run early is still done with *End run*.
- **Autosave after every completed action**, every switch flip, every pause and at a run's end, plus on quit: at most the action under way is lost. Full saves (a few KB), written to a temporary file then swapped in, so a crash mid-write keeps the previous save.
- **Offline progress** (fast-forwarding the time away, using the save's timestamp) isn't built: runs always drain and would simply end. A possible later fit for meta progress in the real lab.

---

## 2026-09-26: Ways, later (planned, not built)

- **For now ways stay as built:** found once by searching, found for good.
- **After the balance pass (from the design chat):** a room's ways unlock only after it has been **searched 8 times** (across runs); then the room counts as unlocked. That comes with a **route-planning screen with automation**, similar to the old separate planning and run screens. To be planned in detail when the time comes.

---

## 2026-09-26: Act I pacing, pockets v2, searching every run, stats by theme

- **Act I pacing** (from the design chat):
  - **0–5 min:** chased, robbed, dropped; revelations, no choices. The first run ends before the player understands why.
  - **5–15:** wisps, the first scarcity. The **candle chain** (~85 s of a ~150 s run) is the first real decision: invest or push. Light is required for the dark.
  - **15–30:** **the branch.** Right: the ring (the key to the lab, and it bleeds her) and the phial chain (a better economy). Left: the dark, Mirrors found, and **the satchel**. Early runs can't do both. The branch is per run, never permanent.
  - **30–45:** the first mastery cap (likely Gathering), so grinding stops working and the meta layer has a reason to exist.
  - **No build decisions in Act I:** no allocation, no pool shape, no loadout.
  - **The test:** at the end of Act I, "what do I wish I had more of?" should be answered "time".
- **Pockets v2** (replaces the pockets entry below):
  - Pockets start at **15** a run, and **every object takes a slot** (wisps, candles, the ring, the phial, the tome, wood, torches). Knowledge and states (Understanding, Steady hands, Bench cleared) don't.
  - Crafted items (e.g. **the satchel**) add slots for the run. Later, some might be taken out of the mirror to keep, and meta currency might raise the starting size permanently (perhaps as a percentage; late game ~100).
- **Restoration:** **one of each kind at a time**: a second wisp waits until the first has run its course (the inventory timer shows when), but different kinds (a wisp and a bottled well) run side by side. Deeper in, weaker items give way to stronger ones: content, not a rule.
- **Searching every run:**
  - **Search and Explore become one verb.**
  - **The hall shifts between runs** (it needs a story beat): **ways, once found, stay known**, but a room's **contents (verbs like gather and instantiate) must be searched out again each run**.
  - Mastery makes re-searching quicker.
- **Skills vs stats:**
  - A **skill** covers all actions of its verb (Gathering: wisps and bottled wells alike).
  - **Stats** affect what skills don't, and **train from their theme**:
    - **Endurance:** how much she can take. Max vitality (linear), overflow tolerance for restoration (a high Endurance uses items more readily), XP rate for Wayfinding and Gathering. Trains from vitality lost.
    - **Composure:** how slowly the hall notices her. Slows the drain's growth, reduces the carried bleed (the ring), later resists the reflection and memory interference. Trains from carrying what costs her, and from the drain.
    - **Perception:** how much she sees. Search yield per pass (not speed), hidden things below a threshold, XP rate for Instantiate. Trains from searching.
    - **Scholarship:** what she can comprehend. Hard gates (tomes unreadable below a level; the only stat that flatly blocks content), Understanding per study, XP rate for Studying and Invoking. Trains from studying.
    - **Attunement:** how cheaply she works. Pathos cost (Act II), instantiation stability (how long an instantiated object holds), craft quality. Trains from instantiating.
- **Light for the dark (revised 2026-09-26, from the design chat):** candles are lit **one at a time** (*Light a candle*: flint and steel + one candle, repeats while she has candles), adding to a per-run count, **Candles lit** (no pocket, up to 25). The count eases the drain in step: ×(1 − 0.25 × lit/15), so **15 lit = ×0.75** (vitality lost per second; its growth rate is unchanged), and more than 15 don't help. **The way into the dark needs 25 lit** (Balance Sheet: Ways). **Milestone at 15:** *The darkness pushed back* (placeholder passage: pushed back, but not enough light to see into the distance), timed each run. Fixes the bug where 15 candles + flint couldn't fit in 15 pockets.
- **XP by time:** an action's usual XP is **75% of its base time, rounded up** (LoopSettings *XP Per Second Of Task*, 0.75; replaces the flat 2 XP): a 20 s action gives 15. Base time is its own Seconds, so getting faster means the same XP sooner. An action's own XP Reward still overrides it.
- **Restoration items are used from the floor first** (where she is), then from her pockets, so her pockets stay full for the road.
- **The queue supplies what a blocked action needs (2026-09-26, general queue rule):** if the top action can't start only because it lacks an item, the queue puts something in the current room that supplies it on top: **picking it up from the floor first**, else an action here that gives it. That repeats until there's **enough to finish the blocked action** (what one go needs × the goes it has left, e.g. one candle per candle still to light), counting pockets plus the floor here, **or until pockets and floor are full**, whichever comes first; then the blocked action runs. It chains (a supplier's own missing item is supplied too, up to 3 deep). No auto-travel: only what's in this room. With nothing to supply it, the action is dropped with its reason, as before.
- **The floor (2026-09-26, the user's call; the design chat advised against floor piles in Act I):** what doesn't fit in her pockets is put down on the floor of the room she's in (overflow, automatic), up to **10 of each object per room** (she can't see far enough into the dark to organise more). **Pockets go back to 5.** **Each candle lit adds +5 floor space** (placeholder reading of "lighting candles can increment floor space by +5": everywhere, since the candles drift after her), so candles serve two functions. **Pick up all [item]**: 2 s, Gathering, one go, takes everything of that type that fits in her pockets. **Piles are gone each run** (the hall shifts).
- **No general partial or resumable actions:** content that builds up in pieces is made of small repeatable steps. **Floor stockpiles/caches** are a later (late-game) mechanic, not Act I; if built, piles should persist between runs so they're infrastructure, not an inventory workaround.
- **The satchel** (placeholder): *Instantiate a satchel* is found on the 2nd search of the left corridor (20 s, once a run); the satchel adds 5 pockets for the run and takes none.
- **The first mastery cap** (placeholder): Mastery Max Level 25 (was 100), aiming for the first cap around minutes 30–45; meta currency raises it later. Retune after playtesting.
- **Supersedes:** Endurance slowing the drain (now Composure slows its growth); each action naming a stat to train; exploration bars kept between runs.
- **GDD needs updating:** stats (§ on attributes), searching, pockets.

---

## 2026-09-26: Pockets, and content before the lab

- **Pockets:** some items go in a pocket (a per-item setting: wisps, bottled wells); others don't (a torch, the ring, tools in her hand, candles hanging in the air). **Undecided:** 5 pockets in total across all kinds, or 5 of each kind. Built so both can be tried: LoopSettings *Pocket Slots* = 5 in total (placeholder); 0 = no shared limit, each item held to its own maximum.
- **The hall mirror is renamed "The smoky mirror".**
- **Content leading up to the mirror lab** (placeholders, numbers to tune):
  - *Gather a wisp* in the smoky mirror, A Dark Hall and the right corridor. A wisp restores 5 over 5 s and takes a pocket.
  - A Dark Hall: *Instantiate flint and steel* (once a run), *Instantiate a candle* (until 15 hang in the air), *Light the candles* (needs the flint and 15 candles, uses the candles, drain ×0.8 for the rest of the run, once a run). Small candles that hang in the air, 10–20 of them; both the flint and the candles instantiated from mirrors.
  - The right corridor: *Instantiate an empty phial* (once a run), then *Fill a bottled well* (needs the phial; restores 50 over 3 s, takes a pocket).
- More actions will be needed before the mirror lab opens; the lab's gate (the ring) is unchanged for now.

---

## 2026-09-26: Carried items and their cost

- **Ending a run early loses carried items**, as if she'd been yanked back. Quitting the game mid-run counts the same (placeholder reading: it ends the run without walking out).
- **Carrying Roland's ring is a hard cost on vitality**, like the chase: items have an **Always Charged** toggle, and the ring's carry cost comes straight out of vitality even while carry costs are off. Composure still softens it.
- **Composure trains from carrying only while carrying costs her something** (so the ring trains it; an uncharged item doesn't).

---

## 2026-09-26: No resting: restoration items, and holding off the darkness

- **There's no rest action.** GDD §9 already says so, though its reason (action-driven drain) no longer holds now that the drain is passive. Two mechanics push back against the drain instead:
  - **Restoration items:** things gathered from realms or instantiated from mirrors (wisps, gem memories, bottled mana wells; perhaps food: not settled). Each restores vitality over a clock and is then used up, e.g. a wisp 1/s for 5s, a gem 20 over 10s, a bottled well 50 over 3s. Clara uses one **automatically as soon as it won't be wasted**: when she's missing at least what it gives, counting what items already in use will still give back. **Several run at once and add up.** On the resource asset: *Restore Vitality* and *Restore Seconds*.
  - **Holding off the darkness:** actions that **lower the current drain by a share** (on the task: *Drain Times*, e.g. ×0.85), for the rest of the run. The drain keeps growing from the lower value.
- Which items and actions exist, and their numbers, is content still to decide.
- GDD §9 ("no Rest or Wait action") stays true, but its reasoning and the new mechanics need writing into the GDD.

---

## 2026-09-25: UI rework: one main screen, an action queue, tooltips (Increlution-style)

- **One main screen.** The Plan and Run pages merge: all planning happens during the run. The planning page is stripped down into an **end-of-run summary and story page**. Runs will be longer (5–10 minutes) and do much more.
- **Layout (to build):**
  - across the top: a large vitality bar (current / max), the drain per second, and a clock of unpaused run time
  - a compact stats and skills block (multiplier, plus two thin bars: level and mastery)
  - left to right: task list | inventory | action queue
  - far right, two text panels: an **ambient** feed (blurbs, minor beats) and a **story** panel updated only at milestones (this run's time vs last run's, the benchmark's name, a paragraph and Read more)
  - hover **tooltips** on everything
  - **the map** (added 2026-09-25): its own frame on the main page (placeholder: top right, above the two text panels). On the main page the map fills that frame and follows Clara, and clicking a room next to where the queue ends schedules the trip at any time.
- **The action queue** replaces the build:
  - Every task has **Play** (to the top, switch now) and **Schedule** (to the bottom). No ×1/×5/×10.
  - Clara works the top entry. It repeats until done: a trip made, a room fully explored, a once-per-run task done, everything it gives at its maximum, or a **Single Action** task done once, which then **pauses the game**. Tasks with no natural end repeat until removed.
  - Play interrupts the running task, which **keeps its progress** for when it's back on top.
  - Every run **starts with an empty queue**, paused. Queuing the first action starts it at once. A game-made pause (queue empty, single action, way found) ends when something is queued while Clara is idle; a pause the player chose doesn't. Automation comes later.
  - Gone: "repeat last task", repeat counts, and stalled runs. An idle run waits instead. The queue isn't saved (save version 3).
- Placeholder: an entry that can't start is dropped with its reason, rather than waited on.
- **Milestones** for the story panel: switches that have a story.
- **Benchmarks recur** (decided 2026-09-26): every run, each milestone already reached is timed again when Clara meets its condition in that run, and shown against last run's time (−0:33 faster, +0:12 slower). The first time is marked "first time" and brings its story. Milestones whose condition stays true between runs (a room explored, kept resources) can't be re-timed, so they appear only the first time. "Last run" means the run just before, not the best run.
- **The gem bar** (2026-09-26): all pathos pools share one bar. The whole bar is Clara's gem full, and each hue fills a slice in its colour as wide as what it holds (seven equal pools share it in sevenths). **Open question:** what "the gem full" is. Placeholder: every pool's maximum added up, so the gem grows with each hue learnt. The alternative is a gem capacity of its own, shared by all hues, with pools no longer having separate maximums: a GDD decision.
- Order of work: tooltips (done) → queue rules → the merged page → the story panel → tooltips everywhere and tidy-up (retire the old pages and their text keys).

---

## 2026-09-25: Mechanics rework, step 1: time, passive drain, skills (branch `rework-time-and-skills`)

- **Reverses "drain is action-driven" (GDD §5 and CLAUDE.md need updating).** Actions now cost **time only**. Vitality **drains on its own**: 0.5/s at the start of a run, smoothly every tick, growing **25% per minute** (compounding, smooth), so every run ends eventually, even with drain resistance and (later) rest actions. Endurance slows it: ×0.97 per level (placeholder).
- Action and carry costs are **switched off, not deleted**: *Charge Action Costs* and *Charge Carry Costs* in LoopSettings (both off). The task assets keep their costs for now. The "a free task can't repeat" stall rule is gone, since the drain ends runs.
- **Stats and skills.** Stats (the five attributes) have broad effects; **skills** are per kind of action and make their tasks faster. Starting skills: Wayfinding (travelling and exploring), Gathering, Instantiate (memories made solid), Invoking, Studying. About 10 eventually, not all unlocked at the start: a skill shows once an unlocked task uses it.
- **XP:** each task gives **2 XP** (LoopSettings *XP Per Task*; each task can override it with *XP Reward*, e.g. longer tasks), delivered smoothly while it runs. It goes to the task's skill **and** its stat, and the same amount to each one's mastery (*Mastery Share* 1).
- **Level curve (skills, stats, mastery):** level 1 needs 10 XP; each level after needs 1% more, but at least 0.5 more. Growth may need raising above 1% in balancing.
- **Speed:** ×1.05 per skill level times ×1.01 per skill mastery level. Stats no longer speed tasks. Stat mastery's old "faster levelling" bonus is removed.
- **Mastery cap:** 100.
- **Always Charged** (a per-task toggle): the task's cost is charged even while action costs are off, as an extra drain on top of the passive one. The chase uses it (20 s, 300 vitality), so the player isn't waiting about 150 s for the drain to exhaust her.
- **Next:** UI (a Skills section, skills on the results card, the drain rate), then the wider UI and feedback rework, then rest and recharge actions.

---

## 2026-09-25: All on-screen text in one file

- **Every line of on-screen text goes in `Assets/Text/game_text.txt`**, grouped by where it appears (results, planning, feed, notices, map, build, menu...), with a comment for each group. `key: text` lines, `{name}` where the game fills something in, `\n` for a new line. It reloads while playing when saved.
- *Why a text file, not JSON:* comments (a writer needs to know where a line appears), and a mistake only breaks its own line, not the whole file. It's the same format as the blurbs, and converts easily to JSON, CSV or Unity's Localization package if needed.
- **Three steps:** (1) the text system and all the screens' text (done); (2) scene labels (button captions, headings) via a small component and a tool; (3) the simulation's messages (skip and stall reasons, entry names), with the tests checking meaning rather than wording.
- Scott's first edit: "Plan the next run" becomes "Plan your next trip".
- **Step 2 (scene labels):** a `TextKey` component makes any scene label read its line from the file (`## scene` section). *Hall of Echoing Mirrors → Text → Scene Labels* lists every label, pre-ticks the safe ones (not written by code, not inside a template), and moves the ticked ones' words into the file. A test checks every Text Key in the scene has its line.
- **Step 3 (the simulation's messages):** skip and stall reasons are in `## reasons`, and how trips and explores read in the build ("Travel to the junction", "Explore the hall mirror") in `## names`. Tests load the real file and compare against it (`Reason(...)` in the test base), so rewording never breaks a test. Every word on screen is now in the file or on a content asset.
- **Ordinals:** `{name:ordinal}` writes any number as 1st, 2nd, 3rd...; the endings are in the file's `## ordinals` section.

---

## 2026-09-25: Inside the lab (Scott's plan)

- **Six lab actions**, each on its own stat: **Search** (Perception: find the tome and the setting; once), **Clear the bench** (Endurance: optional, makes Study cheaper; the first trade-off), **Study the tome** (Scholarship: repeatable until understood, 3 understanding), **Attend** (Composure: stay in the room with him; gates the cut), **Cut the stone** (Attunement: the act break), and **Watch him** (Perception, pointed at *him*, not the room: what she missed at nineteen; the game's thesis).
- **Only the first explore of the lab is kept.** Everything else resets each loop (her memory clouds, the words shift). The first real grind: she arrives carrying the ring on a quarter to half of her vitality, and over a few loops mastery makes the whole plan fit.
- **Watch him builds over loops:** each (once per run, expensive) adds to a kept insight, *What he did that night* (up to 5); each point makes the cut, and the watching itself, ×0.8. With a few loops of it, the plan becomes completable.
- **Composure softens the ring's drain, strongly** (each level ×1/(1+0.15×level)), and trains while travelling (half Travel's XP) and on every action while carrying a taxing item.
- **Mastery: faster levelling only**, but stronger (each mastery level +25% XP, was +10%).
- **The mana stone** is in the lab: **Draw on the mana stone** can only ever be done once. It gives a kept *mana stone's warmth* that makes **every trip cheaper from then on** (placeholder ×0.5), and flips *The Mana Stone* (story 13), which retires the task. Intent: the first journey to the lab nearly exhausts her; after the stone she arrives with at least half her vitality, and more as mastery grows.
- *Engine additions:* once-per-run tasks; "easier with" held items (per one held); a second trained attribute on tasks.
- *Placeholder numbers, to tune in play. Stories 11 (what he did: a guess tying it to the ring) and 12 (the stone is cut) are placeholders.* The finale's wiring (the mirror gem, the exit to the real lab) is next.

---

## 2026-09-25: The other laboratory is Act I's first major hub

- **The other laboratory** (the memory behind the ring door) is **the first major hub**: the first place where **several actions must be completed in one room** before a major unlock. *Being planned by Scott:* what those actions are, including how the memory is corrected. Don't build its contents until that plan exists.
- **Crafting the mirror gem is the finale of Act I.** It lets Clara **exit the hall**, which opens the **real-world lab** as **the hub for future runs**. *(Changes the brief's order: there, the gem was loop 7 and the exit loop 10. Now they're one beat, the finale.)*

---

## 2026-09-25: The map becomes a window onto a bigger map

- **The map is laid out at a fixed scale** (a room's Map Position × pixels per unit), no longer stretched to fit the panel, so rooms keep their size however big the Hall grows.
- **The panel is a clipped window** onto that map (not a second camera: a mask keeps everything real UI, so clicks and text just work). It **follows** where the build leaves Clara (Plan page) or where she is (Run page), gliding smoothly. **Drag to look around**; it glides back after a few seconds.
- **The map's colours, scale and following live in a Map Style asset** (Inspector, no code). Fonts, sprites and the shapes of rooms, lines and text stay in the MapPanel prefab.
- **Map layout is edited in the Scene view**, without playing: the Map View's Inspector has *Edit map layout*, which draws every room and way (dimmed if found later, faint if shut or a door) with the real prefab and Map Style, beside the panel. Dragging a room writes its Map Position (undoable); selecting one shows its settings. The preview is never saved and clears before Play. The Map View's Inspector also shows the Map Style's settings inline.
- *Later:* click to expand to a full map, mouse-wheel zoom, sub-maps, drawing new ways by dragging.

---

## 2026-09-25: Carried items, the ring, and the lab door

- **Carried items** are a third kind of resource (with *This run* and *Forever*). **Walking out keeps them** in a stash in the real lab; being **yanked out, ending early included, loses them**. Stashed items can be **packed** to go in again (moved out of the stash, so at risk). Knowledge is kept either way.
- **Items can tax while held**: extra cost per second of any action (travel too, not while idle), paid like hall travel: split across her pools, vitality while she has none. So once she has pools, the ring draws on those instead, as the brief says. *Placeholder:* the ring costs 0.5 a second.
- **Tasks can take items away** (Put the ring down) and **walk her out** (for exits; none yet: the mirror lab exit comes with loop 10). The run report says what was carried out or left behind.
- **Content:** A Glimmer unlocks searching the right corridor again; the search finds **Roland's ring** and flips **Found the Ring** (story 09), which retires the search and offers **Take Roland's ring** (right corridor, 2s) and **Put the ring down** (anywhere). **The Other Laboratory** (10 mirrors) opens a door from the dark into **the other laboratory** (Clara's memory), and the door **needs the ring**: the ring lies down the corridor that does *not* lead to the dark, so she must fetch it and carry its tax the whole way. Inside, the first explore flips **The Memory Is Wrong** (story 10: the memory has kept Roland and misplaced her), setting up correcting it (the testify verb) next.
- The ring and lab blurb buckets are now switched on with rules (ring_present, ring_carried, ring_refused, lab_sealed).
- *Not yet:* the **packing list** on the Plan page (nothing can be stashed until an exit exists), and choosing between the two entries.

---

## 2026-09-25: The glimmer; placeholders in Clara's voice

- **Searching is unlocked by a glimmer.** In the right corridor (a dead end), the first of three cheap explores flips the switch **A Glimmer** (Room Explored, 34%): a short pop-up, and the corridor's Search unlocks. Data only, the same pattern for any room. What the search finds (the ring) comes with carried items.
- **Placeholder passages are now in Clara's voice** (first person, present, 41, dry), and none of them repeat a blurb line: 03 *A Way On*, 04 *Mirrors in the Dark*, 05 *The Other Laboratory*, 06 *Two Corridors*, 07 *The Dark Beyond*, and new 08 *A Glimmer*. All still marked `[PLACEHOLDER]`.
- **Actions that do nothing are hidden** until they're useful: both corridor searches are locked (kept for later), and A Glimmer is story only until the ring exists (then it unlocks the right corridor's search again).
- **A blurb as every action begins.** Buckets have a moment: *Starting* (as an action begins, e.g. "I begin chasing, but...") or *During* (the 3–8 s timer). If no Starting bucket fits, an ordinary one speaks instead, and the timer restarts from there. Buckets can also be limited to particular tasks (start_chase: only the chase). Starter set: `action_starts_act1.txt` (start_chase, start_travel, start_explore, start_search), drafted as placeholder.
- **Feedback on the Run page:** the build is shown there too (done greyed, running in bold, what's left), and explore entries name their room ("Explore the hall mirror x3") on both pages. An **exploration bar** for the room she's in fills smoothly while she explores and warms from teal to gold as the room fills; every explorable room on the map has a mini bar in the same colours.
- **New builds pause when they run out** (default changed from repeating the last task), so running out of plan hands control back instead of risking the run ending.

---

## 2026-09-24: Floating notices and glow

- **Numbers going up leave the feed.** "+1 Mirrors found", "Perception 2", "The junction: 50% explored" and mastery gains pop up in the **top-left**, drift up and fade (about 2 seconds). Several at once stack.
- **The thing that changed glows briefly:** a stat's level label (level or mastery up), the tools and stats list (anything gained), and a room on the map (explored further; it swells, since its colour is set every frame).
- Nothing glows when numbers change for other reasons (a new run resetting levels, or loading a game).
- **Asset log:** `ASSET-LOG.md` at the project root (committed, unlike DesignNotes) records every asset's source, licence and whether it's AI-made, for Steam's disclosure.

---

## 2026-09-24: The blurb system

- **The blurbs text lives in `Assets/Story/Blurbs/`** (copied from DesignNotes, which git doesn't back up; the Assets copy is the master). *Import Blurbs* makes one Blurb Bucket asset per `##` section: lines and description come from the file, **rules live on the asset** and survive re-importing.
- **Bucket rules:** activities (travelling, exploring, searching, working, idle), rooms, room kinds, loop range, switches needed / switches that end it, items held / not held, low vitality, priority, chance, and tapering between two loops.
- **Choosing:** the highest-priority bucket that fits speaks (equal priorities in random order); a bucket that stays quiet (its chance) lets the next speak. Lines come from a shuffle-bag, and never repeat back to back.
- *Starting rules (placeholders, from the document's notes):* low_vitality 50 (below 30%, chance 0.6); ring_carried 40; reaching 30 (from loop 2, tapers from 3, silent from 5, chance 0.5); searching / exploring 25; ring_present, lab_sealed 25; hall_dark 20 (the dark); hall_corridor 20 (Hall rooms, from loop 2, also while exploring until there's an exploring bucket); ring_refused 15 (chance 0.3). The ring and lab buckets start switched off until their systems exist. The document says hall_corridor is for loops 2–3; it's set open-ended for now so the hall isn't silent from loop 4.
- **Tasks now have a kind** (Search, Study, Gather, Assist, Take, Work; GDD §9), so blurbs can react to "searching" without listing tasks.

---

## 2026-09-24: The story feed, and how story is delivered

- **One feed on the Run page, mostly story** (Clara's voice), with small, quiet notes about the game state (a new way, a skipped task, the build running out). It is larger than the old log, with smaller text.
- **Numbers going up are not feed material:** they get **floating text** in the top-left that rises and fades, and a **brief glow** on the thing that changed (next step).
- The feed **follows new lines unless the player has scrolled up**, older lines fade, story lines can **type themselves out** (to be judged in play; it will become a Settings option), and it is **cleared each run**.
- **Blurbs** (from `exploration_blurbs_act1.txt`) become bucket **data assets**. They fire on a **random timer, one every 3–8 seconds of game time** (paused runs don't advance it), with a small real-time floor so fast-forward doesn't flood the feed (extras are skipped, not queued). Both numbers are tunable. *(Replaces "every three actions".)* Priority: low vitality → ring carried → reaching → the place → the action. **Explore gets its own bucket**; searching is reserved for some rooms, often dead ends.
- **Searching unlocks by exploring:** a little exploring reveals a glimmer (a story pop-up), which unlocks that room's Search. Built as a switch (Room Explored → unlocks the Search task, with a story), so no new code.
- *Placeholder:* until blurbs exist, arriving in a room adds a plain story line ("I reach the junction.").
- **Explore is locked until the chase fails:** the Explore verb doesn't start unlocked; Lost Roland unlocks it (data, no code).

---

## 2026-09-24: Pausing hands control back clearly

- **Whenever a run pauses** (the build ran out, a way was found, or the player paused), a **"Change the build ›"** button pops up on the Run page, saying why it paused, and turns to the Plan page.
- **Resume on the Plan page turns back to the Run page**: resuming means "done changing the build".

---

## 2026-09-24: Ways are found by exploring (teaching mid-run changes)

- **Each way has "found at explored %"** of the room it leaves from (0 = known from the start). Until then it's hidden from the map, the Plan list and travel.
- **Each way can have a story**, shown when it's found (ways found together can share one, shown once). A way that becomes usable because a switch opened it is announced the same way.
- **Finding a way mid-run pauses the run** (LoopSettings: *Pause When A Way Is Found*, on by default), and the log says to add the trip. This teaches that the build can change during a run. The Run page's map can add trips while paused.
- *Placeholder content:* the hall mirror (3 explores), the junction (4) and the left corridor (3) are cheap to explore (×0.25 time and cost: 5s, 5 vitality). Each one's ways on are found at 100%, with three short `[PLACEHOLDER]` passages (Assets/Story 05–07). The right corridor has nothing to explore yet (the ring comes later).
- *Open:* the player can explore the hall mirror before the chase in loop 1, which would find its way before Lost Roland opens it. The way is then announced when the chase fails. Worth checking in play whether that reads well.

---

## 2026-09-24: Exploration bars (step B)

- **Explore is a common verb**, done wherever Clara is. Each room says how many explores fill its bar (0 = nothing to explore) and what each explore gives. The bar is **kept between loops**.
- **A full room can't be explored again**, so an explore at the end of a build can't repeat forever.
- **Discoveries are switches** with the trigger *Room explored to N%*.
- *Placeholder content:* the dark with the hanging mirrors takes **10 explores** to fill (matching Mirrors found's maximum of 10) and gives 1 Mirror found per explore. The two Mirrors found stories (at 5 and 10) are unchanged. The Explore verb takes the old task's numbers (20s, 20 vitality, Perception).

---

## 2026-09-24: Common verbs and room modifiers (replaces one-task-per-trip travel)

- **Travel and Explore are common verbs**, each defined once (base time, cost, who pays, what it trains). Changing the verb changes it everywhere.
- **Each room holds only modifiers**, all starting at 1: **leaving**, **entering** and **exploring**. Each modifier has a **cost ×** and a **time ×**.
  - A trip = the Travel verb × the room left's *leaving* × the room entered's *entering*.
  - Exploring = the Explore verb × the room's *exploring*.
- **The map shows every room reachable from the start through open ways.** A door she can't pass yet (e.g. it needs the ring) still shows what's behind it; a shut way hides everything past it.
- *Placeholder message rules:* repeating a trip at the end of a build stalls the run ("she's already there").
- **Ways are data on rooms:** where each leads, both ways or one-way, starts open or not, and what it needs (e.g. the ring). Switches can open and close ways.
- **Explore fills the room's exploration bar**, which is kept between loops (it's knowledge; GDD §9). At set points it reveals what's there. *Planned:* discoveries are switches with a new trigger, "room explored to N%", so they reuse everything a switch can do (unlock tasks, open ways, unlock pools, reveal a story).
- *Art stage:* round map nodes, and a particle travelling along the line during a trip.
- **Mirrors found stays.** Exploring the dark gives a Mirror found each time *and* fills its exploration bar; the bar's thresholds can reveal more later (items, one or two new branches). So rooms get an optional **"each explore gives"** list as well as thresholds.
- **One map panel, docked bottom-left.** It lives anywhere in the editor and slides into the bottom-left corner on the Plan and Run pages (away on the Menu), switching between planning and following Clara to match the page. It replaces the two per-page copies.
- **Many maps are data, not panels.** Each room will belong to a map (the Hall, the memories behind the mirror lab, a realm); the one panel shows Clara's current map and slides between maps like pages. *To build when a second map exists.*
- *Guideline:* **about 4 exits per room at most** (Hall nodes up to 4, memory rooms 1–3), for readability and meaningful choice. Rooms reveal exits gradually as their bar fills. A place that needs many exits (e.g. the mirror lab into every memory) becomes a **sub-map** at the art stage.

---

## 2026-09-24: The map (node graph step 1b)

- **A map on the Plan and Run pages.** Known places are buttons, and the ways between them are lines (a reddish line means one-way).
  - **Plan page:** centred on where the build leaves Clara. Clicking a neighbour adds the trip to the build.
  - **Run page:** shows where she is, and highlights where she's heading.
  - Clicking any place describes it: its kind, what the trip there costs and needs, and what can be done there.
- *Placeholder:* the map is **stretched to fill its panel** (a schematic, like an underground map), not drawn to scale. Places sealed off (on the map, but with no way in yet) are drawn darker.
---

## 2026-09-24: Places and travel (node graph step 1a)

- **Travel is a task** that moves Clara to another node when done, listed at the node it leaves from. Each direction is its own task, so one-way valves and uphill costs come for free. A door's condition (e.g. the ring) is the travel task's **Needs**. Shortcuts are travel tasks a switch unlocks.
- **A node is on the map** once an unlocked way leads there, or if it's marked *always on the map* (e.g. a sealed door she can see).
- **Tasks listed at no node can be done anywhere** (for later: dropping an item).
- **New cost source, All Pools:** split evenly across the pools she has this run; vitality pays if she has none. Hall travel uses it (GDD §5).
- *Placeholder content* (Setup Step 17): the hall mirror (start, with the chase) → the junction → left and right corridors, and the dark with the hanging mirrors (Explore) past the left corridor. Travel is 5s and 6 from all pools, trains Endurance. The two corridor searches cost 40 vitality each, so she can't do both in one run. The way into the hall opens when Lost Roland flips.

---

## 2026-09-24: The opening brief (prologue and loops 1–10)

Full text: `opening-brief.md`. Key rules it adds, which override GDD v0.3 where they differ:

- **The prologue starts with all seven pools full**, then the reflection takes the gem. (GDD §5 says no pools at the start; that becomes true from loop 2.)
- **Two run endings.** *Yanked out* (vitality hits zero): keeps knowledge, loses carried items. *Walked out* (exit through an exit node): keeps carried items, but pays the trip home and ends with vitality unspent.
- **Entries and exits are one-way valves:** enter through the real lab and exit through the mirror lab, or enter through the mirror lab and exit through the hall mirror.
- **Carried items** are a new kind of item. Anything carried out can be carried in on a later run, but it's lost unless it's carried out again. Some items **tax while held** (the ring: extra vitality per action) and are meant to be dropped.
- **Hub split:** the real lab is the between-run hub (meta currency, translating Roland's notes, build allocation). This supersedes GDD §5, which puts translation in the mirror lab. The mirror lab is an in-run safe node with no drain.
- **Node types:** Hall nodes (junctions, dead ends, doors), constructed nodes, Clara's memories (full verb set) and Roland's memories (watch and gather only). About a third should not be memories.
- **Pools can start empty** (the Amber gem is crafted empty).
- **Connections can have conditions** (the sealed lab memory needs the ring).
- **Answers to the implementation questions:**
  - From loop 2, **runs begin at the hall mirror entrance** (stepping in from the real lab). The exit is the mirror lab, sealed until loop 10, so until then every run ends by being yanked out. The second entry (through the mirror lab, exiting by the hall mirror) opens later.
  - **Ending a run early counts as being yanked out:** carried items are lost.
  - **Carrying items in is chosen per run** on the Plan page (a packing list). Items left in the real lab are safe.
  - **The ring's tax is extra vitality per second of any action** (travel included) while held. Tune it so ten corridors become about seven.

---

## 2026-09-24: Hue list matches GDD v0.3; the node graph

- **The seven hues follow GDD v0.3 §5:** Amber, Citrine, Emerald, Sapphire, Iolite, Amethyst, Ruby. Aquamarine is gone, and Sapphire is now 4th.
- **Travel is planned one step at a time.** The build lists each move ("Travel to the Hall"), and only neighbouring nodes can be picked. The route is part of the puzzle.
- **The node graph goes straight to a visual map** rather than the GDD's text list first. The map is built on node data, so the rule that nodes are data from day one still holds.

---

## 2026-09-24: Drain is action-driven; gem hue names; per-build end of queue

- **Nothing drains with time.** In Scott's words: "traveling is an action so drain can be action based rather than time based; time is a resource for the player, not the character." Every drain comes from an action Clara takes.
  - **Travel inside the Hall will drain faster** than acting inside a realm. It's one more action (Traverse), and costs more there. *To build with the node graph* (e.g. a per-location cost multiplier).
  - **A low or empty pool no longer blocks a task.** It starts anyway, and whatever the pool can't cover comes out of vitality. Only a hue Clara hasn't learned yet blocks it.
  - **Composure** now only softens what actions take from the hue pools (never vitality). Its old "slows the pools' own drain" effect is gone, because pools no longer drain on their own.
  - *Placeholder rule:* with nothing draining over time, **a free task can't be the repeated last task** (the run would never end), so that stalls the run.
- **Hue names are gem names:** Amber, Citrine, Emerald, Aquamarine, Sapphire, Amethyst, Ruby. v0.3's Red…Violet table was a misreading. *The GDD's hue table needs updating in the design session.*
- **Each build has its own end-of-queue choice:** repeat the last task, or pause. It's saved with the build. LoopSettings only sets what a brand-new build starts with (repeat).

---

## 2026-09-24: GDD v0.3 adopted

- **GDD v0.3 replaces v0.2** (moved to `Archive/`). It's written in a separate design session; this log records decisions made while building.
- The implementation review, with conflicts, gaps and the build's status against v0.3, is in `GDD-v0.3-review.md`.
- *Pending Scott's calls* (from the review): length (15–25 vs 40–60); mid-run editing; per-action mastery dropped?; how pool shape is set; what caps Gather. (Drain, hue names and the end-of-queue flag are settled; see the entry above.)

Decisions made in conversation with Claude, newest first. **Where this log and the GDD or spreadsheet disagree, this log wins** until the GDD is updated to match.

Everything is still greybox: any decision here can be changed.

---

## 2026-09-24: Save slots and the Menu page

- **Three save slots.**
- A **third page, the Menu**: save/load/new game, with room for **Settings** and **Achievements** later. It is **the first page the game opens on** (in the editor too), so testing can start with a new game or continue one in progress.
- **Bookmark tabs** at the bottom switch between the three pages (Menu, Plan, Run & Story), to be reskinned later as bookmarks in a book.
- Saving follows the agreed plan: permanent progress only (not the run in progress), by content ID, autosaved at loop end, on switch flips and on quit; JSON in the standard save folder; format versioned; missing content skipped with a warning.
- *Placeholder rules:* New game over a saved slot, and Delete, both need a second click. Loading a different slot saves the current game first. Plan and Run tabs are disabled until a game is going.

---

## 2026-09-24: One kind of resource; permanent IDs for saving

- **Tools, stats and kept resources are one kind of asset** (`ResourceDefinition`) with a **Lasts** setting: *This run* (Tool A, Focus) or *Forever* (Mirrors found). Both kinds can have a maximum. Tasks have one **Needs** list and one **Gives** list; switches can watch either kind. No more typed names anywhere.
- **Every piece of content has a permanent ID** (via the `ContentAsset` parent class), so save files survive renames and moves.
- *Later, if needed:* meta currencies could be a child class of the resource, if they need special behaviour (spending, exchange).

---

## 2026-09-24: "Bearings" renamed "Mirrors found", now its own asset

- The resource is called **Mirrors found**.
- It's a **Kept Resource asset** (`Assets/Data/Kept/MirrorsFound`) holding its display name and maximum (10). Explore and both story switches **point at the asset** instead of typing its name, so renaming it or changing its maximum happens in one place.
- Per-run tools and stats (e.g. Tool A, Focus) are still typed names. They could move to assets the same way if they multiply.

---

## 2026-09-24: Run controls, repeating the last task, and stalls

- **When the build runs out, Clara repeats the last task** until her vitality is spent. This is the default; the old "pause" behaviour is a LoopSettings option.
- **Stalls end the run.** If the last task can't be repeated (blocked, or its pool is empty), or the build is empty, the anchor pulls her back, and the results card says why.
- **Players can pause, and end a run early.** Pause/Resume and End run (click twice to confirm) show on both screens during a run. While paused, the build can be edited on the Plan tab.
- *Placeholder rule:* ending a run early is **not** a collapse, so it doesn't trigger "failed while doing X" switches (e.g. the chase).
- **Bearings: the count and the maximum are the same thing.** There is no hidden "lifetime" total. At 10/10, any more Bearings found are simply not kept or counted. "The Other Laboratory" now triggers the moment she holds **10**. (This replaces the earlier "20 found in total" interpretation.) A switch set above a resource's maximum can't trigger until the maximum is raised.

---

## 2026-09-24: XP follows work; tasks can be retired

- **Bug fixed:** mastery wasn't raising Clara's level from run to run. XP followed *time spent*, but runs are limited by cost (e.g. five explores' worth of vitality), so a faster Clara earned *less* XP per explore, cancelling out mastery. **XP now follows work done:** a task is worth the same XP however fast she does it, and mastery multiplies that. Each run's level now climbs as mastery grows, until mastery hits its cap.
- **Tasks can be switched on and off by game state.** Starts Unlocked is the opening state. Switches turn tasks **on** (Unlocks Tasks) and **off** (Locks Tasks). A locked task is hidden and removed from the build. A later switch can bring it back.
- **The chase is retired after it fails.** The "Lost Roland" switch locks Chase Roland, railroading the player away from a useless action. It will be unlocked again later in the game.

---

## 2026-09-23: Bearings, mastery caps, and speed that applies mid-task

- **Speed now applies continuously.** A level-up partway through a task speeds up that task immediately (previously speed was fixed when a task started, so long tasks like the chase never benefited). The total cost is still spent in full, in step with the work.
- **Bearings** (from Explore the mirror realm) are **permanent**, up to a **maximum of 10**. The maximum will be raisable later (meta currency).
- **Attribute mastery** (expertise in code) is permanent, with a **maximum level of 10** per attribute, also raisable later by meta currency.
- **Story:**
  - **5 Bearings found:** Clara finds dozens of mirrors floating in the dark, each looking onto rooms and dreamlike visions.
  - **20 Bearings found:** another mirror in the halls shows a duplicate of Roland's laboratory. Unlike her own reflection, it holds a book: her memory of first helping Roland translate the tomes to craft a gem that can store pathos.
- *Interpretation:* with a cap of 10, "20 Bearings" counts **every Bearing ever found**, including those found at the cap. Each switch has a *Count Lifetime* tickbox to change this.
- **Not yet built:** saving. Everything permanent survives loops, but not stopping Play or quitting.
- *Next, probably:* the "other laboratory" beat sets up the gem, a natural moment to unlock Clara's **first pathos pool**.

---

## 2026-09-23: Clara's attributes

- Five attributes Clara levels up:
  - **Endurance:** physical action speed (walking, climbing, hauling).
  - **Perception:** search speed and what's revealed. Hidden things need thresholds.
  - **Scholarship:** reading, deciphering, translating Roland's notes.
  - **Attunement:** pathos cost per working. Economy.
  - **Composure:** drain resistance, and holding steady under pressure. *(New; not in GDD v0.2.)*
- Following GDD §7: **levels reset every loop, and expertise persists** and speeds up future levelling.
- *Placeholder mechanics (numbers in LoopSettings):*
  - Each task names the attribute it trains, and earns XP per second of work. Each level needs more XP than the last.
  - Endurance, Perception and Scholarship make **their own** tasks faster, worked out when the task starts.
  - Attunement cuts every **hue** cost (vitality costs unchanged). Composure slows all **background** drain.
  - A share of XP becomes permanent **expertise**, and each expertise level adds XP gain.
  - "Hidden things need thresholds": tasks can **require an attribute level** (e.g. Perception 2), and switches can trigger when an attribute **reaches a level** in a run.
- **Tutorial tasks:** Chase Roland trains Endurance; Explore the mirror realm trains Perception.
- **Answered:**
  - **Composure vs Drain Resistance: both exist, with different jobs.** Composure only applies to *actions that affect the mana (hue) pools*. **Drain Resistance** is a **meta attribute** that applies *specifically to the drain Clara suffers in the Hall of Mirrors*. *(Code still has Composure slowing all background drain, including vitality. Needs changing; see the question below.)*
  - "Holding steady under pressure" is a **placeholder**. It will become relevant as events and tasks are added.
  - **Tasks may train or be affected by several attributes, and several pools.** Multiple pools already work (cost shares); multiple attributes is still to build.
- **Composure affects both** (tune during balancing):
  - it slows the hue pools' own drain, and
  - it softens what tasks take from the hue pools.
  
  It **never touches vitality**. The two effects are separate numbers in LoopSettings. The Hall's drain will get Drain Resistance (meta) once locations exist.
- **Stats box UI:** each attribute shows two bars, touching. The top bar is this run's level. The bar beneath is the permanent progress, labelled **"mastery"** on screen.
- *Open (naming):* in GDD v0.2, an attribute's permanent progress is **expertise**, and **mastery** is per *task* (repeating a task makes it faster). The stats box currently calls expertise "mastery" on screen. Decide which word goes where before per-task mastery is built.

---

## 2026-09-23: Screens as pages of a book

- The UI is split into **screens**, which will eventually turn like **pages in a book**. There may be more than two later, e.g. a journal page.
- **Planning screen:** choose tasks, edit the build, begin the run.
- **Run & Story screen:** watch the run (bars, current task, tools and stats, log), read the story, and see the **results** when the run ends.
- *Placeholder behaviour:* Begin turns to the Run screen automatically. When a run ends, the Run screen shows a results card with "Plan the next run". Tabs let the player flip either way at any time. Story pop-ups appear over whichever screen is showing. The transition is a quick fade until the page-turn is designed.
- *Open:* should story beats be shown *on* the story page (like reading the book) rather than as pop-ups? Is a separate journal page wanted?

---

## 2026-09-23: Costs can come from vitality, or be split across pools

- A task's cost is a **total** plus a list of **cost shares**. Each share names a source, **Vitality** or any hue, and a **percentage** of the total. E.g. Chase Roland = 100% Vitality; a later working = 50% Amber + 50% Citrine. Shares that don't add up to 100 are scaled to fit.
- Each share drains evenly over the task, like before.
- *Placeholder rules:* hue shares must be affordable at the start or the task is skipped. **Vitality shares are never checked up front**, because running out of vitality mid-task is how a run ends (the tutorial chase relies on it).
- Suggested Chase numbers: 60s, 100 Vitality. With vitality's own 1/s drain, she collapses about 38s in, roughly 60% of the way.

---

## 2026-09-23: The tutorial opening

- **Story:** Clara has just chased Roland into the mirror and sees him being hauled away by his own dark reflection.
- **Loop 1:** only **vitality** is unlocked, and the only task is **Chase Roland**. The chase is one long task, longer than her vitality lasts (about 1.5x), so she **always fails the first run**: the player watches her get close and collapse short.
- Failing the chase flips the first **switch** ("Lost Roland"). That shows a **story pop-up** and unlocks **Explore the mirror realm**.
- The first few tasks work as a **tutorial**, introducing game concepts and stats one at a time.
- **Story text** is written as `.txt` files in `Assets/Story/` (first line = title, the rest = passage). Pop-ups pause a running loop. Every beat shown is kept in Clara's **journal** (a journal screen comes later).
- Pathos pools now start **locked** and are unlocked by switches. This replaces the placeholder Unlocked tickboxes.
- *Open:* what Explore grants and where the tutorial goes next; which switch unlocks the first pool, and which hue it is; whether the placeholder passage's "something at her throat pulled" (the anchor gem) is right.

---

## 2026-09-23: Task costs drain while the task runs

- A task's cost is a **total**, **drained evenly (linearly) over the task's duration**, not paid up front. E.g. 20 Amber over 10s drains 2 Amber per second while Clara works. The full total is always spent by the end.
- Different tasks drain different pools (each task picks its cost hue).
- **Later:** experience and meta currencies will **reduce the drain** or **make tasks faster**. A faster task still spends its full (possibly reduced) total, just over less time. The code has one place for each: `TotalCostOf` and `TicksNeededFor` in `Simulation`.
- *Placeholder rules:* a task only starts if its pool holds the whole cost. If background drain empties the pool mid-task anyway, the rest of the cost falls on vitality (the same overflow rule as pool drain).

---

## 2026-09-23: Build entries with counts; pause when the build runs out

- Tasks are added in amounts: **x1, x5, x10** (more amounts can be added later, set per task row in the Inspector). A build entry is a task plus a count, e.g. "Task B x10 (3/10)", like *Idle Loops*. Adding the same task again merges into the last entry.
- If an entry's task can't start, the **rest of that entry** is skipped (logged once), and the build moves on.
- **When the build runs out mid-run, the game pauses** so Clara isn't left idling while her pools drain. The player adds tasks and presses **Resume**. *(Interpretation of "pause once the final step is achieved". Confirm, or change it to pausing when a switch flips.)*

---

## 2026-09-23: Planning between runs

- After vitality is exhausted, the game goes to a **planning screen** where the player reprograms the build before starting the next run. Nothing drains while planning (matches GDD §9: in the Hub, no drain).
- The game also **opens** in planning, so the first run can be set up before anything starts.
- The build can still be edited during a run (it's live). The planning screen is the dedicated place for it.
- Greybox build editing: add by clicking a task, remove by clicking an entry, plus "Remove last" and "Clear build". Reordering and inserting are for later.

---

## 2026-09-23: The action queue is a saved build

- The action queue is **saved between loops** and replays each loop, as in *Idle Loops* and *Stuck in Time*. It's called the **build** (GDD §9: "the build for the next run is set here").
- Editing the build during a loop changes this loop and every loop after it. Each loop starts again from the top of the build.
- **Later feature:** keep several builds and swap between them.

---

## 2026-09-23: Hue names and order

- The seven hues are named for gems, in this order: **Amber, Citrine, Emerald, Aquamarine, Sapphire, Amethyst, Ruby.** In code this is the `Hue` enum.
- This differs from GDD §6 in two ways:
  - **Names:** the GDD uses Red, Amber, Yellow, Green, Blue, Indigo, Violet. Rough mapping: Ruby ≈ Red, Citrine ≈ Yellow, Emerald ≈ Green, Aquamarine ≈ Blue, Sapphire ≈ Indigo, Amethyst ≈ Violet.
  - **Order:** Ruby is **last**, where the GDD's wheel puts Red first.
- *Open:* is Ruby-last deliberate? E.g. love as the final hue, echoing the book, where scarlet/love is the rarest. Does this order also set the unlock order (the GDD's "you cannot skip a station")? How do the book's other hues (rose, maroon, bronze, lavender…) fit in?
- The greybox now supports all seven pools. Each has an **Unlocked** tickbox in LoopSettings as a stand-in until unlocking is built.

---

## 2026-09-23: Source material and Clara's starting speed

- **`Plot Summary of Colours of Longing.txt` summarises the book, not the game.** The game's plot hasn't been written yet. In the game, the player will experience book events as **memories** (e.g. inside the Realms). The summary is background on the source material; don't treat it as game story.
- **Clara starts very slow.** At the start of the game her actions should be very slow, leaving plenty of room to level up and become proficient as the game goes on. *(Design intent. It will be implemented through task durations, attributes, and mastery, not the tick rate.)*

---

## 2026-09-23: Follow-ups

- **Pools (confirmed):** the greybox uses one generic magic pool. In the full game, a new pool is added each time Clara learns a new school of magic.
- **Spelling:** the book uses British spelling, so all player-facing game text does too (colour, grey, armour).
- **Scope:** Claude's milestone plan below is **adopted**: greybox loop → vertical slice → 1–2 hour demo → full game, with the length decided from playtesting.
- New reference file in this folder: `Plot Summary of Clours of Longing.txt`.

---

## 2026-09-23: First design review

### Status of the design docs
- The GDD (v0.2) and the lock & key spreadsheet (v0.1) are **works in progress, not ground truth**. Treat all of it as open to change, including items tagged [DECIDED].

### Vitality, pathos, and how a loop ends
- The loop ends when Clara's **vitality** (her health) runs out, not when pathos runs out.
- Pathos pools drain first. **When a pool is empty, its drain carries on and hits vitality instead.** Each empty pool adds to the drain on vitality.
- The tension this creates: the player wants to keep all pools balanced so none empties early, but some runs need a lot of one or two hues.
- *Supersedes GDD §7 "Emotion is the clock" (loop ends when all seven pools are empty) and answers GDD Open Question #4.*

### What Clara starts with
- At the start of the game Clara has **only her own vitality**. She has no pathos pools yet.
- She has to learn or gain access to each pool of magic over the course of the game.
- *Open:* whether the Hall drains vitality directly before she has any pools. Presumably yes, since vitality is all she has.

### Greybox scope for pathos
- For the first greybox: **one generic "magic" pool** rather than seven hue pools.
- Leave pathos **gathering** out of the first greybox (drain only). GDD Open Question #1 is still open.
- The seven hue pools come back once the core loop is proven.

### Switches
- A switch is flipped by completing a **short series of tasks**.
- A task may need a certain stat or tool. Those stats and tools **reset each loop**.
- Flipping the switch is the **permanent**, persistent reward. The per-loop stats and tools are how Clara reaches that goal within a run.

### Combination magic
- Mid and late game will likely need **combinations of pools** (e.g. the spreadsheet's two-hue spells). Not designed yet. Later.

### Shadow and Clara's reflection
- **Tabled** until further development. Don't build Shadow or reflection mechanics yet.

### Endings
- There may be **several endings**. The spreadsheet's single "Reconciliation" ending is a placeholder.

### Meta currencies
- There will likely be **several meta currencies**, including experience and certain tools. Gems are not the only one. Names and roles are open.

### The books and IP
- The author of this game is also the author of the book (working title *Whispers of Want*, likely to be published as *Colours of Longing*). The book is an unpublished draft. There are no third-party rights concerns with using its setting and characters.
- The GDD's end-game screen promoting the book should use whichever title is final at release.

### Scope
- Not settled. The author is open to advice. See Claude's recommendation below.

### Art and sound
- Plan: **AI tools and open libraries**.
- Implications: Steam requires an AI-content disclosure on the store page. Open-library assets must allow commercial use; CC0 is simplest, CC-BY needs a credits entry, and anything "non-commercial" (NC) can't be used.

---

## Claude's scope recommendation (adopted 2026-09-23)

Build in milestones and decide the final length from real playtesting, not up front:

1. **Greybox loop.** Vitality, one magic pool, drain, a handful of tasks and one switch. Goal: is the loop fun?
2. **Vertical slice.** Prologue, Hub, Hall and the Red realm, still mostly text and bars.
3. **Demo (1–2 hours).** Roughly the spreadsheet's Early phase, with first-pass art and sound. Put up the Steam store page around here to start collecting wishlists, and consider a Steam Next Fest.
4. **Full game.** Pick the final length based on how the demo plays. A tight 15–25 hours is more realistic for a first solo project than 40–60, and players rarely complain that a good game ends too soon.
