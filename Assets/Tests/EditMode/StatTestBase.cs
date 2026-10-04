using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Shared set-up for the tests of stats, skills and the drain (AttributeTests, DrainTests): two
    /// skills and loop settings with round numbers.
    /// </summary>
    public abstract class StatTestBase : SimulationTestBase
    {
        protected SkillDefinition _wayfinding, _studying;

        [SetUp]
        public void SetUpSkills()
        {
            _wayfinding = MakeSkill("Wayfinding");
            _studying = MakeSkill("Studying");
        }

        // Round numbers so the tests are easy to follow: a 10s task (100 ticks) gives 100 XP, exactly
        // 1 per tick, so there's no rounding drift. Level 1 at 100 XP, each level needs double the
        // last; each skill level makes its tasks ×1.1; no mastery unless asked.
        protected LoopSettings MakeStatSettings(float masteryShare = 0f)
        {
            var s = MakeLoopSettings(vitalityMax: 1000f);
            s.pools.Add(HuePool(Hue.Amber, 500f));
            s.xpPerSecondOfTask = 10f; // a 10s task gives 100
            s.xpForFirstLevel = s.masteryXpForFirstLevel = 100f;
            s.xpGrowthPerLevel = 2f;
            s.xpMinIncreasePerLevel = 0f;
            s.skillSpeedPerLevel = 1.1f;
            s.skillMasterySpeedPerLevel = 1f;
            s.attunementPerLevel = 0.1f;
            s.composureActionCostPerLevel = 0.1f;
            s.masteryShare = masteryShare;
            return s;
        }

        protected Simulation Begin(LoopSettings settings, GameContent content = null)
        {
            var sim = new Simulation(settings, TicksPerSecond, content);
            sim.BeginLoop();
            return sim;
        }

        protected TaskDefinition MakeSkillTask(string name, float seconds, SkillDefinition skill,
            ClaraAttribute attribute = ClaraAttribute.None)
        {
            var task = MakeTask(name, seconds, attribute);
            task.skill = skill;
            return task;
        }

        protected LoopSettings DrainSettings()
        {
            var s = MakeStatSettings();
            s.vitalityDrainPerSecond = 0.5f;
            return s;
        }
    }
}
