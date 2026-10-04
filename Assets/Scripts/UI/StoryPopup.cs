using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Shows story beats one at a time as a pop-up, above every page, always filling the view
    /// (wherever it sits in the editor). Pauses a running loop while open and resumes it on Continue. A story only counts as read once Continue is clicked, so
    /// one that was on screen when the game stopped is shown again next time. Sits on an
    /// always-active object (the Canvas), because its own panel is hidden between beats.
    /// </summary>
    public class StoryPopup : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [Tooltip("Scrolls the body when a passage is longer than the card.")]
        [SerializeField] private ScrollRect _bodyScroll;
        [SerializeField] private Button _continueButton;

        private StoryBeat _showing;
        private bool _pausedByPopup;
        private bool _rereading; // opened by Read more: already read, so not waiting in the unread list

        private Simulation Sim => _game.Simulation;

        private void Start()
        {
            _panel.SetActive(false);
            _continueButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            // GameController didn't start (no Loop Settings; it already logged why): stay quiet rather than throw every frame.
            if (Sim == null)
                return;

            // A different game was loaded while a story was open: that story belongs to the old one.
            if (_showing != null && !_rereading && !Contains(Sim.UnreadStories, _showing))
            {
                Hide();
                _pausedByPopup = false;
            }

            // Story waits until a game has actually been started or loaded (not over the menu).
            if (_showing == null && _game.HasActiveGame && Sim.UnreadStories.Count > 0)
                Open(Sim.UnreadStories[0]);
        }

        /// <summary>Opens a story again, e.g. from the Story panel's Read more. Does nothing if another is already showing (it isn't queued).</summary>
        public void Reread(StoryBeat beat)
        {
            if (beat == null || _showing != null)
                return;
            _rereading = true;
            Open(beat);
        }

        private void Open(StoryBeat beat)
        {
            _title.text = beat.Title;
            _body.text = UiStyle.Markup(beat.Body);
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            FillTheView();

            // Every passage starts at the top, not wherever the last one was scrolled to.
            if (_bodyScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                _bodyScroll.verticalNormalizedPosition = 1f;
            }
            _showing = beat;

            if (Sim.Phase == LoopPhase.Running && !_game.IsPaused)
            {
                _game.Pause();
                _pausedByPopup = true;
            }
        }

        private void Close()
        {
            if (_showing != null && !_rereading)
                Sim.MarkRead(_showing);
            _rereading = false;
            Hide();

            // More beats waiting? Keep the game paused and let Update show the next one.
            if (_pausedByPopup && Sim.UnreadStories.Count == 0)
            {
                _game.Resume();
                _pausedByPopup = false;
            }
        }

        /// <summary>
        /// Puts the pop-up over the whole view, wherever it was left in the editor, so it can be
        /// moved aside while editing pages without ever opening off-screen.
        /// </summary>
        private void FillTheView()
        {
            if (!(_panel.transform is RectTransform rect))
                return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private void Hide()
        {
            _panel.SetActive(false);
            _showing = null;
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<StoryBeat> list, StoryBeat beat)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == beat)
                    return true;
            return false;
        }
    }
}
