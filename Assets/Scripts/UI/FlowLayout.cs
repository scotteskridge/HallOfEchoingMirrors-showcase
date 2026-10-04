using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Lays its children out left to right, each at its preferred size, and starts a new line when
    /// the next one won't fit (like words in a paragraph). Its own preferred height is all its lines,
    /// so in a stack that sets its children's heights (or with a ContentSizeFitter, when nothing above
    /// lays it out) it grows downward as items are added (the stats row's chips).
    /// </summary>
    [AddComponentMenu("Layout/Flow Layout (wrapping)")]
    public class FlowLayout : LayoutGroup
    {
        [Tooltip("Gap between items on a line, and between lines.")]
        [SerializeField, Min(0f)] private float _spacing = 6f;

        private readonly List<Vector2> _sizes = new List<Vector2>();
        private Vector2[] _placed = new Vector2[0];

        /// <summary>
        /// Where each item goes, as its top-left corner measured right and down from the top-left of
        /// the area. An item too wide for any line gets a line to itself.
        /// </summary>
        public static Vector2[] Place(IReadOnlyList<Vector2> sizes, float maxWidth, float spacing)
        {
            var placed = new Vector2[sizes.Count];
            float x = 0f, y = 0f, lineHeight = 0f;
            for (int i = 0; i < sizes.Count; i++)
            {
                // Not the first on its line, and doesn't fit: down to a new line.
                if (x > 0f && x + sizes[i].x > maxWidth)
                {
                    y += lineHeight + spacing;
                    x = 0f;
                    lineHeight = 0f;
                }
                placed[i] = new Vector2(x, y);
                x += sizes[i].x + spacing;
                lineHeight = Mathf.Max(lineHeight, sizes[i].y);
            }
            return placed;
        }

        /// <summary>How tall the items are altogether, placed where <see cref="Place"/> put them.</summary>
        public static float Height(IReadOnlyList<Vector2> sizes, IReadOnlyList<Vector2> placed)
        {
            float height = 0f;
            for (int i = 0; i < sizes.Count; i++)
                height = Mathf.Max(height, placed[i].y + sizes[i].y);
            return height;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal(); // collects the children to lay out
            // Its width comes from its parent (it wraps to fit), not from its children.
            SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            _sizes.Clear();
            foreach (var child in rectChildren)
                _sizes.Add(new Vector2(LayoutUtility.GetPreferredWidth(child), LayoutUtility.GetPreferredHeight(child)));
            _placed = Place(_sizes, rectTransform.rect.width - padding.horizontal, _spacing);
            float height = Height(_sizes, _placed) + padding.vertical;
            SetLayoutInputForAxis(height, height, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            // Widths only: where the lines break is worked out with the heights, just after.
            for (int i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 0, padding.left, LayoutUtility.GetPreferredWidth(rectChildren[i]));
        }

        public override void SetLayoutVertical()
        {
            for (int i = 0; i < rectChildren.Count && i < _placed.Length; i++)
            {
                var child = rectChildren[i];
                SetChildAlongAxis(child, 0, padding.left + _placed[i].x, _sizes[i].x);
                SetChildAlongAxis(child, 1, padding.top + _placed[i].y, _sizes[i].y);
            }
        }
    }
}
