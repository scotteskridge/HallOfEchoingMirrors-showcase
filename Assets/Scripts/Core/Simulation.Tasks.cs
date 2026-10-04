using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // The action queue: adding (Schedule, Play, Carry), removing and clearing. In a room: QueueInRoom.cs; starting: Starting.cs; why not: CanStart.cs; when done: Repeating.cs.
    public partial class Simulation
    {
        /// <summary>What she's doing now, e.g. "Travel to the junction". Null when idle.</summary>
        public string CurrentActionName =>
            Loop.CurrentTask != null ? QueueEntry.NameOf(Loop.CurrentTask, Loop.CurrentDestination) : null;

        /// <summary>
        /// The actions queued for this run: the running one on top. Between runs (after one has
        /// ended), the queue being made for the next run.
        /// </summary>
        public ActionQueue Queue => Loop.IsOver ? _nextQueue : Loop.Queue;

        // ---------- Editing the queue ----------

        private readonly List<QueueStop> _removeStops = new List<QueueStop>();

        /// <summary>
        /// Schedule: adds a task to the bottom of the queue. It repeats until it's done, or at most
        /// <paramref name="times"/> times if given (not offered on screen; for tests and automation).
        /// </summary>
        public void Schedule(TaskDefinition task, int? times = null) => Enqueue(new QueueEntry(task, null, times), atTop: false);

        /// <summary>
        /// Play: puts a task on top of the queue and switches to it now. Whatever she was doing
        /// keeps its progress, for when it's back on top. A task already queued moves up rather than doubling.
        /// </summary>
        public void PlayNow(TaskDefinition task, int? times = null) => Enqueue(new QueueEntry(task, null, times), atTop: true);

        /// <summary>
        /// Carry: like Play (on top, now), but it repeats only until her pockets and containers are
        /// full (it never gathers just to leave things on the floor).
        /// </summary>
        public void CarryNow(TaskDefinition task) => Enqueue(new QueueEntry(task) { CarryOnly = true }, atTop: true);

        /// <summary>
        /// Whether Carry is offered for a task: it makes things she carries (objects, pouches; not plain
        /// counts or kept knowledge), which she wants to take with her rather than leave lying about,
        /// and it isn't a one-time action: not a single action or once-a-run task, and nothing it makes
        /// can only ever be held once (a maximum of 1: Roland's ring, flint and steel, the satchel), since those stop at one anyway.
        /// </summary>
        public bool CanCarry(TaskDefinition task)
        {
            if (task == null || task.singleAction || task.oncePerRun || task.picksUp != null || task.putsDown != null)
                return false;
            foreach (var give in task.gives)
            {
                var item = give.resource;
                // A container adds room to fill, so Carry makes no sense for it (decisions log, 2026-10-01).
                if (give.IsReal && !item.IsKept && !IsPlainCount(item) && ResourceCapOf(item) != 1 && !item.IsContainer)
                    return true;
            }
            return false;
        }

        private void Enqueue(QueueEntry entry, bool atTop, int insertAt = -1)
        {
            if (entry.Task == null)
                throw new ArgumentException("A queue entry needs a task.", nameof(entry));
            var entries = Queue.Entries;
            if (insertAt >= entries.Count)
                insertAt = -1; // the end

            // Between runs the plan holds only rooms known by heart: the room she'd be in when this entry
            // starts, and for a trip the room it enters.
            if (Loop.IsOver)
            {
                var where = PlannedNodeAfter(atTop ? 0 : insertAt >= 0 ? insertAt : entries.Count);
                string notByHeart = WhyNotPlannable(new[] { where, entry.Destination });
                if (notByHeart != null)
                {
                    ActionRefused?.Invoke(entry.Task, entry.Destination, notByHeart);
                    return;
                }
            }
            bool inserting = !atTop && insertAt >= 0;

            // Something that would start now but can't (and can't be supplied) is refused at once, so
            // no time passes on it; queued behind other actions, it may be possible by its turn.
            bool startsNow = Phase == LoopPhase.Running && !Loop.IsOver && (atTop || entries.Count == 0 || insertAt == 0);
            string reason = startsNow ? WhyEntryCantStart(entry) : null;
            if (reason != null && (IsFullCarry(entry) || !CanBeSupplied(entry)))
            {
                // No destination: for a trip this reason already says "to {room}, ..." (WhyEntryCantStart).
                ActionRefused?.Invoke(entry.Task, null, reason);
                return;
            }

            if (atTop)
            {
                int existing = Queue.IndexOf(entry.Task, entry.Destination);
                if (existing >= 0)
                    entries[existing].CarryOnly = entry.CarryOnly; // Carry or Play, whichever was pressed last
                if (existing > 0)
                {
                    var moving = entries[existing];
                    entries.RemoveAt(existing);
                    entries.Insert(0, moving);
                }
                else if (existing < 0)
                {
                    entries.Insert(0, entry);
                }
                // Something else was running: it waits underneath, with its progress kept.
                if (!Loop.IsOver && Loop.RunningEntry != null && Loop.RunningEntry != Queue.Top)
                    SuspendCurrentTask();
            }
            else
            {
                // The same task twice in a row is one entry (it repeats anyway). Trips never merge:
                // a second trip to the same place would start from there.
                int at = inserting ? insertAt : entries.Count;
                var last = at > 0 ? entries[at - 1] : null;
                // Put in front of the same task (Play at a visit's start): one entry, not two in a row.
                var next = inserting && at < entries.Count ? entries[at] : null;
                var same = last != null && last.IsSameAs(entry.Task, null) && last.CarryOnly == entry.CarryOnly ? last :
                    next != null && next.IsSameAs(entry.Task, null) && next.CarryOnly == entry.CarryOnly ? next : null;
                if (same != null && entry.Destination == null)
                    same.TimesLeft = same.TimesLeft.HasValue && entry.TimesLeft.HasValue
                        ? same.TimesLeft + entry.TimesLeft
                        : null;
                else
                    entries.Insert(at, entry);
                // Put in front of what's running: that waits underneath, with its progress kept.
                if (inserting && at == 0 && !Loop.IsOver && Loop.RunningEntry != null && Loop.RunningEntry != Queue.Top)
                    SuspendCurrentTask();
            }

            Loop.QueueRanOutNotified = false;
            QueueChanged?.Invoke();
            ActionQueued?.Invoke();
        }

        /// <summary>Takes an entry out of the queue. If it's the one running, she stops at once (its progress is lost).</summary>
        public void RemoveFromQueue(int index)
        {
            var entries = Queue.Entries;
            if (index < 0 || index >= entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The queue has {entries.Count} entries.");
            if (entries[index] == Loop.RunningEntry)
                ClearCurrentTask();
            entries.RemoveAt(index);
            QueueChanged?.Invoke();
        }

        /// <summary>
        /// Takes out this entry wherever it now sits (an index is stale by the time a click lands). False,
        /// with nothing changed, if it has already left the queue.
        /// </summary>
        public bool RemoveFromQueue(QueueEntry entry)
        {
            int index = Queue.Entries.IndexOf(entry);
            if (index < 0)
                return false;
            RemoveFromQueue(index);
            return true;
        }

        /// <summary>
        /// Takes a whole stop out of the queue: its trip and everything done there (an index into
        /// QueueStops). What follows it joins the stop before, as it would after removing each entry by hand.
        /// </summary>
        public void RemoveStop(int stopIndex)
        {
            QueueStops(_removeStops);
            if (stopIndex < 0 || stopIndex >= _removeStops.Count)
                throw new ArgumentOutOfRangeException(nameof(stopIndex), stopIndex, $"There are {_removeStops.Count} stops.");
            var stop = _removeStops[stopIndex];
            var entries = Queue.Entries;
            int running = Loop.RunningEntry != null ? entries.IndexOf(Loop.RunningEntry) : -1;
            if (running >= stop.FirstEntry && running < stop.FirstEntry + stop.EntryCount)
                ClearCurrentTask(); // she stops at once, as when its row is removed
            entries.RemoveRange(stop.FirstEntry, stop.EntryCount);
            QueueChanged?.Invoke();
        }

        /// <summary>Takes out the whole stop that holds a queue entry (an index into QueueStops is stale by the time a click lands). False if the entry has left the queue.</summary>
        public bool RemoveStopOf(QueueEntry entry)
        {
            QueueStops(_removeStops);
            for (int i = 0; i < _removeStops.Count; i++)
            {
                var stop = _removeStops[i];
                for (int e = stop.FirstEntry; e < stop.FirstEntry + stop.EntryCount; e++)
                    if (Queue.Entries[e] == entry)
                    {
                        RemoveStop(i);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>Removes every entry of a task from the queue (e.g. when a switch locks it).</summary>
        private void RemoveTaskFromQueue(TaskDefinition task)
        {
            var entries = Queue.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Task == task)
                    RemoveFromQueue(i);
        }

        public void ClearQueue()
        {
            if (Queue.Count == 0)
                return;
            if (Loop.RunningEntry != null && Queue.Entries.Contains(Loop.RunningEntry))
                ClearCurrentTask();
            Queue.Entries.Clear();
            QueueChanged?.Invoke();
        }
    }
}
