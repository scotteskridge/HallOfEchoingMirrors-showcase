using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The UI's palette, one colour per ColourRole, so a palette change is an edit here and
    /// *Hall of Echoing Mirrors → UI → Apply UI Colours*, not a recolour pass through every prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Hall of Echoing Mirrors/UI Colours", fileName = "UiColours")]
    public class UiColours : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ColourRole role;
            public Color colour = Color.white;
        }

        [SerializeField] private List<Entry> _colours = new List<Entry>();

        public IReadOnlyList<Entry> Colours => _colours;

        /// <summary>The role's entry, or null if it has none yet.</summary>
        public Entry Find(ColourRole role)
        {
            foreach (var entry in _colours)
                if (entry.role == role)
                    return entry;
            return null;
        }

        /// <summary>The role's colour. A role without one is a setup mistake, so it fails loudly.</summary>
        public Color For(ColourRole role)
        {
            var entry = Find(role);
            if (entry == null)
                throw new InvalidOperationException($"UiColours: the {role} role has no colour. Set it in {name}.");
            return entry.colour;
        }

        /// <summary>The role's colour as "#RRGGBB", for a rich-text colour tag (UiStyle.Colour).</summary>
        public string HexOf(ColourRole role) => "#" + ColorUtility.ToHtmlStringRGB(For(role));

        /// <summary>Gives a Graphic its role's colour.</summary>
        public void Apply(Graphic graphic, ColourRole role) => graphic.color = For(role);
    }
}
