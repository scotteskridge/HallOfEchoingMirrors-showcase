namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// How an item reads on the map: its floor dot takes the kind's colour (MapStyle). Stored in
    /// item assets as numbers: never renumber, only add at the end. Tool is first so it's the
    /// default for an item not given a kind.
    /// </summary>
    public enum ItemKind
    {
        /// <summary>Anything useful that isn't one of the others, e.g. Flint and steel.</summary>
        Tool = 0,
        /// <summary>Gives vitality back, e.g. Wisp, Phial of memory.</summary>
        Restorative = 1,
        /// <summary>Holds off the dark, e.g. Hanging candle.</summary>
        Light = 2,
        /// <summary>Something that matters to her, e.g. Roland's ring, The tome.</summary>
        Keepsake = 3,
    }
}
