using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// The map planner's menu entries (plan ui-053): open the map preview from the Tools menu, and add a planned
    /// room (one the game ignores until its Planned tick is cleared) to lay out a later act. The Map View's
    /// Inspector has the same Add Room button.
    /// </summary>
    public static class MapPlannerMenu
    {
        const string MapLayoutItem = "Hall of Echoing Mirrors/Tools/Map Layout";
        const string AddRoomItem = "Hall of Echoing Mirrors/Tools/Add Planned Room";
        const string PlacesFolder = "Assets/Data/Places";

        [MenuItem(MapLayoutItem)]
        private static void ToggleMapLayout()
        {
            if (MapLayoutEditing.IsEditing)
            {
                MapLayoutEditing.Stop();
                return;
            }
            var view = FindMapView();
            if (view != null)
                MapLayoutEditing.Start(view, frame: false);
        }

        [MenuItem(MapLayoutItem, true)]
        private static bool ToggleMapLayoutValid()
        {
            Menu.SetChecked(MapLayoutItem, MapLayoutEditing.IsEditing);
            return !EditorApplication.isPlaying;
        }

        [MenuItem(AddRoomItem)]
        private static void AddRoomFromMenu()
        {
            var view = FindMapView();
            if (view != null && view.Game != null && view.Game.Content != null)
                AddPlannedRoom(view.Game.Content);
            else if (view != null)
                Debug.LogWarning("Add Planned Room: the Map View needs its Game (with Game Content) set.");
        }

        [MenuItem(AddRoomItem, true)]
        private static bool AddRoomValid() => !EditorApplication.isPlaying;

        /// <summary>The scene's one Map View, or null with a warning (editor setup scripts may use Find*).</summary>
        private static MapView FindMapView()
        {
            var view = Object.FindFirstObjectByType<MapView>(FindObjectsInactive.Include);
            if (view == null)
                Debug.LogWarning("Map Layout: this scene has no Map View. Open the scene with the map (SampleScene).");
            return view;
        }

        /// <summary>
        /// Makes a new Planned room asset (kind Hall) in Assets/Data/Places, adds it to the Game Content, and puts
        /// it at the middle of the rooms already laid out. One Undo step undoes all of it. Returns the new room.
        /// </summary>
        public static NodeDefinition AddPlannedRoom(GameContent content)
        {
            if (content == null)
                throw new System.ArgumentNullException(nameof(content));

            int number = 1;
            string path;
            while (AssetDatabase.LoadAssetAtPath<Object>(path = $"{PlacesFolder}/New Room {number}.asset") != null)
                number++;

            Undo.IncrementCurrentGroup();
            var room = ScriptableObject.CreateInstance<NodeDefinition>();
            room.displayName = $"New Room {number}";
            room.planned = true;
            room.mapPosition = FreeSpotNearMiddle(content);
            AssetDatabase.CreateAsset(room, path);
            room.StampId();
            EditorUtility.SetDirty(room);
            Undo.RegisterCreatedObjectUndo(room, "Add planned room");

            Undo.RecordObject(content, "Add planned room");
            content.nodes.Add(room);
            EditorUtility.SetDirty(content);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            Selection.activeObject = room;
            Debug.Log($"Map planner: added '{room.displayName}' (Planned) at {room.mapPosition}. Untick Planned to bring it into the game.");
            return room;
        }

        /// <summary>The middle of every room's map position, nudged right until no room sits on it.</summary>
        private static Vector2 FreeSpotNearMiddle(GameContent content)
        {
            var bounds = new Bounds();
            bool any = false;
            foreach (var node in content.nodes)
            {
                if (node == null)
                    continue;
                if (!any)
                    bounds = new Bounds(node.mapPosition, Vector3.zero);
                else
                    bounds.Encapsulate(node.mapPosition);
                any = true;
            }
            var spot = new Vector2(Mathf.Round(bounds.center.x), Mathf.Round(bounds.center.y));
            while (SitsOnARoom(content, spot))
                spot.x += 1f;
            return spot;
        }

        private static bool SitsOnARoom(GameContent content, Vector2 spot)
        {
            foreach (var node in content.nodes)
                if (node != null && (node.mapPosition - spot).sqrMagnitude < 0.25f)
                    return true;
            return false;
        }
    }
}
