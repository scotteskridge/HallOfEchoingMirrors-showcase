using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>What's offered where: the actions a room lists, and why any of them can't be done.</summary>
    public partial class Simulation
    {
        /// <summary>
        /// The actions shown at a room (not travel: see DestinationsFrom), in order: Search if the
        /// room has anything to find, the room's own tasks, what the room's search has found (its kept bar),
        /// tasks that can be done anywhere, then picking up what lies there and putting down what's
        /// in her pockets. Replaces what's in <paramref name="into"/>.
        /// With a reason: done this run, one of a kind already held, fully searched (Blocked: the
        /// popover leaves these out unless set to show them), or a need missing or a stat or skill too
        /// low (not Blocked). Never shown, so nothing is given away: what's still to be found or
        /// unlocked, and kept rewards already earned (decisions log 2026-09-28).
        /// </summary>
        public void OffersAt(NodeDefinition room, List<ActionOffer> into)
        {
            into.Clear();

            if (ExploreVerb != null && IsUnlocked(ExploreVerb) && HasSomethingToExplore(room))
                into.Add(IsFullyExplored(room)
                    ? new ActionOffer(ExploreVerb, GameText.Get("reasons.fully_explored", ("room", GameText.TitleInSentence(room.DisplayName))), blocked: true)
                    : new ActionOffer(ExploreVerb));

            if (room != null)
            {
                foreach (var task in room.tasks)
                    if (task != null && IsUnlocked(task))
                        AddRoomOffer(task, room, into);
                foreach (var find in room.foundBySearching)
                    if (find?.task != null && IsUnlocked(find.task) && IsFoundHere(find.task, room))
                        AddRoomOffer(find.task, room, into);
            }

            // Tasks that can be done anywhere are listed only when they can be done: greyed, they'd
            // crowd every room (e.g. Put the ring down, without the ring).
            foreach (var task in AllTasks)
                if (task != null && task != TravelVerb && task != ExploreVerb && task != PickUpVerb && task != PutDownVerb &&
                    IsUnlocked(task) && !IsDoneForThisRun(task) && !Offers(into, task) && IsAvailableAt(task, room) &&
                    MissingNeedAt(task, room) == null)
                    into.Add(new ActionOffer(task));

            _floorScratch.Clear();
            AddFloorActions(room, _floorScratch);
            foreach (var task in _floorScratch)
                into.Add(new ActionOffer(task));
        }

        /// <summary>
        /// The actions that can be queued at a room: its offers, less those the queue would refuse
        /// (a missing need still counts: it may be supplied or picked up on the way).
        /// </summary>
        public List<TaskDefinition> TasksAt(NodeDefinition node)
        {
            var offers = new List<ActionOffer>();
            OffersAt(node, offers);
            var result = new List<TaskDefinition>(offers.Count);
            foreach (var offer in offers)
                if (!offer.Blocked)
                    result.Add(offer.Task);
            return result;
        }

        private readonly List<TaskDefinition> _floorScratch = new List<TaskDefinition>();

        private void AddRoomOffer(TaskDefinition task, NodeDefinition room, List<ActionOffer> into)
        {
            if (Offers(into, task))
                return;
            string done = DoneReason(task, out bool hidden);
            if (hidden)
                return;
            if (done != null)
            {
                into.Add(new ActionOffer(task, done, blocked: true));
                return;
            }
            // Greyed with what's missing, but not blocked: it may be supplied, or practised for, before its
            // turn. The attribute gate greys out as the skill gate does (the player's decision, 2026-09-30).
            var missing = MissingNeedAt(task, room);
            // The stats are named before a missing item: they take runs, an item only a trip (decisions log, 2026-10-01).
            into.Add(new ActionOffer(task, CantStartForAttribute(task) ?? CantStartForSkill(task) ?? (missing != null ? NeedsReason(missing) : null)));
        }

        /// <summary>
        /// Why a task is done for this run (see IsDoneForThisRun), or null if it isn't. A kept
        /// reward already earned, or a plain count already at its maximum, is
        /// <paramref name="hidden"/> instead: it's never offered again.
        /// </summary>
        private string DoneReason(TaskDefinition task, out bool hidden)
        {
            hidden = false;
            if (task.oncePerRun && Loop.CompletedTasks.Contains(task))
                return GameText.Get("reasons.done_this_run");
            if (!GivesOnlyWhatExists(task))
                return null;
            foreach (var give in task.gives)
                if (give.IsReal && !IsOneOfAKind(give.resource))
                {
                    hidden = true;
                    return null;
                }
            return GameText.Get("reasons.already_have");
        }

        private static bool Offers(List<ActionOffer> offers, TaskDefinition task)
        {
            foreach (var offer in offers)
                if (offer.Task == task)
                    return true;
            return false;
        }
    }
}
