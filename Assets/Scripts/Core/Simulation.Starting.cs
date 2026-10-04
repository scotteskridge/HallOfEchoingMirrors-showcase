using System;

namespace HallOfEchoingMirrors.Core
{
    // Starting the queue's top entry (dropping or supplying what can't start), and stopping or suspending the running one.
    public partial class Simulation
    {
        /// <summary>
        /// Starts the top entry of the queue. Entries that are done are dropped; entries that can't
        /// start are dropped with a reason (TaskSkipped). With nothing left, Clara waits, and
        /// QueueRanOut fires once so the game can pause for the player.
        /// </summary>
        private bool TryStartNextTask()
        {
            var entries = Loop.Queue.Entries;
            int suppliersQueued = 0; // a guard: content can never make this loop for ever
            while (entries.Count > 0)
            {
                var entry = entries[0];
                if (IsDone(entry))
                {
                    entries.RemoveAt(0);
                    QueueChanged?.Invoke();
                    continue;
                }

                // Placeholder rule: a task that can't start is dropped rather than waited on, so
                // one blocked entry never holds up the rest of the queue. Unless it only lacks an
                // item that something here can supply: then that goes on top first. A full Carry
                // is never supplied: it has nothing left to do.
                string reason = WhyEntryCantStart(entry);
                if (reason != null)
                {
                    if (!IsFullCarry(entry) && suppliersQueued++ < 2 * MaxSupplyDepth && QueueSupplier(entry))
                        continue;
                    entries.RemoveAt(0);
                    TaskSkipped?.Invoke(entry.Task, reason);
                    QueueChanged?.Invoke();
                    continue;
                }

                Loop.QueueRanOutNotified = false;
                StartTask(entry);
                return true;
            }

            if (!Loop.QueueRanOutNotified)
            {
                Loop.QueueRanOutNotified = true;
                QueueRanOut?.Invoke();
            }
            return false;
        }

        /// <summary>Stops the running task but keeps its progress on its queue entry (Play pushed it down).</summary>
        private void SuspendCurrentTask()
        {
            var entry = Loop.RunningEntry;
            // A search's progress is the room's kept bar, not the entry's: it carries on from there.
            if (entry != null && Loop.CurrentTask != null && !IsSearching(Loop.CurrentTask))
            {
                entry.SavedWork = Loop.CurrentTaskWorkDone;
                entry.SavedAt = Loop.CurrentNode;
                entry.SavedFraction = Loop.CurrentTaskProgress;
            }
            ClearCurrentTask();
        }

        private void ClearCurrentTask()
        {
            Loop.CurrentTask = null;
            Loop.RunningEntry = null;
            Loop.CurrentDestination = null;
            Loop.CurrentTaskWorkDone = 0f;
            Loop.CurrentTaskWorkNeeded = 0;
            Loop.CurrentTaskCosts.Clear();
        }

        private void StartTask(QueueEntry entry)
        {
            var task = entry.Task;
            var destination = entry.Destination;
            var price = PriceOf(task, Loop.CurrentNode, destination);

            Loop.RunningEntry = entry;
            Loop.CurrentTask = task;
            Loop.CurrentDestination = destination;
            Loop.CurrentTaskWorkDone = 0f;
            // A search is one go at what's left of the room's bar (its price already counts that).
            bool searching = SearchesWhatIsLeft(task, Loop.CurrentNode);
            Loop.CurrentTaskWorkNeeded = searching
                ? Math.Max(1, (int)MathF.Round(SearchWorkLeft(Loop.CurrentNode)))
                : WorkNeededFor(task, Loop.CurrentNode, price.TimeTimes);

            Loop.CurrentTaskCosts.Clear();
            for (int i = 0; i < price.Costs.Count; i++)
            {
                var (_, pool, amount) = price.Costs[i];
                Loop.CurrentTaskCosts.Add(new LoopState.RunningCost
                {
                    Pool = pool,
                    PerWork = amount / Loop.CurrentTaskWorkNeeded,
                    Remaining = amount,
                    IsCharge = i == price.ChargeIndex,
                });
            }

            // Back on top after Play pushed it down: carry on where she left off (if she's still in
            // the same room), with only the rest of its cost still to pay.
            if (entry.SavedWork > 0f)
            {
                if (entry.SavedAt == Loop.CurrentNode && !searching) // (a search reads the kept bar)
                {
                    float fraction = MathF.Min(entry.SavedWork / Loop.CurrentTaskWorkNeeded, 0.999f);
                    Loop.CurrentTaskWorkDone = fraction * Loop.CurrentTaskWorkNeeded;
                    foreach (var cost in Loop.CurrentTaskCosts)
                        cost.Remaining *= 1f - fraction;
                }
                entry.SavedWork = 0f;
                entry.SavedAt = null;
                entry.SavedFraction = 0f;
            }

            TaskStarted?.Invoke(task);
        }
    }
}
