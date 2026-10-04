using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page's two lists side by side: what changed for good this run, and what Clara
    /// carried out of the Hall (kept things with a gilt border, things left behind struck through).
    /// </summary>
    public class ChipColumns : MonoBehaviour
    {
        [SerializeField] private SummaryChipView _changedTemplate;
        [SerializeField] private TMP_Text _changedNone;
        [SerializeField] private SummaryChipView _carriedTemplate;
        [SerializeField] private TMP_Text _carriedNone;

        private TemplateList<SummaryChipView> _changed;
        private TemplateList<SummaryChipView> _carried;

        public void Show(RunReport report)
        {
            _changed ??= new TemplateList<SummaryChipView>(_changedTemplate);
            _carried ??= new TemplateList<SummaryChipView>(_carriedTemplate);

            // Every switch is permanent, so all the ones flipped this run changed for good.
            _changed.Show(report.SwitchesFlipped.Count);
            for (int i = 0; i < report.SwitchesFlipped.Count; i++)
            {
                var @switch = report.SwitchesFlipped[i];
                _changed[i].Show(@switch.displayName, true, () => GameText.Get("results.changed_for_good", ("switch", @switch.displayName)));
            }
            UiText.SetActive(_changedNone, report.SwitchesFlipped.Count == 0);

            ShowCarried(report);
        }

        // Only a run that ended by walking out shows what she carried; the other endings show none
        // (the user decided this, plan 017).
        private void ShowCarried(RunReport report)
        {
            bool walkedOut = report.EndReason == LoopEndReason.WalkedOut;
            int count = walkedOut ? report.Kept.Count + report.CarriedKept.Count + report.CarriedLost.Count : 0;
            _carried.Show(count);

            if (walkedOut)
            {
                int at = 0;
                foreach (var (resource, before, after) in report.Kept)
                {
                    string name = resource.DisplayName;
                    _carried[at++].Show(GameText.Get("results.chip_kept", ("item", name), ("before", before), ("after", after)), true,
                        () => GameText.Get("results.kept", ("item", name), ("before", before), ("after", after)));
                }
                foreach (var (item, amount) in report.CarriedKept)
                {
                    string text = Amount(item, amount);
                    _carried[at++].Show(text, true, () => GameText.Get("results.carried_out", ("item", text)));
                }
                // Placeholder rule: things left behind show (struck through) on a walked-out run's page
                // unless the user says otherwise (plan 017).
                foreach (var (item, amount) in report.CarriedLost)
                {
                    string text = Amount(item, amount);
                    _carried[at++].Show(UiStyle.Colour($"<s>{text}</s>", UiStyle.Muted), false,
                        () => GameText.Get("results.left_behind", ("item", text)));
                }
            }
            UiText.SetActive(_carriedNone, count == 0);
        }

        private static string Amount(ResourceDefinition item, int amount) =>
            amount > 1 ? GameText.Get("common.amount", ("amount", amount), ("item", item.DisplayName)) : item.DisplayName;
    }
}
