using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// A small table inside a hover pop-up (a stat's or skill's levels, XP and speed): column
    /// headings, then rows with a label at the start. Columns sit at fixed places (TextMeshPro's
    /// &lt;pos&gt;) and their text is fixed-width (&lt;mspace&gt;), padded on the left, so numbers line
    /// up on the right however many digits they have. Builds rich text; no Unity objects needed.
    /// </summary>
    public class TipTable
    {
        // In em (the pop-up's own text size), so the table grows and shrinks with its font.
        private const float LabelWidth = 5f;   // room for the row labels ("This run")
        private const float CharWidth = 0.62f; // every character in a column is this wide, so digits line up (Inter's digits: 0.60–0.64)
        private const float ColumnGap = 1f;

        private readonly string[] _columns;
        private readonly List<(string label, string[] cells, string after)> _rows =
            new List<(string, string[], string)>();

        /// <param name="columns">The column headings, left to right.</param>
        public TipTable(params string[] columns)
        {
            _columns = columns;
        }

        /// <summary>
        /// A row: its label, then a cell per column from the left (fewer is fine; an empty cell
        /// leaves its column blank). <paramref name="after"/> is free text starting at the next
        /// column, not fixed-width (e.g. a stat's "120 vitality" in its Total row).
        /// </summary>
        public void AddRow(string label, string[] cells, string after = null)
        {
            if (cells.Length > _columns.Length)
                throw new System.ArgumentException(
                    $"TipTable: a row has {cells.Length} cells but the table only has {_columns.Length} columns.", nameof(cells));
            _rows.Add((label, cells, after));
        }

        public override string ToString()
        {
            // Each column as wide as its widest text, heading included.
            var widths = new int[_columns.Length];
            for (int c = 0; c < _columns.Length; c++)
                widths[c] = _columns[c].Length;
            foreach (var row in _rows)
                for (int c = 0; c < row.cells.Length; c++)
                    if (row.cells[c] != null && row.cells[c].Length > widths[c])
                        widths[c] = row.cells[c].Length;

            // Where each column starts; one more for text after the last column.
            var starts = new float[_columns.Length + 1];
            starts[0] = LabelWidth;
            for (int c = 0; c < _columns.Length; c++)
                starts[c + 1] = starts[c] + widths[c] * CharWidth + ColumnGap;

            var text = new StringBuilder();
            for (int c = 0; c < _columns.Length; c++)
                AppendCell(text, starts[c], _columns[c], widths[c]);
            foreach (var (label, cells, after) in _rows)
            {
                text.Append('\n').Append(label);
                for (int c = 0; c < cells.Length; c++)
                    if (!string.IsNullOrEmpty(cells[c]))
                        AppendCell(text, starts[c], cells[c], widths[c]);
                if (!string.IsNullOrEmpty(after))
                    text.Append("<pos=").Append(Em(starts[cells.Length])).Append("em>").Append(after);
            }
            return text.ToString();
        }

        private static void AppendCell(StringBuilder text, float start, string cell, int width) =>
            text.Append("<pos=").Append(Em(start)).Append("em><mspace=").Append(Em(CharWidth)).Append("em>")
                .Append(cell.PadLeft(width)).Append("</mspace>");

        // Always a full stop for decimals: TextMeshPro can't read "5,5em" (a comma, in some languages).
        private static string Em(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
