using System;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>Reading a save's JSON, and the ladder that upgrades older versions one step at a time.</summary>
    public static partial class SaveSerializer
    {
        /// <summary>Reads a save's JSON. Returns null, with a reason, if it's unreadable or from a newer version of the game.</summary>
        public static SaveData FromJson(string json, out string error)
        {
            error = null;
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                error = GameText.Get("menu.damaged", ("detail", e.Message));
                return null;
            }

            if (data == null)
            {
                error = GameText.Get("menu.empty_file");
                return null;
            }
            if (data.version > SaveData.CurrentVersion)
            {
                error = GameText.Get("menu.too_new");
                return null;
            }
            // A file with no version (JsonUtility leaves it 0) is as old as a save can be: it goes through every
            // upgrade rather than skipping them all, which would load old fields as if they were current.
            if (data.version <= 0)
                data.version = 1;
            data.loadedVersion = data.version;
            // Older versions are upgraded here, one version at a time.
            // 1 → 2: skill mastery added. An old save simply has none yet, so there's nothing to change.
            if (data.version < 2)
                data.version = 2;
            // 2 → 3: the build is gone (every run starts with an empty queue). Its old fields are simply ignored.
            if (data.version < 3)
                data.version = 3;
            // 3 → 4: milestone times added. An old save has none yet, so there's nothing to change.
            if (data.version < 4)
                data.version = 4;
            // 4 → 5: rooms are searched afresh every run. Restore turns the old kept exploration into found ways.
            if (data.version < 5)
                data.version = 5;
            // 5 → 6: a run under way can be saved. An older save has none, so it loads between runs as before.
            if (data.version < 6)
                data.version = 6;
            // 6 → 7: pick-ups and put-downs are made from two verbs, saved with their item. An older save's
            // queued pick-up names a task asset that's gone, so it's skipped (with a warning) on loading.
            if (data.version < 7)
                data.version = 7;
            // 7 → 8: total game time played. An older save has none recorded, so it counts from 0.
            if (data.version < 8)
                data.version = 8;
            // 8 → 9: a history of every finished run. An older save kept only the total, so its history starts empty.
            if (data.version < 9)
                data.version = 9;
            // 9 → 10: actions done this run are saved with their item, like queued ones. An older save
            // kept only Ids, so its pick-ups and put-downs come back as their bare verbs, as they did.
            if (data.version < 10)
            {
                if (data.run != null)
                {
                    foreach (string id in data.run.completedTasks) data.run.completedActions.Add(new SavedAction { task = id });
                    foreach (string id in data.run.completionLog) data.run.actionLog.Add(new SavedAction { task = id });
                    data.run.completedTasks.Clear();
                    data.run.completionLog.Clear();
                }
                data.version = 10;
            }
            // 10 → 11: the run's time split by skill. A run under way in an older save starts with empty
            // tallies, so its strip shows shares of the time recorded since. Nothing to change.
            if (data.version < 11)
                data.version = 11;
            // 11 → 12: each run in the history keeps the milestone times it reached. Older records have
            // none, so their columns on the Summary page show "—". Nothing to change.
            if (data.version < 12)
                data.version = 12;
            // 12 → 13: the separate "last run" milestone times are gone (the run history holds them
            // now). Nothing to change.
            if (data.version < 13)
                data.version = 13;
            // 13 → 14: rooms are searched once, ever, and the bar is kept. The search a run under way had
            // done is kept too (the larger value wins); Restore fills the rooms whose ways were found.
            if (data.version < 14)
            {
                if (data.run != null)
                {
                    foreach (var searched in data.run.explored)
                    {
                        var kept = data.exploredRooms.Find(room => room.id == searched.id);
                        if (kept == null)
                            data.exploredRooms.Add(new SavedValue { id = searched.id, value = searched.value });
                        else
                            kept.value = Math.Max(kept.value, searched.value);
                    }
                    data.run.explored.Clear();
                }
                data.version = 14;
            }
            // 14 → 15: milestones can be rooms, and the rooms she has entered are kept. An older save has
            // none, so each room's next entry counts as its first. Nothing to change.
            if (data.version < 15)
                data.version = 15;
            // 15 → 16: rooms she has worked in are counted, the queue between runs is saved, and a run's
            // steps record their rooms. An older save has none: every room starts at 0 runs, the next
            // queue is empty, and a run under way carries nothing into the next.
            if (data.version < 16)
                data.version = 16;
            // 16 → 17: the run's log of finished actions is its steps alone. An older run's plain log
            // becomes steps with no room, so it still fills the report but carries nothing on.
            if (data.version < 17)
            {
                if (data.run != null)
                {
                    if (data.run.steps.Count == 0)
                        foreach (var done in data.run.actionLog)
                            data.run.steps.Add(new SavedStep { task = done.task, item = done.item });
                    data.run.actionLog.Clear();
                }
                data.version = 17;
            }
            // 17 → 18: a run under way remembers the vitality its trips' growing charge has taken, and which
            // part of a running cost is that charge. An older run starts those at 0 (its Summary undercounts).
            if (data.version < 18)
                data.version = 18;
            // 18 → 19: Endurance banks kept maximum vitality after each run, and a run under way counts the
            // vitality lost. Older saves had no bank (it starts at 0) and a run under way starts its count at 0.
            if (data.version < 19)
                data.version = 19;
            // 19 → 20: the last finished run's report is saved, for the Summary. An older save has none
            // (hasLastRun is false), so the Summary is empty until the next run ends, as before.
            if (data.version < 20)
                data.version = 20;
            // 20 → 21: a switch can reopen a room's search (plan 027b). Restore starts the search again in
            // rooms a switch flipped before then reopens, as the flip would have.
            if (data.version < 21)
                data.version = 21;
            // 21 → 22: the Summary lists every task's escalating charge, not only trips'. A run under way in an
            // older save, and an older saved report, have none listed (nothing to change).
            if (data.version < 22)
                data.version = 22;
            // 22 → 23: mastery has no cap (the old per-stat cap bonus is gone: its JSON field is simply
            // ignored on load), and stats' mastery settles at a run's end (plan 041). Nothing to change;
            // stat XP, mastery and banked vitality earned in Act I before this stay as they were.
            if (data.version < 23)
                data.version = 23;
            // 23 → 24: each skill's mastery at the start of the last ended run is kept, so the tooltips can say
            // how much faster an action is than last run (plan 042). An older save has none, and hasLastRunStart
            // loads false: nothing is compared until a run has ended after the upgrade, so nothing to fill in.
            if (data.version < 24)
                data.version = 24;
            return data;
        }
    }
}
