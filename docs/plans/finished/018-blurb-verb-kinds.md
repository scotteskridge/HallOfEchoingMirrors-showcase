# 018 — Blurb buckets match by a task's own BlurbTopic

**Status:** Done
**Design:** GDD §9 (Content Vocabulary: Tasks — `TaskKind`), §12 *Narrativising the collapse* (ambient-bucket system); decisions-log-older 2026-09-24 *The blurb system*; decisions-log 2026-09-29 (plan 015, cooldown/fallthrough; plan 018 revised) — backlog Now — the user's ask, before the writing session adds more buckets

## Revised mid-build
This plan first shipped matching by `TaskKind` (see git history for the original text). Code review caught a real bug before commit: `TaskKind` only decides which stat a task trains, and several unrelated tasks share one on purpose — the generic "Pick up" verb and "Cut the stone" are `TaskKind.Gather`/`Instantiate`, same as "Gather a wisp" and the mirror-crafting tasks. Matching blurbs by `TaskKind` made picking up *any* object fire wisp lines, and cutting the stone fire mirror-instantiating lines. Discussed with the user, who proposed the fix: a second, independent label on the task, just for which blurb topic it belongs to. This document describes the corrected design as built.

## Goal
A new task that reuses an existing story moment (another Instantiate, another Gather) lights up the matching generic blurb bucket with no code change and no per-bucket edit — only the task's own `blurbTopic` needs setting, deliberately, by whoever creates it. Context buckets (item-specific, state-specific, location-specific) keep working exactly as now, layered on top by the existing priority/cooldown fallthrough.

## Out of scope
- New blurb buckets or rewriting any lines (writer's job, tracked separately).
- Satchel-upgrade-level gating: no such mechanic exists yet (`ResourceDefinition`/`Simulation.Resources.cs` model the satchel as one held item, not tiered). Adding a gate for it now would be a hook for a system that isn't designed — noted below as deferred, not built.
- Inverting the reference so `TaskDefinition` lists its buckets: rejected in favour of topic-matching, which needs no per-task list at all for the common case (user's choice).
- A shared `Requirement`/`Condition` abstraction: none exists anywhere in Core (walls, switches and tasks each have their own bespoke gate fields), and `BlurbBucket`'s existing fields (`needsFlipped`, `needsHeld`, loop range, rooms, vitality) already cover requirement C's examples. Inventing a shared type now would be exactly the premature abstraction CLAUDE.md warns against.
- Any change to the `.txt` import format or the writer's `#` comment convention — this plan is rules-side (asset fields) only, per requirement E.
- Deciding whether Cut the Stone or Fill a Phial of Memory belong to a blurb topic — left untagged, a placeholder for the writer (see Hand-back).

## Design assumptions
- **`BlurbTopic` (new enum) is the story family; `TaskKind` (GDD §9) stays the stat-training family.** The two are independent by design: a task's `kind` decides which stat trains on finishing it; its `blurbTopic` decides which generic blurb bucket it belongs to. Nothing assumes they're related, because in this repo they aren't always.
- **A generic bucket sets `topics` (new field on `BlurbBucket`)** instead of enumerating tasks; a bucket restricted to one specific task (a one-off beat like `start_chase`, or an item-specific context bucket like "instantiating a candle" once the writer adds one) keeps using the existing `tasks` list, which is already an asset reference, not a name.
- **Combining a generic bucket with a context bucket needs no new picking logic.** `BlurbPicker.Pick` already scans every enabled, fitting, off-cooldown bucket in the library and chooses by priority → chance → cooldown fallthrough (plan 015). A context bucket (e.g. `candlelight`, `ring_carried`) just sits at a different priority than its generic sibling; nothing about that changes here.
- **Satchel-upgrade-level gating is a placeholder note, not a placeholder field.** When a satchel-upgrade mechanic exists, extend `BlurbBucket` the same way `needsHeld` already does (a numeric threshold on whatever represents the tier). Tracked on the backlog; not built speculatively now.
- `Fits()`'s existing pattern — a list field with "empty = no restriction" — is followed exactly for `topics`, so nothing already relying on empty `tasks`/`rooms`/`roomKinds` changes behaviour.

## Reuse
- `BlurbBucket.tasks`, `.rooms`, `.roomKinds` (`Scripts/Core/BlurbBucket.cs`): the exact "empty list = any" pattern `topics` follows.
- `BlurbPicker.Fits` (`Scripts/Core/BlurbPicker.cs:86`): where the new check is added, next to the existing `bucket.tasks.Count > 0` check it complements.
- `BlurbImporter.StartingRules`/`FixKnownRules`/`FixRule` (`Assets/Editor/BlurbImporter.cs`): the guarded one-off-fix pattern plan 015 built, reused for migrating the already-created bucket assets and for tagging the task assets they used to list.
- `GameContent.tasks` (`Scripts/Core/GameContent.cs:20`) plus the common verbs (`travelVerb`/`exploreVerb`/`pickUpVerb`/`putDownVerb`): the live task universe, for the "bucket can never fire" warning.
- `BlurbTests` (`Tests/EditMode/BlurbTests.cs`), `SimulationTestBase.MakeTask`.

## Changes
| File or asset | New / Edit | What |
| --- | --- | --- |
| `Scripts/Core/TaskDefinition.cs` | Edit | new `BlurbTopic` enum (`None`, `Gathering`, `Instantiating`, extended as more buckets need it) and `TaskDefinition.blurbTopic` field, default `None`, independent of `kind` |
| `Scripts/Core/BlurbBucket.cs` | Edit | new `topics: List<BlurbTopic>` field, tooltip explains "any task tagged with this topic; empty = no restriction" |
| `Scripts/Core/BlurbPicker.cs` | Edit | `Fits` gains a `topics` check (against `task.blurbTopic`) mirroring the `tasks` check |
| `Assets/Editor/BlurbImporter.cs` | Edit | `StartingRules`: `gathering_wisps`/`start_gather_wisp` and `instantiating`/`start_instantiate` set `topics` directly; `FixKnownRules` gets a `MigrateTasksToTopic` fix for the **already-existing** bucket assets: it tags every task still in the bucket's old `tasks` list with the topic (unless a task already carries a different one by hand), then clears the list and sets `topics` — the bucket's own hand-curated task list is the evidence for the topic, so nothing is guessed from `TaskKind`; a post-import warning logs any enabled bucket whose `tasks`/`topics`/`rooms`/`roomKinds` matches nothing in `GameContent` (including the common verbs) |
| `Tests/EditMode/BlurbTests.cs` | Edit | new cases for `topics` matching, including a regression test that a task sharing a `TaskKind` but not the `topics`-matching `blurbTopic` stays silent (see Tests) |

New content fields: none balance-relevant (`blurbTopic`/`topics` are structural references, not tunable numbers) — no Balance Sheet column needed.

Save format change? No — `blurbTopic`/`topics` are content data on `TaskDefinition`/`BlurbBucket` assets, referenced like every other field; nothing new is written to `SaveData`.

## Hand-back to the writing session
Bucket scope is unchanged by this plan (no lines move, no buckets are added or renamed) — this just states, per bucket, whether it now fires by **blurb topic** (generic, reusable by any future task tagged with it, with zero data edits) or stays **context-gated** (one specific task, a room, a held item, a switch, a loop range), so the writer knows which lines should stay "names no object" and which may name one:

| Bucket | Scope after this plan | Fires for |
| --- | --- | --- |
| `start_gather_wisp` / `gathering_wisps` | **Blurb topic: Gathering** | Any task tagged `BlurbTopic.Gathering` (currently only "Gather a wisp"; a future task qualifies once its author tags it) |
| `start_instantiate` / `instantiating` | **Blurb topic: Instantiating** | Any task tagged `BlurbTopic.Instantiating` (currently flint & steel, candle, phial, satchel, and "Instantiate Roland's ring"; a future task qualifies once tagged) |
| `start_explore` / `exploring` | Verb kind (activity): Exploring | Unchanged — already activity-based, not task-list-based |
| `start_search` / `searching` | Verb kind (activity): Searching | Unchanged |
| `start_travel` | Verb kind (activity): Travelling | Unchanged |
| `start_chase` | **One-off, pinned to one task** (`ChaseRoland`) | Not a topic family — a single unrepeatable story beat; stays as an explicit `tasks` reference |
| `hall_corridor`, `hall_dark`, `hall_shifted` | Context: room/room-kind + loop range | Unchanged; layer on top of whichever topic bucket is also firing |
| `ring_present`, `ring_carried`, `ring_refused`, `lab_sealed` | Context: room and/or held/not-held + loop range | Unchanged |
| `candlelight`, `satchel` | Context: held item, no topic restriction (ambient state, not tied to the Instantiate action itself) | Unchanged |
| `low_vitality`, `reaching` | Context: vitality threshold / loop taper, no topic restriction | Unchanged |

**Open for the writer:** *Cut the stone* (lab) and *Fill a phial of memory* (hall) both share a `TaskKind` with tagged tasks (Instantiate and Gather respectively) but are left at `BlurbTopic.None` — neither was ever in `instantiating`'s or `gathering_wisps`' lines, and whether either belongs to one of these topics (or needs its own) is a story call, not a code one. Noted as a placeholder in `PROJECT_NOTES.md`.

**For a future item-specific bucket** (e.g. "instantiating a candle" specifically, narrower than the generic Instantiating bucket): give it a higher `priority` than `instantiating` and point its `tasks` field at just that one task asset — the existing mechanism, no new field. The cooldown/fallthrough already built in plan 015 means it won't crowd out the generic bucket; it just wins when that specific task is running and its own chance/cooldown allow it.

## Steps
1. Write failing `BlurbTests` cases for `topics` matching (any task with that topic fits; a different topic doesn't; a task sharing only the `TaskKind` stays silent; empty list = unrestricted).
2. Add `TaskDefinition.blurbTopic`, `BlurbBucket.topics` and the `Fits` check; run tests green.
3. In `BlurbImporter.StartingRules`, switch `gathering_wisps`/`start_gather_wisp` and `instantiating`/`start_instantiate` to set `topics` directly.
4. Add the guarded `MigrateTasksToTopic` fix to `FixKnownRules` for the bucket assets that already exist in this repo (it tags their tasks, then clears the list), plus the "bucket can never fire" warning (including the common verbs).
5. In Unity: run *Import Blurbs*, then *Fix Known Blurb Rules*; check the four bucket assets in the Inspector — `Topics` set, `Tasks` cleared; check the tagged task assets — `Blurb Topic` set, `Kind` untouched.
6. Play a run: gathering a wisp still fires `gathering_wisps` blurbs; instantiating each of the five tagged items still fires `instantiating` blurbs; picking up an unrelated object and cutting the stone do **not** fire either bucket; confirm nothing else changed (candlelight/satchel/ring/etc. still behave as before).

## Tests
| Test (class.method) | Proves |
| --- | --- |
| `BlurbTests.ABucketWithATopic_FitsAnyTaskWithThatTopic` | A bucket with `topics = [Gathering]` fits a task tagged `BlurbTopic.Gathering` it has never seen before, with no task-list entry |
| `BlurbTests.ABucketWithATopic_DoesNotFitADifferentTopic` | The same bucket doesn't fit a task tagged `Instantiating` |
| `BlurbTests.ATaskSharingTheKindButNotTheTopic_DoesNotFit` | Regression guard for the bug this plan fixed: a task with `TaskKind.Gather` but `BlurbTopic.None` (e.g. Pick up) doesn't fit a `Gathering`-topic bucket |
| `BlurbTests.AnEmptyTopicsList_IsUnrestricted` | Existing buckets with no `topics` set are unaffected |
| `BlurbTests.TopicAndExplicitTask_BothMustMatchWhenBothSet` | A bucket with both `tasks` and `topics` set (an item-specific context bucket layered under a topic) requires both, confirming the two fields combine rather than override |

## Done when
- [x] Tests above pass; compile and Console clean
- [x] In Unity: `gathering_wisps`/`instantiating` (and their `start_` counterparts) fire correctly after migration with `Tasks` cleared and `Topics` set; the tagged task assets show their `Blurb Topic` with `Kind` unchanged
- [x] decisions-log updated: buckets match by `BlurbTopic` (independent of `TaskKind`) for topic-generic firing, task list stays for one-off/item-specific pins; the `TaskKind`-based version this plan first shipped and its bug are recorded; backlog line added for satchel-upgrade gating (deferred, no mechanic yet)

## Notes after implementation
- `start_gather_wisp` and `start_instantiate` already existed as bucket assets too (the plan only named `gathering_wisps`/`instantiating`); `FixKnownRules` migrates all four with the same guarded fix.
- The `instantiating` bucket already had a fifth task hand-added (`TakeTheRing`, displayed "Instantiate Roland's ring", beyond the plan's expected four) — since the migration tags whatever is actually in the bucket's own task list rather than checking against a hardcoded set or a `TaskKind`, it picked this up correctly with no manual fix.
- **The `TaskKind`-based version of this plan was implemented, tested (502/502 green) and migrated onto real content before a second review pass caught the bug** (verified directly against `PickUp.asset`, `FillABottledWell.asset` and `CutTheStone.asset`, which are `Gather`/`Instantiate` kind but narratively unrelated to the wisp/mirror buckets). It was replaced with the `BlurbTopic` design in the same session, before any commit — nothing broken ever shipped, but it's worth recording that `TaskKind`-matching looked correct under test (synthetic tasks in `BlurbTests` don't share kinds the way real content does) and only failed against the actual game content.
