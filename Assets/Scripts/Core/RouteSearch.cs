using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Finds the shortest walk between two rooms over the ways she can take this run (open and
    /// found). Shortest means fewest seconds of walking; ties go to the fewest rooms, then to the
    /// order the ways are listed, so the answer never depends on luck.
    /// </summary>
    public static class RouteSearch
    {
        private const float Epsilon = 0.0001f;

        /// <summary>
        /// The rooms to walk to, in order, ending at <paramref name="to"/> (not including
        /// <paramref name="from"/>). Empty if the rooms are the same or there's no way; use
        /// <see cref="TryFind"/> to tell those apart.
        /// </summary>
        public static List<NodeDefinition> Find(Simulation sim, NodeDefinition from, NodeDefinition to)
        {
            TryFind(sim, from, to, out var walk);
            return walk;
        }

        /// <summary>
        /// The working lists of one search, kept by a caller that searches often (every frame) so each
        /// search doesn't make five new lists. One search at a time may use a scratch.
        /// </summary>
        public sealed class Scratch
        {
            internal readonly List<NodeDefinition> Rooms = new List<NodeDefinition>();
            internal readonly List<float> Seconds = new List<float>();
            internal readonly List<int> Hops = new List<int>();
            internal readonly List<int> Before = new List<int>();
            internal readonly List<bool> Done = new List<bool>();

            internal void Start(NodeDefinition from)
            {
                Rooms.Clear();
                Seconds.Clear();
                Hops.Clear();
                Before.Clear();
                Done.Clear();
                Rooms.Add(from);
                Seconds.Add(0f);
                Hops.Add(0);
                Before.Add(-1);
                Done.Add(false);
            }
        }

        /// <summary>True with the walk if <paramref name="to"/> can be reached from <paramref name="from"/> by another room.</summary>
        public static bool TryFind(Simulation sim, NodeDefinition from, NodeDefinition to, out List<NodeDefinition> walk) =>
            TryFind(sim, from, to, out walk, new Scratch());

        /// <summary>As above, reusing <paramref name="scratch"/>'s lists for the search itself (the walk returned is always a new list the caller owns).</summary>
        public static bool TryFind(Simulation sim, NodeDefinition from, NodeDefinition to, out List<NodeDefinition> walk, Scratch scratch)
        {
            walk = new List<NodeDefinition>();
            if (sim == null || from == null || to == null || from == to || sim.TravelVerb == null)
                return false;

            // Dijkstra over a handful of rooms: a list scan is plenty.
            scratch.Start(from);
            var rooms = scratch.Rooms;
            var seconds = scratch.Seconds;
            var hops = scratch.Hops;
            var before = scratch.Before;
            var done = scratch.Done;

            while (true)
            {
                int current = -1;
                for (int i = 0; i < rooms.Count; i++)
                    if (!done[i] && (current < 0 || IsBetter(seconds[i], hops[i], seconds[current], hops[current])))
                        current = i;
                if (current < 0)
                    return false;
                if (rooms[current] == to)
                    break;

                done[current] = true;
                foreach (var next in sim.DestinationsFrom(rooms[current]))
                {
                    float total = seconds[current] + sim.PriceOf(sim.TravelVerb, rooms[current], next).Seconds;
                    int totalHops = hops[current] + 1;
                    int known = rooms.IndexOf(next);
                    if (known < 0)
                    {
                        rooms.Add(next);
                        seconds.Add(total);
                        hops.Add(totalHops);
                        before.Add(current);
                        done.Add(false);
                    }
                    else if (!done[known] && IsBetter(total, totalHops, seconds[known], hops[known]))
                    {
                        seconds[known] = total;
                        hops[known] = totalHops;
                        before[known] = current;
                    }
                }
            }

            for (int at = rooms.IndexOf(to); at > 0; at = before[at])
                walk.Insert(0, rooms[at]);
            return true;
        }

        // Strictly better only, so a tie keeps whatever was found first (the earlier-listed way).
        private static bool IsBetter(float seconds, int hops, float otherSeconds, int otherHops)
        {
            if (seconds < otherSeconds - Epsilon)
                return true;
            return seconds <= otherSeconds + Epsilon && hops < otherHops;
        }
    }
}
