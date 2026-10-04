using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Rooms, the ways between them, the Travel verb, and tasks that belong to one room.</summary>
    public class PlacesTests : SimulationTestBase
    {
        // Any real key will do: the tests check the way's own message is the one given.
        private const string ShutKey = "reasons.shut_dark_corridor";

        private readonly List<string> _log = new List<string>();

        private GameContent _content;
        private TaskDefinition _travel, _search, _anywhere;
        private NodeDefinition _entrance, _junction, _corridor;

        // The hall mirror ↔ the junction ↔ the corridor, where a search can be done.
        // Travel: 1s, 10 from all pools.
        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _travel = MakeTask("Travel", 1f);
            SetCost(_travel, 10f, (CostSource.AllPools, 100f));

            _entrance = MakeNode("The hall mirror");
            _junction = MakeNode("The junction");
            _corridor = MakeNode("The corridor");
            Join(_entrance, _junction);
            Join(_junction, _corridor);

            _search = MakeTask("Search", 1f);
            _corridor.tasks.Add(_search);
            _anywhere = MakeTask("Think", 1f); // listed at no room

            _content = Make<GameContent>();
            _content.travelVerb = _travel;
            _content.startNode = _entrance;
            _content.nodes.AddRange(new[] { _entrance, _junction, _corridor });
            _content.tasks.AddRange(new[] { _search, _anywhere });
        }

        private Simulation MakeSimulation(LoopSettings settings = null)
        {
            var sim = new Simulation(settings ?? MakeLoopSettings(), TicksPerSecond, _content);
            sim.TaskCompleted += task => _log.Add($"done {task.displayName}");
            sim.Arrived += (node, _) => _log.Add($"at {node.displayName}");
            sim.TaskSkipped += (task, reason) => _log.Add($"skip {task.displayName}: {reason}");
            sim.ActionRefused += (task, _, reason) => _log.Add($"skip {task.displayName}: {reason}"); // refused as it's asked for: reads the same
            return sim;
        }

        // A shut way marked Show While Shut (plan 032a): the room behind it shows once the way is found, and a trip
        // there is refused with the way's own message rather than "the way is shut".
        private Way AddSealedRoom(out NodeDefinition sealedRoom, bool showWhileShut, int foundAtExplored)
        {
            sealedRoom = MakeNode("The sealed room");
            _content.nodes.Add(sealedRoom);
            var way = Join(_entrance, sealedRoom, startsOpen: false);
            way.showWhileShut = showWhileShut;
            way.shutMessageKey = ShutKey;
            way.foundAtExplored = foundAtExplored;
            return way;
        }

        [Test]
        public void ShutWayShownWhileShut_OnMap_RefusedWithMessage()
        {
            AddSealedRoom(out var sealedRoom, showWhileShut: true, foundAtExplored: 0);
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.IsOnMap(sealedRoom), Is.True);
            Assert.That(sim.CanScheduleTripTo(sealedRoom), Is.False);
            Assert.That(sim.WhyCantScheduleTripTo(sealedRoom), Is.EqualTo(Reason("shut_dark_corridor")));
            sim.PlayTripNow(sealedRoom);
            Assert.That(_log, Has.Count.EqualTo(1));
            Assert.That(_log[0], Does.EndWith(Reason("shut_dark_corridor")));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void TryScheduleTrip_ToAnOpenRoom_QueuesTheTripSilently()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();

            sim.TryScheduleTrip(_junction);

            Assert.That(sim.Queue.Count, Is.EqualTo(1));
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void TryScheduleTrip_ToARoomThatCantBeReached_SaysWhyAndQueuesNothing()
        {
            AddSealedRoom(out var sealedRoom, showWhileShut: true, foundAtExplored: 0);
            var sim = MakeSimulation();
            sim.BeginLoop();

            sim.TryScheduleTrip(sealedRoom);

            Assert.That(sim.Queue.Count, Is.EqualTo(0));
            Assert.That(_log, Is.EqualTo(new[] { $"skip Travel: {Reason("shut_dark_corridor")}" }), "the toast's reason");
        }

        [Test]
        public void RefusedTrip_ReportsItsDestination()
        {
            AddSealedRoom(out var sealedRoom, showWhileShut: true, foundAtExplored: 0);
            var sim = MakeSimulation();
            sim.BeginLoop();
            var destinations = new List<NodeDefinition>();
            sim.ActionRefused += (_, destination, _) => destinations.Add(destination);

            sim.TryScheduleTrip(sealedRoom); // refused as the click lands
            Assert.That(destinations, Is.EqualTo(new[] { sealedRoom }));

            sim.PlayTripNow(sealedRoom); // refused as it would start: that reason already names the room
            Assert.That(destinations, Is.EqualTo(new NodeDefinition[] { sealedRoom, null }));
        }

        [Test]
        public void TryScheduleTrip_ToTheRoomSheWouldBeIn_SaysNothing()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();

            sim.TryScheduleTrip(_entrance);

            Assert.That(sim.Queue.Count, Is.EqualTo(0));
            Assert.That(_log, Is.Empty, "clicking where she is asks for nothing, so there is nothing to refuse");
        }

        [Test]
        public void ShutWayShown_GivesItsMessage_FromTwoRoomsAway()
        {
            var sealedRoom = MakeNode("The sealed room");
            _content.nodes.Add(sealedRoom);
            var way = Join(_junction, sealedRoom, startsOpen: false);
            way.showWhileShut = true;
            way.shutMessageKey = ShutKey;
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.WhyCantScheduleTripTo(sealedRoom), Is.EqualTo(Reason("shut_dark_corridor")), "from the entrance, one room off");
        }

        [Test]
        public void ShutWayWithoutTheFlag_StaysOffTheMap()
        {
            AddSealedRoom(out var sealedRoom, showWhileShut: false, foundAtExplored: 0);
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.IsOnMap(sealedRoom), Is.False);
        }

        [Test]
        public void ShutWayShown_NotOnMapUntilFound()
        {
            var way = AddSealedRoom(out var sealedRoom, showWhileShut: true, foundAtExplored: 100);
            var sim = MakeSimulation();
            sim.BeginLoop();
            Assert.That(sim.IsOnMap(sealedRoom), Is.False, "hidden until searched");

            sim.Persistent.FoundWays.Add((_entrance, way.to));

            Assert.That(sim.IsOnMap(sealedRoom), Is.True);
            Assert.That(sim.WhyCantScheduleTripTo(sealedRoom), Is.EqualTo(Reason("shut_dark_corridor")));
        }

        [Test]
        public void Arrived_SaysWhetherItsTheFirstEntryThisRun()
        {
            var sim = MakeSimulation();
            var flags = new List<bool>();
            sim.Arrived += (_, first) => flags.Add(first);
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_entrance);
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();

            RunSeconds(sim, 4);

            // The junction is new; the entrance is the start room, which never is; the junction again isn't.
            Assert.That(flags, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void EveryRun_BeginsAtTheStartRoom()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();
            RunSeconds(sim, 2);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));

            sim.EndRunEarly();
            sim.BeginLoop();

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_entrance));
        }

        [Test]
        public void Trips_MoveHer_AndTasksWorkOnlyWhereTheyBelong()
        {
            var sim = MakeSimulation();
            sim.Schedule(_search, 1); // not at the entrance
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_search, 1);
            sim.BeginLoop();

            RunSeconds(sim, 4);

            Assert.That(_log, Is.EqualTo(new[]
            {
                $"skip Search: {Reason("not_here", ("room", "the hall mirror"))}",
                "done Travel", "at The junction",
                "done Travel", "at The corridor",
                "done Search",
            }));
        }

        [Test]
        public void ScheduleTrip_FarRoom_AddsWholeWalk()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();

            Assert.That(sim.CanScheduleTripTo(_corridor), Is.True, "two ways off, over found ways");
            sim.ScheduleTrip(_corridor);

            Assert.That(sim.Queue.Count, Is.EqualTo(2));
            Assert.That(sim.Queue.Entries[0].Destination, Is.EqualTo(_junction));
            Assert.That(sim.Queue.Entries[1].Destination, Is.EqualTo(_corridor));
            Assert.That(sim.PlannedEndNode, Is.EqualTo(_corridor));
        }

        [Test]
        public void ABothWaysWay_CanBeWalkedBack()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_entrance); // the way is listed at the entrance only
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_entrance));
        }

        [Test]
        public void AOneWayValve_CantBeWalkedBack()
        {
            _entrance.ways[0].bothWays = false;
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_entrance);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(_log, Has.Member($"skip Travel: {Reason("trip", ("room", "the hall mirror"), ("reason", Reason("no_way", ("room", "the junction"))))}"));
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));
        }

        [Test]
        public void ADoorWithACondition_StaysShut_UntilSheHasWhatItNeeds()
        {
            var ring = MakeResource("Roland's ring");
            _junction.ways[0].needs.Add(new ResourceAmount { resource = ring, amount = 1 });
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_corridor);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(_log, Has.Member($"skip Travel: {Reason("trip", ("room", "the corridor"), ("reason", Reason("needs", ("amount", 1), ("item", "Roland's ring"))))}"));
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));
        }

        // The junction → corridor way needs the ring until a switch (done by thinking) waives it.
        private SwitchDefinition RingWayWaivedByThinking()
        {
            var ring = MakeResource("Roland's ring");
            _junction.ways[0].needs.Add(new ResourceAmount { resource = ring, amount = 1 });
            var waive = Make<SwitchDefinition>();
            waive.trigger = SwitchTrigger.TasksCompletedInOneRun;
            waive.requiredTasks.Add(_anywhere);
            waive.waivesWayNeeds.Add(new WayRef { from = _junction, to = _corridor });
            _content.switches.Add(waive);
            return waive;
        }

        [Test]
        public void WaivedWay_NeedsItem_UntilSwitchFlips()
        {
            RingWayWaivedByThinking();
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_corridor);
            sim.BeginLoop();
            RunSeconds(sim, 2);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction), "not waived yet: the ring is still needed");
            sim.EndRunEarly();

            sim.BeginLoop();
            sim.Schedule(_anywhere, 1); // flips the switch
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_corridor);
            RunSeconds(sim, 4);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_corridor));
        }

        [Test]
        public void WaivedWay_StaysOpen_AcrossRunsAndSave()
        {
            RingWayWaivedByThinking();
            var settings = MakeLoopSettings();
            var sim = MakeSimulation(settings);
            sim.Schedule(_anywhere, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);
            sim.EndRunEarly();

            var reopened = Reopen(sim, settings, _content);
            reopened.BeginLoop();
            reopened.ScheduleTrip(_junction);
            reopened.ScheduleTrip(_corridor);
            RunSeconds(reopened, 2);

            Assert.That(reopened.Loop.CurrentNode, Is.EqualTo(_corridor));
        }

        [Test]
        public void WaivedWay_OnlyWaivesThatWay()
        {
            RingWayWaivedByThinking();
            var stone = MakeResource("The stone");
            _entrance.ways[0].needs.Add(new ResourceAmount { resource = stone, amount = 1 });
            var sim = MakeSimulation();
            sim.Schedule(_anywhere, 1);
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();
            RunSeconds(sim, 2);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_entrance), "the hall mirror's way still needs the stone");
        }

        [Test]
        public void WaivedWay_NoLongerKeepsItsItemInUse()
        {
            // Kept knowledge that hides once used, needed only by the waived way: its job is done.
            var map = MakeResource("A map", ResourceLifetime.Forever, max: 1);
            map.hideOnceUsed = true;
            _junction.ways[0].needs.Add(new ResourceAmount { resource = map, amount = 1 });
            var watches = Make<SwitchDefinition>();
            watches.trigger = SwitchTrigger.ResourceReached;
            watches.resourceToHold = map;
            watches.triggerAmount = 1;
            var wave = MakeTask("Wave", 1f); // flips the waiver
            _content.tasks.Add(wave);
            var waive = Make<SwitchDefinition>();
            waive.trigger = SwitchTrigger.TasksCompletedInOneRun;
            waive.requiredTasks.Add(wave);
            waive.waivesWayNeeds.Add(new WayRef { from = _junction, to = _corridor });
            _content.switches.AddRange(new[] { watches, waive });
            _anywhere.gives.Add(new ResourceAmount { resource = map, amount = 1 }); // flips the watching switch
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_anywhere, 1);
            sim.Schedule(wave, 1);
            RunSeconds(sim, 1);
            Assert.That(sim.IsFlipped(watches), Is.True, "the knowledge switch flipped");
            Assert.That(sim.HasDoneItsJob(map), Is.False, "the way still needs it");

            RunSeconds(sim, 1);

            Assert.That(sim.HasDoneItsJob(map), Is.True);
        }

        [Test]
        public void AShutWay_OpensWhenASwitchFlips()
        {
            _entrance.ways[0].startsOpen = false;
            var open = Make<SwitchDefinition>();
            open.trigger = SwitchTrigger.TasksCompletedInOneRun;
            open.requiredTasks.Add(_anywhere);
            open.opensWays.Add(new WayRef { from = _entrance, to = _junction });
            _content.switches.Add(open);
            var sim = MakeSimulation();
            Assert.That(sim.DestinationsFrom(_entrance), Is.Empty);
            Assert.That(sim.IsOnMap(_junction), Is.False);

            sim.Schedule(_anywhere, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);

            Assert.That(sim.DestinationsFrom(_entrance), Is.EqualTo(new[] { _junction }));
            Assert.That(sim.IsOnMap(_junction), Is.True);
        }

        [Test]
        public void ASwitch_CanCloseAWay_AndClosingWins()
        {
            var shut = Make<SwitchDefinition>();
            shut.trigger = SwitchTrigger.TasksCompletedInOneRun;
            shut.requiredTasks.Add(_anywhere);
            shut.opensWays.Add(new WayRef { from = _junction, to = _corridor });
            shut.closesWays.Add(new WayRef { from = _corridor, to = _junction }); // named from the other end
            _content.switches.Add(shut);
            var sim = MakeSimulation();

            sim.Schedule(_anywhere, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);

            Assert.That(sim.DestinationsFrom(_junction), Is.EqualTo(new[] { _entrance }));
        }

        [Test]
        public void RoomModifiers_ChangeATripsCostAndTime()
        {
            _entrance.leaving = new ActionModifier { cost = 2f, time = 1f };
            _junction.entering = new ActionModifier { cost = 1.5f, time = 3f };
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();

            RunSeconds(sim, 2);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_entrance), "3s trip: not there yet");

            RunSeconds(sim, 1);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 10f * 2f * 1.5f).Within(0.01f));
        }

        [Test]
        public void AllPoolsCosts_SplitEvenlyAcrossTheUnlockedPools()
        {
            var settings = MakeLoopSettings();
            settings.pools.Add(HuePool(Hue.Amber, 50f));
            settings.pools.Add(HuePool(Hue.Citrine, 50f));
            settings.pools.Add(HuePool(Hue.Ruby, 50f, startsUnlocked: false));
            var sim = MakeSimulation(settings);
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();

            RunSeconds(sim, 1);

            Assert.That(sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(45f).Within(0.01f));
            Assert.That(sim.Loop.FindPool(Hue.Citrine).Current, Is.EqualTo(45f).Within(0.01f));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void AllPoolsCosts_WithNoPools_FallOnVitality()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.BeginLoop();

            RunSeconds(sim, 1);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(90f).Within(0.01f));
        }

        [Test]
        public void ATaskAtNoRoom_CanBeDoneAnywhere()
        {
            var sim = MakeSimulation();
            sim.Schedule(_anywhere, 1);
            sim.ScheduleTrip(_junction);
            sim.Schedule(_anywhere, 1);
            sim.BeginLoop();

            RunSeconds(sim, 3);

            Assert.That(_log, Is.EqualTo(new[] { "done Think", "done Travel", "at The junction", "done Think" }));
        }

        [Test]
        public void PlannedEndNode_IsWhereTheQueueLeavesHer()
        {
            var sim = MakeSimulation();
            Assert.That(sim.PlannedEndNode, Is.EqualTo(_entrance));

            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_corridor);
            Assert.That(sim.PlannedEndNode, Is.EqualTo(_corridor));
            Assert.That(sim.PlannedNodeAfter(1), Is.EqualTo(_junction));

            // Two rooms back: the whole walk is added (ui-015), so the plan ends at the entrance.
            sim.ScheduleTrip(_entrance);
            Assert.That(sim.Queue.Count, Is.EqualTo(4));
            Assert.That(sim.PlannedEndNode, Is.EqualTo(_entrance));

            // A trip that can't be made from where the queue leaves her is skipped in the plan.
            // (the entrance has no way straight to the corridor)
            sim.Queue.Entries.Add(new QueueEntry(sim.TravelVerb, _corridor));
            Assert.That(sim.PlannedEndNode, Is.EqualTo(_entrance));
        }

        [Test]
        public void TripsCanBeScheduled_ToAnyRoomReachableFromWhereTheQueueLeavesHer()
        {
            var sim = MakeSimulation();
            Assert.That(sim.NextStops(), Is.EqualTo(new[] { _junction }));
            Assert.That(sim.CanScheduleTripTo(_junction), Is.True);
            Assert.That(sim.CanScheduleTripTo(_corridor), Is.True, "two rooms away: the walk is added");
            Assert.That(sim.CanScheduleTripTo(_entrance), Is.False, "where the queue already leaves her");
            Assert.That(sim.CanScheduleTripTo(null), Is.False);

            sim.ScheduleTrip(_junction);

            Assert.That(sim.NextStops(), Is.EquivalentTo(new[] { _entrance, _corridor }));
            Assert.That(sim.CanScheduleTripTo(_corridor), Is.True, "the queue now ends at the junction");
        }

        [Test]
        public void TasksAt_ListsThatRoom_PlusTasksDoneAnywhere_ButNotTheTravelVerb()
        {
            var sim = MakeSimulation();

            Assert.That(sim.TasksAt(_junction), Is.EqualTo(new[] { _anywhere }));
            Assert.That(sim.TasksAt(_corridor), Is.EqualTo(new[] { _search, _anywhere }));
            Assert.That(sim.DestinationsFrom(_junction), Is.EquivalentTo(new[] { _corridor, _entrance }));
        }

        [Test]
        public void ATrip_IsMadeOnce_AndATripToWhereSheIs_IsSkipped()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_junction);
            sim.ScheduleTrip(_junction); // trips never merge
            sim.BeginLoop();

            RunSeconds(sim, 3);

            Assert.That(_log, Is.EqualTo(new[]
            {
                "done Travel", "at The junction",
                $"skip Travel: {Reason("trip", ("room", "the junction"), ("reason", Reason("already_there")))}",
            }));
        }

        [Test]
        public void Play_SendsHerOnATripAtOnce()
        {
            var sim = MakeSimulation();
            sim.Schedule(_anywhere); // thinking, forever
            sim.BeginLoop();
            RunSeconds(sim, 2);

            sim.PlayTripNow(_junction);
            RunSeconds(sim, 1);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_junction));
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(_anywhere), "and then back to thinking");
        }
    }
}
