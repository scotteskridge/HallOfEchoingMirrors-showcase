using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// A drawer that slides in from an edge, holding pages (the Queue column is a one-page drawer on
    /// the right). Bookmark tabs, if it has any, open a page: clicking a tab slides the drawer in with
    /// that page; clicking it again slides it away; another tab switches page. It doesn't pause the
    /// game. The panel sits in a masked area, so sliding past its edge hides it. It can push another part (the map) aside as it comes, rather than cover it.
    /// </summary>
    public class SlideDrawer : MonoBehaviour
    {
        /// <summary>No page open.</summary>
        public const int None = -1;

        /// <summary>The side the drawer slides in from.</summary>
        public enum Edge { Bottom, Top, Left, Right }

        [Serializable]
        public class Page
        {
            [Tooltip("The bookmark tab that opens this page.")]
            public Button tab;
            [Tooltip("The page itself, inside the panel. Shown only while it's the open one.")]
            public CanvasGroup content;
        }

        [Tooltip("The part that slides. Place it (in the editor) where it sits when open, touching the edge below.")]
        [SerializeField] private RectTransform _panel;
        [Tooltip("The edge it slides in from and out past. The panel must touch this edge of its masked area " +
                 "when open, or it won't disappear fully when closed.")]
        [SerializeField] private Edge _edge = Edge.Bottom;
        [SerializeField] private List<Page> _pages = new List<Page>();
        [Tooltip("How long the drawer takes to slide in or out (real time, so game speed doesn't change it).")]
        [SerializeField, Min(0f)] private float _slideSeconds = 0.25f;
        [Tooltip("Optional: a part (the map) whose edge on the drawer's side moves in with the drawer, so the " +
                 "drawer pushes it aside instead of covering it. Its contents keep their size; it just shows less.")]
        [SerializeField] private RectTransform _pushed;

        /// <summary>The open page's index, or None.</summary>
        public int Open { get; private set; } = None;

        private CanvasGroup _panelGroup;
        private Vector2 _home;
        private float _openness; // 0 closed, 1 open
        private Vector2 _placedAtSize;
        private Vector2 _pushedMin, _pushedMax; // the pushed part's edges with the drawer closed

        /// <summary>The page that's open after a tab is clicked: the same tab closes it, another switches.</summary>
        public static int OpenAfterClick(int open, int clicked) => clicked == open ? None : clicked;

        /// <summary>
        /// How far the panel sits from where it rests when open: its whole size past the chosen edge
        /// when closed (<paramref name="openness"/> 0), none when open (1).
        /// </summary>
        public static Vector2 Offset(Edge edge, Vector2 size, float openness)
        {
            float away = 1f - Mathf.Clamp01(openness);
            switch (edge)
            {
                case Edge.Bottom: return new Vector2(0f, -size.y * away);
                case Edge.Top:    return new Vector2(0f, size.y * away);
                case Edge.Left:   return new Vector2(-size.x * away, 0f);
                case Edge.Right:  return new Vector2(size.x * away, 0f);
                default: throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unknown drawer edge.");
            }
        }

        /// <summary>
        /// How far the drawer reaches in from its edge: nothing when closed (<paramref name="openness"/> 0),
        /// its whole height (or width, from the sides) when open (1). The pushed part moves its edge in by this.
        /// </summary>
        public static float CoveredInset(Edge edge, Vector2 size, float openness)
        {
            float open = Mathf.Clamp01(openness);
            switch (edge)
            {
                case Edge.Bottom:
                case Edge.Top:    return size.y * open;
                case Edge.Left:
                case Edge.Right:  return size.x * open;
                default: throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unknown drawer edge.");
            }
        }

        private void Start()
        {
            if (_panel == null)
            {
                Debug.LogError("SlideDrawer: no panel set, so the drawer can't open.", this);
                enabled = false;
                return;
            }
            _home = _panel.anchoredPosition;
            // Stops the closed panel catching clicks meant for the map. Added in the setup step.
            _panelGroup = _panel.GetComponent<CanvasGroup>();
            if (_pushed != null)
            {
                _pushedMin = _pushed.offsetMin;
                _pushedMax = _pushed.offsetMax;
            }

            for (int i = 0; i < _pages.Count; i++)
            {
                int index = i;
                if (_pages[i].tab != null)
                    _pages[i].tab.onClick.AddListener(() => Click(index));
            }
            ShowPage(None);
            Place(0f);
        }

        private void Update()
        {
            float target = Open == None ? 0f : 1f;
            // Settled, and the panel hasn't changed size (a stretched panel does when the window does).
            if (Mathf.Approximately(_openness, target) && _panel.rect.size == _placedAtSize)
                return;
            _openness = _slideSeconds <= 0f ? target
                : Mathf.MoveTowards(_openness, target, Time.unscaledDeltaTime / _slideSeconds);
            Place(Mathf.SmoothStep(0f, 1f, _openness));
        }

        /// <summary>
        /// Slides the drawer in on the page holding <paramref name="content"/>. Already open on it: stays
        /// open. <paramref name="instant"/> skips the slide (for the state the game starts in).
        /// </summary>
        public void OpenPage(CanvasGroup content, bool instant = false)
        {
            int index = _pages.FindIndex(page => page.content == content);
            if (index < 0)
            {
                Debug.LogError($"SlideDrawer: {(content != null ? content.name : "(none)")} isn't one of its pages.", this);
                return;
            }
            Open = index;
            ShowPage(index);
            if (instant)
                SnapTo(1f);
        }

        /// <summary>Slides the drawer away (the queue's fold button). The page stays showing while it goes.</summary>
        public void Close(bool instant = false)
        {
            Open = None;
            ColourTabs();
            if (instant)
                SnapTo(0f);
        }

        private void SnapTo(float openness)
        {
            _openness = openness;
            Place(openness);
        }

        private void Click(int index)
        {
            Open = OpenAfterClick(Open, index);
            // Closing keeps the page that was open showing while it slides away.
            if (Open != None)
                ShowPage(Open);
            ColourTabs();
        }

        private void Place(float eased)
        {
            _placedAtSize = _panel.rect.size;
            _panel.anchoredPosition = _home + Offset(_edge, _placedAtSize, eased);
            if (_panelGroup != null)
                _panelGroup.blocksRaycasts = _openness > 0f;
            if (_pushed != null)
                Push(CoveredInset(_edge, _placedAtSize, eased));
        }

        /// <summary>Moves the pushed part's edge on the drawer's side in by <paramref name="inset"/>, from where it was at the start.</summary>
        private void Push(float inset)
        {
            Vector2 min = _pushedMin, max = _pushedMax;
            switch (_edge)
            {
                case Edge.Bottom: min.y += inset; break;
                case Edge.Top:    max.y -= inset; break;
                case Edge.Left:   min.x += inset; break;
                case Edge.Right:  max.x -= inset; break;
            }
            _pushed.offsetMin = min;
            _pushed.offsetMax = max;
        }

        /// <summary>Shows one page and hides the rest. They stay active, so their lists keep up to date.</summary>
        private void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                var content = _pages[i].content;
                if (content == null)
                    continue;
                bool shown = i == index;
                content.alpha = shown ? 1f : 0f;
                content.interactable = shown;
                content.blocksRaycasts = shown;
            }
            ColourTabs();
        }

        private void ColourTabs()
        {
            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i].tab != null)
                    _pages[i].tab.image.color = i == Open ? UiStyle.BookmarkTabOpen : UiStyle.BookmarkTab;
        }
    }
}
