# 028 — Refactor follow-ups (after refactors A and B, 2026-09-30)

**Status:** Done (all parts; Part 4, the editor reload, done by the user 2026-09-30)
**Left to do:** nothing.
**Design:** none. These are refactors and a tooling fix; no rule changes. Source: `docs/code-health/2026-09-28.md` (top refactors 2 to 4 and the tooling note) and the refactor session of 2026-09-30.

## Goal
Finish cleaning up around the container and "gives" code, and give pool (hue) names text keys instead of C# enum names. The player sees no difference. Each part is a separate `/refactor` session unless it says otherwise.

## Where things stand (read first)
- Branch `backlog-tasks`. Three refactor commits are already in:
  - `a2446d1` `ContentsOf` reads what `FillContainers` worked out. `FillContainers` records each container's contents in `_containerContents` (lists are cleared and reused, never rebuilt), so the pockets overlay allocates nothing per frame.
  - `01b4653` `ResourceAmount.IsReal` (`resource != null && amount > 0`) replaced 6 copies in `Simulation.Tasks.cs` and `Simulation.Offers.cs`; the 7th (`CanCarry`) was fixed at wrap-up, along with `ContentsOf` now returning `IReadOnlyList` (reviewer).
  - `5189ccd` `CarriedFull` and `GivesAreFull` share one loop, `NothingItGivesFits(task, bool carriedOnly)`. **Part 0 fixes a standards breach in this commit.**
- Baseline: **582 EditMode tests, all passing**, and the Console was clean after the last commit.
- Characterisation tests added in that session: `ContainerTests.ContentsOf_ATwoKindPouch_FillsItsItemsInItsOwnOrder`, `ContentsOf_TwoContainersOfOneKind_FillInTheOrderSheCameByThem`, `ContentsOf_AnEmptyPouch_IsEmptyWithAllItsRoom_AndAnUnheldOneHasNoRoom`, and `PocketTests.Gathering_IgnoresBlankAndZeroGives_AndStillStopsWhenPocketsAreFull`.
- **Not yet done:** `/wrap-up` for refactors A and B. The user must type it (the skill can't be started by Claude). The code-health report `docs/code-health/2026-09-28.md` doesn't yet mark top refactors 2 and 3 as done; add "✅ done 2026-09-30 (a2446d1, 01b4653, 5189ccd)" to those rows, the way row "Trip adjacency…" is marked.
- The user's uncommitted work (`RunResultsPanel.cs`, `game_text.txt`, GDD, decisions logs, `opening-brief.md`, `BACKLOG.md`, plan 023) is theirs: never stage it. Commit only the files a step touches (`git add <paths>`, never `-A`).
- **Ignore the editor's LSP errors** (for example "`ScheduleTarget` could not be found", "`RunReport` does not contain `Milestones`"). They are stale; see Part 4. Trust Unity's compile (`refresh_unity`, then `read_console`) and the test run.
- Unity MCP drops for a few seconds on every domain reload. If a call says "ping not answered" or "disconnected", retry the call; `.\tools\unity-mcp.ps1` shows which side is down (`docs/unity-mcp-connection.md`).

## Out of scope
- The three 🔴 in the code-health report: they change behaviour, so each is a bug-fix task, not a refactor. (🔴 2's room titles: `Simulation.Tasks.cs` `reasons.not_here` and `Simulation.Places.cs` `reasons.no_way` already use `GameText.TitleInSentence`; check `Simulation.Reorder.cs` before calling it open.)
- Splitting `Simulation.Tasks.cs` (~760 lines): its own refactor (backlog-later).
- Anything in `RunResultsPanel.cs`: the user has uncommitted changes there, and the report marks it "redesign planned".

## Part 0 — Remove the flag parameter from `NothingItGivesFits` (do this first, small)
**Problem.** `docs/CODE-STANDARDS.md` §4: "No flag parameters that switch a method between two behaviours: make two methods." `Simulation.Tasks.cs` now has `NothingItGivesFits(TaskDefinition task, bool carriedOnly)`, called by `CarriedFull` (true) and `GivesAreFull` (false). The flag was chosen over a delegate so that no delegate is allocated each tick (`IsDone` and `TryStartNextTask` call these every tick; §3 hot paths).

**Options (pick one with the user; recommendation first):**
1. **Recommended: pass the room test as a cached delegate.** `NothingItGivesFits(TaskDefinition task, Func<TaskDefinition, ResourceDefinition, bool> fits)`, with two private delegate fields built once, not per call. Field initialisers can't use `this`, so build them lazily (`_fitsCarried ??= (t, item) => RoomFor(item) > 0;`) or in the constructor in `Simulation.cs`. `CarriedFull` and `GivesAreFull` stay one-line wrappers. No per-tick allocation, one loop, no flag. Also delete the doc-comment sentence "A flag rather than a delegate: this runs every tick."
2. Go back to two full loops, sharing only `IsReal`. Compliant and simplest, but it restores the duplication that refactor A removed.
3. Keep the flag and add a justified exception to CODE-STANDARDS. That is a rules edit: CLAUDE.md says propose the exact lines and wait for the user's OK.

**Tests.** No new tests are needed: `PocketTests`, `FullPocketsTests`, `SupplyTests` and the `Carry` tests cover both paths. Run the full suite. Commit: `refactor: NothingItGivesFits takes the room test, not a flag`.

## Part 1 — `FillContainers` runs more often than it needs to (measure first)
**What happens now** (`Simulation.Resources.cs`):
- `FillContainers()` recomputes every container's contents from `Loop.ToolsAndStats` on every call to `PocketsUsed`, `CarriedItems`, `InContainers`, `ContentsOf`, `ContainerUsed` and the private `ContainerRoomFor`.
- `RoomFor(pocketed item)` calls it **twice** (`PocketsUsed`, then `ContainerRoomFor`). `RoomFor` runs per give in `CarriedFull`, `GivesAreFull` (through `SpaceFor`), `GivesOnlyWhatExists` and `GoesUntilGivesAreFull`, so several times per tick.
- `CarriedItems` fills once, then again inside each `ContentsOf` (N + 1 fills). UI callers: `PocketsOverlay` (lines ~78, 106, 223) and `Simulation.Floor.cs:155` `InPockets`.
- Since `a2446d1` it **allocates nothing** after the first call per container, so this is CPU only, and small (a handful of containers × `holds`).

**Why a "dirty flag" (recompute only after a change) is risky:** `LoopState.ToolsAndStats` is a public `Dictionary` written directly from outside the resource code:
- `Simulation.Resources.cs` `Grant`/`Take` (the normal path)
- `Simulation.Carrying.cs:43`
- `Saving/SaveSerializer.cs:277` (resume)
- 71 writes in 10 test files (`ContainerTests`, `PocketTests`, `FullPocketsTests`, `SupplyTests`, `RestorationTests`, …)

Kept items are read through `Persistent.Resources`, which is also written directly. A flag that misses one writer returns stale contents: a silent wrong answer (§4 "fail loudly") that tests may not catch.

**Steps:**
1. **Measure before building.** With the Unity Profiler (Window → Analysis → Profiler, Deep Profile off) in a real run with the pouch, check whether `FillContainers` shows up at all. If it doesn't, record "measured, not worth it" in the code-health report and stop after step 2.
2. **Cheap, safe win (a pure refactor):** `RoomFor` fills once. Add a private `PocketsUsedAfterFill()` that assumes a fill has just happened, and have `ContainerRoomFor` take the same assumption, so `RoomFor` calls `FillContainers()` once and then both. Same for `CarriedItems`: add a private `ContentsAfterFill(container)` used inside its loop. Existing tests pin the behaviour (the 3 `ContentsOf_*` tests, `PocketTests`, `ContainerTests`).
3. **Only if step 1 shows a real cost:** a version counter. That means wrapping `ToolsAndStats` so every write goes through one method (a behaviour-neutral but wide change touching the 71 test writes). It needs `/plan-feature` first, not this plan.

## Part 2 — Hue names from text keys (code-health top refactor 4, "C")
**Problem.** CLAUDE.md: no player-facing text in code. Hue names reach the player as C# enum names in two places:
- `Simulation.Tasks.cs` (~line 661): `GameText.Get("reasons.needs_pool", ("pool", source))`, where `source` is a `CostSource`, so it formats as "Ruby". This fires for a hue Clara hasn't learned, where there is no `Pool` to take a name from.
- `UI/UiStyle.cs:151-154` `NameOf(CostSource)`: `source.ToString(); // a hue: its gem name`, used by `ActionText.cs:46` when `pool == null`.

The report's `FeedNotes.cs:136-137` lines no longer exist: `FeedNotes` now uses `pool.Name`, so there is nothing to change there. `LoopSettings.cs:131` (`ReplacePoolsWithSevenHues`) writes `hue.ToString()` into the asset's pool name: that's an editor-only context-menu tool and fine as it is.

**Reuse:** `GameText.Attribute(ClaraAttribute)` in `Core/GameText.cs:131` is the pattern: `Get("attributes." + attribute.ToString().ToLowerInvariant())`. `GameTextTests.cs:122-124` checks every attribute key exists; copy that loop for hues.

**Changes:**
| File | What |
| --- | --- |
| `Assets/Text/game_text.txt` | New section `## hues` (after `## attributes`, line ~41): `amber: Amber`, `citrine: Citrine`, `emerald: Emerald`, `sapphire: Sapphire`, `iolite: Iolite`, `amethyst: Amethyst`, `ruby: Ruby`. **The user has uncommitted edits in this file:** add only these lines and stage the file only with the user's OK (otherwise their edits go into the commit), or ask them to commit theirs first. |
| `Core/GameText.cs` | `public static string Hue(Hue hue)` and `public static string Hue(CostSource source)` (the `CostSource` hue values share the `Hue` numbers 1–7, but map by name, not by casting). Throw on `Hue.None`, `Vitality` and `AllPools`, which aren't hues. |
| `Core/Simulation.Tasks.cs` | `("pool", GameText.Hue(source))` |
| `UI/UiStyle.cs` | `NameOf`'s last branch: `GameText.Hue(source)` |
| `Tests/EditMode/GameTextTests.cs` | Loop over `Hue` values (skipping `None`): every `hues.*` key exists |

**Player-visible change:** none, because the key text equals today's enum names. `TaskTests.cs:236` builds its expected text with `("pool", CostSource.Ruby)`, which still formats as "Ruby", so it passes unchanged. Leave it as it is: changing it to `GameText.Hue` would be editing a test, which needs the user's OK.

**Settled by the user (2026-09-30): one source.** A pool's name comes only from the `hues.*` keys, so changing one line in `game_text.txt` changes it everywhere (bars, tooltips, feed, reasons, costs). Today learned pools read `Pool.Name`, which `Simulation.cs:198` copies from `LoopSettings` → `pools[i].name` (the shipped asset holds "Amber"…"Ruby", the same words as the enum). That is the second source, and it goes:
- `Core/Pool.cs`: `Name` for a pool with a hue returns `GameText.Hue(Hue)`, read each time rather than stored at construction, so it follows a text hot-reload. A pool with `Hue.None` (only the code default "Magic" and tests such as `DrainTests.cs:33`, `new Pool("Magic", …)`) keeps the name it was given; no shipped pool has `Hue.None`.
- `Pool.Name` readers need no change and then all go through the one source: `PoolBars.cs` (tips, lines ~143 and 157), `FeedNotes.cs:97` (`feed.pool_empty`), `ActionText.cs:46`, `RunHeader.cs`, `DevToolsPanel.cs`. Grep `\.Name\b` to confirm the list (it also matches unrelated types).
- `LoopSettings.PoolSettings.name`: no longer read for hue pools. Don't delete the serialized field in this refactor (the Inspector and `SimulationTestBase.cs:84` set it). Give it a tooltip: "Not shown to the player: a hue pool's name comes from game_text.txt (hues.*). Used only for a pool with no hue." Retiring it fully is a clean-up idea for the end report.
- `LoopSettings.cs:131` (`ReplacePoolsWithSevenHues`, editor-only) can keep writing `hue.ToString()`, since the field is no longer shown.

With this, `UiStyle.NameOf(CostSource)` and the `needs_pool` reason, which have no `Pool` to ask, call the same `GameText.Hue`, so there is exactly one place the words live.

**Player-visible change:** none today, because the key text equals the asset names. Check with a test: for each `Hue`, a pool built from settings with a *different* `name` still shows `GameText.Hue(hue)` (this pins "one source"). It's a new test, not an edit to an old one.

**Same principle, not in this plan:** `LoopSettings.vitalityName` ("Vitality") is also player text kept in an asset. List it as a clean-up idea (a `names.vitality` key) and ask before moving it.

Save format: no change (saves store hue numbers, not names).

## Part 3 — "Does she have what it needs?" is checked in three ways
**What exists:**
- `Simulation.Tasks.cs:294` `MissingNeedAt(task, room)`: `need.resource != null && AmountOf(need.resource) + OnFloor(room, need.resource) < need.amount`. It **counts the floor** where she'd do it.
- `Simulation.Places.cs:133-135` in `CantTravel`: a way's needs, `need.resource != null && AmountOf(need.resource) < need.amount`. It **doesn't count the floor.**
- `BlurbPicker.cs:115`: `sim.AmountOf(need.resource) < need.amount` (not the floor either; it's a blurb condition, so leave it).
- The same reason text, `GameText.Get("reasons.needs", ("amount", …), ("item", ….DisplayName))`, is built 3 times: `Offers.cs:81`, `Places.cs:135`, `Tasks.cs:641`.

**Also (reviewer, 2026-09-30):** `Simulation.Tasks.cs:~350` `g.resource == item && g.amount > 0` could use `IsReal` too, but only because `item` is never null there; check that before changing it.

**Pure refactor (do this):** one private `NeedsReason(ResourceAmount need)` in `Simulation.Tasks.cs` (next to `MissingNeedAt`), used in all three places. Existing reason tests cover them (grep `Reason("needs"`). Note: `need.resource != null` and `need.IsReal` give the same answer in these checks, because a zero or negative need is never "missing" (`x < 0` is false for a count). Switching to `IsReal` is therefore safe, but optional.

**Settled by the user (2026-09-30): yes, a way's needs count the floor of the room she's leaving**, the same as an action's needs (decisions log, 2026-09-30 *A way's needs count the floor where she is*). This **changes behaviour**, so it's **Part 3b**, a separate bug-fix commit after the pure refactor above. Don't put it in a `refactor:` commit.

**Part 3b steps (failing test first):**
1. In `FloorTests` (it already has floor fixtures; use `MakePlaces` from `SimulationTestBase`): a way from room A to room B needs 1 key; the key lies on A's floor and her pockets are empty. Assert the trip can be scheduled and made (`CanScheduleTripTo` / the trip runs), and that the key is **still on the floor** afterwards: a need is checked, not spent. Add a second test: with the key on B's floor (not where she is), the trip is refused with `Reason("needs", ...)`. Run them and see the first one fail.
2. Fix: in `Simulation.Places.cs` `CantTravel(from, to)`, count `AmountOf(need.resource) + OnFloor(from, need.resource)`. Better, extract a shared private `MissingFrom(List<ResourceAmount> needs, NodeDefinition room)` used by both `MissingNeedAt` (`Tasks.cs:294`) and `CantTravel`, so the rule lives in one place. Note `from` is where she'll be when the trip starts (for a planned trip that's the room before it in the queue, which `CantTravel` already receives), not always `Loop.CurrentNode`.
3. Check the other readers of way needs still agree: `Simulation.Unlocks.cs:63` (`Mentions(way.needs, item)`, which unlocks on seeing an item, so no change) and the map (`MapView`/`RoomPopover` show trips through Core's `CanScheduleTripTo`, so they follow automatically). Grep `way.needs` and `\.needs\b` for anything else.
4. Update `.claude/rules/simulation.md`'s gotcha "What's on the floor where she is counts for an action's needs": propose the exact wording ("…for an action's or a way's needs…") and wait for the user's OK (CLAUDE.md: rules edits need approval).
5. Commit: `Ways count what lies on the floor where she is (decisions log 2026-09-30)`.

## Part 4 — Stale editor project files (the user does this; no code)
**Problem.** The VS Code C# language server reports errors that Unity doesn't have ("`ScheduleTarget` could not be found", "`RunRecord` has no `Milestones`", "`SimulationTestBase` could not be found"). `HallOfEchoingMirrors.csproj` does list `ScheduleTarget.cs` and `RouteSearch.cs`, so the likely cause is a language server that loaded an older solution and hasn't reloaded, not a missing file.

**Steps for the user:**
1. In Unity: *Edit → Preferences → External Tools → Regenerate project files*.
2. In VS Code: Command Palette (Ctrl+Shift+P) → *Developer: Reload Window*. If errors remain: *.NET: Restart Language Server* (or *OmniSharp: Restart OmniSharp*, depending on the extension).
3. Open `Assets/Scripts/Core/Simulation.Tasks.cs`. The red squiggles on `ScheduleTarget` should be gone.

**Common mistakes:** regenerating while Unity is still compiling (wait for the spinner in the bottom-right corner to stop), and having two VS Code windows open on the project.

## Part 5 — Leftovers from the 2026-09-30 session
Done in that session, for the record (all `refactor:` commits, suite green): `RoomFor`/`CarriedItems` fill once (`0a2e7bf`); `Simulation.Tasks.cs` split into `Tasks`, `Supplying` and `Paying` (`2327c09`); `CanStart` is a `??` chain of seven `CantStart…` helpers (`7cfa883`); hue names from `hues.*` keys (`73e89dd`, the method is `GameText.HueName`, not `Hue`: a method named `Hue` would clash with the type inside `GameText`).

**5a. `CanStart` has a flag parameter (`ignoreNeeds`).** The same standards breach as Part 0 (CODE-STANDARDS §4). Four callers, all in `Simulation.Supplying.cs` (lines ~24, 78, 127). Fix: two methods, `CanStart(task, destination, out reason)` and `CanStartIgnoringNeeds(task, destination, out reason)`. Don't hide the flag in a shared private method (that only moves it). The chain's pieces are already helpers, so write the short `??` chain in each, `CanStartIgnoringNeeds` without `CantStartForMissingItem`, and keep the order identical (the needs check sits between the room check and the attribute check). Say in the report that the chain is repeated on purpose. No new tests: `SupplyTests` and `TaskTests` cover it. Size S.

**5b. `LoopSettings.vitalityName` is player text in an asset.** Same principle as Part 2 ("Same principle, not in this plan"): a `names.vitality` key in `game_text.txt`, read where the vitality pool is built (`Simulation.cs:181`, `new Pool(_settings.vitalityName, …)`). **Ask the user first** (the settled rule covers hue pools only). Then keep the serialized field with a "not shown to the player" tooltip, like `PoolSettings.name`. Size S.

**5c. `.claude/rules/simulation.md` file table is out of date.** It still says `.Tasks.cs` holds supplying and paying. Rules edits need the user's OK (CLAUDE.md), so propose and wait. Proposed lines:
- `| .Tasks.cs | The action queue (Play, Schedule, Carry) and starting tasks |`
- `| .Supplying.cs | Supplying a blocked entry (QueueSupplier, a private method, 3 deep), and what an action gives or needs |`
- `| .Paying.cs | Paying for tasks (costs, modifiers) and finishing them |`

**5d. Part 3 builds on `CantStartForMissingItem`.** It already builds the `reasons.needs` text in `Simulation.Tasks.cs` (one of the three copies Part 3 names). When doing Part 3, have `NeedsReason(ResourceAmount need)` replace the copy there too, so `CantStartForMissingItem` becomes `missing == null ? null : NeedsReason(missing)`.

**Still open:** nothing (Part 4 done by the user, 2026-09-30).

## Order and size
| Part | Size | Session |
| --- | --- | --- |
| 0 flag parameter | S | `/refactor` (can share a session with 1, step 2) |
| 1 fewer fills | S (steps 1–2); M+ if step 3 | `/refactor`; step 3 needs `/plan-feature` |
| 2 hue names, one source | S–M | `/refactor` (settled: keys only) |
| 3 needs reason | S | `/refactor` |
| 3b ways count the floor | S | bug-fix session (behaviour change, failing test first); settled |
| 4 project files | — | the user, any time |
| 5a `CanStart` flag | S | `/refactor` |
| 5b `vitalityName` key | S | `/refactor` (ask first) |
| 5c rules-file table | XS | needs the user's OK on the wording |
| 5d `NeedsReason` in `CantStartForMissingItem` | — | folded into Part 3 |

## Done when
- [x] Part 0: no `bool` flag on `NothingItGivesFits`; full suite green
- [x] Part 1: measured; `RoomFor` fills once; result noted in the code-health report
- [x] Part 2: `hues.*` keys; `GameText.Hue`; `Pool.Name` reads the key for hue pools; no `ToString()` of a hue and no `PoolSettings.name` reaches the player; key-existence and "one source" tests
- [x] Part 3: one `NeedsReason` (b213164)
- [x] Part 3b: failing tests first, then a way's needs count the floor where she is (shared `MissingFrom`); rules-file wording approved and applied
- [x] Part 4: no false LSP errors in `Simulation.Tasks.cs` (the user, 2026-09-30)
- [x] Part 5a: no `bool` flag on `CanStart`; full suite green (c10c492)
- [x] Part 5b: the user's OK (2026-09-30), `names.vitality` key read where the vitality pool is built
- [x] Part 5c: the user's OK (2026-09-30), `.claude/rules/simulation.md` table updated
- [x] Part 5d: done together with Part 3
- [x] Each part: full EditMode suite green (606 at wrap-up), Console clean, no existing assertion changed, `refactor:` commits holding only that part's files, then `/wrap-up`

## Notes after implementation
- Part 3b ended up with a shared `MissingFrom(needs, room)` in `Simulation.Supplying.cs`; `MissingNeedAt` and `CantTravel` both call it. The trip counts the floor of `from`, the room she is leaving.
- Part 5b: `names.vitality` is read in `Simulation.cs` when a run begins (not on every read like hue names).
- Part 4: regenerating project files from Unity changed nothing (the `.csproj` already listed every file); the red squiggles are VS Code's language server, fixed by *Developer: Reload Window*.
- Test count: 597 at the start of this session, 600 at the end (2 way-needs tests, 1 vitality-name test).
- Review (2026-09-30): the duplicate `costs.vitality` key was removed, so `UiStyle.NameOf` and the vitality pool both read `names.vitality`. The `CanStart` chains repeat on purpose. Commit hashes: `b213164` (NeedsReason), `c10c492` (CanStart), `66d3b0d` (way needs); Part 5b and 5c went into the user's `4a15f49`.
