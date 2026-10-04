using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Containers (a pouch of phials): items carried outside her pockets, up to a number.</summary>
    public class ContainerTests : SimulationTestBase
    {
        private ResourceDefinition _pouch, _phial, _wisp;
        private TaskDefinition _fill, _gather;

        // 2 pockets. A pouch holds up to 3 phials. Filling a phial and gathering a wisp take 1s each.
        [SetUp]
        public void SetUp()
        {
            _phial = MakeObject("Phial of memory", max: 20);
            _wisp = MakeObject("Wisp", max: 20);
            _pouch = MakeResource("Pouch of phials", max: 1);
            _pouch.holds.Add(_phial);
            _pouch.holdsHowMany = 3;
            _fill = MakeGatherTask("Fill a phial", 1f, _phial);
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);
        }

        private Simulation Begin(bool withPouch = true)
        {
            var sim = new Simulation(MakeLoopSettings(pockets: 2), TicksPerSecond);
            sim.BeginLoop();
            if (withPouch)
                sim.Loop.ToolsAndStats[_pouch] = 1;
            return sim;
        }

        [Test]
        public void APouch_HoldsItsItemsFirst_ThenHerPocketsTakeTheRest()
        {
            var sim = Begin();
            sim.Schedule(_fill);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_phial), Is.EqualTo(5), "3 in the pouch + 2 in her pockets");
            Assert.That(sim.InContainers(_phial), Is.EqualTo(3));
            Assert.That(sim.PocketsUsed, Is.EqualTo(2));
            Assert.That(sim.ContentsOf(_pouch).contents, Is.EqualTo(new[] { (_phial, 3) }));
            Assert.That(sim.ContentsOf(_pouch).room, Is.EqualTo(0));
        }

        [Test]
        public void ThePouch_TakesNoPocket_AndHoldsNothingElse()
        {
            var sim = Begin();
            sim.Schedule(_gather);

            RunSeconds(sim, 10f);

            Assert.That(_pouch.IsPocketed, Is.False);
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "wisps don't go in a pouch of phials");
            Assert.That(sim.InContainers(_wisp), Is.EqualTo(0));
        }

        [Test]
        public void WithoutThePouch_PhialsOnlyHaveHerPockets()
        {
            var sim = Begin(withPouch: false);
            sim.Schedule(_fill);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_phial), Is.EqualTo(2));
        }

        [Test]
        public void UsingAPhial_FreesHerPocketsFirst_ThePouchStaysFull()
        {
            _phial.restoreVitality = 5f;
            var sim = Begin();
            sim.Loop.ToolsAndStats[_phial] = 5; // 3 in the pouch, 2 in her pockets
            sim.Loop.Vitality.Drain(5f);        // missing one phial's worth
            KeepBusy(sim, 1f);

            RunTicks(sim, 1);

            Assert.That(sim.AmountOf(_phial), Is.EqualTo(4));
            Assert.That(sim.InContainers(_phial), Is.EqualTo(3), "the pouch is still full");
            Assert.That(sim.PocketsUsed, Is.EqualTo(1), "a pocket is free again");
        }

        [Test]
        public void ContainerFor_IsThePouchForPhials_AndNothingForOtherItemsOrWithoutIt()
        {
            var sim = Begin();
            Assert.That(sim.ContainerFor(_phial), Is.EqualTo(_pouch));
            Assert.That(sim.ContainerFor(_wisp), Is.Null, "the pouch doesn't take wisps");

            var without = Begin(withPouch: false);
            Assert.That(without.ContainerFor(_phial), Is.Null, "she doesn't carry a pouch");
        }

        [Test]
        public void ContainerCapacity_IsItsSizeTimesHowManySheHas_AndUsedIsWhatsInIt()
        {
            var sim = Begin();
            Assert.That(sim.ContainerCapacity(_pouch), Is.EqualTo(3));
            Assert.That(sim.ContainerUsed(_pouch), Is.EqualTo(0));

            sim.Loop.ToolsAndStats[_phial] = 2;
            Assert.That(sim.ContainerUsed(_pouch), Is.EqualTo(2));

            sim.Loop.ToolsAndStats[_phial] = 5; // 3 in the pouch, 2 in her pockets
            Assert.That(sim.ContainerUsed(_pouch), Is.EqualTo(3));

            sim.Loop.ToolsAndStats[_pouch] = 2;
            Assert.That(sim.ContainerCapacity(_pouch), Is.EqualTo(6), "two pouches");
            Assert.That(sim.ContainerUsed(_pouch), Is.EqualTo(5));
        }

        // Characterisation (refactor: ContentsOf reads what FillContainers worked out): pins the fill order.
        [Test]
        public void ContentsOf_ATwoKindPouch_FillsItsItemsInItsOwnOrder()
        {
            var flask = MakeObject("Flask", max: 20);
            _pouch.holds.Add(flask); // holds phials first, then flasks; room for 3
            var sim = Begin();

            sim.Loop.ToolsAndStats[_phial] = 2;
            sim.Loop.ToolsAndStats[flask] = 4;

            var (contents, room) = sim.ContentsOf(_pouch);
            Assert.That(contents, Is.EqualTo(new[] { (_phial, 2), (flask, 1) }));
            Assert.That(room, Is.EqualTo(0));
            Assert.That(sim.InContainers(flask), Is.EqualTo(1));
        }

        [Test]
        public void ContentsOf_TwoContainersOfOneKind_FillInTheOrderSheCameByThem()
        {
            var secondPouch = MakeResource("Second pouch", max: 1);
            secondPouch.holds.Add(_phial);
            secondPouch.holdsHowMany = 2;
            var sim = Begin();
            sim.Loop.ToolsAndStats[secondPouch] = 1; // came second

            sim.Loop.ToolsAndStats[_phial] = 4;

            // Each result is read before the next call, which refills the simulation's lists.
            var first = sim.ContentsOf(_pouch);
            Assert.That(first.contents, Is.EqualTo(new[] { (_phial, 3) }));
            Assert.That(first.room, Is.EqualTo(0));
            var second = sim.ContentsOf(secondPouch);
            Assert.That(second.contents, Is.EqualTo(new[] { (_phial, 1) }));
            Assert.That(second.room, Is.EqualTo(1));
            Assert.That(sim.InContainers(_phial), Is.EqualTo(4));
        }

        [Test]
        public void ContentsOf_AnEmptyPouch_IsEmptyWithAllItsRoom_AndAnUnheldOneHasNoRoom()
        {
            var sim = Begin();
            var held = sim.ContentsOf(_pouch);
            Assert.That(held.contents, Is.Empty);
            Assert.That(held.room, Is.EqualTo(3));

            var without = Begin(withPouch: false);
            Assert.That(without.ContentsOf(_pouch).contents, Is.Empty);
            Assert.That(without.ContentsOf(_pouch).room, Is.EqualTo(0));
            Assert.That(without.ContentsOf(null).room, Is.EqualTo(0));
        }

        // ---------- A kept container (the white sapphire earrings, plan 027b) ----------

        private ResourceDefinition _earrings, _denseWisp;
        private TaskDefinition _draw;

        // Kept earrings hold 2 dense wisps, which go nowhere else. Drawing one takes 1s.
        private void MakeEarrings()
        {
            _denseWisp = MakeObject("Dense wisp");
            _denseWisp.onlyInContainers = true;
            _earrings = MakeResource("White sapphire earrings", ResourceLifetime.Forever, max: 1);
            _earrings.holds.Add(_denseWisp);
            _earrings.holdsHowMany = 2;
            _draw = MakeGatherTask("Draw a dense wisp", 1f, _denseWisp);
        }

        private Simulation BeginWithEarrings(GameContent content = null)
        {
            MakeEarrings();
            var sim = new Simulation(MakeLoopSettings(pockets: 2), TicksPerSecond, content);
            sim.Persistent.Resources[_earrings] = 1;
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void KeptContainer_KeptAcrossRuns_EmptyEachRun()
        {
            var sim = BeginWithEarrings();
            sim.Schedule(_draw);
            RunSeconds(sim, 5f);
            Assert.That(sim.ContentsOf(_earrings).contents, Is.EqualTo(new[] { (_denseWisp, 2) }), "a kept container holds things");
            Assert.That(sim.ContainerFor(_denseWisp), Is.EqualTo(_earrings));

            sim.EndRunEarly();
            sim.BeginLoop();

            Assert.That(sim.AmountOf(_earrings), Is.EqualTo(1), "the earrings are kept");
            Assert.That(sim.AmountOf(_denseWisp), Is.EqualTo(0), "what's in them is this run's only");
            Assert.That(sim.ContentsOf(_earrings).room, Is.EqualTo(2));
        }

        [Test]
        public void KeptContainer_RespectsHoldsHowMany_AndItsOnlyItemsGoNowhereElse()
        {
            var sim = BeginWithEarrings(MakePlaces(MakeNode("The lab")));
            sim.Schedule(_draw);

            RunSeconds(sim, 6f);

            Assert.That(sim.AmountOf(_denseWisp), Is.EqualTo(2), "two, though her pockets are empty");
            Assert.That(sim.OnFloor(sim.Loop.CurrentNode, _denseWisp), Is.EqualTo(0), "none left on the floor");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "drawing stops once the earrings are full");
            Assert.That(sim.RoomFor(_denseWisp), Is.EqualTo(0));
        }

        [Test]
        public void MakingAnItemOnlyForContainers_PushesNothingOutOfHerPockets()
        {
            var sim = BeginWithEarrings(MakePlaces(MakeNode("The lab")));
            _draw.kind = TaskKind.Instantiate; // something she makes pushes things out of full pockets, but a pocket is no use here
            sim.Loop.ToolsAndStats[_denseWisp] = 2; // the earrings are full
            sim.Loop.ToolsAndStats[_phial] = 2;     // so are her pockets

            sim.Schedule(_draw, 1);
            RunSeconds(sim, 2f);

            Assert.That(sim.AmountOf(_phial), Is.EqualTo(2), "nothing put down to make room");
            Assert.That(sim.AmountOf(_denseWisp), Is.EqualTo(2));
        }

        [Test]
        public void AnItemOnlyForContainers_HasNoRoom_WithoutOne()
        {
            MakeEarrings();
            var sim = Begin(withPouch: false);

            Assert.That(sim.RoomFor(_denseWisp), Is.EqualTo(0), "her pockets don't take it");
            Assert.That(sim.ContentsOf(_earrings).room, Is.EqualTo(0), "she doesn't have the earrings");
        }

        [Test]
        public void CarriedItems_ListsAKeptContainer_AndWhatsInIt()
        {
            var sim = BeginWithEarrings();
            sim.Loop.ToolsAndStats[_denseWisp] = 1;
            var carried = new System.Collections.Generic.List<CarriedEntry>();

            sim.CarriedItems(carried);

            Assert.That(carried.Count, Is.EqualTo(2));
            Assert.That(carried[0].Item, Is.EqualTo(_earrings));
            Assert.That(carried[0].IsContainer, Is.True);
            Assert.That(carried[1].Item, Is.EqualTo(_denseWisp));
            Assert.That(carried[1].Container, Is.EqualTo(_earrings));
        }

        [Test]
        public void ContainerCapacity_IsNothing_WithoutTheContainer_AndNullIsAMistake()
        {
            var sim = Begin(withPouch: false);
            Assert.That(sim.ContainerCapacity(_pouch), Is.EqualTo(0));
            Assert.That(sim.ContainerUsed(_pouch), Is.EqualTo(0));
            Assert.Throws<System.ArgumentNullException>(() => sim.ContainerCapacity(null));
        }
    }
}
