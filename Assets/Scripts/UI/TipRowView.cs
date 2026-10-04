using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One fact row of a tooltip: a label column, the value (big for times and vitality, small print
    /// for its unit and base), chips and a rule under it, and a note on the right. Built by
    /// *Hall of Echoing Mirrors → Setup → Rebuild Tooltip Panel*; the panel clones it for each row.
    /// </summary>
    public class TipRowView : MonoBehaviour
    {
        // The value is this much larger than the small print beside it (22 px against 11 px at the Body size).
        private const float BigShare = 1.3f;
        private const float SmallShare = 0.65f;
        // The right-hand note wraps rather than push the value out.
        private const float MaxRightWidth = 130f;

        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private TMP_Text _rule;
        [SerializeField] private TMP_Text _right;
        [SerializeField] private LayoutElement _rightSize;
        [SerializeField] private RectTransform _chips;
        [Tooltip("A chip (an instance of the faint Chip kind with a Label inside), kept switched off: the row clones it for each chip.")]
        [SerializeField] private GameObject _chipTemplate;

        private readonly List<TMP_Text> _chipLabels = new List<TMP_Text>();
        private readonly List<GameObject> _chipObjects = new List<GameObject>();

        public void Show(TipLayout.Row row, UiColours colours)
        {
            // No label at all (null) gives the whole row to the value; an empty one keeps the column, so a second line lines up.
            _label.gameObject.SetActive(row.Label != null);
            _label.text = row.Label ?? "";
            ShowValue(row, colours);
            ShowRight(row, colours);
            ShowOptional(_rule, row.Rule);
            ShowChips(row.Chips, colours);
        }

        private void ShowValue(TipLayout.Row row, UiColours colours)
        {
            if (string.IsNullOrEmpty(row.Value))
            {
                _value.gameObject.SetActive(false);
                return;
            }
            string quiet = colours.HexOf(ColourRole.TextSecondary);
            string value = TipMeanings.CueOf(row.Meaning) + row.Value;
            if (TipMeanings.IsBig(row.Meaning))
                value = UiStyle.Sized(value, BigShare);
            var line = new System.Text.StringBuilder(UiStyle.Colour(value, colours.HexOf(TipMeanings.RoleOf(row.Meaning))));
            if (!string.IsNullOrEmpty(row.Unit))
                line.Append(' ').Append(UiStyle.Sized(UiStyle.Colour(row.Unit, quiet), SmallShare));
            if (!string.IsNullOrEmpty(row.Base))
                line.Append("  ").Append(UiStyle.Sized(UiStyle.Colour(row.Base, quiet), SmallShare));
            _value.gameObject.SetActive(true);
            _value.text = line.ToString();
        }

        private void ShowRight(TipLayout.Row row, UiColours colours)
        {
            var parts = new List<string>();
            if (row.HasSince)
                parts.Add(UiStyle.Colour(row.Since, colours.HexOf(ColourRole.TipMet)));
            if (!string.IsNullOrEmpty(row.Note))
                parts.Add(row.Note);
            _right.gameObject.SetActive(parts.Count > 0);
            if (parts.Count == 0)
                return;
            string text = string.Join("\n", parts);
            _right.text = text;
            // Unconstrained: how wide it would be on one line (the cap above then makes it wrap).
            _rightSize.preferredWidth = Mathf.Min(_right.GetPreferredValues(text).x, MaxRightWidth);
        }

        private void ShowChips(List<string> chips, UiColours colours)
        {
            _chips.gameObject.SetActive(chips.Count > 0);
            string dot = UiStyle.Colour("●", colours.HexOf(ColourRole.Accent));
            for (int i = 0; i < chips.Count; i++)
            {
                if (i == _chipObjects.Count)
                {
                    var chip = Instantiate(_chipTemplate, _chips);
                    _chipObjects.Add(chip);
                    _chipLabels.Add(chip.GetComponentInChildren<TMP_Text>(true));
                }
                _chipObjects[i].SetActive(true);
                _chipLabels[i].text = dot + " " + chips[i];
            }
            for (int i = chips.Count; i < _chipObjects.Count; i++)
                _chipObjects[i].SetActive(false);
        }

        private static void ShowOptional(TMP_Text label, string text)
        {
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text))
                label.text = text;
        }
    }
}
