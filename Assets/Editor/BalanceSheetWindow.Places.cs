using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    // The Balance Sheet's places (a pure move out of BalanceSheetWindow.cs).
    public partial class BalanceSheetWindow
    {
        // ---------- Ways ----------

        /// <summary>Every way between rooms, with what its door needs (e.g. 25 candles lit) editable here.</summary>
        private void DrawWays()
        {
            if (!Section(ref _showWays, "Ways (doors between rooms; add or remove needs with Select)") || _content == null)
                return;

            Header(("From", 170), ("To", 170), ("Both ways", 65), ("Starts open", 75), ("Found at %", 75),
                ("Needs Perception", 110), ("Shown shut", 75), ("Shut message key", 200), ("Needs (item: amount)", 320), ("", 50));
            foreach (var node in _content.nodes)
            {
                if (node == null)
                    continue;
                var so = new SerializedObject(node);
                so.Update();
                for (int i = 0; i < node.ways.Count; i++)
                {
                    var way = node.ways[i];
                    if (way?.to == null)
                        continue;
                    string path = $"ways.Array.data[{i}]";
                    EditorGUILayout.BeginHorizontal();
                    Label(node.DisplayName, 170);
                    Label(way.to.DisplayName, 170);
                    Field(so, $"{path}.bothWays", 65);
                    Field(so, $"{path}.startsOpen", 75);
                    Field(so, $"{path}.foundAtExplored", 75);
                    Field(so, $"{path}.needsPerception", 110);
                    Field(so, $"{path}.showWhileShut", 75);
                    Field(so, $"{path}.shutMessageKey", 200);
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(320));
                    if (way.needs.Count == 0)
                        Label("-", 320);
                    for (int n = 0; n < way.needs.Count; n++)
                    {
                        Label(way.needs[n].resource != null ? way.needs[n].resource.DisplayName + ":" : "(none):", 110);
                        Field(so, $"{path}.needs.Array.data[{n}].amount", 45);
                    }
                    EditorGUILayout.EndHorizontal();
                    SelectButton(node);
                    EditorGUILayout.EndHorizontal();
                }
                so.ApplyModifiedProperties();
            }
        }

        // ---------- Found by searching ----------

        /// <summary>Every action a room's search finds, with its point and Perception need editable.</summary>
        private void DrawFinds()
        {
            if (!Section(ref _showFinds, "Found by searching (the bar is kept between runs; add or remove finds with Select)") || _content == null)
                return;

            Header(("Room", 170), ("Action", 220), ("Found at %", 90), ("Needs Perception", 110), ("After switch", 170), ("", 50));
            foreach (var node in _content.nodes)
            {
                if (node == null)
                    continue;
                var so = new SerializedObject(node);
                so.Update();
                for (int i = 0; i < node.foundBySearching.Count; i++)
                {
                    var find = node.foundBySearching[i];
                    if (find?.task == null)
                        continue;
                    EditorGUILayout.BeginHorizontal();
                    Label(node.DisplayName, 170);
                    Label(find.task.displayName, 220);
                    Field(so, $"foundBySearching.Array.data[{i}].atSearched", 90);
                    Field(so, $"foundBySearching.Array.data[{i}].needsPerception", 110);
                    Field(so, $"foundBySearching.Array.data[{i}].afterSwitch", 170);
                    SelectButton(node);
                    EditorGUILayout.EndHorizontal();
                }
                so.ApplyModifiedProperties();
            }
        }

        // ---------- Places ----------

        private void DrawPlaces()
        {
            if (!Section(ref _showPlaces, "Places (nodes)") || _content == null)
                return;

            if (_settings != null)
                EditorGUILayout.LabelField(RoomSpeedCurve(_settings), EditorStyles.miniLabel);
            Header(("Name", 170), ("Planned", 55), ("Note (planned rooms)", 200), ("Kind", 110), ("Depth", 50), ("Hue", 90), ("Always on map", 95), ("Map position", 130),
                ("Leaving cost/time", 120), ("Entering cost/time", 120), ("Searching cost/time", 120),
                ("Steps to fill", 100), ("Full search (s)", 95), ("Each step gives", 150), ("Found by searching", 260), ("Ways", 300), ("", 50));
            foreach (var node in _content.nodes)
            {
                if (node == null)
                    continue;

                var so = new SerializedObject(node);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                Field(so, "displayName", 170);
                Field(so, "planned", 55);
                Field(so, "planningNote", 200);
                Field(so, "kind", 110);
                Field(so, "depth", 50);
                Field(so, "hue", 90);
                Field(so, "alwaysOnMap", 95);
                Field(so, "mapPosition", 130);
                Field(so, "leaving.cost", 58); Field(so, "leaving.time", 58);
                Field(so, "entering.cost", 58); Field(so, "entering.time", 58);
                Field(so, "exploring.cost", 58); Field(so, "exploring.time", 58);
                Field(so, "exploresToFill", 100);
                Label(FullSearchSeconds(node), 95, "How long searching the whole room takes at normal speed and Perception 0: " +
                    "the Explore verb's time in this room (the cost curve at its depth) × Searching time × Steps to fill. Read-only; the bar is kept between runs, so she does it once.");
                AmountFields(so, "eachExploreGives", node.eachExploreGives, 150);
                Label(Finds(node), 260, "Actions found by searching, at the % the room's kept search must reach. " +
                    "Edit the numbers in the Found by searching section below.");
                Label(WaysOut(node), 300,
                    "↔ both ways, → one way, (shut) opens by switch, (found at N%) hidden until searched that far once, [needs …] a door's condition. Edit ways with Select.");
                SelectButton(node);
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>The clock's speed in a room known by heart, by runs worked there (Runs By Heart to Full Speed Runs in Loop Settings). Read-only.</summary>
        private static string RoomSpeedCurve(LoopSettings settings)
        {
            var text = new System.Text.StringBuilder("Room speed by runs worked in a room: ×1 below ").Append(settings.byHeartRuns);
            for (int runs = settings.byHeartRuns; runs <= Mathf.Max(settings.byHeartRuns, settings.fullSpeedRuns); runs++)
                text.Append(", ").Append(runs).Append(" = ×").Append(settings.RoomSpeedAfter(runs).ToString("0.#"));
            return text.ToString();
        }

        /// <summary>How long a whole search of the room takes at normal speed, e.g. "30", or "-" for a room with nothing to search.</summary>
        private string FullSearchSeconds(NodeDefinition node)
        {
            if (node.exploresToFill <= 0 || _content.exploreVerb == null)
                return "-";
            return (SecondsAt(_content.exploreVerb, node.depth) * node.exploring.time * node.exploresToFill).ToString("0.#");
        }

        /// <summary>E.g. "Gather a wisp 34%, Instantiate a candle 67%".</summary>
        private string Finds(NodeDefinition node)
        {
            _text.Clear();
            foreach (var find in node.foundBySearching)
                if (find?.task != null)
                    _text.Append(_text.Length > 0 ? ", " : "").Append(find.task.displayName).Append(' ').Append(find.atSearched).Append('%');
            return _text.Length > 0 ? _text.ToString() : "-";
        }

        /// <summary>E.g. "↔ The junction, → The mirror lab (shut) [needs 1 Roland's ring]".</summary>
        private string WaysOut(NodeDefinition node)
        {
            _text.Clear();
            foreach (var way in node.ways)
            {
                if (way == null || way.to == null)
                    continue;
                _text.Append(_text.Length > 0 ? ", " : "")
                    .Append(way.bothWays ? "↔ " : "→ ").Append(way.to.DisplayName);
                if (!way.startsOpen)
                    _text.Append(" (shut)");
                if (way.foundAtExplored > 0)
                    _text.Append(node.exploresToFill > 0 ? $" (found at {way.foundAtExplored}%)" : " (found at a % of a room with nothing to search: never!)");
                if (way.needs.Count > 0)
                    _text.Append(" [needs ").Append(Amounts(way.needs)).Append(']');
            }
            if (node.ways.Count > 4)
                _text.Append("  (more than 4 ways: hard to read on the map)");
            return _text.Length > 0 ? _text.ToString() : "-";
        }
    }
}
