using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Tasks themselves: durations, rewards and requirements, and action costs (switched off in the
    /// game for now; on in these tests). The queue's rules are in RunControlTests.
    /// </summary>
    public class TaskTests : SimulationTestBase
    {
        private readonly List<string> _log = new List<string>();
        private readonly Dictionary<string, ResourceDefinition> _resources = new Dictionary<string, ResourceDefinition>();

        [TearDown]
        public void ClearLog()
        {
            _log.Clear();
            _resources.Clear();
        }

        // No drain at all, so pools and vitality never interfere unless a test wants them to.
        private Simulation MakeSimulation(float amberMax = 50f, params LoopSettings.PoolSettings[] extraPools)
        {
            var settings = MakeLoopSettings();
            settings.pools.Add(HuePool(Hue.Amber, amberMax));
            settings.pools.AddRange(extraPools);

            var sim = new Simulation(settings, TicksPerSecond);
            sim.TaskStarted += task => _log.Add($"start {task.displayName}");
            sim.TaskCompleted += task => _log.Add($"done {task.displayName}");
            sim.TaskSkipped += (task, reason) => _log.Add($"skip {task.displayName}: {reason}");
            sim.ActionRefused += (task, _, reason) => _log.Add($"skip {task.displayName}: {reason}"); // refused as it's asked for: reads the same
            sim.BeginLoop();
            return sim;
        }

        /// <summary>One per-run resource per name, e.g. R("Focus"), shared within a test.</summary>
        private ResourceDefinition R(string name)
        {
            if (!_resources.TryGetValue(name, out var resource))
                _resources[name] = resource = MakeResource(name);
            return resource;
        }

        private ResourceAmount Amount(string name, int amount) =>
            new ResourceAmount { resource = R(name), amount = amount };

        [Test]
        public void Task_TakesItsDuration_ThenGrantsItsRewards()
        {
            var sim = MakeSimulation();
            var task = MakeTask("Task B", seconds: 2f);
            task.gives.Add(Amount("Focus", 1));
            sim.Schedule(task, 1);

            RunTicks(sim, 19);
            Assert.That(sim.Loop.CountOf(R("Focus")), Is.EqualTo(0), "finished too early");

            RunTicks(sim, 1);
            Assert.That(sim.Loop.CountOf(R("Focus")), Is.EqualTo(1));
            Assert.That(sim.Loop.CurrentTask, Is.Null);
        }

        [Test]
        public void Grants_StackUp_WhenATaskIsRepeated()
        {
            var sim = MakeSimulation();
            var task = MakeTask("Task B", 1f);
            task.gives.Add(Amount("Focus", 1));
            sim.Schedule(task, 1);
            sim.Schedule(task, 1);

            RunTicks(sim, 20);

            Assert.That(sim.Loop.CountOf(R("Focus")), Is.EqualTo(2));
        }

        [Test]
        public void RequirementMet_ByAnEarlierTask_LetsTheTaskRun()
        {
            var sim = MakeSimulation();
            var giveTool = MakeTask("Task A", 1f);
            giveTool.gives.Add(Amount("Tool A", 1));
            var needsTool = MakeTask("Task C", 1f);
            needsTool.needs.Add(Amount("Tool A", 1));
            sim.Schedule(giveTool, 1);
            sim.Schedule(needsTool, 1);

            RunTicks(sim, 20);

            Assert.That(_log, Is.EqualTo(new[] { "start Task A", "done Task A", "start Task C", "done Task C" }));
        }

        [Test]
        public void Cost_DrainsEvenly_WhileTheTaskRuns()
        {
            var sim = MakeSimulation(amberMax: 50f);
            var task = MakeTask("Task B", 5f); // 20 Amber over 5s = 4 per second
            SetCost(task, 20f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);
            var amber = sim.Loop.FindPool(Hue.Amber);

            RunTicks(sim, 1 * TicksPerSecond);
            Assert.That(amber.Current, Is.EqualTo(46f).Within(0.01f), "after 1s");

            RunTicks(sim, 2 * TicksPerSecond);
            Assert.That(amber.Current, Is.EqualTo(38f).Within(0.01f), "after 3s");
        }

        [Test]
        public void AVitalityCost_DrainsVitality_WhileTheTaskRuns()
        {
            var sim = MakeSimulation(); // vitality 100, no background drain
            var chase = MakeTask("Chase", 10f);
            SetCost(chase, 50f, (CostSource.Vitality, 100f)); // 5 per second
            sim.Schedule(chase, 1);

            RunTicks(sim, 4 * TicksPerSecond);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(80f).Within(0.01f));
        }

        [Test]
        public void AVitalityCost_BiggerThanVitality_StillStarts_AndEndsTheRun()
        {
            var sim = MakeSimulation();
            var chase = MakeTask("Chase", 10f);
            SetCost(chase, 200f, (CostSource.Vitality, 100f)); // 20/s: vitality 100 lasts 5s
            sim.Schedule(chase, 1);

            RunTicks(sim, 10 * TicksPerSecond);

            Assert.That(_log, Is.EqualTo(new[] { "start Chase" }), "started, never finished");
            Assert.That(sim.Loop.IsOver, Is.True);
            Assert.That(sim.Loop.TicksElapsed, Is.EqualTo(5 * TicksPerSecond).Within(1));
        }

        [Test]
        public void ASplitCost_IsSharedByPercentage()
        {
            var sim = MakeSimulation(amberMax: 50f, HuePool(Hue.Citrine, 50f));
            var task = MakeTask("Working", 5f);
            SetCost(task, 40f, (CostSource.Amber, 75f), (CostSource.Citrine, 25f), (CostSource.Vitality, 0f));
            sim.Schedule(task, 1);

            RunTicks(sim, 5 * TicksPerSecond);

            Assert.That(sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(50f - 30f).Within(0.001f));
            Assert.That(sim.Loop.FindPool(Hue.Citrine).Current, Is.EqualTo(50f - 10f).Within(0.001f));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void SharesThatDontAddUpTo100_AreScaledToTheTotal()
        {
            var task = MakeTask("Working", 5f);
            SetCost(task, 30f, (CostSource.Amber, 1f), (CostSource.Vitality, 2f));

            var parts = task.CostBreakdown(task.cost);

            Assert.That(parts[0].amount, Is.EqualTo(10f).Within(0.001f));
            Assert.That(parts[1].amount, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void CostSource_AndHue_UseTheSameNumbers()
        {
            foreach (Hue hue in System.Enum.GetValues(typeof(Hue)))
            {
                if (hue == Hue.None)
                    continue;
                Assert.That(hue.ToCostSource().ToString(), Is.EqualTo(hue.ToString()), $"{hue} doesn't line up");
            }
        }

        [Test]
        public void Cost_SpendsExactlyTheTotal_ByTheEndOfTheTask()
        {
            var sim = MakeSimulation(amberMax: 50f);
            var task = MakeTask("Task B", 3f);
            SetCost(task, 7f, (CostSource.Amber, 100f)); // doesn't divide evenly into 30 ticks
            sim.Schedule(task, 1);

            RunTicks(sim, 60); // well past the end

            Assert.That(sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(43f).Within(0.0001f));
        }

        [Test]
        public void IfThePoolRunsDryMidTask_TheRestOfTheCostFallsOnVitality()
        {
            // Amber holds 5 of the 10 the task needs, so it runs dry halfway through.
            var settings = MakeLoopSettings();
            settings.pools.Add(HuePool(Hue.Amber, max: 5f));
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            var task = MakeTask("Task B", 10f);
            SetCost(task, 10f, (CostSource.Amber, 100f)); // 1 per second
            sim.Schedule(task, 1);

            RunTicks(sim, 10 * TicksPerSecond);

            float spentFromVitality = 100f - sim.Loop.Vitality.Current;
            Assert.That(sim.Loop.FindPool(Hue.Amber).IsEmpty, Is.True);
            Assert.That(spentFromVitality, Is.EqualTo(10f - 5f).Within(0.01f), "total 10 needed, Amber held 5");
        }

        [Test]
        public void NotEnoughPathos_StillStartsTheTask_AndVitalityCoversTheRest()
        {
            var sim = MakeSimulation(amberMax: 10f);
            var task = MakeTask("Task C", 1f);
            SetCost(task, 20f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);

            RunSeconds(sim, 1);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(task));
            Assert.That(sim.Loop.FindPool(Hue.Amber).IsEmpty, Is.True);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 10f).Within(0.01f));
        }

        [Test]
        public void CostInALockedHue_SkipsTheTask()
        {
            var sim = MakeSimulation();
            var task = MakeTask("Task C", 1f);
            SetCost(task, 5f, (CostSource.Ruby, 100f));
            sim.Schedule(task, 1);

            RunTicks(sim, 1);

            Assert.That(_log, Is.EqualTo(new[] { $"skip Task C: {Reason("needs_pool", ("pool", CostSource.Ruby))}" }));
        }

        [Test]
        public void NewLoop_ResetsToolsAndStats_AndEmptiesTheQueue()
        {
            var sim = MakeSimulation();
            var task = MakeTask("Task A", 1f);
            task.gives.Add(Amount("Tool A", 1));
            sim.Schedule(task, 2);
            RunTicks(sim, 10);

            sim.BeginLoop();

            Assert.That(sim.Loop.CountOf(R("Tool A")), Is.EqualTo(0));
            Assert.That(sim.Loop.Queue.Count, Is.EqualTo(0));
            Assert.That(sim.Loop.CurrentTask, Is.Null);
        }

        [Test]
        public void FinishedEntries_LeaveTheQueue()
        {
            var sim = MakeSimulation();
            sim.Schedule(MakeTask("A", 1f), 1);
            sim.Schedule(MakeTask("B", 1f), 1);
            RunTicks(sim, 15); // A done, B running

            Assert.That(sim.Queue.Count, Is.EqualTo(1));
            Assert.That(sim.Queue.Top.Task.displayName, Is.EqualTo("B"));

            RunTicks(sim, 10);
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingAnUpcomingEntry_LeavesTheRunningOneAlone()
        {
            var sim = MakeSimulation();
            sim.Schedule(MakeTask("A", 1f), 1);
            sim.Schedule(MakeTask("B", 1f), 1);
            sim.Schedule(MakeTask("C", 1f), 1);
            RunTicks(sim, 15); // A done, B running: the queue is B, C

            sim.RemoveFromQueue(1); // C
            RunTicks(sim, 15);

            Assert.That(_log, Is.EqualTo(new[] { "start A", "done A", "start B", "done B" }));
        }

        [Test]
        public void ARepeatLimit_RunsTheTaskThatManyTimes()
        {
            var sim = MakeSimulation();
            var task = MakeTask("Task B", 1f);
            task.gives.Add(Amount("Focus", 1));

            sim.Schedule(task, 5);
            Assert.That(sim.Queue.Count, Is.EqualTo(1), "one entry, not five");
            RunTicks(sim, 100);

            Assert.That(sim.Loop.CountOf(R("Focus")), Is.EqualTo(5));
        }

        [Test]
        public void SchedulingTheSameTaskAgain_MergesIntoTheLastEntry()
        {
            var sim = MakeSimulation();
            var taskA = MakeTask("A", 1f);
            var taskB = MakeTask("B", 1f);

            sim.Schedule(taskA, 5);
            sim.Schedule(taskA, 10);
            sim.Schedule(taskB, 1);
            sim.Schedule(taskA, 1);

            var entries = sim.Queue.Entries;
            Assert.That(entries.Count, Is.EqualTo(3));
            Assert.That(entries[0].TimesLeft, Is.EqualTo(15));
            Assert.That(entries[1].Task, Is.EqualTo(taskB));
            Assert.That(entries[2].TimesLeft, Is.EqualTo(1));
        }

        [Test]
        public void ABlockedEntry_IsSkippedOnce_NotOncePerRepeat()
        {
            var sim = MakeSimulation();
            var needsTool = MakeTask("Task C", 1f);
            needsTool.needs.Add(Amount("Tool A", 1));
            sim.Schedule(needsTool, 10);
            sim.Schedule(MakeTask("Task B", 1f), 1);

            RunTicks(sim, 10);

            Assert.That(_log, Is.EqualTo(new[] { $"skip Task C: {Reason("needs", ("amount", 1), ("item", "Tool A"))}", "start Task B", "done Task B" }));
        }

        // WhyEntryCantStart: one reason builder for an entry refused as it's asked for (it would start
        // now) and one skipped when its turn comes. Each case checks both give the same words.

        [Test]
        public void WhyEntryCantStart_AMissingItem_ReadsTheSame_RefusedOrSkipped()
        {
            var sim = MakeSimulation();
            var needsTool = MakeTask("Task C", 1f);
            needsTool.needs.Add(Amount("Tool A", 1));
            string expected = $"skip Task C: {Reason("needs", ("amount", 1), ("item", "Tool A"))}";

            sim.PlayNow(needsTool); // would start now: refused
            Assert.That(_log, Is.EqualTo(new[] { expected }));

            _log.Clear();
            sim.Schedule(MakeTask("Wait", 1f), 1);
            sim.Schedule(needsTool, 1); // queued behind: skipped when reached
            RunSeconds(sim, 2);
            Assert.That(_log, Is.EqualTo(new[] { "start Wait", "done Wait", expected }));
        }

        [Test]
        public void RefusedAction_ReportsNoDestination()
        {
            var sim = MakeSimulation();
            var needsTool = MakeTask("Task C", 1f);
            needsTool.needs.Add(Amount("Tool A", 1));
            bool refused = false;
            NodeDefinition destination = MakeNode("Not null");
            sim.ActionRefused += (_, where, _) => { refused = true; destination = where; };

            sim.PlayNow(needsTool);

            Assert.That(refused, Is.True);
            Assert.That(destination, Is.Null, "only a trip has a destination");
        }

        [Test]
        public void WhyEntryCantStart_AFullCarry_ReadsTheSame_RefusedOrSkipped()
        {
            var sim = MakeSimulation();
            var flint = MakeObject("Flint", max: 1);
            var strike = MakeGatherTask("Strike flint", 1f, flint);
            string expected = $"skip Strike flint: {Reason("cant_carry_more", ("item", "Flint"))}";

            // Queued with room for it, then pushed down under a wait, then her pockets fill.
            sim.CarryNow(strike);
            sim.PlayNow(MakeTask("Wait", 1f), 1);
            sim.Loop.ToolsAndStats[flint] = 1;
            RunSeconds(sim, 2);
            Assert.That(_log, Has.Member(expected), "skipped when reached");

            _log.Clear();
            sim.CarryNow(strike); // would start now: refused
            Assert.That(_log, Is.EqualTo(new[] { expected }));
        }

        [Test]
        public void WhyEntryCantStart_ATrip_WrapsItsReason_RefusedOrSkipped()
        {
            var hall = MakeNode("The Hall");
            var vault = MakeNode("The Vault");
            Join(hall, vault, startsOpen: false);
            var content = MakePlaces(hall, vault);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.ActionRefused += (task, _, reason) => _log.Add(reason);
            sim.TaskSkipped += (task, reason) => _log.Add(reason);
            sim.BeginLoop();
            string expected = Reason("trip", ("room", GameText.TitleInSentence("The Vault")), ("reason", Reason("way_shut")));

            sim.PlayTripNow(vault); // would start now: refused
            Assert.That(_log, Is.EqualTo(new[] { expected }));

            _log.Clear();
            sim.Schedule(MakeTask("Wait", 1f), 1);
            sim.ScheduleTrip(vault); // queued behind: skipped when reached
            RunSeconds(sim, 2);
            Assert.That(_log, Is.EqualTo(new[] { expected }));
        }
    }
}
