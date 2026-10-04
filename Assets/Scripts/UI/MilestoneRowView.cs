using System;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>One line of the Summary page's milestones table: the milestone and its four columns.</summary>
    public class MilestoneRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _improvement;
        [SerializeField] private TMP_Text _thisRun;
        [SerializeField] private TMP_Text _lastRun;
        [SerializeField] private TMP_Text _previousRun;
        [Tooltip("Fades the whole row when the milestone was not reached this run.")]
        [SerializeField] private CanvasGroup _fade;
        [Tooltip("How faded a row is when the milestone was not reached this run.")]
        [SerializeField, Range(0f, 1f)] private float _notReachedAlpha = 0.5f;

        /// <param name="tip">The pop-up over the row.</param>
        public void Show(string name, string improvement, string thisRun, string lastRun, string previousRun,
            bool reached, Func<string> tip)
        {
            UiText.Set(_name, name);
            UiText.Set(_improvement, improvement);
            UiText.Set(_thisRun, thisRun);
            UiText.Set(_lastRun, lastRun);
            UiText.Set(_previousRun, previousRun);
            _fade.alpha = reached ? 1f : _notReachedAlpha;
            ToolTip.On(this, tip);
        }
    }
}
