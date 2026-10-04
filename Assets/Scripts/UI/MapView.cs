using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The map: a window onto the whole map, which is laid out at a fixed scale (so rooms keep
    /// their size however big it grows) and slides around under the window to keep the room Clara
    /// is in at the middle. Drag it to look around; it glides back after a moment (MapWindow).
    /// It lives in the main page's Map frame, filling it, so moving or resizing the frame moves the map.
    /// Clicking a room next to where the queue ends schedules the trip; clicking any room opens its
    /// popover (RoomPopover), and clicking the empty map closes it.
    /// </summary>
    public class MapView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        [SerializeField] private GameController _game;
        [Tooltip("The window: rooms and ways outside it are hidden.")]
        [SerializeField] private RectTransform _area;
        [Tooltip("Colours, scale and following. Empty: the built-in look.")]
        [SerializeField] private MapStyle _style;
        [Tooltip("Copied once per room. Keep it inside the Nodes container, above the Lines one.")]
        [SerializeField] private Button _nodeTemplate;
        [Tooltip("Copied once per way between two rooms.")]
        [SerializeField] private Image _lineTemplate;
        [Tooltip("Optional: opened over the map for the clicked room (its actions, floor and ways on).")]
        [SerializeField] private RoomPopover _popover;
        [Tooltip("How often (seconds) it checks for newly found rooms and which are next stops. " +
                 "Colours and following Clara update every frame.")]
        [SerializeField, Min(0f)] private float _refreshSeconds = 0.2f;

        private TemplateList<MapRoom> _rooms;
        private TemplateList<Image> _lines;

        // Which rooms and ways are drawn (_nodes, _shownLines are its lists), and which rooms are next stops.
        private readonly MapContents _contents = new MapContents();
        private List<NodeDefinition> _nodes => _contents.Nodes;
        private List<MapContents.Line> _shownLines => _contents.Lines;

        private readonly List<QueueStop> _stops = new List<QueueStop>();
        private readonly Dictionary<NodeDefinition, int> _shownExplored = new Dictionary<NodeDefinition, int>();

        private float _laidOutScale, _laidOutThickness; // the style's numbers the rooms were placed with
        private MapWindow _window;
        private RectTransform _tagLayer; // made in code, above the rooms (MakeTagLayer)
        private float _nextRefresh;

        private Simulation Sim => _game.Simulation;

        // For the editor's layout preview, which draws the same rooms and ways without a game running.
        public MapStyle Style => _style != null ? _style : MapStyle.Fallback;
        public RectTransform Area => _area;
        public Button NodeTemplate => _nodeTemplate;
        public Image LineTemplate => _lineTemplate;
        public GameController Game => _game;

        /// <summary>Set by the planning screen: each room known by heart also shows its room speed now and after one more run (once room speed is unlocked).</summary>
        public bool ShowSpeedLines { get; set; }

        /// <summary>Set by the planning screen: a room whose stops will be refused wears a ⚠ after its tag, and one that won't carry over says so.</summary>
        public bool ShowPlanMarks { get; set; }

        private void Awake()
        {
            // Assigned on the MapPanel's instance in the scene (the prefab itself leaves it empty).
            if (_game == null)
            {
                Debug.LogError("MapView: the Game field is empty, so the map is switched off. Assign the GameController in the Inspector.", this);
                enabled = false;
                return;
            }

            // The window hides whatever lies outside it.
            if (_area.GetComponent<RectMask2D>() == null)
                _area.gameObject.AddComponent<RectMask2D>();
            _window = new MapWindow(_area, GetComponentInParent<Canvas>());
            _window.AddLayer((RectTransform)_nodeTemplate.transform.parent);
            _window.AddLayer((RectTransform)_lineTemplate.transform.parent);
            _tagLayer = MakeTagLayer();
            _window.AddLayer(_tagLayer);
        }

        /// <summary>
        /// The rooms' tags get a layer of their own just above the rooms: drawn inside each room, a
        /// two-line tag ran under the frame of the room laid out after it.
        /// </summary>
        private RectTransform MakeTagLayer()
        {
            var nodes = (RectTransform)_nodeTemplate.transform.parent;
            var layer = new GameObject("RoomTags", typeof(RectTransform));
            var rect = (RectTransform)layer.transform;
            rect.SetParent(nodes.parent, false);
            rect.SetSiblingIndex(nodes.GetSiblingIndex() + 1);
            rect.anchorMin = Vector2.zero; // fills the window, pivot at the centre, as MapWindow.AddLayer needs
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void Start()
        {
            // The template gets its MapRoom here, so every copy carries one; each copy builds its own parts.
            var roomTemplate = _nodeTemplate.GetComponent<MapRoom>();
            if (roomTemplate == null)
                roomTemplate = _nodeTemplate.gameObject.AddComponent<MapRoom>();
            _lines = new TemplateList<Image>(_lineTemplate);
            _rooms = new TemplateList<MapRoom>(roomTemplate, (room, index) =>
            {
                room.Build(Style, _tagLayer);
                room.Button.onClick.AddListener(() => OnNodeClicked(index));
                ToolTip.On(room.Button, () => RoomTip(index));
            });

            _game.SimulationChanged += OnSimulationChanged;
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= OnSimulationChanged;
        }

        /// <summary>
        /// A new or loaded game: nothing has "just" been searched, the window jumps straight to her,
        /// and the known rooms are looked at now rather than at the next refresh.
        /// </summary>
        private void OnSimulationChanged(Simulation sim)
        {
            _shownExplored.Clear();
            _window.JumpNext();
            _nextRefresh = 0f;
            if (_popover != null)
                _popover.Close(); // its rows were built from the old game
        }

        private void Update()
        {
            if (Sim == null)
                return;
            if (!Sim.HasPlaces)
            {
                _rooms.Show(0);
                _lines.Show(0);
                return;
            }

            // Which rooms are known, and which are next stops, change rarely: look a few times a second,
            // and straight away for a new or loaded game (OnSimulationChanged).
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + _refreshSeconds;
                // Rebuild only when what's known changes (a switch opened a way)...
                _contents.GatherKnownRooms(Sim);
                // ...or when the style's scale or line thickness is changed (they can be tuned while playing).
                if (!_contents.SameAsShown() || Style.pixelsPerUnit != _laidOutScale || Style.lineThickness != _laidOutThickness)
                    LayOut();
                _contents.FindNextStops(Sim);
                RefreshTexts();
            }

            _window.Follow(Here != null ? PositionOf(Here) : (Vector2?)null, Style);
            ShowStates();
        }

        // ---------- Layout ----------

        private void LayOut()
        {
            _laidOutScale = Style.pixelsPerUnit;
            _laidOutThickness = Style.lineThickness;
            _contents.Commit();

            _rooms.Show(_nodes.Count);
            for (int i = 0; i < _nodes.Count; i++)
            {
                _rooms[i].Rect.anchoredPosition = PositionOf(_nodes[i]);
                _rooms[i].PlaceTag(Style);
                _rooms[i].SetLabel(_nodes[i].DisplayName);
            }

            _lines.Show(_shownLines.Count);
            for (int i = 0; i < _shownLines.Count; i++)
            {
                MapLines.Place((RectTransform)_lines[i].transform,
                    PositionOf(_shownLines[i].A), PositionOf(_shownLines[i].B), Style.lineThickness);
                _lines[i].color = _shownLines[i].Shut ? Style.shutWay : _shownLines[i].BothWays ? Style.way : Style.oneWay;
            }
        }

        /// <summary>A room's place on the whole map, in pixels: its Map Position at the style's scale.</summary>
        public Vector2 PositionOf(NodeDefinition node) => node.mapPosition * Style.pixelsPerUnit;

        // ---------- The window (MapWindow does the moving) ----------

        /// <summary>
        /// Moves and zooms <paramref name="layer"/> with the rooms, so what's drawn on it at a room's
        /// PositionOf stays on that room. It must fill the window with its pivot at the centre.
        /// </summary>
        public void AddLayer(RectTransform layer) => _window.AddLayer(layer);

        public void OnBeginDrag(PointerEventData eventData) => _window.BeginDrag();
        public void OnDrag(PointerEventData eventData) => _window.Drag(eventData.delta);
        public void OnEndDrag(PointerEventData eventData) => _window.EndDrag();

        public void OnScroll(PointerEventData eventData) =>
            _window.Scroll(eventData.scrollDelta.y, eventData.position, eventData.enterEventCamera, Style);

        // ---------- Colours and clicks ----------

        /// <summary>The room the map centres on: where she is (the start room between runs).</summary>
        private NodeDefinition Here => Sim.PlannedNodeAfter(0);

        private void ShowStates()
        {
            var here = Here;
            var heading = Sim.Loop.IsOver ? null : Sim.Loop.CurrentDestination;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                // Clickable (a trip can be scheduled there), or where she's heading now.
                bool reachable = _contents.IsNextStop(node) || node == heading;
                Color colour =
                    node == here ? Style.here :
                    reachable ? Style.reachable :
                    _contents.HasWayIn(node) ? Style.room : Style.sealedRoom;
                _rooms[i].SetColour(colour);

                bool explorable = Sim.HasSomethingToExplore(node);
                if (explorable)
                {
                    // A whole step of the search done: the room swells briefly (no tint: its colour is set
                    // here every frame). Not on every rise: the bar fills a little each tick, and a swell
                    // restarted every frame makes the room shudder.
                    int steps = Sim.SearchStepsDoneIn(node);
                    if (_shownExplored.TryGetValue(node, out int before) && steps > before)
                        Glow.Flash(_rooms[i].Image, tint: false);
                    _shownExplored[node] = steps;
                }
                _rooms[i].ShowExploreBar(explorable, explorable ? Sim.ExploredFraction(node) : 0f, Style);
            }
        }

        // The words on each room: built with the few-times-a-second refresh, not every frame (they change
        // when she moves on, finishes a run or learns a room, not as the bars fill).
        private void RefreshTexts()
        {
            var here = Here;
            if (ShowPlanMarks)
                Sim.QueueStops(_stops);
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                string label = node == here ? UiStyle.Bold(node.DisplayName) : node.DisplayName;
                float roomSpeed = Sim.RoomSpeed(node);
                if (Sim.RoomSpeedUnlocked && roomSpeed > 1f)
                    label += " " + GameText.Get("map.room_speed", ("speed", UiText.Number(roomSpeed)));
                _rooms[i].SetLabel(label);
                bool byHeart = Sim.IsKnownByHeart(node);
                _rooms[i].SetTag(MapTagText.TagText(Sim, node, Style, ShowSpeedLines, ShowPlanMarks) + MapTagText.NotCarriedLine(_stops, node, ShowPlanMarks), byHeart ? Style.byHeartTag : Style.roomTag, byHeart, Style.byHeartGlowColour);
            }
        }

        private string RoomTip(int index) =>
            Sim == null || index >= _nodes.Count ? null : MapTagText.RoomTip(Sim, _nodes[index], ShowPlanMarks);

        private void OnNodeClicked(int index) => ClickRoom(_nodes[index]);

        /// <summary>
        /// A click on a room (on the map, or its chip in a popover): schedules the trip there when
        /// it can be (or toasts why not), and opens its popover.
        /// </summary>
        public void ClickRoom(NodeDefinition node)
        {
            Sim.TryScheduleTrip(node); // or a toast saying why not
            _nextRefresh = 0f; // the next stops have moved on
            if (_popover != null)
                _popover.Open(node);
        }

        /// <summary>The window snaps to her room at once, instead of gliding there (the map was away on another page).</summary>
        public void JumpToHere()
        {
            _window.JumpNext();
            _nextRefresh = 0f;
        }

        /// <summary>A click on the empty map (not a drag) closes the popover.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging && _popover != null)
                _popover.Close();
        }

        /// <summary>Where a room is drawn now, or null if it isn't on the map.</summary>
        public RectTransform RoomRect(NodeDefinition node)
        {
            int index = _nodes.IndexOf(node);
            return index >= 0 && index < _rooms.Count ? _rooms[index].Rect : null;
        }
    }
}
