using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The game's content in one place: every task (in the order the player sees them),
    /// every switch, and the opening story. Adding content means adding assets here, not code.
    /// (Kept resources such as Mirrors found are their own assets and carry their own maximum.)
    /// </summary>
    [CreateAssetMenu(fileName = "GameContent", menuName = "Hall of Echoing Mirrors/Game Content")]
    public class GameContent : ScriptableObject
    {
        [Tooltip("Shown when the game first starts.")]
        public StoryBeat openingStory;
        [Tooltip("All tasks, in display order. Locked ones stay hidden until a switch unlocks them.")]
        public List<TaskDefinition> tasks = new List<TaskDefinition>();
        public List<SwitchDefinition> switches = new List<SwitchDefinition>();

        [Header("Common verbs (defined once, changed per room by its modifiers)")]
        [Tooltip("The one Travel task: its time, cost and attribute are the base for every trip.")]
        public TaskDefinition travelVerb;
        [Tooltip("The one Search task (called Explore in code): the base for searching any room. Where she searches is where she is.")]
        public TaskDefinition exploreVerb;
        [Tooltip("The one Pick up task: the base for \"Pick up all wisps\" and the like, offered wherever an object lies " +
                 "on the floor. The game makes one per object; no asset each.")]
        public TaskDefinition pickUpVerb;
        [Tooltip("The one Put down task: the base for \"Put down the tome\" and the like, offered where she is for " +
                 "whatever is in her pockets.")]
        public TaskDefinition putDownVerb;

        [Header("Skills")]
        [Tooltip("Every skill, in the order the stats box lists them.")]
        public List<SkillDefinition> skills = new List<SkillDefinition>();

        [Header("Story")]
        [Tooltip("Clara's short lines in the story feed (Around her) (buckets made by Import Blurbs).")]
        public BlurbLibrary blurbs;

        [Header("Places")]
        [Tooltip("Where every run begins. If empty, there are no places: every task can be done anywhere.")]
        public NodeDefinition startNode;
        [Tooltip("Every node, for the map. Tasks not listed at any node can be done anywhere.")]
        public List<NodeDefinition> nodes = new List<NodeDefinition>();

        /// <summary>
        /// Whether a planned room lists this task. The one other place (besides editor tools) that reads
        /// <see cref="nodes"/>: a task held only by planned rooms must not read as "listed at no room", which
        /// would make it doable everywhere.
        /// </summary>
        public bool IsListedInAPlannedRoom(TaskDefinition task)
        {
            foreach (var node in nodes)
                if (node != null && node.planned && node.Lists(task))
                    return true;
            return false;
        }

        private readonly List<NodeDefinition> _playable = new List<NodeDefinition>();

        /// <summary>
        /// <see cref="nodes"/> without the planned rooms (plan ui-053): the one place that filter lives. Game code
        /// reads this, never <c>nodes</c>; only editor tools that lay rooms out read <c>nodes</c>. The same list is
        /// handed back until <c>nodes</c> or a room's Planned tick changes, so per-frame callers don't allocate.
        /// </summary>
        public IReadOnlyList<NodeDefinition> PlayableNodes
        {
            get
            {
                int kept = 0;
                bool same = true;
                foreach (var node in nodes)
                {
                    if (node != null && node.planned)
                        continue;
                    if (kept >= _playable.Count || _playable[kept] != node)
                    {
                        same = false;
                        break;
                    }
                    kept++;
                }
                if (same && kept == _playable.Count)
                    return _playable;

                _playable.Clear();
                foreach (var node in nodes)
                    if (node == null || !node.planned)
                        _playable.Add(node);
                return _playable;
            }
        }
    }
}
