using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The shortest walk between two rooms: fewest seconds, then fewest rooms, then listed order.</summary>
    public class RouteSearchTests : SimulationTestBase
    {
        private GameContent _content;
        private NodeDefinition _a, _b, _c, _d;

        [SetUp]
        public void SetUp()
        {
            _a = MakeNode("A");
            _b = MakeNode("B");
            _c = MakeNode("C");
            _d = MakeNode("D");
            _content = MakePlaces(_a, _b, _c, _d); // Travel: 1s before rooms change it
        }

        private Simulation Begin()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void Find_TwoWalks_PicksTheFewestSeconds()
        {
            Join(_a, _d);
            Join(_a, _b);
            Join(_b, _d);
            _a.leaving.time = 10f;   // leaving A is slow: the direct way takes 10s...
            _b.entering.time = 0.1f; // ...but A to B takes 1s, and B to D another 1s

            var walk = RouteSearch.Find(Begin(), _a, _d);

            Assert.That(walk, Is.EqualTo(new[] { _b, _d }));
        }

        [Test]
        public void Find_EqualSeconds_TieBreaksByRoomsThenListOrder()
        {
            // Two-room walks of 2s each, and a direct 2s way: the direct one has fewest rooms.
            Join(_a, _b);
            Join(_a, _c);
            Join(_a, _d);
            Join(_b, _d);
            Join(_c, _d);
            _d.entering.time = 2f;
            _b.entering.time = 0.5f;
            _b.leaving.time = 0.75f;
            _c.entering.time = 0.5f;
            _c.leaving.time = 0.75f;

            var sim = Begin();
            Assert.That(RouteSearch.Find(sim, _a, _d), Is.EqualTo(new[] { _d }), "fewest rooms");

            // Without the direct way, B and C tie: the one listed first wins.
            _a.ways.RemoveAll(way => way.to == _d);
            Assert.That(RouteSearch.Find(sim, _a, _d), Is.EqualTo(new[] { _b, _d }), "listed order");
        }

        [Test]
        public void TryFind_AWayNotFoundThisRun_IsIgnored()
        {
            Join(_a, _b);
            var hidden = Join(_b, _c);
            hidden.foundAtExplored = 50; // not found until B is half searched

            var sim = Begin();

            Assert.That(RouteSearch.TryFind(sim, _a, _c, out var walk), Is.False);
            Assert.That(walk, Is.Empty);
            Assert.That(RouteSearch.TryFind(sim, _a, _b, out _), Is.True);
        }
    }
}
