using HallOfEchoingMirrors.UI;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// The Map View's Inspector, with its Map Style's settings shown right underneath, so the
    /// map's colours, scale and following can be changed where the map is, not only on the asset.
    /// </summary>
    [CustomEditor(typeof(MapView))]
    public class MapViewEditor : Editor
    {
        // SessionState keeps the foldout open or shut for the editor session without static state.
        const string ShowStyleKey = "MapViewEditor.ShowStyle";
        private Editor _styleEditor;

        public override void OnInspectorGUI()
        {
            DrawLayoutButtons();
            DrawDefaultInspector();

            var style = serializedObject.FindProperty("_style").objectReferenceValue as MapStyle;
            EditorGUILayout.Space(8f);
            if (style == null)
            {
                EditorGUILayout.HelpBox("No Map Style: the map uses the built-in look. Drag one into Style above " +
                                        "(or make one: Create → Hall of Echoing Mirrors → Map Style).", MessageType.Info);
                return;
            }

            bool showStyle = EditorGUILayout.Foldout(SessionState.GetBool(ShowStyleKey, true),
                $"Map Style: {style.name}", true, EditorStyles.foldoutHeader);
            SessionState.SetBool(ShowStyleKey, showStyle);
            if (!showStyle)
                return;

            EditorGUILayout.HelpBox("Shared by every map: changes here change the asset. Changes made while " +
                                    "playing are kept when you stop.", MessageType.None);
            CreateCachedEditor(style, null, ref _styleEditor);
            using (new EditorGUI.IndentLevelScope())
                _styleEditor.OnInspectorGUI();
        }

        /// <summary>Editing the map's layout in the Scene view (not while playing).</summary>
        private void DrawLayoutButtons()
        {
            if (EditorApplication.isPlaying)
                return;

            EditorGUILayout.LabelField("Map layout", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var view = (MapView)target;
                var content = view.Game != null ? view.Game.Content : null;
                using (new EditorGUI.DisabledScope(content == null))
                    if (GUILayout.Button(new GUIContent("Add Room",
                            "Adds a Planned room (faded; ignored by the game until you untick Planned) to lay out a later act.")))
                    {
                        MapPlannerMenu.AddPlannedRoom(content);
                        GUIUtility.ExitGUI(); // the Inspector is about to show the new room: don't finish drawing this one
                    }
                if (!MapLayoutEditing.IsEditing)
                {
                    if (GUILayout.Button("Edit map layout"))
                        MapLayoutEditing.Start((MapView)target);
                }
                else
                {
                    if (GUILayout.Button("Stop editing"))
                        MapLayoutEditing.Stop();
                    if (GUILayout.Button("Frame map"))
                        MapLayoutEditing.Frame();
                }
            }
            EditorGUILayout.HelpBox(MapLayoutEditing.IsEditing
                ? "Every room is shown beside the panel (dimmed ones are found later; faint ways are shut, found by " +
                  "exploring, or doors; faded ones are Planned rooms the game ignores). Drag rooms with the Rect Tool (T); click one to edit its settings. Not saved " +
                  "into the scene; it clears itself before Play."
                : "Shows every room in the Scene view, with the real look, so you can arrange them by dragging.",
                MessageType.None);
            EditorGUILayout.Space(6f);
        }

        private void OnDisable()
        {
            if (_styleEditor != null)
                DestroyImmediate(_styleEditor);
        }
    }
}
