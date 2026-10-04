using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The summary shown when a run ends: time, actions, and what Clara gained.</summary>
    public class RunReportTests : SimulationTestBase
    {
        // 10 vitality. Searching takes 2s and 2 vitality, so a run of searching is 5 searches, 10s.
        // Queued without a limit, Search repeats until she's spent.
        private (Simulation sim, TaskDefinition search) MakeWorld(GameContent content = null)
        {
            var settings = MakeLoopSettings(vitalityMax: 10f);
            settings.xpPerSecondOfTask = 10f; // 1 XP a tick, so no rounding drift
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 10f;
            settings.xpGrowthPerLevel = 2f;
            settings.xpMinIncreasePerLevel = 0f;
            settings.masteryShare = 1f;
            var search = MakeTiringTask("Search", 2f, vitalityCost: 2f); // no skill: the timings stay exact
            search.kind = TaskKind.Search; // trains Perception
            return (new Simulation(settings, TicksPerSecond, content), search);
        }

        private static void BeginAndRunUntilOver(Simulation sim)
        {
            sim.BeginLoop();
            RunUntilOver(sim, 100f);
        }

        [Test]
        public void BeforeAnyRunEnds_ThereIsNoReport()
        {
            var (sim, _) = MakeWorld();

            Assert.That(sim.LastRun, Is.Null);
            Assert.That(sim.Persistent.RunHistory, Is.Empty);
        }

        [Test]
        public void TheReport_CountsTimeAndEveryActionFinished()
        {
            var (sim, search) = MakeWorld();
            var look = MakeTask("Look around", 1f);
            sim.Schedule(look, 1);
            sim.Schedule(search);

            BeginAndRunUntilOver(sim);

            var run = sim.LastRun;
            Assert.That(run.LoopNumber, Is.EqualTo(1));
            Assert.That(run.EndReason, Is.EqualTo(LoopEndReason.Exhausted));
            Assert.That(run.Seconds, Is.EqualTo(11f).Within(0.15f), "1s looking, then 5 searches of 2s");
            Assert.That(run.ActionsCompleted, Is.EqualTo(6));
            Assert.That(run.Tasks, Is.EqualTo(new[] { (look, 1), (search, 5) }));
            Assert.That(run.Unfinished, Is.Null);
        }

        [Test]
        public void ATaskCutShort_IsReportedAsUnfinished()
        {
            var (sim, _) = MakeWorld();
            var chase = MakeTiringTask("Chase", 20f, vitalityCost: 20f);
            sim.Schedule(chase, 1);

            BeginAndRunUntilOver(sim);

            Assert.That(sim.LastRun.ActionsCompleted, Is.EqualTo(0));
            Assert.That(sim.LastRun.Unfinished, Is.EqualTo(chase));
        }

        [Test]
        public void TheRunBefore_IsKeptForComparison()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);
            BeginAndRunUntilOver(sim);
            var first = sim.LastRun;

            sim.Schedule(search); // queued between runs: for the next one
            BeginAndRunUntilOver(sim);

            var before = sim.LastRun.RunBefore;
            Assert.That(before.LoopNumber, Is.EqualTo(first.LoopNumber));
            Assert.That(before.Ticks, Is.EqualTo(first.Ticks));
            Assert.That(before.ActionsCompleted, Is.EqualTo(first.ActionsCompleted));
            Assert.That(sim.LastRun.LoopNumber, Is.EqualTo(2));
        }

        [Test]
        public void EndingARunEarly_StillMakesAReport()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);
            sim.BeginLoop();
            RunSeconds(sim, 3);

            sim.EndRunEarly();

            Assert.That(sim.LastRun.EndReason, Is.EqualTo(LoopEndReason.EndedByPlayer));
            Assert.That(sim.LastRun.ActionsCompleted, Is.EqualTo(1));
            Assert.That(sim.LastRun.Unfinished, Is.EqualTo(search));
        }

        [Test]
        public void TheReport_ShowsLevelsReached_AndMasteryGained()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);

            BeginAndRunUntilOver(sim);

            var gain = sim.LastRun.Attributes.Find(a => a.Attribute == ClaraAttribute.Perception);
            Assert.That(gain, Is.Not.Null);
            Assert.That(gain.Level, Is.EqualTo(sim.LevelOf(ClaraAttribute.Perception)));
            Assert.That(gain.Level, Is.GreaterThan(0));
            Assert.That(gain.MasteryBefore, Is.EqualTo(0));
            Assert.That(gain.MasteryAfter, Is.EqualTo(sim.MasteryOf(ClaraAttribute.Perception)));
            Assert.That(gain.MasteryAfter, Is.GreaterThan(0));
        }

        [Test]
        public void TheReport_OfASecondRun_StartsMasteryFromWhatTheFirstRunKept()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);
            BeginAndRunUntilOver(sim);
            int keptAfterFirst = sim.LastRun.Attributes.Find(a => a.Attribute == ClaraAttribute.Perception).MasteryAfter;
            Assert.That(keptAfterFirst, Is.GreaterThan(0), "set-up: the first run built some mastery");

            sim.Schedule(search);
            BeginAndRunUntilOver(sim);

            var second = sim.LastRun.Attributes.Find(a => a.Attribute == ClaraAttribute.Perception);
            Assert.That(second.MasteryBefore, Is.EqualTo(keptAfterFirst), "the start-of-run snapshot, not zero");
            Assert.That(second.MasteryAfter, Is.GreaterThanOrEqualTo(second.MasteryBefore));
        }

        [Test]
        public void TheReport_OfASecondRun_StartsASkillsMasteryFromWhatTheFirstRunKept()
        {
            var studying = Make<SkillDefinition>();
            var (sim, _) = MakeWorld();
            var read = MakeTiringTask("Read", 2f, vitalityCost: 2f);
            read.skill = studying;
            sim.Schedule(read);
            BeginAndRunUntilOver(sim);
            int keptAfterFirst = sim.LastRun.Skills.Find(s => s.Skill == studying).MasteryAfter;
            Assert.That(keptAfterFirst, Is.GreaterThan(0), "set-up: the first run built some mastery");

            sim.Schedule(read);
            BeginAndRunUntilOver(sim);

            Assert.That(sim.LastRun.Skills.Find(s => s.Skill == studying).MasteryBefore, Is.EqualTo(keptAfterFirst));
        }

        [Test]
        public void TheReport_ShowsSkillsTrained()
        {
            var studying = Make<SkillDefinition>();
            var (sim, _) = MakeWorld();
            var read = MakeTiringTask("Read", 2f, vitalityCost: 2f);
            read.skill = studying;
            sim.Schedule(read);

            BeginAndRunUntilOver(sim);

            var gain = sim.LastRun.Skills.Find(s => s.Skill == studying);
            Assert.That(gain, Is.Not.Null);
            Assert.That(gain.Level, Is.EqualTo(sim.LevelOf(studying)));
            Assert.That(gain.Level, Is.GreaterThan(0));
            Assert.That(gain.MasteryBefore, Is.EqualTo(0));
            Assert.That(gain.MasteryAfter, Is.EqualTo(sim.MasteryOf(studying)));
            Assert.That(gain.MasteryAfter, Is.GreaterThan(0));
        }

        [Test]
        public void TheReport_ShowsKeptResourcesGained_AndToolsHeld()
        {
            var mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            var focus = MakeResource("Focus");
            var (sim, search) = MakeWorld();
            search.gives.Add(new ResourceAmount { resource = mirrors, amount = 1 });
            search.gives.Add(new ResourceAmount { resource = focus, amount = 1 });
            sim.Schedule(search);
            BeginAndRunUntilOver(sim);
            int afterFirst = sim.AmountOf(mirrors);

            sim.Schedule(search);
            BeginAndRunUntilOver(sim);

            Assert.That(sim.LastRun.Kept, Is.EqualTo(new[] { (mirrors, afterFirst, sim.AmountOf(mirrors)) }));
            Assert.That(sim.LastRun.ToolsAndStats.Count, Is.EqualTo(1));
            Assert.That(sim.LastRun.ToolsAndStats[0].resource, Is.EqualTo(focus));
        }

        [Test]
        public void TheReport_ListsSwitchesFlippedThisRun_IncludingByTheCollapse()
        {
            var chase = MakeTiringTask("Chase", 20f, vitalityCost: 20f);
            var lost = Make<SwitchDefinition>();
            lost.trigger = SwitchTrigger.LoopEndedDuringTask;
            lost.triggerTask = chase;
            var content = Make<GameContent>();
            content.tasks.Add(chase);
            content.switches.Add(lost);
            var (sim, _) = MakeWorld(content);
            sim.Schedule(chase, 1);

            BeginAndRunUntilOver(sim);
            Assert.That(sim.LastRun.SwitchesFlipped, Is.EqualTo(new[] { lost }));

            sim.Schedule(chase, 1);
            BeginAndRunUntilOver(sim);
            Assert.That(sim.LastRun.SwitchesFlipped, Is.Empty, "already flipped: not news this time");
        }

        // ---------- The run history: last and longest ----------

        /// <summary>A run of exactly this many seconds, ended by the player (well before her vitality runs out).</summary>
        private static void RunFor(Simulation sim, float seconds)
        {
            sim.BeginLoop();
            RunSeconds(sim, seconds);
            sim.EndRunEarly();
        }

        /// <summary>
        /// A run that collapses after about this many seconds: a chase twice as long as that, costing
        /// twice her vitality, spends it all halfway.
        /// </summary>
        private void CollapseAfter(Simulation sim, float seconds)
        {
            sim.Schedule(MakeTiringTask("Chase", seconds * 2f, vitalityCost: 20f), 1);
            BeginAndRunUntilOver(sim);
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.Exhausted), "the test run should have collapsed");
        }

        [Test]
        public void EndingARun_AddsItToTheHistory()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);
            RunFor(sim, 3); // one search finished, the second cut short
            CollapseAfter(sim, 1);

            var history = sim.Persistent.RunHistory;
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history[0].LoopNumber, Is.EqualTo(1), "oldest first");
            Assert.That(history[0].Ticks, Is.EqualTo(3 * TicksPerSecond));
            Assert.That(history[0].ActionsCompleted, Is.EqualTo(1));
            Assert.That(history[0].EndReason, Is.EqualTo(LoopEndReason.EndedByPlayer));
            Assert.That(history[1].LoopNumber, Is.EqualTo(2));
            Assert.That(history[1].Ticks, Is.EqualTo(1 * TicksPerSecond).Within(1));
            Assert.That(history[1].ActionsCompleted, Is.EqualTo(0));
            Assert.That(history[1].EndReason, Is.EqualTo(LoopEndReason.Exhausted));
        }

        [Test]
        public void Report_LongestIsTheLongestEarlierRun()
        {
            var (sim, _) = MakeWorld();
            CollapseAfter(sim, 2);
            CollapseAfter(sim, 4);
            CollapseAfter(sim, 1);
            CollapseAfter(sim, 5);

            var report = sim.LastRun;
            Assert.That(report.Longest.LoopNumber, Is.EqualTo(2), "the longest before this run, not this one");
            Assert.That(report.SecondsOf(report.Longest), Is.EqualTo(4f).Within(0.15f));
            Assert.That(report.RunBefore.LoopNumber, Is.EqualTo(3));
        }

        [Test]
        public void Report_NewLongest_OnlyWhenBeatingAnEarlierRun()
        {
            var (sim, _) = MakeWorld();

            CollapseAfter(sim, 2);
            Assert.That(sim.LastRun.Longest, Is.Null, "nothing earlier to compare with");
            Assert.That(sim.LastRun.RunBefore, Is.Null);
            Assert.That(sim.LastRun.IsNewLongest, Is.False, "not on the very first run");

            CollapseAfter(sim, 2);
            Assert.That(sim.LastRun.IsNewLongest, Is.False, "not on a tie");

            CollapseAfter(sim, 1);
            Assert.That(sim.LastRun.IsNewLongest, Is.False, "shorter");

            CollapseAfter(sim, 3);
            Assert.That(sim.LastRun.IsNewLongest, Is.True, "longer than every earlier run");
        }

        [Test]
        public void Report_RunsEndedEarly_DontCountForLongest()
        {
            var (sim, _) = MakeWorld();
            CollapseAfter(sim, 2);

            RunFor(sim, 5); // longer, but ended by the player
            Assert.That(sim.LastRun.IsNewLongest, Is.False, "a run ended early can't set the record");
            Assert.That(sim.LastRun.Longest.LoopNumber, Is.EqualTo(1));

            CollapseAfter(sim, 3);
            Assert.That(sim.LastRun.Longest.LoopNumber, Is.EqualTo(1), "the 5s run ended early is skipped");
            Assert.That(sim.LastRun.IsNewLongest, Is.True);
            Assert.That(sim.LastRun.RunBefore.LoopNumber, Is.EqualTo(2), "it's still the last run, though");
        }

        [Test]
        public void ARunRestartedBeforeItEnds_IsInTheHistory_AsEndedEarly()
        {
            var (sim, _) = MakeWorld();
            sim.BeginLoop();
            KeepBusy(sim, 2f);
            RunSeconds(sim, 2);

            sim.BeginLoop(); // restarted mid-run (the dev panel's Restart loop)

            var history = sim.Persistent.RunHistory;
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history[0].LoopNumber, Is.EqualTo(1), "under its own number, not the next run's");
            Assert.That(history[0].Ticks, Is.EqualTo(2 * TicksPerSecond));
            Assert.That(history[0].EndReason, Is.EqualTo(LoopEndReason.EndedByPlayer));
            Assert.That(sim.Persistent.LoopNumber, Is.EqualTo(2));
        }

        [Test]
        public void LastRun_SurvivesSavingAndLoading()
        {
            var (sim, _) = MakeWorld();
            CollapseAfter(sim, 2);

            var content = Make<GameContent>(); // empty: a ContentIndex now complains about no content at all
            var loaded = new Simulation(MakeLoopSettings(vitalityMax: 10f), TicksPerSecond, content, SaveAndLoad(sim, content));
            RunFor(loaded, 1);

            Assert.That(loaded.LastRun.RunBefore, Is.Not.Null);
            Assert.That(loaded.LastRun.RunBefore.LoopNumber, Is.EqualTo(1));
            Assert.That(loaded.LastRun.SecondsOf(loaded.LastRun.RunBefore), Is.EqualTo(2f).Within(0.15f));
            Assert.That(loaded.LastRun.Longest.LoopNumber, Is.EqualTo(1));
        }

        // ---------- Time by skill ----------

        private TaskDefinition SkilledTask(string name, float seconds, SkillDefinition skill)
        {
            var task = MakeTiringTask(name, seconds, vitalityCost: seconds);
            task.skill = skill;
            return task;
        }

        [Test]
        public void TimeBySkill_SumsToRunTicks()
        {
            var reading = MakeSkill("Reading");
            var (sim, search) = MakeWorld();
            sim.Schedule(SkilledTask("Read", 3f, reading), 1);
            sim.Schedule(search); // no skill

            BeginAndRunUntilOver(sim);

            long sum = 0;
            foreach (var slice in sim.LastRun.TimeBySkill)
                sum += slice.Ticks;
            Assert.That(sum, Is.EqualTo(sim.LastRun.Ticks), "the clock only runs during an action, so every tick has an owner");
        }

        [Test]
        public void TimeBySkill_TravelCountsForItsSkill()
        {
            var wayfinding = MakeSkill("Wayfinding");
            var start = MakeNode("Start");
            var other = MakeNode("Other");
            Join(start, other);
            var content = MakePlaces(start, other);
            content.travelVerb.skill = wayfinding;
            var (sim, _) = MakeWorld(content);
            sim.BeginLoop();
            sim.ScheduleTrip(other);
            RunSeconds(sim, 2f); // the trip is free, so nothing drains her: end the run by hand
            sim.EndRunEarly();

            var slice = sim.LastRun.TimeBySkill.Find(s => s.Skill == wayfinding);
            Assert.That(slice, Is.Not.Null);
            Assert.That(slice.Ticks, Is.GreaterThan(0));
        }

        [Test]
        public void TimeBySkill_LongestFirst_OtherLast()
        {
            var short1 = MakeSkill("Short");
            var long1 = MakeSkill("Long");
            var (sim, search) = MakeWorld();
            sim.Schedule(SkilledTask("Quick", 1f, short1), 1);
            sim.Schedule(SkilledTask("Slow", 3f, long1), 1);
            sim.Schedule(search, 3); // no skill, and the longest of all

            BeginAndRunUntilOver(sim);

            var order = sim.LastRun.TimeBySkill;
            Assert.That(order.Count, Is.EqualTo(3));
            Assert.That(order[0].Skill, Is.EqualTo(long1));
            Assert.That(order[1].Skill, Is.EqualTo(short1));
            Assert.That(order[2].Skill, Is.Null, "Other comes last even when it is the widest");
        }

        [Test]
        public void TimeBySkill_NoSkillAction_GoesToOther()
        {
            var (sim, search) = MakeWorld();
            sim.Schedule(search);

            BeginAndRunUntilOver(sim);

            Assert.That(sim.LastRun.TimeBySkill.Count, Is.EqualTo(1));
            Assert.That(sim.LastRun.TimeBySkill[0].Skill, Is.Null);
            Assert.That(sim.LastRun.TimeBySkill[0].Ticks, Is.EqualTo(sim.LastRun.Ticks));
        }

        [Test]
        public void TimeBySkill_ResetsEachRun()
        {
            var reading = MakeSkill("Reading");
            var (sim, search) = MakeWorld();
            sim.Schedule(SkilledTask("Read", 2f, reading), 1);
            sim.Schedule(search);
            BeginAndRunUntilOver(sim);

            sim.Schedule(search); // only the next run's queue
            BeginAndRunUntilOver(sim);

            Assert.That(sim.LastRun.TimeBySkill.Find(s => s.Skill == reading), Is.Null);
            Assert.That(sim.LastRun.TimeBySkill[0].Ticks, Is.EqualTo(sim.LastRun.Ticks));
        }

        [Test]
        public void TimeBySkill_SharesSumToOne()
        {
            var reading = MakeSkill("Reading");
            var (sim, search) = MakeWorld();
            sim.Schedule(SkilledTask("Read", 3f, reading), 1);
            sim.Schedule(search);

            BeginAndRunUntilOver(sim);

            float sum = 0f;
            foreach (var slice in sim.LastRun.TimeBySkill)
                sum += slice.Share;
            Assert.That(sum, Is.EqualTo(1f).Within(0.0001f));
        }

        // ---------- Milestones table ----------

        private TaskDefinition _find;   // 2s; finishing it reaches _found
        private TaskDefinition _wait;   // 1s; finishing it reaches _waited
        private SwitchDefinition _found;
        private SwitchDefinition _waited;
        private GameContent _milestoneContent;

        private void MakeMilestoneWorld()
        {
            _find = MakeTask("Find", 2f);
            _wait = MakeTask("Wait", 1f);
            SwitchDefinition Milestone(string title, TaskDefinition task)
            {
                var milestone = Make<SwitchDefinition>();
                milestone.story = MakeStory($"{title}\nIt was there all along.");
                milestone.trigger = SwitchTrigger.TasksCompletedInOneRun;
                milestone.requiredTasks.Add(task);
                return milestone;
            }
            _found = Milestone("Found it", _find);
            _waited = Milestone("Waited", _wait);
            _milestoneContent = Make<GameContent>();
            _milestoneContent.tasks.Add(_find);
            _milestoneContent.tasks.Add(_wait);
            _milestoneContent.switches.Add(_found);
            _milestoneContent.switches.Add(_waited);
        }

        /// <summary>One run: queue these (once each, in order), run a while, end it early.</summary>
        private static RunReport MilestoneRun(Simulation sim, params TaskDefinition[] tasks)
        {
            foreach (var task in tasks)
                sim.Schedule(task, 1);
            sim.BeginLoop();
            RunSeconds(sim, 10f);
            sim.EndRunEarly();
            return sim.LastRun;
        }

        private Simulation MakeMilestoneSim() =>
            new Simulation(MakeLoopSettings(), TicksPerSecond, _milestoneContent);

        private static RunReport.MilestoneRow RowOf(RunReport report, ContentAsset milestone) =>
            report.Milestones.Find(r => r.Milestone == milestone);

        [Test]
        public void Milestones_ThisRunLastAndPrevious()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            MilestoneRun(sim, _find);                 // found at 2s
            MilestoneRun(sim, _wait, _find);          // found at 3s
            var report = MilestoneRun(sim, _wait, _wait, _find); // found at 4s

            var row = RowOf(report, _found);
            Assert.That(row.ThisRun, Is.EqualTo(4f).Within(0.01f));
            Assert.That(row.LastRun, Is.EqualTo(3f).Within(0.01f));
            Assert.That(row.PreviousRun, Is.EqualTo(2f).Within(0.01f));
        }

        [Test]
        public void Milestones_NotReachedThisRun_ListedLast()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            MilestoneRun(sim, _wait, _find);          // waited at 1s, found at 3s
            var report = MilestoneRun(sim, _find);    // found at 2s; never waited

            Assert.That(report.Milestones.Count, Is.EqualTo(2));
            Assert.That(report.Milestones[0].Milestone, Is.EqualTo(_found));
            Assert.That(report.Milestones[1].Milestone, Is.EqualTo(_waited));
            Assert.That(report.Milestones[1].ThisRun, Is.Null);
            Assert.That(report.Milestones[1].LastRun, Is.EqualTo(1f).Within(0.01f), "still shows when it was last reached");
            Assert.That(report.Milestones[1].Change, Is.Null);
        }

        [Test]
        public void Milestones_ReachedThisRun_AreInTheOrderReached()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            var report = MilestoneRun(sim, _find, _wait); // found at 2s, waited at 3s

            Assert.That(report.Milestones.ConvertAll(r => r.Milestone), Is.EqualTo(new[] { _found, _waited }));
        }

        [Test]
        public void Milestones_FirstTime_HasNoImprovement()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            var first = MilestoneRun(sim, _find);

            var row = RowOf(first, _found);
            Assert.That(row.IsNew, Is.True);
            Assert.That(row.Change, Is.Null);
            Assert.That(row.LastRun, Is.Null);
            Assert.That(row.PreviousRun, Is.Null);

            var second = MilestoneRun(sim, _find);
            Assert.That(RowOf(second, _found).IsNew, Is.False, "it is not new any more");
        }

        [Test]
        public void Milestones_ImprovementIsThisMinusLast()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            MilestoneRun(sim, _wait, _find);          // 3s
            var faster = MilestoneRun(sim, _find);    // 2s
            var slower = MilestoneRun(sim, _wait, _find); // 3s

            Assert.That(RowOf(faster, _found).Change, Is.EqualTo(-1f).Within(0.01f), "faster is negative");
            Assert.That(RowOf(slower, _found).Change, Is.EqualTo(1f).Within(0.01f), "slower is positive");
        }

        [Test]
        public void Milestones_EndedEarly_ComparesLikeAnyRun()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            MilestoneRun(sim, _wait, _find);          // ended early (MilestoneRun always does)
            var report = MilestoneRun(sim, _find);

            Assert.That(report.RunBefore.EndReason, Is.EqualTo(LoopEndReason.EndedByPlayer));
            Assert.That(RowOf(report, _found).LastRun, Is.EqualTo(3f).Within(0.01f));
        }

        [Test]
        public void Milestones_CannotBeReachedAgain_DropsFromTheTableAfterItsRun()
        {
            var chase = MakeTiringTask("Chase", 20f, vitalityCost: 20f);
            var lost = Make<SwitchDefinition>();
            lost.story = MakeStory("Lost Roland\nHe's gone.");
            lost.trigger = SwitchTrigger.LoopEndedDuringTask;
            lost.triggerTask = chase;
            lost.locksTasks.Add(chase); // the collapse locks its own trigger task: it can't happen again
            var content = Make<GameContent>();
            content.tasks.Add(chase);
            content.switches.Add(lost);
            var sim = new Simulation(MakeLoopSettings(vitalityMax: 10f), TicksPerSecond, content);
            sim.Schedule(chase, 1);
            BeginAndRunUntilOver(sim); // collapses mid-chase: the switch flips, reached this run

            var firstReport = sim.LastRun;
            Assert.That(RowOf(firstReport, lost), Is.Not.Null, "shown in the run it was reached");

            var nextReport = MilestoneRun(sim); // any later run, not reaching it again

            Assert.That(RowOf(nextReport, lost), Is.Null, "never shown again: it can't be reached a second time");
        }

        [Test]
        public void RoomEntry_AppearsInTableEveryRun()
        {
            var start = MakeNode("The start");
            var hall = MakeNode("The hall");
            Join(start, hall);
            var content = MakePlaces(start, hall);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.ScheduleTrip(hall);
            sim.BeginLoop();
            RunSeconds(sim, 2f);
            sim.EndRunEarly();
            var first = sim.LastRun;
            Assert.That(RowOf(first, hall).IsNew, Is.True, "the first entry ever");
            Assert.That(RowOf(first, hall).ThisRun, Is.EqualTo(1f).Within(0.01f));
            Assert.That(first.Milestones.Exists(r => r.Milestone == start), Is.False, "never the start room");

            sim.BeginLoop();
            KeepBusy(sim, 1f);
            RunSeconds(sim, 2f);
            sim.EndRunEarly();
            var second = sim.LastRun;

            var row = RowOf(second, hall);
            Assert.That(row, Is.Not.Null, "a room can be entered again in a later run, so it stays listed");
            Assert.That(row.ThisRun, Is.Null);
            Assert.That(row.LastRun, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void Milestones_SurviveSavingAndLoading()
        {
            MakeMilestoneWorld();
            var sim = MakeMilestoneSim();
            MilestoneRun(sim, _find);

            var loaded = new Simulation(MakeLoopSettings(), TicksPerSecond, _milestoneContent, SaveAndLoad(sim, _milestoneContent));
            var report = MilestoneRun(loaded, _wait, _find);

            Assert.That(RowOf(report, _found).LastRun, Is.EqualTo(2f).Within(0.01f));
            Assert.That(RowOf(report, _found).IsNew, Is.False);
        }
    }
}
