using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Clara's lines in the story feed. One as each action begins (a "Starting" bucket if one
    /// fits, otherwise an ordinary one), then one every few seconds while she's at it (a random
    /// 3–8 by default). All of it counts real seconds, not game time, so the lines stay readable at
    /// any game speed; the clock stops while the game is paused.
    /// </summary>
    public class BlurbTeller : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private StoryFeed _feed;

        private readonly BlurbPicker _picker = new BlurbPicker();
        private Simulation _listeningTo;
        private int _loop = -1;
        private float _realClock;       // real seconds spent running (not paused)
        private float _nextAt;          // _realClock when the timer next offers a line
        private float _lastSpokeAt = float.NegativeInfinity;

        private BlurbLibrary Library => _game.Content != null ? _game.Content.blurbs : null;

        private void OnEnable()
        {
            _game.SimulationChanged += ListenTo;
            ListenTo(_game.Simulation);
        }

        private void OnDisable()
        {
            _game.SimulationChanged -= ListenTo;
            ListenTo(null);
        }

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
                _listeningTo.TaskStarted -= OnTaskStarted;
            _listeningTo = sim;
            _loop = -1;
            if (sim != null)
                sim.TaskStarted += OnTaskStarted;
        }

        private void OnTaskStarted(TaskDefinition task)
        {
            var library = Library;
            if (library == null)
                return;

            string line = _picker.Pick(library, _listeningTo, _realClock, BlurbMoment.Starting)
                          ?? _picker.Pick(library, _listeningTo, _realClock, BlurbMoment.During);
            if (Say(line, library))
                _nextAt = _realClock + Wait(library); // the timer starts again from here
        }

        private void Update()
        {
            var sim = _listeningTo;
            var library = Library;
            if (sim == null || library == null || !sim.RunUnderWay)
                return;

            if (_game.IsPaused)
                return;
            _realClock += Time.unscaledDeltaTime;

            // A new run: the first timed line comes after one wait, not at once.
            if (sim.Persistent.LoopNumber != _loop)
            {
                _loop = sim.Persistent.LoopNumber;
                _nextAt = _realClock + Wait(library);
            }

            if (_realClock < _nextAt)
                return;
            _nextAt = _realClock + Wait(library);
            Say(_picker.Pick(library, sim, nowRealSeconds: _realClock), library);
        }

        /// <summary>Adds the line to the feed, unless there's none or it's too soon (fast-forwarding).</summary>
        private bool Say(string line, BlurbLibrary library)
        {
            if (line == null || _realClock - _lastSpokeAt < library.minRealSecondsApart)
                return false;
            _feed.AddStory(line);
            _lastSpokeAt = _realClock;
            return true;
        }

        /// <summary>A random wait, in real seconds, between the library's shortest and longest.</summary>
        private static float Wait(BlurbLibrary library) =>
            Random.Range(library.minSeconds, Mathf.Max(library.minSeconds, library.maxSeconds));
    }
}
