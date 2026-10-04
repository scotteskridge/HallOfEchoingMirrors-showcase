using System.Collections.Generic;
using System.Text.RegularExpressions;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The small tables in stat and skill pop-ups: columns that line up however long the numbers are.</summary>
    public class TipTableTests
    {
        // A cell as TipTable writes it: its place in the line, then its fixed-width text.
        private static readonly Regex Cell = new Regex("<pos=([0-9.]+)em><mspace=[^>]+>([^<]*)</mspace>");

        /// <summary>Each cell on a line: where it starts, and its (padded) text.</summary>
        private static List<(string pos, string text)> CellsOf(string line)
        {
            var cells = new List<(string, string)>();
            foreach (Match match in Cell.Matches(line))
                cells.Add((match.Groups[1].Value, match.Groups[2].Value));
            return cells;
        }

        [Test]
        public void Rows_LineUpOnTheRight()
        {
            var table = new TipTable("Level");
            table.AddRow("This run", new[] { "12" });
            table.AddRow("Mastery", new[] { "3" });

            var lines = table.ToString().Split('\n');
            var heading = CellsOf(lines[0])[0];
            var run = CellsOf(lines[1])[0];
            var mastery = CellsOf(lines[2])[0];

            Assert.That(run.pos, Is.EqualTo(heading.pos), "every row's cell starts where its column does");
            Assert.That(mastery.pos, Is.EqualTo(heading.pos));
            Assert.That(run.text.Length, Is.EqualTo(heading.text.Length), "cells are padded to the column's width");
            Assert.That(mastery.text.Length, Is.EqualTo(heading.text.Length));
            Assert.That(mastery.text, Does.EndWith("3").And.StartWith(" "), "padded on the left, so it lines up on the right");
            Assert.That(run.text, Does.EndWith("12"));
        }

        [Test]
        public void AnEmptyCell_LeavesItsColumnBlank()
        {
            var table = new TipTable("Level", "XP", "Speed");
            table.AddRow("This run", new[] { "12", "40 / 120", "×1.80" });
            table.AddRow("Total", new[] { "", "", "×2.10" });

            var lines = table.ToString().Split('\n');
            var headings = CellsOf(lines[0]);
            var total = CellsOf(lines[2]);

            Assert.That(total.Count, Is.EqualTo(1), "blank cells write nothing");
            Assert.That(total[0].pos, Is.EqualTo(headings[2].pos), "the one cell still sits under its own column");
            Assert.That(total[0].text.Trim(), Is.EqualTo("×2.10"));
            Assert.That(lines[2], Does.StartWith("Total"));
        }

        [Test]
        public void TextAfterTheCells_StartsAtTheNextColumn()
        {
            var table = new TipTable("Level", "XP");
            table.AddRow("Total", new[] { "15" }, "120 vitality");

            var lines = table.ToString().Split('\n');
            string xpColumn = CellsOf(lines[0])[1].pos;

            Assert.That(lines[1], Does.EndWith($"<pos={xpColumn}em>120 vitality"));
        }

        [Test]
        public void MoreCellsThanColumns_FailsLoudly()
        {
            var table = new TipTable("Level");

            Assert.Throws<System.ArgumentException>(() => table.AddRow("This run", new[] { "1", "2" }));
        }
    }
}
