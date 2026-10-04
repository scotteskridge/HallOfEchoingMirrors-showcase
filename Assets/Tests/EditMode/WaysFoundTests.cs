using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Ways hidden until the room they leave from has been searched far enough. A search is kept for
    /// good, so a way once found stays found in every later run.
    /// </summary>
    public class WaysFoundTests : SimulationTestBase
    {
        private readonly List<string> _found = new List<string>();

        private GameContent _content;
        private TaskDefinition _explore;
        private NodeDefinition _hall, _junction;
        private Way _way;
        private StoryBeat _story;

        // The hall (2 explores to fill) → the junction, found once the hall is fully explored.
        [SetUp]
        public void SetUp()
        {
            _found.Clear();
            _explore = MakeTask("Explore", 1f);
            _hall = Make<NodeDefinition>();
            _hall.displayName = "The hall";
            _hall.exploresToFill = 2;
            _junction = Make<NodeDefinition>();
            _junction.displayName = "The junction";

            _story = MakeStory("A Way On\nShe found it.");
            _way = new Way { to = _junction, foundAtExplored = 100, story = _story };
            _hall.ways.Add(_way);

            _content = Make<GameContent>();
            _content.travelVerb = MakeTask("Travel", 1f);
            _content.exploreVerb = _explore;
            _content.startNode = _hall;
            _content.nodes.AddRange(new[] { _hall, _junction });
        }

        private Simulation MakeSimulation(LoopSettings settings = null)
        {
            var sim = new Simulation(settings ?? MakeLoopSettings(), TicksPerSecond, _content);
            sim.WayFound += (from, to) => _found.Add($"{from.displayName} → {to.displayName}");
            return sim;
        }

        [Test]
        public void AWayNotYetFound_IsHidden_AndCantBeTaken()
        {
            var sim = MakeSimulation();

            Assert.That(sim.DestinationsFrom(_hall), Is.Empty);
            Assert.That(sim.IsOnMap(_junction), Is.False);

            var skipped = RecordRefusals(sim, withTaskName: false);
            sim.ScheduleTrip(_junction); // queued anyway (automation could do this)
            sim.BeginLoop();
            RunSeconds(sim, 1);
            Assert.That(skipped, Is.EqualTo(new[] { Reason("trip", ("room", GameText.TitleInSentence(_junction.DisplayName)), ("reason", Reason("way_not_found"))) }));
        }

        [Test]
        public void ExploringTheRoom_FindsTheWay_Once_AndRevealsItsStory()
        {
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();

            RunSeconds(sim, 1);
            Assert.That(_found, Is.Empty, "half explored");

            RunSeconds(sim, 1);
            Assert.That(_found, Is.EqualTo(new[] { "The hall → The junction" }));
            Assert.That(sim.UnreadStories, Has.Member(_story));
            Assert.That(sim.DestinationsFrom(_hall), Is.EqualTo(new[] { _junction }));
            Assert.That(sim.IsOnMap(_junction), Is.True);
        }

        [Test]
        public void AFoundWay_CanBeAddedMidRun_AndTaken()
        {
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            RunSeconds(sim, 2); // found; the queue has run out

            Assert.That(sim.PlannedEndNode, Is.EqualTo(_hall));
            sim.ScheduleTrip(_junction);
            RunSeconds(sim, 1);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));
        }

        [Test]
        public void AFoundWay_StaysFound_InTheNextRun_WithoutBeingAnnouncedAgain()
        {
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            RunSeconds(sim, 2);
            sim.EndRunEarly();

            sim.BeginLoop();
            Assert.That(sim.IsOnMap(_junction), Is.True, "still on the map");
            Assert.That(sim.DestinationsFrom(_hall), Is.EqualTo(new[] { _junction }), "and to be taken, with no search");
            Assert.That(_found.Count, Is.EqualTo(1), "not news the second time");
        }

        [Test]
        public void HiddenWay_UsableNextRun_WithoutSearching()
        {
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            RunSeconds(sim, 2);
            sim.EndRunEarly();
            var skipped = RecordRefusals(sim, withTaskName: false);

            sim.BeginLoop();
            sim.ScheduleTrip(_junction);
            RunSeconds(sim, 1);

            Assert.That(skipped, Is.Empty);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction), "walked straight through");
        }

        [Test]
        public void AWayAlreadyFound_WhenAGameLoads_IsntAnnounced()
        {
            var saved = new PersistentState();
            saved.FoundWays.Add((_hall, _junction));

            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, saved);
            sim.WayFound += (from, to) => _found.Add("announced");
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            Assert.That(sim.IsOnMap(_junction), Is.True, "known from the save");
            RunSeconds(sim, 2); // searched anyway

            Assert.That(_found, Is.Empty);
            Assert.That(sim.DestinationsFrom(_hall), Is.EqualTo(new[] { _junction }));
        }

        [Test]
        public void AWayThatNeedsPerception_IsFound_WhenHerPerceptionRises_AfterTheSearch()
        {
            _way.needsPerception = 2;
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            RunSeconds(sim, 2);
            Assert.That(_found, Is.Empty, "searched far enough, but she doesn't see it");

            sim.Loop.AttributeXp[ClaraAttribute.Perception] = 20f; // just short of Perception 2 (20.5)
            sim.Schedule(MakeTask("Study the seam", 1f, ClaraAttribute.Perception), 1); // its 1 XP reaches it
            RunSeconds(sim, 1);

            Assert.That(_found, Is.EqualTo(new[] { "The hall → The junction" }), "announced when she sees it");
            Assert.That(sim.Persistent.FoundWays, Has.Member((_hall, _junction)), "and found for good");
            Assert.That(sim.UnreadStories, Has.Member(_story));
        }

        [Test]
        public void AnAsleepPerception_SeesNothingHidden_HoweverStrong()
        {
            _way.needsPerception = 2;
            var settings = MakeLoopSettings();
            settings.asleepAttributes.Add(ClaraAttribute.Perception);
            var sim = MakeSimulation(settings);
            sim.Schedule(_explore, 2);
            sim.BeginLoop();
            sim.Loop.AttributeXp[ClaraAttribute.Perception] = 300f; // strong, were it awake
            RunSeconds(sim, 2);

            Assert.That(_found, Is.Empty);
            Assert.That(sim.Persistent.FoundWays, Has.No.Member((_hall, _junction)));
        }

        [Test]
        public void AFoundWayThatIsShut_IsAnnounced_WhenASwitchOpensIt()
        {
            _way.startsOpen = false;
            var open = Make<SwitchDefinition>();
            open.trigger = SwitchTrigger.RoomExplored;
            open.roomToExplore = _hall;
            open.explorePercent = 100;
            open.opensWays.Add(new WayRef { from = _hall, to = _junction });
            _content.switches.Add(open);
            var sim = MakeSimulation();
            sim.Schedule(_explore, 2);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(_found, Is.EqualTo(new[] { "The hall → The junction" }));
        }
    }
}
