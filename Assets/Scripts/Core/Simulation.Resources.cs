using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Resources: per-run tools and stats, and kept ones (e.g. Mirrors found).
    public partial class Simulation
    {
        /// <summary>How many she has now: this run's count for tools and stats, the kept count otherwise.</summary>
        public int AmountOf(ResourceDefinition resource)
        {
            if (resource == null)
                return 0;
            return resource.IsKept ? Persistent.ResourceOf(resource) : Loop.CountOf(resource);
        }

        /// <summary>How many she can hold, or int.MaxValue if the resource has no maximum.</summary>
        public int ResourceCapOf(ResourceDefinition resource)
        {
            if (resource == null || resource.startingMax <= 0)
                return int.MaxValue;
            return resource.startingMax + (Persistent.ResourceCapBonus.TryGetValue(resource, out int bonus) ? bonus : 0);
        }

        /// <summary>How many pocketed items she holds this run, all kinds together.</summary>
        public int PocketsUsed
        {
            get
            {
                FillContainers();
                return PocketsUsedAfterFill();
            }
        }

        /// <summary>PocketsUsed for a caller that has just called FillContainers.</summary>
        private int PocketsUsedAfterFill()
        {
            int used = 0;
            foreach (var entry in Loop.ToolsAndStats)
                if (entry.Key.IsPocketed)
                    used += entry.Value - Contained(entry.Key);
            return used;
        }

        /// <summary>
        /// What she carries, in the order it's shown: what's in her pockets (carried items first, as
        /// they're most at risk, then this run's), then objects she carries outside them that add
        /// pockets (a satchel), then each container she holds (kept ones too) followed by what's in it.
        /// Knowledge, other kept things and floor piles aren't listed. Clears the list first.
        /// </summary>
        public void CarriedItems(List<CarriedEntry> into)
        {
            into.Clear();
            FillContainers();
            AddPocketed(into, carried: true);
            AddPocketed(into, carried: false);
            foreach (var entry in Loop.ToolsAndStats)
                if (AddsPocketsOutsideThem(entry.Key) && entry.Value > 0)
                    into.Add(new CarriedEntry(entry.Key, entry.Value, null));
            foreach (var entry in Loop.ToolsAndStats)
                AddContainer(into, entry.Key, entry.Value);
            foreach (var entry in Persistent.Resources)
                AddContainer(into, entry.Key, entry.Value);
        }

        /// <summary>
        /// Knowledge and progress, in the order it's shown: this run's things she doesn't carry (CarriedItems
        /// lists those), then what she keeps, then one in use with none left (amount 0). A kept container
        /// (the earrings) is carried, not listed here; knowledge that has done its job (Mirrors Found, once its
        /// ways are open) is shown by its room instead. Clears the list first.
        /// </summary>
        public void KnowledgeItems(List<(ResourceDefinition item, int amount)> into)
        {
            into.Clear();
            foreach (var entry in Loop.ToolsAndStats)
                if (entry.Value > 0 && !entry.Key.IsPocketed && !entry.Key.IsContainer && !AddsPocketsOutsideThem(entry.Key))
                    into.Add((entry.Key, entry.Value));
            foreach (var entry in Persistent.Resources)
                if (entry.Value > 0 && !entry.Key.IsContainer && !HasDoneItsJob(entry.Key))
                    into.Add((entry.Key, entry.Value));
            foreach (var restoring in Loop.Restorings)
                if (!restoring.Item.IsPocketed && !Lists(into, restoring.Item))
                    into.Add((restoring.Item, 0));
        }

        // A loop, not List.Exists: this runs every frame, and a capturing lambda would allocate.
        private static bool Lists(List<(ResourceDefinition item, int amount)> known, ResourceDefinition item)
        {
            foreach (var entry in known)
                if (entry.item == item)
                    return true;
            return false;
        }

        // An object carried outside her pockets that adds pockets (a satchel): CarriedItems lists it, so knowledge doesn't.
        private static bool AddsPocketsOutsideThem(ResourceDefinition item) =>
            item.goesInPocket && !item.IsKept && item.addsPockets > 0 && !item.IsContainer;

        private void AddContainer(List<CarriedEntry> into, ResourceDefinition container, int held)
        {
            if (!container.IsContainer || held <= 0)
                return;
            into.Add(new CarriedEntry(container, held, null));
            foreach (var (item, amount) in ContentsAfterFill(container).contents)
                into.Add(new CarriedEntry(item, amount, container));
        }

        private void AddPocketed(List<CarriedEntry> into, bool carried)
        {
            foreach (var entry in Loop.ToolsAndStats)
            {
                if (!entry.Key.IsPocketed || entry.Key.IsCarried != carried)
                    continue;
                int inPockets = entry.Value - Contained(entry.Key);
                if (inPockets > 0)
                    into.Add(new CarriedEntry(entry.Key, inPockets, null));
            }
        }

        // ---------- Containers (a pouch of phials) ----------

        // Worked out afresh each time they're asked about: how many of each item her containers hold,
        // and each container's room left. Filled in the order she came by them (this run's, then kept
        // ones such as the earrings), each with its items in its own order. A kept container is kept;
        // what's in it is this run's, like anything else she holds.
        private readonly Dictionary<ResourceDefinition, int> _contained = new Dictionary<ResourceDefinition, int>();
        private readonly Dictionary<ResourceDefinition, int> _containerRoom = new Dictionary<ResourceDefinition, int>();
        // The containers she holds, in the order she came by them (what ContainerFor reads).
        private readonly List<ResourceDefinition> _containerOrder = new List<ResourceDefinition>();
        // What each container holds, kept between fills (cleared, not rebuilt) so the pockets overlay
        // asking every frame allocates nothing. Only containers in _containerRoom are current.
        private readonly Dictionary<ResourceDefinition, List<(ResourceDefinition item, int amount)>> _containerContents =
            new Dictionary<ResourceDefinition, List<(ResourceDefinition item, int amount)>>();

        private void FillContainers()
        {
            _contained.Clear();
            _containerRoom.Clear();
            _containerOrder.Clear();
            foreach (var list in _containerContents.Values)
                list.Clear();
            foreach (var entry in Loop.ToolsAndStats)
                FillContainer(entry.Key, entry.Value);
            foreach (var entry in Persistent.Resources)
                FillContainer(entry.Key, entry.Value);
        }

        private void FillContainer(ResourceDefinition container, int held)
        {
            if (!container.IsContainer || held <= 0)
                return;
            if (!_containerContents.TryGetValue(container, out var inside))
                _containerContents[container] = inside = new List<(ResourceDefinition item, int amount)>();
            if (!_containerOrder.Contains(container))
                _containerOrder.Add(container);
            int room = container.holdsHowMany * held;
            foreach (var item in container.holds)
            {
                if (item == null || room <= 0)
                    continue;
                int loose = AmountOf(item) - Contained(item);
                int put = Math.Min(room, Math.Max(0, loose));
                _contained[item] = Contained(item) + put;
                room -= put;
                if (put > 0)
                    inside.Add((item, put));
            }
            _containerRoom[container] = room;
        }

        private int Contained(ResourceDefinition item) => _contained.TryGetValue(item, out int n) ? n : 0;

        /// <summary>How many of an item are in her containers (the rest are in her pockets).</summary>
        public int InContainers(ResourceDefinition item)
        {
            FillContainers();
            return Contained(item);
        }

        /// <summary>The first container she holds that takes this kind of item (where one in use came from), or null.</summary>
        public ResourceDefinition ContainerFor(ResourceDefinition item)
        {
            FillContainers(); // the order it records is the one rule for "first"
            foreach (var container in _containerOrder)
                if (container.holds.Contains(item))
                    return container;
            return null;
        }

        /// <summary>
        /// What a container she holds has in it, and how much room it has left. The list is the
        /// simulation's own: read it at once, don't keep it (the next call refills it).
        /// </summary>
        public (IReadOnlyList<(ResourceDefinition item, int amount)> contents, int room) ContentsOf(ResourceDefinition container)
        {
            FillContainers();
            return ContentsAfterFill(container);
        }

        /// <summary>ContentsOf for a caller that has just called FillContainers.</summary>
        private (IReadOnlyList<(ResourceDefinition item, int amount)> contents, int room) ContentsAfterFill(ResourceDefinition container)
        {
            if (container == null || !container.IsContainer || AmountOf(container) <= 0)
                return (Array.Empty<(ResourceDefinition item, int amount)>(), 0);
            if (!_containerRoom.TryGetValue(container, out int room))
                return (Array.Empty<(ResourceDefinition item, int amount)>(), 0);
            return (_containerContents[container], room);
        }

        /// <summary>How many a container holds when full: its size times how many of it she has.</summary>
        public int ContainerCapacity(ResourceDefinition container) =>
            (container ?? throw new ArgumentNullException(nameof(container))).holdsHowMany * AmountOf(container);

        /// <summary>How many a container holds now: its capacity less the room it has left.</summary>
        public int ContainerUsed(ResourceDefinition container) => ContainerCapacity(container) - ContentsOf(container).room;

        /// <summary>Room left for an item in the containers she holds that take it. The caller has just called FillContainers.</summary>
        private int ContainerRoomFor(ResourceDefinition item)
        {
            int room = 0;
            foreach (var entry in _containerRoom)
                if (entry.Key.holds.Contains(item))
                    room += entry.Value;
            return room;
        }

        /// <summary>Pockets she has now: the run's starting pockets, plus what a satchel or the like adds.</summary>
        public int PocketSlots => Settings.pocketSlots + SumOverHeld((item, held) => item.addsPockets * held);

        /// <summary>Adds up what everything she holds brings, this run's and kept (e.g. extra pockets).</summary>
        private int SumOverHeld(Func<ResourceDefinition, int, int> each)
        {
            int total = 0;
            foreach (var entry in Loop.ToolsAndStats)
                total += each(entry.Key, entry.Value);
            foreach (var entry in Persistent.Resources)
                total += each(entry.Key, entry.Value);
            return total;
        }

        /// <summary>Multiplies together what everything she holds does (e.g. to the drain).</summary>
        private float MultiplyOverHeld(Func<ResourceDefinition, int, float> each)
        {
            float total = 1f;
            foreach (var entry in Loop.ToolsAndStats)
                total *= each(entry.Key, entry.Value);
            foreach (var entry in Persistent.Resources)
                total *= each(entry.Key, entry.Value);
            return total;
        }

        /// <summary>
        /// Room for more of this anywhere she can put it: her pockets, then the floor here. A
        /// one-of-a-kind object has room only while there's none of it this run: her pockets first
        /// (something else pushed out for it, MakeWayFor), the floor here failing that (Grant), so
        /// there's always somewhere for it to land.
        /// </summary>
        public int SpaceFor(ResourceDefinition item)
        {
            if (IsOneOfAKind(item))
            {
                if (CopiesThisRun(item) > 0)
                    return 0;
                // With no floor to land on (a game without rooms), only her pockets can take it.
                bool hasFloor = HasPlaces && Loop.CurrentNode != null;
                return hasFloor ? 1 : Math.Max(RoomFor(item), PushedOutFor(item) != null ? 1 : 0);
            }
            long space = (long)RoomFor(item) + FloorRoomFor(item);
            return space > int.MaxValue ? int.MaxValue : (int)space;
        }

        /// <summary>How many she has to hand: in her pockets plus on the floor where she is.</summary>
        public int HeldOrHere(ResourceDefinition item) => AmountOf(item) + OnFloor(Loop.CurrentNode, item);

        /// <summary>
        /// How many more of this she can take: up to its maximum and, for pocketed items, the free
        /// pocket space. int.MaxValue if nothing limits it.
        /// </summary>
        public int RoomFor(ResourceDefinition resource)
        {
            if (resource == null)
                return 0;
            int cap = ResourceCapOf(resource);
            int room = cap == int.MaxValue ? int.MaxValue : Math.Max(0, cap - AmountOf(resource));
            if (resource.IsPocketed)
            {
                FillContainers();
                int pockets = resource.onlyInContainers ? 0 : Math.Max(0, PocketSlots - PocketsUsedAfterFill());
                room = Math.Min(room, pockets + ContainerRoomFor(resource));
            }
            return room;
        }

        /// <summary>Whether anything limits how many she can hold: a maximum, or pockets.</summary>
        public bool IsLimited(ResourceDefinition resource) =>
            resource != null && (ResourceCapOf(resource) < int.MaxValue || resource.IsPocketed);

        /// <summary>
        /// An object there's only one of (its maximum is 1: Roland's ring, the tome): never a second
        /// this run, not even one put down on the floor.
        /// </summary>
        public bool IsOneOfAKind(ResourceDefinition item) => item != null && item.IsPocketed && ResourceCapOf(item) == 1;

        /// <summary>
        /// A count with nothing carried about it: not pocketed, not a container, and doesn't add
        /// pocket space (a pouch or a satchel still acts like an object, even without goesInPocket).
        /// Its maximum has nothing left to free up, unlike an object stuck only by full pockets.
        /// </summary>
        private static bool IsPlainCount(ResourceDefinition resource) =>
            !resource.IsPocketed && !resource.IsContainer && resource.addsPockets == 0;

        /// <summary>How many of this there are this run: what she holds, plus what lies on every floor.</summary>
        public int CopiesThisRun(ResourceDefinition item)
        {
            int copies = AmountOf(item);
            foreach (var pile in Loop.Floor.Values)
                if (pile.TryGetValue(item, out int lying))
                    copies += lying;
            return copies;
        }

        /// <param name="makeWay">Something she made: with her pockets full, something else is pushed out for it (MakesThings).</param>
        private void Grant(ResourceDefinition resource, int amount, bool makeWay = false)
        {
            if (resource == null || amount <= 0)
                return;

            int held = AmountOf(resource);
            bool hadPlanning = resource.unlocksPlanning && PlanningUnlocked;
            bool oneOfAKind = IsOneOfAKind(resource);
            if (oneOfAKind)
                amount = Math.Min(amount, Math.Max(0, 1 - CopiesThisRun(resource)));
            if (amount <= 0)
                return;
            // A one-of-a-kind thing prefers her pockets, and something she made goes there too:
            // something else makes way for it.
            if (oneOfAKind || makeWay)
                MakeWayFor(resource, amount);
            int fits = Math.Min(amount, RoomFor(resource));
            int kept = held + fits;
            if (resource.IsKept)
                Persistent.Resources[resource] = kept;
            else
                Loop.ToolsAndStats[resource] = kept;

            // What doesn't fit in her pockets is put down here, if the floor has room. A one-of-a-kind
            // thing that still doesn't fit anywhere lands here anyway, past the floor's usual limit:
            // it has to go somewhere, so it's never simply lost. Placeholder rule: see PushedOutFor.
            int remaining = amount - fits;
            int putDown = remaining <= 0 ? 0 : PutDown(resource, remaining);
            if (putDown < remaining && oneOfAKind)
                putDown += PutDown(resource, remaining - putDown, ignoreFloorLimit: true);
            if (fits > 0 || putDown == 0)
                ResourceGained?.Invoke(resource, fits);
            if (resource.unlocksPlanning && !hadPlanning && PlanningUnlocked)
                PlanningUnlockedNow?.Invoke();
            CheckSwitches(SwitchTrigger.ResourceReached);
        }

        /// <summary>Removes up to <paramref name="amount"/> of something she has.</summary>
        private void Take(ResourceDefinition resource, int amount)
        {
            if (resource == null || amount <= 0)
                return;
            int held = AmountOf(resource);
            int taken = Math.Min(held, amount);
            if (taken <= 0)
                return;
            if (resource.IsKept)
                Persistent.Resources[resource] = held - taken;
            else
                Loop.ToolsAndStats[resource] = held - taken;
            ResourceLost?.Invoke(resource, taken);
        }
    }
}
