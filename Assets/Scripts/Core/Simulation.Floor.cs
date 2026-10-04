using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // The floor: what doesn't fit in her pockets is put down in the room she's in, up to the floor
    // space for each kind of item, and picked up again with a Pick up action (Put down does the
    // reverse). Piles belong to the run: the hall shifts, and they're gone next time.
    public partial class Simulation
    {
        /// <summary>How many of each item a room's floor holds: the base, plus what lit candles and the like add.</summary>
        public int FloorSpace => _settings.floorSpace + SumOverHeld((item, held) => item.FloorSpaceFor(held));

        /// <summary>How many of this item lie on the floor of a room this run.</summary>
        public int OnFloor(NodeDefinition room, ResourceDefinition item) =>
            room != null && item != null && Loop.Floor.TryGetValue(room, out var pile) && pile.TryGetValue(item, out int n) ? n : 0;

        /// <summary>Everything on a room's floor this run, in the order it was put down.</summary>
        public List<(ResourceDefinition item, int amount)> FloorAt(NodeDefinition room)
        {
            var result = new List<(ResourceDefinition, int)>();
            FloorAt(room, result);
            return result;
        }

        /// <summary>Fills a list the caller keeps (screens refreshing every frame) with a room's floor.</summary>
        public void FloorAt(NodeDefinition room, List<(ResourceDefinition item, int amount)> into)
        {
            into.Clear();
            if (room != null && Loop.Floor.TryGetValue(room, out var pile))
                foreach (var entry in pile)
                    if (entry.Value > 0)
                        into.Add((entry.Key, entry.Value));
        }

        /// <summary>How many more of this item the floor here can take: objects only (not one kept only in containers), and only where there are rooms.</summary>
        public int FloorRoomFor(ResourceDefinition item)
        {
            if (item == null || !item.IsPocketed || item.onlyInContainers || !HasPlaces || Loop.CurrentNode == null)
                return 0;
            return Math.Max(0, FloorSpace - OnFloor(Loop.CurrentNode, item));
        }

        /// <summary>
        /// Puts what didn't fit in her pockets on the floor here, as much as fits (or, forced, all of
        /// it regardless: the last resort for a one-of-a-kind thing that has to land somewhere). Returns
        /// how many.
        /// </summary>
        private int PutDown(ResourceDefinition item, int amount, bool ignoreFloorLimit = false)
        {
            int put = ignoreFloorLimit ? amount : Math.Min(amount, FloorRoomFor(item));
            if (put <= 0)
                return 0;
            if (!Loop.Floor.TryGetValue(Loop.CurrentNode, out var pile))
                Loop.Floor[Loop.CurrentNode] = pile = new Dictionary<ResourceDefinition, int>();
            pile[item] = OnFloor(Loop.CurrentNode, item) + put;
            return put;
        }

        /// <summary>Uses up what a task takes: from the floor where she is first, then her pockets.</summary>
        private void UseUp(ResourceDefinition item, int amount)
        {
            int fromFloor = Math.Min(amount, OnFloor(Loop.CurrentNode, item));
            TakeFromFloor(item, fromFloor);
            Take(item, amount - fromFloor);
        }

        /// <summary>Uses some of what lies on the floor here (e.g. a wisp she's restoring from).</summary>
        private void TakeFromFloor(ResourceDefinition item, int amount)
        {
            int taken = Math.Min(amount, OnFloor(Loop.CurrentNode, item));
            if (taken > 0)
                Loop.Floor[Loop.CurrentNode][item] -= taken;
        }

        // ---------- Pick up and Put down ----------

        public TaskDefinition PickUpVerb => _content != null ? _content.pickUpVerb : null;
        public TaskDefinition PutDownVerb => _content != null ? _content.putDownVerb : null;

        private readonly Dictionary<ResourceDefinition, TaskDefinition> _pickUps = new Dictionary<ResourceDefinition, TaskDefinition>();
        private readonly Dictionary<ResourceDefinition, TaskDefinition> _putDowns = new Dictionary<ResourceDefinition, TaskDefinition>();

        /// <summary>"Pick up all wisps": the Pick up verb for one item (null if there's no verb).</summary>
        public TaskDefinition PickUpActionFor(ResourceDefinition item) => FloorActionFor(PickUpVerb, _pickUps, item, pickUp: true);

        /// <summary>"Put down the tome": the Put down verb for one item (null if there's no verb).</summary>
        public TaskDefinition PutDownActionFor(ResourceDefinition item) => FloorActionFor(PutDownVerb, _putDowns, item, pickUp: false);

        // Like a trip, these are one verb plus what it's about, so objects don't each need two task
        // assets. Each is a copy of its verb made the first time it's wanted, and then kept, so the
        // queue and the action list see the same action every time. (Its Id is the verb's: a save
        // stores the item beside it.)
        private static TaskDefinition FloorActionFor(TaskDefinition verb, Dictionary<ResourceDefinition, TaskDefinition> made,
            ResourceDefinition item, bool pickUp)
        {
            if (verb == null || item == null)
                return null;
            if (made.TryGetValue(item, out var action))
                return action;
            _ = verb.Id; // content made in code gets its Id on first asking: before copying, so the copy shares it
            action = UnityEngine.Object.Instantiate(verb);
            action.name = $"{verb.name} {item.name}";
            action.displayName = GameText.Get("names.floor_action", ("verb", verb.displayName), ("item", item.NameInActions));
            if (pickUp)
                action.picksUp = item;
            else
                action.putsDown = item;
            made[item] = action;
            return action;
        }

        /// <summary>The verb an action was made from (itself for anything else), e.g. for whether it's unlocked.</summary>
        private TaskDefinition VerbOf(TaskDefinition task)
        {
            if (task.picksUp != null && PickUpVerb != null)
                return PickUpVerb;
            if (task.putsDown != null && PutDownVerb != null)
                return PutDownVerb;
            return task;
        }

        /// <summary>Adds the pick-ups for what lies at a room, and (where she is) the put-downs for what's in her pockets.</summary>
        private void AddFloorActions(NodeDefinition node, List<TaskDefinition> into)
        {
            if (!HasPlaces || node == null)
                return;
            if (PickUpVerb != null && IsUnlocked(PickUpVerb) && Loop.Floor.TryGetValue(node, out var pile))
                foreach (var lying in pile)
                    if (lying.Value > 0)
                        into.Add(PickUpActionFor(lying.Key));
            if (PutDownVerb != null && IsUnlocked(PutDownVerb) && node == Loop.CurrentNode)
                foreach (var held in Loop.ToolsAndStats)
                    if (held.Key.IsPocketed && InPockets(held.Key) > 0)
                        into.Add(PutDownActionFor(held.Key));
        }

        /// <summary>A pick-up action: everything of its item on the floor here, as much as her pockets hold.</summary>
        private void PickUp(ResourceDefinition item)
        {
            var room = Loop.CurrentNode;
            // Picking something up is choosing it: with her pockets full, other things make way for the whole pile.
            MakeWayFor(item, OnFloor(room, item));
            int taken = Math.Min(OnFloor(room, item), RoomFor(item));
            if (taken <= 0)
                return;
            Loop.Floor[room][item] -= taken;
            Grant(item, taken);
        }

        /// <summary>How many of an item are in her pockets (not in a container).</summary>
        public int InPockets(ResourceDefinition item) => AmountOf(item) - InContainers(item);

        /// <summary>A put-down action: everything of its item in her pockets onto the floor here, as much as fits.</summary>
        private void PutDownFromPockets(ResourceDefinition item)
        {
            int put = PutDown(item, InPockets(item));
            Take(item, put);
        }

        /// <summary>Why a put-down action can't start here, or null if it can.</summary>
        private string CantPutDown(ResourceDefinition item)
        {
            if (InPockets(item) <= 0)
                return GameText.Get("reasons.nothing_to_put_down", ("item", item.DisplayName));
            if (FloorRoomFor(item) <= 0)
                return GameText.Get("reasons.floor_full", ("item", item.DisplayName));
            return null;
        }

        /// <summary>Why a pick-up action can't start here, or null if it can.</summary>
        private string CantPickUp(ResourceDefinition item)
        {
            if (OnFloor(Loop.CurrentNode, item) <= 0)
                return GameText.Get("reasons.nothing_to_pick_up", ("item", item.DisplayName));
            if (RoomFor(item) <= 0 && !CanMakeWayFor(item))
                return GameText.Get("reasons.no_room", ("item", item.DisplayName));
            return null;
        }

        /// <summary>
        /// Placeholder rule: what makes way in her full pockets for something that pushes in (a
        /// one-of-a-kind thing, something she made, or a pile she picks up; see MakeWayFor): one
        /// of what she has most of in her pockets (never the same thing, nor a one-of-a-kind thing),
        /// put down here. Null if nothing can make way, or it wouldn't help (not an object, or she
        /// already holds its maximum, or it goes only in containers, where a free pocket is no use).
        /// Whether room is needed at all is the caller's question.
        /// </summary>
        private ResourceDefinition PushedOutFor(ResourceDefinition item)
        {
            if (item == null || !item.IsPocketed || item.onlyInContainers || AmountOf(item) >= ResourceCapOf(item))
                return null;
            ResourceDefinition most = null;
            int mostHeld = 0;
            foreach (var held in Loop.ToolsAndStats)
            {
                var other = held.Key;
                // Something carried in from the lab can make way too: left on a floor, it goes back to the
                // mirror realm with the run and must be found again (the user's call, 2026-09-28).
                if (other == item || !other.IsPocketed || IsOneOfAKind(other) || FloorRoomFor(other) <= 0)
                    continue;
                int inPockets = InPockets(other);
                if (inPockets > mostHeld)
                {
                    most = other;
                    mostHeld = inPockets;
                }
            }
            return most;
        }

        /// <summary>What there's most of on the floor here (never a one-of-a-kind thing), or null if the floor is bare.</summary>
        private ResourceDefinition MostOnFloor()
        {
            if (!HasPlaces || Loop.CurrentNode == null || !Loop.Floor.TryGetValue(Loop.CurrentNode, out var pile))
                return null;
            ResourceDefinition most = null;
            int mostLying = 0;
            foreach (var lying in pile)
            {
                if (lying.Value <= 0 || IsOneOfAKind(lying.Key))
                    continue;
                if (lying.Value > mostLying)
                {
                    most = lying.Key;
                    mostLying = lying.Value;
                }
            }
            return most;
        }

        /// <summary>
        /// Loses one of what's most on the floor here, to free the floor space that was blocking a
        /// push-out (see MakeWayFor). Returns whether there was anything to lose.
        /// </summary>
        private bool DestroySomethingOnFloor()
        {
            var most = MostOnFloor();
            if (most == null)
                return false;
            Loop.Floor[Loop.CurrentNode][most] -= 1;
            return true;
        }

        /// <summary>Whether MakeWayFor could free a pocket for this, one way or another.</summary>
        private bool CanMakeWayFor(ResourceDefinition item) =>
            PushedOutFor(item) != null || (IsOneOfAKind(item) && MostOnFloor() != null);

        /// <summary>Pushes things out of her pockets onto the floor here until <paramref name="amount"/> of an item fit, or nothing more can make way.</summary>
        private void MakeWayFor(ResourceDefinition item, int amount)
        {
            // Never more than its maximum lets her hold: pushing out can't make room past that.
            int cap = ResourceCapOf(item);
            if (cap != int.MaxValue)
                amount = Math.Min(amount, cap - AmountOf(item));
            while (RoomFor(item) < amount)
            {
                if (PushedOutFor(item) is ResourceDefinition pushed)
                {
                    PutDown(pushed, 1);
                    Take(pushed, 1);
                    continue;
                }
                // Placeholder rule (the user's call, 2026-09-29): a one-of-a-kind thing must never stay
                // stuck behind a full floor forever, so what's blocking every candidate's own floor
                // space is lost, one at a time, until something can make way after all.
                if (IsOneOfAKind(item) && DestroySomethingOnFloor())
                    continue;
                break;
            }
        }
    }
}
