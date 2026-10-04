using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Planned rooms (plan ui-053): shown only in the map preview, ignored by the game until the tick is cleared.</summary>
    public class PlannedRoomTests : SimulationTestBase
    {
        private NodeDefinition _hall, _junction, _planned;
        private GameContent _content;

        // The hall ↔ the junction are real; the planned room hangs off the junction.
        [SetUp]
        public void SetUp()
        {
            _hall = MakeNode("The hall");
            _junction = MakeNode("The junction");
            _planned = MakeNode("A room for Act II");
            _planned.planned = true;
            Join(_hall, _junction);
            _content = MakePlaces(_hall, _junction, _planned);
        }

        private Simulation MakeSimulation() => new Simulation(MakeLoopSettings(), TicksPerSecond, _content);

        [Test]
        public void PlayableNodes_SkipsPlannedRooms()
        {
            Assert.That(_content.PlayableNodes, Is.EqualTo(new[] { _hall, _junction }));
            Assert.That(_content.nodes, Has.Count.EqualTo(3), "the asset list still holds it");
        }

        [Test]
        public void TaskListedOnlyInAPlannedRoom_IsNotOfferedAnywhere()
        {
            // Room actions are also listed in GameContent.tasks; with the room planned, the action must not
            // read as "listed at no room" and so be doable everywhere.
            var task = MakeTask("An Act II action", 1f);
            _planned.tasks.Add(task);
            _content.tasks.Add(task);
            var sim = MakeSimulation();
            sim.BeginLoop();
            var offers = new System.Collections.Generic.List<ActionOffer>();

            sim.OffersAt(_hall, offers);

            Assert.That(sim.IsAvailableAt(task, _hall), Is.False);
            Assert.That(offers.Exists(o => o.Task == task), Is.False);
        }

        [Test]
        public void PlayableNodes_FollowsLaterChanges()
        {
            Assert.That(_content.PlayableNodes, Has.No.Member(_planned), "read once, so the list is cached");
            _planned.planned = false;
            Assert.That(_content.PlayableNodes, Has.Member(_planned), "clearing the tick is seen at once");

            var another = MakeNode("Another");
            _content.nodes.Add(another);
            Assert.That(_content.PlayableNodes, Has.Member(another), "a room added later is seen");

            another.planned = true;
            Assert.That(_content.PlayableNodes, Has.No.Member(another), "ticking Planned after a read hides it again");
        }

        [Test]
        public void PlannedRoom_NeverOnTheMap()
        {
            _planned.alwaysOnMap = true;
            Join(_junction, _planned);
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.AllNodes, Has.No.Member(_planned));
            Assert.That(sim.RoomsOnMap(), Is.EquivalentTo(new[] { _hall, _junction }));
            Assert.That(sim.IsOnMap(_planned), Is.False);
        }

        [Test]
        public void WayIntoPlannedRoom_IsNeverOffered()
        {
            Join(_junction, _planned);
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.FindWay(_junction, _planned).way, Is.Null, "not found");
            Assert.That(sim.DestinationsFrom(_junction), Is.EquivalentTo(new[] { _hall }), "not opened");
            Assert.That(sim.CanScheduleTripTo(_planned), Is.False);
            sim.PlayTripNow(_planned);
            sim.TryScheduleTrip(_planned);
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "not travelled");
        }

        [Test]
        public void WayIntoPlannedRoom_IsNeverAnnounced()
        {
            Join(_junction, _planned, startsOpen: false);
            var wave = MakeTask("Wave", 1f);
            _content.tasks.Add(wave);
            var opener = Make<SwitchDefinition>();
            opener.trigger = SwitchTrigger.TasksCompletedInOneRun;
            opener.requiredTasks.Add(wave);
            opener.opensWays.Add(new WayRef { from = _junction, to = _planned });
            _content.switches.Add(opener);
            var sim = MakeSimulation();
            int found = 0;
            sim.WayFound += (_, _) => found++;
            sim.BeginLoop();
            sim.Schedule(wave, 1);

            RunSeconds(sim, 2);

            Assert.That(sim.IsFlipped(opener), Is.True);
            Assert.That(found, Is.EqualTo(0), "but the game never announces it");
        }

        [Test]
        public void BothWaysFromPlannedRoom_GivesNoWayBack()
        {
            Join(_planned, _hall, bothWays: true);
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.FindWay(_hall, _planned).way, Is.Null);
            Assert.That(sim.DestinationsFrom(_hall), Is.EquivalentTo(new[] { _junction }));
            Assert.That(sim.RoomsOnMap(), Is.EquivalentTo(new[] { _hall, _junction }));
        }

        [Test]
        public void ContentIndex_SkipsPlannedRooms()
        {
            Join(_junction, _planned);
            var task = MakeTask("A planned task", 1f);
            _planned.tasks.Add(task);

            var index = new ContentIndex(_content);

            Assert.That(index.Find<NodeDefinition>(_planned.Id), Is.Null, "a save can't name it");
            Assert.That(index.Find<TaskDefinition>(task.Id), Is.Null, "nor what only it holds");
            Assert.That(index.Find<NodeDefinition>(_junction.Id), Is.SameAs(_junction));
        }

        [Test]
        public void UntickedRoom_JoinsTheGame()
        {
            Join(_junction, _planned);
            _planned.planned = false;
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.AllNodes, Has.Member(_planned));
            Assert.That(sim.DestinationsFrom(_junction), Is.EquivalentTo(new[] { _hall, _planned }));
            Assert.That(new ContentIndex(_content).Find<NodeDefinition>(_planned.Id), Is.SameAs(_planned));
            Assert.That(sim.RoomsOnMap().Contains(_planned), Is.True);
        }
    }
}
