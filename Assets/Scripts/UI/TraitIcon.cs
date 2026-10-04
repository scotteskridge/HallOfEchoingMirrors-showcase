using System;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One stat or skill icon (on a stats chip, or at the start of an action row), with its own
    /// small hover pop-up. With no icon it goes blank but keeps its space, so names stay lined up.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TraitIcon : MonoBehaviour
    {
        private Image _image;

        // Found when first needed, not in Awake: an action row inside a section that's switched off
        // is filled before Unity has run its Awake.
        private Image Image => _image != null ? _image : _image = GetComponent<Image>();

        /// <param name="sprite">Null leaves the space blank.</param>
        /// <param name="tip">Its pop-up; null for none, so the pointer passes through to whatever it sits on (a chip's own pop-up).</param>
        /// <param name="tint">Multiplies the icon's colour (e.g. dimmer); white if not given.</param>
        public void Show(Sprite sprite, Func<string> tip, Color? tint = null)
        {
            var image = Image;
            bool shown = sprite != null;
            if (image.sprite != sprite)
                image.sprite = sprite;
            if (image.enabled != shown)
                image.enabled = shown;
            image.color = tint ?? Color.white;
            image.raycastTarget = shown && tip != null;
            if (tip != null)
                ToolTip.On(this, tip);
        }
    }
}
