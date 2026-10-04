using System;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Where a room's popover sits (split out of RoomPopover): beside its room, following it as the map moves,
    /// or wherever the player dragged it (the DragToMove on the same object, set up here). Placing is done by
    /// PopoverPlacement's tested sums; this holds the rectangles and the drag set-up around them.
    /// </summary>
    public class PopoverDocker
    {
        private readonly RectTransform _self;
        private readonly MapView _map;
        private readonly DragToMove _drag;
        private readonly Vector3[] _corners = new Vector3[4];

        /// <param name="owner">The popover; it must have a DragToMove beside it (else the popover can't be dragged, and an error says so).</param>
        /// <param name="room">The room it's open for now, null while closed.</param>
        public PopoverDocker(MonoBehaviour owner, MapView map, Func<NodeDefinition> room)
        {
            _self = (RectTransform)owner.transform;
            _map = map;
            // Anywhere on it that isn't a button drags it: the header, the gaps, the sections. Its place
            // is measured from the room's centre, so it keeps that place as she moves on.
            _drag = owner.GetComponent<DragToMove>();
            if (_drag == null)
            {
                Debug.LogError("RoomPopover: there is no DragToMove component on this object, so it can't be dragged. Add one to the RoomPopover object in the Inspector.", owner);
                return;
            }
            _drag.OwnerPlaces = true; // it follows its room: placed every frame by PlaceBeside
            _drag.CanDrag = () => room() != null && _map.RoomRect(room()) != null;
            _drag.Origin = () => RectInArea(_map.RoomRect(room()), (RectTransform)_self.parent).center;
        }

        /// <summary>Puts the popover beside its room (or, once dragged, at the player's place), a frame after the map has moved.</summary>
        public void PlaceBeside(RectTransform roomRect, float gap)
        {
            // Dragged somewhere: the same place from every room.
            if (_drag != null && _drag.Placed.HasValue)
            {
                _drag.KeepPlaced();
                return;
            }
            var area = (RectTransform)_self.parent;
            Vector2 size = _self.rect.size;
            Vector2 lowerLeft = PopoverPlacement.LowerLeft(RectInArea(roomRect, area), size, area.rect, gap);
            _self.localPosition = lowerLeft + Vector2.Scale(size, _self.pivot);
        }

        private Rect RectInArea(RectTransform rect, RectTransform area)
        {
            rect.GetWorldCorners(_corners);
            Vector2 min = area.InverseTransformPoint(_corners[0]);
            Vector2 max = area.InverseTransformPoint(_corners[2]);
            return new Rect(min, max - min);
        }
    }
}
