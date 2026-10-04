using System.Linq;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Schedule in a room: where the action lands in the queue, and the walk added to get there.</summary>
    public class ScheduleTargetTests : SimulationTestBase
    {
        private GameContent _content;
        private TaskDefinition _look, _listen, _think;
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
            _look = MakeTask("Look", 1f);
            _listen = MakeTask("Listen", 1f);
            _think = MakeTask("Think", 1f);
            _content.tasks.AddRange(new[] { _look, _listen, _think });
        }

        private Simulation Begin()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();
            return sim;
        }

        // The queue as words: an action by name, a trip as "> room".
        private static string[] Queued(Simulation sim) =>
            sim.Queue.Entries.Select(e => e.Destination != null ? "> " + e.Destination.displayName : e.Task.displayName).ToArray();

        [Test]
        public void EndRoom_AppendsAtEnd()
        {
            var sim = Begin();
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_look);

            var target = sim.WhereScheduleLands(_corridor);
            sim.ScheduleIn(_listen, _corridor);

            Assert.That(target.OnRoute, Is.True);
            Assert.That(target.InsertIndex, Is.EqualTo(2));
            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "Look", "Listen" }));
        }

        [Test]
        public void RoomOnRoute_InsertsAtEndOfThatVisit()
        {
            var sim = Begin();
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_listen);

            sim.ScheduleIn(_think, _hall);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "Think", "> The Corridor", "Listen" }));
            Assert.That(sim.WhereScheduleLands(_hall).StopNumber, Is.EqualTo(1));
        }

        [Test]
        public void CurrentRoom_InsertsBeforeWalkingOn()
        {
            var sim = Begin();
            sim.ScheduleTrip(_corridor);

            sim.ScheduleIn(_look, _hall);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "> The Corridor" }));
        }

        [Test]
        public void RoomVisitedTwice_AddsToLastVisit()
        {
            var sim = Begin();
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_listen);
            sim.ScheduleTrip(_hall);
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);

            sim.ScheduleIn(_think, _hall);

            Assert.That(Queued(sim), Is.EqualTo(new[]
                { "Look", "> The Corridor", "Listen", "> A Dark Hall", "Look", "Think", "> The Corridor" }));
        }

        [Test]
        public void RoomOffRoute_AddsShortestWalkThenAction()
        {
            var sim = Begin();

            var target = sim.WhereScheduleLands(_vault);
            sim.ScheduleIn(_look, _vault);

            Assert.That(target.CanSchedule, Is.True);
            Assert.That(target.OnRoute, Is.False);
            Assert.That(target.Walk, Is.EqualTo(new[] { _corridor, _vault }));
            Assert.That(target.WalkSeconds, Is.EqualTo(2f).Within(0.001f));
            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "> The Vault", "Look" }));
        }

        [Test]
        public void RoomsOwnAction_CanBeScheduledBeforeSheEntersIt()
        {
            var read = MakeTask("Read the inscription", 1f);
            _corridor.tasks.Add(read);
            _content.tasks.Add(read);
            var sim = Begin();
            sim.ScheduleTrip(_corridor);

            var offers = new System.Collections.Generic.List<ActionOffer>();
            sim.OffersAt(_corridor, offers);
            sim.ScheduleIn(read, _corridor);

            Assert.That(offers.Select(o => o.Task), Does.Contain(read));
            Assert.That(offers.First(o => o.Task == read).Blocked, Is.False);
            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "Read the inscription" }));
        }

        [Test]
        public void PlayIn_RoomOnRoute_GoesFirstInThatVisit()
        {
            var sim = Begin();
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_listen);

            sim.PlayIn(_think, _corridor);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "> The Corridor", "Think", "Listen" }));
        }

        [Test]
        public void PlayIn_RoomOnRoute_SameTaskFirst_DoesNotDouble()
        {
            var sim = Begin();
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_listen);

            sim.PlayIn(_listen, _corridor);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "> The Corridor", "Listen" }));
        }

        [Test]
        public void PlayIn_RoomOffRoute_AddsWalkThenAction()
        {
            var sim = Begin();
            sim.Schedule(_look);

            sim.PlayIn(_think, _vault);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "> The Corridor", "> The Vault", "Think" }));
        }

        [Test]
        public void PlayIn_WhereSheIs_IsPlayNow()
        {
            var sim = Begin();
            sim.Schedule(_look);

            sim.PlayIn(_think, _hall);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Think", "Look" }));
        }

        private TaskDefinition MakeWispGatherer()
        {
            var wisp = MakeObject("Wisp");
            wisp.restoreVitality = 5f;
            var gather = MakeGatherTask("Gather a wisp", 1f, wisp);
            _content.tasks.Add(gather);
            return gather;
        }

        [Test]
        public void CarryIn_RoomOffRoute_AddsWalkThenACarryEntryAtTheEnd()
        {
            var gather = MakeWispGatherer();
            var sim = Begin();
            sim.Schedule(_look);

            sim.CarryIn(gather, _vault);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "Look", "> The Corridor", "> The Vault", "Gather a wisp" }));
            Assert.That(sim.Queue.Entries.Last().CarryOnly, Is.True, "it will stop once her pockets are full");
        }

        [Test]
        public void CarryIn_RoomOnRoute_GoesAtTheEndOfThatVisit_NotOnTop()
        {
            var gather = MakeWispGatherer();
            var sim = Begin();
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_look);
            sim.ScheduleTrip(_vault);

            sim.CarryIn(gather, _corridor);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "Look", "Gather a wisp", "> The Vault" }));
            Assert.That(sim.Queue.Entries[2].CarryOnly, Is.True);
        }

        [Test]
        public void CarryIn_WhereSheIs_IsCarryNow()
        {
            var gather = MakeWispGatherer();
            var sim = Begin();
            sim.Schedule(_look);

            sim.CarryIn(gather, _hall);

            Assert.That(sim.Queue.Top.Task, Is.SameAs(gather));
            Assert.That(sim.Queue.Top.CarryOnly, Is.True);
        }

        [Test]
        public void CarryIn_AFarRoom_StopsOnceHerPocketsAreFull()
        {
            var gather = MakeWispGatherer();
            var settings = MakeLoopSettings(pockets: 2);
            var sim = new Simulation(settings, TicksPerSecond, _content);
            sim.BeginLoop();
            sim.CarryIn(gather, _vault);

            RunSeconds(sim, 20f);

            Assert.That(sim.Loop.CurrentNode, Is.SameAs(_vault));
            Assert.That(sim.AmountOf(gather.gives[0].resource), Is.EqualTo(2), "pockets full");
            Assert.That(sim.OnFloor(_vault, gather.gives[0].resource), Is.EqualTo(0), "nothing gathered just to leave it on the floor");
        }

        [Test]
        public void NoFoundWay_CantWithReason()
        {
            var sim = Begin();

            var target = sim.WhereScheduleLands(_attic);
            sim.ScheduleIn(_look, _attic);

            Assert.That(target.CanSchedule, Is.False);
            Assert.That(target.Reason, Is.EqualTo(Reason("no_found_way", ("room", "the Attic"))));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        // ---------- Between runs: only rooms known by heart (plan 032a) ----------

        // One run that works in the corridor and the vault, or only in the vault: each room worked in counts once,
        // and the start room counts every run. Known by heart after one run.
        private Simulation BetweenRuns(bool workInCorridor)
        {
            var settings = MakeLoopSettings();
            settings.byHeartRuns = 1;
            var sim = new Simulation(settings, TicksPerSecond, _content);
            var quickened = MakeResource("Quickened Hours", ResourceLifetime.Forever, max: 1);
            quickened.unlocksPlanning = true;
            sim.Persistent.Resources[quickened] = 1; // planning earned
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            if (workInCorridor)
                sim.Schedule(_look, 1);
            sim.ScheduleTrip(_vault);
            sim.Schedule(_listen, 1);
            RunSeconds(sim, 10f);
            sim.EndRunEarly();
            sim.ClearQueue(); // whatever the run carried over: these tests plan from nothing
            return sim;
        }

        [Test]
        public void BetweenRuns_RoomNotByHeart_Refused()
        {
            var sim = BetweenRuns(workInCorridor: false);
            var refusals = new System.Collections.Generic.List<string>();
            sim.ActionRefused += (_, _, reason) => refusals.Add(reason);

            // The corridor was only walked through; acting there is not plannable.
            var target = sim.WhereScheduleLands(_corridor);
            sim.ScheduleIn(_look, _corridor);

            string why = Reason("not_by_heart_plan", ("room", "the Corridor"), ("runs", 0), ("needed", 1));
            Assert.That(target.CanSchedule, Is.False);
            Assert.That(target.Reason, Is.EqualTo(why));
            Assert.That(refusals, Is.EqualTo(new[] { why }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void BetweenRuns_ByHeart_Allowed()
        {
            var sim = BetweenRuns(workInCorridor: true);

            Assert.That(sim.WhereScheduleLands(_vault).CanSchedule, Is.True);
            sim.ScheduleIn(_listen, _vault);

            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "> The Vault", "Listen" }));
        }

        [Test]
        public void BetweenRuns_TripThroughRoomNotByHeart_Refused()
        {
            var sim = BetweenRuns(workInCorridor: false);
            Assert.That(sim.IsKnownByHeart(_vault), Is.True, "worked in");

            // Known at both ends, but the walk passes through the corridor.
            var target = sim.WhereScheduleLands(_vault);
            Assert.That(target.CanSchedule, Is.False);
            Assert.That(target.Reason, Is.EqualTo(Reason("not_by_heart_plan", ("room", "the Corridor"), ("runs", 0), ("needed", 1))));
            Assert.That(sim.CanScheduleTripTo(_vault), Is.False);
            sim.ScheduleTrip(_vault);
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "the first trip is refused, so nothing follows it");
        }

        [Test]
        public void DuringRun_RoomNotByHeart_Allowed()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            var quickened = MakeResource("Quickened Hours", ResourceLifetime.Forever, max: 1);
            quickened.unlocksPlanning = true;
            sim.Persistent.Resources[quickened] = 1; // planning earned: the refusal would apply between runs
            sim.BeginLoop();

            Assert.That(sim.IsKnownByHeart(_vault), Is.False);
            Assert.That(sim.WhereScheduleLands(_vault).CanSchedule, Is.True);
            Assert.That(sim.CanScheduleTripTo(_vault), Is.True);
            sim.ScheduleIn(_look, _vault);
            Assert.That(Queued(sim), Is.EqualTo(new[] { "> The Corridor", "> The Vault", "Look" }));
        }
    }
}
