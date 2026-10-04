using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>How long a resource lasts.</summary>
    public enum ResourceLifetime
    {
        /// <summary>A tool or stat for this run only, e.g. Tool A or Focus. Gone when the loop ends.</summary>
        ThisRun = 0,
        /// <summary>Kept forever across loops, e.g. Mirrors found.</summary>
        Forever = 1,
        /// <summary>
        /// Something she carries, e.g. Roland's ring. Kept only if she walks out; lost if the
        /// anchor pulls her back. Kept items wait in the real lab until packed for another run.
        /// </summary>
        Carried = 2,
    }

    /// <summary>
    /// Anything Clara can have an amount of: per-run tools and stats, and kept resources such as
    /// Mirrors found. Tasks and switches point at this asset rather than typing its name, so
    /// renaming it here renames it everywhere.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "Hall of Echoing Mirrors/Resource (Tool, Stat or Kept)")]
    public class ResourceDefinition : ContentAsset
    {
        // The Inspector and the Balance Sheet group these settings into sections, showing only the
        // ones an item uses (Editor/ItemSections.cs). A new setting must be added to a section there.
        [Tooltip("What the player sees, e.g. \"Mirrors found\". Change it here and it changes everywhere.")]
        public string displayName = "New Resource";
        [Tooltip("How it reads in \"Pick up ...\" and \"Put down ...\": e.g. \"all wisps\", \"the tome\", \"Roland's ring\". " +
                 "Empty: its name.")]
        public string nameInActions;
        [Tooltip("Optional: a line for its hover pop-up in the inventory, e.g. what it's for.")]
        [TextArea(1, 3)] public string description;
        [Tooltip("How it reads on the map: the colour of its dot when it lies on a room's floor (colours in the Map Style). " +
                 "Tool for anything that isn't restorative, a light or a keepsake.")]
        public ItemKind kind = ItemKind.Tool;

        [Tooltip("This run only (tools and stats), or kept forever (e.g. Mirrors found).")]
        public ResourceLifetime lasts = ResourceLifetime.Forever;
        [Tooltip("Knowledge that only counts towards switches (e.g. Mirrors Found): once every switch watching it " +
                 "has flipped, and no action needs it, it leaves her list of what she knows and is shown in the " +
                 "popover of the room where it's found instead.")]
        public bool hideOnceUsed;

        [Tooltip("The most she can hold. 0 means no limit. Meta currency will raise this later.")]
        [Min(0)] public int startingMax = 10;

        [Tooltip("While she holds it, every second of any action (travel too) costs this much more, from all " +
                 "her pools (vitality while she has none). 0 = no cost. E.g. Roland's ring.")]
        [Min(0f)] public float carryCostPerSecond;
        [Tooltip("A hard cost, like the chase: its Carry Cost comes straight out of vitality, even while " +
                 "carry costs are switched off (LoopSettings). E.g. Roland's ring.")]
        public bool alwaysCharged;

        [Tooltip("An object: each one she holds takes a pocket (wisps, candles, the ring, the tome). Untick for " +
                 "knowledge and states (Understanding, Steady hands). Ignored for things kept forever.")]
        public bool goesInPocket = true;
        [Tooltip("Goes only in a container that holds it (a dense wisp in the earrings), never loose in her pockets or on " +
                 "the floor: with every such container full, she has no room for more.")]
        public bool onlyInContainers;
        [Tooltip("Extra pockets each one gives while she holds it, e.g. a satchel: +5. It doesn't take a pocket itself.")]
        [Min(0)] public int addsPockets;
        [Tooltip("Extra floor space in every room when she holds its maximum, in step with how many she holds " +
                 "(rounded down), e.g. candles lit in A Dark Hall: +5 with all 15, so +1 for every 3 lit (she can see far enough " +
                 "to organise more). With no maximum: this much for each one.")]
        [Min(0)] public int addsFloorSpace;

        [Tooltip("The items it holds, carried outside her pockets. New ones go in here first, then her pockets, " +
                 "then the floor. It takes no pocket itself.")]
        public List<ResourceDefinition> holds = new List<ResourceDefinition>();
        [Tooltip("How many of those items each one holds, all kinds together (e.g. 10 phials).")]
        [Min(0)] public int holdsHowMany;

        [Tooltip("Vitality one of these gives back, over Restore Seconds. 0 = not a restoration item. " +
                 "She uses one on her own as soon as she's missing at least this much, less what Endurance lets her waste.")]
        [Min(0f)] public float restoreVitality;
        [Tooltip("How long one takes to give all of it back, e.g. a wisp: 5 over 5 seconds; a bottled well: 50 over 3.")]
        [Min(0.1f)] public float restoreSeconds = 5f;

        [Tooltip("The drain while she holds Counts Up To of these (0.75 = 25% less), in step with how many she " +
                 "holds, e.g. candles lit. 1 = no effect. The drain still grows at its usual rate.")]
        [Range(0.05f, 1f)] public float drainAtFull = 1f;
        [Tooltip("How many of these count towards Drain At Full; any more don't help (e.g. 15 of up to 25 candles). " +
                 "0 = its maximum.")]
        [Min(0)] public int countsUpTo;

        [Tooltip("Holding this lets the player run the game this fast (e.g. 2 = ×2 on the Speed button). 0 = no " +
                 "effect. Usually something kept forever, given by a one-time action.")]
        [Min(0f)] public float unlocksSpeed;
        [Tooltip("Holding this lets rooms she knows by heart run faster (Feed your hours to the flames gives it). " +
                 "Without it, room speed stays at ×1.")]
        public bool unlocksRoomSpeed;
        [Tooltip("Holding this lets her plan the next run before it begins (the Plan button on the Summary). Separate from " +
                 "Unlocks Room Speed so the two can be given by different things later.")]
        public bool unlocksPlanning;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string NameInActions => string.IsNullOrWhiteSpace(nameInActions) ? DisplayName : nameInActions;
        public bool Restores => restoreVitality > 0f;
        public bool HoldsOffTheDarkness => drainAtFull < 1f;

        /// <summary>What holding this many does to the drain: in step with the amount, up to Counts Up To.</summary>
        public float DrainMultiplierFor(int held)
        {
            int full = countsUpTo > 0 ? countsUpTo : startingMax;
            if (!HoldsOffTheDarkness || held <= 0 || full <= 0)
                return 1f;
            return 1f - (1f - drainAtFull) * Math.Min(held, full) / full;
        }
        /// <summary>The floor space holding this many adds: Adds Floor Space at its maximum, in step (rounded down).</summary>
        public int FloorSpaceFor(int held)
        {
            if (addsFloorSpace <= 0 || held <= 0)
                return 0;
            if (startingMax <= 0)
                return addsFloorSpace * held;
            return addsFloorSpace * Math.Min(held, startingMax) / startingMax;
        }
        public bool IsKept => lasts == ResourceLifetime.Forever;
        public bool IsCarried => lasts == ResourceLifetime.Carried;
        public bool IsPocketed => goesInPocket && !IsKept && addsPockets == 0 && !IsContainer;
        public bool IsContainer => holdsHowMany > 0 && holds.Count > 0;
    }

    /// <summary>An amount of a resource, e.g. +1 Mirrors found, or "needs 2 Focus".</summary>
    [Serializable]
    public class ResourceAmount
    {
        public ResourceDefinition resource;
        public int amount = 1;

        /// <summary>Whether this entry counts: a blank slot or a zero amount in a list gives or takes nothing.</summary>
        public bool IsReal => resource != null && amount > 0;
    }
}
