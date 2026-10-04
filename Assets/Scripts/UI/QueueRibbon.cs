using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The folded queue: a strip under the Story box with what Clara is doing now (name, progress
    /// bar, time left) and what comes next. With nothing queued, a hint instead; between runs, the
    /// queue planned for the next run. Shown only while the Queue column is folded (QueueFold).
    /// </summary>
    public class QueueRibbon : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("Holds the name, bar, time left and next: hidden while nothing is queued.")]
        [SerializeField] private GameObject _nowParts;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TMP_Text _timeLeftLabel;
        [SerializeField] private TMP_Text _nextLabel;
        [Tooltip("Shown instead while nothing is queued.")]
        [SerializeField] private TMP_Text _hintLabel;

        private Simulation Sim => _game.Simulation;

        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;

            var entries = sim.Queue.Entries;
            bool empty = entries.Count == 0;
            UiText.SetActive(_nowParts.transform, !empty);
            UiText.ShowEmpty(_hintLabel, empty, sim.Loop.IsOver ? "ribbon.empty_between_runs" : "ribbon.empty");
            if (empty)
                return;

            // The top of the queue is what she's doing (or will do first, between runs).
            var current = entries[0];
            UiText.Set(_nameLabel, ActionText.NameOf(sim, 0, current));
            _progressBar.value = QueueEntryText.Progress(sim, current);
            UiText.Set(_timeLeftLabel, QueueEntryText.TimeLeft(sim, current) + RoomSpeedNote(sim));
            UiText.Set(_nextLabel, entries.Count > 1
                ? GameText.Get("ribbon.then", ("action", ActionText.NameOf(sim, 1, entries[1])))
                : "");
        }

        /// <summary>Nothing until room speed is earned. "×1.8" while a room known by heart speeds the clock, "held" while a skipped or refused action or a milestone holds it at the player's speed.</summary>
        private static string RoomSpeedNote(Simulation sim)
        {
            if (sim.Loop.RunningEntry == null || !sim.RoomSpeedUnlocked)
                return "";
            if (sim.SpeedHeld)
                return "  " + GameText.Get("ribbon.speed_held");
            float speed = sim.RoomSpeedNow;
            return speed > 1f ? "  " + GameText.Get("ribbon.room_speed", ("speed", UiText.Number(speed))) : "";
        }
    }
}
