using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One line of the action queue: a task, repeated until it's done. A trip is the common Travel
    /// verb plus where to. An entry pushed down by Play keeps its progress for when it's back on top.
    /// </summary>
    public class QueueEntry
    {
        public TaskDefinition Task { get; }
        /// <summary>For a trip, where she's going. Null for everything else.</summary>
        public NodeDefinition Destination { get; }

        /// <summary>
        /// Null: repeat until done (the normal case). A number: stop after this many more, even if
        /// not done. Not offered on screen; used by tests, and by automation later.
        /// </summary>
        public int? TimesLeft { get; set; }

        /// <summary>How many times this entry has been finished so far.</summary>
        public int TimesDone { get; set; }

        /// <summary>Work done before Play pushed it down (0 if none), and in which room.</summary>
        public float SavedWork { get; set; }
        public NodeDefinition SavedAt { get; set; }
        /// <summary>How far through it was when pushed down (0 to 1), for showing on screen.</summary>
        public float SavedFraction { get; set; }

        /// <summary>
        /// Set when the queue put this entry on top to supply a blocked one: the entry it's for, the
        /// item it's getting, and how many are enough (in her pockets plus on the floor here).
        /// </summary>
        public QueueEntry SuppliesFor { get; set; }
        public ResourceDefinition Supplies { get; set; }
        public int SupplyTarget { get; set; }

        /// <summary>
        /// Carry: gather only what she can carry, stopping once her pockets and containers are full,
        /// never making things just to leave them on the floor.
        /// </summary>
        public bool CarryOnly { get; set; }
        /// <summary>Carried over from the last run because the room is known by heart (not put here by the player this run).</summary>
        public bool ByHeart { get; set; }
        /// <summary>How many suppliers deep this one is (a supplier's own supplier is 2).</summary>
        public int SupplyDepth => SuppliesFor == null ? 0 : SuppliesFor.SupplyDepth + 1;

        public QueueEntry(TaskDefinition task, NodeDefinition destination = null, int? times = null)
        {
            Task = task;
            Destination = destination;
            TimesLeft = destination != null ? 1 : times; // a trip is always made once
        }

        /// <summary>"Travel to the junction", or just the task's name.</summary>
        public string DisplayName => NameOf(Task, Destination);

        public bool IsSameAs(TaskDefinition task, NodeDefinition destination) =>
            Task == task && Destination == destination;

        public static string NameOf(TaskDefinition task, NodeDefinition destination) =>
            destination == null
                ? task.displayName
                : GameText.Get("names.trip", ("verb", task.displayName), ("room", GameText.TitleInSentence(destination.DisplayName)));
    }

    /// <summary>
    /// The actions Clara will do this run, in order: she works the top one until it's done, then
    /// the next. Belongs to one run: every run starts with an empty queue.
    /// </summary>
    public class ActionQueue
    {
        public List<QueueEntry> Entries { get; } = new List<QueueEntry>();

        public int Count => Entries.Count;
        public QueueEntry Top => Entries.Count > 0 ? Entries[0] : null;

        public int IndexOf(TaskDefinition task, NodeDefinition destination) =>
            Entries.FindIndex(e => e.IsSameAs(task, destination));
    }
}
