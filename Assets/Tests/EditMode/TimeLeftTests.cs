using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>How long the running action has left, as the ribbon and the queue show it.</summary>
    public class TimeLeftTests : SimulationTestBase
    {
        [Test]
        public void HalfDone_HalfTheTimeLeft()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            sim.BeginLoop();
            sim.Schedule(MakeTask("Wait", 10f));

            RunSeconds(sim, 5f);

            Assert.That(sim.CurrentTimeLeftSeconds, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void FasterSpeed_LessTimeLeft()
        {
            var settings = MakeLoopSettings();
            settings.skillSpeedPerLevel = 1f;
            settings.skillMasterySpeedPerLevel = 2f;
            settings.masteryXpForFirstLevel = 100f;
            var skill = MakeSkill("Wayfinding");
            var walk = MakeTask("Walk", 10f);
            walk.skill = skill;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.Persistent.SkillMasteryXp[skill] = 100f; // mastery 1: twice as fast
            sim.BeginLoop();
            sim.Schedule(walk);

            RunTicks(sim, 1); // starts, and does one tick's work at double speed

            Assert.That(sim.CurrentSpeed, Is.EqualTo(2f).Within(0.001f));
            Assert.That(sim.CurrentTimeLeftSeconds, Is.EqualTo(4.9f).Within(0.001f), "(100 - 2) work / 2 a tick / 10 ticks a second");
        }

        [Test]
        public void NothingRunning_Zero()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            sim.BeginLoop();

            RunTicks(sim, 3);

            Assert.That(sim.CurrentTimeLeftSeconds, Is.EqualTo(0f));
        }
    }
}
