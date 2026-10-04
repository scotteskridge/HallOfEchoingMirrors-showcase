using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// A skill: a kind of action Clara gets better at, e.g. Wayfinding (travelling and exploring).
    /// Its level resets each run; its mastery is kept. Each level and each mastery level make its
    /// tasks quicker (LoopSettings). A skill is shown once any unlocked task uses it.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Hall of Echoing Mirrors/Skill")]
    public class SkillDefinition : ContentAsset
    {
        public string displayName = "New Skill";
        [Tooltip("A note for the writer: what the skill covers.")]
        [TextArea(1, 3)] public string description;
        [Tooltip("The stat that makes this skill learn faster (e.g. Gathering: Endurance). Each level of it adds " +
                 "Stat Skill Xp Per Level (LoopSettings) to this skill's XP. None for no stat.")]
        public ClaraAttribute learnsFasterWith = ClaraAttribute.None;
        [Tooltip("Its icon: on its chip in the stats row and on the actions it speeds up. " +
                 "Stat icons are in the Trait Icons asset (Assets/Data/UI).")]
        [SerializeField] private Sprite _icon;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>Its icon, or null if it has none yet.</summary>
        public Sprite Icon => _icon;
    }
}
