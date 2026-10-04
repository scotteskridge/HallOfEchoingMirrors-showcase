using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>How a room's floor pile is drawn on the map: one dot per kind of item, in kind order.</summary>
    public class FloorDotsTests : SimulationTestBase
    {
        private ResourceDefinition _wisp, _candle, _flint, _ring;

        [SetUp]
        public void SetUp()
        {
            _wisp = Item(ItemKind.Restorative);
            _candle = Item(ItemKind.Light);
            _flint = Item(ItemKind.Tool);
            _ring = Item(ItemKind.Keepsake);
        }

        private ResourceDefinition Item(ItemKind kind)
        {
            var item = Make<ResourceDefinition>();
            item.kind = kind;
            return item;
        }

        private static List<(ResourceDefinition item, int amount)> DotsOf(params (ResourceDefinition item, int amount)[] floor)
        {
            var dots = new List<(ResourceDefinition item, int amount)>();
            FloorDots.For(floor, dots);
            return dots;
        }

        [Test]
        public void EmptyFloor_NoDots()
        {
            Assert.That(DotsOf(), Is.Empty);
        }

        [Test]
        public void Dots_ForSeveralOfAKind_AreOneDotPerKind()
        {
            var dots = DotsOf((_candle, 6), (_wisp, 2));

            Assert.That(dots.Count, Is.EqualTo(2), "6 candles and 2 wisps: one dot each, not eight");
            Assert.That(dots, Does.Contain((_candle, 6)));
            Assert.That(dots, Does.Contain((_wisp, 2)));
        }

        [Test]
        public void Dots_OrderedByKind()
        {
            var dots = DotsOf((_ring, 1), (_flint, 1), (_candle, 3), (_wisp, 2));

            Assert.That(dots.ConvertAll(d => d.item), Is.EqualTo(new[] { _wisp, _candle, _flint, _ring }),
                "Restorative, Light, Tool, Keepsake, whatever order they were put down in");
        }

        [Test]
        public void Dots_OfTheSameKind_KeepTheFloorsOrder()
        {
            var phial = Item(ItemKind.Restorative);

            var dots = DotsOf((_candle, 1), (phial, 1), (_wisp, 1));

            Assert.That(dots.ConvertAll(d => d.item), Is.EqualTo(new[] { phial, _wisp, _candle }),
                "the phial was put down before the wisp, so it stays first");
        }

        [Test]
        public void EveryKind_GetsADot()
        {
            var floor = new List<(ResourceDefinition item, int amount)>();
            foreach (ItemKind kind in System.Enum.GetValues(typeof(ItemKind)))
                floor.Add((Item(kind), 1));

            var dots = new List<(ResourceDefinition item, int amount)>();
            Assert.DoesNotThrow(() => FloorDots.For(floor, dots), "add each new ItemKind to FloorDots.Order");
            Assert.That(dots.Count, Is.EqualTo(floor.Count), "one dot for an item of every kind");
        }

        [Test]
        public void CountRose_Swells_CountFell_DoesNot()
        {
            var before = new Dictionary<ResourceDefinition, int> { { _candle, 6 }, { _wisp, 2 } };

            Assert.That(FloorDots.Rose(before, _candle, 8), Is.True, "more candles put down");
            Assert.That(FloorDots.Rose(before, _wisp, 1), Is.False, "a wisp picked up or used");
            Assert.That(FloorDots.Rose(before, _candle, 6), Is.False, "nothing changed");
            Assert.That(FloorDots.Rose(before, _flint, 1), Is.False, "a new dot appears quietly");
            Assert.That(FloorDots.Rose(null, _candle, 8), Is.False, "nothing shown before: a new or loaded game");
        }

        [Test]
        public void Row_StartsAtTheRoomsLeftEdge_AndGrowsRight()
        {
            // A room 100 wide and 60 tall at the origin; 10-pixel dots, 4 apart, hanging 5 below its bottom-left corner.
            var room = new Vector2(100f, 60f);
            var offset = new Vector2(0f, -5f);

            var first = FloorDots.Position(Vector2.zero, room, 0, 10f, 4f, offset);
            var third = FloorDots.Position(Vector2.zero, room, 2, 10f, 4f, offset);

            Assert.That(first, Is.EqualTo(new Vector2(-45f, -40f)), "the first dot's left edge is on the room's left edge");
            Assert.That(third, Is.EqualTo(new Vector2(-45f + 2 * 14f, -40f)), "later dots to its right");
        }
    }
}
