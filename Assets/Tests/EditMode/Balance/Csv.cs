using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>A comma-separated table for the balance reports: opens in Excel or Google Sheets.</summary>
    public class Csv
    {
        private readonly StringBuilder _text = new StringBuilder();

        public Csv(params string[] header) => Row(header);

        /// <summary>Adds a row; numbers are written with a dot, whatever the PC's language.</summary>
        public Csv Row(params object[] cells)
        {
            var parts = new List<string>();
            foreach (var cell in cells)
                parts.Add(Quote(cell is float f ? f.ToString("0.###", CultureInfo.InvariantCulture)
                    : System.Convert.ToString(cell, CultureInfo.InvariantCulture) ?? ""));
            _text.AppendLine(string.Join(",", parts));
            return this;
        }

        public void Save(string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, _text.ToString(), new UTF8Encoding(true)); // the BOM makes Excel read £ and é right
        }

        private static string Quote(string cell) =>
            cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + cell.Replace("\"", "\"\"") + "\"" : cell;
    }
}
