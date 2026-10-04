using System;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Asking the queue for something impossible (no task, an entry that isn't there) is a bug in the caller, so it fails loudly.</summary>
    public class QueueGuardTests : SimulationTestBase
    {
        private Simulation _sim;

        [SetUp]
        public void SetUp()
        {
            _sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            _sim.BeginLoop();
        }

        [Test]
        public void SchedulingNoTask_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sim.Schedule(null, 1));
            Assert.Throws<ArgumentException>(() => _sim.PlayNow(null));
            Assert.Throws<ArgumentException>(() => _sim.CarryNow(null));
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(3)]
        public void RemovingAnEntryThatIsNotThere_Throws(int index)
        {
            _sim.Schedule(MakeTask("One", 1f), 1); // one entry: only index 0 exists

            Assert.Throws<ArgumentOutOfRangeException>(() => _sim.RemoveFromQueue(index));
            Assert.That(_sim.Queue.Count, Is.EqualTo(1), "the queue is untouched");
        }

        [Test]
        public void RemovingAnEntryThatIsThere_TakesItOut()
        {
            _sim.Schedule(MakeTask("One", 1f), 1);

            _sim.RemoveFromQueue(0);

            Assert.That(_sim.Queue.Count, Is.EqualTo(0));
        }

        // A click lands a frame after the queue changed, so rows name their entry rather than an index.
        [Test]
        public void RemoveByEntry_RemovesThatEntry()
        {
            _sim.Schedule(MakeTask("One", 1f), 1);
            _sim.Schedule(MakeTask("Two", 1f), 1);
            _sim.Schedule(MakeTask("Three", 1f), 1);
            var three = _sim.Queue.Entries[2];
            _sim.RemoveFromQueue(0); // the queue shifts: three is now index 1

            Assert.That(_sim.RemoveFromQueue(three), Is.True);

            Assert.That(_sim.Queue.Count, Is.EqualTo(1));
            Assert.That(_sim.Queue.Entries[0].Task.displayName, Is.EqualTo("Two"));
        }

        [Test]
        public void RemoveByEntry_GoneEntry_ReturnsFalse()
        {
            _sim.Schedule(MakeTask("One", 1f), 1);
            _sim.Schedule(MakeTask("Two", 1f), 1);
            var one = _sim.Queue.Entries[0];
            _sim.RemoveFromQueue(0);

            Assert.That(_sim.RemoveFromQueue(one), Is.False, "a stale click does nothing");
            Assert.That(_sim.Queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveByEntry_FiresQueueChangedOnce()
        {
            _sim.Schedule(MakeTask("One", 1f), 1);
            var one = _sim.Queue.Entries[0];
            int changed = 0;
            _sim.QueueChanged += () => changed++;

            _sim.RemoveFromQueue(one);
            _sim.RemoveFromQueue(one); // already gone

            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void RemovingFromAnEmptyQueue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sim.RemoveFromQueue(0));
        }
    }
}
