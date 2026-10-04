using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page's strip showing where a run's time went: one slice per skill, widest first,
    /// each as wide as its share. Slices are made from a template and re-used for every report.
    /// </summary>
    public class RunTimeStrip : MonoBehaviour
    {
        [SerializeField] private TimeSliceView _sliceTemplate;
        [Tooltip("A slice narrower than this (pixels) shows no skill name.")]
        [SerializeField, Min(0f)] private float _nameMinWidth = 90f;
        [Tooltip("A slice narrower than this (pixels) shows no icon either.")]
        [SerializeField, Min(0f)] private float _iconMinWidth = 22f;
        [Tooltip("The gap between slices; must match the strip's Horizontal Layout Group.")]
        [SerializeField, Min(0f)] private float _spacing = 2f;

        private TemplateList<TimeSliceView> _slices;
        private RunReport _report;

        private RectTransform Rect => (RectTransform)transform;

        /// <summary>Shows a run's time split, one slice per skill (an empty split shows none).</summary>
        public void Show(RunReport report)
        {
            _report = report;
            _slices ??= new TemplateList<TimeSliceView>(_sliceTemplate);
            var split = report.TimeBySkill;
            _slices.Show(split.Count);

            for (int i = 0; i < split.Count; i++)
            {
                int index = i; // for the pop-up, which reads the report it was made for
                var slice = split[i];
                // Widest slice brightest, in shades of the gilt colour; the dark ones need light writing.
                float brightness = split.Count > 1 ? 1f - (float)i / (split.Count - 1) : 1f;
                var colour = Color.Lerp(UiStyle.Accent * 0.5f, UiStyle.Accent, brightness);
                colour.a = 1f;
                _slices[i].Show(slice.Skill != null ? slice.Skill.Icon : null, NameOf(slice), slice.Share, colour,
                    brightness >= 0.5f ? UiStyle.AccentText : UiStyle.TextMain, () => TipFor(index));
            }
            Fit();
        }

        // The strip may not have its final width the moment it is filled, so slices fit again whenever it changes.
        private void OnRectTransformDimensionsChange()
        {
            if (_slices != null && _report != null)
                Fit();
        }

        private void Fit()
        {
            var split = _report.TimeBySkill;
            float room = Mathf.Max(0f, Rect.rect.width - _spacing * Mathf.Max(0, split.Count - 1));
            for (int i = 0; i < split.Count; i++)
            {
                float width = room * split[i].Share;
                _slices[i].Fit(width >= _iconMinWidth, width >= _nameMinWidth,
                    split[i].Skill != null && split[i].Skill.Icon != null);
            }
        }

        private static string NameOf(RunReport.TimeSlice slice) =>
            slice.Skill != null ? slice.Skill.DisplayName : GameText.Get("results.other");

        private string TipFor(int index)
        {
            if (_report == null || index >= _report.TimeBySkill.Count)
                return null;
            var slice = _report.TimeBySkill[index];
            return GameText.Get("results.slice_tip", ("skill", NameOf(slice)),
                ("time", UiText.Clock(slice.Seconds)), ("percent", UiText.Percent(slice.Share)));
        }
    }
}
