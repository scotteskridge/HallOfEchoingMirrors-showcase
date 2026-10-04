namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How long the mouse must rest before a tooltip opens: the full delay for the first one, none
    /// while moving between neighbours (a tip was showing a moment ago), so scanning a list isn't a
    /// slideshow. Plain C# fed the unscaled time; UI only, never gameplay.
    /// </summary>
    public class TipTiming
    {
        private readonly float _delay;
        private readonly float _window;
        private float _lastShown = float.NegativeInfinity;

        /// <param name="delay">Seconds to wait for the first tip.</param>
        /// <param name="window">How long after a tip was last showing the next one opens at once.</param>
        public TipTiming(float delay, float window)
        {
            _delay = delay;
            _window = window;
        }

        /// <summary>The wait before a tip that wants to open at <paramref name="now"/>.</summary>
        public float DelayAt(float now) => now - _lastShown < _window ? 0f : _delay;

        /// <summary>Call every moment a tip is actually on screen.</summary>
        public void NoteShown(float now) => _lastShown = now;
    }
}
