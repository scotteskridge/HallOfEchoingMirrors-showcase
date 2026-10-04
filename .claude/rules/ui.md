---
# UI scripts, UI prefabs and UI assets (MapStyle).
paths:
  - "Assets/Scripts/UI/**"
  - "Assets/Prefabs/UI/**"
  - "Assets/Data/UI/**"
---
# UI

Namespace `HallOfEchoingMirrors.UI`, uGUI + TextMeshPro (switching to UI Toolkit is a decision for build stages 2–3). UI only shows the simulation and forwards input: **no game rules here.**

## Rules
- **Never hold on to a Simulation:** loading replaces it. Look it up through `_game.Simulation` each time (lambdas for button clicks), and re-subscribe to its events on `GameController.SimulationChanged`.
- **Tooltips:** a `ToolTip` component (a text key, or live text via `ToolTip.On(component, () => text)`), shown in the one `ToolTipPanel`. Stat and skill wording is shared in `ClaraTips`.
- Labels typed into the scene read their wording through a `TextKey` component set up with the Scene Labels tool (`editor-tools.md`). New static labels get one too.
- No `Find`/`FindObjectOfType` in UI scripts: references are `[SerializeField]`. (`transform.Find` for a template's own child is fine.)
- There are no floating notices: numbers that rise glow where they're shown.

## Helpers (use them; don't write private versions)
- `TemplateList<T>` for any "one copy per item" display.
- `UiStyle` for colours not yet given a role (never inline colour codes), plus `UiStyle.Aside` and `Heading` for side notes and tip headings. `EditorUiFactory`'s `MakeText`/`MakeButton`/`MakeDisplayBar`/`MakeFrame`/`MakeChip` take a `ColourRole` so new UI is born tagged.
- `UiText` for setting text only when it changes, "nothing here yet" labels, button captions, the clock, percentages and number formats (`Number`, `Whole`, `Rate`, `Setting`, `Countdown`). Never `ToString("0.#")`.
- `ActionText` for how actions read anywhere: name, details, cost, repeat rule, the full tooltip.
- `SlideDrawer` for pages that slide in from an edge (one open at a time; the edge is an Inspector setting; bookmark tabs are optional); it can push a part aside (the map) instead of covering it.
- `RoomPopover` for everything about one room over the map; `MapRoom` is one room on the map.
- `QueueEntryText` for how a queue entry reads anywhere (progress, time left, details, tooltip, To top); `QueueRibbon` is the folded strip under the Story box, `QueueFold` folds the Queue column (Q, saved in PlayerPrefs), `QueueDrawer`/`StopCard` are the column's cards. `NestedScroll` on a list scrolling inside another.
- `QueueDragController` (on the drawer's Queue page) for dragging rows and stops: the drawer and cards find the gap, Core decides.
- `FeedNotes` for new event lines in *Around her*; `NoticeToast` (bottom right) for why an action was refused or skipped.
- `StoryFeed` is the Story box: *Around her* lines and milestone cards in one list (`StoryPanel` posts the cards; `FeedTrim` decides what fades or is dropped). `FlowLayout` for rows that wrap.

## Screens are pages of a book
- `ScreenManager` shows one page at a time: `MenuScreen`, `MainScreen`, and the Summary page (`PlanningScreen` in the scene, `ScreenId.Summary`).
- **Where pages sit in the editor doesn't matter:** `ScreenManager` positions them when Play starts.
- **New pages:** add to the **end** of the `ScreenId` enum, give them a CanvasGroup, add them to the Screen Manager list.
- Full-screen overlays (the story pop-up, above all screens) put themselves in view when shown; new ones must too.
- The transition (`ScreenManager.Turn`) is a placeholder cross-fade, to become a page turn.

## Prefabs (`Assets/Prefabs/UI/`)
- ActionRow, QueueRow, MilestoneRow, SlotRow, MapPanel, StoryPopup, ToolTip. Runtime copies are made from their scene instances: change their look by editing the prefab.
- New buttons and chips are instances of a kind in `Assets/Prefabs/UI/Kinds/` (Button, AccentButton, IconButton, TabButton, SmallButton, Chip, ChipFaint), made with `EditorUiFactory.MakeButton`/`MakeChip`. Restyle a kind by editing its prefab, not each object; a place keeps only its size, layout, words, tooltip and view scripts. *Build Kind Prefabs* never overwrites an existing kind.

## Gotchas
- **Fonts:** every text has a `FontRole`; its font and size come from `Assets/Data/UI/UiFonts` (*UI → Apply UI Fonts*). Atlases are static: new characters need *UI → Update UI Font Atlases*. Rich-text headings use `UiStyle.Heading`, which names the font (`UiStyle.HeadingFont`).
- **Colours:** every recoloured Graphic (panels, rows, buttons, bars) has a `ColourRoleTag`; its colour comes from `Assets/Data/UI/UiColours` (*UI → Apply UI Colours*). Other colours (icons, map lines, state pairs) stay in `UiStyle`.
- The `RunHeader` speed tiers stay hidden until a speed above ×1 is earned; they show the earned speeds plus the next one, locked (`SpeedTiers`). A new speed needs adding to its *Speeds* list.
- **One layout driver:** a child of a layout group never gets its own ContentSizeFitter (`LayoutDriverTests`); turn on the parent's *Control Child Size* instead. Unity saves layout-driven values as 0, so 0s in a scene diff are its normal form: don't restore them by hand.
- `MapView` (prefab `MapPanel`) is the one map; its look is the `MapStyle` asset in `Assets/Data/UI`. `BlurbTeller` only times story-feed blurbs; `BlurbPicker` (Core) chooses them.
