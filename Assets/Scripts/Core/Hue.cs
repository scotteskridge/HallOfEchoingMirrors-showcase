namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The seven hues of pathomancy, named for their gems, in wheel order (GDD v0.3 §5). None is
    /// for pools that aren't a hue (vitality, the generic greybox Magic pool).
    /// </summary>
    /// <remarks>
    /// Save files will store these as numbers, so once the game ships, never reorder or
    /// renumber them. Only add new values at the end.
    /// </remarks>
    public enum Hue
    {
        None = 0,
        Amber = 1,
        Citrine = 2,
        Emerald = 3,
        Sapphire = 4,
        Iolite = 5,
        Amethyst = 6,
        Ruby = 7,
    }
}
