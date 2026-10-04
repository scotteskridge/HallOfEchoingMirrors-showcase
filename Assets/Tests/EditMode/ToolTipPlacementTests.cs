using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>A pop-up tooltip keeps clear of the notice toast, and always stays on screen.</summary>
    public class ToolTipPlacementTests
    {
        // The screen: 1000 × 600, lower-left at (0, 0), y up. A tooltip 200 × 100. The toast sits bottom right.
        private static readonly Rect Screen = new Rect(0f, 0f, 1000f, 600f);
        private static readonly Vector2 Size = new Vector2(200f, 100f);
        private static readonly Rect Toast = new Rect(700f, 20f, 280f, 60f);
        private const float Gap = 8f;

        private static bool Overlaps(Vector2 lowerLeft, Rect other) => new Rect(lowerLeft, Size).Overlaps(other);

        [Test]
        public void ClearOf_Overlap_MovesAway()
        {
            var start = new Vector2(750f, 40f);
            Assume.That(Overlaps(start, Toast));

            var corner = ToolTipPlacement.ClearOf(start, Size, Toast, Screen, Gap);

            Assert.IsFalse(Overlaps(corner, Toast), "the tooltip no longer covers the toast");
        }

        [Test]
        public void ClearOf_NoOverlap_Unchanged()
        {
            var start = new Vector2(100f, 300f);

            var corner = ToolTipPlacement.ClearOf(start, Size, Toast, Screen, Gap);

            Assert.AreEqual(start, corner);
        }

        [Test]
        public void ClearOf_StaysOnScreen()
        {
            // Toast against the top edge, tooltip beneath it and squeezed against the bottom: both ways are tight.
            var toast = new Rect(400f, 500f, 300f, 90f);
            var start = new Vector2(450f, 420f);
            Assume.That(Overlaps(start, toast));

            var corner = ToolTipPlacement.ClearOf(start, Size, toast, Screen, Gap);

            Assert.IsTrue(Screen.Contains(corner) && Screen.Contains(corner + Size), "inside the screen");
            Assert.IsFalse(Overlaps(corner, toast), "and clear of the toast");
        }

        [Test]
        public void ClearOf_NoToast_Unchanged()
        {
            var start = new Vector2(750f, 40f);

            var corner = ToolTipPlacement.ClearOf(start, Size, Rect.zero, Screen, Gap);

            Assert.AreEqual(start, corner, "an empty avoid-rectangle (toast hidden) moves nothing");
        }

        // Beside: a 100 × 50 target in a 1000 × 600 screen, and the 200 × 100 tooltip.
        private static readonly Rect Target = new Rect(300f, 250f, 100f, 50f);

        // Rect.Contains leaves out its far edges, but a tooltip may touch the screen's edge.
        private static bool Inside(Vector2 lowerLeft, Rect screen) =>
            lowerLeft.x >= screen.xMin && lowerLeft.y >= screen.yMin && lowerLeft.x + Size.x <= screen.xMax && lowerLeft.y + Size.y <= screen.yMax;

        [Test]
        public void Beside_RoomOnTheRight_SitsRightWithTheGap()
        {
            var corner = ToolTipPlacement.Beside(Target, Size, Screen, Gap);

            Assert.AreEqual(Target.xMax + Gap, corner.x, 0.001f, "8 px right of the target");
            Assert.IsFalse(Overlaps(corner, Target));
        }

        [Test]
        public void Beside_NoRoomOnTheRight_FlipsLeft()
        {
            var target = new Rect(850f, 250f, 100f, 50f);

            var corner = ToolTipPlacement.Beside(target, Size, Screen, Gap);

            Assert.AreEqual(target.xMin - Gap - Size.x, corner.x, 0.001f);
            Assert.IsFalse(Overlaps(corner, target));
        }

        [Test]
        public void Beside_NoRoomEitherSide_GoesBelow()
        {
            // 500 wide in a 600 wide area leaves 50 on each side: too tight for 200 + gap.
            var screen = new Rect(0f, 0f, 600f, 600f);
            var target = new Rect(50f, 400f, 500f, 50f);

            var corner = ToolTipPlacement.Beside(target, Size, screen, Gap);

            Assert.AreEqual(target.yMin - Gap - Size.y, corner.y, 0.001f, "below, with the gap");
            Assert.IsFalse(Overlaps(corner, target));
        }

        [Test]
        public void Beside_NoRoomEitherSideOrBelow_GoesAbove()
        {
            var screen = new Rect(0f, 0f, 600f, 600f);
            var target = new Rect(50f, 50f, 500f, 50f);

            var corner = ToolTipPlacement.Beside(target, Size, screen, Gap);

            Assert.AreEqual(target.yMax + Gap, corner.y, 0.001f);
            Assert.IsFalse(Overlaps(corner, target));
        }

        [Test]
        public void Beside_NothingFits_ClampsOnScreen()
        {
            var screen = new Rect(0f, 0f, 300f, 150f);
            var target = new Rect(10f, 10f, 280f, 130f);

            var corner = ToolTipPlacement.Beside(target, Size, screen, Gap);

            Assert.IsTrue(Inside(corner, screen), "better covering it than off screen");
        }

        [Test]
        public void Beside_NearTheTop_StaysOnScreen()
        {
            var target = new Rect(300f, 560f, 100f, 40f);

            var corner = ToolTipPlacement.Beside(target, Size, Screen, Gap);

            Assert.IsTrue(Inside(corner, Screen), "centred on the target, then clamped inside");
        }
    }
}
