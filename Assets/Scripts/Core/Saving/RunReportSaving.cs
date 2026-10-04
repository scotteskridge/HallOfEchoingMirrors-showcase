using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Turns the last run's report into <see cref="SavedRunReport"/> and back, so the Summary survives a load.
    /// Split from SaveSerializer, which is long already; it shares that class's lookup helpers.
    /// </summary>
    internal static class RunReportSaving
    {
        private const float NoTime = -1f;

        private static float Saved(float? seconds) => seconds ?? NoTime;
        private static float? Loaded(float seconds) => seconds < 0f ? (float?)null : seconds;

        public static SavedRunReport Capture(RunReport report)
        {
            var saved = new SavedRunReport
            {
                loopNumber = report.LoopNumber,
                ticks = report.Ticks,
                endReason = (int)report.EndReason,
                actionsCompleted = report.ActionsCompleted,
                unfinished = SaveSerializer.IdOf(report.Unfinished),
                unfinishedItem = report.Unfinished != null ? SaveSerializer.ItemIdOf(report.Unfinished) : "",
                moves = report.Moves,
                moveVitality = report.MoveVitality,
                keptVitalityGained = report.KeptVitalityGained,
                runBeforeLoop = report.RunBefore?.LoopNumber ?? -1,
                longestLoop = report.Longest?.LoopNumber ?? -1,
            };
            foreach (var (task, goes, vitality) in report.Charges)
                saved.charges.Add(new SavedCharge { task = task.Id, goes = goes, vitality = vitality });
            foreach (var (task, count) in report.Tasks)
                saved.tasks.Add(new SavedReportTask { task = task.Id, item = SaveSerializer.ItemIdOf(task), count = count });
            foreach (var gain in report.Attributes)
                saved.attributes.Add(new SavedGain
                {
                    attribute = (int)gain.Attribute, level = gain.Level,
                    masteryBefore = gain.MasteryBefore, masteryAfter = gain.MasteryAfter,
                    progressBefore = gain.MasteryProgressBefore, progressAfter = gain.MasteryProgressAfter,
                });
            foreach (var gain in report.Skills)
                saved.skills.Add(new SavedGain
                {
                    skill = gain.Skill.Id, level = gain.Level,
                    masteryBefore = gain.MasteryBefore, masteryAfter = gain.MasteryAfter,
                    progressBefore = gain.MasteryProgressBefore, progressAfter = gain.MasteryProgressAfter,
                });
            foreach (var slice in report.TimeBySkill)
                saved.timeBySkill.Add(new SavedTimeSlice { skill = SaveSerializer.IdOf(slice.Skill), ticks = slice.Ticks, share = slice.Share });
            foreach (var (resource, before, after) in report.Kept)
                saved.kept.Add(new SavedKept { id = resource.Id, before = before, after = after });
            foreach (var (resource, amount) in report.ToolsAndStats)
                saved.toolsAndStats.Add(new SavedAmount { id = resource.Id, amount = amount });
            foreach (var flipped in report.SwitchesFlipped)
                saved.switchesFlipped.Add(flipped.Id);
            foreach (var (item, amount) in report.CarriedKept)
                saved.carriedKept.Add(new SavedAmount { id = item.Id, amount = amount });
            foreach (var (item, amount) in report.CarriedLost)
                saved.carriedLost.Add(new SavedAmount { id = item.Id, amount = amount });
            foreach (var (room, searched) in report.Explored)
                saved.explored.Add(new SavedValue { id = room.Id, value = searched });
            foreach (var room in report.KnownByHeartNow)
                saved.knownByHeartNow.Add(room.Id);
            foreach (var row in report.Milestones)
                saved.milestones.Add(new SavedMilestoneRow
                {
                    id = row.Milestone.Id, isNew = row.IsNew,
                    thisRun = Saved(row.ThisRun), lastRun = Saved(row.LastRun), previousRun = Saved(row.PreviousRun),
                });
            return saved;
        }

        /// <summary>Rebuilds a saved report. Anything that no longer exists is skipped with a warning, as everywhere in loading.</summary>
        public static RunReport Restore(SavedRunReport saved, Simulation sim, ContentIndex index, int ticksPerSecond, List<string> warnings)
        {
            // An unknown reason (a damaged file) counts as a collapse, as with the run history.
            var reason = Enum.IsDefined(typeof(LoopEndReason), saved.endReason) ? (LoopEndReason)saved.endReason : LoopEndReason.Exhausted;
            var unfinished = string.IsNullOrEmpty(saved.unfinished) ? null : SaveSerializer.TaskFor(sim, index, saved.unfinished, saved.unfinishedItem, "unfinished task", warnings);
            var report = new RunReport(saved.loopNumber, Math.Max(0L, saved.ticks), ticksPerSecond, reason,
                Math.Max(0, saved.actionsCompleted), unfinished)
            {
                Moves = Math.Max(0, saved.moves),
                MoveVitality = Math.Max(0f, saved.moveVitality),
                KeptVitalityGained = Math.Max(0f, saved.keptVitalityGained),
                RunBefore = HistoryRecord(sim, saved.runBeforeLoop),
                Longest = HistoryRecord(sim, saved.longestLoop),
            };

            foreach (var entry in saved.tasks)
                if (SaveSerializer.TaskFor(sim, index, entry.task, entry.item, "task in the report", warnings) is TaskDefinition task)
                    report.Tasks.Add((task, entry.count));
            foreach (var charge in saved.charges)
                if (SaveSerializer.TaskFor(sim, index, charge.task, "", "task in the report", warnings) is TaskDefinition charged)
                    report.Charges.Add((charged, Math.Max(0, charge.goes), Math.Max(0f, charge.vitality)));
            foreach (var gain in saved.attributes)
                if (Enum.IsDefined(typeof(ClaraAttribute), gain.attribute))
                    report.Attributes.Add(new RunReport.AttributeGain
                    {
                        Attribute = (ClaraAttribute)gain.attribute, Level = gain.level,
                        MasteryBefore = gain.masteryBefore, MasteryAfter = gain.masteryAfter,
                        MasteryProgressBefore = gain.progressBefore, MasteryProgressAfter = gain.progressAfter,
                    });
                else
                    SaveSerializer.SkippedValue(warnings, "attribute in the report", gain.attribute);
            foreach (var gain in saved.skills)
                if (SaveSerializer.Find<SkillDefinition>(index, gain.skill, "skill in the report", warnings) is SkillDefinition skill)
                    report.Skills.Add(new RunReport.SkillGain
                    {
                        Skill = skill, Level = gain.level,
                        MasteryBefore = gain.masteryBefore, MasteryAfter = gain.masteryAfter,
                        MasteryProgressBefore = gain.progressBefore, MasteryProgressAfter = gain.progressAfter,
                    });
            foreach (var slice in saved.timeBySkill)
            {
                SkillDefinition skill = null;
                if (!string.IsNullOrEmpty(slice.skill) &&
                    (skill = SaveSerializer.Find<SkillDefinition>(index, slice.skill, "skill in the report", warnings)) == null)
                    continue; // a gone skill's slice is dropped; the strip fills its width from the shares left
                long ticks = Math.Max(0L, slice.ticks);
                report.TimeBySkill.Add(new RunReport.TimeSlice { Skill = skill, Ticks = ticks, Seconds = (float)ticks / ticksPerSecond, Share = slice.share });
            }
            foreach (var entry in saved.kept)
                if (SaveSerializer.Find<ResourceDefinition>(index, entry.id, "item in the report", warnings) is ResourceDefinition resource)
                    report.Kept.Add((resource, entry.before, entry.after));
            foreach (var entry in saved.toolsAndStats)
                if (SaveSerializer.Find<ResourceDefinition>(index, entry.id, "item in the report", warnings) is ResourceDefinition resource)
                    report.ToolsAndStats.Add((resource, entry.amount));
            foreach (string id in saved.switchesFlipped)
                if (SaveSerializer.Find<SwitchDefinition>(index, id, "switch in the report", warnings) is SwitchDefinition flipped)
                    report.SwitchesFlipped.Add(flipped);
            foreach (var entry in saved.carriedKept)
                if (SaveSerializer.Find<ResourceDefinition>(index, entry.id, "item in the report", warnings) is ResourceDefinition item)
                    report.CarriedKept.Add((item, entry.amount));
            foreach (var entry in saved.carriedLost)
                if (SaveSerializer.Find<ResourceDefinition>(index, entry.id, "item in the report", warnings) is ResourceDefinition item)
                    report.CarriedLost.Add((item, entry.amount));
            foreach (var entry in saved.explored)
                if (SaveSerializer.Find<NodeDefinition>(index, entry.id, "room in the report", warnings) is NodeDefinition room)
                    report.Explored.Add((room, entry.value));
            foreach (string id in saved.knownByHeartNow)
                if (SaveSerializer.Find<NodeDefinition>(index, id, "room in the report", warnings) is NodeDefinition room)
                    report.KnownByHeartNow.Add(room);
            foreach (var row in saved.milestones)
                if (SaveSerializer.FindMilestone(index, row.id, warnings) is ContentAsset milestone)
                    report.Milestones.Add(new RunReport.MilestoneRow
                    {
                        Milestone = milestone, IsNew = row.isNew,
                        ThisRun = Loaded(row.thisRun), LastRun = Loaded(row.lastRun), PreviousRun = Loaded(row.previousRun),
                    });
            return report;
        }

        // The records a report compares with are in the run history, which loads with the permanent state.
        private static RunRecord HistoryRecord(Simulation sim, int loopNumber) =>
            loopNumber < 0 ? null : sim.Persistent.RunHistory.Find(record => record.LoopNumber == loopNumber);
    }
}
