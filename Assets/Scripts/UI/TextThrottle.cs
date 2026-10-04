using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Says when a panel should build its words afresh: a few times a second, and at once after
    /// <see cref="MarkDirty"/> (the queue changed, the wording was reloaded). Building text makes new strings,
    /// so doing it every frame for a panel nobody is reading that fast only feeds the garbage collector.
    /// Bars and other numbers that must look smooth are not throttled: set those every frame.
    /// </summary>
    public class TextThrottle
    {
        /// <summary>Real seconds between rebuilds (the same beat the map and room popover use).</summary>
        public const float Seconds = 0.2f;

        private bool _dirty = true;
        private float _nextAt;

        /// <summary>Build the words at the next <see cref="Due"/>, whatever the timer says.</summary>
        public void MarkDirty() => _dirty = true;

        /// <summary>True when it's time to rebuild; asking starts the next wait.</summary>
        public bool Due()
        {
            if (!_dirty && Time.unscaledTime < _nextAt)
                return false;
            _dirty = false;
            _nextAt = Time.unscaledTime + Seconds;
            return true;
        }
    }
}
