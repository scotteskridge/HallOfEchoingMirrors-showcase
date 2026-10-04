namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// A reserve Clara spends from: her vitality, or one of her pathos pools. Vitality also drains
    /// on its own (Simulation) and can be restored (restoration items).
    /// </summary>
    public class Pool
    {
        private readonly string _name;

        /// <summary>
        /// A hue pool's name comes from the text file (one source, read each time so a text reload
        /// shows); a pool with no hue keeps the name it was given.
        /// </summary>
        public string Name => Hue == Hue.None ? _name : GameText.HueName(Hue);
        public Hue Hue { get; }
        public float Max { get; private set; }
        public float Current { get; private set; }

        public bool IsEmpty => Current <= 0f;
        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>A new maximum (e.g. Endurance lengthening the vitality bar). What she has stays; it's only trimmed if over.</summary>
        public void SetMax(float max)
        {
            Max = max;
            if (Current > Max)
                Current = Max;
        }

        /// <summary>A hue pool: its name comes from the text file.</summary>
        public Pool(Hue hue, float max)
        {
            Hue = hue;
            Max = max;
            Current = max;
        }

        /// <summary>A pool with no hue (vitality), with its own name.</summary>
        public Pool(string name, float max)
        {
            _name = name;
            Max = max;
            Current = max;
        }

        /// <summary>
        /// Removes up to <paramref name="amount"/>. Returns whatever the pool could not cover,
        /// so an empty pool passes the rest of an action's cost on to vitality.
        /// </summary>
        public float Drain(float amount)
        {
            float taken = amount < Current ? amount : Current;
            Current -= taken;
            return amount - taken;
        }

        /// <summary>Adds up to <paramref name="amount"/>, never past the maximum. Returns how much was added.</summary>
        public float Fill(float amount)
        {
            float room = Max - Current;
            float added = amount < room ? amount : room;
            if (added <= 0f)
                return 0f;
            Current += added;
            return added;
        }
    }
}
