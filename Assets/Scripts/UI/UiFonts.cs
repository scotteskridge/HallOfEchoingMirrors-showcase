using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The UI's fonts and type sizes, one entry per TextRole, so the whole UI can be retuned here.
    /// Sizes are at the 1920×1080 reference (the Canvas Scaler). In the editor, every text tagged
    /// with a FontRole takes its font and size from here with *Hall of Echoing Mirrors → UI →
    /// Apply UI Fonts*; at runtime only the story feed switches roles (story lines and notes).
    /// </summary>
    [CreateAssetMenu(menuName = "Hall of Echoing Mirrors/UI Fonts", fileName = "UiFonts")]
    public class UiFonts : ScriptableObject
    {
        [Serializable]
        public class Style
        {
            public TextRole role;
            [Tooltip("The TextMeshPro font asset (an SDF asset in Assets/Fonts).")]
            public TMP_FontAsset font;
            [Tooltip("Size in pixels at 1920×1080.")]
            [Min(6f)] public float size = 17f;
            [Tooltip("Extra space between lines (TextMeshPro's Line Spacing: 0 is the font's own).")]
            public float lineSpacing;
        }

        [SerializeField] private List<Style> _styles = new List<Style>();

        [Tooltip("A text that shrinks to fit (Auto Size) goes no smaller than this share of its size.")]
        [SerializeField, Range(0.5f, 1f)] private float _autoSizeFloor = 0.85f;

        public IReadOnlyList<Style> Styles => _styles;

        /// <summary>The role's style, or null if it has none yet.</summary>
        public Style Find(TextRole role)
        {
            foreach (var style in _styles)
                if (style.role == role)
                    return style;
            return null;
        }

        /// <summary>The role's style. A role without a font is a setup mistake, so it fails loudly.</summary>
        public Style For(TextRole role)
        {
            var style = Find(role);
            if (style == null || style.font == null)
                throw new InvalidOperationException($"UiFonts: the {role} role has no font. Set it in {name}.");
            return style;
        }

        /// <summary>Gives a text its role's font, size and line spacing (<paramref name="scale"/> times the size).</summary>
        public void Apply(TMP_Text text, TextRole role, float scale = 1f)
        {
            var style = For(role);
            text.font = style.font;
            text.fontSharedMaterial = style.font.material;
            float size = style.size * scale;
            text.fontSize = size;
            text.lineSpacing = style.lineSpacing;
            if (text.enableAutoSizing)
            {
                text.fontSizeMax = size;
                text.fontSizeMin = Mathf.Round(size * _autoSizeFloor);
            }
        }

        /// <summary>
        /// Lets rich text switch to these fonts by name (UiStyle.Heading's &lt;font&gt; tag). TextMeshPro
        /// otherwise only finds fonts in a Resources folder, which this project doesn't use.
        /// </summary>
        public void RegisterForMarkup()
        {
            foreach (var style in _styles)
                if (style.font != null)
                    MaterialReferenceManager.AddFontAsset(style.font);
        }
    }
}
