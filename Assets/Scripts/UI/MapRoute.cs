using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The queue's route drawn on the map: a line from room to room for each trip, a badge on each
    /// room it stops at with the stop numbers (a room she comes back to shows "1, 3"), and Clara's
    /// token, which rests by her room and walks the line on a trip. A newly scheduled leg grows in,
    /// and the part of the line she has walked disappears behind her. Display only: the stops come
    /// from Simulation.QueueStops, the maths from RouteLayout, and the look from the Map Style.
    /// Its layers move and zoom with the map (MapView.AddLayer).
    /// </summary>
    public class MapRoute : MonoBehaviour
    {
        [SerializeField] private MapView _map;
        [Tooltip("Copied once per leg of the route. Keep it in the Route layer, between the Lines and Nodes layers, so the rooms sit on top.")]
        [SerializeField] private Image _legTemplate;
        [Tooltip("Copied once per room the route stops at; its text shows the stop numbers. Keep it in the RouteMarks layer, above the Nodes layer.")]
        [SerializeField] private Image _badgeTemplate;
        [Tooltip("Clara's token: the dot (its label is a child). In the RouteMarks layer.")]
        [SerializeField] private Image _token;

        private TemplateList<Image> _legImages;
        private TemplateList<Image> _badgeImages;

        private readonly List<QueueStop> _stops = new List<QueueStop>();
        private readonly List<NodeDefinition> _shownRooms = new List<NodeDefinition>(); // the stops the legs and badges were built for
        // Each leg drawn, and when it appeared (for the draw-in).
        private readonly List<(NodeDefinition from, NodeDefinition to, float born)> _legs = new List<(NodeDefinition from, NodeDefinition to, float born)>();
        private readonly List<(NodeDefinition from, NodeDefinition to, float born)> _oldLegs = new List<(NodeDefinition from, NodeDefinition to, float born)>();
        private readonly List<(NodeDefinition from, NodeDefinition to)> _scratchLegs = new List<(NodeDefinition from, NodeDefinition to)>();
        private readonly List<TMP_Text> _badgeLabels = new List<TMP_Text>(); // one per badge copy, in order
        private readonly List<(NodeDefinition room, List<int> numbers)> _badges = new List<(NodeDefinition room, List<int> numbers)>();
        private readonly List<string> _scratchNumbers = new List<string>();
        // What each room's badge said before the last rebuild, so a badge whose numbers change glows.
        private readonly Dictionary<NodeDefinition, string> _shownLabels = new Dictionary<NodeDefinition, string>();

        private Vector2 _tokenPosition;
        private bool _tokenPlaced;
        // A new or loaded game: its route is shown as it is, without growing in.
        private bool _skipDrawIn = true;

        private MapStyle Style => _map.Style;

        private void Awake()
        {
            if (_map == null || _legTemplate == null || _badgeTemplate == null || _token == null)
                throw new System.InvalidOperationException($"{name}: MapRoute needs its Map, Leg Template, Badge Template and Token set (setup Step 81).");
            if (_badgeTemplate.GetComponentInChildren<TMP_Text>(true) == null)
                throw new System.InvalidOperationException($"{name}: the Badge Template needs a text child for its stop numbers.");
        }

        private void Start()
        {
            // In Start, not Awake: the map makes its window in its own Awake, which may run after ours.
            _map.AddLayer((RectTransform)_legTemplate.transform.parent);
            _map.AddLayer((RectTransform)_badgeTemplate.transform.parent);
            _map.AddLayer((RectTransform)_token.transform.parent);

            _legImages = new TemplateList<Image>(_legTemplate);
            _badgeImages = new TemplateList<Image>(_badgeTemplate, (badge, index) =>
            {
                _badgeLabels.Add(badge.GetComponentInChildren<TMP_Text>(true));
                ToolTip.On(badge, () => index < _badges.Count ? BadgeTip(_badges[index].numbers) : null);
            });
            _map.Game.SimulationChanged += OnSimulationChanged;
        }

        private void OnDestroy()
        {
            if (_map != null && _map.Game != null)
                _map.Game.SimulationChanged -= OnSimulationChanged;
        }

        private void OnSimulationChanged(Simulation sim)
        {
            _tokenPlaced = false;
            _skipDrawIn = true;
            _shownRooms.Clear();
        }

        private void Update()
        {
            var sim = _map.Game.Simulation;
            if (sim == null || !sim.HasPlaces)
            {
                _legImages.Show(0);
                _badgeImages.Show(0);
                UiText.SetActive(_token, false);
                return;
            }

            sim.QueueStops(_stops);
            if (!SameRoute())
                Rebuild();
            _skipDrawIn = false;

            // The token first: the line she's walking ends where it is.
            var walking = MoveToken(sim);
            DrawLegs(walking);
            PlaceBadges();
        }

        // ---------- The route's shape ----------

        private bool SameRoute()
        {
            if (_stops.Count != _shownRooms.Count)
                return false;
            for (int i = 0; i < _stops.Count; i++)
                if (_stops[i].Room != _shownRooms[i])
                    return false;
            return true;
        }

        /// <summary>Works out the legs and badges again, keeping when each leg that's still there appeared.</summary>
        private void Rebuild()
        {
            _shownRooms.Clear();
            foreach (var stop in _stops)
                _shownRooms.Add(stop.Room);

            _oldLegs.Clear();
            _oldLegs.AddRange(_legs);

            RouteLayout.Legs(_stops, _scratchLegs);
            _legs.Clear();
            foreach (var (from, to) in _scratchLegs)
            {
                int old = _oldLegs.FindIndex(o => RouteLayout.SameWay(o.from, o.to, from, to));
                _legs.Add((from, to, old >= 0 ? _oldLegs[old].born : _skipDrawIn ? float.NegativeInfinity : Time.unscaledTime));
            }
            _legImages.Show(_legs.Count);

            RouteLayout.Badges(_stops, _badges);
            _badgeImages.Show(_badges.Count);
            var before = _skipDrawIn ? null : new Dictionary<NodeDefinition, string>(_shownLabels);
            _shownLabels.Clear();
            for (int i = 0; i < _badges.Count; i++)
            {
                string text = Numbers(_badges[i].numbers);
                _shownLabels[_badges[i].room] = text;
                UiText.Set(_badgeLabels[i], text);
                // New or renumbered (she moved on, or a trip was added): a brief swell draws the eye.
                // No tint: PlaceBadges sets the badge's colour. A new or loaded game shows its badges quietly.
                if (before != null && (!before.TryGetValue(_badges[i].room, out string old) || old != text))
                    Glow.Flash(_badgeImages[i], tint: false);
            }
        }

        // ---------- Drawing ----------

        /// <summary>
        /// Each leg from room centre to room centre, grown in over the style's draw-in time; the
        /// leg she's walking (<paramref name="walking"/>, 0 to 1) starts where she has got to.
        /// </summary>
        private void DrawLegs(float walking)
        {
            for (int i = 0; i < _legs.Count; i++)
            {
                var image = _legImages[i];
                float grown = Style.routeDrawInSeconds > 0f ? Mathf.Clamp01((Time.unscaledTime - _legs[i].born) / Style.routeDrawInSeconds) : 1f;
                float from = i == 0 ? walking : 0f;
                var (start, end) = RouteLayout.Part(_map.PositionOf(_legs[i].from), _map.PositionOf(_legs[i].to), from, Mathf.Max(from, grown));
                MapLines.Place((RectTransform)image.transform, start, end, Style.routeThickness);
                if (image.color != Style.route)
                    image.color = Style.route;
            }
        }

        /// <summary>Each badge on the top-left corner of its room, hidden if the room isn't drawn yet.</summary>
        private void PlaceBadges()
        {
            for (int i = 0; i < _badges.Count; i++)
            {
                var badge = _badgeImages[i];
                var room = _map.RoomRect(_badges[i].room);
                UiText.SetActive(badge, room != null);
                if (room == null)
                    continue;
                var size = room.rect.size;
                ((RectTransform)badge.transform).anchoredPosition = _map.PositionOf(_badges[i].room) + new Vector2(-size.x, size.y) / 2f;
                if (badge.color != Style.badge)
                    badge.color = Style.badge;
                if (_badgeLabels[i].color != Style.badgeText)
                    _badgeLabels[i].color = Style.badgeText;
            }
        }

        /// <summary>
        /// Eases the token towards where she is: by her room's bottom-right corner, or on a trip as far
        /// along the way as the trip has gone. The easing is only for the eye; the run's timing is
        /// untouched. Returns how far along the first leg she has walked (0 when she isn't on a trip).
        /// </summary>
        private float MoveToken(Simulation sim)
        {
            var here = _stops[0].Room;
            var hereRect = _map.RoomRect(here);
            UiText.SetActive(_token, hereRect != null);
            if (hereRect == null)
                return 0f;

            var tokenRect = (RectTransform)_token.transform;
            tokenRect.sizeDelta = new Vector2(Style.tokenSize, Style.tokenSize);
            if (_token.color != Style.token)
                _token.color = Style.token;

            var roomSize = hereRect.rect.size;
            var rest = new Vector2(roomSize.x / 2f + Style.tokenSize, -roomSize.y / 2f + Style.tokenSize / 2f);
            var destination = sim.Loop.IsOver ? null : sim.Loop.CurrentDestination;
            Vector2 from = _map.PositionOf(here);
            Vector2? heading = destination != null ? _map.PositionOf(destination) : (Vector2?)null;
            var target = RouteLayout.TokenPoint(from, heading, sim.Loop.CurrentTaskProgress, rest);

            _tokenPosition = _tokenPlaced
                ? Vector2.Lerp(_tokenPosition, target, 1f - Mathf.Exp(-Style.tokenEase * Time.unscaledDeltaTime))
                : target;
            _tokenPlaced = true;
            tokenRect.anchoredPosition = _tokenPosition;

            // The first leg is the trip under way (a trip she can't make starts no stop, so draws nothing).
            bool walkingFirstLeg = heading.HasValue && _legs.Count > 0 && _legs[0].from == here && _legs[0].to == destination;
            return walkingFirstLeg ? RouteLayout.FractionAlong(from, heading.Value, _tokenPosition - rest) : 0f;
        }

        // ---------- Text ----------

        /// <summary>The stop numbers as one line, e.g. "1, 3".</summary>
        private string Numbers(List<int> numbers)
        {
            _scratchNumbers.Clear();
            foreach (int number in numbers)
                _scratchNumbers.Add(number.ToString());
            return UiText.List(_scratchNumbers);
        }

        private string BadgeTip(List<int> numbers) =>
            GameText.Get(numbers.Count > 1 ? "map.badge_tip_many" : "map.badge_tip", ("stops", Numbers(numbers)));
    }
}
