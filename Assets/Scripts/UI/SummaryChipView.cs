using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>One chip in the Summary page's "Changed for good" and "What she carried" lists.</summary>
    public class SummaryChipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [Tooltip("The gilt border, shown on things that were kept for good.")]
        [SerializeField] private Outline _border;

        /// <param name="kept">Kept for good: gets the gilt border. Otherwise the border is off.</param>
        /// <param name="tip">The pop-up over the chip; null for none.</param>
        public void Show(string label, bool kept, Func<string> tip)
        {
            UiText.Set(_label, label);
            _border.enabled = kept;
            if (tip != null)
                ToolTip.On(this, tip);
        }
    }
}
