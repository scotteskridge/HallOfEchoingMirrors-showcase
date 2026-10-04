using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>One skill on the Summary page: its icon, name, speed before › after the run, and a mastery progress bar.</summary>
    public class SkillChipView : MonoBehaviour
    {
        [SerializeField] private TraitIcon _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _multiplier;
        [SerializeField] private Slider _bar;

        /// <param name="tip">The pop-up over the whole chip.</param>
        public void Show(Sprite icon, string name, string multiplier, float progress, System.Func<string> tip)
        {
            _icon.Show(icon, null);
            UiText.Set(_name, name);
            UiText.Set(_multiplier, multiplier);
            _bar.value = Mathf.Clamp01(progress);
            ToolTip.On(this, tip);
        }
    }
}
