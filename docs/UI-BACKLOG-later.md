# UI backlog: Later, polish and Ideas

Moved out of `docs/UI-BACKLOG.md` so that file stays short: search this one with Grep when you need it (`/plan-feature` and `/wrap-up` add new Later and Ideas lines here). Same one-line format.

## Later
*Wanted, not scheduled.*

- **Clara on screen** — a small portrait or silhouette whose reflection reacts to her vitality; the game has no face or place at the moment · §13a · L · **design question first**: Clara's reflection is tabled in `DesignNotes/VISION.md`; needs art, so commercially licensed or commissioned
- **Ghost of last run on the map** — a faint trail of last run's path and best times, so the loop is visible · §13a *Benchmarks* · M · pairs with `docs/BACKLOG.md` *Next* 5 ("Since last run" on actions), which needs a save format change
- **Story cards as illuminated pages** — the Story panel already reads as prose; borders, drop caps and page-turn feel would push the book look · §3, §13a · M · with the "UI as a book page" item below
- **Hue art direction for colour as the payoff** — the exact shade of each hue (the hue Amber a honey-orange distinct from the gilt accent and the "time" amber), what each hue's room pictures look like, and how the one-off colour flood looks · §5, §13a · M · art stage (3); decisions log 2026-10-01 *Colour as the payoff*; licensed art only, Steam AI disclosure if generated
- **Pathos as seven facets, with real art** — the design chat's critique calls the rainbow bar a test pattern and proposes seven small dark facets; plan ui-026 tried plain diamonds and the user kept the bar for now, because facets need a full art pass to work (decisions log 2026-09-30) · §13a · M · art stage; the plain-diamond code is in commit a287e6d (`PoolBars.ShowFacet`, `PoolFacets`) as a starting point
- **A milestone shows three times** — the feed's switch note, the Story card and the pop-up all mark one milestone; decide which to keep · §13a · S · design call for `/design`; parked from plan ui-034 (2026-10-01), first noted in `docs/code-health/2026-09-30.md`

## Later — polish and small items
*Moved from `docs/BACKLOG.md` on 2026-09-29; cross-references such as "Next 2" or "Next 4" in these lines mean the items now under *Next* here (Button prefabs = 2, tooltips = 3).*

- **Rework of art asset for map nodes** — *(Moved 2026-10-02 to `docs/UI-BACKLOG.md` Next 15,* Map Planner and mirror shapes*.)*
- **Real map backdrop art, lit toward Clara** — plan ui-026 put a generated dark gradient and a static centred vignette behind the map (`Backdrop` and `Vignette` images, `MapStyle.backdropTint`/`vignetteStrength`); swap in painted mirror-hall art and let the light gather where Clara is, so the eye goes to her · §13a · M · art stage; must be commercially licensed (Steam AI disclosure if generated); an animated version (slow drift, faint candlelight) was considered but risks distracting from the rooms and route lines
- **Vignette that closes in as vitality drops** — the static vignette (plan ui-026) creeps inward and darkens as her vitality falls, so the drain is felt at the edges of the screen, not only read off a bar · §13a · S–M · raised 2026-09-30 by the user; plan ui-026 leaves `vignetteStrength` as one `MapStyle` number for this to drive; presentation only, so it must not read `Time.deltaTime` for gameplay (visual-only animation is fine)
- **Map room names readable over the lines** — since plan 011 a name may reach past its frame, so the lines between rooms run through it · §13a · S · a dark underlay behind the name, or names below the frames; with the map-node art rework if soon
- **Pockets overlay draws over the room popover** — an open popover shows the pockets list on top of it · §13a · S · seen in plan 011's screenshots; sibling order in the map
- **Tip table headings look unevenly spaced** — headings are fixed-width like the numbers, so "Level" reads "Leve l" in Inter · §13a · S · with the tooltip redesign (Next 3 here)
- **Pockets overlay follows the stats row** — its start is a fixed height, so a third line of chips (more skills) will cover it again · — · S · playtest 2026-09-29
- **`StatsRow` keeps one list of chip parts** — labels, icons and bars are three lists kept in step by index · — · S · reviewer note, 2026-09-29; `/refactor`
- **Resolve “by heart” rooms in one summary line past ~10×** — fast sequences are unreadable and flickering bars look broken · §13a · S · needs realm mastery first; from the 2026-09-27 mockup
- **Run sparkline** — shows progress across runs at a glance · §13a *Benchmarks* · S · run history recorded since plan 001; several stats
- **Journal screen and Roland's scraps** — story beyond pop-ups · §3 · M
- **Supplied actions counted in the run report** — they're marked in the queue but not counted · §13a · S
- **UI layout pass** — the user's (e.g. the Story box's size, plan 002's proportions; when the stats chips wrap onto a second line they overlap the top of the pockets overlay) · — · S · Part 1 is built, so it can start (decisions log 2026-09-28)
- **Milestone cards size to their paragraph** — each card is a fixed height, so a long opening paragraph could be clipped · §3 · S · from the plan 009 review; check in play first
- **Stats chips are still Buttons** — they tint on hover but a click does nothing since plan 009; a plain Image would say what they are · — · S · clean-up; `StatsRow._chipTemplate` is typed Button, so change the field and the scene together
- **The UI as a book page, drawer tabs as bookmark ribbons** — the user's intended feel; which edge the ribbons pull from is for playtesting (the drawer's side is an Inspector setting from plan 002) · §13a · ? · art stage for the look
- **Room popover: chips wrap onto a second line** — many kinds of item on a floor, or many ways on, overflow the card · §13a · S · from plan 003
- **Room popover: less work per frame** — rebuild offers and ways on MapView's refresh, not every frame; reason strings allocated each frame · — · S · reviewer, plan 003
- **Queue drawer: row buttons cramped in a stop card** — the QueueRow prefab was laid out for the full-width list, so To top and Remove are ~38px wide in a 360px card · §13a · S · with the UI layout pass; card width is StopCard's LayoutElement
- **Circled stop numbers (①②) as in the mockup** — no font in the project has them, so the cards show plain numbers · §13a · S · needs a font with the glyphs (a licensed asset); art stage
- **Close the drawer with Escape or a click on the map** — quicker than finding its tab again · §13a · S · New Input System; from plan 002
- **Keyboard keys for the speed tiers (1, 2, 3)** — speed changes without the mouse · §13a · S · New Input System; from plan 002
- **Room popover: colour its floor chips to match the map's dots** — one colour language for item kinds · §13a · S · out of scope in plan 007
- **Floor dots: keep each dot on its own copy** — dots share one list across rooms, so a change in an earlier room shifts a swell or hover onto the neighbouring dot for a moment · — · S · reviewer, plan 007; rare and cosmetic
- **Pathos dots on the map in the item's hue** — the GDD's third pip colour; plan 007 has only Restorative, Light, Tool, Keepsake · §13a · S · needs pathos items (and a hue on the item)
- **Build the route on the map: click rooms in order, drag stops** — the GDD's quick routing; today trips come from the room popover · §13a *How it works* · M · left out of plan 006 (2026-09-28); judge after playtesting whether the popover is enough
- **Move the route layers into the MapPanel prefab** — Route and RouteMarks were added to the scene's MapPanel instance, so reverting that instance would delete them and Prefab Mode doesn't show them · — · S · *Overrides → Apply* on the instance after Step 81 is committed; the user's call (it edits the prefab); from plan 006
- **Route badges cover a corner of their room** — a click on the badge doesn't reach the room (it closes the popover instead); the badge must stay clickable for its tooltip · §13a · S · move it just outside the corner (`MapRoute.PlaceBadges`) if playtesting finds it annoying; reviewer, plan 006
- **Route line: a leg walked there and back is trimmed on the way out** — one line is drawn for both directions, so the part walked disappears though she'll walk it again · §13a · S · draw the two directions side by side if playtesting finds it confusing; from plan 006
- **Queue drawer: drag the cards or rows to scroll them** — since plan 005 a click-drag on a card or row reorders, so the lists scroll only by wheel, scrollbar or dragging near an edge · §13a · S · only if playtesting misses it
- **Split RoomPopover** — 315 lines (its dragging moved out into DragToMove in plan 008); its floor or ways sections could be their own parts · — · S · clean-up; reviewer
- **Map zoom may not zoom towards the pointer** — `MapWindow.Scroll` uses `ScreenPointToLocalPointInRectangle`, which fails on the map (MapPanel sits at Z −191 on the overlay canvas; the popover drag broke the same way), so it likely zooms on the centre · §13a · S · check `QueueDragController` and `ToolTipPanel` too
- **Put down from the pockets overlay** — queue a Put down without finding the room's popover (e.g. right-click a chip) · §13a · S · left out of plan 008; only if playtesting misses it
- **Pockets overlay: switch it off** — it can be dragged and double-clicked back, but not hidden · §13a · S · from plan 008; judge after playtesting
- **Tidy PopoverPlacement** — the room-`Rect` `LowerLeftAt`/`TopLeftFromRoom` and the keep-wholly-inside `LowerLeftAt(origin…)` are now used only by their tests (dragging uses `KeepGrabbable`); point the tests at what the game uses, or remove both (removing tests needs the user's OK) · — · S · reviewer, plan 008
- **Chip parts by name** — `PocketsOverlay` finds each chip's Label and Edge with `transform.Find`, which breaks quietly if a child is renamed; a small serialized parts component would be sturdier · — · S · reviewer, plan 008
- **Text size setting** — players (and Steam Deck at 1280×800) need bigger text; plan 011's `UiFonts` sizes per role make a global scale easy · — · S · shipping need; a settings-screen item
- **Story box lift is instant, the map slides** — when the queue folds or unfolds, the Story box's bottom edge jumps by the strip's height while the map eases over; drive it from the drawer's openness · §13a · S · from plan ui-019, 2026-09-29
- **Queue column: a whole-queue forecast** — the mock-up's "9 actions · 2:41 left" and "vitality left ≈ 11" line; needs a Core forecast, so a design check first · §13a · M · left out of plan ui-019
- **Q folds the queue mid-drag** — pressing Q while dragging a row or stop slides the column away and the drop fails silently; ignore Q while `QueueDragController.Dragged` is set · §13a · S · from the plan ui-019 review
- **`/writing` check on the queue text keys** — low priority; the wording of `popover.on_route`, `popover.adds_walk`, `reasons.no_found_way` and the `queue.fold`/`unfold` keys was drafted in plans, not by a writing session · §13a · S · from planning ui-015/ui-019, 2026-09-29; can run before or after they're built
- **Show the next escalating charge** (partly built 2026-09-30: the tooltip shows the current charge and "costs N% more each go", queued trips show their projected charge; still to do: "next: X" in the tooltip and the projected charge on the room popover's way rows) — tooltips show "Costs: 1.2 vitality (next: 1.3)" for travel and training verbs, so a rising cost reads as a decision, not a surprise · formulas doc §10 *Show the next cost* · S · plan 030b, 2026-09-30

## Ideas
*Not yet thought through.*

- **A line of Clara's voice under the Summary headline, one per ending** — the mock-up `docs/mockups/summary/` has a placeholder quote; needs the user's prose · — · S · from planning 016, 2026-09-29

- **Animated backdrop** — slow drift, faint candlelight, glints on the mirrors · §13a · ? · already weighed below (the vignette item): risks distracting from rooms and route lines
- **Reflections in the nodes** — each mirror node shows a hint of the room it leads to · §13a · ? · needs the per-node art rework first
- **Drag mirror nodes to lay out the map** — the player arranges the rooms as they like, on the main screen and the planning screen (they share the map) · §13a · M · the user, planning ui-024a, 2026-09-30; open: saved per room (a save format change), a "reset layout" button, how corridors and new rooms added at runtime place themselves; clashes with click-drag panning and the popover's drag (`DragToMove`)
- **Planning screen candles: real art and a lit backdrop** — the candles on the planning screen (ui-024c) use an AI-generated sprite (xAI licence checked by the user 2026-09-30: fine with Steam's AI disclosure) and a procedural UI light shader; at the art pass, consider a real lit backdrop instead (a world-space backdrop sprite lit by Light2D candles, drawn by a second camera into a RenderTexture shown behind the map, with normal maps so the light rakes across the mirrors) · §13a · S–M · ui-024a/ui-024c, 2026-09-30
- **Kind prefab guards and polish** — `ChipTemplatesAreKinds` checks only the scene (no UI prefab holds a chip today); the retired *Convert to Kinds* reverted whole label texts, so check no wrapping/margin override was lost · — · S · plan ui-035 review, 2026-10-01
- **Fresh reference screenshots of the main screen** — the kind-prefab change (ui-035) went in with a property-dump comparison and no before/after shots; take a dated set at 1920×1080 (main screen, popover, queue column, planning screen, menu, Summary) into `docs/mockups/main-screen/` and index them in `docs/mockups/README.md` · — · S · plan ui-035, 2026-10-01
