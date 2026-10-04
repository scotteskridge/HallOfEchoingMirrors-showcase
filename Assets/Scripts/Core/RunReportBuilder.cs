using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Builds the end-of-run <see cref="RunReport"/> from the run that just ended. It only reads:
    /// the run (<see cref="Simulation.Loop"/>), what is kept (<see cref="Simulation.Persistent"/>) and
    /// a few helpers on the Simulation. The Simulation's EndLoop calls <see cref="Build"/> once,
    /// after the run's switches are checked and before the run goes into the history.
    /// </summary>
    internal class RunReportBuilder
    {
        private readonly Simulation _sim;
        private readonly int _ticksPerSecond;
        private readonly GameContent _content;

        // Read through the Simulation each time, not kept: each run gets a new LoopState.
        private LoopState Loop => _sim.Loop;
        private PersistentState Persistent => _sim.Persistent;

        public RunReportBuilder(Simulation sim, int ticksPerSecond, GameContent content)
        {
            _sim = sim ?? throw new ArgumentNullException(nameof(sim));
            _ticksPerSecond = ticksPerSecond;
            _content = content;
        }

        public RunReport Build()
        {
            var report = new RunReport(Persistent.LoopNumber, Loop.TicksElapsed, _ticksPerSecond,
                Loop.EndReason, Loop.Steps.Count, Loop.CurrentTask);

            report.Moves = _sim.MovesThisRun;
            report.MoveVitality = Loop.MoveVitalityPaid;

            // A task's goes are the ones she finished; its vitality may include a go cut short by the run's end.
            foreach (var step in Loop.Steps)
                if (step.Destination == null && Loop.TaskChargePaid.ContainsKey(step.Task))
                {
                    int index = report.Charges.FindIndex(c => c.task == step.Task);
                    if (index < 0)
                        report.Charges.Add((step.Task, 1, Loop.TaskChargePaid[step.Task]));
                    else
                        report.Charges[index] = (step.Task, report.Charges[index].goes + 1, report.Charges[index].vitality);
                }
            // A go cut short by the run's end has paid part of its charge with no finished go to list it by.
            foreach (var paid in Loop.TaskChargePaid)
                if (!report.Charges.Exists(c => c.task == paid.Key))
                    report.Charges.Add((paid.Key, 0, paid.Value));

            var log = Loop.CompletionLog;
            var counts = new Dictionary<TaskDefinition, int>();
            foreach (var task in log)
            {
                if (counts.ContainsKey(task))
                    counts[task]++;
                else
                    counts[task] = 1;
            }
            // CompletionLog keeps the order each task was first finished in.
            var listed = new HashSet<TaskDefinition>();
            foreach (var task in log)
                if (listed.Add(task))
                    report.Tasks.Add((task, counts[task]));

            foreach (var entry in Loop.AttributeXp)
            {
                var attribute = entry.Key;
                float xpBefore = Loop.AttributeMasteryAtStart.TryGetValue(attribute, out float xp) ? xp : 0f;
                report.Attributes.Add(new RunReport.AttributeGain
                {
                    Attribute = attribute,
                    Level = _sim.LevelOf(attribute),
                    MasteryBefore = _sim.MasteryFor(xpBefore),
                    MasteryAfter = _sim.MasteryOf(attribute),
                    MasteryProgressBefore = _sim.MasteryProgressFor(xpBefore),
                    MasteryProgressAfter = _sim.MasteryProgressOf(attribute),
                });
            }
            report.Attributes.Sort((a, b) => a.Attribute.CompareTo(b.Attribute));

            foreach (var entry in Loop.SkillXp)
            {
                var skill = entry.Key;
                float xpBefore = Loop.SkillMasteryAtStart.TryGetValue(skill, out float xp) ? xp : 0f;
                report.Skills.Add(new RunReport.SkillGain
                {
                    Skill = skill,
                    Level = _sim.LevelOf(skill),
                    MasteryBefore = _sim.MasteryFor(xpBefore),
                    MasteryAfter = _sim.MasteryOf(skill),
                    MasteryProgressBefore = _sim.MasteryProgressFor(xpBefore),
                    MasteryProgressAfter = _sim.MasteryProgressOf(skill),
                });
            }
            if (_content != null)
                report.Skills.Sort((a, b) => _content.skills.IndexOf(a.Skill).CompareTo(_content.skills.IndexOf(b.Skill)));

            AddTimeSlices(report);

            foreach (var entry in Persistent.Resources)
            {
                int before = Loop.KeptAtStart.TryGetValue(entry.Key, out int held) ? held : 0;
                if (entry.Value > before)
                    report.Kept.Add((entry.Key, before, entry.Value));
            }

            foreach (var entry in Loop.ToolsAndStats)
                if (entry.Value > 0 && !entry.Key.IsCarried)
                    report.ToolsAndStats.Add((entry.Key, entry.Value));

            report.SwitchesFlipped.AddRange(Loop.SwitchesFlipped);
            report.CarriedKept.AddRange(Loop.CarriedKept);
            report.CarriedLost.AddRange(Loop.CarriedLost);

            // Rooms whose bar rose this run, as full as they now are.
            foreach (var entry in Persistent.Explored)
            {
                float before = Loop.ExploredAtStart.TryGetValue(entry.Key, out float start) ? start : 0f;
                if (entry.Value > before && entry.Key.exploresToFill > 0)
                    report.Explored.Add((entry.Key, _sim.ExploredFraction(entry.Key)));
            }

            // Compared with earlier runs only: this one is added to the history after the report is made.
            var history = Persistent.RunHistory;
            if (history.Count > 0)
                report.RunBefore = history[history.Count - 1];
            report.Longest = _sim.LongestRun;
            AddMilestoneRows(report, history);
            return report;
        }

        private void AddMilestoneRows(RunReport report, List<RunRecord> history)
        {
            RunRecord last = history.Count > 0 ? history[history.Count - 1] : null;
            RunRecord previous = history.Count > 1 ? history[history.Count - 2] : null;

            // The order first ever reached: oldest run first, then this run's new ones.
            var everReached = new List<ContentAsset>();
            foreach (var record in history)
                foreach (var (milestone, _) in record.Milestones)
                    if (!everReached.Contains(milestone))
                        everReached.Add(milestone);
            var earlier = new HashSet<ContentAsset>(everReached);
            foreach (var (milestone, _) in Loop.Milestones)
                if (!everReached.Contains(milestone))
                    everReached.Add(milestone);

            // This run's, in the order reached (which is by time).
            foreach (var (milestone, seconds) in Loop.Milestones)
                report.Milestones.Add(RowFor(milestone, seconds, last, previous, isNew: !earlier.Contains(milestone)));
            foreach (var milestone in everReached)
                if (!Loop.HasReached(milestone) && Simulation.CanBeReachedAgain(milestone))
                    report.Milestones.Add(RowFor(milestone, null, last, previous, isNew: false));
        }

        private static RunReport.MilestoneRow RowFor(ContentAsset milestone, float? thisRun, RunRecord last, RunRecord previous, bool isNew) =>
            new RunReport.MilestoneRow
            {
                Milestone = milestone,
                ThisRun = thisRun,
                LastRun = last?.TimeOf(milestone),
                PreviousRun = previous?.TimeOf(milestone),
                IsNew = isNew,
            };

        // Shares are of the time tallied, not of Ticks: a run resumed from an older save has fewer
        // tallied ticks than it has elapsed, and the strip should still fill its width.
        private void AddTimeSlices(RunReport report)
        {
            long total = Loop.OtherTicks;
            foreach (var entry in Loop.SkillTicks)
                total += entry.Value;
            if (total <= 0)
                return;

            RunReport.TimeSlice Slice(SkillDefinition skill, long ticks) => new RunReport.TimeSlice
            {
                Skill = skill,
                Ticks = ticks,
                Seconds = (float)ticks / _ticksPerSecond,
                Share = (float)ticks / total,
            };
            foreach (var entry in Loop.SkillTicks)
                if (entry.Value > 0)
                    report.TimeBySkill.Add(Slice(entry.Key, entry.Value));
            // Longest first; a tie keeps Game Content order so the strip doesn't shuffle.
            int Order(SkillDefinition s) => _content != null ? _content.skills.IndexOf(s) : 0;
            report.TimeBySkill.Sort((a, b) =>
            {
                int byTime = b.Ticks.CompareTo(a.Ticks);
                return byTime != 0 ? byTime : Order(a.Skill).CompareTo(Order(b.Skill));
            });
            if (Loop.OtherTicks > 0)
                report.TimeBySkill.Add(Slice(null, Loop.OtherTicks));
        }
    }
}
