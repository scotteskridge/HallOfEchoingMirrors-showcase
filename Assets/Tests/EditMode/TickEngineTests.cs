using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The game clock's speed limits.</summary>
    public class TickEngineTests
    {
        private GameObject _host;
        private TickEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("TickEngineTest");
            _engine = _host.AddComponent<TickEngine>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        private int TicksInOneRealSecond()
        {
            int ticks = 0;
            _engine.Ticked += () => ticks++;
            // A hair over one second: float sums can land just short of an exact tick boundary.
            _engine.Advance(1.01f);
            return ticks;
        }

        [Test]
        public void SpeedOne_RunsOneGameSecondPerRealSecond()
        {
            Assert.That(TicksInOneRealSecond(), Is.EqualTo(TickEngine.TicksPerSecond));
        }

        [Test]
        public void Tier_MultipliesTicksPerRealSecond()
        {
            _engine.Speed = 2f;
            Assert.That(TicksInOneRealSecond(), Is.EqualTo(2 * TickEngine.TicksPerSecond));
        }

        [Test]
        public void RoomSpeed_Multiplies()
        {
            _engine.Speed = 2f;
            _engine.RoomSpeed = () => 3f;
            Assert.That(TicksInOneRealSecond(), Is.EqualTo(6 * TickEngine.TicksPerSecond));
        }

        [Test]
        public void RoomSpeed_NeverSlowsTheClock()
        {
            _engine.RoomSpeed = () => 0.2f;
            Assert.That(TicksInOneRealSecond(), Is.EqualTo(TickEngine.TicksPerSecond));
        }

        // Earned x5 in one save, then loaded one that has only earned x2 (slows down); nothing earned (back to 1); never speeds up.
        [TestCase(5f, 2f, 2f, TestName = "LimitSpeedTo_SlowsDownToTheLimit")]
        [TestCase(2f, 1f, 1f, TestName = "LimitSpeedTo_BackToOneWhenNothingIsEarned")]
        [TestCase(2f, 5f, 2f, TestName = "LimitSpeedTo_NeverSpeedsUp")]
        public void LimitSpeedTo_ClampsTheSpeedDownToTheLimit(float speed, float limit, float expected)
        {
            _engine.Speed = speed;
            _engine.LimitSpeedTo(limit);
            Assert.That(_engine.Speed, Is.EqualTo(expected));
        }
    }
}
