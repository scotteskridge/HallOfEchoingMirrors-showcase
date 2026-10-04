using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The maths of drawing the queue's route on the map (MapRoute does the drawing): which legs
    /// to draw, which numbers each room's badge shows, how much of a leg is left, and where
    /// Clara's token sits. Pure, so it's tested without a scene.
    /// </summary>
    public static class RouteLayout
    {
        /// <summary>
        /// One leg per trip between stops, in order. A way walked more than once (either direction)
        /// draws once, the first time: the badges carry the order.
        /// </summary>
        public static void Legs(IReadOnlyList<QueueStop> stops, List<(NodeDefinition from, NodeDefinition to)> into)
        {
            into.Clear();
            for (int i = 1; i < stops.Count; i++)
            {
                var from = stops[i - 1].Room;
                var to = stops[i].Room;
                if (!into.Exists(leg => SameWay(leg.from, leg.to, from, to)))
                    into.Add((from, to));
            }
        }

        /// <summary>Whether two legs join the same two rooms, in either direction.</summary>
        public static bool SameWay(NodeDefinition a1, NodeDefinition b1, NodeDefinition a2, NodeDefinition b2) =>
            (a1 == a2 && b1 == b2) || (a1 == b2 && b1 == a2);

        /// <summary>
        /// One badge per room the route stops at, in the order first reached, listing every stop
        /// number there (a room visited first and third lists 1 and 3). None without a trip: a lone
        /// "1" on her room says nothing the token doesn't.
        /// </summary>
        public static void Badges(IReadOnlyList<QueueStop> stops, List<(NodeDefinition room, List<int> numbers)> into)
        {
            into.Clear();
            if (stops.Count < 2)
                return;
            foreach (var stop in stops)
            {
                int index = into.FindIndex(badge => badge.room == stop.Room);
                if (index < 0)
                    into.Add((stop.Room, new List<int> { stop.Number }));
                else
                    into[index].numbers.Add(stop.Number);
            }
        }

        /// <summary>The part of the leg from a to b between two fractions of the way (0 = a, 1 = b).</summary>
        public static (Vector2 start, Vector2 end) Part(Vector2 a, Vector2 b, float from, float to) =>
            (Vector2.Lerp(a, b, from), Vector2.Lerp(a, b, to));

        /// <summary>How far along the leg from a to b a point is (0 to 1), measured straight across to the leg.</summary>
        public static float FractionAlong(Vector2 a, Vector2 b, Vector2 point)
        {
            Vector2 leg = b - a;
            float lengthSquared = leg.sqrMagnitude;
            return lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, leg) / lengthSquared) : 1f;
        }

        /// <summary>
        /// Where the token belongs: at <paramref name="rest"/> (an offset from a room's centre, by its
        /// corner) from her room, or on a trip that far along the way to <paramref name="heading"/>.
        /// </summary>
        public static Vector2 TokenPoint(Vector2 here, Vector2? heading, float progress, Vector2 rest) =>
            (heading.HasValue ? Vector2.Lerp(here, heading.Value, Mathf.Clamp01(progress)) : here) + rest;
    }
}
