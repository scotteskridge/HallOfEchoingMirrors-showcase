using System;
using HallOfEchoingMirrors.Core;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// The one list of what an item's settings are for, in groups: the Inspector and the Balance
    /// Sheet both show an item only the groups it uses. Every setting of ResourceDefinition must be
    /// in exactly one group (ItemSectionsTests checks), so a new setting is never silently hidden.
    /// </summary>
    public static class ItemSections
    {
        public class Section
        {
            public string Title { get; }
            /// <summary>What it's for, shown when the section is unused and closed.</summary>
            public string Hint { get; }
            /// <summary>Whether an item uses this section (null: every item does).</summary>
            public Func<ResourceDefinition, bool> UsedBy { get; }
            /// <summary>The fields, by their names in ResourceDefinition.</summary>
            public string[] Fields { get; }

            public Section(string title, string hint, Func<ResourceDefinition, bool> usedBy, params string[] fields)
            {
                Title = title;
                Hint = hint;
                UsedBy = usedBy;
                Fields = fields;
            }

            public bool IsUsedBy(ResourceDefinition item) => UsedBy == null || UsedBy(item);
        }

        public static readonly Section[] All =
        {
            new Section("What it is", null, null,
                "displayName", "description", "lasts", "startingMax", "goesInPocket"),
            new Section("An object in her pockets", "Tick Goes In Pocket (and make it last this run or be carried).",
                item => item.IsPocketed,
                "nameInActions", "kind", "onlyInContainers"),
            new Section("Knowledge", "Mirrors Found: leaves her list once every switch watching it has flipped.",
                item => item.IsKept || item.hideOnceUsed,
                "hideOnceUsed"),
            new Section("Restores vitality", "Wisps and phials: vitality back over a few seconds.",
                item => item.restoreVitality > 0f,
                "restoreVitality", "restoreSeconds"),
            new Section("A container", "A pouch: holds certain items outside her pockets.",
                item => item.holdsHowMany > 0 || item.holds.Count > 0,
                "holds", "holdsHowMany"),
            new Section("Adds room", "A satchel (pockets), lit candles (floor space).",
                item => item.addsPockets > 0 || item.addsFloorSpace > 0,
                "addsPockets", "addsFloorSpace"),
            new Section("Costs her while carried", "Roland's ring: a drain every second she holds it.",
                item => item.carryCostPerSecond > 0f || item.alwaysCharged,
                "carryCostPerSecond", "alwaysCharged"),
            new Section("Eases the drain", "Lit candles: the drain lower while she holds them.",
                item => item.drainAtFull < 1f || item.countsUpTo > 0,
                "drainAtFull", "countsUpTo"),
            new Section("Unlocks a game speed", "A reward: the Speed button can go this fast.",
                item => item.unlocksSpeed > 0f,
                "unlocksSpeed"),
            new Section("Unlocks room speed", "A reward: rooms known by heart run faster once she holds this.",
                item => item.unlocksRoomSpeed,
                "unlocksRoomSpeed"),
            new Section("Unlocks planning", "A reward: she can plan the next run before it begins once she holds this.",
                item => item.unlocksPlanning,
                "unlocksPlanning"),
        };
    }
}
