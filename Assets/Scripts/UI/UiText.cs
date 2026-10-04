using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Small helpers every screen uses: setting text and showing things only when they change, and
    /// how numbers and times look. Change a format here and it changes everywhere.
    /// </summary>
    public static class UiText
    {
        // ---------- Setting what's on screen ----------

        /// <summary>Sets a label's text, only if it's different (setting it makes TextMeshPro rebuild).</summary>
        public static void Set(TMP_Text label, string text)
        {
            if (label != null && label.text != text)
                label.text = text;
        }

        /// <summary>Shows or hides something, only if that changes.</summary>
        public static void SetActive(Component part, bool show)
        {
            if (part != null && part.gameObject.activeSelf != show)
                part.gameObject.SetActive(show);
        }

        /// <summary>Sets the words on a button (its first text).</summary>
        public static void SetCaption(Button button, string text)
        {
            if (button != null)
                Set(button.GetComponentInChildren<TMP_Text>(), text);
        }

        /// <summary>A list's "nothing here yet" label: shown with its line from the text file while the list is empty.</summary>
        public static void ShowEmpty(TMP_Text label, bool empty, string key)
        {
            if (label == null)
                return;
            SetActive(label, empty);
            if (empty)
                Set(label, GameText.Get(key));
        }

        /// <summary>Items joined into one line with a list line from the text file ("common.list": "A, B, C").</summary>
        public static string List(IEnumerable<string> items, string key = "common.list")
        {
            string list = null;
            foreach (var item in items)
                list = list == null ? item : GameText.Get(key, ("list", list), ("item", item));
            return list ?? "";
        }

        // ---------- How numbers look ----------

        /// <summary>An everyday amount: one decimal place when there is one, e.g. "12" or "9.5".</summary>
        public static string Number(float value) => value.ToString("0.#");

        /// <summary>A whole number, e.g. "73" (vitality on the bar).</summary>
        public static string Whole(float value) => value.ToString("0");

        /// <summary>A rate or multiplier, to two places, e.g. "0.62" a second or "×1.16".</summary>
        public static string Rate(float value) => value.ToString("0.00");

        /// <summary>A countdown, always to one place so it doesn't jump between "12" and "11.9".</summary>
        public static string Countdown(float seconds) => seconds.ToString("0.0");

        /// <summary>A setting's number, as short as it can be, e.g. "0.97" or "5".</summary>
        public static string Setting(float value) => value.ToString("0.##");

        /// <summary>A fraction (0 to 1) as a whole percentage, e.g. 0.4 → 40.</summary>
        public static int Percent(float fraction) => Mathf.RoundToInt(fraction * 100f);

        /// <summary>
        /// Whole seconds between two times as the clock shows them (each rounded down first), so a
        /// difference always agrees with the two times printed beside it.
        /// </summary>
        public static int ClockDifference(float now, float before) => Mathf.FloorToInt(now) - Mathf.FloorToInt(before);

        /// <summary>A <see cref="ClockDifference"/> as text: "+0:50", "-0:50" or "same".</summary>
        public static string ClockChange(int difference) =>
            difference == 0 ? GameText.Get("common.same") : (difference > 0 ? "+" : "-") + Clock(Mathf.Abs(difference));

        /// <summary>E.g. 151.3 seconds → "2:31".</summary>
        public static string Clock(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            return $"{whole / 60}:{whole % 60:00}";
        }
    }
}
