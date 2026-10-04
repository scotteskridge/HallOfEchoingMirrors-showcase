using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One room on the map: its button, colour, name and search bar. MapView adds it to its room
    /// template when the game starts, so every copy carries one and the template prefab needs no
    /// changes. The floor dots under it are drawn by MapFloor, on a layer of their own; its tag lives
    /// on MapView's tag layer, above every room, so a long tag is never hidden under a neighbour.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MapRoom : MonoBehaviour
    {
        private Button _button;
        private Image _image;
        private TMP_Text _label;
        private Image _exploreFill; // made in code, along the bottom edge
        private Image _glow; // made in code: the soft glow of a room known by heart
        private TMP_Text _tag; // made in code under the room: how well she knows it, and its speed

        public Button Button => _button;
        public Image Image => _image;
        public TMP_Text Tag => _tag;
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>
        /// Finds its parts and builds the search bar. Called once per copy, never on the template.
        /// <paramref name="tagLayer"/> holds the tag: it must line up with the room's own layer (MapView.AddLayer).
        /// </summary>
        public void Build(MapStyle style, RectTransform tagLayer)
        {
            if (_button != null)
                throw new System.InvalidOperationException($"{name}: MapRoom.Build called twice.");
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();
            _label = GetComponentInChildren<TMP_Text>();
            _exploreFill = MakeExploreBar(style);
            _glow = MakeGlow(style);
            _tag = MakeTag(style, tagLayer);
            PlaceTag(style);
        }

        // The tag is on another layer, so it is shown, hidden and thrown away along with its room by hand.
        private void OnEnable()
        {
            if (_tag != null)
                _tag.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_tag != null)
                _tag.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_tag != null)
                Destroy(_tag.gameObject);
        }

        /// <summary>Puts the tag under the room's bottom edge. Call after the room is moved (MapView.LayOut).</summary>
        public void PlaceTag(MapStyle style)
        {
            var room = Rect;
            var bottomCentre = new Vector2((0.5f - room.pivot.x) * room.rect.width, -room.pivot.y * room.rect.height);
            ((RectTransform)_tag.transform).anchoredPosition = room.anchoredPosition + bottomCentre + style.tagOffset;
        }

        /// <summary>The lines under the room's name (empty hides them), and whether it glows as known by heart.</summary>
        public void SetTag(string text, Color colour, bool glow, Color glowColour)
        {
            UiText.Set(_tag, text);
            if (_tag.color != colour)
                _tag.color = colour;
            bool showGlow = glow && _glow.sprite != null;
            if (_glow.gameObject.activeSelf != showGlow)
                _glow.gameObject.SetActive(showGlow);
            if (_glow.color != glowColour)
                _glow.color = glowColour;
        }

        /// <summary>A soft glow just outside the room (a sliced sprite, so it fits any room size), behind it. Hidden until a room is known by heart.</summary>
        private Image MakeGlow(MapStyle style)
        {
            var glow = new GameObject("ByHeartGlow", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)glow.transform;
            rect.SetParent(transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-style.byHeartGlowMargin, -style.byHeartGlowMargin);
            rect.offsetMax = new Vector2(style.byHeartGlowMargin, style.byHeartGlowMargin);
            var image = glow.GetComponent<Image>();
            image.sprite = style.byHeartGlow;
            image.type = Image.Type.Sliced; // the soft edge keeps its width whatever the room's size
            image.fillCenter = false; // only the halo: the room itself must stay clear
            image.raycastTarget = false;
            glow.SetActive(false);
            return image;
        }

        /// <summary>
        /// A small text under the room, made from a copy of its name label (same font) so it follows the UI fonts.
        /// It sits on the tag layer with the room's own anchors, so PlaceTag can work from the room's position.
        /// </summary>
        private TMP_Text MakeTag(MapStyle style, RectTransform tagLayer)
        {
            var copy = Instantiate(_label.gameObject, tagLayer);
            copy.name = "Tag " + name;
            foreach (var key in copy.GetComponents<TextKey>())
                Destroy(key);
            var text = copy.GetComponent<TMP_Text>();
            var rect = (RectTransform)copy.transform;
            rect.anchorMin = Rect.anchorMin;
            rect.anchorMax = Rect.anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = style.tagSize;
            rect.localScale = Vector3.one; // placed by PlaceTag, by default below the floor dots' first row
            text.enableAutoSizing = false;
            text.fontSize = style.tagFontSize;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Top;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }

        public void SetColour(Color colour)
        {
            if (_image.color != colour)
                _image.color = colour;
        }

        public void SetLabel(string text)
        {
            if (_label.text != text)
                _label.text = text;
        }

        public void ShowExploreBar(bool show, float fraction, MapStyle style)
        {
            var track = _exploreFill.transform.parent.gameObject;
            if (track.activeSelf != show)
                track.SetActive(show);
            if (!show)
                return;
            ((RectTransform)_exploreFill.transform).anchorMax = new Vector2(fraction, 1f);
            _exploreFill.color = style.ExploreColour(fraction);
        }

        /// <summary>A thin bar along the bottom of the room button. Returns its fill.</summary>
        private Image MakeExploreBar(MapStyle style)
        {
            var track = new GameObject("ExploreBar", typeof(RectTransform), typeof(Image));
            var trackRect = (RectTransform)track.transform;
            trackRect.SetParent(transform, false);
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.offsetMin = new Vector2(6f, 4f);
            trackRect.offsetMax = new Vector2(-6f, 10f); // 6 pixels tall, just inside the bottom edge
            var trackImage = track.GetComponent<Image>();
            trackImage.color = style.exploreTrack;
            trackImage.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(trackRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f); // the width follows the fraction explored
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.GetComponent<Image>();
            fillImage.raycastTarget = false;
            return fillImage;
        }
    }
}
