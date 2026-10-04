using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    public enum SwitchTrigger
    {
        /// <summary>Vitality ran out while Clara was doing a particular task (e.g. the first failed chase).</summary>
        LoopEndedDuringTask,
        /// <summary>Every listed task was completed at least once in the same run.</summary>
        TasksCompletedInOneRun,
        /// <summary>An attribute reached a level during a run (e.g. Perception 3 notices something hidden).</summary>
        AttributeLevelReached,
        /// <summary>She holds enough of a resource (e.g. 5 Mirrors found, or 3 Focus this run).</summary>
        ResourceReached,
        /// <summary>A room's search bar reached a point (e.g. the dark searched to 50%). The bar is kept between runs.</summary>
        RoomExplored,
    }

    /// <summary>
    /// A permanent milestone. Once flipped it stays flipped across loops, and it can unlock
    /// tasks and pathos pools and reveal a piece of story.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSwitch", menuName = "Hall of Echoing Mirrors/Switch")]
    public class SwitchDefinition : ContentAsset
    {
        public string displayName = "New Switch";

        [Header("What flips it")]
        public SwitchTrigger trigger = SwitchTrigger.TasksCompletedInOneRun;
        [Tooltip("For LoopEndedDuringTask: the task Clara must be doing when vitality runs out.")]
        public TaskDefinition triggerTask;
        [Tooltip("For TasksCompletedInOneRun: every task that must be completed in a single run.")]
        public List<TaskDefinition> requiredTasks = new List<TaskDefinition>();
        [Tooltip("For AttributeLevelReached: which attribute, and the strength it must reach (its level this run plus its mastery).")]
        public ClaraAttribute triggerAttribute = ClaraAttribute.Perception;
        [Min(1)] public int triggerLevel = 1;
        [Tooltip("For ResourceReached: drag in the resource, e.g. Mirrors found.")]
        public ResourceDefinition resourceToHold;
        [Tooltip("How many she must hold. Can't be more than the resource's maximum, or it will never trigger.")]
        [Min(1)] public int triggerAmount = 5;

        [Tooltip("For RoomExplored: which room.")]
        public NodeDefinition roomToExplore;
        [Tooltip("For RoomExplored: how far its bar must be filled, in percent.")]
        [Range(1, 100)] public int explorePercent = 100;

        [Header("What it changes (permanently)")]
        [Tooltip("Tasks that become available. Also re-enables tasks an earlier switch locked.")]
        public List<TaskDefinition> unlocksTasks = new List<TaskDefinition>();
        [Tooltip("Tasks that stop being available, e.g. the chase once it's clearly hopeless. " +
                 "They're hidden and taken out of the queue. A later switch can unlock them again.")]
        public List<TaskDefinition> locksTasks = new List<TaskDefinition>();
        public List<Hue> unlocksPools = new List<Hue>();
        [Tooltip("Stats this switch wakes: one the Loop Settings start asleep has no effect until a switch that wakes it flips.")]
        public List<ClaraAttribute> wakesAttributes = new List<ClaraAttribute>();
        [Tooltip("Ways that open, e.g. a shortcut. Name the room that lists the way, and where it leads.")]
        public List<WayRef> opensWays = new List<WayRef>();
        [Tooltip("Ways that shut. Closing wins over opening.")]
        public List<WayRef> closesWays = new List<WayRef>();
        [Tooltip("Ways whose Needs no longer apply, e.g. the lab's way once Roland has the ring. The way stays as it is otherwise.")]
        public List<WayRef> waivesWayNeeds = new List<WayRef>();
        [Tooltip("Rooms whose search starts again at 0% when this flips, once: everything found before stays found, and " +
                 "the room's finds with After Switch set to this switch can now be found. One switch per room.")]
        public List<NodeDefinition> reopensSearch = new List<NodeDefinition>();
        [Tooltip("Shown as a pop-up when the switch flips, then kept in the journal.")]
        public StoryBeat story;
    }
}
