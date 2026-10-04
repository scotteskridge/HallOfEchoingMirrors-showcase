using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One slice of the run's time strip: a coloured block with a skill icon and name. It grows
    /// with its share of the strip (the LayoutElement's flexible width); RunTimeStrip hides the
    /// name, then the icon, when the block is too narrow for them.
    /// </summary>
    public class TimeSliceView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private LayoutElement _layout;
        [SerializeField] private TraitIcon _icon;
        [SerializeField] private TMP_Text _label;

        // Room left of the name for the icon (20 px wide plus gaps), and the plain inset without one.
        private const float WithIcon = 28f;
        private const float WithoutIcon = 6f;

        /// <param name="share">Fraction of the strip (0 to 1).</param>
        /// <param name="tip">Its hover pop-up (null for none).</param>
        public void Show(Sprite icon, string name, float share, Color colour, Color textColour, System.Func<string> tip)
        {
            _background.color = colour;
            _layout.flexibleWidth = share;
            _label.text = name;
            _label.color = textColour;
            // The pop-up sits on the whole block, so the label and icon let the pointer through.
            _icon.Show(icon, null);
            if (tip != null)
                ToolTip.On(this, tip);
            _background.raycastTarget = true;
        }

        /// <summary>Hides the name and/or the icon when the block is too narrow (a hidden icon frees its space).</summary>
        public void Fit(bool showIcon, bool showName, bool hasIcon)
        {
            _icon.gameObject.SetActive(showIcon && hasIcon);
            _label.gameObject.SetActive(showName);
            var rect = _label.rectTransform;
            rect.offsetMin = new Vector2(showIcon && hasIcon ? WithIcon : WithoutIcon, rect.offsetMin.y);
        }
    }
}
