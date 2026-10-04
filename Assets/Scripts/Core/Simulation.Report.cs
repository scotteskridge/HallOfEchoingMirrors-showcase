namespace HallOfEchoingMirrors.Core
{
    // The run report: a summary of each finished run, so the player can see what it achieved
    // and compare it with the run before. RunReportBuilder makes it; this keeps the result.
    public partial class Simulation
    {
        private readonly RunReportBuilder _reportBuilder;

        /// <summary>The run that last ended, or null if none has ended yet (it is saved with the game).</summary>
        public RunReport LastRun { get; private set; }

        /// <summary>Puts back the last run's report when loading a save, so the Summary still shows it.</summary>
        internal void RestoreLastRun(RunReport report) => LastRun = report;

        /// <summary>The longest finished run that counts for the record (not ended early), or null. The first wins a tie.</summary>
        public RunRecord LongestRun
        {
            get
            {
                RunRecord longest = null;
                foreach (var record in Persistent.RunHistory)
                    if (record.CountsForLongest && (longest == null || record.Ticks > longest.Ticks))
                        longest = record;
                return longest;
            }
        }

        /// <summary>
        /// The best (lowest) time any finished run reached this milestone, or null if none has. Every run
        /// counts, ended early or collapsed included: a time she reached is a real time.
        /// </summary>
        public float? BestTimeOf(ContentAsset milestone)
        {
            float? best = null;
            foreach (var record in Persistent.RunHistory)
            {
                float? time = record.TimeOf(milestone);
                if (time.HasValue && (best == null || time.Value < best.Value))
                    best = time;
            }
            return best;
        }
    }
}
