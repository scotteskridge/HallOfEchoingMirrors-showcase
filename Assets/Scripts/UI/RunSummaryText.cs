using System.Text;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Wording for parts of the Summary that need a rule, kept out of the panel so it can be tested
    /// without a scene.
    /// </summary>
    public static class RunSummaryText
    {
        /// <summary>
        /// One " · Practise the cut: 4, costing 9.4 vitality" entry for every task whose rising charge took
        /// vitality, in the order they were first finished; "" when none did. Trips are the Moves figure, not here.
        /// Placeholder rule: an entry whose vitality shows as 0 is skipped (as "+0 kept" is); the test is on
        /// vitality, not goes, because a go cut short by the run's end has 0 goes but did cost vitality.
        /// </summary>
        public static string ChargeSuffixes(RunReport run)
        {
            var text = new StringBuilder();
            foreach (var (task, goes, vitality) in run.Charges)
            {
                string cost = UiText.Number(vitality);
                if (cost == "0")
                    continue;
                text.Append(' ').Append(GameText.Get("results.charge_suffix",
                    ("task", task.displayName), ("goes", goes), ("vitality", cost)));
            }
            return text.ToString();
        }
    }
}
