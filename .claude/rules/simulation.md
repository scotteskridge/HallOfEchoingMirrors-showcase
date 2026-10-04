---
# Simulation (Core), saving, GameController/TickEngine and the EditMode tests.
paths:
  - "Assets/Scripts/Core/**"
  - "Assets/Scripts/Saving/**"
  - "Assets/Scripts/*.cs"
  - "Assets/Tests/**"
---
# Simulation, saving and tests

## Core constraints
- Namespace `HallOfEchoingMirrors.Core`. No MonoBehaviours; UnityEngine only for content ScriptableObjects, `Mathf` and `Debug`.
- `GameText` is the one allowed static state.
- Public types get a one-line `///` summary.
- `TickEngine` and `GameController` live in `Assets/Scripts/`, not Core. `DevToolsPanel` (same folder) is editor/development-only.

## Where the rules live
`Simulation` is one partial class split by topic. **Put a new rule in the matching file:**

| File | Holds |
| --- | --- |
| `Simulation.cs` | The loop and the tick (applies the drain); beginning, ending and resuming runs |
| `.Tasks.cs` | The action queue: Play, Schedule, Carry (`CanCarry`), `Enqueue`, removing entries and stops, `ClearQueue` |
| `.QueueInRoom.cs` | Schedule/Play/Carry in a room (`ScheduleIn`, `PlayIn`, `CarryIn`) and where they land (`WhereScheduleLands`) |
| `.Starting.cs` | Starting the top entry (`TryStartNextTask`, `StartTask`) and stopping or suspending the running one |
| `.CanStart.cs` | Why an action can't start (`CanStart`, `WhyEntryCantStart`, the `CantStart…` chain): the first reason wins |
| `.Repeating.cs` | When a queued entry is done (`IsDone`, `RepeatKindOf`) and whether what it gives has room (`GivesAreFull`, `CarriedFull`) |
| `.Supplying.cs` | Supplying a blocked entry (`QueueSupplier`, a private method, 3 deep), and what an action gives or needs |
| `.Paying.cs` | Paying for tasks (costs, modifiers) and finishing them |
| `.Attributes.cs` | Stats, skills, speed, XP, mastery, and the drain's rate and growth (`VitalityDrainPerSecond`, `GrowTheDrain`) |
| `.Resources.cs` | Holding things, pockets, containers (`RoomFor`), one-of-a-kind objects |
| `.Floor.cs` | Floor piles, Pick up and Put down |
| `.Places.cs` | Rooms, ways, travel |
| `.Offers.cs` | What each room offers (`OffersAt`, `TasksAt`) and why an action can't be done there |
| `.Exploring.cs` | Searching |
| `.Carrying.cs` | Carried items, walking out, the stash |
| `.Restoring.cs` | Restoration items, holding off the darkness; an item starts when missing + `SteadyLossPerSecond` × its seconds covers it; `MaxRestorePerSecond` |
| `.Unlocks.cs` | Switches, task unlocks, story reveals, earned game speeds (`FastestSpeedUnlocked`) |
| `.Route.cs` | The queue read as a route: its stops (`QueueStops`, `WillTravel`, `SkippedTripReason`) and the running action's time left |
| `.Reorder.cs` | Moving queue entries and whole stops (`CanMoveEntry`/`MoveEntry`, `CanMoveStop`/`MoveStop`) and the rule refusing moves that break the route |
| `.ByHeart.cs` | Rooms known by heart: runs worked per room (`RunsWorkedIn`), room speed (`RoomSpeed`, `RoomSpeedNow`; the hold that drops it to the player's speed is `RoomSpeedHold`, its own class), carrying the plan into the next run (`CarryPlanToNextRun`) and the between-runs planning rule (`WhyNotPlannable`) |
| `.PlanWarnings.cs` | Warnings ahead on the plan (`PlanWarnings`, `RoomWarnings`, `PlanWarningCount`): forwards to `PlanWarningsTracker` (its own class), which walks the queue from where it starts to find the entries that will be refused. A change its 7 events don't cover calls `MarkPlanWarningsStale` |
| `.DevJump.cs` | Dev panel only (plan 027d): `JumpTo` a stage of the game (the `DevJumpStage` assets in `Assets/Data/Dev/`) |
| `.Report.cs` | The end-of-run `RunReport` as kept (`LastRun`, `LongestRun`) and best-ever milestone times (`BestTimeOf`). It's built by `RunReportBuilder` (its own class): record anything worth showing after a run in `LoopState`, then add it in `RunReportBuilder.Build` |
| `.SinceLastRun.cs` | "Faster than last run": `KeepThisRunsStart` at every run's end (mastery XP as the run began; 0 for a skill with no entry, null until a run has ended) and `SecondsFasterSinceLastRun` (mastery only) |

Other Core types: `ActionQueue`/`QueueEntry`; `PauseControl` (owns the pause rules; `GameController` only makes the clock follow them); `BlurbPicker`; `XpTrack` (mastery for both stats and skills); `Saving/`.

## Gotchas
- **Vitality drains with time, not actions** (decided 2026-09-25; GDD v0.3 §5 agrees), and time passes only while something is queued: a tick with nothing to do is a no-op (2026-09-28). Tests that let time pass use `KeepBusy`. Action and carry costs exist but are switched off in LoopSettings; a task marked **Always Charged** still pays.
- **Game code reads `GameContent.PlayableNodes`, never `nodes`** (planned rooms, plan ui-053, are in `nodes` but not part of the game); only editor tools that lay rooms out read `nodes` (and `IsListedInAPlannedRoom`, so a task held only by a planned room isn't doable anywhere). A way into a planned room is skipped (`Way.IntoPlannedRoom`).
- The Hub isn't built: a run starts at GameContent's start node.
- `RoomFor(item)` is the one rule for "how many more can she take". Don't write a second.
- **What's on the floor where she is counts for an action's or a way's needs (`MissingFrom`) and is used first.** New items go container → pockets → floor.
- One-of-a-kind objects (maximum 1) are never duplicated in a run, counting every floor (`IsOneOfAKind`, `CopiesThisRun`). `MakeWayFor`/`PushedOutFor`/`MakesThings` (push out with full pockets) are placeholder rules.
- Actions that can't be done now are **refused as they're asked for** (nothing queued, no time spent, `ActionRefused` fires). `IsDoneForThisRun` hides actions with nothing left to give.
- Pick up / Put down are one copy of the common verb per item (`PickUpActionFor`/`PutDownActionFor`); a queue entry saves the verb's `Id` plus the item (`SavedEntry.item`).
- Hidden ways: `IsFound` (this run, placeholder: re-found each run) vs `IsKnown` (`PersistentState.FoundWays`).
- Rooms known by heart (`Simulation.ByHeart.cs`): at run end each worked room counts once (`RoomRuns`), and `CarryPlanToNextRun` fills `_nextQueue` from `Loop.Steps` up to the first room not known by heart.
- Mastery saves keep the old field name `expertiseXp`: don't rename it.
- For how a mechanic works (stats, searching, containers, carried items, milestones), read its `Simulation.*.cs` file above.

## Saving
- `SaveSerializer` turns `PersistentState` into `SaveData` and back; `ContentIndex` finds content by `Id`, following references out from GameContent; `SaveSlots` (`Assets/Scripts/Saving/`) handles the 3 slot files. Loading skips missing content with a warning rather than failing.
- A run under way is saved whole (`SavedRun`; `SaveSerializer.ResumeRun` / `Simulation.ResumeRun`) and resumes exactly, paused.
- `GameController` autosaves after every completed action, switch flip, pause and run end, and on quit. Its private `RequestSave` defers the write to `LateUpdate` so a save never catches an action half-done. Loading **replaces the Simulation**.

## Tests (`Assets/Tests/EditMode/`)
- Test classes inherit `SimulationTestBase` (`Make`, `MakeLoopSettings`, `MakeTask`, `MakeResource`, `MakeObject`, `MakeGatherTask`, `MakePlaces`, `SetCost`, `RunTicks`/`RunSeconds`, `KeepBusy`, `SaveAndLoad`, automatic clean-up). Use its helpers rather than building fixtures by hand.
- Compare messages with `Reason("key", ...)`, never fixed English.
- Extend the existing suite for the area (e.g. `SupplyTests`, `PocketTests`, `ContainerTests`, `FloorTests`, `FullPocketsTests`, `RunSaveTests` (tick-for-tick resume), `PauseControlTests`) rather than starting a new one. `TutorialContentTests` check the real content in `Assets/Data`.
- Balance probe and what-ifs: `Assets/Tests/EditMode/Balance/` (`docs/balance-tools.md`); edit balance assets without Undo.
