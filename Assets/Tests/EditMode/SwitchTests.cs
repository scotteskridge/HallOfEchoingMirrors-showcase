using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    public class SwitchTests : SimulationTestBase
    {
        // 10 vitality: 10s of a task that costs 1 a second. One locked Amber pool.
        private LoopSettings MakeSettings()
        {
            var settings = MakeLoopSettings(vitalityMax: 10f);
            settings.pools.Add(HuePool(Hue.Amber, 50f, startsUnlocked: false));
            return settings;
        }

        private GameContent MakeContent(params SwitchDefinition[] switches)
        {
            var content = Make<GameContent>();
            content.switches.AddRange(switches);
            return content;
        }

        // The tutorial opening: a chase too long to finish, and a switch for failing it.
        private (Simulation sim, TaskDefinition chase, TaskDefinition explore, SwitchDefinition lost) MakeTutorial()
        {
            var chase = MakeTiringTask("Chase Roland", 15f, vitalityCost: 15f);
            var explore = MakeTask("Explore the mirror realm", 5f, startsUnlocked: false);
            var lost = Make<SwitchDefinition>();
            lost.displayName = "Lost Roland";
            lost.trigger = SwitchTrigger.LoopEndedDuringTask;
            lost.triggerTask = chase;
            lost.unlocksTasks.Add(explore);
            lost.unlocksPools.Add(Hue.Amber);
            lost.story = MakeStory("The Chase Ends\nHer legs gave way.");

            var content = MakeContent(lost);
            content.tasks.Add(chase);
            content.tasks.Add(explore);
            var sim = new Simulation(MakeSettings(), TicksPerSecond, content);
            return (sim, chase, explore, lost);
        }

        [Test]
        public void ASwitchThatRetiresTheRunningTask_OnItsLastTick_StopsItCleanly()
        {
            // Searching levels Perception on its very last tick, which flips a switch retiring the search.
            var search = MakeTask("Search", 1f, ClaraAttribute.Perception);
            var noticed = Make<SwitchDefinition>();
            noticed.trigger = SwitchTrigger.AttributeLevelReached;
            noticed.triggerAttribute = ClaraAttribute.Perception;
            noticed.triggerLevel = 1;
            noticed.locksTasks.Add(search);
            var settings = MakeSettings();
            settings.xpPerSecondOfTask = 10f; // 1 XP a tick...
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 10f; // ...so level 1 arrives on the 10th tick, the search's last
            var sim = new Simulation(settings, TicksPerSecond, MakeContent(noticed));
            sim.Schedule(search, 1);
            sim.BeginLoop();

            Assert.DoesNotThrow(() => RunSeconds(sim, 2));
            Assert.That(sim.IsFlipped(noticed), Is.True);
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(search), "stopped, not finished");
        }

        [Test]
        public void ASwitchCanRetireATask_HidingItAndTakingItOutOfTheQueue()
        {
            var learn = MakeTask("Learn a better way", 1f);
            var oldWay = MakeTask("The old way", 1f);
            var wait = MakeTask("Wait", 1f);
            var better = Make<SwitchDefinition>();
            better.trigger = SwitchTrigger.TasksCompletedInOneRun;
            better.requiredTasks.Add(learn);
            better.locksTasks.Add(oldWay);
            var sim = new Simulation(MakeSettings(), TicksPerSecond, MakeContent(better));
            sim.Schedule(learn, 1);
            sim.Schedule(oldWay, 1);
            sim.Schedule(wait, 1);
            sim.BeginLoop();

            RunSeconds(sim, 1); // learnt: the switch flips

            Assert.That(sim.IsUnlocked(oldWay), Is.False);
            Assert.That(sim.Queue.Count, Is.EqualTo(1), "the old way is taken out of the queue");
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(wait));
        }

        [Test]
        public void ALaterSwitch_CanBringARetiredTaskBack()
        {
            var (sim, chase, explore, lost) = MakeTutorial();
            lost.locksTasks.Add(chase);
            var again = Make<SwitchDefinition>();
            again.trigger = SwitchTrigger.TasksCompletedInOneRun;
            again.requiredTasks.Add(explore);
            again.unlocksTasks.Add(chase);
            var content = Make<GameContent>();
            content.switches.Add(lost);
            content.switches.Add(again);
            content.tasks.Add(chase);
            content.tasks.Add(explore);
            sim = new Simulation(MakeSettings(), TicksPerSecond, content);

            sim.Schedule(chase, 1);
            sim.BeginLoop();
            RunSeconds(sim, 12);
            Assert.That(sim.IsUnlocked(chase), Is.False, "retired after failing");

            sim.Schedule(explore, 1);
            sim.BeginLoop();
            RunSeconds(sim, 12);
            Assert.That(sim.IsUnlocked(chase), Is.True, "back again later in the game");
        }

        [Test]
        public void LockedTasks_StayHidden_UntilUnlocked()
        {
            var (sim, chase, explore, _) = MakeTutorial();

            Assert.That(sim.IsUnlocked(chase), Is.True);
            Assert.That(sim.IsUnlocked(explore), Is.False);
        }

        [Test]
        public void RunningOutOfVitality_MidChase_FlipsTheSwitch_AndUnlocksTheNextTask()
        {
            var (sim, chase, explore, lost) = MakeTutorial();
            sim.Schedule(chase, 1);
            sim.BeginLoop();

            RunSeconds(sim, 12); // vitality lasts 10s; the chase needs 15s

            Assert.That(sim.IsFlipped(lost), Is.True);
            Assert.That(sim.IsUnlocked(explore), Is.True);
        }

        [Test]
        public void RunningOutOfVitality_DuringADifferentTask_DoesNotFlipTheSwitch()
        {
            var (sim, _, explore, lost) = MakeTutorial();
            var other = MakeTiringTask("Wait", 20f, vitalityCost: 20f);
            sim.Schedule(other, 1);
            sim.BeginLoop();

            RunSeconds(sim, 12);

            Assert.That(sim.IsFlipped(lost), Is.False);
            Assert.That(sim.IsUnlocked(explore), Is.False);
        }

        [Test]
        public void FlippedSwitches_AndUnlocks_SurviveLaterLoops()
        {
            var (sim, chase, explore, lost) = MakeTutorial();
            sim.Schedule(chase, 1);
            sim.BeginLoop();
            RunSeconds(sim, 12);

            sim.BeginLoop();
            RunSeconds(sim, 12);
            sim.BeginLoop();

            Assert.That(sim.IsFlipped(lost), Is.True);
            Assert.That(sim.IsUnlocked(explore), Is.True);
        }

        [Test]
        public void AnUnlockedPool_AppearsFromTheNextLoop()
        {
            var (sim, chase, _, _) = MakeTutorial();
            sim.Schedule(chase, 1);
            sim.BeginLoop();
            Assert.That(sim.Loop.Pools, Is.Empty, "vitality only at first");

            RunSeconds(sim, 12);
            sim.BeginLoop();

            Assert.That(sim.Loop.FindPool(Hue.Amber), Is.Not.Null);
        }

        [Test]
        public void ASwitch_RevealsItsStoryOnce_AndKeepsItInTheJournal()
        {
            var (sim, chase, _, lost) = MakeTutorial();
            sim.Schedule(chase, 1);
            sim.BeginLoop();
            RunSeconds(sim, 12);
            sim.BeginLoop();
            RunSeconds(sim, 12);

            Assert.That(sim.UnreadStories.Count, Is.EqualTo(1));
            Assert.That(sim.UnreadStories[0], Is.EqualTo(lost.story));
            Assert.That(sim.Persistent.Journal, Is.EqualTo(new[] { lost.story }));
        }

        [Test]
        public void TheOpeningStory_IsQueuedAtTheStart()
        {
            var content = MakeContent();
            content.openingStory = MakeStory("Through the Glass\nShe ran.");

            var sim = new Simulation(MakeSettings(), TicksPerSecond, content);

            Assert.That(sim.UnreadStories.Count, Is.EqualTo(1));
            Assert.That(sim.UnreadStories[0].Title, Is.EqualTo("Through the Glass"));
            Assert.That(sim.UnreadStories[0].Body, Is.EqualTo("She ran."));
        }

        [Test]
        public void TasksCompletedInOneRun_FlipsWhenAllAreDone()
        {
            var a = MakeTask("A", 1f);
            var b = MakeTask("B", 1f);
            var both = Make<SwitchDefinition>();
            both.trigger = SwitchTrigger.TasksCompletedInOneRun;
            both.requiredTasks.Add(a);
            both.requiredTasks.Add(b);
            var sim = new Simulation(MakeSettings(), TicksPerSecond, MakeContent(both));
            sim.Schedule(a, 1);
            sim.Schedule(b, 1);
            sim.BeginLoop();

            RunSeconds(sim, 1);
            Assert.That(sim.IsFlipped(both), Is.False, "only A done");

            RunSeconds(sim, 1);
            Assert.That(sim.IsFlipped(both), Is.True);
        }

        [Test]
        public void TasksCompletedInOneRun_DoesNotCountTasksFromEarlierRuns()
        {
            var a = MakeTask("A", 1f);
            var b = MakeTask("B", 1f);
            var both = Make<SwitchDefinition>();
            both.trigger = SwitchTrigger.TasksCompletedInOneRun;
            both.requiredTasks.Add(a);
            both.requiredTasks.Add(b);
            var sim = new Simulation(MakeSettings(), TicksPerSecond, MakeContent(both));

            sim.Schedule(a, 1);
            sim.BeginLoop();
            RunSeconds(sim, 2); // A done
            sim.EndRunEarly();
            sim.ClearQueue();
            sim.Schedule(b, 1);
            sim.BeginLoop();
            RunSeconds(sim, 2); // B done in a different run

            Assert.That(sim.IsFlipped(both), Is.False);
        }
    }
}
