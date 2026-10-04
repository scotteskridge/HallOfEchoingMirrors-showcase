using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Draws a <see cref="TipLayout"/>: the kicker and title, one row each (cloned from a template),
    /// the description and the footer strip. The panel (ToolTipPanel) owns where it sits and how big
    /// it is; this only fills it in. Built by *Hall of Echoing Mirrors → Setup → Rebuild Tooltip Panel*.
    /// </summary>
    public class TipLayoutView : MonoBehaviour
    {
        [SerializeField] private UiColours _colours;
        [SerializeField] private GameObject _kickerLine;
        [SerializeField] private TMP_Text _kicker;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _titleValue;
        [SerializeField] private RectTransform _rows;
        [Tooltip("A row, kept switched off: a clone is made for each fact.")]
        [SerializeField] private TipRowView _rowTemplate;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private GameObject _footer;
        [SerializeField] private TMP_Text _footerText;
        [SerializeField] private TMP_Text _footerCount;
        [SerializeField] private GameObject _bar;
        [Tooltip("The bar's fill: stretched from the left to the share done.")]
        [SerializeField] private RectTransform _barFill;

        private readonly List<TipRowView> _rowViews = new List<TipRowView>();

        public void Show(TipLayout layout)
        {
            bool kicker = !layout.Compact && !string.IsNullOrEmpty(layout.Kicker);
            _kickerLine.SetActive(kicker);
            if (kicker)
                _kicker.text = layout.Kicker;
            _title.text = layout.Title;
            _titleValue.gameObject.SetActive(!string.IsNullOrEmpty(layout.TitleValue));
            if (!string.IsNullOrEmpty(layout.TitleValue))
            {
                _titleValue.text = layout.TitleValue;
                _titleValue.color = _colours.For(ColourRole.TipTime);
            }

            for (int i = 0; i < layout.Rows.Count; i++)
            {
                if (i == _rowViews.Count)
                {
                    var view = Instantiate(_rowTemplate, _rows);
                    view.name = "Row";
                    _rowViews.Add(view);
                }
                _rowViews[i].gameObject.SetActive(true);
                _rowViews[i].transform.SetSiblingIndex(i);
                _rowViews[i].Show(layout.Rows[i], _colours);
            }
            for (int i = layout.Rows.Count; i < _rowViews.Count; i++)
                _rowViews[i].gameObject.SetActive(false);

            bool described = !layout.Compact && !string.IsNullOrEmpty(layout.Description);
            _description.gameObject.SetActive(described);
            if (described)
                _description.text = layout.Description;
            _description.transform.SetAsLastSibling();

            ShowFooter(layout.Compact ? null : layout.Strip);
        }

        private void ShowFooter(TipLayout.Footer strip)
        {
            _footer.SetActive(strip != null);
            if (strip == null)
                return;
            _footerText.text = strip.Text;
            _footerCount.gameObject.SetActive(!string.IsNullOrEmpty(strip.Count));
            _footerCount.text = strip.Count;
            _bar.SetActive(strip.Progress.HasValue);
            if (strip.Progress.HasValue)
                _barFill.anchorMax = new Vector2(Mathf.Clamp01(strip.Progress.Value), 1f);
        }
    }
}
