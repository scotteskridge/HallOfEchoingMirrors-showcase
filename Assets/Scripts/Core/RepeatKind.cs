namespace HallOfEchoingMirrors.Core
{
    /// <summary>How long a queued entry keeps repeating (see <see cref="Simulation.RepeatKindOf"/>); screens turn it into words.</summary>
    public enum RepeatKind
    {
        /// <summary>A trip: made once.</summary>
        Trip,
        /// <summary>A single action: done once, ever.</summary>
        SingleAction,
        /// <summary>A once-a-run task: done once this run.</summary>
        OncePerRun,
        /// <summary>Pick up: once.</summary>
        PickUp,
        /// <summary>Put down: once.</summary>
        PutDown,
        /// <summary>A search: until the room is fully explored.</summary>
        Explore,
        /// <summary>Until what it gives is full (her pockets, containers, a maximum).</summary>
        UntilFull,
        /// <summary>Over and over, with nothing to stop it but the run's end.</summary>
        Forever,
    }
}
