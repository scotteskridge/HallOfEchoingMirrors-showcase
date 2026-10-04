using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The rules the lab needs: once-per-run tasks, tasks made easier by what she holds, Composure
    /// against a carried item's drain.
    /// </summary>
    public class LabRulesTests : SimulationTestBase
    {
        private List<string> _skipped = new List<string>();

        private Simulation Begin(LoopSettings settings = null)
        {
            _skipped.Clear();
            var sim = new Simulation(settings ?? MakeLoopSettings(), TicksPerSecond);
            _skipped = RecordRefusals(sim);
            return sim;
        }

        [Test]
        public void AOncePerRunTask_IsSkippedTheSecondTime_ButWorksAgainNextRun()
        {
            var search = MakeTask("Search the laboratory", 1f);
            search.oncePerRun = true;
            var sim = Begin();
            sim.Schedule(search); // done after one go, even without a limit
            sim.Schedule(MakeTask("Look", 1f), 1);
            sim.Schedule(search, 1);
            sim.BeginLoop();
            RunSeconds(sim, 3);

            Assert.That(_skipped, Is.EqualTo(new[] { $"Search the laboratory: {Reason("done_this_run")}" }));

            sim.EndRunEarly();
            sim.Schedule(search, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1);
            Assert.That(sim.Loop.CompletedTasks, Has.Member(search), "a new run, a new chance");
        }

        [Test]
        public void OnceEverTask_LockedBySwitch_AfterCompletion()
        {
            // As Talk to Roland: unlocked from the start (the room's search decides when it shows),
            // and its own switch locks it once done.
            var talk = MakeTask("Talk to Roland", 1f);
            var done = Make<SwitchDefinition>();
            done.trigger = SwitchTrigger.TasksCompletedInOneRun;
            done.requiredTasks.Add(talk);
            done.locksTasks.Add(talk);
            var content = Make<GameContent>();
            content.tasks.Add(talk);
            content.switches.Add(done);
            _skipped.Clear();
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            _skipped = RecordRefusals(sim);
            Assert.That(sim.IsUnlocked(talk), Is.True);

            sim.BeginLoop();
            sim.Schedule(talk, 1);
            RunSeconds(sim, 1.5f);
            sim.EndRunEarly();

            Assert.That(sim.IsUnlocked(talk), Is.False);
            sim.BeginLoop();
            sim.Schedule(talk, 1);
            Assert.That(sim.Loop.CompletedTasks, Has.No.Member(talk));
            Assert.That(_skipped, Is.Not.Empty, "refused as it's asked for");
        }

        [Test]
        public void HoldingSomething_MakesATaskCheaperAndQuicker_OncePerOneHeld()
        {
            var insight = MakeResource("What he did that night", ResourceLifetime.Forever, max: 5);
            var cut = MakeTask("Cut the stone", 10f);
            SetCost(cut, 20f, (CostSource.Vitality, 100f));
            cut.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = insight, costEach = 0.5f, timeEach = 0.5f });
            var sim = Begin();
            sim.Persistent.Resources[insight] = 2; // ×0.5 twice = ×0.25

            Assert.That(sim.HeldModifierFor(cut), Is.EqualTo((0.25f, 0.25f)));

            sim.Schedule(cut, 1);
            sim.BeginLoop();
            RunSeconds(sim, 2.5f); // 10s × 0.25
            Assert.That(sim.Loop.CompletedTasks, Has.Member(cut));
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(100f - 20f * 0.25f).Within(0.01f));
        }

        [Test]
        public void Composure_SoftensWhatACarriedItemCosts()
        {
            var settings = MakeLoopSettings();
            settings.composureCarryCostPerLevel = 1f; // each level of strength: ÷2, ÷3...
            settings.xpForFirstLevel = settings.masteryXpForFirstLevel = 1f; // level 1 at 1 XP, level 2 at 2.5
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 1f;
            var sim = Begin(settings);
            sim.BeginLoop();
            sim.Loop.AttributeXp[ClaraAttribute.Composure] = 1f;                 // level 1 this run
            sim.Persistent.AttributeMasteryXp[ClaraAttribute.Composure] = 1f;    // + mastery 1
            sim.Loop.ToolsAndStats[ring] = 1;

            Assert.That(sim.StrengthOf(ClaraAttribute.Composure), Is.EqualTo(2));
            Assert.That(sim.CarryCostPerSecond(), Is.EqualTo(1f / 3f).Within(0.001f), "level and mastery both count");
        }

        [Test]
        public void CarryingATaxingItem_TrainsComposure_WhateverSheDoes()
        {
            var settings = MakeLoopSettings();
            settings.composureXpPerSecondCarrying = 1f;
            var ring = MakeResource("Roland's ring", ResourceLifetime.Carried, max: 1);
            ring.carryCostPerSecond = 0.5f;
            var take = MakeTask("Take the ring", 1f);
            take.gives.Add(new ResourceAmount { resource = ring, amount = 1 });
            var walk = MakeTask("Walk", 5f); // trains no stat of its own
            var sim = Begin(settings);
            sim.Schedule(take, 1);
            sim.Schedule(walk, 1);
            sim.BeginLoop();

            RunSeconds(sim, 6);

            Assert.That(sim.Loop.XpOf(ClaraAttribute.Composure), Is.EqualTo(5f).Within(0.2f), "about 5 seconds carrying it");
        }

        [Test]
        public void AKeptItem_CanMakeEveryTripCheaper()
        {
            var warmth = MakeResource("The mana stone's warmth", ResourceLifetime.Forever, max: 1);
            var travel = MakeTask("Travel", 1f);
            SetCost(travel, 10f, (CostSource.AllPools, 100f));
            travel.easierWith.Add(new TaskDefinition.HeldModifier { whileHolding = warmth, costEach = 0.5f, timeEach = 1f });
            var hall = Make<NodeDefinition>();
            var junction = Make<NodeDefinition>();
            hall.ways.Add(new Way { to = junction });
            var content = Make<GameContent>();
            content.travelVerb = travel;
            content.startNode = hall;
            content.nodes.AddRange(new[] { hall, junction });
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.Persistent.Resources[warmth] = 1;

            var shown = sim.PriceOf(travel, hall, junction); // what the actions and map show: the cost is halved (10 -> 5), the time unchanged
            Assert.That(shown.TimeTimes, Is.EqualTo(1f));
            Assert.That(System.Linq.Enumerable.Sum(shown.Costs, c => c.amount), Is.EqualTo(5f).Within(0.001f));

            sim.ScheduleTrip(junction);
            sim.BeginLoop();
            RunSeconds(sim, 1);
            Assert.That(sim.Loop.Vitality.Current, Is.EqualTo(95f).Within(0.01f));
        }

        // ---------- A skill gate, and the walk out (plan 027c) ----------

        private SkillDefinition _crafting;
        private TaskDefinition _practise, _craft;
        private NodeDefinition _lab;
        private GameContent _content;

        // In the lab: Practise the cut (1s, 30 XP to Crafting: run level 2 and mastery 2, so Crafting 4)
        // and Craft the gem (1s), which needs Crafting 3 (this run's level plus mastery).
        private Simulation BeginInTheLab()
        {
            _crafting = MakeSkill("Crafting");
            _practise = MakeTask("Practise the cut", 1f);
            _practise.skill = _crafting;
            _practise.xpReward = 30f;
            _craft = MakeTask("Craft the gem", 1f);
            _craft.skill = _crafting;
            _craft.requiresSkills.Add(new TaskDefinition.SkillRequirement { skill = _crafting, level = 3 });
            _lab = MakeNode("The lab");
            _lab.tasks.AddRange(new[] { _practise, _craft });
            _content = MakePlaces(_lab);
            _content.tasks.AddRange(new[] { _practise, _craft });
            _skipped.Clear();
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            _skipped = RecordRefusals(sim);
            sim.BeginLoop();
            return sim;
        }

        [Test]
        public void SkillGate_Unmet_OfferGreyedWithReason()
        {
            var sim = BeginInTheLab();

            var offer = OfferOf(sim, _lab, _craft);

            Assert.That(offer, Is.Not.Null, "shown, never secret");
            Assert.That(offer.Value.Reason, Is.EqualTo(Reason("needs_skill", ("skill", "Crafting"), ("level", 3), ("has", 0))));
            Assert.That(offer.Value.Blocked, Is.False, "it can still be queued behind practice");

            sim.PlayNow(_craft);
            Assert.That(_skipped, Is.EqualTo(new[] { $"Craft the gem: {Reason("needs_skill", ("skill", "Crafting"), ("level", 3), ("has", 0))}" }));
        }

        [Test]
        public void SkillGate_Met_Offered()
        {
            var sim = BeginInTheLab();
            sim.Schedule(_practise, 1);
            RunSeconds(sim, 1.1f);
            Assert.That(sim.StrengthOf(_crafting), Is.EqualTo(4), "run level 2 plus mastery 2");

            Assert.That(OfferOf(sim, _lab, _craft).Value.Reason, Is.Null);
            sim.Schedule(_craft, 1);
            RunSeconds(sim, 1.1f);
            Assert.That(sim.Loop.CompletedTasks, Has.Member(_craft));
        }

        [Test]
        public void SkillGate_QueuedBehindPractice_StartsOnceMet()
        {
            var sim = BeginInTheLab();
            sim.Schedule(_practise, 1);
            sim.Schedule(_craft, 1);

            RunSeconds(sim, 2.2f);

            Assert.That(_skipped, Is.Empty);
            Assert.That(sim.Loop.CompletedTasks, Has.Member(_craft));
        }

        [Test]
        public void SkillGate_Unmet_NoSupplierIsQueuedForTheBlockedAction_UntilTheGateIsMet()
        {
            var sim = BeginInTheLab();
            var facet = MakeObject("Facet", max: 5);
            _craft.needs.Add(new ResourceAmount { resource = facet, amount = 1 });
            var cutFacet = MakeGatherTask("Cut a facet", 1f, facet);
            _lab.tasks.Add(cutFacet);
            _content.tasks.Add(cutFacet);
            var supplied = new List<TaskDefinition>();
            sim.SupplyQueued += (supplier, _, _) => supplied.Add(supplier);
            sim.Schedule(_craft, 1);

            RunSeconds(sim, 3f);

            Assert.That(supplied, Is.Empty, "no point fetching a facet for a craft she can't start");
            Assert.That(sim.AmountOf(facet), Is.EqualTo(0));
            Assert.That(sim.Queue.Count, Is.EqualTo(0), "the craft was dropped, with nothing queued ahead of it");

            sim.Persistent.SkillMasteryXp[_crafting] = 32f; // mastery 3: the gate is met
            sim.Schedule(_craft, 1);
            RunSeconds(sim, 3f);

            Assert.That(supplied, Is.EqualTo(new[] { cutFacet }), "once she can craft, the missing facet is made first");
            Assert.That(sim.Loop.CompletedTasks, Has.Member(_craft));
        }

        [Test]
        public void SkillGate_AndAMissingItem_TheRefusalNamesTheSkillFirst()
        {
            // A skill takes runs, an item only a trip: naming the item first would send her on a wasted trip
            // (decisions log, 2026-10-01). No shipped task needs both, so the craft is given a facet to need.
            var sim = BeginInTheLab();
            var facet = MakeObject("Facet", max: 5);
            _craft.needs.Add(new ResourceAmount { resource = facet, amount = 1 });
            string needsSkill = Reason("needs_skill", ("skill", "Crafting"), ("level", 3), ("has", 0));

            Assert.That(OfferOf(sim, _lab, _craft).Value.Reason, Is.EqualTo(needsSkill));
            sim.PlayNow(_craft);
            Assert.That(_skipped, Is.EqualTo(new[] { $"Craft the gem: {needsSkill}" }));
        }

        [Test]
        public void SkillGate_CountsKeptMastery()
        {
            var sim = BeginInTheLab();
            sim.Persistent.SkillMasteryXp[_crafting] = 25f; // mastery 2: levels need 10, 10.5, 11
            Assert.That(sim.StrengthOf(_crafting), Is.EqualTo(2));
            Assert.That(OfferOf(sim, _lab, _craft).Value.Reason, Is.Not.Null, "2 of 3");

            sim.Persistent.SkillMasteryXp[_crafting] = 32f; // mastery 3
            Assert.That(OfferOf(sim, _lab, _craft).Value.Reason, Is.Null);
        }

        [Test]
        public void ARunEndingMidCraft_KeepsTheXpItEarned()
        {
            var sim = BeginInTheLab();
            sim.Persistent.SkillMasteryXp[_crafting] = 33f;
            var longCraft = _craft;
            longCraft.durationMultiplier *= 10f; // 10s, 15 XP over its length
            sim.Schedule(longCraft, 1);
            RunSeconds(sim, 5f);

            sim.EndRunEarly();

            Assert.That(sim.Persistent.SkillMasteryXpOf(_crafting), Is.GreaterThan(33f + 5f),
                "half a craft still taught her: mastery XP is given as it runs");
        }

        [Test]
        public void WalkOutTask_EndsRunWalkedOut_FlipsSwitch()
        {
            var gem = MakeResource("The gem", ResourceLifetime.Forever, max: 1);
            var start = MakeNode("The smoky mirror");
            var exit = MakeTask("Exit through the glowing mirror", 1f);
            exit.walksOut = true;
            exit.needs.Add(new ResourceAmount { resource = gem, amount = 1 });
            start.tasks.Add(exit);
            var content = MakePlaces(start);
            content.tasks.Add(exit);
            var home = Make<SwitchDefinition>();
            home.trigger = SwitchTrigger.TasksCompletedInOneRun;
            home.requiredTasks.Add(exit);
            home.story = MakeStory("Through the Glass, Home\n[PLACEHOLDER]");
            content.switches.Add(home);
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, content);
            sim.BeginLoop();
            Assert.That(OfferOf(sim, start, exit).Value.Reason, Is.Not.Null, "not without the gem");

            sim.Persistent.Resources[gem] = 1;
            sim.Schedule(exit, 1);
            RunSeconds(sim, 1.5f);

            Assert.That(sim.Phase, Is.Not.EqualTo(LoopPhase.Running));
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut));
            Assert.That(sim.IsFlipped(home), Is.True);
            Assert.That(sim.UnreadStories, Has.Member(home.story), "its story waits to be shown before the Summary");
        }

        // ---------- The first exit wakes the stats (plan 041) ----------

        private NodeDefinition _mirror;
        private TaskDefinition _exit, _study;
        private SwitchDefinition _home;

        // Act I as shipped: every stat asleep, the gem kept, the exit at the start room, and Home (the first
        // walk out) waking all five. A 1 s study trains Scholarship, so a run shows whether stats earn.
        private Simulation BeginActOne()
        {
            var gem = MakeResource("The gem", ResourceLifetime.Forever, max: 1);
            _mirror = MakeNode("The smoky mirror");
            _exit = MakeTask("Exit through the glowing mirror", 1f);
            _exit.walksOut = true;
            _exit.needs.Add(new ResourceAmount { resource = gem, amount = 1 });
            _study = MakeTask("Study", 1f, ClaraAttribute.Scholarship);
            _mirror.tasks.AddRange(new[] { _exit, _study });
            var content = MakePlaces(_mirror);
            content.tasks.AddRange(new[] { _exit, _study });
            _home = Make<SwitchDefinition>();
            _home.trigger = SwitchTrigger.TasksCompletedInOneRun;
            _home.requiredTasks.Add(_exit);
            _home.wakesAttributes.AddRange(AttributeMath.All);
            content.switches.Add(_home);
            var settings = MakeLoopSettings();
            settings.asleepAttributes.AddRange(AttributeMath.All);
            settings.enduranceBankShare = 0.5f; // a bank that would show if the first exit paid it
            settings.vitalityDrainPerSecond = 1f; // so the run loses something to bank
            var sim = new Simulation(settings, TicksPerSecond, content);
            sim.Persistent.Resources[gem] = 1;
            return sim;
        }

        [Test]
        public void FirstWalkOut_WakesAllFiveStats_AtZero()
        {
            var sim = BeginActOne();
            sim.BeginLoop();
            sim.Schedule(_study, 1);
            sim.Schedule(_exit, 1);
            RunSeconds(sim, 3f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut));
            Assert.That(sim.IsFlipped(_home), Is.True);
            Assert.That(sim.Loop.VitalityLostThisRun, Is.GreaterThan(0f), "set-up: the run lost something to bank");
            Assert.That(sim.Persistent.KeptVitality, Is.EqualTo(0f), "Endurance was asleep all run, so the first exit banks nothing");
            Assert.That(sim.LastRun.KeptVitalityGained, Is.EqualTo(0f));
            foreach (var stat in AttributeMath.All)
            {
                Assert.That(sim.IsAwake(stat), Is.True, stat.ToString());
                Assert.That(sim.StrengthOf(stat), Is.EqualTo(0), $"{stat} wakes at zero: asleep all run, it settled nothing");
                Assert.That(sim.Persistent.AttributeMasteryXpOf(stat), Is.EqualTo(0f), stat.ToString());
            }
        }

        [Test]
        public void NextRun_AfterTheExit_StatsEarnAndTheExitIsOffered()
        {
            var sim = BeginActOne();
            sim.BeginLoop();
            sim.Schedule(_exit, 1);
            RunSeconds(sim, 2f);
            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut), "set-up: the first exit");

            sim.BeginLoop();
            sim.Schedule(_study, 1);
            RunSeconds(sim, 1.5f);

            Assert.That(sim.Loop.XpOf(ClaraAttribute.Scholarship), Is.GreaterThan(0f));
            Assert.That(OfferOf(sim, _mirror, _exit).Value.Reason, Is.Null, "the gem is kept, so the way out stays open");
        }

        [Test]
        public void SecondWalkOut_IsANormalEnding()
        {
            var sim = BeginActOne();
            sim.BeginLoop();
            sim.Schedule(_exit, 1);
            RunSeconds(sim, 2f);
            sim.BeginLoop();
            sim.Schedule(_study, 1);
            sim.Schedule(_exit, 1);
            RunSeconds(sim, 3f);

            Assert.That(sim.Loop.EndReason, Is.EqualTo(LoopEndReason.WalkedOut));
            Assert.That(sim.LastRun.SwitchesFlipped, Is.Empty, "Home was flipped by the first walk out, not again");
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship), Is.GreaterThan(0f), "a walk out settles all of the run's stat XP");
            Assert.That(sim.Persistent.AttributeMasteryXpOf(ClaraAttribute.Scholarship),
                Is.EqualTo(sim.Loop.XpOf(ClaraAttribute.Scholarship) * sim.Settings.masteryShare).Within(0.01f));
        }
    }
}
