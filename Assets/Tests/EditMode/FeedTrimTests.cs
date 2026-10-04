using HallOfEchoingMirrors.UI;
using NUnit.Framework;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// The Story box's list (ambient lines and milestone cards together): which entries a long
    /// run throws away, and which fade.
    /// </summary>
    public class FeedTrimTests
    {
        private const bool M = true;   // a milestone card
        private const bool A = false;  // an Around her line

        [Test]
        public void Trim_DropsOldestAmbientFirst()
        {
            // Oldest first: M A A M A, with room for 3.
            var dropped = FeedTrim.Dropped(new[] { M, A, A, M, A }, 3);

            CollectionAssert.AreEqual(new[] { 1, 2 }, dropped);
        }

        [Test]
        public void Trim_NeverDropsMilestones()
        {
            CollectionAssert.AreEqual(new[] { 1 }, FeedTrim.Dropped(new[] { M, A, M, M }, 2),
                "Only the one ambient line goes, even though that leaves more than 2.");
            CollectionAssert.IsEmpty(FeedTrim.Dropped(new[] { M, M, M }, 1));
            CollectionAssert.IsEmpty(FeedTrim.Dropped(new[] { A, A }, 5));
        }

        [Test]
        public void Faded_OnlyAmbientBeyondNewestFour()
        {
            // Oldest first: A A M A A M A A. An ambient line's age counts only newer ambient lines
            // (0 = newest); milestones have none (-1).
            var entries = new[] { A, A, M, A, A, M, A, A };
            var ages = FeedTrim.AmbientAges(entries);

            CollectionAssert.AreEqual(new[] { 5, 4, -1, 3, 2, -1, 1, 0 }, ages);
            bool[] fades = new bool[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                fades[i] = FeedTrim.Fades(ages[i], 4);
            CollectionAssert.AreEqual(new[] { true, true, false, false, false, false, false, false }, fades,
                "Milestones never fade; ambient lines fade beyond the newest four of them.");
        }

        [Test]
        public void Expired_DropsAmbientPastVisibleWindow()
        {
            // Oldest first: A A M A A, visible window of 2 (ages 0 and 1 stay).
            var entries = new[] { A, A, M, A, A };

            CollectionAssert.AreEqual(new[] { 0, 1 }, FeedTrim.Expired(entries, 2));
        }

        [Test]
        public void Expired_NeverDropsMilestones()
        {
            CollectionAssert.IsEmpty(FeedTrim.Expired(new[] { M, M, M }, 0));
        }
    }
}
