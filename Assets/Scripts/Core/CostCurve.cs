using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The one curve every task's time comes from (plan 030a, formulas doc §4):
    /// family coefficient × standard trip × room step ^ depth × the task's own multiplier.
    /// Pure maths, so the game and the setup tool agree.
    /// </summary>
    public static class CostCurve
    {
        /// <summary>A task's base time in seconds (before skill speed, room modifiers or what she holds).</summary>
        public static float BaseSeconds(float coefficient, float tripSeconds, float roomStep, int depth, float multiplier) =>
            coefficient * tripSeconds * Mathf.Pow(roomStep, depth) * multiplier;

        /// <summary>
        /// The task multiplier that makes <see cref="BaseSeconds"/> come out at <paramref name="seconds"/>:
        /// how the migration gave every task its old time.
        /// </summary>
        public static float MultiplierKeeping(float seconds, float coefficient, float tripSeconds, float roomStep, int depth)
        {
            float curve = BaseSeconds(coefficient, tripSeconds, roomStep, depth, 1f);
            if (curve <= 0f)
                throw new System.ArgumentException("The curve gives no time here (a zero coefficient or trip).");
            return seconds / curve;
        }
    }
}
