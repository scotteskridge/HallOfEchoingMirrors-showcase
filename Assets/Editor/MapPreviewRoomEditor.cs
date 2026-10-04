using HallOfEchoingMirrors.UI;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Selecting a room in the map layout preview shows that room's own settings (its ways, tasks,
    /// exploring and modifiers) right in the Inspector, so it can be edited where it's placed.
    /// </summary>
    [CustomEditor(typeof(MapPreviewRoom))]
    public class MapPreviewRoomEditor : Editor
    {
        private Editor _roomEditor;

        public override void OnInspectorGUI()
        {
            var room = ((MapPreviewRoom)target).room;
            if (room == null)
                return;

            EditorGUILayout.HelpBox("A room in the map layout preview. Drag it with the Rect Tool (T) to move it; " +
                                    "its settings are below, and changes go straight into the room asset.",
                                    MessageType.Info);
            if (GUILayout.Button("Select the room asset"))
            {
                Selection.activeObject = room;
                EditorGUIUtility.PingObject(room);
            }

            EditorGUILayout.Space(6f);
            CreateCachedEditor(room, null, ref _roomEditor);
            _roomEditor.OnInspectorGUI();
        }

        private void OnDisable()
        {
            if (_roomEditor != null)
                DestroyImmediate(_roomEditor);
        }
    }
}
