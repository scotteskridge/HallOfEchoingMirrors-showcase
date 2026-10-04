using System;
using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The maths of drawing floor piles on the map (MapFloor does the drawing): which dots a room's
    /// floor gives, in what order, which have just grown, and where each sits. Pure, so it's tested
    /// without a scene.
    /// </summary>
    public static class FloorDots
    {
        // The order dots are drawn in, left to right. Not the enum's order: Tool is first there
        // only so it's the default.
        private static readonly ItemKind[] Order = { ItemKind.Restorative, ItemKind.Light, ItemKind.Tool, ItemKind.Keepsake };

        /// <summary>
        /// One dot per kind of item on a floor (6 candles and 2 wisps: two dots), ordered by kind,
        /// then in the floor's own order, so dots don't shuffle as the counts change.
        /// </summary>
        public static void For(IReadOnlyList<(ResourceDefinition item, int amount)> floor, List<(ResourceDefinition item, int amount)> into)
        {
            into.Clear();
            foreach (var kind in Order)
                foreach (var lying in floor)
                    if (lying.amount > 0 && lying.item.kind == kind)
                        into.Add(lying);
            if (into.Count != CountLying(floor))
                throw new InvalidOperationException("FloorDots: an item's kind isn't in the dot order. Add it to FloorDots.Order.");
        }

        private static int CountLying(IReadOnlyList<(ResourceDefinition item, int amount)> floor)
        {
            int count = 0;
            foreach (var lying in floor)
                if (lying.amount > 0)
                    count++;
            return count;
        }

        /// <summary>
        /// Whether more of an item lies here than when last shown (<paramref name="before"/>, by item
        /// or by room and item). A new item doesn't count (its dot appears quietly), and nor does
        /// anything with nothing shown before (a new or loaded game).
        /// </summary>
        public static bool Rose<TKey>(IReadOnlyDictionary<TKey, int> before, TKey item, int now) =>
            before != null && before.TryGetValue(item, out int was) && now > was;

        /// <summary>
        /// Where dot <paramref name="index"/> of a room's row sits: the row hangs from
        /// <paramref name="offset"/> past the room's bottom-left corner, its first dot's left edge
        /// there, the others to its right. (Not the bottom-right: Clara's token and name rest there.)
        /// </summary>
        public static Vector2 Position(Vector2 roomCentre, Vector2 roomSize, int index, float size, float spacing, Vector2 offset)
        {
            var firstDot = roomCentre - roomSize / 2f + offset + new Vector2(size, -size) / 2f;
            return firstDot + new Vector2(index * (size + spacing), 0f);
        }
    }
}
