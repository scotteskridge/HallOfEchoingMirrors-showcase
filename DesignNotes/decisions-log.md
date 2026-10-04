# Design Decisions Log

---

## 2026-10-02: "Faster than last run" Core: what's kept, what counts (plan 042; nothing visible yet)

Core only: the display is plan ui-044, so no GDD edit yet (§13a *Benchmarks* is edited when ui-044 shows it).
- **The kept value is each skill's mastery XP at the start of the last run, not its speed multiplier** (a deviation from `docs/mockups/tooltips/design-notes.md`, OK'd by the user with the plan). Why: speed is worked out from it on demand, so retuning `skillMasterySpeedPerLevel` in the Balance Sheet never shows a change she didn't earn.
- **Mastery only, as each run began** (the user, 2026-10-01): the figure holds still all run; level speed, room speed and what she holds are left out.
- **Every run counts for best-ever times and for last run's start**, collapsed, ended early and dev-restarted included: a time she reached is a real time. (Longest run still skips early-ended runs, because it measures length.)
- **A skill with no entry counts as 0 once any run has ended** (the user, 2026-10-02: steadier than "no value"). So a skill first trained last run shows its gain at once. Before any run has ended, and in saves from before version 24, there is no figure (null), kept by one flag, `PersistentState.HasLastRunStart`.
- **Save version 24:** `skillMasteryXpAtLastRunStart` (skill `Id`, XP) and `hasLastRunStart`; the upgrade step fills nothing in.
- Whether a figure of exactly zero is hidden is a display rule for ui-044.

---

## 2026-10-02: Planned rooms: the map planner is built (UI lane, plan ui-053; no design change)

Tooling only, so no GDD change and nothing the player sees.
- **A planned room is an ordinary room asset ticked *Planned*** (and a one-line planning note), listed in `GameContent.nodes`. Why: promoting it to the game is then just clearing the tick, with no moving between lists.
- **The game ignores it completely:** `GameContent.PlayableNodes` is the one place the filter lives; the map, ways, saves and Summary never see a planned room. A way into a planned room, or out of one (even both ways), does nothing in the game and is drawn only in the preview. A save can't name a planned room (`ContentIndex`).
- **Shipped-content tests walk only playable rooms** (the user signed this off 2026-10-02). Not a weakened test: planned rooms aren't game rooms. Once a room loses its tick it is checked as before, so the depth test then asks for its depth.
- **Tools:** *Hall of Echoing Mirrors → Tools → Map Layout* (preview, no camera jump) and *Add Planned Room*, the Map View's *Add Room* button; planned rooms and their ways are drawn faded; Balance Sheet Places has Planned and Note columns.
- **Signed off with the new rooms still Planned:** the verbs that unlock them aren't made yet. The Hall Mirror was renamed *The Smoky Mirror* by the user; the shipped tests follow the new asset name.
- **Next:** mirror shapes per room kind and per-room size, plan `ui-054` (UI-BACKLOG *Next* 15).

---

## 2026-10-02: Act I balance pass, the lab's talk rebuilt, candles every run, and a drain-aware restore rule (backlog balance pass, plan 055)

From the user's answers in one Features-lane session (a cold playtest in the middle). Why, overall: after plan 041 removed stats the exit was out of reach; the playtest then showed the lab played like a checklist, the corridor was a one-off, and dense wisps were never used when needed.
- **Balance (Balance Sheet only):** drain growth 0.5625 → 0.4 a minute (Composure used to slow it); every task's Time × kept to 3 decimals (a test enforces it; the user's rule); *Feed your hours* trains Invoking (it had no skill, so nothing sped it up); XP per second of task stays 1.5 (tried 1.8, put back so mastery is earned over runs). Hanging Mirrors' search and Travel's charge unchanged.
- **Phials and wisps are required** to reach the lab with the ring (the user: intended). The ring's cost stays.
- **Earrings and dense wisps are found in the lab's first search** (15%), not after the talk; the earrings hold 10 dense wisps (was 2). **Supersedes** 2026-09-29 *The Mirror's Laboratory* "phase 2: the earrings found by Search only after phase 1". The story text still says "two at most" (the user's to change).
- **A restorative starts when what she's missing, plus what the steady drain will take while it gives back, covers what it gives** (the user's rule): the drain, a task's extra drain and a carried item's cost count; one-off charges don't. A drain as fast as an item gives uses it at once. One of each kind at a time, as before. Why: a 40-point dense wisp on a 50 bar fired only at 10 or less, and anything mid-restore blocked it. New number in Core, not yet shown: her max restore per second (UI backlog).
- **The corridor's 10 candles must be lit again every run** to enter the Dark Corridor (the user: otherwise runs are too easy, Feed included). The first lighting still plays its story, unlocks Feed and opens the way for good.
- **The lab's talk is one long push, shortened by kept insight** (the user chose a "persuasion bar" that resets): *Talk to Roland* needs only the ring; *Watch him*, *Study the tome* and *Attend* each give 1 kept insight once a run (cap 6); each insight makes the talk ×0.85; the talk is long (Time × 6, ~160 s with none) so first visits fail but still train Convincing. Understanding and Steady hands are no longer used. The user wants this stretch **grindy**: repeated runs and mastery against the drain, never "not enough insight". **Supersedes** 2026-09-29's fixed prerequisites (3 insight, 2 Understanding, Attend). Claude's call: *What He Did That Night* fires on finishing *Watch him*.
- **Before planning is earned the Summary offers one button, "Go back in"**, with "I wake at the Smoky Mirror. Again." above it (placeholder wording); Repeat and Plan after *Feed your hours*.
- **Balance tools** for future passes: the probe, what-ifs and snapshot (`docs/balance-tools.md`, `/balance`). Measured: tidy play walks out on run 9 (3 lab visits, one failed talk), loose play on 15. **The user expects more passes like this one, after more features are built.**
- **GDD:** §12 *The memory lab*, §12a *Act I's arc to the lab* and §5's restore wording to edit (proposed at this wrap-up).

---

## 2026-10-02: Mirror shapes: curves are memories (planning ui-054, nothing built)

- **Which shape for which kind** (settles GDD §13a [OPEN]): Hall rectangle, Constructed octagon, Clara memory oval, Roland memory arch. Why: pillar 4; the player learns one rule at a glance (curved = memory, corners = the Hall's own rooms), then the detail. Hall rooms are the most common, so they keep today's look. Rejected: one oval for both memories (the shape couldn't say whether Clara can be seen); four unrelated shapes (no family rule to learn).
- **The kind decides the shape:** no per-room override, so the shape never lies to the player. Size still varies per room (0.75 to 2, a placeholder range).
- **Tags** sit under each shape's lowest edge; **planned rooms** keep the 45% fade (no outline-only look); **the by-heart glow** matches its room's shape and size, including any resize.
- **GDD:** §13a updated.

## 2026-10-02: Mirror shapes carry meaning; acts are planned in the editor (design session, nothing built)

- **Room shape signals room kind** (Scott: "having some inherent meaning is better" than purely aesthetic shapes). Which shapes: open. Why: pillar 4, the player reads the map by heart. GDD §13a *A room's shape says what kind of room it is*.
- **Next acts are laid out in Unity, not sketched on paper:** planned rooms (shown in the editor, skipped by the game), Add Room, ways and a one-line note per planned room, and a *Tools → Map Layout* menu entry for the preview button that was hard to find (*Edit map layout* on the Map View, `MapLayoutEditing.cs`).
- **Timing:** not urgent, but in UI-BACKLOG *Next* 15 so it isn't lost; before Act II layout work starts. Folds in UI-BACKLOG-later *Rework of art asset for map nodes*.

## 2026-10-01: Scene saves: zeros are normal, and one layout driver per object (UI lane, no design change)

Why saving SampleScene rewrote ~740 layout values, and what we decided.
- **Unity 6 saves layout-driven RectTransform values (anchors, position, size) as 0,** once the editor has run the layout. The scene had real numbers in those fields, so any save after layout replaced them. The scene was normalised once (commit 635888d); later saves leave it alone. **0s in a scene diff are Unity's normal form: don't restore them by hand** (the hand-restore in c0ef286 is no longer needed).
- **Five Summary report blocks had two drivers** (their own ContentSizeFitter inside a parent stack), so their positions churned between saves. The parent stack now controls child height and the fitters are gone; the report's sizes are unchanged.
- **Rule:** a child of a layout group never gets its own ContentSizeFitter; `LayoutDriverTests` enforces it, and `.claude/rules/ui.md` says so.
- **GDD:** no change.

---

## 2026-10-02: Code structure only: room speed hold extracted (plan 052 #2)

No game rule or design point changed.
- **#2 `RoomSpeedHold` was judged worth doing, though small** (about 25 lines). The gain is encapsulation, not size: the run start calls `Reset()` instead of writing another topic's two fields. It doesn't settle the stop rule for #3 and #4; judge `PlanWarningsTracker` on its own, after the Act I playtests.
- **The hold stays unsaved:** a resumed run starts unheld (now pinned by a test).

---

## 2026-10-01: Code structure only: report builder extracted, scenes closed around git (plan 052 #1)

No game rule or design point changed; recorded so the reasons survive.
- **`RunReportBuilder` is `internal` and reads `Loop`/`Persistent` through the `Simulation` on each `Build()`**, not copies kept at construction: every run replaces `Loop`, so a kept copy would describe the first run forever. `LongestRun`, `LastRun` and `RestoreLastRun` stay on `Simulation` (the dev panel and saving use them). The plan listed 10 helpers; the report uses 7 (`StrengthOf` and `Charges` are not read).
- **Extractions continue only if each is visibly worth it** (plan 052's stop rule). #2 `RoomSpeedHold` moves about 25 lines, not 60, and the plan named the wrong readers (`GameController` and `QueueRibbon`, not `PauseControl`): judge it before doing it.
- **Scenes are closed before a git command that may change them (`SceneGitGuard`).** Git rewriting an open `.unity` makes Unity block on a "modified externally" dialog and every MCP call hang. Closing the scene (refused while it has unsaved changes) and reopening it afterwards avoids the dialog; chosen over an editor setting because it also never loses work. A scene diff that only changes anchors, sizes and positions is layout noise: discard, don't commit.

---

## 2026-10-01: What a full pocket pushes out; "I left it where it lay" stays (ring blurbs)

The `/writing` pass growing `ring_present`, `ring_carried` and `ring_refused`, with the user.
- **The item pushed out of her pockets is whatever she holds most of**, usually restorative items (wisps, phials). The ring text keeps saying "something". Answers the open point in 2026-09-29 *Instantiate never fails*.
- **`ring_refused`'s "I left it where it lay" stays:** it fits a ring left in its mirror (she would have to pull it out) and one she has put down.
- **GDD § changed:** none.

---

## 2026-10-01: Two voices: Clara's flavour says "I", the interface says "she"

The `/writing` pass over the lab's hover text, with the user.
- **The split is by job, not by surface.** The interface talks to the player about Clara: fact rows, refusals (`reasons.*`), feed notes and the Summary say "she". Clara talks to herself and the player overhears: passages, blurbs and the one-sentence flavour description on a task or item say "I". Nothing addresses the player as "you".
- **Why:** the player is the planner, not Clara; system text must read correctly in a hurry, so it describes her from outside. The flavour line is the voice guide's "one place a single figurative line earns its keep"; in "she" every tooltip reads like a manual, and Clara's voice is the game's selling point.
- **A flavour line shouldn't carry a number** the fact rows can show; her complaint about a cost is fine, the figure belongs in a row.
- **Narrows** the 2026-10-01 *System lines speak in third person* entry ("item and task descriptions say 'she'"). *Feed your hours*' description went back to "I"; the lab's and the Hall's descriptions are all "I".
- **GDD § changed:** none.

---

## 2026-10-01: The game is set in 1928; the lab's story beats

The `/writing` pass over the room and lab passages, with the user.
- **The game is set in 1928,** twenty-one years after the novel's 1907. Why: Clara is 41 and twenty-one years past her training; "1907" in the docs was the novel's year.
- **Roland's memory believes her** because she convinces him her older self is the same woman as the student his memory comes from (passage 22: she tells him about the earrings he has not yet given her).
- **The earrings** were Roland's gift: expensive, and magically attuned to her (the novel). That is all the game needs.
- **Walking home:** she panics about her children, finds that no time has passed, and decides to go back in for Roland (passage 26).
- **GDD § changed:** §3 (twenty-one years; 1928 Evercrest) and §12 (the real 1928 lab). `CLAUDE.md` updated to match. `VISION.md` still says 1907: the user's to change.

---

## 2026-10-01: System lines speak in third person; shut-way messages are text keys (plan 049)

The `/writing` pass over the planning placeholders, with the user.
- **Voice:** game-system lines (the feed's notes, every `reasons.*` line, item and task descriptions) say "she"; Clara's first person stays in blurbs and story passages. No line addresses the player as "you".
- **Feed your hours' note** now says both things its item unlocks: planning each run, and rooms known by heart passing faster (as the 2026-09-30 "Summary gets one line" entry intended).
- **Refusal line** keeps `({runs} of {needed} runs)` and its way out ("it can only be done during a run"): the player must be able to diagnose a refusal (§14.12).
- **The Dark Corridor's shut message names the Left Corridor's candles.** It is the only on-screen pointer to the candle task, so it may be poetic but never vaguer than "candles".
- **Shut-way messages are `game_text.txt` keys** (`Way.shutMessageKey`, here `reasons.shut_dark_corridor`), not English on the asset, so all player text lives in one file. An empty key still falls back to `reasons.way_shut`.
- `reasons.way_shut` ("the way is shut for now") and `feed.known_by_heart` were tightened in the same pass; their meaning is unchanged.
- **GDD § changed:** none.

---

## 2026-10-01: Plan 041 playtested: the first exit works; Act I balance blocks the rest (nothing new decided)

The user's check of plan 041, with a dev jump stage (*Gem in hand*, Step 103) added so the exit could be reached.
- **Confirmed:** stats do nothing in Act I; after the first walk out they are awake and the next run's stats are no longer greyed out.
- **Found:** at current balance a run can't reach the exit on its own (no vitality bank, no Composure, no stat learning bonuses). That is a balance problem, not a bug: the *Act I balance pass* is now the top of the backlog's *Now*.
- **Open question for `/design`:** after the exit, stats rise only from tasks that train them (Attunement from every Instantiate, Perception from Search and *Watch Him*, Composure/Endurance slowly); the Smoky Mirror, where every run now starts, trains none, so they looked frozen. The user's belief is that the stats belong to Act II. Plan 041 as settled wakes them at the first exit, and that is what is built. Whether they should instead earn nothing until Act II content exists is undecided.
- **GDD § changed:** none.

---

## 2026-10-01: Stats rework built (plan 041)

Built as settled in the entry below; what the build chose or found.
- **Order at a run's end:** the Endurance bank is paid first (with the Endurance the run had), then the run's stat XP settles into mastery. A dev restart does neither.
- **Skills are unchanged:** skill mastery still rises live; only stats hold theirs back.
- **An asleep Perception sees nothing hidden** (`SeesWell` now reads the effect strength), so "asleep" means no effect everywhere, not just in the numbers.
- **Mastery XP stays a float:** a test shows small gains still count at mastery 200.
- **Shipped numbers:** the build's old mastery cap of 150 is gone from the asset; `collapseStatXpShare` 0.5 sits in the Balance Sheet's Rules section. The balance pass is the user's.
- **GDD § changed:** §4 *Mastery has no cap*, §6 (stats exist from the first exit) and §12 (the first walk out wakes the stats) go from [DIRECTION] to [BUILT]; edits proposed at wrap-up.

---

## 2026-10-01: Buttons and chips come from kind prefabs (plan ui-035)

Settled with the user in a UI session.
- **The choice:** every button and chip template is an instance of a kind prefab in `Assets/Prefabs/UI/Kinds/`: Button, AccentButton, IconButton, TabButton, SmallButton, Chip, ChipFaint. A kind owns the face (sprite, colour role), the Button transition and colours, and the label font and colour roles; each place keeps its size, layout, words, tooltip and view scripts.
- **Why:** a restyle (tooltips, star bar, juice) is then one prefab edit, not forty.
- **GDD section changed:** none (tooling).

---

## 2026-10-01: How stats arrive at the first exit, and what each ending settles (plan 041, nothing built)

Settled with the user while planning plan 041 (the stats rework), each checked against the code first.
- **Stats arrive at zero.** The first walk out wakes all five at level 0, mastery 0. The one-off starting amount stays `[PROPOSED]` (2026-09-30 *Features come in layers*), for the *stats arrive* moment to revisit.
- **No Endurance bank in Act I.** The bank (3% of vitality lost, more with Endurance) is off until the stats wake, so max vitality stays 50 every Act I run. Why: banking is Endurance's job, and Act I has no stats; Act I is rebalanced for it (the user, in the Balance Sheet).
- **Stat mastery settles when a run ends.** During a run, stat XP raises only this run's levels; at the end it goes into mastery: all of it on a walk out, `collapseStatXpShare` (**placeholder rule:** 0.5) when she runs dry. Skill mastery is unchanged (still rises live). Why: a share kept on collapse needs the run's gain held apart; the alternative (mastery rising live, then half taken back) would show mastery dropping on the Summary.
- **End run settles like a collapse.** Only walking out keeps everything. Why: otherwise ending a run early is a free walk out.
- **After the first exit, runs carry on.** She steps back in at the Smoky Mirror with her stats awake; the exit stays offered every run while she holds the gem (already built), and later walk outs are ordinary endings. Nothing else is added until the Hub and Act II.
- **An asleep stat earns no XP.** "No stats in Act I" reuses plan 031's asleep stats (all five start asleep; the *Home* switch, the first walk out, wakes them; *Roland Takes the Ring* no longer wakes Attunement). **Supersedes** plan 031's "an asleep stat still gains XP" and 2026-09-30 *Stats pass*'s "Attunement is asleep until the talk… it still gains XP".
- **Mastery uncapped** as GDD §4 already says; the build's cap goes in plan 041.
- **Old saves aren't carried over** (the user): start a new save after plan 041.
- **GDD § changed:** §12 *The three run endings* (proposed with this entry; it still tied walking out to meta currency, stale since 2026-09-30).

---

## 2026-10-01: An empty stop isn't marked as won't-carry (plan ui-033 follow-ups)

Settled with the user in a UI session.
- **The choice:** a stop with nothing queued in it wears no won't-carry ⚠, in the Queue column or on the map tag. In practice only the first stop (the start room, before she knows it by heart) can be empty; its grey "by heart in N of 4 runs" note stays.
- **Why:** nothing in an empty stop can be lost, so the ⚠ was a false alarm on every early planning screen, against the warnings rule of staying silent rather than crying wolf (2026-09-30 *Warnings ahead: the rule*). The two screens disagreed before; one rule (`StopCard.MarksNotCarried`) now decides both.
- **GDD § changed:** none (§13a no longer describes the marks; the build is in `docs/plans/finished/ui-033-planning-screen-finish.md`).

---

## 2026-10-01: The ring's drain stays on and needs balancing; carrying tools out after Act I (plan 040b batch 4)

Settled with the user while slimming GDD §11 and §12.
- **The ring's drain stays on, and is likely too low.** It should make carrying, dropping and picking up the ring a real decision. The GDD no longer states a rate; the build's number is in `docs/BUILD-STATE.md`, and the rate is balanced from playtest feedback (§11 `[OPEN]`).
- **Tools may be carried out once Act I is complete.** Tools reset every run until then; after it, walking out lets the player take them out of the Hall to keep for the next run (the §11 `[DIRECTION]` carry-out rule).
- **Switch count dropped from the GDD** ("eleven"): it goes stale; the count lives in BUILD-STATE.
- **GDD § changed:** §11 (tools, ring, lost items, checkpoints), §12 *The memory lab: Act I's finish* (10 bullets to 2, pacing and verb list left to this log, plans 027a–d and BUILD-STATE).

---

## 2026-10-01: Endurance banks, seven skills, and searching is open again (plan 040b batch 3)

Settled with the user while slimming GDD §6 and §9.
- **Endurance banks.** Its bonus banks from each run's vitality lost and is paid as extra max vitality next run (as built, plan 031); the GDD's "+max vitality per level" job is gone.
- **Seven skills, more to come.** The GDD named five; Crafting and Convincing (placeholder name) were added by the lab. More will be added as new verbs reveal the need for them.
- **Searching: `[OPEN]`.** Once-ever (2026-09-29, built) versus search afresh each run until the room is known by heart is still being playtested. **Supersedes** the 2026-09-29 note that GDD §9's "searched afresh" was replaced: the GDD no longer claims either way.
- **Idea, `[PROPOSED]`:** exploring further costs steeply more, so Act I affords one exploring but hints that more will be needed later; or it is locked until the Amber pool is unlocked.
- **GDD § changed:** §6 (skills count, Skills table, Endurance row and paragraph), §9 (Explore/Search, searching, Rest or Wait).

---

## 2026-10-01: Crafting a container may become two steps (open question, nothing built)

Raised by the user during plan 040b batch 2 (GDD slim of §5), from playtesting.
- **The problem:** making a storage item (the satchel, the pouch) is one action today, so it isn't an interesting puzzle.
- **The idea, not decided:** gather a resource first, then combine it into the finished container, so the player plans two actions and where each happens. It would also bear on §5's open question of whether the meta layer buys capacity.
- **Status:** `[OPEN]` in GDD §5 *Pockets and carry capacity*; backlog Next. Supersedes nothing: the one-step craft stays as built until it is designed.

---

## 2026-10-01: Design docs: one question, one place (plan 040)

Settled with the user (workflow question, then plan 040). Design now happens only in Claude Code, so the docs no longer need to serve a second chat.
- **Three documents, one job each.** `DesignNotes/VISION.md` says what the game is for (pillars; only the user changes it). `GDD.md` is the living design. `docs/CHANGELOG.md` lists finished features in player language, so its lines can become Steam patch notes.
- **The GDD is kept current.** When the user settles a point, Claude shows the § edit and applies it on the user's OK; this log then records *why*. The log stops being a layer of corrections on top of the GDD: if the two disagree, a GDD edit was missed. Supersedes the rule that the GDD is edited only when asked (2026-09-29 *No design chat*).
- **`docs/BUILD-STATE.md` comes back** as the plain-English summary of what's built, kept by a project `/sync-state` skill that `/wrap-up` runs. Supersedes the part of *No design chat* that retired it. Its *Recent changes* now points at the changelog.
- **The opening brief is merged into the GDD** as §12a; the original is in `Archive/`. Its conflicts with later decisions are marked [OPEN] there (drain in the prologue; Act I's length), not resolved.
- **Log upkeep:** `/wrap-up` moves entries over two weeks old to `decisions-log-older.md`.
- Supersedes nothing in the GDD's design; its *Order of precedence*, §1 and the v0.4 change list were rewritten or removed.

---

## 2026-10-01: The Story box stays one panel; a refused click doesn't hold room speed

Settled with the user, from the UI backlog roadmap (plan-feature session).
- **Story and ambient text stay in one panel** (the verdict asked for in the 2026-09-27 *Main screen UI rework*). The Story box reads like a log: it stays scrolled to the bottom, new entries push older ones up and off the top, ambient lines fade out, and the player can scroll up to reread the story beats. This is how `StoryFeed`/`FeedTrim` already behave (ambient lines fade and are dropped; milestone cards never fade and stay for the run), so nothing is to build. Not decided: whether earlier runs' beats can be scrolled back to (the box is cleared each run).
- **A refused click does not hold room speed.** Only the queue's own refusals (an entry refused or skipped when its turn comes) and milestones hold it; a click the game refuses as it's asked for doesn't, so a mis-click mid-blur doesn't slow the run. Answers the question raised in *Room speed made visible* (below). Built with plan ui-038, which already adds the hold's reason to Core.
- Supersedes nothing in the GDD (§13a trap 3 says speed holds when attention is needed; a refused click needs none beyond its toast).

---

## 2026-10-01: Colour as the payoff: hue colour returns on the map, not the chrome; parked to stage 2 (design session, nothing built)

Settled with the user, for UI-BACKLOG Next 6. Checked against the code first: Act I already looks like the "monochrome" mock-up (warm near-black, cream text, one gold accent), apart from the map's cold-grey rooms; the only hue colour on screen is the rainbow pathos bar. All seven pools are open and full from run 1, no switch unlocks a hue, and the crafted gem doesn't give the empty Amber pool yet (that rule is design only). Most colours are set at edit time (`UiColours`, *UI → Apply UI Colours*); only `MapBackdrop`'s tint, `PoolBars` and `Glow` change at runtime.
- **"Monochrome" means no hue colour, not grey.** Act I keeps the warm gilt chrome as the house style. In Act I nothing on screen carries a hue: the map's rooms move from cold grey to the gilt-and-dark palette, and the seven pool slots show dark.
- **Only hue-owned things recolour:** a hue's own rooms and the lines between them, that hue's slot in the pathos display, and a faint tint on the map backdrop. **The chrome never recolours** (frame, panels, text, buttons). Why: the mock-ups already do this, text stays legible, and a recolouring chrome would need a new runtime colour system.
- **Trigger: a pool won, per hue, plus a little warmth.** Winning a pool (in code, the same moment as learning its hue: `PersistentState.UnlockedHues`) colours that hue's own places in its colour (Amber tints amber). The backdrop also warms one step per pool won, so the whole hall brightens across the game.
- **The first before/after moment is the gem at Act I's end:** the Amber slot lights (empty, but in colour). Act II's colour is Amber only; the mock-up *Act II — three hues won back* shows what the screen looks like at the start of Act IV (one act per pool, 2026-09-30).
- **The moment:** the first time a pool is won, its colour floods in over a few seconds (visual only, never `Time.deltaTime` for gameplay). Every later run starts already in that state.
- **Meaning colours and hue colours never share a surface.** Hue colour lives only on the map and the pathos display; the tooltip's meaning colours (times amber, vitality rose, met green, unmet orange; 2026-10-01 *Tooltips*) live only in the chrome. The hue Amber gets a deeper honey-orange, distinct from the gilt accent and the "time" amber (shade is Claude's call in the plan). Every meaning colour carries a second cue (a tick or cross, an icon, a word), so colour-blind players never rely on hue alone.
- **When: parked.** The recolour system is built in stage 2 (vertical slice), with the Amber pool, once pools are really won. Real art (room pictures per hue, backdrop) and the Steam before/after shots wait for stage 3 (the demo, when the Steam page goes up); art must be commercially licensed, and generated art needs Steam's AI disclosure. The only stage-1 piece is the map in the warm palette, folded into plan `ui-033` (step 2). Why: in stage 1 no pool is ever won, so anything built now would be faked over seven always-full pools.
- **Art direction, not design (parked, `UI-BACKLOG-later.md`):** the exact hue shades, what each hue's room pictures look like, and how the flood looks.
- **Depends on rule work already in `docs/BACKLOG-later.md`:** *Pools taken in the prologue* and the gem giving the empty Amber pool.
- Supersedes nothing in the GDD (§5 and §13a don't specify a colour progression; this builds on §5 *The seven pools are taken from her in the prologue*).

---

## 2026-10-01: Five small rule questions settled (backlog clean-up, batch 3)

Settled with the user, one at a time, each checked against the code first.
- **The Summary counts every escalating charge, per task.** The "Moves" line stays for trips; each other task with a charge gets its own count and vitality total (today only Practise the cut: "· Practice: N, costing X vitality"). Why: the run report exists so the player can tell why a run ended, and a rising cost they can't see reads as the game punishing them; one entry per charged task also covers future charges with no special case. Rejected: labelling the line "moves only" (leaves the practice drain invisible) and one combined total. Exact wording and placement are UI work. Serves GDD §13a *Legibility: the run report* and §4 *Show the next cost* (hidden escalation); supersedes nothing there.
- **A refusal names what she can't fix this run first.** When an action is missing both an item and a skill or attribute level, the short reason (toast, offer row) gives the skill or attribute first, then the item. Why: a skill takes runs, an item takes a trip; naming the item first sends her on a wasted trip. No shipped task hits this today (only Craft the gem has a skill gate, and it needs no items). The Locked tooltip (2026-10-01) still lists every condition. Supersedes nothing in the GDD (no § sets a refusal order).
- **Containers are never carried.** Carry is not offered for an action that makes a container, even one without a maximum of 1. Why: Carry means "fill up on what I'm making"; on a container each one adds room to fill. Nothing the player sees changes today: every shipped container (pouch of phials, the earrings) already has a maximum of 1. Narrows the 2026-09-30 *Carry works in any room and on any repeatable making action* ("objects, pouches and satchels"). Supersedes nothing in the GDD.
- **A way counts only the floor where she is: intended.** A key left on one side of a two-way way opens it from that side only; from the far side the way says what it needs. Why: it is the same rule as an action's needs (items count where she is), and being stranded lasts only until the run ends. Rejected: counting the floor at both ends (a key working from another room). Confirms the 2026-09-30 *A way's needs count the floor where she is*; no code change. In play it can only happen if she puts Roland's ring down before talking to him, since the switch waives the way's needs after. Supersedes nothing in the GDD.
- **Hidden finds: deferred to Act II.** Act I has no stats (2026-09-30), so nothing in it can be hidden by Perception, and the earrings were built ungated. Whether a search shows "something you can't make out: Perception N" is decided when stats arrive (backlog Later). Lean: show it, as the GDD's gates are never secret and it gives Perception a visible reason to grow. Leaves open GDD §6 *Each stat's primary job* (Perception "reveals hidden things") and the 2026-09-30 stats-pass **Open** on the same point.

---

## 2026-10-01: Room speed made visible: the open points settled (before the room-speed UI plan, nothing built)

Settled with the user, for UI-BACKLOG Next 8. Checked against the code first: the gate is live (Feed your hours gives Quickened Hours, which unlocks room speed, plan 029), the header's ×1/×2 buttons are hidden because no item grants a tier, the clock and every time-left figure are game seconds, and a room becomes known by heart only when a run ends.
- **The header gets a speed readout beside the run clock:** "×3.4" while room speed is speeding things up, "held" while it is held, nothing at ×1. It shows the player's tier × the room's speed now (`RoomSpeedNow`), never the dev panel's speed. The tier buttons stay hidden; if a tier ever returns, the readout already shows the product. The map badge keeps showing each room's own speed (a fact about the room).
- **With the Queue column open, the header readout covers it;** no speed note on the column's rows. **Time-left figures stay in game seconds** to match the clock (one time scale everywhere, 2026-09-30 *Faster play by halving game time*), and their tooltip says so.
- **Known by heart:** the run-end feed line stays; at the start of the next run, rooms that just became known by heart glow briefly on the map. **The first time the clock ever speeds up,** one feed note says so, framed as her noticing she crosses the room without looking (GDD §13a's framing); a `[PLACEHOLDER]` key for `/writing`.
- **Held:** the readout flips to "held" with a brief flash (the juice plan's glow), and its tooltip names the cause (a skipped action, a refused one, a milestone) and that speed returns once the next action is done. No new feed lines. Core keeps the hold's reason as read-only state; no rule changes.
- **The Summary gets one line,** "Faster next run: {rooms with their speed}", only once room speed is unlocked and only when a room's speed rises. The Summary does not explain *Feed your hours* or Draw on the mana stone: Feed's own feed note (`feed.planning_unlocked`) also says rooms known by heart will now run faster; Draw stays with `docs/BACKLOG.md` Next 9.
- **No progress bar for the ramp:** the map badge's tooltip adds the planning screen's line ("×1.8 here · ×2.6 after 1 more run"). Room speed never shows in the stats panel (GDD §13a trap 2).
- **Dropped:** a combined readout including the dev panel; speed on the queue column's rows; real-second time-left; a per-room ramp bar; a Summary explanation of Feed or Draw.
- **Left open (a rule question, `docs/BACKLOG.md`):** a refused *click* also holds the speed, so a mis-click mid-blur slows the run; whether only the queue's own refusals should hold.
- Supersedes nothing in the GDD. Plan 025's "room speed shows on the map and the folded ribbon only" is extended by this entry.

---

## 2026-10-01: Tooltips: the open points settled (before plan ui-036, nothing built)

Settled with the user, from `docs/mockups/tooltips/design-notes.md` *Open*. The six-state shape is adopted as the direction for UI-BACKLOG Next 3.
- **Locked (state 6) is for actions already listed but missing something** (an item, a skill level, a hue): a lock, every condition ticked green when met, and the unmet one saying what to do. Nothing hidden today becomes visible: not-yet-unlocked, not-yet-found and done-for-this-run actions stay hidden, so the locked form can never reveal a secret.
- **"This will end the run" (state 3) is an estimate, worded "likely".** Red border, header and cost when her vitality now is less than the action's estimated cost (drain over its time plus its charges), with "you have N". It's a warning only: queuing is never refused for vitality (vitality is never checked up front; running out mid-task ends the run).
- **"Since last run" per action waits for its own plan after ui-036**, together with best-ever milestone times (`docs/BACKLOG.md` Next 5). ui-036 leaves a place for it in the full form.
- **Compact form: the common verbs** (Pick up, Put down and the other one-per-item verbs). Every room action, search and trip gets the full form.
- Already settled elsewhere: the serif is EB Garamond (plan 011); in Act I the Trains and Faster-with rows list skills only (*Features come in layers*, 2026-09-30); item lines in Clara's voice are the user's prose, built as `[PLACEHOLDER]` keys.
- Supersedes nothing in the GDD (§13a doesn't specify tooltips; §14.12 and §14.19 are what this serves).

---

## 2026-09-30: A reopened search pays its XP and gives again

Settled with the user (a code-health question): XP and a room's explore gives are paid on every completed search action, including a reopened search's second round ("exp is awarded continuously as an action is completed"). So the lab's second round pays Perception XP and its eachExploreGives again, as the first did. Pinned by a test (`SearchRoundTests`).
- No GDD § changed.

---

## 2026-09-30: The lab's end is built: earrings first, the gem behind Crafting 8, the exit at the Smoky Mirror (plans 027b–d)

Settled with the user before building; all numbers placeholders.
- **The reopened search finds the earrings first:** Take the earrings and Draw a dense wisp at **15%**, Practise the cut and Craft the gem at **25%** (the spec had the gem at 25% and the earrings at 50%; 027b's later note had the earrings first). Why: the stronger restorative arrives before she starts practising for the gem.
- **The gem's gate is Crafting 8** (this run's level plus mastery; the user chose it over 5). About five practices on the first try, so the first attempt barely starts; a failed attempt's XP is kept, so later runs need fewer.
- **027d (the dev panel's Jump to) is built** with 027b and 027c.
- **How it's built (what differs from the spec):** the phase 2 tasks start unlocked and the reopened search alone shows them, rather than *Roland Takes the Ring* unlocking them (the same reason as phase 1: a switch writes its unlocks only as it flips). Earlier finds count as found once a room is reopened (placeholder rule). Only one switch may reopen a room. Dense wisps go **only** in the earrings (a new item flag, *Only In Containers*), so Draw a dense wisp stops at two. Crafting learns faster with no stat (Act I has none); the gem and practice keep kind Instantiate. A save that flipped *Roland Takes the Ring* before this change gets the lab's search reset when loaded (save version 21).
- Supersedes nothing in the GDD.

---

## 2026-09-30: The classmate: Listen for a local trickle, or Take for a run-only overcharge that pays for the first slot (design session, nothing built)

Settled with the user (their design), continuing the Ratchet entry below. Fills in its open "what the classmate would give".
- **Listen (keep him):** while Clara is in the Lecture Room, Amber trickles in from him **while she does other actions there**; no queue slot of its own. It works only in that room and can't be carried out. Its use is getting things done there: the Lecture Room's magnitude wall (Amber ≥ X) and fuel for binds. Note: Amber shields only its seventh of the drain, so the trickle is a small help to survival, not a full restorative.
- **Take (burst him):** a large surge of Amber as a **run-only overcharge**, above the pool's maximum for this run only; it is enough to pay for the first harvest that unlocks the bind slot. He is gone for the rest of the run, and with him the trickle. The real maximum still rises only with meta currency (unchanged).
- **The ordinary path:** Take him once to afford the first harvest and the slot; on later runs choose between the steady local trickle and an overcharge to carry elsewhere.
- **Watch in playtest:** whether Take dominates once the slot is earned. Expected to limit itself, because each later harvest costs far more than one burst gives.
- **Open:** the overcharge's size, and whether the extra above maximum drains away over the run or lasts until spent; the trickle's rate; whom the harvest itself takes from.

---

## 2026-09-30: The Ratchet is retired; run-long choices and an escalating harvest replace it; Act II starts with no bind slots (design session, nothing built)

Settled with the user, continuing the Amber realm session below.
- **The permanent Ratchet is retired** from the challenge vocabulary (future-sketches §10). Why: a permanent loss works against the loop's "experiment freely"; players hoard it and never use it; it fails §10's own tests (not informed the first time, and "spend it on the final push" dominates); the fixed Amber maximum caps its payoff; and it breaks the realm's pull-back rule. **Settles** the open Ratchet question in the entry below.
- **Run-long choices replace it in Act II:** a one-way choice that lasts the rest of the run and resets next run. The first is the classmate: *Take* gives a large surge of Amber, but he turns away for the run and what he would have given is closed off; *Ask* or *Listen* gives less and keeps him (GDD §12 *gather honestly or hold back*). What he would give is open.
- **An escalating harvest is the soft cap**, built on GDD §4 *Harvesting is an action with an escalating cost* (stat check, cost compounds per repeat, eases back over runs, next cost shown). The user wants a **large** increase per repeat; numbers open (`~`). A harvest takes a lot of feeling from someone in the memory and should feel heavier than a gather (Roland's guilt, §2); whom she harvests from is open with the plot.
- **Clara starts Act II with no bind slots.** Her **first harvest gives the first slot**, which the realm's puzzle (A → B, C → D, B + D + E) needs. Why: the bind verb arrives as something earned, one layer at a time. **Supersedes** "1 slot in Act II" in the entry *Act II's runs start at the Smoky Mirror* only in that the slot is earned, not given.
- **In Act II, harvests pay only that first slot.** Harvests start paying meta currency from the end of Act II, so "currency arrives at the end of Act II" still holds. Partly answers the open "how currency is earned" (repeatable harvests).
- **A second slot can't be harvested until Act II is finished**, so grinding can't break the one-bind puzzle. After Act II, harvests can buy more slots and currency at the large escalating cost.

---

## 2026-09-30: The Amber realm pulls back; pathos is only ever gathered from others; a first room sketch (design session, nothing built)

Settled with the user in the `/design` session on the Amber realm, continuing the Act II entries below.
- **The realm's rule: the memory pulls back.** Anything she changes in a realm room (a moved ladder, a propped door) goes back to how the memory had it once she leaves that room. The realm has **no world switches**: it is whole at the start of every run. **Amber binding is the only way to make a change stay put**, for the rest of a run or across runs. Why: Amber is adhesion, and here the memory clings to how it was; the rule is felt every run, not only between runs, and the one bind slot is contested within a run as well as across runs. Tooltips must say plainly what will slip back. **Settles** the open question raised by the user in the entry below (nothing in the realm stays put unless bound). GDD §11 *Items persist by being carried out* is unchanged: carried-out items are on her, not in the realm.
- **Pathos is only ever gathered from others** (the novel's eromancy, kept). Amber comes from people who are, or were, attracted to Clara: people in the realm's memory, and in Act I's rooms, traces where Roland passed carrying his feeling for her. Act I's wisps read as others' feeling the Hall has soaked up, so nothing built changes. Why: it is already implied by GDD §12 *Memory nodes* (Listen, Console, Ask, Refuse, Take as transfer modes) and gives weight to Roland's guilt over teaching harvesting (§2) and to *gather honestly or hold back*.
- **Plot frame, placeholder:** the realm is Clara's memory of meeting Roland, as his student. Placeholder until the plot is written.
- **A classmate with a crush she never noticed**, placeholder: a strong Amber source in the lecture room. The Hall showing her something she never knew; taking from it is a real *honest or hold back* choice.
- **Working room list, all placeholder** (names, jobs and count open): Amber Threshold (Hall node, entry junction); The Lecture Room (memory; first Amber source, the classmate; magnitude wall); The Sliding Stair (constructed; bind A); Roland's Workshop (memory; item B to walk out with); The Stopped Garden (memory; a Fork); The Balance Room (constructed; bind C, precision wall, D); The First Meeting (memory, heart; Roland as the deepest source; B + D + bind E finish Act II). Gamble and the Trade-off pair go in rooms 4–6.
- **Open:** the **Ratchet** (one source spent forever). It breaks "no world switches in the realm", and the user isn't sure about a once-in-the-whole-game ratchet at all; look at the concept more closely before placing it. Also still open: which Act I room the realm opens from (beside GDD §12's one-way-valve conflict), the comfort-bind list, the plot, and all numbers.

---

## 2026-09-30: Act I's exit stays the Smoky Mirror; pools have three jobs and each shields a seventh of the drain; Amber binds comfort as well as puzzles (design session, nothing built)

Settled with the user, continuing the Act II session below.
- **Act I's exit stays the Smoky Mirror.** The user considered a second exit mirror in the Mirror's Laboratory and kept the 2026-09-29 design: she carries the cut gem back to the mirror she came in through. Note added to `docs/plans/finished/027-lab-redesign.md`. The GDD §12 one-way-valves conflict stays **[OPEN]**.
- **A pool has three jobs:** a **shield** (absorbs drain), **fuel** (workings and binds spend it) and a **threshold** (walls read the amount she holds right now: magnitude needs at least X, precision a band). Why: every point of pathos is contested between staying alive, getting something done and opening a wall.
- **Each pool shields only its own seventh of the drain**; an empty or missing pool's share falls on vitality. **Placeholder rule:** one-seventh each. Why: with only Amber in Act II, six-sevenths of the drain still hits vitality, so restoratives stay her main way to survive; she grows visibly sturdier as each pool returns. Travel charges always hit vitality directly. **Settles** GDD §5 *[OPEN] The empty-pool penalty* as the **partial** form.
- **Pools refill mid-run by gathering** at their hue's sources (Amber: memory nodes in the realm and a few new spots in Act I's rooms), not from wisps. Gathering takes time, so topping up always costs drain.
- **The Amber pool's maximum is raised only by meta currency**, so it is fixed through Act II (currency arrives at its end).
- **Amber binds comfort things as well as puzzle objects**, and with one bind in Act II, comfort has to be given up while the puzzle needs the bind. A **way held open** (a shortcut) is the first comfort bind; more are to be invented (open).
- **Light isn't Amber's to bind.** Which hue owns light against the darkness is **[OPEN]**: Citrine (joy; Act III), Amethyst (passion, fire), or Sapphire (concealment; GDD §5 *Holding off the darkness*).
- **Open, raised by the user:** whether nothing in the realm stays put across runs, even after a walk out, unless Amber binds it. It would change §11's "items persist by being carried out".

---

## 2026-09-30: Act II's runs start at the Smoky Mirror; Amber binding is slots plus reserved capacity; the reflection is a voice (design session, nothing built)

Settled with the user, continuing the Act II session below.
- **Act II's runs start at the Smoky Mirror** and walk back through Act I's rooms, chasing Roland again: each act she gets closer, and Act I's rooms gain places where Amber is useful. Why: mastery pays off, old rooms keep a job, and the chase gives every act a story beat.
- **Amber binding has two separate dials.** **Slots** (how many binds at once) are the puzzle rule: **1 in Act II**, a second as a later reward. **Reservation** is the price: a bind spends some Amber once when made and holds back part of the Amber pool's maximum for as long as it stands, run after run. Unbinding is done at the bound thing, quick and free. Why: grinding Amber can't break the puzzle, and currency can later raise either dial without a rule change. All numbers open (`~`). Builds on the 2026-09 direction *Amber binds things to a place*.
- **In Act II, Clara's reflection is a voice only:** collapse text and run-end lines, making its case. No mechanics. A body (e.g. a ghost runner, the Pursuit challenge) waits for Act III, still tabled.
- **Open, raised by the user:** whether Act I's exit is the Smoky Mirror (decisions 2026-09-29, plan `027c`) or a mirror in the Mirror's Laboratory; what pools do besides shielding vitality; how pools refill mid-run; what vitality restoratives are for once pools shield her.

---

## 2026-09-30: One act per pool; Act II is the Amber realm; Act I runs 10–15 runs (design session, nothing built)

Settled with the user in the `/design` session on Act II.
- **Each pool gets its own act.** Act II is the Amber realm only: a cluster of nodes, like Act I's rooms on the map. Why: one new layer per act, and currency (end of Act II) can't wait behind six realms. **Supersedes** Narrative Document §5's act table (Act II = "loops 5 onward", carrying six systems) and §7's "six realms between the mirror lab and the heart" as Act II.
- **Act I ends by walking out through the mirror back to the real lab** (the gem opens it; decisions 2026-09-29 and above). **Act I takes 10–15 runs at minimum**, depending on how efficiently the player plays, and **30 minutes to 2 hours** (still being tuned). **Supersedes** Narrative Document §5 "Act I, loops 2–4, ~5 minutes played optimally" and "Four loops, and out".
- **What pulls against the rescue in Act II is the family in the real lab, as story only for now** (the Hub is the real lab, so the children are really there; no mechanic yet).
- **Clara's reflection and Shadow stay tabled**, possibly to Act III; the user has no implementation in mind yet and is open to ideas. Narrative Document §7's "her reflection must appear" in Act II and the Bargain and Pursuit challenges wait with them.
- **Clara's age in the Narrative Document** brought in line with the 2026-09-29 decision (41, twenty-one years after the novel): every "old", "decades", "forty years" and "seventy" reference reworded.
- **Still open from this session:** where Act II's runs start (the starting mirror and the walk through Act I's rooms, or nearer the realm) and whether she chases Roland again through them; Amber binding's rules (the user's idea: each bound thing lowers the Amber pool's maximum, so she can bind one or two at first); the Amber realm's rule.

---

## 2026-09-30: The last run's report is saved, so the Summary survives a load (plan 032b)

No design change. `Simulation.LastRun` is now in the save (save version 20; older saves load with no report, as before). The report stores content by `Id`, and the two run-history records it compares with by loop number. Load still follows plan 032a's rule: it doesn't open the Summary. Also a dev-only control: the dev panel's − / + / "by heart" buttons per room set the runs worked (`Simulation.DevSetRoomRuns`); the start room has none, as its count comes from runs finished.

---

## 2026-09-30: Planning is earned by Feed your hours, and plans only what she knows by heart (plan 032a)

From the user's playtest answers. **Reverses** the same day's "Planning in a room not known by heart is allowed, but it must say it won't carry over": between runs, an action or trip is now refused unless every room on the walk (where she'd start, each room she passes, the one she ends in) is known by heart, so every plan carries over. During a run the queue stays free. Why: the Plan button now arrives after *Feed your hours to the flames*, by which time rooms are known, and a plan that carries in full is easier to understand than one with won't-carry marks.
- **Planning unlock:** `unlocksPlanning` on Quickened Hours (beside `unlocksRoomSpeed`, separate flag). Before it, the Summary has no Plan button and Continue/Load starts the next run straight away. Refusals between runs apply only once planning is unlocked. Feed line on unlocking (placeholder wording).
- **The start room counts every run** toward known-by-heart (the user chose "counts every run"): she begins each run there, whatever she does. Worked out when read.
- **A map click that can't add a trip says why** (toast), in a run or between.
- **The Dark Corridor shows on the map once the Left Corridor is 100% searched**, its line dim, and a trip there is refused ("too dark to go in…") until the candles are lit (`Way.showWhileShut`/`shutMessage`).
- **Look:** rooms known by heart glow a soft gold (was a violet dotted ring). Placeholder look, checked by eye against the route line and badges.
- **Supersedes:** GDD §12a *Mid-run editing* on planning before any run, and the 2026-09-30 *Planning in a room not known by heart* entry.

## 2026-09-30: Features come in layers: Act I has no stats; the first exit wakes them; meta currency waits for the end of Act II (design session, nothing built)

Settled with the user, in the `/design` session on meta currency and the start of Act II. Why, overall: the user doesn't want to swamp the player with features early, so they can learn the mechanics and the complexity a layer at a time (GDD §14: no choices → one choice → a choice that persists).
- **The Hub is the real lab.** *Exit through the glowing mirror* takes her truly home to the real 1907 lab. She chooses to step back into the Hall each run for Roland. Why: it matches the premise (she keeps going back for him), and it puts the family and the safe place on the same side of the glass.
- **Act I has no stats at all.** No stat XP, no stat effects, no stats row. Act I is skills, skill mastery, switches and items. (The user chose this over "stats asleep and hidden, but earning XP".)
- **The first exit gives her stats.** Walking home for the first time is the reward: the five stats start existing, from zero.
- **In Act II, stat XP settles at home.** A walk out keeps all of the run's stat XP; a collapse keeps only part of it. **Placeholder rule:** a collapse keeps ~50%. This is walking out's reason to exist until currency arrives. Skill XP and mastery are unchanged (always kept).
- **Meta currency arrives at the end of Act II**, as its reward. **Supersedes** "walking out and meta currency unlock together" (GDD §12 *The three run endings*, decisions log 2026-09-27): walking out now arrives at the end of Act I, currency at the end of Act II. The crafted gem is unchanged: it still opens the exit mirror and gives the empty Amber pool at the end of Act I.
- **Also supersedes:** GDD §12 *The three run endings* ("Reach the mirror lab", the Hub on the mirror side) and *Verbs in the mirror lab* (the mirror lab as the Hub, with Cross, Store and Look through); §4's timing of currency; §6's stat jobs in Act I; the 2026-09-29 lab design's stat pairings (Talk to Roland and the gem craft with Attunement, Watch him with Perception, Attend with Composure, and so on) and "the gem wakes Attunement"; plans `027b`/`027c`'s stat gates (earrings at Perception 15, the gem at Attunement 26), which become skill gates.
- **[PROPOSED] Stats don't arrive at zero.** Starting at zero makes the reward a row of empty bars and does little on the first Act II run. The idea: the first exit gives a one-off starting amount in each stat, told as what the walk home did to her, so she feels changed at once and builds on it from there. The amount, and whether it's flat or scales with how Act I went, are open. Not settled; revisit when the first exit is built (backlog Next 8).
- **The 027 lab plans are patched, not replanned:** each carries a *Design change 2026-09-30* note. They add no new stat dependencies (the gem's gate becomes a Crafting skill gate, and the earrings stop needing Perception); the stat pairings already built are removed later by backlog *Act I without stats*.
- **Earlier today, now open again:** "Act I's milestones pay out in one lump on the first exit" was chosen while currency still arrived there. With currency moved to the end of Act II, whether Act I and Act II milestones pay out in one lump when it arrives is open.
- **Still open, from this:** what the mirror-side lab is now; whether stepping back in costs anything; whether Store and Look through move to the real lab; whether "no recovery in the Hub" still holds; the Hub's stakes (§12 [OPEN]); the currency's name (§17 q23); how currency is earned (milestone gems or repeatable harvests; the GDD's physical gems still stand as written); how Act I stays balanced without Endurance's growth of max vitality or Composure's slower drain; what Act II gives the Hub to do before currency (at least: see her stats and what the walk out settled).

---

## 2026-09-30: The lab's numbers, refreshed as placeholders; phase 1 built (plan 027a)

The user chose labelled placeholders over a playtest reading. The only reading is the user's save after 16 runs (dev-speed play): Attunement mastery ~21, Perception ~11. Full table in `docs/plans/finished/027-lab-redesign.md` *Where the numbers come from*.
- **Placeholder rules:** about 40 s of work in the lab on the visit the talk first fits; Talk to Roland 24 s base (17.5 s at Insight 3); Craft the gem 60 s; **the gem's gate Attunement 26** (was 3, which Instantiate training would already meet); the earrings a hidden find at Perception 15; Practise the cut 4 s, XP × 3, charge 2 × 1.15. Existing lab verbs keep their cost-curve times (Watch him 7.5 s, its Insight discount stays ×0.8 as tuned, not the spec's 0.9).
- **The lab's phase 1 tasks start unlocked**, and the lab's staged search (20/34/67%) alone decides when they show. Why: a switch writes its unlocks into the save once, as it flips, so tasks added to an already-flipped switch would never unlock in an existing save. *The Memory Is Wrong* now unlocks only Draw on the mana stone.
- **A switch can waive a way's needs** (`waivesWayNeeds`): the way stays as it is, but its Needs stop counting for travel, the plan's warnings and whether an item is still used. *Roland Takes the Ring* waives the ring on the Hanging Mirrors → lab way, locks phase 1 and Instantiate Roland's ring, and wakes Attunement.
- **A skill's chip shows once it's found or trained** (the user, 2026-09-30): she has any XP in it, or an unlocked task that uses it is one she can see (a task a room's search hides counts once found). Was: any unlocked task. Why: the lab's tasks start unlocked, so Studying, Invoking and Convincing would have shown from run 1, and Studying and Convincing would have vanished once the talk locked their tasks.
- **The ring's blurbs end at the talk:** `ring_present`, `ring_refused` and `lab_sealed` end when *Roland Takes the Ring* flips (*Fix Known Blurb Rules*).
- Supersedes nothing in the GDD.

---

## 2026-09-30: The pathos display stays the rainbow bar; facets wait for the art pass

**Settles** the placeholder in *Look pass built as placeholders (plan ui-026)*.
- The user judged bar against facets (plan ui-026) and **kept the bar for now**. The seven facets (plain diamonds) weren't rejected as an idea: they need a full art pass to work, so they're parked in `UI-BACKLOG-later.md`.
- The facets code and the dev-panel toggle are deleted (last in commit a287e6d if wanted as a starting point).
- The look pass's backdrop, vignette, one top strip and tooltip-clear-of-toast rule stay as built. The map's share of the frame was not resized (the plan's assumption, not re-measured).
- Supersedes nothing in the GDD (§13a leaves the pathos display open).

---

## 2026-09-30: Warnings ahead built (plan ui-024d): what differs from the rule

- Built as decided in *Warnings ahead: the rule*. **Differences** (all towards silence, placeholder rule in `PROJECT_NOTES.md`): a trip through a hidden way that a search earlier in the plan (or rising Perception) may find is not warned, and nothing after it is; an action in a room's Found By Searching list is not warned `not here` after such a search; pick-ups and put-downs are never warned; after a trip warned for a missing item the walk goes silent (she stays put while the stops show her arriving); an entry sure to be refused gives nothing and uses up no once-a-run task.
- The ⚠ shows after the action's name and in its tooltip; the reason replaces the row's details line in the warning colour; the stop heading and a folded block carry it too; the foot reads "· N warnings" (won't-carry stops not counted).
- Supersedes nothing in the GDD.

---

## 2026-09-30: The laboratory search is deleted; the corridor searches stay unplaced

- *Search the laboratory* was in no room's list and had no purpose in the lab redesign: deleted. `SearchLeftCorridor` and `SearchRightCorridor` stay as assets, in no room's list, as logged earlier ("kept for later"; the right one unlocks again once A Glimmer's ring exists). Placing them is on `BACKLOG-later.md`.
- Supersedes nothing in the GDD.

---

## 2026-09-30: The queue's trip charge counts what she holds when the plan starts

**Refines** *Travel's cost rises with every move this run* (plan 030b).
- **Placeholder rule:** a queued trip's projected charge counts the trips planned before it and uses what she holds where the queue starts (during a run: now; between runs: what she keeps and has packed, as the plan-warnings walk does). Gives, pick-ups and put-downs by earlier entries aren't projected, so full pockets, failing entries and unfound ways need no guess. Shown on queue rows only; the trip's tooltip says it counts what she holds now.
- **Why:** the only item that changes Travel's cost is the mana stone's warmth, from a parked lab verb (backlog Next 9), so the error is rare, late in Act I, and errs dearer. A second holdings projection separate from the plan-warnings walk would drift from it.
- **Rejected for now:** projecting the items that change Travel's cost (option b) and projecting everything (option c).
- **Later:** project holdings inside the plan-warnings walk (assuming earlier gives succeed), so needs and trip charges agree, once a second Travel-cost item exists or the mana stone verb returns.
- Supersedes nothing in the GDD.

---

## 2026-09-30: Stats pass built (plan 031): what differs from the decision

- Built as decided. **Differences:** a run under way saves its vitality-lost count (save version 19); the bank is paid the moment a run ends, by any ending except a dev restart; a stat that is asleep still counts for switches, task gates and `needsPerception` (only its effects are off). No switch wakes Attunement yet: the lab's talk switch (plan 027) will list it under Wakes Attributes.
- Chips now read "+N vitality kept", "sees hidden things", "×N restoring" or "asleep". The Summary's "+N vitality kept" line and the asleep look are still UI-BACKLOG Next 10.
- No GDD § superseded beyond the ones listed in *Stats pass: each stat gets a job skills can't do*.

---

## 2026-09-30: Warnings ahead: the rule (plan ui-024d)

Settles the rule left open in *The planning screen (plan ui-024a)*; nothing built yet.
- **A dry run of the queue** from its start warns about what is sure: ways that are shut or not found, tasks locked by a switch or not offered in that room, hues not learned, a doubled once-a-run task. **Items too**, but only when nothing can give her the item in time (not held or packed, on no floor, no earlier entry gives it, no action in that room could supply it). **Silent:** stat gates, pools, vitality, pocket space, and everything after an entry that can flip a switch. **Placeholder rule.**
- **Shown during a run too** (same queue column), walking from where she is; the top entry is left to the real check.
- **Won't-carry stops get the ⚠ but aren't counted** in the foot's "· N warnings" (they still run this time).
- No GDD § superseded.

---

## 2026-09-30: Stats pass: each stat gets a job skills can't do (backlog Next 13)

Settled in `/design`; nothing built. Skills stay the speed layer; each stat's job is now one a skill can't do. All numbers `~`.
- **Endurance (quantity): a kept vitality bank.** At the end of every run (any ending), her kept max vitality rises by a share of the vitality she lost that run, and a higher Endurance raises the share. **Placeholder rule:** gain = vitality lost this run × ~3% × (1 + ~0.01 × Endurance strength), about +2 early and +75 mid game; the user's target is +2–3 early, +30–50 mid, +100–250 late. Max vitality = 50 + the bank; **the old +1 max vitality per strength is removed.** Overflow tolerance and the XP rate for Wayfinding and Gathering stay. Why: the drain grows 56% a minute, so vitality buys time only in multiples; a flat +1 per level can't be felt. Tying the gain to vitality lost means quick deaths can't farm it. Save format change (one kept number).
- **Composure (rate): adds one job.** It also softens a task's own extra drain per second (Feed's 2/s today; the lab's heavy verbs if they get some). **Placeholder rule:** ×0.97 per strength, like its growth effect. Its drain-growth and carry-bleed jobs stay. **Supersedes** plan 029's "none of them change it" for Feed's extra drain.
- **Perception (information): hidden finds only.** A find with `needsPerception` N stays unseen until Perception reaches N (the field is built; unused until now). **Explore no longer fills faster with Perception**; Wayfinding still speeds it. The XP rate for the Instantiate skill stays. **Supersedes** the 2026-09-29 placeholder rule "Perception makes the Explore bar fill faster". **Open:** whether the search shows that something is there that she can't yet see.
- **Scholarship (permission): unchanged.** Understanding per study, hard gates on tomes, XP rate for Studying and Invoking.
- **Attunement (efficiency): how well things answer her.** Restoratives she makes or draws (phials, wisps, dense wisps) restore more. **Placeholder rule:** ×(1 + ~0.02 × strength). Pathos costs join this job in Act II; the "instantiation stability" and "craft quality" jobs are dropped for now. Convincing and Crafting learn faster with Attunement.
- **Attunement is asleep until the talk.** From run 1 its chip shows as asleep. It still gains XP (from Instantiate tasks, as now), but its effects are off. **Talk to Roland wakes it** (the switch *Roland Takes the Ring*): a story beat, the chip lights, and every effect switches on at once at its full banked strength. **Supersedes** the 2026-09-29 line "the gem… wakes Attunement".
- **Stats' learning-rate job boosts this run's level only.** A stat's XP bonus for its skills applies to the run's skill XP; skill mastery gets the plain XP. Why: an XP multiplier adds about ln(multiplier) ÷ 0.082 levels on the 1.085 curve, and feeding it into mastery (whose own strength feeds it back) would push past the mastery soft cap (ruling 2026-09-27). **Rejected as a stat job:** a flat +n starting level (it does mastery's job and gives speed in disguise). **Idea:** meta currency might buy a flat +n starting level for a skill; see GDD §6 [OPEN] *How deep content stays startable*.
- **How a task trains a stat:** by its kind (`AttributeMath.StatForKind`) by default, as settled 2026-09-26; a task may name a different stat with `trainsAttribute`. Kinds that train nothing (Gather, Take, Other…) stay that way unless a task overrides. Partly reverses 2026-09-26 "each action naming a stat to train" (only as an override).
- **The lab's pairings:** confirmed: Watch him → Perception (override), Attend → Composure (override), Talk to Roland → Attunement (override), Practise the cut and Craft the gem → Attunement (Instantiate default). **Changed:** Take the earrings trains no stat, and **the earrings are the game's first hidden find** (the reopened search shows them only at Perception ~N, N from a playtest reading); Draw a dense wisp trains no stat (Endurance stays "what surviving teaches"). **Supersedes** those two pairings in 2026-09-29 *The Mirror's Laboratory*.
- **The gem's gate stays an Attunement gate (`requiresAttributes`: this run's level plus mastery), sized from a playtest reading:** her Attunement on first reaching the lab plus what the talk and a few practices add (`~`). The planned 3 would already be met by Instantiate training and stop nothing. **Relaxes** 2026-09-26 "Scholarship is the only stat that flatly blocks content" to: Scholarship blocks knowledge; another stat may gate one wall at a time, always shown in the tooltip.
- **Naming:** the build's stat is Endurance; "Stamina" in the plan 029 entry (Feed's drain) and in `ClaraAttribute.cs`'s comment on Endurance are stale wording.
- **GDD sections superseded:** §6 *Each stat's primary job* (the Endurance, Perception and Attunement rows; Composure gains a job), §6 [OPEN] *Attunement currently does nothing*, and §6 [BUILT] *Stats influence skills through XP rate* (now this run's level only). The GDD is not edited until the user asks.

---

## 2026-09-30: The planning screen is built (plan ui-024c): the choices made while building

Builds *The planning screen (plan ui-024a)*; nothing in the GDD changes.
- **Continue / Load of a game between runs opens the planning screen** (not the run at once); a new game still starts its first run. The Main tab is unusable between runs; the planning screen has no tab and no way back to the Summary.
- **The header's Begin button is gone**: Repeat (Summary) and Begin (planning screen) are the only ways to start a run from between runs.
- **By-heart tags and the violet ring show on both maps**, reusing the popover's wording; the room speed line ("x1.8 here · x2.6 after 1 more run", "x5, full speed") shows on the planning screen only, and only once *Feed your hours* has unlocked room speed. **Placeholder rule:** the wording of the speed line.
- **Popover Play is greyed between runs** ("Nothing is running yet: plan it with +"); Schedule and Carry work.
- **Candle light is a procedural UI shader** (two candles, placeholder art), tuned in the Inspector on each candle's `CandleFlicker`; a lit backdrop with real lights is parked for the art pass. **Placeholder rule:** the candle look and its default numbers.
- **"N actions carried" counts every carried entry, trips included**, a repeat once per repeat.

---

## 2026-09-30: Lab plan check: the insight's name, Practise's charge, build order kept

Settled while checking plans 027a–d against the build (they predate the cost curve):
- **The kept insight shows as "Insight"** (was "What he did that night"; the asset keeps its name and Id).
- **Practise the cut gets an escalating charge**, as the cost curve asks of training verbs. **Placeholder rule:** 2 vitality × 1.15 per repeat this run. Draw a dense wisp does not.
- **Build order kept:** the lab waits for the stats pass (backlog Next 13) and the rest of 1b (ui-024c/d); then a `/plan-feature` pass re-derives the lab's numbers from the cost curve (walls for the talk and the gem) before 027a is built.
- Supersedes nothing in the GDD.

---

## 2026-09-30: The cost curve is built: room depths, and XP follows the final time

**Refines** *A cost curve that new verbs default to* (plan 030a); its "each room's depth" was open.
- **Room depths (fixed on each room):** the Smoky Mirror 0, A Dark Hall 1, both corridors 2, the Dark Corridor with Hanging Mirrors 3, the Mirror's Laboratory 4. A task's depth is the room she does it in; a trip uses the room she leaves; no rooms means depth 0.
- **XP follows the final time:** `ceil(1.5 × base seconds × the task's XP multiplier)`, so a ×2 duration pays ×2 XP and the XP multiplier changes XP per second. `roomStep` 1.1 and the standard trip of 15 s stay placeholders.
- **The one change in play:** tasks shared between rooms run a little longer deeper in the hall (a wisp in the right corridor 1.21 s, was 1; Travel from the lab 22 s, was 15). Everything else keeps its old time in its shallowest room.
- **Still open:** XP rounding (`ceil` or `round`), whether hall edges cost ×2, the wall sizes.

## 2026-09-30: Travel's cost rises with every move this run (a vitality charge, not time)

**Refines** the entry *A cost curve that new verbs default to* (its traversal charge is now built as plan 030b).
- Playtesting 15–20 moves showed no meaningful rise, and nothing told the player it rises. Travel now charges a **flat vitality amount that grows with each move this run**, resets every run, is paid while action costs are off and is never softened by stats. **Placeholder rule:** 3 vitality on the first move, ×1.08 each move after (move 20 ≈ 13, about 137 over 20 moves; the old flat 5 was about 100). The log's earlier ~5 × 1.2 would cost about 160 by move 20, more than her whole vitality, so it was too steep.
- **Time does not grow per move** (the user's idea, considered): her Wayfinding skill would speed it back up, so only the vitality charge stays costly (invariant I7).
- Feedback: the Travel tooltip shows the current charge and "Each go this run costs 8% more than the last. Done N so far."
- **Open:** the start and growth are a balance-pass job (Balance Sheet, Tasks: Charge and Charge ×).

## 2026-09-30: Planning in a room not known by heart is allowed, but it must say it won't carry over

Found in playtest of ui-024b: the player queued actions in rooms not yet known by heart, pressed Repeat, and got no carried queue, with nothing on screen saying why. The user called it a design bug (poor feedback), not a code bug.
- **Decided: don't block planning there; say so clearly.** Between and during runs the player can still queue anything anywhere. Stops in a room not known by heart, and every stop after the first such room (the carry stops at the first room that isn't known, as built in plan 023), are marked *won't carry over*, with how many of the runs needed she has worked there ("by heart in 2 of 4 runs") and a tooltip explaining the rule. The carry rule itself is unchanged; unmastered rooms still never carry (2026-09-29).
- **Rejected:** locking unmastered rooms out of the planning screen (it would leave Plan empty and unusable in runs 1-3), and carrying the player's own queue anyway.
- **Built:** the Queue column's stop cards (`QueueStop.CarriesOver`, plan ui-024b). **Still to do in ui-024c/d:** the same mark on the planning screen's map tags and rows, and ui-024d's warnings should cover it.
- Supersedes nothing in the GDD; adds to §13a (*rooms known by heart*) and the 2026-09-30 entry *The planning screen (plan ui-024a)*.

---

## 2026-09-30: A cost curve that new verbs default to

Asked for before the lab's numbers: a rule new verbs start from, so the lab is costlier than the rooms before it by a reasoned amount, not a guess. Builds on the Balancing Formulas doc v0.1 (§2 anchor, §4 family table, invariants I3 and I7), whose `tierMult` only steps between realms, so all of Act I had no curve. That doc is older than the build (vitality 100, drain 0.5/s, 0.75 XP/s, Explore ×0.3; the build has 50, 1/s, 1.5 XP/s, Explore ×0.5 after the game-time rescale), so its numbers are a guide, not the source.
- **Three layers.**
  1. **Ordinary verbs derive from one curve:** `base duration = familyCoefficient × B × roomStep^depth`. `B` is the build's standard trip (15 s). Family coefficients start from the formulas doc §4 table. **Depth is a fixed number set on each room**, never worked out from the live map (GDD §12 *Tier is baked*), so a shortcut never makes a room's work cheaper.
  2. **Walls (heavy once-ever verbs such as Talk to Roland, Craft the gem) are sized in runs, not by the curve:** `base ≈ 0.6–0.9 × (vitality-seconds she has left on arriving, on the visit it should first fit) × (her skill speed on that visit)`. This is where the lab gets much bigger.
  3. **XP stays derived:** 1.5 XP per base second, as built. A longer verb pays more by itself; XP is not set separately.
- **`roomStep` tracks her speed** (I3: base ÷ speed stays ~5–20 real seconds). **Placeholder rule:** `roomStep` ~1.1 per room, from an estimated ×1.3–1.5 skill speed on first reaching the lab; replace it with a playtest reading (the skill chips and vitality left on first walking into the lab). The consequence, accepted: the lab's ordinary verbs are only modestly longer than the corridors' (~1.5×).
- **Keeping early rooms relevant late is the traversal charge's job, not `roomStep`'s.** Each move between rooms gets a flat vitality charge that **grows with every move this run** and resets each run (GDD §12 *Traversal economy*, `chargeBase` ~5 × `chargeGrowth` ~1.2^(n−1)). Skill can't reduce it. This makes that [DIRECTION] a decision; the build still charges a flat 5 with no growth.
- **Per-verb adjustments on top of the default:** a duration (cost) multiplier and an XP multiplier, set independently (a verb can cost more and pay less); the existing extra drain per second; and the escalating charge below.
- **Training verbs: high XP, and each repeat this run costs more.** The cost is a **flat vitality charge that grows per repeat this run** (duration and XP stay the same), the same escalating-charge rule as traversal, counted per verb. Once or twice is worth it; dozens of times costs more than the XP is worth, a soft cap that skill speed can't erase. Fits the lab's practise verb (decisions log 2026-09-29: farming possible, never better than advancing).
- **Why:** every new verb gets a defensible default; the frontier keeps pace with her speed; the only costs meant to stay forever (travel, training repeats) are the ones skill can't touch (I7).
- **Still open (placeholders, `~`):** each room's depth; the training charge's start and growth; whether hall edges cost ×2 (`hallMultiplier`); the wall fraction per wall; whether XP rounding moves from `ceil` to `round` (formulas doc bad number 3).
- **Flagged:** the lab's placeholder verbs (4–10 s) are shorter than the corridors' (15–30 s), the wrong way round; the lab plans (027a–c) should take their numbers from this curve.
- Backlog Next 15.

---

## 2026-09-30: Feed your hours unlocks room speed

**Supersedes** plan 025's placeholder "*Feed your hours* keeps its flat 2× tier; room speed needs no unlock and stacks on it", GDD §4 (Feed unlocks the ×2 speed control) and the 2026-09-27 line that Feed unlocks Speed 2.
- **Feed your hours to the flames (the Quickened Hours switch) now unlocks room speed.** Before it, no room speeds up however well known by heart. The flat ×2 tier is retired: room speed is the only speed-up besides the dev panel.
- **Why:** the player has to have felt the pace before being handed a lever (GDD §4), and a speed-up that switches on unannounced explains nothing. The verb is the announcement.
- **Open:** what the header's speed buttons do without the ×2 tier; whether Feed's cost (20 s, 50 vitality) and needs (the corridor's candles) stay. Feedback for the player (badge, ribbon, tooltip, a first-time explanation) is UI backlog item 8, built with the planning screen.
- Backlog Next 14.

**Open points settled later the same day (plan 029):**
- The header's speed buttons are hidden (code and assets kept, so they can come back).
- Feed takes 40 s and has no up-front vitality cost; instead it drains a flat extra 2~ a second while it runs, added after candles, growth and Stamina, and shown in the top bar's drain. Its needs (the corridor's candles) are unchanged.
- The map and the ribbon show no room speed (×N, "held") until it is unlocked.

---

## 2026-09-30: The planning screen (plan ui-024a)

**Supersedes** 2026-09-29 *After the Summary, two buttons: Repeat and Plan*: its line "*Plan* opens the main screen paused… A separate Planning screen can come later". *Plan* now opens a planning screen of its own. Mock-up and answers: `docs/mockups/planning/`.
- **Its own screen, the main screen's cousin:** no top bar, no Story box; the map and the queue column fill the screen. Same palette, fonts, popovers, cards and tooltips as the main screen. Two flickering candles on either side for atmosphere (placeholder art until the art pass).
- **Shows:** the carried plan as folded by-heart blocks; on every room its by-heart progress ("Known by heart" / "Worked here in N runs, by heart at 4" / "Not worked yet") and its room speed. A spare slot is kept for a run-ahead strip, not chosen.
- **The player can:** remove, reorder, fold and unfold blocks; add actions from the map popovers; use ways and scheduling ahead as in play; *Clear plan*.
- **Leaving:** one *Begin* button, no way back to the Summary. **The between-runs plan is saved**, so quitting keeps it (a save format change, in ui-024b).
- **Blocks that can't start are warned about ahead** on the block and entry, with the reason. The exact rule (how far ahead it can tell) is settled with the user in ui-024b's plan; where it can't tell, it says nothing. Closes the question open since 2026-09-29.
- **Doing an action N times** (a count stepper) is wanted for coming verbs, not Act I: BACKLOG-later Ideas; the mock-up only draws the place for it.
- No GDD § superseded beyond the 2026-09-29 line (GDD §13a's planning screen was a direction, not a design).

---

## 2026-09-30: Room speed built (plan 025)

Settled while building:
- **Room speed is a clock speed-up only:** it changes how many ticks arrive per real second, never what a tick does, so drain, XP and task durations per game second are untouched (GDD §13a trap 1).
- **The ramp counts the first by-heart run as a step:** ×1.8 at 4 runs to ×5 at 8 (`LoopSettings.RoomSpeedAfter`); placeholders `byHeartRuns` 4, `fullSpeedRuns` 8, `roomSpeedCap` 5, all in the Balance Sheet.
- **The hold** (skipped action, refused action, milestone) lasts until the first action that starts after it is done, and resets with each run, so the action that follows is watched at the player's own speed (the user, at wrap-up: the first version released it in the same tick for skips and arrivals, so it did nothing). Supersedes plan 025's "until the next action starts".
- **Room speed shows on the map and the folded ribbon only;** the rest is UI backlog item 8.
- Supersedes GDD §13a's visit power law (already noted 2026-09-29/30).

---

## 2026-09-30: Lab plan: search reopens, phase 1 locks

Settled while planning 027 (split into `027a-lab-phase-1.md`, `027b-lab-earrings.md`, `027c-lab-gem-and-exit.md`; content spec `027-lab-redesign.md`):
- **The earrings are found by the lab's search reopening.** Talking to Roland restarts the room's search bar at 0%; everything found before stays found, and a second round of finds (the earrings, the phase 2 tasks) appears at its own thresholds. Keeps the 2026-09-29 "Search finds the earrings" rather than replacing it with a plain task. Placeholder rule: earlier finds stay found whatever their threshold.
- **Watch him, Attend and Study the tome lock after the talk.** Phase 1 is over and the room reads as changed; the insight stays kept.
- **Build order:** planned now, built after backlog Next 1b, 6 and 13.
- No GDD § superseded.

---

## 2026-09-30: A way's needs count the floor where she is

**Extends** *Items on the floor of the room she's in count for what an action needs* (decisions-log-older). A way that needs something (a key) is satisfied by one lying on the floor of the room she's leaving, as an action's needs already are. The need is checked, not spent: the item stays on the floor. Until now only her pockets counted for ways. Built as plan 028 Part 3b (`MissingFrom`, shared by an action's and a way's needs).

**Also settled:** a pool's name has one source, the `hues.*` keys in `game_text.txt`, not the pool's name in LoopSettings (plan 028 Part 2). Nothing the player sees changes. Vitality's name follows the same rule (the user's OK, plan 028 Part 5b): the `names.vitality` key, read when a run begins; `LoopSettings.vitalityName` is no longer shown to the player.

---

## 2026-09-30: Rooms known by heart built (plan 023, Core half)

Built as settled in *Known-by-heart details*; the UI half is ui-024. Small choices made while building:
- **A room counts a run when she finishes any action there that isn't a trip or an auto-supply**; counted when the run ends, so the 4th run already carries into the 5th. Only a run that ends counts (a run restarted mid-way from the dev tools does not).
- **The carried plan stops at the first step in, or trip into, a room not known by heart**; supply steps and tasks a switch has locked are left out; repeats merge into one entry with a count.
- **The Summary's actions line names the rooms this run made known by heart** (" · Known by heart: The Junction"), once, in the run that crosses the threshold. A placeholder for ui-024's fuller display.
- **Balance Sheet gets no runs-worked column:** run counts live in the save, not in an asset; the dev panel shows them instead.
- Save version 16 keeps the room counts and the queue made between runs (which also fixes the old "between-runs queue isn't saved" problem).

---

## 2026-09-30: Look pass built as placeholders (plan ui-026)

Built, awaiting the user's visual check. Follows 2026-09-29 *UI colour roles*; nothing about play changes.
- **Backdrop and vignette are generated placeholder art** (a dark gradient and a black radial vignette, no licence or Steam AI disclosure issue), one image slot each on the map; strength and tint are `MapStyle` fields. Real art comes later.
- **Tooltips keep clear of the notice toast** (hop above or below it) instead of "one popover or tooltip at a time", which would have hidden the popover's own tooltips.
- **Top of the screen is one strip:** the speed tiers sit inside the header row.
- **Placeholder rule: the pathos facets** (seven small diamonds) can be switched on with the dev panel button and are remembered in PlayerPrefs. The user judges bar against facets; the loser gets deleted. Not settled. *(Settled 2026-09-30: the bar stays, see above.)*

---

## 2026-09-30: A room's first entry this run replaces the "arrived" line

Building plan 021. **Refines** the 2026-09-29 *Benchmarks move to entering rooms* entry.
- **On a room's first entry this run, its milestone card stands in for the "arrived" line** in the story feed; later entries in the same run still get the line.
- Milestones are keyed by `ContentAsset`, so a room and a switch share one list and one save format (save version 15). The start room never counts.

---

## 2026-09-30: Carry works in any room and on any repeatable making action

Playtest: Carry (gather only until pockets and containers are full) only worked in the room she is in, and only for restoration items. **Supersedes** the earlier decisions that Carry stays only where she is (see the older entries, grep *Carry*).
- **Carry in another room queues low down, like Schedule** (after the walk there, or at the end of that room's last visit); in the room she is in it is still on top, now.
- **Offered for every action that makes things she carries** (objects, pouches and satchels), **except** one-of-a-kind items (maximum 1: the ring, flint and steel), once-a-run and single actions, Pick Up and Put Down, and plain counts.
- **Play, then a supplier:** if she is part-way through an action that a newly played action needs (a pouch being made), the supply logic resumes that action rather than starting a second one and stranding the progress.

---

## 2026-09-30: Faster play by halving game time, not a ×2 clock

**Supersedes** the 2026-09-29 *Faster base speed* decision's method (a ×2 clock, plan 022) but not its goal. The ×2 pace felt right in play, but the run timer then no longer matched real time. The user wants one time scale everywhere (balancing, display, notes), so instead every game-time number is rescaled: durations ×0.5, per-second rates ×2 (drain, XP, carry cost), drain growth per minute compounds twice as fast (25% to 56.25%), so a run holds the same actions and XP in half the seconds. `TickEngine` keeps its old behaviour (game second = real second at 1×), and the CLAUDE.md "never the tick rate" wording needs no bending.
- **Feed Your Hours keeps its 20 s** (the user's call); its story text says twenty seconds.
- **Blurbs run on real seconds** (timer, cooldowns), so they stay readable at any game speed. Their numbers are unchanged.
- Composure slows the drain's growth by multiplying the rate, so at high Composure the rescaled growth differs from the old by about 1%. Left as is.
- Plan `022b-halve-game-time`; the one-click tool is *Hall of Echoing Mirrors → Tools → Halve Game Time*.

---

## 2026-09-30: Known-by-heart details (planning 022–025)

Settles part of the open list in the 2026-09-29 entry below.
- **The carried plan stops at the first room not known by heart.** The automatic part is always the start of a run; the frontier begins where it ends. Known rooms after an unknown one are not carried.
- **A carried action that can't start is skipped with a reason** (the queue's existing behaviour); the rest of the block runs.
- **A trip between rooms of different speeds runs at the slower room's speed**, so walking into or out of the frontier is never a blur.
- **What *Feed your hours* unlocks stays open until playtests.** Placeholder rule (plan 025): it keeps its flat 2× tier and room speed needs no unlock.
- **Numbers chosen for planning, all `~`:** by heart at 4 runs, full speed at 8, cap 5×, linear ramp between (a placeholder replacing GDD §13a's visit power law, whose range Act I's 7–12 runs would never reach).
- Plans: `022-base-speed`, `023-rooms-known-by-heart`, `ui-024-repeat-and-plan`, `025-room-speed`, built in that order.

---

## 2026-09-29: Faster base speed; rooms known by heart carry over and speed up

Playtests: the game is too slow to watch and has too little to attend to while actions run; it is turning into a background game when it should be an active one. The number of runs feels right. By run 4 or 5 the earliest tasks shouldn't have to be requeued every run. The user is merging *Idle Loops* (repeating a run's plan), *Increlution* and *Stuck in Time*. **Supersedes** GDD §13a *Realm mastery and adaptive speed* (per-realm speed averaged from per-node visits, the power-law curve to 20×), §4 *Game speed is earned* where it says the multiplier comes from per-realm visit counts, and the 2026-09-27 per-realm decision. Builds `docs/plans/ui-rework-roadmap.md` Part 2 (the planning screen).

- **Base speed ×2, as a speed setting, not in the numbers.** The player's "1×" becomes two game seconds per real second. Balance, XP, drain and benchmark times (all in game time) don't change; the number of runs stays the same. Halving only the task times was rejected: drain is per game second and XP per task scales with its length, so she would do twice as much per run and Act I would take fewer runs. **The user approved bending CLAUDE.md's "never the tick rate" wording**: ticks per game second stay 10; only game seconds per real second change, the lever the speed control already uses. *Feed your hours*' ×2 stacks on it until the room-speed work replaces it.
- **Mastery is per room, counted in runs.** Each room counts the runs in which she worked in it (once per run, not per entry, since corridors are crossed many times in one run). Counts start now, so they are already there when the rest is built.
- **Known by heart at ~4 runs.** A mastered room's actions from last run (what she actually did there, in order, not auto-inserted supply; once-ever actions dropped) carry into the next run's plan as one block. **Unmastered rooms never carry over**: the player is not given full automation until content has been solved a few times, and the frontier is always planned and played live. Runs 1–3 carry nothing.
- **Mastered rooms speed up, from ~4 runs to full speed at ~8.** Each room has its own speed (no averaging into a realm). GDD §13a's three implementation traps stand: drain scales with game time, room speed is shown on the map or queue and never in the stats panel, and anything needing a decision breaks the speed-up (a *hold*).
- **After the Summary, two buttons: Repeat and Plan.** *Repeat* starts the next run at once with the carried blocks. *Plan* opens the main screen paused before the run, with the carried blocks in the queue column to remove, reorder or add to (scheduling ahead stays as now). A separate Planning screen can come later if playtests want one.
- **Draw on the mana stone is not the automation unlock** (the lab comes around run 8+, too late for run 4). It goes back on the lab redesign's open list. **Supersedes** the 2026-09-29 lab entry's "parked for the mastered-room speed-up".

Still open (to settle when planning the builds): the numbers (`~`4 runs to by heart, `~`8 to full speed, the full-speed cap); which room a trip counts for, and how fast a trip between two rooms of different speeds goes; what *Feed your hours* unlocks once room speed replaces the global ×2 (the concept, as GDD §4 intends, or nothing); what a carried block does when something in it can't start (skip with a reason, or stop and hand over to live play); whether blocks show the GDD's >10× one-line summary.

---

## 2026-09-29: Exploring a room is done once, ever; entering rooms becomes the benchmark

Playtests (2026-09-28 and since) found re-searching every room every run, in several passes, unintuitive. **Supersedes** GDD §9 *[BUILT] Rooms are searched afresh every run* and its [OPEN] hidden-ways placeholder, and the 2026-09-26 *Searching every run* rule ("the hall shifts between runs"; contents re-searched each run). Builds §13a *Benchmarks*' "auto-designate" extension.

- **One Explore action per room, done once ever.** It replaces the repeated passes: a single long action whose bar fills continuously. **Its progress is kept between runs**, including partway (a run that ends mid-explore continues next run), so a long explore is never a wall.
- **Things still appear along the bar**, at their existing percentages (a wisp at 33%, the ring at 66%, ways at 100%). Once found, actions and ways stay found in every later run. Once-per-run actions (the satchel, the ring) are still once per run; they are just available from the start.
- **Cost:** the Explore time starts as the old full search (passes × pass time), a balance number to tune in the balance pass.
- **Perception (placeholder rule):** makes the Explore bar fill faster, the like-for-like of "yield per pass". Its real job waits for a pass over all the stats (the user: stats are mostly placeholders for ideas not yet fleshed out; skills carry the game now).
- **Benchmarks move to entering rooms.** Every room except the start room is a milestone on her first entry each run, timed against last run as usual. **The first-ever entry to each room shows a story passage** (placeholder text until the user writes it).
- **Search-progress switches fire once, ever** (*A Glimmer*, *Found the Ring*, *The Memory Is Wrong*): they are no longer timed each run; room entry does that job now.
- **No switch back.** The backlog's Loop Settings toggle for the old rule is dropped: one rule to build and test. If the hall is ever scrambled again, that is a story event resetting chosen rooms, not a setting. *The Hall Has Shifted* story and the "she must search this room again" reason go.
- **Also superseded:** backlog *Ways unlock after 8 searches* (ways now stay found once explored).

---

## 2026-09-29: Milestones table drops one-off milestones after their run (bug found in plan 017's build)

The user noticed "Lost Roland" (a prologue-only collapse, locked out for good once flipped) kept showing as a permanent dim "not reached" row on every later Summary page. **Supersedes** part of plan 017's settled rule 3 ("every milestone she has ever reached, in any run") for milestones that structurally can't be reached a second time: a milestone whose switch locks its own trigger task when it flips (`Simulation.Unlocks.cs`, `CanBeReachedAgain`) stops appearing in the table from the run after it was reached, instead of lingering forever. General rule, not special-cased to Lost Roland — the same `CanBeReachedAgain` check already used for kept-resource milestones now also covers `LoopEndedDuringTask` triggers that lock their own task.

---

## 2026-09-29: Summary page layout bug fixed and playtested (plans 016/017)

Plan 017's notes flagged as unchecked by eye: "a run with skills, stats, chips and a walked-out ending." The user hit it: `SkillRow` overlapped the milestones table and chip columns below it, missing a `ContentSizeFitter` (see plan 016 notes). Fixed and confirmed by playtest.

---

## 2026-09-29: Queue column built (plan ui-019; user playtested)

Choices made while building it (the user approved the assumptions and the extras):
- **The column starts open**, and the fold choice is remembered as a player setting (not in the run save). **Confirms** the 2026-09-29 "Queue column" entry.
- **Widths:** Story 400 px, Queue column 360 px; the map takes the rest. Folded, a strip (current action, bar, time left, what's next) sits under the Story box.
- **Q folds and unfolds** the queue (main page only). The strip glows when the player queues something while folded.
- **Backtick (`) shows and hides the dev panel**, as well as F1 (development builds only).
- The pockets overlay stays on the map's left.

---

## 2026-09-29: Play works in any room she can reach (plan ui-015 built; user playtested)

Settled with the user while playtesting Schedule anywhere:
- **Play in a room she isn't in** puts the action at the **start of her visit to that room** (straight after the trip that enters it; the room's last visit if it appears twice). For a room off her route, the shortest walk is added, then the action. Where she is now, Play is unchanged (top of the queue, starts now). **Supersedes** the 2026-09-29 line "Play is unchanged (only where she is now)" and GDD §13 "Play (top)" acting only in her room.
- **Carry stays only where she is** (it starts now).
- **Schedule and Play greyed only when no way there was found this run**; the tooltip says why.
- The map click on a room next door still takes that way, even if a longer path would be quicker; a far room gets the shortest walk.
- **Tooltip wording** for these was written by Claude, not the writing session (user's call: simple tooltips need no `/writing` pass).
- Not settled, left as built: playing two actions in the same room stacks the second ahead of the first.

---

## 2026-09-29: Queue column and Schedule anywhere (UI session, plans ui-015 and ui-019, not built)

Settled with the user after playtesting:
- **Schedule works in any room Clara can reach.** In a room already on her route, the action goes in at the end of that visit. In a room off her route, the queue adds the walk there by the shortest way, then the action. Play is unchanged (only where she is now). **Supersedes** the built rule that Schedule works only in the room where the queue ends (GDD §13 "Schedule (bottom)").
- **The queue is a right-hand column** beside the Story box, top to bottom. A button folds it to a slim strip (current action and progress bar) and opens it again, pushing the map left. The player chooses. The full-width bottom strip goes. **Supersedes** §13a's roadmap departure "a tab at the bottom slides up a drawer with the whole queue laid out horizontally".

- **A room visited more than once:** the action follows the visit, so Schedule adds to the room's **last** visit on the route (*Room 1: a, b → Room 2 → Room 1: a again, c*).
- **The shortest way** is the fewest seconds of walking, through ways found this run only (so a queued walk is never skipped); a room reachable only by a way known from a past run stays greyed with a reason. Ties go to the fewest rooms, then the order the ways are listed.
- **Clicking a far room on the map** adds the whole shortest walk there, by the same rule.
- **The fold choice** is a player setting, not part of the run save.
- **Stop numbers and the route line** on the map: already built (plan 006), so nothing new.

---

## 2026-09-29: The Mirror's Laboratory, redesign in progress (design session, nothing built)

Settled with the user, in the `/design` session (a work in progress; the session is not finished, see the open list):
- **Every lab verb, switch and number in the build is a placeholder.** The room is redesigned from what it gives, and verbs are derived from that. **Supersedes** the 2026-09-25 "Inside the lab" six actions, and the placeholder answers proposed by the writing session (a persistent mana-stone resource; testify as a separate verb).
- **Act I's shape:** about 7–12 loops. Runs 1–7 reach the lab mirror with the ring (chase, wisps, phials and candles, satchel and the ten candles, the ring). The lab room takes about 3–5 further loops. **Supersedes** the opening brief's fixed loop numbers 5–10 (its order of beats stands only where restated here).
- **Two lab mirrors:** the one she starts at (no exit) and the lab mirror inside the realm, which the lab room's work unlocks and which becomes the exit (the portal home, safe, where she spends meta currency and can stash items to keep).
- **Phase 1, put the room to rights.** Ends with a once-ever verb: talking to Roland's memory and convincing him to take the ring. Its two permanent effects: the ring never has to be obtained (and carried) again, and the lab is open on the next visit without it. It also unlocks phase 2's verbs on the next run. It is the room's first benchmark.
- **Phase 2, after Roland takes the ring.** The white sapphire earrings are found by Search (only after phase 1); they add a second consumable slot for denser wisps of memory, beside the pouch of phials. The well of memories still gives phials. The dense-wisp restorative is slower and stronger than phials. Then a large stat-check verb crafts the gem: the memory's replica of the gem her reflection stole.
- **The gem:** gives access to the first pool (Amber, arriving empty; no Amber magic is usable in Act I) and wakes Attunement, which currently does nothing. It opens the mirror and unlocks the last verb, *Exit through the glowing mirror*: a story beat (she is back in the lab), then the Summary screen (being redesigned), then the meta-currency screen (**not designed or built**).
- **Failure model:** the Increlution way: the gem task is so long and costly that she first runs out of vitality partway, keeping the XP. A low stat or skill gate (visible in the tooltip, never secret) plus a repeatable **practice verb** with a high XP multiple that is worse than advancing (farming possible, never better). The multiple is a `~` number.
- **Pacing:** on the first visit she completes only a few tasks; after repeats her stats, vitality and speed let her do the whole room in one go. One or two heavy verbs are once-ever switches (very long, may nearly drain her, never repeated once done). Each visit she arrives with more vitality (the ring's cost is removed after phase 1). Design principle: the further she goes, the harder the actions and the more XP they give, which makes earlier rooms easier.
- **More of her skills are used** in the room, and new ones may be introduced here.
- **Dependencies:** some automation and speed-up must exist before this room, so earlier content can be run through quickly (backlog: game speed from realm mastery, Planning screen). The meta-currency screen and the Summary rework are separate, later work.
- **"Draw on the mana stone" is parked unchanged as a placeholder**, reserved for the mastered-room speed-up rework (backlog Next 2). **Supersedes in intent** the 2026-09-25 "warmth halves every trip" entry.
- **GDD sections superseded:** §12 *Verbs in the mirror lab* (Craft ends Act I) and the Act I finish description in the opening summary ("completing the lab and exiting through the mirror"). The GDD was updated 2026-09-30: a new §12 subsection, *The memory lab: Act I's finish*, and the Craft row. The opening brief's loops 5–10 table was replaced the same day by "The rest of Act I: the lab memory".

- **Phase 1's shape:** the old verbs become prerequisites, not a chain: Search (shows what is wrong), Watch him (kept insight), Study the tome (Understanding), Attend (staying in the memory). **Talk to Roland** is one big once-ever verb that needs some of them, so the player chooses what to afford each visit over 2–3 visits. Which prerequisites, and how many of each, are placeholders.
- **A new skill, Convincing** (placeholder name), paired with **Attunement**: trained by talking to Roland and reused whenever she interacts with other memories. Every task trains one skill and one stat; the game keeps five stats and grows to about 7–10 skills. Attunement's first use is now the talk, before the gem. The user has not yet done a full pass on what each stat does, so the pairing may move.

- **The earrings are kept once found**, like knowledge: after phase 1 restores them to the memory, finding them once means they are always hers. (The user has already made searching and exploring do-once, so this matches.)

- **The exit is the starting mirror.** The gem opens the mirror she came in through (which could not be exited until now), so the last run of Act I is the long walk home through the whole hall, with the gem. It reuses every Act I room and shows off how much faster she has become. *Exit through the glowing mirror* lives at that node, not in the memory room. The ring-gated mirror into the memory stays an entrance only.
- **[DIRECTION, Act II, placeholder] Amber binds things to a place; exiting binds things to her.** Carrying an item out keeps it on her, portable, only at a run's end. Amber binding keeps an item or state *where she left it* in the realm, across runs: a stash, a candle kept lit, a way held open. Limits so it doesn't replace the exit: it costs Amber pathos (the pool arrives empty), has a few binding slots (more with mastery, `~`), and stays in its place. It gives the old Act I rooms a job in Act II: she returns to bind shortcuts there, and the hall becomes hers. Not designed; revisit with Act II (relates to backlog *Floor stashes kept between runs*).

- **The room's verbs (placeholder list, mechanics only; all numbers `~`):**
  - *Phase 1:* **Search the room** (Searching + Perception, once ever: shows that the memory is wrong, and the verbs) · **Gather at the well** (phials, as now) · **Watch him** (skill tbd + Perception, once a run, gives a **kept** insight) · **Study the tome** (Studying + Scholarship, Understanding, **per run**) · **Attend** (skill tbd + Composure, once a run, **per run**, lets her stay; needed for the talk) · **Talk to Roland** (Convincing + Attunement, once ever, heavy; needs some insight, Understanding and Attend).
  - *Phase 2:* **Search again** (finds the earrings, kept) · **Draw a dense wisp** (skill tbd + Endurance, repeatable, fills the earrings) · **Practise the cut** (the new crafting skill + Attunement, repeatable, high XP, always worse than advancing) · **Craft the gem** (the crafting skill + Attunement, once ever, very long, low visible gate).
  - *At the starting mirror:* **Exit through the glowing mirror**.
  - *Dropped:* Clear the bench (cut), Take the tome (folded into Study), Cut the stone (becomes Craft the gem), Draw on the mana stone (parked). The build's Understanding and Steady hands resources, and the switches *The Memory Is Wrong*, *What He Did That Night* and *The Stone Is Cut*, are to be reworked to fit.
- **Carry-over in phase 1 (option a):** Watch him's insight is kept; Understanding and Attend reset each run. Each visit adds insight until the talk fits in one run, and the first successful talk is a full-run effort.
- **A new crafting skill** (name tbd), paired with the existing Attunement stat; reused in Act II for recutting the gem and adding facets.
- All five stats are used in the room (Perception, Scholarship, Composure, Attunement, Endurance).

Still open or placeholder (labelled, to settle next): the verbs' numbers; the skills marked tbd; the crafting skill's name; how close the Act I rooms are (map and pacing, for later); the practice verb's numbers; the plot of what the memories reveal (unwritten, not to be invented).

---

## 2026-09-29: Blurb buckets match by a task's own BlurbTopic, not TaskKind or a task list (plan 018, revised)

- **A generic bucket (`gathering_wisps`, `instantiating`, and their `start_` moments) fires for any task carrying a matching `BlurbTopic`** (Gathering, Instantiating), a new field on `TaskDefinition` set per task, independent of `TaskKind`. A future task tagged with that topic lights up the bucket automatically, with no bucket edit. Context buckets (item-specific, room-specific, held/not-held, loop range) are unaffected; a bucket can combine `topics` with a specific `tasks` entry to pin one task's own line under the generic one (priority/cooldown fallthrough from plan 015 already handles the layering).
- **First shipped matching by `TaskKind` instead; that was wrong and was replaced same-session, before commit.** `TaskKind` only ever meant "which stat trains" — several unrelated tasks share one on purpose (the generic Pick up verb and Cut the Stone are `TaskKind.Gather`/`Instantiate`, same as Gather a wisp and the mirror-crafting tasks), so matching blurbs by `TaskKind` made picking up any object fire wisp lines, and cutting the stone fire mirror-instantiating lines. Caught by review before commit; `BlurbTopic` is the fix, a label that means "this task's story moment," chosen deliberately per task rather than inherited from what trains a stat.
- **`GatherAWisp` and the four mirror-crafting tasks plus `TakeTheRing`** ("Instantiate Roland's ring," already added to `instantiating`'s old hand-set task list beyond the plan's expected four) are tagged with their `BlurbTopic`. **Cut the Stone and Fill a Phial of Memory are deliberately left untagged** (`BlurbTopic.None`): they share a `TaskKind` with the tagged tasks but were never in these buckets' hand-set lists, and whether they belong to a topic is an open call for the writer, not guessed here (`PROJECT_NOTES.md` placeholder list).
- Satchel-upgrade-level gating (a bucket for the satchel's tier) stays deferred: no such mechanic exists in `ResourceDefinition`/`Simulation.Resources.cs` yet (backlog).
- No GDD § superseded.
- Plan: `docs/plans/finished/018-blurb-verb-kinds.md`.

---

## 2026-09-29: The run report lists milestones against the last two runs (plan 017)

- **Improvement compares this run with the last run** (negative = faster). A milestone reached for the very first time shows "new" instead.
- **The table lists every milestone she has ever reached**, this run's first (by time), then the rest in the order first reached, dimmed as "not reached". Best-ever times are stored (each run keeps its milestone times) but not shown yet; no fifth column.
- **"Previous run" is the run before last**, counting every run whatever its ending; a run ended early is compared like any other.
- **"Changed for good" shows the permanent switches flipped this run; "What she carried" shows kept things only when she walked out**, and nothing on other endings. The Done list, tools and stats held, and rooms explored are no longer on the page (the data stays in the run report).
- Extends GDD §13a *Legibility: the run report* [PROPOSED] and *Benchmarks*; no GDD § edited.

---

## 2026-09-29: The run report shows time by skill (plan 016)

- **The run clock only runs while Clara is doing an action.** Every tick belongs to one action, so there is no idle or stalled time to show; a test checks that the time split adds up to the run's length.
- **The Summary page shows where the time went by skill:** one strip slice per skill (icon, widest first), and under it what she learned (×before › ×after from mastery). Actions with no skill go into one "Other" slice (a placeholder).
- **"Best" means the fastest time to reach a milestone** (part B of the run report), not the longest run; the headline compares with the last run only. Ending a run early only means fewer milestones were reached.
- **A wall is a challenge that needs a stat or pool to be high enough.** None is built yet; the lab could hold the first one.
- Extends GDD §13a *Legibility: the run report* [PROPOSED]; no GDD § edited.

## 2026-09-29: Instantiate never fails; a full pocket pushes an item out (ring text)

- **Instantiating an object from a mirror can't fail.** When Clara's pockets are full, the new item pushes one of her carried items out to make room. Which item goes, and where it ends up, is still open. The ring text says only that "something" leaves her pockets (`ring_present`, `ring_carried`).
- **Passage 09 is rewritten for the mirror:** the ring is behind the glass in a mirror along the Right Corridor, and the passage ends by naming *Instantiate Roland's ring* and its vitality cost per second (a placeholder in the text, to be filled from data).
- No GDD § superseded (extends §9, Instantiate).

## 2026-09-29: Blurb cooldowns and a short-lived story feed (plan 015)

- **A bucket that just spoke steps aside, not takes turns:** it's skipped even while still the best fit, until its cooldown (game seconds this run) elapses; picking falls through to the next fitting bucket, and a moment with every fitting bucket cooling down stays silent, as today. `BlurbLibrary.defaultCooldownSeconds` covers every bucket unless it sets its own `cooldownSeconds`. A new run (a different loop number) always clears it, whatever the last run did.
- **The story feed shows only a handful of *Around her* lines at once:** they fade to nothing (not a 0.35 floor) between `_fullStrengthLines` and `_maxAmbientVisible`, then are dropped, rather than only at the 150-line safety cap (which stays as a long-run memory guard). Milestone cards are unaffected: they never fade or drop.
- **`reaching`'s "cut off once the mirror gem is crafted" fix is deferred, not guessed:** nothing past *Cut the stone* in the lab is built yet, so there's no mirror-gem task or switch to wire (the user's call, 2026-09-29 — every verb past reaching the lab is still being designed). The other three rule gaps (`hall_corridor` → *Mirrors in the dark*, `ring_refused` → the corridor's room kind, `start_chase`/`lab_sealed`'s loop limits) are fixed. `BlurbImporter.FixKnownRules` is written as a list of guarded one-off fixes precisely so `reaching`'s (and any future bucket's) can be added later without restructuring it.
- No GDD § superseded.
- Plan: `docs/plans/finished/015-blurb-triggers-and-frequency.md`.

---

## 2026-09-29: A one-of-a-kind thing with nowhere to go lands on the floor (playtest)

- **Bug seen:** with her pockets full of Wisps and the floor full of Wisps, *Instantiate flint and steel* was refused ("she can't hold any more Flint and steel…"), and it can't be got another way, so the run was stuck without it. Cause: a one-of-a-kind thing may only push something out of her pockets, and the only candidates were Wisps (no floor room for more) and phials inside her pouch (not in her pockets).
- **Rule (the user's call, placeholder):** the player expects to complete the action however full she is. A one-of-a-kind thing that can't get a pocket, even by pushing something out, is put down on the floor here instead of being refused, past the floor limit if the floor is full too. Nothing in her pockets is destroyed; she picks it up when there is room. Only where there is a floor (a room): a game without rooms still refuses it.
- Supersedes *One-of-a-kind objects* (2026-09-27, in `decisions-log-older.md`) where it says such an object "never overflows to the floor", and the same line in *Full pockets* (2026-09-28). A second copy is still never made: the copy on the floor counts.
- Not decided, for later: the feed says nothing when it lands on the floor (only the map's floor dot shows it).
- No GDD § superseded.

---

## 2026-09-29: A blocked one-of-a-kind thing destroys something on the floor to get in (playtest)

- **Bug seen:** with her pockets full of Candles and the floor's Candle space also full, picking up (or Instantiating) a one-of-a-kind thing like the ring or flint and steel had nowhere to go: the only pushable candidate (Candles) couldn't be put down either, so `PushedOutFor` found nothing and the action refused, or the thing sat on the floor forever unable to be picked up. Worse than the *lands on the floor* fix below: an item already lying on the floor can't "land on the floor" again to get unstuck.
- **Rule (the user's call, placeholder):** the player must always be able to pick it up. When nothing can be pushed out of her pockets because every candidate's own floor space is full, what's most of it on the floor here is destroyed instead, one at a time, until a candidate has room and can be pushed out to free her pocket. Never a one-of-a-kind thing on the floor. Which item is destroyed (just the most-held kind, for now) and whether the feed says anything about it aren't chosen yet.
- Extends *A one-of-a-kind thing with nowhere to go lands on the floor* (2026-09-29, above): that fix still applies when nothing on the floor can be destroyed either (an empty floor, or one holding only one-of-a-kind things).
- No GDD § superseded.

---

## 2026-09-29: A this-run count at its maximum counts as done this run (plan 013)

- **Bug:** *Light a candle* in A Dark Hall stayed offered even with all 15 candles lit, and refused with "she can't hold any more Candlelight, in her pockets or on the floor here" — the wording for full pockets, though the candle count has no pockets at all. Cause: `GivesOnlyWhatExists` only treated a *kept* resource at its maximum as "already exists"; a this-run count (not pocketed) at its maximum fell through and stayed offered, with a stale reason from before it filled up.
- **Fix:** a non-pocketed this-run count at its maximum now counts as "already exists" too, so the action leaves the popover once it's full — the same rule already used for a one-time kept reward once earned (no new UI state). If refused anyway (a queued entry that finishes the room), the reason is now `reasons.count_at_max` ("all {max} {item} are already done this run"), not the pockets-and-floor wording; an object's pockets-full refusal is unchanged.
- Confirmed against the player's real save (`Assets/Data/Tasks/Hall/LightTheCandles.asset`, `Items/Candlelight.asset`, max 15) via Unity MCP, not only the test fixture; the same fix covers The Left Corridor's candles (max 10).
- **`InstantiateACandle` is untouched**: it still gives a Hanging candle to carry to the next room even once the hall is fully lit — that's a separate task from *Light a candle* (the user's clarification, 2026-09-29).
- No GDD § superseded: a code/text bug fix, not a design change.

---

## 2026-09-29: UI colour roles (plan 012)

- **Every recoloured Graphic has a role** (panel, row, band, drawer shade, map background, popover, stop card, chip, chip (faint), tip, notice, track, bookmark tab, drag ghost, button face, accent, accent text, main text, secondary text, page tint, vitality fill, mastery bar, level bar, queue progress, searched bar, drop line), and each role's colour is set in one asset (`Assets/Data/UI/UiColours`), so a palette change is an asset edit and *UI → Apply UI Colours*, not code plus a recolour pass. Mirrors plan 011's fonts (`ColourRole`/`UiColours`/`ColourRoleTag` mirror `TextRole`/`UiFonts`/`FontRole`).
- **Out of scope, staying in `UiStyle`:** map, floor and explore-bar colours (own asset, `MapStyle`); hue colours; rich-text hex strings (`Warning`, `Milestone`, `Levelling`, `Muted`, `ReturnMarker`); state-dependent colour pairs (`SpeedTierLocked`/`Current`, `BookmarkTab`/`TabOpen`, `StopCard`/`StopCardCurrent`, `Glow`, drag/unavailable alpha).
- **A tagged button's hover/press/disabled shades are derived**, not hand-set, from a fixed tint (Step 88's own ratios), the first time its ColorBlock is still Unity's default — a hand-tuned ColorBlock after that is left alone. This only reaches a Selectable whose `targetGraphic` is on the same GameObject as the Selectable itself (true of every Button; not true of a Scrollbar, whose Handle is a child of its root) — a limitation Step 88 already had, now just documented instead of silently repeated.
- **Setting a tagged Graphic's colour on a prefab instance in the scene needs `PrefabUtility.RecordPrefabInstancePropertyModifications` after the field set**, or the scene keeps re-serialising the old override value on save even though the live value is correct. Found via a leftover scene-only alpha override on `MapPanel` that predated this plan; fixed in `UiTools.ApplyUiColours`. `UiFonts`'s Apply (plan 011) likely has the same latent gap for a prefab-instance text with an existing override — not fixed here (backlog *Later*).
- Step 88 (*Warm colours*) retired: `UiColours.asset`, seeded from `UiStyle`'s current values, and Step 90's tagging reproduce its result exactly (467/467 EditMode tests, confirmed live via Unity MCP).
- No GDD § superseded: §13a asks only for legibility.
- Settled with the user 2026-09-29; built the same session.

---

## 2026-09-29: Clara is 41; game text is plain, present tense

- **Canon:** the novel is set in 1907 with Clara at 20; the game is **21 years later**. Clara is 41, a practised eromancer, a mother, happily married to Roland. "Twenty-one years" is the one number used in all text. The game's calendar date is **[OPEN]** (CLAUDE.md said 1907).
- **Voice:** first person, **present tense** for action; past tense only for memory and backstory. Plain, short, dry; the game's voice is not the novel's. Roland and the novel's "mad narrator" are not game voices. Full rules: `DesignNotes/Writing Guides/ProseVoiceGuide.md`.
- **Supersedes** GDD §3 ("twenty years past her training"; "Setting is 1907 … some years after the events of the book"): the GDD still needs updating.
- Settled with the user 2026-09-29.

---

## 2026-09-29: No design chat: design work happens in Claude Code

- The claude.ai design chat and its GitHub sync are retired (the user's call: too many steps, too many tokens). Design talk now happens in Claude Code sessions, with `/design`, which reads only the GDD section under discussion.
- **Supersedes** the 2026-09-28 "BUILD-STATE split" and 2026-09-27 "GDD labels describe the build; BUILD-STATE wins" entries: `/sync-state` and the two BUILD-STATE files are gone (kept in `/Remove Me/`). The code and `docs/plans/` are the truth on what's built; the GDD's [BUILT] / [DIRECTION] labels are updated only when the user asks.
- Claude Code no longer waits for handoff notes: when the user settles something in a design session, it adds a decisions-log entry and, if asked, edits the GDD (commit message starting GDD:).
- The log is split: `decisions-log.md` holds 2026-09-28 onward; `decisions-log-older.md` holds the rest.

---

## 2026-09-29: Book fonts and type roles (plan 011)

- **Two book fonts, one per job:** EB Garamond (the mock-ups' serif) for headings, room and action names, story lines, story cards, the story pop-up and pop-up titles; Inter for pop-up text and tables, numbers, buttons, chips, the feed's notes and small print. Boecklins Universe stays for the menu title only. **EB Garamond over Cormorant Garamond**, to match the design chat's mock-up. Boecklins was hard to read small.
- **Every text has a role** (Title, Heading, Name, Story, Body, Small), and each role's font and size are set in one asset (`Assets/Data/UI/UiFonts`), so the whole UI can be retuned in one place. Starting sizes at 1920×1080: Title 48 · Heading 24 · Name 19 · Story 22 · Body 17 · Small 14 (all `~`, a little above the mock-up for Steam Deck).
- **Readability pass** (the user's screenshot of 2026-09-29): pop-ups get more padding (16 px); room names on the map may reach past their frame and shrink a little rather than break inside a word; action names end in "…" when cut; the popover's floor and way chips are small print; the story pop-up's passage no longer shrinks to fit (it scrolls); the speed tiers have room for their labels. The tooltip's layout is unchanged: its redesign is backlog *Next* 2.
- **Claude manages the UI layout** (the user's call, 2026-09-29, now in CLAUDE.md); the user tweaks it by hand as needed.
- No GDD § superseded: the GDD has no font rule (§13a asks only for legibility).
- Settled with the user 2026-09-29 before building.

---

## 2026-09-29: Playtest batch: action times, one-line rows, warm colours

- **An action shows how long it takes her now**, her skill included, never the time at normal speed: "9.4s" a go, or "3.2/s" once she does it more than once a second (counted in whole ticks, as it runs, so at most 10 a second). The row no longer names the skill and its level: its icons show them. The user's call.
- **Action pop-ups follow Increlution's shape:** the name, how long it takes (or "Completes 3.2 times a second"), what it gives, needs and costs; then small print: what it is, "Faster with her Wayfinding skill: ×1.30 now", its XP, and how long it repeats. The "at normal speed" line is gone. Icons inside the text wait for the content pass.
- **Action rows in a room's popover are one line** (34 px, was 58): icons, name, time on the right, then small Carry, Play and Schedule buttons. A reason it can't be done follows the time and may take two lines; a long name shrinks a little, then is cut off (its pop-up has it whole). The popover is a little wider (540 px, was 480) to fit them. It was too tall (the user's report): 819 px for the lab, now about 580.
- **The pockets overlay starts lower**, clear of the stats row now that its chips take two lines.
- **Warm colours, from the user's mock-ups** (kept outside the project, inspiration only, not a settled layout): warm near-black panels, cream text, dark buttons that brighten when hovered, amber for Play, the speed in use, mastery and queue progress; the searched bar green; vitality stays red. Claude's judgement, at the user's invitation; the layout and fonts of the mock-ups aren't adopted yet.

---

## 2026-09-29: Stat and skill icons, and table tooltips (plan 010)

- **Every stat and skill has an icon** (the user's set, `hall_icons_v1`: stats solid, skills line). It shows before the words on its chip in the stats row, and at the far left of each action in a room's popover: **the skill first, then the stat it trains**, the stat's icon dimmer so the skill (which sets the speed) reads first. An action with no skill or no trained stat leaves the space blank, so names stay lined up. Hovering an action's icon names it with the chip's short text ("Wayfinding ×2.10"); hovering a chip's icon shows the chip's full pop-up.
- **Stat and skill pop-ups are a table**, like Increlution's: the heading (name and what it does now), then rows *This run*, *Mastery* and *Total* under *Level*, *XP* and (skills only) *Speed*, the numbers lined up on the right; then the explanation in small, muted print. A skill's rows show each part's share of its speed; a stat's *Total* row shows its strength (level plus mastery) and what that does. At the most mastery allowed, the XP cell reads "max". Supersedes the Stats tab's old tooltip text kept on the chips (2026-09-28, plan 009).
- **All hover pop-ups are smaller:** text size 30 → 20, widest box 420 (was 720). The user's OK, 2026-09-29.
- Still to come, not part of this: what the action pop-ups and other pop-ups say, with icons inside the text (the content pass); icons on queue rows, the ribbon, stop cards and the pockets overlay; a colour per stat or skill.
- **Steam:** the icons were drawn by a script in an AI session; list them in the AI disclosure.
- Settled with the user 2026-09-29 (playtest, plan 010) before building.
