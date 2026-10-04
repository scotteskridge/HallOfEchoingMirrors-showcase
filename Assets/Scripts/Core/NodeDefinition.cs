using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>What kind of place a node is. Roughly a third of nodes should not be memories.</summary>
    public enum NodeKind
    {
        /// <summary>A junction, dead end or door in the Hall. No content: these make routes readable.</summary>
        Hall = 0,
        /// <summary>The hall's own architecture: the mirror lab, storage, where it feeds.</summary>
        Constructed = 1,
        /// <summary>One of Clara's memories. She was there, so it can see her.</summary>
        ClaraMemory = 2,
        /// <summary>One of Roland's memories. It can't see her: she can only watch and gather.</summary>
        RolandMemory = 3,
    }

    /// <summary>
    /// How a room changes a common verb (travel, exploring) done there: multiplies its cost and
    /// its time. 1 means an ordinary room.
    /// </summary>
    [Serializable]
    public class ActionModifier
    {
        [Min(0f)] public float cost = 1f;
        [Min(0f)] public float time = 1f;
    }

    /// <summary>A way out of a room to another. Travel along it uses the common Travel verb.</summary>
    [Serializable]
    public class Way
    {
        public NodeDefinition to;
        [Tooltip("Can be walked back the other way too. Untick for a one-way valve.")]
        public bool bothWays = true;
        [Tooltip("Untick for a way a switch has to open (a shortcut, a hidden door).")]
        public bool startsOpen = true;
        [Tooltip("What she must hold to pass, e.g. 1 Roland's ring. The door stays on the map either way.")]
        public List<ResourceAmount> needs = new List<ResourceAmount>();
        [Tooltip("Hidden until the room it leaves from is searched this far (percent) in some run. Once found, " +
                 "it stays found (the hall shifts, but not its ways). 0 = known from the start.")]
        [Range(0, 100)] public int foundAtExplored;
        [Tooltip("Hidden from her below this Perception (level this run + mastery), however far she searches. 0 = no need.")]
        [Min(0)] public int needsPerception;
        [Tooltip("Optional: pops up when the way is found. Two ways can share one story; it's only shown once.")]
        public StoryBeat story;
        [Tooltip("Once found, the way and the room behind it show on the map even while the way is shut (drawn dim), and " +
                 "a trip there is refused with the Shut Message. Untick to keep it off the map until it opens.")]
        public bool showWhileShut;
        [Tooltip("The game text key for why she can't go in while the way is shut, e.g. \"reasons.shut_dark_corridor\" " +
                 "(the wording lives in Assets/Text/game_text.txt). Only used with Show While Shut. Empty = the generic \"reasons.way_shut\".")]
        [FormerlySerializedAs("shutMessage")]
        public string shutMessageKey;

        /// <summary>A way into a planned room (plan ui-053): the game ignores it; only the map preview draws it.</summary>
        public bool IntoPlannedRoom => to != null && to.planned;
    }

    /// <summary>
    /// An action found by searching a room: it's there once the room's kept search bar has reached
    /// this far, and stays found. A switch can reopen the search for a second round of finds.
    /// </summary>
    [Serializable]
    public class RoomFind
    {
        public TaskDefinition task;
        [Tooltip("Found once the room's search reaches this far (percent; the bar is kept between runs). In a room of 3 searches: 33, 66, 100.")]
        [Range(1, 100)] public int atSearched = 33;
        [Tooltip("Hidden from her below this Perception (level this run + mastery). 0 = anyone can find it.")]
        [Min(0)] public int needsPerception;
        [Tooltip("Found in the search round this switch begins (it lists this room under Reopens Search): hidden until it " +
                 "flips, then found at At Searched of the fresh bar. Empty = the first round.")]
        public SwitchDefinition afterSwitch;
    }

    /// <summary>Names a way by its two ends, so a switch can open or close it.</summary>
    [Serializable]
    public class WayRef
    {
        [Tooltip("The room whose Ways list holds the way.")]
        public NodeDefinition from;
        public NodeDefinition to;
    }

    /// <summary>
    /// A place Clara can be during a run. Its own tasks can only be done there. The common verbs
    /// (Travel, Explore) work in every room, changed only by the room's modifiers.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNode", menuName = "Hall of Echoing Mirrors/Node")]
    public class NodeDefinition : ContentAsset
    {
        // First in the Inspector, so a planned room's tick and note are the first thing seen (plan ui-053).
        [Header("Planning (editor only)")]
        [Tooltip("A room being laid out for a later act: drawn faded in the map preview (Tools → Map Layout) and ignored by the game, " +
                 "the save and the Summary. Its ways, and ways into it, do nothing in the game. Untick to bring it into the game.")]
        public bool planned;
        [Tooltip("One line for the author: what this planned room is for. Never shown to players.")]
        public string planningNote;

        [Header("Room")]
        public string displayName = "New Node";
        public NodeKind kind = NodeKind.Hall;
        [Tooltip("The realm's hue, for memory nodes. None for the Hall and constructed places.")]
        public Hue hue = Hue.None;
        [Tooltip("How deep in the hall this room is (0 = the first room). Every task done here is Room Step (Loop Settings) " +
                 "times longer for each level, and a trip out of here is priced by it. Fixed on the room, never worked out from the map.")]
        [Min(0)] public int depth;
        [Tooltip("Shown on the map before any way there is open, e.g. a sealed door she can see.")]
        public bool alwaysOnMap;

        [Tooltip("Ways out of this room. About 4 at most keeps the map and the choice readable.")]
        public List<Way> ways = new List<Way>();

        [Tooltip("Tasks that are always here, without searching. They can only be done here. The common verbs don't go here.")]
        public List<TaskDefinition> tasks = new List<TaskDefinition>();

        [Header("Searching (the common Search verb; the bar is kept between runs)")]
        [Tooltip("How many searches fill this room's bar (kept between runs, not per run). 0 means there's nothing to search here.")]
        [Min(0)] public int exploresToFill;
        [Tooltip("Actions found by searching: each appears once the kept search bar reaches its point, and stays found.")]
        public List<RoomFind> foundBySearching = new List<RoomFind>();
        [Tooltip("What each step of this room's bar gives, e.g. 1 Mirrors found: one search, or more with Perception, " +
                 "so a full bar always gives Explores To Fill of it (again on a reopened search's next round). " +
                 "Discoveries with a story at set points are switches (trigger: Room Explored), flipped the first time the search gets there.")]
        public List<ResourceAmount> eachExploreGives = new List<ResourceAmount>();

        [Header("Story")]
        [Tooltip("Optional: pops up the first time Clara ever enters this room, and is timed as a milestone every run. " +
                 "The start room never counts. Without one, the entry is still timed.")]
        public StoryBeat firstEntry;

        [Header("Modifiers (1 = ordinary)")]
        [Tooltip("Travel out of this room.")]
        public ActionModifier leaving = new ActionModifier();
        [Tooltip("Travel into this room.")]
        public ActionModifier entering = new ActionModifier();
        [Tooltip("Searching this room.")]
        public ActionModifier exploring = new ActionModifier();

        [Header("Map")]
        [Tooltip("Where this node sits on the map, in map units (the map stretches to fit).")]
        public Vector2 mapPosition;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>Whether this room has the task at all: always, or found by searching.</summary>
        public bool Lists(TaskDefinition task)
        {
            if (task == null)
                return false;
            if (tasks.Contains(task))
                return true;
            foreach (var find in foundBySearching)
                if (find != null && find.task == task)
                    return true;
            return false;
        }
    }
}
