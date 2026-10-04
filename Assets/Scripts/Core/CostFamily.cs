using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// A kind of work with a standard length (Traverse, Search, Gather...). A task names its family
    /// and gets a sensible time from the cost curve (<see cref="CostCurve"/>) without a number typed in.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCostFamily", menuName = "Hall of Echoing Mirrors/Cost Family")]
    public class CostFamily : ContentAsset
    {
        public string displayName = "New Family";

        [Tooltip("How long this kind of work takes, as a share of the standard trip (Loop Settings: Standard Trip Seconds) " +
                 "in the first room. 1 = as long as a trip; 0.2 = a fifth of one. Deeper rooms multiply it by Room Step each.")]
        [Min(0.001f)] public float durationCoefficient = 1f;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
