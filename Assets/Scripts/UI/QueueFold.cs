using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Folds the Queue column to a strip and opens it again. Folded, the strip (current action and
    /// its bar) sits under the Story box, which gives up that much height, and the map widens;
    /// open, the column pushes the map left. The choice is the player's setting, kept in
    /// PlayerPrefs (not in the run save). Q folds and unfolds it on the main page. While folded, the
    /// strip glows when the player queues something, since the queue itself is out of sight.
    /// </summary>
    // After SlideDrawer.Start, which shuts the drawer: opening it before then would leave it open with its page hidden.
    [DefaultExecutionOrder(100)]
    public class QueueFold : MonoBehaviour
    {
        /// <summary>The PlayerPrefs key (prefixed like the others): 1 when the player last left the queue folded.</summary>
        public const string FoldedKey = "HallOfEchoingMirrors.QueueFold.folded";

        [SerializeField] private GameController _game;
        [Tooltip("Q only works while the main page is showing.")]
        [SerializeField] private ScreenManager _screens;
        [Tooltip("The drawer the Queue column is in (a one-page drawer on the right, pushing the map).")]
        [SerializeField] private SlideDrawer _drawer;
        [Tooltip("The Queue page inside the drawer.")]
        [SerializeField] private CanvasGroup _queuePage;
        [Tooltip("The folded strip under the Story box; shown only while folded.")]
        [SerializeField] private GameObject _strip;
        [Tooltip("The Story box: its bottom edge rises by the strip's height while folded.")]
        [SerializeField] private RectTransform _story;
        [Tooltip("How far the Story box's bottom edge rises while the strip is showing.")]
        [SerializeField, Min(0f)] private float _stripHeight = 84f;
        [Tooltip("Under the queue: folds it.")]
        [SerializeField] private Button _foldButton;
        [Tooltip("On the strip: opens the queue.")]
        [SerializeField] private Button _unfoldButton;

        private float _storyBottom; // the Story box's bottom edge with the queue open

        private Simulation _watched;
        private Graphic _stripGraphic;

        public bool Folded { get; private set; }

        private void Start()
        {
            _storyBottom = _story.offsetMin.y;
            _foldButton.onClick.AddListener(() => SetFolded(true));
            _unfoldButton.onClick.AddListener(() => SetFolded(false));
            _stripGraphic = _strip.GetComponent<Graphic>();
            Apply(PlayerPrefs.GetInt(FoldedKey, 0) == 1, instant: true);
            _game.SimulationChanged += Watch;
            Watch(_game.Simulation);
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= Watch;
            if (_watched != null)
                _watched.ActionQueued -= Flash;
        }

        // Loading replaces the simulation, so the event is re-hooked each time.
        private void Watch(Simulation sim)
        {
            if (_watched != null)
                _watched.ActionQueued -= Flash;
            _watched = sim;
            if (_watched != null)
                _watched.ActionQueued += Flash;
        }

        private void Flash()
        {
            if (Folded)
                Glow.Flash(_stripGraphic);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame && _screens.Current == ScreenId.Main)
                SetFolded(!Folded);
        }

        private void SetFolded(bool folded)
        {
            if (folded == Folded)
                return;
            Apply(folded);
            PlayerPrefs.SetInt(FoldedKey, folded ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Apply(bool folded, bool instant = false)
        {
            Folded = folded;
            ToolTipPanel.HideAny(); // the button that was pressed is about to vanish
            if (folded)
                _drawer.Close(instant);
            else
                _drawer.OpenPage(_queuePage, instant);
            _strip.SetActive(folded);
            var min = _story.offsetMin;
            min.y = _storyBottom + (folded ? _stripHeight : 0f);
            _story.offsetMin = min;
        }
    }
}
