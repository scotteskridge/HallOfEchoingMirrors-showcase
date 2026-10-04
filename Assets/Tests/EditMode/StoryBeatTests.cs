using HallOfEchoingMirrors.Core;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Passage placeholders, filled from a resource's data rather than hard-coded.</summary>
    public class StoryBeatTests : SimulationTestBase
    {
        [Test]
        public void CostPerSecond_ReadsTheResourcesOwnValue()
        {
            var ring = MakeResource("Roland's ring");
            ring.carryCostPerSecond = 0.5f;

            var beat = Make<StoryBeat>();
            beat.text = new TextAsset("The Ring\n\nPay {cost per second} vitality for every step I carry it.");
            beat.costSource = ring;

            Assert.AreEqual("Pay 0.5 vitality for every step I carry it.", beat.Body);

            // Changing the asset's own number changes the passage: never a hard-coded copy.
            ring.carryCostPerSecond = 1.25f;
            Assert.AreEqual("Pay 1.25 vitality for every step I carry it.", beat.Body);
        }

        [Test]
        public void NoCostSource_LeavesThePlaceholderUntouched()
        {
            var beat = Make<StoryBeat>();
            beat.text = new TextAsset("Title\n\nPay {cost per second} vitality.");

            Assert.AreEqual("Pay {cost per second} vitality.", beat.Body);
        }
    }
}
