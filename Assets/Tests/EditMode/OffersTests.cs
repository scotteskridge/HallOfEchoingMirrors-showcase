using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// What a room's popover lists: the actions there, greyed with a reason when they can't be done,
    /// without giving away what's still to be found or unlocked (decisions log 2026-09-28).
    /// </summary>
    public class OffersTests : SimulationTestBase
    {
        private GameContent _content;
        private NodeDefinition _hall, _corridor;
        private TaskDefinition _explore, _gather, _candleWork;
        private ResourceDefinition _wisp, _candle;

        // The hall (2 searches to fill) has Gather a wisp; the corridor next door has Work by candlelight,
        // which needs a candle.
        [SetUp]
        public void SetUp()
        {
            _wisp = MakeObject("Wisp", max: 10);
            _candle = MakeObject("Candle", max: 10);
            _gather = MakeGatherTask("Gather a wisp", 1f, _wisp);
            _candleWork = MakeTask("Work by candlelight", 1f);
            _candleWork.needs.Add(new ResourceAmount { resource = _candle, amount = 1 });
            _explore = MakeTask("Search", 1f, ClaraAttribute.Perception);

            _hall = MakeNode("The hall");
            _hall.exploresToFill = 2;
            _corridor = MakeNode("The corridor");
            Join(_hall, _corridor);
            _hall.tasks.Add(_gather);
            _corridor.tasks.Add(_candleWork);
            _content = MakePlaces(_hall, _corridor);
            _content.exploreVerb = _explore;
            _content.tasks.Add(_gather);
            _content.tasks.Add(_candleWork);
            _content.tasks.Add(_explore);
        }

        private Simulation Begin()
        {
            var sim = new Simulation(MakeLoopSettings(pockets: 5, floor: 5), TicksPerSecond, _content);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void Available_HasNoReason()
        {
            var sim = Begin();

            var offer = OfferOf(sim, _hall, _gather);

            Assert.That(offer, Is.Not.Null);
            Assert.That(offer.Value.Reason, Is.Null);
            Assert.That(offer.Value.Blocked, Is.False);
        }

        [Test]
        public void DoneThisRun_GreyedWithReason()
        {
            _gather.oncePerRun = true;
            var sim = Begin();
            sim.Schedule(_gather);
            RunSeconds(sim, 2f);

            var offer = OfferOf(sim, _hall, _gather);

            Assert.That(offer, Is.Not.Null, "still listed, greyed");
            Assert.That(offer.Value.Blocked, Is.True);
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("done_this_run")));
        }

        [Test]
        public void OneOfAKindHeld_GreyedWithReason()
        {
            var ring = MakeObject("Roland's ring", max: 1);
            var take = MakeGatherTask("Take the ring", 1f, ring);
            _hall.tasks.Add(take);
            _content.tasks.Add(take);
            var sim = Begin();
            sim.Schedule(take);
            RunSeconds(sim, 2f);

            var offer = OfferOf(sim, _hall, take);

            Assert.That(offer, Is.Not.Null, "still listed, greyed");
            Assert.That(offer.Value.Blocked, Is.True);
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("already_have")));
        }

        [Test]
        public void MissingNeed_ShowsReason_ButCanStillBeQueued()
        {
            // She may pick one up on the way, or something may supply it: the queue decides when it comes up.
            var sim = Begin();

            var offer = OfferOf(sim, _corridor, _candleWork);

            Assert.That(offer, Is.Not.Null);
            Assert.That(offer.Value.Blocked, Is.False);
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("needs", ("amount", 1), ("item", "Candle"))));
        }

        [Test]
        public void MissingNeed_CountsThatRoomsFloor()
        {
            var sim = Begin();
            sim.Loop.Floor[_corridor] = new Dictionary<ResourceDefinition, int> { [_candle] = 1 };

            var offer = OfferOf(sim, _corridor, _candleWork);

            Assert.That(offer.Value.Reason, Is.Null, "a candle lies on the corridor's floor");
        }

        [Test]
        public void AnywhereTask_ListedOnlyWhereItsNeedLiesOnTheFloor()
        {
            // Listed at no room, so it can be done anywhere: shown only where she'd have what it needs.
            var melt = MakeTask("Melt a candle", 1f);
            melt.needs.Add(new ResourceAmount { resource = _candle, amount = 1 });
            _content.tasks.Add(melt);
            var sim = Begin(); // she's in the hall, with no candle
            sim.Loop.Floor[_corridor] = new Dictionary<ResourceDefinition, int> { [_candle] = 1 };

            Assert.That(OfferOf(sim, _corridor, melt), Is.Not.Null, "a candle lies on the corridor's floor");
            Assert.That(OfferOf(sim, _hall, melt), Is.Null, "no candle in the hall");
        }

        [Test]
        public void FullySearched_GreyedWithReason()
        {
            var sim = Begin();
            sim.Persistent.Explored[_hall] = 2f;

            var offer = OfferOf(sim, _hall, _explore);

            Assert.That(offer, Is.Not.Null, "still listed, greyed");
            Assert.That(offer.Value.Blocked, Is.True);
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("fully_explored", ("room", "the hall"))));
        }

        [Test]
        public void NotFound_Hidden()
        {
            var seam = MakeTask("Work the seam", 1f);
            _hall.foundBySearching.Add(new RoomFind { task = seam, atSearched = 50 });
            _content.tasks.Add(seam);
            var sim = Begin();

            Assert.That(OfferOf(sim, _hall, seam), Is.Null);
        }

        [Test]
        public void Locked_Hidden()
        {
            var secret = MakeTask("A secret", 1f, startsUnlocked: false);
            _hall.tasks.Add(secret);
            _content.tasks.Add(secret);
            var sim = Begin();

            Assert.That(OfferOf(sim, _hall, secret), Is.Null);
        }

        [Test]
        public void KeptRewardOwned_Hidden()
        {
            var charm = MakeResource("A kept charm", ResourceLifetime.Forever, max: 1);
            var earn = MakeGatherTask("Earn the charm", 1f, charm);
            _hall.tasks.Add(earn);
            _content.tasks.Add(earn);
            var sim = Begin();
            sim.Schedule(earn);
            RunSeconds(sim, 2f);

            Assert.That(OfferOf(sim, _hall, earn), Is.Null, "a one-time reward, once earned, is never offered again");
        }

        [Test]
        public void OtherRoom_ListsThatRoomsActions()
        {
            var sim = Begin(); // she's in the hall

            Assert.That(OfferOf(sim, _corridor, _candleWork), Is.Not.Null);
            Assert.That(OfferOf(sim, _corridor, _gather), Is.Null, "the hall's action isn't the corridor's");
        }

        [Test]
        public void TasksAt_ListsExactlyTheUnblockedOffers()
        {
            _gather.oncePerRun = true;
            var sim = Begin();
            sim.Schedule(_gather);
            RunSeconds(sim, 2f);

            var offers = new List<ActionOffer>();
            sim.OffersAt(_hall, offers);
            var unblocked = offers.FindAll(o => !o.Blocked).ConvertAll(o => o.Task);

            CollectionAssert.AreEqual(unblocked, sim.TasksAt(_hall));
        }
    }
}
