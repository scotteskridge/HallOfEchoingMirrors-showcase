using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// A task's extra drain (plan 029): a flat rate on top of the normal drain while that task is the
    /// one running, e.g. Feed your hours to the flames. Candles, growth and skill don't change it.
    /// </summary>
    public class ExtraDrainTests : SimulationTestBase
    {
        private LoopSettings _settings;
        private TaskDefinition _feed, _sweep;
        private GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _settings = MakeLoopSettings(vitalityMax: 1000f);
            _settings.vitalityDrainPerSecond = 0.5f;
            _feed = MakeTask("Feed", 10f);
            _feed.extraDrainPerSecond = 2f;
            _sweep = MakeTask("Sweep", 10f);
            _content = Make<GameContent>();
            _content.tasks.AddRange(new[] { _feed, _sweep });
        }

        private Simulation Begin()
        {
            var sim = new Simulation(_settings, TicksPerSecond, _content);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void DrainPerSecond_RisesByExtra_WhileTaskRuns_AndDropsAfter()
        {
            var sim = Begin();
            sim.Schedule(_feed, 1);
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 5f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(2.5f).Within(0.0001f));

            RunSeconds(sim, 6f); // Feed is done, the sweep runs
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ExtraDrain_IsFlat_NotHeldOffOrGrown()
        {
            var candles = MakeResource("Candle", max: 25);
            candles.drainAtFull = 0.5f;
            _settings.drainGrowthPerMinute = 0.25f;
            var sim = Begin();
            sim.Loop.ToolsAndStats[candles] = 25;
            sim.Loop.DrainGrown = 2f;
            sim.Schedule(_feed, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.DrainHeldOffNow, Is.LessThan(1f), "the candles do hold the drain off");
            Assert.That(sim.DrainGrowthMultiplier, Is.GreaterThan(1f), "and it has grown");
            float withoutExtra = _settings.vitalityDrainPerSecond * sim.DrainGrowthMultiplier * sim.DrainHeldOffNow;
            Assert.That(sim.ExtraDrainNow, Is.EqualTo(2f));
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(withoutExtra + 2f).Within(0.0001f));
        }

        [Test]
        public void ExtraDrain_NotCharged_WhenTaskNotRunning()
        {
            var sim = Begin();
            sim.Schedule(_sweep, 1);
            sim.Schedule(_feed, 1); // queued behind the sweep
            RunSeconds(sim, 5f);
            Assert.That(sim.ExtraDrainNow, Is.EqualTo(0f));
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Composure_SoftensExtraDrain()
        {
            _settings.composureExtraDrainPerLevel = 0.1f;
            _settings.xpForFirstLevel = 100f;
            _settings.xpGrowthPerLevel = 2f;
            _settings.xpMinIncreasePerLevel = 0f;
            var sim = Begin();
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 300f; // Composure 2
            sim.Schedule(_feed, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.ExtraDrainNow, Is.EqualTo(2f * 0.9f * 0.9f).Within(0.001f), "extra drain × 0.9^strength");
            Assert.That(sim.VitalityDrainPerSecond - sim.ExtraDrainNow, Is.EqualTo(0.5f).Within(0.01f), "the background drain is left to its own growth rule");
        }

        [Test]
        public void VitalityLost_OverTask_IsBasePlusExtraTimesSeconds()
        {
            var sim = Begin();
            sim.Schedule(_feed, 1);
            RunSeconds(sim, 10f);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(1000f - (0.5f + 2f) * 10f).Within(0.01f));
        }
    }
}
