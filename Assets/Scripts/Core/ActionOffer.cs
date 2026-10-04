namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// An action shown at a room (Simulation.OffersAt), with why it can't be done, if it can't.
    /// </summary>
    public readonly struct ActionOffer
    {
        public TaskDefinition Task { get; }

        /// <summary>Why it can't be done, as the player reads it; null if nothing stands in the way.</summary>
        public string Reason { get; }

        /// <summary>
        /// True when the queue would refuse it outright (done this run, fully searched). A missing
        /// need has a Reason but isn't Blocked: she may pick the thing up on the way, or something
        /// may supply it when its turn comes.
        /// </summary>
        public bool Blocked { get; }

        public ActionOffer(TaskDefinition task, string reason = null, bool blocked = false)
        {
            Task = task;
            Reason = reason;
            Blocked = blocked;
        }
    }
}
