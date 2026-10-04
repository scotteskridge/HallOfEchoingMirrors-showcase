using System.Collections.Generic;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Which speed buttons the top bar shows: the earned speeds, plus the next one to earn.</summary>
    public class SpeedTiersTests
    {
        private static readonly float[] Speeds = { 1f, 2f, 5f };

        private static List<SpeedTiers.Tier> Shown(float fastestEarned)
        {
            var tiers = new List<SpeedTiers.Tier>();
            SpeedTiers.Fill(Speeds, fastestEarned, tiers);
            return tiers;
        }

        [Test]
        public void OnlyX1Earned_ShowsNothing()
        {
            Assert.IsEmpty(Shown(1f));
        }

        [Test]
        public void SomeEarned_ShowsEarnedPlusOneLocked()
        {
            var tiers = Shown(2f);

            Assert.AreEqual(3, tiers.Count);
            Assert.AreEqual(1f, tiers[0].Speed);
            Assert.IsFalse(tiers[0].Locked);
            Assert.AreEqual(2f, tiers[1].Speed);
            Assert.IsFalse(tiers[1].Locked);
            Assert.AreEqual(5f, tiers[2].Speed);
            Assert.IsTrue(tiers[2].Locked);
        }

        [Test]
        public void AllEarned_NoLockedTier()
        {
            var tiers = Shown(5f);

            Assert.AreEqual(3, tiers.Count);
            Assert.IsFalse(tiers.Exists(t => t.Locked));
        }

        [Test]
        public void EarnedBetweenListedSpeeds_LocksTheNextListedOne()
        {
            // Something that unlocks ×3 earns ×2 (the fastest listed speed within it), not ×5.
            var tiers = Shown(3f);

            Assert.AreEqual(3, tiers.Count);
            Assert.IsFalse(tiers[1].Locked);
            Assert.IsTrue(tiers[2].Locked);
            Assert.AreEqual(5f, tiers[2].Speed);
        }

        [Test]
        public void FillingAgain_ReplacesTheLastList()
        {
            var tiers = new List<SpeedTiers.Tier>();
            SpeedTiers.Fill(Speeds, 5f, tiers);
            SpeedTiers.Fill(Speeds, 1f, tiers);

            Assert.IsEmpty(tiers);
        }
    }
}
