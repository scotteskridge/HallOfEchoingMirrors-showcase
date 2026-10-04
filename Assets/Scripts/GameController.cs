using System;
using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using UnityEngine;

namespace HallOfEchoingMirrors
{
    /// <summary>
    /// Connects the Unity scene to the simulation (each tick from the TickEngine advances the rules),
    /// and runs games and save slots: new game, load, save, delete. Saves automatically at the end
    /// of every loop, whenever a switch flips, and on quit.
    /// </summary>
    [RequireComponent(typeof(TickEngine))]
    public class GameController : MonoBehaviour
    {
        [SerializeField] private TickEngine _tickEngine;
        [SerializeField] private LoopSettings _loopSettings;
        [Tooltip("Tasks, switches and story.")]
        [SerializeField] private GameContent _content;
        [Tooltip("Every line of on-screen text (Assets/Text/game_text.txt).")]
        [SerializeField] private TextAsset _gameText;

#if UNITY_EDITOR
        private string _gameTextPath;
        private System.DateTime _gameTextSaved;
        private float _nextTextCheck;
#endif

        private ContentIndex _index;

        /// <summary>
        /// The game being played. Before a game is started or loaded, this is a blank one so the
        /// screens have something to show behind the menu.
        /// </summary>
        public Simulation Simulation { get; private set; }
        public TickEngine TickEngine => _tickEngine;

        /// <summary>The save slot being played (0-2), or -1 before a game is started or loaded.</summary>
        public int ActiveSlot { get; private set; } = -1;
        public bool HasActiveGame => ActiveSlot >= 0;

        /// <summary>A different game was started or loaded. UI that listens to the simulation should re-listen.</summary>
        public event Action<Simulation> SimulationChanged;
        /// <summary>A slot's file changed (saved or deleted), so the menu can refresh.</summary>
        public event Action SlotsChanged;
        /// <summary>A save couldn't be written (the player-facing reason). The Menu shows it; the Console gets the details.</summary>
        public event Action<string> SaveFailed;

        private void Awake()
        {
            if (_tickEngine == null)
                _tickEngine = GetComponent<TickEngine>();

            if (_loopSettings == null)
            {
                Debug.LogError("GameController: drag a LoopSettings asset into the 'Loop Settings' field in the Inspector.", this);
                enabled = false;
                return;
            }

            LoadGameText();
            _index = new ContentIndex(_content);
            UseSimulation(new Simulation(_loopSettings, TickEngine.TicksPerSecond, _content));
        }

        // ---------- Game text ----------

        private void LoadGameText()
        {
            if (_gameText == null)
            {
                Debug.LogError("GameController: drag Assets/Text/game_text.txt into the 'Game Text' field in the Inspector.", this);
                return;
            }
            Report(GameText.Load(_gameText.text));
#if UNITY_EDITOR
            _gameTextPath = System.IO.Path.GetFullPath(UnityEditor.AssetDatabase.GetAssetPath(_gameText));
            _gameTextSaved = System.IO.File.GetLastWriteTimeUtc(_gameTextPath);
#endif
        }

        private void Report(List<string> problems)
        {
            foreach (var problem in problems)
                Debug.LogWarning($"Game text: {problem}.", _gameText);
        }

#if UNITY_EDITOR
        /// <summary>In the editor, saving the text file while playing updates the words at once.</summary>
        private void Update()
        {
            if (_gameTextPath == null || Time.unscaledTime < _nextTextCheck)
                return;
            _nextTextCheck = Time.unscaledTime + 1f;
            var saved = System.IO.File.GetLastWriteTimeUtc(_gameTextPath);
            if (saved == _gameTextSaved)
                return;
            _gameTextSaved = saved;
            Report(GameText.Load(System.IO.File.ReadAllText(_gameTextPath)));
            Debug.Log("Game text reloaded.", _gameText);
        }
#endif

        private void OnEnable()
        {
            if (Simulation != null)
                _tickEngine.Ticked += OnTick;
        }

        private void OnDisable()
        {
            if (_tickEngine != null)
                _tickEngine.Ticked -= OnTick;
        }

        private void OnApplicationQuit() => SaveNow();

        // Saves asked for during a tick (an action done, a switch flipped, a pause) are written once
        // the frame's ticks are over, so a save never catches an action half-finished.
        private bool _saveWanted;

        private void RequestSave() => _saveWanted = true;

        private void LateUpdate()
        {
            if (!_saveWanted)
                return;
            _saveWanted = false;
            SaveNow();
        }

        private void OnTick() => Simulation.Tick();

        // ---------- Games and slots ----------

        /// <summary>Starts a fresh game in a slot (replacing whatever was saved there).</summary>
        public void NewGame(int slot)
        {
            SaveNow(); // keep the game being left
            UseSimulation(new Simulation(_loopSettings, TickEngine.TicksPerSecond, _content));
            ActiveSlot = slot;
            SaveNow();
        }

        /// <summary>Loads the game saved in a slot. Returns false (with a reason) if it can't.</summary>
        public bool LoadGame(int slot, out string error)
        {
            var data = SaveSlots.Read(slot, out error);
            if (data == null)
                return false;

            SaveNow(); // keep the game being left
            var warnings = new List<string>();
            // A run under way when it was saved carries on where it was, paused until the player resumes.
            var loaded = SaveSerializer.Load(data, _index, _loopSettings, TickEngine.TicksPerSecond, _content, warnings, out bool resumed);
            foreach (string warning in warnings)
                Debug.LogWarning($"Loading slot {slot + 1}: {warning}");

            UseSimulation(loaded);
            ActiveSlot = slot;
            if (resumed)
                Pause(GameText.Get("run.pause_reason.resumed"));
            return true;
        }

        /// <summary>Saves the game being played to its slot. Does nothing before a game is started (true: nothing failed). Returns false if the write failed.</summary>
        public bool SaveNow()
        {
            if (!HasActiveGame || Simulation == null)
                return true;

            if (SaveSlots.Write(ActiveSlot, SaveSerializer.Capture(Simulation), out string error))
            {
                SlotsChanged?.Invoke();
                return true;
            }
            Debug.LogError($"Couldn't save to slot {ActiveSlot + 1}: {error}");
            SaveFailed?.Invoke(GameText.Get("menu.couldnt_save", ("slot", ActiveSlot + 1), ("error", error)));
            return false;
        }

        /// <summary>Deletes a slot's save. Returns false, with the reason, if the file couldn't be removed (nothing else changes then).</summary>
        public bool DeleteSlot(int slot, out string error)
        {
            if (!SaveSlots.Delete(slot, out error))
            {
                Debug.LogError($"Couldn't delete slot {slot + 1}: {error}");
                return false;
            }
            if (slot == ActiveSlot)
            {
                // The game being played has no save any more: back to a blank game behind the menu.
                UseSimulation(new Simulation(_loopSettings, TickEngine.TicksPerSecond, _content));
                ActiveSlot = -1;
            }
            SlotsChanged?.Invoke();
            return true;
        }

        private void UseSimulation(Simulation next)
        {
            Simulation = next;
            next.ContentProblem = Debug.LogError; // Core has no logger: content mistakes reach the Console from here

            // Speed belongs to the game that earned it: another game (or a blank one) mustn't
            // inherit it, or it runs fast with the Speed button hidden and no way back to ×1.
            // The dev panel's speed is limited too, so the editor behaves as players will see it.
            _tickEngine.LimitSpeedTo(next.FastestSpeedUnlocked);
            _tickEngine.RoomSpeed = () => next.RoomSpeedNow;

            // When the game pauses and why (the rules are PauseControl's); the clock follows it.
            _pause = new PauseControl(next, _loopSettings);
            _pause.PausedChanged += paused =>
            {
                _tickEngine.Paused = paused;
                if (paused)
                    RequestSave();
            };
            Resume();

            // Autosave whenever something is done: each action, each switch, a run's end, a pause.
            // A run under way is saved whole, so closing the game loses at most the action under way.
            // (The old simulation is simply dropped, and its handlers with it.)
            next.TaskCompleted += _ => RequestSave();
            next.SwitchFlipped += _ => RequestSave();
            next.LoopEnded += RequestSave;

            SimulationChanged?.Invoke(next);
        }

        // ---------- Running the game ----------

        private PauseControl _pause;

        public bool IsPaused => _pause != null && _pause.IsPaused;

        /// <summary>The game's content (for presentation that reads it directly, e.g. blurbs).</summary>
        public GameContent Content => _content;

        /// <summary>Why the game is paused, e.g. "Nothing queued". Null while running.</summary>
        public string PauseReason => _pause?.Reason;

        public void Resume()
        {
            _pause?.Resume();
            // Also before any game exists; with nothing queued, PauseControl keeps it paused.
            _tickEngine.Paused = _pause != null && _pause.IsPaused;
        }

        /// <summary>Pauses the game (the Pause button, a story pop-up). Stays paused until resumed.</summary>
        public void Pause(string reason = null) => _pause?.Pause(reason);

        /// <summary>The player ends the run early (from the End run button).</summary>
        public void EndRunEarly()
        {
            Simulation.EndRunEarly();
            Resume();
        }

        /// <summary>Starts the next loop from planning, or restarts the current one (dev tools).</summary>
        public void BeginLoop()
        {
            Simulation.BeginLoop();
            Resume();
        }
    }
}
