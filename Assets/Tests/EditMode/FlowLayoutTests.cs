using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The wrapping row (the stats chips): where each item goes, and how tall the rows get.</summary>
    public class FlowLayoutTests
    {
        private static readonly Vector2 Chip = new Vector2(100f, 30f);

        [Test]
        public void Place_FitsOnOneLine()
        {
            var at = FlowLayout.Place(new[] { Chip, Chip, Chip }, 400f, 10f);

            Assert.AreEqual(new Vector2(0f, 0f), at[0]);
            Assert.AreEqual(new Vector2(110f, 0f), at[1]);
            Assert.AreEqual(new Vector2(220f, 0f), at[2]);
        }

        [Test]
        public void Place_WrapsWhenFull()
        {
            // Three fit in 320 (100 + 10 + 100 + 10 + 100); the fourth starts a new line.
            var at = FlowLayout.Place(new[] { Chip, Chip, Chip, Chip }, 320f, 10f);

            Assert.AreEqual(new Vector2(220f, 0f), at[2]);
            Assert.AreEqual(new Vector2(0f, 40f), at[3]);
        }

        [Test]
        public void Place_TooWideGetsOwnLine()
        {
            var wide = new Vector2(500f, 30f);
            var at = FlowLayout.Place(new[] { Chip, wide, Chip }, 320f, 10f);

            Assert.AreEqual(new Vector2(0f, 0f), at[0]);
            Assert.AreEqual(new Vector2(0f, 40f), at[1], "Too wide for any line: it starts its own.");
            Assert.AreEqual(new Vector2(0f, 80f), at[2], "Nothing goes beside it.");
        }

        [Test]
        public void Place_HeightGrowsWithLines()
        {
            var one = new[] { Chip, Chip };
            var two = new[] { Chip, Chip, Chip, Chip };

            Assert.AreEqual(30f, FlowLayout.Height(one, FlowLayout.Place(one, 320f, 10f)));
            Assert.AreEqual(70f, FlowLayout.Height(two, FlowLayout.Place(two, 320f, 10f)));
            Assert.AreEqual(0f, FlowLayout.Height(new Vector2[0], new Vector2[0]));
        }
    }
}
