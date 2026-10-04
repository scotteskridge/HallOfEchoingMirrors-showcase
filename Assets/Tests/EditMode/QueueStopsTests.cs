using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The queue read as stops: one per visit to a room, as the queue drawer shows it.</summary>
    public class QueueStopsTests : SimulationTestBase
    {
        private GameContent _content;
        private TaskDefinition _look, _listen;
        private NodeDefinition _hall, _corridor, _vault, _attic;

        // The hall ↔ the corridor ↔ the vault. The attic is joined to nothing.
        [SetUp]
        public void SetUp()
        {
            _hall = MakeNode("A Dark Hall");
            _corridor = MakeNode("The Corridor");
            _vault = MakeNode("The Vault");
            _attic = MakeNode("The Attic");
            Join(_hall, _corridor);
            Join(_corridor, _vault);
            _content = MakePlaces(_hall, _corridor, _vault, _attic);
            _look = MakeTask("Look", 1f);   // listed at no room: can be done anywhere
            _listen = MakeTask("Listen", 1f);
            _content.tasks.AddRange(new[] { _look, _listen });
        }

        private Simulation MakeSimulation() => new Simulation(MakeLoopSettings(), TicksPerSecond, _content);

        [Test]
        public void EmptyQueue_OneStopHere()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();

            var stops = StopsOf(sim);

            Assert.That(stops.Count, Is.EqualTo(1));
            Assert.That(stops[0].Room, Is.EqualTo(_hall));
            Assert.That(stops[0].Number, Is.EqualTo(1));
            Assert.That(stops[0].IsReturn, Is.False);
            Assert.That(stops[0].FirstEntry, Is.EqualTo(0));
            Assert.That(stops[0].EntryCount, Is.EqualTo(0));
        }

        [Test]
        public void ActionsBeforeFirstTrip_BelongToFirstStop()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);
            sim.Schedule(_listen);

            var stops = StopsOf(sim);

            Assert.That(stops.Count, Is.EqualTo(1));
            Assert.That(stops[0].EntryCount, Is.EqualTo(2));
        }

        [Test]
        public void EachTrip_StartsNumberedStop()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);            // 0: the hall
            sim.ScheduleTrip(_corridor);    // 1: starts the corridor
            sim.Schedule(_look);            // 2
            sim.Schedule(_listen);          // 3
            sim.ScheduleTrip(_vault);       // 4: starts the vault

            var stops = StopsOf(sim);

            Assert.That(stops.Count, Is.EqualTo(3));
            Assert.That(stops[0].Room, Is.EqualTo(_hall));
            Assert.That((stops[0].FirstEntry, stops[0].EntryCount), Is.EqualTo((0, 1)));
            Assert.That(stops[1].Room, Is.EqualTo(_corridor));
            Assert.That(stops[1].Number, Is.EqualTo(2));
            Assert.That((stops[1].FirstEntry, stops[1].EntryCount), Is.EqualTo((1, 3)), "the trip is the stop's first entry");
            Assert.That(stops[2].Room, Is.EqualTo(_vault));
            Assert.That(stops[2].Number, Is.EqualTo(3));
            Assert.That((stops[2].FirstEntry, stops[2].EntryCount), Is.EqualTo((4, 1)));
            Assert.That(sim.WillTravel(1), Is.True);
            Assert.That(sim.WillTravel(0), Is.False, "not a trip");
        }

        [Test]
        public void RoomVisitedAgain_IsReturn()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            sim.ScheduleTrip(_vault);
            sim.ScheduleTrip(_corridor);

            var stops = StopsOf(sim);

            Assert.That(stops.Count, Is.EqualTo(4));
            Assert.That(stops[1].IsReturn, Is.False, "first visit");
            Assert.That(stops[2].IsReturn, Is.False);
            Assert.That(stops[3].Room, Is.EqualTo(_corridor));
            Assert.That(stops[3].IsReturn, Is.True);
        }

        [Test]
        public void StartRoomRevisited_IsReturn()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            sim.ScheduleTrip(_hall);

            var stops = StopsOf(sim);

            Assert.That(stops[0].IsReturn, Is.False, "where she starts is never a return");
            Assert.That(stops[2].Room, Is.EqualTo(_hall));
            Assert.That(stops[2].IsReturn, Is.True);
        }

        [Test]
        public void UnreachableTrip_StartsNoStop()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_attic);       // queued between runs: nothing is refused yet
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);

            var stops = StopsOf(sim);

            Assert.That(sim.WillTravel(0), Is.False, "no way to the attic");
            Assert.That(sim.PlannedNodeAfter(1), Is.EqualTo(_hall), "PlannedNodeAfter skips it too");
            Assert.That(stops.Count, Is.EqualTo(2));
            Assert.That((stops[0].FirstEntry, stops[0].EntryCount), Is.EqualTo((0, 2)), "it falls in the stop it's in");
            Assert.That(stops[1].Room, Is.EqualTo(_corridor));
            Assert.That(sim.SkippedTripReason(0), Is.EqualTo(Reason("no_way", ("room", "a Dark Hall"))));
            Assert.That(sim.SkippedTripReason(2), Is.Null, "a trip she'll make");
        }

        [Test]
        public void TripToWhereSheWillBe_SaysAlreadyThere()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_corridor);
            sim.ScheduleTrip(_corridor); // between runs nothing is refused, so this can be queued

            Assert.That(sim.WillTravel(1), Is.False);
            Assert.That(sim.SkippedTripReason(1), Is.EqualTo(Reason("already_there")),
                "the same words the run uses when it skips the trip");
        }

        [Test]
        public void BetweenRuns_UsesNextRunsQueueFromStart()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            RunSeconds(sim, 2);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_corridor));
            sim.EndRunEarly();

            sim.ScheduleTrip(_corridor);
            sim.ScheduleTrip(_vault);
            var stops = StopsOf(sim);

            Assert.That(stops.Count, Is.EqualTo(3));
            Assert.That(stops[0].Room, Is.EqualTo(_hall), "the next run starts at the start room");
            Assert.That(stops[0].EntryCount, Is.EqualTo(0));
            Assert.That(stops[2].Room, Is.EqualTo(_vault));
        }
    }
}
