using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Each stat's icon, for the stats row and action rows. (A skill's icon is on its own asset,
    /// since skills are content; stats are fixed, so theirs live here.)
    /// </summary>
    [CreateAssetMenu(fileName = "TraitIcons", menuName = "Hall of Echoing Mirrors/UI/Trait Icons")]
    public class TraitIcons : ScriptableObject
    {
        [System.Serializable]
        public struct StatIcon
        {
            public ClaraAttribute stat;
            public Sprite icon;
        }

        [Tooltip("One icon per stat. A stat without one stops the game with an error when it's shown.")]
        [SerializeField] private List<StatIcon> _stats = new List<StatIcon>();

        /// <summary>The stat's icon; null for None (an action that trains no stat).</summary>
        public Sprite IconOf(ClaraAttribute stat)
        {
            if (stat == ClaraAttribute.None)
                return null;
            foreach (var entry in _stats)
                if (entry.stat == stat && entry.icon != null)
                    return entry.icon;
            throw new System.InvalidOperationException(
                $"TraitIcons: no icon for {stat}. Add one to the Trait Icons asset in Assets/Data/UI.");
        }
    }
}
