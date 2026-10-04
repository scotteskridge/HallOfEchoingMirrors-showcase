using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The action queue (Play, Schedule, repeating until done) and ending a run.</summary>
    public class RunControlTests : SimulationTestBase
    {
        private readonly List<string> _log = new List<string>();

        [TearDown]
        public void ClearLog() => _log.Clear();

        // 10 vitality, no passive drain (the test base's default).
        private Simulation MakeSimulation(GameContent content = null)
        {
            var sim = new Simulation(MakeLoopSettings(vitalityMax: 10f), TicksPerSecond, content);
            sim.TaskCompleted += task => _log.Add($"done {task.displayName}");
            sim.TaskSkipped += (task, reason) => _log.Add($"skip {task.displayName}: {reason}");
            sim.ActionRefused += (task, _, reason) => _log.Add($"skip {task.displayName}: {reason}"); // refused as it's asked for: reads the same
            return sim;
        }

        [Test]
        public void ARun_StartsWithAnEmptyQueue_AndSheWaits()
        {
            var sim = MakeSimulation();
            int ranOut = 0;
            sim.QueueRanOut += () => ranOut++;
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(sim.Queue.Count, Is.EqualTo(0));
            Assert.That(ranOut, Is.EqualTo(1), "once, so the game can pause for the player");
            Assert.That(sim.Loop.IsOver, Is.False, "an idle run doesn't end");
        }

        [Test]
        public void AQueuedTask_RepeatsUntilSheIsSpent()
        {
            var sim = MakeSimulation();
            sim.Schedule(MakeTiringTask("Search", 2f, vitalityCost: 2f));
            sim.BeginLoop();

            RunSeconds(sim, 12);

            Assert.That(_log, Is.EqualTo(new[] { "done Search", "done Search", "done Search", "done Search", "done Search" }));
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
        }

        [Test]
        public void Schedule_AddsToTheBottom()
        {
            var sim = MakeSimulation();
            sim.Schedule(MakeTask("Search", 1f), 1);
            sim.Schedule(MakeTask("Explore", 1f), 1);
            sim.BeginLoop();

            RunSeconds(sim, 3);

            Assert.That(_log, Is.EqualTo(new[] { "done Search", "done Explore" }));
        }

        [Test]
        public void ActionsQueuedMidRun_StartAsSoonAsSheIsFree()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            RunSeconds(sim, 1); // waiting: nothing queued

            sim.Schedule(MakeTask("Search", 1f), 1);
            RunSeconds(sim, 1);

            Assert.That(_log, Is.EqualTo(new[] { "done Search" }));
        }

        [Test]
        public void Play_SwitchesAtOnce_AndTheInterruptedTaskKeepsItsProgress()
        {
            var settings = MakeLoopSettings(vitalityMax: 100f);
            var sim = new Simulation(settings, TicksPerSecond);
            sim.TaskCompleted += task => _log.Add($"done {task.displayName}");
            var climb = MakeTiringTask("Climb", 10f, vitalityCost: 10f); // 100 ticks
            sim.Schedule(climb, 1);
            sim.BeginLoop();
            RunTicks(sim, 40); // 40% of the way

            sim.PlayNow(MakeTask("Look", 2f), 1); // ticks 41-60
            RunTicks(sim, 20);
            Assert.That(_log, Is.EqualTo(new[] { "done Look" }));

            // Back to the climb at 40%: 60 ticks more, so it's done at tick 120, not 160.
            RunTicks(sim, 59);
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(climb), "not yet at tick 119");
            RunTicks(sim, 1);
            Assert.That(sim.Loop.CompletedTasks, Has.Member(climb), "done at tick 120");
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(90f).Within(0.01f), "and it cost 10 in all, not more");
        }

        [Test]
        public void Play_OnATaskAlreadyQueued_MovesItToTheTop()
        {
            var sim = MakeSimulation();
            var search = MakeTask("Search", 1f);
            var explore = MakeTask("Explore", 1f);
            sim.Schedule(search);
            sim.Schedule(explore);

            sim.PlayNow(explore);

            Assert.That(sim.Queue.Count, Is.EqualTo(2), "moved, not doubled");
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(explore));
        }

        [Test]
        public void RemovingTheRunningEntry_StopsHerAtOnce()
        {
            var sim = MakeSimulation();
            var climb = MakeTask("Climb", 10f);
            sim.Schedule(climb, 1);
            sim.Schedule(MakeTask("Look", 1f), 1);
            sim.BeginLoop();
            RunSeconds(sim, 2);

            sim.RemoveFromQueue(0);
            RunSeconds(sim, 1);

            Assert.That(_log, Is.EqualTo(new[] { "done Look" }));
        }

        [Test]
        public void ASingleAction_RunsOnce_AndAsksForAPause()
        {
            var sim = MakeSimulation();
            var pull = MakeTask("Pull the lever", 1f);
            pull.singleAction = true;
            var paused = new List<TaskDefinition>();
            sim.SingleActionDone += task => paused.Add(task);
            sim.Schedule(pull);
            sim.BeginLoop();

            RunSeconds(sim, 3);

            Assert.That(_log, Is.EqualTo(new[] { "done Pull the lever" }));
            Assert.That(paused, Is.EqualTo(new[] { pull }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void AnEntry_IsDone_WhenSheCanHoldNoMoreOfWhatItGives()
        {
            var sim = MakeSimulation();
            var glimmer = MakeResource("Glimmer", max: 3);
            var gather = MakeTask("Gather", 1f);
            gather.gives.Add(new ResourceAmount { resource = glimmer, amount = 1 });
            sim.Schedule(gather);
            sim.Schedule(MakeTask("Look", 1f), 1);
            sim.BeginLoop();

            RunSeconds(sim, 5);

            Assert.That(_log, Is.EqualTo(new[] { "done Gather", "done Gather", "done Gather", "done Look" }));
            Assert.That(sim.AmountOf(glimmer), Is.EqualTo(3));
        }

        [Test]
        public void AnEntryThatCantStart_IsDroppedWithAReason_AndTheRestCarriesOn()
        {
            var sim = MakeSimulation();
            var pry = MakeTask("Pry the frame", 1f);
            pry.needs.Add(new ResourceAmount { resource = MakeResource("Crowbar"), amount = 1 });
            sim.Schedule(pry);
            sim.Schedule(MakeTask("Search", 1f), 1);
            sim.BeginLoop();

            RunSeconds(sim, 2);

            Assert.That(_log, Is.EqualTo(new[]
            {
                $"skip Pry the frame: {Reason("needs", ("amount", 1), ("item", "Crowbar"))}",
                "done Search",
            }));
        }

        [Test]
        public void QueueRanOut_FiresOnce_AndAgainAfterMoreIsDone()
        {
            var sim = MakeSimulation();
            int ranOut = 0;
            sim.QueueRanOut += () => ranOut++;
            sim.Schedule(MakeTask("Search", 1f), 1);
            sim.BeginLoop();
            RunSeconds(sim, 3);
            Assert.That(ranOut, Is.EqualTo(1));

            sim.Schedule(MakeTask("Explore", 1f), 1);
            RunSeconds(sim, 3);

            Assert.That(ranOut, Is.EqualTo(2));
        }

        [Test]
        public void ActionsQueuedBetweenRuns_AreForTheNextRun_AndEveryRunStartsEmpty()
        {
            var sim = MakeSimulation();
            var search = MakeTask("Search", 100f);
            sim.Schedule(search, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);
            sim.EndRunEarly();

            sim.Schedule(search, 1); // between runs
            Assert.That(sim.Queue.Count, Is.EqualTo(1));
            sim.BeginLoop();
            Assert.That(sim.Loop.Queue.Count, Is.EqualTo(1), "what was queued between runs");

            sim.EndRunEarly();
            sim.BeginLoop();
            Assert.That(sim.Loop.Queue.Count, Is.EqualTo(0), "nothing carries over from the run before");
        }

        [Test]
        public void EndRunEarly_WhileRunning_EndsItAsEndedByPlayer()
        {
            var sim = MakeSimulation();
            sim.Schedule(MakeTask("Search", 100f), 1);
            sim.BeginLoop();
            RunSeconds(sim, 3);

            sim.EndRunEarly();

            Assert.That(sim.Phase, Is.EqualTo(LoopPhase.BetweenRuns));
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.EndedByPlayer));
            Assert.That(sim.NextLoopNumber, Is.EqualTo(2));
        }

        [Test]
        public void EndingAChaseEarly_IsNotTheSameAsFailingIt()
        {
            var chase = MakeTask("Chase Roland", 60f);
            var lost = Make<SwitchDefinition>();
            lost.trigger = SwitchTrigger.LoopEndedDuringTask;
            lost.triggerTask = chase;
            var content = Make<GameContent>();
            content.tasks.Add(chase);
            content.switches.Add(lost);

            var sim = MakeSimulation(content);
            sim.Schedule(chase, 1);
            sim.BeginLoop();
            RunSeconds(sim, 3);
            sim.EndRunEarly();

            Assert.That(sim.IsFlipped(lost), Is.False, "she chose to stop; she didn't collapse");
        }
    }
}
