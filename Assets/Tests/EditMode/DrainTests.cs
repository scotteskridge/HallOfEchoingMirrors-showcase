using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Action costs (switched off in the game, on in these tests): actions spend vitality and pathos,
    /// and whatever an empty pool can't cover comes out of vitality. Also the loop's phases. The
    /// test base switches the passive drain off, so its own tests are the ones at the end (they use
    /// StatTestBase's settings); AttributeTests holds the stat and skill rules.
    /// </summary>
    public class DrainTests : StatTestBase
    {
        const float Tolerance = 0.01f;

        private LoopSettings MakeSettings(float vitalityMax, params (Hue hue, float max)[] pools)
        {
            var settings = MakeLoopSettings(vitalityMax);
            foreach (var (hue, max) in pools)
                settings.pools.Add(HuePool(hue, max));
            return settings;
        }

        [Test]
        public void APoolsName_ComesFromTheHueText()
        {
            var settings = MakeSettings(100f);
            foreach (Hue hue in System.Enum.GetValues(typeof(Hue)))
                if (hue != Hue.None)
                    settings.pools.Add(HuePool(hue, 10f));
            var sim = Begin(new Simulation(settings, TicksPerSecond));

            foreach (var pool in sim.Loop.Pools)
                if (pool.Hue != Hue.None)
                    Assert.That(pool.Name, Is.EqualTo(GameText.HueName(pool.Hue)));
            Assert.That(sim.Loop.Pools.Exists(p => p.Hue != Hue.None), Is.True, "the test built hue pools");
        }

        [Test]
        public void APoolSettingWithNoHue_FailsLoudly()
        {
            var settings = MakeSettings(100f);
            settings.pools.Add(new LoopSettings.PoolSettings { max = 10f });

            Assert.Throws<System.InvalidOperationException>(() => new Simulation(settings, TicksPerSecond));
        }

        [Test]
        public void VitalitysName_ComesFromTheText()
        {
            var settings = MakeSettings(100f);
            var sim = Begin(new Simulation(settings, TicksPerSecond));

            Assert.That(sim.Loop.Vitality.Name, Is.EqualTo(GameText.Get("names.vitality")));
        }

        [Test]
        public void APoolWithNoHue_KeepsTheNameItWasGiven()
        {
            Assert.That(new Pool("Magic", 5f).Name, Is.EqualTo("Magic"));
        }

        private static Simulation Begin(Simulation sim)
        {
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void Pool_DrainingMoreThanItHolds_ReturnsTheOverflow()
        {
            var pool = new Pool("Magic", max: 5f);

            float overflow = pool.Drain(8f);

            Assert.That(pool.Current, Is.EqualTo(0f));
            Assert.That(overflow, Is.EqualTo(3f));
        }

        [Test]
        public void GemCapacity_IsEveryPoolsMaximumAddedUp_AndNothingWithoutPools()
        {
            var none = Begin(new Simulation(MakeSettings(100f), TicksPerSecond));
            Assert.That(none.GemCapacity, Is.EqualTo(0f));

            var settings = MakeSettings(100f, (Hue.Amber, 50f), (Hue.Sapphire, 30f));
            settings.pools.Add(HuePool(Hue.Emerald, 40f, startsUnlocked: false));
            var sim = Begin(new Simulation(settings, TicksPerSecond));

            Assert.That(sim.GemCapacity, Is.EqualTo(80f), "a pool she hasn't learnt isn't part of the gem");
        }

        [Test]
        public void WithTheDrainOff_NothingDrainsWhileIdle()
        {
            var sim = Begin(new Simulation(MakeSettings(100f, (Hue.Amber, 50f)), TicksPerSecond));
            KeepBusy(sim, 30f); // time passes only while something is queued

            RunSeconds(sim, 30);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f));
            Assert.That(sim.Loop.Pools[0].Current, Is.EqualTo(50f));
        }

        [Test]
        public void WithNothingQueued_VitalityNeverDrains_AndTheRunClockStands()
        {
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 1f;
            var sim = Begin(new Simulation(settings, TicksPerSecond));

            RunSeconds(sim, 5f); // even if the game's clock were left running

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f));
            Assert.That(sim.Loop.TicksElapsed, Is.EqualTo(0));
        }

        [Test]
        public void AVitalityTask_DrainsVitalityAsItRuns()
        {
            var sim = Begin(new Simulation(MakeSettings(100f), TicksPerSecond));
            sim.Schedule(MakeTiringTask("Walk", seconds: 20f, vitalityCost: 40f), 1);

            RunSeconds(sim, 10);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(80f).Within(Tolerance));
        }

        [Test]
        public void WhileAPoolHasPathos_ItPaysItsShare()
        {
            var sim = Begin(new Simulation(MakeSettings(100f, (Hue.Amber, 50f)), TicksPerSecond));
            var task = MakeTask("Cast", 10f);
            SetCost(task, 20f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);

            RunSeconds(sim, 10);

            Assert.That(sim.Loop.Pools[0].Current, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void ALowPool_DoesNotStopATask_ItsShortfallHitsVitality()
        {
            var sim = Begin(new Simulation(MakeSettings(100f, (Hue.Amber, 5f)), TicksPerSecond));
            var task = MakeTask("Cast", 10f);
            SetCost(task, 20f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);

            RunSeconds(sim, 10);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(task));
            Assert.That(sim.Loop.Pools[0].IsEmpty, Is.True);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 15f).Within(Tolerance));
        }

        [Test]
        public void LockedPools_AreLeftOutOfTheLoop()
        {
            var settings = MakeSettings(100f, (Hue.Amber, 50f), (Hue.Ruby, 50f));
            settings.pools[1].startsUnlocked = false;

            var sim = Begin(new Simulation(settings, TicksPerSecond));

            Assert.That(sim.Loop.Pools.Count, Is.EqualTo(1));
            Assert.That(sim.Loop.Pools[0].Hue, Is.EqualTo(Hue.Amber));
        }

        [Test]
        public void PoolEmptied_FiresOnce_WhenAPoolRunsDry()
        {
            var sim = Begin(new Simulation(MakeSettings(100f, (Hue.Amber, 5f)), TicksPerSecond));
            var task = MakeTask("Cast", 10f);
            SetCost(task, 20f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);
            var emptied = new List<Hue>();
            sim.PoolEmptied += pool => emptied.Add(pool.Hue);

            RunSeconds(sim, 20);

            Assert.That(emptied, Is.EqualTo(new[] { Hue.Amber }));
        }

        [Test]
        public void LoopEnds_WhenVitalityReachesZero_AndFiresLoopEndedOnce()
        {
            var sim = Begin(new Simulation(MakeSettings(10f), TicksPerSecond));
            sim.Schedule(MakeTiringTask("Chase", seconds: 20f, vitalityCost: 20f), 1); // 1 vitality a second
            int endedCount = 0;
            sim.LoopEnded += () => endedCount++;

            RunSeconds(sim, 15); // well past the 10s it takes to empty

            Assert.That(sim.Loop.IsOver, Is.True);
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(endedCount, Is.EqualTo(1));
            Assert.That(sim.Loop.TicksElapsed, Is.EqualTo(10 * TicksPerSecond).Within(1));
        }

        [Test]
        public void TheGameOpensBetweenRuns_AndNothingRunsUntilBegin()
        {
            var sim = new Simulation(MakeSettings(100f, (Hue.Amber, 50f)), TicksPerSecond);
            sim.Schedule(MakeTiringTask("Walk", 20f, 40f), 1);

            RunSeconds(sim, 10);

            Assert.That(sim.Phase, Is.EqualTo(LoopPhase.BetweenRuns));
            Assert.That(sim.Loop.TicksElapsed, Is.EqualTo(0));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f));
        }

        [Test]
        public void FirstBegin_RunsLoopOne()
        {
            var sim = new Simulation(MakeSettings(100f), TicksPerSecond);
            Assert.That(sim.NextLoopNumber, Is.EqualTo(1));

            sim.BeginLoop();

            Assert.That(sim.Phase, Is.EqualTo(LoopPhase.Running));
            Assert.That(sim.Persistent.LoopNumber, Is.EqualTo(1));
        }

        [Test]
        public void WhenVitalityRunsOut_TheGameIsBetweenRunsAgain()
        {
            var sim = Begin(new Simulation(MakeSettings(10f), TicksPerSecond));
            sim.Schedule(MakeTiringTask("Chase", 20f, 20f), 1);

            RunSeconds(sim, 15);

            Assert.That(sim.Phase, Is.EqualTo(LoopPhase.BetweenRuns));
            Assert.That(sim.NextLoopNumber, Is.EqualTo(2));
        }

        [Test]
        public void LoopStarted_FiresOnBegin_NotBefore()
        {
            var sim = new Simulation(MakeSettings(100f), TicksPerSecond);
            int started = 0;
            sim.LoopStarted += () => started++;

            RunSeconds(sim, 5);
            Assert.That(started, Is.EqualTo(0));

            sim.BeginLoop();
            Assert.That(started, Is.EqualTo(1));
        }

        [Test]
        public void BeginAfterALoopEnds_RefillsPools_AndCountsTheLoop()
        {
            var sim = Begin(new Simulation(MakeSettings(10f, (Hue.Amber, 5f)), TicksPerSecond));
            var task = MakeTask("Cast", 20f);
            SetCost(task, 40f, (CostSource.Amber, 100f));
            sim.Schedule(task, 1);
            RunSeconds(sim, 20);
            Assert.That(sim.Loop.IsOver, Is.True);

            sim.BeginLoop();

            Assert.That(sim.Persistent.LoopNumber, Is.EqualTo(2));
            Assert.That(sim.Loop.IsOver, Is.False);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(10f));
            Assert.That(sim.Loop.Pools[0].Current, Is.EqualTo(5f));
        }

        // ---------- The passive drain ----------

        [Test]
        public void Vitality_DrainsOnItsOwn_EvenWhileWhatSheDoesIsFree()
        {
            var sim = Begin(DrainSettings());
            KeepBusy(sim, 10f);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(1000f - 5f).Within(0.01f));
        }

        [Test]
        public void TheDrain_GrowsEveryMinute()
        {
            var settings = DrainSettings();
            settings.drainGrowthPerMinute = 0.25f;
            var sim = Begin(settings);
            KeepBusy(sim, 120f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.5f).Within(0.0001f));

            RunSeconds(sim, 30f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.5f * UnityEngine.Mathf.Sqrt(1.25f)).Within(0.0001f), "smoothly, not in steps");

            RunSeconds(sim, 30f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.625f).Within(0.0001f));

            RunSeconds(sim, 60f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.78125f).Within(0.0001f), "and it compounds");
        }

        [Test]
        public void Composure_SlowsHowFastTheDrainGrows()
        {
            var settings = DrainSettings();
            settings.drainGrowthPerMinute = 0.25f;
            settings.composureDrainGrowthPerLevel = 0.9f;
            var sim = Begin(settings);
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 300f; // Composure 2: growth ×0.81
            KeepBusy(sim, 60f);

            RunSeconds(sim, 60f);

            Assert.That(sim.DrainGrowthMultiplier, Is.EqualTo(1f + 0.25f * 0.81f).Within(0.001f));
        }

        // ---------- What the stats do while costs are on ----------

        [Test]
        public void AFasterTask_StillSpendsItsFullCost()
        {
            var settings = MakeStatSettings();
            settings.vitalityMax = 100f;
            var sim = Begin(settings);
            var climb = MakeSkillTask("Climb", 30f, _wayfinding);
            climb.xpReward = 300f;
            SetCost(climb, 30f, (CostSource.Vitality, 100f));
            sim.Schedule(climb, 1);

            RunSeconds(sim, 40f);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(climb));
            Assert.That(100f - sim.Loop.Vitality.Current, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Attunement_MakesHueCostsCheaper_ButNotVitality()
        {
            var sim = Begin(MakeStatSettings());
            sim.Schedule(MakeTask("Steady the breath", 10f, ClaraAttribute.Attunement), 3); // Attunement 2
            var working = MakeTask("Working", 10f, ClaraAttribute.None);
            SetCost(working, 60f, (CostSource.Amber, 50f), (CostSource.Vitality, 50f));
            sim.Schedule(working, 1);

            RunSeconds(sim, 30f);
            float amberBefore = sim.Loop.FindPool(Hue.Amber).Current;
            float vitalityBefore = sim.Loop.Vitality.Current;
            RunSeconds(sim, 10f);

            // 30 Amber at Attunement 2 (+20%) costs 30 / 1.2 = 25. Vitality's 30 is unchanged.
            Assert.That(amberBefore - sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(25f).Within(0.01f));
            Assert.That(vitalityBefore - sim.Loop.Vitality.Current, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Composure_NeverSoftensVitalityCosts()
        {
            var sim = Begin(MakeStatSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 300f; // Composure 2
            sim.Schedule(MakeTiringTask("Run", 10f, vitalityCost: 30f), 1);

            float before = sim.Loop.Vitality.Current;
            RunSeconds(sim, 10f);

            Assert.That(before - sim.Loop.Vitality.Current, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void Composure_AlsoSoftensWhatTasksTakeFromThePools()
        {
            var sim = Begin(MakeStatSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 300f; // Composure 2
            var grieve = MakeTask("Face the grief", 10f, ClaraAttribute.None);
            SetCost(grieve, 30f, (CostSource.Amber, 100f));
            sim.Schedule(grieve, 1);

            float before = sim.Loop.FindPool(Hue.Amber).Current;
            RunSeconds(sim, 10f);

            Assert.That(before - sim.Loop.FindPool(Hue.Amber).Current, Is.EqualTo(30f / 1.2f).Within(0.01f));
        }
    }
}
