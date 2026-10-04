using System.Text.RegularExpressions;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Every colour the greybox UI uses, in one place. Placeholder values: at the art stage these
    /// move into a theme asset (with hue icons and descriptions), and only this file changes.
    /// </summary>
    public static class UiStyle
    {
        // The palette (2026-09-29, from the user's mock-ups, kept outside the project): warm near-black panels,
        // cream text, and one amber accent for what matters most (Play, the speed in use, mastery).

        // Colours used inside text (TextMeshPro rich text tags).
        public const string Warning = "#D9894A";   // skipped tasks, slower than last run
        public const string Milestone = "#E0A650"; // switches, mastery, kept resources
        public const string Levelling = "#8FB8C8"; // attribute level-ups, speed
        public const string Muted = "#8C8070";     // side notes, at-the-maximum

        /// <summary>Text: the main cream, and the quieter second colour (headings of sections, details).</summary>
        public static readonly Color TextMain = new Color32(0xED, 0xE3, 0xD1, 0xFF);
        public static readonly Color TextSecondary = new Color32(0xA8, 0x9A, 0x86, 0xFF);
        /// <summary>The one accent (the Play button), and text on it.</summary>
        public static readonly Color Accent = new Color32(0xE0, 0xA6, 0x50, 0xFF);
        public static readonly Color AccentText = new Color32(0x1E, 0x14, 0x08, 0xFF);
        /// <summary>An ordinary button's face (a little brighter than it shows: the button dims it until hovered).</summary>
        public static readonly Color ButtonFace = new Color32(0x4E, 0x42, 0x36, 0xFF);
        /// <summary>The empty part of a bar (levels, mastery, searching) and of a scrollbar.</summary>
        public static readonly Color Track = new Color32(0x3A, 0x31, 0x29, 0xFF);
        /// <summary>A room's searched bar in its popover.</summary>
        public static readonly Color SearchedBar = new Color32(0x6E, 0x8F, 0x62, 0xFF);
        /// <summary>Tints the painted page behind everything, darker and warmer.</summary>
        public static readonly Color PageTint = new Color32(0x5A, 0x50, 0x44, 0xFF);
        /// <summary>Behind the map.</summary>
        public static readonly Color MapBackground = new Color32(0x12, 0x0F, 0x0C, 0xD2);

        // The "can't do that" notice in the bottom right.
        public static readonly Color NoticeBackground = new Color32(0x16, 0x12, 0x0E, 0xEB);

        public static readonly Color MapNode = new Color(0.24f, 0.19f, 0.15f);
        public static readonly Color MapHere = new Color(0.72f, 0.60f, 0.25f);      // where she is
        public static readonly Color MapReachable = new Color(0.42f, 0.31f, 0.17f); // one step away: click to travel
        public static readonly Color MapSealed = new Color(0.13f, 0.11f, 0.09f);    // on the map, but no way there yet
        public static readonly Color MapWay = new Color(0.55f, 0.47f, 0.36f);
        public static readonly Color MapOneWay = new Color(0.50f, 0.36f, 0.30f);
        // The queued route: its gold line, the stop-number badges, and Clara's token.
        public static readonly Color MapRoute = new Color(0.90f, 0.66f, 0.26f);
        public static readonly Color MapBadge = new Color(0.90f, 0.66f, 0.26f);
        public static readonly Color MapBadgeText = new Color(0.10f, 0.08f, 0.05f);
        // Placeholder rule (plan 032a): a soft gold glow for rooms known by heart, and a paler gold for their tag. The
        // glow is meant to read apart from the route line and badges (MapRoute, MapBadge): tune there if they blur.
        public static readonly Color MapByHeartGlow = new Color(1.00f, 0.90f, 0.62f, 16f / 255f);
        public static readonly Color MapByHeartTag = new Color(0.95f, 0.85f, 0.55f); // the same gold as ByHeart (#F2D98C): keep in step
        public static readonly Color MapShutWay = new Color(0.28f, 0.23f, 0.19f); // placeholder: dimmer than MapWay
        public static readonly Color MapRoomTag = new Color(0.62f, 0.60f, 0.56f);
        public static readonly Color MapSpeedLine = new Color(0.56f, 0.72f, 0.78f);
        public static readonly Color MapToken = new Color(0.95f, 0.93f, 0.88f);
        // Floor dots on the map, one colour per kind of item lying there (MapStyle).
        public static readonly Color FloorRestorative = new Color(0.72f, 0.64f, 0.88f); // lavender
        public static readonly Color FloorLight = new Color(0.90f, 0.66f, 0.26f);       // gold
        public static readonly Color FloorTool = new Color(0.60f, 0.60f, 0.62f);        // grey
        public static readonly Color FloorKeepsake = new Color(0.90f, 0.66f, 0.70f);    // pale rose

        // Exploration bars: teal while there's more to find, warming to gold as the room fills.
        public static readonly Color ExploreStart = new Color(0.25f, 0.62f, 0.66f);
        public static readonly Color ExploreFull = new Color(0.95f, 0.78f, 0.30f);
        public static readonly Color ExploreTrack = new Color32(0x2A, 0x23, 0x1D, 0xFF);

        /// <summary>What something briefly warms to when it has just gone up (see Glow).</summary>
        public static readonly Color GlowColour = new Color(1.00f, 0.85f, 0.45f);

        /// <summary>The main page's panels (behind each section) and its top bar.</summary>
        public static readonly Color PanelBackground = new Color32(0x1E, 0x19, 0x14, 0xF0);
        public static readonly Color VitalityFill = new Color32(0xB8, 0x47, 0x4A, 0xFF);
        /// <summary>Behind each row of a list (actions, queue, inventory).</summary>
        public static readonly Color RowBackground = new Color32(0x2B, 0x24, 0x1E, 0xF0);
        /// <summary>A stat or skill's two thin bars: this run's level, and mastery (kept).</summary>
        public static readonly Color LevelBar = new Color32(0x8F, 0xB8, 0xC8, 0xFF);
        public static readonly Color MasteryBar = Accent;
        /// <summary>A stat's icon on an action row: a dim warm grey, dimmer than the skill's, so the skill (which sets the speed) reads first.</summary>
        public static readonly Color StatIconOnRow = new Color(0.62f, 0.58f, 0.52f);

        /// <summary>A queue entry's progress bar.</summary>
        public static readonly Color QueueProgress = Accent;

        /// <summary>A stop card in the queue drawer, the one she's at now, and the *return* marker's text colour.</summary>
        public static readonly Color StopCard = new Color32(0x2B, 0x24, 0x1E, 0xF2);
        public static readonly Color StopCardCurrent = new Color32(0x3A, 0x2E, 0x1E, 0xF2);
        public const string ReturnMarker = "#E0A650";
        /// <summary>The "by heart" badge and row mark: a soft gold (placeholder, plan 032a), paler than the route's gilt.</summary>
        public const string ByHeart = "#F2D98C";

        /// <summary>Dragging in the queue drawer: the line where a drop would land, and the box following the pointer.</summary>
        public static readonly Color DropLine = Accent;
        public static readonly Color DragGhost = new Color32(0x3A, 0x2E, 0x1E, 0xE6);
        /// <summary>How visible the row or stop being dragged stays in its old place.</summary>
        public const float DraggedAlpha = 0.4f;

        /// <summary>The main page's empty bands (the ribbon and the bottom strip) until they're filled.</summary>
        public static readonly Color BandBackground = new Color32(0x17, 0x13, 0x0F, 0xF0);

        /// <summary>The slide-up drawer's bookmark tabs (brighter while their page is open).</summary>
        public static readonly Color BookmarkTab = new Color32(0x4A, 0x2A, 0x24, 0xFF);
        public static readonly Color BookmarkTabOpen = new Color32(0x8A, 0x46, 0x36, 0xFF);

        /// <summary>The speed tier being used, and the next one still to earn.</summary>
        public static readonly Color SpeedTierCurrent = Accent;
        public static readonly Color SpeedTierLocked = new Color32(0x4A, 0x40, 0x38, 0xFF);

        // A room's popover over the map, and the small chips inside it (floor items, ways on).
        public static readonly Color PopoverBackground = new Color32(0x21, 0x1B, 0x16, 0xFA);
        public static readonly Color ChipBackground = new Color32(0x2F, 0x27, 0x21, 0xFF);
        /// <summary>How visible an action is that can't be done (greyed in a room's popover).</summary>
        public const float UnavailableAlpha = 0.45f;

        /// <summary>The hover pop-up's box.</summary>
        public static readonly Color TipBackground = new Color32(0x16, 0x12, 0x0E, 0xF5);

        public static string Colour(string text, string hex) => $"<color={hex}>{text}</color>";
        public static string Small(string text) => Sized(text, 0.8f);
        /// <summary>The text at a share of the surrounding size (0.8 = 80%).</summary>
        public static string Sized(string text, float share) => $"<size={Mathf.RoundToInt(share * 100f)}%>{text}</size>";

        /// <summary>A quiet side note: small and grey (e.g. how long an action repeats).</summary>
        public static string Aside(string text) => Small(Colour(text, Muted));

        /// <summary>
        /// The font a pop-up's heading switches to: the UiFonts asset's Heading font, by its asset name
        /// (TextMeshPro's &lt;font&gt; tag only takes a name; UiFontsTests checks the two match).
        /// </summary>
        public const string HeadingFont = "EBGaramond-SemiBold SDF";

        /// <summary>
        /// How much larger a pop-up's heading is than the pop-up's own text. A share, not the Heading
        /// role's size: rich text can't read UiFonts, so this stays in step with the Body size instead.
        /// </summary>
        public const string HeadingSize = "130%";

        /// <summary>A pop-up's heading: the name in the serif, a little larger than the text under it.</summary>
        public static string Heading(string text) => $"<font=\"{HeadingFont}\"><size={HeadingSize}>{text}</size></font>";

        /// <summary>Words in bold, in whatever font the text uses (e.g. the room she's in, on the map).</summary>
        public static string Bold(string text) => $"<b>{text}</b>";

        /// <summary>
        /// Turns the story files' *asterisks* into italics, as they'd read in a manuscript
        /// (Clara's inner voice). Everything else is left as written.
        /// </summary>
        public static string Markup(string text) =>
            string.IsNullOrEmpty(text) ? text : Regex.Replace(text, @"\*([^*\n]+)\*", "<i>$1</i>");

        /// <summary>How a cost source reads on screen, e.g. "all pools" rather than "AllPools".</summary>
        public static string NameOf(CostSource source) =>
            source == CostSource.AllPools ? GameText.Get("costs.all_pools") :
            source == CostSource.Vitality ? GameText.Get("names.vitality") :
            GameText.HueName(source);

        /// <summary>Bar colour for each hue.</summary>
        public static bool TryGetHueColour(Hue hue, out Color colour)
        {
            switch (hue)
            {
                case Hue.Amber:      colour = new Color(1.00f, 0.60f, 0.10f); return true;
                case Hue.Citrine:    colour = new Color(0.95f, 0.85f, 0.20f); return true;
                case Hue.Emerald:    colour = new Color(0.10f, 0.70f, 0.35f); return true;
                case Hue.Sapphire:   colour = new Color(0.15f, 0.30f, 0.80f); return true;
                case Hue.Iolite:     colour = new Color(0.45f, 0.45f, 0.90f); return true;
                case Hue.Amethyst:   colour = new Color(0.60f, 0.30f, 0.80f); return true;
                case Hue.Ruby:       colour = new Color(0.80f, 0.10f, 0.20f); return true;
                default:             colour = default; return false;
            }
        }
    }
}
