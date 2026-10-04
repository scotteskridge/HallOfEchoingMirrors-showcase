using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    // Switches, what they unlock and lock, and the story they reveal.
    public partial class Simulation
    {
        /// <summary>Every task in the game content, in display order (locked ones included).</summary>
        public IReadOnlyList<TaskDefinition> AllTasks =>
            _content != null ? _content.tasks : (IReadOnlyList<TaskDefinition>)Array.Empty<TaskDefinition>();

        /// <summary>
        /// Whether a task is available right now. Starts Unlocked sets the opening state; after that,
        /// switches turn tasks on (unlocksTasks) and off (locksTasks).
        /// </summary>
        public bool IsUnlocked(TaskDefinition task)
        {
            task = VerbOf(task); // "Pick up all wisps" is as unlocked as Pick up
            return !Persistent.LockedTasks.Contains(task) &&
                   (task.startsUnlocked || Persistent.UnlockedTasks.Contains(task));
        }

        public bool IsFlipped(SwitchDefinition @switch) => Persistent.FlippedSwitches.Contains(@switch);

        /// <summary>
        /// Kept knowledge ticked Hide Once Used that has nothing left to do: at least one switch
        /// watches it, every one of them has flipped, and nothing else uses it (no action or way
        /// needs it, no action is easier with it). It leaves her list of what she knows.
        /// </summary>
        public bool HasDoneItsJob(ResourceDefinition item)
        {
            if (item == null || !item.hideOnceUsed || !item.IsKept || _content == null || StillUsed(item))
                return false;
            bool watched = false;
            foreach (var @switch in _content.switches)
            {
                if (@switch == null || @switch.trigger != SwitchTrigger.ResourceReached || @switch.resourceToHold != item)
                    continue;
                if (!IsFlipped(@switch))
                    return false;
                watched = true;
            }
            return watched;
        }

        // Plain loops, no lambdas: asked every frame by the pockets overlay and the room popover.
        private bool StillUsed(ResourceDefinition item)
        {
            foreach (var task in _content.tasks)
            {
                if (task == null)
                    continue;
                if (Mentions(task.needs, item))
                    return true;
                foreach (var easier in task.easierWith)
                    if (easier.whileHolding == item)
                        return true;
            }
            foreach (var node in _content.PlayableNodes)
                if (node != null)
                    foreach (var way in node.ways)
                        if (way != null && !way.IntoPlannedRoom && Mentions(NeedsOf(way, node), item))
                            return true;
            return false;
        }

        private static bool Mentions(IReadOnlyList<ResourceAmount> amounts, ResourceDefinition item)
        {
            foreach (var amount in amounts)
                if (amount.resource == item)
                    return true;
            return false;
        }

        /// <summary>
        /// Knowledge that has done its job and is found by searching this room (Mirrors Found in the
        /// Hanging Mirrors), with how much she has: the room shows it instead. Clears the list first.
        /// </summary>
        public void DoneKnowledgeFoundIn(NodeDefinition room, List<(ResourceDefinition item, int amount)> into)
        {
            into.Clear();
            if (room == null)
                return;
            foreach (var give in room.eachExploreGives)
                if (HasDoneItsJob(give.resource) && AmountOf(give.resource) > 0)
                    into.Add((give.resource, AmountOf(give.resource)));
        }

        /// <summary>
        /// The fastest game speed the player has earned (1 = normal only): the highest Unlocks Speed of
        /// anything she holds, kept things included. Faster speeds are a reward, not there from the start.
        /// </summary>
        public float FastestSpeedUnlocked
        {
            get
            {
                float fastest = 1f;
                foreach (var entry in Persistent.Resources)
                    if (entry.Value > 0)
                        fastest = Math.Max(fastest, entry.Key.unlocksSpeed);
                foreach (var entry in Loop.ToolsAndStats)
                    if (entry.Value > 0)
                        fastest = Math.Max(fastest, entry.Key.unlocksSpeed);
                return fastest;
            }
        }

        /// <summary>Whether she has earned room speed: she holds anything with Unlocks Room Speed, kept things included.</summary>
        public bool RoomSpeedUnlocked => HoldsItemWith(item => item.unlocksRoomSpeed);

        /// <summary>Whether she has earned planning between runs: she holds anything with Unlocks Planning, kept things included.</summary>
        public bool PlanningUnlocked => HoldsItemWith(item => item.unlocksPlanning);

        /// <summary>
        /// Whether the next run starts at once when a game begins or loads between runs: until she has earned
        /// planning there is no planning screen to stop at.
        /// </summary>
        public bool NextRunBeginsAtOnce => !PlanningUnlocked;

        private bool HoldsItemWith(Func<ResourceDefinition, bool> flag)
        {
            foreach (var entry in Persistent.Resources)
                if (entry.Value > 0 && flag(entry.Key))
                    return true;
            foreach (var entry in Loop.ToolsAndStats)
                if (entry.Value > 0 && flag(entry.Key))
                    return true;
            return false;
        }

        /// <summary>Adds a story beat to the journal and queues it for the pop-up. Each beat is shown once.</summary>
        public void Reveal(StoryBeat beat)
        {
            if (beat == null || Persistent.Journal.Contains(beat))
                return;
            Persistent.Journal.Add(beat);
            Persistent.UnreadStories.Add(beat);
        }

        /// <summary>Story beats revealed but not yet read, oldest first. Kept in saves until read.</summary>
        public IReadOnlyList<StoryBeat> UnreadStories => Persistent.UnreadStories;

        /// <summary>The pop-up calls this when the player closes a story (not when it opens), so a
        /// story that was on screen when the game stopped is shown again next time.</summary>
        public void MarkRead(StoryBeat beat) => Persistent.UnreadStories.Remove(beat);

        private void CheckSwitches(SwitchTrigger trigger)
        {
            if (_content == null)
                return;

            foreach (var @switch in _content.switches)
            {
                if (@switch == null || @switch.trigger != trigger)
                    continue;
                if (!IsFlipped(@switch))
                {
                    if (IsMet(@switch))
                    {
                        Flip(@switch);
                        ReachMilestone(@switch, firstTime: true);
                    }
                }
                // Already flipped: a benchmark, timed again the first time it's met in each run.
                else if (@switch.story != null && SwitchCanBeReachedAgain(@switch) && !Loop.HasReached(@switch) && IsMet(@switch))
                {
                    ReachMilestone(@switch, firstTime: false);
                }
            }
        }

        /// <summary>
        /// A switch with a story, or a room, is a milestone: note when this run reached it. Any other
        /// kind of content can't be one.
        /// </summary>
        private void ReachMilestone(ContentAsset milestone, bool firstTime)
        {
            switch (milestone)
            {
                case SwitchDefinition @switch when @switch.story == null:
                    return;
                case SwitchDefinition _:
                case NodeDefinition _:
                    break;
                default:
                    throw NotAMilestone(milestone);
            }
            if (Loop.HasReached(milestone))
                return;
            float seconds = SecondsThisRun;
            Loop.Milestones.Add((milestone, seconds));
            MilestoneReached?.Invoke(milestone, seconds, firstTime);
        }

        /// <summary>
        /// She has just arrived in a room. Every room but the start room is a benchmark, timed on her
        /// first entry each run; the first entry ever also opens the room's story. Returns whether
        /// this was her first entry this run (the card then stands in for the "arrived" line).
        /// </summary>
        private bool EnterRoom(NodeDefinition room)
        {
            if (room == _content?.startNode || Loop.HasReached(room))
                return false;
            bool firstEver = Persistent.RoomsEntered.Add(room);
            if (firstEver)
            {
                Loop.RoomsFirstEntered.Add(room);
                // Story first, so it is already in the journal when the milestone is recorded.
                Reveal(room.firstEntry);
            }
            ReachMilestone(room, firstEver);
            return true;
        }

        /// <summary>The story a milestone opens: a switch's, or a room's first-entry passage. May be null (a room with none).</summary>
        public static StoryBeat StoryOf(ContentAsset milestone) => milestone switch
        {
            SwitchDefinition @switch => @switch.story,
            NodeDefinition room => room.firstEntry,
            _ => throw NotAMilestone(milestone),
        };

        /// <summary>What a milestone is called: the switch's or the room's display name.</summary>
        public static string NameOf(ContentAsset milestone) => milestone switch
        {
            SwitchDefinition @switch => @switch.displayName,
            NodeDefinition room => room.DisplayName,
            _ => throw NotAMilestone(milestone),
        };

        private static InvalidOperationException NotAMilestone(ContentAsset asset) =>
            new InvalidOperationException($"{asset} can't be a milestone: only a switch or a room can.");

        /// <summary>Whether this milestone's story is new this run: a switch flipped now, or a room first entered now.</summary>
        public bool IsFirstTimeEver(ContentAsset milestone) =>
            milestone is SwitchDefinition @switch ? Loop.SwitchesFlipped.Contains(@switch)
            : milestone is NodeDefinition room && Loop.RoomsFirstEntered.Contains(room);

        /// <summary>
        /// Whether a milestone can be met afresh in a later run, to time it again. Not when its
        /// condition stays true between runs (kept resources): it would be met at the very start
        /// of every run. Nor a room's search (RoomExplored): its bar is kept between runs, so it is
        /// reached once (a reopened search is a new round, not a new run). Not when the
        /// switch locks its own trigger task on flipping (e.g. a collapse during a one-off chase):
        /// its condition can then never become true again.
        /// </summary>
        private static bool SwitchCanBeReachedAgain(SwitchDefinition @switch) => @switch.trigger switch
        {
            SwitchTrigger.RoomExplored => false,
            SwitchTrigger.ResourceReached => @switch.resourceToHold != null && !@switch.resourceToHold.IsKept,
            SwitchTrigger.LoopEndedDuringTask =>
                @switch.triggerTask == null || !@switch.locksTasks.Contains(@switch.triggerTask),
            _ => true,
        };

        // A room, always: every run's first entry is timed afresh.
        internal static bool CanBeReachedAgain(ContentAsset milestone) => milestone switch
        {
            NodeDefinition _ => true,
            SwitchDefinition @switch => SwitchCanBeReachedAgain(@switch),
            _ => throw NotAMilestone(milestone),
        };

        /// <summary>When this milestone was reached in the run before, or null if it wasn't.</summary>
        public float? LastRunTimeOf(ContentAsset milestone) =>
            milestone != null && Persistent.RunHistory.Count > 0
                ? Persistent.RunHistory[Persistent.RunHistory.Count - 1].TimeOf(milestone)
                : null;

        private bool IsMet(SwitchDefinition @switch) => @switch.trigger switch
        {
            SwitchTrigger.LoopEndedDuringTask =>
                @switch.triggerTask != null && Loop.CurrentTask == @switch.triggerTask,
            SwitchTrigger.TasksCompletedInOneRun =>
                @switch.requiredTasks.Count > 0 && @switch.requiredTasks.TrueForAll(Loop.CompletedTasks.Contains),
            SwitchTrigger.AttributeLevelReached =>
                @switch.triggerAttribute != ClaraAttribute.None && StrengthOf(@switch.triggerAttribute) >= @switch.triggerLevel,
            SwitchTrigger.ResourceReached =>
                @switch.resourceToHold != null && AmountOf(@switch.resourceToHold) >= @switch.triggerAmount,
            SwitchTrigger.RoomExplored => IsExploredTo(@switch.roomToExplore, @switch.explorePercent),
            _ => false,
        };

        private void Flip(SwitchDefinition @switch)
        {
            if (!Persistent.FlippedSwitches.Add(@switch))
                return;
            Loop.SwitchesFlipped.Add(@switch);

            foreach (var task in @switch.unlocksTasks)
            {
                if (task == null)
                    continue;
                Persistent.UnlockedTasks.Add(task);
                Persistent.LockedTasks.Remove(task);
            }
            foreach (var task in @switch.locksTasks)
            {
                if (task == null)
                    continue;
                Persistent.LockedTasks.Add(task);
                RemoveTaskFromQueue(task);
            }
            foreach (var hue in @switch.unlocksPools)
                Persistent.UnlockedHues.Add(hue);
            foreach (var room in @switch.reopensSearch)
                if (room != null)
                    ReopenSearch(room);

            // Story first: SwitchFlipped triggers an autosave, and the save must already include
            // the story, or a reload would have the switch flipped but its story never shown.
            Reveal(@switch.story);
            SwitchFlipped?.Invoke(@switch);
            CheckForNewWays(); // a way it opened may be one she's already found
        }
    }
}
