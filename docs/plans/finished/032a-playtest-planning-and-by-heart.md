# 032a — Playtest batch: planning unlock, by-heart fixes, the dark way

**Status:** Done 2026-09-30 (the user's playtest passed; the look check passed, glow alpha 16; step 101 retired)
**Left to do:** nothing.
**Design:** GDD §12a *Mid-run editing* (Direction: the planning screen automates mastered content; unmastered content is played live), §13a; decisions-log 2026-09-30 *Feed your hours unlocks room speed*, *Planning in a room not known by heart is allowed* (reversed here), *The planning screen is built (ui-024c)*. The user's answers, 2026-09-30 (this session).
**Followed by:** `032b-saved-report-and-dev-runs.md` (same folder).

## Goal
Planning is earned by *Feed your hours to the flames*. From then on it plans only what she knows by heart, so every plan carries over. The start room can finally become known by heart. Rooms known by heart glow a soft gold instead of the violet dotted ring. The Dark Corridor shows on the map as soon as the Left Corridor is searched, and a trip there says it's too dark until the candles are lit.

## Out of scope
- The queue during a run: it stays free, and its won't-carry marks stay as built.
- Other hidden or shut ways: only the Left Corridor → Hanging Mirrors way uses the new option.
- Resizing the planning screen (UI-BACKLOG Now), warnings on map tags, and the rest of the planning-screen tidy.
- Saving the last run's report, and the dev control for runs worked: both are plan 032b.

## Design assumptions
- **Planning unlock:** a new item flag `unlocksPlanning` on Quickened Hours, next to `unlocksRoomSpeed` (same item, separate flag, so they can be split later). Saves where she already holds it keep planning.
- **Before the unlock:** the Summary's Plan button is hidden. Continue/Load between runs starts the next run straight away, as New Game does (the user's choice).
- **Announcement:** one feed line when the flag is first gained, key `feed.planning_unlocked`: "The hours burn clear: you can plan the next run before you begin it." (wording is a placeholder).
- **Start room:** its runs worked equals the number of runs finished (the user chose "counts every run"). It's worked out when read, so existing saves catch up at once and no save change is needed.
- **Planning refusal (between runs only):** an action, or a trip, is refused unless every room on the walk is known by heart: where she'd start, each room she passes through, and the room she ends in. That's the same test the carry uses. Reason key `reasons.not_by_heart_plan`: "she doesn't know {room} by heart yet ({runs} of {needed} runs); do it during a run".
- **A map click that can't add a trip says why** (toast via `ActionRefused`), both between runs and during a run. Before this change such a click only opened the popover silently.
- **The shut-but-shown way:** new `Way.showWhileShut` (bool) and `Way.shutMessage` (English text on the asset, following the `displayName` pattern). Found still means "Left Corridor 100% searched". While the way is shut, its line is drawn in a dim new colour `MapStyle.shutWay` and the room keeps the sealed colour. Travel is refused with the message "too dark to go in: the corridor's candles might show the way".
- **Placeholder rule:** the glow's look (a generated soft rounded glow, alpha, margin) and the gold colours, and the `shutWay` colour.
- **The look check (added by the user):** if the glow and the gilt route line or badges blur together, tune `MapStyle` (`byHeartGlowColour`, `route`, `badge`), not code. This is a check by eye, done together with the user.

## Reuse
- `Simulation.RoomSpeedUnlocked` (`Simulation.Unlocks.cs:110`): pattern for `PlanningUnlocked`. Share one "holds an item with flag X" helper.
- `Simulation.WhereScheduleLands` / `ScheduleTarget.Cant(reason)` (`Simulation.Tasks.cs:113`): the popover's Schedule/Carry greying and tooltips already read its reason.
- `Simulation.RunsWorkedIn`, `IsKnownByHeart`, `CountRoomsWorked`, `CarryPlanToNextRun` (`Simulation.ByHeart.cs`), and `Simulation.StartNode`.
- `CantUseWay` / `RoomsOnMap` / `KnownDestinationsFrom` (`Simulation.Places.cs:147, 269, 288`). `CantUseWay` already feeds `PlanWarnings`, so the plan warns with the same message for free.
- `ActionRefused` + `NoticeToast` for the click refusal. `ItemSections` / `BalanceSheetWindow` columns (`unlocksRoomSpeed` at l.750, the Ways table at l.121).
- The glow texture: `WritePng` / radial falloff from `git show a287e6d:Assets/Editor/LookPassSetup.cs`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `ResourceDefinition.cs` | Edit | `unlocksPlanning` bool (tooltip) |
| `Simulation.Unlocks.cs` | Edit | `PlanningUnlocked`; an event when it first turns true |
| `Simulation.ByHeart.cs` | Edit | the start room's `RunsWorkedIn` = runs finished; `WhyNotPlannable(walk)` |
| `Simulation.Tasks.cs`, `Simulation.Places.cs` | Edit | between runs, `WhereScheduleLands`, `CanScheduleTripTo` and `Enqueue` refuse rooms not known by heart; `WhyCantScheduleTripTo`; the shut-shown way on the map and in `CantUseWay` |
| `NodeDefinition.cs` (`Way`) | Edit | `showWhileShut`, `shutMessage` |
| `RunResultsPanel.cs`, `MainMenu.cs` / `ScreenManager.ShowGame` | Edit | Plan button only once unlocked; Load before the unlock begins the run |
| `FeedNotes.cs` | Edit | the unlock line |
| `MapView.cs`, `MapRoom.cs`, `MapStyle.cs`, `UiStyle.cs` | Edit | shut line colour; click refusal; ring → glow (`byHeartGlow` sprite, colour, margin from `MapStyle`; `[FormerlySerializedAs]` on renamed fields); violet → gold (`MapByHeartTag`, `ByHeart` hex) |
| `BalanceSheetWindow.cs`, `ItemSections.cs` | Edit | "Plan" item column; "Shown shut" way column |
| `game_text.txt` | Edit | `feed.planning_unlocked`, `reasons.not_by_heart_plan` |
| `GreyboxSetup.cs` | Edit | **Step 101: Planning playtest fixes**: makes `Assets/Art/Map/ByHeartGlow.png`, assigns it and the gold colour in `MapStyle` (the colour only if it's still the old violet), ticks `unlocksPlanning` on Quickened Hours, and sets the corridor way's `showWhileShut` + `shutMessage` (only if empty). Deletes `ByHeartRing.png` (it can be recovered from git). |

Save format change? **No.** The new fields are content only, and the start room's count is worked out when read.

## Steps
1. **Bug first:** failing `ByHeartTests` for the start room (known by heart after 4 runs with no work there; a plan's first trip out of it carries), then the fix.
2. `unlocksPlanning` + `PlanningUnlocked` + unlock event, with tests; Balance Sheet column.
3. Between-runs refusal in Core (actions, trips, walks through rooms not known by heart), with tests; `WhyCantScheduleTripTo`.
4. `Way.showWhileShut` / `shutMessage` in Core: on the map when found and shut, refused with its message, same warning in `PlanWarnings`; tests; Ways column.
5. UI: Plan button, Load-before-unlock, feed line, map click toast, shut line colour.
6. The glow and gold: `MapRoom`/`MapStyle`/`UiStyle` changes, and setup Step 101 (also sets the content fields). Run Step 101; content test in `TutorialContentTests`; full EditMode run.
7. The look check with the user: glow against the route line and badges; tune `MapStyle` if needed.
8. Docs: decisions-log entry (reverses the 2026-09-30 "don't block" line and gives the reason: Plan now arrives after Feed, when rooms are known); `PROJECT_NOTES.md` placeholder list; drop "ring margin 7" from the UI-BACKLOG tidy item.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `ByHeartTests.StartRoom_KnownByHeart_AfterEnoughRuns_WithoutWork` | the bug: the start room counts every run |
| `ByHeartTests.CarryPlan_CarriesTripOutOfStartRoom` | plans carry again |
| `RewardTests.PlanningUnlocked_ByItemFlag_KeptThroughSave` | the unlock and its save |
| `ScheduleTargetTests.BetweenRuns_RoomNotByHeart_Refused` / `_ByHeart_Allowed` | action refusal and reason |
| `ScheduleTargetTests.BetweenRuns_TripThroughRoomNotByHeart_Refused` | walks are checked, not only the end |
| `ScheduleTargetTests.DuringRun_RoomNotByHeart_Allowed` | the run's queue stays free |
| `PlacesTests.ShutWayShownWhileShut_OnMap_RefusedWithMessage` | map + refusal text |
| `PlacesTests.ShutWayShown_NotOnMapUntilFound` | hidden until searched |
| `PlanWarningTests.ShutShownWay_WarnsWithItsMessage` | the plan says "too dark" |
| `TutorialContentTests.CorridorWay_ShowsWhileShut` / `Quickened_UnlocksPlanning` | content is set |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity, new game: no Plan button until Feed is done; the feed line appears when it finishes; Plan appears on the next Summary
- [x] Planning screen: a room not known by heart can't be scheduled (greyed, with the tooltip reason; a map click gives a toast); a plan made there carries in full
- [x] The Smoky Mirror shows "Known by heart" after 4 runs; by-heart rooms glow soft gold; no violet left
- [x] After the Left Corridor is fully searched, the Dark Corridor appears with a dim line; a trip there toasts "too dark to go in…" until 10 candles are lit
- [x] The glow reads apart from the route line and badges (user's eye)
- [x] decisions-log updated; placeholders listed in `PROJECT_NOTES.md`

## Notes after implementation
- The between-runs refusal applies only once `PlanningUnlocked` (`WhyNotPlannable`): before it there is no planning, and many older tests plan freely between runs.
- The start room's count is `max(runs finished, stored count)` so pre-history saves keep what they had; run end reads the stored count when adding.
- Added `Simulation.TryScheduleTrip` (the map click) and `WhyCantScheduleTripTo`; `AddIn` (Schedule in a room) now fires `ActionRefused` with the reason instead of returning silently.
- `ByHeartTests.TripsAndSupply_DontCountAsWork` and `Stop_InARoomOneRunShort_...` updated: the start room now counts every run.
- Review fixes: step 101 now forces the glow PNG to Single sprite mode with border 16 (a new PNG imported as Multiple, so the slice never applied); a shut way's message shows from further than one room away (`ShutWayMessageInto`); the start room's "known by heart now" no longer depends on the history count hitting the threshold exactly; `DuringRun_RoomNotByHeart_Allowed` now holds planning.
- Follow-ups made after the plan: Plan tab is greyed until planning is earned; `Simulation.NextRunBeginsAtOnce`.
- Glow: 48 px 9-slice sprite (border 16), margin 16 (`MapStyle.byHeartGlowMargin`).
