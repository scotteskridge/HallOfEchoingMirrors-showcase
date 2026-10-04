using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The slide-up drawer: which page its tabs open, and how far it slides.</summary>
    public class SlideDrawerTests
    {
        private const int Closed = SlideDrawer.None;

        [Test]
        public void ClickClosedTab_Opens()
        {
            Assert.AreEqual(2, SlideDrawer.OpenAfterClick(Closed, 2));
        }

        [Test]
        public void ClickOpenTab_Closes()
        {
            Assert.AreEqual(Closed, SlideDrawer.OpenAfterClick(2, 2));
        }

        [Test]
        public void ClickOtherTab_Switches()
        {
            Assert.AreEqual(0, SlideDrawer.OpenAfterClick(2, 0));
        }

        [TestCase(SlideDrawer.Edge.Bottom, 0f, -300f)]
        [TestCase(SlideDrawer.Edge.Top, 0f, 300f)]
        [TestCase(SlideDrawer.Edge.Left, -400f, 0f)]
        [TestCase(SlideDrawer.Edge.Right, 400f, 0f)]
        public void Offset_ClosedIsPastEdge_OpenIsHome(SlideDrawer.Edge edge, float closedX, float closedY)
        {
            var size = new Vector2(400f, 300f);

            Assert.AreEqual(new Vector2(closedX, closedY), SlideDrawer.Offset(edge, size, 0f));
            Assert.AreEqual(Vector2.zero, SlideDrawer.Offset(edge, size, 1f));
        }

        [Test]
        public void Offset_HalfOpen_IsHalfWay()
        {
            Assert.AreEqual(new Vector2(0f, -150f), SlideDrawer.Offset(SlideDrawer.Edge.Bottom, new Vector2(400f, 300f), 0.5f));
        }

        [Test]
        public void CoveredInset_ClosedIsZero()
        {
            Assert.AreEqual(0f, SlideDrawer.CoveredInset(SlideDrawer.Edge.Bottom, new Vector2(400f, 300f), 0f));
        }

        [TestCase(SlideDrawer.Edge.Bottom, 300f)]
        [TestCase(SlideDrawer.Edge.Top, 300f)]
        [TestCase(SlideDrawer.Edge.Left, 400f)]
        [TestCase(SlideDrawer.Edge.Right, 400f)]
        public void CoveredInset_OpenIsPanelHeight(SlideDrawer.Edge edge, float expected)
        {
            // Across the edge it slides from: height for top and bottom, width for the sides.
            Assert.AreEqual(expected, SlideDrawer.CoveredInset(edge, new Vector2(400f, 300f), 1f));
        }

        [Test]
        public void CoveredInset_HalfOpenIsHalf()
        {
            Assert.AreEqual(150f, SlideDrawer.CoveredInset(SlideDrawer.Edge.Bottom, new Vector2(400f, 300f), 0.5f));
        }
    }
}
