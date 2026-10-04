using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Full pockets: what she makes, and one-of-a-kind things, push something else out onto the
    /// floor to make way (placeholder rule); what she only gathers goes on the floor instead. With
    /// nothing able to make way, a one-of-a-kind thing lands on the floor rather than being lost.
    /// </summary>
    public class FullPocketsTests : SimulationTestBase
    {
        private List<string> _skipped = new List<string>();
        private GameContent _content;
        private NodeDefinition _hall;
        private ResourceDefinition _wisp, _ring;
        private TaskDefinition _gather, _takeRing;

        // 2 pockets, floors of 3. The hall has wisps to gather and the ring to pull from a mirror.
        [SetUp]
        public void SetUp()
        {
            _skipped.Clear();
            _wisp = MakeObject("Wisp", max: 10);
            _wisp.nameInActions = "all wisps";
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);
            _ring = MakeObject("Roland's ring", max: 1);
            _takeRing = MakeGatherTask("Instantiate Roland's ring", 1f, _ring);
            _takeRing.kind = TaskKind.Instantiate;

            _hall = MakeNode("The hall");
            _hall.tasks.Add(_gather);
            _hall.tasks.Add(_takeRing);
            _content = MakePlaces(_hall);
            _content.tasks.Add(_gather);
            _content.tasks.Add(_takeRing);
            _content.pickUpVerb = MakeTask("Pick up", 1f);
            _content.putDownVerb = MakeTask("Put down", 1f);
        }

        private Simulation Begin(int floor = 3)
        {
            var sim = new Simulation(MakeLoopSettings(pockets: 2, floor: floor), TicksPerSecond, _content);
            _skipped = RecordRefusals(sim);
            sim.BeginLoop();
            return sim;
        }

        /// <summary>Offers an action in the hall.</summary>
        private void Here(TaskDefinition task)
        {
            _hall.tasks.Add(task);
            _content.tasks.Add(task);
        }

        /// <summary>Both pockets full of wisps, nothing on the floor.</summary>
        private static void FillPockets(Simulation sim, TaskDefinition gather)
        {
            sim.Schedule(gather, 2);
            RunSeconds(sim, 2f);
        }

        [Test]
        public void TheRing_PulledFromItsMirror_WithFullPockets_PushesSomethingOut()
        {
            var sim = Begin();
            FillPockets(sim, _gather);

            sim.Schedule(_takeRing);
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty);
            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "the ring, in her pockets");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "a wisp made way");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1), "put down here");
        }

        [Test]
        public void SomethingMadeFromAMirror_WithFullPockets_PushesSomethingOut()
        {
            var candle = MakeObject("Candle", max: 10);
            var makeCandle = MakeGatherTask("Instantiate a candle", 1f, candle);
            makeCandle.kind = TaskKind.Instantiate;
            Here(makeCandle);
            var sim = Begin();
            FillPockets(sim, _gather);

            sim.Schedule(makeCandle, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(candle), Is.EqualTo(1), "in her pockets, not on the floor");
            Assert.That(sim.OnFloor(_hall, candle), Is.EqualTo(0));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1), "a wisp made way");
        }

        [Test]
        public void SomethingMade_PushesOut_EvenWhenTheFloorHasNoRoomForIt()
        {
            var candle = MakeObject("Candle", max: 10);
            var makeCandle = MakeGatherTask("Instantiate a candle", 1f, candle);
            makeCandle.kind = TaskKind.Instantiate;
            Here(makeCandle);
            var sim = Begin();
            FillPockets(sim, _gather);
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { candle, 3 } }; // the floor holds 3 of each

            sim.PlayNow(makeCandle, 1);
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty);
            Assert.That(sim.AmountOf(candle), Is.EqualTo(1));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1));
        }

        [Test]
        public void Crafting_UsesUpWhatItTakesFirst_SoThatPocketIsFreeForWhatItMakes()
        {
            var wood = MakeObject("Wood", max: 10);
            var torch = MakeObject("Torch", max: 10);
            var bind = MakeGatherTask("Bind a torch", 1f, torch);
            bind.needs.Add(new ResourceAmount { resource = wood, amount = 1 });
            bind.takes.Add(new ResourceAmount { resource = wood, amount = 1 });
            Here(bind);
            var sim = Begin();
            sim.Loop.ToolsAndStats[wood] = 1;
            sim.Loop.ToolsAndStats[_wisp] = 1; // both pockets full

            sim.Schedule(bind, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(torch), Is.EqualTo(1));
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "nothing needed pushing out");
            Assert.That(sim.FloorAt(_hall), Is.Empty);
        }

        [Test]
        public void Gathering_WithFullPockets_StillGoesOnTheFloor()
        {
            var candle = MakeObject("Candle", max: 10);
            var sim = Begin();
            sim.Loop.ToolsAndStats[candle] = 2; // both pockets full

            sim.Schedule(_gather, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(candle), Is.EqualTo(2), "gathering pushes nothing out");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1));
        }

        [Test]
        public void PickingUpAPile_WithFullPockets_PushesOutAsManyAsItTakes()
        {
            var candle = MakeObject("Candle", max: 10);
            var sim = Begin();
            sim.Loop.ToolsAndStats[candle] = 2; // both pockets full
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _wisp, 2 } };

            sim.Schedule(sim.PickUpActionFor(_wisp));
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty);
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "the whole pile");
            Assert.That(sim.AmountOf(candle), Is.EqualTo(0));
            Assert.That(sim.OnFloor(_hall, candle), Is.EqualTo(2), "the candles, put down here");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(0));
        }

        [Test]
        public void PickingUp_PushesOutOnlyWhatMakesRoom_UpToTheItemsOwnMaximum()
        {
            _wisp.startingMax = 2;
            var candle = MakeObject("Candle", max: 10);
            var stone = MakeObject("Stone", max: 10);
            var sim = new Simulation(MakeLoopSettings(pockets: 3, floor: 3), TicksPerSecond, _content);
            sim.BeginLoop();
            sim.Loop.ToolsAndStats[_wisp] = 1;
            sim.Loop.ToolsAndStats[candle] = 1;
            sim.Loop.ToolsAndStats[stone] = 1; // all three pockets full
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _wisp, 3 } };

            sim.Schedule(sim.PickUpActionFor(_wisp));
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "her maximum");
            Assert.That(sim.AmountOf(candle) + sim.AmountOf(stone), Is.EqualTo(1), "only one made way");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(2));
        }

        [Test]
        public void PickingUpAOneOfAKindObject_WithFullPockets_PushesSomethingElseOntoTheFloor()
        {
            var sim = Begin();
            sim.Schedule(_gather, 2); // both pockets full of wisps, nothing on the floor
            RunSeconds(sim, 2f);
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _ring, 1 } };

            sim.Schedule(sim.PickUpActionFor(_ring));
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "the ring, in her pockets");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "a wisp made way");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1), "put down here");
            Assert.That(sim.OnFloor(_hall, _ring), Is.EqualTo(0));
        }

        [Test]
        public void PickingUpWisps_WithFullPockets_PushesNothingOut()
        {
            var sim = Begin();
            sim.Loop.ToolsAndStats[_ring] = 1;
            sim.Schedule(_gather, 2); // a wisp and the ring fill both pockets; the next wisp goes on the floor
            RunSeconds(sim, 2f);
            _skipped.Clear();

            sim.PlayNow(sim.PickUpActionFor(_wisp));

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "the ring stays in her pockets");
            Assert.That(_skipped, Is.EqualTo(new[] { $"Pick up all wisps: {Reason("no_room", ("item", "Wisp"))}" }));
        }

        [Test]
        public void AnActionNeedingTheRing_WithFullPockets_HasItSupplied()
        {
            var door = MakeTask("Open the door", 1f);
            door.needs.Add(new ResourceAmount { resource = _ring, amount = 1 });
            Here(door);
            var sim = Begin();
            FillPockets(sim, _gather);

            sim.Schedule(door, 1);
            RunSeconds(sim, 3f);

            Assert.That(_skipped, Is.Empty);
            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "pulled from its mirror first, a wisp making way");
            Assert.That(sim.Loop.CompletedTasks, Has.Member(door));
        }

        [Test]
        public void CarriedItems_CanBePushedOut_AndAreLeftBehindWithTheFloor()
        {
            var lantern = MakeObject("Lantern", max: 5, lasts: ResourceLifetime.Carried);
            var candle = MakeObject("Candle", max: 10);
            var makeCandle = MakeGatherTask("Instantiate a candle", 1f, candle);
            makeCandle.kind = TaskKind.Instantiate;
            Here(makeCandle);
            var sim = Begin();
            sim.Loop.ToolsAndStats[lantern] = 2; // brought from the lab: both pockets

            sim.Schedule(makeCandle, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(candle), Is.EqualTo(1), "in her pockets");
            Assert.That(sim.OnFloor(_hall, lantern), Is.EqualTo(1), "a lantern made way");

            sim.EndRunEarly();
            Assert.That(sim.Loop.CarriedLost, Has.Member((lantern, 1)), "back to the mirror realm: to be found again");
        }

        [Test]
        public void TheRing_LandsOnTheFloor_WhenOnlyOneOfAKindThingsFillHerPockets()
        {
            var tome = MakeObject("The tome", max: 1);
            var flint = MakeObject("Flint and steel", max: 1);
            var sim = Begin();
            sim.Loop.ToolsAndStats[tome] = 1;
            sim.Loop.ToolsAndStats[flint] = 1;

            sim.PlayNow(_takeRing);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(0), "not in her pockets: nothing one of a kind was pushed out");
            Assert.That(sim.OnFloor(_hall, _ring), Is.EqualTo(1), "so it waits here instead, rather than never arriving at all");
            Assert.That(sim.AmountOf(tome) + sim.AmountOf(flint), Is.EqualTo(2), "nothing one of a kind is pushed out");
            Assert.That(_skipped, Is.Empty, "it still completes");
            Assert.That(sim.Loop.CompletedTasks, Has.Member(_takeRing));
        }

        [Test]
        public void AOneOfAKindThing_StillLands_EvenWhenTheFloorHasNoRoomAtAll()
        {
            var tome = MakeObject("The tome", max: 1);
            var flint = MakeObject("Flint and steel", max: 1);
            var sim = Begin(floor: 0);
            sim.Loop.ToolsAndStats[tome] = 1;
            sim.Loop.ToolsAndStats[flint] = 1;

            sim.PlayNow(_takeRing);
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty, "a one-time thing is never simply lost");
            Assert.That(sim.OnFloor(_hall, _ring), Is.EqualTo(1), "put down here anyway: it has to land somewhere");
            Assert.That(sim.Loop.CompletedTasks, Has.Member(_takeRing));
        }

        [Test]
        public void TheRing_PulledFromItsMirror_WithPocketsAndTheFloorBothFullOfWisps_DestroysAWispToMakeWay()
        {
            var sim = Begin();
            FillPockets(sim, _gather); // both pockets full of wisps
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _wisp, 3 } }; // the floor's wisp space is full too

            sim.Schedule(_takeRing);
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty, "never refused: a wisp is lost to make way instead");
            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "the ring, in her pockets");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "a wisp made way");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(3), "one lost to make room, the pushed-out one taking its place");
        }

        [Test]
        public void PickingUpAOneOfAKindThing_WithPocketsAndTheFloorBothFullOfWisps_DestroysAWispToMakeWay()
        {
            var sim = Begin();
            FillPockets(sim, _gather); // both pockets full of wisps
            sim.Loop.Floor[_hall] = new Dictionary<ResourceDefinition, int> { { _wisp, 3 }, { _ring, 1 } }; // the wisp floor space is full too

            sim.Schedule(sim.PickUpActionFor(_ring));
            RunSeconds(sim, 1f);

            Assert.That(_skipped, Is.Empty, "never refused: a wisp is lost to make way instead");
            Assert.That(sim.AmountOf(_ring), Is.EqualTo(1), "the ring, in her pockets");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(1), "a wisp made way");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(3), "one lost to make room, the pushed-out one taking its place");
            Assert.That(sim.OnFloor(_hall, _ring), Is.EqualTo(0));
        }

        [Test]
        public void AOneOfAKindThing_WithNoRoomsAndFullPockets_IsRefused_NotLostOrCrashing()
        {
            var tome = MakeObject("The tome", max: 1);
            var flint = MakeObject("Flint and steel", max: 1);
            var sim = new Simulation(MakeLoopSettings(pockets: 2, floor: 3), TicksPerSecond); // no rooms: no floor
            sim.ActionRefused += (task, _, reason) => _skipped.Add($"{task.displayName}: {reason}");
            sim.BeginLoop();
            sim.Loop.ToolsAndStats[tome] = 1;
            sim.Loop.ToolsAndStats[flint] = 1;

            sim.PlayNow(_takeRing);

            Assert.That(sim.AmountOf(_ring), Is.EqualTo(0));
            Assert.That(_skipped, Has.Count.EqualTo(1), "refused up front: there's nowhere for it to go");
        }
    }
}
