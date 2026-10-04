using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Warnings ahead on the plan (plan ui-024d): a queued entry that will be refused when it's reached
    /// is marked with why, where the walk along the plan can be sure; everywhere else it stays silent.
    /// </summary>
    public class PlanWarningTests : SimulationTestBase
    {
        private const string ShutKey = "reasons.shut_dark_corridor";

        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _hall, _dark, _vault;
        private ResourceDefinition _candle;
        private TaskDefinition _sweep, _once, _dust, _makeCandle, _light;

        // The hall (start) joins the dark, and a shut door to the vault. Candles are made in the hall
        // and lit in the dark.
        [SetUp]
        public void SetUp()
        {
            _candle = MakeObject("Candle", max: 5);
            _sweep = MakeTask("Sweep", 1f);
            _once = MakeTask("Wind the clock", 1f);
            _once.oncePerRun = true;
            _dust = MakeTask("Dust", 1f);
            _makeCandle = MakeGatherTask("Make a candle", 1f, _candle);
            _light = MakeTask("Light a candle", 1f);
            _light.needs.Add(new ResourceAmount { resource = _candle, amount = 1 });

            _hall = MakeNode("The Hall");
            _dark = MakeNode("The Dark");
            _vault = MakeNode("The Vault");
            Join(_hall, _dark);
            Join(_hall, _vault, startsOpen: false);
            _hall.tasks.AddRange(new[] { _sweep, _once, _makeCandle });
            _dark.tasks.AddRange(new[] { _dust, _light });

            _content = MakePlaces(_hall, _dark, _vault);
            _content.tasks.AddRange(new[] { _sweep, _once, _dust, _makeCandle, _light });
            _settings = MakeLoopSettings();
        }

        private Simulation NewGame() => new Simulation(_settings, TicksPerSecond, _content);

        private static string WarningAt(Simulation sim, int index) => sim.PlanWarnings()[index];

        private string NeedsACandle => Reason("needs", ("amount", 1), ("item", "Candle"));

        [Test]
        public void ATripThroughAShutWay_IsWarned()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_vault);

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("way_shut")));
            Assert.That(sim.PlanWarningCount, Is.EqualTo(1));
        }

        [Test]
        public void ShutShownWay_WarnsWithItsMessage()
        {
            var way = _hall.ways.Find(w => w.to == _vault);
            way.showWhileShut = true;
            way.shutMessageKey = ShutKey;
            var sim = NewGame();
            sim.ScheduleTrip(_vault);

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("shut_dark_corridor")));
        }

        [Test]
        public void AnEntryNeedingAnItemNothingGives_IsWarned()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);

            Assert.That(WarningAt(sim, 0), Is.Null, "the trip is fine");
            Assert.That(WarningAt(sim, 1), Is.EqualTo(NeedsACandle));
        }

        [Test]
        public void AnEntryNeedingAnItemThatAnEarlierEntryGives_IsNot()
        {
            var sim = NewGame();
            sim.Schedule(_makeCandle, 1);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);

            Assert.That(sim.PlanWarningCount, Is.EqualTo(0));
        }

        [Test]
        public void AnEntryNeedingAnItemThatThisRoomCanSupply_IsNot()
        {
            _dark.tasks.Add(_makeCandle); // the queue would put Make a candle on top when she gets there
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);

            Assert.That(sim.PlanWarningCount, Is.EqualTo(0));
        }

        [Test]
        public void AnEntryNeedingAnItemThatIsPacked_IsNot()
        {
            var lantern = MakeObject("Lantern", max: 1, lasts: ResourceLifetime.Carried);
            var shine = MakeTask("Shine the lantern", 1f);
            shine.needs.Add(new ResourceAmount { resource = lantern, amount = 1 });
            _dark.tasks.Add(shine);
            _content.tasks.Add(shine);
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(shine, 1);
            Assert.That(WarningAt(sim, 1), Is.EqualTo(Reason("needs", ("amount", 1), ("item", "Lantern"))), "not packed yet");

            sim.Persistent.Stash[lantern] = 1;
            sim.SetPacked(lantern, true);

            Assert.That(WarningAt(sim, 1), Is.Null);
        }

        [Test]
        public void ASecondOncePerRunTask_IsWarned()
        {
            var sim = NewGame();
            sim.Schedule(_once, 1);
            sim.Schedule(_sweep, 1);
            sim.Schedule(_once, 1);

            Assert.That(WarningAt(sim, 0), Is.Null);
            Assert.That(WarningAt(sim, 2), Is.EqualTo(Reason("done_this_run")));
        }

        [Test]
        public void ALockedTask_IsWarned()
        {
            _sweep.startsUnlocked = false;
            var sim = NewGame();
            sim.Schedule(_sweep, 1);

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("no_longer_possible")));
        }

        [Test]
        public void AnUnlearnedHue_IsWarned()
        {
            SetCost(_sweep, 5f, (CostSource.Ruby, 100f));
            var sim = NewGame();
            sim.Schedule(_sweep, 1);

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("needs_pool", ("pool", CostSource.Ruby))));
        }

        [Test]
        public void ATaskNotOfferedInThePlannedRoom_IsWarned()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_sweep, 1); // a hall task, queued after walking into the dark

            Assert.That(WarningAt(sim, 1), Is.EqualTo(Reason("not_here", ("room", GameText.TitleInSentence("The Dark")))));
        }

        [Test]
        public void AfterAnEntryThatFlipsASwitch_ItIsSilent()
        {
            // Sweeping flips a switch: it could open the vault, so nothing after it is sure.
            var @switch = Make<SwitchDefinition>();
            @switch.trigger = SwitchTrigger.TasksCompletedInOneRun;
            @switch.requiredTasks.Add(_sweep);
            _content.switches.Add(@switch);
            var sim = NewGame();
            sim.ScheduleTrip(_vault);
            sim.Schedule(_sweep, 1);
            sim.ScheduleTrip(_vault);

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("way_shut")), "before the switch: still warned");
            Assert.That(WarningAt(sim, 2), Is.Null);
        }

        [Test]
        public void AStatGate_IsSilent()
        {
            _sweep.requiresAttributes.Add(new TaskDefinition.AttributeRequirement { attribute = ClaraAttribute.Perception, level = 5 });
            var sim = NewGame();
            sim.Schedule(_sweep, 1);

            Assert.That(sim.PlanWarningCount, Is.EqualTo(0), "her level rises during the run");
        }

        [Test]
        public void BetweenRuns_LastRunsFloorAndDoneTasks_AreIgnored()
        {
            var sim = NewGame();
            sim.BeginLoop();
            sim.Schedule(_once, 1);
            RunSeconds(sim, 2);
            sim.Loop.Floor[_dark] = new Dictionary<ResourceDefinition, int> { [_candle] = 1 };
            sim.EndRunEarly();

            sim.Schedule(_once, 1);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);

            Assert.That(WarningAt(sim, 0), Is.Null, "done last run, not this one");
            Assert.That(WarningAt(sim, 2), Is.EqualTo(NeedsACandle), "last run's floor is gone");
        }

        [Test]
        public void DuringARun_AFloorElsewhere_Counts()
        {
            var sim = NewGame();
            sim.BeginLoop();
            KeepBusy(sim, 5f);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);
            Assert.That(WarningAt(sim, 2), Is.EqualTo(NeedsACandle));

            sim.Loop.Floor[_dark] = new Dictionary<ResourceDefinition, int> { [_candle] = 1 };
            sim.RemoveFromQueue(1); // any change to the queue: the warnings are worked out again
            sim.ScheduleTrip(_dark);

            Assert.That(WarningAt(sim, 2), Is.Null);
        }

        [Test]
        public void AfterATripRefusedForAnItem_ItIsSilent()
        {
            // The door to the vault is open but needs a key nothing gives: she'll stay in the hall,
            // though the plan's stops show her in the vault. Where she is after it isn't sure.
            var key = MakeObject("Key", max: 1);
            _hall.ways.Find(way => way.to == _vault).startsOpen = true;
            _hall.ways.Find(way => way.to == _vault).needs.Add(new ResourceAmount { resource = key, amount = 1 });
            var sim = NewGame();
            sim.ScheduleTrip(_vault);
            sim.Schedule(_sweep, 1); // fine in the hall, where she'll really be

            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("needs", ("amount", 1), ("item", "Key"))));
            Assert.That(WarningAt(sim, 1), Is.Null);
        }

        [Test]
        public void WaivedWay_NoMissingItemWarning()
        {
            var key = MakeObject("Key", max: 1);
            var toVault = _hall.ways.Find(way => way.to == _vault);
            toVault.startsOpen = true;
            toVault.needs.Add(new ResourceAmount { resource = key, amount = 1 });
            var waive = Make<SwitchDefinition>();
            waive.trigger = SwitchTrigger.TasksCompletedInOneRun;
            waive.requiredTasks.Add(_sweep);
            waive.waivesWayNeeds.Add(new WayRef { from = _hall, to = _vault });
            _content.switches.Add(waive);
            var sim = NewGame();
            sim.ScheduleTrip(_vault);
            Assert.That(WarningAt(sim, 0), Is.EqualTo(Reason("needs", ("amount", 1), ("item", "Key"))), "before the waiver");
            sim.ClearQueue();
            sim.BeginLoop();
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 1.5f);
            sim.EndRunEarly();

            sim.ScheduleTrip(_vault);

            Assert.That(sim.PlanWarningCount, Is.EqualTo(0));
        }

        [Test]
        public void AWarnedOncePerRunTask_DoesntCountAsDone()
        {
            _once.needs.Add(new ResourceAmount { resource = MakeObject("Oil"), amount = 1 });
            var sim = NewGame();
            sim.Schedule(_once, 1);
            sim.Schedule(_sweep, 1);
            sim.Schedule(_once, 1);

            Assert.That(WarningAt(sim, 2), Is.EqualTo(Reason("needs", ("amount", 1), ("item", "Oil"))), "refused for the oil, not as done");
        }

        [Test]
        public void ATripThroughAHiddenWay_ASearchBeforeItMayFind_IsSilent()
        {
            var hidden = Join(_dark, _vault);
            hidden.foundAtExplored = 50;
            _dark.exploresToFill = 4;
            _content.exploreVerb = MakeTask("Search", 1f);
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.ScheduleTrip(_vault);
            Assert.That(WarningAt(sim, 1), Is.EqualTo(Reason("way_not_found")), "no search planned");

            sim.RemoveFromQueue(1);
            sim.Schedule(_content.exploreVerb, 1);
            sim.ScheduleTrip(_vault);

            Assert.That(WarningAt(sim, 2), Is.Null);
        }

        [Test]
        public void AnActionASearchBeforeItMayFind_IsNotWarned()
        {
            var polish = MakeTask("Polish the mirror", 1f);
            _dark.foundBySearching.Add(new RoomFind { task = polish, atSearched = 50 });
            _dark.exploresToFill = 4;
            _content.tasks.Add(polish);
            _content.exploreVerb = MakeTask("Search", 1f);
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(polish, 1);
            Assert.That(WarningAt(sim, 1), Is.EqualTo(Reason("not_here", ("room", GameText.TitleInSentence("The Dark")))), "not found yet");

            sim.RemoveFromQueue(1);
            sim.Schedule(_content.exploreVerb, 1);
            sim.Schedule(polish, 1);

            Assert.That(WarningAt(sim, 2), Is.Null);
        }

        [Test]
        public void AfterAnEntryGivingWhatASwitchWatches_ItIsSilent()
        {
            var @switch = Make<SwitchDefinition>();
            @switch.trigger = SwitchTrigger.ResourceReached;
            @switch.resourceToHold = _candle;
            @switch.triggerAmount = 3;
            _content.switches.Add(@switch);
            var sim = NewGame();
            sim.Schedule(_makeCandle, 1);
            sim.ScheduleTrip(_vault);

            Assert.That(WarningAt(sim, 1), Is.Null);
        }

        [Test]
        public void DuringARun_MakingWhatANeedWaitsOn_ClearsItsWarning()
        {
            var sim = NewGame();
            sim.BeginLoop();
            KeepBusy(sim, 1f);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);
            Assert.That(WarningAt(sim, 2), Is.EqualTo(NeedsACandle));

            // A candle made on top of the plan (Play), done before the rest.
            sim.PlayNow(_makeCandle, 1);
            RunSeconds(sim, 1.5f);

            Assert.That(sim.Loop.CountOf(_candle), Is.EqualTo(1));
            Assert.That(sim.Queue.Entries.Exists(entry => entry.Task == _makeCandle), Is.False, "done and gone: the candle she holds answers the need");
            Assert.That(WarningAt(sim, sim.Queue.Count - 1), Is.Null);
        }

        [Test]
        public void FlippingASwitchThatUnlocksATask_ClearsItsWarning()
        {
            _dust.startsUnlocked = false;
            var @switch = Make<SwitchDefinition>();
            @switch.trigger = SwitchTrigger.TasksCompletedInOneRun;
            @switch.requiredTasks.Add(_once);
            @switch.unlocksTasks.Add(_dust);
            _content.switches.Add(@switch);
            var sim = NewGame();
            sim.BeginLoop();
            KeepBusy(sim, 5f);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_dust, 1);
            Assert.That(WarningAt(sim, 2), Is.EqualTo(Reason("no_longer_possible")));

            sim.PlayNow(_once, 1); // flips the switch when done
            RunSeconds(sim, 1.5f);

            Assert.That(sim.IsUnlocked(_dust), Is.True);
            Assert.That(sim.PlanWarningCount, Is.EqualTo(0));
        }

        [Test]
        public void DuringARun_TheTopEntry_IsNotWarned()
        {
            _dust.startsUnlocked = false;
            var sim = NewGame();
            sim.BeginLoop();
            KeepBusy(sim, 5f);
            sim.Schedule(_dust, 1); // behind the wait, so it's queued, not refused
            Assert.That(WarningAt(sim, 1), Is.EqualTo(Reason("no_longer_possible")));

            sim.RemoveFromQueue(0);

            Assert.That(WarningAt(sim, 0), Is.Null, "the real check owns the top entry");
        }

        [Test]
        public void TheRunEnding_RefreshesTheWarnings()
        {
            var sim = NewGame();
            sim.BeginLoop();
            sim.Schedule(_once, 1);
            RunSeconds(sim, 1.5f);
            KeepBusy(sim, 5f);
            sim.Schedule(_once, 1);
            Assert.That(sim.PlanWarningCount, Is.EqualTo(1), "done this run");

            sim.EndRunEarly();

            Assert.That(sim.PlanWarningCount, Is.EqualTo(0), "read again with nothing queued between runs");
        }

        // ---------- By room (the map's tags, plan ui-033) ----------

        [Test]
        public void RoomWarnings_ListsReasonsOfStopsInThatRoom()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1); // needs a candle nothing gives

            CollectionAssert.AreEqual(new[] { NeedsACandle }, sim.RoomWarnings(_dark));
            Assert.That(sim.RoomWarnings(_hall), Is.Empty, "the hall's stop has nothing warned");
        }

        [Test]
        public void RoomWarnings_EmptyWhenPlanClean()
        {
            var sim = NewGame();
            sim.Schedule(_makeCandle, 1);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);

            Assert.That(sim.RoomWarnings(_dark), Is.Empty);
            Assert.That(sim.RoomWarnings(_hall), Is.Empty);
        }

        [Test]
        public void RoomWarnings_ATripTheWayCantMake_CountsInTheRoomSheWouldLeave()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_vault); // the door is shut

            CollectionAssert.AreEqual(new[] { Reason("way_shut") }, sim.RoomWarnings(_hall));
            Assert.That(sim.RoomWarnings(_vault), Is.Empty, "she never gets there");
        }

        [Test]
        public void RoomWarnings_ATripRefusedForAnItem_CountsInTheRoomItEnters()
        {
            var key = MakeObject("Key");
            _hall.ways.Find(way => way.to == _vault).needs.Add(new ResourceAmount { resource = key, amount = 1 });
            _hall.ways.Find(way => way.to == _vault).startsOpen = true;
            var sim = NewGame();
            sim.ScheduleTrip(_vault);

            CollectionAssert.AreEqual(new[] { Reason("needs", ("amount", 1), ("item", "Key")) }, sim.RoomWarnings(_vault));
            Assert.That(sim.RoomWarnings(_hall), Is.Empty);
        }

        [Test]
        public void RoomWarnings_FollowsQueueEdit()
        {
            var sim = NewGame();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_light, 1);
            Assert.That(sim.RoomWarnings(_dark), Has.Count.EqualTo(1));

            sim.RemoveFromQueue(1);

            Assert.That(sim.RoomWarnings(_dark), Is.Empty);
        }
    }
}
