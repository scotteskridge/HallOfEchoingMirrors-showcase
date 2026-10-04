using System;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The one float power function for game rules, so Core needs no UnityEngine maths. It is
    /// (float)Math.Pow, exactly what Mathf.Pow does, so every balance number comes out the same as before.
    /// </summary>
    public static class CoreMath
    {
        public static float Pow(float value, float exponent) => (float)Math.Pow(value, exponent);
    }
}
