using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The window onto the map (MapView's part that moves): which spot of the map is at its centre,
    /// how far it's zoomed, and the layers it slides and scales to show that. It glides to the room
    /// that matters, lets the player drag and wheel-zoom to look around, and glides back after a
    /// moment. MapView owns one and passes it the mouse events (only a component can receive them).
    /// </summary>
    public class MapWindow
    {
        private readonly RectTransform _area;
        private readonly Canvas _canvas;
        private readonly List<RectTransform> _layers = new List<RectTransform>();

        // The window's position over the map, in map pixels (what's at the window's centre).
        private Vector2 _centre;
        private float _zoom = 1f; // mouse wheel; 1 = the style's scale
        private bool _placed;
        private float _lastDragTime = float.NegativeInfinity;
        private bool _dragging;

        /// <param name="area">The window: the rectangle the map is seen through.</param>
        /// <param name="canvas">Its canvas, to turn screen pixels into map pixels while dragging.</param>
        public MapWindow(RectTransform area, Canvas canvas)
        {
            _area = area;
            _canvas = canvas;
        }

        /// <summary>
        /// Moves and zooms <paramref name="layer"/> with the rest of the map, so what's drawn on it at
        /// a room's MapView.PositionOf stays on that room. It must fill the window with its pivot at
        /// the centre (as the Lines and Nodes layers do).
        /// </summary>
        public void AddLayer(RectTransform layer)
        {
            if (!_layers.Contains(layer))
                _layers.Add(layer);
        }

        /// <summary>The next Follow jumps straight to its target (a new or loaded game) rather than gliding.</summary>
        public void JumpNext() => _placed = false;

        /// <summary>
        /// Glides the window towards <paramref name="target"/> (a spot on the map; null for none),
        /// unless the player has just dragged or zoomed, then slides and scales the layers under it.
        /// </summary>
        public void Follow(Vector2? target, MapStyle style)
        {
            if (target.HasValue && !_dragging && Time.unscaledTime - _lastDragTime >= style.returnAfterSeconds)
            {
                _centre = _placed
                    ? Vector2.Lerp(_centre, target.Value, 1f - Mathf.Exp(-style.followSpeed * Time.unscaledDeltaTime))
                    : target.Value;
                _placed = true;
            }

            // Every layer is anchored at the window's centre, so this puts _centre in the middle.
            // Zooming scales the layers (rooms, lines and route together) around that centre.
            var scale = new Vector3(_zoom, _zoom, 1f);
            foreach (var layer in _layers)
            {
                layer.localScale = scale;
                layer.anchoredPosition = -_centre * _zoom;
            }
        }

        public void BeginDrag() => _dragging = true;

        public void Drag(Vector2 screenDelta)
        {
            // Screen pixels to map pixels, so the map moves exactly with the pointer at any zoom.
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            _centre -= screenDelta / scale / _zoom;
            _lastDragTime = Time.unscaledTime;
        }

        public void EndDrag()
        {
            _dragging = false;
            _lastDragTime = Time.unscaledTime;
        }

        /// <summary>
        /// One turn of the mouse wheel zooms in or out, towards the pointer: the spot under it stays
        /// under it. Like dragging, it glides back to Clara after a moment.
        /// </summary>
        public void Scroll(float scrollY, Vector2 pointer, Camera eventCamera, MapStyle style)
        {
            float zoom = ZoomAfterScroll(_zoom, scrollY, style);
            if (Mathf.Approximately(zoom, _zoom))
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, pointer, eventCamera, out var local))
            {
                Vector2 fromCentre = local - _area.rect.center;
                Vector2 underPointer = _centre + fromCentre / _zoom;
                _centre = underPointer - fromCentre / zoom;
            }
            _zoom = zoom;
            _lastDragTime = Time.unscaledTime;
        }

        /// <summary>
        /// The zoom after one wheel event: one step in or out per event, however far the wheel
        /// turned, kept between the style's limits.
        /// </summary>
        public static float ZoomAfterScroll(float zoom, float scrollY, MapStyle style)
        {
            // Checked before Sign: Unity's Mathf.Sign(0) is 1, so a sideways swipe would zoom in.
            if (scrollY == 0f)
                return zoom;
            float notches = Mathf.Sign(scrollY);
            return Mathf.Clamp(zoom * Mathf.Pow(1f + style.zoomStep, notches), style.minZoom, style.maxZoom);
        }
    }
}
