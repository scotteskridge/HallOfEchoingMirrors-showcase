using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Tunable numbers for a loop. Lives as an asset so balancing never needs a code change.
    /// All values are greybox placeholders.
    /// </summary>
    [CreateAssetMenu(fileName = "LoopSettings", menuName = "Hall of Echoing Mirrors/Loop Settings")]
    public class LoopSettings : ScriptableObject
    {
        [Tooltip("Pause the run when exploring finds a new way, so the player can queue the trip. " +
                 "Teaches that the queue can change during a run; untick once that's learned.")]
        public bool pauseWhenAWayIsFound = true;

        [Serializable]
        public class PoolSettings
        {
            [Tooltip("The pool's hue. Its name comes from game_text.txt (hues.*), so every pool needs one.")]
            public Hue hue = Hue.None;
            [Tooltip("Available from the start. If unticked, a switch must unlock this hue.")]
            [UnityEngine.Serialization.FormerlySerializedAs("unlocked")]
            public bool startsUnlocked = true;
            public float max = 50f;
        }

        [Header("Vitality (the run ends when this reaches 0)")]
        public float vitalityMax = 100f;
        [Tooltip("Vitality lost per second at the start of a run, whatever she is doing. Smooth: a little every tick.")]
        [Min(0f)] public float vitalityDrainPerSecond = 1f;
        [Tooltip("The drain grows by this much every minute of the run, smoothly and compounding " +
                 "(0.25 = 25% more each minute), so every run ends eventually.")]
        [Min(0f)] public float drainGrowthPerMinute = 0.5625f;

        [Header("Pathos pools (actions spend from them; whatever an empty pool can't cover hits vitality)")]
        public List<PoolSettings> pools = new List<PoolSettings> { new PoolSettings() };

        [Header("Action costs (off for now: actions cost time, and vitality drains on its own)")]
        [Tooltip("Tasks spend their Cost from vitality and pools while they run.")]
        public bool chargeActionCosts;
        [Tooltip("Carried items (the ring) cost their Carry Cost Per Second while she acts.")]
        public bool chargeCarryCosts;

        [Header("Rooms known by heart")]
        [Tooltip("Runs she must work in a room before she knows it by heart: what she did there last run is then " +
                 "already in the next run's queue. Counted when a run ends, so the 4th run carries into the 5th. Placeholder: 4.")]
        [Min(1)] public int byHeartRuns = 4;
        [Tooltip("Runs worked in a room at which its clock reaches Room Speed Cap. From Runs By Heart it rises in " +
                 "equal steps (the first by-heart run is already a step up); a room the player has not yet known by heart runs at ×1. Placeholder: 8.")]
        [Min(1)] public int fullSpeedRuns = 8;
        [Tooltip("How many times faster the clock runs in a room known by heart once she has worked there Full Speed Runs times. " +
                 "Multiplies the player's own speed tier. Placeholder: 5.")]
        [Min(1f)] public float roomSpeedCap = 5f;

        // Placeholder rule: linear ramp. Speed is 1 below byHeartRuns and rises in equal steps to
        // roomSpeedCap at fullSpeedRuns (the GDD's visit power law would never reach its range in 7-12 runs).
        /// <summary>How many times faster the clock runs in a room she has worked in this many runs: 1 until it's known by heart, then up to <see cref="roomSpeedCap"/>.</summary>
        public float RoomSpeedAfter(int runs)
        {
            if (runs < byHeartRuns)
                return 1f;
            int steps = Math.Max(1, fullSpeedRuns - byHeartRuns + 1);
            return 1f + (roomSpeedCap - 1f) * Math.Min(1f, (runs - byHeartRuns + 1f) / steps);
        }

        [Header("Pockets")]
        [Tooltip("Pockets she starts every run with. Every object she holds takes one (Goes In Pocket on the item); " +
                 "things like a satchel add more for the run. Placeholder: 5.")]
        [Min(0)] public int pocketSlots = 5;
        [Tooltip("How many of each object a room's floor holds in a run: what doesn't fit in her pockets is put down " +
                 "there, and picked up again with a Pick Up action. Lit candles and the like add more. Placeholder: 10.")]
        [Min(0)] public int floorSpace = 10;

        [Header("Cost curve (every task's time: family × standard trip × room step ^ depth × its own multiplier)")]
        [Tooltip("Seconds of one trip between rooms at normal speed: the yardstick every family's coefficient is a share of. " +
                 "Placeholder: 15, today's Travel.")]
        [Min(0.1f)] public float standardTripSeconds = 15f; // Placeholder rule: today's Travel time
        [Tooltip("Each room deeper in the hall makes every task done there this many times longer (1.1 = 10% longer per room). " +
                 "Tracks how fast her skills grow. Placeholder: 1.1, until a playtest reading replaces it.")]
        [Min(1f)] public float roomStep = 1.1f; // Placeholder rule: from an estimated skill speed on first reaching the lab

        [Header("XP (skills and stats)")]
        [Tooltip("XP for finishing a task, per second of its base time (its Seconds, before skills speed it up), " +
                 "rounded up: 1.5 gives a 10 s task 15 XP, however fast she gets. Given smoothly while it runs, to " +
                 "its skill and its stat. A task's own XP Reward overrides this.")]
        [Min(0f)] public float xpPerSecondOfTask = 1.5f;
        [Tooltip("Share of that XP that also goes into the skill's and the stat's mastery (1 = all of it).")]
        [Min(0f)] public float masteryShare = 1f;
        [Tooltip("XP needed for a skill's or stat's level 1 (this run's level).")]
        [Min(0.01f)] public float xpForFirstLevel = 10f;
        [Tooltip("XP needed for mastery level 1 (kept between runs). Later levels grow the same way as levels do.")]
        [Min(0.01f)] public float masteryXpForFirstLevel = 20f;
        [Tooltip("Each level needs this many times the XP of the one before (1.01 = 1% more)...")]
        [Min(1f)] public float xpGrowthPerLevel = 1.01f;
        [Tooltip("...but never less than this much more than the one before.")]
        [Min(0f)] public float xpMinIncreasePerLevel = 0.5f;

        [Header("Skills (speed)")]
        [Tooltip("Each skill level multiplies its tasks' speed by this (1.05 = 5% faster, compounding).")]
        [Min(1f)] public float skillSpeedPerLevel = 1.05f;
        [Tooltip("Each skill mastery level multiplies its tasks' speed by this. Multiplied with the above.")]
        [Min(1f)] public float skillMasterySpeedPerLevel = 1.01f;

        /// <summary>How much faster a skill makes its tasks at this mastery level (1 = no mastery): the one copy of the formula, for the rules and for screens that show it.</summary>
        public float MasterySpeedAt(float masteryLevel) => CoreMath.Pow(skillMasterySpeedPerLevel, masteryLevel);

        [Header("Stats that soften costs (only charged costs: the chase, the ring, or all while costs are on)")]
        [Tooltip("Attunement: each level makes every hue cost this much cheaper.")]
        public float attunementPerLevel = 0.04f;
        [Tooltip("Composure: each level makes actions take this much less from the hue pools.")]
        public float composureActionCostPerLevel = 0.03f;
        [Tooltip("Composure: each level makes carried items (the ring) drain this much less. Strong on purpose.")]
        public float composureCarryCostPerLevel = 0.15f;

        [Header("Stats train from their theme (searching: Perception, studying: Scholarship, instantiating: Attunement)")]
        [Tooltip("Endurance XP for each point of vitality she loses, whatever takes it.")]
        [Min(0f)] public float enduranceXpPerVitalityLost = 0.2f;
        [Tooltip("Composure XP every second of a run: the hall slowly noticing her.")]
        [Min(0f)] public float composureXpPerSecond = 0.1f;
        [Tooltip("Extra Composure XP every second she carries something that costs her (the ring).")]
        [Min(0f)] public float composureXpPerSecondCarrying = 0.2f;

        [Header("What stats do (each counts its level this run + its mastery)")]
        [Tooltip("Endurance: after every run, this share of the vitality she lost is banked as kept maximum vitality. " +
                 "Placeholder rule: ~0.03, a playtest sets it.")]
        [Min(0f)] public float enduranceBankShare = 0.03f;
        [Tooltip("Endurance: each level (at the run's end) makes the banked share this much bigger (0.01 = +1%).")]
        [Min(0f)] public float enduranceBankPerLevel = 0.01f;
        [Tooltip("Endurance: per level, how much of a restoration item may be wasted and she'll still use it " +
                 "(so a high Endurance uses items more readily).")]
        [Min(0f)] public float enduranceOverflowPerLevel = 0.5f;
        [Tooltip("Composure: each level multiplies how fast the drain grows (0.97 = 3% slower growth).")]
        [Range(0.5f, 1f)] public float composureDrainGrowthPerLevel = 0.97f;
        [Tooltip("Composure: each level makes a task's own extra drain this much smaller (0.03 = 3% less, compounding).")]
        [Range(0f, 0.5f)] public float composureExtraDrainPerLevel = 0.03f;
        [Tooltip("Attunement: each level makes a restorative give back this much more vitality (0.02 = +2%).")]
        [Min(0f)] public float attunementRestorePerLevel = 0.02f;
        [Tooltip("Stats that start asleep: their effects stay off (and earn no XP) until a switch that wakes them flips. " +
                 "A switch lists what it wakes under Wakes Attributes.")]
        public List<ClaraAttribute> asleepAttributes = new List<ClaraAttribute>();
        [Tooltip("Placeholder rule: when a run ends in a collapse or by the player, this share of the run's stat XP joins mastery " +
                 "(a walk out settles all of it).")]
        [Range(0f, 1f)] public float collapseStatXpShare = 0.5f;
        [Tooltip("Scholarship: every this many levels, each study action gives one more of what it gives. 0 = off.")]
        [Min(0)] public int scholarshipLevelsPerExtraStudy = 5;
        [Tooltip("A skill learns faster with its stat (the skill's Learns Faster With): +this much XP per level of that stat.")]
        [Min(0f)] public float statSkillXpPerLevel = 0.05f;

        // Right-click the asset's header in the Inspector (or its ⋮ menu) to run this.
        [ContextMenu("Replace Pools With The Seven Hues")]
        private void ReplacePoolsWithSevenHues()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Replace Pools With The Seven Hues");
#endif
            pools = new List<PoolSettings>();
            foreach (Hue hue in Enum.GetValues(typeof(Hue)))
            {
                if (hue != Hue.None)
                    pools.Add(new PoolSettings { hue = hue });
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
