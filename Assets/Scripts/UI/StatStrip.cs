using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page's quiet stats row: one chip per stat trained this run, with its icon, level
    /// reached and mastery. The hover text is the same as on the main screen's stats row.
    /// </summary>
    public class StatStrip : MonoBehaviour
    {
        [SerializeField] private StatChipView _chipTemplate;
        [SerializeField] private TraitIcons _traitIcons;

        private TemplateList<StatChipView> _chips;

        /// <param name="game">Asked for the Simulation each time the hover text is read: loading replaces it.</param>
        public void Show(RunReport report, GameController game)
        {
            _chips ??= new TemplateList<StatChipView>(_chipTemplate);
            _chips.Show(report.Attributes.Count);
            for (int i = 0; i < report.Attributes.Count; i++)
            {
                var gain = report.Attributes[i];
                string name = GameText.Get("results.stat_chip", ("stat", GameText.Attribute(gain.Attribute)),
                    ("level", UiStyle.Colour(GameText.Get("results.level", ("level", gain.Level)), UiStyle.Levelling)));
                _chips[i].Show(_traitIcons.IconOf(gain.Attribute), name, MasteryNote(gain),
                    () => ClaraTips.StatTip(game.Simulation, gain.Attribute));
            }
        }

        private static string MasteryNote(RunReport.AttributeGain gain)
        {
            if (gain.MasteryAfter > gain.MasteryBefore)
                return UiStyle.Colour(GameText.Get("results.mastery.gained", ("before", gain.MasteryBefore), ("after", gain.MasteryAfter)),
                    UiStyle.Milestone);
            if (gain.MasteryProgressAfter >= 1f)
                return UiStyle.Colour(GameText.Get("results.mastery.maxed", ("level", gain.MasteryAfter)), UiStyle.Muted);
            return GameText.Get("results.mastery.progress", ("level", gain.MasteryAfter));
        }
    }
}
