using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Checks the shipped content (Assets/Data) for mistakes about reopened searches, so they show
    /// here before play: the game throws on the first, and silently never finds the second.
    /// </summary>
    public class ShippedSearchRoundsTests
    {
        private GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
            Assert.That(_content, Is.Not.Null, "missing Assets/Data/GameContent.asset");
        }

        [Test]
        public void NoRoom_IsReopenedByTwoSwitches()
        {
            var reopenedBy = new Dictionary<NodeDefinition, SwitchDefinition>();
            foreach (var @switch in _content.switches)
            {
                if (@switch == null)
                    continue;
                foreach (var room in @switch.reopensSearch)
                {
                    if (room == null)
                        continue;
                    Assert.That(reopenedBy.TryGetValue(room, out var first), Is.False,
                        $"{room.name} is reopened by both {first?.name} and {@switch.name}: only one switch may reopen a room");
                    reopenedBy[room] = @switch;
                }
            }
        }

        [Test]
        public void EveryAfterSwitchFind_NamesASwitchThatReopensItsRoom()
        {
            foreach (var room in _content.PlayableNodes)
            {
                if (room == null)
                    continue;
                foreach (var find in room.foundBySearching)
                {
                    if (find == null || find.afterSwitch == null)
                        continue;
                    Assert.That(find.afterSwitch.reopensSearch, Does.Contain(room),
                        $"{room.name}'s find of {(find.task != null ? find.task.name : "?")} waits on {find.afterSwitch.name}, which doesn't reopen {room.name}'s search: it would never be found");
                }
            }
        }
    }
}
