using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Schedule, Play and Carry in a room: where the action lands on her route (WhereScheduleLands), or the walk there.
    public partial class Simulation
    {
        /// <summary>
        /// Schedule in a room: the action joins the end of that room's last visit on her route, or,
        /// for a room off it, goes in after the shortest walk there. Nothing happens if there's no
        /// way (see <see cref="WhereScheduleLands"/>).
        /// </summary>
        public void ScheduleIn(TaskDefinition task, NodeDefinition room) => AddIn(task, room, atVisitStart: false);

        /// <summary>
        /// Play in a room: where she is now, the same as <see cref="PlayNow"/>. In another room, the
        /// action goes first in that room's last visit (straight after the trip that enters it), or,
        /// for a room off her route, after the walk there.
        /// </summary>
        public void PlayIn(TaskDefinition task, NodeDefinition room)
        {
            if (!HasPlaces || room == null || room == PlannedNodeAfter(0))
                PlayNow(task);
            else
                AddIn(task, room, atVisitStart: true);
        }

        /// <summary>
        /// Carry in a room: where she is now, the same as <see cref="CarryNow"/>. In another room it is
        /// queued low down like Schedule (the end of that room's last visit, or after the walk there),
        /// and when its turn comes it gathers only until her pockets and containers are full.
        /// </summary>
        public void CarryIn(TaskDefinition task, NodeDefinition room)
        {
            if (!HasPlaces || room == null || room == PlannedNodeAfter(0))
                CarryNow(task);
            else
                AddIn(task, room, atVisitStart: false, carryOnly: true);
        }

        private void AddIn(TaskDefinition task, NodeDefinition room, bool atVisitStart, bool carryOnly = false)
        {
            var target = WhereScheduleLands(room);
            if (task == null)
                return;
            if (!target.CanSchedule)
            {
                ActionRefused?.Invoke(task, null, target.Reason);
                return;
            }
            int before = Queue.Count;
            foreach (var step in target.Walk)
            {
                Enqueue(new QueueEntry(TravelVerb, step), atTop: false);
                if (Queue.Count == before) // refused: the rest of the walk would start from the wrong room
                    return;
                before = Queue.Count;
            }
            // A walk is added at the end, so the action lands there too; otherwise where the visit ends.
            int at = target.Walk.Count > 0 ? -1 : atVisitStart ? target.VisitStartIndex : target.InsertIndex;
            Enqueue(new QueueEntry(task) { CarryOnly = carryOnly }, atTop: false, insertAt: at);
        }

        // Scratch for WhereScheduleLands, which the room popover asks for every frame: the stops, the rooms
        // checked against "known by heart", and the route search's working lists. (Only the walk it returns is new.)
        private readonly List<QueueStop> _scheduleStops = new List<QueueStop>();
        private readonly List<NodeDefinition> _scheduleRooms = new List<NodeDefinition>();
        private readonly RouteSearch.Scratch _scheduleSearch = new RouteSearch.Scratch();

        /// <summary>Where Schedule in <paramref name="room"/> would put an action, or why it can't.</summary>
        public ScheduleTarget WhereScheduleLands(NodeDefinition room)
        {
            if (!HasPlaces || room == null)
                return ScheduleTarget.Onto(Queue.Count, 1, 0);

            QueueStops(_scheduleStops);
            var stops = _scheduleStops;
            for (int i = stops.Count - 1; i >= 0; i--)
                if (stops[i].Room == room)
                {
                    // Every stop after the first begins with the trip that enters it.
                    int start = stops[i].FirstEntry + (stops[i].Number > 1 ? 1 : 0);
                    _scheduleRooms.Clear();
                    _scheduleRooms.Add(room);
                    string onRoute = WhyNotPlannable(_scheduleRooms);
                    return onRoute != null ? ScheduleTarget.Cant(onRoute)
                        : ScheduleTarget.Onto(stops[i].FirstEntry + stops[i].EntryCount, stops[i].Number, start);
                }

            var end = PlannedEndNode;
            if (!RouteSearch.TryFind(this, end, room, out var walk, _scheduleSearch))
                return ScheduleTarget.Cant(GameText.Get("reasons.no_found_way", ("room", GameText.TitleInSentence(room.DisplayName))));
            _scheduleRooms.Clear();
            _scheduleRooms.Add(end);
            _scheduleRooms.AddRange(walk);
            string notByHeart = WhyNotPlannable(_scheduleRooms);
            if (notByHeart != null)
                return ScheduleTarget.Cant(notByHeart);
            float seconds = 0f;
            var at = end;
            foreach (var step in walk)
            {
                seconds += PriceOf(TravelVerb, at, step).Seconds;
                at = step;
            }
            return ScheduleTarget.After(Queue.Count, walk, seconds);
        }
    }
}
