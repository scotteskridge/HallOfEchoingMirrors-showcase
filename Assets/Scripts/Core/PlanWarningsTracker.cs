using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Warnings ahead on the plan (plan ui-024d): which queued entries will be refused when they're
    /// reached, found by walking the queue from where it starts. Lives as long as its Simulation, listens
    /// to its events to know when the warnings are stale, and reads its rules (where the queue starts,
    /// ways, searches, offers) without changing anything. The Simulation forwards
    /// <see cref="Simulation.PlanWarnings"/>, <see cref="Simulation.RoomWarnings"/> and
    /// <see cref="Simulation.PlanWarningCount"/> here.
    /// </summary>
    internal class PlanWarningsTracker
    {
        private readonly Simulation _sim;
        private readonly LoopSettings _settings;
        private readonly GameContent _content;

        private readonly List<string> _warnings = new List<string>();
        private readonly List<NodeDefinition> _warningRooms = new List<NodeDefinition>(); // the stop each entry falls in, by queue index
        private readonly List<string> _roomWarnings = new List<string>();
        private int _count;
        private bool _stale = true;

        // The walk's notes so far: what earlier entries give, once-a-run tasks already planned, rooms searched.
        private readonly HashSet<ResourceDefinition> _plannedGives = new HashSet<ResourceDefinition>();
        private readonly HashSet<TaskDefinition> _plannedOnce = new HashSet<TaskDefinition>();
        private readonly HashSet<NodeDefinition> _plannedSearches = new HashSet<NodeDefinition>();

        /// <summary>Hooks up what makes the warnings stale: the queue, a switch, an action done, something used up, a way found, a run beginning or ending.</summary>
        public PlanWarningsTracker(Simulation sim, LoopSettings settings, GameContent content)
        {
            _sim = sim;
            _settings = settings;
            _content = content;
            sim.QueueChanged += MarkStale;
            sim.SwitchFlipped += _ => MarkStale();
            sim.TaskCompleted += _ => MarkStale();
            sim.ResourceLost += (_, _) => MarkStale(); // mid-action too (a restorative starting)
            sim.WayFound += (_, _) => MarkStale();
            sim.LoopStarted += MarkStale;
            sim.LoopEnded += MarkStale;
        }

        /// <summary>Something the events don't cover changed what the walk would find (packing an item, a restored queue).</summary>
        public void MarkStale() => _stale = true;

        /// <summary>See <see cref="Simulation.PlanWarnings"/>: worked out again only after something that can change it.</summary>
        public IReadOnlyList<string> Warnings()
        {
            if (_stale)
            {
                WalkThePlan();
                _stale = false;
            }
            return _warnings;
        }

        /// <summary>See <see cref="Simulation.RoomWarnings"/>.</summary>
        public IReadOnlyList<string> RoomWarnings(NodeDefinition room)
        {
            var warnings = Warnings();
            _roomWarnings.Clear();
            for (int i = 0; i < warnings.Count; i++)
                if (warnings[i] != null && _warningRooms[i] == room)
                    _roomWarnings.Add(warnings[i]);
            return _roomWarnings;
        }

        /// <summary>How many queued entries are warned about.</summary>
        public int Count
        {
            get
            {
                Warnings();
                return _count;
            }
        }

        private void WalkThePlan()
        {
            _warnings.Clear();
            _warningRooms.Clear();
            _count = 0;
            _plannedGives.Clear();
            _plannedOnce.Clear();
            _plannedSearches.Clear();

            var entries = _sim.Queue.Entries;
            var room = _sim.PlanStart;
            bool sure = true;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string warning = sure && !(_sim.RunUnderWay && i == 0) ? WarningFor(entry, room) : null;
                _warnings.Add(warning);
                if (warning != null)
                    _count++;

                var next = _sim.NodeAfterEntry(room, entry);
                _warningRooms.Add(next); // a trip that's made starts the stop it arrives at (as QueueStops)
                // Nothing is sure after a switch may flip (it can open ways, teach hues, unlock tasks), a
                // trip a search may yet make possible, or a trip refused for an item (the stops show her
                // arriving; she'll stay where she was).
                if (sure)
                    sure = !MightFlipASwitch(entry, room) &&
                           !(entry.Destination != null && (MightFindWay(room, entry.Destination) || (warning != null && next != room)));
                // An entry sure to be refused gives nothing and uses up no once-a-run task.
                if (warning == null)
                    NoteWhatItDoes(entry, room);
                room = next;
            }
        }

        private string WarningFor(QueueEntry entry, NodeDefinition room) =>
            entry.Destination != null ? TripWarning(room, entry.Destination) : ActionWarning(entry.Task, room);

        private string TripWarning(NodeDefinition from, NodeDefinition to)
        {
            string reason = _sim.CantUseWay(from, to, out var way, out var owner);
            if (reason != null)
                return MightFindWay(from, to) ? null : reason;
            var missing = NothingGives(_sim.NeedsOf(way, owner), suppliedIn: null);
            return missing == null ? null : Simulation.NeedsReason(missing);
        }

        // In CanStart's order, so the reason is the one the run would give.
        private string ActionWarning(TaskDefinition task, NodeDefinition room)
        {
            if (!_sim.IsUnlocked(task))
                return GameText.Get("reasons.no_longer_possible");
            if (task.oncePerRun && (_plannedOnce.Contains(task) || (_sim.RunUnderWay && _sim.Loop.CompletedTasks.Contains(task))))
                return GameText.Get("reasons.done_this_run");
            var missing = NothingGives(task.needs, suppliedIn: room);
            if (missing != null)
                return Simulation.NeedsReason(missing);
            string hue = CantPayInAHue(task);
            if (hue != null)
                return hue;
            // A pick-up or put-down depends on floors and pockets as they'll be then: silent.
            if (task.picksUp != null || task.putsDown != null || !_sim.HasPlaces)
                return null;
            return _sim.IsAvailableAt(task, room) || MightBeFoundIn(task, room) ? null : Simulation.NotHereReason(room);
        }

        /// <summary>
        /// The first need nothing can meet in time, or null: not held, packed or on a floor now, given by no
        /// earlier entry, and made by no action offered in <paramref name="suppliedIn"/> (null for a way:
        /// nothing supplies a trip).
        /// </summary>
        private ResourceAmount NothingGives(IReadOnlyList<ResourceAmount> needs, NodeDefinition suppliedIn)
        {
            foreach (var need in needs)
            {
                var item = need.resource;
                if (item == null || HeldAtPlanStart(item) >= need.amount || _plannedGives.Contains(item))
                    continue;
                if (suppliedIn != null && MightBeMadeIn(item, suppliedIn))
                    continue;
                return need;
            }
            return null;
        }

        /// <summary>
        /// What she'll have of an item where the queue starts. Between runs the last run's pockets and
        /// floors are gone: only what she keeps, and what she has packed from the stash.
        /// </summary>
        private int HeldAtPlanStart(ResourceDefinition item)
        {
            int held = _sim.HeldWhenPlanStarts(item); // shared with the trip charge (Simulation.Route.cs): one rule
            if (item.IsKept || !_sim.RunUnderWay)
                return held;
            foreach (var pile in _sim.Loop.Floor.Values)
                if (pile.TryGetValue(item, out int lying))
                    held += lying;
            return held;
        }

        /// <summary>Whether an action offered in this room makes the item (the queue would supply it there).</summary>
        private bool MightBeMadeIn(ResourceDefinition item, NodeDefinition room)
        {
            foreach (var task in _sim.AllTasks)
            {
                if (task == null || task.picksUp != null || task.putsDown != null || !_sim.IsUnlocked(task) || !Simulation.Gives(task, item))
                    continue;
                if (_sim.IsAvailableAt(task, room) || MightBeFoundIn(task, room))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Why the task costs a hue she won't have a pool for, or null. During a run, this run's pools;
        /// between runs, the pools the next run starts with.
        /// </summary>
        private string CantPayInAHue(TaskDefinition task)
        {
            if (_sim.RunUnderWay)
                return _sim.CantStartForUnknownHue(task);
            foreach (var (source, _) in task.CostBreakdown(_sim.TotalCostOf(task)))
                if (source != CostSource.Vitality && source != CostSource.AllPools && !PoolOpensNextRun(source.ToHue()))
                    return Simulation.NeedsPoolReason(source);
            return null;
        }

        private bool PoolOpensNextRun(Hue hue)
        {
            foreach (var pool in _settings.pools)
                if (pool.hue == hue && _sim.OpensAtRunStart(pool))
                    return true;
            return false;
        }

        /// <summary>
        /// Whether a hidden way she hasn't found may be found before she gets to it: its room is searched
        /// earlier in the plan, or searched far enough already and only her Perception (which rises) is short.
        /// </summary>
        private bool MightFindWay(NodeDefinition from, NodeDefinition to)
        {
            var (way, owner) = _sim.FindWay(from, to);
            return way != null && way.foundAtExplored > 0 && !_sim.IsFound(way, owner) &&
                   (_plannedSearches.Contains(owner) || _sim.IsExploredTo(owner, way.foundAtExplored));
        }

        /// <summary>As <see cref="MightFindWay"/>, for an action the room's search may find.</summary>
        private bool MightBeFoundIn(TaskDefinition task, NodeDefinition room)
        {
            if (room == null)
                return false;
            foreach (var find in room.foundBySearching)
                // Only a find of the round being searched now waits on the bar (a past round's is found already).
                if (find != null && find.task == task && _sim.BarCountsFor(find, room) &&
                    (_plannedSearches.Contains(room) || _sim.IsExploredTo(room, find.atSearched)))
                    return true;
            return false;
        }

        /// <summary>
        /// Whether this entry could flip a switch not yet flipped: its trigger task, one of the tasks it
        /// needs done, something giving what it waits for, or a search of the room it watches.
        /// Placeholder rule: a switch waiting on a stat's level is ignored (it could flip at any point).
        /// </summary>
        private bool MightFlipASwitch(QueueEntry entry, NodeDefinition room)
        {
            if (_content == null)
                return false;
            foreach (var @switch in _content.switches)
            {
                if (@switch == null || _sim.IsFlipped(@switch))
                    continue;
                bool might = @switch.trigger switch
                {
                    SwitchTrigger.LoopEndedDuringTask => @switch.triggerTask == entry.Task,
                    SwitchTrigger.TasksCompletedInOneRun => @switch.requiredTasks.Contains(entry.Task),
                    SwitchTrigger.ResourceReached => @switch.resourceToHold != null && MightGive(entry, room, @switch.resourceToHold),
                    SwitchTrigger.RoomExplored => IsSearch(entry) && @switch.roomToExplore == room,
                    SwitchTrigger.AttributeLevelReached => false, // the placeholder rule above
                    _ => throw new System.InvalidOperationException($"Switch trigger {@switch.trigger} isn't known to the plan's warnings: say which entries could flip it (MightFlipASwitch)."),
                };
                if (might)
                    return true;
            }
            return false;
        }

        private bool IsSearch(QueueEntry entry) => entry.Destination == null && _sim.IsSearching(entry.Task);

        /// <summary>Whether the entry can give her this item: what the task gives, picks up, or (a search) what the room's search gives.</summary>
        private bool MightGive(QueueEntry entry, NodeDefinition room, ResourceDefinition item)
        {
            if (entry.Destination != null)
                return false;
            if (entry.Task.picksUp == item || Simulation.Gives(entry.Task, item))
                return true;
            if (IsSearch(entry) && room != null)
                foreach (var give in room.eachExploreGives)
                    if (give.resource == item)
                        return true;
            return false;
        }

        /// <summary>Notes what an entry does for the entries after it: what it gives, a once-a-run task planned, a room searched.</summary>
        private void NoteWhatItDoes(QueueEntry entry, NodeDefinition room)
        {
            if (entry.Destination != null)
                return;
            var task = entry.Task;
            foreach (var give in task.gives)
                if (give.IsReal)
                    _plannedGives.Add(give.resource);
            if (task.picksUp != null)
                _plannedGives.Add(task.picksUp);
            if (task.oncePerRun)
                _plannedOnce.Add(task);
            if (IsSearch(entry) && room != null && !_sim.IsFullyExplored(room))
            {
                _plannedSearches.Add(room);
                foreach (var give in room.eachExploreGives)
                    if (give.resource != null)
                        _plannedGives.Add(give.resource);
            }
        }
    }
}
