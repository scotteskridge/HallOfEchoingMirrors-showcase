using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// Every line of on-screen text, looked up by key from the game text file
    /// (Assets/Text/game_text.txt), so wording is edited in one place, never in code.
    /// Lines are "key: text" under "## section" headings; the full key is "section.key".
    /// {name} marks where a value goes, and \n starts a new line. A missing key shows as
    /// "[key?]" (and warns once), so a typo is visible rather than breaking anything.
    /// </summary>
    public static class GameText
    {
        private static readonly Dictionary<string, string> _lines = new Dictionary<string, string>();
        private static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>The text was (re)loaded, e.g. the file was saved while playing: redraw anything cached.</summary>
        public static event Action Changed;

        public static bool IsLoaded => _lines.Count > 0;

        public static IEnumerable<string> Keys => _lines.Keys;

        /// <summary>Reads the file's contents. Problems (a line with no key) are returned, not thrown.</summary>
        public static List<string> Load(string contents)
        {
            var problems = new List<string>();
            _lines.Clear();
            _warned.Clear();

            string section = "";
            int number = 0;
            foreach (var raw in (contents ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                number++;
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("##"))
                {
                    section = line.Substring(2).Trim();
                    continue;
                }
                if (line.StartsWith("#"))
                    continue;

                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    problems.Add($"line {number}: no \"key:\" at the start");
                    continue;
                }
                string key = line.Substring(0, colon).Trim();
                string text = line.Substring(colon + 1).Trim().Replace("\\n", "\n");
                string fullKey = section.Length > 0 ? $"{section}.{key}" : key;
                if (_lines.ContainsKey(fullKey))
                    problems.Add($"line {number}: \"{fullKey}\" appears twice; the later one is used");
                _lines[fullKey] = text;
            }

            Changed?.Invoke();
            return problems;
        }

        public static bool Has(string key) => _lines.ContainsKey(key);

        /// <summary>
        /// A room's title for use mid-sentence: only a leading "The", "A" or "An" is lowercased, so
        /// "The Dark Corridor" reads "the Dark Corridor" and "Dark Hall" stays "Dark Hall".
        /// </summary>
        public static string TitleInSentence(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;
            foreach (string article in new[] { "The ", "A ", "An " })
                if (title.StartsWith(article, System.StringComparison.Ordinal))
                    return char.ToLowerInvariant(title[0]) + title.Substring(1);
            return title;
        }

        /// <summary>"Light a candle" becomes "light a candle", for use mid-sentence (action names, not room titles).</summary>
        public static string LowerFirst(string name) =>
            string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

        /// <summary>The line for this key, with each {name} replaced by its value.</summary>
        public static string Get(string key, params (string name, object value)[] values)
        {
            if (!_lines.TryGetValue(key, out string text))
            {
                if (IsLoaded && _warned.Add(key))
                    Debug.LogWarning($"Game text: no line called \"{key}\" in the text file.");
                return $"[{key}?]";
            }
            if (values == null || values.Length == 0)
                return text;

            var result = new StringBuilder(text);
            foreach (var (name, value) in values)
            {
                // {loop:ordinal} writes a number as 1st, 2nd, 3rd, 4th...
                if (value is int || value is long)
                    result.Replace("{" + name + ":ordinal}", Ordinal(Convert.ToInt64(value)));
                result.Replace("{" + name + "}", value?.ToString() ?? "");
            }
            return result.ToString();
        }

        /// <summary>
        /// 1st, 2nd, 3rd, 4th... 11th, 12th, 13th, 21st: the English rule, with the endings from
        /// the file's "ordinals" section (so another language can change them).
        /// </summary>
        public static string Ordinal(long number)
        {
            long lastTwo = Math.Abs(number) % 100;
            long last = Math.Abs(number) % 10;
            string form =
                lastTwo >= 11 && lastTwo <= 13 ? "other" :
                last == 1 ? "one" :
                last == 2 ? "two" :
                last == 3 ? "three" :
                "other";
            return Has("ordinals." + form)
                ? Get("ordinals." + form).Replace("{n}", number.ToString())
                : number.ToString();
        }

        /// <summary>Clara's attribute by name, e.g. "Perception".</summary>
        public static string Attribute(ClaraAttribute attribute) =>
            Get("attributes." + attribute.ToString().ToLowerInvariant());

        /// <summary>A pathos hue's name, e.g. "Ruby": the one place pool names live. Throws for Hue.None.</summary>
        public static string HueName(Hue hue) =>
            hue == Hue.None
                ? throw new ArgumentOutOfRangeException(nameof(hue), "Hue.None isn't a hue, so it has no name.")
                : Get("hues." + hue.ToString().ToLowerInvariant());

        /// <summary>
        /// The name of a cost source that is a hue. Mapped by name, not by casting, so the two enums
        /// can drift apart. Throws for Vitality and AllPools, which aren't hues.
        /// </summary>
        public static string HueName(CostSource source)
        {
            if (source == CostSource.Vitality || source == CostSource.AllPools)
                throw new ArgumentOutOfRangeException(nameof(source), source, "Only a hue has a hue name.");
            return Get("hues." + source.ToString().ToLowerInvariant());
        }
    }
}
