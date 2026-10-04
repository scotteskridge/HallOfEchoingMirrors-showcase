using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The floor: what she can't hold is put down in the room, and picked up again.</summary>
    public class FloorTests : SimulationTestBase
    {
        private List<string> _skipped = new List<string>();
        private GameContent _content;
        private NodeDefinition _hall, _corridor;
        private ResourceDefinition _wisp;
        private TaskDefinition _gather, _pickUp;

        // 2 pockets, floors of 3. The hall has wisps to gather (1s each); the corridor is next door.
        [SetUp]
        public void SetUp()
        {
            _skipped.Clear();
            _wisp = MakeObject("Wisp", max: 10);
            _wisp.nameInActions = "all wisps";
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);

            _hall = MakeNode("The hall");
            _corridor = MakeNode("The corridor");
            Join(_hall, _corridor);
            _hall.tasks.Add(_gather);
            _content = MakePlaces(_hall, _corridor);
            _content.tasks.Add(_gather);
            _content.pickUpVerb = MakeTask("Pick up", 1f);
            _content.putDownVerb = MakeTask("Put down", 1f);
        }

        private Simulation Begin()
        {
            var sim = new Simulation(MakeLoopSettings(pockets: 2, floor: 3), TicksPerSecond, _content);
            _skipped = RecordRefusals(sim);
            sim.BeginLoop();
            _pickUp = sim.PickUpActionFor(_wisp);
            return sim;
        }

        /// <summary>Gathers until pockets (2) and the floor (3) are full.</summary>
        private static void FillPocketsAndFloor(Simulation sim, TaskDefinition gather)
        {
            sim.Schedule(gather);
            RunSeconds(sim, 10f);
        }

        [Test]
        public void WhatDoesntFitInHerPockets_GoesOnTheFloor_UntilThatsFullToo()
        {
            var sim = Begin();

            FillPocketsAndFloor(sim, _gather);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "pockets");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(3), "the floor");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "gathering stops once there's no room anywhere");
        }

        [Test]
        public void AnItemsOwnMaximum_OverflowsToTheFloor_Too()
        {
            _wisp.startingMax = 2; // she can hold only two, however many pockets she has
            var sim = new Simulation(MakeLoopSettings(pockets: 5, floor: 3), TicksPerSecond, _content);
            sim.BeginLoop();

            FillPocketsAndFloor(sim, _gather);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(3));
        }

        [Test]
        public void AOneOfAKindObject_IsNeverDuplicated_AndItsActionGoesOnceItExists()
        {
            var ring = MakeObject("Roland's ring", max: 1);
            var take = MakeGatherTask("Take the ring", 1f, ring);
            _hall.tasks.Add(take);
            _content.tasks.Add(take);
            var sim = Begin();

            sim.Schedule(take);
            RunSeconds(sim, 3f);

            Assert.That(sim.AmountOf(ring), Is.EqualTo(1));
            Assert.That(sim.OnFloor(_hall, ring), Is.EqualTo(0), "no second ring on the floor");
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "done once she has it");
            Assert.That(sim.TasksAt(_hall), Has.No.Member(take), "and no longer offered");

            sim.Schedule(sim.PutDownActionFor(ring));
            RunSeconds(sim, 1f);
            Assert.That(sim.OnFloor(_hall, ring), Is.EqualTo(1));
            Assert.That(sim.TasksAt(_hall), Has.No.Member(take), "still one ring, on the floor");

            sim.Schedule(sim.PickUpActionFor(ring));
            RunSeconds(sim, 1f);
            Assert.That(sim.AmountOf(ring), Is.EqualTo(1), "picked up again");
        }

        [Test]
        public void APickUpAction_IsOfferedOnlyWhereItsItemLies()
        {
            var sim = Begin();
            Assert.That(sim.TasksAt(_hall), Has.No.Member(_pickUp), "nothing on the floor yet");

            sim.Schedule(_gather, 3); // 2 into pockets, 1 onto the floor
            RunSeconds(sim, 3f);

            Assert.That(sim.TasksAt(_hall), Has.Member(_pickUp));
            Assert.That(sim.TasksAt(_corridor), Has.No.Member(_pickUp), "the pile is in the hall");
        }

        [Test]
        public void PickingUp_TakesAsManyAsFitInHerPockets_AndLeavesTheRest()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather); // 2 in pockets, 3 on the floor
            sim.Loop.ToolsAndStats[_wisp] = 0; // as if she'd used them

            sim.Schedule(_pickUp);
            RunSeconds(sim, 2f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(1));
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "one pick-up, not a repeat");
        }

        [Test]
        public void PickingUp_WithFullPockets_IsSkipped_WithAReason()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather);

            sim.Schedule(_pickUp);
            RunTicks(sim, 1);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Pick up all wisps: {Reason("no_room", ("item", "Wisp"))}" }));
        }

        [Test]
        public void FloorSpace_WithAnItemThatAddsSome_GrowsInSteps()
        {
            var lit = MakeResource("Candles lit", max: 25);
            lit.addsFloorSpace = 5;
            var sim = Begin();

            sim.Loop.ToolsAndStats[lit] = 25;
            Assert.That(sim.FloorSpace, Is.EqualTo(3 + 5), "all 25 lit: +5 in all");
            sim.Loop.ToolsAndStats[lit] = 12;
            Assert.That(sim.FloorSpace, Is.EqualTo(3 + 2), "in step, rounded down: +1 for every 5");
        }

        [Test]
        public void TheFloor_IsGone_NextRun()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather);
            sim.EndRunEarly();

            sim.BeginLoop();

            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(0));
        }

        [Test]
        public void ACarriedItemLeftOnTheFloor_IsLeftBehind_WhenTheRunEnds()
        {
            var lantern = MakeObject("Lantern", max: 10, lasts: ResourceLifetime.Carried);
            var fetch = MakeGatherTask("Fetch a lantern", 1f, lantern);
            _hall.tasks.Add(fetch);
            _content.tasks.Add(fetch);
            var sim = Begin();
            FillPocketsAndFloor(sim, fetch); // 2 carried, 3 on the floor

            sim.EndRunEarly();

            int lost = 0;
            foreach (var (item, amount) in sim.Loop.CarriedLost)
                if (item == lantern)
                    lost += amount;
            Assert.That(lost, Is.EqualTo(5), "the 2 she carried and the 3 she put down");
        }

        [Test]
        public void RestorationItems_AreUsedFromTheFloorFirst_ThenHerPockets()
        {
            _wisp.restoreVitality = 5f;
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather); // 2 in pockets, 3 on the floor
            sim.Loop.Vitality.Drain(5f); // missing 5: one wisp's worth
            KeepBusy(sim, 1f);

            RunTicks(sim, 1);

            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(2), "one from the floor");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "her pockets untouched");
        }

        [Test]
        public void WhatANeedsLyingOnTheFloorHere_Counts_AndIsUsedUpFirst()
        {
            var burn = MakeTask("Burn a wisp", 1f);
            burn.needs.Add(new ResourceAmount { resource = _wisp, amount = 1 });
            burn.takes.Add(new ResourceAmount { resource = _wisp, amount = 1 });
            _hall.tasks.Add(burn);
            _content.tasks.Add(burn);
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather); // 2 in pockets, 3 on the floor

            sim.Schedule(burn, 1);
            RunSeconds(sim, 1f);

            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(2), "from the floor");
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "her pockets untouched");
        }

        [Test]
        public void AnActionWithNoRoomForWhatItGives_IsRefusedAtOnce_AndNoTimePasses()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather);
            _skipped.Clear();
            long ticks = sim.Loop.TicksElapsed;

            sim.PlayNow(_gather);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Gather a wisp: {Reason("cant_hold_more", ("item", "Wisp"))}" }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "not queued");
            Assert.That(sim.Loop.TicksElapsed, Is.EqualTo(ticks), "and no time spent on it");
        }

        [Test]
        public void AnActionQueuedBehindOthers_IsAccepted_ItMayBePossibleByItsTurn()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather);
            _skipped.Clear();

            sim.ScheduleTrip(_corridor);
            sim.Schedule(_gather); // can't be done now, but might be after the trip

            Assert.That(sim.Queue.Count, Is.EqualTo(2));
            Assert.That(_skipped, Is.Empty);
        }

        [Test]
        public void AOnceARunAction_IsNoLongerOffered_OnceDone()
        {
            var flint = MakeObject("Flint and steel", max: 1);
            var make = MakeGatherTask("Make flint and steel", 1f, flint);
            make.oncePerRun = true;
            _hall.tasks.Add(make);
            _content.tasks.Add(make);
            var sim = Begin();
            Assert.That(sim.TasksAt(_hall), Has.Member(make));

            sim.Schedule(make);
            RunSeconds(sim, 1f);

            Assert.That(sim.TasksAt(_hall), Has.No.Member(make), "done this run: gone from the list");
            sim.EndRunEarly();
            sim.BeginLoop();
            Assert.That(sim.TasksAt(_hall), Has.Member(make), "back next run");
        }

        [Test]
        public void Carry_StopsWhenHerPocketsAreFull_LeavingNothingOnTheFloor()
        {
            var sim = Begin();
            sim.CarryNow(_gather);

            RunSeconds(sim, 10f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "pockets full");
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(0), "nothing gathered just to be put down");
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void Carry_WithFullPockets_IsRefused_ThoughTheFloorHasRoom()
        {
            var sim = Begin();
            sim.CarryNow(_gather);
            RunSeconds(sim, 5f);
            _skipped.Clear();

            sim.CarryNow(_gather);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Gather a wisp: {Reason("cant_carry_more", ("item", "Wisp"))}" }));
            Assert.That(sim.Queue.Count, Is.EqualTo(0));
        }

        [Test]
        public void Carry_IsOfferedForActionsThatMakeThingsSheCarries_ButNotOneTimeOnes()
        {
            var sim = Begin();
            Assert.That(sim.CanCarry(_gather), Is.True, "a wisp, whether or not it restores anything");

            var ring = MakeObject("Ring", max: 1);
            var makeRing = MakeGatherTask("Make the ring", 1f, ring);
            Assert.That(sim.CanCarry(makeRing), Is.False, "one of a kind");

            var once = MakeGatherTask("Make a candle once", 1f, MakeObject("Candle"));
            once.oncePerRun = true;
            Assert.That(sim.CanCarry(once), Is.False, "once a run");

            var knowledge = MakeGatherTask("Find a clue", 1f, MakeResource("Clue"));
            Assert.That(sim.CanCarry(knowledge), Is.False, "a plain count, nothing carried");

            var pouch = MakeResource("Pouch");
            pouch.addsPockets = 3;
            Assert.That(sim.CanCarry(MakeGatherTask("Make a pouch", 1f, pouch)), Is.True, "things that hold things count too");
        }

        [Test]
        public void Carry_IsNeverOfferedForAContainer_EvenWithoutAMaximumOfOne()
        {
            // Carry means "fill up on what I'm making"; each container adds room to fill (decisions log, 2026-10-01).
            var sim = Begin();
            var phials = MakeObject("Pouch of phials");
            phials.holds.Add(MakeObject("Phial"));
            phials.holdsHowMany = 3;
            Assert.That(phials.IsContainer, Is.True, "the test needs a real container");
            Assert.That(sim.ResourceCapOf(phials), Is.Not.EqualTo(1), "and one that isn't one of a kind");

            Assert.That(sim.CanCarry(MakeGatherTask("Sew a pouch of phials", 1f, phials)), Is.False);
        }

        [Test]
        public void Carry_GoesOnTop_LikePlay()
        {
            var other = MakeGatherTask("Hum a tune", 30f, MakeResource("Calm", max: 0));
            _hall.tasks.Add(other);
            _content.tasks.Add(other);
            var sim = Begin();
            sim.Schedule(other);

            sim.CarryNow(_gather);

            Assert.That(sim.Queue.Top.Task, Is.SameAs(_gather));
            Assert.That(sim.Queue.Top.CarryOnly, Is.True);
            Assert.That(sim.Queue.Count, Is.EqualTo(2), "what she was doing waits underneath");
        }

        [Test]
        public void PuttingDown_MovesWhatsInHerPockets_ToTheFloorHere_AndIsOfferedOnlyWhenSheHasSome()
        {
            var sim = Begin();
            var putDown = sim.PutDownActionFor(_wisp);
            Assert.That(sim.TasksAt(_hall), Has.No.Member(putDown), "nothing to put down yet");

            sim.CarryNow(_gather);
            RunSeconds(sim, 5f); // 2 in her pockets
            Assert.That(sim.TasksAt(_hall), Has.Member(putDown));

            sim.Schedule(putDown);
            RunSeconds(sim, 1f);

            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(2));
            Assert.That(sim.TasksAt(_hall), Has.No.Member(putDown), "none left to put down");
        }

        // Decisions log 2026-09-30: a way's needs count the floor of the room she's leaving, like an action's.
        [Test]
        public void AWaysNeeds_CountWhatLiesOnTheFloorWhereSheIs_AndAreNotSpentByTheTrip()
        {
            var sim = Begin();
            sim.CarryNow(_gather);
            RunSeconds(sim, 5f);
            sim.Schedule(sim.PutDownActionFor(_wisp));
            RunSeconds(sim, 1f);
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(0), "her pockets are empty");
            _hall.ways[0].needs.Add(new ResourceAmount { resource = _wisp, amount = 1 });

            sim.ScheduleTrip(_corridor);
            Assert.That(sim.Queue.Count, Is.EqualTo(1), "the trip is allowed: the wisps at her feet count");
            RunSeconds(sim, 2f);

            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_corridor));
            Assert.That(sim.OnFloor(_hall, _wisp), Is.EqualTo(2), "a need is checked, not spent");
        }

        [Test]
        public void AWaysNeeds_DontCountTheFloorOfAnotherRoom()
        {
            var sim = Begin();
            sim.CarryNow(_gather);
            RunSeconds(sim, 5f);
            sim.Schedule(sim.PutDownActionFor(_wisp));
            RunSeconds(sim, 1f);
            sim.ScheduleTrip(_corridor);
            RunSeconds(sim, 2f);
            Assert.That(sim.Loop.CurrentNode, Is.EqualTo(_corridor));
            _hall.ways[0].needs.Add(new ResourceAmount { resource = _wisp, amount = 1 });

            sim.ScheduleTrip(_hall);

            Assert.That(sim.Queue.Count, Is.EqualTo(0), "the wisps are in the hall, not here");
            Assert.That(_skipped, Has.Some.Contains(Reason("needs", ("amount", 1), ("item", "Wisp"))));
        }

        [Test]
        public void PickUpAndPutDown_AreMadeFromTheirVerbs_OncePerItem_AndNamedForIt()
        {
            var sim = Begin();

            Assert.That(_pickUp.displayName, Is.EqualTo("Pick up all wisps"));
            Assert.That(_pickUp.family, Is.SameAs(_content.pickUpVerb.family), "the verb's time");
            Assert.That(_pickUp.durationMultiplier, Is.EqualTo(_content.pickUpVerb.durationMultiplier), "the verb's time");
            Assert.That(sim.PickUpActionFor(_wisp), Is.SameAs(_pickUp), "the same action every time it's asked for");
            Assert.That(sim.PutDownActionFor(_wisp).displayName, Is.EqualTo("Put down all wisps"));
        }

        [Test]
        public void LockingThePickUpVerb_HidesEveryPickUp()
        {
            var sim = Begin();
            sim.Schedule(_gather, 3); // 2 into pockets, 1 onto the floor
            RunSeconds(sim, 3f);

            sim.Persistent.LockedTasks.Add(_content.pickUpVerb);

            Assert.That(sim.TasksAt(_hall), Has.No.Member(_pickUp));
        }

        [Test]
        public void KnowledgeAndStates_NeverGoOnTheFloor()
        {
            var understanding = MakeResource("Understanding", max: 3); // not an object
            var sim = Begin();

            Assert.That(sim.FloorRoomFor(understanding), Is.EqualTo(0));
        }

        [Test]
        public void PuttingDown_WithTheFloorFull_IsRefusedAsFloorFull()
        {
            var sim = Begin();
            FillPocketsAndFloor(sim, _gather);
            _skipped.Clear();

            sim.Schedule(sim.PutDownActionFor(_wisp));

            Assert.That(_skipped, Is.EqualTo(new[] { $"Put down all wisps: {Reason("floor_full", ("item", "Wisp"))}" }));
            Assert.That(sim.AmountOf(_wisp), Is.EqualTo(2), "nothing moved");
        }

        [Test]
        public void PuttingDown_WithNoneInHerPockets_IsRefusedAsNothingToPutDown()
        {
            var sim = Begin();

            sim.Schedule(sim.PutDownActionFor(_wisp));

            Assert.That(_skipped, Is.EqualTo(new[] { $"Put down all wisps: {Reason("nothing_to_put_down", ("item", "Wisp"))}" }));
        }

        [Test]
        public void PuttingDown_ADenseWispHeldInAContainer_IsRefused_ItNeverGoesOnTheFloor()
        {
            var dense = MakeObject("Dense wisp");
            dense.onlyInContainers = true;
            var earrings = MakeResource("White sapphire earrings", ResourceLifetime.Forever, max: 1);
            earrings.holds.Add(dense);
            earrings.holdsHowMany = 2;
            dense.nameInActions = "all dense wisps";
            var sim = new Simulation(MakeLoopSettings(pockets: 2, floor: 3), TicksPerSecond, _content);
            _skipped = RecordRefusals(sim);
            sim.Persistent.Resources[earrings] = 1;
            sim.BeginLoop();
            sim.Schedule(MakeGatherTask("Draw a dense wisp", 1f, dense), 1);
            RunSeconds(sim, 2f);
            Assert.That(sim.AmountOf(dense), Is.EqualTo(1), "set-up: she holds one, in the earrings");
            _skipped.Clear();

            sim.Schedule(sim.PutDownActionFor(dense));

            Assert.That(_skipped, Is.EqualTo(new[] { $"Put down all dense wisps: {Reason("nothing_to_put_down", ("item", "Dense wisp"))}" }),
                "none in her pockets: it lives in the container, and no floor takes it");
            Assert.That(sim.FloorRoomFor(dense), Is.EqualTo(0));
        }

        [Test]
        public void PickingUp_WithNothingOnTheFloor_IsRefusedAsNothingToPickUp()
        {
            var sim = Begin();

            sim.Schedule(_pickUp);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Pick up all wisps: {Reason("nothing_to_pick_up", ("item", "Wisp"))}" }));
        }
    }
}
