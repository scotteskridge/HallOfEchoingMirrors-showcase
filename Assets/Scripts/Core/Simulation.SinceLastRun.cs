namespace HallOfEchoingMirrors.Core
{
    // "Faster than last run" (plan 042): how much an action's skill mastery has sped it up since the
    // last run began. Only mastery counts, and only as each run began, so the figure holds still all
    // run however she levels up. The XP is kept rather than a speed, so retuning the mastery speed in
    // the Balance Sheet never shows a change she didn't earn.
    public partial class Simulation
    {
        /// <summary>
        /// When a run ends (any ending, or cut short by a dev restart): what it began with becomes
        /// "last run's start". Replaced whole, so a skill it began without reads as 0.
        /// </summary>
        private void KeepThisRunsStart()
        {
            Persistent.SkillMasteryXpAtLastRunStart.Clear();
            foreach (var entry in Loop.SkillMasteryAtStart)
                Persistent.SkillMasteryXpAtLastRunStart[entry.Key] = entry.Value;
            Persistent.HasLastRunStart = true;
        }

        /// <summary>
        /// How many seconds faster one go of <paramref name="task"/> at <paramref name="at"/> is, from its
        /// skill's mastery, than as the last run began: its base time at last run's starting mastery minus
        /// at this run's (between runs: the next run's). Null until a run has ended; 0 for a task with no skill.
        /// Room speed, what she holds and this run's levels are left out.
        /// </summary>
        public float? SecondsFasterSinceLastRun(TaskDefinition task, NodeDefinition at)
        {
            if (task == null)
                throw new System.ArgumentNullException(nameof(task));
            float? before = Persistent.SkillMasteryXpAtLastRunStartOf(task.skill);
            if (before == null)
                return null;
            if (task.skill == null)
                return 0f;
            float seconds = BaseSecondsOf(task, at);
            return seconds / MasterySpeedAtXp(before.Value) - seconds / MasterySpeedAtXp(MasteryXpAtThisRunsStart(task.skill));
        }

        // Between runs the ended run's own start is last run's: what she holds now is what the next run starts with.
        private float MasteryXpAtThisRunsStart(SkillDefinition skill) =>
            Loop.IsOver
                ? Persistent.SkillMasteryXpOf(skill)
                : Loop.SkillMasteryAtStart.TryGetValue(skill, out float xp) ? xp : 0f;

        private float MasterySpeedAtXp(float masteryXp) => _settings.MasterySpeedAt(MasteryFor(masteryXp));
    }
}
