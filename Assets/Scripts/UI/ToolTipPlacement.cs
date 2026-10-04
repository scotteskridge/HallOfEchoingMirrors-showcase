using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Keeps a tooltip clear of a box it shouldn't cover (the notice toast): moved straight up or
    /// down, whichever is closer and fits, and never off the screen. Kept apart so it can be tested.
    /// </summary>
    public static class ToolTipPlacement
    {
        /// <summary>
        /// The tooltip's lower-left corner when it sits <paramref name="gap"/> beside what it describes
        /// and never covers it: right of it, else left, else below, else above. Centred on the target
        /// along the free edge, but kept inside the screen. If nothing fits it is clamped on screen
        /// (covering the target is better than being cut off). All rectangles in the same space, y up.
        /// </summary>
        public static Vector2 Beside(Rect target, Vector2 size, Rect screen, float gap)
        {
            float centredY = Clamp(target.center.y - size.y * 0.5f, screen.yMin, screen.yMax - size.y);
            float centredX = Clamp(target.center.x - size.x * 0.5f, screen.xMin, screen.xMax - size.x);

            float right = target.xMax + gap;
            if (right + size.x <= screen.xMax)
                return new Vector2(right, centredY);
            float left = target.xMin - gap - size.x;
            if (left >= screen.xMin)
                return new Vector2(left, centredY);
            float below = target.yMin - gap - size.y;
            if (below >= screen.yMin)
                return new Vector2(centredX, below);
            float above = target.yMax + gap;
            if (above + size.y <= screen.yMax)
                return new Vector2(centredX, above);
            return new Vector2(Clamp(right, screen.xMin, screen.xMax - size.x), centredY);
        }

        // Mathf.Clamp throws nothing but gives min > max nonsense when the box is bigger than the screen; min wins here.
        private static float Clamp(float value, float min, float max) => Mathf.Max(min, Mathf.Min(value, max));

        /// <summary>
        /// The tooltip's lower-left corner, moved off <paramref name="avoid"/> if it overlaps it.
        /// All rectangles in the same space, y up. An empty <paramref name="avoid"/> (no toast showing) moves nothing.
        /// </summary>
        public static Vector2 ClearOf(Vector2 lowerLeft, Vector2 size, Rect avoid, Rect screen, float gap)
        {
            if (avoid.width <= 0f || avoid.height <= 0f || !new Rect(lowerLeft, size).Overlaps(avoid))
                return lowerLeft;

            float above = avoid.yMax + gap;
            float below = avoid.yMin - gap - size.y;
            bool aboveFits = above + size.y <= screen.yMax;
            bool belowFits = below >= screen.yMin;

            float y;
            if (aboveFits && belowFits)
                y = Mathf.Abs(above - lowerLeft.y) <= Mathf.Abs(below - lowerLeft.y) ? above : below;
            else if (aboveFits)
                y = above;
            else if (belowFits)
                y = below;
            else
                return lowerLeft; // no room above or below: better on screen and covering it than off screen
            return new Vector2(lowerLeft.x, y);
        }
    }
}
