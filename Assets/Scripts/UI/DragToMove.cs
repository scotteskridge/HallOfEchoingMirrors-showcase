using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Put this on any UI panel the player may move: dragging it (anywhere on it that catches the
    /// mouse and isn't a button) moves it, even partly off its parent to get it out of the way, but
    /// never so far it can't be grabbed again (some always stays in view, and its top never goes
    /// above the parent's); it remembers where it was left
    /// (PlayerPrefs, for every save slot, if given a key); double-clicking it puts it back where it
    /// started. Where it was left is its top-left corner measured from an origin: the parent's
    /// top-left corner unless the owner says otherwise (the room popover measures from its room, so
    /// it keeps that place beside every room).
    /// By default it keeps itself in place every frame. An owner that places the panel itself (the
    /// room popover, which follows its room) sets <see cref="OwnerPlaces"/> and calls KeepPlaced.
    /// </summary>
    public class DragToMove : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Tooltip("Where its place is remembered (PlayerPrefs), e.g. HallOfEchoingMirrors.PocketsOverlay. " +
                 "Keep it the same once players have used it, or their places are forgotten. Empty: never remembered.")]
        [SerializeField] private string _prefsKey;
        [Tooltip("Dragged out of the way, how much of it (pixels, across and down) always stays in view, so it " +
                 "can be grabbed again. Its top never goes above the parent's top.")]
        [SerializeField, Min(20f)] private float _keepVisible = 80f;

        private Vector2 _home;       // where it sat in the scene: double-click goes back there
        private Canvas _canvas;
        private bool _dragging;

        /// <summary>Its top-left corner from the origin, where the player left it; null until they drag it.</summary>
        public Vector2? Placed { get; private set; }

        /// <summary>What its place is measured from, in the parent's space. Unset: the parent's top-left corner.</summary>
        public Func<Vector2> Origin { get; set; }

        /// <summary>Whether it may be dragged now (e.g. the popover only while it's open for a room). Unset: always.</summary>
        public Func<bool> CanDrag { get; set; }

        /// <summary>On: the owner places it (calling KeepPlaced once it's been placed). Off: it keeps itself in place.</summary>
        public bool OwnerPlaces { get; set; }

        private RectTransform Self => (RectTransform)transform;
        private RectTransform Parent => (RectTransform)transform.parent;

        private void Awake()
        {
            _home = Self.anchoredPosition;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (!string.IsNullOrEmpty(_prefsKey) && PlayerPrefs.GetInt(Key("placed"), 0) == 1)
                Placed = new Vector2(PlayerPrefs.GetFloat(Key("x")), PlayerPrefs.GetFloat(Key("y")));
        }

        // Kept inside its parent every frame: it may grow, and the screen may change size.
        private void LateUpdate()
        {
            if (!OwnerPlaces)
                KeepPlaced();
        }

        /// <summary>Puts it where the player left it (partly off its parent, perhaps), or just inside its parent if they haven't moved it.</summary>
        public void KeepPlaced()
        {
            Vector2 size = Self.rect.size;
            Vector2 lowerLeft = Placed.HasValue
                ? PopoverPlacement.LowerLeftAt(OriginNow(), size, Parent.rect, Placed.Value, _keepVisible)
                : PopoverPlacement.KeepInside(LowerLeft(), size, Parent.rect);
            MoveTo(lowerLeft);
        }

        // ---------- Dragging ----------

        public void OnBeginDrag(PointerEventData eventData) => _dragging = CanDrag == null || CanDrag();

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;
            // Screen pixels to the canvas's own units, so it follows the pointer at any screen size (as
            // MapWindow pans). Not a ray onto the parent: the map panel sits in front of the canvas in
            // depth, so a ray from the screen never meets it.
            Vector2 move = eventData.delta / _canvas.scaleFactor;
            Vector2 size = Self.rect.size;
            Vector2 lowerLeft = PopoverPlacement.KeepGrabbable(LowerLeft() + move, size, Parent.rect, _keepVisible);
            Placed = PopoverPlacement.TopLeftFrom(OriginNow(), size, lowerLeft);
            MoveTo(lowerLeft);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bool wasDragging = _dragging;
            _dragging = false;
            if (!wasDragging || !Placed.HasValue || string.IsNullOrEmpty(_prefsKey))
                return;
            PlayerPrefs.SetInt(Key("placed"), 1);
            PlayerPrefs.SetFloat(Key("x"), Placed.Value.x);
            PlayerPrefs.SetFloat(Key("y"), Placed.Value.y);
            PlayerPrefs.Save();
        }

        // ---------- Double-click: back where it started ----------

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount != 2 || eventData.button != PointerEventData.InputButton.Left)
                return;
            Placed = null;
            if (!string.IsNullOrEmpty(_prefsKey))
            {
                PlayerPrefs.DeleteKey(Key("placed"));
                PlayerPrefs.DeleteKey(Key("x"));
                PlayerPrefs.DeleteKey(Key("y"));
                PlayerPrefs.Save();
            }
            // An owner that places it (the popover) puts it back beside its room on its next frame.
            Self.anchoredPosition = _home;
        }

        // ---------- Helpers ----------

        private string Key(string part) => _prefsKey + "." + part;

        private Vector2 OriginNow()
        {
            if (Origin != null)
                return Origin();
            var parent = Parent.rect;
            return new Vector2(parent.xMin, parent.yMax);
        }

        private Vector2 LowerLeft() => (Vector2)Self.localPosition - Vector2.Scale(Self.rect.size, Self.pivot);

        private void MoveTo(Vector2 lowerLeft)
        {
            Vector2 position = lowerLeft + Vector2.Scale(Self.rect.size, Self.pivot);
            if ((Vector2)Self.localPosition != position)
                Self.localPosition = new Vector3(position.x, position.y, Self.localPosition.z);
        }
    }
}
