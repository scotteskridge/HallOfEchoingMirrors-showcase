using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The one hover pop-up box, shared by every ToolTip. It appears after a short delay (none when
    /// moving between neighbours), and sits above everything else. A plain-text tip follows the
    /// mouse and flips to stay on screen; a layout tip (<see cref="TipLayout"/>) is a fixed-width
    /// panel 8 px beside the element it describes and never covers it. Like the other overlays,
    /// it places itself when shown, so where it sits in the editor doesn't matter.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToolTipPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [Tooltip("The box that holds the plain text (shown for a tip that is just words).")]
        [SerializeField] private GameObject _plainRoot;
        [Tooltip("Draws a tip in the new shape: kicker, title, rows, footer.")]
        [SerializeField] private TipLayoutView _view;
        [Tooltip("The UI Fonts asset, so headings in a pop-up can switch to the serif (UiStyle.Heading).")]
        [SerializeField] private UiFonts _fonts;
        [Tooltip("Seconds (real time) the mouse must rest before the first pop-up appears.")]
        [SerializeField] private float _delay = 0.35f;
        [Tooltip("A pop-up that opens within this many seconds of another one showing opens at once, so scanning a list isn't a slideshow.")]
        [SerializeField] private float _neighbourWindow = 0.25f;
        [Tooltip("Width of a layout pop-up.")]
        [SerializeField] private float _fullWidth = 340f;
        [Tooltip("Width of a layout pop-up's compact form (Pick up, Put down).")]
        [SerializeField] private float _compactWidth = 260f;
        [Tooltip("Space between a layout pop-up and the element it describes.")]
        [SerializeField] private float _targetGap = 8f;
        [Tooltip("Widest the box gets before the text wraps.")]
        [SerializeField] private float _maxWidth = 360f;
        [Tooltip("Space between the text and the box's edge.")]
        [SerializeField] private float _padding = 10f;
        [Tooltip("How far from the mouse pointer the box sits.")]
        [SerializeField] private Vector2 _offset = new Vector2(18f, -18f);
        [Tooltip("The notice toast (with its CanvasGroup): while it shows, the pop-up moves clear of it rather than covering it. Optional.")]
        [SerializeField] private RectTransform _keepClearOf;
        [Tooltip("Space kept between the pop-up and the toast when it moves clear.")]
        [SerializeField] private float _clearGap = 8f;

        // The frame around the panel (an Image behind it, showing 1 px on each side).
        private const float Border = 1f;

        private static ToolTipPanel _instance;

        private CanvasGroup _group;
        private Canvas _canvas;
        private CanvasGroup _clearGroup;
        private RectTransform _rect;
        private ToolTip _target;
        private float _shownAt;
        private float _wait;
        private TipTiming _timing;
        private string _shownKey;

        private void Awake()
        {
            _instance = this;
            _timing = new TipTiming(_delay, _neighbourWindow);
            _group = GetComponent<CanvasGroup>();
            _rect = (RectTransform)transform;
            _group.blocksRaycasts = false; // the box must never catch the mouse itself, or it would flicker
            _group.interactable = false;
            _group.alpha = 0f;
            if (_fonts != null)
                _fonts.RegisterForMarkup();
            else
                Debug.LogError("ToolTipPanel: no UI Fonts asset set, so pop-up headings show in the body font. Assign the Fonts field (the UiFonts asset) in the Inspector.", this);
        }

        public static void Show(ToolTip tip)
        {
            if (_instance == null || tip == null)
                return;
            // Entering a button inside a row tells both; the innermost (the button) wins.
            var current = _instance._target;
            if (current != null && current != tip && current.transform.IsChildOf(tip.transform))
                return;
            _instance._target = tip;
            _instance._shownAt = Time.unscaledTime;
            _instance._wait = _instance._timing.DelayAt(Time.unscaledTime);
        }

        public static void Hide(ToolTip tip)
        {
            if (_instance == null || _instance._target != tip)
                return;
            // Leaving a button but still over its row: go back to the row's pop-up, without the delay.
            var outer = tip.transform.parent != null ? tip.transform.parent.GetComponentInParent<ToolTip>() : null;
            if (outer != null && outer.isActiveAndEnabled && MouseIsOver(outer))
            {
                _instance._target = outer;
                _instance._shownAt = Time.unscaledTime;
                _instance._wait = 0f;
            }
            else
            {
                _instance._target = null;
            }
        }

        /// <summary>Hides whatever pop-up is showing (e.g. when a drag starts). The next element the mouse enters shows its own.</summary>
        public static void HideAny()
        {
            if (_instance != null)
                _instance._target = null;
        }

        private static bool MouseIsOver(Component target)
        {
            if (Mouse.current == null || !(target.transform is RectTransform rect))
                return false;
            var canvas = target.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), camera);
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.isActiveAndEnabled || Time.unscaledTime - _shownAt < _wait)
            {
                if (_target != null && !_target.isActiveAndEnabled)
                    _target = null;
                _group.alpha = 0f;
                return;
            }

            var layout = _target.CurrentLayout;
            string text = layout == null ? _target.Current : null;
            if ((layout == null && string.IsNullOrEmpty(text)) || Mouse.current == null)
            {
                _group.alpha = 0f;
                return;
            }

            // Measuring text is a layout pass: only when the words change (live tips update often, but not every frame).
            string key = layout != null ? layout.Key() : text;
            if (key != _shownKey)
            {
                _shownKey = key;
                if (layout != null)
                    ShowLayout(layout);
                else
                    ShowPlain(text);
            }

            if (layout != null)
                PlaceBeside(_target);
            else
                PlaceBesideMouse();
            transform.SetAsLastSibling(); // above everything, even story pop-ups
            _group.alpha = 1f;
            _timing.NoteShown(Time.unscaledTime);
        }

        private void ShowPlain(string text)
        {
            _plainRoot.SetActive(true);
            _view.gameObject.SetActive(false);
            _text.text = text;
            Vector2 size = _text.GetPreferredValues(text, _maxWidth - 2f * _padding - 2f * Border, 0f);
            Resize(Mathf.Min(size.x, _maxWidth - 2f * _padding - 2f * Border) + 2f * (_padding + Border));
        }

        private void ShowLayout(TipLayout layout)
        {
            _plainRoot.SetActive(false);
            _view.gameObject.SetActive(true);
            _view.Show(layout);
            Resize(layout.Compact ? _compactWidth : _fullWidth);
        }

        // The width is fixed first, so its text wraps; then the layout says how tall that makes it.
        private void Resize(float width)
        {
            _rect.sizeDelta = new Vector2(width, _rect.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
            _rect.sizeDelta = new Vector2(width, LayoutUtility.GetPreferredHeight(_rect));
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        }

        // Beside what it describes, never over it (right, else left, else below, else above).
        private void PlaceBeside(ToolTip target)
        {
            var parent = (RectTransform)_rect.parent;
            Rect area = parent.rect;
            Vector2 size = _rect.sizeDelta;
            Vector2 corner = ToolTipPlacement.Beside(LocalRect(parent, (RectTransform)target.transform), size, area, _targetGap);

            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = Vector2.zero;
            _rect.anchoredPosition = corner - area.center;
            KeepClearOfToast(parent, area, size);
        }

        private void PlaceBesideMouse()
        {
            var parent = (RectTransform)_rect.parent;
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>().rootCanvas;
            var canvas = _canvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Mouse.current.position.ReadValue(), camera, out var mouse);

            // Below-right of the pointer, unless that would go off the edge: then flip to the other side.
            Rect area = parent.rect;
            Vector2 size = _rect.sizeDelta;
            bool flipX = mouse.x + _offset.x + size.x > area.xMax;
            bool flipY = mouse.y + _offset.y - size.y < area.yMin;

            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(flipX ? 1f : 0f, flipY ? 0f : 1f);
            Vector2 local = mouse + new Vector2(flipX ? -_offset.x : _offset.x, flipY ? -_offset.y : _offset.y);
            // anchoredPosition is measured from the parent's centre (the anchors above).
            _rect.anchoredPosition = local - area.center;

            KeepClearOfToast(parent, area, size);
        }

        // The toast is only in the way while it can be seen; then the pop-up hops above or below it.
        private void KeepClearOfToast(RectTransform parent, Rect area, Vector2 size)
        {
            if (_keepClearOf == null)
                return;
            if (_clearGroup == null)
                _clearGroup = _keepClearOf.GetComponent<CanvasGroup>();
            if (_clearGroup != null && _clearGroup.alpha <= 0.01f)
                return;

            Rect avoid = LocalRect(parent, _keepClearOf);
            Vector2 lowerLeft = _rect.anchoredPosition + area.center - new Vector2(_rect.pivot.x * size.x, _rect.pivot.y * size.y);
            Vector2 moved = ToolTipPlacement.ClearOf(lowerLeft, size, avoid, area, _clearGap);
            if (moved == lowerLeft)
                return;
            _rect.pivot = Vector2.zero;
            _rect.anchoredPosition = moved - area.center;
        }

        /// <summary>A rectangle's bounds in another rectangle's local space.</summary>
        private static Rect LocalRect(RectTransform space, RectTransform of)
        {
            var corners = new Vector3[4];
            of.GetWorldCorners(corners);
            Vector2 low = space.InverseTransformPoint(corners[0]);
            Vector2 high = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
        }
    }
}
