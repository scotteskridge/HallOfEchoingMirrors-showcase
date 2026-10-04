using System;
using System.Collections;
using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>The game's screens, like pages of a book. Add new ones at the end.</summary>
    public enum ScreenId
    {
        /// <summary>The end-of-run summary (it was the Plan page, before planning moved to the main page).</summary>
        Summary = 0,
        /// <summary>Retired: the old Run page. Kept so the numbers don't shift.</summary>
        Run = 1,
        /// <summary>Save slots, new game, and (later) settings and achievements.</summary>
        Menu = 2,
        /// <summary>The main page: everything during a run (queue, actions, stats, story).</summary>
        Main = 3,
        /// <summary>The planning screen between runs: the map and the queue fill the page, and Begin starts the run.</summary>
        Plan = 4,
    }

    /// <summary>
    /// Shows one page at a time, sliding pages in and out of view like turning pages of a book.
    /// In the editor, pages can sit anywhere in the Scene view (side by side, so they're easy to
    /// edit); when the game starts, this moves them itself: the current page to the centre, the
    /// rest out of sight. Pages stay active while out of sight, so their scripts keep updating.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        [Serializable]
        public class ScreenEntry
        {
            public ScreenId id;
            public CanvasGroup group;
            [Tooltip("Optional tab button that turns to this screen.")]
            public Button tab;
        }

        [SerializeField] private GameController _game;
        [SerializeField] private List<ScreenEntry> _screens = new List<ScreenEntry>();
        [SerializeField] private ScreenId _startScreen = ScreenId.Menu;
        [Tooltip("Turn to the main page when a run begins, and to the summary when it ends.")]
        [SerializeField] private bool _followTheRun = true;
        [Tooltip("How long a page takes to slide into place.")]
        [SerializeField, Min(0f)] private float _transitionSeconds = 0.35f;

        public ScreenId Current { get; private set; }

        /// <summary>Raised as a page starts to turn in (or is placed at the start), so a page can borrow what it shows from another.</summary>
        public event Action<ScreenId> Shown;

        /// <summary>The pages, in tab order. The editor tools use this to lay them out.</summary>
        public IReadOnlyList<ScreenEntry> Screens => _screens;

        private Coroutine _transition;
        private Simulation _listeningTo;

        private void Start()
        {
            foreach (var screen in _screens)
            {
                var target = screen.id;
                if (screen.tab != null)
                    screen.tab.onClick.AddListener(() => Show(target));
            }

            ShowInstantly(_startScreen);

            _game.SimulationChanged += ListenTo;
            ListenTo(_game.Simulation);
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= ListenTo;
            ListenTo(null);
        }

        // Re-checked every frame: which tabs can be used depends on whether a game is going.
        private void Update() => UpdateTabs();

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
            {
                _listeningTo.LoopStarted -= OnLoopStarted;
                _listeningTo.LoopEnded -= OnLoopEnded;
            }
            _listeningTo = sim;
            if (sim != null)
            {
                sim.LoopStarted += OnLoopStarted;
                sim.LoopEnded += OnLoopEnded;
            }
        }

        private void OnLoopStarted()
        {
            if (_followTheRun)
                Show(ScreenId.Main);
        }

        private void OnLoopEnded()
        {
            if (_followTheRun)
                Show(ScreenId.Summary);
        }

        public void ShowPlan() => Show(ScreenId.Plan);

        /// <summary>The page the game is played on: Main while a run is under way, the planning screen between runs.</summary>
        public void ShowGame() => Show(RunUnderWay ? ScreenId.Main : PlanningUnlocked ? ScreenId.Plan : ScreenId.Summary);

        public void Show(ScreenId id)
        {
            // Even mid-turn: a second Show of the page already being turned to would restart the turn
            // (New game asks for Main twice, once from the menu and once from LoopStarted).
            if (id == Current)
                return;

            var from = Find(Current);
            var to = Find(id);
            if (to == null)
                return;

            // Pages later in the tab order come in from the right, earlier ones from the left.
            int direction = _screens.IndexOf(to) > _screens.IndexOf(from) ? 1 : -1;

            if (_transition != null)
            {
                StopCoroutine(_transition);
                foreach (var screen in _screens)
                    if (screen != from)
                        PutAway(screen);
            }
            Current = id;
            UpdateTabs();
            Shown?.Invoke(id);
            _transition = StartCoroutine(Turn(from, to, direction));
        }

        private void ShowInstantly(ScreenId id)
        {
            Current = id;
            foreach (var screen in _screens)
            {
                if (screen.id == id)
                    PlaceInView(screen, 0f);
                else
                    PutAway(screen);
            }
            UpdateTabs();
            Shown?.Invoke(id);
        }

        /// <summary>
        /// The page turn: the new page slides into view as the old one slides away. A book-style
        /// page-turn animation can replace this one method later.
        /// </summary>
        private IEnumerator Turn(ScreenEntry from, ScreenEntry to, int direction)
        {
            float width = PageWidth(to);
            SetInteractive(to, false);
            if (from != null)
                SetInteractive(from, false);

            for (float t = 0f; t < _transitionSeconds; t += Time.unscaledDeltaTime)
            {
                float eased = Mathf.SmoothStep(0f, 1f, t / _transitionSeconds);
                PlaceInView(to, direction * width * (1f - eased));
                if (from != null && from != to)
                    PlaceInView(from, -direction * width * eased);
                yield return null;
            }

            PlaceInView(to, 0f);
            SetInteractive(to, true);
            if (from != null && from != to)
                PutAway(from);
            _transition = null;
        }

        /// <summary>Puts a page in the camera's view, shifted sideways by <paramref name="offsetX"/>.</summary>
        private static void PlaceInView(ScreenEntry screen, float offsetX)
        {
            var rect = PageRect(screen);
            if (rect == null)
                return;
            // Fill the screen, whatever size or position it was given in the editor.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = new Vector2(offsetX, 0f);
        }

        /// <summary>Moves a page out of sight (far to the side) and makes it unclickable.</summary>
        private static void PutAway(ScreenEntry screen)
        {
            PlaceInView(screen, PageWidth(screen) * 3f);
            SetInteractive(screen, false);
        }

        private static void SetInteractive(ScreenEntry screen, bool on)
        {
            if (screen?.group == null)
                return;
            screen.group.alpha = 1f;
            screen.group.interactable = on;
            screen.group.blocksRaycasts = on;
        }

        private static RectTransform PageRect(ScreenEntry screen) =>
            screen?.group != null ? screen.group.transform as RectTransform : null;

        /// <summary>The width of the view a page slides across (its parent: the canvas).</summary>
        private static float PageWidth(ScreenEntry screen)
        {
            var parent = PageRect(screen)?.parent as RectTransform;
            return parent != null ? parent.rect.width : 1920f;
        }

        private void UpdateTabs()
        {
            // The current screen's tab is greyed out, like a bookmark you're already on. Until a
            // game is started or loaded, only the Menu can be turned to.
            foreach (var screen in _screens)
            {
                if (screen.tab == null)
                    continue;
                // The Main page is for a run under way: between runs there is nowhere to start one from there.
                // The Plan page is earned (Feed your hours to the flames).
                bool available = screen.id == ScreenId.Menu ||
                    (_game.HasActiveGame && (screen.id != ScreenId.Main || RunUnderWay) && (screen.id != ScreenId.Plan || PlanningUnlocked));
                bool interactable = available && screen.id != Current;
                if (screen.tab.interactable != interactable)
                    screen.tab.interactable = interactable;
                // The planning screen keeps only the Menu tab: Main isn't usable between runs and the
                // Summary can't be returned to, and the other tabs crowd the queue column's head.
                bool visible = screen.id == ScreenId.Menu || Current != ScreenId.Plan;
                if (screen.tab.gameObject.activeSelf != visible)
                    screen.tab.gameObject.SetActive(visible);
            }
        }

        private bool PlanningUnlocked => _game.Simulation != null && _game.Simulation.PlanningUnlocked;

        private bool RunUnderWay => _game.Simulation != null && _game.Simulation.RunUnderWay;

        private ScreenEntry Find(ScreenId id) => _screens.Find(s => s.id == id);
    }
}
