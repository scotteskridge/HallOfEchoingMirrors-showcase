using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// What sort of task this is (GDD §9), so other systems can react to kinds rather than to
    /// each task, e.g. "searching" blurbs. Travel and Explore are the common verbs, not kinds.
    /// </summary>
    public enum TaskKind
    {
        Other = 0,
        Search = 1,
        Study = 2,
        Gather = 3,
        Assist = 4,
        Take = 5,
        Work = 6,
        /// <summary>Making something solid out of a mirror (trains Attunement).</summary>
        Instantiate = 7,
    }

    /// <summary>
    /// The generic blurb bucket a task belongs to (GDD §12, ambient buckets), independent of
    /// TaskKind: several tasks can share a TaskKind (what stat trains) without sharing a story
    /// moment, e.g. Pick up and Gather a wisp are both TaskKind.Gather, but only the wisp task
    /// is Gathering. None (the default) means the task only speaks through a bucket that names
    /// it directly (BlurbBucket.tasks), never a topic-matched one.
    /// </summary>
    public enum BlurbTopic
    {
        None = 0,
        Gathering = 1,
        Instantiating = 2,
    }

    /// <summary>
    /// One thing Clara can do in a loop. Created as an asset, so new tasks need no code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTask", menuName = "Hall of Echoing Mirrors/Task")]
    public class TaskDefinition : ContentAsset
    {
        /// <summary>Part of a task's cost: which source pays, and what percentage of the total.</summary>
        [Serializable]
        public class CostShare
        {
            public CostSource source = CostSource.Vitality;
            [Tooltip("Percentage of the total cost. If the shares don't add up to 100, they're scaled to fit.")]
            [Min(0f)] public float percent = 100f;
        }

        /// <summary>A minimum stat strength (level this run + mastery), e.g. Perception 3 to notice something hidden.</summary>
        [Serializable]
        public class AttributeRequirement
        {
            public ClaraAttribute attribute = ClaraAttribute.Perception;
            [Min(1)] public int level = 1;
        }

        /// <summary>A minimum skill strength (level this run + mastery), e.g. Crafting 8 to craft the gem.</summary>
        [Serializable]
        public class SkillRequirement
        {
            public SkillDefinition skill;
            [Min(1)] public int level = 1;
        }

        /// <summary>
        /// Holding something changes this task, once per one held: e.g. with the bench cleared,
        /// Study costs ×0.5; each "What he did that night" makes cutting the stone ×0.85.
        /// </summary>
        [Serializable]
        public class HeldModifier
        {
            public ResourceDefinition whileHolding;
            [Tooltip("Cost multiplier for each one held (0.85 twice = 0.72).")]
            [Min(0f)] public float costEach = 1f;
            [Tooltip("Time multiplier for each one held.")]
            [Min(0f)] public float timeEach = 1f;
        }

        public string displayName = "New Task";
        [Tooltip("What sort of task it is, e.g. Search: decides which stat it trains (see AttributeMath.StatForKind).")]
        public TaskKind kind = TaskKind.Other;
        [Tooltip("Which generic blurb bucket this fits, independent of Kind, e.g. Gathering. " +
                 "None: this task only speaks through a bucket that names it directly.")]
        public BlurbTopic blurbTopic = BlurbTopic.None;
        [Tooltip("The kind of work it is: gives its base time from the cost curve (family × standard trip × room step ^ depth of " +
                 "the room it's done in × Time ×). A task with no family can't be done.")]
        public CostFamily family;
        [Tooltip("This task's own stretch of its family's time (2 = twice as long, and pays twice the XP).")]
        [Min(0.01f)] public float durationMultiplier = 1f;
        [Tooltip("This task's own stretch of the XP it pays for its time (2 = twice the XP per second).")]
        [Min(0f)] public float xpMultiplier = 1f;
        [Tooltip("Available from the start. If unticked, a switch must unlock it.")]
        public bool startsUnlocked = true;
        [Tooltip("Can only be done once in a run, e.g. making flint and steel.")]
        public bool oncePerRun;
        [Tooltip("Done once when queued, then the game pauses so the player can choose what's next. " +
                 "Otherwise a queued task repeats until it's done (its room explored, or she can hold no more).")]
        public bool singleAction;
        [Tooltip("Optional: a line of flavour shown in its hover explanation, under its name.")]
        [TextArea(1, 4)] public string description;
        [Tooltip("The skill this task trains, and which makes it quicker as it levels (e.g. Wayfinding).")]
        public SkillDefinition skill;
        [Tooltip("XP for finishing it, given smoothly while it runs, to its skill and to the stat its kind trains " +
                 "(Search: Perception, Study: Scholarship, Instantiate: Attunement), and their mastery. " +
                 "0 = the usual amount: its base seconds (from the cost curve) × LoopSettings' XP Per Second Of Task × XP ×, rounded up.")]
        [Min(0f)] public float xpReward;
        [Tooltip("The stat this task's XP trains, instead of the one its kind trains. None = by its kind " +
                 "(Search: Perception, Study: Scholarship, Instantiate: Attunement; Explore: Perception).")]
        public ClaraAttribute trainsAttribute = ClaraAttribute.None;
        /// <summary>
        /// Set only on the actions the game makes from the Pick up verb (GameContent), one per item:
        /// takes every one of these from the floor here, as many as fit in her pockets.
        /// </summary>
        [NonSerialized] public ResourceDefinition picksUp;
        /// <summary>The same for the Put down verb: puts every one of these in her pockets on the floor here.</summary>
        [NonSerialized] public ResourceDefinition putsDown;

        [Header("Cost (drained evenly while the task runs)")]
        [Tooltip("Total spent over the whole task. E.g. 20 over 10 seconds drains 2 per second.")]
        [Min(0f)] public float cost;
        [Tooltip("Who pays the total, e.g. 100% Vitality, or 50% Amber + 50% Citrine. Empty means free.")]
        public List<CostShare> costShares = new List<CostShare>();
        [Tooltip("Charge this cost even while action costs are switched off (LoopSettings): an extra drain on " +
                 "top of the passive one, e.g. the chase, so it exhausts her quickly.")]
        public bool alwaysCharged;
        [Tooltip("Extra vitality lost per second, on top of the normal drain, while this task is the one running " +
                 "(not held off by candles, not grown, not changed by skill). It shows in the top bar's drain. 0 = none.")]
        [Min(0f)] public float extraDrainPerSecond;

        [Header("Escalating charge (vitality, growing with each use this run)")]
        [Tooltip("A flat vitality charge every time she does this, paid even while action costs are switched off and never " +
                 "softened by her stats (it's how travel stays costly however skilled she gets). The first use this run " +
                 "costs this much; each use after costs Growth × more. Resets every run. Rooms and held items that ease " +
                 "the task's cost still scale it. 0 = none.")]
        [Min(0f)] public float escalatingCharge;
        [Tooltip("Each use this run multiplies the next charge by this (1.2 = 20% more each time). 1 = never grows.")]
        [Min(1f)] public float chargeGrowth = 1f;

        [Header("Resources (tools, stats and kept)")]
        [Tooltip("What she must already have to start, e.g. 1 Tool A, or 3 Mirrors found.")]
        public List<ResourceAmount> needs = new List<ResourceAmount>();
        [Tooltip("What she gets when the task is done. Per-run tools reset each loop; kept resources stay.")]
        public List<ResourceAmount> gives = new List<ResourceAmount>();
        [Tooltip("What's taken from her when the task is done, e.g. putting the ring down. List it in Needs too.")]
        public List<ResourceAmount> takes = new List<ResourceAmount>();
        [Tooltip("Things that make this task cheaper or quicker while she holds them.")]
        public List<HeldModifier> easierWith = new List<HeldModifier>();

        [Header("Holding off the darkness")]
        [Tooltip("Finishing it multiplies the current drain by this (0.85 = 15% lower), for the rest of the run. " +
                 "The drain keeps growing from the lower value. 1 = no effect.")]
        [Range(0.05f, 1f)] public float drainTimes = 1f;

        [Header("Ending the run")]
        [Tooltip("Finishing this task walks her out of the hall: the run ends, vitality unspent, and whatever " +
                 "she carries is kept. For exits, e.g. the mirror lab.")]
        public bool walksOut;

        [Header("Stat thresholds (level this run + mastery)")]
        public List<AttributeRequirement> requiresAttributes = new List<AttributeRequirement>();

        [Header("Skill thresholds (level this run + mastery)")]
        [Tooltip("Skills she must have this strong to start it, e.g. Crafting 8. Shown greyed with the gate until met, never hidden.")]
        public List<SkillRequirement> requiresSkills = new List<SkillRequirement>();

        public bool HasCost => cost > 0f && costShares.Count > 0;

        /// <summary>
        /// Splits a total cost into what each source pays. Shares are scaled so they always add up
        /// to the whole total.
        /// </summary>
        public List<(CostSource source, float amount)> CostBreakdown(float total)
        {
            var result = new List<(CostSource, float)>();
            if (total <= 0f)
                return result;

            float sum = 0f;
            foreach (var share in costShares)
                sum += Mathf.Max(0f, share.percent);
            if (sum <= 0f)
                return result;

            foreach (var share in costShares)
            {
                float amount = total * Mathf.Max(0f, share.percent) / sum;
                if (amount > 0f)
                    result.Add((share.source, amount));
            }
            return result;
        }
    }
}
