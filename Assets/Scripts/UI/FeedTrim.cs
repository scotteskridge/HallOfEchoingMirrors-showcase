using System.Collections.Generic;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Story box's rules for a long run, kept apart from the display so they can be tested: which
    /// entries are thrown away, and which fade. Only *Around her* lines are ever dropped or faded;
    /// milestone cards stay for the whole run. Entries are listed oldest first.
    /// </summary>
    public static class FeedTrim
    {
        /// <summary>
        /// The entries (by index) to throw away so at most <paramref name="max"/> remain: the oldest
        /// ambient lines. Never a milestone, even if that leaves more than the cap.
        /// </summary>
        public static List<int> Dropped(IReadOnlyList<bool> isMilestone, int max)
        {
            var dropped = new List<int>();
            int over = isMilestone.Count - max;
            for (int i = 0; i < isMilestone.Count && dropped.Count < over; i++)
                if (!isMilestone[i])
                    dropped.Add(i);
            return dropped;
        }

        /// <summary>
        /// Each entry's age among the ambient lines: 0 for the newest, 1 for the one before it, and so
        /// on (milestones in between don't count). -1 for a milestone, which has no age: it never fades.
        /// </summary>
        public static int[] AmbientAges(IReadOnlyList<bool> isMilestone)
        {
            var ages = new int[isMilestone.Count];
            int age = 0;
            for (int i = isMilestone.Count - 1; i >= 0; i--)
                ages[i] = isMilestone[i] ? -1 : age++;
            return ages;
        }

        /// <summary>Whether an entry of this age (AmbientAges) is fading: an ambient line older than the newest few.</summary>
        public static bool Fades(int age, int fullStrength) => age >= fullStrength;

        /// <summary>
        /// The entries (by index) past the visible window: ambient lines old enough to have faded fully
        /// out of sight, so they're gone rather than left dim. Milestones are never returned; they have
        /// no age and stay for the whole run.
        /// </summary>
        public static List<int> Expired(IReadOnlyList<bool> isMilestone, int maxVisible)
        {
            var ages = AmbientAges(isMilestone);
            var expired = new List<int>();
            for (int i = 0; i < isMilestone.Count; i++)
                if (!isMilestone[i] && ages[i] >= maxVisible)
                    expired.Add(i);
            return expired;
        }
    }
}
