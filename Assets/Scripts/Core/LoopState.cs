using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>Why a run ended. (Numbered by hand: 2 was "stalled", which runs no longer do.)</summary>
    public enum LoopEndReason
    {
        /// <summary>Vitality ran out: the anchor pulled her back.</summary>
        Exhausted = 0,
        /// <summary>The player chose to end the run early (quitting mid-run counts the same).</summary>
        EndedByPlayer = 1,
        /// <summary>She walked out through an exit: what she carries is kept.</summary>
        WalkedOut = 3,
    }

    /// <summary>
    /// Everything that resets when a loop ends. A new loop gets a brand-new LoopState.
    /// </summary>
    public class LoopState
    {
        public Pool Vitality { get; }
        public List<Pool> Pools { get; } = new List<Pool>();
        public long TicksElapsed { get; set; }
        public bool IsOver { get; set; }
        public LoopEndReason EndReason { get; set; }
        /// <summary>At the end of the run: carried items kept (walked out) or lost (yanked out).</summary>
        public List<(ResourceDefinition item, int amount)> CarriedKept { get; } = new List<(ResourceDefinition, int)>();
        public List<(ResourceDefinition item, int amount)> CarriedLost { get; } = new List<(ResourceDefinition, int)>();


        /// <summary>
        /// Milestones reached this run, in order, with the run time each was reached at. A milestone is
        /// a switch with a story (the first time it flips, and again in later runs whenever its condition
        /// is met) or a room (her first entry into it each run, except the start room).
        /// </summary>
        public List<(ContentAsset milestone, float seconds)> Milestones { get; } = new List<(ContentAsset, float)>();

        // A plain loop: asked every tick, so no closure per call.
        public bool HasReached(ContentAsset milestone)
        {
            foreach (var reached in Milestones)
                if (reached.milestone == milestone)
                    return true;
            return false;
        }

        /// <summary>A restoration item giving vitality back over time (one per item used).</summary>
        public class Restoring
        {
            public ResourceDefinition Item;
            public float PerSecond;
            public float Left;
        }

        /// <summary>Restoration items being used now: one of each kind at a time; different kinds add together.</summary>
        public List<Restoring> Restorings { get; } = new List<Restoring>();

        /// <summary>
        /// How far the darkness has been held off this run: the drain is multiplied by this
        /// (1 = not at all; 0.85 after one ×0.85 action). Resets every run.
        /// </summary>
        public float DrainHeldOff { get; set; } = 1f;

        /// <summary>
        /// How much the drain has grown this run (1 at the start). Grown a little every tick at the
        /// rate of the moment, so Composure slowing the growth mid-run doesn't rewrite what's past.
        /// </summary>
        public float DrainGrown { get; set; } = 1f;

        /// <summary>This run's actions, in order. Starts empty every run.</summary>
        public ActionQueue Queue { get; }
        /// <summary>The queue entry the running task belongs to (always the top one), or null.</summary>
        public QueueEntry RunningEntry { get; set; }
        /// <summary>Set once the queue has run dry, so QueueRanOut fires once until more is added.</summary>
        public bool QueueRanOutNotified { get; set; }

        public TaskDefinition CurrentTask { get; set; }
        /// <summary>For a trip under way, where she's going. Null otherwise.</summary>
        public NodeDefinition CurrentDestination { get; set; }

        /// <summary>Where Clara is. Null if the game has no places.</summary>
        public NodeDefinition CurrentNode { get; set; }

        /// <summary>
        /// Work done on the running task, in ticks at normal speed. At speed x1.2 a tick does 1.2 work,
        /// so a level-up mid-task speeds up the task already under way.
        /// </summary>
        public float CurrentTaskWorkDone { get; set; }
        /// <summary>Work the running task needs in total: its duration in ticks at normal speed.</summary>
        public int CurrentTaskWorkNeeded { get; set; }
        public float CurrentTaskProgress => CurrentTaskWorkNeeded > 0 ? CurrentTaskWorkDone / CurrentTaskWorkNeeded : 0f;

        /// <summary>One source paying for the running task (vitality or a hue pool).</summary>
        public class RunningCost
        {
            public Pool Pool;
            /// <summary>Cost per unit of work, so a faster task spends the same total, just sooner.</summary>
            public float PerWork;
            public float Remaining;
            /// <summary>This part is the task's escalating charge (plan 030b), not its ordinary cost.</summary>
            public bool IsCharge;
        }

        /// <summary>Vitality this run's trips have charged her so far, their escalating charge only (for the Summary).</summary>
        public float MoveVitalityPaid { get; set; }

        /// <summary>
        /// Vitality each other task's escalating charge has taken this run (the Summary's line for it; trips have
        /// <see cref="MoveVitalityPaid"/>). Only tasks that have paid a charge appear.
        /// </summary>
        public Dictionary<TaskDefinition, float> TaskChargePaid { get; } = new Dictionary<TaskDefinition, float>();

        /// <summary>Every point of vitality she has lost this run, from any source (restoring doesn't take it back). Feeds Endurance's bank and XP.</summary>
        public float VitalityLostThisRun { get; set; }

        /// <summary>What the running task still has to spend, per source. Empty if it's free.</summary>
        public List<RunningCost> CurrentTaskCosts { get; } = new List<RunningCost>();

        /// <summary>Per-run tools and stats (resources that last this run only), e.g. Tool A, Focus.</summary>
        public Dictionary<ResourceDefinition, int> ToolsAndStats { get; } = new Dictionary<ResourceDefinition, int>();

        /// <summary>XP earned this run, per attribute. Levels come from this, so they reset each loop.</summary>
        public Dictionary<ClaraAttribute, float> AttributeXp { get; } = new Dictionary<ClaraAttribute, float>();

        public float XpOf(ClaraAttribute attribute) =>
            AttributeXp.TryGetValue(attribute, out float xp) ? xp : 0f;

        /// <summary>XP earned this run, per skill. Skill levels come from this, so they reset each loop.</summary>
        public Dictionary<SkillDefinition, float> SkillXp { get; } = new Dictionary<SkillDefinition, float>();

        public float XpOf(SkillDefinition skill) =>
            skill != null && SkillXp.TryGetValue(skill, out float xp) ? xp : 0f;

        /// <summary>
        /// Ticks spent this run on actions of each skill, for the run report's time strip. The run
        /// clock only runs during an action, so these add up to TicksElapsed.
        /// </summary>
        public Dictionary<SkillDefinition, long> SkillTicks { get; } = new Dictionary<SkillDefinition, long>();
        /// <summary>Ticks spent on actions with no skill.</summary>
        public long OtherTicks { get; set; }

        /// <summary>Tasks completed at least once this run (for "do these in one run" switches).</summary>
        public HashSet<TaskDefinition> CompletedTasks { get; } = new HashSet<TaskDefinition>();

        // ---------- For the run report ----------

        /// <summary>Every action finished this run, in order, repeats included, each with its room and whether it was a trip or an auto-supply (for the run report and for what she carries into the next run).</summary>
        public List<CompletedStep> Steps { get; } = new List<CompletedStep>();
        /// <summary>Every task finished this run, in order, repeats included: a view of <see cref="Steps"/>, made fresh on each read.</summary>
        public List<TaskDefinition> CompletionLog => Steps.ConvertAll(step => step.Task);
        public List<SwitchDefinition> SwitchesFlipped { get; } = new List<SwitchDefinition>();
        /// <summary>Rooms entered for the first time ever this run (their story is new), as SwitchesFlipped is for switches.</summary>
        public List<NodeDefinition> RoomsFirstEntered { get; } = new List<NodeDefinition>();

        /// <summary>What lies on each room's floor this run (the hall shifts: gone next run).</summary>
        public Dictionary<NodeDefinition, Dictionary<ResourceDefinition, int>> Floor { get; } =
            new Dictionary<NodeDefinition, Dictionary<ResourceDefinition, int>>();

        /// <summary>Each room's search progress (steps) as it stood when the run began, to show what the run added.</summary>
        public Dictionary<NodeDefinition, float> ExploredAtStart { get; } = new Dictionary<NodeDefinition, float>();

        /// <summary>Kept progress as it stood when the run began, to show what the run added.</summary>
        public Dictionary<ClaraAttribute, float> AttributeMasteryAtStart { get; } = new Dictionary<ClaraAttribute, float>();
        public Dictionary<ResourceDefinition, int> KeptAtStart { get; } = new Dictionary<ResourceDefinition, int>();
        public Dictionary<SkillDefinition, float> SkillMasteryAtStart { get; } = new Dictionary<SkillDefinition, float>();

        /// <param name="queue">Actions queued for this run before it began, or null for none.</param>
        public LoopState(Pool vitality, ActionQueue queue = null)
        {
            Vitality = vitality;
            Queue = queue ?? new ActionQueue();
        }

        public int CountOf(ResourceDefinition toolOrStat) =>
            toolOrStat != null && ToolsAndStats.TryGetValue(toolOrStat, out int count) ? count : 0;

        public Pool FindPool(Hue hue) => Pools.Find(pool => pool.Hue == hue);
    }
}
