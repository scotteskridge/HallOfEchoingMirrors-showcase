using System;
using UnityEngine;



namespace HallOfEchoingMirrors
{

    /// <summary>
    /// Turns real time into fixed simulation ticks. All game rules count in ticks,
    /// so a run plays out identically at any speed, and pausing is just "no ticks".
    /// </summary>
    public class TickEngine : MonoBehaviour
    {
        // The clock's resolution, not Clara's pace. Her speed comes from task durations,
        // attributes, and mastery. Keep this an int: tick math elsewhere relies on it.
        public const int TicksPerSecond = 10;
        const float SecondsPerTick = 1f / TicksPerSecond;

        // Stops a slow frame from triggering a huge burst of catch-up ticks.
        const int MaxTicksPerFrame = 100;

        // Players will get x1/x2/x5. Higher speeds are for testing.
        public const float MaxSpeed = 50f;

        [SerializeField, Range(1f, MaxSpeed)] private float _speed = 1f;
        [SerializeField] private bool _paused;

        private float _accumulator;

        /// <summary>Raised once per tick. The simulation subscribes to this.</summary>
        public event Action Ticked;

        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Clamp(value, 1f, MaxSpeed);
        }

        /// <summary>
        /// Extra clock multiplier from the game itself (the room she works in), on top of <see cref="Speed"/>.
        /// Read once per update; null means ×1. Kept out of Speed so the player's tier stays theirs.
        /// </summary>
        public Func<float> RoomSpeed { get; set; }

        /// <summary>Slows down to the given speed if running faster than it; never speeds up.</summary>
        public void LimitSpeedTo(float fastest)
        {
            if (_speed > fastest)
                Speed = fastest;
        }

        public bool Paused
        {
            get => _paused;
            set => _paused = value;
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Runs however many ticks the given real time covers (Update's body, callable from tests).</summary>
        public void Advance(float realSeconds)
        {
            if (_paused)
                return;

            // Placeholder rule: room speed stacks on the player's tier, Feed your hours included (plan 025).
            _accumulator += realSeconds * _speed * Mathf.Max(1f, RoomSpeed?.Invoke() ?? 1f);

            int ticksThisFrame = 0;
            // Re-check _paused each tick: a tick can pause the game (e.g. the queue ran out),
            // and the rest of this frame's ticks must not run after that.
            while (!_paused && _accumulator >= SecondsPerTick && ticksThisFrame < MaxTicksPerFrame)
            {
                _accumulator -= SecondsPerTick;
                ticksThisFrame++;
                RunOneTick();
            }

            if (ticksThisFrame == MaxTicksPerFrame)
                _accumulator = 0f;
        }

        private void RunOneTick()
        {
            Ticked?.Invoke();
        }
    }
}
