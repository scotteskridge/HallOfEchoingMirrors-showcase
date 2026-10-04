using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Where a dragged stop would drop among the stop cards, stacked top to bottom in the Queue column.</summary>
    public class QueueGapTests
    {
        // Three cards, their middles in world y (which grows upwards): top card first.
        private static readonly float[] Middles = { 300f, 200f, 100f };

        [TestCase(250f, 1)]  // between the first two cards
        [TestCase(190f, 2)]  // just under the second card's middle
        [TestCase(150f, 2)]
        [TestCase(50f, 3)]   // below the last card's middle: after the last
        public void StopGap_Vertical_LandsBeforeTheFirstCardBelowThePointer(float pointerY, int expected)
        {
            Assert.AreEqual(expected, QueueDrawer.StopGapBefore(Middles, pointerY));
        }

        [Test]
        public void StopGap_AboveTheFirstCard_IsNeverBeforeTheFirstStop()
        {
            Assert.AreEqual(1, QueueDrawer.StopGapBefore(Middles, 400f));
        }
    }
}
