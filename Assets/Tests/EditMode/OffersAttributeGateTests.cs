using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>A room's offer greys out for a stat gate (Perception 5) exactly as for a skill gate: listed, with the reason, not blocked.</summary>
    public class OffersAttributeGateTests : SimulationTestBase
    {
        private NodeDefinition _hall;
        private TaskDefinition _sweep;
        private GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _sweep = MakeTask("Sweep for dust", 1f);
            _sweep.requiresAttributes.Add(new TaskDefinition.AttributeRequirement { attribute = ClaraAttribute.Perception, level = 5 });
            _hall = MakeNode("The hall");
            _hall.tasks.Add(_sweep);
            _content = MakePlaces(_hall);
            _content.tasks.Add(_sweep);
        }

        private ActionOffer? OfferOf(Simulation sim)
        {
            var offers = new List<ActionOffer>();
            sim.OffersAt(_hall, offers);
            foreach (var offer in offers)
                if (offer.Task == _sweep)
                    return offer;
            return null;
        }

        [Test]
        public void APerceptionGatedRoomTask_IsOfferedGreyedWithTheReason_WhilePerceptionIsShort()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();

            var offer = OfferOf(sim);

            Assert.That(offer, Is.Not.Null, "still listed");
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("needs_attribute", ("attribute", GameText.Attribute(ClaraAttribute.Perception)), ("level", 5))));
            Assert.That(offer.Value.Blocked, Is.False, "she may train Perception before its turn");
        }

        [Test]
        public void AnAttributeGate_AndAMissingItem_TheOfferNamesTheAttributeFirst()
        {
            // An attribute takes runs, an item only a trip (decisions log, 2026-10-01).
            _sweep.needs.Add(new ResourceAmount { resource = MakeObject("Broom", max: 1), amount = 1 });
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();

            Assert.That(OfferOf(sim).Value.Reason, Is.EqualTo(Reason("needs_attribute", ("attribute", GameText.Attribute(ClaraAttribute.Perception)), ("level", 5))));
        }

        [Test]
        public void APerceptionGatedRoomTask_HasNoReason_OnceHerPerceptionReachesIt()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();
            sim.Loop.AttributeXp[ClaraAttribute.Perception] = 100000f;

            Assert.That(OfferOf(sim).Value.Reason, Is.Null);
        }
    }
}
