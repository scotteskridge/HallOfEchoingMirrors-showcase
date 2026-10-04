namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// What a tooltip value means, which decides its colour (and, for met and unmet, a second cue so
    /// colour is never the only signal). See <see cref="TipMeanings"/>.
    /// </summary>
    public enum TipMeaning
    {
        Plain,
        Time,
        Vitality,
        Met,
        Unmet,
    }
}
