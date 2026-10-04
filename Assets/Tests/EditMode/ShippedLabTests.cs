using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The shipped lab, gem and exit (plans 027a-c), read from the real assets in Assets/Data: that
    /// the wiring between them is whole. Assert relationships (this task names that skill), not the
    /// balance numbers, so retuning a level never breaks these. Read-only: nothing is saved.
    /// </summary>
    public class ShippedLabTests
    {
        private GameContent _content;
        private LoopSettings _settings;
        private NodeDefinition _lab, _hallMirror;
        private SwitchDefinition _rolandTakesTheRing, _gemIsMade;
        private TaskDefinition _practise, _craft, _exit;
        private ResourceDefinition _gem;
        private SkillDefinition _crafting;

        [SetUp]
        public void SetUp()
        {
            GameText.Load(System.IO.File.ReadAllText("Assets/Text/game_text.txt"));
            _content = Load<GameContent>("Assets/Data/GameContent.asset");
            _settings = Load<LoopSettings>("Assets/Data/LoopSettings.asset");
            _lab = Load<NodeDefinition>("Assets/Data/Places/OtherLaboratory.asset");
            _hallMirror = Load<NodeDefinition>("Assets/Data/Places/TheSmokyMirror.asset");
            _rolandTakesTheRing = Load<SwitchDefinition>("Assets/Data/Switches/RolandTakesTheRing.asset");
            _gemIsMade = Load<SwitchDefinition>("Assets/Data/Switches/TheGemIsMade.asset");
            _practise = Load<TaskDefinition>("Assets/Data/Tasks/Lab/PractiseTheCut.asset");
            _craft = Load<TaskDefinition>("Assets/Data/Tasks/Lab/CraftTheGem.asset");
            _exit = Load<TaskDefinition>("Assets/Data/Tasks/Hall/ExitThroughTheGlowingMirror.asset");
            _gem = Load<ResourceDefinition>("Assets/Data/Items/Thegem.asset");
            _crafting = Load<SkillDefinition>("Assets/Data/Skills/Crafting.asset");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"missing {path}");
            return asset;
        }

        [Test]
        public void RolandTakesTheRing_ReopensTheLabsSearch_AndTheSecondRoundHoldsPractiseAndCraft()
        {
            Assert.That(_rolandTakesTheRing.reopensSearch, Has.Member(_lab));
            Assert.That(_lab.foundBySearching.Exists(f => f.task == _practise && f.afterSwitch == _rolandTakesTheRing), Is.True,
                "Practise the cut is found in the reopened round");
            Assert.That(_lab.foundBySearching.Exists(f => f.task == _craft && f.afterSwitch == _rolandTakesTheRing), Is.True,
                "and so is Craft the gem");
        }

        [Test]
        public void CraftTheGem_NeedsCrafting_AndPractiseTheCutTrainsIt()
        {
            Assert.That(_craft.requiresSkills, Has.Count.EqualTo(1));
            var gate = _craft.requiresSkills[0];
            Assert.That(gate.skill, Is.EqualTo(_crafting));
            Assert.That(gate.level, Is.GreaterThan(0));
            Assert.That(_practise.skill, Is.EqualTo(_crafting), "the practice trains the skill the gate asks for");
            Assert.That(_craft.gives.Exists(g => g.resource == _gem && g.amount >= 1), Is.True, "and the craft gives the gem");

            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            sim.BeginLoop();
            Assert.That(sim.StrengthOf(_crafting), Is.LessThan(gate.level), "short of it at the start, so practice comes first");
        }

        [Test]
        public void TheExit_NeedsTheGem_AndOpensOnlyOnceTheGemIsMade()
        {
            Assert.That(_exit.needs.Exists(n => n.resource == _gem), Is.True, "the exit needs the gem");
            Assert.That(_exit.walksOut, Is.True);
            Assert.That(_exit.startsUnlocked, Is.False, "locked until the gem exists");
            Assert.That(_hallMirror.Lists(_exit), Is.True, "offered at the starting mirror");
            Assert.That(_gemIsMade.trigger, Is.EqualTo(SwitchTrigger.ResourceReached));
            Assert.That(_gemIsMade.resourceToHold, Is.EqualTo(_gem));
            Assert.That(_gemIsMade.unlocksTasks, Has.Member(_exit));
            Assert.That(_gemIsMade.locksTasks, Has.Member(_craft), "no second gem");

            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            Assert.That(sim.IsUnlocked(_exit), Is.False);
        }

        // ---------- The talk: one long push, shortened by kept insight (plan 055) ----------

        private TaskDefinition Lab(string name) => Load<TaskDefinition>($"Assets/Data/Tasks/Lab/{name}.asset");
        private ResourceDefinition Insight => Load<ResourceDefinition>("Assets/Data/Items/Whathedidthatnight.asset");

        [Test]
        public void TalkToRoland_NeedsOnlyTheRing()
        {
            var talk = Lab("TalkToRoland");
            var ring = Load<ResourceDefinition>("Assets/Data/Items/Rolandsring.asset");

            Assert.That(talk.needs.ConvertAll(n => n.resource), Is.EquivalentTo(new[] { ring }), "no checklist: only the ring");
            Assert.That(talk.takes.ConvertAll(t => t.resource), Is.EquivalentTo(new[] { ring }), "and he takes it");
        }

        [Test]
        public void ThreeActions_EachGiveOneKeptInsight_OnceARun()
        {
            Assert.That(Insight.lasts, Is.EqualTo(ResourceLifetime.Forever), "insight is kept");
            foreach (var name in new[] { "WatchHim", "StudyTheTome", "Attend" })
            {
                var task = Lab(name);
                Assert.That(task.oncePerRun, Is.True, $"{name}: once a run");
                Assert.That(task.gives.Exists(g => g.resource == Insight && g.amount == 1), Is.True, $"{name} gives 1 insight");
                Assert.That(_lab.foundBySearching.Exists(f => f.task == task && f.afterSwitch == null), Is.True,
                    $"{name} is found in the lab's first search");
            }
        }

        [Test]
        public void Insight_MakesTheTalkShorter()
        {
            var talk = Lab("TalkToRoland");
            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            sim.BeginLoop();
            float without = sim.SecondsAtSpeedNow(talk, _lab);

            sim.Persistent.Resources[Insight] = 3;

            Assert.That(sim.SecondsAtSpeedNow(talk, _lab), Is.LessThan(without), "each insight held shortens the talk");
        }

        [Test]
        public void WhatHeDidThatNight_FollowsWatchingHim()
        {
            var story = Load<SwitchDefinition>("Assets/Data/Switches/WhatHeDidThatNight.asset");

            Assert.That(story.trigger, Is.EqualTo(SwitchTrigger.TasksCompletedInOneRun),
                "its passage is about watching him, so studying first mustn't fire it");
            Assert.That(story.requiredTasks, Is.EquivalentTo(new[] { Lab("WatchHim") }));
        }

        // The dev panel's "Gem in hand" stage exists so the first walk out can be checked while Act I's
        // balance keeps the exit out of reach in play: it must really put the exit one action away.
        [Test]
        public void GemInHandStage_PutsTheExitOneActionAway_AndWalkingOutWakesTheStats()
        {
            var stage = Load<DevJumpStage>("Assets/Data/Dev/GemInHand.asset");
            var home = Load<SwitchDefinition>("Assets/Data/Switches/Home.asset");
            var sim = new Simulation(_settings, TickEngine.TicksPerSecond, _content);
            sim.BeginLoop();

            sim.JumpTo(stage);

            Assert.That(sim.AmountOf(_gem), Is.EqualTo(1), "she holds the gem");
            Assert.That(sim.IsFlipped(_gemIsMade), Is.True);
            Assert.That(sim.IsUnlocked(_exit), Is.True);
            Assert.That(sim.IsAvailableAt(_exit, sim.Loop.CurrentNode), Is.True, "offered where the run starts");
            foreach (var attribute in AttributeMath.All)
                Assert.That(sim.IsAwake(attribute), Is.False, $"{attribute} still asleep: Act I has no stats");

            sim.Schedule(_exit, 1);
            for (int i = 0; i < 600 * TickEngine.TicksPerSecond && !sim.Loop.IsOver; i++)
                sim.Tick();

            Assert.That(sim.Loop.IsOver, Is.True, "the walk out ends the run");
            Assert.That(sim.IsFlipped(home), Is.True, "she walked out: Home");
            foreach (var attribute in AttributeMath.All)
                Assert.That(sim.IsAwake(attribute), Is.True, $"{attribute} woke");
        }
    }
}
