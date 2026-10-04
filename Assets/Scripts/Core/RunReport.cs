using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// What happened in one finished run, for the results card: how long it lasted, what Clara
    /// did, and what she gained. Built by Simulation when a run ends; never changes afterwards.
    /// </summary>
    public class RunReport
    {
        public int LoopNumber { get; }
        public long Ticks { get; }
        public float Seconds { get; }
        public LoopEndReason EndReason { get; }

        /// <summary>Every task finished this run, counting repeats.</summary>
        public int ActionsCompleted { get; }

        /// <summary>Each task finished this run and how many times, in the order first finished.</summary>
        public List<(TaskDefinition task, int count)> Tasks { get; } = new List<(TaskDefinition, int)>();

        /// <summary>Trips she made this run, and the vitality their growing charge took (the Summary's line about moves).</summary>
        public int Moves { get; internal set; }
        public float MoveVitality { get; internal set; }

        /// <summary>
        /// Each other task whose escalating charge took vitality this run (trips are the Moves above): the task,
        /// how many goes she finished, and the vitality its charge took, in the order first finished.
        /// </summary>
        public List<(TaskDefinition task, int goes, float vitality)> Charges { get; } = new List<(TaskDefinition, int, float)>();

        /// <summary>Maximum vitality Endurance banked when this run ended (the Summary's "+N vitality kept" line).</summary>
        public float KeptVitalityGained { get; internal set; }

        /// <summary>The task she was partway through when the run ended, or null.</summary>
        public TaskDefinition Unfinished { get; }

        public class AttributeGain
        {
            public ClaraAttribute Attribute;
            /// <summary>Level reached this run (levels reset every loop).</summary>
            public int Level;
            public int MasteryBefore;
            public int MasteryAfter;
            /// <summary>Progress (0–1) toward the next mastery level, before and after the run.</summary>
            public float MasteryProgressBefore;
            public float MasteryProgressAfter;
        }

        /// <summary>Attributes trained this run.</summary>
        public List<AttributeGain> Attributes { get; } = new List<AttributeGain>();

        public class SkillGain
        {
            public SkillDefinition Skill;
            /// <summary>Level reached this run (levels reset every loop).</summary>
            public int Level;
            public int MasteryBefore;
            public int MasteryAfter;
            /// <summary>Progress (0–1) toward the next mastery level, before and after the run.</summary>
            public float MasteryProgressBefore;
            public float MasteryProgressAfter;
        }

        /// <summary>Skills trained this run, in Game Content order.</summary>
        public List<SkillGain> Skills { get; } = new List<SkillGain>();

        /// <summary>One slice of the run's time: the ticks spent on one skill's actions.</summary>
        public class TimeSlice
        {
            /// <summary>The skill, or null for the "Other" slice (actions with no skill).</summary>
            public SkillDefinition Skill;
            public long Ticks;
            public float Seconds;
            /// <summary>Fraction (0–1) of the time recorded; all slices add up to 1.</summary>
            public float Share;
        }

        /// <summary>Where the run's time went, longest slice first, "Other" (skill null) last.</summary>
        public List<TimeSlice> TimeBySkill { get; } = new List<TimeSlice>();

        /// <summary>Kept resources that went up this run, e.g. Mirrors found 3 → 5.</summary>
        public List<(ResourceDefinition resource, int before, int after)> Kept { get; } =
            new List<(ResourceDefinition, int, int)>();

        /// <summary>Per-run tools and stats she was holding at the end.</summary>
        public List<(ResourceDefinition resource, int amount)> ToolsAndStats { get; } =
            new List<(ResourceDefinition, int)>();

        public List<SwitchDefinition> SwitchesFlipped { get; } = new List<SwitchDefinition>();

        /// <summary>Carried items she walked out with (kept), or that the anchor made her leave behind (lost).</summary>
        public List<(ResourceDefinition item, int amount)> CarriedKept { get; } = new List<(ResourceDefinition, int)>();
        public List<(ResourceDefinition item, int amount)> CarriedLost { get; } = new List<(ResourceDefinition, int)>();

        /// <summary>Rooms searched this run, and how far each bar got (0 to 1). Every run starts from empty.</summary>
        public List<(NodeDefinition room, float searched)> Explored { get; } =
            new List<(NodeDefinition, float)>();

        /// <summary>Rooms this run made known by heart (their count reached the threshold).</summary>
        public List<NodeDefinition> KnownByHeartNow { get; } = new List<NodeDefinition>();

        /// <summary>One line of the milestones table: this run against the two before it.</summary>
        public class MilestoneRow
        {
            public ContentAsset Milestone; // a switch with a story, or a room
            /// <summary>Run seconds when reached this run, or null.</summary>
            public float? ThisRun;
            /// <summary>Run seconds in the run before this one, or null (not reached, or no such run).</summary>
            public float? LastRun;
            /// <summary>Run seconds in the run before that, or null.</summary>
            public float? PreviousRun;
            /// <summary>This run minus last run, in seconds (negative = faster). Null unless both reached it.</summary>
            public float? Change => ThisRun.HasValue && LastRun.HasValue ? ThisRun - LastRun : null;
            /// <summary>Reached this run and never in any earlier run.</summary>
            public bool IsNew;
        }

        /// <summary>
        /// Every milestone she has reached in any run. Those reached this run come first, by time;
        /// the rest follow in the order they were first ever reached.
        /// Placeholder rule: this order is a guess; see PROJECT_NOTES.
        /// </summary>
        public List<MilestoneRow> Milestones { get; } = new List<MilestoneRow>();

        /// <summary>The run before this one, from the run history (so it survives a load), or null.</summary>
        public RunRecord RunBefore { get; internal set; }

        /// <summary>The longest run before this one (the record she was trying to beat), or null. Runs ended early don't count.</summary>
        public RunRecord Longest { get; internal set; }

        /// <summary>
        /// This run beat every earlier run. Never on the first counted run, nor on a tie, nor when
        /// the player ended this run early.
        /// </summary>
        public bool IsNewLongest => RunRecord.CountsForLongestWhen(EndReason) && Longest != null && Ticks > Longest.Ticks;

        private readonly int _ticksPerSecond;

        /// <summary>An earlier run's length in seconds, for showing next to this one.</summary>
        public float SecondsOf(RunRecord record) => (float)record.Ticks / _ticksPerSecond;

        public RunReport(int loopNumber, long ticks, int ticksPerSecond, LoopEndReason endReason,
            int actionsCompleted, TaskDefinition unfinished)
        {
            LoopNumber = loopNumber;
            Ticks = ticks;
            _ticksPerSecond = ticksPerSecond;
            Seconds = (float)ticks / ticksPerSecond;
            EndReason = endReason;
            ActionsCompleted = actionsCompleted;
            Unfinished = unfinished;
        }
    }
}
