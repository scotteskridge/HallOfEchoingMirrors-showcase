using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Everything that survives a loop reset, and what a save file holds (see SaveSerializer).
    /// </summary>
    public class PersistentState
    {
        public int LoopNumber { get; set; }
        /// <summary>Game time (ticks) of every run that has ended, for how long a game has been played.</summary>
        public long TicksPlayed { get; set; }

        public HashSet<SwitchDefinition> FlippedSwitches { get; } = new HashSet<SwitchDefinition>();
        public HashSet<TaskDefinition> UnlockedTasks { get; } = new HashSet<TaskDefinition>();
        /// <summary>Tasks a switch has turned off. Wins over Starts Unlocked until a switch unlocks them again.</summary>
        public HashSet<TaskDefinition> LockedTasks { get; } = new HashSet<TaskDefinition>();
        public HashSet<Hue> UnlockedHues { get; } = new HashSet<Hue>();

        /// <summary>Mastery XP per attribute. Never resets. (Saved as "expertiseXp", its old name.)</summary>
        public Dictionary<ClaraAttribute, float> AttributeMasteryXp { get; } = new Dictionary<ClaraAttribute, float>();

        /// <summary>Maximum vitality Endurance has banked at the end of past runs, added to the base for every run. Never resets.</summary>
        public float KeptVitality { get; set; }

        public float AttributeMasteryXpOf(ClaraAttribute attribute) =>
            AttributeMasteryXp.TryGetValue(attribute, out float xp) ? xp : 0f;

        /// <summary>Every finished run, oldest first (saves from before version 9 start empty).</summary>
        public List<RunRecord> RunHistory { get; } = new List<RunRecord>();

        /// <summary>Mastery XP per skill. Never resets.</summary>
        public Dictionary<SkillDefinition, float> SkillMasteryXp { get; } = new Dictionary<SkillDefinition, float>();

        public float SkillMasteryXpOf(SkillDefinition skill) =>
            skill != null && SkillMasteryXp.TryGetValue(skill, out float xp) ? xp : 0f;

        /// <summary>
        /// Each skill's mastery XP as the last ended run began, for "faster than last run". Replaced
        /// whole at every run's end; a skill with no entry had none (0).
        /// </summary>
        public Dictionary<SkillDefinition, float> SkillMasteryXpAtLastRunStart { get; } = new Dictionary<SkillDefinition, float>();

        /// <summary>Whether a run has ended since saves began keeping <see cref="SkillMasteryXpAtLastRunStart"/> (version 24): until then there's nothing to compare.</summary>
        public bool HasLastRunStart { get; set; }

        /// <summary>A skill's mastery XP as the last ended run began: 0 if it had none, null if no run has ended yet.</summary>
        public float? SkillMasteryXpAtLastRunStartOf(SkillDefinition skill)
        {
            if (!HasLastRunStart)
                return null;
            return skill != null && SkillMasteryXpAtLastRunStart.TryGetValue(skill, out float xp) ? xp : 0f;
        }

        /// <summary>
        /// Kept resources held, e.g. Mirrors found. This is the only count: nothing is tracked
        /// past the maximum, so any found while she's full are simply not kept.
        /// </summary>
        public Dictionary<ResourceDefinition, int> Resources { get; } = new Dictionary<ResourceDefinition, int>();

        /// <summary>Extra room above a resource's starting maximum (meta currency will raise these).</summary>
        public Dictionary<ResourceDefinition, int> ResourceCapBonus { get; } = new Dictionary<ResourceDefinition, int>();

        public int ResourceOf(ResourceDefinition resource) =>
            resource != null && Resources.TryGetValue(resource, out int held) ? held : 0;

        /// <summary>Carried items she walked out with: safe in the real lab until packed for a run.</summary>
        public Dictionary<ResourceDefinition, int> Stash { get; } = new Dictionary<ResourceDefinition, int>();

        /// <summary>Stashed items the player has chosen to take in on the next run (at risk once in).</summary>
        public HashSet<ResourceDefinition> Packed { get; } = new HashSet<ResourceDefinition>();

        /// <summary>
        /// Ways found by searching, by the room that lists them and where they lead. The hall shifts
        /// between runs, but a way once found stays found.
        /// </summary>
        public HashSet<(NodeDefinition from, NodeDefinition to)> FoundWays { get; } = new HashSet<(NodeDefinition, NodeDefinition)>();

        /// <summary>
        /// How far each room's search has got, in steps (never more than the room's exploresToFill;
        /// a tick can count for a part of a step, so it's not always whole). Kept for good, partway
        /// included: a room is searched once, ever.
        /// </summary>
        public Dictionary<NodeDefinition, float> Explored { get; } = new Dictionary<NodeDefinition, float>();

        /// <summary>How many runs she has worked in each room (counted once per run, when it ends). A room worked in enough runs is known by heart.</summary>
        public Dictionary<NodeDefinition, int> RoomRuns { get; } = new Dictionary<NodeDefinition, int>();

        /// <summary>Rooms she has entered at least once, ever: the first entry opens the room's story.</summary>
        public HashSet<NodeDefinition> RoomsEntered { get; } = new HashSet<NodeDefinition>();

        /// <summary>Every story beat Clara has seen, in order. The start of her journal.</summary>
        public List<StoryBeat> Journal { get; } = new List<StoryBeat>();

        /// <summary>Revealed story beats the player hasn't closed yet, oldest first.</summary>
        public List<StoryBeat> UnreadStories { get; } = new List<StoryBeat>();
    }
}
