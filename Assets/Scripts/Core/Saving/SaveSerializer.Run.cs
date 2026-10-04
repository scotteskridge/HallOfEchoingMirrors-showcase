using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>Capturing a run under way, and putting it back (resuming), or the queue held between runs.</summary>
    public static partial class SaveSerializer
    {
        private static List<SavedEntry> SavedQueue(List<QueueEntry> entries)
        {
            var saved = new List<SavedEntry>();
            foreach (var entry in entries)
                saved.Add(new SavedEntry
                {
                    task = IdOf(entry.Task),
                    destination = IdOf(entry.Destination),
                    item = ItemIdOf(entry.Task),
                    limited = entry.TimesLeft.HasValue,
                    timesLeft = entry.TimesLeft ?? 0,
                    timesDone = entry.TimesDone,
                    savedWork = entry.SavedWork,
                    savedFraction = entry.SavedFraction,
                    savedAt = IdOf(entry.SavedAt),
                    suppliesFor = entry.SuppliesFor != null ? entries.IndexOf(entry.SuppliesFor) : -1,
                    supplies = IdOf(entry.Supplies),
                    supplyTarget = entry.SupplyTarget,
                    carryOnly = entry.CarryOnly,
                    byHeart = entry.ByHeart,
                });
            return saved;
        }

        private static SavedRun CaptureRun(LoopState loop)
        {
            var run = new SavedRun
            {
                ticks = loop.TicksElapsed,
                vitality = loop.Vitality.Current,
                vitalityMax = loop.Vitality.Max,
                drainHeldOff = loop.DrainHeldOff,
                drainGrown = loop.DrainGrown,
                moveVitalityPaid = loop.MoveVitalityPaid,
                vitalityLostThisRun = loop.VitalityLostThisRun,
                node = IdOf(loop.CurrentNode),
                task = IdOf(loop.CurrentTask),
                workDone = loop.CurrentTaskWorkDone,
                workNeeded = loop.CurrentTaskWorkNeeded,
                runningEntry = loop.RunningEntry != null ? loop.Queue.Entries.IndexOf(loop.RunningEntry) : -1,
                queueRanOutNotified = loop.QueueRanOutNotified,
            };
            foreach (var pool in loop.Pools)
                run.pools.Add(new SavedHueValue { hue = (int)pool.Hue, value = pool.Current });
            foreach (var cost in loop.CurrentTaskCosts)
                run.costs.Add(new SavedCost
                {
                    hue = cost.Pool == loop.Vitality ? (int)Hue.None : (int)cost.Pool.Hue,
                    perWork = cost.PerWork,
                    remaining = cost.Remaining,
                    charge = cost.IsCharge,
                });

            run.queue = SavedQueue(loop.Queue.Entries);

            foreach (var r in loop.ToolsAndStats) run.toolsAndStats.Add(new SavedAmount { id = r.Key.Id, amount = r.Value });
            foreach (var room in loop.Floor)
                foreach (var pile in room.Value)
                    run.floor.Add(new SavedFloorPile { room = room.Key.Id, item = pile.Key.Id, amount = pile.Value });
            run.hasExploredAtStart = true;
            foreach (var room in loop.ExploredAtStart) run.exploredAtStart.Add(new SavedValue { id = room.Key.Id, value = room.Value });
            foreach (var r in loop.Restorings) run.restorings.Add(new SavedRestoring { item = r.Item.Id, perSecond = r.PerSecond, left = r.Left });
            foreach (var a in loop.AttributeXp) run.attributeXp.Add(new SavedAttributeValue { attribute = (int)a.Key, value = a.Value });
            foreach (var s in loop.SkillXp) run.skillXp.Add(new SavedValue { id = s.Key.Id, value = s.Value });
            foreach (var s in loop.SkillTicks) run.skillTicks.Add(new SavedAmount { id = s.Key.Id, amount = (int)s.Value });
            foreach (var t in loop.TaskChargePaid) run.taskChargePaid.Add(new SavedValue { id = t.Key.Id, value = t.Value });
            run.otherTicks = loop.OtherTicks;

            foreach (var t in loop.CompletedTasks) run.completedActions.Add(ActionOf(t));
            foreach (var step in loop.Steps)
                run.steps.Add(new SavedStep
                {
                    task = step.Task.Id, item = ItemIdOf(step.Task), room = IdOf(step.Room),
                    destination = IdOf(step.Destination), supply = step.IsSupply,
                });
            foreach (var s in loop.SwitchesFlipped) run.switchesFlipped.Add(s.Id);
            foreach (var (milestone, seconds) in loop.Milestones) run.milestones.Add(new SavedValue { id = milestone.Id, value = seconds });
            foreach (var room in loop.RoomsFirstEntered) run.roomsFirstEntered.Add(room.Id);
            foreach (var a in loop.AttributeMasteryAtStart) run.attributeMasteryAtStart.Add(new SavedAttributeValue { attribute = (int)a.Key, value = a.Value });
            foreach (var s in loop.SkillMasteryAtStart) run.skillMasteryAtStart.Add(new SavedValue { id = s.Key.Id, value = s.Value });
            foreach (var r in loop.KeptAtStart) run.keptAtStart.Add(new SavedAmount { id = r.Key.Id, amount = r.Value });
            return run;
        }

        /// <summary>
        /// Makes the simulation a save describes: its permanent state, the run under way (paused where
        /// it was) or else the queue held between runs. The one way to load, so the two can't drift apart.
        /// </summary>
        /// <param name="resumed">Whether a run was under way in the save.</param>
        public static Simulation Load(SaveData data, ContentIndex index, LoopSettings settings, int ticksPerSecond,
            GameContent content, List<string> warnings, out bool resumed)
        {
            var sim = new Simulation(settings, ticksPerSecond, content, Restore(data, index, warnings));
            if (data != null && data.hasLastRun && data.lastRun != null)
                sim.RestoreLastRun(RunReportSaving.Restore(data.lastRun, sim, index, ticksPerSecond, warnings));
            resumed = ResumeRun(sim, data, index, warnings);
            RestoreNextQueue(sim, data, index, warnings);
            return sim;
        }

        /// <summary>
        /// Picks up the run a save was made in, if it had one: the simulation (made from the same
        /// save's permanent state) carries on exactly where it was. Returns whether there was one.
        /// </summary>
        private static bool ResumeRun(Simulation sim, SaveData data, ContentIndex index, List<string> warnings)
        {
            if (data == null || !data.hasRun || data.run == null)
                return false;
            sim.ResumeRun(loop => FillRun(sim, loop, data.run, data.loadedVersion, index, warnings));
            return true;
        }

        /// <summary>
        /// Puts back the queue a save held between runs (what carried over from the last run, and
        /// anything the player added). Does nothing for a save with a run under way.
        /// </summary>
        private static void RestoreNextQueue(Simulation sim, SaveData data, ContentIndex index, List<string> warnings)
        {
            if (data == null || data.hasRun || data.nextQueue.Count == 0)
                return;
            sim.RestoreNextQueue(queue => FillQueue(sim, queue, data.nextQueue, index, warnings));
        }

        /// <summary>Puts back a saved queue's entries and which entry each supplier is for. Returns the entries by saved index (null for one that is gone).</summary>
        private static List<QueueEntry> FillQueue(Simulation sim, ActionQueue queue, List<SavedEntry> saved, ContentIndex index, List<string> warnings)
        {
            var restored = new List<QueueEntry>();
            foreach (var entrySaved in saved)
            {
                var task = TaskFor(sim, index, entrySaved.task, entrySaved.item, "queued task", warnings);
                var entry = task != null
                    ? new QueueEntry(task, Optional<NodeDefinition>(index, entrySaved.destination, "room", warnings))
                    : null;
                if (entry != null)
                {
                    entry.TimesLeft = entrySaved.limited ? entrySaved.timesLeft : (int?)null;
                    entry.TimesDone = entrySaved.timesDone;
                    entry.SavedWork = entrySaved.savedWork;
                    entry.SavedFraction = entrySaved.savedFraction;
                    entry.SavedAt = Optional<NodeDefinition>(index, entrySaved.savedAt, "room", warnings);
                    entry.Supplies = Optional<ResourceDefinition>(index, entrySaved.supplies, "item", warnings);
                    entry.SupplyTarget = entrySaved.supplyTarget;
                    entry.CarryOnly = entrySaved.carryOnly;
                    entry.ByHeart = entrySaved.byHeart;
                    queue.Entries.Add(entry);
                }
                restored.Add(entry); // keeps indexes lined up with the save, gaps and all
            }
            for (int i = 0; i < saved.Count; i++)
            {
                int target = saved[i].suppliesFor;
                if (restored[i] != null && target >= 0 && target < restored.Count)
                    restored[i].SuppliesFor = restored[target];
            }
            return restored;
        }

        /// <summary>Puts a pool at a saved amount, up or down (never past its maximum).</summary>
        private static void SetTo(Pool pool, float amount)
        {
            float change = amount - pool.Current;
            if (change > 0f)
                pool.Fill(change);
            else
                pool.Drain(-change);
        }

        private static void FillRun(Simulation sim, LoopState loop, SavedRun run, int loadedVersion, ContentIndex index, List<string> warnings)
        {
            loop.TicksElapsed = Math.Max(0, run.ticks);
            loop.Vitality.SetMax(run.vitalityMax);
            SetTo(loop.Vitality, run.vitality);
            // Only the pools she had in that run (one unlocked since appears next run, as ever).
            loop.Pools.RemoveAll(pool => !run.pools.Exists(p => p.hue == (int)pool.Hue));
            foreach (var saved in run.pools)
                if (loop.FindPool((Hue)saved.hue) is Pool pool)
                    SetTo(pool, saved.value);
                else
                    SkippedValue(warnings, "pool in the run", saved.hue);
            loop.DrainHeldOff = run.drainHeldOff;
            loop.DrainGrown = run.drainGrown;

            if (Optional<NodeDefinition>(index, run.node, "room", warnings) is NodeDefinition node)
                loop.CurrentNode = node;

            AddAll(index, run.toolsAndStats, loop.ToolsAndStats, "item", warnings, atLeastZero: true);
            foreach (var pile in run.floor)
                if (Find<NodeDefinition>(index, pile.room, "room", warnings) is NodeDefinition room &&
                    Find<ResourceDefinition>(index, pile.item, "item", warnings) is ResourceDefinition item)
                {
                    if (!loop.Floor.TryGetValue(room, out var piles))
                        loop.Floor[room] = piles = new Dictionary<ResourceDefinition, int>();
                    piles[item] = Math.Max(0, pile.amount);
                }
            // (A run saved before version 14 didn't record it: the fresh run's own snapshot stands.)
            if (run.hasExploredAtStart)
            {
                loop.ExploredAtStart.Clear();
                AddAll(index, run.exploredAtStart, loop.ExploredAtStart, "room", warnings, atLeastZero: true);
                // The same upgrade as Restore's (version 21), for this run's snapshot: a room a flipped switch
                // reopened began this run at 0, or its Summary would hide the search done in it.
                if (loadedVersion > 0 && loadedVersion < 21)
                    foreach (var room in RoomsReopenedBy(sim.Persistent))
                        if (loop.ExploredAtStart.ContainsKey(room))
                            loop.ExploredAtStart[room] = 0f;
            }
            foreach (var r in run.restorings)
                if (Find<ResourceDefinition>(index, r.item, "item", warnings) is ResourceDefinition item)
                    loop.Restorings.Add(new LoopState.Restoring { Item = item, PerSecond = r.perSecond, Left = r.left });
            foreach (var a in run.attributeXp)
                if (Enum.IsDefined(typeof(ClaraAttribute), a.attribute))
                    loop.AttributeXp[(ClaraAttribute)a.attribute] = Math.Max(0f, a.value);
                else
                    SkippedValue(warnings, "attribute's XP in the run", a.attribute);
            AddAll(index, run.skillXp, loop.SkillXp, "skill", warnings, atLeastZero: true);
            foreach (var s in run.skillTicks)
                if (Find<SkillDefinition>(index, s.id, "skill", warnings) is SkillDefinition timed)
                    loop.SkillTicks[timed] = Math.Max(0, s.amount);
            loop.OtherTicks = Math.Max(0L, run.otherTicks);
            loop.MoveVitalityPaid = Math.Max(0f, run.moveVitalityPaid);
            AddAll(index, run.taskChargePaid, loop.TaskChargePaid, "task", warnings, atLeastZero: true);
            loop.VitalityLostThisRun = Math.Max(0f, run.vitalityLostThisRun);

            foreach (var done in run.completedActions)
                if (TaskFor(sim, index, done.task, done.item, "task", warnings) is TaskDefinition task)
                    loop.CompletedTasks.Add(task);
            foreach (var done in run.steps)
                if (TaskFor(sim, index, done.task, done.item, "task", warnings) is TaskDefinition task)
                    loop.Steps.Add(new CompletedStep(task, Optional<NodeDefinition>(index, done.room, "room", warnings),
                        Optional<NodeDefinition>(index, done.destination, "room", warnings), done.supply));
            AddAll(index, run.switchesFlipped, loop.SwitchesFlipped, "switch", warnings);
            foreach (var m in run.milestones)
                if (FindMilestone(index, m.id, warnings) is ContentAsset milestone)
                    loop.Milestones.Add((milestone, m.value));
            AddAll(index, run.roomsFirstEntered, loop.RoomsFirstEntered, "room", warnings);
            // What she had as the run began (the start values a new run made are replaced by the saved ones).
            loop.AttributeMasteryAtStart.Clear();
            foreach (var a in run.attributeMasteryAtStart)
                if (Enum.IsDefined(typeof(ClaraAttribute), a.attribute))
                    loop.AttributeMasteryAtStart[(ClaraAttribute)a.attribute] = a.value;
                else
                    SkippedValue(warnings, "attribute's mastery at the run's start", a.attribute);
            loop.SkillMasteryAtStart.Clear();
            AddAll(index, run.skillMasteryAtStart, loop.SkillMasteryAtStart, "skill", warnings, atLeastZero: false);
            loop.KeptAtStart.Clear();
            AddAll(index, run.keptAtStart, loop.KeptAtStart, "item", warnings, atLeastZero: false);

            var restored = FillQueue(sim, loop.Queue, run.queue, index, warnings);
            loop.QueueRanOutNotified = run.queueRanOutNotified;

            // The action under way, with its progress and what it still has to spend.
            var current = Optional<TaskDefinition>(index, run.task, "task", warnings);
            if (current != null && run.runningEntry >= 0 && run.runningEntry < restored.Count && restored[run.runningEntry] != null)
            {
                loop.RunningEntry = restored[run.runningEntry];
                loop.CurrentTask = loop.RunningEntry.Task; // the same task, as its pick-up for an item if it's one
                loop.CurrentDestination = loop.RunningEntry.Destination; // what the entry was started with
                loop.CurrentTaskWorkNeeded = Math.Max(1, run.workNeeded);
                loop.CurrentTaskWorkDone = Math.Max(0f, run.workDone);
                foreach (var cost in run.costs)
                {
                    var pool = cost.hue == (int)Hue.None ? loop.Vitality : loop.FindPool((Hue)cost.hue);
                    if (pool != null)
                        loop.CurrentTaskCosts.Add(new LoopState.RunningCost { Pool = pool, PerWork = cost.perWork, Remaining = cost.remaining, IsCharge = cost.charge });
                    else // without it the action under way would be free
                        warnings?.Add($"Dropped a cost of the action under way ({IdOf(current)}): its pool (hue {cost.hue}) no longer exists, so the action is free.");
                }
            }
            else if (current != null)
                warnings?.Add($"The action under way ({IdOf(current)}) couldn't be picked up again: its queue entry is gone.");
        }
    }
}
