using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Moving queue entries and whole stops (dragging in the queue drawer), and the rule that refuses
    // a move that breaks the route.
    public partial class Simulation
    {
        // Reused on every check, as the drawer asks again each time the pointer moves over a gap.
        private readonly List<QueueEntry> _trialOrder = new List<QueueEntry>();
        private readonly HashSet<QueueEntry> _goesToPlanBefore = new HashSet<QueueEntry>();
        private readonly List<QueueStop> _moveStops = new List<QueueStop>();

        /// <summary>
        /// Whether the entry at <paramref name="from"/> can go into the gap <paramref name="to"/> (0 is
        /// above the first entry, Queue.Count below the last). If not, <paramref name="reason"/> says why.
        /// </summary>
        public bool CanMoveEntry(int from, int to, out string reason)
        {
            CheckRange(from, 1, to);
            reason = RouteBreakAfter(from, 1, to);
            return reason == null;
        }

        /// <summary>
        /// Moves the entry at <paramref name="from"/> into the gap <paramref name="to"/>, unless the
        /// move is refused (see CanMoveEntry). Like To top, a running action that no longer comes
        /// first waits with its progress kept. Returns whether it was allowed.
        /// </summary>
        public bool MoveEntry(int from, int to)
        {
            if (!CanMoveEntry(from, to, out _))
                return false;
            ApplyMove(from, 1, to);
            return true;
        }

        /// <summary>
        /// Whether stop <paramref name="stop"/> (its trip and everything done there; an index into
        /// QueueStops) can go before stop <paramref name="beforeStop"/> (the number of stops for the end).
        /// The first stop (where the queue starts) has no trip and never moves, and nothing goes before
        /// it (its actions would quietly move to the other room): false, with no reason.
        /// </summary>
        public bool CanMoveStop(int stop, int beforeStop, out string reason)
        {
            reason = null;
            if (!StopMoveRange(stop, beforeStop, out int first, out int count, out int to))
                return false;
            reason = RouteBreakAfter(first, count, to);
            return reason == null;
        }

        /// <summary>Moves a whole stop (see CanMoveStop), unless refused. Returns whether it was allowed.</summary>
        public bool MoveStop(int stop, int beforeStop)
        {
            if (!CanMoveStop(stop, beforeStop, out _))
                return false;
            StopMoveRange(stop, beforeStop, out int first, out int count, out int to);
            ApplyMove(first, count, to);
            return true;
        }

        // ---------- The rule ----------

        /// <summary>
        /// Why moving <paramref name="count"/> entries from <paramref name="first"/> into gap
        /// <paramref name="to"/> would break the route, or null if it wouldn't. It breaks if a trip
        /// that would have been made is skipped, or an action that could be done in its stop's room
        /// ends up in a room that doesn't offer it. Entries already broken don't count: a move can't
        /// make them worse.
        /// </summary>
        private string RouteBreakAfter(int first, int count, int to)
        {
            var entries = Queue.Entries;
            _goesToPlanBefore.Clear();
            var room = PlanStart;
            foreach (var entry in entries)
            {
                if (WhyNotToPlan(room, entry) == null)
                    _goesToPlanBefore.Add(entry);
                room = NodeAfterEntry(room, entry);
            }

            _trialOrder.Clear();
            _trialOrder.AddRange(entries);
            MoveRange(_trialOrder, first, count, to);

            room = PlanStart;
            foreach (var entry in _trialOrder)
            {
                if (_goesToPlanBefore.Contains(entry))
                {
                    string reason = WhyNotToPlan(room, entry);
                    if (reason != null)
                        return reason;
                }
                room = NodeAfterEntry(room, entry);
            }
            return null;
        }

        /// <summary>
        /// Why an entry done from <paramref name="room"/> wouldn't go as planned (in the run's own
        /// words), or null if it would.
        /// </summary>
        private string WhyNotToPlan(NodeDefinition room, QueueEntry entry)
        {
            if (entry.Destination != null)
                return NodeAfterEntry(room, entry) != room ? null : CantTravel(room, entry.Destination);
            return IsAvailableAt(entry.Task, room) ? null : NotHereReason(room);
        }

        // ---------- Doing the move ----------

        private void ApplyMove(int first, int count, int to)
        {
            if (to >= first && to <= first + count)
                return; // it's already there
            MoveRange(Queue.Entries, first, count, to);
            // Something else now comes first: what she was doing waits, with its progress kept (as To top).
            if (!Loop.IsOver && Loop.RunningEntry != null && Loop.RunningEntry != Queue.Top)
                SuspendCurrentTask();
            QueueChanged?.Invoke();
        }

        /// <summary>Moves a run of entries into a gap counted in the list as it was before the move.</summary>
        private static void MoveRange(List<QueueEntry> list, int first, int count, int to)
        {
            if (to >= first && to <= first + count)
                return;
            var moving = list.GetRange(first, count);
            list.RemoveRange(first, count);
            list.InsertRange(to > first ? to - count : to, moving);
        }

        /// <summary>The entries a stop move would take, and the gap they'd go into. False for moving the first stop, or before it.</summary>
        private bool StopMoveRange(int stop, int beforeStop, out int first, out int count, out int to)
        {
            QueueStops(_moveStops);
            if (stop < 0 || stop >= _moveStops.Count)
                throw new ArgumentOutOfRangeException(nameof(stop), $"There are {_moveStops.Count} stops; no stop {stop}.");
            if (beforeStop < 0 || beforeStop > _moveStops.Count)
                throw new ArgumentOutOfRangeException(nameof(beforeStop), $"There are {_moveStops.Count} stops; no gap before stop {beforeStop}.");

            first = _moveStops[stop].FirstEntry;
            count = _moveStops[stop].EntryCount;
            to = beforeStop == _moveStops.Count ? Queue.Count : _moveStops[beforeStop].FirstEntry;
            return stop > 0 && beforeStop > 0;
        }

        private void CheckRange(int first, int count, int to)
        {
            if (first < 0 || first + count > Queue.Count)
                throw new ArgumentOutOfRangeException(nameof(first), $"The queue has {Queue.Count} entries; no entry {first}.");
            if (to < 0 || to > Queue.Count)
                throw new ArgumentOutOfRangeException(nameof(to), $"The queue has {Queue.Count} entries; no gap {to}.");
        }
    }
}
