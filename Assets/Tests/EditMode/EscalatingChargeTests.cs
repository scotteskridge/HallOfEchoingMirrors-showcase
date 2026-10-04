using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Plan 030b: a flat vitality charge that grows with each use of a verb this run (Travel), is
    /// paid even while action costs are off, and isn't softened by her stats.
    /// </summary>
    public class EscalatingChargeTests : SimulationTestBase
    {
        private const float Start = 5f, Growth = 1.2f;

        private LoopSettings _settings;

        private Simulation Begin(bool costsOn = true, GameContent content = null)
        {
            _settings = MakeLoopSettings();
            _settings.chargeActionCosts = costsOn;
            var sim = new Simulation(_settings, TicksPerSecond, content);
            sim.BeginLoop();
            return sim;
        }

        private TaskDefinition MakeCharged(string name = "Walk")
        {
            var task = MakeTask(name, 1f);
            task.escalatingCharge = Start;
            task.chargeGrowth = Growth;
            return task;
        }

        private static float VitalityCharged(Simulation sim, TaskDefinition task)
        {
            foreach (var (_, pool, amount) in sim.PriceOf(task, null).Costs)
                if (pool == sim.Loop.Vitality)
                    return amount;
            return 0f;
        }

        [Test]
        public void FirstUse_ChargesStart()
        {
            var sim = Begin();
            var walk = MakeCharged();
            sim.Schedule(walk, 1);

            RunSeconds(sim, 1.2f);

            Assert.That(100f - sim.Loop.Vitality.Current, Is.EqualTo(Start).Within(0.01f));
        }

        [Test]
        public void NthUse_ChargesStartTimesGrowthToNMinus1()
        {
            var sim = Begin();
            var walk = MakeCharged();
            sim.Schedule(walk, 3);

            RunSeconds(sim, 3.5f);

            Assert.That(100f - sim.Loop.Vitality.Current, Is.EqualTo(5f + 5f * 1.2f + 5f * 1.44f).Within(0.01f), "5 + 6 + 7.2");
            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(5f * 1.728f).Within(0.001f), "the fourth would cost 8.64");
        }

        [Test]
        public void Charge_NextRun_StartsOverFromTheBase()
        {
            var sim = Begin();
            var walk = MakeCharged();
            sim.Schedule(walk, 3);
            RunSeconds(sim, 3.5f);

            sim.BeginLoop();

            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(Start).Within(0.001f));
        }

        [Test]
        public void Charge_EachVerb_IsCountedSeparately()
        {
            var sim = Begin();
            var walk = MakeCharged("Walk");
            var practise = MakeCharged("Practise");
            sim.Schedule(walk, 2);
            RunSeconds(sim, 2.5f);

            Assert.That(VitalityCharged(sim, practise), Is.EqualTo(Start).Within(0.001f), "a verb not yet used this run");
            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(5f * 1.44f).Within(0.001f));
        }

        [Test]
        public void Charge_WithActionCostsOff_IsStillPaid()
        {
            var sim = Begin(costsOn: false);
            var walk = MakeCharged();
            SetCost(walk, 10f, (CostSource.Vitality, 100f)); // its ordinary cost is off; the charge isn't
            sim.Schedule(walk, 1);

            RunSeconds(sim, 1.2f);

            Assert.That(100f - sim.Loop.Vitality.Current, Is.EqualTo(Start).Within(0.01f));
        }

        [Test]
        public void GoesToVitality_NotSoftenedByStats()
        {
            _settings = MakeLoopSettings();
            _settings.pools.Add(HuePool(Hue.Amber, 100f));
            var sim = new Simulation(_settings, TicksPerSecond, null);
            sim.BeginLoop();
            var walk = MakeCharged();
            SetCost(walk, 10f, (CostSource.Amber, 100f)); // an ordinary cost that Attunement does soften
            var before = sim.PriceOf(walk, null);
            sim.Loop.AttributeXp[ClaraAttribute.Attunement] = 100000f;
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 100000f;
            var after = sim.PriceOf(walk, null);

            Assert.That(after.Costs[0].amount, Is.LessThan(before.Costs[0].amount), "the stats do soften an ordinary cost");
            Assert.That(after.EscalatingCharge, Is.EqualTo(Start).Within(0.001f));
            Assert.That(after.Costs[after.ChargeIndex].pool, Is.SameAs(sim.Loop.Vitality));
        }

        [Test]
        public void BetweenRuns_TheNextRunsPlanStartsFromNone()
        {
            var sim = Begin();
            var walk = MakeCharged();
            sim.Schedule(walk, 3);
            RunSeconds(sim, 3.5f);
            sim.EndRunEarly();

            Assert.That(sim.UsesThisRun(walk), Is.EqualTo(0));
            Assert.That(sim.PriceOf(walk, null).EscalatingCharge, Is.EqualTo(Start).Within(0.001f));
        }

        [Test]
        public void AHeldItemOrRoomThatEasesCost_StillScalesIt()
        {
            var sim = Begin();
            var warmth = MakeResource("Warmth", max: 1);
            var walk = MakeCharged();
            walk.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = warmth, costEach = 0.5f, timeEach = 1f });
            sim.Loop.ToolsAndStats[warmth] = 1;

            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(Start * 0.5f).Within(0.001f));
        }

        [Test]
        public void InterruptedUse_DoesNotCount()
        {
            var sim = Begin();
            var walk = MakeCharged();
            sim.Schedule(walk, 1);

            RunSeconds(sim, 0.5f); // part-way through the first use

            Assert.That(sim.UsesThisRun(walk), Is.EqualTo(0));
            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(Start).Within(0.001f));
        }

        [Test]
        public void UseCount_SurvivesSavingAndLoading()
        {
            var walk = MakeCharged();
            var content = Make<GameContent>();
            content.tasks.Add(walk);
            var sim = Begin(content: content);
            sim.Schedule(walk, 3);
            RunSeconds(sim, 2.5f);
            Assert.That(sim.UsesThisRun(walk), Is.EqualTo(2));

            var reopened = Reopen(sim, _settings, content, new List<string>());

            Assert.That(reopened.UsesThisRun(walk), Is.EqualTo(2));
            Assert.That(VitalityCharged(reopened, walk), Is.EqualTo(5f * 1.44f).Within(0.001f));
        }

        // Three rooms in a row; Travel carries the charge (5, growing 1.2).
        private (Simulation sim, GameContent content, NodeDefinition a, NodeDefinition b, NodeDefinition c) MakeHall()
        {
            var a = MakeNode("A");
            var b = MakeNode("B");
            var c = MakeNode("C");
            Join(a, b);
            Join(b, c);
            var content = MakePlaces(a, b, c);
            content.travelVerb.escalatingCharge = Start;
            content.travelVerb.chargeGrowth = Growth;
            return (Begin(content: content), content, a, b, c);
        }

        [Test]
        public void PlannedTripCharges_ProjectTheGrowthPastTheTripsQueuedBeforeIt()
        {
            var (sim, _, _, b, c) = MakeHall();
            sim.ScheduleTrip(b);
            sim.ScheduleTrip(c);

            var charges = new List<float>();
            sim.PlannedTripCharges(charges);

            Assert.That(charges, Has.Count.EqualTo(2));
            Assert.That(charges[0], Is.EqualTo(5f).Within(0.001f), "the trip under way");
            Assert.That(charges[1], Is.EqualTo(6f).Within(0.001f), "one move made by then");
        }

        [Test]
        public void PlannedTripCharges_BetweenRuns_IgnoreWhatTheLastRunHeld()
        {
            var (sim, content, _, b, _) = MakeHall();
            var warmth = MakeResource("Warmth", max: 1);
            content.travelVerb.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = warmth, costEach = 0.5f, timeEach = 1f });
            sim.Loop.ToolsAndStats[warmth] = 1;
            var charges = new List<float>();
            sim.ScheduleTrip(b);
            sim.PlannedTripCharges(charges);
            Assert.That(charges[0], Is.EqualTo(Start * 0.5f).Within(0.001f), "held now, during the run");

            sim.EndRunEarly();
            sim.ScheduleTrip(b);
            sim.PlannedTripCharges(charges);

            Assert.That(charges[^1], Is.EqualTo(Start).Within(0.001f), "between runs only what she keeps or has packed counts");
        }

        [Test]
        public void PlannedTripCharges_BetweenRuns_CountWhatSheHasPacked()
        {
            var (sim, content, _, b, _) = MakeHall();
            var warmth = MakeResource("Warmth", max: 1);
            content.travelVerb.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = warmth, costEach = 0.5f, timeEach = 1f });
            sim.EndRunEarly();
            sim.Persistent.Stash[warmth] = 1;
            var charges = new List<float>();
            sim.ScheduleTrip(b);

            sim.PlannedTripCharges(charges);
            Assert.That(charges[0], Is.EqualTo(Start).Within(0.001f), "in the stash but not packed");

            sim.Persistent.Packed.Add(warmth);
            sim.PlannedTripCharges(charges);
            Assert.That(charges[0], Is.EqualTo(Start * 0.5f).Within(0.001f), "packed, so it goes in with her");
        }

        [Test]
        public void PlannedTripCharges_AreZeroForActionsAndTripsSheWontMake()
        {
            var (sim, _, a, b, _) = MakeHall();
            sim.ScheduleTrip(b);
            sim.ScheduleTrip(b); // already there by then
            sim.ScheduleTrip(a);

            var charges = new List<float>();
            sim.PlannedTripCharges(charges);

            Assert.That(charges[0], Is.EqualTo(5f).Within(0.001f));
            Assert.That(charges[1], Is.EqualTo(0f), "a trip she won't make");
            Assert.That(charges[2], Is.EqualTo(6f).Within(0.001f), "the skipped trip raised nothing");
        }

        [Test]
        public void MovesAndTheirVitality_AreTalliedForTheReport()
        {
            var (sim, _, _, b, c) = MakeHall();
            sim.ScheduleTrip(b);
            sim.ScheduleTrip(c);

            RunSeconds(sim, 2.5f);
            Assert.That(sim.MovesThisRun, Is.EqualTo(2));
            sim.EndRunEarly();

            Assert.That(sim.LastRun.Moves, Is.EqualTo(2));
            Assert.That(sim.LastRun.MoveVitality, Is.EqualTo(5f + 6f).Within(0.01f));
        }

        [Test]
        public void TheMovesTally_SurvivesSaveAndLoad()
        {
            var (sim, content, _, b, c) = MakeHall();
            sim.ScheduleTrip(b);
            sim.ScheduleTrip(c);
            RunSeconds(sim, 1.5f); // the first trip done, the second part-way

            var reopened = Reopen(sim, _settings, content, new List<string>());
            reopened.EndRunEarly();

            Assert.That(reopened.LastRun.Moves, Is.EqualTo(1));
            Assert.That(reopened.LastRun.MoveVitality, Is.EqualTo(5f + 3f).Within(0.4f), "the first move's 5 and about half of the second's 6");
        }

        [Test]
        public void ACharged_NonTripTask_IsTalliedForTheReport_ApartFromMoves()
        {
            var sim = Begin();
            var walk = MakeCharged("Practise the cut");
            var plain = MakeTask("Hum", 1f);
            sim.Schedule(walk, 2);
            sim.Schedule(plain, 1);

            RunSeconds(sim, 3.5f);
            sim.EndRunEarly();

            Assert.That(sim.LastRun.Charges, Has.Count.EqualTo(1), "only a task with a charge is listed");
            var (task, goes, vitality) = sim.LastRun.Charges[0];
            Assert.That(task, Is.SameAs(walk));
            Assert.That(goes, Is.EqualTo(2));
            Assert.That(vitality, Is.EqualTo(5f + 6f).Within(0.01f));
            Assert.That(sim.LastRun.Moves, Is.EqualTo(0), "trips keep their own line");
        }

        [Test]
        public void AChargedTask_CutShortOnItsFirstGo_StillListsWhatItTook()
        {
            var sim = Begin();
            var walk = MakeCharged("Practise the cut");
            sim.Schedule(walk, 1);

            RunSeconds(sim, 0.5f);
            sim.EndRunEarly();

            var (task, goes, vitality) = sim.LastRun.Charges[0];
            Assert.That(task, Is.SameAs(walk));
            Assert.That(goes, Is.EqualTo(0), "no go was finished");
            Assert.That(vitality, Is.EqualTo(Start / 2f).Within(0.4f), "about half the charge was paid");
        }

        [Test]
        public void ATrip_IsNotListedAmongTheOtherCharges()
        {
            var (sim, _, _, b, _) = MakeHall();
            sim.ScheduleTrip(b);

            RunSeconds(sim, 1.5f);
            sim.EndRunEarly();

            Assert.That(sim.LastRun.Moves, Is.EqualTo(1));
            Assert.That(sim.LastRun.Charges, Is.Empty);
        }

        [Test]
        public void TheChargesTally_SurvivesSaveAndLoad_MidRunAndInTheSavedReport()
        {
            var hall = MakeNode("Hall");
            var walk = MakeCharged("Practise the cut");
            hall.tasks.Add(walk);
            var content = MakePlaces(hall);
            content.tasks.Add(walk);
            var sim = Begin(content: content);
            sim.Schedule(walk, 3);
            RunSeconds(sim, 1.5f); // one go done, the second part-way

            var reopened = Reopen(sim, _settings, content, new List<string>());
            reopened.EndRunEarly();

            var (task, goes, vitality) = reopened.LastRun.Charges[0];
            Assert.That(task, Is.SameAs(walk));
            Assert.That(goes, Is.EqualTo(1));
            Assert.That(vitality, Is.EqualTo(5f + 3f).Within(0.4f), "the first go's 5 and about half of the second's 6");

            var again = Reopen(reopened, _settings, content, new List<string>());
            Assert.That(again.LastRun.Charges, Is.EqualTo(reopened.LastRun.Charges), "the Summary keeps it after a load");
        }

        [Test]
        public void PriceOf_ShowsCurrentCharge()
        {
            var sim = Begin(costsOn: false);
            var walk = MakeCharged();

            Assert.That(sim.PriceOf(walk, null).HasCost, Is.True, "costs are off, but the charge shows");
            Assert.That(VitalityCharged(sim, walk), Is.EqualTo(Start).Within(0.001f));
        }
    }
}
