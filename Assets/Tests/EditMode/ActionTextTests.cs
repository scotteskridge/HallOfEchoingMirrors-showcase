using System.Linq;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>An action's tooltip as rows (plan ui-036a): the same facts as the old sentences, one place each.</summary>
    public class ActionTextTests : SimulationTestBase
    {
        private GameContent _content;
        private NodeDefinition _hall;
        private TaskDefinition _explore, _gather, _tiring, _pickUp, _putDown;
        private SkillDefinition _wayfinding;
        private ResourceDefinition _wisp;

        // A hall of 4 searches with a free gather, a 5-vitality task, and the Pick up and Put down verbs.
        [SetUp]
        public void SetUp()
        {
            _wayfinding = MakeSkill("Wayfinding");
            _wisp = MakeObject("Wisp", max: 10);
            _explore = MakeTask("Search", 4f, ClaraAttribute.Perception);
            _explore.skill = _wayfinding;
            _gather = MakeGatherTask("Gather a wisp", 2f, _wisp);
            _gather.kind = TaskKind.Gather;
            _tiring = MakeTiringTask("Heave", 3f, 5f);
            _pickUp = MakeTask("Pick up", 1f);
            _putDown = MakeTask("Put down", 1f);

            _hall = MakeNode("The Hall");
            _hall.exploresToFill = 4;
            _hall.tasks.Add(_gather);
            _hall.tasks.Add(_tiring);
            _content = MakePlaces(_hall);
            _content.exploreVerb = _explore;
            _content.pickUpVerb = _pickUp;
            _content.putDownVerb = _putDown;
            _content.tasks.Add(_gather);
            _content.tasks.Add(_tiring);
            _content.tasks.Add(_explore);
        }

        private Simulation Begin()
        {
            var sim = new Simulation(MakeLoopSettings(pockets: 5, floor: 5), TicksPerSecond, _content);
            sim.BeginLoop();
            return sim;
        }

        private static TipLayout.Row RowLabelled(TipLayout layout, string key) =>
            layout.Rows.FirstOrDefault(row => row.Label == GameText.Get(key));

        [Test]
        public void Full_KickerNamesTheVerbClassAndThePlace()
        {
            var layout = ActionText.Layout(Begin(), _gather, null, _hall);

            Assert.That(layout.Kicker, Is.EqualTo(GameText.Get("tips.kicker", ("verb", GameText.Get("tips.kicker_gather")), ("room", "The Hall"))));
            Assert.That(layout.Compact, Is.False);
        }

        [Test]
        public void Full_TakesRow_IsTimeAndHasNoBaseWithoutASpeedUp()
        {
            var layout = ActionText.Layout(Begin(), _explore, null, _hall);

            var takes = RowLabelled(layout, "tips.row_takes");
            Assert.That(takes, Is.Not.Null);
            Assert.That(takes.Meaning, Is.EqualTo(TipMeaning.Time));
            Assert.That(takes.Base, Is.Null.Or.Empty, "no skill speeds it yet");
        }

        [Test]
        public void Full_TakesRow_ShowsTheBaseWhenAMasterySpeedsItUp()
        {
            var sim = Begin();
            sim.Persistent.SkillMasteryXp[_wayfinding] = 700f; // mastery 3

            var layout = ActionText.Layout(sim, _explore, null, _hall);

            var takes = RowLabelled(layout, "tips.row_takes");
            float baseSeconds = sim.PriceOf(_explore, _hall).Seconds;
            Assert.That(takes.Base, Is.EqualTo(GameText.Get("tips.from_base", ("seconds", UiText.Number(baseSeconds)))));
        }

        [Test]
        public void Full_CostsRow_IsVitalityWithTheFlatNote()
        {
            var layout = ActionText.Layout(Begin(), _tiring, null, _hall);

            var costs = RowLabelled(layout, "tips.row_costs");
            Assert.That(costs, Is.Not.Null);
            Assert.That(costs.Meaning, Is.EqualTo(TipMeaning.Vitality));
            Assert.That(costs.Value, Is.EqualTo(UiText.Number(5f)));
            Assert.That(costs.Unit, Is.EqualTo(GameText.Get("tips.unit_vitality")), "vitality always carries its word");
            Assert.That(costs.Note, Is.EqualTo(GameText.Get("tips.flat_cost")));
        }

        [Test]
        public void Full_FreeAction_HasNoCostsRow()
        {
            var layout = ActionText.Layout(Begin(), _gather, null, _hall);

            Assert.That(RowLabelled(layout, "tips.row_costs"), Is.Null);
        }

        [Test]
        public void Full_TrainsRow_HasAChipForEachTargetAndTheMasteryRule()
        {
            var layout = ActionText.Layout(Begin(), _explore, null, _hall);

            var trains = RowLabelled(layout, "tips.row_trains");
            Assert.That(trains, Is.Not.Null);
            Assert.That(trains.Chips, Has.Count.EqualTo(2), "Wayfinding (its skill) and Perception (what a search trains)");
            Assert.That(trains.Chips[0], Does.Contain("Wayfinding"));
            Assert.That(trains.Rule, Is.EqualTo(GameText.Get("tips.mastery_rule")));
        }

        [Test]
        public void Full_FasterRow_NamesTheSkill()
        {
            var layout = ActionText.Layout(Begin(), _explore, null, _hall);

            var faster = RowLabelled(layout, "tips.row_faster");
            Assert.That(faster, Is.Not.Null);
            Assert.That(faster.Value, Does.Contain("Wayfinding"));
        }

        [Test]
        public void Full_SkillGate_ShowsMetOrUnmet()
        {
            _tiring.requiresSkills.Add(new TaskDefinition.SkillRequirement { skill = _wayfinding, level = 1 });
            var sim = Begin();

            var unmet = ActionText.Layout(sim, _tiring, null, _hall);
            sim.Persistent.SkillMasteryXp[_wayfinding] = 700f;
            var met = ActionText.Layout(sim, _tiring, null, _hall);

            Assert.That(RowLabelled(unmet, "tips.row_needs").Meaning, Is.EqualTo(TipMeaning.Unmet));
            Assert.That(RowLabelled(met, "tips.row_needs").Meaning, Is.EqualTo(TipMeaning.Met));
        }

        [Test]
        public void Full_Footer_CountsTheSearch()
        {
            var layout = ActionText.Layout(Begin(), _explore, null, _hall);

            Assert.That(layout.Strip, Is.Not.Null);
            Assert.That(layout.Strip.Count, Is.EqualTo(GameText.Get("tips.count", ("done", 0), ("total", 4))));
            Assert.That(layout.Strip.Progress, Is.EqualTo(0f));
        }

        [Test]
        public void Full_Footer_HasNoCountForAnActionWithoutOne()
        {
            var layout = ActionText.Layout(Begin(), _gather, null, _hall);

            Assert.That(layout.Strip.Text, Is.Not.Empty);
            Assert.That(layout.Strip.Count, Is.Null.Or.Empty);
            Assert.That(layout.Strip.Progress, Is.Null);
        }

        [Test]
        public void Compact_PutDown_IsTheTitleAndOneLine()
        {
            var sim = Begin();
            var action = sim.PutDownActionFor(_wisp);

            var layout = ActionText.Layout(sim, action, null, _hall);

            Assert.That(layout.Compact, Is.True);
            Assert.That(layout.Kicker, Is.Null.Or.Empty);
            Assert.That(layout.Strip, Is.Null);
            Assert.That(layout.Rows, Has.Count.EqualTo(1));
            Assert.That(layout.Rows[0].Unit, Does.Contain(GameText.Get("tips.compact_free")));
            Assert.That(layout.Rows[0].Unit, Does.Contain(GameText.Get("tips.compact_trains_nothing")));
            Assert.That(layout.Rows[0].Note, Is.EqualTo(GameText.Get("tips.compact_put_down", ("amount", 0))));
        }

        [Test]
        public void Compact_PickUp_IsCompactToo()
        {
            var sim = Begin();
            var action = sim.PickUpActionFor(_wisp);

            var layout = ActionText.Layout(sim, action, null, _hall);

            Assert.That(layout.Compact, Is.True);
        }

        [Test]
        public void Compact_PickUp_CountsThePileInTheRoomShown()
        {
            // Any room's popover offers its own pile, not just the one she's standing in.
            var gallery = MakeNode("The Gallery");
            Join(_hall, gallery);
            _content.nodes.Add(gallery);
            var sim = Begin();
            sim.Loop.Floor[gallery] = new System.Collections.Generic.Dictionary<ResourceDefinition, int> { { _wisp, 3 } };

            var layout = ActionText.Layout(sim, sim.PickUpActionFor(_wisp), null, gallery);

            Assert.That(layout.Rows[0].Note, Is.EqualTo(GameText.Get("tips.compact_pick_up", ("amount", 3))));
        }
    }
}
