using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// When the top of the queue lacks only an item, an action in the room that gives it goes on top
    /// first (what's on the floor here already counts), until there's enough.
    /// </summary>
    public class SupplyTests : SimulationTestBase
    {
        private List<string> _skipped = new List<string>();
        private GameContent _content;
        private NodeDefinition _hall;
        private ResourceDefinition _candle, _flint, _lit;
        private TaskDefinition _makeCandle, _makeFlint, _light;

        // In the hall: make a candle (1s), make flint and steel (1s, once a run), and light a candle
        // (1s; needs flint and a candle, uses the candle, up to 3 lit). Flint is a tool: needed, not used up.
        [SetUp]
        public void SetUp()
        {
            _skipped.Clear();
            _candle = MakeObject("Candle", max: 20);
            _flint = MakeResource("Flint and steel", max: 1);
            _lit = MakeResource("Candles lit", max: 3);

            _makeCandle = MakeGatherTask("Make a candle", 1f, _candle);
            _makeFlint = MakeGatherTask("Make flint and steel", 1f, _flint);
            _makeFlint.oncePerRun = true;
            _light = MakeGatherTask("Light a candle", 1f, _lit);
            _light.needs.Add(new ResourceAmount { resource = _flint, amount = 1 });
            _light.needs.Add(new ResourceAmount { resource = _candle, amount = 1 });
            _light.takes.Add(new ResourceAmount { resource = _candle, amount = 1 });

            _hall = MakeNode("The hall");
            _hall.tasks.AddRange(new[] { _makeCandle, _makeFlint, _light });
            _content = MakePlaces(_hall);
            _content.tasks.AddRange(new[] { _makeCandle, _makeFlint, _light });
        }

        private Simulation Begin(int pockets = 5, int floor = 10)
        {
            var sim = new Simulation(MakeLoopSettings(pockets: pockets, floor: floor), TicksPerSecond, _content);
            _skipped = RecordRefusals(sim);
            sim.BeginLoop();
            return sim;
        }

        /// <summary>An action in the hall (listed nowhere else, so it's only done here).</summary>
        private TaskDefinition AddToHall(TaskDefinition task)
        {
            _hall.tasks.Add(task);
            _content.tasks.Add(task);
            return task;
        }

        private static string NeedsSkip(string task, string item) => $"{task}: {Reason("needs", ("amount", 1), ("item", item))}";

        [Test]
        public void PlayingSomethingThatNeedsWhatIsBeingMade_ResumesTheUnfinishedAction_NotAFreshOne()
        {
            var sim = Begin();
            sim.PlayNow(_makeFlint);
            RunSeconds(sim, 0.5f); // half made

            sim.PlayNow(_light); // needs the flint that's half made

            RunSeconds(sim, 0.8f); // half a second of work left, if the progress was kept
            Assert.That(sim.AmountOf(_flint), Is.EqualTo(1), "the flint's progress was kept, not restarted");
        }

        [Test]
        public void TheProgressMovesToTheSupplier_ButThePlayersOwnRepeatingEntryStaysQueued()
        {
            var sim = Begin();
            sim.Schedule(_makeCandle); // repeats until she can hold no more
            RunSeconds(sim, 0.5f);     // half a candle made

            sim.PlayNow(_makeFlint);   // something else on top; the candle entry waits underneath
            RunSeconds(sim, 0.3f);
            sim.PlayNow(_light);       // needs flint (made once) and a candle

            RunSeconds(sim, 12f);
            Assert.That(sim.AmountOf(_lit), Is.EqualTo(3), "everything she asked for was made");
            Assert.That(sim.Queue.Entries.Exists(e => e.Task == _makeCandle), Is.True,
                "her own repeat-until-full candle entry wasn't used up as a supplier");
        }

        [Test]
        public void ABlockedAction_GetsWhatItNeedsFirst_ThenRuns()
        {
            var sim = Begin();
            sim.Schedule(_light);

            RunSeconds(sim, 0.5f);
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(_makeFlint), "flint first: it's the first thing missing");

            RunSeconds(sim, 30f);
            Assert.That(sim.AmountOf(_lit), Is.EqualTo(3), "all three lit");
            Assert.That(sim.AmountOf(_candle), Is.EqualTo(0), "and only enough candles made for them");
            Assert.That(_skipped, Is.Empty, "nothing was dropped");
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void TheSupplier_MakesEnoughToFinish_NotMore()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light);

            RunSeconds(sim, 3.5f); // three candles made, one second each

            Assert.That(sim.AmountOf(_candle), Is.EqualTo(3), "3 lit to go: 3 candles");
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(_light), "enough: back to lighting");
        }

        [Test]
        public void ARepeatLimit_SetsHowManyAreEnough()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light, 2); // two goes, though 3 could be lit

            RunSeconds(sim, 2.5f);

            Assert.That(sim.AmountOf(_candle), Is.EqualTo(2));
            Assert.That(sim.Queue.Top.Task, Is.EqualTo(_light));
        }

        [Test]
        public void ATool_IsWantedOnce_ItIsntUsedUp()
        {
            var taper = MakeResource("Taper", max: 10);
            var carve = AddToHall(MakeGatherTask("Carve a taper", 1f, taper)); // can be made again and again
            _light.needs.Add(new ResourceAmount { resource = taper, amount = 1 }); // needed, never taken
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Loop.ToolsAndStats[_candle] = 3;
            sim.Schedule(_light);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(taper), Is.EqualTo(1), "one taper for all three lights");
            Assert.That(sim.AmountOf(_lit), Is.EqualTo(3));
        }

        [Test]
        public void WhatsOnTheFloorHere_IsUsed_WithoutPickingItUpOrMakingMore()
        {
            var sim = Begin(pockets: 1);
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_makeCandle, 4); // 1 in her pocket, 3 on the floor
            RunSeconds(sim, 4f);
            Assert.That(sim.OnFloor(_hall, _candle), Is.EqualTo(3));

            sim.Schedule(_light, 2);
            RunSeconds(sim, 2.5f);

            Assert.That(sim.AmountOf(_lit), Is.EqualTo(2));
            Assert.That(sim.OnFloor(_hall, _candle), Is.EqualTo(1), "used from the floor first");
            Assert.That(sim.AmountOf(_candle), Is.EqualTo(1), "the one in her pocket kept");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "nothing made, nothing picked up");
        }

        [Test]
        public void ASuppliersOwnMissingItem_IsSuppliedToo()
        {
            var wax = MakeObject("Wax", max: 20);
            _makeCandle.needs.Add(new ResourceAmount { resource = wax, amount = 1 });
            _makeCandle.takes.Add(new ResourceAmount { resource = wax, amount = 1 });
            AddToHall(MakeGatherTask("Gather wax", 1f, wax));
            var sim = Begin(pockets: 10);
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light);

            RunSeconds(sim, 30f);

            Assert.That(sim.AmountOf(_lit), Is.EqualTo(3));
            Assert.That(sim.AmountOf(wax), Is.EqualTo(0), "just enough wax for the candles");
            Assert.That(sim.AmountOf(_candle), Is.EqualTo(0));
            Assert.That(_skipped, Is.Empty);
        }

        [Test]
        public void AChainDeeperThanTheLimit_IsDropped_WithoutLooping()
        {
            // Light ← candle ← wax ← comb ← bees: four suppliers deep, one more than the limit.
            var wax = MakeObject("Wax", max: 20);
            var comb = MakeObject("Comb", max: 20);
            var bees = MakeObject("Bees", max: 20);
            _makeCandle.needs.Add(new ResourceAmount { resource = wax, amount = 1 });
            var gatherWax = AddToHall(MakeGatherTask("Gather wax", 1f, wax));
            gatherWax.needs.Add(new ResourceAmount { resource = comb, amount = 1 });
            var cutComb = AddToHall(MakeGatherTask("Cut comb", 1f, comb));
            cutComb.needs.Add(new ResourceAmount { resource = bees, amount = 1 });
            AddToHall(MakeGatherTask("Keep bees", 1f, bees));
            var sim = Begin(pockets: 10);
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light);

            RunTicks(sim, 1);

            Assert.That(_skipped, Is.EqualTo(new[] { NeedsSkip("Light a candle", "Candle") }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void AnActionBlockedForAnotherReason_GetsNoSupplier()
        {
            _light.requiresAttributes.Add(new TaskDefinition.AttributeRequirement { attribute = ClaraAttribute.Perception, level = 5 });
            var sim = Begin();
            sim.Schedule(_light);

            RunTicks(sim, 1);

            // The refusal names the stat before the item (decisions log, 2026-10-01).
            string needsPerception = Reason("needs_attribute", ("attribute", GameText.Attribute(ClaraAttribute.Perception)), ("level", 5));
            Assert.That(_skipped, Is.EqualTo(new[] { $"Light a candle: {needsPerception}" }),
                "supplying the flint wouldn't be enough: it still needs Perception 5");
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void ASupplier_Stops_WhenItsActionIsTakenOutOfTheQueue()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light);
            RunSeconds(sim, 0.5f); // making the first candle, for the light below it

            sim.RemoveFromQueue(1); // the light
            RunSeconds(sim, 3f);

            Assert.That(sim.AmountOf(_candle), Is.EqualTo(1), "finishes the candle under way, then stops");
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void WithNothingHereToSupplyIt_TheActionIsDroppedAsBefore()
        {
            _hall.tasks.Remove(_makeCandle);
            _content.tasks.Remove(_makeCandle); // (listed nowhere, it could be done anywhere)
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Schedule(_light);

            RunTicks(sim, 1);

            Assert.That(_skipped, Is.EqualTo(new[] { NeedsSkip("Light a candle", "Candle") }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void ASupplierThatCantStart_IsntPicked()
        {
            var sim = Begin();
            sim.Loop.CompletedTasks.Add(_makeFlint); // flint already made this run, and since lost
            sim.Schedule(_light);

            RunTicks(sim, 1);

            Assert.That(_skipped, Is.EqualTo(new[] { NeedsSkip("Light a candle", "Flint and steel") }), "no way to get flint: dropped, no loop");
        }

        [Test]
        public void LightingACandle_WhenAllAreLit_IsRefusedWithTheCountReason()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_flint] = 1;
            sim.Loop.ToolsAndStats[_candle] = 1;
            sim.Loop.ToolsAndStats[_lit] = 3; // the hall's maximum

            sim.PlayNow(_light);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Light a candle: {Reason("count_at_max", ("item", "Candles lit"), ("max", 3))}" }));
        }

        [Test]
        public void LightingACandle_WhenAllAreLit_LeavesThePopover()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_lit] = 3; // the hall's maximum

            Assert.That(sim.TasksAt(_hall), Has.No.Member(_light), "nothing more to light this run");
        }

        [Test]
        public void MakingACandle_StillWorks_WhenAllAreLit()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_lit] = 3; // the hall's maximum
            Assert.That(sim.TasksAt(_hall), Has.Member(_makeCandle), "still offered to carry a candle onward");

            sim.PlayNow(_makeCandle);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_candle), Is.EqualTo(1), "still makes one, though the hall is fully lit");
        }
    }
}
