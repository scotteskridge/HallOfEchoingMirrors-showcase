using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The small red-edged box that tells the player why an action couldn't be done, e.g. "Can't
    /// light a candle: needs 1 Hanging candle". It appears when an action is refused as it's asked
    /// for, or dropped when its turn comes, then fades. It never catches the mouse.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class NoticeToast : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private TMP_Text _text;
        [Tooltip("Seconds (real time) it stays fully visible.")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 3f;
        [Tooltip("Seconds it takes to fade out afterwards.")]
        [SerializeField, Min(0f)] private float _fadeSeconds = 0.6f;

        private CanvasGroup _group;
        private Simulation _listeningTo;
        private float _shownAt = float.NegativeInfinity;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void Start()
        {
            _game.SimulationChanged += ListenTo;
            ListenTo(_game.Simulation);
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= ListenTo;
            ListenTo(null);
        }

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
            {
                _listeningTo.ActionRefused -= ShowRefused;
                _listeningTo.TaskSkipped -= Show;
            }
            _listeningTo = sim;
            if (sim != null)
            {
                sim.ActionRefused += ShowRefused;
                sim.TaskSkipped += Show;
            }
        }

        private void ShowRefused(TaskDefinition task, NodeDefinition destination, string reason)
        {
            // A refused trip says where it was going; Travel alone ("Can't travel: ...") told the player nothing.
            if (destination != null)
                Show(GameText.Get("notices.cant_travel", ("room", GameText.TitleInSentence(destination.DisplayName)), ("reason", reason)));
            else
                Show(task, reason);
        }

        private void Show(TaskDefinition task, string reason) =>
            Show(GameText.Get("notices.cant", ("task", GameText.LowerFirst(task.displayName)), ("reason", reason)));

        private void Show(string message)
        {
            UiText.Set(_text, message);
            _shownAt = Time.unscaledTime;
            transform.SetAsLastSibling(); // above the pages
        }

        private void Update()
        {
            float since = Time.unscaledTime - _shownAt;
            float alpha = since < _showSeconds ? 1f
                : _fadeSeconds > 0f ? Mathf.Clamp01(1f - (since - _showSeconds) / _fadeSeconds) : 0f;
            if (_group.alpha != alpha)
                _group.alpha = alpha;
        }
    }
}
