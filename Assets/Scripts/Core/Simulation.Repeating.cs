using System;

namespace HallOfEchoingMirrors.Core
{
    // When a queued entry stops repeating: done, its limit reached, or nothing it gives has room left.
    public partial class Simulation
    {
        /// <summary>
        /// Whether a queue entry has nothing more to do: a trip made, a single action or a
        /// once-a-run task done, a room fully explored, everything it gives at its maximum, or
        /// its repeat limit reached. An entry is never "done" before its first go: if it can't
        /// even start, CanStart says why instead.
        /// </summary>
        private bool IsDone(QueueEntry entry)
        {
            if (entry.TimesLeft.HasValue && entry.TimesLeft.Value <= 0)
                return true;
            if (entry.SuppliesFor != null && !Loop.Queue.Entries.Contains(entry.SuppliesFor))
                return true;
            if (entry.TimesDone == 0)
                return false;

            var task = entry.Task;
            if (OneGoKindOf(task, entry.Destination) != RepeatKind.Forever)
                return true;
            if (entry.CarryOnly && CarriedFull(task))
                return true;
            // A supplier is done once there's enough for the entry it's supplying.
            if (entry.Supplies != null &&
                HeldOrHere(entry.Supplies) >= entry.SupplyTarget)
                return true;
            if (task == ExploreVerb && HasPlaces && CantExplore(Loop.CurrentNode) != null)
                return true;
            return GivesAreFull(task);
        }

        /// <summary>
        /// How long a queued entry of this task keeps repeating, the rule <see cref="IsDone"/> follows, for
        /// screens that say so. <paramref name="destination"/> is a trip's room (null otherwise);
        /// <paramref name="room"/> is where the task would be done: with one given, a search counts as
        /// <see cref="RepeatKind.Explore"/> (until that room is fully explored).
        /// </summary>
        public RepeatKind RepeatKindOf(TaskDefinition task, NodeDefinition destination, NodeDefinition room)
        {
            var kind = OneGoKindOf(task, destination);
            if (kind != RepeatKind.Forever)
                return kind;
            if (task == ExploreVerb && room != null)
                return RepeatKind.Explore;
            foreach (var give in task.gives)
                if (IsLimited(give.resource))
                    return RepeatKind.UntilFull;
            return RepeatKind.Forever;
        }

        // The kinds that end after one go; Forever here just means "none of them" (IsDone runs this every tick, so it stays cheap).
        private static RepeatKind OneGoKindOf(TaskDefinition task, NodeDefinition destination)
        {
            if (destination != null)
                return RepeatKind.Trip;
            if (task.singleAction)
                return RepeatKind.SingleAction;
            if (task.oncePerRun)
                return RepeatKind.OncePerRun;
            if (task.picksUp != null)
                return RepeatKind.PickUp;
            if (task.putsDown != null)
                return RepeatKind.PutDown;
            return RepeatKind.Forever;
        }

        private bool IsFullCarry(QueueEntry entry) => entry.CarryOnly && CarriedFull(entry.Task);

        /// <summary>True if the task gives something and none of it fits in her pockets or containers.</summary>
        private bool CarriedFull(TaskDefinition task) =>
            NothingItGivesFits(task, _fitsCarried ??= (_, item) => RoomFor(item) > 0);

        private static ResourceDefinition FirstGive(TaskDefinition task)
        {
            foreach (var give in task.gives)
                if (give.IsReal)
                    return give.resource;
            return null;
        }

        /// <summary>True if the task gives something and she has no room for more of any of it (its maximum, or full pockets).</summary>
        private bool GivesAreFull(TaskDefinition task) =>
            NothingItGivesFits(task, _fitsHere ??= (t, item) => SpaceFor(item) > 0 || CanPushIn(t, item));

        // The two room tests, built once: these run every tick, so a new delegate per call would
        // allocate each time (a lambda that uses `this` isn't cached by the compiler).
        // _fitsCarried: room in her pockets and containers only (CarriedFull).
        private Func<TaskDefinition, ResourceDefinition, bool> _fitsCarried;
        // _fitsHere: that, plus the floor here, or pushing something out for a made item (GivesAreFull).
        private Func<TaskDefinition, ResourceDefinition, bool> _fitsHere;

        /// <summary>
        /// True if the task gives something and none of it fits, where <paramref name="fits"/> says
        /// whether one kind of give has room: in what she carries (pockets and containers), or
        /// also on the floor here and by pushing something out for a made item.
        /// </summary>
        private static bool NothingItGivesFits(TaskDefinition task, Func<TaskDefinition, ResourceDefinition, bool> fits)
        {
            bool givesAny = false;
            foreach (var give in task.gives)
            {
                if (!give.IsReal)
                    continue;
                givesAny = true;
                if (fits(task, give.resource))
                    return false;
            }
            return givesAny;
        }

        /// <summary>
        /// Placeholder rule: an action that makes something (pulling it out of a mirror, or crafting it
        /// from what it takes) puts it in her pockets even when they're full, pushing something else
        /// out (MakeWayFor). What she only gathers (wisps) goes on the floor instead.
        /// </summary>
        private static bool MakesThings(TaskDefinition task) =>
            task.kind == TaskKind.Instantiate || task.takes.Exists(take => take.IsReal);

        /// <summary>Whether this task's item can go in her full pockets by pushing something out (it's made). A one-of-a-kind item never needs this: SpaceFor always has room for it.</summary>
        private bool CanPushIn(TaskDefinition task, ResourceDefinition item) =>
            MakesThings(task) && PushedOutFor(item) != null;
    }
}
