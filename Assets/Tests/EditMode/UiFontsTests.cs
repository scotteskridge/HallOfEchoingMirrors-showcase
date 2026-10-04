using System;
using System.Collections.Generic;
using System.Linq;
using HallOfEchoingMirrors.EditorTools;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The UI's fonts (plan 011): every role set, every character drawable, every text tagged.</summary>
    public class UiFontsTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        // The symbols the code writes into text (… is also what TextMeshPro cuts long text off with).
        private const string CodeSymbols = "…×→·−";

        private static UiFonts Fonts()
        {
            var fonts = UiTools.LoadUiFonts();
            Assert.That(fonts, Is.Not.Null, $"{UiTools.UiFontsPath} is missing: run Setup Step 89.");
            return fonts;
        }

        [Test]
        public void Fonts_EveryRole_HasExactlyOneFontAndSize()
        {
            var fonts = Fonts();
            foreach (TextRole role in Enum.GetValues(typeof(TextRole)))
            {
                var style = fonts.Find(role);
                Assert.That(style, Is.Not.Null, $"{role} has no entry in UiFonts");
                Assert.That(style.font, Is.Not.Null, $"{role} has no font");
                Assert.That(style.size, Is.GreaterThan(0f), $"{role} has no size");
                Assert.That(fonts.Styles.Count(s => s.role == role), Is.EqualTo(1), $"{role} is in UiFonts more than once");
            }
        }

        [Test]
        public void Fonts_CoverEveryCharacterInGameText()
        {
            var gameText = AssetDatabase.LoadAssetAtPath<TextAsset>(UiTools.GameTextPath);
            Assert.That(gameText, Is.Not.Null);
            var needed = new HashSet<char>(gameText.text + CodeSymbols);
            needed.RemoveWhere(c => char.IsControl(c) || char.IsWhiteSpace(c));

            // The title's font only shows the title, but its fallbacks must still cover everything.
            foreach (var style in Fonts().Styles)
            {
                var missing = needed.Where(c => !Draws(style.font, c, new HashSet<TMP_FontAsset>())).ToArray();
                Assert.That(missing, Is.Empty,
                    $"{style.role}'s font {style.font.name} (and its fallbacks) can't draw: {new string(missing)}. " +
                    "Run Hall of Echoing Mirrors → UI → Update UI Font Atlases.");
            }
        }

        /// <summary>
        /// The font, or one of its own fallbacks, has the character. Only fonts with a fixed character
        /// set count: a dynamic font would add it when asked, but only if its source file has it.
        /// </summary>
        private static bool Draws(TMP_FontAsset font, char c, HashSet<TMP_FontAsset> seen)
        {
            if (font == null || !seen.Add(font))
                return false;
            if (font.atlasPopulationMode == AtlasPopulationMode.Static && font.HasCharacter(c, searchFallbacks: false, tryAddCharacter: false))
                return true;
            return font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Any(f => Draws(f, c, seen));
        }

        [Test]
        public void HeadingMarkup_NamesTheHeadingRolesFont()
        {
            Assert.That(Fonts().For(TextRole.Heading).font.name, Is.EqualTo(UiStyle.HeadingFont),
                "UiStyle.Heading switches font by name: rename UiStyle.HeadingFont to match the Heading role's font");
        }

        [Test]
        public void Texts_EachHasARole()
        {
            var fonts = Fonts();
            var problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
                    Check(text, fonts, problems);
            }

            // The scene as it is on disk (or as it's open, if it is).
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                        Check(text, fonts, problems);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void ApplyPersistsOnPrefabInstanceOverride()
        {
            var fonts = Fonts();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/EditMode/_FontsTestPrefab.prefab");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var source = new GameObject("T", typeof(RectTransform));
                source.AddComponent<TextMeshProUGUI>().fontSize = 50f;
                source.AddComponent<FontRole>(); // after the text: FontRole requires one
                PrefabUtility.SaveAsPrefabAsset(source, path);
                UnityEngine.Object.DestroyImmediate(source);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), preview);
                var text = instance.GetComponent<TMP_Text>();
                text.fontSize = 99f; // an override of the prefab's size
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);

                UiTools.ApplyUnder(instance, fonts, undoable: true, new List<string>());

                var recorded = PrefabUtility.GetPropertyModifications(instance)
                    .Where(m => m.target is TMP_Text && m.propertyPath == "m_fontSize").ToList();
                Assert.That(recorded.Select(m => m.value), Does.Not.Contain("99"), "the old override is still what the scene would save");
                Assert.That(recorded, Has.Count.EqualTo(1));
                Assert.That(float.Parse(recorded[0].value, System.Globalization.CultureInfo.InvariantCulture),
                    Is.EqualTo(fonts.For(TextRole.Body).size).Within(0.001f));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void Check(TMP_Text text, UiFonts fonts, List<string> problems)
        {
            var role = text.GetComponent<FontRole>();
            if (role == null)
                problems.Add($"{UiTools.PathOf(text.transform)} has no Font Role");
            else if (text.font != fonts.For(role.Role).font)
                problems.Add($"{UiTools.PathOf(text.transform)} is {role.Role} but uses {(text.font != null ? text.font.name : "no font")}: " +
                             "run Hall of Echoing Mirrors → UI → Apply UI Fonts");
        }
    }
}
