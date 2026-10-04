using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's switches (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Switches ----------

        private void DrawSwitches()
        {
            if (!Section(ref _showSwitches, "Switches") || _content == null)
                return;

            Header(("Name", 170), ("Flips when", 170), ("Threshold", 260), ("Changes", 330), ("", 50));

            foreach (var @switch in _content.switches)
            {
                if (@switch == null)
                    continue;

                var so = new SerializedObject(@switch);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Field(so, "trigger", 170);
                DrawThreshold(so, @switch);
                Label(Changes(@switch), 330);
                SelectButton(@switch);
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>Only the fields the switch's trigger uses, in a fixed 260-wide space.</summary>
        private void DrawThreshold(SerializedObject so, SwitchDefinition @switch)
        {
            switch (@switch.trigger)
            {
                case SwitchTrigger.LoopEndedDuringTask:
                    Field(so, "triggerTask", 260);
                    break;
                case SwitchTrigger.TasksCompletedInOneRun:
                    _text.Clear();
                    foreach (var task in @switch.requiredTasks)
                        if (task != null)
                            _text.Append(_text.Length > 0 ? " + " : "").Append(task.displayName);
                    Label(_text.Length > 0 ? _text.ToString() : "(no tasks listed)", 260);
                    break;
                case SwitchTrigger.AttributeLevelReached:
                    Field(so, "triggerAttribute", 150);
                    Field(so, "triggerLevel", 106);
                    break;
                case SwitchTrigger.RoomExplored:
                    Field(so, "roomToExplore", 150);
                    Field(so, "explorePercent", 106);
                    break;
                case SwitchTrigger.ResourceReached:
                    Field(so, "resourceToHold", 150);
                    Field(so, "triggerAmount", 50);
                    var resource = @switch.resourceToHold;
                    bool unreachable = resource != null && resource.startingMax > 0 && @switch.triggerAmount > resource.startingMax;
                    Label(unreachable ? "over max!" : "", 56,
                        unreachable ? "More than the resource's maximum, so this can't flip until the maximum is raised." : null);
                    break;
                default:
                    Label("", 260);
                    break;
            }
        }

        private string Changes(SwitchDefinition @switch)
        {
            _text.Clear();
            AppendNames("unlocks", @switch.unlocksTasks);
            AppendNames("locks", @switch.locksTasks);
            if (@switch.wakesAttributes.Count > 0)
                _text.Append(_text.Length > 0 ? "; " : "").Append("wakes ").Append(string.Join(", ", @switch.wakesAttributes));
            if (@switch.unlocksPools.Count > 0)
                _text.Append(_text.Length > 0 ? "; " : "").Append("pools ").Append(string.Join(", ", @switch.unlocksPools));
            AppendWays("opens", @switch.opensWays);
            AppendWays("closes", @switch.closesWays);
            AppendWays("waives needs on", @switch.waivesWayNeeds);
            bool firstRoom = true;
            foreach (var room in @switch.reopensSearch)
            {
                if (room == null)
                    continue;
                _text.Append(firstRoom ? (_text.Length > 0 ? "; " : "") + "reopens the search of " : ", ").Append(room.DisplayName);
                firstRoom = false;
            }
            if (@switch.story != null)
                _text.Append(_text.Length > 0 ? "; " : "").Append("story");
            return _text.Length > 0 ? _text.ToString() : "-";
        }

        private void AppendWays(string verb, List<WayRef> ways)
        {
            bool first = true;
            foreach (var way in ways)
            {
                if (way?.from == null || way.to == null)
                    continue;
                _text.Append(first ? (_text.Length > 0 ? "; " : "") + verb + " " : ", ")
                     .Append(way.from.DisplayName).Append(" → ").Append(way.to.DisplayName);
                first = false;
            }
        }

        private void AppendNames(string verb, List<TaskDefinition> tasks)
        {
            bool first = true;
            foreach (var task in tasks)
            {
                if (task == null)
                    continue;
                _text.Append(first ? (_text.Length > 0 ? "; " : "") + verb + " " : ", ").Append(task.displayName);
                first = false;
            }
        }

        // ---------- Skills ----------

        private void DrawSkills()
        {
            if (!Section(ref _showSkills, "Skills (what they speed up is set per task; their numbers are in the rules above)") ||
                _content == null)
                return;

            Header(("Name", 170), ("Learns faster with", 130), ("", 50));
            foreach (var skill in _content.skills)
            {
                if (skill == null)
                    continue;
                var so = new SerializedObject(skill);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Field(so, "learnsFasterWith", 130);
                SelectButton(skill);
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }
    }
}
