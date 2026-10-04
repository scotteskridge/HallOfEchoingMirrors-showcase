using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Dragging in the queue drawer: a row to a new place, or a whole stop (by its heading or its
    /// trip) to a new place in the route. A box with its name follows the pointer; a line shows where
    /// it would land, only where the simulation allows the move; over a refused gap, the box says why.
    /// Near the edge of the cards or of a card's rows, the list scrolls. Letting go anywhere else,
    /// or pressing Escape, leaves the queue as it was.
    /// </summary>
    public class QueueDragController : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private QueueDrawer _drawer;
        [Tooltip("Where a drop would land. Shown only at gaps where the move is allowed.")]
        [SerializeField] private RectTransform _dropLine;
        [Tooltip("Follows the pointer while dragging, with the name of what's being moved.")]
        [SerializeField] private RectTransform _ghost;
        [SerializeField] private TMP_Text _ghostLabel;
        [Tooltip("Under the ghost's name: why what's being dragged can't go where the pointer is.")]
        [SerializeField] private TMP_Text _reasonLabel;
        [Tooltip("How thick the drop line is.")]
        [SerializeField] private float _lineThickness = 3f;
        [Tooltip("How close to the edge of the cards (or of a card's rows) the pointer must be for the list to scroll while dragging.")]
        [SerializeField] private float _edgeZone = 40f;
        [Tooltip("How fast the list scrolls while dragging near its edge (units a second, real time).")]
        [SerializeField] private float _scrollSpeed = 600f;

        // What's being dragged, held by the entry itself (or the trip starting the stop), since the
        // queue can change under the pointer: the running action finishes, or a skipped one drops out.
        private QueueEntry _dragged;
        private bool _draggingStop;
        private int _gap = -1;       // where a drop would land now, if allowed; -1 if not
        private Vector2 _pointer;    // kept between frames, so the list keeps scrolling while the mouse rests
        private Camera _camera;
        private readonly List<QueueStop> _stops = new List<QueueStop>();

        /// <summary>The entry being dragged (for a stop, its trip); null when nothing is.</summary>
        public QueueEntry Dragged => _dragged;
        /// <summary>A whole stop is being dragged, not one row.</summary>
        public bool DraggingStop => _draggingStop;

        private void Awake() => Hide();

        // Loading a game mid-drag replaces the simulation (and its queue): the drag is dropped.
        private void OnEnable() => _game.SimulationChanged += DropDrag;

        private void OnDisable()
        {
            _game.SimulationChanged -= DropDrag;
            Hide();
        }

        private void DropDrag(Simulation _) => Hide();

        // Every frame, not only when the mouse moves: Escape, scrolling at the edges, and the queue
        // changing under a still pointer (the running action finishing) all happen with the mouse at rest.
        private void Update()
        {
            if (_dragged == null)
                return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide(); // the rest of this drag's events find nothing being dragged, so letting go does nothing
                return;
            }
            float distance = _scrollSpeed * Time.unscaledDeltaTime;
            EdgeScroll(_drawer.CardsScroll, distance);
            EdgeScroll(_drawer.RowsScrollAt(_pointer, _camera), distance);
            FindGap(out _);
        }

        /// <summary>A row was picked up. A trip she'll make starts its stop, so it takes the whole stop along.</summary>
        public void BeginEntryDrag(QueueEntry entry, PointerEventData eventData)
        {
            var sim = _game.Simulation;
            int index = sim != null && entry != null ? sim.Queue.Entries.IndexOf(entry) : -1;
            if (!IsLeft(eventData) || index < 0) // gone from the queue since the row was drawn
                return;
            Begin(sim, entry, sim.WillTravel(index), eventData);
        }

        /// <summary>A stop's heading was picked up. The first stop (where the queue starts) doesn't move.</summary>
        public void BeginStopDrag(QueueStop stop, PointerEventData eventData)
        {
            var sim = _game.Simulation;
            if (!IsLeft(eventData) || sim == null || stop.Number <= 1 || stop.FirstEntry >= sim.Queue.Count)
                return;
            Begin(sim, sim.Queue.Entries[stop.FirstEntry], true, eventData);
        }

        public void Drag(PointerEventData eventData)
        {
            if (_dragged == null || !IsLeft(eventData))
                return;
            TakePointer(eventData);
            FindGap(out _);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (_dragged == null || !IsLeft(eventData))
                return;
            // Worked out again and acted on at once, so the drop uses the queue as it is this moment.
            TakePointer(eventData);
            if (FindGap(out int from))
            {
                if (_draggingStop)
                    _game.Simulation.MoveStop(StopStartedBy(from), _gap);
                else
                    _game.Simulation.MoveEntry(from, _gap);
            }
            Hide();
        }

        // Only the left button drags: uGUI sends drags for every button, and two at once would mix up.
        private static bool IsLeft(PointerEventData eventData) => eventData.button == PointerEventData.InputButton.Left;

        private void Begin(Simulation sim, QueueEntry entry, bool wholeStop, PointerEventData eventData)
        {
            int index = sim.Queue.Entries.IndexOf(entry);
            if (wholeStop)
            {
                // The card may show last frame's stops: if the queue has shifted since, this isn't a trip starting a stop any more.
                sim.QueueStops(_stops);
                int stopIndex = StopStartedBy(index);
                if (stopIndex < 0)
                    return;
                var stop = _stops[stopIndex];
                UiText.Set(_ghostLabel, GameText.Get("queue.drag_stop", ("number", stop.Number), ("room", stop.Room.DisplayName)));
            }
            else
            {
                UiText.Set(_ghostLabel, ActionText.NameOf(sim, index, entry));
            }
            _dragged = entry;
            _draggingStop = wholeStop;
            ToolTipPanel.HideAny(); // the row's pop-up would sit over the gaps
            _ghost.gameObject.SetActive(true);
            TakePointer(eventData);
            FindGap(out _);
        }

        private void TakePointer(PointerEventData eventData)
        {
            _pointer = eventData.position;
            _camera = eventData.pressEventCamera;
        }

        /// <summary>
        /// Moves the ghost to the pointer and finds the gap under it: shows the line there if the move
        /// is allowed, or why not. Returns whether a drop now would move something.
        /// </summary>
        private bool FindGap(out int from)
        {
            _gap = -1;
            var sim = _game.Simulation;
            from = sim != null ? sim.Queue.Entries.IndexOf(_dragged) : -1;
            if (from < 0)
            {
                Hide(); // gone from the queue (done, or dropped out as skipped)
                return false;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_ghost, _pointer, _camera, out var pointer))
                _ghost.position = pointer;

            sim.QueueStops(_stops);
            int gap = -1;
            Vector3 lineStart = default, lineEnd = default;
            bool found, allowed = false;
            string reason = null;
            if (_draggingStop)
            {
                int stop = StopStartedBy(from);
                found = stop > 0 && _drawer.StopGapAt(_stops, _pointer, _camera, out gap, out lineStart, out lineEnd);
                found &= gap != stop && gap != stop + 1; // where it already is
                allowed = found && sim.CanMoveStop(stop, gap, out reason);
            }
            else
            {
                found = _drawer.EntryGapAt(_stops, _pointer, _camera, out gap, out lineStart, out lineEnd);
                found &= gap != from && gap != from + 1;
                allowed = found && sim.CanMoveEntry(from, gap, out reason);
            }

            UiText.SetActive(_dropLine, allowed);
            if (allowed)
            {
                _gap = gap;
                PlaceLine(lineStart, lineEnd);
            }
            bool refused = found && !allowed && reason != null;
            UiText.SetActive(_reasonLabel, refused);
            if (refused)
                UiText.Set(_reasonLabel, UiStyle.Colour(GameText.Get("queue.cant_move", ("reason", reason)), UiStyle.Warning));
            return allowed;
        }

        /// <summary>Which stop (an index into _stops) the entry at <paramref name="index"/> is the trip that starts; -1 if none.</summary>
        private int StopStartedBy(int index)
        {
            for (int i = 1; i < _stops.Count; i++)
                if (_stops[i].FirstEntry == index)
                    return i;
            return -1;
        }

        /// <summary>
        /// Scrolls a list a little toward the edge the pointer is near (within _edgeZone, inside the
        /// list's window). Returns whether it moved.
        /// </summary>
        private bool EdgeScroll(ScrollRect scroll, float distance)
        {
            if (scroll == null || scroll.content == null)
                return false;
            var window = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(window, _pointer, _camera, out var local) ||
                !window.rect.Contains(local))
                return false;

            var area = window.rect;
            var spare = scroll.content.rect.size - area.size; // how far the list can scroll each way
            bool moved = false;
            if (scroll.horizontal && spare.x > 0f)
            {
                float toward = local.x < area.xMin + _edgeZone ? -1f : local.x > area.xMax - _edgeZone ? 1f : 0f;
                float before = scroll.horizontalNormalizedPosition;
                scroll.horizontalNormalizedPosition = Mathf.Clamp01(before + toward * distance / spare.x);
                moved |= !Mathf.Approximately(scroll.horizontalNormalizedPosition, before);
            }
            if (scroll.vertical && spare.y > 0f)
            {
                // 1 is the top of a vertical list.
                float toward = local.y > area.yMax - _edgeZone ? 1f : local.y < area.yMin + _edgeZone ? -1f : 0f;
                float before = scroll.verticalNormalizedPosition;
                scroll.verticalNormalizedPosition = Mathf.Clamp01(before + toward * distance / spare.y);
                moved |= !Mathf.Approximately(scroll.verticalNormalizedPosition, before);
            }
            return moved;
        }

        /// <summary>Stretches the drop line between two points (world space), across or up and down.</summary>
        private void PlaceLine(Vector3 start, Vector3 end)
        {
            var parent = (RectTransform)_dropLine.parent;
            Vector2 a = parent.InverseTransformPoint(start);
            Vector2 b = parent.InverseTransformPoint(end);
            _dropLine.position = (start + end) / 2f;
            var size = b - a;
            _dropLine.sizeDelta = Mathf.Abs(size.x) >= Mathf.Abs(size.y)
                ? new Vector2(Mathf.Abs(size.x), _lineThickness)
                : new Vector2(_lineThickness, Mathf.Abs(size.y));
        }

        private void Hide()
        {
            _dragged = null;
            _gap = -1;
            if (_ghost != null)
                _ghost.gameObject.SetActive(false);
            UiText.SetActive(_dropLine, false);
        }
    }
}
