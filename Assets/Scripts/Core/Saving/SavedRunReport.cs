using System;
using System.Collections.Generic;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The last finished run's report as a save holds it (version 20 on), so Menu → Summary still shows
    /// it after a load. Content is stored by Id; "none" for a time or an Id is -1 or empty.
    /// </summary>
    [Serializable]
    public class SavedRunReport
    {
        public int loopNumber;
        public long ticks;
        public int endReason; // a LoopEndReason
        public int actionsCompleted;
        public string unfinished;
        public string unfinishedItem; // for a pick-up or put-down: which item (the task is its verb)

        public List<SavedReportTask> tasks = new List<SavedReportTask>();
        public int moves;
        public float moveVitality;
        /// <summary>Each other task's escalating charge this run. Version 22 on.</summary>
        public List<SavedCharge> charges = new List<SavedCharge>();
        public float keptVitalityGained;

        public List<SavedGain> attributes = new List<SavedGain>();
        public List<SavedGain> skills = new List<SavedGain>();
        public List<SavedTimeSlice> timeBySkill = new List<SavedTimeSlice>();
        public List<SavedKept> kept = new List<SavedKept>();
        public List<SavedAmount> toolsAndStats = new List<SavedAmount>();
        public List<string> switchesFlipped = new List<string>();
        public List<SavedAmount> carriedKept = new List<SavedAmount>();
        public List<SavedAmount> carriedLost = new List<SavedAmount>();
        public List<SavedValue> explored = new List<SavedValue>();
        public List<string> knownByHeartNow = new List<string>();
        public List<SavedMilestoneRow> milestones = new List<SavedMilestoneRow>();

        /// <summary>The run history records this report compares with, by loop number (-1: none).</summary>
        public int runBeforeLoop = -1;
        public int longestLoop = -1;
    }

    [Serializable]
    public class SavedCharge
    {
        public string task;
        public int goes;
        public float vitality;
    }

    [Serializable]
    public class SavedReportTask
    {
        public string task;
        public string item; // for a pick-up or put-down: which item (the task is its verb)
        public int count;
    }

    /// <summary>A stat's or skill's gain in one run: the attribute number, or the skill Id.</summary>
    [Serializable]
    public class SavedGain
    {
        public int attribute;
        public string skill;
        public int level, masteryBefore, masteryAfter;
        public float progressBefore, progressAfter;
    }

    [Serializable]
    public class SavedTimeSlice
    {
        public string skill; // empty: the "Other" slice
        public long ticks;
        public float share;
    }

    [Serializable]
    public class SavedKept
    {
        public string id;
        public int before, after;
    }

    [Serializable]
    public class SavedMilestoneRow
    {
        public string id;
        public float thisRun = -1f, lastRun = -1f, previousRun = -1f;
        public bool isNew;
    }
}
