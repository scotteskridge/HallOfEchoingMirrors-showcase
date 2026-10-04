# Backlog

Future work, one line each: **name** — why it matters · GDD § · size (S/M/L) · notes.
UI and look work (screens, layout, look, UI polish and UI clean-up) lives in `docs/UI-BACKLOG.md`; add new UI items there. Specs go in `docs/plans/`, not here. `/plan-feature` takes items from here; `/wrap-up` removes finished ones and adds new ones.

## Now
*Being built: at most 1–2 items.*

- **Act I playtest batch (plan 055, built 2026-10-02):** waiting on the user's play check on a new save (candles relit every run, the lab's long talk with insight, "Go back in"); then move the plan to `finished/` · §12, §12a, §13a · S
- **Open question (plan 041 playtest, 2026-10-01):** after the first walk out the stats wake but barely move (the Smoky Mirror trains none; only Instantiate, Search, *Watch Him* and a trickle of Composure do). The user believes the stats belong to Act II. Should they stay woken at the first exit (as built) or earn nothing until Act II content exists, and should the Summary say so? Needs the user's answer (`/design`) · §6, §12 · S
- **Open question (plan 032b):** now the Summary survives a load, should Load before *Feed your hours* show the Summary instead of starting the run? Needs the user's answer (`/design`) · S
- **Test gap (plan 032b review):** `RunSaveTests.LastRun_KeptThroughSave` leaves `Kept`, `CarriedKept`, `CarriedLost`, `SwitchesFlipped`, `KnownByHeartNow` empty, so their restore code is untested; extend the set-up (use `DevSetRoomRuns`) and assert each is non-empty · S

- **Follow-up (balance):** Travel's escalating charge (3, ×1.08 a move) was left as it is in the 2026-10-02 pass (the probe showed it barely changes the run count); revisit if playtests still find the back-and-forth costly. Placeholders still to tune: `roomStep` 1.1, Gather 0.2, Handle 0.1 (`PROJECT_NOTES.md`) · S
- **Follow-up (plans 022/022b):** the Feed Your Hours 20 s exception means it is now twice as long relative to everything else: check in playtests.
- **Before Steam (AI art):** add the AI-content disclosure on the store page; email support@x.ai to confirm Grok Imagine output may be used in a commercial game (its Terms want "permission" and attribution for Output, see `Assets/Art/GROK-IMAGINE-LICENCE.md`) and keep the reply in `Assets/Art`; note the account type and date for each Grok image · S

- **GDD stale-and-duplicate pass (plan 040c, after ui-033):** one short session, each § edit shown for the user's OK. §16 *Build Approach* → pointers (its engine rules repeat `CLAUDE.md`, its stages `PROJECT_NOTES.md`, its *Tabled* list `VISION.md`); §17 drop or reword answered questions (Q1 UI rework done; Q3 end of queue, see decisions 2026-09-28 *the queue drives the pause*); *About this document* trimmed (the labels are explained in `.claude/rules/design-docs.md`); §13a's built UI detail (e.g. *Proposed layout*) → pointers to BUILD-STATE, `docs/UI-BACKLOG.md` and `docs/mockups/`, keeping the reasoning; §5's unlabelled numbers table under "Vitality drains with time" repeats stale `~` figures (50 max, 0.5 drain, +25% growth, 85 s): shrink to design targets, and its Endurance row should say the bank, as §6 does (040b C3). Aim: nothing said in two places (~1,000 words) · GDD · S–M

- **More balance passes, after more features are built (the user, 2026-10-02):** run `/balance` again once Act I's remaining features (and later each act) are in; the probe's route must be updated for new content first (`docs/balance-tools.md`) · — · M
- **Open question (balance pass, 2026-10-02):** the code's own defaults (`LoopSettings.drainGrowthPerMinute` 0.5625) no longer match the shipped asset (0.4), and `ShippedRulesTests` builds "the game as shipped" from those defaults (`AttributeTests.TheCodeDefaults_MatchTheAgreedNumbers` pins them). Make the defaults follow the asset, or have the shipped-rule tests read the asset? Ask the user · S
- **Clean-up (plan 055):** Understanding and Steady hands are no longer used by any task; with the orphaned Clear the bench, Take the tome and Cut the stone (and their bench/tome items), retire them to `/Remove Me/` or reuse them in the lab's story · S
- **Idea (plan 055):** a story beat when she collapses mid-talk with Roland, like *The Gem Cracks* (a `LoopEndedDuringTask` switch; needs writing) · S

## Next

*The next few, in order.*

9. **Rework "Draw on the mana stone" for room speed-up** — the task is parked unchanged as a placeholder in the lab, reserved for the Summary/Plan rework that lets the player speed up rooms she has mastered · §13a, §12 · M · raised 2026-09-29 in the lab design session (the user: high, not the top); design it with 1b and 6 and decide then what Draw does; supersedes in intent the 2026-09-25 "warmth halves every trip" and the Ideas item on the warmth
2–4. *Moved to `docs/UI-BACKLOG.md`: Button and Chip prefabs, Main screen re-composition, Better tooltips. The numbers stay so other notes that say "Next 2/3/4" still point here.*
5. *(Core half moved to* Now *as plan 042; the display half is `docs/UI-BACKLOG.md` Next 14, plan ui-044. The number is kept.)*
13. *Moved to Now (plan 031).*
15. *Moved to Now (plans 030a, 030b).*
16. **Code-quality pass: have Opus plan it (`/plan-feature`, then `/refactor`)** — a code review on 2026-09-30 found a sound codebase (655 tests, rules mostly kept) with four weak spots; the user asked to be reminded to get Opus to plan the fixes · CODE-STANDARDS · M · **do it after the stats pass is committed and before the lab (item 7) adds more code; don't mix with feature work.** Of the four, (a) and (b) were done in the 2026-09-30 code-health pass; left:
    - **(c) Still open, oversized files** (guideline ~300 lines): `Simulation.Tasks.cs` ✅ **split 2026-10-01** (693 → 218 lines; new `QueueInRoom`, `Starting`, `CanStart`, `Repeating` partials, commits 77437bf..2a45929; a pure move). **Still open:** `Simulation` as a whole is one class of ~4,800 lines over 25 partial files, which hides coupling; the files still over ~300 are Attributes, Simulation.cs, Resources, Places and Unlocks. **Planned 2026-10-01 as plan 052 (approved; four `/refactor` sessions, low priority until the Act I playtests).** **#1 `RunReportBuilder` extracted 2026-10-01** (`Simulation.Report.cs` 198 → 29 lines); **#2 `RoomSpeedHold` extracted 2026-10-01** (the speed hold's 2 fields and 5 event handlers); **#3 `PlanWarningsTracker` extracted 2026-10-02** (`Simulation.PlanWarnings.cs` 316 → 47 lines; the walk and its 8 fields); #4 `Stats` still to do. Opus's job: **plan** (don't just move lines) which cohesive pieces to extract into their own small classes the simulation calls, which are not worth it, and in what order. Small tested steps, full EditMode run after each, no behaviour change · M–L
    - **Checked in the 2026-09-30 code-health pass:** the tests assert meaningful things (outcomes the player would see; 6 `DoesNotThrow`, all justified). The UI layer (8,200 lines) was audited: see `docs/code-health/2026-09-30.md`.
    - **Why wait for Act I playtests before (c):** nothing is broken, and the design may still change; refactoring code that might be rewritten is wasted. Do (c) once the loop has been judged fun.
- **Design Act II's plot: the Amber Realm (the user, 2026-10-02)** — the plot of Act II, including naming the planned rooms laid out with the map planner (`Assets/Data/Places/New Room 1`–`6`) and making each room's display name match its asset name · §12 · M · design first (`/design`, design lane); then rename the assets in Unity (keeps each `.meta` and content `Id`, so saves are safe) and set `displayName` to match
- **Two-step container crafting (open, raised 2026-10-01 in plan 040b batch 2):** a playtest says one-step craft of the satchel/pouch isn't an interesting puzzle; try gathering a resource first, then combining it into the container; design first (`/design`), then a plan; touches the satchel and pouch tasks and the lab's earrings · §5 *Pockets* (`[OPEN]`) · M
- **The first exit's moment: her stats arrive (on screen)** — when she first walks home, the five stats appear with a glow and a story beat; was UI-BACKLOG Next 10 (light Attunement's chip as the talk wakes it), retargeted 2026-10-01 because Act I has no stats · §6, §12 · S · decisions log 2026-09-30 *Features come in layers*; build with *Act I without stats* or the first exit; whether stats arrive at zero is [PROPOSED]
10. *Superseded by item 12 (decisions log 2026-09-29): ways stay found once a room is explored.*
11. *First version folded into item 1b (decisions log 2026-09-29): Plan opens the paused main screen. A separate Planning screen only if playtests want one.*
12. *Moved to Now (plans 020 and 021).*

- **Find what keeps re-saving `SampleScene.unity` with layout noise (raised 2026-10-01, plan 052 session):** two stashes in one afternoon held only anchor/size/position changes (about 340–380 lines each); `SceneGitGuard` stops the freeze but not the noise. Look for a layout component or editor script that dirties the scene on open or on a merge; candidates are `UiTools`, `PageLayout` and `MapLayoutEditing` · S–M

*Later and Ideas: see `docs/BACKLOG-later.md` (grep it; don't read it whole).*
