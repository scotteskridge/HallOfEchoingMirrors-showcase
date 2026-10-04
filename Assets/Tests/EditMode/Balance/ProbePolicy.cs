namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// One way of playing for the balance probe: the knobs a route reads to play tidily or loosely.
    /// A route ignores knobs it has no use for. See docs/balance-tools.md.
    /// </summary>
    public struct ProbePolicy
    {
        /// <summary>Short label for reports, e.g. "C: 15 hall candles, 10 phials".</summary>
        public string Name;
        /// <summary>Light candles in A Dark Hall (they ease the drain).</summary>
        public bool LightTheDarkHall;
        /// <summary>How many Dark Hall candles to light (15 is the most that helps).</summary>
        public int HallCandles;
        /// <summary>Make the pouch and fill phials of memory before going deep.</summary>
        public bool FillPhials;
        /// <summary>How many phials to fill before going on.</summary>
        public int Phials;

        public override string ToString() => Name;
    }
}
