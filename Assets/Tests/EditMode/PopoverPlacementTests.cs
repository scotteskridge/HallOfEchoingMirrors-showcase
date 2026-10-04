using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Where a room's popover opens: beside the room, or where the player left it relative to the room; always inside the map.</summary>
    public class PopoverPlacementTests
    {
        // The map: 1000 × 600, its lower-left corner at (0, 0). The popover: 300 × 200.
        private static readonly Rect Map = new Rect(0f, 0f, 1000f, 600f);
        private static readonly Vector2 Size = new Vector2(300f, 200f);
        private const float Gap = 10f;

        private static Rect Room(float x, float y) => new Rect(x, y, 100f, 50f);

        [Test]
        public void FitsRight_GoesRight()
        {
            var corner = PopoverPlacement.LowerLeft(Room(200f, 275f), Size, Map, Gap);

            Assert.AreEqual(310f, corner.x, 1e-4f, "just right of the room");
            Assert.AreEqual(200f, corner.y, 1e-4f, "level with the room's middle (300)");
        }

        [Test]
        public void NoRoomRight_FlipsLeft()
        {
            var corner = PopoverPlacement.LowerLeft(Room(800f, 275f), Size, Map, Gap);

            Assert.AreEqual(490f, corner.x, 1e-4f, "its right edge a gap left of the room");
        }

        [Test]
        public void NoRoomEitherSide_KeptInside()
        {
            var narrow = new Rect(0f, 0f, 400f, 600f);

            var corner = PopoverPlacement.LowerLeft(Room(150f, 275f), Size, narrow, Gap);

            Assert.That(corner.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(corner.x + Size.x, Is.LessThanOrEqualTo(400f));
        }

        [Test]
        public void NearTopOrBottom_StaysInside()
        {
            var nearTop = PopoverPlacement.LowerLeft(Room(200f, 580f), Size, Map, Gap);
            var nearBottom = PopoverPlacement.LowerLeft(Room(200f, -20f), Size, Map, Gap);

            Assert.AreEqual(400f, nearTop.y, 1e-4f, "its top at the map's top");
            Assert.AreEqual(0f, nearBottom.y, 1e-4f, "its bottom at the map's bottom");
        }

        [Test]
        public void TallerThanTheMap_TopAligned()
        {
            var low = new Rect(0f, 0f, 1000f, 150f);

            var corner = PopoverPlacement.LowerLeft(Room(200f, 50f), Size, low, Gap);

            Assert.AreEqual(-50f, corner.y, 1e-4f, "its header stays in view; the bottom is cut off");
        }

        // ---------- Once the player has dragged it ----------

        [Test]
        public void Placed_SameOffsetFromEveryRoom()
        {
            var offset = new Vector2(-400f, 150f); // up and to the left of the room's centre

            var byFirst = PopoverPlacement.LowerLeftAt(Room(500f, 175f), Size, Map, offset);
            var bySecond = PopoverPlacement.LowerLeftAt(Room(600f, 225f), Size, Map, offset);

            Assert.AreEqual(new Vector2(150f, 150f), byFirst, "top-left at (150, 350): the centre (550, 200) plus the offset");
            Assert.AreEqual(new Vector2(100f, 50f), bySecond - byFirst, "moves with the room");
        }

        [Test]
        public void Placed_KeptInsideTheMap()
        {
            var corner = PopoverPlacement.LowerLeftAt(Room(50f, 500f), Size, Map, new Vector2(-400f, 300f));

            Assert.AreEqual(0f, corner.x, 1e-4f, "not past the left edge");
            Assert.AreEqual(400f, corner.y, 1e-4f, "not past the top");
        }

        [Test]
        public void Placed_OffsetFromWhereItWasLeft_PutsItBackThere()
        {
            var room = Room(300f, 100f);
            var leftAt = new Vector2(620f, 330f);

            var offset = PopoverPlacement.TopLeftFromRoom(room, Size, leftAt);

            Assert.AreEqual(leftAt, PopoverPlacement.LowerLeftAt(room, Size, Map, offset));
        }

        // ---------- From any origin (DragToMove: a room's centre, or the map's top-left corner) ----------

        [Test]
        public void FromAnOrigin_OffsetFromWhereItWasLeft_PutsItBackThere()
        {
            var mapTopLeft = new Vector2(Map.xMin, Map.yMax);
            var leftAt = new Vector2(40f, 250f);

            var offset = PopoverPlacement.TopLeftFrom(mapTopLeft, Size, leftAt);

            Assert.AreEqual(new Vector2(40f, -150f), offset, "its top-left corner, 40 right and 150 down from the map's");
            Assert.AreEqual(leftAt, PopoverPlacement.LowerLeftAt(mapTopLeft, Size, Map, offset));
        }

        [Test]
        public void FromAnOrigin_KeptInsideTheMap()
        {
            var corner = PopoverPlacement.LowerLeftAt(new Vector2(Map.xMin, Map.yMax), Size, Map, new Vector2(900f, 100f));

            Assert.AreEqual(new Vector2(700f, 400f), corner, "pulled back from the right edge and the top");
        }

        // ---------- Dragged partly off the map (DragToMove): some always stays in view ----------

        private const float Visible = 80f;

        [Test]
        public void Grabbable_CanHangOffTheBottom_KeepingItsTopInView()
        {
            var corner = PopoverPlacement.KeepGrabbable(new Vector2(400f, -500f), Size, Map, Visible);

            Assert.AreEqual(400f, corner.x, 1e-4f);
            Assert.AreEqual(Visible - Size.y, corner.y, 1e-4f, "its top 80 above the bottom edge: 120 of it hangs below");
        }

        [Test]
        public void Grabbable_CanHangOffEitherSide_KeepingSomeInView()
        {
            var left = PopoverPlacement.KeepGrabbable(new Vector2(-1000f, 200f), Size, Map, Visible);
            var right = PopoverPlacement.KeepGrabbable(new Vector2(2000f, 200f), Size, Map, Visible);

            Assert.AreEqual(Visible - Size.x, left.x, 1e-4f, "80 of it still in view on the left");
            Assert.AreEqual(Map.xMax - Visible, right.x, 1e-4f, "80 of it still in view on the right");
        }

        [Test]
        public void Grabbable_NeverAboveTheTop_SoItsHeaderCanBeReached()
        {
            var corner = PopoverPlacement.KeepGrabbable(new Vector2(400f, 900f), Size, Map, Visible);

            Assert.AreEqual(Map.yMax - Size.y, corner.y, 1e-4f);
        }

        [Test]
        public void Grabbable_TallerThanTheMap_CanStillMoveDown()
        {
            var tall = new Vector2(300f, 900f);

            var corner = PopoverPlacement.KeepGrabbable(new Vector2(400f, -1000f), tall, Map, Visible);

            Assert.AreEqual(Visible, corner.y + tall.y, 1e-4f, "its top can come down to 80 above the bottom edge");
        }

        [Test]
        public void Grabbable_FromAnOrigin_UsesTheSameRule()
        {
            var corner = PopoverPlacement.LowerLeftAt(new Vector2(Map.xMin, Map.yMax), Size, Map, new Vector2(100f, -900f), Visible);

            Assert.AreEqual(new Vector2(100f, Visible - Size.y), corner);
        }
    }
}
