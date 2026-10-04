using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page's "what she learned" row: one chip per skill trained, in the time strip's
    /// order, showing the speed her mastery gives before › after the run.
    /// </summary>
    public class SkillGainRow : MonoBehaviour
    {
        [SerializeField] private SkillChipView _chipTemplate;

        private TemplateList<SkillChipView> _chips;

        /// <param name="settings">Turns mastery levels into ×speed (the one copy of the formula, in Core).</param>
        public void Show(RunReport report, LoopSettings settings)
        {
            _chips ??= new TemplateList<SkillChipView>(_chipTemplate);
            var gains = InTimeOrder(report);
            _chips.Show(gains.Count);

            for (int i = 0; i < gains.Count; i++)
            {
                var gain = gains[i];
                float before = settings.MasterySpeedAt(gain.MasteryBefore);
                float after = settings.MasterySpeedAt(gain.MasteryAfter);
                string multiplier = gain.MasteryAfter > gain.MasteryBefore
                    ? GameText.Get("results.skill_mult", ("before", UiText.Rate(before)), ("after", UiText.Rate(after)))
                    : GameText.Get("results.skill_unchanged", ("value", UiText.Rate(after)));
                _chips[i].Show(gain.Skill.Icon, gain.Skill.DisplayName, multiplier, gain.MasteryProgressAfter,
                    () => GameText.Get("results.skill_tip", ("skill", gain.Skill.DisplayName),
                        ("level", gain.Level), ("mastery", MasteryNote(gain))));
            }
        }

        // The strip's order (widest first), then any skill trained without a slice of its own.
        private static List<RunReport.SkillGain> InTimeOrder(RunReport report)
        {
            var ordered = new List<RunReport.SkillGain>();
            foreach (var slice in report.TimeBySkill)
            {
                var gain = report.Skills.Find(s => s.Skill == slice.Skill);
                if (slice.Skill != null && gain != null)
                    ordered.Add(gain);
            }
            foreach (var gain in report.Skills)
                if (!ordered.Contains(gain))
                    ordered.Add(gain);
            return ordered;
        }

        private static string MasteryNote(RunReport.SkillGain gain)
        {
            if (gain.MasteryAfter > gain.MasteryBefore)
                return GameText.Get("results.mastery.gained", ("before", gain.MasteryBefore), ("after", gain.MasteryAfter));
            if (gain.MasteryProgressAfter >= 1f)
                return GameText.Get("results.mastery.maxed", ("level", gain.MasteryAfter));
            return GameText.Get("results.mastery.progress", ("level", gain.MasteryAfter)) + " " +
                   GameText.Get("results.mastery.toward", ("percent", UiText.Percent(gain.MasteryProgressAfter - gain.MasteryProgressBefore)),
                       ("next", gain.MasteryAfter + 1));
        }
    }
}
