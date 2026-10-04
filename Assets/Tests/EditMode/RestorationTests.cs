using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Restoration items (vitality back over a clock) and holding off the darkness.</summary>
    public class RestorationTests : SimulationTestBase
    {
        private ResourceDefinition _wisp;

        // A wisp gives back 5 vitality over 5 seconds (1 a second). 100 vitality, no passive drain.
        [SetUp]
        public void SetUp()
        {
            _wisp = MakeResource("Wisp", max: 10);
            _wisp.restoreVitality = 5f;
            _wisp.restoreSeconds = 5f;
        }

        private Simulation Begin(int wisps, float vitalityLost)
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            if (vitalityLost > 0f)
                sim.Schedule(MakeTiringTask("Strain", 1f, vitalityCost: vitalityLost), 1);
            sim.BeginLoop();
            sim.Loop.ToolsAndStats[_wisp] = wisps;
            return sim;
        }

        [Test]
        public void AnItem_IsntUsed_WhileItWouldBeWasted()
        {
            var sim = Begin(wisps: 1, vitalityLost: 4f); // missing 4: a wisp's 5 would waste 1

            RunSeconds(sim, 3f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(96f).Within(0.01f));
        }

        [Test]
        public void AnItem_IsUsed_OnceSheIsMissingEnough_AndGivesItBackOverItsClock()
        {
            var sim = Begin(wisps: 1, vitalityLost: 5f);
            KeepBusy(sim, 10f);

            RunSeconds(sim, 1f); // the strain costs 5; the wisp starts
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0), "used up as it starts");
            Assert.That(sim.RestoringPerSecond, Is.EqualTo(1f).Within(0.001f));

            RunSeconds(sim, 2.5f);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(97.5f).Within(0.05f), "halfway through its clock");

            RunSeconds(sim, 3f);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f).Within(0.01f));
            Assert.That(sim.Loop.Restorings, Is.Empty, "exhausted");
        }

        [Test]
        public void RestoringSecondsLeft_CountsDownWhileTheItemIsInUse_AndIsZeroOtherwise()
        {
            var sim = Begin(wisps: 1, vitalityLost: 5f);
            KeepBusy(sim, 10f);
            Assert.That(sim.RestoringSecondsLeft(_wisp), Is.EqualTo(0f), "not in use yet");

            RunSeconds(sim, 3f); // starts at 1s, so 2s into a 5s clock

            Assert.That(sim.RestoringSecondsLeft(_wisp), Is.EqualTo(3f).Within(0.15f));
            Assert.That(sim.RestoringSecondsLeft(MakeResource("Other")), Is.EqualTo(0f), "only the item asked about");

            RunSeconds(sim, 4f);
            Assert.That(sim.RestoringSecondsLeft(_wisp), Is.EqualTo(0f), "used up");
        }

        [Test]
        public void ReachingZero_EndsTheRun_EvenWithAnItemInUse()
        {
            // The drain takes 0.1 a tick; the item in use gives back 1 a tick, more than the drain. So
            // if restoring could come after the drain in the same tick, she'd never reach zero.
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 1f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            sim.Loop.Restorings.Add(new LoopState.Restoring { Item = _wisp, PerSecond = 10f, Left = 50f });
            sim.Loop.Vitality.Drain(sim.Loop.Vitality.Max - 0.05f); // 0.05 left: less than one tick's drain
            Assert.That(sim.IsRestoring(_wisp), Is.True);
            KeepBusy(sim, 1f);

            RunTicks(sim, 1);

            Assert.That(sim.Phase, Is.EqualTo(LoopPhase.BetweenRuns), "zero is the end, whatever is in use");
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
        }

        [Test]
        public void EndurancesAllowedWaste_AndOneOfEachKind_WorkTogether()
        {
            var well = MakeResource("Bottled well", max: 5);
            well.restoreVitality = 10f;
            var settings = MakeLoopSettings();
            settings.enduranceOverflowPerLevel = 1f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 25f; // Endurance 2: 2 may be wasted
            sim.Loop.ToolsAndStats[_wisp] = 2;
            sim.Loop.ToolsAndStats[well] = 1;
            sim.Loop.Vitality.Drain(12f);
            KeepBusy(sim, 1f);

            RunTicks(sim, 1);

            // Missing 12, + 2 allowed = 14: one wisp (5, and only one at a time); 9 left is short of the well's 10.
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1));
            Assert.That(sim.AmountOf(well), Is.EqualTo(1), "the well waits");
        }

        [Test]
        public void EverythingHoldingOffTheDarkness_MultipliesTogether()
        {
            var lit = MakeResource("Candles lit", max: 25);
            lit.drainAtFull = 0.75f;
            lit.countsUpTo = 15;
            var charm = MakeResource("A kept charm", ResourceLifetime.Forever, max: 1);
            charm.drainAtFull = 0.8f;
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 1f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            sim.Loop.DrainHeldOff = 0.5f; // as if a ward had been done
            sim.Loop.ToolsAndStats[lit] = 15;
            sim.Persistent.Resources[charm] = 1;

            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.5f * 0.75f * 0.8f).Within(0.0001f));
        }

        [Test]
        public void OnlyOneOfEachKind_RunsAtATime_TheNextWaitsForItToFinish()
        {
            var sim = Begin(wisps: 3, vitalityLost: 20f); // missing enough for all three
            KeepBusy(sim, 10f);

            RunSeconds(sim, 1f);
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "one wisp at a time");
            Assert.That(sim.RestoringPerSecond, Is.EqualTo(1f).Within(0.001f));

            RunSeconds(sim, 5f); // the first has run its course
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "then the next starts");
            Assert.That(sim.Loop.Restorings.Count, Is.EqualTo(1));
        }

        [Test]
        public void DifferentKinds_RunTogether_AndAdd()
        {
            var well = MakeResource("Bottled well", max: 5);
            well.restoreVitality = 10f;
            well.restoreSeconds = 5f;
            var sim = Begin(wisps: 1, vitalityLost: 20f);
            sim.Loop.ToolsAndStats[well] = 1;

            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0));
            Assert.That(sim.AmountOf(well), Is.EqualTo(0));
            Assert.That(sim.RestoringPerSecond, Is.EqualTo(1f + 2f).Within(0.001f), "a wisp and a well side by side");
        }

        [Test]
        public void WhatsStillComing_CountsAgainstWaste()
        {
            var well = MakeResource("Bottled well", max: 5);
            well.restoreVitality = 10f;
            var sim = Begin(wisps: 1, vitalityLost: 12f);
            sim.Loop.ToolsAndStats[well] = 1;

            RunSeconds(sim, 1f);

            // Missing 12: the wisp (5) starts; with 5 on its way only 7 is really missing, so the well (10) waits.
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0));
            Assert.That(sim.AmountOf(well), Is.EqualTo(1));
        }

        // ---------- What she'll lose meanwhile isn't waste (the user's rule, 2026-10-02) ----------

        private Simulation BeginDraining(float drainPerSecond, int wisps, float vitalityLost)
        {
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = drainPerSecond; // growth stays 0, so the drain is flat
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            sim.Loop.ToolsAndStats[_wisp] = wisps;
            if (vitalityLost > 0f)
                sim.Loop.Vitality.Drain(vitalityLost);
            KeepBusy(sim, 10f);
            return sim;
        }

        [Test]
        public void AnItem_IsUsedEarly_WhenTheDrainWouldUseItUpWhileItRestores()
        {
            // Missing 1, and 1 a second over the wisp's 5 second clock: 1 + 5 = 6, enough for its 5.
            var sim = BeginDraining(drainPerSecond: 1f, wisps: 1, vitalityLost: 1f);

            RunTicks(sim, 1);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0), "used: the drain takes back what would overflow");
        }

        [Test]
        public void ADrainAsFastAsTheItemGives_UsesIt_EvenAtFullVitality()
        {
            var sim = BeginDraining(drainPerSecond: 1f, wisps: 1, vitalityLost: 0f); // the wisp gives 1 a second too

            RunTicks(sim, 1);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0));
        }

        [Test]
        public void ASlowDrain_StillWaits_UntilTheItemWouldFit()
        {
            // Missing 1, and 0.5 a second over 5 seconds: 1 + 2.5 = 3.5, short of the wisp's 5.
            var sim = BeginDraining(drainPerSecond: 0.5f, wisps: 1, vitalityLost: 1f);

            RunTicks(sim, 1);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "it would still waste some");
        }

        [Test]
        public void ACarriedItemsCost_CountsAsDrain_WhileSheActs()
        {
            var ring = MakeResource("Ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 1f;
            ring.alwaysCharged = true;
            var sim = BeginDraining(drainPerSecond: 0f, wisps: 1, vitalityLost: 0f);
            sim.Loop.ToolsAndStats[ring] = 1;

            RunTicks(sim, 2); // the first tick starts the task; the ring's cost counts once she's acting

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0), "the ring's 1 a second over the wisp's 5 seconds covers it");
        }

        [Test]
        public void MaxRestorePerSecond_AddsOneOfEachKindSheCanUse()
        {
            var well = MakeResource("Bottled well", max: 5);
            well.restoreVitality = 10f;
            well.restoreSeconds = 5f;
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            sim.BeginLoop();
            Assert.That(sim.MaxRestorePerSecond, Is.EqualTo(0f), "nothing held");

            sim.Loop.ToolsAndStats[_wisp] = 3;
            Assert.That(sim.MaxRestorePerSecond, Is.EqualTo(1f).Within(0.0001f), "three wisps still restore one at a time");

            sim.Loop.ToolsAndStats[well] = 1;
            Assert.That(sim.MaxRestorePerSecond, Is.EqualTo(1f + 2f).Within(0.0001f), "a well runs alongside");
        }

        [Test]
        public void MaxRestorePerSecond_CountsAKindInUse_EvenWithNoneLeft()
        {
            var sim = BeginDraining(drainPerSecond: 0f, wisps: 1, vitalityLost: 20f);

            RunTicks(sim, 1);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0), "the last wisp is in use");
            Assert.That(sim.MaxRestorePerSecond, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void HoldingOffTheDarkness_LowersTheDrain_WhichKeepsGrowing()
        {
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 1f;
            settings.drainGrowthPerMinute = 0.25f;
            var sim = new Simulation(settings, TicksPerSecond);
            var ward = MakeTask("Ward the dark", 1f);
            ward.drainTimes = 0.5f;
            sim.Schedule(ward, 1);
            KeepBusy(sim, 60f);
            sim.BeginLoop();

            RunSeconds(sim, 1f);
            float justAfter = sim.VitalityDrainPerSecond;
            Assert.That(justAfter, Is.EqualTo(0.5f * UnityEngine.Mathf.Pow(1.25f, 1f / 60f)).Within(0.0001f), "halved");

            RunSeconds(sim, 60f);
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(justAfter * 1.25f).Within(0.001f), "and still growing");
        }

        [Test]
        public void HeldItems_CanHoldOffTheDarkness_InStepWithHowManyUpToACap()
        {
            var lit = MakeResource("Candles lit", max: 25);
            lit.drainAtFull = 0.75f;
            lit.countsUpTo = 15;
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 1f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();

            sim.Loop.ToolsAndStats[lit] = 6;
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(1f - 0.25f * 6f / 15f).Within(0.0001f), "6 of 15: ×0.9");

            sim.Loop.ToolsAndStats[lit] = 15;
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.75f).Within(0.0001f));

            sim.Loop.ToolsAndStats[lit] = 25;
            Assert.That(sim.VitalityDrainPerSecond, Is.EqualTo(0.75f).Within(0.0001f), "more than 15 don't help");
        }

        [Test]
        public void EachRun_StartsWithTheDarknessNotHeldOff()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            var ward = MakeTask("Ward the dark", 1f);
            ward.drainTimes = 0.5f;
            sim.Schedule(ward, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1f);
            Assert.That(sim.Loop.DrainHeldOff, Is.EqualTo(0.5f));

            sim.EndRunEarly();
            sim.BeginLoop();

            Assert.That(sim.Loop.DrainHeldOff, Is.EqualTo(1f));
        }
    }
}
