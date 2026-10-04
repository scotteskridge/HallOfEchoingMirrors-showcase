using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Menu: Hall of Echoing Mirrors → Text → Scene Labels. Lists every label in the open scene
    /// and moves the ticked ones' words into the game text file (a "## scene" section), adding a
    /// Text Key to each so it reads from there. Labels that code writes, and those inside
    /// templates the game copies, are shown but left alone.
    /// </summary>
    public class SceneLabelsWindow : EditorWindow
    {
        const string TextPath = "Assets/Text/game_text.txt";
        const string SceneSection = "## scene";

        private enum Use { Movable, SetByCode, InTemplate, FromFile }

        private class Label
        {
            public TMP_Text Text;
            public Use Use;
            public bool Ticked;
            public string Key;
        }

        private readonly List<Label> _labels = new List<Label>();
        // The list is rebuilt whenever the window gains focus, so the user's unticks live here
        // rather than on the Label, or clicking away and back would tick everything again.
        private readonly HashSet<TMP_Text> _unticked = new HashSet<TMP_Text>();
        private Vector2 _scroll;

        [MenuItem("Hall of Echoing Mirrors/Text/Scene Labels", false, 41)]
        public static void Open() => GetWindow<SceneLabelsWindow>("Scene Labels");

        private void OnEnable() => Find();
        private void OnFocus() => Find();

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Ticked labels have their words copied into " + TextPath + " (section \"scene\") and get a Text Key, " +
                "so from then on they're edited in the file. \"Set by code\" labels are written by the game's scripts " +
                "(their words are already in the file); \"In a template\" ones are copied by the game. If a ticked " +
                "label turns out to be changed by code too, the code wins: untick it or remove its Text Key.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    Find();
                GUI.enabled = _labels.Exists(l => l.Ticked);
                if (GUILayout.Button($"Move {_labels.FindAll(l => l.Ticked).Count} ticked into the text file"))
                    MoveTicked();
                GUI.enabled = true;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var label in _labels)
            {
                if (label.Text == null)
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = label.Use == Use.Movable;
                    bool ticked = EditorGUILayout.Toggle(label.Ticked, GUILayout.Width(18));
                    if (ticked != label.Ticked)
                    {
                        label.Ticked = ticked;
                        if (ticked)
                            _unticked.Remove(label.Text);
                        else
                            _unticked.Add(label.Text);
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button(PathOf(label.Text.transform), EditorStyles.linkLabel, GUILayout.Width(280)))
                    {
                        Selection.activeObject = label.Text.gameObject;
                        EditorGUIUtility.PingObject(label.Text.gameObject);
                    }
                    EditorGUILayout.LabelField(Preview(label.Text.text), GUILayout.Width(260));
                    EditorGUILayout.LabelField(label.Use switch
                    {
                        Use.SetByCode => "set by code",
                        Use.InTemplate => "in a template",
                        Use.FromFile => label.Key,
                        _ => label.Key,
                    }, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // ---------- Finding the labels ----------

        private void Find()
        {
            _labels.Clear();
            _unticked.RemoveWhere(text => text == null); // deleted from the scene
            var setByCode = new HashSet<TMP_Text>();
            var templates = new HashSet<Transform>();
            FindWhatCodeUses(setByCode, templates);

            var usedKeys = SceneKeysInFile(); // never reuse a key the file already has
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((text.hideFlags & HideFlags.DontSave) != 0 || EditorUtility.IsPersistent(text) || string.IsNullOrWhiteSpace(text.text))
                    continue; // preview objects, prefab assets, empty labels

                var label = new Label { Text = text };
                var textKey = text.GetComponent<TextKey>();
                if (textKey != null)
                {
                    label.Use = Use.FromFile;
                    label.Key = textKey.Key;
                }
                else if (setByCode.Contains(text))
                    label.Use = Use.SetByCode;
                else if (IsInside(text.transform, templates))
                    label.Use = Use.InTemplate;
                else
                {
                    label.Use = Use.Movable;
                    label.Ticked = !_unticked.Contains(text);
                    label.Key = KeyFor(text.transform, usedKeys);
                }
                _labels.Add(label);
            }
            _labels.Sort((a, b) => string.CompareOrdinal(PathOf(a.Text.transform), PathOf(b.Text.transform)));
            Repaint();
        }

        /// <summary>
        /// Labels any script points at are written by code. Anything a script calls a "template"
        /// is copied at runtime, so everything inside it is too.
        /// </summary>
        private static void FindWhatCodeUses(HashSet<TMP_Text> setByCode, HashSet<Transform> templates)
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour is TMP_Text || behaviour is TextKey)
                    continue;
                var property = new SerializedObject(behaviour).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                        continue;
                    var value = property.objectReferenceValue;
                    if (value is TMP_Text text)
                        setByCode.Add(text);
                    if (property.name.ToLowerInvariant().Contains("template"))
                    {
                        if (value is Component component)
                            templates.Add(component.transform);
                        else if (value is GameObject go)
                            templates.Add(go.transform);
                    }
                }
            }
        }

        private static bool IsInside(Transform child, HashSet<Transform> parents)
        {
            for (var t = child; t != null; t = t.parent)
                if (parents.Contains(t))
                    return true;
            return false;
        }

        // ---------- Moving them ----------

        private void MoveTicked()
        {
            var ticked = _labels.FindAll(l => l.Ticked && l.Use == Use.Movable && l.Text != null);
            if (ticked.Count == 0)
                return;

            // Into the file first, so nothing is left pointing at a line that isn't there.
            string file = File.Exists(TextPath) ? File.ReadAllText(TextPath) : "";
            var lines = new StringBuilder();
            // New lines go at the end, so the file's last section must be "scene" (start one if not).
            if (LastSection(file) != "scene")
            {
                bool first = !file.Contains("\n" + SceneSection) && !file.StartsWith(SceneSection);
                lines.Append("\n\n").Append(SceneSection).Append('\n');
                if (first)
                    lines.Append("# Labels typed into the scene (buttons, headings, tabs). Keys made by the Scene Labels tool.\n");
            }
            foreach (var label in ticked)
                lines.Append($"{label.Key.Substring("scene.".Length)}: {OneLine(label.Text.text)}\n");
            // End the file's last line before adding (a new section brings its own blank line).
            string added = lines.ToString();
            File.WriteAllText(TextPath, file.TrimEnd('\r', '\n') + (added.StartsWith("\n") ? "" : "\n") + added);
            AssetDatabase.ImportAsset(TextPath);

            Undo.SetCurrentGroupName("Move labels into the text file");
            foreach (var label in ticked)
                EditorUiFactory.SetTextKey(label.Text, label.Key);
            EditorSceneManager.MarkSceneDirty(ticked[0].Text.gameObject.scene);

            Debug.Log($"Scene Labels: moved {ticked.Count} labels into {TextPath} (section \"scene\"). " +
                      "Press Ctrl+S to save the scene.");
            Find();
        }

        /// <summary>"scene.run_screen_plan_next_button": the page and the object, tidied, and unique.</summary>
        private static string KeyFor(Transform label, HashSet<string> used)
        {
            // A button's caption is usually a child called "Text (TMP)": name it after the button.
            var owner = label.name.StartsWith("Text") && label.parent != null ? label.parent : label;
            var page = owner;
            while (page.parent != null && page.parent.GetComponent<Canvas>() == null)
                page = page.parent;

            string key = "scene." + Slug(page == owner ? owner.name : page.name + "_" + owner.name);
            string unique = key;
            for (int n = 2; used.Contains(unique); n++)
                unique = $"{key}_{n}";
            used.Add(unique);
            return unique;
        }

        /// <summary>The name of the last "## section" in the file, or "" if there's none.</summary>
        private static string LastSection(string file)
        {
            string last = "";
            foreach (var raw in file.Replace("\r\n", "\n").Split('\n'))
                if (raw.Trim().StartsWith("##"))
                    last = raw.Trim().Substring(2).Trim();
            return last;
        }

        /// <summary>Every "scene.…" key the file already holds.</summary>
        private static HashSet<string> SceneKeysInFile()
        {
            var keys = new HashSet<string>();
            if (!File.Exists(TextPath))
                return keys;
            string section = "";
            foreach (var raw in File.ReadAllLines(TextPath))
            {
                string line = raw.Trim();
                if (line.StartsWith("##"))
                    section = line.Substring(2).Trim();
                else if (section == "scene" && !line.StartsWith("#") && line.IndexOf(':') > 0)
                    keys.Add("scene." + line.Substring(0, line.IndexOf(':')).Trim());
            }
            return keys;
        }

        private static string Slug(string name) =>
            Regex.Replace(Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_"), "^_|_$", "");

        private static string OneLine(string text) => text.Replace("\r", "").Replace("\n", "\\n").Trim();

        private static string Preview(string text)
        {
            string one = text.Replace("\n", " ");
            return one.Length > 40 ? one.Substring(0, 40) + "…" : one;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null && p.GetComponent<Canvas>() == null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
