using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How a way between two rooms is drawn: one image stretched from room to room and turned to
    /// point along the way. Shared by the map and the editor's layout preview, so they always agree.
    /// </summary>
    public static class MapLines
    {
        /// <summary>Stretches and turns <paramref name="line"/> (centre pivot) to run from a to b.</summary>
        public static void Place(RectTransform line, Vector2 a, Vector2 b, float thickness)
        {
            line.anchoredPosition = (a + b) / 2f;
            line.sizeDelta = new Vector2(Vector2.Distance(a, b), thickness);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }
    }
}
