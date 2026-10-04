using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Supplying a blocked queue entry: what it lacks, and which action in the room gives it.
    public partial class Simulation
    {
        // ---------- Supplying a blocked entry ----------

        /// <summary>How many suppliers deep the queue will go (a supplier for a supplier...).</summary>
        private const int MaxSupplyDepth = 3;

        /// <summary>
        /// The top entry can't start. If all it lacks is an item that an action in this room gives
        /// (what's on the floor here already counts), that action goes on top and repeats until
        /// there's enough to finish the entry, or no room for more.
        /// Returns whether one was put on top.
        /// </summary>
        private bool QueueSupplier(QueueEntry blocked)
        {
            if (blocked.Destination != null || blocked.SupplyDepth >= MaxSupplyDepth)
                return false;
            var need = MissingNeed(blocked.Task);
            if (need == null || !CanStartIgnoringNeeds(blocked.Task, null, out _))
                return false;
            var supplier = SupplierFor(need.resource, blocked.Task, blocked.SupplyDepth + 1);
            if (supplier == null)
                return false;

            var entry = new QueueEntry(supplier)
            {
                SuppliesFor = blocked,
                Supplies = need.resource,
                SupplyTarget = EnoughFor(blocked, need),
            };

            // If she was already part-way through this very action in this room (Play pushed it down
            // to start the blocked one), the supplier carries on from there: a fresh one would waste
            // that progress. Only the progress moves; the player's own entry stays queued as it was.
            var pushedDown = Loop.Queue.Entries.Find(e => e != blocked && e.Destination == null && e.Task == supplier &&
                                                          e.SavedWork > 0f && e.SavedAt == Loop.CurrentNode);
            if (pushedDown != null)
            {
                entry.SavedWork = pushedDown.SavedWork;
                entry.SavedAt = pushedDown.SavedAt;
                entry.SavedFraction = pushedDown.SavedFraction;
                pushedDown.SavedWork = 0f;
                pushedDown.SavedAt = null;
                pushedDown.SavedFraction = 0f;
            }
            Loop.Queue.Entries.Insert(0, entry);
            SupplyQueued?.Invoke(supplier, blocked.Task, need.resource);
            QueueChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// The first thing a task needs that she doesn't have enough of, or null. What lies on the
        /// floor where she is counts: she can use what's at her feet.
        /// </summary>
        private ResourceAmount MissingNeed(TaskDefinition task) => MissingNeedAt(task, Loop.CurrentNode);

        /// <summary>As MissingNeed, but counting the floor of another room (where she'd do it).</summary>
        private ResourceAmount MissingNeedAt(TaskDefinition task, NodeDefinition room) => MissingFrom(task.needs, room);

        /// <summary>
        /// The first of these needs she can't meet standing in this room, or null: what she holds
        /// plus what lies on that room's floor. One rule for an action's needs and a way's.
        /// </summary>
        private ResourceAmount MissingFrom(IReadOnlyList<ResourceAmount> needs, NodeDefinition room)
        {
            foreach (var need in needs)
                if (need.resource != null && AmountOf(need.resource) + OnFloor(room, need.resource) < need.amount)
                    return need;
            return null;
        }

        /// <summary>The refusal text for a need she can't meet: one wording for actions, offers and ways.</summary>
        internal static string NeedsReason(ResourceAmount need) =>
            GameText.Get("reasons.needs", ("amount", need.amount), ("item", need.resource.DisplayName));

        /// <summary>Whether all that stops an entry is an item something here can supply.</summary>
        private bool CanBeSupplied(QueueEntry entry)
        {
            if (entry.Destination != null)
                return false;
            var need = MissingNeed(entry.Task);
            return need != null && CanStartIgnoringNeeds(entry.Task, null, out _) &&
                   SupplierFor(need.resource, entry.Task, 1) != null;
        }

        /// <summary>
        /// No longer offered this run: a once-a-run task already done, or one that only gives
        /// one-of-a-kind objects that already exist (Instantiate Roland's ring, once she has it), kept
        /// things she has all of (a one-time reward, once earned: never offered again), or a plain
        /// count already at its maximum (every candle in a hall already lit).
        /// </summary>
        public bool IsDoneForThisRun(TaskDefinition task) =>
            (task.oncePerRun && Loop.CompletedTasks.Contains(task)) || GivesOnlyWhatExists(task);

        private bool GivesOnlyWhatExists(TaskDefinition task)
        {
            bool givesAny = false;
            foreach (var give in task.gives)
            {
                if (!give.IsReal)
                    continue;
                bool exists = IsOneOfAKind(give.resource)
                    ? CopiesThisRun(give.resource) > 0
                    : (give.resource.IsKept || IsPlainCount(give.resource)) && RoomFor(give.resource) <= 0;
                if (!exists)
                    return false;
                givesAny = true;
            }
            return givesAny;
        }

        /// <summary>
        /// What here can supply this item: an action here that gives it and has room to (what lies
        /// on the floor here already counts as hers to use). It must be able to start, or be missing
        /// only something that can itself be supplied (within the depth limit). Null if nothing can.
        /// </summary>
        private TaskDefinition SupplierFor(ResourceDefinition item, TaskDefinition forTask, int depth)
        {
            if (depth > MaxSupplyDepth)
                return null;

            foreach (var task in TasksAt(Loop.CurrentNode))
            {
                if (task == forTask || task.picksUp != null || !Gives(task, item))
                    continue;
                if (RoomFor(item) <= 0 && !CanPushIn(task, item))
                    continue;
                if (CanStart(task, null, out _))
                    return task;
                var itsNeed = MissingNeed(task);
                if (itsNeed != null && itsNeed.resource != item && CanStartIgnoringNeeds(task, null, out _) &&
                    SupplierFor(itsNeed.resource, task, depth + 1) != null)
                    return task;
            }
            return null;
        }

        /// <summary>
        /// How many of an item are enough to finish a blocked entry: what one go needs, plus what
        /// each further go uses up (a tool it only needs is wanted once). An entry that repeats for
        /// ever wants as many as she can hold.
        /// </summary>
        private int EnoughFor(QueueEntry blocked, ResourceAmount need)
        {
            int usedEachGo = 0;
            foreach (var take in blocked.Task.takes)
                if (take.resource == need.resource)
                    usedEachGo += take.amount;
            if (usedEachGo == 0)
                return need.amount;

            int goes = GoesLeft(blocked);
            if (goes == int.MaxValue)
                return int.MaxValue;
            return (int)System.Math.Min(int.MaxValue, need.amount + (long)(goes - 1) * usedEachGo);
        }

        /// <summary>
        /// How many more goes an entry has: its repeat limit, once for a one-off, for a supplier
        /// what's still short of its own target, else until what it gives is full.
        /// </summary>
        private int GoesLeft(QueueEntry entry)
        {
            var task = entry.Task;
            if (entry.TimesLeft.HasValue)
                return System.Math.Max(1, entry.TimesLeft.Value);
            if (task.singleAction || task.oncePerRun || task.picksUp != null)
                return 1;
            if (entry.Supplies != null && entry.SupplyTarget != int.MaxValue)
            {
                int each = 0;
                foreach (var give in task.gives)
                    if (give.resource == entry.Supplies)
                        each += give.amount;
                int shortBy = entry.SupplyTarget - HeldOrHere(entry.Supplies);
                return each > 0 ? System.Math.Max(1, (shortBy + each - 1) / each) : 1;
            }
            return GoesUntilGivesAreFull(task);
        }

        /// <summary>How many more goes of a task until she can hold no more of what it gives (int.MaxValue if never).</summary>
        private int GoesUntilGivesAreFull(TaskDefinition task)
        {
            int goes = -1;
            foreach (var give in task.gives)
            {
                if (!give.IsReal)
                    continue;
                long room = SpaceFor(give.resource);
                if (room >= int.MaxValue)
                    return int.MaxValue; // one thing it gives never fills: it would go on for ever
                int forThis = (int)((room + give.amount - 1) / give.amount);
                goes = System.Math.Max(goes, forThis);
            }
            return goes < 0 ? int.MaxValue : goes;
        }
    }
}
