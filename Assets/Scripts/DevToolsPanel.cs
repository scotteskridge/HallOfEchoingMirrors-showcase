using UnityEngine;
using UnityEngine.InputSystem;

namespace HallOfEchoingMirrors
{
    /// <summary>
    /// A developer-only overlay for testing: game speed, pause, restart, and live pool values.
    /// Drawn with Unity's quick debug GUI, so it needs no scene setup. Stripped from release builds.
    /// Drag it by its title bar; F1 or ` (backtick) hides and shows it; it remembers where you left it.
    /// In the editor, Ctrl + right-click any UI element in the Game view to select it.
    /// </summary>
    [RequireComponent(typeof(GameController))]
    public class DevToolsPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        const int WindowId = 7351;
        const string PrefsKey = "HallOfEchoingMirrors.DevTools.";
        const float Width = 320f;

        [SerializeField] private bool _visible = true;
        [Tooltip("The Jump to buttons, in order (plan 027d): each puts the kept state at a point in the game and begins a run. " +
                 "Only adds, never undoes: use a spare save slot.")]
        [SerializeField] private System.Collections.Generic.List<Core.DevJumpStage> _jumpStages =
            new System.Collections.Generic.List<Core.DevJumpStage>();

        private GameController _game;
        private Rect _window = new Rect(10f, 10f, Width, 0f);
        private bool _minimised;

        private void Awake()
        {
            _game = GetComponent<GameController>();
            _window.x = PlayerPrefs.GetFloat(PrefsKey + "x", _window.x);
            _window.y = PlayerPrefs.GetFloat(PrefsKey + "y", _window.y);
            _minimised = PlayerPrefs.GetInt(PrefsKey + "minimised", 0) == 1;
        }

        private void OnDisable()
        {
            PlayerPrefs.SetFloat(PrefsKey + "x", _window.x);
            PlayerPrefs.SetFloat(PrefsKey + "y", _window.y);
            PlayerPrefs.SetInt(PrefsKey + "minimised", _minimised ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (Keyboard.current != null && (Keyboard.current.f1Key.wasPressedThisFrame || Keyboard.current.backquoteKey.wasPressedThisFrame))
                _visible = !_visible;

#if UNITY_EDITOR
            // Ctrl + right-click in the Game view selects the UI element under the mouse.
            // Right-click, because a left-click would also press whatever button is there.
            if (Mouse.current != null && Keyboard.current != null &&
                Keyboard.current.ctrlKey.isPressed && Mouse.current.rightButton.wasPressedThisFrame)
                SelectUiUnderMouse();
#endif
        }

#if UNITY_EDITOR
        private readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> _hits =
            new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();

        private void SelectUiUnderMouse()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events == null)
                return;

            var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = Mouse.current.position.ReadValue() };
            _hits.Clear();
            events.RaycastAll(pointer, _hits);
            if (_hits.Count == 0)
            {
                Debug.Log("Dev tools: nothing clickable under the mouse. (Elements with Raycast Target off can't be picked.)");
                return;
            }

            var hit = _hits[0].gameObject;
            UnityEditor.Selection.activeGameObject = hit;
            UnityEditor.EditorGUIUtility.PingObject(hit);

            // Copies made at runtime (action rows, queue rows, gem slices) vanish when Play stops.
            // Point at the template or prefab to edit instead.
            var copy = FindRuntimeCopy(hit.transform);
            if (copy == null)
            {
                Debug.Log($"Dev tools: selected '{PathOf(hit.transform)}'.", hit);
                return;
            }

            string templateName = copy.name.Replace("(Clone)", "").Trim();
            var template = copy.parent != null ? copy.parent.Find(templateName) : null;
            var prefab = template != null ? UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(template.gameObject) : null;
            string where = prefab != null
                ? $"edit the prefab {UnityEditor.AssetDatabase.GetAssetPath(prefab)}"
                : $"edit the template '{templateName}'";
            Debug.Log($"Dev tools: selected '{PathOf(hit.transform)}'. This is a runtime copy, so changes are lost when Play stops. " +
                      $"To change it for good, {where} (click this message to highlight it).",
                      prefab != null ? prefab : template != null ? template.gameObject : hit);
        }

        // The outermost copy: clicking a button inside a copied row should point at the row.
        private static Transform FindRuntimeCopy(Transform t)
        {
            Transform outermost = null;
            for (; t != null; t = t.parent)
                if (t.name.EndsWith("(Clone)"))
                    outermost = t;
            return outermost;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (t = t.parent; t != null && t.GetComponent<Canvas>() == null; t = t.parent)
                path = t.name + "/" + path;
            return path;
        }
#endif

        private void OnGUI()
        {
            if (!_visible || _game.Simulation == null)
                return;

            _window.height = 0f; // let the window fit its contents
            _window = GUILayout.Window(WindowId, _window, DrawWindow, "Dev tools (drag me, F1 or ` hides, Ctrl+right-click selects)", GUILayout.Width(Width));

            // Keep it on screen, however it was dragged or however the Game view was resized.
            _window.x = Mathf.Clamp(_window.x, 0f, Mathf.Max(0f, Screen.width - _window.width));
            _window.y = Mathf.Clamp(_window.y, 0f, Mathf.Max(0f, Screen.height - 20f));
        }

        private void DrawWindow(int id)
        {
            var engine = _game.TickEngine;
            var sim = _game.Simulation;

            if (GUILayout.Button(_minimised ? "Show" : "Minimise"))
                _minimised = !_minimised;

            if (!_minimised)
            {
                GUILayout.Label($"Speed: x{engine.Speed:0}");
                engine.Speed = Mathf.Round(GUILayout.HorizontalSlider(engine.Speed, 1f, TickEngine.MaxSpeed));

                GUILayout.BeginHorizontal();
                foreach (float preset in new[] { 1f, 5f, 20f, 50f })
                    if (GUILayout.Button($"x{preset:0}"))
                        engine.Speed = preset;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                // Through the game's own pause, so its reasons and auto-resume stay right.
                if (GUILayout.Button(engine.Paused ? "Resume" : "Pause"))
                {
                    if (_game.IsPaused)
                        _game.Resume();
                    else
                        _game.Pause();
                }
                if (GUILayout.Button(sim.Phase == Core.LoopPhase.Running ? "Restart loop" : "Begin loop"))
                    _game.BeginLoop();
                GUILayout.EndHorizontal();

                string status = sim.Phase == Core.LoopPhase.BetweenRuns ? "  (planning)" : "";
                GUILayout.Label($"Loop {sim.Persistent.LoopNumber}, {sim.Loop.TicksElapsed / TickEngine.TicksPerSecond}s{status}");
                GUILayout.Label(DescribePools(sim.Loop));
                // For pacing: a whole game at 1x, however fast it was played.
                string runs = sim.Phase == Core.LoopPhase.Running ? $"{sim.Persistent.LoopNumber} (this one included)" : $"{sim.Persistent.LoopNumber}";
                GUILayout.Label($"Runs played: {runs}");
                GUILayout.Label($"Game time played: {DescribeTime(sim.TotalTicksPlayed / (float)TickEngine.TicksPerSecond)}");
                // The run history behind the Summary's Last run and Longest columns.
                var longest = sim.LongestRun;
                string record = longest == null ? "none yet" :
                    $"{DescribeTime(longest.Ticks / (float)TickEngine.TicksPerSecond)} (loop {longest.LoopNumber})";
                GUILayout.Label($"Runs recorded: {sim.Persistent.RunHistory.Count}   Longest: {record}");
                DrawRoomRuns(sim);
                DrawJumpStages();
            }

            // Dragging by the title bar strip at the top of the window.
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        /// <summary>E.g. "1h 07m 32s" (game time, as a player at x1 would spend it).</summary>
        private static string DescribeTime(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            return whole >= 3600
                ? $"{whole / 3600}h {whole / 60 % 60:00}m {whole % 60:00}s"
                : $"{whole / 60}m {whole % 60:00}s";
        }

        // One row per room: runs worked, and - / + / "by heart" buttons (the start room's count comes from runs finished, so it has none).
        private void DrawRoomRuns(Core.Simulation sim)
        {
            GUILayout.Label($"Rooms worked in (runs / by heart at {sim.Settings.byHeartRuns}):");
            foreach (var room in sim.AllNodes)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{room.DisplayName} {sim.RunsWorkedIn(room)}{(sim.IsKnownByHeart(room) ? " ✓" : "")}");
                if (room != sim.StartNode)
                {
                    int runs = sim.RunsWorkedIn(room);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                        sim.DevSetRoomRuns(room, runs - 1);
                    if (GUILayout.Button("+", GUILayout.Width(24f)))
                        sim.DevSetRoomRuns(room, runs + 1);
                    if (GUILayout.Button("by heart", GUILayout.Width(64f)))
                        sim.DevSetRoomRuns(room, sim.Settings.byHeartRuns);
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>The Jump to stages, in button order.</summary>
        public System.Collections.Generic.IReadOnlyList<Core.DevJumpStage> JumpStages => _jumpStages;

        /// <summary>
        /// What a Jump to button does: ends the run under way, puts the kept state at the stage, begins a new
        /// run and saves. Public so editor scripts and checks can press it without the debug GUI.
        /// </summary>
        public void JumpTo(Core.DevJumpStage stage)
        {
            if (stage == null)
                throw new System.ArgumentNullException(nameof(stage));
            var sim = _game.Simulation;
            if (sim.Phase == Core.LoopPhase.Running && sim.Loop.TicksElapsed > 0)
                _game.EndRunEarly();
            _game.Simulation.JumpTo(stage);
            _game.BeginLoop();
            _game.SaveNow();
            Debug.Log($"Dev tools: jumped to {stage.DisplayName}.", stage);
        }

        private void DrawJumpStages()
        {
            if (_jumpStages.Count == 0)
                return;
            GUILayout.Label("Jump to (adds to this game; use a spare slot):");
            foreach (var stage in _jumpStages)
                if (stage != null && GUILayout.Button(stage.DisplayName))
                    JumpTo(stage);
        }

        private static string DescribePools(Core.LoopState loop)
        {
            string text = $"{loop.Vitality.Name} {loop.Vitality.Current:0.0}";
            foreach (var pool in loop.Pools)
                text += $"  {pool.Name} {pool.Current:0.0}";
            return text;
        }
#endif
    }
}
