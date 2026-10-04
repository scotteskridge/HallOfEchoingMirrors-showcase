using System.Collections.Generic;
using System.Text;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One tooltip described as plain data: a kicker, a title, fact rows and a footer. The panel turns
    /// it into rows and colours; the builder (ActionText.Layout) only says what each fact is.
    /// </summary>
    public class TipLayout
    {
        /// <summary>One fact: a label, a large value, a small base beside it, and notes on the right.</summary>
        public class Row
        {
            /// <summary>The label column's words; empty keeps the column (a second line of the same fact), null removes it.</summary>
            public string Label;
            /// <summary>The big number or words, e.g. "1.1s".</summary>
            public string Value;
            public TipMeaning Meaning;
            /// <summary>Small words after the value, e.g. "vitality".</summary>
            public string Unit;
            /// <summary>Small, e.g. "from 3.9s base": what it was before her skill.</summary>
            public string Base;
            /// <summary>"−0.3s since last run". Empty until ui-044 fills it, and then only when it isn't zero.</summary>
            public string Since;
            /// <summary>A small note on the right, e.g. "flat · skill cannot reduce it".</summary>
            public string Note;
            /// <summary>Chips under the value, e.g. "Wayfinding +12".</summary>
            public List<string> Chips = new List<string>();
            /// <summary>A rule in small print under the chips.</summary>
            public string Rule;

            public bool HasSince => !string.IsNullOrEmpty(Since);
        }

        /// <summary>The strip at the bottom: the state, not a description.</summary>
        public class Footer
        {
            public string Text;
            /// <summary>"0 / 4", or empty.</summary>
            public string Count;
            /// <summary>The bar's fill (0–1), or null for no bar.</summary>
            public float? Progress;
        }

        /// <summary>"SEARCH · THE MIRROR'S LABORATORY". Empty on the compact form.</summary>
        public string Kicker;
        public string Title;
        /// <summary>Right of the title (the skill form's "×3.54"); empty for none.</summary>
        public string TitleValue;
        /// <summary>The 260 px form: the title and one row.</summary>
        public bool Compact;
        public List<Row> Rows = new List<Row>();
        /// <summary>One muted line above the footer, or empty.</summary>
        public string Description;
        public Footer Strip;

        /// <summary>Everything the layout says, so the panel can tell when it has changed and only then rebuild.</summary>
        public string Key()
        {
            var key = new StringBuilder();
            key.Append(Kicker).Append('|').Append(Title).Append('|').Append(TitleValue).Append('|').Append(Compact).Append('|').Append(Description);
            foreach (var row in Rows)
            {
                key.Append('\n').Append(row.Label).Append('|').Append(row.Value).Append('|').Append(row.Meaning).Append('|').Append(row.Unit)
                    .Append('|').Append(row.Base).Append('|').Append(row.Since).Append('|').Append(row.Note).Append('|').Append(row.Rule);
                foreach (var chip in row.Chips)
                    key.Append('|').Append(chip);
            }
            if (Strip != null)
                key.Append('\n').Append(Strip.Text).Append('|').Append(Strip.Count).Append('|').Append(Strip.Progress);
            return key.ToString();
        }
    }
}
