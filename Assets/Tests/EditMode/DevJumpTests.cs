using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The dev panel's "Jump to" (plan 027d): a stage puts the kept state where that point in the game leaves it.</summary>
    public class DevJumpTests : SimulationTestBase
    {
        private GameContent _content;
        private NodeDefinition _hall, _lab;
        private TaskDefinition _talk, _earringsTask;
        private SwitchDefinition _takesTheRing, _earringsFound;
        private ResourceDefinition _insight, _earrings;
        private SkillDefinition _crafting;
        private DevJumpStage _stage;

        // The hall ↔ the lab (4 searches). Roland Takes the Ring locks Talk, unlocks Take the earrings
        // and reopens the lab's search; holding the earrings flips The Earrings. Both have stories.
        [SetUp]
        public void SetUp()
        {
            _hall = MakeNode("The hall");
            _lab = MakeNode("The lab");
            _lab.exploresToFill = 4;
            _lab.firstEntry = MakeStory("The Lab\n[PLACEHOLDER]");
            Join(_hall, _lab);
            _content = MakePlaces(_hall, _lab);
            _content.exploreVerb = MakeTask("Search", 1f);
            _insight = MakeResource("Insight", ResourceLifetime.Forever, max: 5);
            _earrings = MakeResource("Earrings", ResourceLifetime.Forever, max: 1);
            _crafting = MakeSkill("Crafting");
            _content.skills.Add(_crafting);

            _talk = MakeTask("Talk to Roland", 1f);
            _earringsTask = MakeTask("Take the earrings", 1f, startsUnlocked: false);
            _content.tasks.AddRange(new[] { _talk, _earringsTask });

            _takesTheRing = Make<SwitchDefinition>();
            _takesTheRing.trigger = SwitchTrigger.TasksCompletedInOneRun;
            _takesTheRing.requiredTasks.Add(_talk);
            _takesTheRing.locksTasks.Add(_talk);
            _takesTheRing.unlocksTasks.Add(_earringsTask);
            _takesTheRing.reopensSearch.Add(_lab);
            _takesTheRing.story = MakeStory("He Takes the Ring\n[PLACEHOLDER]");
            _earringsFound = Make<SwitchDefinition>();
            _earringsFound.trigger = SwitchTrigger.ResourceReached;
            _earringsFound.resourceToHold = _earrings;
            _earringsFound.triggerAmount = 1;
            _earringsFound.story = MakeStory("The Earrings\n[PLACEHOLDER]");
            _content.switches.AddRange(new[] { _takesTheRing, _earringsFound });

            _stage = Make<DevJumpStage>();
            _stage.switches.Add(_takesTheRing);
            _stage.keptItems.Add(new ResourceAmount { resource = _insight, amount = 3 });
            _stage.keptItems.Add(new ResourceAmount { resource = _earrings, amount = 1 });
            _stage.roomsSearched.Add(new DevJumpStage.RoomSearched { room = _lab, percent = 25 });
            _stage.skillMastery.Add(new DevJumpStage.SkillLevel { skill = _crafting, level = 3 });
        }

        private Simulation BetweenRuns()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();
            sim.EndRunEarly();
            return sim;
        }

        [Test]
        public void JumpTo_FlipsSwitches_WithEffects_StoriesRead()
        {
            var sim = BetweenRuns();

            sim.JumpTo(_stage);

            Assert.That(sim.IsFlipped(_takesTheRing), Is.True);
            Assert.That(sim.IsUnlocked(_talk), Is.False, "its locks applied");
            Assert.That(sim.IsUnlocked(_earringsTask), Is.True, "and its unlocks");
            Assert.That(sim.IsFlipped(_earringsFound), Is.True, "holding the earrings flips their switch too");
            Assert.That(sim.Persistent.Journal, Has.Member(_takesTheRing.story), "in the journal");
            Assert.That(sim.UnreadStories, Is.Empty, "but no pop-ups: not the switches', nor the room's first entry");
            Assert.That(sim.Persistent.RoomsEntered, Has.Member(_lab), "a room searched has been entered");
        }

        [Test]
        public void JumpTo_GrantsKeptItems_SetsSearchAndMastery()
        {
            var sim = BetweenRuns();

            sim.JumpTo(_stage);

            Assert.That(sim.AmountOf(_insight), Is.EqualTo(3));
            Assert.That(sim.AmountOf(_earrings), Is.EqualTo(1));
            Assert.That(sim.ExploredFraction(_lab), Is.EqualTo(0.25f).Within(0.001f), "after the switch's reset, then set");
            Assert.That(sim.MasteryOf(_crafting), Is.EqualTo(3));
        }

        [Test]
        public void JumpTo_NeverLowers()
        {
            var sim = BetweenRuns();
            sim.Persistent.Resources[_insight] = 5;
            sim.Persistent.SkillMasteryXp[_crafting] = 1000f;
            sim.JumpTo(_stage); // the switch resets the lab once: that's its real effect
            sim.Persistent.Explored[_lab] = 3f;

            sim.JumpTo(_stage);

            Assert.That(sim.AmountOf(_insight), Is.EqualTo(5));
            Assert.That(sim.Persistent.SkillMasteryXpOf(_crafting), Is.EqualTo(1000f));
            Assert.That(sim.ExploresDoneIn(_lab), Is.EqualTo(3f), "75% stays 75%");
        }

        [Test]
        public void JumpTo_Twice_ChangesNothing()
        {
            var sim = BetweenRuns();
            sim.JumpTo(_stage);
            var first = SaveAndLoad(sim, _content);

            sim.JumpTo(_stage);
            var second = SaveAndLoad(sim, _content);

            Assert.That(second.Explored, Is.EquivalentTo(first.Explored));
            Assert.That(second.Resources, Is.EquivalentTo(first.Resources));
            Assert.That(second.SkillMasteryXp, Is.EquivalentTo(first.SkillMasteryXp));
            Assert.That(second.FlippedSwitches, Is.EquivalentTo(first.FlippedSwitches));
        }

        [Test]
        public void JumpTo_ARunNotYetBegun_StartsFromTheStage()
        {
            // As straight after loading: a fresh run, no time passed. It mustn't credit itself with the jump.
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();

            sim.JumpTo(_stage);

            Assert.That(sim.Loop.SwitchesFlipped, Is.Empty, "the switches flipped before this run, not in it");
            Assert.That(sim.Loop.Milestones, Is.Empty);
            Assert.That(sim.Loop.KeptAtStart[_earrings], Is.EqualTo(1), "she began the run with the earrings");
            Assert.That(sim.Loop.SkillMasteryAtStart[_crafting], Is.EqualTo(sim.Persistent.SkillMasteryXpOf(_crafting)));
            Assert.That(sim.Loop.ExploredAtStart[_lab], Is.EqualTo(1f).Within(0.001f), "25% of 4");
        }

        [Test]
        public void JumpTo_MidRun_IsRefused()
        {
            var sim = new Simulation(MakeLoopSettings(), TicksPerSecond, _content);
            sim.BeginLoop();
            KeepBusy(sim, 5f);
            RunSeconds(sim, 1f);

            Assert.Throws<System.InvalidOperationException>(() => sim.JumpTo(_stage));
        }

        [Test]
        public void JumpTo_ANullEntry_IsAMistake()
        {
            _stage.switches.Add(null);
            var sim = BetweenRuns();

            Assert.Throws<System.InvalidOperationException>(() => sim.JumpTo(_stage));
        }
    }
}
