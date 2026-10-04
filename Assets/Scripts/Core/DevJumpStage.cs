using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// A developer-only testing aid (plan 027d): a point in the game, e.g. "After the talk", that the
    /// dev panel's Jump to buttons put the kept state at, so a late stage can be checked without
    /// replaying the act. Data, so the stages follow the content. Not game content: saves never name one.
    /// </summary>
    [CreateAssetMenu(fileName = "NewJumpStage", menuName = "Hall of Echoing Mirrors/Dev Jump Stage")]
    public class DevJumpStage : ScriptableObject
    {
        /// <summary>A room's search, at least this far.</summary>
        [Serializable]
        public class RoomSearched
        {
            public NodeDefinition room;
            [Range(0, 100)] public int percent = 100;
        }

        /// <summary>A skill's mastery, at least this level.</summary>
        [Serializable]
        public class SkillLevel
        {
            public SkillDefinition skill;
            [Min(0)] public int level;
        }

        [Tooltip("The button's caption, e.g. \"After the talk\".")]
        public string displayName = "New stage";
        [Tooltip("Flipped in this order, with all their real effects (unlocks, locks, ways, a reopened search); their stories go " +
                 "in the journal already read.")]
        public List<SwitchDefinition> switches = new List<SwitchDefinition>();
        [Tooltip("Kept things she holds at least this many of, e.g. 3 Insight.")]
        public List<ResourceAmount> keptItems = new List<ResourceAmount>();
        [Tooltip("Rooms searched at least this far, after the switches (so after any reset they make). Each counts as entered.")]
        public List<RoomSearched> roomsSearched = new List<RoomSearched>();
        [Tooltip("Skills whose kept mastery is at least this level, e.g. Crafting at the gem's gate.")]
        public List<SkillLevel> skillMastery = new List<SkillLevel>();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
