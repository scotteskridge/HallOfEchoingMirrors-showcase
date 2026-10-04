namespace HallOfEchoingMirrors.Core
{
    // Why an action can't start now: the chain of checks, in the order the player is told (the first reason found wins).
    public partial class Simulation
    {
        /// <summary>
        /// Why a queue entry can't start now, in the words the player is told whether it's refused as
        /// it's asked for or skipped when reached: a Carry with nothing left to fit, CanStart's reason,
        /// and for a trip that reason with where she was going. Null if it can start.
        /// </summary>
        private string WhyEntryCantStart(QueueEntry entry)
        {
            if (IsFullCarry(entry))
                return GameText.Get("reasons.cant_carry_more", ("item", FirstGive(entry.Task).DisplayName));
            if (CanStart(entry.Task, entry.Destination, out string reason))
                return null;
            return entry.Destination != null
                ? GameText.Get("reasons.trip", ("room", GameText.TitleInSentence(entry.Destination.DisplayName)), ("reason", reason))
                : reason;
        }

        // The first reason found wins, so the order is the order the player is told about them.
        // The two chains below repeat on purpose (a flag would only hide the difference): keep them in step.

        /// <param name="destination">For a trip, where to. Null for everything else.</param>
        private bool CanStart(TaskDefinition task, NodeDefinition destination, out string reason)
        {
            reason = CantStartAtAll(task)
                ?? CantStartInThisPlace(task, destination)
                ?? CantStartForRoom(task)
                ?? CantStartForAttribute(task)
                ?? CantStartForSkill(task)
                ?? CantStartForMissingItem(task) // after the stats: they take runs, an item only a trip (decisions log, 2026-10-01)
                ?? CantStartForUnknownHue(task)
                ?? CantStartHere(task);
            return reason == null;
        }

        /// <summary>As CanStart, but ignoring the items it needs: whether supplying them would be enough.</summary>
        private bool CanStartIgnoringNeeds(TaskDefinition task, NodeDefinition destination, out string reason)
        {
            reason = CantStartAtAll(task)
                ?? CantStartInThisPlace(task, destination)
                ?? CantStartForRoom(task)
                ?? CantStartForAttribute(task)
                ?? CantStartForSkill(task)
                ?? CantStartForUnknownHue(task)
                ?? CantStartHere(task);
            return reason == null;
        }

        /// <summary>Why the task is out of the question this run (locked, or already done), or null.</summary>
        private string CantStartAtAll(TaskDefinition task)
        {
            if (_content != null && !IsUnlocked(task))
                return GameText.Get("reasons.no_longer_possible");
            if (task.oncePerRun && Loop.CompletedTasks.Contains(task))
                return GameText.Get("reasons.done_this_run");
            if (!HasFamily(task))
                return GameText.Get("reasons.no_cost_family");
            return null;
        }

        /// <summary>Why the room she's in rules it out (a trip with no way, a search with nothing left, a pick-up or put-down that can't be done), or null.</summary>
        private string CantStartInThisPlace(TaskDefinition task, NodeDefinition destination)
        {
            if (destination != null)
                return CantTravel(Loop.CurrentNode, destination);
            if (task == ExploreVerb && HasPlaces)
                return CantExplore(Loop.CurrentNode);
            if (task.picksUp != null && HasPlaces)
                return CantPickUp(task.picksUp);
            if (task.putsDown != null && HasPlaces)
                return CantPutDown(task.putsDown);
            return null;
        }

        /// <summary>Why there's nothing to spend time on, because there's no room for anything it gives, or null.</summary>
        private string CantStartForRoom(TaskDefinition task)
        {
            if (task.picksUp != null || !GivesAreFull(task))
                return null;
            var give = FirstGive(task);
            // An object's pockets or floor can free up; a plain count has nothing left to reach.
            return IsPlainCount(give)
                ? GameText.Get("reasons.count_at_max", ("item", give.DisplayName), ("max", ResourceCapOf(give)))
                : GameText.Get("reasons.cant_hold_more", ("item", give.DisplayName));
        }

        /// <summary>Why she lacks an item the task needs, or null.</summary>
        private string CantStartForMissingItem(TaskDefinition task)
        {
            var missing = MissingNeed(task);
            return missing == null ? null : NeedsReason(missing);
        }

        /// <summary>Why her attributes are too weak for the task, or null.</summary>
        private string CantStartForAttribute(TaskDefinition task)
        {
            foreach (var need in task.requiresAttributes)
                if (need.attribute != ClaraAttribute.None && StrengthOf(need.attribute) < need.level)
                    return GameText.Get("reasons.needs_attribute", ("attribute", GameText.Attribute(need.attribute)), ("level", need.level));
            return null;
        }

        /// <summary>Why her skills are too weak for the task (Crafting 8, has 5), or null.</summary>
        private string CantStartForSkill(TaskDefinition task)
        {
            foreach (var need in task.requiresSkills)
                if (need.skill != null && StrengthOf(need.skill) < need.level)
                    return GameText.Get("reasons.needs_skill",
                        ("skill", need.skill.DisplayName), ("level", need.level), ("has", StrengthOf(need.skill)));
            return null;
        }

        /// <summary>
        /// Why the task costs a hue she hasn't learned, or null. A low or empty pool doesn't stop a
        /// task: whatever it can't cover comes out of vitality. (Vitality is never checked up front:
        /// running out of it mid-task is how a run ends, and the chase depends on exactly that.)
        /// </summary>
        internal string CantStartForUnknownHue(TaskDefinition task)
        {
            foreach (var (source, pool, _) in EffectiveCosts(task, 1f))
                if (pool == null)
                    return NeedsPoolReason(source);
            return null;
        }

        internal static string NeedsPoolReason(CostSource source) =>
            GameText.Get("reasons.needs_pool", ("pool", GameText.HueName(source)));

        /// <summary>Why the room she's in doesn't offer the task, or null.</summary>
        private string CantStartHere(TaskDefinition task) =>
            IsAvailableHere(task) ? null : NotHereReason(Loop.CurrentNode);

        internal static string NotHereReason(NodeDefinition room) =>
            GameText.Get("reasons.not_here", ("room", GameText.TitleInSentence(room.DisplayName)));
    }
}
