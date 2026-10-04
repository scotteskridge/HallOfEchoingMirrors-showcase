using HallOfEchoingMirrors.UI;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>Adds page-editing buttons to the Screen Manager's Inspector.</summary>
    [CustomEditor(typeof(ScreenManager))]
    public class ScreenManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var screens = (ScreenManager)target;
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Editing pages", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Where pages sit here doesn't affect the game: when Play starts, each page slides into view when its tab is clicked.",
                MessageType.None);

            if (GUILayout.Button("Lay pages out side by side"))
                PageLayout.LayOutSideBySide(screens);

            foreach (var entry in screens.Screens)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string name = entry.group != null ? entry.group.name : "(no page)";
                    EditorGUILayout.LabelField($"{entry.id}: {name}", GUILayout.MinWidth(120f));
                    using (new EditorGUI.DisabledScope(entry.group == null))
                    {
                        if (GUILayout.Button("Frame", GUILayout.Width(70f)))
                            PageLayout.Frame(entry);
                        if (GUILayout.Button("Preview in Game view", GUILayout.Width(150f)))
                            PageLayout.Preview(screens, entry);
                    }
                }
            }
        }
    }
}
