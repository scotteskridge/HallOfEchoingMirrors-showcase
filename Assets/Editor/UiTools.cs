using System.Collections.Generic;
using System.Linq;
using System.Text;
using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>Editor helpers for working on the UI: its fonts and colours.</summary>
    public static class UiTools
    {
        private const string MenuApplyFonts = "Hall of Echoing Mirrors/UI/Apply UI Fonts";
        private const string MenuUpdateAtlases = "Hall of Echoing Mirrors/UI/Update UI Font Atlases";
        private const string MenuApplyColours = "Hall of Echoing Mirrors/UI/Apply UI Colours";

        public const string UiFontsPath = "Assets/Data/UI/UiFonts.asset";
        public const string UiColoursPath = "Assets/Data/UI/UiColours.asset";
        public const string GameTextPath = "Assets/Text/game_text.txt";
        public const string StoryFolder = "Assets/Story";
        private const string FontFolder = "Assets/Fonts";

        /// <summary>Symbols the code writes that the text files may not: … is what TextMeshPro cuts long text off with.</summary>
        public const string ExtraCharacters = "…×→·−–—‘’“”";

        // The book fonts (plan 011). Static weights, because TextMeshPro can't use variable fonts;
        // Inter's 18pt optical size is the one drawn for small text.
        /// <summary>The asset names of the book fonts' SDF assets (e.g. "Inter-Regular SDF").</summary>
        public static IEnumerable<string> BookFontNames => BookFonts.Select(font => font.asset);

        private static readonly (string source, string asset)[] BookFonts =
        {
            ("EB_Garamond/static/EBGaramond-Regular.ttf", "EBGaramond-Regular SDF"),
            ("EB_Garamond/static/EBGaramond-SemiBold.ttf", "EBGaramond-SemiBold SDF"),
            ("EB_Garamond/static/EBGaramond-Italic.ttf", "EBGaramond-Italic SDF"),
            ("EB_Garamond/static/EBGaramond-SemiBoldItalic.ttf", "EBGaramond-SemiBoldItalic SDF"),
            ("Inter/Inter_18pt-Regular.ttf", "Inter-Regular SDF"),
            ("Inter/Inter_18pt-SemiBold.ttf", "Inter-SemiBold SDF"),
            ("Inter/Inter_18pt-Italic.ttf", "Inter-Italic SDF"),
            ("Inter/Inter_18pt-SemiBoldItalic.ttf", "Inter-SemiBoldItalic SDF"),
        };

        // Atlas settings: 48 px glyphs with 5 px of padding stay crisp from 11 to 48 px on screen,
        // and about 300 of them fit in one 1024 square.
        private const int SamplingSize = 48;
        private const int AtlasPadding = 5;
        private const int AtlasSize = 1024;

        public static UiFonts LoadUiFonts() => AssetDatabase.LoadAssetAtPath<UiFonts>(UiFontsPath);
        public static UiColours LoadUiColours() => AssetDatabase.LoadAssetAtPath<UiColours>(UiColoursPath);

        /// <summary>A book font's SDF asset by its asset name (e.g. "Inter-Regular SDF"), or null if it isn't built yet.</summary>
        public static TMP_FontAsset BookFont(string assetName) =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontFolder}/{assetName}.asset");

        // ---------- Font atlases ----------

        /// <summary>
        /// Every character the UI needs: printable ASCII and Latin-1 (accents, £, «»), the symbols in
        /// ExtraCharacters, and whatever else the game text and story files use.
        /// </summary>
        public static string CharactersNeeded()
        {
            var needed = new SortedSet<char>();
            for (char c = ' '; c <= '~'; c++)
                needed.Add(c);
            for (char c = '\u00A0'; c <= '\u00FF'; c++)
                needed.Add(c);
            foreach (char c in ExtraCharacters)
                needed.Add(c);
            foreach (string text in TextFilesShown())
                foreach (char c in text)
                    if (!char.IsControl(c))
                        needed.Add(c);
            return string.Concat(needed);
        }

        /// <summary>The contents of the game text file and every story file.</summary>
        public static IEnumerable<string> TextFilesShown()
        {
            var gameText = AssetDatabase.LoadAssetAtPath<TextAsset>(GameTextPath);
            if (gameText == null)
                throw new System.InvalidOperationException($"UiTools: {GameTextPath} is missing.");
            yield return gameText.text;
            foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { StoryFolder }))
                yield return AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid)).text;
        }

        /// <summary>
        /// Builds any book font's SDF asset that's missing, and adds any needed character that an
        /// existing one lacks. The atlases are static (they don't change while playing), so run this
        /// again when new text brings a new character. Logs what each font can't draw (its fallbacks
        /// supply those).
        /// </summary>
        [MenuItem(MenuUpdateAtlases)]
        public static void UpdateFontAtlases()
        {
            string needed = CharactersNeeded();
            foreach (var (source, assetName) in BookFonts)
            {
                string sourcePath = $"{FontFolder}/{source}";
                var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
                if (font == null)
                {
                    Debug.LogError($"UI fonts: {sourcePath} is missing, so {assetName} wasn't built.");
                    continue;
                }

                string path = $"{FontFolder}/{assetName}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset == null)
                    asset = CreateStaticFont(font, assetName, path, needed);
                else
                    AddCharacters(asset, needed);

                string missing = Missing(asset, needed);
                if (missing.Length > 0)
                    Debug.Log($"UI fonts: {assetName} has no {missing} (its fallback fonts draw them).", asset);
            }
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset CreateStaticFont(Font font, string assetName, string path, string characters)
        {
            var asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
            asset.name = assetName;
            asset.TryAddCharacters(characters, out _);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            // The atlas texture and material live inside the font asset, as TextMeshPro's own Font Asset Creator saves them.
            AssetDatabase.CreateAsset(asset, path);
            foreach (var texture in asset.atlasTextures)
            {
                texture.name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            Debug.Log($"UI fonts: built {path} ({asset.characterTable.Count} characters).", asset);
            return asset;
        }

        private static void AddCharacters(TMP_FontAsset asset, string characters)
        {
            if (Missing(asset, characters).Length == 0)
                return;
            int before = asset.characterTable.Count;
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            asset.TryAddCharacters(characters, out _);
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            if (asset.characterTable.Count > before)
            {
                EditorUtility.SetDirty(asset);
                Debug.Log($"UI fonts: added {asset.characterTable.Count - before} characters to {asset.name}.", asset);
            }
        }

        /// <summary>The characters in <paramref name="characters"/> that this font asset itself can't draw (fallbacks not counted).</summary>
        public static string Missing(TMP_FontAsset asset, string characters)
        {
            var missing = new StringBuilder();
            foreach (char c in characters)
                if (c != ' ' && c != '\u00A0' && !asset.HasCharacter(c, searchFallbacks: false, tryAddCharacter: false))
                    missing.Append(c);
            return missing.ToString();
        }

        // ---------- Applying the fonts ----------

        /// <summary>
        /// Every text tagged with a FontRole, in the UI prefabs and the open scene, takes its role's
        /// font and size from the UI Fonts asset. Prefabs first, so their copies in the scene follow.
        /// </summary>
        [MenuItem(MenuApplyFonts)]
        public static void ApplyUiFontsFromMenu()
        {
            var fonts = LoadUiFonts();
            if (fonts == null)
            {
                EditorUtility.DisplayDialog("Apply UI Fonts", $"{UiFontsPath} doesn't exist yet: run Setup Step 89 first.", "OK");
                return;
            }
            // Prefab changes are saved straight away and Ctrl+Z can't undo them, so ask first.
            if (!EditorUtility.DisplayDialog("Apply UI Fonts?",
                    "Every tagged text in the UI prefabs and the open scene takes its font and size from UiFonts. " +
                    "The prefabs are saved at once and can't be undone with Ctrl+Z (git can bring them back).",
                    "Apply", "Cancel"))
                return;
            ApplyUiFonts(fonts);
        }

        /// <summary>As the menu command, without asking. Returns how many texts were set.</summary>
        public static int ApplyUiFonts(UiFonts fonts)
        {
            int applied = 0;
            var untagged = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    applied += ApplyUnder(root, fonts, undoable: false, untagged);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                applied += ApplyUnder(root, fonts, undoable: true, untagged);
            EditorSceneManager.MarkSceneDirty(scene);

            foreach (string path in untagged)
                Debug.LogWarning($"UI fonts: {path} has no Font Role, so its font wasn't set. Add one (Setup Step 89 tags every text).");
            Debug.Log($"UI fonts: {applied} texts set from {fonts.name}. Press Ctrl+S to save the scene.", fonts);
            return applied;
        }

        /// <summary>Gives every FontRole-tagged text under <paramref name="root"/> its role's font and size; returns how many.</summary>
        public static int ApplyUnder(GameObject root, UiFonts fonts, bool undoable, List<string> untagged)
        {
            int applied = 0;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var role = text.GetComponent<FontRole>();
                if (role == null)
                {
                    untagged.Add(PathOf(text.transform));
                    continue;
                }
                // Also records a prefab copy's override, for just what changed (usually nothing: it follows its prefab).
                if (undoable)
                    Undo.RecordObject(text, "Apply UI Fonts");
                fonts.Apply(text, role.Role, role.Scale);
                applied++;
                // As for colours below: Undo.RecordObject misses a prefab copy's own override, so a text whose
                // font or size was overridden would keep serialising the old value.
                if (undoable || PrefabUtility.IsPartOfPrefabInstance(text))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            return applied;
        }

        /// <summary>A UI object's place in its hierarchy, e.g. "Canvas/MainScreen/TopBar/ClockLabel".</summary>
        public static string PathOf(Transform target) =>
            target.parent == null ? target.name : PathOf(target.parent) + "/" + target.name;

        // ---------- Applying the colours (plan 012) ----------

        /// <summary>
        /// Every Graphic tagged with a Colour Role, in the UI prefabs and the open scene, takes its
        /// role's colour from the UI Colours asset. A tagged button whose ColorBlock is still
        /// ColorBlock.defaultColorBlock gets its highlighted/pressed/disabled shades derived (a hand-tuned
        /// ColorBlock is left alone after that). Prefabs first, so their copies in the scene follow.
        /// </summary>
        [MenuItem(MenuApplyColours)]
        public static void ApplyUiColoursFromMenu()
        {
            var colours = LoadUiColours();
            if (colours == null)
            {
                EditorUtility.DisplayDialog("Apply UI Colours", $"{UiColoursPath} doesn't exist yet: run Setup Step 90 first.", "OK");
                return;
            }
            // Prefab changes are saved straight away and Ctrl+Z can't undo them, so ask first.
            if (!EditorUtility.DisplayDialog("Apply UI Colours?",
                    "Every Graphic tagged with a Colour Role in the UI prefabs and the open scene takes its colour from " +
                    "UiColours. The prefabs are saved at once and can't be undone with Ctrl+Z (git can bring them back).",
                    "Apply", "Cancel"))
                return;
            ApplyUiColours(colours);
        }

        /// <summary>As the menu command, without asking. Returns how many Graphics were set.</summary>
        public static int ApplyUiColours(UiColours colours)
        {
            int applied = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    applied += ApplyColoursUnder(root, colours, undoable: false);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                applied += ApplyColoursUnder(root, colours, undoable: true);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"UI colours: {applied} Graphics set from {colours.name}. Press Ctrl+S to save the scene.", colours);
            return applied;
        }

        private static int ApplyColoursUnder(GameObject root, UiColours colours, bool undoable)
        {
            int applied = 0;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                var tag = graphic.GetComponent<ColourRoleTag>();
                if (tag == null)
                    continue;
                if (undoable)
                    Undo.RecordObject(graphic, "Apply UI Colours");
                colours.Apply(graphic, tag.Role);
                applied++;
                // A prefab copy's own override isn't picked up by Undo.RecordObject: without this, the scene
                // keeps serialising its old override value even though the live field is now correct.
                if (undoable || PrefabUtility.IsPartOfPrefabInstance(graphic))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(graphic);

                var button = graphic.GetComponent<Selectable>();
                if (button != null && button.targetGraphic == graphic && button.colors.Equals(ColorBlock.defaultColorBlock))
                {
                    if (undoable)
                        Undo.RecordObject(button, "Apply UI Colours");
                    button.colors = DerivedColorBlock;
                    if (undoable)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
                }
            }
            return applied;
        }

        /// <summary>
        /// A recoloured button's face dims until hovered (a tint can only darken): Step 88's ratios,
        /// now derived here instead of hard-coded per button.
        /// </summary>
        private static ColorBlock DerivedColorBlock
        {
            get
            {
                var colours = ColorBlock.defaultColorBlock;
                colours.normalColor = colours.selectedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                colours.highlightedColor = Color.white;
                colours.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
                colours.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);
                return colours;
            }
        }
    }
}
