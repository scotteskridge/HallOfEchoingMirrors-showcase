using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Which stops wear the won't-carry ⚠, in the Queue column and on the map tag alike (StopCard.MarksNotCarried).</summary>
    public class StopCardTests
    {
        private NodeDefinition _room;

        [SetUp]
        public void SetUp() => _room = ScriptableObject.CreateInstance<NodeDefinition>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_room);

        [Test]
        public void MarksNotCarried_EmptyStop_IsNotMarked()
        {
            var stop = new QueueStop(_room, 1, false, 0, 0, carriesOver: false);
            Assert.IsFalse(StopCard.MarksNotCarried(stop), "an empty stop has nothing to lose (the user, 2026-10-01)");
        }

        [Test]
        public void MarksNotCarried_StopWithActionsThatWontCarry_IsMarked()
        {
            var stop = new QueueStop(_room, 2, false, 0, 2, carriesOver: false);
            Assert.IsTrue(StopCard.MarksNotCarried(stop));
        }

        [Test]
        public void MarksNotCarried_StopThatCarriesOver_IsNotMarked()
        {
            var stop = new QueueStop(_room, 2, false, 0, 2, carriesOver: true);
            Assert.IsFalse(StopCard.MarksNotCarried(stop));
        }
    }
}
