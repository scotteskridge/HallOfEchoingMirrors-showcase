using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Places: where Clara is, the ways between rooms, which tasks can be done where, and where
    // the queue will take her.
    public partial class Simulation
    {
        /// <summary>True once the content has a start node. Without one, every task can be done anywhere.</summary>
        public bool HasPlaces => _content != null && _content.startNode != null;

        public NodeDefinition StartNode => _content != null ? _content.startNode : null;

        public TaskDefinition TravelVerb => _content != null ? _content.travelVerb : null;

        public IReadOnlyList<NodeDefinition> AllNodes =>
            _content != null ? _content.PlayableNodes : (IReadOnlyList<NodeDefinition>)System.Array.Empty<NodeDefinition>();

        // ---------- Ways ----------

        /// <summary>
        /// The way from one room straight to another, and the room that lists it (a both-ways way
        /// is listed at one end only). Null if there's none, open or not.
        /// </summary>
        public (Way way, NodeDefinition owner) FindWay(NodeDefinition from, NodeDefinition to)
        {
            // A planned room is not part of the game: nothing joins it, and its own ways reach nobody.
            if (from == null || to == null || from == to || from.planned || to.planned)
                return (null, null);
            foreach (var way in from.ways)
                if (way != null && way.to == to)
                    return (way, from);
            foreach (var way in to.ways)
                if (way != null && way.to == from && way.bothWays)
                    return (way, to);
            return (null, null);
        }

        /// <summary>Open unless a flipped switch closed it; otherwise open from the start or opened by a switch.</summary>
        public bool IsOpen(Way way, NodeDefinition owner)
        {
            if (way == null)
                return false;
            bool opened = way.startsOpen;
            foreach (var @switch in Persistent.FlippedSwitches)
            {
                if (Names(@switch.closesWays, way, owner))
                    return false;
                if (Names(@switch.opensWays, way, owner))
                    opened = true;
            }
            return opened;
        }

        /// <summary>
        /// What a way needs to be taken: its own needs, or nothing once a flipped switch waives them.
        /// Read from the flipped switches, so the waiver is kept without a save field of its own.
        /// </summary>
        public IReadOnlyList<ResourceAmount> NeedsOf(Way way, NodeDefinition owner)
        {
            foreach (var @switch in Persistent.FlippedSwitches)
                if (Names(@switch.waivesWayNeeds, way, owner))
                    return Array.Empty<ResourceAmount>();
            return way.needs;
        }

        private static bool Names(List<WayRef> refs, Way way, NodeDefinition owner)
        {
            foreach (var r in refs)
                if (r != null && ((r.from == owner && r.to == way.to) || (way.bothWays && r.from == way.to && r.to == owner)))
                    return true;
            return false;
        }

        /// <summary>
        /// Whether she can take a hidden way: not hidden at all, or found by searching its room (in
        /// this run or an earlier one: a way once found stays found).
        /// </summary>
        public bool IsFound(Way way, NodeDefinition owner) =>
            way != null && (way.foundAtExplored <= 0 || Persistent.FoundWays.Contains((owner, way.to)) || SearchReaches(way, owner));

        /// <summary>Open and found: a way she knows and can take (if she has what it needs).</summary>
        public bool IsUsable(Way way, NodeDefinition owner) => IsOpen(way, owner) && IsFound(way, owner);

        /// <summary>Rooms she can set off for from here: every open, found way, including doors she can't pass yet.</summary>
        public List<NodeDefinition> DestinationsFrom(NodeDefinition node)
        {
            var result = new List<NodeDefinition>();
            DestinationsFrom(node, result);
            return result;
        }

        /// <summary>As <see cref="DestinationsFrom(NodeDefinition)"/>, replacing what's in <paramref name="into"/> (no new list: for per-frame callers).</summary>
        public void DestinationsFrom(NodeDefinition node, List<NodeDefinition> into)
        {
            _usableWay ??= IsUsable; // made once: a method group would allocate on every call
            NeighboursOf(node, _usableWay, into);
        }

        private Func<Way, NodeDefinition, bool> _usableWay, _shownWay;

        /// <summary>
        /// The rooms joined to <paramref name="node"/> by a way the test accepts, in either direction
        /// (a both-ways way is listed at one end only), each once. Replaces what's in <paramref name="into"/>.
        /// </summary>
        /// <param name="accepts">Given a way and the room that lists it.</param>
        private void NeighboursOf(NodeDefinition node, Func<Way, NodeDefinition, bool> accepts, List<NodeDefinition> into)
        {
            into.Clear();
            if (node == null)
                return;
            foreach (var way in node.ways)
                if (way != null && way.to != null && !way.IntoPlannedRoom && accepts(way, node) && !into.Contains(way.to))
                    into.Add(way.to);
            foreach (var other in AllNodes)
            {
                if (other == null || other == node)
                    continue;
                foreach (var way in other.ways)
                    if (way != null && way.to == node && way.bothWays && accepts(way, other) && !into.Contains(other))
                        into.Add(other);
            }
        }

        /// <summary>
        /// How a trip is changed by the rooms at each end: the room left's Leaving times the room
        /// entered's Entering.
        /// </summary>
        public (float cost, float time) TripModifier(NodeDefinition from, NodeDefinition to)
        {
            float cost = 1f, time = 1f;
            if (from != null) { cost *= from.leaving.cost; time *= from.leaving.time; }
            if (to != null) { cost *= to.entering.cost; time *= to.entering.time; }
            return (cost, time);
        }

        /// <summary>Why a trip from here can't be made now, or null if it can.</summary>
        private string CantTravel(NodeDefinition from, NodeDefinition to)
        {
            string reason = CantUseWay(from, to, out var way, out var owner);
            if (reason != null)
                return reason;
            var missing = MissingFrom(NeedsOf(way, owner), from);
            return missing == null ? null : NeedsReason(missing);
        }

        /// <summary>
        /// Why there's no way from here to there she can take, leaving aside what it needs, or null
        /// with the <paramref name="way"/> and the room that lists it (its needs are checked by the caller).
        /// </summary>
        internal string CantUseWay(NodeDefinition from, NodeDefinition to, out Way way, out NodeDefinition owner)
        {
            way = null;
            owner = null;
            if (from == to)
                return GameText.Get("reasons.already_there");
            (way, owner) = FindWay(from, to);
            if (way == null)
                return GameText.Get("reasons.no_way", ("room", GameText.TitleInSentence(from.DisplayName)));
            // Not found comes first: a shut way she hasn't found shouldn't give itself away.
            if (!IsFound(way, owner))
                return GameText.Get("reasons.way_not_found");
            if (!IsOpen(way, owner))
                return ShutMessageOf(way) ?? GameText.Get("reasons.way_shut");
            return null;
        }

        // ---------- Where tasks can be done ----------

        /// <summary>
        /// Whether a task can be done at a node: always there, or found by the node's search (its kept bar;
        /// see <see cref="IsFoundHere"/>).
        /// Tasks listed at no node at all can be done anywhere (e.g. dropping an item). The Travel
        /// verb is handled by ways instead.
        /// </summary>
        public bool IsAvailableAt(TaskDefinition task, NodeDefinition node)
        {
            if (!HasPlaces || node == null)
                return true;
            // A pick-up action is offered wherever its item lies on the floor; a put-down wherever
            // she has some in her pockets (and there's a floor: a room).
            if (task.picksUp != null)
                return OnFloor(node, task.picksUp) > 0;
            if (task.putsDown != null)
                return node == Loop.CurrentNode && InPockets(task.putsDown) > 0;
            if (node.tasks.Contains(task) || IsFoundHere(task, node))
                return true;
            foreach (var other in AllNodes)
                if (other != null && other.Lists(task))
                    return false;
            // Held only by a planned room: not part of the game yet, so not doable anywhere.
            return !_content.IsListedInAPlannedRoom(task);
        }

        private bool IsAvailableHere(TaskDefinition task) => IsAvailableAt(task, Loop.CurrentNode);

        // ---------- The plan ----------

        /// <summary>
        /// Where Clara will be after the first <paramref name="entryCount"/> queue entries, if all
        /// goes to plan, starting from where she is now (the start room before a run). A trip that
        /// can't be made from where she'd be is skipped, as in a run (needs aren't checked: she may
        /// pick up what a door needs on the way).
        /// </summary>
        public NodeDefinition PlannedNodeAfter(int entryCount)
        {
            var node = PlanStart;
            var entries = Queue.Entries;
            for (int i = 0; i < entryCount && i < entries.Count; i++)
                node = NodeAfterEntry(node, entries[i]);
            return node;
        }

        /// <summary>Where the queue's plan starts: where she is, or the start room before a run.</summary>
        internal NodeDefinition PlanStart => Loop.IsOver || Loop.CurrentNode == null ? StartNode : Loop.CurrentNode;

        /// <summary>
        /// One step of the plan: where she'd be after this entry, from <paramref name="node"/>. Only a
        /// trip she can make moves her; the queue's stops (QueueStops) follow the same rule.
        /// </summary>
        internal NodeDefinition NodeAfterEntry(NodeDefinition node, QueueEntry entry)
        {
            if (entry.Destination == null)
                return node;
            var (way, owner) = FindWay(node, entry.Destination);
            return IsUsable(way, owner) ? entry.Destination : node;
        }

        /// <summary>Where the queue leaves Clara: where newly scheduled trips start from.</summary>
        public NodeDefinition PlannedEndNode => PlannedNodeAfter(Queue.Count);

        /// <summary>Rooms a trip can be scheduled to now: those next to where the queue leaves her.</summary>
        public List<NodeDefinition> NextStops() => DestinationsFrom(PlannedEndNode);

        /// <summary>
        /// The message of a found, shut, shown way into <paramref name="room"/> from a room she can walk to from
        /// <paramref name="end"/> (so the reason shows from further away too), or null.
        /// </summary>
        private string ShutWayMessageInto(NodeDefinition end, NodeDefinition room)
        {
            foreach (var owner in AllNodes)
            {
                if (owner == null)
                    continue;
                foreach (var way in owner.ways)
                {
                    string message = way != null ? ShutMessageOf(way) : null;
                    if (message == null)
                        continue;
                    // The room on the near side of the way, when the way leads to the room asked about.
                    NodeDefinition near = way.to == room ? owner : way.bothWays && owner == room ? way.to : null;
                    if (near != null && !IsOpen(way, owner) && IsFound(way, owner) && (near == end || RouteSearch.TryFind(this, end, near, out _)))
                        return message;
                }
            }
            return null;
        }

        /// <summary>A shown-while-shut way's own refusal, looked up from its text key, or null if it has none.</summary>
        private static string ShutMessageOf(Way way) =>
            way.showWhileShut && !string.IsNullOrWhiteSpace(way.shutMessageKey) ? GameText.Get(way.shutMessageKey) : null;

        /// <summary>
        /// Whether a trip to this room can be scheduled now: it's next to where the queue leaves her,
        /// or there's a walk there over ways she has found this run.
        /// </summary>
        public bool CanScheduleTripTo(NodeDefinition node) => node != null && WhyCantScheduleTripTo(node) == null;

        /// <summary>
        /// Why a trip to this room can't be scheduled now (for a toast or tooltip), or null if it can: no
        /// found way, a shown-but-shut way (its own message), or, between runs, a room on the walk that
        /// she doesn't know by heart.
        /// </summary>
        public string WhyCantScheduleTripTo(NodeDefinition node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            var end = PlannedEndNode;
            if (node == end)
                return GameText.Get("reasons.already_there");

            var walk = new List<NodeDefinition>();
            if (DestinationsFrom(end).Contains(node))
                walk.Add(node);
            else if (!RouteSearch.TryFind(this, end, node, out var found))
            {
                // A way she knows of but can't use says why (e.g. too dark); otherwise she hasn't found one.
                var (way, owner) = FindWay(end, node);
                string why = way != null && IsFound(way, owner) ? CantUseWay(end, node, out _, out _) : null;
                return why ?? ShutWayMessageInto(end, node)
                    ?? GameText.Get("reasons.no_found_way", ("room", GameText.TitleInSentence(node.DisplayName)));
            }
            else
                walk.AddRange(found);

            walk.Insert(0, end);
            return WhyNotPlannable(walk);
        }

        /// <summary>
        /// Schedules a trip at the bottom of the queue, from wherever the queue leaves her. A room
        /// that isn't next door gets the shortest walk there, one trip per way (see RouteSearch).
        /// </summary>
        public void ScheduleTrip(NodeDefinition destination)
        {
            if (TravelVerb == null || destination == null)
                return;
            var end = PlannedEndNode;
            // Next door: the click means that way, even if a longer path would be quicker.
            if (DestinationsFrom(end).Contains(destination) || !RouteSearch.TryFind(this, end, destination, out var walk))
                walk = new List<NodeDefinition> { destination };
            int before = Queue.Count;
            foreach (var step in walk)
            {
                Enqueue(new QueueEntry(TravelVerb, step), atTop: false);
                if (Queue.Count == before) // refused: the rest would start from the wrong room
                    return;
                before = Queue.Count;
            }
        }

        /// <summary>
        /// A click asking for a trip: schedules it, or says why not (<see cref="ActionRefused"/>, which shows as a
        /// toast) when it can't be scheduled. Clicking the room she'd be in is no request, so it says nothing.
        /// </summary>
        public void TryScheduleTrip(NodeDefinition destination)
        {
            if (destination == null)
                return;
            string why = WhyCantScheduleTripTo(destination);
            if (why == null)
                ScheduleTrip(destination);
            else if (TravelVerb != null && destination != PlannedEndNode)
                ActionRefused?.Invoke(TravelVerb, destination, why);
        }

        /// <summary>Play for a trip: go there now, from where she is.</summary>
        public void PlayTripNow(NodeDefinition destination)
        {
            if (TravelVerb != null && destination != null)
                Enqueue(new QueueEntry(TravelVerb, destination), atTop: true);
        }

        /// <summary>
        /// Rooms that belong on the map: every room reachable from the start through open, found ways
        /// (a door she can't pass yet still shows what's behind it), plus anything marked always on the map.
        /// A found way that is shut but marked Show While Shut counts too (plan 032a).
        /// </summary>
        public List<NodeDefinition> RoomsOnMap()
        {
            var known = new List<NodeDefinition>();
            if (StartNode != null)
                known.Add(StartNode);
            for (int i = 0; i < known.Count; i++) // the list grows as rooms are found
                foreach (var next in KnownDestinationsFrom(known[i]))
                    if (!known.Contains(next))
                        known.Add(next);

            foreach (var node in AllNodes)
                if (node != null && node.alwaysOnMap && !known.Contains(node))
                    known.Add(node);
            return known;
        }

        public bool IsOnMap(NodeDefinition node) => node != null && RoomsOnMap().Contains(node);

        /// <summary>
        /// Rooms an open way she knows of leads to from here (found this run or before), and rooms behind a found
        /// way that is shut but shown (Way.showWhileShut). Note: RoomsOnMap keeps walking from a shown room, so an
        /// open way out of one would put the rooms beyond it on the map too.
        /// </summary>
        private List<NodeDefinition> KnownDestinationsFrom(NodeDefinition node)
        {
            _shownWay ??= (way, owner) => (IsOpen(way, owner) || way.showWhileShut) && IsFound(way, owner);
            var result = new List<NodeDefinition>();
            NeighboursOf(node, _shownWay, result);
            return result;
        }
    }
}
