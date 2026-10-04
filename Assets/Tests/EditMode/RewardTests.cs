using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>One-time rewards kept for good, e.g. a faster game speed earned by a costly action.</summary>
    public class RewardTests : SimulationTestBase
    {
        private GameContent _content;
        private ResourceDefinition _quickened;
        private TaskDefinition _feed;

        // Feed your hours to the flames (1s) gives Quickened Hours: kept forever, one only, unlocks room speed.
        [SetUp]
        public void SetUp()
        {
            _quickened = MakeResource("Quickened Hours", ResourceLifetime.Forever, max: 1);
            _quickened.unlocksRoomSpeed = true;
            _feed = MakeGatherTask("Feed your hours to the flames", 1f, _quickened);
            _content = Make<GameContent>();
            _content.tasks.Add(_feed);
        }

        private Simulation NewGame() => new Simulation(MakeLoopSettings(), TicksPerSecond, _content);

        [Test]
        public void FeedingYourHours_UnlocksRoomSpeed_NotSpeedTwo()
        {
            var sim = NewGame();
            sim.BeginLoop();
            Assert.That(sim.RoomSpeedUnlocked, Is.False, "not there from the start");

            sim.Schedule(_feed);
            RunSeconds(sim, 1f);

            Assert.That(sim.RoomSpeedUnlocked, Is.True);
            Assert.That(sim.FastestSpeedUnlocked, Is.EqualTo(1f), "the ×2 tier is retired");
        }

        [Test]
        public void PlanningUnlocked_ByItemFlag_KeptThroughSave()
        {
            _quickened.unlocksPlanning = true;
            var sim = NewGame();
            int announced = 0;
            sim.PlanningUnlockedNow += () => announced++;
            sim.BeginLoop();
            Assert.That(sim.PlanningUnlocked, Is.False, "not there from the start");

            sim.Schedule(_feed);
            RunSeconds(sim, 1f);
            Assert.That(sim.PlanningUnlocked, Is.True);
            Assert.That(announced, Is.EqualTo(1), "said once, when it was earned");

            var warnings = new List<string>();
            var loaded = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, SaveAndLoad(sim, _content, warnings));
            Assert.That(loaded.PlanningUnlocked, Is.True, "kept in the save");
        }

        [Test]
        public void NextRunBeginsAtOnce_UntilPlanningIsEarned()
        {
            _quickened.unlocksPlanning = true;
            var sim = NewGame();
            sim.BeginLoop();
            Assert.That(sim.NextRunBeginsAtOnce, Is.True, "no planning screen yet: a loaded game just begins its run");

            sim.Schedule(_feed);
            RunSeconds(sim, 1f);
            Assert.That(sim.NextRunBeginsAtOnce, Is.False);
        }

        [Test]
        public void AOneTimeReward_IsNeverOfferedAgain_AndIsKeptThroughRunsAndSaves()
        {
            var sim = NewGame();
            sim.BeginLoop();
            Assert.That(sim.TasksAt(null), Has.Member(_feed));
            sim.Schedule(_feed);
            RunSeconds(sim, 1f);
            Assert.That(sim.TasksAt(null), Has.No.Member(_feed), "earned: gone from the list");

            sim.EndRunEarly();
            sim.BeginLoop();
            Assert.That(sim.TasksAt(null), Has.No.Member(_feed), "and in later runs");

            var warnings = new List<string>();
            var loaded = new Simulation(MakeLoopSettings(), TicksPerSecond, _content, SaveAndLoad(sim, _content, warnings));
            loaded.BeginLoop();
            Assert.That(warnings, Is.Empty);
            Assert.That(loaded.RoomSpeedUnlocked, Is.True, "kept in the save");
            Assert.That(loaded.TasksAt(null), Has.No.Member(_feed));
        }
    }
}
