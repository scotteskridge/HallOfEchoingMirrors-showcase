using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Where an action scheduled in a room would land: at the end of that room's last visit if it's
    /// on her route, otherwise after the shortest walk there from where the queue ends. Or that it
    /// can't be scheduled there, and why.
    /// </summary>
    public readonly struct ScheduleTarget
    {
        public bool CanSchedule { get; }
        /// <summary>Why not, when it can't be scheduled. Null otherwise.</summary>
        public string Reason { get; }
        /// <summary>Where in the queue the action goes (after any walk is added at the end).</summary>
        public int InsertIndex { get; }
        /// <summary>The rooms to walk to first, one trip each; empty when the room is on her route.</summary>
        public IReadOnlyList<NodeDefinition> Walk { get; }
        /// <summary>The number of the stop it joins, when the room is on her route (0 when a walk is added).</summary>
        public int StopNumber { get; }

        /// <summary>How long the walk takes at normal speed, in seconds.</summary>
        public float WalkSeconds { get; }
        /// <summary>
        /// Where in the queue the visit's first action would go: just after the trip that enters the
        /// room. For a room off her route, the same as <see cref="InsertIndex"/> (the walk's end).
        /// </summary>
        public int VisitStartIndex { get; }

        private ScheduleTarget(bool can, string reason, int index, IReadOnlyList<NodeDefinition> walk, int stop, float walkSeconds, int visitStart)
        {
            VisitStartIndex = visitStart;
            CanSchedule = can;
            Reason = reason;
            InsertIndex = index;
            Walk = walk;
            StopNumber = stop;
            WalkSeconds = walkSeconds;
        }

        public bool OnRoute => CanSchedule && Walk.Count == 0;

        public static ScheduleTarget Onto(int index, int stopNumber, int visitStart) =>
            new ScheduleTarget(true, null, index, System.Array.Empty<NodeDefinition>(), stopNumber, 0f, visitStart);

        public static ScheduleTarget After(int index, IReadOnlyList<NodeDefinition> walk, float walkSeconds) =>
            new ScheduleTarget(true, null, index, walk, 0, walkSeconds, index);

        public static ScheduleTarget Cant(string reason) =>
            new ScheduleTarget(false, reason, -1, System.Array.Empty<NodeDefinition>(), 0, 0f, -1);
    }
}
