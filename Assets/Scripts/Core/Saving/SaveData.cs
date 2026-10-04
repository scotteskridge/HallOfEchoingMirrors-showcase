using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Everything a save file holds: the permanent state that survives loops, and the run in
    /// progress if there is one (version 6 on). Content is stored by permanent ID, never by name, so renaming or moving an asset
    /// never breaks a save. Written as JSON text, so it's readable and easy to debug.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bump this when the format changes, and teach SaveSerializer to upgrade older saves.</summary>
        public const int CurrentVersion = 24;

        /// <summary>0, not CurrentVersion, so a file with no version counts as the oldest (SaveSerializer.Capture sets the real one): it must go through every upgrade.</summary>
        public int version;
        /// <summary>The version a loaded file had before FromJson upgraded it (not saved), for upgrades that need the content too. 0 when not loaded from a file.</summary>
        [NonSerialized] public int loadedVersion;
        public string savedAtUtc;

        /// <summary>Loops played so far. Loading starts the next one.</summary>
        public int loopsCompleted;

        public List<string> flippedSwitches = new List<string>();
        public List<string> unlockedTasks = new List<string>();
        public List<string> lockedTasks = new List<string>();
        public List<int> unlockedHues = new List<int>();

        public List<SavedAmount> resources = new List<SavedAmount>();
        public List<SavedAmount> resourceCapBonus = new List<SavedAmount>();

        /// <summary>Maximum vitality Endurance has banked over past runs. Version 19 on (older saves start at 0).</summary>
        public float keptVitality;

        public List<SavedAttributeValue> expertiseXp = new List<SavedAttributeValue>();

        /// <summary>Mastery XP per skill (skill ID, XP). Added in version 2.</summary>
        public List<SavedValue> skillMasteryXp = new List<SavedValue>();

        /// <summary>Each skill's mastery XP as the last ended run began (skill ID, XP), for "faster than last run". Version 24 on (older saves have none).</summary>
        public List<SavedValue> skillMasteryXpAtLastRunStart = new List<SavedValue>();
        /// <summary>Whether a run has ended since version 24, so the list above means something (a skill missing from it had 0).</summary>
        public bool hasLastRunStart;

        /// <summary>Carried items waiting in the real lab (item ID, count), and which are packed for the next run.</summary>
        public List<SavedAmount> stash = new List<SavedAmount>();
        public List<string> packed = new List<string>();

        /// <summary>Only in saves up to version 4: each room's kept exploration (room ID, count). Read to find ways; never written.</summary>
        public List<SavedAmount> explored = new List<SavedAmount>(); // up to version 4 only: rooms' kept exploration
        public List<SavedWay> foundWays = new List<SavedWay>();
        /// <summary>How far each room's search has got, kept for good (room ID, steps). Version 14 on.</summary>
        public List<SavedValue> exploredRooms = new List<SavedValue>();
        public long ticksPlayed; // game time of every ended run

        /// <summary>Every finished run, oldest first. Added in version 9.</summary>
        public List<SavedRunRecord> runHistory = new List<SavedRunRecord>();

        /// <summary>Rooms she has entered at least once, by ID. Version 15 on.</summary>
        public List<string> roomsEntered = new List<string>();

        /// <summary>How many runs she has worked in each room (room ID, runs). Version 16 on.</summary>
        public List<SavedAmount> roomRuns = new List<SavedAmount>();

        /// <summary>The queue made for the next run when saved between runs: what carried over, and anything the player added. Version 16 on.</summary>
        public List<SavedEntry> nextQueue = new List<SavedEntry>();

        /// <summary>Story beats seen, in order: the journal.</summary>
        public List<string> journal = new List<string>();

        /// <summary>Story beats revealed but not yet read (shown again on load).</summary>
        public List<string> unreadStories = new List<string>();

        /// <summary>The report of the last finished run, for the Summary. Version 20 on (older saves have none).</summary>
        public bool hasLastRun;
        public SavedRunReport lastRun = new SavedRunReport();

        /// <summary>A run under way when saved (version 6 on): loading picks it up where it was.</summary>
        public bool hasRun;
        public SavedRun run = new SavedRun();
    }

    /// <summary>Everything a run under way holds, so it can carry on exactly where it was.</summary>
    [Serializable]
    public class SavedRun
    {
        public long ticks;
        public float vitality, vitalityMax;
        public List<SavedHueValue> pools = new List<SavedHueValue>();
        public float drainHeldOff = 1f, drainGrown = 1f;
        /// <summary>Vitality this run's trips have charged her so far, for the Summary. Version 18 on.</summary>
        public float moveVitalityPaid;
        /// <summary>Vitality each other task's escalating charge has taken so far this run (task ID, vitality), for the Summary. Version 22 on.</summary>
        public List<SavedValue> taskChargePaid = new List<SavedValue>();
        /// <summary>Vitality lost so far this run, for the bank and Endurance. Version 19 on.</summary>
        public float vitalityLostThisRun;

        // Where she is and what she's doing (empty ids: nothing).
        public string node, task; // (a destination was saved too up to version 20: the running entry holds it)
        public float workDone;
        public int workNeeded;
        public List<SavedCost> costs = new List<SavedCost>();
        public int runningEntry = -1;
        public bool queueRanOutNotified;
        public List<SavedEntry> queue = new List<SavedEntry>();

        public List<SavedAmount> toolsAndStats = new List<SavedAmount>();
        public List<SavedFloorPile> floor = new List<SavedFloorPile>();
        public List<SavedValue> explored = new List<SavedValue>(); // up to version 13 only: this run's search. FromJson moves it into exploredRooms (empty from version 14)
        /// <summary>Each room's search progress as the run began (room ID, steps), for the run report. Version 14 on.</summary>
        public List<SavedValue> exploredAtStart = new List<SavedValue>();
        public bool hasExploredAtStart;
        public List<SavedRestoring> restorings = new List<SavedRestoring>();
        public List<SavedAttributeValue> attributeXp = new List<SavedAttributeValue>();
        public List<SavedValue> skillXp = new List<SavedValue>();
        /// <summary>Ticks this run spent on each skill's actions (skill ID, ticks), and on actions with no skill. Version 11 on.</summary>
        public List<SavedAmount> skillTicks = new List<SavedAmount>();
        public long otherTicks;

        // For this run's report and milestones. Actions done are saved as a pick-up or put-down's
        // verb plus its item (version 10 on); the plain-Id lists are only in older saves, and
        // FromJson moves them into these.
        public List<SavedAction> completedActions = new List<SavedAction>();
        public List<SavedAction> actionLog = new List<SavedAction>(); // up to version 16 only: FromJson moves it into steps
        /// <summary>Every action finished this run, in order, with where it was done. Version 17 on; an older run's log is moved here with no rooms, so it carries nothing into the next.</summary>
        public List<SavedStep> steps = new List<SavedStep>();
        public List<string> completedTasks = new List<string>(); // up to version 9 only
        public List<string> completionLog = new List<string>();  // up to version 9 only
        public List<string> switchesFlipped = new List<string>();
        public List<SavedValue> milestones = new List<SavedValue>(); // a switch's or a room's Id (rooms from version 15)
        public List<string> roomsFirstEntered = new List<string>(); // rooms first entered this run. Version 15 on.
        public List<SavedAttributeValue> attributeMasteryAtStart = new List<SavedAttributeValue>();
        public List<SavedValue> skillMasteryAtStart = new List<SavedValue>();
        public List<SavedAmount> keptAtStart = new List<SavedAmount>();
    }

    [Serializable]
    public class SavedEntry
    {
        public string task, destination;
        public string item; // for a pick-up or put-down: which item (the task is its verb)
        public bool limited;
        public int timesLeft, timesDone;
        public float savedWork, savedFraction;
        public string savedAt;
        public int suppliesFor = -1; // the index of the entry it's supplying, or -1
        public string supplies;
        public int supplyTarget;
        public bool carryOnly;
        public bool byHeart;
    }

    [Serializable]
    public class SavedStep
    {
        public string task, item, room, destination;
        public bool supply;
    }

    [Serializable]
    public class SavedAction
    {
        public string task;
        public string item; // for a pick-up or put-down: which item (the task is its verb)
    }

    [Serializable]
    public class SavedCost
    {
        public int hue; // Hue.None (0) is vitality
        public float perWork, remaining;
        public bool charge; // the task's escalating charge rather than its ordinary cost. Version 18 on.
    }

    [Serializable]
    public class SavedFloorPile
    {
        public string room, item;
        public int amount;
    }

    [Serializable]
    public class SavedRestoring
    {
        public string item;
        public float perSecond, left;
    }

    [Serializable]
    public class SavedHueValue
    {
        public int hue;
        public float value;
    }

    [Serializable]
    public class SavedAmount
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public class SavedWay
    {
        public string from;
        public string to;
    }

    [Serializable]
    public class SavedValue
    {
        public string id;
        public float value;
    }

    [Serializable]
    public class SavedRunRecord
    {
        public int loopNumber;
        public long ticks;
        public int actionsCompleted;
        public int endReason; // a LoopEndReason
        public List<SavedValue> milestones = new List<SavedValue>(); // switch or room Id, run seconds
    }

    [Serializable]
    public class SavedAttributeValue
    {
        public int attribute;
        public float value;
    }
}
