using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Moving queue entries and whole stops (dragging in the queue drawer): a move is refused if it
    /// makes a trip skip or leaves an action in a room that doesn't offer it; the running action is
    /// handled like To top.
    /// </summary>
    public class QueueReorderTests : SimulationTestBase
    {
        private GameContent _content;
        private TaskDefinition _look, _listen, _sweep, _polish, _long;
        private NodeDefinition _hall, _corridor, _vault, _attic;

        // The hall ↔ the corridor ↔ the vault. The attic is joined to nothing. Sweep is only done in
        // the corridor, Polish only in the vault; the rest anywhere.
        [SetUp]
        public void SetUp()
        {
            _hall = MakeNode("A Dark Hall");
            _corridor = MakeNode("The Corridor");
            _vault = MakeNode("The Vault");
            _attic = MakeNode("The Attic");
            Join(_hall, _corridor);
            Join(_corridor, _vault);
            _content = MakePlaces(_hall, _corridor, _vault, _attic);
            _look = MakeTask("Look", 1f);
            _listen = MakeTask("Listen", 1f);
            _long = MakeTask("Wait", 10f);
            _sweep = MakeTask("Sweep", 1f);
            _polish = MakeTask("Polish", 1f);
            _corridor.tasks.Add(_sweep);
            _vault.tasks.Add(_polish);
            _content.tasks.AddRange(new[] { _look, _listen, _long, _sweep, _polish });
        }

        private Simulation MakeSimulation() => new Simulation(MakeLoopSettings(), TicksPerSecond, _content);

        private static TaskDefinition[] TasksOf(Simulation sim)
        {
            var tasks = new List<TaskDefinition>();
            foreach (var entry in sim.Queue.Entries)
                tasks.Add(entry.Task);
            return tasks.ToArray();
        }

        // ---------- Moving an entry ----------

        [Test]
        public void MoveActionWithinStop_Reorders()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);
            sim.Schedule(_listen);
            int changes = 0;
            sim.QueueChanged += () => changes++;

            Assert.That(sim.CanMoveEntry(1, 0, out string reason), Is.True);
            Assert.That(reason, Is.Null);
            Assert.That(sim.MoveEntry(1, 0), Is.True);

            Assert.That(TasksOf(sim), Is.EqualTo(new[] { _listen, _look }));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void MoveToOwnPlace_ChangesNothingButIsAllowed()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);
            sim.Schedule(_listen);

            Assert.That(sim.CanMoveEntry(0, 1, out _), Is.True, "the gap just below itself is where it already is");
            Assert.That(sim.MoveEntry(0, 1), Is.True);
            Assert.That(TasksOf(sim), Is.EqualTo(new[] { _look, _listen }));
        }

        [Test]
        public void MoveActionToStopForSameRoom_Allowed()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor); // 0
            sim.Schedule(_sweep);        // 1: in the corridor
            sim.ScheduleTrip(_vault);    // 2
            sim.ScheduleTrip(_corridor); // 3: back to the corridor
            sim.Schedule(_look);         // 4

            Assert.That(sim.MoveEntry(1, 5), Is.True, "the corridor offers Sweep on the return visit too");

            Assert.That(TasksOf(sim)[4], Is.EqualTo(_sweep));
            Assert.That(sim.PlannedNodeAfter(4), Is.EqualTo(_corridor));
        }

        [Test]
        public void MoveActionToOtherRoom_RefusedNotHere()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor); // 0
            sim.Schedule(_sweep);        // 1
            sim.ScheduleTrip(_vault);    // 2

            Assert.That(sim.CanMoveEntry(1, 3, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(Reason("not_here", ("room", "the Vault"))));
        }

        [Test]
        public void MoveThatSkipsATrip_RefusedWithTripReason()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor); // 0
            sim.ScheduleTrip(_vault);    // 1

            Assert.That(sim.CanMoveEntry(1, 0, out string reason), Is.False, "no way from the hall to the vault");
            Assert.That(reason, Is.EqualTo(Reason("no_way", ("room", "a Dark Hall"))),
                "the words the run uses when it skips the trip");
        }

        [Test]
        public void AlreadyBrokenEntries_DontBlockAMove()
        {
            var sim = MakeSimulation();
            sim.ScheduleTrip(_attic); // 0: no way there, already skipped
            sim.Schedule(_polish);    // 1: not offered in the hall, already broken
            sim.Schedule(_look);      // 2
            sim.Schedule(_listen);    // 3

            Assert.That(sim.CanMoveEntry(3, 0, out string reason), Is.True, reason);
            Assert.That(sim.CanMoveEntry(1, 4, out reason), Is.True, "moving a broken entry to another bad place is no worse");
        }

        [Test]
        public void RefusedMove_ChangesNothing()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_sweep);
            sim.ScheduleTrip(_vault);
            var before = new List<QueueEntry>(sim.Queue.Entries);
            int changes = 0;
            sim.QueueChanged += () => changes++;

            Assert.That(sim.MoveEntry(1, 3), Is.False);

            Assert.That(sim.Queue.Entries, Is.EqualTo(before));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void BetweenRuns_MovesNextRunsQueue()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            RunSeconds(sim, 2);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_corridor));
            sim.EndRunEarly();

            sim.ScheduleTrip(_corridor); // 0
            sim.Schedule(_sweep);        // 1
            sim.Schedule(_look);         // 2

            Assert.That(sim.CanMoveEntry(1, 0, out string reason), Is.False,
                "planned from the start room, not where the last run ended");
            Assert.That(reason, Is.EqualTo(Reason("not_here", ("room", "a Dark Hall"))));
            Assert.That(sim.MoveEntry(2, 0), Is.True);
            Assert.That(TasksOf(sim), Is.EqualTo(new[] { _look, sim.TravelVerb, _sweep }));
        }

        // ---------- The running entry, and supplier links ----------

        [Test]
        public void DropAboveRunning_SuspendsItKeepingProgress()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_long);
            sim.Schedule(_look);
            RunSeconds(sim, 3);
            var waiting = sim.Queue.Entries[0];
            Assert.That(sim.Loop.RunningEntry, Is.SameAs(waiting));

            Assert.That(sim.MoveEntry(1, 0), Is.True);

            Assert.That(sim.Loop.RunningEntry, Is.Null, "she stops the old one at once");
            Assert.That(sim.Queue.Entries[1], Is.SameAs(waiting));
            Assert.That(waiting.SavedFraction, Is.EqualTo(0.3f).Within(0.02f), "and it keeps its progress");
            RunTicks(sim, 1);
            Assert.That(sim.Loop.CurrentTask, Is.EqualTo(_look), "the dropped one starts");
        }

        [Test]
        public void DragRunningDown_SuspendsIt()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_long);
            sim.Schedule(_look);
            RunSeconds(sim, 3);
            var waiting = sim.Queue.Entries[0];

            Assert.That(sim.MoveEntry(0, 2), Is.True);

            Assert.That(sim.Queue.Entries[1], Is.SameAs(waiting));
            Assert.That(waiting.SavedFraction, Is.GreaterThan(0f));
            RunTicks(sim, 1);
            Assert.That(sim.Loop.CurrentTask, Is.EqualTo(_look));
        }

        [Test]
        public void MoveBelowRunning_KeepsItRunning()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_long);
            sim.Schedule(_look);
            sim.Schedule(_listen);
            RunSeconds(sim, 3);
            var running = sim.Loop.RunningEntry;

            Assert.That(sim.MoveEntry(2, 1), Is.True);

            Assert.That(sim.Loop.RunningEntry, Is.SameAs(running));
            Assert.That(sim.Loop.CurrentTaskProgress, Is.GreaterThan(0.25f));
        }

        [Test]
        public void MoveEntry_ASuppliedEntry_KeepsItsSupplierLink()
        {
            var sim = MakeSimulation();
            var blocked = new QueueEntry(_listen);
            var supplier = new QueueEntry(_look) { SuppliesFor = blocked };
            sim.Queue.Entries.Add(supplier);
            sim.Queue.Entries.Add(new QueueEntry(_long));
            sim.Queue.Entries.Add(blocked);

            Assert.That(sim.MoveEntry(2, 1), Is.True);

            Assert.That(supplier.SuppliesFor, Is.SameAs(blocked), "links are to the entry itself, not its place");
            Assert.That(sim.Queue.Entries.IndexOf(blocked), Is.EqualTo(1));
        }

        // ---------- Moving a whole stop ----------

        [Test]
        public void MoveStop_MovesTripAndItsActions()
        {
            Join(_hall, _vault); // a triangle, so the stops can go either way round
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor); // stop 1 (0-based): 0, 1
            sim.Schedule(_sweep);
            sim.ScheduleTrip(_vault);    // stop 2: 2, 3
            sim.Schedule(_polish);
            int changes = 0;
            sim.QueueChanged += () => changes++;

            Assert.That(sim.CanMoveStop(2, 1, out string reason), Is.True, reason);
            Assert.That(sim.MoveStop(2, 1), Is.True);

            Assert.That(TasksOf(sim), Is.EqualTo(new[] { sim.TravelVerb, _polish, sim.TravelVerb, _sweep }));
            var stops = StopsOf(sim);
            Assert.That(stops[1].Room, Is.EqualTo(_vault));
            Assert.That(stops[2].Room, Is.EqualTo(_corridor));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void MoveStopToEnd_Allowed()
        {
            Join(_hall, _vault);
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor);
            sim.Schedule(_sweep);
            sim.ScheduleTrip(_vault);
            sim.Schedule(_polish);

            Assert.That(sim.MoveStop(1, 3), Is.True, "3 is after the last stop");

            Assert.That(TasksOf(sim), Is.EqualTo(new[] { sim.TravelVerb, _polish, sim.TravelVerb, _sweep }));
        }

        [Test]
        public void MoveStopWhereNoWay_Refused()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.ScheduleTrip(_corridor); // stop 1
            sim.Schedule(_sweep);
            sim.ScheduleTrip(_vault);    // stop 2
            sim.Schedule(_polish);
            var before = new List<QueueEntry>(sim.Queue.Entries);

            Assert.That(sim.CanMoveStop(2, 1, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(Reason("no_way", ("room", "a Dark Hall"))));
            Assert.That(sim.MoveStop(2, 1), Is.False);
            Assert.That(sim.Queue.Entries, Is.EqualTo(before));
        }

        [Test]
        public void FirstStop_CantMove()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);
            sim.ScheduleTrip(_corridor);

            Assert.That(sim.CanMoveStop(0, 2, out _), Is.False, "where the queue starts has no trip to move");
            Assert.That(sim.MoveStop(0, 2), Is.False);
            Assert.That(TasksOf(sim), Is.EqualTo(new[] { _look, sim.TravelVerb }));
        }

        [Test]
        public void StopBeforeFirstStop_Refused()
        {
            var sim = MakeSimulation();
            sim.BeginLoop();
            sim.Schedule(_look);         // stop 0: where she is
            sim.ScheduleTrip(_corridor); // stop 1

            Assert.That(sim.CanMoveStop(1, 0, out string reason), Is.False,
                "a trip dropped in front of where she is would quietly move stop 0's actions to another room");
            Assert.That(reason, Is.Null);
            Assert.That(sim.MoveStop(1, 0), Is.False);
            Assert.That(TasksOf(sim), Is.EqualTo(new[] { _look, sim.TravelVerb }));
        }
    }
}
