using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Turns what happens in the game into quiet notes in the story feed (Around her). Numbers going
    /// up aren't noted: they show, and glow, where they are. Only listens; it holds no game rules.
    /// </summary>
    public class FeedNotes : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("The feed in Around her, where notes go.")]
        [SerializeField] private StoryFeed _feed;

        private Simulation _listeningTo;

        private Simulation Sim => _game.Simulation;
        private long Seconds => Sim.Loop.TicksElapsed / TickEngine.TicksPerSecond;

        private void Start()
        {
            _game.SimulationChanged += OnSimulationChanged;
            OnSimulationChanged(Sim);
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= OnSimulationChanged;
            ListenTo(null);
        }

        /// <summary>A new or loaded game: listen to it instead, and start a fresh feed.</summary>
        private void OnSimulationChanged(Simulation sim)
        {
            ListenTo(sim);
            if (sim == null)
                return;
            if (_feed != null)
                _feed.Clear();
            Note(sim.Persistent.LoopNumber <= 1 && sim.Loop.TicksElapsed == 0 && sim.Persistent.Journal.Count <= 1
                ? GameText.Get("feed.welcome.first")
                : GameText.Get("feed.welcome.back", ("loop", sim.NextLoopNumber)));
        }

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
                Subscribe(_listeningTo, false);
            _listeningTo = sim;
            if (sim != null)
                Subscribe(sim, true);
        }

        /// <summary>Hooks every line up to its event (or unhooks them), in one list.</summary>
        private void Subscribe(Simulation sim, bool on)
        {
            if (on)
            {
                sim.LoopStarted += OnLoopStarted;
                sim.PoolEmptied += OnPoolEmptied;
                sim.LoopEnded += OnLoopEnded;
                sim.Arrived += OnArrived;
                sim.WayFound += OnWayFound;
                sim.TaskSkipped += OnTaskSkipped;
                sim.QueueRanOut += OnQueueRanOut;
                sim.SwitchFlipped += OnSwitchFlipped;
                sim.DarknessHeldOff += OnDarknessHeldOff;
                sim.PlanningUnlockedNow += OnPlanningUnlocked;
            }
            else
            {
                sim.LoopStarted -= OnLoopStarted;
                sim.PoolEmptied -= OnPoolEmptied;
                sim.LoopEnded -= OnLoopEnded;
                sim.Arrived -= OnArrived;
                sim.WayFound -= OnWayFound;
                sim.TaskSkipped -= OnTaskSkipped;
                sim.QueueRanOut -= OnQueueRanOut;
                sim.SwitchFlipped -= OnSwitchFlipped;
                sim.DarknessHeldOff -= OnDarknessHeldOff;
                sim.PlanningUnlockedNow -= OnPlanningUnlocked;
            }
        }

        // ---------- Feed lines ----------
        // Note: a small line in the feed about the game state. Stat: a number going up, which is
        // already on screen, so it gets a floating notice instead. (Story lines come from BlurbTeller.)

        private void OnLoopStarted()
        {
            // Each run starts a fresh feed.
            if (_feed != null)
                _feed.Clear();
            Note(GameText.Get("feed.loop_started", ("loop", Sim.Persistent.LoopNumber)));
        }

        private void OnPoolEmptied(Pool pool) => Note(GameText.Get("feed.pool_empty", ("pool", pool.Name)));

        private void OnLoopEnded()
        {
            switch (Sim.Loop.EndReason)
            {
                case LoopEndReason.WalkedOut:
                    Note(UiStyle.Colour(GameText.Get("feed.ended.walked_out", ("seconds", Seconds)), UiStyle.Milestone));
                    break;
                case LoopEndReason.EndedByPlayer:
                    Note(GameText.Get("feed.ended.early", ("seconds", Seconds)));
                    break;
                default:
                    Note(GameText.Get("feed.ended.exhausted", ("seconds", Seconds)));
                    break;
            }
            foreach (var room in Sim.LastRun.KnownByHeartNow)
                Note(UiStyle.Colour(GameText.Get("feed.known_by_heart", ("room", room.DisplayName)), UiStyle.ByHeart));
        }

        // Where she is, quietly: the story itself comes from the blurbs (BlurbTeller). A first entry
        // this run gets a milestone card (StoryPanel) instead of the line.
        private void OnArrived(NodeDefinition node, bool firstEntryThisRun)
        {
            if (!firstEntryThisRun)
                Note(GameText.Get("feed.arrived", ("room", node.DisplayName)));
        }

        private void OnWayFound(NodeDefinition from, NodeDefinition to)
        {
            string line = GameText.Get("feed.way_found",
                ("from", GameText.TitleInSentence(from.DisplayName)), ("to", GameText.TitleInSentence(to.DisplayName)));
            if (_game.IsPaused && Sim.Phase == LoopPhase.Running)
                line += " " + GameText.Get("feed.way_found.paused");
            Note(UiStyle.Colour(line, UiStyle.Milestone));
        }

        private void OnTaskSkipped(TaskDefinition task, string reason) =>
            Note(UiStyle.Colour(GameText.Get("feed.skipped", ("task", task.displayName), ("reason", reason)), UiStyle.Warning));

        private void OnSwitchFlipped(SwitchDefinition flipped)
        {
            string line = UiStyle.Colour(GameText.Get("feed.switch", ("switch", flipped.displayName)), UiStyle.Milestone);
            foreach (var task in flipped.unlocksTasks)
                if (task != null)
                    line += " " + GameText.Get("feed.switch.new_task", ("task", task.displayName));
            foreach (var hue in flipped.unlocksPools)
                line += " " + GameText.Get("feed.switch.new_pool", ("pool", GameText.HueName(hue)));
            foreach (var task in flipped.locksTasks)
                if (task != null)
                    line += " " + GameText.Get("feed.switch.locked_task", ("task", task.displayName));
            Note(line);
        }

        private void OnDarknessHeldOff(TaskDefinition task) =>
            Note(UiStyle.Colour(GameText.Get("feed.held_off", ("task", task.displayName)), UiStyle.Milestone));

        private void OnPlanningUnlocked() =>
            Note(UiStyle.Colour(GameText.Get("feed.planning_unlocked"), UiStyle.Milestone));

        private void OnQueueRanOut()
        {
            // Not at the very start of a run: every run begins with nothing queued.
            if (Sim.Loop.TicksElapsed > 1)
                Note(GameText.Get("feed.queue_ran_out"));
        }

        private void Note(string line)
        {
            if (_feed != null)
                _feed.AddNote(line);
        }
    }
}
