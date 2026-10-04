using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's items (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Resources ----------

        private void DrawResources()
        {
            if (!Section(ref _showResources, "Resources (tools, stats and kept)"))
                return;

            EditorGUILayout.LabelField("One table for every item, then one per thing items can do (ItemSections), listing " +
                "only the items that do it. To give an item a new ability, Select it and open that section in the Inspector.",
                EditorStyles.wordWrappedMiniLabel);

            // Every item: what it is, and what it does.
            Header(("Name", 170), ("Lasts", 100), ("Max (0 = none)", 100), ("Pocket", 50), ("Does", 330), ("", 50));
            foreach (var resource in _resources)
            {
                var so = new SerializedObject(resource);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Field(so, "lasts", 100);
                Field(so, "startingMax", 100);
                Field(so, "goesInPocket", 50);
                so.ApplyModifiedProperties(); // before the summary, so it shows the new settings
                Label(DoesOf(resource), 330);
                SelectButton(resource);
                EditorGUILayout.EndHorizontal();
            }

            // Then a table per ability, with only its own columns.
            foreach (var section in ItemSections.All)
            {
                if (section.UsedBy == null)
                    continue;
                var users = _resources.FindAll(section.IsUsedBy);
                string key = "HallOfEchoingMirrors.BalanceSheet.Items." + section.Title;
                EditorGUILayout.Space(6f);
                bool open = EditorGUILayout.Foldout(SessionState.GetBool(key, true), $"{section.Title} ({users.Count})", true);
                SessionState.SetBool(key, open);
                if (!open)
                    continue;
                if (users.Count == 0)
                {
                    EditorGUILayout.LabelField("  No items yet. " + section.Hint, EditorStyles.miniLabel);
                    continue;
                }

                var columns = new List<(string, float)> { ("Name", 170) };
                foreach (string field in section.Fields)
                    columns.Add(ColumnFor(field));
                columns.Add(("", 50));
                Header(columns.ToArray());
                foreach (var resource in users)
                {
                    var so = new SerializedObject(resource);
                    so.Update();
                    EditorGUILayout.BeginHorizontal();
                    Label(resource.DisplayName, 170);
                    foreach (string field in section.Fields)
                    {
                        if (field == "holds")
                            Label(HoldsOf(resource), ColumnFor(field).width, "The items it holds, outside her pockets. Add or remove them with Select.");
                        else
                            Field(so, field, ColumnFor(field).width);
                    }
                    SelectButton(resource);
                    EditorGUILayout.EndHorizontal();
                    so.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>"Restores vitality, An object in her pockets": the sections an item uses.</summary>
        private static string DoesOf(ResourceDefinition item)
        {
            var uses = new List<string>();
            foreach (var section in ItemSections.All)
                if (section.UsedBy != null && section.IsUsedBy(item))
                    uses.Add(section.Title);
            return uses.Count > 0 ? string.Join(", ", uses) : "-";
        }

        // Column titles and widths for item settings in the per-ability tables.
        private static (string title, float width) ColumnFor(string field) => field switch
        {
            "nameInActions" => ("Name in actions", 170),
            "restoreVitality" => ("Restores", 70),
            "restoreSeconds" => ("Over (s)", 60),
            "holds" => ("Holds (edit with Select)", 220),
            "holdsHowMany" => ("How many", 70),
            "addsPockets" => ("+Pockets", 70),
            "addsFloorSpace" => ("+Floor (all held)", 110),
            "carryCostPerSecond" => ("Cost / s", 70),
            "alwaysCharged" => ("Always", 60),
            "drainAtFull" => ("Drain at full", 90),
            "countsUpTo" => ("Counts up to", 90),
            "unlocksSpeed" => ("Speed ×", 70),
            "unlocksRoomSpeed" => ("Room speed", 80),
            "unlocksPlanning" => ("Plan", 50),
            _ => (ObjectNames.NicifyVariableName(field), 100),
        };
    }
}
