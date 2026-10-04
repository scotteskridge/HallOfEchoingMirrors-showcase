# 021 — Room entry as the benchmark

**Status:** Done (tests pass; playtested by the user 2026-09-30)
**Design:** GDD v0.5 §13a *Benchmarks* (the "auto-designate" extension: "a benchmark is any node she has reached before, fired on first entry this run"), decisions-log 2026-09-29 *Exploring a room is done once, ever*. Backlog *Now* (part B of old Next 12; part A is plan 020, built).

## Goal
Each run, her first entry into any room except the start room puts a milestone card in the feed with this run's time against last run's. The first entry ever also opens that room's story passage. This keeps the "you got here sooner" feedback now that search milestones fire only once.

## Out of scope
- Best-ever times on cards (backlog Next 5). Sparklines.
- Writing the real passages (a `/writing` session).
- Dropping `IsKnown` (backlog *Now* follow-up; a separate clean-up after this plan).

## Settled (2026-09-29 and this session; don't re-ask)
- **Which rooms:** every room except `GameContent.startNode` (The Smoky Mirror). No authored list, so a room added later is a benchmark automatically. Going back into the start room never fires.
- **When:** the first arrival in a room each run. Later arrivals in the same run do nothing. Every hop of a multi-room trip counts, because trips are queued one room at a time (`ScheduleTrip`).
- **Timing and report:** the same as switch milestones. The card shows this run against last run, or "first time" on the first entry ever. Rows go in the run report's milestones table. `CanBeReachedAgain` is always true for rooms.
- **First-ever story:** one `StoryBeat` per room, shown once through `Reveal` (the story popup, like other beats). The text is placeholder until the user writes it. Never overwrite it once written.
- **Feed (user, 2026-09-30):** on a room's first entry this run, the card replaces the "arrived" line. Later entries still get the line.
- **No Loop Settings switch.**

## Design assumptions (decided by Claude; state in the report)
- **Milestone key = `ContentAsset`** (the shared base of `SwitchDefinition` and `NodeDefinition`), not a new wrapper type. Saves already store content Ids, so a node Id fits the existing milestone lists: no kind field and no Id prefix. Code that needs switch-only data (story, trigger) type-checks the key. Any other type is an impossible state and throws.
- **Story storage:** a new optional `StoryBeat firstEntry` field on `NodeDefinition`. A room with no beat still gets its timed card.
- **Card and table title** = the room's `DisplayName`. No new text key per room.
- **"First time ever"** = a kept set, `PersistentState.RoomsEntered`, mirrored per run by `LoopState.RoomsFirstEntered` (as `SwitchesFlipped` is for switches). Old saves upgrade with an empty set, so each room's next entry counts as its first (a dev-only effect; friends start fresh).
- **Placeholder passages:** 5 `.txt` files in `Assets/Story/`, numbered on from the existing ones (17–21). Line 1 is the room name, which is the beat's title. The body starts `PLACEHOLDER` so `/writing` can find it. A one-click menu creates the files, beats and links, and skips any file or link that already exists.

## Reuse
- `Simulation.Tasks.cs` `CompleteCurrentTask` (~809): sets `CurrentNode` (827) and raises `Arrived` (828). The hook goes just before 828.
- `Simulation.Unlocks.cs` `ReachMilestone` (151), `CanBeReachedAgain` (167), `LastRunTimeOf` (177), `Reveal` (110). `SecondsThisRun` (`Simulation.Attributes.cs:265`).
- `RunRecord.TimeOf`, `Simulation.Report.cs` `AddMilestoneRows`/`RowFor` (98-130), `StoryPanel.Times`/`Refresh`, `MilestoneTable.Show`.
- `ContentIndex.AddNode` (70-89): add `firstEntry` there, or the journal won't find the beat on load.
- `EditorUiFactory.GetOrCreateAsset<T>` / `EnsureFolder`, and `GreyboxSetup`'s numbered Setup steps.
- Test helpers: `SimulationTestBase.MakePlaces`/`Join`/`MakeStory`/`SaveAndLoad`, and `MilestoneTests.MakeSimulation` (`_reached`).

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `NodeDefinition.cs` | Edit | `StoryBeat firstEntry` (tooltip). |
| `LoopState.cs`, `RunRecord.cs`, `RunReport.cs` | Edit | Milestone lists, `HasReached`, `TimeOf` and `MilestoneRow.Milestone` keyed by `ContentAsset`. `LoopState.RoomsFirstEntered`. |
| `PersistentState.cs` | Edit | `RoomsEntered` (kept set). |
| `Simulation.Tasks.cs`, `Simulation.Unlocks.cs` | Edit | `EnterRoom(node)`: skip the start room and repeats this run; reach the milestone; on the first entry ever, record it and `Reveal(firstEntry)`. `ReachMilestone` takes a `ContentAsset`; the "needs a story" check applies to switches only. |
| `Simulation.cs` | Edit | `MilestoneReached` → `Action<ContentAsset, float, bool>`. `Arrived` → `Action<NodeDefinition, bool firstEntryThisRun>`. |
| `Simulation.Report.cs` | Edit | Rows and `CanBeReachedAgain` for room keys. |
| `ContentIndex.cs` | Edit | Index `firstEntry`. |
| `SaveData.cs`, `SaveSerializer.cs` | Edit | v15: `roomsEntered` (kept) and `SavedRun.roomsFirstEntered`. Milestones read via `Find<ContentAsset>`. |
| `StoryPanel.cs`, `MilestoneTable.cs` | Edit | Title, "first time" and tooltip/Read more for room keys (Read more is hidden when there's no beat). |
| `FeedNotes.cs` | Edit | Skip the arrived line when `firstEntryThisRun`. |
| `Editor/RoomStorySetup.cs` | New | Menu *Hall of Echoing Mirrors → Setup → Step 95: Room entry stories*. |
| `Assets/Story/17-21_*.txt`, `Data/Story/*.asset`, `Data/Places/*.asset` | New/Edit | Made by the menu, not by hand. |
| `PROJECT_NOTES.md` | Edit | List the placeholder passages. |

New content fields: `NodeDefinition.firstEntry` (story, not balance: no Balance Sheet column).

Save format change? **Yes** → `SaveData.CurrentVersion` 15. Upgrade step: add empty `roomsEntered`/`roomsFirstEntered`. Old milestone Ids are switch Ids and still resolve through `Find<ContentAsset>`.

## Steps
1. Failing tests (MilestoneTests): a room entry is timed; the start room and repeat entries don't fire; the same room is timed against last run.
2. Re-key milestones to `ContentAsset` across Core. Existing tests stay green.
3. `EnterRoom` hook, `RoomsEntered`/`RoomsFirstEntered`, `firstEntry` reveal, `ContentIndex`. Then the `Arrived` flag.
4. Report rows for rooms (RunReportTests).
5. Save v15 and the upgrade, with tests.
6. UI: StoryPanel, MilestoneTable, FeedNotes.
7. Setup menu (Step 95), then run it. Docs: PROJECT_NOTES, backlog.

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `MilestoneTests.EnteringARoom_IsTimed_FirstTime` | Card data on the first ever entry, `firstTime` true |
| `MilestoneTests.EnteringARoom_NextRun_ComparesToLastRun` | Last-run time found for a room key |
| `MilestoneTests.StartRoom_NeverFires` | Including walking back into it |
| `MilestoneTests.SecondEntrySameRun_DoesNothing` | Once per run |
| `MilestoneTests.EachHopOfATrip_Counts` | A 2-room trip gives 2 milestones |
| `MilestoneTests.RunEndingOnArrival_StillRecordsEntry` | The entry lands in the run record |
| `MilestoneTests.FirstEntryStory_RevealedOnce_EvenAfterReload` | Journal, across save/load mid-run |
| `MilestoneTests.RoomWithoutStory_StillTimed` | `firstEntry` optional |
| `PlacesTests` (existing `Arrived` log) | Flag true on the first entry, false after |
| `RunReportTests.RoomEntry_AppearsInTableEveryRun` | `CanBeReachedAgain` true for rooms |
| `SaveTests.RoomMilestones_RoundTrip` and `SaveTests.V14Save_Upgrades` | Node Ids in milestone lists; v14 loads |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] Setup menu run once; 5 rooms show their beat in the Inspector; `.txt` files start with the room name, then `PLACEHOLDER`
- [x] In Unity, new save: walk to A Dark Hall → story popup plus a "first time" card, with no arrived line. Walk back to The Smoky Mirror → only the line. Next run, walk to A Dark Hall → card with this run vs last, no popup. The Summary page lists the room.
- [x] decisions-log already records the rule; propose a `.claude/rules/simulation.md` line on milestone keys (and the stale IsFound/IsKnown line)

## Notes after implementation
- Built as planned. Milestone key is `ContentAsset`; `Simulation.StoryOf`/`NameOf`/`IsFirstTimeEver` keep the switch-or-room type checks out of the UI.
- Placeholder passages use `[PLACEHOLDER]` (the marker in `.claude/rules/content.md`), not bare `PLACEHOLDER`.
- Step 95 made the beats as `Assets/Data/Story/<Room>Entry.asset`. Rooms with no story hide Read more on the card.
- Tests: 552 EditMode pass. Playtested in Unity by the user: looks good.
- A stray second `MilestoneRow` in the scene's Story feed (never hidden) showed "The Chase Ends" at the top of every run; deleted from `SampleScene`.
