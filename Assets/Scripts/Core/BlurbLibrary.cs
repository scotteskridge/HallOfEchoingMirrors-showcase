using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>Every blurb bucket, and how often Clara speaks. Referenced from Game Content.</summary>
    [CreateAssetMenu(fileName = "BlurbLibrary", menuName = "Hall of Echoing Mirrors/Blurb Library")]
    public class BlurbLibrary : ScriptableObject
    {
        [Tooltip("Shortest wait between blurbs, in real seconds (it stops while paused, and does not speed up with the game).")]
        [Min(0.5f)] public float minSeconds = 3f;
        [Tooltip("Longest wait between blurbs, in real seconds.")]
        [Min(0.5f)] public float maxSeconds = 8f;
        [Tooltip("At high game speeds, never more than one blurb per this many real seconds. Extras are skipped.")]
        [Min(0f)] public float minRealSecondsApart = 2f;
        [Tooltip("How long a bucket that just spoke steps aside for others, in real seconds. " +
                 "A bucket with its own Cooldown Seconds ignores this.")]
        [Min(0f)] public float defaultCooldownSeconds = 20f;

        public List<BlurbBucket> buckets = new List<BlurbBucket>();
    }
}
