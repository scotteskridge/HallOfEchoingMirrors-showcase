# 006 — Route on the map

**Status:** Done (2026-09-28)
**Design:** GDD v0.4 §13a *How it works* (numbered line, token walks it) and *The route ribbon* (a revisited room shows *1, 3*); decisions-log 2026-09-27 *Main screen UI rework* (motion: the route drawing in, the token walking; no art); roadmap row 4
**Mockup:** `DesignNotes/mockups/Act I — the hall, monochrome@1x.png` (badge on the room's top-left corner, CLARA figure by the room, gold route line): read it before step 4

## Goal
The queued trips draw on the map as a gold line from room to room, with a numbered badge on each room it stops at (a room visited twice shows *1, 3*). Clara's token (a dot labelled CLARA) sits by her room and slides along the way during a trip; the walked part of the line disappears behind her, and a newly scheduled trip's line grows in.

## Out of scope
- Building the route by clicking rooms, and dragging stops on the map (backlog line added).
- Dimming rooms that aren't on the route; the queue forecast ("dry at"); visit counts and "by heart" badges.
- Ambient lines on the map (they stay in the story panel, decisions-log 2026-09-27).
- Any change to the queue rules or to `QueueStops`.

## Design assumptions
- **Numbers count from where she is now**, as the drawer's stop cards do: stop 1 is her room, so badges shift down as she moves. Between runs, the route is the next run's queue from the start room, and the token sits there.
- A trip she can't make starts no stop (the existing `QueueStops` rule), so it draws nothing.
- A leg is a straight line between room centres, like the ways. A leg walked both ways draws once; the badges carry the order.
- The token's position comes from the running trip's progress; it is eased on screen so it doesn't step with each tick. This is display only; gameplay timing is untouched.
- Draw-in (~0.3 s), the ease, colours and sizes go in the Map Style asset: they're look, not balance, so no Balance Sheet column.

## Reuse
- `Simulation.QueueStops` / `QueueStop` (`Core/Simulation.Route.cs`, `Core/QueueStop.cs`): the stops and their order
- `Loop.CurrentDestination`, `Loop.CurrentTaskProgress` (`Core/LoopState.cs`): where the token is during a trip
- `MapLines.Place` (`UI/MapLines.cs`): draws each leg as a stretched Image
- `MapView` (`UI/MapView.cs`): room positions (`PositionOf`, made public), zoom/pan of the layers (`FollowHere`), `RoomRect`
- `MapStyle` (`UI/MapStyle.cs`, `Assets/Data/UI/MapStyle.asset`), `UiStyle`, `TemplateList`, `UiText`/`game_text.txt`
- `GreyboxSetup` + `EditorUiFactory` for the setup step; `MapLinesTests` as the pattern for testing UI maths

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `UI/MapRoute.cs` | New | Draws legs, badges and the token each refresh; draw-in and shrink motion. MapView is already 326 lines, so this is its own component |
| `UI/RouteLayout.cs` | New | Pure static maths: legs from stops (one per trip, duplicates merged), badge labels per room ("1, 3"), a leg trimmed to a fraction, the token's point |
| `UI/MapView.cs` | Edit | `PositionOf` public; moves and zooms the route and token layers with the others |
| `UI/MapStyle.cs` | Edit | Route: colour, thickness, draw-in seconds; badge colour; token colour, size, ease |
| `game_text.txt` | Edit | new keys: `map.clara_token` ("CLARA"), `map.token_tip`, `map.badge_tip` |
| `Editor/GreyboxSetup.cs` | Edit | Step 81: route layer, leg/badge/token templates, wire `MapRoute` |

Save format change? No.

## Steps
1. Failing EditMode tests for `RouteLayout` (below).
2. Implement `RouteLayout` until they pass.
3. `MapStyle` fields and `MapView` changes (public `PositionOf`, layers follow zoom/pan).
4. `MapRoute`: static route (legs + badges) from `QueueStops`, with tooltips.
5. Token: rest spot by her room's corner, slides along the leg during a trip, eased; line shrinks behind it.
6. Draw-in for newly added legs.
7. Setup step: *Hall of Echoing Mirrors → Setup → Step 81 Route on the map*.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `RouteLayoutTests.NoTrips_NoLegs_OneBadge` | an empty queue draws no line; her room shows "1" |
| `RouteLayoutTests.EachTrip_OneLeg_InOrder` | stops A→B→C give legs A–B, B–C |
| `RouteLayoutTests.RoomVisitedTwice_BadgeListsBoth` | A→B→A gives A the label "1, 3" |
| `RouteLayoutTests.SameWayBothWays_OneLeg` | A→B→A draws the A–B leg once |
| `RouteLayoutTests.Trim_HalfWay_StartsAtMidpoint` | the walked part of a leg is cut off |
| `RouteLayoutTests.TokenPoint_FollowsProgress` | the token sits at the right fraction of the leg, and at the room's rest spot when not travelling |

## Done when
- [x] Tests above pass (and the existing suite); compile and Console clean
- [x] In Unity: run Step 81; play; schedule trips from the popovers → gold legs grow in, badges show the order (a return shows "1, 3"); on a trip the CLARA dot slides along the way and the line shrinks behind it; between runs the route starts at the start room; zoom and pan keep it all in place
- [x] Screenshot of the map in play compared side by side with the mockup; differences listed for the user (look only; proportions wait for the UI layout pass)
- [x] Roadmap and BACKLOG updated; `ui.md` rules line proposed for `MapRoute`

## Notes after implementation
<!-- filled in at wrap-up: what changed from the plan and why -->
- **No screenshot comparison.** Unity's screen capture returned blank images during the session, so the route was checked by reading positions in Play (legs, badges "1, 3" / "2", the token mid-trip, the layers following pan and zoom). The side-by-side with the mockup is still to do by eye.
- **Badges show only when a trip is queued** (the user's call after the first build): `NoTrips_NoLegs_OneBadge` became `NoTrips_NoLegs_NoBadges`, plus `OneTrip_BothRoomsBadged`.
- **Badges swell briefly when their numbers change** (the user's request): `Glow.Flash`, no tint.
- **The token walks alongside the line, offset by its resting spot**, rather than on the line: it rests by the room's corner, so walking the line itself would make it jump at each end. The line is cut where the token is (its eased position projected onto the leg), so they never drift apart. `RouteLayout.FractionAlong` was added for that.
- **Badge tests check the numbers, not the text "1, 3"**: the comma comes from `common.list` in the game text, which the tests don't load.
- **MapView split** (the user's request): pan, zoom and following Clara moved into `MapWindow` (a plain class MapView owns), taking MapView from 389 to ~300 lines; `MapView.AddLayer` passes layers to it, and `MapZoomTests` now call `MapWindow.ZoomAfterScroll`. `MapRoute` adds its layers in `Start`, since MapView makes its window in its own `Awake`.
- **Two tooltips for badges**: `map.badge_tip` (one stop) and `map.badge_tip_many` ("Stops 1, 3"), after review.
- **Review polish applied:** badge labels cached once per copy (not looked up every frame), each leg and its draw-in start kept in one list, `RouteLayout.SameWay` for the either-direction test.
- **Left for later (backlog):** the route layers live on the scene's MapPanel instance, not in the prefab; a badge covers a corner of its room for clicks; a leg walked there and back is trimmed on the way out.
