using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The planning screen, shown between runs: the map on the left and the Queue column on the right
    /// fill the page, with how many actions were carried and Clear plan above the queue and where the
    /// plan ends and Begin below it. There is only one map and one queue, so this page borrows them
    /// from the main page while it shows (re-parenting them into its slots) and puts them back,
    /// exactly as they were, when another page turns in. Between runs only: it holds no game rules.
    /// </summary>
    public class PlanPage : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private ScreenManager _screens;

        [Header("Borrowed from the main page")]
        [Tooltip("The map (the MapPanel under the main page's Map frame).")]
        [SerializeField] private MapView _map;
        [Tooltip("The Queue column's page (the Queue under the main page's Drawer).")]
        [SerializeField] private QueueDrawer _queue;
        [Tooltip("The queue's own Fold button: pointless here, so it is hidden while the page shows.")]
        [SerializeField] private GameObject _queueFoldButton;
        [Tooltip("Candle light, drawn over the map's backdrop and under its rooms while the page shows. Optional.")]
        [SerializeField] private RectTransform _candleLight;

        [Header("This page")]
        [SerializeField] private RectTransform _mapSlot;
        [SerializeField] private RectTransform _queueSlot;
        [SerializeField] private TMP_Text _title;
        [Tooltip("Above the queue: how many actions were carried over.")]
        [SerializeField] private TMP_Text _carriedLabel;
        [SerializeField] private Button _clearButton;
        [Tooltip("Shown while the queue is empty.")]
        [SerializeField] private TMP_Text _hint;
        [Tooltip("Below the queue: the room the plan ends in.")]
        [SerializeField] private TMP_Text _endsInLabel;
        [SerializeField] private Button _beginButton;
        [Tooltip("Empty, for what planning turns out to need (a run-ahead strip of vitality and stats, say).")]
        [SerializeField] private RectTransform _spareSlot;

        /// <summary>Where a borrowed part sat, to put it back the way it was.</summary>
        private struct Home
        {
            public Transform Parent;
            public int Sibling;
            public Vector2 AnchorMin, AnchorMax, Pivot, OffsetMin, OffsetMax;
        }

        private Home _mapHome, _queueHome, _lightOrigin;
        private CanvasGroup _queueGroup;
        private float _queueAlpha;
        private bool _queueInteractable, _queueBlocks;
        private bool _borrowed;
        private bool _foldButtonWasOn;
        private readonly TextThrottle _text = new TextThrottle(); // the page's words are rebuilt a few times a second

        private Simulation Sim => _game.Simulation;

        private void Start()
        {
            _clearButton.onClick.AddListener(() => Sim.ClearQueue());
            _beginButton.onClick.AddListener(() => _game.BeginLoop());
            ToolTip.On(_clearButton, () => GameText.Get("plan.clear_tip"));
            ToolTip.On(_beginButton, () => GameText.Get("plan.begin_tip", ("loop", Sim.NextLoopNumber)));
            ToolTip.On(_carriedLabel, () => GameText.Get("plan.carried_tip"));
            ToolTip.On(_endsInLabel, () => GameText.Get("plan.ends_in_tip", ("needed", Sim.Settings.byHeartRuns)));
            _queueGroup = _queue.GetComponent<CanvasGroup>();
            _screens.Shown += OnShown;
            GameText.Changed += _text.MarkDirty;
            _game.SimulationChanged += ListenTo;
            ListenTo(_game.Simulation);
            if (_screens.Current == ScreenId.Plan)
                Borrow();
        }

        private void OnDestroy()
        {
            if (_screens != null)
                _screens.Shown -= OnShown;
            GameText.Changed -= _text.MarkDirty;
            if (_game != null)
                _game.SimulationChanged -= ListenTo;
            ListenTo(null);
            // Not handed back here: the scene is going away and the parts may be gone already.
        }

        // The words follow a queue edit at once.
        private Simulation _listeningTo;

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
                _listeningTo.QueueChanged -= _text.MarkDirty;
            _listeningTo = sim;
            if (sim != null)
                sim.QueueChanged += _text.MarkDirty;
            _text.MarkDirty();
        }

        private void OnShown(ScreenId id)
        {
            _text.MarkDirty(); // a page turning in shows its words at once
            if (id == ScreenId.Plan && !_borrowed)
                Borrow();
            else if (id != ScreenId.Plan && _borrowed)
                GiveBack();
        }

        // ---------- Borrowing the map and the queue ----------

        private void Borrow()
        {
            _borrowed = true;
            _mapHome = MoveInto((RectTransform)_map.transform, _mapSlot);
            _queueHome = MoveInto((RectTransform)_queue.transform, _queueSlot);
            _queueAlpha = _queueGroup.alpha;
            _queueInteractable = _queueGroup.interactable;
            _queueBlocks = _queueGroup.blocksRaycasts;
            _foldButtonWasOn = _queueFoldButton.activeSelf;
            _queueFoldButton.SetActive(false);
            if (_candleLight != null)
            {
                _lightOrigin = Remember(_candleLight);
                _candleLight.SetParent(_map.Area.parent, false);
                _candleLight.SetSiblingIndex(_map.Area.GetSiblingIndex()); // over the backdrop, under the rooms and ways
                Stretch(_candleLight);
                _candleLight.gameObject.SetActive(true);
            }
            _map.ShowSpeedLines = true;
            _map.ShowPlanMarks = true;
            _map.JumpToHere();
        }

        private void GiveBack()
        {
            _borrowed = false;
            _map.ShowSpeedLines = false;
            _map.ShowPlanMarks = false;
            if (_candleLight != null)
            {
                _candleLight.gameObject.SetActive(false);
                Restore(_candleLight, _lightOrigin);
            }
            _queueFoldButton.SetActive(_foldButtonWasOn);
            _queueGroup.alpha = _queueAlpha;
            _queueGroup.interactable = _queueInteractable;
            _queueGroup.blocksRaycasts = _queueBlocks;
            Restore((RectTransform)_queue.transform, _queueHome);
            Restore((RectTransform)_map.transform, _mapHome);
        }

        private static Home Remember(RectTransform part) => new Home
        {
            Parent = part.parent,
            Sibling = part.GetSiblingIndex(),
            AnchorMin = part.anchorMin,
            AnchorMax = part.anchorMax,
            Pivot = part.pivot,
            OffsetMin = part.offsetMin,
            OffsetMax = part.offsetMax,
        };

        /// <summary>Puts the part in a slot, filling it. Returns where it was.</summary>
        private static Home MoveInto(RectTransform part, RectTransform slot)
        {
            var home = Remember(part);
            part.SetParent(slot, false);
            Stretch(part);
            return home;
        }

        private static void Stretch(RectTransform part)
        {
            part.anchorMin = Vector2.zero;
            part.anchorMax = Vector2.one;
            part.pivot = new Vector2(0.5f, 0.5f);
            part.offsetMin = Vector2.zero;
            part.offsetMax = Vector2.zero;
            part.localScale = Vector3.one;
        }

        private static void Restore(RectTransform part, Home home)
        {
            part.SetParent(home.Parent, false);
            part.SetSiblingIndex(home.Sibling);
            part.anchorMin = home.AnchorMin;
            part.anchorMax = home.AnchorMax;
            part.pivot = home.Pivot;
            part.offsetMin = home.OffsetMin;
            part.offsetMax = home.OffsetMax;
        }

        // ---------- The page ----------

        private void Update()
        {
            if (!_borrowed || Sim == null)
                return;

            // The drawer hides its page while folded; here the queue is always open.
            if (_queueGroup.alpha != 1f)
                _queueGroup.alpha = 1f;
            _queueGroup.interactable = true;
            _queueGroup.blocksRaycasts = true;

            var sim = Sim;
            bool empty = sim.Queue.Count == 0;
            if (_clearButton.interactable == empty)
                _clearButton.interactable = !empty;
            if (!_text.Due())
                return;
            UiText.Set(_title, GameText.Get("plan.title", ("loop", sim.NextLoopNumber)));
            int carried = sim.CarriedActionCount;
            UiText.Set(_carriedLabel, carried == 0 ? GameText.Get("plan.carried_none")
                : GameText.Get(carried == 1 ? "plan.carried.one" : "plan.carried.many", ("count", carried)));
            UiText.ShowEmpty(_hint, empty, "plan.empty_hint");
            var endsIn = sim.PlanEndsIn;
            UiText.Set(_endsInLabel, endsIn == null ? GameText.Get("plan.nothing_queued")
                : GameText.Get("plan.ends_in", ("room", GameText.TitleInSentence(endsIn.DisplayName))));
        }
    }
}
