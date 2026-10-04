using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// What lies on each room's floor, drawn on the map: a row of coloured dots under the room, one
    /// per kind of item (coloured by the item's kind), so the player can see what's cached in rooms
    /// Clara isn't in. Hover a dot for the item and its count; a dot swells when more is put down.
    /// Display only: the piles come from Simulation.FloorAt, the order and places from FloorDots,
    /// and the look from the Map Style. Its layer moves and zooms with the map (MapView.AddLayer).
    /// </summary>
    public class MapFloor : MonoBehaviour
    {
        [SerializeField] private MapView _map;
        [Tooltip("Copied once per dot. Keep it in the FloorMarks layer, above the Nodes layer, so the dots sit on top.")]
        [SerializeField] private Image _dotTemplate;

        private TemplateList<Image> _dotImages;

        private readonly List<(ResourceDefinition item, int amount)> _floor = new List<(ResourceDefinition item, int amount)>();
        private readonly List<(ResourceDefinition item, int amount)> _roomDots = new List<(ResourceDefinition item, int amount)>();
        // Every dot drawn this frame, in the order of the copies: its room, item and count, and its place in the room's row.
        private readonly List<(NodeDefinition room, ResourceDefinition item, int amount, int place)> _dots =
            new List<(NodeDefinition room, ResourceDefinition item, int amount, int place)>();
        // How many of each item each room showed last frame and this one (swapped each frame), so a
        // pile that grows swells its dot.
        private Dictionary<(NodeDefinition, ResourceDefinition), int> _shown = new Dictionary<(NodeDefinition, ResourceDefinition), int>();
        private Dictionary<(NodeDefinition, ResourceDefinition), int> _showing = new Dictionary<(NodeDefinition, ResourceDefinition), int>();
        // A new or loaded game: its piles appear quietly.
        private bool _quiet = true;

        private MapStyle Style => _map.Style;

        private void Awake()
        {
            if (_map == null || _dotTemplate == null)
                throw new System.InvalidOperationException($"{name}: MapFloor needs its Map and Dot Template set (setup Step 82).");
        }

        private void Start()
        {
            // In Start, not Awake: the map makes its window in its own Awake, which may run after ours.
            _map.AddLayer((RectTransform)_dotTemplate.transform.parent);

            _dotImages = new TemplateList<Image>(_dotTemplate, (dot, index) =>
                ToolTip.On(dot, () => index < _dots.Count ? DotTip(_dots[index]) : null));
            _map.Game.SimulationChanged += OnSimulationChanged;
        }

        private void OnDestroy()
        {
            if (_map != null && _map.Game != null)
                _map.Game.SimulationChanged -= OnSimulationChanged;
        }

        private void OnSimulationChanged(Simulation sim) => _quiet = true;

        private void Update()
        {
            var sim = _map.Game.Simulation;
            _dots.Clear();
            // No floor-changed event: polled each frame, as the room popover does.
            if (sim != null && sim.HasPlaces)
                foreach (var room in sim.Loop.Floor.Keys)
                    GatherRoom(sim, room);

            // Shown once, after every room is gathered: showing each room's dots as it came switched
            // the later rooms' dots off and on again every frame, which cancelled their tooltips.
            _dotImages.Show(_dots.Count);
            _showing.Clear();
            for (int i = 0; i < _dots.Count; i++)
                PlaceDot(i);

            (_shown, _showing) = (_showing, _shown);
            _quiet = false;
        }

        /// <summary>Adds one room's row of dots (none if its floor is empty or the room isn't on the map).</summary>
        private void GatherRoom(Simulation sim, NodeDefinition room)
        {
            if (_map.RoomRect(room) == null)
                return;
            sim.FloorAt(room, _floor);
            FloorDots.For(_floor, _roomDots);
            for (int i = 0; i < _roomDots.Count; i++)
                _dots.Add((room, _roomDots[i].item, _roomDots[i].amount, i));
        }

        /// <summary>Sizes, places and colours one dot, swelling it if its pile has grown.</summary>
        private void PlaceDot(int index)
        {
            var (room, item, amount, place) = _dots[index];
            var dot = _dotImages[index];
            var rect = (RectTransform)dot.transform;
            rect.sizeDelta = new Vector2(Style.floorDotSize, Style.floorDotSize);
            rect.anchoredPosition = FloorDots.Position(_map.PositionOf(room), _map.RoomRect(room).rect.size, place,
                Style.floorDotSize, Style.floorDotSpacing, Style.floorDotOffset);
            var colour = Style.KindColour(item.kind);
            if (dot.color != colour)
                dot.color = colour;
            // More put down: a brief swell (no tint: the colour is set here every frame).
            if (FloorDots.Rose(_quiet ? null : _shown, (room, item), amount))
                Glow.Flash(dot, tint: false);
            _showing[(room, item)] = amount;
        }

        private static string DotTip((NodeDefinition room, ResourceDefinition item, int amount, int place) dot) =>
            GameText.Get("map.floor_dot_tip", ("amount", UiText.Whole(dot.amount)), ("item", dot.item.DisplayName));
    }
}
