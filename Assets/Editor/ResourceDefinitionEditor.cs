using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// An item's Inspector, in the groups of ItemSections: the ones it uses are open, the rest are
    /// closed (open one to set it up: giving a setting a value makes the item use it).
    /// </summary>
    [CustomEditor(typeof(ResourceDefinition))]
    public class ResourceDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var item = (ResourceDefinition)target;

            foreach (var section in ItemSections.All)
            {
                EditorGUILayout.Space(4f);
                if (section.UsedBy == null || section.IsUsedBy(item))
                {
                    EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                    DrawFields(section);
                    continue;
                }

                // Unused: closed unless opened here (remembered for this editor session).
                string key = "HallOfEchoingMirrors.ItemSection." + section.Title;
                bool open = EditorGUILayout.Foldout(SessionState.GetBool(key, false),
                    $"{section.Title}  (not used)", true);
                SessionState.SetBool(key, open);
                if (open)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(section.Hint, EditorStyles.wordWrappedMiniLabel);
                    DrawFields(section);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_id"), new GUIContent("Id (for saves)"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFields(ItemSections.Section section)
        {
            foreach (string field in section.Fields)
            {
                var property = serializedObject.FindProperty(field);
                if (property != null)
                    EditorGUILayout.PropertyField(property, true);
                else
                    EditorGUILayout.HelpBox($"No setting called {field} (ItemSections is out of date).", MessageType.Warning);
            }
        }
    }
}
