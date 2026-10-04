using System;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// WhereScheduleLands reuses scratch lists (the room popover asks every frame), so an answer must
    /// not change when it is asked again, and an earlier answer must not be overwritten by a later one.
    /// </summary>
    public class ScheduleLandsScratchTests : SimulationTestBase
    {
        private Simulation _sim;
        private NodeDefinition _a, _b, _c;

        [SetUp]
        public void SetUp()
        {
            _a = MakeNode("A");
            _b = MakeNode("B");
            _c = MakeNode("C");
            Join(_a, _b);
            Join(_b, _c);
            var content = MakePlaces(_a, _b, _c);
            _sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            _sim.BeginLoop();
        }

        [Test]
        public void AskingTwice_GivesTheSameAnswer()
        {
            var first = _sim.WhereScheduleLands(_c);
            var second = _sim.WhereScheduleLands(_c);

            Assert.That(second.CanSchedule, Is.EqualTo(first.CanSchedule));
            Assert.That(second.Walk, Is.EqualTo(new[] { _b, _c }));
            Assert.That(second.InsertIndex, Is.EqualTo(first.InsertIndex));
            Assert.That(second.WalkSeconds, Is.EqualTo(first.WalkSeconds));
        }

        [Test]
        public void ALaterAnswer_DoesNotChangeAnEarlierOne()
        {
            var farRoom = _sim.WhereScheduleLands(_c);

            _sim.WhereScheduleLands(_b);
            _sim.WhereScheduleLands(_a);

            Assert.That(farRoom.Walk, Is.EqualTo(new[] { _b, _c }));
            Assert.That(farRoom.WalkSeconds, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ARoomOnTheRoute_AfterAWalkWasAsked_IsStillOnTheRoute()
        {
            _sim.WhereScheduleLands(_c); // fills the scratch lists with a longer walk
            _sim.Schedule(MakeTask("Wait", 1f), 1);

            var here = _sim.WhereScheduleLands(_a);

            Assert.That(here.OnRoute, Is.True);
            Assert.That(here.StopNumber, Is.EqualTo(1));
        }

        [Test]
        public void ARoomOnTheRoute_NeedsNoNewMemoryEachTime()
        {
            _sim.Schedule(MakeTask("Wait", 1f), 1);
            _sim.WhereScheduleLands(_a); // the first call may grow the scratch lists

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
                _sim.WhereScheduleLands(_a);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.That(allocated, Is.LessThan(100 * 8), "nothing allocated per call (the odd measuring byte aside)");
        }
    }
}
