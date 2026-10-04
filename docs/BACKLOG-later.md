# Backlog: Later and Ideas

Moved out of `docs/BACKLOG.md` so that file stays short: search this one with Grep when you need it (`/plan-feature` and `/wrap-up` add new Later and Ideas lines here). Same one-line format.

- ## Now
- implement a hotkey space bar for pause and resume
## Later
*Wanted, not scheduled.*

- **Hidden finds: does a search hint at them? (design question, deferred 2026-10-01)** — when stats arrive (first exit), should a search show that a Perception-hidden find is there ("something you can't make out: Perception N")? Act I has no stats, so it can't arise before; the log leans towards showing it · §6 · S · decide with the stats work (Now/Next *Act I without stats*, Next 8)
- **Trip charges project holdings (plan 030b, decided 2026-09-30):** work out what she'll hold at each queued trip inside the plan-warnings walk (assume earlier gives succeed), so needs and trip charges agree; when a second Travel-cost item exists or the mana stone verb returns (Next 9) · M
- **Place the corridor searches** — `SearchLeftCorridor` and `SearchRightCorridor` are kept but in no room's list (decisions log: locked, kept for later; the right one unlocks again once A Glimmer's ring exists); add them to the corridors' task lists with that unlock, and re-tune their cost (40 vitality each) · §13 · S · plans 030a/030b follow-up; lab search deleted 2026-09-30
- **Queue's time left counted in whole ticks** — `CurrentTimeLeftSeconds` is fractional, while action rows now round to whole ticks (`SecondsAtSpeedNow`), so the two can differ slightly · — · S · reviewer note, 2026-09-29
- **Core refuses a trip with no way from where it starts** — `ScheduleTrip`/`PlayTripNow` accept any room; only the map checks (`CanScheduleTripTo`), so other callers can queue a trip that is skipped later · — · S · reviewer note, refactor 2026-09-28; changes behaviour, so not a refactor
- **Move RoomPopover's `CanPlayHere`/`CanScheduleHere` into Core** — two more small rules in the UI (where Play and Schedule apply) · — · S · reviewer note, refactor 2026-09-28; `/refactor`
- **Tell the player when a one-of-a-kind thing lands on the floor** — nothing in the feed says the flint and steel (or ring) went on the floor because her pockets were full; only the map's floor dot shows it · §13a · S · seen in the 2026-09-29 review; a feed line via `FeedNotes`; the unused `ItemsPutDown` event was deleted in the 2026-09-30 code-health pass, so this item re-adds it
- **A queued Instantiate for a one-of-a-kind thing already lying on another floor still starts** — `CanStart` never checks that a copy exists, so it spends its time and gives nothing · — · S · reviewer note, 2026-09-29; Core change, tests first
- **Room speed read per tick, not per frame (plan 025)** — `TickEngine` reads the room speed once per frame, so a task that starts mid-frame at ×50 tier runs its first few ticks at the old speed; harmless now, fix only if it shows · — · S · parked from Now, 2026-10-01
- **Composure's drain growth as an exponent (plan 022b)** — `Composure` multiplies the drain's growth *rate*, so the halved growth (56.25%/min) differs ~1% from the old curve at high Composure; only if that ever matters · — · S · parked from Now, 2026-10-01
- **Merge duplicate suppliers (supply fix)** — `QueueSupplier` reuses a suspended entry only if it has saved progress and no supply role; one pushed down with no progress still gets a fresh duplicate supplier; harmless · — · S · parked from Now, 2026-10-01
- **`ContainerFor` without rebuilding the containers** — it works them out afresh on every call, which is fine at today's sizes; cache them if a profile ever shows it · — · S · parked from Now (code-health pass), 2026-10-01
- **Vitality pool name as a text key (plan 028)** — the vitality pool's name is read when a run begins, so a `game_text.txt` hot-reload shows from the next run (hue pools read it each time); give `Pool` a name source if that ever matters · — · S · parked from Now, 2026-10-01
- **Rethink full pockets and containers as a whole** — `PushedOutFor` is a placeholder: it never pushes out what's in a pouch, and skips Wisps once their floor is full; consider a clearer rule (or a bigger floor) so a stuck state can't come up · §13a · M · playtest 2026-09-29
- **`SupplierFor` checks only pocket room** — when picking an action to supply a need, it ignores floor space, so a supplier that would drop onto the floor may be passed over · — · S · found 2026-09-29 investigating plan 013; didn't cause the candle bug
- **Queue look-ahead counts push-out room** — `GoesUntilGivesAreFull` still says 0 goes for a make-action once pockets and floor are full, though it can push something out · — · S · reviewer note, playtest batch 2026-09-28
- **Queue forecast: projected finish and “dry at”** — shows when the queue ends and when her vitality runs out, so a route can be judged before running it · §13a · M · left out of plan 004 (2026-09-28); fits the drawer's footer and the ribbon's right end; needs a Core look-ahead with tests
- **Visit counts per room** — the data behind “by heart” and realm speed; shown in the room popover · §13a *Realm mastery* · S · not tracked yet; count from the start so the unlock is retroactive
- **Milestones pay once (meta currency)** — the economy's spine; no grind path · §4 · L · currency name still open (§17 q21)
- **The gem as a carried object** — makes harvesting a route puzzle; dropped where she falls · §4 · M
- **Harvest cost: stat check, +25% per repeat, show next cost** — soft-caps grinding and makes it a decision · §4 · M
- **Carry capacity bought in steps** — a meta purchase · §4, §6 · S · build uses 5 pockets + satchel; numbers differ from the GDD
- **Pools taken in the prologue; Act I vitality only** — pools return as progression, in wheel order · §5 · M · GDD v0.5 draft proposal; build: all seven full every run; also the gem giving the empty Amber pool at Act I's end (design only today: `TheGemIsMade`'s `unlocksPools` is empty); the colour payoff (UI-BACKLOG Next 6) waits on this
- **Each pool shields a seventh of the drain; pools refill by gathering** — an empty or missing pool's share falls on vitality; pools have three jobs (shield, fuel for workings and binds, threshold for walls); the Amber maximum rises only with meta currency · §5 · M · decisions log 2026-09-30 (partial empty-pool penalty; one-seventh is a placeholder rule); build: all seven full, paying only for travel; with *Pools taken in the prologue*
- **Boundary hues (Maroon, Golden-bronze) as mechanics** — not pathos pools · §5 · M
- **Traversal economy with baked tiers** — travel cost rises with distance; hall drains faster than realms · §12 · M · build: flat 5
- **Mirror lab: no drain, no recovery** — a safe place that can't be farmed · §12 · S · name clash: the build's *Mirror's Laboratory* is Clara's memory, not this lab
- **A longer prologue (~90 s)** — three seconds can't teach the drain · §5 · S · GDD v0.5 draft proposal and opening brief (now GDD §12a); build ~3 s
- **The Hub and realms (Amber first)** — the vertical slice · §12 · L · stage 2
- **Floor stashes kept between runs** — the build's piles vanish every run; drawing them on the map is in the UI rework · §13a · M
- **Clamp speed at a new run too** — `FastestSpeedUnlocked` counts this-run items, so a per-run item that unlocks speed would leave the next run fast with the speed tiers hidden · — · S · no such content today (Quickened Hours is kept); only needed if one is added
- **Standing orders** — an ordered list of ~5, never reroute · §13a · M
- **Items kept if carried out** — physical items (satchel, pocket expansions, tools) persist only by walking out with them; knowledge-like rewards stay kept on obtaining · §5, §11 · M · decided 2026-09-27; needs walking out
- **Remove leftover assets** — two unused Search tasks (the corridor ones are kept on purpose), Tool A, Focus; after 027a also the unlisted lab assets (ClearTheBench, TakeTheTome, CutTheStone, Benchcleared, Thetome, TheStoneIsCut) · — · S · clean-up; deleting is permanent, ask first (saves skip missing Ids with a warning)
- **Review whether PROJECT_NOTES.md duplicates BACKLOG or the decisions log; merge or retire it** — the same fact kept in two places drifts apart · — · S · docs clean-up
- **Placeholder hover text for the other hall actions** — Gather a wisp, Fill a phial, the searches have none · — · S · playtest 2026-09-28 (the mirror actions got theirs)
- **The first wall, in the lab** — a challenge that needs a stat or pool high enough; the run report can then show "the wall needed X, she brought Y" (§13a *Legibility*) · §8, §13a · M · the user, 2026-09-29, while planning 016: "the lab might be a good place for the first one"
- **Satchel-upgrade-level gating for blurbs** — a context bucket keyed to a satchel tier once satchel upgrades exist; no upgrade mechanic is built yet (satchel is one held item, not tiered) · — · S · deferred while planning 018 (blurb verb kinds)

- **Writing brief for the lab's placeholder text** — list every `[PLACEHOLDER]` key 027a–c add (five passages, tooltip asides, insight lines) for a `/writing` session, so the lab playtest reads as a story · §12 · S · from plan 027 follow-ups, 2026-09-30
- **Pacing record: runs to each lab milestone** — the run report and Summary show how many runs it took to reach the talk, the gem and the exit, to check a cold playtest against 7–12 runs · §13a · S · from plan 027 follow-ups, 2026-09-30; milestone times are already stored per run (plan 017)
- **Two Claude instances working at once** — run a second coding session in a git worktree (own folder and branch, merged back later) with its own Unity Editor open (own `Library/`, slow first import, own MCP connection via `set_active_instance`); until then, pair one coding session with a docs/design/read-only one in the same folder, off each other's files (`game_text.txt`, BACKLOG files and `decisions-log.md` are the collision points) · — · M · the user, 2026-09-30; ask before creating the branch

## Ideas
*Not yet thought through; most need a design decision first.*

- **Meta currency buys starting skill levels** — a flat +n starting level for a skill, bought between runs; a candidate answer to GDD §6 [OPEN] *How deep content stays startable*; rejected as a stat job (decisions log 2026-09-30 *Stats pass*) · §6, §4
- **By-heart blocks resolve instantly past ~10×** — GDD §13a's one-line summary ("The dark: 10 searches, 4 wisps, 38 s") instead of flickering bars · §13a · M · left out of plan 025 (2026-09-30); only matters if the room-speed cap goes above ~10×

- **Searches kept for good, not every run** — playtest found re-searching each run unintuitive; keep a switch so it can go back if later verbs shift rooms again · §9 · M · open: what a search gives each step, once-per-run finds (the satchel), the hall-shifts story; save format change (decisions log 2026-09-28 *The room popover*)
- **Pools at the start** — all seven full from run 1; the v0.5 draft proposes taking them in the prologue (see Later) · §5 · ? · not yet ruled on
- **What "the gem full" means** — placeholder is every pool's max added up · §5 · ?
- **Blocked actions: drop or wait?** — dropped with a reason now · §13 · ?
- **One-of-a-kind "push out" rule** — placeholder rule · — · ?
- **The mana stone's warmth** — halves a cost the pools pay, so no felt effect · §12 · ?
- **"Plan fulfilled" run ending** — a third way to end a run · §12 · ? · new in the v0.5 draft
- **Run endings have weight** — don't over-soften collapse · §12 *Narrativising the collapse* · ? · a tone check on any ending work
- **Let the player choose where items go** — with full pockets, what she makes or picks up pushes out the most-held item; an option (keep, push out, floor) may be wanted if playtesting shows a problem · §9 · M · playtest batch 2026-09-28
- **Do an action N times** — Idle Loops style: a queued entry with a count the player steps up and down (− N +), then the queue moves on; Act I verbs are either "once until complete" or "loop until full", but new verbs soon need a set count · §13 · M · the user, planning ui-024a, 2026-09-30; the planning screen mock-up draws the stepper as a placeholder; open: what counts as one go, how it sits with ways, repeats merged into by-heart blocks, and the between-runs save
- **Whole-queue forecast** — low priority; time left for the whole queue and vitality left at its end (the queue-column mock-up shows "2:41 left", "vitality left ≈ 11"); today only the running action's time is known, and repeats ("until searched") and the growing drain make it a new Core calculation, not a UI sum · §13, §13a · M · left out of plans ui-015/ui-019, 2026-09-29; open: how to show an estimate for repeats
- **Dev panel: rooms in map order, and a "reset runs" button** — the room list follows the content list, and there is no way to zero every room at once · S · plan 032b, 2026-09-30
