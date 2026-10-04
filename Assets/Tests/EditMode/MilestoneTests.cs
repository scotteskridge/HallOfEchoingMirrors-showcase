using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Milestones (switches with a story): timed each run, and compared with the run before.</summary>
    public class MilestoneTests : SimulationTestBase
    {
        private readonly List<(ContentAsset milestone, float seconds, bool firstTime)> _reached =
            new List<(ContentAsset, float, bool)>();

        private GameContent _content;
        private TaskDefinition _search;
        private SwitchDefinition _found;

        // Searching (2s) flips "Found it", which has a story.
        [SetUp]
        public void SetUp()
        {
            _reached.Clear();
            _search = MakeTask("Search", 2f);
            _found = MakeMilestone("Found it");
            _found.trigger = SwitchTrigger.TasksCompletedInOneRun;
            _found.requiredTasks.Add(_search);

            _content = Make<GameContent>();
            _content.tasks.Add(_search);
            _content.switches.Add(_found);
        }

        private SwitchDefinition MakeMilestone(string title)
        {
            var milestone = Make<SwitchDefinition>();
            milestone.story = MakeStory($"{title}\nIt was there all along.");
            return milestone;
        }

        private Simulation MakeSimulation()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.MilestoneReached += (milestone, seconds, first) => _reached.Add((milestone, seconds, first));
            return sim;
        }

        /// <summary>One run: queue these (once each), run for a while, then end it.</summary>
        private static void Run(Simulation sim, float seconds, params TaskDefinition[] tasks)
        {
            foreach (var task in tasks)
                sim.Schedule(task, 1);
            sim.BeginLoop();
            RunSeconds(sim, seconds);
            sim.EndRunEarly();
        }

        [Test]
        public void TheFirstTime_IsTimed_AndSaysSo()
        {
            var sim = MakeSimulation();

            Run(sim, 3f, _search);

            Assert.That(sim.Loop.Milestones.Count, Is.EqualTo(1));
            Assert.That(sim.Loop.Milestones[0].milestone, Is.EqualTo(_found));
            Assert.That(sim.Loop.Milestones[0].seconds, Is.EqualTo(2f).Within(0.01f));
            Assert.That(_reached[0].firstTime, Is.True);
        }

        [Test]
        public void ReachedAgainInALaterRun_IsTimedAgain_AndComparedWithTheRunBefore()
        {
            var sim = MakeSimulation();
            Run(sim, 3f, _search); // 2s

            sim.Schedule(MakeTask("Look around", 1f), 1);
            sim.Schedule(_search, 1);
            sim.BeginLoop();
            Assert.That(sim.LastRunTimeOf(_found), Is.EqualTo(2f).Within(0.01f), "the run before");
            RunSeconds(sim, 4f);

            Assert.That(sim.Loop.Milestones[0].seconds, Is.EqualTo(3f).Within(0.01f), "1s looking, then 2s searching");
            Assert.That(_reached[1].firstTime, Is.False, "a benchmark: its story isn't new");
        }

        [Test]
        public void AMilestoneThatStaysTrue_IsntTimedAgain()
        {
            var mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            var enough = MakeMilestone("Enough mirrors");
            enough.trigger = SwitchTrigger.ResourceReached;
            enough.resourceToHold = mirrors;
            enough.triggerAmount = 1;
            _content.switches.Add(enough);
            _search.gives.Add(new ResourceAmount { resource = mirrors, amount = 1 });
            var sim = MakeSimulation();
            Run(sim, 3f, _search);

            Run(sim, 3f, _search);

            Assert.That(sim.Loop.HasReached(enough), Is.False, "she already had them at the start: nothing to time");
            Assert.That(sim.Loop.HasReached(_found), Is.True, "while this one can be timed again");
        }

        [Test]
        public void RoomExploredSwitch_NotReachedAgain()
        {
            var room = MakeNode("The dark");
            room.exploresToFill = 1;
            var explore = MakeTask("Explore", 1f);
            var places = MakePlaces(room);
            places.exploreVerb = explore;
            var discovery = MakeMilestone("Something in the dark");
            discovery.trigger = SwitchTrigger.RoomExplored;
            discovery.roomToExplore = room;
            discovery.explorePercent = 100;
            places.switches.Add(discovery);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, places);

            Run(sim, 2f, explore);
            Assert.That(sim.Loop.HasReached(discovery), Is.True, "the first time");

            sim.BeginLoop();
            KeepBusy(sim, 1f);
            RunSeconds(sim, 2f);
            Assert.That(sim.Loop.HasReached(discovery), Is.False, "the bar is kept: it fires once, ever");
        }

        [Test]
        public void ARunThatMissesAMilestone_LeavesTheNextOneNothingToCompare()
        {
            var sim = MakeSimulation();
            Run(sim, 3f, _search);
            Run(sim, 1f); // didn't search

            sim.BeginLoop();

            Assert.That(sim.LastRunTimeOf(_found), Is.Null);
        }

        [Test]
        public void LastRunsTimes_SurviveSavingAndLoading()
        {
            var sim = MakeSimulation();
            Run(sim, 3f, _search);

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, _content, warnings);
            var loaded = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, restored);

            Assert.That(warnings, Is.Empty);
            Assert.That(loaded.LastRunTimeOf(_found), Is.EqualTo(2f).Within(0.01f));
        }

        // ---------- Room entry ----------

        private NodeDefinition _start, _hall, _cellar;
        private GameContent _rooms;

        // The start room, a hall and a cellar in a row; Travel takes 1s. Only the cellar has a story.
        private Simulation MakeRoomSim()
        {
            _start = MakeNode("The start");
            _hall = MakeNode("The hall");
            _cellar = MakeNode("The cellar");
            Join(_start, _hall);
            Join(_hall, _cellar);
            _cellar.firstEntry = MakeStory("Down below\nIt is cold here.");
            _rooms = MakePlaces(_start, _hall, _cellar);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _rooms);
            sim.MilestoneReached += (milestone, seconds, first) => _reached.Add((milestone, seconds, first));
            return sim;
        }

        [Test]
        public void EnteringARoom_IsTimed_FirstTime()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_hall);
            sim.BeginLoop();

            RunSeconds(sim, 2f);

            Assert.That(sim.Loop.Milestones.Count, Is.EqualTo(1));
            Assert.That(sim.Loop.Milestones[0].milestone, Is.EqualTo(_hall));
            Assert.That(sim.Loop.Milestones[0].seconds, Is.EqualTo(1f).Within(0.01f));
            Assert.That(_reached[0].firstTime, Is.True);
            Assert.That(sim.IsFirstTimeEver(_hall), Is.True);
        }

        [Test]
        public void EnteringARoom_NextRun_ComparesToLastRun()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_hall);
            sim.BeginLoop();
            RunSeconds(sim, 2f);
            sim.EndRunEarly();

            sim.Schedule(MakeTask("Look around", 2f), 1);
            sim.ScheduleTrip(_hall);
            sim.BeginLoop();
            RunSeconds(sim, 4f);

            Assert.That(sim.LastRunTimeOf(_hall), Is.EqualTo(1f).Within(0.01f), "last run's time is found for a room key");
            Assert.That(sim.Loop.Milestones[0].seconds, Is.EqualTo(3f).Within(0.01f));
            Assert.That(_reached[1].firstTime, Is.False, "she has been there before");
            Assert.That(sim.IsFirstTimeEver(_hall), Is.False);
        }

        [Test]
        public void StartRoom_NeverFires()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_hall);
            sim.ScheduleTrip(_start);
            sim.BeginLoop();

            RunSeconds(sim, 3f);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_start), "she did walk back");
            Assert.That(sim.Loop.Milestones.ConvertAll(m => m.milestone), Is.EqualTo(new[] { _hall }));
        }

        [Test]
        public void SecondEntrySameRun_DoesNothing()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_hall);
            sim.ScheduleTrip(_start);
            sim.ScheduleTrip(_hall);
            sim.BeginLoop();

            RunSeconds(sim, 4f);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_hall));
            Assert.That(sim.Loop.Milestones.Count, Is.EqualTo(1));
            Assert.That(_reached.Count, Is.EqualTo(1));
        }

        [Test]
        public void EachHopOfATrip_Counts()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_cellar); // through the hall
            sim.BeginLoop();

            RunSeconds(sim, 3f);

            Assert.That(sim.Loop.Milestones.ConvertAll(m => m.milestone), Is.EqualTo(new ContentAsset[] { _hall, _cellar }));
        }

        [Test]
        public void RunEndingOnArrival_StillRecordsEntry()
        {
            var sim = MakeRoomSim();
            var travel = _rooms.travelVerb;
            SetCost(travel, 100f, (CostSource.Vitality, 100f)); // the one trip takes all she has
            sim.ScheduleTrip(_hall);
            sim.BeginLoop();

            RunSeconds(sim, 3f);

            Assert.That(sim.Phase, Is.Not.EqualTo(LoopPhase.Running), "the run ended");
            Assert.That(sim.Persistent.RunHistory[0].TimeOf(_hall), Is.Not.Null, "the entry is in the run record");
        }

        [Test]
        public void FirstEntryStory_RevealedOnce_EvenAfterReload()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_cellar);
            sim.BeginLoop();
            RunSeconds(sim, 3f);
            Assert.That(sim.Persistent.Journal, Is.EqualTo(new[] { _cellar.firstEntry }), "opened on the first entry");

            // Saved mid-run, then reopened, as closing and restarting the game would.
            var warnings = new List<string>();
            var reopened = Reopen(sim, MakeLoopSettings(), _rooms, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.Persistent.Journal, Is.EqualTo(new[] { _cellar.firstEntry }));
            Assert.That(reopened.Persistent.UnreadStories, Is.EqualTo(new[] { _cellar.firstEntry }), "still waiting to be read");
            Assert.That(reopened.Loop.HasReached(_cellar), Is.True, "this run's entries survive");
            Assert.That(reopened.IsFirstTimeEver(_cellar), Is.True, "and so does which were the first ever");

            reopened.MarkRead(_cellar.firstEntry);
            reopened.EndRunEarly();
            reopened.ScheduleTrip(_cellar);
            reopened.BeginLoop();
            RunSeconds(reopened, 3f);

            Assert.That(reopened.Persistent.UnreadStories, Is.Empty, "not shown a second time");
            Assert.That(reopened.Persistent.Journal.Count, Is.EqualTo(1));
        }

        [Test]
        public void RoomWithoutStory_StillTimed()
        {
            var sim = MakeRoomSim();
            sim.ScheduleTrip(_hall); // no firstEntry
            sim.BeginLoop();

            RunSeconds(sim, 2f);

            Assert.That(sim.Loop.HasReached(_hall), Is.True);
            Assert.That(sim.Persistent.Journal, Is.Empty);
            Assert.That(Simulation.StoryOf(_hall), Is.Null);
            Assert.That(Simulation.NameOf(_hall), Is.EqualTo("The hall"));
        }

        // ---------- Best-ever times (plan 042) ----------

        [Test]
        public void BestTime_IsTheLowestOverEveryRun()
        {
            var sim = MakeSimulation();
            var history = sim.Persistent.RunHistory;
            history.Add(new RunRecord(1, 900, 3, LoopEndReason.WalkedOut, new[] { ((ContentAsset)_found, 50f) }));
            history.Add(new RunRecord(2, 600, 2, LoopEndReason.Exhausted, new[] { ((ContentAsset)_found, 30f) }));
            history.Add(new RunRecord(3, 100, 1, LoopEndReason.EndedByPlayer, new[] { ((ContentAsset)_found, 20f) }));
            history.Add(new RunRecord(4, 800, 4, LoopEndReason.WalkedOut)); // not reached: doesn't count as 0

            Assert.That(sim.BestTimeOf(_found), Is.EqualTo(20f), "an ended-early run counts: the time was reached");

            history.RemoveAt(2);
            Assert.That(sim.BestTimeOf(_found), Is.EqualTo(30f), "and so does a collapse");
        }

        [Test]
        public void BestTime_IsNull_ForAMilestoneNeverReached()
        {
            var sim = MakeSimulation();
            Run(sim, 1f); // a run that reached nothing

            Assert.That(sim.Persistent.RunHistory, Has.Count.EqualTo(1), "set-up");
            Assert.That(sim.BestTimeOf(_found), Is.Null);
        }
    }
}
