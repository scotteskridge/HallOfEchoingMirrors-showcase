using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The common Search verb (Explore in code): one long action per room whose bar is kept for good,
    /// what each step gives, actions found by searching, and discoveries.
    /// </summary>
    public class ExploringTests : SimulationTestBase
    {
        private readonly List<string> _log = new List<string>();

        private GameContent _content;
        private TaskDefinition _explore;
        private NodeDefinition _hall, _dark;
        private ResourceDefinition _mirrors;

        // The hall (nothing to explore) ↔ the dark (4 steps to fill, 1 Mirror found each).
        // Explore: 1s a step, 5 vitality a step.
        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _explore = MakeTask("Explore", 1f);
            SetCost(_explore, 5f, (CostSource.Vitality, 100f));
            var travel = MakeTask("Travel", 1f);

            _mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            _hall = Make<NodeDefinition>();
            _hall.displayName = "The hall";
            _dark = Make<NodeDefinition>();
            _dark.displayName = "The dark";
            _dark.exploresToFill = 4;
            _dark.eachExploreGives.Add(new ResourceAmount { resource = _mirrors, amount = 1 });
            _hall.ways.Add(new Way { to = _dark });

            _content = Make<GameContent>();
            _content.travelVerb = travel;
            _content.exploreVerb = _explore;
            _content.startNode = _hall;
            _content.nodes.AddRange(new[] { _hall, _dark });
        }

        private Simulation MakeSimulation()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.TaskSkipped += (task, reason) => _log.Add($"skip {task.displayName}: {reason}");
            sim.ActionRefused += (task, _, reason) => _log.Add($"skip {task.displayName}: {reason}"); // refused as it's asked for: reads the same
            return sim;
        }

        /// <summary>Travel to the dark and search it for <paramref name="steps"/> steps' worth of time (at Perception 0), then end the run.</summary>
        private static void ExploreTheDark(Simulation sim, NodeDefinition dark, TaskDefinition explore, float steps)
        {
            sim.ClearQueue();
            sim.ScheduleTrip(dark);
            sim.Schedule(explore); // one long action, until the bar is full
            sim.BeginLoop();
            RunSeconds(sim, 1 + steps);
            sim.EndRunEarly();
        }

        [Test]
        public void Exploring_FillsTheBar_AndGivesWhatTheRoomGives()
        {
            var sim = MakeSimulation();

            ExploreTheDark(sim, _dark, _explore, 2);

            Assert.That(sim.ExploredFraction(_dark), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(2));
        }

        [Test]
        public void Progress_IsKept_IntoNextRun()
        {
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 1);

            ExploreTheDark(sim, _dark, _explore, 2);

            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(3).Within(0.001f), "the hall stays as she left it: 1 + 2");
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(3));
        }

        [Test]
        public void PartialProgress_IsKept_WhenRunEndsMidSearch()
        {
            var sim = MakeSimulation();

            ExploreTheDark(sim, _dark, _explore, 1.5f);

            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(1.5f).Within(0.02f));
            sim.BeginLoop();
            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(1.5f).Within(0.02f), "the new run starts where the bar was");
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(1), "only the whole step gave its Mirror");
        }

        [Test]
        public void SearchStepsDone_CountsOnlyWholeSteps()
        {
            var sim = MakeSimulation();
            Assert.That(sim.SearchStepsDoneIn(_dark), Is.EqualTo(0));

            ExploreTheDark(sim, _dark, _explore, 1.5f);

            Assert.That(sim.SearchStepsDoneIn(_dark), Is.EqualTo(1), "half a step isn't a step");
        }

        [Test]
        public void ASearchThatContinues_TakesOnlyWhatIsLeft()
        {
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 3);

            sim.ClearQueue();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 1.5f); // the trip, then half a second of the last step

            Assert.That(sim.Loop.CurrentTask, Is.EqualTo(_explore));
            Assert.That(sim.CurrentTimeLeftSeconds, Is.EqualTo(0.5f).Within(0.11f), "one step was left, not four");
        }

        [Test]
        public void Explore_SpeedIgnoresPerception()
        {
            // Plan 031: Perception reveals hidden finds; it no longer speeds a search (Wayfinding does).
            float StepsDoneAfterThreeSeconds(float perceptionXp)
            {
                var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
                sim.ScheduleTrip(_dark);
                sim.Schedule(_explore);
                sim.BeginLoop();
                sim.Loop.AttributeXp[ClaraAttribute.Perception] = perceptionXp;
                RunSeconds(sim, 3f); // the trip, then 2s
                return sim.ExploresDoneIn(_dark);
            }

            Assert.That(StepsDoneAfterThreeSeconds(0f), Is.EqualTo(2f).Within(0.02f));
            Assert.That(StepsDoneAfterThreeSeconds(500f), Is.EqualTo(2f).Within(0.02f), "a high Perception searches no faster");
        }

        [Test]
        public void AFullBar_GivesOneForEveryStep()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();

            RunSeconds(sim, 5.1f); // the trip and the 4 steps

            Assert.That(sim.IsFullyExplored(_dark), Is.True);
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(4), "all 4 mirrors");
        }

        [Test]
        public void StepRewards_AndXp_OncePerStep()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            float perStep = sim.XpRewardOf(_explore, _dark);

            RunSeconds(sim, 1 + 1.5f);
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(1), "one whole step");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Perception), Is.EqualTo(perStep).Within(0.001f), "the half step has given no XP yet");

            RunSeconds(sim, 0.5f);
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(2));
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Perception), Is.EqualTo(2 * perStep).Within(0.001f));
        }

        [Test]
        public void AFindThatNeedsPerception_StaysHidden_UntilHerPerceptionIsHighEnough()
        {
            var seam = MakeTask("Open the seam", 1f);
            _dark.foundBySearching.Add(new RoomFind { task = seam, atSearched = 25, needsPerception = 2 });
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 2f);
            Assert.That(sim.TasksAt(_dark), Has.No.Member(seam), "searched far enough, but she doesn't see it");

            sim.Loop.AttributeXp[ClaraAttribute.Perception] = 25f; // Perception 2
            Assert.That(sim.TasksAt(_dark), Has.Member(seam));
        }

        [Test]
        public void Finds_AppearMidAction_AtTheirPercent()
        {
            var gather = MakeTask("Gather a wisp", 1f);
            _dark.foundBySearching.Add(new RoomFind { task = gather, atSearched = 50 });
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();

            RunSeconds(sim, 2f); // the trip and 1 step: 25%
            Assert.That(sim.TasksAt(_dark), Has.No.Member(gather));

            RunSeconds(sim, 1.2f); // past 50%, with the search still going
            Assert.That(sim.Loop.CurrentTask, Is.EqualTo(_explore), "same action");
            Assert.That(sim.TasksAt(_dark), Has.Member(gather));
            Assert.That(sim.IsAvailableAt(gather, _hall), Is.False, "it belongs to the dark");
        }

        [Test]
        public void AnActionFoundBySearching_StaysFound_InTheNextRun()
        {
            var gather = MakeTask("Gather a wisp", 1f);
            _dark.foundBySearching.Add(new RoomFind { task = gather, atSearched = 50 });
            var sim = MakeSimulation();

            ExploreTheDark(sim, _dark, _explore, 2);
            sim.BeginLoop();

            Assert.That(sim.TasksAt(_dark), Has.Member(gather), "no need to search again");
        }

        [Test]
        public void ARoomWithNothingToExplore_SkipsIt()
        {
            var sim = MakeSimulation();
            sim.Schedule(_explore, 1);
            sim.BeginLoop();

            RunSeconds(sim, 1);

            Assert.That(_log, Is.EqualTo(new[] { $"skip Explore: {Reason("nothing_to_explore")}" }));
            Assert.That(sim.TasksAt(_hall), Has.No.Member(_explore));
        }

        [Test]
        public void Exploring_RunsUntilTheRoomIsFull_AndAFullRoomCantBeExploredAgain()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();

            RunSeconds(sim, 10);
            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(4), "never more than the bar holds");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "done, so it leaves the queue");
            Assert.That(_log, Is.Empty, "finishing isn't a skip");
            Assert.That(sim.Loop.CompletionLog.FindAll(task => task == _explore).Count, Is.EqualTo(1), "one long action");

            sim.Schedule(_explore, 1);
            RunSeconds(sim, 1);
            Assert.That(_log, Is.EqualTo(new[] { $"skip Explore: {Reason("fully_explored", ("room", "the dark"))}" }));
            Assert.That(sim.TasksAt(_dark), Has.No.Member(_explore));
        }

        [Test]
        public void APushedDownSearch_CarriesOnFromTheBar()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 2.5f); // the trip and 1.5 steps
            var wait = MakeTask("Wait", 1f);

            sim.PlayNow(wait, 1);
            RunSeconds(sim, 1f);
            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(1.5f).Within(0.02f), "waiting doesn't lose or add to the bar");

            RunSeconds(sim, 3f); // back to the search: 2.5 steps left
            Assert.That(sim.IsFullyExplored(_dark), Is.True);
        }

        [Test]
        public void TheRoomsExploringModifier_ChangesCostAndTime()
        {
            _dark.exploring = new ActionModifier { cost = 2f, time = 3f };
            var sim = MakeSimulation();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();

            RunSeconds(sim, 3); // 1s trip, then 2s of a 3s step
            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(2f / 3f).Within(0.02f));

            RunSeconds(sim, 1);
            Assert.That(sim.ExploresDoneIn(_dark), Is.EqualTo(1f).Within(0.02f));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 5f * 2f).Within(0.01f), "a step costs 5 × 2");
        }

        [Test]
        public void ThePriceShown_IsWhatSheIsCharged_ForWhatIsLeft()
        {
            _dark.exploring = new ActionModifier { cost = 2f, time = 3f };
            var sim = MakeSimulation();

            var price = sim.PriceOf(_explore, _dark);

            Assert.That(price.Seconds, Is.EqualTo(12f).Within(0.001f), "4 steps of 3s");
            Assert.That(price.Costs.Count, Is.EqualTo(1));
            Assert.That(price.Costs[0].amount, Is.EqualTo(40f).Within(0.001f), "4 steps of 5 × 2");
        }

        [Test]
        public void ADiscovery_IsASwitch_ThatFlipsWhenTheKeptBarGetsThere()
        {
            var halfway = Make<SwitchDefinition>();
            halfway.trigger = SwitchTrigger.RoomExplored;
            halfway.roomToExplore = _dark;
            halfway.explorePercent = 50;
            _content.switches.Add(halfway);
            var sim = MakeSimulation();

            ExploreTheDark(sim, _dark, _explore, 1);
            Assert.That(sim.IsFlipped(halfway), Is.False, "25%");

            ExploreTheDark(sim, _dark, _explore, 1);
            Assert.That(sim.IsFlipped(halfway), Is.True, "25% and 25% add up: the bar is kept");
        }

        [Test]
        public void TheActions_OfferExploringFirst_WhereThereIsSomethingLeft()
        {
            var search = MakeTask("Search", 1f);
            _dark.tasks.Add(search);
            var sim = MakeSimulation();

            Assert.That(sim.TasksAt(_dark), Is.EqualTo(new[] { _explore, search }));
        }

        [Test]
        public void TheReport_ShowsTheRoomsWhoseBarRoseThisRun()
        {
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 1);

            ExploreTheDark(sim, _dark, _explore, 2);

            Assert.That(sim.LastRun.Explored.Count, Is.EqualTo(1));
            Assert.That(sim.LastRun.Explored[0].room, Is.EqualTo(_dark));
            Assert.That(sim.LastRun.Explored[0].searched, Is.EqualTo(0.75f).Within(0.01f), "how full it is now");

            sim.BeginLoop();
            sim.ScheduleTrip(_dark);
            sim.EndRunEarly();
            Assert.That(sim.LastRun.Explored, Is.Empty, "a run that didn't search");
        }

        [Test]
        public void FoundWays_AndKeptProgress_SurviveSavingAndLoading()
        {
            var beyond = MakeNode("Beyond");
            _dark.ways.Add(new Way { to = beyond, foundAtExplored = 50 });
            _content.nodes.Add(beyond);
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 2);

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, _content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(restored.FoundWays, Is.EquivalentTo(new[] { (_dark, beyond) }));
            Assert.That(restored.Explored[_dark], Is.EqualTo(2f).Within(0.02f));
        }

        [Test]
        public void AVersion4Save_TurnsItsKeptExploration_IntoFoundWays()
        {
            var beyond = MakeNode("Beyond");
            _dark.ways.Add(new Way { to = beyond, foundAtExplored = 50 });
            var further = MakeNode("Further");
            _dark.ways.Add(new Way { to = further, foundAtExplored = 100 });
            _content.nodes.AddRange(new[] { beyond, further });

            var data = new SaveData { version = 4 };
            data.explored.Add(new SavedAmount { id = _dark.Id, amount = 2 }); // 2 of 4: 50%
            var upgraded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            var restored = SaveSerializer.Restore(upgraded, new ContentIndex(_content), new List<string>());

            Assert.That(error, Is.Null);
            Assert.That(restored.FoundWays, Is.EquivalentTo(new[] { (_dark, beyond) }), "found at 50%, not yet at 100%");
            Assert.That(restored.Explored[_dark], Is.EqualTo(2f), "and the bar is kept as it was");
        }

        [Test]
        public void V13Save_Upgrades_KeptExploreProgress()
        {
            var beyond = MakeNode("Beyond");
            _dark.ways.Add(new Way { to = beyond, foundAtExplored = 50 });
            var elsewhere = MakeNode("Elsewhere");
            elsewhere.exploresToFill = 3;
            _content.nodes.AddRange(new[] { beyond, elsewhere });

            var discovery = Make<SwitchDefinition>();
            discovery.trigger = SwitchTrigger.RoomExplored;
            discovery.roomToExplore = _dark;
            discovery.explorePercent = 100;
            _content.switches.Add(discovery);

            var data = new SaveData { version = 13 };
            data.foundWays.Add(new SavedWay { from = _dark.Id, to = beyond.Id }); // found in an earlier run
            data.hasRun = true;
            data.run.explored.Add(new SavedValue { id = elsewhere.Id, value = 1.5f }); // this run's search, under way
            var upgraded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            var restored = SaveSerializer.Restore(upgraded, new ContentIndex(_content), new List<string>());

            Assert.That(error, Is.Null);
            Assert.That(upgraded.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(restored.Explored[_dark], Is.EqualTo(4f), "a room with a way found counts as fully searched");
            Assert.That(restored.Explored[elsewhere], Is.EqualTo(1.5f), "the run's own progress is merged in");
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, restored);
            Assert.That(sim.IsFlipped(discovery), Is.True, "a room the upgrade filled has reached its discovery");
        }

        [Test]
        public void ANewSave_DoesntFillRoomsByFoundWays()
        {
            var beyond = MakeNode("Beyond");
            _dark.ways.Add(new Way { to = beyond, foundAtExplored = 50 });
            _content.nodes.Add(beyond);
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 2); // found the way at 50%, the bar at 50%

            var restored = SaveAndLoad(sim, _content);

            Assert.That(restored.Explored[_dark], Is.EqualTo(2f).Within(0.02f), "still half, not full");
        }

        // ---------- A search reopened by a switch (plan 027b) ----------

        private TaskDefinition _firstFind, _secondFind, _talk;
        private SwitchDefinition _reopens;

        /// <summary>
        /// The dark finds one action at 50% in its first round. Talking (done anywhere, 1s) flips a
        /// switch that reopens the dark's search, whose second round finds another action at 25%.
        /// </summary>
        private void AddSecondRound()
        {
            _firstFind = MakeTask("Gather a wisp", 1f);
            _secondFind = MakeTask("Take the earrings", 1f);
            _talk = MakeTask("Talk", 1f);
            _content.tasks.Add(_talk);
            _reopens = Make<SwitchDefinition>();
            _reopens.trigger = SwitchTrigger.TasksCompletedInOneRun;
            _reopens.requiredTasks.Add(_talk);
            _reopens.reopensSearch.Add(_dark);
            _content.switches.Add(_reopens);
            _dark.foundBySearching.Add(new RoomFind { task = _firstFind, atSearched = 50 });
            _dark.foundBySearching.Add(new RoomFind { task = _secondFind, atSearched = 25, afterSwitch = _reopens });
        }

        private static void Talk(Simulation sim, TaskDefinition talk)
        {
            sim.ClearQueue();
            sim.Schedule(talk, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1.5f);
            sim.EndRunEarly();
        }

        [Test]
        public void ReopenedSearch_StartsAtZero_KeepsEarlierFinds()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 4);
            Assert.That(sim.IsFullyExplored(_dark), Is.True);

            Talk(sim, _talk);

            Assert.That(sim.IsFlipped(_reopens), Is.True);
            Assert.That(sim.ExploredFraction(_dark), Is.EqualTo(0f), "the bar starts again");
            Assert.That(sim.IsFoundHere(_firstFind, _dark), Is.True, "what the first round found stays found");
            Assert.That(sim.TasksAt(_dark), Has.Member(_explore), "and there's searching to do again");
        }

        [Test]
        public void ReopenedSearch_KeepsAnEarlierFind_TheFirstRoundNeverReached()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 1); // 25%: the first find (50%) not reached

            Talk(sim, _talk);

            Assert.That(sim.IsFoundHere(_firstFind, _dark), Is.True, "Placeholder rule: a reopened room has found everything from before");
        }

        [Test]
        public void AfterSwitchFind_HiddenUntilFlip_ThenAtThreshold()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 4);
            Assert.That(sim.IsFoundHere(_secondFind, _dark), Is.False, "the second round hasn't begun");

            Talk(sim, _talk);
            Assert.That(sim.IsFoundHere(_secondFind, _dark), Is.False, "begun, but the bar is at 0%");

            ExploreTheDark(sim, _dark, _explore, 1);
            Assert.That(sim.ExploredFraction(_dark), Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(sim.IsFoundHere(_secondFind, _dark), Is.True, "found at 25% of the second round");
        }

        [Test]
        public void ReopenedSearch_ResetsOnce_NotEveryRun()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            Talk(sim, _talk);
            ExploreTheDark(sim, _dark, _explore, 2);

            Talk(sim, _talk); // the switch is already flipped: nothing resets

            Assert.That(sim.ExploredFraction(_dark), Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void ReopenedSearch_TheRunsReport_CountsFromZero()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 4);

            sim.ClearQueue();
            sim.Schedule(_talk, 1);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 3f); // talk, trip, one step
            sim.EndRunEarly();

            Assert.That(sim.LastRun.Explored.Count, Is.EqualTo(1), "the bar rose from the reset, though it's lower than at the start");
            Assert.That(sim.LastRun.Explored[0].searched, Is.EqualTo(0.25f).Within(0.01f));
        }

        [Test]
        public void ReopenedSearch_SurvivesSave()
        {
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 4);
            Talk(sim, _talk);
            ExploreTheDark(sim, _dark, _explore, 1);

            var loaded = Reopen(sim, MakeLoopSettings(), _content);

            Assert.That(loaded.ExploredFraction(_dark), Is.EqualTo(0.25f).Within(0.01f), "loading doesn't reset it again");
            Assert.That(loaded.IsFoundHere(_firstFind, _dark), Is.True);
            Assert.That(loaded.IsFoundHere(_secondFind, _dark), Is.True);
        }

        [Test]
        public void AVersion20Save_PastTheReopeningSwitch_StartsItsRoomsSearchAgain()
        {
            // Saved before switches could reopen a search: the switch had flipped, but nothing was reset.
            AddSecondRound();
            var other = Make<SwitchDefinition>(); // flipped too, reopens nothing
            _content.switches.Add(other);
            var data = new SaveData { version = 20 };
            data.flippedSwitches.Add(_reopens.Id);
            data.flippedSwitches.Add(other.Id);
            data.exploredRooms.Add(new SavedValue { id = _dark.Id, value = 4 }); // the field a real version 20 save uses
            var upgraded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);

            var restored = SaveSerializer.Restore(upgraded, new ContentIndex(_content), new List<string>());

            Assert.That(error, Is.Null);
            Assert.That(restored.Explored[_dark], Is.EqualTo(0f), "the second round starts at 0%, as if the switch had just flipped");
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, restored);
            Assert.That(sim.IsFoundHere(_firstFind, _dark), Is.True, "what the first round found stays found");
            Assert.That(sim.IsFoundHere(_secondFind, _dark), Is.False);
        }

        [Test]
        public void AVersion20Run_UnderWay_PastTheReopeningSwitch_BeganItsRoomsSearchAtZero()
        {
            // The run's start-of-run snapshot (for its Summary) must agree with the reset bar, or the
            // Summary hides the search done in that room.
            AddSecondRound();
            var sim = MakeSimulation();
            ExploreTheDark(sim, _dark, _explore, 4);
            Talk(sim, _talk); // flips the switch: the bar is 0 again
            sim.ClearQueue();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 1.5f);
            var data = SaveSerializer.Capture(sim);
            data.version = 20;
            data.run.exploredAtStart.RemoveAll(room => room.id == _dark.Id);
            data.run.exploredAtStart.Add(new SavedValue { id = _dark.Id, value = 4 }); // as a version 20 run kept it
            Assert.That(data.hasRun, Is.True, "set-up: a run under way");

            var upgraded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            var reopened = SaveSerializer.Load(upgraded, new ContentIndex(_content), MakeLoopSettings(), TicksPerSecond, _content, new List<string>(), out bool resumed);

            Assert.That(error, Is.Null);
            Assert.That(resumed, Is.True);
            Assert.That(reopened.Loop.ExploredAtStart[_dark], Is.EqualTo(0f));
        }

        [Test]
        public void TwoSwitchesReopeningOneRoom_IsAContentMistake()
        {
            AddSecondRound();
            var again = Make<SwitchDefinition>();
            again.trigger = SwitchTrigger.TasksCompletedInOneRun;
            again.requiredTasks.Add(_talk);
            again.reopensSearch.Add(_dark);
            _content.switches.Add(again);
            var sim = MakeSimulation();

            Talk(sim, _talk);

            Assert.Throws<System.InvalidOperationException>(() => sim.IsFoundHere(_firstFind, _dark),
                "one reopening per room: a second would need an order of rounds the content can't express yet");
        }
    }
}
