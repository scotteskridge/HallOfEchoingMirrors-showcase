using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    public class PermanentResourceTests : SimulationTestBase
    {
        private ResourceDefinition _mirrors;

        [SetUp]
        public void SetUp() => _mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 10);

        // Exploring takes 1s and finds one mirror, kept forever (max 10).
        private (Simulation sim, TaskDefinition explore, GameContent content) MakeWorld(params SwitchDefinition[] switches)
        {
            var settings = MakeLoopSettings(vitalityMax: 10f);

            var explore = MakeTask("Explore", 1f);
            explore.gives.Add(new ResourceAmount { resource = _mirrors, amount = 1 });

            var content = Make<GameContent>();
            content.tasks.Add(explore);
            content.switches.AddRange(switches);

            return (new Simulation(settings, TicksPerSecond, content), explore, content);
        }

        private SwitchDefinition MirrorsSwitch(int amount)
        {
            var sw = Make<SwitchDefinition>();
            sw.trigger = SwitchTrigger.ResourceReached;
            sw.resourceToHold = _mirrors;
            sw.triggerAmount = amount;
            return sw;
        }

        private ResourceDefinition PerRun(string name, int max = 0) => MakeResource(name, ResourceLifetime.ThisRun, max);

        [Test]
        public void APerRunResource_IsGoneNextLoop_AKeptOneStays()
        {
            var (sim, explore, _) = MakeWorld();
            var focus = PerRun("Focus");
            explore.gives.Add(new ResourceAmount { resource = focus, amount = 1 });
            Plan(explore, 3);

            RunLoop(sim);
            Assert.That(sim.AmountOf(focus), Is.EqualTo(3), "during the loop that earned it");

            sim.BeginLoop();
            Assert.That(sim.AmountOf(focus), Is.EqualTo(0), "per-run: reset");
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(3), "kept: still there");
        }

        [Test]
        public void APerRunResource_CanHaveAMaximumToo()
        {
            var (sim, explore, _) = MakeWorld();
            var lantern = PerRun("Lantern oil", max: 2);
            explore.gives.Add(new ResourceAmount { resource = lantern, amount = 1 });
            Plan(explore, 5);

            RunLoop(sim);

            Assert.That(sim.AmountOf(lantern), Is.EqualTo(2));
        }

        [Test]
        public void ASwitch_CanWatchAPerRunResource()
        {
            var focus = PerRun("Focus");
            var focused = MirrorsSwitch(3);
            focused.resourceToHold = focus;
            var (sim, explore, _) = MakeWorld(focused);
            explore.gives.Add(new ResourceAmount { resource = focus, amount = 1 });
            Plan(explore, 3);

            RunLoop(sim);

            Assert.That(sim.IsFlipped(focused), Is.True);
        }

        [Test]
        public void EveryContentAsset_HasItsOwnPermanentId()
        {
            var a = Make<TaskDefinition>();
            var b = Make<TaskDefinition>();

            Assert.That(a.Id, Is.Not.Empty);
            Assert.That(a.Id, Is.Not.EqualTo(b.Id));
            Assert.That(a.Id, Is.EqualTo(a.Id), "the same every time it's asked");
            Assert.That(_mirrors.Id, Is.Not.EqualTo(a.Id), "resources get one too");
        }

        [Test]
        public void PermanentGrants_AreKeptBetweenLoops()
        {
            var (sim, explore, _) = MakeWorld();
            Plan(explore, 3);

            RunLoop(sim);
            sim.BeginLoop();

            Assert.That(sim.Persistent.ResourceOf(_mirrors), Is.EqualTo(3));
            Assert.That(sim.Loop.CountOf(_mirrors), Is.EqualTo(0), "not a per-run tool");
        }

        [Test]
        public void PermanentResources_StopAtTheirMaximum_AndTheTaskStopsWithThem()
        {
            var (sim, explore, _) = MakeWorld();
            var gained = new List<int>();
            sim.ResourceGained += (name, kept) => gained.Add(kept);
            Plan(explore, 8);

            RunLoop(sim); // 8
            RunLoop(sim); // 2 more fit, and then she can hold no more, so she stops

            Assert.That(sim.Persistent.ResourceOf(_mirrors), Is.EqualTo(10));
            Assert.That(sim.Loop.CompletionLog.Count, Is.EqualTo(2), "only the explores that found something");
            Assert.That(gained.FindAll(k => k == 0), Is.Empty, "nothing wasted while full");
        }

        [Test]
        public void RaisingTheMaximum_LetsMoreBeKept()
        {
            var (sim, explore, _) = MakeWorld();
            Plan(explore, 8);
            RunLoop(sim);
            RunLoop(sim);

            sim.Persistent.ResourceCapBonus[_mirrors] = 5; // what meta currency will do
            RunLoop(sim);

            Assert.That(sim.Persistent.ResourceOf(_mirrors), Is.EqualTo(15));
        }

        [Test]
        public void Requirements_CountPermanentResources()
        {
            var (sim, explore, _) = MakeWorld();
            var descend = MakeTask("Descend", 1f);
            descend.needs.Add(new ResourceAmount { resource = _mirrors, amount = 2 });
            Plan(explore, 2);
            RunLoop(sim);

            ClearPlan(); // a new plan for the next run
            Plan(descend, 1);
            RunLoop(sim);

            Assert.That(sim.Loop.CompletedTasks, Has.Member(descend));
        }

        [Test]
        public void ASwitch_FlipsWhenEnoughHaveBeenFound()
        {
            var five = MirrorsSwitch(5);
            var (sim, explore, _) = MakeWorld(five);
            Plan(explore, 4);

            RunLoop(sim);
            Assert.That(sim.IsFlipped(five), Is.False, "4 found");

            RunLoop(sim);
            Assert.That(sim.IsFlipped(five), Is.True, "8 found");
        }

        [Test]
        public void ATaskThatFinishesOnTheTickVitalityRunsOut_StillCounts()
        {
            var five = MirrorsSwitch(5);
            var (sim, explore, _) = MakeWorld(five);
            // No background drain; each explore costs exactly a fifth of her vitality. The fifth
            // explore's last payment empties her on the same tick it finishes.
            var settings = MakeLoopSettings(vitalityMax: 100f);
            var content = Make<GameContent>();
            content.tasks.Add(explore);
            content.switches.Add(five);
            sim = new Simulation(settings, TicksPerSecond, content);
            explore.cost = 20f;
            explore.costShares.Add(new TaskDefinition.CostShare { source = CostSource.Vitality, percent = 100f });
            Plan(explore, 10);

            RunLoop(sim);

            Assert.That(sim.Persistent.ResourceOf(_mirrors), Is.EqualTo(5), "all five explores count");
            Assert.That(sim.IsFlipped(five), Is.True, "so the 5-mirrors story fires in this run, not the next");
        }

        [Test]
        public void ASwitchAtTheMaximum_FlipsTheMomentSheIsFull()
        {
            var ten = MirrorsSwitch(10);
            var (sim, explore, _) = MakeWorld(ten);
            Plan(explore, 8);

            RunLoop(sim); // 8
            Assert.That(sim.IsFlipped(ten), Is.False, "8 of 10");

            RunLoop(sim); // reaches 10 on the second explore
            Assert.That(sim.IsFlipped(ten), Is.True, "10 of 10: flips in this run, not later");
        }

        [Test]
        public void ASwitchAboveTheMaximum_CannotFlipUntilTheMaximumIsRaised()
        {
            var twenty = MirrorsSwitch(20);
            var (sim, explore, _) = MakeWorld(twenty);
            Plan(explore, 8);

            RunLoop(sim);
            RunLoop(sim);
            RunLoop(sim);
            Assert.That(sim.IsFlipped(twenty), Is.False, "she can only hold 10");

            sim.Persistent.ResourceCapBonus[_mirrors] = 10; // meta currency, later
            RunLoop(sim);
            RunLoop(sim);
            Assert.That(sim.IsFlipped(twenty), Is.True);
        }

        // ---------- Knowledge that has done its job ----------

        // Mirrors Found, ticked Hide Once Used, found by searching the Hanging Mirrors; switches at 5 and 10.
        private (Simulation sim, NodeDefinition room, GameContent content) KnowledgeWorld(int mirrors, bool bothFlipped)
        {
            _mirrors.hideOnceUsed = true;
            var five = MirrorsSwitch(5);
            var ten = MirrorsSwitch(10);
            var room = MakeNode("Hanging Mirrors");
            room.eachExploreGives.Add(new ResourceAmount { resource = _mirrors, amount = 1 });
            var content = MakePlaces(room);
            content.switches.AddRange(new[] { five, ten });
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.BeginLoop();
            sim.Persistent.Resources[_mirrors] = mirrors;
            sim.Persistent.FlippedSwitches.Add(five);
            if (bothFlipped)
                sim.Persistent.FlippedSwitches.Add(ten);
            return (sim, room, content);
        }

        [Test]
        public void Knowledge_HasDoneItsJob_OnlyOnceEverySwitchWatchingItHasFlipped()
        {
            Assert.That(KnowledgeWorld(5, bothFlipped: false).sim.HasDoneItsJob(_mirrors), Is.False, "the 10 is still to come");
            Assert.That(KnowledgeWorld(10, bothFlipped: true).sim.HasDoneItsJob(_mirrors), Is.True);
        }

        [Test]
        public void KnowledgeItems_LeaveOutKnowledgeThatHasDoneItsJob()
        {
            var (sim, _, _) = KnowledgeWorld(10, bothFlipped: true);
            var known = new List<(ResourceDefinition item, int amount)>();

            sim.KnowledgeItems(known);

            Assert.That(known, Is.Empty, "Mirrors Found is shown by its room instead");
        }

        [Test]
        public void Knowledge_NotTickedHideOnceUsed_IsAlwaysShown()
        {
            var (sim, _, _) = KnowledgeWorld(10, bothFlipped: true);
            _mirrors.hideOnceUsed = false;

            Assert.That(sim.HasDoneItsJob(_mirrors), Is.False);
        }

        [Test]
        public void Knowledge_NoSwitchWatches_StillHasAJobToDo()
        {
            var warmth = MakeResource("The mana stone's warmth", ResourceLifetime.Forever, max: 1);
            warmth.hideOnceUsed = true;
            var (sim, _, _) = KnowledgeWorld(10, bothFlipped: true);
            sim.Persistent.Resources[warmth] = 1;

            Assert.That(sim.HasDoneItsJob(warmth), Is.False, "nothing watches it: it isn't a counter");
        }

        [Test]
        public void Knowledge_AnActionOrAWayStillUses_StillHasAJobToDo(
            [Values("an action needs it", "an action is easier with it", "a way needs it")] string use)
        {
            var (sim, room, content) = KnowledgeWorld(10, bothFlipped: true);
            var draw = MakeTask("Draw on the mana stone", 1f);
            content.tasks.Add(draw);
            var need = new ResourceAmount { resource = _mirrors, amount = 3 };
            if (use == "an action needs it")
                draw.needs.Add(need);
            else if (use == "an action is easier with it")
                draw.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = _mirrors, timeEach = 0.9f });
            else
                room.ways.Add(new Way { to = MakeNode("The other laboratory"), needs = { need } });

            Assert.That(sim.HasDoneItsJob(_mirrors), Is.False);
        }

        [Test]
        public void TheRoomItWasFoundIn_ShowsKnowledgeThatHasDoneItsJob()
        {
            var shown = new List<(ResourceDefinition item, int amount)>();

            var (before, room, _) = KnowledgeWorld(5, bothFlipped: false);
            before.DoneKnowledgeFoundIn(room, shown);
            Assert.That(shown, Is.Empty, "still in her list of what she knows");

            var (after, room2, _) = KnowledgeWorld(10, bothFlipped: true);
            after.DoneKnowledgeFoundIn(room2, shown);
            Assert.That(shown, Is.EqualTo(new[] { (_mirrors, 10) }));
        }
    }
}
