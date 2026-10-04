using HallOfEchoingMirrors.Core;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// A search reopened by a switch (rounds): what the plan's warnings make of a later round's
    /// finds, the message when content gives a room two reopenings, and what a second round pays.
    /// </summary>
    public class SearchRoundTests : SimulationTestBase
    {
        private GameContent _content;
        private TaskDefinition _explore, _talk, _secondFind;
        private NodeDefinition _hall, _dark;
        private SwitchDefinition _reopens;
        private ResourceDefinition _mirrors;

        // The dark: 4 steps to fill, 1 Mirror each; its second round (after Talk) finds an action at 25%.
        [SetUp]
        public void SetUp()
        {
            _explore = MakeTask("Explore", 1f, ClaraAttribute.Perception);
            SetCost(_explore, 5f, (CostSource.Vitality, 100f));
            _secondFind = MakeTask("Take the earrings", 1f);
            _talk = MakeTask("Talk", 1f);
            _mirrors = MakeResource("Mirrors found", ResourceLifetime.Forever, max: 20);

            _hall = MakeNode("The hall");
            _dark = MakeNode("The dark");
            _dark.exploresToFill = 4;
            _dark.eachExploreGives.Add(new ResourceAmount { resource = _mirrors, amount = 1 });
            Join(_hall, _dark);

            _reopens = Make<SwitchDefinition>();
            _reopens.displayName = "The talk";
            _reopens.trigger = SwitchTrigger.TasksCompletedInOneRun;
            _reopens.requiredTasks.Add(_talk);
            _reopens.reopensSearch.Add(_dark);
            _dark.foundBySearching.Add(new RoomFind { task = _secondFind, atSearched = 25, afterSwitch = _reopens });

            _content = MakePlaces(_hall, _dark);
            _content.exploreVerb = _explore;
            _content.tasks.Add(_talk);
            _content.switches.Add(_reopens);
        }

        private Simulation MakeSimulation() => new Simulation(MakeLoopSettings(), TicksPerSecond, _content);

        private void SearchTheDark(Simulation sim, float steps)
        {
            sim.ClearQueue();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_explore);
            sim.BeginLoop();
            RunSeconds(sim, 1 + steps);
        }

        private void Talk(Simulation sim)
        {
            sim.ClearQueue();
            sim.Schedule(_talk, 1);
            sim.BeginLoop();
            RunSeconds(sim, 1.5f);
            sim.EndRunEarly();
        }

        [Test]
        public void ASecondRoundFind_IsNotMightBeFound_WhileTheFirstRoundsBarIsFull()
        {
            var sim = MakeSimulation();
            SearchTheDark(sim, 4);
            sim.EndRunEarly();
            Assert.That(sim.IsFullyExplored(_dark), Is.True);

            // Planned before the switch has flipped: the full bar is the first round's, not the second's.
            sim.ClearQueue();
            sim.ScheduleTrip(_dark);
            sim.Schedule(_secondFind);

            Assert.That(sim.PlanWarnings()[1], Is.EqualTo(Reason("not_here", ("room", "the dark"))),
                "the first round's full bar doesn't make the second round's find possible");
        }

        [Test]
        public void ReopenedSearch_PaysTheExploreGivesAndXpAgain_OnItsSecondRound()
        {
            // Decided 2026-09-30: XP and the explore gives are paid on every completed search action.
            var sim = MakeSimulation();
            SearchTheDark(sim, 4);
            sim.EndRunEarly();
            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(4));
            Talk(sim);

            SearchTheDark(sim, 2);

            Assert.That(sim.AmountOf(_mirrors), Is.EqualTo(6), "two more steps, two more Mirrors");
            Assert.That(sim.Loop.XpOf(ClaraAttribute.Perception), Is.EqualTo(2 * sim.XpRewardOf(_explore, _dark)).Within(0.001f));
        }

        [Test]
        public void TwoSwitchesReopeningOneRoom_ThrowsNamingBothSwitchesAndTheRoom()
        {
            var again = Make<SwitchDefinition>();
            again.displayName = "The second talk";
            again.trigger = SwitchTrigger.TasksCompletedInOneRun;
            again.requiredTasks.Add(_talk);
            again.reopensSearch.Add(_dark);
            _content.switches.Add(again);
            var sim = MakeSimulation();
            Talk(sim);

            var thrown = Assert.Throws<System.InvalidOperationException>(() => sim.IsFoundHere(_secondFind, _dark));

            Assert.That(thrown.Message, Does.Contain("The talk").And.Contain("The second talk").And.Contain("The dark"));
        }
    }
}
