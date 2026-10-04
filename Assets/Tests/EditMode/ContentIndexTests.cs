using System.Reflection;
using System.Text.RegularExpressions;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The save loader finds content by Id by following references out from Game Content.</summary>
    public class ContentIndexTests : SimulationTestBase
    {
        [Test]
        public void ContentReachableOnlyThroughNewReferences_StillResolves()
        {
            var room = MakeNode("The lab");
            var reopened = MakeNode("The reopened room");
            var gate = MakeSkill("Crafting");
            var afterSwitch = Make<SwitchDefinition>();
            var reopener = Make<SwitchDefinition>();
            reopener.reopensSearch.Add(reopened);
            var gated = MakeTask("Craft the gem", 1f);
            gated.requiresSkills.Add(new TaskDefinition.SkillRequirement { skill = gate, level = 8 });
            var find = MakeTask("Take the earrings", 1f);
            room.foundBySearching.Add(new RoomFind { task = find, afterSwitch = afterSwitch });
            room.tasks.Add(gated);
            var content = MakePlaces(room);
            content.switches.Add(reopener);

            var index = new ContentIndex(content);

            Assert.That(index.Find<SkillDefinition>(gate.Id), Is.SameAs(gate), "reached through a task's skill requirement");
            Assert.That(index.Find<SwitchDefinition>(afterSwitch.Id), Is.SameAs(afterSwitch), "reached through a find's afterSwitch");
            Assert.That(index.Find<NodeDefinition>(reopened.Id), Is.SameAs(reopened), "reached through a switch's reopensSearch");
        }

        [Test]
        public void TwoAssetsWithOneId_AreWarnedAbout()
        {
            var room = MakeNode("The hall");
            var other = MakeNode("The other hall");
            typeof(ContentAsset).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(other, room.Id);
            var content = MakePlaces(room, other);

            LogAssert.Expect(LogType.Warning, new Regex("share the Id"));
            var index = new ContentIndex(content);

            Assert.That(index.Find<NodeDefinition>(room.Id), Is.SameAs(room), "the first one wins");
        }

        [Test]
        public void NoContent_IsLoggedAsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("no Game Content"));

            new ContentIndex(null);
        }
    }
}
