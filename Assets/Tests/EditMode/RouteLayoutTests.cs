using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>How the queue's stops are drawn on the map: the legs, the badges and Clara's token.</summary>
    public class RouteLayoutTests : SimulationTestBase
    {
        private NodeDefinition _a, _b, _c;

        [SetUp]
        public void SetUp()
        {
            _a = Make<NodeDefinition>();
            _b = Make<NodeDefinition>();
            _c = Make<NodeDefinition>();
        }

        // Stops as QueueStops makes them: numbered from 1, one entry each (the entries don't matter here).
        private static List<QueueStop> Stops(params NodeDefinition[] rooms)
        {
            var stops = new List<QueueStop>();
            for (int i = 0; i < rooms.Length; i++)
                stops.Add(new QueueStop(rooms[i], i + 1, stops.Exists(s => s.Room == rooms[i]), i, 1));
            return stops;
        }

        private static List<(NodeDefinition from, NodeDefinition to)> LegsOf(List<QueueStop> stops)
        {
            var legs = new List<(NodeDefinition from, NodeDefinition to)>();
            RouteLayout.Legs(stops, legs);
            return legs;
        }

        private static List<(NodeDefinition room, List<int> numbers)> BadgesOf(List<QueueStop> stops)
        {
            var badges = new List<(NodeDefinition room, List<int> numbers)>();
            RouteLayout.Badges(stops, badges);
            return badges;
        }

        [Test]
        public void NoTrips_NoLegs_NoBadges()
        {
            var stops = Stops(_a);

            Assert.That(LegsOf(stops), Is.Empty);
            Assert.That(BadgesOf(stops), Is.Empty); // the token already shows where she is
        }

        [Test]
        public void OneTrip_BothRoomsBadged()
        {
            var badges = BadgesOf(Stops(_a, _b));

            Assert.That(badges.Count, Is.EqualTo(2));
            Assert.That(badges[0].numbers, Is.EqualTo(new[] { 1 }));
            Assert.That(badges[1].numbers, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void EachTrip_OneLeg_InOrder()
        {
            var legs = LegsOf(Stops(_a, _b, _c));

            Assert.That(legs.Count, Is.EqualTo(2));
            Assert.That(legs[0], Is.EqualTo((_a, _b)));
            Assert.That(legs[1], Is.EqualTo((_b, _c)));
        }

        [Test]
        public void RoomVisitedTwice_BadgeListsBoth()
        {
            var badges = BadgesOf(Stops(_a, _b, _a));

            Assert.That(badges.Count, Is.EqualTo(2));
            Assert.That(badges[0].room, Is.EqualTo(_a));
            Assert.That(badges[0].numbers, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(badges[1].room, Is.EqualTo(_b));
            Assert.That(badges[1].numbers, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void SameWayBothWays_OneLeg()
        {
            var legs = LegsOf(Stops(_a, _b, _a));

            Assert.That(legs.Count, Is.EqualTo(1));
            Assert.That(legs[0], Is.EqualTo((_a, _b)));
        }

        [Test]
        public void Trim_HalfWay_StartsAtMidpoint()
        {
            var (start, end) = RouteLayout.Part(new Vector2(0f, 0f), new Vector2(100f, 50f), 0.5f, 1f);

            Assert.That(Vector2.Distance(start, new Vector2(50f, 25f)), Is.LessThan(1e-4f));
            Assert.That(Vector2.Distance(end, new Vector2(100f, 50f)), Is.LessThan(1e-4f));
            // And back: how far along the leg a point is, so the line can end where the token is.
            Assert.That(RouteLayout.FractionAlong(new Vector2(0f, 0f), new Vector2(100f, 50f), start), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void TokenPoint_FollowsProgress()
        {
            var from = new Vector2(0f, 0f);
            var to = new Vector2(200f, 0f);
            var rest = new Vector2(90f, -24f); // by the room's bottom-right corner

            Assert.That(Vector2.Distance(RouteLayout.TokenPoint(from, to, 0.25f, rest), new Vector2(140f, -24f)), Is.LessThan(1e-4f));
            // Not travelling: at her room's rest spot, whatever the progress of what she's doing.
            Assert.That(Vector2.Distance(RouteLayout.TokenPoint(from, null, 0.25f, rest), rest), Is.LessThan(1e-4f));
        }
    }
}
