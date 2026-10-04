using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The dark picture behind the map's rooms and the vignette (darkened edges) over them, both
    /// coloured from the MapStyle so the look is tuned in one asset. Neither image catches the mouse.
    /// Placeholder art: a generated gradient and vignette, to be swapped for painted art later.
    /// </summary>
    public class MapBackdrop : MonoBehaviour
    {
        [SerializeField] private MapView _map;
        [Tooltip("Behind the rooms: tinted by the style's Backdrop Tint.")]
        [SerializeField] private Image _backdrop;
        [Tooltip("Over the rooms (under popovers): black at the style's Vignette Strength.")]
        [SerializeField] private Image _vignette;

        private void Update()
        {
            var style = _map.Style;
            if (_backdrop.color != style.backdropTint)
                _backdrop.color = style.backdropTint;
            var vignette = new Color(1f, 1f, 1f, style.vignetteStrength);
            if (_vignette.color != vignette)
                _vignette.color = vignette;
        }
    }
}
