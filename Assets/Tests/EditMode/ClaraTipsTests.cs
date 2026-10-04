using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>A stat's and a skill's pop-up: the table of this run's level, mastery and the total.</summary>
    public class ClaraTipsTests : SimulationTestBase
    {
        private SkillDefinition _wayfinding;

        [SetUp]
        public void SetUpSkill() => _wayfinding = MakeSkill("Wayfinding");

        // Level 1 at 100 XP, level 2 at 300, level 3 at 700 (each level needs double the last).
        private Simulation Begin()
        {
            var settings = MakeLoopSettings(); // ×1.05 a skill level, ×1.01 a mastery level
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 100f;
            settings.xpGrowthPerLevel = 2f;
            settings.xpMinIncreasePerLevel = 0f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.BeginLoop();
            return sim;
        }

        /// <summary>The line of the tip that starts with this row's label (from the text file).</summary>
        private static string Row(string tip, string key)
        {
            string label = GameText.Get(key);
            foreach (string line in tip.Split('\n'))
                if (line.StartsWith(label))
                    return line;
            Assert.Fail($"no {label} row in:\n{tip}");
            return null;
        }

        [Test]
        public void ASkillTip_ShowsLevelMasteryAndTotal()
        {
            var sim = Begin();
            sim.Loop.SkillXp[_wayfinding] = 340f;              // level 2, 40 of 400 towards level 3
            sim.Persistent.SkillMasteryXp[_wayfinding] = 715f; // mastery 3, 15 of 800 towards the next

            string tip = ClaraTips.SkillTip(sim, _wayfinding);

            string run = Row(tip, "tips.row_run");
            Assert.That(run, Does.Contain(" 2</mspace>").And.Contain("40 / 400"));
            Assert.That(run, Does.Contain(UiText.Rate(1.05f * 1.05f)), "this run's part of the speed");
            string mastery = Row(tip, "tips.row_mastery");
            Assert.That(mastery, Does.Contain(" 3</mspace>").And.Contain("15 / 800"));
            Assert.That(mastery, Does.Contain(UiText.Rate(1.01f * 1.01f * 1.01f)), "mastery's part of the speed");
            Assert.That(Row(tip, "tips.row_total"), Does.Contain(UiText.Rate(sim.SpeedMultiplierFor(_wayfinding))));
        }

        [Test]
        public void AStatTipTotal_ShowsWhatItDoes()
        {
            var sim = Begin();
            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 100f;                 // level 1
            sim.Persistent.AttributeMasteryXp[ClaraAttribute.Endurance] = 300f;    // mastery 2

            string tip = ClaraTips.StatTip(sim, ClaraAttribute.Endurance);

            string total = Row(tip, "tips.row_total");
            Assert.That(total, Does.Contain(" 3</mspace>"), "its strength: level plus mastery");
            Assert.That(total, Does.EndWith(ClaraTips.StatEffect(sim, ClaraAttribute.Endurance)), "what it does now");
            Assert.That(Row(tip, "tips.row_run"), Does.Contain(" 1</mspace>"));
            Assert.That(Row(tip, "tips.row_mastery"), Does.Contain(" 2</mspace>"));
        }

        [Test]
        public void StatEffect_ShowsNewJobs()
        {
            var sim = Begin();
            sim.Persistent.KeptVitality = 7f;

            Assert.That(ClaraTips.StatEffect(sim, ClaraAttribute.Endurance), Does.Contain("7"), "the banked vitality");
            Assert.That(ClaraTips.StatEffect(sim, ClaraAttribute.Perception), Is.EqualTo(GameText.Get("stats.effect_perception")));
            Assert.That(ClaraTips.StatEffect(sim, ClaraAttribute.Composure), Does.Contain(UiText.Rate(sim.ComposureGrowthMultiplier)));
            Assert.That(ClaraTips.StatEffect(sim, ClaraAttribute.Attunement), Does.Contain(UiText.Rate(sim.AttunementRestoreMultiplier)));

            sim.Settings.asleepAttributes.Add(ClaraAttribute.Attunement);
            Assert.That(ClaraTips.StatEffect(sim, ClaraAttribute.Attunement), Is.EqualTo(GameText.Get("stats.effect_attunement_asleep")));
            Assert.That(ClaraTips.StatTip(sim, ClaraAttribute.Attunement), Does.Contain(GameText.Get("tips.attunement_asleep")));
        }
    }
}
