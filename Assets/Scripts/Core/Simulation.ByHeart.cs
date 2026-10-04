using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Rooms known by heart: what she did in them last run is already queued for the next.
    public partial class Simulation
    {
        /// <summary>
        /// How many runs she has worked in a room (see <see cref="CompletedStep.CountsAsWork"/>), this one not
        /// included until it ends. The start room counts every run finished: she begins each one there, and
        /// nothing in it may be worth doing, so it would never be known by heart otherwise.
        /// </summary>
        public int RunsWorkedIn(NodeDefinition room)
        {
            if (room == null)
                return 0;
            if (room == StartNode)
                return Math.Max(Persistent.RunHistory.Count, Persistent.RoomRuns.TryGetValue(room, out int worked) ? worked : 0); // worked out when read, so old saves catch up at once
            return Persistent.RoomRuns.TryGetValue(room, out int runs) ? runs : 0;
        }

        /// <summary>
        /// For the dev panel only: sets how many runs she has worked in a room, so "known by heart" can be tested
        /// without playing them. Never below 0. The start room's count is read from runs finished (see
        /// <see cref="RunsWorkedIn"/>), so it can't be set: that would be a silent no-op.
        /// </summary>
        public void DevSetRoomRuns(NodeDefinition room, int runs)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room));
            if (room == StartNode)
                throw new InvalidOperationException("The start room's runs come from runs finished and can't be set.");
            Persistent.RoomRuns[room] = Math.Max(0, runs);
        }

        /// <summary>Whether she has worked in a room in enough runs (LoopSettings.byHeartRuns) to know it by heart.</summary>
        public bool IsKnownByHeart(NodeDefinition room) => room != null && RunsWorkedIn(room) >= _settings.byHeartRuns;

        /// <summary>
        /// Between runs only (plan 032a): why a plan can't include these rooms, or null. Planning covers what
        /// she knows by heart, so every plan carries over: each room she starts in, passes through or ends in
        /// must be known by heart (the same test <see cref="CarryPlanToNextRun"/> uses). During a run the
        /// queue stays free, so this is always null then, and before <see cref="PlanningUnlocked"/>.
        /// </summary>
        private string WhyNotPlannable(IReadOnlyList<NodeDefinition> rooms)
        {
            // Before she has earned planning there is none: nothing is planned between runs (the Plan button is hidden).
            if (!HasPlaces || !Loop.IsOver || !PlanningUnlocked)
                return null;
            // A for loop, not foreach: foreach over the interface would allocate an enumerator on every call.
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room != null && !IsKnownByHeart(room))
                    return GameText.Get("reasons.not_by_heart_plan", ("room", GameText.TitleInSentence(room.DisplayName)),
                        ("runs", RunsWorkedIn(room)), ("needed", _settings.byHeartRuns));
            }
            return null;
        }

        /// <summary>How many actions in the queue were carried over from the last run (a repeat counts each time; a trip counts once).</summary>
        public int CarriedActionCount
        {
            get
            {
                int count = 0;
                foreach (var entry in Queue.Entries)
                    if (entry.ByHeart)
                        count += entry.TimesLeft ?? 1;
                return count;
            }
        }

        /// <summary>The room she'd end in if the whole queue ran as planned, or null when nothing is queued.</summary>
        public NodeDefinition PlanEndsIn => Queue.Count == 0 ? null : PlannedEndNode;

        private readonly HashSet<NodeDefinition> _workedRooms = new HashSet<NodeDefinition>();

        /// <summary>
        /// Sets <see cref="QueueStop.CarriesOver"/> on each stop the way <see cref="CarryPlanToNextRun"/> will
        /// judge it when the run ends: a room counts as known by heart if the run ending adds it to her
        /// count (she has worked there this run, or has work queued there), and the carry stops at the first
        /// room that isn't. Mid-run, the actions already done count first: a room left behind that won't be
        /// known breaks the carry for everything queued after it.
        /// </summary>
        private void MarkStopsThatCarry(List<QueueStop> stops)
        {
            _workedRooms.Clear();
            bool carrying = true;
            if (!Loop.IsOver)
            {
                foreach (var step in Loop.Steps)
                    if (step.CountsAsWork && step.Room != null)
                        _workedRooms.Add(step.Room);
            }
            var entries = Queue.Entries;
            foreach (var stop in stops)
                for (int i = stop.FirstEntry; i < stop.FirstEntry + stop.EntryCount; i++)
                    if (entries[i].Destination == null && stop.Room != null)
                        _workedRooms.Add(stop.Room);

            if (!Loop.IsOver)
            {
                foreach (var step in Loop.Steps)
                {
                    if (!KnownByHeartAfterThisRun(step.Room) || (step.Destination != null && !KnownByHeartAfterThisRun(step.Destination)))
                    {
                        carrying = false;
                        break;
                    }
                }
            }
            for (int i = 0; i < stops.Count; i++)
            {
                carrying &= KnownByHeartAfterThisRun(stops[i].Room);
                stops[i] = stops[i].WithCarriesOver(carrying);
            }
        }

        // The room's run count with this run added, if she works there (MarkStopsThatCarry's _workedRooms);
        // the start room is added by every run.
        private bool KnownByHeartAfterThisRun(NodeDefinition room) =>
            room != null && RunsWorkedIn(room) + (room == StartNode || _workedRooms.Contains(room) ? 1 : 0) >= _settings.byHeartRuns;

        /// <summary>How many times faster the clock runs while she works in this room (<see cref="LoopSettings.RoomSpeedAfter"/>).</summary>
        public float RoomSpeed(NodeDefinition room) => room == null ? 1f : _settings.RoomSpeedAfter(RunsWorkedIn(room));

        /// <summary>
        /// The clock multiplier right now, on top of the player's own speed tier: the room she is working in,
        /// or for a trip the slower of its two rooms. 1 while nothing runs, the speed is held, or she
        /// hasn't earned room speed yet (<see cref="RoomSpeedUnlocked"/>).
        /// </summary>
        public float RoomSpeedNow
        {
            get
            {
                if (_speedHold.Held || Loop == null || !RoomSpeedUnlocked || Loop.IsOver || Loop.RunningEntry == null)
                    return 1f;
                float speed = RoomSpeed(Loop.CurrentNode);
                return Loop.CurrentDestination != null ? MathF.Min(speed,RoomSpeed(Loop.CurrentDestination)) : speed;
            }
        }

        /// <summary>
        /// True from a skipped action, a refused action or a milestone until the action that follows it
        /// is done: anything needing a decision, and what comes next, runs at the player's speed.
        /// </summary>
        public bool SpeedHeld => _speedHold.Held;

        private readonly RoomSpeedHold _speedHold;

        /// <summary>
        /// At the end of a run: each room she worked in counts once, however many actions she did there.
        /// Returns the rooms this run made known by heart.
        /// </summary>
        private List<NodeDefinition> CountRoomsWorked()
        {
            var worked = new HashSet<NodeDefinition>();
            foreach (var step in Loop.Steps)
                if (step.CountsAsWork && step.Room != null)
                    worked.Add(step.Room);
            var learned = new List<NodeDefinition>();
            foreach (var room in worked)
            {
                bool wasKnown = IsKnownByHeart(room);
                // The stored count, not RunsWorkedIn: the start room's is read as the runs finished.
                Persistent.RoomRuns[room] = (Persistent.RoomRuns.TryGetValue(room, out int before) ? before : 0) + 1;
                if (!wasKnown && IsKnownByHeart(room))
                    learned.Add(room);
            }
            // The start room counted this run whether or not she worked in it (RunsWorkedIn), so it was known
            // before this run if the history without it, or the stored count without it, already reached the threshold.
            var start = StartNode;
            if (start != null && !learned.Contains(start))
            {
                int storedBefore = Persistent.RoomRuns.TryGetValue(start, out int startRuns) ? startRuns - (WorkedThisRun(start) ? 1 : 0) : 0;
                bool startWasKnown = Math.Max(Persistent.RunHistory.Count - 1, storedBefore) >= _settings.byHeartRuns;
                if (!startWasKnown && IsKnownByHeart(start))
                    learned.Add(start);
            }
            return learned;
        }

        private bool WorkedThisRun(NodeDefinition room)
        {
            foreach (var step in Loop.Steps)
                if (step.CountsAsWork && step.Room == room)
                    return true;
            return false;
        }

        /// <summary>
        /// Puts what she did this run into the queue for the next, up to the first room she doesn't
        /// know by heart. Supplying is left out (the supplier adds it again when needed), and so are
        /// tasks a switch has since turned off; repeats become one entry with a count. Anything else
        /// is carried even if it may not start: at run time the queue skips it with a reason.
        /// </summary>
        private void CarryPlanToNextRun()
        {
            var plan = new ActionQueue();
            foreach (var step in Loop.Steps)
            {
                if (!IsKnownByHeart(step.Room) || (step.Destination != null && !IsKnownByHeart(step.Destination)))
                    break;
                if (step.IsSupply || Persistent.LockedTasks.Contains(step.Task))
                    continue;

                var last = plan.Entries.Count > 0 ? plan.Entries[plan.Entries.Count - 1] : null;
                if (step.Destination == null && last != null && last.IsSameAs(step.Task, null))
                    last.TimesLeft++;
                else
                    plan.Entries.Add(new QueueEntry(step.Task, step.Destination, 1) { ByHeart = true });
            }
            _nextQueue = plan;
            if (plan.Count > 0)
                QueueChanged?.Invoke();
        }
    }
}
