using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The formulas the rules share with screens and editor tools (mastery speed, the drain, an escalating
    /// charge, the stat a task trains, how long an entry repeats): one copy in Core, so a retune moves everything.
    /// </summary>
    public class SharedFormulaTests : SimulationTestBase
    {
        // ---------- Mastery speed ----------

        [TestCase(0, 1f)]
        [TestCase(1, 1.1f)]
        [TestCase(2, 1.21f)]
        public void MasterySpeedAt_CompoundsPerLevel(int mastery, float expected)
        {
            var settings = MakeLoopSettings();
            settings.skillMasterySpeedPerLevel = 1.1f;

            Assert.That(settings.MasterySpeedAt(mastery), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void SkillsMasterySpeed_IsTheSharedFormulaAtItsMastery()
        {
            var settings = MakeLoopSettings();
            settings.skillMasterySpeedPerLevel = 1.1f;
            var skill = MakeSkill("Wayfinding");
            var sim = new Simulation(settings, TicksPerSecond);
            sim.Persistent.SkillMasteryXp[skill] = 1000f;

            Assert.That(sim.MasteryOf(skill), Is.GreaterThan(0), "she has some mastery for this to mean anything");
            Assert.That(sim.MasterySpeedFor(skill), Is.EqualTo(settings.MasterySpeedAt(sim.MasteryOf(skill))));
        }

        // ---------- The drain ----------

        [Test]
        public void DrainPerSecondAt_IsBaseTimesGrowthTimesHeldOff_PlusTheFlatExtra()
        {
            Assert.That(Simulation.DrainPerSecondAt(2f, 1.5f, 0.5f, 0.3f), Is.EqualTo(1.8f).Within(0.0001f));
        }

        [Test]
        public void DrainGrowthAfter_CompoundsPerMinute()
        {
            Assert.That(Simulation.DrainGrowthAfter(0.25f, 0f), Is.EqualTo(1f));
            Assert.That(Simulation.DrainGrowthAfter(0.25f, 2f), Is.EqualTo(1.5625f).Within(0.0001f));
        }

        [Test]
        public void DrainPerSecondAt_FromSettings_GrowsWithTimeIntoTheRun()
        {
            var settings = MakeLoopSettings();
            settings.vitalityDrainPerSecond = 2f;
            settings.drainGrowthPerMinute = 0.5f;

            Assert.That(Simulation.DrainPerSecondAt(settings, 0f, 1f, 0f), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(Simulation.DrainPerSecondAt(settings, 60f, 1f, 0.25f), Is.EqualTo(3.25f).Within(0.0001f), "2 x 1.5, plus the extra");
        }

        [Test]
        public void TheRunsOwnDrain_MatchesTheStaticFormula()
        {
            var settings = MakeLoopSettings(vitalityMax: 100000f);
            settings.vitalityDrainPerSecond = 2f;
            settings.drainGrowthPerMinute = 0.25f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            KeepBusy(sim, 100f);

            RunSeconds(sim, 60f);

            Assert.That(sim.VitalityDrainPerSecond,
                Is.EqualTo(Simulation.DrainPerSecondAt(settings, 60f, 1f, 0f)).Within(0.001f));
        }

        // ---------- Escalating charge ----------

        [TestCase(3f, 1.08f, 0, 3f)]
        [TestCase(3f, 1.08f, 2, 3.4992f)]
        [TestCase(0f, 2f, 5, 0f)]
        public void EscalatingChargeAfter_GrowsOncePerFinishedGo(float baseCharge, float growth, int uses, float expected)
        {
            Assert.That(Simulation.EscalatingChargeAfter(baseCharge, growth, uses), Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ATasksEscalatingCharge_IsTheStaticFormulaOnItsOwnNumbers()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond);
            var task = MakeTask("Travel", 1f);
            task.escalatingCharge = 2f;
            task.chargeGrowth = 1.15f;

            Assert.That(sim.EscalatingChargeAfter(task, 3), Is.EqualTo(Simulation.EscalatingChargeAfter(2f, 1.15f, 3)));
        }

        // ---------- The stat a task trains ----------

        [Test]
        public void StatTrainedBy_Static_FollowsTheSameRulesAsTheSimulations()
        {
            var explore = MakeTask("Search", 1f);
            var study = MakeTask("Study", 1f, ClaraAttribute.Scholarship);
            var named = MakeTask("Named", 1f);
            named.trainsAttribute = ClaraAttribute.Composure;
            var plain = MakeTask("Plain", 1f);
            var content = Make<GameContent>();
            content.exploreVerb = explore;
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);

            foreach (var (task, expected) in new[]
            {
                (explore, ClaraAttribute.Perception), (study, ClaraAttribute.Scholarship),
                (named, ClaraAttribute.Composure), (plain, ClaraAttribute.None), ((TaskDefinition)null, ClaraAttribute.None),
            })
            {
                Assert.That(Simulation.StatTrainedBy(task, explore), Is.EqualTo(expected));
                Assert.That(sim.StatTrainedBy(task), Is.EqualTo(expected));
            }
        }

        // ---------- How long an entry repeats ----------

        private Simulation RepeatSim(out NodeDefinition room, out TaskDefinition explore)
        {
            room = MakeNode("Hall");
            var content = MakePlaces(room);
            explore = content.exploreVerb = MakeTask("Search", 1f);
            return new Simulation(MakeLoopSettings(), TicksPerSecond, content);
        }

        [Test]
        public void RepeatKindOf_ATripIsATrip_WhateverTheTask()
        {
            var sim = RepeatSim(out var room, out _);

            Assert.That(sim.RepeatKindOf(sim.TravelVerb, room, null), Is.EqualTo(RepeatKind.Trip));
        }

        [Test]
        public void RepeatKindOf_SingleAction_OncePerRun_PickUp_PutDown()
        {
            var sim = RepeatSim(out _, out _);
            var single = MakeTask("Single", 1f);
            single.singleAction = true;
            var once = MakeTask("Once", 1f);
            once.oncePerRun = true;
            var pick = MakeTask("Pick up", 1f);
            pick.picksUp = MakeObject("Candle");
            var put = MakeTask("Put down", 1f);
            put.putsDown = MakeObject("Candle");

            Assert.That(sim.RepeatKindOf(single, null, null), Is.EqualTo(RepeatKind.SingleAction));
            Assert.That(sim.RepeatKindOf(once, null, null), Is.EqualTo(RepeatKind.OncePerRun));
            Assert.That(sim.RepeatKindOf(pick, null, null), Is.EqualTo(RepeatKind.PickUp));
            Assert.That(sim.RepeatKindOf(put, null, null), Is.EqualTo(RepeatKind.PutDown));
        }

        [Test]
        public void RepeatKindOf_ASearchInARoom_IsExplore_ButWithNoRoomItIsOrdinary()
        {
            var sim = RepeatSim(out var room, out var explore);

            Assert.That(sim.RepeatKindOf(explore, null, room), Is.EqualTo(RepeatKind.Explore));
            Assert.That(sim.RepeatKindOf(explore, null, null), Is.EqualTo(RepeatKind.Forever));
        }

        [Test]
        public void RepeatKindOf_GivingALimitedThing_IsUntilFull_AndAnUnlimitedOneForever()
        {
            var sim = RepeatSim(out _, out _);
            var wisp = MakeObject("Wisp"); // an object: her pockets are the limit
            var dust = MakeResource("Dust"); // a plain count with no maximum
            var gatherWisp = MakeGatherTask("Gather a wisp", 1f, wisp);
            var gatherDust = MakeGatherTask("Sweep", 1f, dust);

            Assert.That(sim.RepeatKindOf(gatherWisp, null, null), Is.EqualTo(RepeatKind.UntilFull));
            Assert.That(sim.RepeatKindOf(gatherDust, null, null), Is.EqualTo(RepeatKind.Forever));
        }
    }
}
