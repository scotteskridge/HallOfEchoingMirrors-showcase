using System;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One entry in the queue: its name, a line of details (time left, or how long it repeats),
    /// a progress bar, and two buttons: to the top (do it now) and remove. It can be dragged to a
    /// new place in the queue (QueueDragController). Its look is the QueueRow prefab.
    /// </summary>
    public class QueueRow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _detailsLabel;
        [Tooltip("Progress of this entry's current go (the running one, or one waiting with progress kept).")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Button _toTopButton;
        [SerializeField] private Button _removeButton;

        private Action<QueueEntry> _onToTop, _onRemove;
        private QueueDragController _drag;
        private CanvasGroup _group; // fades the row while it's being dragged

        // Made on first use, not in Awake: a row made inside a folded block is inactive, so Awake hasn't run
        // when its card first refreshes it.
        private CanvasGroup Group
        {
            get
            {
                if (_group == null)
                {
                    _group = GetComponent<CanvasGroup>();
                    if (_group == null)
                        _group = gameObject.AddComponent<CanvasGroup>();
                }
                return _group;
            }
        }

        private void Awake()
        {
            _toTopButton.onClick.AddListener(() => _onToTop?.Invoke(Entry));
            _removeButton.onClick.AddListener(() => _onRemove?.Invoke(Entry));
        }

        /// <summary>
        /// The queue entry this row shows, set every frame. Clicks and drags act on the entry, not on its
        /// place in the queue: a place can change in the frame a click lands (a trip finishing).
        /// </summary>
        public QueueEntry Entry { get; private set; }

        public void Setup(Action<QueueEntry> onToTop, Action<QueueEntry> onRemove, Func<string> tip)
        {
            _onToTop = onToTop;
            _onRemove = onRemove;
            UiText.SetCaption(_toTopButton, GameText.Get("queue.to_top_button"));
            UiText.SetCaption(_removeButton, GameText.Get("queue.remove_button"));
            ToolTip.On(this, tip);
            ToolTip.On(_toTopButton, () => GameText.Get("queue.to_top_tip"));
            ToolTip.On(_removeButton, () => GameText.Get("queue.remove_tip"));
        }

        /// <summary>Lets the row be dragged.</summary>
        public void SetupDrag(QueueDragController drag) => _drag = drag;

        /// <summary>The parts that move every frame: which entry this is, its bar and its look.</summary>
        /// <param name="isTop">The top entry can't go higher, so its To top button is hidden.</param>
        /// <param name="isDragged">It's being dragged: faded where it was.</param>
        public void Refresh(QueueEntry entry, float progress, bool isTop, bool isDragged)
        {
            Entry = entry;
            Group.alpha = isDragged ? UiStyle.DraggedAlpha : 1f;
            _progressBar.value = progress;
            UiText.SetActive(_toTopButton, !isTop);
        }

        /// <summary>The words, rebuilt only now and then (the card says when).</summary>
        public void SetText(string name, string details)
        {
            UiText.Set(_nameLabel, name);
            UiText.Set(_detailsLabel, details);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.BeginEntryDrag(Entry, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.EndDrag(eventData);
        }
    }
}
