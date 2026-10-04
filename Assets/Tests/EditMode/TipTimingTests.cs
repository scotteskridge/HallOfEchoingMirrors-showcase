using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The first tooltip waits; the ones after it, while scanning a list, open at once.</summary>
    public class TipTimingTests
    {
        private const float Delay = 0.35f;
        private const float Window = 0.25f;

        [Test]
        public void First_WaitsTheFullDelay()
        {
            var timing = new TipTiming(Delay, Window);

            Assert.That(timing.DelayAt(10f), Is.EqualTo(Delay));
        }

        [Test]
        public void WithinTheWindow_OpensAtOnce()
        {
            var timing = new TipTiming(Delay, Window);
            timing.NoteShown(10f);

            Assert.That(timing.DelayAt(10.1f), Is.EqualTo(0f), "moving to a neighbour right after one closed");
        }

        [Test]
        public void AfterTheWindow_WaitsAgain()
        {
            var timing = new TipTiming(Delay, Window);
            timing.NoteShown(10f);

            Assert.That(timing.DelayAt(10.5f), Is.EqualTo(Delay));
        }
    }
}
