using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// One finished run, kept for good in the run history: enough to compare runs (and, later, draw
    /// them as a sparkline). Never changes once made.
    /// </summary>
    public class RunRecord
    {
        public int LoopNumber { get; }
        /// <summary>How long the run lasted, in game time (ticks).</summary>
        public long Ticks { get; }
        /// <summary>Every task finished that run, counting repeats.</summary>
        public int ActionsCompleted { get; }
        public LoopEndReason EndReason { get; }

        /// <summary>
        /// The milestones she reached that run and when (run seconds), in the order reached. Empty for
        /// runs from saves older than version 12, which didn't keep them.
        /// </summary>
        public IReadOnlyList<(ContentAsset milestone, float seconds)> Milestones { get; }

        /// <summary>When this run reached the milestone, or null if it didn't.</summary>
        public float? TimeOf(ContentAsset milestone)
        {
            foreach (var (reached, seconds) in Milestones)
                if (reached == milestone)
                    return seconds;
            return null;
        }

        /// <summary>
        /// Whether the run can hold or beat the longest-run record. A run the player ended early
        /// is still in the history, but it wasn't a real attempt at lasting.
        /// </summary>
        public bool CountsForLongest => CountsForLongestWhen(EndReason);

        public static bool CountsForLongestWhen(LoopEndReason reason) => reason != LoopEndReason.EndedByPlayer;

        public RunRecord(int loopNumber, long ticks, int actionsCompleted, LoopEndReason endReason,
            IEnumerable<(ContentAsset milestone, float seconds)> milestones = null)
        {
            Milestones = milestones == null
                ? new List<(ContentAsset, float)>()
                : new List<(ContentAsset, float)>(milestones);
            LoopNumber = loopNumber;
            Ticks = ticks;
            ActionsCompleted = actionsCompleted;
            EndReason = endReason;
        }
    }
}
