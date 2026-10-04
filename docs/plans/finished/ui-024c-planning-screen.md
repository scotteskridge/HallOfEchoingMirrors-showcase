# ui-024c — The planning screen

**Status:** Done (2026-09-30)
**Left to do:** nothing.
**Design:** decisions-log 2026-09-30 *The planning screen (plan ui-024a)*, *Feed your hours unlocks room speed* (+ plan 029's settled points), *Planning in a room not known by heart is allowed, but it must say it won't carry over*; GDD §13a (*rooms known by heart*, room speed); mock-up `docs/mockups/planning/` (`notes.md`, `2026-09-30-planning-screen.html`). Second of three: ui-024b → **ui-024c** → ui-024d. UI-BACKLOG *Now*; BACKLOG Next 1b.

## Goal
**Plan** on the Summary opens a planning screen of its own: the map and the queue column fill the screen, each room says how well she knows it, and one **Begin** starts the run. It replaces ui-024b's interim "main screen paused" and the header's Begin button.

## Out of scope
- ui-024d's warnings on blocks that can't start (the won't-carry mark built in ui-024b is reused as is).
- The run-ahead strip (only an empty slot is kept), the count stepper, dragging map nodes, real candle art, a Light2D lit backdrop (art pass).
- The rest of UI-BACKLOG Next 8 (room speed on the header, ribbon, Summary; first-time explanation).

## Design assumptions (user's answers, 2026-09-30, and Claude's choices)
- **User:** by-heart tags and the violet ring show on **both** maps (it is one map object). The room speed line shows **nothing** until *Feed your hours* has unlocked it (`Sim.RoomSpeedUnlocked`). The header's **Begin is removed**: Repeat and the planning screen's Begin are the only ways to start a run from between runs.
- **Claude's choice, check it:** Continue / Load of a game that is **between runs** opens the planning screen instead of starting the run at once (today `MainMenu.BeginAndShow` calls `BeginLoop`). Without the header Begin there'd otherwise be no way to look at a saved plan. One helper, `ScreenManager.ShowGame()`, picks Main (running) or Plan (between runs), so every route agrees.
- **Claude's choice:** between runs the Main tab is not usable (nowhere to start a run from there); the Plan page has no tab. No way back to the Summary (decided).
- **Tags** (under each room name): "Known by heart" · "Worked here in N runs · by heart at 4" · "Not worked yet". Rooms not known by heart get a hover tip reusing the won't-carry wording (`queue.not_carried_tip`). Known rooms get a violet dotted ring.
- **Speed line** (planning screen only, once unlocked): "×1.8 here · ×2.6 after 1 more run" from `RoomSpeed` and `LoopSettings.RoomSpeedAfter(runs + 1)`; "×5, full speed" at the cap. The main map keeps its existing ×N badge.
- **Candle light (Claude's advice, 2026-09-30; the user asked for procedural light over a looped image): a procedural light shader**, not Light2D (the canvas is Screen Space Overlay, so 2D lights can't reach UI; switching the canvas mode was judged too risky). One full-map UI Image under the map's nodes and lines, with a custom UI shader (hand-written `.shader`, no new package): per candle a warm radial falloff whose brightness, radius and centre wobble are driven by layered noise of the shader's `_Time`, blended additively, with a gentle darkening away from the candles so the flicker reads as light and swaying shadow. **Tuning in the Inspector (the user's request, 2026-09-30):** `CandleFlicker` has a *Candle light* header with tooltipped, range-limited fields: flicker speed, flicker strength (how far brightness dips), light radius, sway (how far the centre wobbles), warm colour, brightness, darkening away from the candles, and the flame's own flicker. It pushes them to the material every frame (through a runtime copy of the material, since UI Images can't use property blocks; the material asset on disk is never changed), so edits in Play mode show at once. Play-mode edits are lost on stopping, as usual in Unity: note the values and re-enter them. Candle positions come from the candle objects themselves, not code constants. Visual only: never reads the simulation. The flame sprite itself gets a small scale/alpha flicker. A real lit backdrop (Light2D into a RenderTexture, with normal maps) is parked for the art pass.
- **Candle art:** `Assets/Art/Candles/grok-image-….png` is AI-generated; the user checked the xAI licence (2026-09-30): usable as long as the Steam AI disclosure is made.
- **Popover ▶ between runs:** disabled (not hidden) with a tooltip "Nothing is running yet: plan it with +". Core `PlayIn` is unchanged.
- Naming: the Summary's scene object is confusingly called `PlanningScreen`; the setup step renames it `SummaryScreen`. The new page is `PlanScreen`, `ScreenId.Plan = 4` (new ids go at the end).

## Reuse
- `ScreenManager` (`Show`, `Turn`, `_screens`, `UpdateTabs`, `OnLoopEnded` → Summary) in `UI/ScreenManager.cs`: add the Plan screen.
- `MapView` (`ShowStates`, `RoomSpeedTip`, `AddLayer`), `QueueDrawer`/`QueueDragController`/`StopCard` (`QueueStop.CarriesOver`): **borrowed, not copied**: the page re-parents `MainScreen/Middle/Map` and `Drawer/Panel/Queue` while it shows and puts them back (parent, sibling index, anchors, offsets) when it leaves. All pages share one Canvas, so `MapView`'s `MapWindow` stays valid.
- Core: `Simulation.RunsWorkedIn`, `IsKnownByHeart`, `RoomSpeed`, `RoomSpeedUnlocked`, `LoopSettings.RoomSpeedAfter`/`byHeartRuns`, `ClearQueue()` (clears `_nextQueue` between runs), `PlannedNodeAfter`, `NextLoopNumber`, `QueueEntry.ByHeart`.
- `RunResultsPanel._planButton`, `RunHeader._beginButton`, `RoomPopover` Play button, `MainMenu.BeginAndShow`.
- `EditorUiFactory` (`MainPage`, `MakeButton`, `MakeText`), `GreyboxSetup` step pattern (last step 98; the Summary page's Step 44 and Step 97 in git history show how `_screens` was edited).
- Text keys reused: `queue.not_carried_tip`, `queue.stop_empty_planning`, `map.room_speed_tip`, `tips.begin`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Core/Simulation.ByHeart.cs` | Edit | `CarriedActionCount` (sum of counts of `ByHeart` entries in the next queue); `PlanEndsIn` (room she'd be in after the whole plan, or null if empty) |
| `UI/ScreenManager.cs` | Edit | `ScreenId.Plan`; `ShowPlan()`; `ShowGame()` (Main if running, else Plan); `event Action<ScreenId> Shown`; Main tab unusable between runs |
| `UI/PlanPage.cs` | New | borrows map and queue on `Shown(Plan)`, returns them on leaving; title, queue head (count, *Clear plan*), foot ("Ends in …", *Begin*), empty hint; spare slot; sets `MapView.ShowSpeedLines` |
| `UI/CandleFlicker.cs` | New | flame sprite flicker; feeds candle positions and colours to the light material, visual only |
| `Assets/Shaders/CandleLight.shader` + material | New | procedural candle light for UI (noise-driven falloff, additive), respects UI masking |
| `UI/MapView.cs` | Edit | by-heart tag and ring per room (both screens); speed line when `ShowSpeedLines && RoomSpeedUnlocked` |
| `UI/MapStyle.cs` | Edit | ring colour (violet), tag and speed-line colours |
| `UI/RoomPopover.cs` | Edit | ▶ not interactable between runs + tooltip |
| `UI/RunHeader.cs` | Edit | drop the Begin logic (button object kept, switched off by the setup step) |
| `UI/RunResultsPanel.cs` | Edit | Plan → `ShowPlan()` |
| `UI/MainMenu.cs` | Edit | `BeginAndShow` → `ShowGame()` without `BeginLoop` between runs |
| `Editor/GreyboxSetup.cs` | Edit | **Step 99: Planning screen** (below) |
| `game_text.txt` | Edit | `## plan` section: `plan.title` ({loop}), `plan.carried.one/many`, `plan.carried_none`, `plan.clear`, `plan.clear_tip`, `plan.ends_in`, `plan.nothing_queued`, `plan.empty_hint`, `plan.begin`; `map.known_by_heart`, `map.runs_worked.one/many`, `map.not_worked`, `map.speed_line`, `map.speed_line_next.one/many`, `map.speed_full`; `popover.play_between_runs_tip` |

New content fields: `MapStyle` colours only (style, not balance: no Balance Sheet column).
Save format change? **No** (the plan is already saved, v16).

## Steps
1. Failing EditMode tests for `CarriedActionCount` and `PlanEndsIn`, then implement (Core).
2. `ScreenManager`: `Plan` id, `ShowPlan`, `ShowGame`, `Shown` event, tab rule. Point `RunResultsPanel`, `MainMenu` at them; remove the header Begin logic.
3. `MapView`: tags, ring, speed line (+ `MapStyle` fields, text keys).
4. `PlanPage`: borrow/return, head, foot, hint, Begin (`_game.BeginLoop()`; `OnLoopStarted` already shows Main, borrow returns first). `RoomPopover` ▶ rule.
5. `CandleLight.shader` and `CandleFlicker`.
6. **Step 99** (*Hall of Echoing Mirrors → Setup → Step 99: Planning screen*, safe to run twice): rename `PlanningScreen` → `SummaryScreen`; build `PlanScreen` page (title, map slot left, queue slot right ~480 px at 1920, head, foot, hint, spare slot, two candle sprites at the map's outer edges and the candle-light Image with its material); add it to `ScreenManager._screens`; lay pages out (`PageLayout`); wire `RunResultsPanel`, `PlanPage`; switch off the header's Begin button; generate the dotted ring sprite and assign it in `MapStyle`.
7. Compile, console, all EditMode tests; editor check below. Retire nothing yet (the user runs step 99, then a later commit retires it).

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ByHeartTests.CarriedActionCount_SumsByHeartEntryCounts` | repeats merged with a count add up; player-added entries don't count |
| `ByHeartTests.CarriedActionCount_ZeroWhenNothingCarried` | empty plan and runs 1–3 read 0 |
| `ByHeartTests.PlanEndsIn_IsRoomAfterLastEntry` | a plan with trips ends in the last trip's room |
| `ByHeartTests.PlanEndsIn_NullWhenPlanEmpty` | the foot can show "Nothing queued yet" |

(No tests exist for `ScreenManager` or the pages; the rest is checked in the editor.)

## Done when
- [ ] Tests above pass; compile and Console clean; all EditMode tests pass
- [ ] Run Step 99, Ctrl+S. Play, finish a run: Summary → **Plan** slides to the planning screen: title "Plan the next run · Loop N", map left with tags on every room, queue right with "N actions carried" / *Clear plan* on top, "Ends in …" / *Begin* at the foot, two candles whose warm light flickers and sways over the map without covering room names or blocking clicks. No top bar, no Story box.
- [ ] In Play mode on the planning screen, select a candle's `CandleFlicker`: changing flicker speed, strength, radius, sway and colour under *Candle light* changes the light at once.
- [ ] *Clear plan* empties the queue at once; the hint text appears; "Ends in" reads "Nothing queued yet". Popover + adds actions; ▶ is greyed with its tooltip; drag, fold, ✕ work as on the main screen.
- [ ] Rooms not known by heart show the runs tag and the won't-carry tip; known rooms show "Known by heart" and the violet ring, on the main map too.
- [ ] Before *Feed your hours*: no speed lines. After (dev panel): each known room shows its speed line.
- [ ] **Begin** starts the run on the main screen with the map and queue back in place, laid out as before. The header has no Begin button.
- [ ] Quit to menu between runs, Continue: lands on the planning screen with the plan intact.
- [ ] decisions-log entry; PROJECT_NOTES placeholder list: candle light, speed-line wording.

## Notes after implementation
- **Text keys:** room tags reuse `popover.known_by_heart`, `popover.runs_worked.*` and `popover.not_worked` instead of new `map.*` keys; `map.speed_line_next.one/many` weren't needed (the line always says one more run); added `plan.begin_tip`, `plan.carried_tip`, `plan.ends_in_tip`; `tips.begin` and `main.begin` removed.
- **Borrowing:** `PlanPage` borrows only the MapPanel (not the whole Map frame, so stats and pockets stay on Main) and the Queue page. The candle light is moved under the map's rooms while the page shows.
- **Shader:** the flicker is computed in C# (`CandleFlicker`, Perlin noise on the real clock) not from the shader's `_Time`, so the Inspector fields drive it; `CandleLightSurface` holds the runtime material. Defaults lowered after the first look.
- **Tabs:** the Main and Summary tabs hide on the planning screen; Menu stays (Save and Quit live there). The header's Begin button was deleted, not just switched off.
- **Added:** `Simulation.RunUnderWay` (one rule for "a run is under way", used by ScreenManager, RoomPopover, RunHeader, BlurbTeller); the map jumps to Clara when the page opens.
- **Review:** fixed the `OnDestroy` hand-back, the frozen switched-off candle, missing tooltips, the Begin tip. Not fixed: the tag's size and the ring/tag offsets are typed-in numbers in `MapRoom` (should move to `MapStyle`): logged in UI-BACKLOG. The Summary can still be reached via its tab from the planning screen.
- Step 99 was run and retired (commit 81a69a8). Play-test: a short one by the user; element resizing goes to a later step.
