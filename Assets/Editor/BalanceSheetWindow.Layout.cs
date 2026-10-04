using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's layout (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Layout helpers ----------

        private static bool Section(ref bool open, string title)
        {
            EditorGUILayout.Space(8f);
            open = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
            return open;
        }

        private static void Header(params (string title, float width)[] columns)
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var (title, width) in columns)
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(width));
            EditorGUILayout.EndHorizontal();
        }

        private static void Field(SerializedObject so, string field, float width)
        {
            var property = so.FindProperty(field);
            if (property != null)
                EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(width));
            else
                EditorGUILayout.LabelField($"({field}?)", GUILayout.Width(width));
        }

        /// <summary>Each item in a list with its amount editable, e.g. "Candle [1]  Flint and steel [1]".</summary>
        private static void AmountFields(SerializedObject so, string list, List<ResourceAmount> items, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            int shown = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i]?.resource == null)
                    continue;
                Label(items[i].resource.DisplayName, 85, items[i].resource.DisplayName);
                Field(so, $"{list}.Array.data[{i}].amount", 35);
                shown++;
            }
            if (shown == 0)
                Label("-", width - 10);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>A task's stat thresholds, e.g. "Perception [2]": the level (this run + mastery) it needs.</summary>
        private static void StatLevelFields(SerializedObject so, TaskDefinition task, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            for (int i = 0; i < task.requiresAttributes.Count; i++)
            {
                Label(task.requiresAttributes[i].attribute.ToString(), 80);
                Field(so, $"requiresAttributes.Array.data[{i}].level", 35);
            }
            if (task.requiresAttributes.Count == 0)
                Label("-", width - 10);
            EditorGUILayout.EndHorizontal();
        }

        // Add or remove a gate with Select; the levels are edited here.
        private static void SkillLevelFields(SerializedObject so, TaskDefinition task, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            for (int i = 0; i < task.requiresSkills.Count; i++)
            {
                var skill = task.requiresSkills[i].skill;
                Label(skill != null ? skill.DisplayName : "(none)", 80);
                Field(so, $"requiresSkills.Array.data[{i}].level", 35);
            }
            if (task.requiresSkills.Count == 0)
                Label("-", width - 10);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Who pays a task's cost and in what share, e.g. "Vitality [60] Blue [40]": both editable (shares scale to 100). Add or remove shares with Select.</summary>
        private static void CostShareFields(SerializedObject so, TaskDefinition task, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            for (int i = 0; i < task.costShares.Count; i++)
            {
                Field(so, $"costShares.Array.data[{i}].source", 85);
                Field(so, $"costShares.Array.data[{i}].percent", 40);
            }
            if (task.costShares.Count == 0)
                Label("-", width - 10);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>What a held item does to a task, e.g. "The mana stone's warmth [0.5] [1]": cost × and time × each.</summary>
        private static void EasierWithFields(SerializedObject so, TaskDefinition task, float width)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(width));
            int shown = 0;
            for (int i = 0; i < task.easierWith.Count; i++)
            {
                if (task.easierWith[i]?.whileHolding == null)
                    continue;
                Label(task.easierWith[i].whileHolding.DisplayName, 110, task.easierWith[i].whileHolding.DisplayName);
                Field(so, $"easierWith.Array.data[{i}].costEach", 40);
                Field(so, $"easierWith.Array.data[{i}].timeEach", 40);
                shown++;
            }
            if (shown == 0)
                Label("-", width - 10);
            EditorGUILayout.EndHorizontal();
        }

        private static void Label(string text, float width, string tooltip = null) =>
            EditorGUILayout.LabelField(new GUIContent(text, tooltip ?? text), GUILayout.Width(width));

        private static void SelectButton(Object asset)
        {
            if (GUILayout.Button(new GUIContent("Select", "Show it in the Inspector, for lists like cost shares and gives."), GUILayout.Width(50)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
