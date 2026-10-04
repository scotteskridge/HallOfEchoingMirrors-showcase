using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Where a room's popover opens: to the right of the room, or to its left if there's no room
    /// there, level with it, and always inside the map; or, once the player has dragged it, at the
    /// same place relative to whichever room it's for. The same sums place anything the player drags
    /// (DragToMove), measured from any origin. Kept apart so it can be tested.
    /// </summary>
    public static class PopoverPlacement
    {
        /// <summary>
        /// The popover's lower-left corner. All rectangles in the same space (the map's), y up.
        /// </summary>
        public static Vector2 LowerLeft(Rect room, Vector2 size, Rect map, float gap)
        {
            float x = room.xMax + gap;
            if (x + size.x > map.xMax)
                x = room.xMin - gap - size.x;
            return KeepInside(new Vector2(x, room.center.y - size.y / 2f), size, map);
        }

        /// <summary>
        /// The popover's lower-left corner when the player has placed it: its top-left corner sits
        /// <paramref name="topLeftFromRoom"/> from the room's centre (the top, so a popover that
        /// grows keeps its header where it was put), kept inside the map.
        /// </summary>
        public static Vector2 LowerLeftAt(Rect room, Vector2 size, Rect map, Vector2 topLeftFromRoom) =>
            LowerLeftAt(room.center, size, map, topLeftFromRoom);

        /// <summary>The offset LowerLeftAt takes to put the popover's lower-left corner here.</summary>
        public static Vector2 TopLeftFromRoom(Rect room, Vector2 size, Vector2 lowerLeft) =>
            TopLeftFrom(room.center, size, lowerLeft);

        /// <summary>
        /// Anything placed by the player (DragToMove): its lower-left corner when its top-left corner
        /// sits <paramref name="topLeftFromOrigin"/> from an origin (a room's centre, the map's
        /// corner), kept inside the map.
        /// </summary>
        public static Vector2 LowerLeftAt(Vector2 origin, Vector2 size, Rect map, Vector2 topLeftFromOrigin)
        {
            Vector2 topLeft = origin + topLeftFromOrigin;
            return KeepInside(new Vector2(topLeft.x, topLeft.y - size.y), size, map);
        }

        /// <summary>
        /// As LowerLeftAt, but allowed partly off the map (a panel the player has dragged out of the
        /// way): see KeepGrabbable.
        /// </summary>
        public static Vector2 LowerLeftAt(Vector2 origin, Vector2 size, Rect map, Vector2 topLeftFromOrigin, float keepVisible)
        {
            Vector2 topLeft = origin + topLeftFromOrigin;
            return KeepGrabbable(new Vector2(topLeft.x, topLeft.y - size.y), size, map, keepVisible);
        }

        /// <summary>
        /// Moved as little as it takes for part of it to stay on the map: it may hang off the sides
        /// or the bottom, but at least <paramref name="keepVisible"/> of it stays in view each way,
        /// and its top never goes above the map's, so its header can always be grabbed again.
        /// </summary>
        public static Vector2 KeepGrabbable(Vector2 lowerLeft, Vector2 size, Rect map, float keepVisible)
        {
            float wide = Mathf.Min(keepVisible, size.x);
            float high = Mathf.Min(keepVisible, size.y);
            float x = Mathf.Clamp(lowerLeft.x, map.xMin - size.x + wide, Mathf.Max(map.xMin, map.xMax - wide));
            float top = Mathf.Clamp(lowerLeft.y + size.y, map.yMin + high, map.yMax);
            return new Vector2(x, top - size.y);
        }

        /// <summary>The offset LowerLeftAt takes to put a lower-left corner here.</summary>
        public static Vector2 TopLeftFrom(Vector2 origin, Vector2 size, Vector2 lowerLeft) =>
            new Vector2(lowerLeft.x, lowerLeft.y + size.y) - origin;

        /// <summary>Moved as little as it takes to be inside the map.</summary>
        public static Vector2 KeepInside(Vector2 lowerLeft, Vector2 size, Rect map)
        {
            // Wider than the map: as far in as it goes.
            float x = Mathf.Clamp(lowerLeft.x, map.xMin, Mathf.Max(map.xMin, map.xMax - size.x));
            float y = Mathf.Max(lowerLeft.y, map.yMin);
            // The top wins when it's too tall: the header (the room's name) stays in view.
            y = Mathf.Min(y, map.yMax - size.y);
            return new Vector2(x, y);
        }
    }
}
