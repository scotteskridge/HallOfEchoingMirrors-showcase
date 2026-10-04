using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// A named set of balance changes to try in memory, read from docs/balance/scenarios.txt:
    /// <c>name | asset path | field path | value</c>, one change per line; lines with the same name form
    /// one scenario, and a line with only a name is a scenario with no changes (the shipped numbers).
    /// Field paths are Unity's serialized paths, e.g. <c>xpPerSecondOfTask</c>, <c>exploring.time</c>,
    /// <c>easierWith.Array.data[0].timeEach</c>. Applied without Undo and never saved; <see cref="Undo"/>
    /// puts every value back.
    /// </summary>
    public class WhatIf
    {
        /// <summary>One change: which asset, which field, what value.</summary>
        public struct Change
        {
            public string AssetPath, FieldPath, Value;
            public override string ToString() => $"{System.IO.Path.GetFileNameWithoutExtension(AssetPath)}.{FieldPath} = {Value}";
        }

        public string Name { get; }
        public List<Change> Changes { get; } = new List<Change>();
        private readonly List<Action> _undo = new List<Action>();

        public WhatIf(string name) => Name = name;

        /// <summary>Reads every scenario in the file, in the order they first appear.</summary>
        public static List<WhatIf> ReadFile(string path)
        {
            var scenarios = new List<WhatIf>();
            int lineNumber = 0;
            foreach (var raw in System.IO.File.ReadAllLines(path))
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                var parts = line.Split('|');
                string name = parts[0].Trim();
                var scenario = scenarios.Find(s => s.Name == name);
                if (scenario == null)
                    scenarios.Add(scenario = new WhatIf(name));
                if (parts.Length == 1)
                    continue;
                if (parts.Length != 4)
                    throw new FormatException($"{path} line {lineNumber}: expected 'name | asset path | field path | value', got '{line}'");
                scenario.Changes.Add(new Change { AssetPath = parts[1].Trim(), FieldPath = parts[2].Trim(), Value = parts[3].Trim() });
            }
            return scenarios;
        }

        /// <summary>Sets every change in memory (no Undo, no save). Fails loudly on a missing asset or field.</summary>
        public void Apply()
        {
            foreach (var change in Changes)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(change.AssetPath);
                Assert.That(asset, Is.Not.Null, $"what-if '{Name}': no asset at {change.AssetPath}");
                var so = new SerializedObject(asset);
                var field = so.FindProperty(change.FieldPath);
                Assert.That(field, Is.Not.Null, $"what-if '{Name}': {change.AssetPath} has no field {change.FieldPath}");
                string old = Read(field);
                bool wasDirty = EditorUtility.IsDirty(asset);
                Write(field, change.Value);
                so.ApplyModifiedPropertiesWithoutUndo();
                _undo.Add(() =>
                {
                    var back = new SerializedObject(asset);
                    Write(back.FindProperty(change.FieldPath), old);
                    back.ApplyModifiedPropertiesWithoutUndo();
                    // Back as it was on disk: don't leave it looking unsaved.
                    if (!wasDirty)
                        EditorUtility.ClearDirty(asset);
                });
            }
        }

        /// <summary>Puts every value back, newest first. Safe to call twice.</summary>
        public void Undo()
        {
            for (int i = _undo.Count - 1; i >= 0; i--)
                _undo[i]();
            _undo.Clear();
        }

        private static string Read(SerializedProperty field) => field.propertyType switch
        {
            SerializedPropertyType.Float => field.floatValue.ToString("R", CultureInfo.InvariantCulture),
            SerializedPropertyType.Integer => field.intValue.ToString(CultureInfo.InvariantCulture),
            SerializedPropertyType.Boolean => field.boolValue ? "true" : "false",
            SerializedPropertyType.Enum => field.enumValueIndex.ToString(CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException($"what-if: {field.propertyPath} is a {field.propertyType}; only numbers, true/false and enums can be tried"),
        };

        private static void Write(SerializedProperty field, string value)
        {
            switch (field.propertyType)
            {
                case SerializedPropertyType.Float: field.floatValue = float.Parse(value, CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.Integer: field.intValue = int.Parse(value, CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.Boolean: field.boolValue = bool.Parse(value); break;
                case SerializedPropertyType.Enum: field.enumValueIndex = int.Parse(value, CultureInfo.InvariantCulture); break;
                default: throw new NotSupportedException($"what-if: {field.propertyPath} is a {field.propertyType}; only numbers, true/false and enums can be tried");
            }
        }
    }
}
