using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Chooses what Clara says: first the bucket (the highest priority that fits the moment,
    /// equal priorities in random order, each allowed to stay quiet by its chance), then the next
    /// line from that bucket's shuffle-bag, so no line repeats until the whole bucket has been used.
    /// </summary>
    public class BlurbPicker
    {
        private readonly System.Random _random;
        private readonly Dictionary<BlurbBucket, List<int>> _bags = new Dictionary<BlurbBucket, List<int>>();
        private readonly Dictionary<BlurbBucket, int> _lastShown = new Dictionary<BlurbBucket, int>();
        // Keeps the Simulation it fired in so a loaded save or new game starts fresh; a replaced Simulation is held
        // only until that bucket next fires (minor, and the picker is small).
        private readonly Dictionary<BlurbBucket, (Simulation sim, int loop, float seconds)> _firedAt = new Dictionary<BlurbBucket, (Simulation sim, int loop, float seconds)>();

        /// <param name="seed">Fixed for tests; leave out for a different order each game.</param>
        public BlurbPicker(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// A line for this moment (the timer's, or an action starting), or null if no bucket fits
        /// (or all chose to stay quiet). <paramref name="nowRealSeconds"/> is a clock in real seconds
        /// (not game time, so cooldowns stay readable at any game speed); it only has to keep rising.
        /// </summary>
        public string Pick(BlurbLibrary library, Simulation sim, float nowRealSeconds, BlurbMoment moment = BlurbMoment.During)
        {
            if (library == null || sim == null)
                return null;

            var activity = ActivityOf(sim);
            int loop = sim.Persistent.LoopNumber;
            var fitting = new List<BlurbBucket>();
            foreach (var bucket in library.buckets)
                if (bucket != null && bucket.enabled && bucket.moment == moment && bucket.lines.Count > 0 &&
                    Fits(bucket, sim, activity) && !OnCooldown(bucket, library, sim, loop, nowRealSeconds))
                    fitting.Add(bucket);

            // Highest priority first; within a priority, a fresh random order each time.
            Shuffle(fitting);
            fitting.Sort((a, b) => b.priority.CompareTo(a.priority)); // List.Sort is unstable, which only adds to the mixing

            foreach (var bucket in fitting)
                if (_random.NextDouble() < ChanceOf(bucket, loop))
                {
                    _firedAt[bucket] = (sim, loop, nowRealSeconds);
                    return Next(bucket);
                }
            return null;
        }

        /// <summary>
        /// Whether a bucket that just spoke is still sitting out its cooldown (in real seconds). A
        /// fitting, top-priority bucket on cooldown is skipped even so; picking falls through to the
        /// next one down. A new run (a different loop number) always finds it free again, whatever it
        /// did last time; so does a different Simulation altogether (a loaded save, a new game), which
        /// this picker outlives (BlurbTeller keeps one for as long as the game runs).
        /// </summary>
        private bool OnCooldown(BlurbBucket bucket, BlurbLibrary library, Simulation sim, int loop, float nowRealSeconds)
        {
            if (!_firedAt.TryGetValue(bucket, out var firedAt) || firedAt.sim != sim || firedAt.loop != loop)
                return false;
            float cooldown = bucket.cooldownSeconds > 0f ? bucket.cooldownSeconds : library.defaultCooldownSeconds;
            const float epsilon = 0.001f; // float error shouldn't extend the cooldown
            return nowRealSeconds - firedAt.seconds < cooldown - epsilon;
        }

        /// <summary>What Clara is doing right now, as a blurb sees it.</summary>
        public static BlurbActivity ActivityOf(Simulation sim)
        {
            var loop = sim.Loop;
            if (loop.CurrentTask == null)
                return BlurbActivity.Idle;
            if (loop.CurrentDestination != null)
                return BlurbActivity.Travelling;
            if (loop.CurrentTask == sim.ExploreVerb)
                return BlurbActivity.Exploring;
            if (loop.CurrentTask.kind == TaskKind.Search)
                return BlurbActivity.Searching;
            return BlurbActivity.Working;
        }

        public static bool Fits(BlurbBucket bucket, Simulation sim, BlurbActivity now)
        {
            if (bucket.activities != BlurbActivity.Any && (bucket.activities & now) == 0)
                return false;
            if (bucket.tasks.Count > 0 && !bucket.tasks.Contains(sim.Loop.CurrentTask))
                return false;
            if (bucket.topics.Count > 0 && (sim.Loop.CurrentTask == null || !bucket.topics.Contains(sim.Loop.CurrentTask.blurbTopic)))
                return false;

            var room = sim.Loop.CurrentNode;
            if (bucket.rooms.Count > 0 && !bucket.rooms.Contains(room))
                return false;
            if (bucket.roomKinds.Count > 0 && (room == null || !bucket.roomKinds.Contains(room.kind)))
                return false;

            int loop = sim.Persistent.LoopNumber;
            if (bucket.fromLoop > 0 && loop < bucket.fromLoop)
                return false;
            if (bucket.toLoop > 0 && loop > bucket.toLoop)
                return false;

            foreach (var @switch in bucket.needsFlipped)
                if (@switch != null && !sim.IsFlipped(@switch))
                    return false;
            foreach (var @switch in bucket.endsWhenFlipped)
                if (@switch != null && sim.IsFlipped(@switch))
                    return false;
            foreach (var need in bucket.needsHeld)
                if (need.resource != null && sim.AmountOf(need.resource) < need.amount)
                    return false;
            foreach (var resource in bucket.needsNotHeld)
                if (resource != null && sim.AmountOf(resource) > 0)
                    return false;

            if (bucket.vitalityBelow > 0f && sim.Loop.Vitality.Fraction >= bucket.vitalityBelow)
                return false;
            return true;
        }

        /// <summary>The bucket's chance this loop, falling away evenly between the taper loops.</summary>
        public static float ChanceOf(BlurbBucket bucket, int loop)
        {
            if (bucket.silentFromLoop > 0 && loop >= bucket.silentFromLoop)
                return 0f;
            if (bucket.taperFromLoop > 0 && bucket.silentFromLoop > bucket.taperFromLoop && loop >= bucket.taperFromLoop)
                return bucket.chance * (bucket.silentFromLoop - loop) / (float)(bucket.silentFromLoop - bucket.taperFromLoop);
            return bucket.chance;
        }

        /// <summary>The next line from the bucket's shuffle-bag, refilling it (shuffled) when it runs out.</summary>
        public string Next(BlurbBucket bucket)
        {
            if (bucket.lines.Count == 0)
                return null;
            if (!_bags.TryGetValue(bucket, out var bag) || bag.Count == 0)
            {
                bag = new List<int>();
                for (int i = 0; i < bucket.lines.Count; i++)
                    bag.Add(i);
                Shuffle(bag);
                // Don't let the first line of a new round be the one just heard.
                if (bag.Count > 1 && _lastShown.TryGetValue(bucket, out int last) && bag[bag.Count - 1] == last)
                    (bag[0], bag[bag.Count - 1]) = (bag[bag.Count - 1], bag[0]);
                _bags[bucket] = bag;
            }

            int index = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            if (index >= bucket.lines.Count) // the lines were re-imported shorter mid-game
            {
                _bags.Remove(bucket);
                return Next(bucket);
            }
            _lastShown[bucket] = index;
            return bucket.lines[index];
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
