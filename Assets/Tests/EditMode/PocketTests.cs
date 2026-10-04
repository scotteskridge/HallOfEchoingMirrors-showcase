using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Pockets: pocketed items (wisps, bottled wells) share a number of slots.</summary>
    public class PocketTests : SimulationTestBase
    {
        private ResourceDefinition _wisp, _well;
        private TaskDefinition _gatherWisp, _fillWell;

        [SetUp]
        public void SetUp()
        {
            _wisp = MakeObject("Wisp", max: 10);
            _well = MakeObject("Bottled well", max: 10);
            _gatherWisp = MakeGatherTask("Gather a wisp", 1f, _wisp);
            _fillWell = MakeGatherTask("Fill a bottled well", 1f, _well);
        }

        private Simulation Begin(int pocketSlots)
        {
            var sim = new Simulation(MakeLoopSettings(pockets: pocketSlots), TicksPerSecond);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void KeptContainer_NotCountedAsPocket()
        {
            var denseWisp = MakeObject("Dense wisp");
            denseWisp.onlyInContainers = true;
            var earrings = MakeResource("Earrings", ResourceLifetime.Forever, max: 1);
            earrings.goesInPocket = true; // ticked, but a kept thing never takes a pocket
            earrings.holds.Add(denseWisp);
            earrings.holdsHowMany = 2;
            var sim = Begin(pocketSlots: 2);
            sim.Persistent.Resources[earrings] = 1;
            sim.Loop.ToolsAndStats[denseWisp] = 2;
            sim.Schedule(_gatherWisp);

            RunSeconds(sim, 5f);

            Assert.That(sim.PocketSlots, Is.EqualTo(2));
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "both pockets still free for wisps");
            Assert.That(sim.PocketsUsed, Is.EqualTo(2), "the wisps; not the earrings or what's in them");
        }

        [Test]
        public void Gathering_StopsWhenPocketsAreFull()
        {
            var sim = Begin(pocketSlots: 5);
            sim.Schedule(_gatherWisp);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(5), "its own maximum is 10, but she has 5 pockets");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "done once there's no room");
        }

        // Characterisation (refactor: one "real gives" check): blank and zero-amount gives are ignored.
        [Test]
        public void Gathering_IgnoresBlankAndZeroGives_AndStillStopsWhenPocketsAreFull()
        {
            _gatherWisp.gives.Insert(0, new ResourceAmount { resource = null, amount = 1 });
            // Not pocketed and no maximum: if the zero give were counted, it would always have room
            // and the gathering would never be done.
            var resolve = MakeResource("Resolve");
            _gatherWisp.gives.Add(new ResourceAmount { resource = resolve, amount = 0 });
            var sim = Begin(pocketSlots: 5);
            sim.Schedule(_gatherWisp);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(5));
            Assert.That(sim.AmountOf(resolve), Is.EqualTo(0), "a zero give gives nothing");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "done once there's no room for the real give");
        }

        [Test]
        public void DifferentPocketedItems_ShareTheSlots()
        {
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[_wisp] = 3;
            sim.Schedule(_fillWell);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_well), Is.EqualTo(2));
            Assert.That(sim.PocketsUsed, Is.EqualTo(5));
        }

        [Test]
        public void ASatchel_AddsPockets_AndTakesNone()
        {
            var satchel = MakeObject("Satchel", max: 1);
            satchel.addsPockets = 5; // (so it takes no pocket itself)
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[satchel] = 1;
            sim.Schedule(_gatherWisp);

            RunSeconds(sim, 15f);

            Assert.That(sim.PocketSlots, Is.EqualTo(10));
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(10), "5 pockets + 5 from the satchel, which takes none");
        }

        [Test]
        public void NewItems_AreObjects_AndARunStartsWith5Pockets_And10FloorSpace()
        {
            Assert.That(Make<ResourceDefinition>().goesInPocket, Is.True);
            Assert.That(Make<LoopSettings>().pocketSlots, Is.EqualTo(5));
            Assert.That(Make<LoopSettings>().floorSpace, Is.EqualTo(10));
        }

        [Test]
        public void ItemsNotInPockets_TakeNoSpace()
        {
            var understanding = MakeResource("Understanding", max: 3); // knowledge, not an object
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[understanding] = 3;
            sim.Schedule(_gatherWisp);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(5));
        }

        [Test]
        public void UsingAWisp_FreesItsPocket()
        {
            _wisp.restoreVitality = 5f;
            var sim = new Simulation(MakeLoopSettings(pockets: 5), TicksPerSecond);
            sim.Schedule(MakeTiringTask("Strain", 1f, vitalityCost: 5f), 1);
            sim.BeginLoop();
            sim.Loop.ToolsAndStats[_wisp] = 5;
            Assert.That(sim.RoomFor(_wisp), Is.EqualTo(0));

            RunSeconds(sim, 1f); // the strain costs 5, so one wisp starts

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(4));
            Assert.That(sim.RoomFor(_wisp), Is.EqualTo(1));
        }

        // ---------- What she carries (the pockets overlay and the Pockets tab) ----------

        [Test]
        public void CarriedItems_ListsPocketedItemsWithAmounts()
        {
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[_wisp] = 3;
            sim.Loop.ToolsAndStats[_well] = 1;
            var carried = new List<CarriedEntry>();

            sim.CarriedItems(carried);

            Assert.That(carried.Count, Is.EqualTo(2));
            Assert.That(carried[0].Item, Is.EqualTo(_wisp));
            Assert.That(carried[0].Amount, Is.EqualTo(3));
            Assert.That(carried[0].Container, Is.Null);
            Assert.That(carried[1].Item, Is.EqualTo(_well));
            Assert.That(carried[1].Amount, Is.EqualTo(1));
        }

        [Test]
        public void CarriedItems_PouchContentsNamedWithContainer()
        {
            var phial = MakeObject("Phial of memory", max: 20);
            var pouch = MakeResource("Pouch of phials", max: 1);
            pouch.holds.Add(phial);
            pouch.holdsHowMany = 3;
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[pouch] = 1;
            sim.Loop.ToolsAndStats[phial] = 4; // 3 in the pouch, 1 in her pockets
            var carried = new List<CarriedEntry>();

            sim.CarriedItems(carried);

            Assert.That(carried.Count, Is.EqualTo(3));
            Assert.That(carried[0].Item, Is.EqualTo(phial), "the one in her pockets");
            Assert.That(carried[0].Amount, Is.EqualTo(1));
            Assert.That(carried[0].Container, Is.Null);
            Assert.That(carried[1].Item, Is.EqualTo(pouch), "the pouch itself, before what's in it");
            Assert.That(carried[1].Container, Is.Null);
            Assert.That(carried[2].Item, Is.EqualTo(phial));
            Assert.That(carried[2].Amount, Is.EqualTo(3));
            Assert.That(carried[2].Container, Is.EqualTo(pouch));
        }

        [Test]
        public void CarriedItems_SkipsKnowledgeAndFloor()
        {
            var understanding = MakeResource("Understanding", max: 3);
            var room = MakeNode("Hall");
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[understanding] = 2;
            sim.Loop.ToolsAndStats[_wisp] = 1;
            sim.Loop.Floor[room] = new Dictionary<ResourceDefinition, int> { { _well, 4 } };
            var carried = new List<CarriedEntry>();

            sim.CarriedItems(carried);

            Assert.That(carried.Count, Is.EqualTo(1));
            Assert.That(carried[0].Item, Is.EqualTo(_wisp));
        }

        [Test]
        public void CarriedItems_ListsTheSatchel_AfterPocketedItems_TakingNoPocket()
        {
            var satchel = MakeObject("Satchel", max: 1);
            satchel.addsPockets = 7;
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[satchel] = 1;
            sim.Loop.ToolsAndStats[_wisp] = 2;
            var carried = new List<CarriedEntry>();

            sim.CarriedItems(carried);

            Assert.That(carried.Count, Is.EqualTo(2));
            Assert.That(carried[0].Item, Is.EqualTo(_wisp));
            Assert.That(carried[1].Item, Is.EqualTo(satchel), "an object she carries outside her pockets");
            Assert.That(carried[1].Amount, Is.EqualTo(1));
            Assert.That(carried[1].Container, Is.Null);
            Assert.That(carried[1].IsContainer, Is.False);
            Assert.That(sim.PocketsUsed, Is.EqualTo(2), "the satchel still takes no pocket");
        }

        [Test]
        public void KnowledgeItems_ListsWhatSheDoesntCarry_ThisRunsThenKept()
        {
            var understanding = MakeResource("Understanding", max: 3);
            var mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            var satchel = MakeObject("Satchel", max: 1);
            satchel.addsPockets = 7;
            var sim = Begin(pocketSlots: 5);
            sim.Loop.ToolsAndStats[understanding] = 2;
            sim.Loop.ToolsAndStats[_wisp] = 1;
            sim.Loop.ToolsAndStats[satchel] = 1;
            sim.Persistent.Resources[mirrors] = 3;
            var known = new List<(ResourceDefinition item, int amount)> { (_well, 9) }; // cleared first

            sim.KnowledgeItems(known);

            Assert.That(known, Is.EqualTo(new List<(ResourceDefinition, int)> { (understanding, 2), (mirrors, 3) }),
                "not the wisp (pocketed) nor the satchel (CarriedItems lists both)");
        }

        [Test]
        public void KnowledgeItems_ListOneInUseWithNoneLeft_Once()
        {
            var warmth = MakeResource("Warmth"); // not an object, so it's knowledge
            var sim = Begin(pocketSlots: 5);
            sim.Loop.Restorings.Add(new LoopState.Restoring { Item = warmth, PerSecond = 1f, Left = 5f });
            sim.Loop.Restorings.Add(new LoopState.Restoring { Item = warmth, PerSecond = 1f, Left = 3f });
            var known = new List<(ResourceDefinition item, int amount)>();

            sim.KnowledgeItems(known);

            Assert.That(known, Is.EqualTo(new List<(ResourceDefinition, int)> { (warmth, 0) }), "its timer still shows, once");
        }

        [Test]
        public void CarriedItems_EmptyAtRunStart()
        {
            var sim = Begin(pocketSlots: 5);
            var carried = new List<CarriedEntry> { new CarriedEntry(_wisp, 9, null) }; // cleared first

            sim.CarriedItems(carried);

            Assert.That(carried, Is.Empty);
            Assert.That(sim.PocketsUsed, Is.EqualTo(0));
            Assert.That(sim.PocketSlots, Is.EqualTo(5), "the heading still reads 0/5");
        }

        [Test]
        public void KeptItems_NeverGoInPockets()
        {
            var mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);
            mirrors.goesInPocket = true;

            Assert.That(mirrors.IsPocketed, Is.False);
        }
    }
}
