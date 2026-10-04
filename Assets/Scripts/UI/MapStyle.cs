using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// How the map looks and moves: its scale, the colours rooms and ways take in each state, and
    /// how the window follows Clara. Edit it in the Inspector; no code needed. (The shapes, fonts
    /// and sprites of rooms, lines and text are in the MapPanel prefab.)
    /// </summary>
    [CreateAssetMenu(fileName = "MapStyle", menuName = "Hall of Echoing Mirrors/Map Style")]
    public class MapStyle : ScriptableObject
    {
        [Header("Scale")]
        [Tooltip("Pixels per map unit: a room's Map Position times this is where it sits. Rooms keep their size however big the map gets.")]
        [Min(10f)] public float pixelsPerUnit = 220f;
        [Min(1f)] public float lineThickness = 4f;

        [Header("Zoom (mouse wheel over the map)")]
        [Tooltip("Furthest out (0.5 = everything half size).")]
        [Range(0.1f, 1f)] public float minZoom = 0.4f;
        [Tooltip("Furthest in (2 = everything twice the size).")]
        [Range(1f, 5f)] public float maxZoom = 2.5f;
        [Tooltip("How much one notch of the wheel zooms (0.15 = 15%).")]
        [Range(0.01f, 1f)] public float zoomStep = 0.15f;

        [Header("Following Clara")]
        [Tooltip("How quickly the window glides to her room. Higher is snappier.")]
        [Min(0.5f)] public float followSpeed = 6f;
        [Tooltip("After the player drags the map to look around, how long before it glides back to her.")]
        [Min(0f)] public float returnAfterSeconds = 4f;

        [Header("Backdrop (behind and over the map)")]
        [Tooltip("Colours the dark backdrop picture behind the rooms. White shows the picture as it is.")]
        public Color backdropTint = Color.white;
        [Tooltip("How dark the map's edges get, over the rooms: 0 = no vignette, 1 = edges fully black.")]
        [Range(0f, 1f)] public float vignetteStrength = 0.55f;

        [Header("Rooms")]
        public Color room = UiStyle.MapNode;
        [Tooltip("The room she's in.")]
        public Color here = UiStyle.MapHere;
        [Tooltip("One step away: click to add the trip.")]
        public Color reachable = UiStyle.MapReachable;
        [Tooltip("On the map, but no way in yet (a sealed door she can see).")]
        public Color sealedRoom = UiStyle.MapSealed;

        [Header("By heart (under each room's name)")]
        [Tooltip("The soft glow round a room she knows by heart (a sliced sprite). Made by setup Step 101; empty: no glow.")]
        [FormerlySerializedAs("byHeartRing")] public Sprite byHeartGlow;
        [FormerlySerializedAs("byHeartRingColour")] public Color byHeartGlowColour = UiStyle.MapByHeartGlow;
        [Tooltip("How far the glow reaches beyond the room's edge, in pixels. Placeholder rule (plan 032a).")]
        [Min(0f)] public float byHeartGlowMargin = 16f;
        [Tooltip("The line 'Known by heart' under a room's name.")]
        public Color byHeartTag = UiStyle.MapByHeartTag;
        [Tooltip("The line under a room she doesn't know by heart yet ('Worked here in 2 runs...').")]
        public Color roomTag = UiStyle.MapRoomTag;
        [Tooltip("The room speed line on the planning screen.")]
        public Color speedLine = UiStyle.MapSpeedLine;
        [Tooltip("The tag's text size, in pixels at 1920×1080.")]
        [Min(6f)] public float tagFontSize = 15f;
        [Tooltip("The speed line's size, as a share of the tag's text size (0.8 = 80%). Smaller keeps " +
                 "neighbouring rooms' speed lines apart on a crowded map.")]
        [Range(0.5f, 1f)] public float speedLineSize = 0.8f;
        [Tooltip("The tag's width and height, in pixels. Text past the height still shows (it overflows).")]
        public Vector2 tagSize = new Vector2(280f, 36f);
        [Tooltip("Where the tag hangs from: its top centre, measured from the room's bottom centre (y below zero is below the room).")]
        public Vector2 tagOffset = new Vector2(0f, -16f);

        [Header("Ways")]
        public Color way = UiStyle.MapWay;
        public Color oneWay = UiStyle.MapOneWay;
        [Tooltip("A way she has found but that is shut, and is shown anyway (the Dark Corridor before its candles are lit).")]
        public Color shutWay = UiStyle.MapShutWay;

        [Header("The route (the queue's trips)")]
        [Tooltip("The line joining the rooms the queue stops at.")]
        public Color route = UiStyle.MapRoute;
        [Min(1f)] public float routeThickness = 3f;
        [Tooltip("How long (seconds) a newly scheduled trip's line takes to grow in. 0 = at once.")]
        [Min(0f)] public float routeDrawInSeconds = 0.3f;
        [Tooltip("The numbered badge on the corner of each room the route stops at.")]
        public Color badge = UiStyle.MapBadge;
        public Color badgeText = UiStyle.MapBadgeText;

        [Header("Clara's token")]
        public Color token = UiStyle.MapToken;
        [Tooltip("The dot's width and height, in pixels.")]
        [Min(4f)] public float tokenSize = 14f;
        [Tooltip("How quickly the token catches up with where she is on a trip. Higher is snappier; it only smooths the look.")]
        [Min(0.5f)] public float tokenEase = 10f;

        [Header("Floor dots (what lies on a room's floor)")]
        [Tooltip("Wisps, phials: things that give vitality back.")]
        public Color restorativeDot = UiStyle.FloorRestorative;
        [Tooltip("Hanging candles: things that hold off the dark.")]
        public Color lightDot = UiStyle.FloorLight;
        [Tooltip("Flint and steel, and any item not given a kind.")]
        public Color toolDot = UiStyle.FloorTool;
        [Tooltip("Roland's ring, the tome: things that matter to her.")]
        public Color keepsakeDot = UiStyle.FloorKeepsake;
        [Tooltip("Each dot's width and height, in pixels.")]
        [Min(2f)] public float floorDotSize = 7f;
        [Tooltip("The gap between dots, in pixels.")]
        [Min(0f)] public float floorDotSpacing = 3f;
        [Tooltip("Where the row of dots hangs from, measured from the room's bottom-left corner: the first dot's " +
                 "top-left corner sits here (y below zero is below the room).")]
        public Vector2 floorDotOffset = new Vector2(0f, -5f);

        /// <summary>The dot colour for a kind of item on the floor.</summary>
        public Color KindColour(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Restorative: return restorativeDot;
                case ItemKind.Light: return lightDot;
                case ItemKind.Tool: return toolDot;
                case ItemKind.Keepsake: return keepsakeDot;
                default: throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "MapStyle: no dot colour for this kind of item. Add one.");
            }
        }

        [Header("Exploration bars")]
        [Tooltip("While there's plenty left to find...")]
        public Color exploreStart = UiStyle.ExploreStart;
        [Tooltip("...warming to this as the room fills.")]
        public Color exploreFull = UiStyle.ExploreFull;
        public Color exploreTrack = UiStyle.ExploreTrack;

        public Color ExploreColour(float fraction) => Color.Lerp(exploreStart, exploreFull, fraction * fraction);

        private static MapStyle _fallback;

        /// <summary>The built-in look, for a map that hasn't been given a style asset.</summary>
        public static MapStyle Fallback
        {
            get
            {
                if (_fallback == null)
                    _fallback = CreateInstance<MapStyle>();
                return _fallback;
            }
        }
    }
}
