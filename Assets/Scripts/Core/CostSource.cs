namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// What a task's cost can be paid from: Clara's own vitality, or one of the hue pools.
    /// The hue values deliberately match <see cref="Hue"/> (Amber = 1 ... Ruby = 7), so the two
    /// convert directly. Like Hue, never renumber these once saves exist.
    /// </summary>
    public enum CostSource
    {
        Vitality = 0,
        Amber = 1,
        Citrine = 2,
        Emerald = 3,
        Sapphire = 4,
        Iolite = 5,
        Amethyst = 6,
        Ruby = 7,
        /// <summary>
        /// Split evenly across every pool Clara has this run (vitality if she has none), e.g. hall
        /// travel, or the prologue. Not a hue, so it sits apart from the hue numbers.
        /// </summary>
        AllPools = 100,
    }

    public static class CostSourceExtensions
    {
        public static Hue ToHue(this CostSource source) => (Hue)(int)source;
        public static CostSource ToCostSource(this Hue hue) => (CostSource)(int)hue;
    }
}
