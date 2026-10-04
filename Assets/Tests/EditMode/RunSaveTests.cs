using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// A run under way is saved whole and carries on exactly where it was: the same game, tick for
    /// tick, as if it had never stopped.
    /// </summary>
    public class RunSaveTests : SimulationTestBase
    {
        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _hall, _dark;
        private ResourceDefinition _wisp, _candle, _flint, _lit;
        private TaskDefinition _gather, _makeCandle, _light;

        // The hall has wisps (restore 5 over 5s; 2 pockets, floors of 3) and candles to light; the
        // dark next door takes 4 searches. The drain is on and growing, and Endurance and Composure
        // train, so a run has plenty going on.
        [SetUp]
        public void SetUp()
        {
            _wisp = MakeObject("Wisp", max: 10);
            _wisp.restoreVitality = 5f;
            _candle = MakeObject("Candle", max: 20);
            _flint = MakeResource("Flint and steel", max: 1);
            _lit = MakeResource("Candles lit", max: 3);
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);
            _makeCandle = MakeGatherTask("Make a candle", 1f, _candle);
            _light = MakeGatherTask("Light a candle", 1f, _lit);
            _light.needs.Add(new ResourceAmount { resource = _flint, amount = 1 });
            _light.needs.Add(new ResourceAmount { resource = _candle, amount = 1 });
            _light.takes.Add(new ResourceAmount { resource = _candle, amount = 1 });

            _hall = MakeNode("The hall");
            _dark = MakeNode("The dark");
            _dark.exploresToFill = 4;
            Join(_hall, _dark);
            _hall.tasks.AddRange(new[] { _gather, _makeCandle, _light });
            _content = MakePlaces(_hall, _dark);
            _content.exploreVerb = MakeTask("Search", 1.5f);
            _content.tasks.AddRange(new[] { _gather, _makeCandle, _light });

            _settings = MakeLoopSettings(pockets: 2, floor: 3);
            _settings.vitalityDrainPerSecond = 0.5f;
            _settings.drainGrowthPerMinute = 0.25f;
            _settings.enduranceXpPerVitalityLost = 0.2f;
            _settings.composureXpPerSecond = 0.05f;
        }

        private Simulation NewGame() => new Simulation(_settings, TicksPerSecond, _content);

        /// <summary>Saves to JSON and loads it into a new game, as closing and restarting would.</summary>
        private Simulation SaveAndReopen(Simulation sim, List<string> warnings = null)
        {
            return Reopen(sim, _settings, _content, warnings);
        }

        private static void AssertSame(Simulation a, Simulation b, NodeDefinition hall, NodeDefinition dark,
            ResourceDefinition wisp, string when)
        {
            Assert.That(b.Phase, Is.EqualTo(a.Phase), when);
            Assert.That(b.Loop.TicksElapsed, Is.EqualTo(a.Loop.TicksElapsed), when);
            Assert.That(b.Loop.Vitality.Current, Is.EqualTo(a.Loop.Vitality.Current).Within(0.0001f), when);
            Assert.That(b.VitalityDrainPerSecond, Is.EqualTo(a.VitalityDrainPerSecond).Within(0.00001f), when);
            Assert.That(b.Loop.CurrentNode, Is.EqualTo(a.Loop.CurrentNode), when);
            Assert.That(b.Loop.CurrentTask, Is.EqualTo(a.Loop.CurrentTask), when);
            Assert.That(b.Loop.CurrentTaskWorkDone, Is.EqualTo(a.Loop.CurrentTaskWorkDone).Within(0.0001f), when);
            Assert.That(b.Queue.Count, Is.EqualTo(a.Queue.Count), when);
            Assert.That(b.AmountOf(wisp), Is.EqualTo(a.AmountOf(wisp)), when);
            Assert.That(b.OnFloor(hall, wisp), Is.EqualTo(a.OnFloor(hall, wisp)), when);
            Assert.That(b.ExploresDoneIn(dark), Is.EqualTo(a.ExploresDoneIn(dark)).Within(0.0001f), when);
            Assert.That(b.RestoringPerSecond, Is.EqualTo(a.RestoringPerSecond).Within(0.0001f), when);
            Assert.That(b.Loop.XpOf(ClaraAttribute.Endurance), Is.EqualTo(a.Loop.XpOf(ClaraAttribute.Endurance)).Within(0.0001f), when);
            Assert.That(b.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(a.Loop.XpOf(ClaraAttribute.Composure)).Within(0.0001f), when);
            Assert.That(b.Loop.XpOf(ClaraAttribute.Perception), Is.EqualTo(a.Loop.XpOf(ClaraAttribute.Perception)).Within(0.0001f), when);
            Assert.That(b.Persistent.LoopNumber, Is.EqualTo(a.Persistent.LoopNumber), when);
        }

        [Test]
        public void SkillTicks_RoundTripMidRun()
        {
            var lifting = MakeSkill("Lifting");
            _gather.skill = lifting;
            var original = NewGame();
            original.Schedule(_gather);
            original.Schedule(original.ExploreVerb); // no skill: counts as Other
            original.BeginLoop();
            RunSeconds(original, 4f);
            Assert.That(original.Loop.SkillTicks.ContainsKey(lifting), "set-up: something was tallied");

            var reopened = SaveAndReopen(original);

            Assert.That(reopened.Loop.SkillTicks[lifting], Is.EqualTo(original.Loop.SkillTicks[lifting]));
            Assert.That(reopened.Loop.OtherTicks, Is.EqualTo(original.Loop.OtherTicks));
        }

        [Test]
        public void MidRunSave_ThenLoad_StillSettlesStatMastery()
        {
            var original = NewGame();
            original.Schedule(_gather);
            original.BeginLoop();
            RunSeconds(original, 2f);
            original.Loop.AttributeXp[ClaraAttribute.Perception] = 300f;

            var reopened = SaveAndReopen(original);
            Assert.That(reopened.Persistent.AttributeMasteryXpOf(ClaraAttribute.Perception), Is.EqualTo(0f), "held, not settled, by the save");
            reopened.EndRunEarly();

            float expected = 300f * _settings.masteryShare * _settings.collapseStatXpShare;
            Assert.That(reopened.Persistent.AttributeMasteryXpOf(ClaraAttribute.Perception), Is.EqualTo(expected).Within(0.01f));
        }

        // A costed action under way: she's paying vitality for it as it goes, so a save must keep what's left to pay.
        private TaskDefinition AddToilingTask()
        {
            var toil = MakeTask("Toil", 4f);
            SetCost(toil, 5f, (CostSource.Vitality, 100f));
            toil.alwaysCharged = true;
            _hall.tasks.Add(toil);
            _content.tasks.Add(toil);
            return toil;
        }

        [Test]
        public void ACostedActionUnderWay_KeepsWhatItStillHasToPay_ThroughASave()
        {
            var toil = AddToilingTask();
            var original = NewGame();
            original.Schedule(toil);
            original.BeginLoop();
            RunSeconds(original, 1.7f);
            Assert.That(original.Loop.CurrentTaskCosts.Count, Is.GreaterThan(0), "set-up: the action has costs to pay");

            var warnings = new List<string>();
            var reopened = SaveAndReopen(original, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.Loop.CurrentTaskCosts.Count, Is.EqualTo(original.Loop.CurrentTaskCosts.Count));
            for (int i = 0; i < original.Loop.CurrentTaskCosts.Count; i++)
            {
                Assert.That(reopened.Loop.CurrentTaskCosts[i].Remaining, Is.EqualTo(original.Loop.CurrentTaskCosts[i].Remaining).Within(0.0001f));
                Assert.That(reopened.Loop.CurrentTaskCosts[i].IsCharge, Is.EqualTo(original.Loop.CurrentTaskCosts[i].IsCharge));
            }
            RunSeconds(original, 10f);
            RunSeconds(reopened, 10f);
            AssertSame(original, reopened, _hall, _dark, _wisp, "10 seconds on, costs paid alike");
        }

        [Test]
        public void ARunningCost_WhosePoolIsGone_IsWarnedAbout()
        {
            var toil = AddToilingTask();
            var original = NewGame();
            original.Schedule(toil);
            original.BeginLoop();
            RunSeconds(original, 1.7f);
            var data = SaveSerializer.Capture(original);
            Assert.That(data.run.costs.Count, Is.GreaterThan(0), "set-up: a cost was saved");
            data.run.costs[0].hue = 999; // a pool that doesn't exist
            data.run.pools.Add(new SavedHueValue { hue = 998, value = 1f });
            var warnings = new List<string>();

            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            SaveSerializer.Load(loaded, new ContentIndex(_content), _settings, TicksPerSecond, _content, warnings, out _);

            Assert.That(error, Is.Null);
            Assert.That(warnings, Has.Some.Contain("Dropped a cost"), "the action would otherwise be free, silently");
            Assert.That(warnings, Has.Some.Contain("pool in the run"));
        }

        [Test]
        public void ATripUnderWay_ResumesHeadingForItsDestination()
        {
            var original = NewGame();
            original.ScheduleTrip(_dark);
            original.Schedule(original.ExploreVerb);
            original.BeginLoop();
            RunSeconds(original, 0.5f); // on the way to the dark
            Assert.That(original.Loop.CurrentDestination, Is.EqualTo(_dark), "set-up: travelling");

            var reopened = SaveAndReopen(original);

            Assert.That(reopened.Loop.CurrentDestination, Is.EqualTo(_dark));
        }

        [Test]
        public void ARunSavedMidAction_CarriesOnExactlyAsIfItNeverStopped()
        {
            var original = NewGame();
            original.Schedule(_gather);   // 2 in pockets, 3 on the floor
            original.ScheduleTrip(_dark);
            original.Schedule(original.ExploreVerb, 3);
            original.BeginLoop();
            RunSeconds(original, 7.3f); // part-way through the first search, with wisps on the hall floor

            var reopened = SaveAndReopen(original);
            AssertSame(original, reopened, _hall, _dark, _wisp, "straight after loading");
            Assert.That(reopened.Loop.ExploredAtStart, Is.EquivalentTo(original.Loop.ExploredAtStart), "the report's start-of-run snapshot is saved too");

            RunSeconds(original, 20f);
            RunSeconds(reopened, 20f);
            AssertSame(original, reopened, _hall, _dark, _wisp, "20 seconds on");
        }

        [Test]
        public void AResumedRun_RestoresFromTheFloor_AndUsesItemsLikeTheOriginal()
        {
            var original = NewGame();
            original.Schedule(_gather);
            original.BeginLoop();
            RunSeconds(original, 25f); // the drain has taken enough for wisps to be in use

            var reopened = SaveAndReopen(original);
            RunSeconds(original, 15f);
            RunSeconds(reopened, 15f);

            AssertSame(original, reopened, _hall, _dark, _wisp, "15 seconds on");
        }

        [Test]
        public void APickUpUnderWay_IsResumedAsThePickUpForItsItem()
        {
            _content.pickUpVerb = MakeTask("Pick up", 2f);
            // No drain, so no wisp is used before the pick-up starts (the drain counts towards using one):
            // this test is about resuming the pick-up, not restoring.
            _settings.vitalityDrainPerSecond = 0f;
            var original = NewGame();
            original.Schedule(_gather); // 2 in pockets, 3 on the floor
            original.BeginLoop();
            RunSeconds(original, 5f);
            original.Loop.ToolsAndStats[_wisp] = 0; // as if she'd used them
            original.Schedule(original.PickUpActionFor(_wisp));
            RunSeconds(original, 1f); // half-way through picking up

            var warnings = new List<string>();
            var reopened = SaveAndReopen(original, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.Loop.CurrentTask, Is.SameAs(reopened.PickUpActionFor(_wisp)));
            Assert.That(reopened.Queue.Top.Task, Is.SameAs(reopened.PickUpActionFor(_wisp)));
            RunSeconds(original, 1.5f);
            RunSeconds(reopened, 1.5f);
            Assert.That(reopened.AmountOf(_wisp), Is.EqualTo(original.AmountOf(_wisp)).And.EqualTo(2), "picked up, as in the original");
        }

        [Test]
        public void PickUpsAndPutDownsDone_KeepTheirItems_ThroughASave()
        {
            _content.pickUpVerb = MakeTask("Pick up", 1f);
            _content.putDownVerb = MakeTask("Put down", 1f);
            var original = NewGame();
            original.BeginLoop();
            original.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _wisp, 1 }, { _candle, 1 } };
            original.Schedule(original.PickUpActionFor(_wisp));
            original.Schedule(original.PickUpActionFor(_candle));
            original.Schedule(original.PutDownActionFor(_candle));
            original.Schedule(_makeCandle); // keeps the run going past them
            RunSeconds(original, 3.5f);

            var warnings = new List<string>();
            var reopened = SaveAndReopen(original, warnings);

            Assert.That(warnings, Is.Empty);
            var done = new[] { reopened.PickUpActionFor(_wisp), reopened.PickUpActionFor(_candle), reopened.PutDownActionFor(_candle) };
            Assert.That(reopened.Loop.CompletionLog, Is.EqualTo(done), "each for its own item, in order, as the run report lists them");
            Assert.That(reopened.Loop.CompletedTasks, Is.EquivalentTo(done));
        }

        [Test]
        public void AVersion9Save_KeepsItsActionsDone_AsTheirBareTasks()
        {
            var original = NewGame();
            original.Schedule(_makeCandle);
            original.BeginLoop();
            RunSeconds(original, 2.5f);

            // As a version 9 save wrote it: plain task Ids.
            var data = SaveSerializer.Capture(original);
            data.version = 9;
            foreach (var done in data.run.completedActions) data.run.completedTasks.Add(done.task);
            foreach (var done in data.run.actionLog) data.run.completionLog.Add(done.task);
            data.run.completedActions.Clear();
            data.run.actionLog.Clear();

            var warnings = new List<string>();
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            Assert.That(error, Is.Null);
            Assert.That(loaded.version, Is.EqualTo(SaveData.CurrentVersion));
            var reopened = SaveSerializer.Load(loaded, new ContentIndex(_content), _settings, TicksPerSecond, _content, warnings, out _);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.Loop.CompletionLog, Is.EqualTo(original.Loop.CompletionLog));
            Assert.That(reopened.Loop.CompletedTasks, Is.EquivalentTo(original.Loop.CompletedTasks));
        }

        [Test]
        public void TheQueue_KeepsItsRepeatCounts_AndWhatEachSupplierIsFor()
        {
            var original = NewGame();
            original.Loop.ToolsAndStats[_flint] = 1;
            original.Schedule(_light, 2);
            original.BeginLoop();
            original.Loop.ToolsAndStats[_flint] = 1;
            RunSeconds(original, 0.5f); // making a candle, for the light below it

            var reopened = SaveAndReopen(original);

            var entries = reopened.Queue.Entries;
            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(entries[0].Task, Is.EqualTo(_makeCandle));
            Assert.That(entries[0].SuppliesFor, Is.SameAs(entries[1]), "still making it for the light");
            Assert.That(entries[0].Supplies, Is.EqualTo(_candle));
            Assert.That(entries[0].SupplyTarget, Is.EqualTo(original.Queue.Entries[0].SupplyTarget));
            Assert.That(entries[1].TimesLeft, Is.EqualTo(2));

            RunSeconds(original, 10f);
            RunSeconds(reopened, 10f);
            Assert.That(reopened.AmountOf(_lit), Is.EqualTo(original.AmountOf(_lit)).And.EqualTo(2));
        }

        [Test]
        public void AResumedRun_IsTheSameRun_AndTheNextOneCountsOn()
        {
            var original = NewGame();
            original.Schedule(_gather);
            original.BeginLoop();
            RunSeconds(original, 3f);
            int loop = original.Persistent.LoopNumber;

            var reopened = SaveAndReopen(original);
            Assert.That(reopened.Phase, Is.EqualTo(LoopPhase.Running));
            Assert.That(reopened.Persistent.LoopNumber, Is.EqualTo(loop), "not counted twice");

            reopened.EndRunEarly();
            reopened.BeginLoop();
            Assert.That(reopened.Persistent.LoopNumber, Is.EqualTo(loop + 1));
        }

        [Test]
        public void ASaveBetweenRuns_OrAtARunsFirstTick_HasNoRunToResume()
        {
            var between = NewGame();
            Assert.That(SaveSerializer.Capture(between).hasRun, Is.False, "a new game, not yet begun");

            between.BeginLoop();
            Assert.That(SaveSerializer.Capture(between).hasRun, Is.False, "begun but not a tick played: it simply starts again");

            KeepBusy(between, 1f);
            RunTicks(between, 1);
            Assert.That(SaveSerializer.Capture(between).hasRun, Is.True);

            between.EndRunEarly();
            Assert.That(SaveSerializer.Capture(between).hasRun, Is.False, "the run has ended");
        }

        [Test]
        public void LastRun_KeptThroughSave()
        {
            var lifting = MakeSkill("Lifting");
            _gather.skill = lifting;
            var original = NewGame();
            original.Schedule(_gather, 2);
            original.ScheduleTrip(_dark);
            original.Schedule(original.ExploreVerb, 1);
            original.BeginLoop();
            RunSeconds(original, 12f);
            original.EndRunEarly();
            original.BeginLoop();
            original.Schedule(_gather, 1);
            RunSeconds(original, 3f);
            original.EndRunEarly(); // a second run, so the report has a run before it to compare with
            // (ended early, so neither counts for the record: Longest stays null on both sides)
            var report = original.LastRun;
            Assert.That(report, Is.Not.Null, "set-up: a run ended");

            var warnings = new List<string>();
            var last = SaveAndReopen(original, warnings).LastRun;

            Assert.That(warnings, Is.Empty);
            Assert.That(last, Is.Not.Null);
            Assert.That(last.LoopNumber, Is.EqualTo(report.LoopNumber));
            Assert.That(last.Ticks, Is.EqualTo(report.Ticks));
            Assert.That(last.Seconds, Is.EqualTo(report.Seconds).Within(0.0001f));
            Assert.That(last.EndReason, Is.EqualTo(report.EndReason));
            Assert.That(last.ActionsCompleted, Is.EqualTo(report.ActionsCompleted));
            Assert.That(last.Tasks, Is.EqualTo(report.Tasks));
            Assert.That(last.Skills.Count, Is.EqualTo(report.Skills.Count));
            Assert.That(last.Skills[0].Skill, Is.SameAs(lifting));
            Assert.That(last.Skills[0].MasteryProgressAfter, Is.EqualTo(report.Skills[0].MasteryProgressAfter).Within(0.0001f));
            Assert.That(last.TimeBySkill.Count, Is.EqualTo(report.TimeBySkill.Count));
            Assert.That(last.TimeBySkill[0].Ticks, Is.EqualTo(report.TimeBySkill[0].Ticks));
            Assert.That(last.Moves, Is.EqualTo(report.Moves));
            Assert.That(last.MoveVitality, Is.EqualTo(report.MoveVitality).Within(0.0001f));
            Assert.That(last.KeptVitalityGained, Is.EqualTo(report.KeptVitalityGained).Within(0.0001f));
            Assert.That(last.Attributes.Count, Is.EqualTo(report.Attributes.Count));
            for (int i = 0; i < report.Attributes.Count; i++)
            {
                Assert.That(last.Attributes[i].Attribute, Is.EqualTo(report.Attributes[i].Attribute));
                Assert.That(last.Attributes[i].MasteryAfter, Is.EqualTo(report.Attributes[i].MasteryAfter));
            }
            Assert.That(last.SwitchesFlipped, Is.EqualTo(report.SwitchesFlipped));
            Assert.That(last.CarriedKept, Is.EqualTo(report.CarriedKept));
            Assert.That(last.CarriedLost, Is.EqualTo(report.CarriedLost));
            Assert.That(last.KnownByHeartNow, Is.EqualTo(report.KnownByHeartNow));
            Assert.That(last.Explored, Is.EqualTo(report.Explored));
            Assert.That(last.Kept, Is.EqualTo(report.Kept));
            Assert.That(last.ToolsAndStats, Is.EqualTo(report.ToolsAndStats));
            Assert.That(last.Milestones.Count, Is.EqualTo(report.Milestones.Count));
            for (int i = 0; i < report.Milestones.Count; i++)
            {
                Assert.That(last.Milestones[i].Milestone, Is.SameAs(report.Milestones[i].Milestone));
                Assert.That(last.Milestones[i].ThisRun, Is.EqualTo(report.Milestones[i].ThisRun));
                Assert.That(last.Milestones[i].LastRun, Is.EqualTo(report.Milestones[i].LastRun));
            }
            Assert.That(last.RunBefore?.LoopNumber, Is.EqualTo(report.RunBefore?.LoopNumber), "the run before, from the history");
            Assert.That(last.Longest?.LoopNumber, Is.EqualTo(report.Longest?.LoopNumber));
        }

        [Test]
        public void NoRunYet_SavesNoLastRun()
        {
            var reopened = SaveAndReopen(NewGame());

            Assert.That(reopened.LastRun, Is.Null);
        }

        [Test]
        public void OldSave_LoadsWithNoLastRun()
        {
            var original = NewGame();
            original.Schedule(_gather);
            original.BeginLoop();
            RunSeconds(original, 3f);
            original.EndRunEarly();

            // As a version 19 save wrote it: no report.
            var data = SaveSerializer.Capture(original);
            data.version = 19;
            data.hasLastRun = false;
            data.lastRun = null;

            var warnings = new List<string>();
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            Assert.That(error, Is.Null);
            Assert.That(loaded.version, Is.EqualTo(SaveData.CurrentVersion));
            var reopened = SaveSerializer.Load(loaded, new ContentIndex(_content), _settings, TicksPerSecond, _content, warnings, out _);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.LastRun, Is.Null);
            Assert.That(reopened.Persistent.RunHistory.Count, Is.EqualTo(1), "the rest of the save is untouched");
        }

        [Test]
        public void ResumingARun_DoesntAnnounceWaysAlreadyFound()
        {
            var found = new List<string>();
            var original = NewGame();
            original.Schedule(_gather);
            original.BeginLoop();
            RunSeconds(original, 2f);

            var reopened = SaveAndReopen(original);
            reopened.WayFound += (from, to) => found.Add(to.DisplayName);
            RunSeconds(reopened, 2f);

            Assert.That(found, Is.Empty);
        }
    }
}
