using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Editing the map's layout in the Scene view, without playing. Draws every room and way from
    /// the room assets with the real map templates (so fonts, sprites and Map Style colours are
    /// what the game shows), beside the map panel. Drag a room with the Rect Tool and its Map
    /// Position is written back (Ctrl+Z undoes it). The preview is never saved into the scene and
    /// clears itself before Play. Started and stopped from the Map View's Inspector.
    /// </summary>
    [InitializeOnLoad]
    public static class MapLayoutEditing
    {
        const string RootName = "Map layout (editing: not saved)";

        private class Room
        {
            public NodeDefinition Node;
            public RectTransform Rect;
            public Image Image;
            public TMP_Text Label;
            public Vector2 LastMapPosition;
        }

        private class Link
        {
            public Room From, To;
            public Way Way;
            public Image Image;
        }

        private static MapView _view;
        private static RectTransform _root;
        private static readonly List<Room> _rooms = new List<Room>();
        private static readonly List<Link> _links = new List<Link>();
        private static int _shape; // how many rooms and ways there were: a change means rebuild
        private static float _builtScale, _builtThickness; // the style's numbers it was built with: a change means rebuild

        public static bool IsEditing => _view != null && _root != null;

        static MapLayoutEditing()
        {
            // The game builds its own map: clear the preview before Play, and before scripts reload.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    Stop();
            };
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.update += Tick;
        }

        /// <param name="frame">Move the Scene view to show the whole map. The Tools menu passes false so the author's camera stays put.</param>
        public static void Start(MapView view, bool frame = true)
        {
            Stop();
            if (view == null || view.Game == null || view.Game.Content == null)
            {
                Debug.LogWarning("Map layout: the Map View needs its Game (with Game Content) set.");
                return;
            }
            _view = view;
            Build();
            if (frame)
                Frame();
        }

        public static void Stop()
        {
            if (_root != null)
            {
                // A preview object still selected would leave the Inspector erroring on it once it's gone.
                if (Selection.activeGameObject != null && Selection.activeGameObject.transform.IsChildOf(_root))
                    Selection.activeObject = null;
                Object.DestroyImmediate(_root.gameObject);
            }
            _root = null;
            _view = null;
            _rooms.Clear();
            _links.Clear();
        }

        /// <summary>Moves the Scene view to show the whole map.</summary>
        public static void Frame()
        {
            if (_root == null || SceneView.lastActiveSceneView == null)
                return;
            var bounds = new Bounds(_root.position, Vector3.zero);
            var corners = new Vector3[4];
            foreach (var room in _rooms)
            {
                room.Rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                    bounds.Encapsulate(corner);
            }
            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        // ---------- Building ----------

        private static void Build()
        {
            var content = _view.Game.Content;
            var canvas = _view.GetComponentInParent<Canvas>();
            var root = new GameObject(RootName, typeof(RectTransform));
            root.hideFlags = HideFlags.DontSave;
            _root = (RectTransform)root.transform;
            _root.SetParent(canvas != null ? canvas.rootCanvas.transform : _view.transform.parent, false);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = Vector2.zero;

            // Lines first, so rooms draw on top of them.
            var linesLayer = Layer("Ways");
            var roomsLayer = Layer("Rooms");

            float scale = _view.Style.pixelsPerUnit;
            _builtScale = scale;
            _builtThickness = _view.Style.lineThickness;
            foreach (var node in content.nodes)
            {
                if (node == null)
                    continue;
                var button = Object.Instantiate(_view.NodeTemplate, roomsLayer, false);
                button.gameObject.SetActive(true);
                button.gameObject.name = node.DisplayName;
                Unsave(button.gameObject);
                var preview = button.gameObject.AddComponent<MapPreviewRoom>();
                preview.room = node;
                // First in the Inspector, right under the Rect Transform (which always stays on top), so the
                // room's settings are the first thing seen on selecting it. (Checked: records no Undo step and
                // doesn't dirty the scene.)
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(preview)) { }

                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = node.mapPosition * scale;
                _rooms.Add(new Room
                {
                    Node = node,
                    Rect = rect,
                    Image = button.GetComponent<Image>(),
                    Label = button.GetComponentInChildren<TMP_Text>(),
                    LastMapPosition = node.mapPosition,
                });
            }

            foreach (var from in _rooms)
            {
                foreach (var way in from.Node.ways)
                {
                    var to = _rooms.Find(r => way != null && r.Node == way.to);
                    if (to == null)
                        continue;
                    var line = Object.Instantiate(_view.LineTemplate, linesLayer, false);
                    line.gameObject.SetActive(true);
                    line.gameObject.name = $"{from.Node.DisplayName} → {to.Node.DisplayName}";
                    Unsave(line.gameObject);
                    _links.Add(new Link { From = from, To = to, Way = way, Image = line });
                }
            }

            _shape = ShapeOf(content);
            Refresh();
        }

        private static RectTransform Layer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform));
            layer.hideFlags = HideFlags.DontSave;
            var rect = (RectTransform)layer.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static void Unsave(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
        }

        /// <summary>Rooms and ways counted together: when it changes, the preview is rebuilt.</summary>
        private static int ShapeOf(GameContent content)
        {
            int shape = content.nodes.Count * 1000;
            foreach (var node in content.nodes)
                if (node != null)
                    shape += node.ways.Count;
            return shape;
        }

        // ---------- Keeping up ----------

        private static void Tick()
        {
            if (_view == null || _root == null)
            {
                if (_root != null || _view != null)
                    Stop();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var content = _view.Game != null ? _view.Game.Content : null;
            if (content == null)
            {
                Stop();
                return;
            }
            if (ShapeOf(content) != _shape)
            {
                // A room or way was added or removed: rebuild in place, without moving the Scene view
                // (the author is working where they are looking; Frame map is the button for a full view).
                // The rooms are new objects, so keep the selection on the room she was working on.
                var view = _view;
                var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<MapPreviewRoom>() : null;
                var selectedNode = selected != null ? selected.room : null;
                if (selected != null)
                    Selection.activeObject = null; // or the Inspector errors on the object destroyed under it
                Stop();
                _view = view;
                Build();
                var again = selectedNode != null ? _rooms.Find(r => r.Node == selectedNode) : null;
                if (again != null)
                {
                    var target = again.Rect.gameObject;
                    // Next editor update: a selection set in the same one as the clear is dropped.
                    EditorApplication.delayCall += () =>
                    {
                        if (target != null)
                            Selection.activeGameObject = target;
                    };
                }
                return;
            }
            if (_view.Style.pixelsPerUnit != _builtScale || _view.Style.lineThickness != _builtThickness)
            {
                // The map's scale changed: rebuild in place, without moving the Scene view.
                var view = _view;
                Stop();
                _view = view;
                Build();
                return;
            }

            // Sit over the map panel, wherever it is in the editor.
            _root.position = _view.Area.position;

            float scale = _view.Style.pixelsPerUnit;
            foreach (var room in _rooms)
            {
                if (room.Node == null || room.Rect == null)
                    continue;
                if (room.Node.mapPosition != room.LastMapPosition)
                {
                    // Changed elsewhere (undo, the Balance Sheet): follow the data.
                    room.Rect.anchoredPosition = room.Node.mapPosition * scale;
                    room.LastMapPosition = room.Node.mapPosition;
                }
                else if ((room.Rect.anchoredPosition - room.Node.mapPosition * scale).sqrMagnitude > 0.25f)
                {
                    // Dragged in the Scene view: write it back, rounded so the numbers stay tidy.
                    Vector2 moved = room.Rect.anchoredPosition / scale;
                    moved = new Vector2(Mathf.Round(moved.x * 100f) / 100f, Mathf.Round(moved.y * 100f) / 100f);
                    Undo.RecordObject(room.Node, "Move room on the map");
                    room.Node.mapPosition = moved;
                    room.LastMapPosition = moved;
                    // Snap the rect to the rounded value too, or the check above trips again every update
                    // (and dirties the asset each time).
                    room.Rect.anchoredPosition = moved * scale;
                    EditorUtility.SetDirty(room.Node);
                }
            }
            Refresh();
        }

        /// <summary>How much of its colour a planned room, and a way touching one, keeps.</summary>
        private const float PlannedFade = 0.45f;

        /// <summary>"Name (planned)" with the author's one-line note beneath, if any.</summary>
        private static string PlannedLabel(NodeDefinition node)
        {
            string label = $"{node.DisplayName} (planned)";
            return string.IsNullOrWhiteSpace(node.planningNote) ? label : $"{label}\n<size=75%>{node.planningNote}</size>";
        }

        /// <summary>Labels, colours (from the Map Style, so its changes show at once) and lines.</summary>
        private static void Refresh()
        {
            var style = _view.Style;
            var content = _view.Game.Content;
            var knownAtStart = KnownAtStart(content);

            foreach (var room in _rooms)
            {
                if (room.Node == null)
                    continue;
                bool start = room.Node == content.startNode;
                bool known = knownAtStart.Contains(room.Node);
                bool planned = room.Node.planned;
                if (room.Image != null)
                {
                    Color fill = start ? style.here : known ? style.room : style.sealedRoom;
                    fill.a *= planned ? PlannedFade : 1f;
                    room.Image.color = fill;
                }
                if (room.Label != null)
                {
                    room.Label.text = planned ? PlannedLabel(room.Node)
                        : known ? room.Node.DisplayName : $"{room.Node.DisplayName}\n<size=75%>(found later)</size>";
                    room.Label.alpha = planned ? PlannedFade : 1f;
                }
            }

            float thickness = style.lineThickness;
            foreach (var link in _links)
            {
                MapLines.Place((RectTransform)link.Image.transform,
                    link.From.Rect.anchoredPosition, link.To.Rect.anchoredPosition, thickness);

                // Faint: shut until a switch, found by exploring, or a door that needs something.
                Color colour = link.Way.bothWays ? style.way : style.oneWay;
                bool open = link.Way.startsOpen && link.Way.foundAtExplored <= 0 && link.Way.needs.Count == 0;
                colour.a = open ? 1f : 0.4f;
                // The game ignores a way into or out of a planned room, so the preview draws it faded too.
                if (link.From.Node.planned || link.To.Node.planned)
                    colour.a *= PlannedFade;
                link.Image.color = colour;
            }
        }

        /// <summary>Rooms reachable from the start through ways that are open and known from the start.</summary>
        private static HashSet<NodeDefinition> KnownAtStart(GameContent content)
        {
            var known = new HashSet<NodeDefinition>();
            if (content.startNode == null)
                return known;
            var queue = new Queue<NodeDefinition>();
            known.Add(content.startNode);
            queue.Enqueue(content.startNode);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var other in content.PlayableNodes)
                {
                    if (other == null)
                        continue;
                    foreach (var way in other.ways)
                    {
                        if (way == null || way.IntoPlannedRoom || !way.startsOpen || way.foundAtExplored > 0)
                            continue;
                        NodeDefinition next =
                            other == node ? way.to :
                            way.bothWays && way.to == node ? other : null;
                        if (next != null && known.Add(next))
                            queue.Enqueue(next);
                    }
                }
            }
            // Rooms marked Always On Map show from the start too (the game's Simulation.RoomsOnMap adds them
            // after the walk, without walking on from them).
            foreach (var node in content.PlayableNodes)
                if (node != null && node.alwaysOnMap)
                    known.Add(node);
            return known;
        }
    }
}
