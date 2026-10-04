using System;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// When the game stops the clock, and why. The player (the Pause button) and story pop-ups
    /// pause it; the game also pauses by itself to hand control back to the player: when nothing's
    /// queued, when a single action is done, and when a new way is found (if LoopSettings says so).
    /// A pause the game made ends as soon as something is queued while Clara is idle; one the player
    /// chose stays until they resume. With nothing queued it never runs: Resume stays paused, and
    /// the Simulation itself lets no time pass. The clock (TickEngine) follows <see cref="PausedChanged"/>.
    /// </summary>
    public class PauseControl
    {
        private readonly Simulation _sim;
        private readonly LoopSettings _settings;
        private bool _pausedByGame;

        public bool IsPaused { get; private set; }
        /// <summary>Why it's paused, e.g. "Clara needs to know what to do next". Null while running.</summary>
        public string Reason { get; private set; }

        /// <summary>Paused or running now. The clock follows this.</summary>
        public event Action<bool> PausedChanged;

        public PauseControl(Simulation sim, LoopSettings settings)
        {
            _sim = sim;
            _settings = settings;

            // Nothing queued (every run starts that way): stop the clock until the player adds something.
            sim.QueueRanOut += () =>
            {
                if (RunUnderWay)
                    PauseForPlayer(GameText.Get("run.pause_reason.queue_empty"));
            };

            // A single action done: stop so the player can choose what's next.
            sim.SingleActionDone += task =>
                PauseForPlayer(GameText.Get("run.pause_reason.single_action", ("task", task.displayName)));

            // A new way found mid-run: stop and let the player queue the trip (it teaches that the
            // queue can change during a run). A story pop-up, if any, leaves it paused on close.
            sim.WayFound += (from, to) =>
            {
                if (_settings.pauseWhenAWayIsFound && RunUnderWay)
                    PauseForPlayer(GameText.Get("run.pause_reason.new_way", ("room", GameText.TitleInSentence(to.DisplayName))));
            };

            // Something queued while she's waiting and the game paused itself for her: go at once.
            // (Only when the player adds something: an entry finishing mustn't undo a pause, e.g.
            // the explore that found a new way leaving the queue.)
            sim.ActionQueued += () =>
            {
                if (IsPaused && _pausedByGame && RunUnderWay && sim.Loop.CurrentTask == null && sim.Queue.Count > 0)
                    Resume();
            };
        }

        private bool RunUnderWay => _sim.Phase == LoopPhase.Running && !_sim.Loop.IsOver;

        /// <summary>Pauses the game (the Pause button, a story pop-up). Stays paused until resumed.</summary>
        public void Pause(string reason = null) => Set(true, reason ?? GameText.Get("run.pause_reason.paused"), byGame: false);

        /// <summary>
        /// Carries on, unless she's idle with nothing queued: then it stays paused (by the game, so
        /// queuing something carries on), since there's nothing to spend time on.
        /// </summary>
        public void Resume()
        {
            if (RunUnderWay && _sim.Loop.CurrentTask == null && _sim.Queue.Count == 0)
                PauseForPlayer(GameText.Get("run.pause_reason.queue_empty"));
            else
                Set(false, null, byGame: false);
        }

        // The game stops to hand control to the player: queuing an action while she's idle carries on.
        private void PauseForPlayer(string reason) => Set(true, reason, byGame: true);

        private void Set(bool paused, string reason, bool byGame)
        {
            bool changed = paused != IsPaused;
            IsPaused = paused;
            Reason = reason;
            _pausedByGame = byGame;
            if (changed)
                PausedChanged?.Invoke(paused);
        }
    }
}
