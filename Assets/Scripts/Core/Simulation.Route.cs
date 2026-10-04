using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // The queue read as a route: the stops it makes, and how long the running action has left.
    public partial class Simulation
    {
        /// <summary>
        /// Fills <paramref name="into"/> with the queue's stops, in order: where the queue starts
        /// (where she is; the start room between runs), then one per trip she'll actually make. A trip
        /// that can't be made from where she'd be starts no stop: it stays in the stop it falls in.
        /// </summary>
        public void QueueStops(List<QueueStop> into)
        {
            into.Clear();
            var entries = Queue.Entries;
            var room = PlanStart;
            int first = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var next = NodeAfterEntry(room, entries[i]);
                if (next == room)
                    continue;
                into.Add(MakeStop(into, entries, room, first, i - first));
                room = next;
                first = i;
            }
            into.Add(MakeStop(into, entries, room, first, entries.Count - first));
            MarkStopsThatCarry(into);
        }

        /// <summary>How many trips she has made this run (every one pays Travel's growing charge; for the header and the Summary, which count trips, not uses of Travel).</summary>
        public int MovesThisRun
        {
            get
            {
                int moves = 0;
                foreach (var step in Loop.Steps)
                    if (step.Destination != null)
                        moves++;
                return moves;
            }
        }

        /// <summary>
        /// Fills <paramref name="into"/> with the vitality each queue entry's trip will charge, by queue
        /// index, in one walk along the plan: the trips before it count as made (each raises the next
        /// one's charge), with the rooms as they are now. 0 for anything that isn't a trip she'll make,
        /// or when Travel has no charge.
        /// Placeholder rule (decisions log 2026-09-30): what she holds is read where the queue starts
        /// and isn't projected: pick-ups, gives and put-downs by earlier entries are not predicted, so
        /// working out what she'll hold then is part of the player's puzzle. Between runs that is only
        /// what she keeps and has packed, the same start as the plan warnings.
        /// </summary>
        public void PlannedTripCharges(List<float> into)
        {
            into.Clear();
            var entries = Queue.Entries;
            bool charged = TravelVerb != null && TravelVerb.escalatingCharge > 0f;
            int uses = charged ? UsesThisRun(TravelVerb) : 0;
            float heldCost = charged ? HeldModifierFor(TravelVerb, atPlanStart: true).cost : 1f;
            var room = PlanStart;
            foreach (var entry in entries)
            {
                var next = NodeAfterEntry(room, entry);
                bool makesTrip = charged && entry.Destination != null && next != room;
                into.Add(makesTrip ? EscalatingChargeAfter(TravelVerb, uses) * TripModifier(room, entry.Destination).cost * heldCost : 0f);
                if (makesTrip)
                    uses++;
                room = next;
            }
        }

        /// <summary>How many of an item she holds where the queue starts: her pockets now during a run; between runs, what she keeps, or has packed from the stash.</summary>
        internal int HeldWhenPlanStarts(ResourceDefinition item)
        {
            if (RunUnderWay || item.IsKept)
                return AmountOf(item);
            return IsPacked(item) ? StashOf(item) : 0;
        }

        /// <summary>Whether the queue entry at <paramref name="index"/> is a trip she'll actually make, as planned.</summary>
        public bool WillTravel(int index)
        {
            if (index < 0 || index >= Queue.Count || Queue.Entries[index].Destination == null)
                return false;
            var from = PlannedNodeAfter(index);
            return NodeAfterEntry(from, Queue.Entries[index]) != from;
        }

        /// <summary>
        /// Why the queued trip at <paramref name="index"/> won't be made as planned (e.g. "she's already
        /// there"), in the words the run uses when it skips it. Null for a trip she'll make, or not a trip.
        /// </summary>
        public string SkippedTripReason(int index)
        {
            if (index < 0 || index >= Queue.Count || Queue.Entries[index].Destination == null || WillTravel(index))
                return null;
            return CantTravel(PlannedNodeAfter(index), Queue.Entries[index].Destination);
        }

        /// <summary>Seconds of game time until the running action is done, at her speed now. 0 when nothing is running.</summary>
        public float CurrentTimeLeftSeconds
        {
            get
            {
                if (Loop.IsOver || Loop.CurrentTask == null)
                    return 0f;
                float workLeft = WorkLeftOnCurrentTask();
                return workLeft / CurrentSpeed / _ticksPerSecond;
            }
        }

        private static QueueStop MakeStop(List<QueueStop> earlier, List<QueueEntry> entries, NodeDefinition room, int first, int count)
        {
            bool isReturn = false;
            foreach (var stop in earlier)
                isReturn |= stop.Room == room;
            bool byHeart = count > 0;
            for (int i = first; i < first + count; i++)
                byHeart &= entries[i].ByHeart;
            return new QueueStop(room, earlier.Count + 1, isReturn, first, count, byHeart);
        }
    }
}
