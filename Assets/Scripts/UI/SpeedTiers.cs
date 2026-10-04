using System.Collections.Generic;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Which speed buttons the top bar shows: every speed Clara has earned, plus the next one still
    /// to earn (greyed out, so the player can see there's another). Nothing at all while only ×1 is
    /// earned. Kept apart from RunHeader so the rule can be tested without a scene.
    /// </summary>
    public static class SpeedTiers
    {
        public readonly struct Tier
        {
            public readonly float Speed;
            /// <summary>Not earned yet: shown to tease, can't be chosen.</summary>
            public readonly bool Locked;

            public Tier(float speed, bool locked)
            {
                Speed = speed;
                Locked = locked;
            }
        }

        // Speeds are floats from the Inspector and the unlocks: "the same" means within this.
        private const float Tolerance = 0.001f;

        /// <summary>
        /// Fills <paramref name="into"/> (cleared first) with the tiers to show, in the list's order:
        /// the speeds up to <paramref name="fastestEarned"/>, then the first one above it, locked.
        /// </summary>
        /// <param name="speeds">RunHeader's speeds, slowest first (×1 first).</param>
        /// <param name="into">Reused every frame, so nothing new is made while the game runs.</param>
        public static void Fill(IReadOnlyList<float> speeds, float fastestEarned, List<Tier> into)
        {
            into.Clear();
            int earned = 0;
            foreach (float speed in speeds)
            {
                if (speed <= fastestEarned + Tolerance)
                {
                    into.Add(new Tier(speed, locked: false));
                    earned++;
                }
                else
                {
                    into.Add(new Tier(speed, locked: true));
                    break;
                }
            }

            // Only ×1: there's no choice to make yet, so no buttons (as before the tiers).
            if (earned <= 1)
                into.Clear();
        }
    }
}
