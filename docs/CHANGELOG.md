# Changelog

What a player can now do or see, newest first: one line per finished feature, in player language (no class names, British spelling), with the plan in brackets. `/wrap-up` adds a line when a feature is finished. Work the player can't see (refactors, tools, tests) isn't listed; `git log` has it. These lines are the first draft of the Steam page's "What's new".

## 2026-10-02
- Act I can be finished again from a new save: the drain grows more slowly, every action's time is a tidy number, and *Feed your hours to the flames* gets quicker as Invoking grows. (balance pass)
- Restoratives now step in when you need them: a dense wisp or phial is used as soon as the drain would take back what it gives, so a strong one is no longer saved until you're nearly empty. (balance pass)
- The white sapphire earrings and dense wisps turn up on your first search of the Mirror's Laboratory, and the earrings hold 10 dense wisps. (balance pass)
- The Left Corridor's candles must be lit again on every run before you can go into the Dark Corridor. (plan 055)
- Talking to Roland is one long effort: watching him, studying the tome and attending each give you a lasting insight once a run, and every insight makes the talk shorter. It's long at first, so expect a few visits; each try still makes you better at it. (plan 055)
- Until you've earned planning, the Summary has one "Go back in" button, with a line from Clara above it. (plan 055)
- The Summary now lists what each rising charge cost you: after Moves, every task whose cost climbed during the run is named with how many times you did it and the vitality it took ("Practise the cut: 4, costing 9.4 vitality"). (plan ui-051)

## 2026-10-01
- Action tooltips have a new shape: a small-caps line naming the kind of action and the room, a serif title, then rows for what it takes (the time large, with "from X base" once a skill speeds it), costs (vitality in rose, with a note that it's flat), what it needs (✓ met, ○ not yet), gives, trains (a chip each) and which skill makes it faster, with a footer for how it repeats and how far a search has got. It sits beside what it describes and never covers it, and moving down a list opens each one at once after the first. Pick up and Put down get a short one-line form. (plan ui-036a)
- Menu → Summary shows the last run's report again after you load a game. (plan 032b)
- The planning screen's map is finished: rooms, ways and reachable marks are in the warm gilt-and-dark colours, each room's tag floats above its neighbours with a smaller speed line, and a stop that would be refused (or won't carry over to the next run) shows ⚠ on its room's tag too, with the reasons when you hover. A folded block's ⚠ lists its reasons, and a refusal notice no longer covers the Begin button. (plan ui-033)
- Act I has no stats: her stats sleep and earn nothing, and her maximum vitality stays the same from run to run. Walking home for the first time wakes all five at zero; from then on a walk out keeps all of a run's stat XP as mastery, and a collapse or ending a run keeps half. Mastery has no ceiling now. Start a new save to play it. (plan 041)
- Buttons and chips are now built from a few shared templates, so a later restyle changes them all at once. The one visible change: the room popover's close button (and the speed button) is warm like the others, not white. (plan ui-035)
- A refused trip now says where she was going ("Can't travel to the Long Gallery: …"), and queue buttons act on the row you clicked even as a trip finishes. A running by-heart block's "2 of 5" drops when you remove an action from it. (plan ui-034)
- When an action needs both a stat or skill she lacks and an item she doesn't have, the reason names the stat or skill first, since it takes runs to earn while an item takes only a trip; Carry is no longer offered for things that hold other things.
- The Mirror's Laboratory has its ending: put the room to rights, talk to Roland and give him the ring, find the white sapphire earrings and fill them with dense wisps, practise the cut, craft the gem, then walk home through the mirror she came in by. Act I can be played start to finish. (plans 027a, 027b, 027c)

## 2026-09-30
- Planning between runs: after the Summary, choose **Repeat** to start straight away or **Plan** to open a planning screen with the map and the queue, then **Begin**. (plans ui-024b, ui-024c)
- Warnings ahead: anything in the plan that will be refused when its turn comes is marked ⚠ with the reason, before the run starts. (plan ui-024d)
- Planning is earned by feeding your hours to the flames, and rooms known by heart glow a soft gold. (plan 032a)
- Rooms she has worked in over several runs become known by heart: what she did there carries into the next run's plan, and time runs faster there, up to five times as fast once she has fed her hours to the flames. (plans 023, 025, 029)
- Every stat has its own job: Endurance banks a little extra maximum vitality after each run, Composure softens extra drains, Perception reveals hidden finds, and Attunement makes restoratives give back more once it wakes. (plan 031)
- Each move between rooms costs a little more vitality than the last, within a run. (plan 030b)
- The game moves at a brisker pace. (plan 022b)
- A new look: a dark backdrop with a vignette behind the map, and one slim strip across the top. (plan ui-026)
- Searching a room is done once, ever: the bar is kept, even partway, and what she finds stays found in every later run. (plan 020)
- Her first entry into a room each run shows a milestone card comparing this run's time with the last; her first entry ever opens that room's story passage. (plan 021)

## 2026-09-29
- Schedule actions in any room she can reach; the walk there is added for you. (plan ui-015)
- The queue sits in a column on the right, beside the Story box, and folds away to widen the map. (plan ui-019)
- Every stat and skill has an icon, and their tooltips are tidy tables. (plan 010)
- Clearer text: tooltips, story and small print read cleanly at 1920×1080 and 1280×720. (plan 011)
- The Summary shows how the run's time was split across her skills, and her milestones against the last two runs. (plans 016, 017)
- The ambient lines take turns instead of one voice crowding the feed, and only the newest few stay on screen. (plan 015)
- *Light a candle* leaves a room's actions once every candle there is lit. (plan 013)

## 2026-09-28
- A new main screen built around the map: click a room for its actions, its floor and the ways on; a ribbon shows what Clara is doing and what's next; the queue shows each stop as a card you can drag to reorder; the route is drawn on the map with Clara moving along it; things left on floors show as dots; a small overlay shows what's in her pockets. (plans 002–009)

## 2026-09-27
- The Summary compares this run's length with the last run and the longest ever, and marks a new record. (plan 001)
