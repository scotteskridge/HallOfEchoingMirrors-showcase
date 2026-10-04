# UI backlog roadmap: clearing `docs/UI-BACKLOG.md` in batches

**Status:** Approved (order and grouping, 2026-10-01)
**Left to do:** plans still to write, in order: ui-036b, ui-044, ui-037, ui-038, ui-039, ui-054 (ui-033, 034, 035 done; ui-036a done 2026-10-02; ui-053 done 2026-10-02). Design passes done for ui-036 (tooltips) and ui-038 (room speed); Next 6 (colour) designed and parked (below). Each later plan gets its own `/plan-feature` session, in this order, so it can use what the earlier ones taught.

One plan per batch, each ≤8 steps. Item numbers are `docs/UI-BACKLOG.md`'s (*Now* items by position, *Next* by number).

| Order | Plan | Backlog items | Size | Before starting |
| --- | --- | --- | --- | --- |
| 1 | `finished/ui-033-planning-screen-finish` | Now 1 (resize, `MapRoom` numbers), Now 2 (warnings on map tags, folded-block tip), Now 3's anchoring and play check, + the map in the warm palette (from Next 6's design pass) | S | — |
| 2 | `finished/ui-034` code health and queue fixes | Now 4 (split `RoomPopover`/`MapView`, a refused trip names its room, "Read more" follows a text reload, per-frame text) + Next 9 (stale clicks, `StopCard.Refresh` strings, "2 of 5") | M | Mostly unseen by the player; do it before ui-035 so the prefabs land on tidier code |
| 3 | `finished/ui-035` button and chip prefabs | Next 2 | M | Done 2026-10-01 |
| 4 | `ui-036a` tooltip shape, `ui-036b` the six states | Next 3 | L | Design pass done (decisions log 2026-10-01 *Tooltips: the open points settled*); see **Tooltip design pass** below. 036a done 2026-10-02 (`finished/`); 036b also takes 036a's follow-ups in UI-BACKLOG *Now* (the first-tip delay, 0.1 s in the scene and likely too fast; trip charge's own meaning, long names wrapping in the compact form, item needs met/unmet, a repeat count in the footer) |
| 4b | `ui-044-since-last-run-display` (its Core half is plan 042, Features lane; see `backlog-roadmap.md`) | `docs/BACKLOG.md` Next 5 | S–M | After 042 and ui-036; fills the place ui-036 leaves in the full form |
| 5 | `ui-037` star bar and juice | Next 4, Next 5, Next 13 | M+M (may split) | Next 13 (hide the stats while all are asleep, `Simulation.IsAwake`; orphaned keys `tips.xp_at_max`, `tips.mastery_at_max`) is folded in here because this plan reworks that strip. In Act I there's no stats row (decisions log 2026-09-30), so the "quieter secondary strip" is the skills only; juice reuses `Glow.Flash` |
| 6 | `ui-038` room speed visible | Next 8 | M | After ui-036 (tooltip shape) and ui-037 (the glow); design pass done (decisions log 2026-10-01 *Room speed made visible*); see **Room speed design pass** below |
| 7 | `ui-039` Summary part C | Next 12 | M | The user wants it (2026-10-01). Its `/plan-feature` session interviews the user: which numbers the sparkline tracks (Act I has no stats: run length, vitality lost, milestones reached, mastery?), how many runs it spans, and what the layout pass changes (read `docs/mockups/summary/` and the UI context in `UI-BACKLOG.md`). Room speed's "Faster next run" line (ui-038) lands first, so lay out around it. Also takes ui-051's follow-ups in UI-BACKLOG *Now* (reword a go cut short, wrap or fold a long charge line, a public way to build a `RunReport` in tests) |
| 8 | `ui-053-map-planner` | Next 15, first half | M | Done 2026-10-02 (`finished/ui-053-map-planner.md`) |
| 9 | `ui-054` mirror shapes | Next 15, second half | M | After ui-053. Settled 2026-10-02: plain geometric shapes as the stage-1 placeholder, one scale number per room. **Still [OPEN]: which shape goes with which kind of room**; settle it with the user before or at the start of its `/plan-feature` session (decisions log 2026-10-02 *Mirror shapes carry meaning*) |

## Tooltip design pass (2026-10-01, for ui-036)
Source: `docs/mockups/tooltips/` (the six-state mock-up and its notes). Decided with the user; logged in the decisions log.

| Point | Settled as |
| --- | --- |
| Shape | One layout: kicker (verb class · place), serif title, fact rows (Takes, Costs, Trains, Faster with, Gives, Bleeds, Opens), actual number large and base small, a footer strip for state (repeat rule, "0 / 4", bar). Fixed 340 px wide, 260 compact |
| Compact form | The common verbs (Pick up, Put down, other one-per-item verbs). Room actions, searches and trips get the full form |
| State 3, run-ender | Red when vitality now < the action's estimated cost (drain over its time + charges); "likely to end the run", "you have N". Warning only, never a refusal |
| State 4, skill | Level, XP, Speed table; "This run" and "Mastery KEPT" rows; one rule line. Skills only in Act I (no stats) |
| State 5, item | Rows Bleeds, Trains, Opens; one line in Clara's voice as a `[PLACEHOLDER]` key (the user's prose, for `/writing`) |
| State 6, locked | Only actions already listed but missing something (item, skill level, hue): every condition, ticked when met, the unmet one says what to do. Hidden actions stay hidden |
| "Since last run" | Not in ui-036; leave a place for it in the full form (plan 4b fills it) |
| Fonts and colours | EB Garamond titles, Inter body (plan 011). Times amber, vitality rose, met/kept green, unmet orange, through `UiColours` roles, not inline colours; each also carries a second cue (tick or cross, icon, word) for colour-blind players (decisions log 2026-10-01 *Colour as the payoff*) |
| Timing | 8 px gap, flip near the edge, never cover what it describes; 350 ms before the first tip, then 0 ms between neighbours |

**Before starting ui-036 and ui-037 (plan ui-035):** restyle buttons and chips through the kind prefabs in `Assets/Prefabs/UI/Kinds/` (Button, AccentButton, IconButton, TabButton, SmallButton, Chip, ChipFaint), not per object; new buttons and chips come from `EditorUiFactory.MakeButton`/`MakeChip`, which instantiate the kinds.

**For the ui-036 planner (Claude's calls, not design):** split 036a (the panel's new shape, compact form, timing, colour roles; content still built by `ActionText`, `ClaraTips`, `ItemTips`, `TipTable`) from 036b (the six states' content). Check whether rich text in one TMP box can do the layout, or the panel needs real rows. Add the meaning colours as `UiColours` roles in 036a, including a **"met" green** (the code has none today: only `SearchedBar` #6E8F62, used for the popover's searched bar, and the rich-text `Warning` orange in `UiStyle`); keep them clear of the hue colours (decisions log 2026-10-01 *Colour as the payoff*).

## Room speed design pass (2026-10-01, for ui-038)
Decided with the user; logged in the decisions log. Every cue stays hidden until `RoomSpeedUnlocked` (Feed your hours). Wording says "faster", never "cheaper" or "easier".

| Point | Settled as |
| --- | --- |
| Header readout | Beside the run clock: "×3.4" while sped up, "held" while held, nothing at ×1. Tier × `RoomSpeedNow`, never the dev panel's speed. Tier buttons stay hidden |
| Map badge | Keeps the room's own speed; its tooltip adds the planning screen's "×now here · ×next after 1 more run" line. No ramp bar |
| Queue column open | The header covers it; no speed note on rows. Time-left stays in game seconds; its tooltip says so |
| Newly known by heart | The run-end feed line stays; at the next run's start those rooms glow briefly on the map |
| First speed-up ever | One feed note, her noticing she crosses the room without looking: a `[PLACEHOLDER]` key for `/writing` |
| Held | Readout flips to "held" with a brief flash; its tooltip names the cause (skipped, refused, milestone) and that speed returns after the next action. No feed line. **A refused click no longer holds** (decisions log 2026-10-01 *The Story box stays one panel…*): only the queue's own refusals and skips, and milestones. A small Core rule change with tests, built in ui-038 |
| Summary | "Faster next run: {rooms ×speed}", only when a room's speed rises. Feed's feed note (`feed.planning_unlocked`) also mentions room speed; Draw stays with BACKLOG Next 9 |
| Stats panel | Never (GDD §13a trap 2) |

**For the ui-038 planner (Claude's calls, not design):** Core needs the hold's reason as read-only state (`WatchRoomSpeedHold` sees the event, `SpeedHeld` only a bool); "first speed-up ever" is a once-ever flag, so check whether it needs a save format bump or can be derived; the glow needs last run's `KnownByHeartNow` at the next run's start (the Summary survives a load, plan 032b). `RoomSpeed(room)` doesn't check the unlock itself (only `RoomSpeedNow` and the UI do), so every new cue checks `RoomSpeedUnlocked`. Badge and readout tooltips use ui-036's shape and the gilt colours through `UiColours`, and must not crowd a room label or the popover (gap f).

**Not plans yet**
- Next 11 (story and ambient in one panel): settled 2026-10-01, kept as built; nothing to do.
- Next 10 (Attunement's wake moment): retargeted 2026-10-01 to the first exit and moved to `docs/BACKLOG.md` (*Act I has no stats*).

**Parked to stage 2 and 3**
- Next 6 (colour as the payoff): designed (decisions log 2026-10-01 *Colour as the payoff*). The system is parked to stage 2, with the Amber pool, because no pool is won in stage 1; art to stage 3. The only stage-1 piece, the map in the warm palette, is folded into ui-033 (step 2).
- Next 7 (Steam shots): stage 3, when the Steam page goes up with the demo; after Next 4 and 6.
