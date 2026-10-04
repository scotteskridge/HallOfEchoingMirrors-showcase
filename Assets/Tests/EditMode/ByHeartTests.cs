using System;
using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Rooms known by heart (plan 023): a room worked in enough runs is known by heart, and what she
    /// did in such rooms last run is already queued for the next.
    /// </summary>
    public class ByHeartTests : SimulationTestBase
    {
        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _hall, _dark, _cellar;
        private ResourceDefinition _wisp;
        private TaskDefinition _sweep, _once, _dust, _mop, _gather, _use;

        // The hall (start) joins the dark, which joins the cellar. Sweep and the wisp work are in the
        // hall, dust in the dark, mop in the cellar.
        [SetUp]
        public void SetUp()
        {
            _wisp = MakeObject("Wisp", max: 5);
            _sweep = MakeTask("Sweep", 1f);
            _once = MakeTask("Wind the clock", 1f);
            _dust = MakeTask("Dust", 1f);
            _mop = MakeTask("Mop", 1f);
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);
            _use = MakeTask("Burn a wisp", 1f);
            _use.needs.Add(new ResourceAmount { resource = _wisp, amount = 1 });
            _use.takes.Add(new ResourceAmount { resource = _wisp, amount = 1 });

            _hall = MakeNode("The hall");
            _dark = MakeNode("The dark");
            _cellar = MakeNode("The cellar");
            Join(_hall, _dark);
            Join(_dark, _cellar);
            _hall.tasks.AddRange(new[] { _sweep, _once, _gather, _use });
            _dark.tasks.Add(_dust);
            _cellar.tasks.Add(_mop);

            _content = MakePlaces(_hall, _dark, _cellar);
            _content.tasks.AddRange(new[] { _sweep, _once, _dust, _mop, _gather, _use });
            _settings = MakeLoopSettings();
        }

        private Simulation NewGame() => new Simulation(_settings, TicksPerSecond, _content);

        /// <summary>One run: begins, queues what <paramref name="queue"/> adds, plays it out, and ends it early.</summary>
        private void PlayRun(Simulation sim, Action queue, float seconds = 20f)
        {
            sim.BeginLoop();
            queue();
            RunSeconds(sim, seconds);
            sim.EndRunEarly();
        }

        /// <summary>What she does in the hall, then the dark, then walks to the cellar without working there.</summary>
        private void HallThenDark(Simulation sim) => PlayRun(sim, () =>
        {
            sim.Schedule(_sweep, 2);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_dust, 1);
            sim.ScheduleTrip(_cellar);
        });

        [Test]
        public void DevSetRoomRuns_ChangesKnownByHeart()
        {
            var sim = NewGame();

            sim.DevSetRoomRuns(_dark, _settings.byHeartRuns);
            Assert.That(sim.IsKnownByHeart(_dark));

            sim.DevSetRoomRuns(_dark, _settings.byHeartRuns - 1);
            Assert.That(sim.IsKnownByHeart(_dark), Is.False);

            sim.DevSetRoomRuns(_dark, -5);
            Assert.That(sim.RunsWorkedIn(_dark), Is.EqualTo(0), "never below 0");
        }

        [Test]
        public void DevSetRoomRuns_RefusesTheStartRoom()
        {
            var sim = NewGame();

            Assert.Throws<InvalidOperationException>(() => sim.DevSetRoomRuns(_hall, 3));
        }

        [Test]
        public void RoomRuns_CountOncePerRun_NotPerEntry()
        {
            var sim = NewGame();
            PlayRun(sim, () =>
            {
                sim.Schedule(_sweep, 3);
                sim.ScheduleTrip(_dark);
                sim.Schedule(_dust, 2);
                sim.ScheduleTrip(_hall);
                sim.Schedule(_once, 1);
            });

            Assert.That(sim.RunsWorkedIn(_hall), Is.EqualTo(1), "five actions there, one run");
            Assert.That(sim.RunsWorkedIn(_dark), Is.EqualTo(1));
            Assert.That(sim.RunsWorkedIn(_cellar), Is.EqualTo(0));

            PlayRun(sim, () => sim.Schedule(_sweep, 1));
            Assert.That(sim.RunsWorkedIn(_hall), Is.EqualTo(2));
            Assert.That(sim.RunsWorkedIn(_dark), Is.EqualTo(1), "not worked in this run");
        }

        [Test]
        public void TripsAndSupply_DontCountAsWork()
        {
            var sim = NewGame();
            PlayRun(sim, () =>
            {
                sim.ScheduleTrip(_dark);
                sim.ScheduleTrip(_cellar);
                sim.ScheduleTrip(_dark);
            });
            Assert.That(sim.RunsWorkedIn(_hall), Is.EqualTo(1), "the start room counts every run, worked in or not");
            Assert.That(sim.RunsWorkedIn(_dark), Is.EqualTo(0));
            Assert.That(sim.RunsWorkedIn(_cellar), Is.EqualTo(0));

            // Burning a wisp with none in her pockets: the queue puts the gathering on top.
            sim.BeginLoop();
            sim.Schedule(_use, 1);
            RunSeconds(sim, 10);
            var gathering = sim.Loop.Steps.Find(step => step.Task == _gather);
            Assert.That(gathering.IsSupply, Is.True);
            Assert.That(gathering.CountsAsWork, Is.False);
            Assert.That(sim.Loop.Steps.Find(step => step.Task == _use).CountsAsWork, Is.True);
        }

        [Test]
        public void FourthRun_MakesRoomKnownByHeart()
        {
            var sim = NewGame();
            Assert.That(_settings.byHeartRuns, Is.EqualTo(4), "the agreed starting point");
            for (int run = 1; run <= 3; run++)
            {
                PlayRun(sim, () => sim.Schedule(_sweep, 1));
                Assert.That(sim.IsKnownByHeart(_hall), Is.False, $"after {run} runs");
            }

            PlayRun(sim, () => sim.Schedule(_sweep, 1));

            Assert.That(sim.IsKnownByHeart(_hall), Is.True);
            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(1), "the 4th run already carries into the 5th");
        }

        [Test]
        public void CarriedPlan_StopsAtFirstUnknownRoom()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();

            HallThenDark(sim);

            // The cellar was only walked into, never worked in: the trip there is where it stops.
            var entries = sim.Queue.Entries;
            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(entries[0].Task, Is.EqualTo(_sweep));
            Assert.That(entries[1].Destination, Is.EqualTo(_dark));
            Assert.That(entries[2].Task, Is.EqualTo(_dust));
            Assert.That(entries, Has.All.Matches<QueueEntry>(e => e.ByHeart));
            Assert.That(sim.IsKnownByHeart(_cellar), Is.False);
        }

        [Test]
        public void RunReport_NamesTheRoomsThisRunMadeKnownByHeart()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            HallThenDark(sim);
            Assert.That(sim.LastRun.KnownByHeartNow, Is.Empty, "worked in once: not yet");

            HallThenDark(sim);
            Assert.That(sim.LastRun.KnownByHeartNow, Is.EquivalentTo(new[] { _hall, _dark }));

            HallThenDark(sim);
            Assert.That(sim.LastRun.KnownByHeartNow, Is.Empty, "already known: not news again");
        }

        // The start room is where every run begins, so it counts every run, worked in or not (plan 032a).
        [Test]
        public void StartRoom_KnownByHeart_AfterEnoughRuns_WithoutWork()
        {
            var sim = NewGame();
            for (int run = 1; run <= 3; run++)
            {
                PlayRun(sim, () =>
                {
                    sim.ScheduleTrip(_dark);
                    sim.Schedule(_dust, 1);
                });
                Assert.That(sim.RunsWorkedIn(_hall), Is.EqualTo(run), "every run counts");
                Assert.That(sim.IsKnownByHeart(_hall), Is.False, $"after {run} runs");
            }

            PlayRun(sim, () =>
            {
                sim.ScheduleTrip(_dark);
                sim.Schedule(_dust, 1);
            });

            Assert.That(sim.IsKnownByHeart(_hall), Is.True);
            Assert.That(sim.LastRun.KnownByHeartNow, Has.Member(_hall));
        }

        [Test]
        public void CarryPlan_CarriesTripOutOfStartRoom()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();

            PlayRun(sim, () =>
            {
                sim.ScheduleTrip(_dark);
                sim.Schedule(_dust, 1);
            });

            var entries = sim.Queue.Entries;
            Assert.That(entries, Has.Count.EqualTo(2), "no work in the hall, yet the plan carries");
            Assert.That(entries[0].Destination, Is.EqualTo(_dark));
            Assert.That(entries[1].Task, Is.EqualTo(_dust));
        }

        [Test]
        public void CarriedPlan_EmptyWhenStartRoomUnknown()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();

            HallThenDark(sim);

            Assert.That(sim.IsKnownByHeart(_hall), Is.False, "worked in once");
            Assert.That(sim.Queue.Entries, Is.Empty, "runs 1 to 3 carry nothing");
        }

        [Test]
        public void CarriedPlan_LeavesOutSupplyAndLockedTasks()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            sim.BeginLoop();
            sim.Schedule(_use, 1);
            sim.Schedule(_once, 1);
            sim.Schedule(_sweep, 1);
            RunSeconds(sim, 10);
            sim.Persistent.LockedTasks.Add(_once); // a switch turns it off during the run
            sim.EndRunEarly();

            var carried = sim.Queue.Entries;
            Assert.That(carried, Has.Count.EqualTo(2));
            Assert.That(carried[0].Task, Is.EqualTo(_use), "the wisp gathering isn't carried: it is added again when needed");
            Assert.That(carried[1].Task, Is.EqualTo(_sweep), "the locked task dropped out");
        }

        [Test]
        public void CarriedPlan_MergesRepeats()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();

            PlayRun(sim, () => sim.Schedule(_sweep, 3));

            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(1));
            Assert.That(sim.Queue.Entries[0].TimesLeft, Is.EqualTo(3));
        }

        [Test]
        public void CarriedEntry_ThatCantStart_IsSkippedWithReason()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            var skipped = new List<string>();
            sim.TaskSkipped += (task, reason) => skipped.Add($"{task.displayName}: {reason}");
            PlayRun(sim, () =>
            {
                sim.Schedule(_once, 1);
                sim.Schedule(_sweep, 1);
            });
            sim.Persistent.LockedTasks.Add(_once); // turned off since: the plan still holds it

            Assert.That(sim.Queue.Entries[0].Task, Is.EqualTo(_once));
            sim.BeginLoop();
            RunSeconds(sim, 5);

            Assert.That(skipped, Is.EqualTo(new[] { $"{_once.displayName}: {Reason("no_longer_possible")}" }));
            Assert.That(sim.Loop.CompletionLog, Is.EqualTo(new[] { _sweep }), "the rest of the plan went on");
        }

        [Test]
        public void NextQueue_AndRoomRuns_RoundTrip()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);

            var warnings = new List<string>();
            var reopened = Reopen(sim, _settings, _content, warnings);

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.RunsWorkedIn(_hall), Is.EqualTo(1));
            Assert.That(reopened.RunsWorkedIn(_dark), Is.EqualTo(1));
            var entries = reopened.Queue.Entries;
            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(entries[0].Task, Is.EqualTo(_sweep));
            Assert.That(entries[0].TimesLeft, Is.EqualTo(2));
            Assert.That(entries[1].Destination, Is.EqualTo(_dark));
            Assert.That(entries[2].Task, Is.EqualTo(_dust));
            Assert.That(entries, Has.All.Matches<QueueEntry>(e => e.ByHeart));
        }

        [Test]
        public void RunUnderWay_KeepsItsSteps_SoItStillCarriesOver()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            sim.BeginLoop();
            sim.Schedule(_sweep, 2);
            sim.ScheduleTrip(_dark);
            sim.Schedule(_dust, 3);
            RunSeconds(sim, 4);

            var warnings = new List<string>();
            var reopened = Reopen(sim, _settings, _content, warnings); // resumes the run; nothing in the between-runs queue
            Assert.That(reopened.Phase, Is.EqualTo(LoopPhase.Running));
            RunSeconds(reopened, 10);
            reopened.EndRunEarly();

            Assert.That(warnings, Is.Empty);
            Assert.That(reopened.Queue.Entries, Has.Count.EqualTo(3), "sweep, the trip, dust: as if it had never been saved");
        }

        [Test]
        public void OldSave_Upgrades_WithNoRoomRuns()
        {
            var data = SaveSerializer.FromJson("{ \"version\": 15, \"loopsCompleted\": 6 }", out string error);

            Assert.That(error, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            var warnings = new List<string>();
            var sim = SaveSerializer.Load(data, new ContentIndex(_content), _settings, TicksPerSecond, _content, warnings, out _);

            Assert.That(warnings, Is.Empty);
            Assert.That(sim.RunsWorkedIn(_hall), Is.EqualTo(0));
            Assert.That(sim.Queue.Entries, Is.Empty);
        }

        [Test]
        public void ARunLoggedBeforeVersion17_KeepsItsReport_ButCarriesNothing()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            sim.BeginLoop();
            sim.Schedule(_sweep, 2);
            RunSeconds(sim, 3);

            // As a version 16 save wrote it: the plain log, and no steps.
            var data = SaveSerializer.Capture(sim);
            data.version = 16;
            foreach (var step in data.run.steps) data.run.actionLog.Add(new SavedAction { task = step.task, item = step.item });
            data.run.steps.Clear();
            var loaded = SaveSerializer.FromJson(SaveSerializer.ToJson(data), out string error);
            Assert.That(error, Is.Null);
            var warnings = new List<string>();
            var reopened = SaveSerializer.Load(loaded, new ContentIndex(_content), _settings, TicksPerSecond, _content, warnings, out bool resumed);

            Assert.That(warnings, Is.Empty);
            Assert.That(resumed, Is.True);
            Assert.That(reopened.Loop.CompletionLog, Is.EqualTo(sim.Loop.CompletionLog));
            reopened.EndRunEarly();
            Assert.That(reopened.Queue.Entries, Is.Empty, "no rooms were recorded, so nothing is known by heart");
        }

        // ---------- Blocks in the queue (plan ui-024b) ----------

        private readonly List<QueueStop> _stops = new List<QueueStop>();

        [Test]
        public void Stop_AllCarriedEntries_IsByHeart()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);

            sim.QueueStops(_stops);

            Assert.That(_stops, Has.Count.EqualTo(2));
            Assert.That(_stops[0].ByHeart, Is.True, "the sweep in the hall");
            Assert.That(_stops[1].ByHeart, Is.True, "the trip and dust in the dark");
        }

        [Test]
        public void Stop_WithPlayerEntry_IsNotByHeart()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);
            // The player puts their own dusting into the dark stop, between the carried trip and dusting.
            sim.Queue.Entries.Insert(2, new QueueEntry(_dust, null, 1));

            sim.QueueStops(_stops);

            Assert.That(_stops[0].ByHeart, Is.True, "the hall's stop is untouched");
            Assert.That(_stops[1].ByHeart, Is.False, "one player entry unfolds the whole stop");
        }

        [Test]
        public void Stop_WithNoEntries_IsNotByHeart()
        {
            var sim = NewGame();
            sim.QueueStops(_stops);
            Assert.That(_stops, Has.Count.EqualTo(1));
            Assert.That(_stops[0].ByHeart, Is.False);
        }

        [Test]
        public void Stop_ByHeartEntriesInTwoRooms_AreTwoBlocks()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            PlayRun(sim, () =>
            {
                sim.Schedule(_sweep, 1);
                sim.ScheduleTrip(_dark);
                sim.Schedule(_dust, 1);
                sim.ScheduleTrip(_hall);
                sim.Schedule(_once, 1);
            });

            sim.QueueStops(_stops);

            Assert.That(_stops.ConvertAll(s => s.Room), Is.EqualTo(new[] { _hall, _dark, _hall }));
            Assert.That(_stops, Has.All.Matches<QueueStop>(s => s.ByHeart), "one block per visit, the return to the hall too");
        }

        [Test]
        public void Stop_InARoomOneRunShort_CarriesOnceWorkIsQueuedThere()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            HallThenDark(sim); // the dark: 1 of 2 runs (the hall counts every run, so it will be known)
            sim.Queue.Entries.Clear();
            sim.ScheduleTrip(_dark);

            sim.QueueStops(_stops);
            Assert.That(_stops[0].CarriesOver, Is.True, "the start room is added by every run");
            Assert.That(_stops[1].CarriesOver, Is.False, "nothing queued in the dark: this run wouldn't add it");

            sim.Schedule(_dust, 1);
            sim.QueueStops(_stops);
            Assert.That(_stops[1].CarriesOver, Is.True, "working there this run makes it known by heart when the run ends");
        }

        [Test]
        public void Stop_AfterWorkingInARoomThatWontBeKnown_DoesntCarry_MidRun()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            for (int run = 0; run < 2; run++)
                PlayRun(sim, () => sim.Schedule(_sweep, 1)); // the hall is known; the dark never worked in
            sim.Queue.Entries.Clear();

            sim.BeginLoop();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_dust, 1);
            sim.ScheduleTrip(_hall);
            sim.Schedule(_sweep, 50);
            for (int i = 0; i < 12 && sim.Loop.Steps.Count < 3; i++)
                RunSeconds(sim, 5);
            Assert.That(sim.Loop.Steps.Count, Is.GreaterThanOrEqualTo(3), "the trip, the dusting and the trip back are done");

            sim.QueueStops(_stops);

            Assert.That(_stops[0].Room, Is.EqualTo(_hall));
            Assert.That(sim.IsKnownByHeart(_hall), Is.True);
            Assert.That(_stops[0].CarriesOver, Is.False, "the dark was worked in once and won't be known: the carry stops there");
        }

        [Test]
        public void Stop_CarriesOver_OnlyWhileEveryRoomSoFarIsKnownByHeart()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            HallThenDark(sim);
            HallThenDark(sim); // the hall and the dark are now known; the cellar never worked in
            sim.ScheduleTrip(_cellar);

            sim.QueueStops(_stops);

            Assert.That(_stops.ConvertAll(s => s.Room), Is.EqualTo(new[] { _hall, _dark, _cellar }));
            Assert.That(_stops[0].CarriesOver, Is.True);
            Assert.That(_stops[1].CarriesOver, Is.True);
            var cellar = _stops.Find(s => s.Room == _cellar);
            Assert.That(cellar.CarriesOver, Is.False, "not known by heart");
        }

        [Test]
        public void Stop_AfterARoomThatDoesntCarry_DoesntCarryEither()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            // The hall and the cellar are worked twice; the dark (between them) never is.
            for (int run = 0; run < 2; run++)
                PlayRun(sim, () =>
                {
                    sim.Schedule(_sweep, 1);
                    sim.ScheduleTrip(_dark);
                    sim.ScheduleTrip(_cellar);
                    sim.Schedule(_mop, 1);
                });
            sim.Queue.Entries.Clear();
            sim.ScheduleTrip(_dark);
            sim.ScheduleTrip(_cellar);
            sim.Schedule(_mop, 1);

            sim.QueueStops(_stops);

            Assert.That(sim.IsKnownByHeart(_cellar), Is.True);
            Assert.That(sim.IsKnownByHeart(_dark), Is.False);
            Assert.That(_stops[0].CarriesOver, Is.True, "the hall");
            Assert.That(_stops[1].CarriesOver, Is.False, "the dark");
            Assert.That(_stops[2].CarriesOver, Is.False, "the cellar is known, but the carry stopped at the dark");
        }

        [Test]
        public void RemoveStop_RemovesAllItsEntries_AndNothingElse()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim); // sweep | trip to the dark, dust

            sim.RemoveStop(1);

            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(1));
            Assert.That(sim.Queue.Entries[0].Task, Is.EqualTo(_sweep));
        }

        [Test]
        public void RemoveStop_BadIndex_Throws()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);

            Assert.Throws<ArgumentOutOfRangeException>(() => sim.RemoveStop(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.RemoveStop(-1));
            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(3), "nothing removed");
        }

        [Test]
        public void RemoveStopOf_AnEntry_RemovesItsWholeStop()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim); // sweep | trip to the dark, dust
            var dust = sim.Queue.Entries[2];

            Assert.That(sim.RemoveStopOf(dust), Is.True);

            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(1), "the trip went with it");
            Assert.That(sim.Queue.Entries[0].Task, Is.EqualTo(_sweep));
        }

        [Test]
        public void RemoveStopOf_AnEntryNoLongerInTheQueue_ReturnsFalseAndChangesNothing()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);
            var gone = new QueueEntry(_sweep);

            Assert.That(sim.RemoveStopOf(gone), Is.False);

            Assert.That(sim.Queue.Entries, Has.Count.EqualTo(3));
        }

        // ---------- The planning screen's foot (plan ui-024c) ----------

        [Test]
        public void CarriedActionCount_SumsByHeartEntryCounts()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim); // sweep x2 | trip to the dark, dust

            Assert.That(sim.CarriedActionCount, Is.EqualTo(4), "2 sweeps + the trip + dust: repeats add up");

            sim.Schedule(_once, 3); // the player's own: not carried
            Assert.That(sim.CarriedActionCount, Is.EqualTo(4));
        }

        [Test]
        public void CarriedActionCount_ZeroWhenNothingCarried()
        {
            _settings.byHeartRuns = 2;
            var sim = NewGame();
            Assert.That(sim.CarriedActionCount, Is.Zero, "an empty plan");

            HallThenDark(sim);
            Assert.That(sim.CarriedActionCount, Is.Zero, "runs 1 to 3 carry nothing");
        }

        [Test]
        public void PlanEndsIn_IsRoomAfterLastEntry()
        {
            _settings.byHeartRuns = 1;
            var sim = NewGame();
            HallThenDark(sim);

            Assert.That(sim.PlanEndsIn, Is.EqualTo(_dark), "sweep in the hall, then the trip, then dust in the dark");
        }

        [Test]
        public void RunUnderWay_TrueOnlyBetweenBeginAndEnd()
        {
            var sim = NewGame();
            Assert.That(sim.RunUnderWay, Is.False, "the game opens between runs");
            sim.BeginLoop();
            Assert.That(sim.RunUnderWay, Is.True);
            sim.EndRunEarly();
            Assert.That(sim.RunUnderWay, Is.False);
        }

        [Test]
        public void PlanEndsIn_NullWhenPlanEmpty()
        {
            var sim = NewGame();
            Assert.That(sim.PlanEndsIn, Is.Null);
        }
    }
}
