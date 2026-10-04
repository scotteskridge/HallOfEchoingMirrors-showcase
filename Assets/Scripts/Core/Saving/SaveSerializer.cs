using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Turns a game's permanent state into SaveData (and JSON), and back again. Anything a save
    /// refers to that no longer exists is skipped with a warning rather than breaking the load.
    /// </summary>
    public static partial class SaveSerializer
    {
        public static SaveData Capture(Simulation sim)
        {
            var p = sim.Persistent;
            var data = new SaveData
            {
                version = SaveData.CurrentVersion, // SaveData's own default is 0 (a file with no version is the oldest)
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                // A loop that has started counts as played: loading always starts a fresh one.
                loopsCompleted = sim.NextLoopNumber - 1,
            };

            foreach (var s in p.FlippedSwitches) data.flippedSwitches.Add(s.Id);
            foreach (var t in p.UnlockedTasks) data.unlockedTasks.Add(t.Id);
            foreach (var t in p.LockedTasks) data.lockedTasks.Add(t.Id);
            foreach (var h in p.UnlockedHues) data.unlockedHues.Add((int)h);

            foreach (var r in p.Resources) data.resources.Add(new SavedAmount { id = r.Key.Id, amount = r.Value });
            foreach (var r in p.ResourceCapBonus) data.resourceCapBonus.Add(new SavedAmount { id = r.Key.Id, amount = r.Value });

            data.keptVitality = p.KeptVitality;
            foreach (var a in p.AttributeMasteryXp) data.expertiseXp.Add(new SavedAttributeValue { attribute = (int)a.Key, value = a.Value });
            foreach (var s in p.SkillMasteryXp) data.skillMasteryXp.Add(new SavedValue { id = s.Key.Id, value = s.Value });
            foreach (var s in p.SkillMasteryXpAtLastRunStart) data.skillMasteryXpAtLastRunStart.Add(new SavedValue { id = s.Key.Id, value = s.Value });
            data.hasLastRunStart = p.HasLastRunStart;

            foreach (var (from, to) in p.FoundWays) data.foundWays.Add(new SavedWay { from = from.Id, to = to.Id });
            foreach (var room in p.Explored) data.exploredRooms.Add(new SavedValue { id = room.Key.Id, value = room.Value });
            data.ticksPlayed = p.TicksPlayed;
            foreach (var r in p.RunHistory)
            {
                var saved = new SavedRunRecord
                {
                    loopNumber = r.LoopNumber, ticks = r.Ticks, actionsCompleted = r.ActionsCompleted, endReason = (int)r.EndReason,
                };
                foreach (var (milestone, seconds) in r.Milestones)
                    saved.milestones.Add(new SavedValue { id = milestone.Id, value = seconds });
                data.runHistory.Add(saved);
            }
            foreach (var item in StashToSave(sim)) data.stash.Add(new SavedAmount { id = item.Key.Id, amount = item.Value });
            foreach (var item in p.Packed) data.packed.Add(item.Id);

            foreach (var room in p.RoomsEntered) data.roomsEntered.Add(room.Id);
            foreach (var room in p.RoomRuns) data.roomRuns.Add(new SavedAmount { id = room.Key.Id, amount = room.Value });

            foreach (var beat in p.Journal) data.journal.Add(beat.Id);
            foreach (var beat in p.UnreadStories) data.unreadStories.Add(beat.Id);

            if (sim.LastRun != null)
            {
                data.hasLastRun = true;
                data.lastRun = RunReportSaving.Capture(sim.LastRun);
            }

            // A run under way (and begun: a run at tick 0 just starts again) is saved whole.
            if (HasRunToSave(sim))
            {
                data.hasRun = true;
                data.run = CaptureRun(sim.Loop);
            }
            else
                data.nextQueue = SavedQueue(sim.Queue.Entries); // not begun: what's queued is for the next run
            return data;
        }

        private static bool RunIsUnderWay(Simulation sim) => sim.Phase == LoopPhase.Running && !sim.Loop.IsOver;

        /// <summary>A run is saved whole once it has begun: one at tick 0 just starts again on loading.</summary>
        private static bool HasRunToSave(Simulation sim) => RunIsUnderWay(sim) && sim.Loop.TicksElapsed > 0;

        /// <summary>
        /// The stash, plus what she took in from it if the run isn't saved: begun, but not a tick
        /// played (she waits for the first action). That run starts again on loading, and what she
        /// packed must still be in the lab for it.
        /// </summary>
        private static Dictionary<ResourceDefinition, int> StashToSave(Simulation sim)
        {
            var stash = new Dictionary<ResourceDefinition, int>(sim.Persistent.Stash);
            if (!RunIsUnderWay(sim) || HasRunToSave(sim))
                return stash;
            foreach (var entry in sim.Loop.ToolsAndStats)
                if (entry.Key.IsCarried && entry.Value > 0)
                    stash[entry.Key] = (stash.TryGetValue(entry.Key, out int kept) ? kept : 0) + entry.Value;
            return stash;
        }

        internal static string IdOf(ContentAsset asset) => asset != null ? asset.Id : "";

        // A pick-up or put-down is a copy of its verb and shares its Id, so a save stores the item beside it.
        internal static string ItemIdOf(TaskDefinition task) => IdOf(task.picksUp != null ? task.picksUp : task.putsDown);

        private static SavedAction ActionOf(TaskDefinition task) => new SavedAction { task = task.Id, item = ItemIdOf(task) };

        /// <summary>A saved task, as the pick-up or put-down for its item if it was one (null, with a warning, if it's gone).</summary>
        internal static TaskDefinition TaskFor(Simulation sim, ContentIndex index, string taskId, string itemId, string what, List<string> warnings)
        {
            var task = Find<TaskDefinition>(index, taskId, what, warnings);
            if (task == null || string.IsNullOrEmpty(itemId))
                return task;
            var item = Find<ResourceDefinition>(index, itemId, "item", warnings);
            return item == null ? null
                : task == sim.PickUpVerb ? sim.PickUpActionFor(item)
                : task == sim.PutDownVerb ? sim.PutDownActionFor(item)
                : task;
        }

        /// <summary>
        /// Rebuilds the permanent state from a save. Give the result to a new Simulation. Anything
        /// the save mentions that can't be found is listed in <paramref name="warnings"/>.
        /// </summary>
        public static PersistentState Restore(SaveData data, ContentIndex index, List<string> warnings)
        {
            var p = new PersistentState { LoopNumber = Math.Max(0, data.loopsCompleted) };

            AddAll(index, data.flippedSwitches, p.FlippedSwitches, "switch", warnings);
            AddAll(index, data.unlockedTasks, p.UnlockedTasks, "unlocked task", warnings);
            AddAll(index, data.lockedTasks, p.LockedTasks, "locked task", warnings);
            foreach (int hue in data.unlockedHues)
                if (Enum.IsDefined(typeof(Hue), hue))
                    p.UnlockedHues.Add((Hue)hue);
                else
                    SkippedValue(warnings, "unlocked hue", hue);

            AddAll(index, data.resources, p.Resources, "resource", warnings, atLeastZero: false);
            AddAll(index, data.stash, p.Stash, "stashed item", warnings, atLeastZero: true);
            AddAll(index, data.packed, p.Packed, "packed item", warnings);
            foreach (var way in data.foundWays)
                if (Find<NodeDefinition>(index, way.from, "room", warnings) is NodeDefinition from &&
                    Find<NodeDefinition>(index, way.to, "room", warnings) is NodeDefinition to)
                    p.FoundWays.Add((from, to));
            p.TicksPlayed = Math.Max(0L, data.ticksPlayed);
            foreach (var r in data.runHistory)
            {
                // An unknown reason (a damaged file) counts as a collapse, the commonest way a run ends.
                var reason = Enum.IsDefined(typeof(LoopEndReason), r.endReason) ? (LoopEndReason)r.endReason : LoopEndReason.Exhausted;
                var milestones = new List<(ContentAsset, float)>();
                foreach (var m in r.milestones)
                    if (FindMilestone(index, m.id, warnings) is ContentAsset milestone)
                        milestones.Add((milestone, Mathf.Max(0f, m.value)));
                p.RunHistory.Add(new RunRecord(r.loopNumber, Math.Max(0L, r.ticks), Math.Max(0, r.actionsCompleted), reason, milestones));
            }
            RestoreExplored(data, index, p, warnings);
            AddAll(index, data.resourceCapBonus, p.ResourceCapBonus, "resource maximum", warnings, atLeastZero: false);

            p.KeptVitality = Mathf.Max(0f, data.keptVitality);
            foreach (var a in data.expertiseXp)
                if (Enum.IsDefined(typeof(ClaraAttribute), a.attribute))
                    p.AttributeMasteryXp[(ClaraAttribute)a.attribute] = Mathf.Max(0f, a.value);
                else
                    SkippedValue(warnings, "attribute's mastery", a.attribute);
            AddAll(index, data.skillMasteryXp, p.SkillMasteryXp, "skill", warnings, atLeastZero: true);
            AddAll(index, data.skillMasteryXpAtLastRunStart, p.SkillMasteryXpAtLastRunStart, "skill", warnings, atLeastZero: true);
            p.HasLastRunStart = data.hasLastRunStart;
            AddAll(index, data.roomsEntered, p.RoomsEntered, "entered room", warnings);
            AddAll(index, data.roomRuns, p.RoomRuns, "room", warnings, atLeastZero: true);
            AddAll(index, data.journal, p.Journal, "story beat", warnings);
            AddAll(index, data.unreadStories, p.UnreadStories, "unread story beat", warnings);

            return p;
        }

        /// <summary>
        /// Each room's kept search progress. A save from before version 5 kept it too (whole searches:
        /// the ways it had found stay found). A save from before version 14 searched afresh every
        /// run, so a room with a hidden way already found counts as fully searched.
        /// </summary>
        private static void RestoreExplored(SaveData data, ContentIndex index, PersistentState p, List<string> warnings)
        {
            void Keep(NodeDefinition room, float steps) =>
                p.Explored[room] = Math.Min(room.exploresToFill, Math.Max(steps, p.Explored.TryGetValue(room, out float held) ? held : 0f));

            var counted = new HashSet<NodeDefinition>(); // rooms a version 4 save recorded exactly
            foreach (var room in data.exploredRooms)
                if (Find<NodeDefinition>(index, room.id, "explored room", warnings) is NodeDefinition node && node.exploresToFill > 0)
                    Keep(node, room.value);
            foreach (var room in data.explored)
            {
                if (!(Find<NodeDefinition>(index, room.id, "explored room", warnings) is NodeDefinition node) || node.exploresToFill <= 0)
                    continue;
                Keep(node, room.amount);
                counted.Add(node);
                foreach (var way in node.ways)
                    if (way?.to != null && !way.IntoPlannedRoom && way.foundAtExplored > 0 && room.amount * 100 >= way.foundAtExplored * node.exploresToFill)
                        p.FoundWays.Add((node, way.to));
            }
            if (data.loadedVersion > 0 && data.loadedVersion < 14)
                foreach (var (from, _) in p.FoundWays)
                    if (!counted.Contains(from) && from.exploresToFill > 0)
                        Keep(from, from.exploresToFill);
            // Before version 21 a switch that reopens a search had nothing to reset: do what its flip now does
            // (the lab's bar after Roland Takes the Ring). Needs the flipped switches restored first.
            if (data.loadedVersion > 0 && data.loadedVersion < 21)
                foreach (var room in RoomsReopenedBy(p))
                    p.Explored[room] = 0f;
        }

        /// <summary>The rooms whose search a flipped switch reopens (for the version 21 upgrade).</summary>
        private static IEnumerable<NodeDefinition> RoomsReopenedBy(PersistentState p)
        {
            foreach (var @switch in p.FlippedSwitches)
                foreach (var room in @switch.reopensSearch)
                    if (room != null)
                        yield return room;
        }

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        /// <summary>A saved milestone: a switch or a room. Anything else (a damaged file) is skipped with a warning.</summary>
        internal static ContentAsset FindMilestone(ContentIndex index, string id, List<string> warnings)
        {
            var found = Find<ContentAsset>(index, id, "milestone", warnings);
            if (found == null || found is SwitchDefinition || found is NodeDefinition)
                return found;
            warnings?.Add($"Skipped a milestone that is neither a switch nor a room ({id}).");
            return null;
        }

        /// <summary>Like Find, but an empty id means "none" and is not a problem.</summary>
        internal static T Optional<T>(ContentIndex index, string id, string what, List<string> warnings) where T : ContentAsset =>
            string.IsNullOrEmpty(id) ? null : Find<T>(index, id, what, warnings);

        /// <summary>A saved number that names a hue or attribute the game no longer has (a damaged file, or content that was removed).</summary>
        internal static void SkippedValue(List<string> warnings, string what, int value) =>
            warnings?.Add($"Skipped a {what} that no longer exists (id {value}).");

        internal static T Find<T>(ContentIndex index, string id, string what, List<string> warnings) where T : ContentAsset
        {
            var found = index.Find<T>(id);
            if (found == null)
                warnings?.Add($"Skipped a {what} that no longer exists ({id}).");
            return found;
        }

        private static void AddAll<T>(ContentIndex index, List<string> ids, ICollection<T> into, string what, List<string> warnings)
            where T : ContentAsset
        {
            foreach (string id in ids)
                if (Find<T>(index, id, what, warnings) is T found)
                    into.Add(found);
        }

        // atLeastZero: for counts that can't be negative, so a damaged file can't make them so.
        private static void AddAll<T>(ContentIndex index, List<SavedAmount> saved, IDictionary<T, int> into, string what,
            List<string> warnings, bool atLeastZero) where T : ContentAsset
        {
            foreach (var entry in saved)
                if (Find<T>(index, entry.id, what, warnings) is T found)
                    into[found] = atLeastZero ? Math.Max(0, entry.amount) : entry.amount;
        }

        private static void AddAll<T>(ContentIndex index, List<SavedValue> saved, IDictionary<T, float> into, string what,
            List<string> warnings, bool atLeastZero) where T : ContentAsset
        {
            foreach (var entry in saved)
                if (Find<T>(index, entry.id, what, warnings) is T found)
                    into[found] = atLeastZero ? Math.Max(0f, entry.value) : entry.value;
        }
    }
}
