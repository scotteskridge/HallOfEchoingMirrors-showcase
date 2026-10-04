using System;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>One stat in the Summary page's quiet stats row: its icon, name, level and how its mastery went.</summary>
    public class StatChipView : MonoBehaviour
    {
        [SerializeField] private TraitIcon _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _mastery;

        /// <param name="tip">The pop-up over the whole chip.</param>
        public void Show(Sprite icon, string name, string mastery, Func<string> tip)
        {
            _icon.Show(icon, null);
            UiText.Set(_name, name);
            UiText.Set(_mastery, mastery);
            ToolTip.On(this, tip);
        }
    }
}
