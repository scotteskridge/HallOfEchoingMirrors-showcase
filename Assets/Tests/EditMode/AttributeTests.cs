using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Stats and skills: XP, levels, mastery, speed, and the passive vitality drain.</summary>
    public class AttributeTests : StatTestBase
    {
        // ---------- The XP curve ----------

        [Test]
        public void TheXpCurve_NeedsMoreForEachLevel()
        {
            // Level 1 at 10 XP, level 2 at 10 + 20 = 30, level 3 at 30 + 40 = 70.
            var curve = new XpCurve(10f, 2f, 0f);
            Assert.That(AttributeMath.LevelFor(9f, curve), Is.EqualTo(0));
            Assert.That(AttributeMath.LevelFor(10f, curve), Is.EqualTo(1));
            Assert.That(AttributeMath.LevelFor(29f, curve), Is.EqualTo(1));
            Assert.That(AttributeMath.LevelFor(30f, curve), Is.EqualTo(2));
            Assert.That(AttributeMath.LevelFor(70f, curve), Is.EqualTo(3));
            Assert.That(AttributeMath.ProgressToNext(20f, curve), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(AttributeMath.Breakdown(35f, curve), Is.EqualTo((2, 5f, 40f)), "level 2, 5 of the 40 the next needs");
        }

        [Test]
        public void TheXpCurve_GrowsByAtLeastTheMinimum()
        {
            // 1% more each level would be 10.1, but never less than 0.5 more: 10, 10.5, 11...
            var curve = new XpCurve(10f, 1.01f, 0.5f);
            Assert.That(AttributeMath.XpForLevel(3, curve), Is.EqualTo(10f + 10.5f + 11f).Within(0.001f));
            Assert.That(AttributeMath.LevelFor(20.4f, curve), Is.EqualTo(1));
            Assert.That(AttributeMath.LevelFor(20.5f, curve), Is.EqualTo(2));

            // Once 1% is more than 0.5 (above 50 XP a level), the percentage takes over.
            var big = new XpCurve(60f, 1.01f, 0.5f);
            Assert.That(AttributeMath.XpForLevel(2, big), Is.EqualTo(60f + 60.6f).Within(0.001f));
        }

        [Test]
        public void TheCodeDefaults_MatchTheAgreedNumbers()
        {
            var s = Make<LoopSettings>();
            // Doubled, with the durations halved, by plan 022b (game time halved): same XP per task, same run length.
            Assert.That(s.vitalityDrainPerSecond, Is.EqualTo(1f));
            Assert.That(s.drainGrowthPerMinute, Is.EqualTo(0.5625f)); // 1.25 squared, minus 1
            Assert.That(s.xpPerSecondOfTask, Is.EqualTo(1.5f));
            Assert.That(s.xpForFirstLevel, Is.EqualTo(10f));
            Assert.That(s.masteryXpForFirstLevel, Is.EqualTo(20f));
            Assert.That(s.xpGrowthPerLevel, Is.EqualTo(1.01f));
            Assert.That(s.xpMinIncreasePerLevel, Is.EqualTo(0.5f));
            Assert.That(s.skillSpeedPerLevel, Is.EqualTo(1.05f));
            Assert.That(s.skillMasterySpeedPerLevel, Is.EqualTo(1.01f));
            Assert.That(s.chargeActionCosts, Is.False);
            Assert.That(s.chargeCarryCosts, Is.False);
        }

        // ---------- XP ----------

        [Test]
        public void FinishingATask_GivesItsXp_ToItsSkillAndItsStat_AndTheSkillsMastery()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.xpPerSecondOfTask = 0.2f; // the 10s read gives 2
            var sim = Begin(settings);
            sim.Schedule(MakeSkillTask("Read", 10f, _studying, ClaraAttribute.Scholarship), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.XpOf(_studying), Is.EqualTo(2f).Within(0.001f));
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Scholarship), Is.EqualTo(2f).Within(0.001f));
            Assert.That(sim.Persistent.SkillMasteryXpOf(_studying), Is.EqualTo(2f).Within(0.001f));
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(0f), "the stat's mastery waits for the run's end");
        }

        [Test]
        public void TheUsualXp_Is150PercentOfTheTasksBaseSeconds_RoundedUp()
        {
            var sim = Begin(MakeLoopSettings());

            // 1.5 XP a second of base time (was 0.75 before game time was halved: the same XP per task).
            Assert.That(sim.XpRewardOf(MakeTask("Search", 10f), null), Is.EqualTo(15f));
            Assert.That(sim.XpRewardOf(MakeTask("Gather a wisp", 3f), null), Is.EqualTo(5f), "4.5 rounds up");
            Assert.That(sim.XpRewardOf(MakeTask("Light a candle", 1f), null), Is.EqualTo(2f), "1.5 rounds up");
        }

        [Test]
        public void Xp_ArrivesSmoothly_WhileTheTaskRuns()
        {
            var sim = Begin(MakeStatSettings());
            sim.Schedule(MakeSkillTask("Read", 10f, _studying), 1);

            RunSeconds(sim, 2.5f);

            Assert.That(sim.Loop.XpOf(_studying), Is.EqualTo(25f).Within(0.01f), "a quarter of the way: a quarter of the XP");
        }

        [Test]
        public void ATasksOwnXpReward_OverridesTheUsualAmount()
        {
            var sim = Begin(MakeStatSettings());
            var longRead = MakeSkillTask("Read the whole book", 10f, _studying);
            longRead.xpReward = 250f;
            sim.Schedule(longRead, 1);

            RunSeconds(sim, 30f);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(longRead));
            Assert.That(sim.Loop.XpOf(_studying), Is.EqualTo(250f).Within(0.01f));
        }

        [Test]
        public void ATaskWithNoSkillOrStat_TrainsNothing()
        {
            var sim = Begin(MakeStatSettings(masteryShare: 1f));
            sim.Schedule(MakeTask("Wait", 10f, ClaraAttribute.None), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.AttributeXp, Is.Empty);
            Assert.That(sim.Loop.SkillXp, Is.Empty);
            Assert.That(sim.Persistent.SkillMasteryXp, Is.Empty);
        }

        [Test]
        public void XpFollowsWorkDone_SoAFasterTaskIsWorthTheSameXp()
        {
            var sim = Begin(MakeStatSettings());
            var walk = MakeSkillTask("Walk", 30f, _wayfinding);
            walk.xpReward = 300f; // Wayfinding 1 a third of the way through: it finishes early
            sim.Schedule(walk, 1);

            RunSeconds(sim, 30f);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(walk));
            Assert.That(sim.Loop.XpOf(_wayfinding), Is.EqualTo(300f).Within(0.01f));
        }

        [Test]
        public void Levels_ResetEachLoop_ButMasteryIsKept()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.collapseStatXpShare = 1f; // so the stat's whole 100 XP settles when the run ends
            var sim = Begin(settings);
            sim.Schedule(MakeSkillTask("Search", 10f, _wayfinding, ClaraAttribute.Perception), 1);
            RunSeconds(sim, 10f);
            Assert.That(sim.LevelOf(_wayfinding), Is.EqualTo(1));
            Assert.That(sim.LevelOf(ClaraAttribute.Perception), Is.EqualTo(1));

            sim.EndRunEarly();
            sim.BeginLoop();

            Assert.That(sim.LevelOf(_wayfinding), Is.EqualTo(0));
            Assert.That(sim.LevelOf(ClaraAttribute.Perception), Is.EqualTo(0));
            Assert.That(sim.MasteryOf(_wayfinding), Is.EqualTo(1));
            Assert.That(sim.MasteryOf(ClaraAttribute.Perception), Is.EqualTo(1));
        }

        // ---------- Stats train from their theme ----------

        [Test]
        public void EachKindOfAction_TrainsItsStat()
        {
            var sim = Begin(MakeStatSettings());
            var search = MakeTask("Search", 1f, ClaraAttribute.Perception);
            var study = MakeTask("Study", 1f, ClaraAttribute.Scholarship);
            var make = MakeTask("Instantiate", 1f, ClaraAttribute.Attunement);
            var gather = MakeTask("Gather", 1f);
            gather.kind = TaskKind.Gather;

            Assert.That(sim.StatTrainedBy(search), Is.EqualTo(ClaraAttribute.Perception));
            Assert.That(sim.StatTrainedBy(study), Is.EqualTo(ClaraAttribute.Scholarship));
            Assert.That(sim.StatTrainedBy(make), Is.EqualTo(ClaraAttribute.Attunement));
            Assert.That(sim.StatTrainedBy(gather), Is.EqualTo(ClaraAttribute.None), "gathering trains its skill only");
        }

        [Test]
        public void Endurance_TrainsFromVitalityLost()
        {
            var settings = MakeStatSettings();
            settings.enduranceXpPerVitalityLost = 0.2f;
            var sim = Begin(settings);
            sim.Schedule(MakeTiringTask("Run", 10f, vitalityCost: 50f), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.XpOf(ClaraAttribute.Endurance), Is.EqualTo(10f).Within(0.01f), "50 lost × 0.2");
        }

        [Test]
        public void Composure_TrainsWithTime_AndMoreWhileCarryingWhatCostsHer()
        {
            var settings = MakeStatSettings();
            settings.composureXpPerSecond = 0.05f;
            settings.composureXpPerSecondCarrying = 0.1f;
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 0.5f;
            ring.alwaysCharged = true;
            var sim = Begin(settings);
            KeepBusy(sim, 10f);

            RunSeconds(sim, 10f);
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(0.5f).Within(0.01f), "10s × 0.05");

            sim.Loop.ToolsAndStats[ring] = 1;
            sim.Schedule(MakeTask("Walk", 10f), 1); // the ring costs her while she acts
            RunSeconds(sim, 10f);
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(0.5f + 1.5f).Within(0.02f), "then 10s × 0.15");
        }

        // ---------- Speed ----------

        [Test]
        public void ASkillLevel_SpeedsUpItsNextTask()
        {
            var sim = Begin(MakeStatSettings());
            sim.Schedule(MakeSkillTask("Walk", 10f, _wayfinding), 1); // ticks 1-100: Wayfinding 1
            var second = MakeSkillTask("Walk on", 10f, _wayfinding);
            sim.Schedule(second, 1);

            // 100 work at ×1.1 takes 91 ticks: done at tick 191, not 200.
            for (int i = 0; i < 190; i++)
                sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(second), "not yet at tick 190");

            sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.Member(second), "done at tick 191");
        }

        [Test]
        public void SecondsAtSpeedNow_CountsTheWholeTicksItReallyTakes()
        {
            var sim = Begin(MakeStatSettings());
            var walk = MakeSkillTask("Walk on", 10f, _wayfinding);
            var blink = MakeSkillTask("Blink", 0.1f, _wayfinding); // one tick at normal speed

            Assert.That(sim.SecondsAtSpeedNow(walk, null), Is.EqualTo(10f).Within(0.0001f), "normal speed: its own time");

            sim.Loop.SkillXp[_wayfinding] = 100f; // Wayfinding 1: ×1.1
            // 100 work at ×1.1 is 91 ticks, as ASkillLevel_SpeedsUpItsNextTask runs it.
            Assert.That(sim.SecondsAtSpeedNow(walk, null), Is.EqualTo(9.1f).Within(0.0001f));
            Assert.That(sim.SecondsAtSpeedNow(blink, null), Is.EqualTo(0.1f).Within(0.0001f), "never quicker than one tick");
        }

        [Test]
        public void ALevelUpMidTask_SpeedsUpTheTaskAlreadyRunning()
        {
            var sim = Begin(MakeStatSettings());
            var climb = MakeSkillTask("Climb", 30f, _wayfinding); // 300 work at normal speed
            climb.xpReward = 300f;
            sim.Schedule(climb, 1);

            // Ticks 1-100 at ×1 (100 work, Wayfinding 1). Then ×1.1: the other 200 work
            // takes 182 ticks, so the climb finishes at tick 282 instead of 300.
            for (int i = 0; i < 281; i++)
                sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(climb), "not yet at tick 281");

            sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.Member(climb), "done at tick 282");
        }

        [Test]
        public void ASkill_OnlySpeedsUpItsOwnTasks_AndStatsSpeedNothing()
        {
            var sim = Begin(MakeStatSettings());
            sim.Schedule(MakeSkillTask("Walk", 10f, _wayfinding, ClaraAttribute.Scholarship), 1); // Wayfinding 1, Scholarship 1
            var read = MakeSkillTask("Read", 10f, _studying, ClaraAttribute.Scholarship);
            sim.Schedule(read, 1);

            // Read runs at ×1: 100 ticks, from tick 101 to 200.
            for (int i = 0; i < 199; i++)
                sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(read), "Wayfinding and Scholarship don't speed up reading");

            sim.Tick();
            Assert.That(sim.Loop.CompletedTasks, Has.Member(read));
        }

        [Test]
        public void SkillLevel_AndSkillMastery_MultiplyTogether()
        {
            var settings = MakeLoopSettings(); // the real speed numbers: ×1.05 a level, ×1.01 a mastery level
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 100f;
            settings.xpGrowthPerLevel = 2f;
            settings.xpMinIncreasePerLevel = 0f;
            var sim = Begin(settings);
            var walk = MakeSkillTask("Walk", 10f, _wayfinding);
            sim.Loop.SkillXp[_wayfinding] = 300f;                 // level 2
            sim.Persistent.SkillMasteryXp[_wayfinding] = 700f;    // mastery 3

            Assert.That(sim.SpeedMultiplierFor(walk), Is.EqualTo(1.05f * 1.05f * 1.01f * 1.01f * 1.01f).Within(0.0001f));
            Assert.That(sim.SpeedMultiplierFor(MakeTask("Wait", 1f)), Is.EqualTo(1f), "no skill: normal speed");
        }

        [Test]
        public void SkillSpeed_SplitsIntoItsLevelPart_AndItsMasteryPart()
        {
            var settings = MakeLoopSettings(); // ×1.05 a level, ×1.01 a mastery level
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 100f;
            settings.xpGrowthPerLevel = 2f;
            settings.xpMinIncreasePerLevel = 0f;
            var sim = Begin(settings);
            sim.Loop.SkillXp[_wayfinding] = 300f;                 // level 2
            sim.Persistent.SkillMasteryXp[_wayfinding] = 700f;    // mastery 3

            Assert.That(sim.LevelSpeedFor(_wayfinding), Is.EqualTo(1.05f * 1.05f).Within(0.0001f));
            Assert.That(sim.MasterySpeedFor(_wayfinding), Is.EqualTo(1.01f * 1.01f * 1.01f).Within(0.0001f));
            Assert.That(sim.LevelSpeedFor(_wayfinding) * sim.MasterySpeedFor(_wayfinding),
                Is.EqualTo(sim.SpeedMultiplierFor(_wayfinding)).Within(0.0001f));
        }

        [Test]
        public void SkillMastery_MakesItsTasksFaster_InLaterLoops()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.skillSpeedPerLevel = 1f;
            settings.skillMasterySpeedPerLevel = 1.1f;
            var sim = Begin(settings);
            var walk = MakeSkillTask("Walk", 10f, _wayfinding);
            sim.Schedule(walk, 1);
            RunSeconds(sim, 10f);
            Assert.That(sim.SpeedMultiplierFor(walk), Is.EqualTo(1.1f).Within(0.0001f), "mastery 1 already");

            sim.BeginLoop();

            Assert.That(sim.LevelOf(_wayfinding), Is.EqualTo(0));
            Assert.That(sim.SpeedMultiplierFor(walk), Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void MoreMastery_MeansMoreDoneInTheSameRun()
        {
            // Each run lasts 100 seconds: 100 vitality, draining 1 a second.
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.vitalityMax = 100f;
            settings.vitalityDrainPerSecond = 1f;
            settings.skillSpeedPerLevel = 1f;
            settings.skillMasterySpeedPerLevel = 1.1f;
            var sim = new Simulation(settings, TicksPerSecond);
            var search = MakeSkillTask("Search", 10f, _wayfinding);

            var doneEachRun = new List<int>();
            for (int run = 0; run < 3; run++)
            {
                sim.Schedule(search); // the same plan each run: search until she's spent
                sim.BeginLoop();
                for (int i = 0; i < 2000 && sim.Phase == LoopPhase.Running; i++)
                    sim.Tick();
                Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
                doneEachRun.Add(sim.Loop.CompletionLog.Count);
            }

            Assert.That(doneEachRun[1], Is.GreaterThan(doneEachRun[0]));
            Assert.That(doneEachRun[2], Is.GreaterThan(doneEachRun[1]));
        }

        [Test]
        public void Mastery_HasItsOwnFirstLevel()
        {
            // Levels start at 10 XP, mastery at 20: 15 XP is level 1 but not yet mastery 1.
            var track = new XpTrack<ClaraAttribute>(new Dictionary<ClaraAttribute, float>(), new Dictionary<ClaraAttribute, float>(),
                new XpCurve(10f, 1.01f, 0.5f), new XpCurve(20f, 1.01f, 0.5f), masteryShare: 1f);

            track.Gain(ClaraAttribute.Endurance, 15f);
            Assert.That(track.Level(ClaraAttribute.Endurance), Is.EqualTo(1));
            Assert.That(track.Mastery(ClaraAttribute.Endurance), Is.EqualTo(0));

            track.Gain(ClaraAttribute.Endurance, 5f);
            Assert.That(track.Mastery(ClaraAttribute.Endurance), Is.EqualTo(1));
        }

        // ---------- Mastery has no cap (plan 041) ----------

        [Test]
        public void Mastery_KeepsRisingPastTheOldCap()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.xpGrowthPerLevel = 1.01f; // the real, gentle curve: 150 levels is about 34,000 XP
            var sim = Begin(settings);
            var climb = MakeSkillTask("Search", 50f, _wayfinding, ClaraAttribute.Perception);
            climb.xpReward = 200000f;
            sim.Schedule(climb, 1);

            RunSeconds(sim, 50f);
            sim.EndRunEarly(); // a stat's mastery settles when the run ends

            Assert.That(sim.MasteryOf(_wayfinding), Is.GreaterThan(150), "a skill's mastery isn't stopped at 100");
            Assert.That(sim.MasteryOf(ClaraAttribute.Perception), Is.GreaterThan(150), "nor a stat's");
        }

        [Test]
        public void SmallGains_StillCount_AtHighMastery()
        {
            // Float XP rounds small gains away past about 16 million: mastery stays far below that.
            var masteryXp = new Dictionary<ClaraAttribute, float>();
            var track = new XpTrack<ClaraAttribute>(new Dictionary<ClaraAttribute, float>(), masteryXp,
                new XpCurve(10f, 1.01f, 0.5f), new XpCurve(20f, 1.01f, 0.5f), masteryShare: 1f);
            masteryXp[ClaraAttribute.Endurance] = AttributeMath.XpForLevel(200, new XpCurve(20f, 1.01f, 0.5f));
            float before = masteryXp[ClaraAttribute.Endurance];

            track.Gain(ClaraAttribute.Endurance, 1f);

            Assert.That(masteryXp[ClaraAttribute.Endurance], Is.GreaterThan(before), "one XP still lands at mastery 200");
            Assert.That(track.Mastery(ClaraAttribute.Endurance), Is.GreaterThanOrEqualTo(200));
        }

        // ---------- Stat mastery settles at a run's end (plan 041) ----------

        private LoopSettings SettlingSettings()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.collapseStatXpShare = 0.5f;
            return settings;
        }

        [Test]
        public void StatMastery_HeldDuringTheRun()
        {
            var sim = Begin(SettlingSettings());
            sim.Schedule(MakeTask("Read", 10f, ClaraAttribute.Scholarship), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.LevelOf(ClaraAttribute.Scholarship), Is.EqualTo(1), "this run's level rises");
            Assert.That(sim.MasteryOf(ClaraAttribute.Scholarship), Is.EqualTo(0));
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(0f));
        }

        [Test]
        public void SettledMastery_CanFlipAStatSwitch_AtTheRunsEnd()
        {
            var reach = Make<SwitchDefinition>();
            reach.trigger = SwitchTrigger.AttributeLevelReached;
            reach.triggerAttribute = ClaraAttribute.Scholarship;
            reach.triggerAmount = 1;
            var content = Make<GameContent>();
            content.switches.Add(reach);
            var sim = Begin(SettlingSettings(), content);
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 50f; // level 0: the run's own XP is short of level 1
            sim.Persistent.AttributeMasteryXp[ClaraAttribute.Scholarship] = 90f; // 10 short of mastery 1
            sim.Schedule(MakeTask("Wait", 10f), 1);
            RunSeconds(sim, 1f);
            Assert.That(sim.IsFlipped(reach), Is.False, "set-up: not reached mid-run");

            sim.EndRunEarly(); // settles half of 50: 25 more, so mastery 1

            Assert.That(sim.IsFlipped(reach), Is.True);
        }

        [Test]
        public void WalkOut_SettlesAllStatXp()
        {
            var sim = Begin(SettlingSettings());
            var leave = MakeTask("Leave", 10f, ClaraAttribute.Scholarship);
            leave.walksOut = true;
            sim.Schedule(leave, 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut));
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(100f).Within(0.01f));
            Assert.That(sim.MasteryOf(ClaraAttribute.Scholarship), Is.EqualTo(1));
        }

        [Test]
        public void Exhausted_SettlesTheCollapseShare()
        {
            var sim = Begin(SettlingSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 200f;
            sim.Schedule(MakeTiringTask("Strain", 4f, vitalityCost: 2000f), 1);

            RunSeconds(sim, 4f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(100f).Within(0.01f), "half of 200");
        }

        [Test]
        public void EndRun_SettlesTheCollapseShare()
        {
            var sim = Begin(SettlingSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 200f;
            sim.Schedule(MakeTask("Wait", 10f), 1);
            RunSeconds(sim, 1f);

            sim.EndRunEarly();

            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void DevRestart_SettlesNothing()
        {
            var sim = Begin(SettlingSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 200f;
            sim.Schedule(MakeTask("Wait", 10f), 1);
            RunSeconds(sim, 1f);

            sim.BeginLoop(); // restarts a run that hadn't ended, so it never reaches EndLoop

            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(0f));
        }

        [Test]
        public void SkillMastery_StillRisesLive()
        {
            var sim = Begin(SettlingSettings());
            sim.Schedule(MakeSkillTask("Study", 10f, _studying, ClaraAttribute.Scholarship), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Persistent.SkillMasteryXpOf(_studying), Is.EqualTo(100f).Within(0.01f));
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.EqualTo(0f), "while the stat's waits");
        }

        [Test]
        public void RunReport_ShowsSettledStatMastery()
        {
            var sim = Begin(SettlingSettings());
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 200f;
            sim.Schedule(MakeTask("Wait", 10f), 1);
            RunSeconds(sim, 1f);

            sim.EndRunEarly();

            var gain = sim.LastRun.Attributes.Find(a => a.Attribute == ClaraAttribute.Scholarship);
            Assert.That(gain.MasteryBefore, Is.EqualTo(0));
            Assert.That(gain.MasteryAfter, Is.EqualTo(1), "100 settled XP is mastery 1");
        }

        // ---------- What stats do (level this run + mastery) ----------

        [Test]
        public void Endurance_DoesNotLengthenTheBarMidRun()
        {
            var sim = Begin(MakeStatSettings());

            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 300f; // Endurance 2
            KeepBusy(sim, 1f);
            RunTicks(sim, 1);

            Assert.That(sim.Loop.Vitality.Max, Is.EqualTo(1000f).Within(0.001f), "the bar is fixed for the run; the bank is paid at its end");
        }

        // ---------- What she loses, and the bank (plan 031) ----------

        private LoopSettings BankSettings(float share = 0.1f, float perLevel = 0f)
        {
            var s = MakeStatSettings();
            s.vitalityDrainPerSecond = 0f; // only what a task costs is lost, so the sums are exact
            s.drainGrowthPerMinute = 0f;
            s.enduranceBankShare = share;
            s.enduranceBankPerLevel = perLevel;
            return s;
        }

        [Test]
        public void VitalityLost_CountsEverySource()
        {
            var settings = BankSettings();
            settings.vitalityDrainPerSecond = 1f; // the drain: 10 over 10s
            settings.enduranceXpPerVitalityLost = 1f;
            var sim = Begin(settings);
            var strain = MakeTiringTask("Strain", 10f, vitalityCost: 50f); // a cost: 50
            strain.extraDrainPerSecond = 2f; // its own extra drain: 20
            sim.Schedule(strain, 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.VitalityLostThisRun, Is.EqualTo(80f).Within(0.05f), "10 drain + 20 extra + 50 cost");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Endurance), Is.EqualTo(sim.Loop.VitalityLostThisRun).Within(0.05f), "the same count trains Endurance");
        }

        [Test]
        public void VitalityLost_IncludesTheRingsCarryCost()
        {
            var settings = BankSettings();
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 0.5f;
            ring.alwaysCharged = true;
            var sim = Begin(settings);
            sim.Loop.ToolsAndStats[ring] = 1;
            sim.Schedule(MakeTask("Walk", 10f), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.VitalityLostThisRun, Is.EqualTo(5f).Within(0.05f), "10s × 0.5");
        }

        [Test]
        public void Bank_GainsShareOfLossAtEveryEnding()
        {
            // Exhausted, ended early and walked out all bank the same formula.
            foreach (var ending in new[] { LoopEndReason.Exhausted, LoopEndReason.EndedByPlayer, LoopEndReason.WalkedOut })
            {
                var settings = BankSettings(share: 0.1f, perLevel: 0.5f);
                settings.vitalityMax = 100f;
                var sim = Begin(settings);
                sim.Persistent.AttributeMasteryXp[ClaraAttribute.Endurance] = 100f; // Endurance 1: share ×1.5
                var cost = ending == LoopEndReason.Exhausted ? 200f : 40f;
                var task = MakeTiringTask("Strain", 4f, vitalityCost: cost);
                if (ending == LoopEndReason.WalkedOut)
                    task.walksOut = true;
                sim.Schedule(task, 1);
                RunSeconds(sim, 4f);
                if (ending == LoopEndReason.EndedByPlayer)
                    sim.EndRunEarly();

                float lost = ending == LoopEndReason.Exhausted ? 100f : 40f;
                Assert.That(sim.Loop.EndReason, Is.EqualTo(ending));
                Assert.That(sim.Persistent.KeptVitality, Is.EqualTo(lost * 0.1f * 1.5f).Within(0.05f), ending.ToString());
                Assert.That(sim.LastRun.KeptVitalityGained, Is.EqualTo(sim.Persistent.KeptVitality).Within(0.001f));
            }
        }

        [Test]
        public void Bank_RaisesNextRunsMaxVitality()
        {
            var settings = BankSettings(share: 0.1f);
            settings.vitalityMax = 100f;
            var sim = Begin(settings);
            sim.Schedule(MakeTiringTask("Strain", 4f, vitalityCost: 40f), 1);
            RunSeconds(sim, 4f);
            sim.EndRunEarly();

            sim.BeginLoop();

            Assert.That(sim.Loop.Vitality.Max, Is.EqualTo(104f).Within(0.05f), "100 + 10% of the 40 lost");
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(104f).Within(0.05f), "and starts full");
        }

        [Test]
        public void Bank_DevRestartBanksNothing()
        {
            var settings = BankSettings(share: 0.1f);
            var sim = Begin(settings);
            sim.Schedule(MakeTiringTask("Strain", 4f, vitalityCost: 40f), 1);
            RunSeconds(sim, 2f);

            sim.BeginLoop(); // restarts a run that hadn't ended (dev tools), so it never reaches EndLoop

            Assert.That(sim.Persistent.KeptVitality, Is.EqualTo(0f));
        }

        // ---------- Asleep stats (plan 031) ----------

        [Test]
        public void AsleepStat_HasNoEffect()
        {
            var settings = BankSettings();
            settings.vitalityMax = 100f;
            settings.attunementRestorePerLevel = 0.5f;
            settings.asleepAttributes.Add(ClaraAttribute.Attunement);
            var wisp = MakeResource("Wisp", max: 5);
            wisp.restoreVitality = 10f;
            var sim = Begin(settings);
            sim.Loop.ToolsAndStats[wisp] = 1;
            sim.Loop.AttributeXp[ClaraAttribute.Attunement] = 300f; // Attunement 2: it would be strong if it were awake
            sim.Schedule(MakeTiringTask("Strain", 1f, vitalityCost: 20f), 1);
            KeepBusy(sim, 60f);

            RunSeconds(sim, 40f); // the strain, then the wisp has long run its course

            Assert.That(sim.IsAwake(ClaraAttribute.Attunement), Is.False);
            Assert.That(sim.StrengthOf(ClaraAttribute.Attunement), Is.GreaterThanOrEqualTo(2));
            Assert.That(sim.AttunementRestoreMultiplier, Is.EqualTo(1f));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 20f + 10f).Within(0.05f), "the wisp gives its own 10");
        }

        [Test]
        public void AsleepStat_GainsNoXp()
        {
            var settings = BankSettings();
            settings.enduranceXpPerVitalityLost = 1f;
            settings.composureXpPerSecond = 1f;
            settings.asleepAttributes.AddRange(new[] { ClaraAttribute.Attunement, ClaraAttribute.Endurance, ClaraAttribute.Composure });
            var sim = Begin(settings);
            sim.Schedule(MakeTask("Draw", 5f, ClaraAttribute.Attunement), 1);
            sim.Schedule(MakeTask("Look", 5f, ClaraAttribute.Perception), 1);
            sim.Schedule(MakeTiringTask("Strain", 5f, vitalityCost: 30f), 1);

            RunSeconds(sim, 15f);

            Assert.That(sim.Loop.XpOf(ClaraAttribute.Attunement), Is.EqualTo(0f), "task XP");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Endurance), Is.EqualTo(0f), "from the vitality she lost");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(0f), "per second");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Perception), Is.GreaterThan(0f), "an awake stat still earns");
        }

        [Test]
        public void AsleepEndurance_BanksNothing()
        {
            var settings = BankSettings(share: 0.1f);
            settings.vitalityMax = 100f;
            settings.asleepAttributes.Add(ClaraAttribute.Endurance);
            var sim = Begin(settings);
            sim.Schedule(MakeTiringTask("Strain", 4f, vitalityCost: 40f), 1);
            RunSeconds(sim, 4f);
            sim.EndRunEarly();

            Assert.That(sim.Persistent.KeptVitality, Is.EqualTo(0f));
            Assert.That(sim.LastRun.KeptVitalityGained, Is.EqualTo(0f));
            sim.BeginLoop();
            Assert.That(sim.Loop.Vitality.Max, Is.EqualTo(100f), "max vitality stays the base");
        }

        [Test]
        public void WakingSwitch_TurnsEffectsOnAtFullStrength()
        {
            var waking = Make<SwitchDefinition>();
            waking.trigger = SwitchTrigger.ResourceReached; // never reached by itself; the test flips it by hand
            waking.resourceToHold = MakeResource("Nothing", max: 1);
            waking.wakesAttributes.Add(ClaraAttribute.Attunement);
            var content = Make<GameContent>();
            content.switches.Add(waking);
            var settings = BankSettings();
            settings.asleepAttributes.Add(ClaraAttribute.Attunement);
            settings.attunementRestorePerLevel = 0.02f;
            var sim = Begin(settings, content);
            sim.Loop.AttributeXp[ClaraAttribute.Attunement] = 300f; // Attunement 2
            Assert.That(sim.AttunementRestoreMultiplier, Is.EqualTo(1f), "asleep");

            sim.Persistent.FlippedSwitches.Add(waking);

            Assert.That(sim.IsAwake(ClaraAttribute.Attunement), Is.True);
            Assert.That(sim.AttunementRestoreMultiplier, Is.EqualTo(1f + 0.02f * sim.StrengthOf(ClaraAttribute.Attunement)).Within(0.0001f),
                "at once, at the strength it built up asleep");
        }

        [Test]
        public void AnAsleepStat_HasNoLearningBonusForItsSkills()
        {
            var settings = MakeStatSettings();
            settings.statSkillXpPerLevel = 0.5f;
            settings.asleepAttributes.Add(ClaraAttribute.Endurance);
            _wayfinding.learnsFasterWith = ClaraAttribute.Endurance;
            var sim = Begin(settings);
            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 300f;
            Assert.That(sim.SkillXpMultiplierFor(_wayfinding), Is.EqualTo(1f));
        }

        // ---------- The stat's learning bonus and trains (plan 031) ----------

        [Test]
        public void StatXpBonus_BoostsRunLevelOnly()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.statSkillXpPerLevel = 0.5f;
            _wayfinding.learnsFasterWith = ClaraAttribute.Endurance;
            var sim = Begin(settings);
            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 300f; // Endurance 2: ×2
            sim.Schedule(MakeSkillTask("Walk", 10f, _wayfinding), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.XpOf(_wayfinding), Is.EqualTo(200f).Within(0.01f), "this run's level is boosted");
            Assert.That(sim.Persistent.SkillMasteryXpOf(_wayfinding), Is.EqualTo(100f).Within(0.01f), "mastery gets the plain XP");
        }

        [Test]
        public void TrainsAttribute_OverridesKind()
        {
            var sim = Begin(MakeStatSettings());
            var study = MakeTask("Study", 1f, ClaraAttribute.Scholarship);
            study.trainsAttribute = ClaraAttribute.Perception;

            Assert.That(sim.StatTrainedBy(study), Is.EqualTo(ClaraAttribute.Perception));
        }

        [Test]
        public void TrainsAttribute_NoneUsesKind()
        {
            var sim = Begin(MakeStatSettings());
            var study = MakeTask("Study", 1f, ClaraAttribute.Scholarship);

            Assert.That(study.trainsAttribute, Is.EqualTo(ClaraAttribute.None));
            Assert.That(sim.StatTrainedBy(study), Is.EqualTo(ClaraAttribute.Scholarship));
        }

        [Test]
        public void Endurance_LetsHerUseAnItem_EvenIfALittleOfItWouldBeWasted()
        {
            var settings = MakeStatSettings();
            settings.vitalityMax = 100f; // small numbers, so rounding can't nudge the sums
            settings.enduranceOverflowPerLevel = 1f;
            var wisp = MakeResource("Wisp", max: 5);
            wisp.restoreVitality = 5f;
            var sim = Begin(settings);
            sim.Loop.ToolsAndStats[wisp] = 1;
            sim.Schedule(MakeTiringTask("Strain", 1f, vitalityCost: 3.5f), 1);
            KeepBusy(sim, 5f);

            RunSeconds(sim, 1.5f);
            Assert.That(sim.AmountOf(wisp), Is.EqualTo(1), "missing 3.5 of a wisp's 5: too much would be wasted");

            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 300f; // Endurance 2: up to 2 may be wasted
            RunTicks(sim, 1);
            Assert.That(sim.AmountOf(wisp), Is.EqualTo(0), "3.5 missing + 2 allowed is enough");
        }

        [Test]
        public void Scholarship_GivesMoreFromEachStudy_EveryFewLevels()
        {
            var settings = MakeStatSettings();
            settings.scholarshipLevelsPerExtraStudy = 2;
            var understanding = MakeResource("Understanding");
            var study = MakeTask("Study the tome", 10f, ClaraAttribute.Scholarship);
            study.gives.Add(new ResourceAmount { resource = understanding, amount = 1 });
            var sim = Begin(settings);
            sim.Loop.AttributeXp[ClaraAttribute.Scholarship] = 300f; // Scholarship 2: +1 (the study's own 100 XP makes 400: still 2)
            var copy = MakeTask("Copy out a page", 10f);
            copy.kind = TaskKind.Gather;
            copy.gives.Add(new ResourceAmount { resource = understanding, amount = 1 });
            sim.Schedule(study, 1);
            sim.Schedule(copy, 1);

            RunSeconds(sim, 10f);
            Assert.That(sim.AmountOf(understanding), Is.EqualTo(2), "a study: one more");

            RunSeconds(sim, 10f);
            Assert.That(sim.AmountOf(understanding), Is.EqualTo(3), "not a study: just what it gives");
        }

        [Test]
        public void AStat_HelpsTheSkillsThatLearnWithIt()
        {
            var settings = MakeStatSettings();
            settings.statSkillXpPerLevel = 0.5f;
            _wayfinding.learnsFasterWith = ClaraAttribute.Endurance;
            var sim = Begin(settings);
            sim.Loop.AttributeXp[ClaraAttribute.Endurance] = 300f; // Endurance 2: ×2
            sim.Schedule(MakeSkillTask("Walk", 10f, _wayfinding), 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.XpOf(_wayfinding), Is.EqualTo(200f).Within(0.01f));
        }

        [Test]
        public void RepeatingAFreeTask_StillEndsTheRun()
        {
            var settings = DrainSettings();
            settings.vitalityMax = 10f;
            var sim = new Simulation(settings, TicksPerSecond);
            sim.Schedule(MakeTask("Wait", 1f)); // repeats forever: only the drain ends it
            sim.BeginLoop();

            RunSeconds(sim, 21f);

            Assert.That(sim.Loop.IsOver, Is.True);
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
        }

        [Test]
        public void WithCostsSwitchedOff_TasksOnlyCostTime()
        {
            var settings = MakeStatSettings();
            settings.chargeActionCosts = false;
            settings.chargeCarryCosts = false;
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 5f;
            var sim = Begin(settings);
            sim.Loop.ToolsAndStats[ring] = 1;
            var run = MakeTiringTask("Run", 10f, vitalityCost: 30f);
            sim.Schedule(run, 1);

            RunSeconds(sim, 10f);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(run));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(1000f).Within(0.001f));
            Assert.That(sim.CarryCostPerSecond(), Is.EqualTo(0f));
        }

        [Test]
        public void AnAlwaysChargedTask_StillCosts_WithCostsSwitchedOff()
        {
            var settings = MakeStatSettings();
            settings.chargeActionCosts = false;
            var sim = Begin(settings);
            var chase = MakeTiringTask("Chase", 10f, vitalityCost: 30f);
            chase.alwaysCharged = true;
            var walk = MakeTiringTask("Walk", 10f, vitalityCost: 30f);
            sim.Schedule(chase, 1);
            sim.Schedule(walk, 1);

            RunSeconds(sim, 20f);

            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(1000f - 30f).Within(0.01f), "the chase costs; the walk doesn't");
            Assert.That(sim.Charges(chase), Is.True);
            Assert.That(sim.Charges(walk), Is.False);
        }

        // ---------- Thresholds, switches, known skills and saving ----------

        [Test]
        public void AnAttributeThreshold_SkipsTheTask_UntilItIsMet()
        {
            var sim = Begin(MakeStatSettings());
            var notice = MakeTask("Notice the seam", 1f, ClaraAttribute.None);
            notice.requiresAttributes.Add(new TaskDefinition.AttributeRequirement { attribute = ClaraAttribute.Perception, level = 1 });
            var skipped = RecordRefusals(sim, withTaskName: false);

            sim.Schedule(notice, 1);
            RunSeconds(sim, 1f);
            Assert.That(skipped, Is.EqualTo(new[] { Reason("needs_attribute", ("attribute", GameText.Attribute(ClaraAttribute.Perception)), ("level", 1)) }));

            sim.Schedule(MakeTask("Search", 10f, ClaraAttribute.Perception), 1); // Perception 1
            sim.Schedule(notice, 1);
            RunSeconds(sim, 12f);
            Assert.That(sim.Loop.CompletedTasks, Has.Member(notice));
        }

        [Test]
        public void ReachingAnAttributeLevel_CanFlipASwitch()
        {
            var hidden = MakeTask("The hidden door", 5f, ClaraAttribute.None);
            hidden.startsUnlocked = false;
            var noticed = Make<SwitchDefinition>();
            noticed.trigger = SwitchTrigger.AttributeLevelReached;
            noticed.triggerAttribute = ClaraAttribute.Perception;
            noticed.triggerLevel = 2;
            noticed.unlocksTasks.Add(hidden);
            var content = Make<GameContent>();
            content.switches.Add(noticed);

            var sim = Begin(MakeStatSettings(), content);
            sim.Schedule(MakeTask("Search", 10f, ClaraAttribute.Perception), 10);

            RunSeconds(sim, 10f); // 100 XP
            Assert.That(sim.IsUnlocked(hidden), Is.False, "Perception 1");

            RunSeconds(sim, 30f); // 400 XP
            Assert.That(sim.IsUnlocked(hidden), Is.True, "Perception 2 notices it");
        }

        [Test]
        public void AStatLevelSwitch_CountsMastery_Too()
        {
            var noticed = Make<SwitchDefinition>();
            noticed.trigger = SwitchTrigger.AttributeLevelReached;
            noticed.triggerAttribute = ClaraAttribute.Perception;
            noticed.triggerLevel = 2;
            var content = Make<GameContent>();
            content.switches.Add(noticed);
            var sim = Begin(MakeStatSettings(), content);
            sim.Persistent.AttributeMasteryXp[ClaraAttribute.Perception] = 100f; // mastery 1

            sim.Schedule(MakeTask("Search", 10f, ClaraAttribute.Perception), 1);
            RunSeconds(sim, 10f); // level 1 this run

            Assert.That(sim.IsFlipped(noticed), Is.True, "level 1 + mastery 1 = 2");
        }

        [Test]
        public void OnlySkillsThatAnUnlockedTaskUses_AreKnown()
        {
            var read = MakeSkillTask("Read", 1f, _studying);
            var walk = MakeSkillTask("Walk", 1f, _wayfinding);
            walk.startsUnlocked = false;
            var content = Make<GameContent>();
            content.skills.AddRange(new[] { _wayfinding, _studying });
            content.tasks.AddRange(new[] { read, walk });

            var sim = new Simulation(MakeStatSettings(), TicksPerSecond, content);

            Assert.That(sim.KnownSkills(), Is.EqualTo(new[] { _studying }));
        }

        [Test]
        public void SkillsLearningFasterWith_ListsOnlyKnownSkillsThatTheStatSpeedsUp()
        {
            _wayfinding.learnsFasterWith = ClaraAttribute.Endurance;
            _studying.learnsFasterWith = ClaraAttribute.Scholarship;
            var unseen = MakeSkill("Gathering"); // learns with Endurance too, but no task uses it yet
            unseen.learnsFasterWith = ClaraAttribute.Endurance;
            var content = Make<GameContent>();
            content.skills.AddRange(new[] { _wayfinding, _studying, unseen });
            content.tasks.AddRange(new[] { MakeSkillTask("Walk", 1f, _wayfinding), MakeSkillTask("Read", 1f, _studying) });
            var sim = new Simulation(MakeStatSettings(), TicksPerSecond, content);

            Assert.That(sim.SkillsLearningFasterWith(ClaraAttribute.Endurance), Is.EqualTo(new[] { _wayfinding }));
            Assert.That(sim.SkillsLearningFasterWith(ClaraAttribute.Perception), Is.Empty);
        }

        [Test]
        public void TasksUsing_ListsUnlockedTasksOfTheSkill_TheCommonVerbsIncluded_AndNotLockedOnes()
        {
            var walk = MakeSkillTask("Walk", 1f, _wayfinding);
            var locked = MakeSkillTask("Run", 1f, _wayfinding);
            locked.startsUnlocked = false;
            var read = MakeSkillTask("Read", 1f, _studying);
            var content = Make<GameContent>();
            content.skills.AddRange(new[] { _wayfinding, _studying });
            content.tasks.AddRange(new[] { walk, locked, read });
            content.travelVerb = MakeSkillTask("Travel", 1f, _wayfinding);
            var sim = new Simulation(MakeStatSettings(), TicksPerSecond, content);

            Assert.That(sim.TasksUsing(_wayfinding), Is.EquivalentTo(new[] { content.travelVerb, walk }));
            Assert.That(sim.TasksUsing(_studying), Is.EqualTo(new[] { read }));
            Assert.That(sim.TasksUsing(null), Is.Empty);
        }

        [Test]
        public void ASkillOnlyAnUnfoundSearchFindUses_IsNotKnownYet()
        {
            // As the lab: its tasks start unlocked, but only show once its search finds them.
            var watch = MakeSkillTask("Watch him", 1f, _studying);
            var lab = MakeNode("The lab");
            lab.foundBySearching.Add(new RoomFind { task = watch, atSearched = 20 });
            var content = MakePlaces(lab);
            content.skills.Add(_studying);
            content.tasks.Add(watch);

            var sim = new Simulation(MakeStatSettings(), TicksPerSecond, content);

            Assert.That(sim.KnownSkills(), Is.Empty);
        }

        [Test]
        public void ASkillWithXp_StaysKnown_AfterItsTasksAreLocked()
        {
            var talk = MakeSkillTask("Talk to Roland", 1f, _studying);
            talk.startsUnlocked = false; // locked by the switch after the talk
            var content = Make<GameContent>();
            content.skills.Add(_studying);
            content.tasks.Add(talk);
            var sim = new Simulation(MakeStatSettings(), TicksPerSecond, content);
            sim.Persistent.SkillMasteryXp[_studying] = 5f;

            Assert.That(sim.KnownSkills(), Is.EqualTo(new[] { _studying }));
        }

        [Test]
        public void SkillMastery_SurvivesSavingAndLoading()
        {
            var read = MakeSkillTask("Read", 10f, _studying);
            var content = Make<GameContent>();
            content.skills.Add(_studying);
            content.tasks.Add(read);
            var sim = Begin(MakeStatSettings(masteryShare: 1f), content);
            sim.Schedule(read, 1);
            RunSeconds(sim, 10f);

            var warnings = new List<string>();
            var restored = SaveAndLoad(sim, content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(restored.SkillMasteryXpOf(_studying), Is.EqualTo(100f).Within(0.01f));
        }

        // ---------- Faster than last run (plan 042) ----------

        // Mastery only, ×1.1 a mastery level; each level also ×1.1, so a test sees it if levels leak in.
        private LoopSettings SinceLastRunSettings()
        {
            var settings = MakeStatSettings(masteryShare: 1f);
            settings.skillMasterySpeedPerLevel = 1.1f;
            return settings;
        }

        /// <summary>A game whose kept Wayfinding mastery XP is <paramref name="masteryXp"/> as its first run begins.</summary>
        private Simulation BeginWithMastery(LoopSettings settings, float masteryXp)
        {
            var kept = new PersistentState();
            kept.SkillMasteryXp[_wayfinding] = masteryXp;
            var sim = new Simulation(settings, TicksPerSecond, null, kept);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void EndingARun_KeepsItsStartingMastery()
        {
            foreach (var ending in new[] { LoopEndReason.WalkedOut, LoopEndReason.Exhausted, LoopEndReason.EndedByPlayer })
            {
                var sim = BeginWithMastery(SinceLastRunSettings(), 300f);
                var task = ending == LoopEndReason.Exhausted
                    ? MakeTiringTask("Strain", 10f, vitalityCost: 2000f)
                    : MakeTask("Walk", 10f);
                task.skill = _wayfinding;
                task.walksOut = ending == LoopEndReason.WalkedOut;
                sim.Schedule(task, 1);
                sim.Schedule(MakeTask("Wait", 10f), 1);
                RunSeconds(sim, 10f);
                if (ending == LoopEndReason.EndedByPlayer)
                    sim.EndRunEarly();

                Assert.That(sim.Loop.EndReason, Is.EqualTo(ending));
                Assert.That(sim.Persistent.SkillMasteryXpOf(_wayfinding), Is.GreaterThan(300f), $"{ending}: set-up, mastery rose this run");
                Assert.That(sim.Persistent.HasLastRunStart, Is.True, ending.ToString());
                Assert.That(sim.Persistent.SkillMasteryXpAtLastRunStartOf(_wayfinding), Is.EqualTo(300f), ending.ToString());
            }
        }

        [Test]
        public void ADevRestart_KeepsTheCutRunsStart()
        {
            var sim = BeginWithMastery(SinceLastRunSettings(), 300f);
            sim.Schedule(MakeSkillTask("Walk", 10f, _wayfinding), 1);
            RunSeconds(sim, 5f);

            sim.BeginLoop(); // restarts a run that hadn't ended, so it never reaches EndLoop

            Assert.That(sim.Persistent.SkillMasteryXpAtLastRunStartOf(_wayfinding), Is.EqualTo(300f));
            Assert.That(sim.Loop.SkillMasteryAtStart[_wayfinding], Is.GreaterThan(300f), "the new run starts with what the cut run added");
        }

        [Test]
        public void SinceLastRun_IsNull_BeforeAnyRunEnded()
        {
            var sim = BeginWithMastery(SinceLastRunSettings(), 300f);

            Assert.That(sim.SecondsFasterSinceLastRun(MakeSkillTask("Walk", 10f, _wayfinding), null), Is.Null);
        }

        [Test]
        public void SinceLastRun_ShowsTheMasteryGain()
        {
            var settings = SinceLastRunSettings();
            var sim = Begin(settings); // no Wayfinding mastery at all as run 1 begins: counts as 0
            var walk = MakeSkillTask("Walk", 10f, _wayfinding);
            sim.Schedule(walk, 1);
            RunSeconds(sim, 10f); // 100 mastery XP: mastery 1
            sim.EndRunEarly();
            float expected = 10f - 10f / 1.1f;

            Assert.That(sim.SecondsFasterSinceLastRun(walk, null), Is.EqualTo(expected).Within(0.0001f), "between runs: the next run against the last");

            sim.BeginLoop();
            Assert.That(sim.SecondsFasterSinceLastRun(walk, null), Is.EqualTo(expected).Within(0.0001f), "as the next run begins");

            sim.Schedule(walk, 3);
            RunSeconds(sim, 30f); // levels this run, and more mastery, both gained mid-run
            Assert.That(sim.LevelOf(_wayfinding), Is.GreaterThan(0), "set-up: a level gained mid-run");
            Assert.That(sim.MasteryOf(_wayfinding), Is.GreaterThanOrEqualTo(2), "set-up: mastery gained mid-run");
            Assert.That(sim.SecondsFasterSinceLastRun(walk, null), Is.EqualTo(expected).Within(0.0001f), "steady during the run");
        }

        [Test]
        public void SinceLastRun_IsZero_WhenMasteryDidNotMove()
        {
            var sim = BeginWithMastery(SinceLastRunSettings(), 300f);
            sim.EndRunEarly();

            Assert.That(sim.SecondsFasterSinceLastRun(MakeSkillTask("Walk", 10f, _wayfinding), null), Is.EqualTo(0f));
            Assert.That(sim.SecondsFasterSinceLastRun(MakeTask("Wait", 10f), null), Is.EqualTo(0f), "no skill: no change, but a figure");
        }
    }
}
