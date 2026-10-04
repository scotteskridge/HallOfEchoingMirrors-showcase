namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Drops the room speed back to the player's own speed while something needs a decision: from a
    /// skipped action, a refused action or a milestone until the action that follows it is done.
    /// Lives as long as its Simulation and listens to its events; the Simulation asks <see cref="Held"/>
    /// and calls <see cref="Reset"/> at the start of every run. Not saved: a resumed run starts unheld.
    /// </summary>
    internal class RoomSpeedHold
    {
        /// <summary>True while the room speed is held at the player's speed.</summary>
        public bool Held { get; private set; }

        private bool _heldActionStarted; // an action has started since the hold was set

        /// <summary>Hooks up the events that hold the room speed, and the start and end of an action that release it.</summary>
        public RoomSpeedHold(Simulation sim)
        {
            sim.TaskSkipped += (_, _) => Hold();
            sim.ActionRefused += (_, _, _) => Hold();
            sim.MilestoneReached += (_, _, _) => Hold();
            sim.TaskStarted += _ => _heldActionStarted = true;
            sim.TaskCompleted += _ =>
            {
                if (_heldActionStarted)
                    Held = false;
            };
        }

        /// <summary>A new run starts unheld.</summary>
        public void Reset()
        {
            Held = false;
            _heldActionStarted = false;
        }

        private void Hold()
        {
            Held = true;
            _heldActionStarted = false;
        }
    }
}
