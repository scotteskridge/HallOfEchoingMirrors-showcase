namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One action finished in a run, with where it happened: what the run's record needs to count
    /// the rooms she worked in and to build the plan carried into the next run.
    /// </summary>
    public readonly struct CompletedStep
    {
        public TaskDefinition Task { get; }
        /// <summary>Where she was when it finished (for a trip, the room she left). Null if the game has no places.</summary>
        public NodeDefinition Room { get; }
        /// <summary>For a trip, where it went. Null for everything else.</summary>
        public NodeDefinition Destination { get; }
        /// <summary>Done because the queue put it on top to supply a blocked action (the supplier adds these again when needed).</summary>
        public bool IsSupply { get; }

        public CompletedStep(TaskDefinition task, NodeDefinition room, NodeDefinition destination, bool isSupply)
        {
            Task = task;
            Room = room;
            Destination = destination;
            IsSupply = isSupply;
        }

        /// <summary>Work in a room, as opposed to a trip or an auto-supply.</summary>
        public bool CountsAsWork => Destination == null && !IsSupply;
    }
}
