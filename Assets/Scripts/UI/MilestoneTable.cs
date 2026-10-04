using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page's milestones table: one row per milestone she has ever reached, with how this
    /// run's time compares with the last one and the one before. The header row is static labels.
    /// </summary>
    public class MilestoneTable : MonoBehaviour
    {
        [SerializeField] private MilestoneRowView _rowTemplate;

        private TemplateList<MilestoneRowView> _rows;

        public void Show(RunReport report)
        {
            _rows ??= new TemplateList<MilestoneRowView>(_rowTemplate);
            _rows.Show(report.Milestones.Count);
            for (int i = 0; i < report.Milestones.Count; i++)
            {
                var row = report.Milestones[i];
                var milestone = row.Milestone;
                bool reached = row.ThisRun.HasValue;
                _rows[i].Show(Simulation.NameOf(milestone), Improvement(row),
                    reached ? UiText.Clock(row.ThisRun.Value) : Muted(GameText.Get("results.not_reached")),
                    Time(row.LastRun), Time(row.PreviousRun), reached,
                    () => Simulation.StoryOf(milestone)?.Title ?? Simulation.NameOf(milestone)); // a room may have no story
            }
        }

        private static string Muted(string text) => UiStyle.Colour(text, UiStyle.Muted);

        // A time, or a quiet dash where the milestone was not reached (or there is no such run).
        private static string Time(float? seconds) =>
            seconds.HasValue ? UiText.Clock(seconds.Value) : Muted(GameText.Get("common.none"));

        // Faster than last run is good news (negative), slower is a warning. Nothing to compare: a dash, or "new".
        private static string Improvement(RunReport.MilestoneRow row)
        {
            if (row.IsNew)
                return UiStyle.Colour(GameText.Get("results.new_milestone"), UiStyle.Milestone);
            if (!row.Change.HasValue)
                return Muted(GameText.Get("common.none"));
            int change = UiText.ClockDifference(row.ThisRun.Value, row.LastRun.Value);
            if (change == 0)
                return GameText.Get("common.same");
            return UiStyle.Colour(UiText.ClockChange(change), change < 0 ? UiStyle.Milestone : UiStyle.Warning);
        }
    }
}
