using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// The button and chip kinds (plan ui-035): small prefabs in Assets/Prefabs/UI/Kinds that every
    /// button and chip is an instance of, so restyling a kind is one prefab edit. A kind owns the
    /// face's sprite and colour role, the button's transition and colours, and the label's font role
    /// and colour role. Each place keeps its own size, layout, label text, TextKey, ToolTip and view scripts.
    /// <para>*Build Kind Prefabs* is repeatable (never overwrites a kind that exists).</para>
    /// </summary>
    public static class UiKinds
    {
        public const string KindFolder = EditorUiFactory.PrefabFolder + "/Kinds";
        private const string MenuBuild = "Hall of Echoing Mirrors/UI/Build Kind Prefabs";

        public const string Button = "Button";
        public const string AccentButton = "AccentButton";
        public const string IconButton = "IconButton";
        public const string TabButton = "TabButton";
        public const string SmallButton = "SmallButton";
        public const string Chip = "Chip";
        public const string ChipFaint = "ChipFaint";

        public static string PathOfKind(string kind) => $"{KindFolder}/{kind}.prefab";

        public static GameObject LoadKind(string kind)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOfKind(kind));
            if (prefab == null)
                throw new System.InvalidOperationException($"Kind prefab {PathOfKind(kind)} is missing: run Hall of Echoing Mirrors → UI → Build Kind Prefabs.");
            return prefab;
        }

        /// <summary>True if this object is the root of a kind instance (a plain one, or nested in a bigger prefab or the scene).</summary>
        public static bool IsKind(GameObject go)
        {
            if (PrefabUtility.GetNearestPrefabInstanceRoot(go) != go)
                return false;
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            return !string.IsNullOrEmpty(path) && path.StartsWith(KindFolder + "/");
        }

        // ---------- Which kind is this object? ----------

        /// <summary>The kind a Button should be, from its name and size. Null for a map room node (MapRoom), which MapStyle styles.</summary>
        public static string ButtonKindFor(Button button)
        {
            // MapNode is the map's room template (MapView clones it; MapStyle styles the clones); a chip that is also a button is a chip kind.
            if (button.name == "MapNode" || button.GetComponent<MapRoom>() != null || ChipKindFor(button.gameObject) != null)
                return null;
            string name = button.name;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var labelRole = label != null ? label.GetComponent<FontRole>() : null;
            var colourTag = button.GetComponent<ColourRoleTag>();

            if (colourTag != null && colourTag.Role == ColourRole.Accent)
                return AccentButton;
            if (name.EndsWith("Tab") || name == "SpeedTier")
                return TabButton;
            if (labelRole != null && labelRole.Role == TextRole.Small)
                return SmallButton;
            // An icon button's caption is one symbol (× − + ^). Not its width: a prefab's size is often set by a layout.
            if (label != null && label.text.Trim().Length <= 1)
                return IconButton;
            return Button;
        }

        /// <summary>The kind a chip template should be (Chip or ChipFaint), or null if this isn't a chip (no chip colour role).</summary>
        public static string ChipKindFor(GameObject go)
        {
            var tag = go.GetComponent<ColourRoleTag>();
            if (tag == null)
                return null;
            return tag.Role switch
            {
                ColourRole.ChipBackground => Chip,
                ColourRole.ChipBackgroundFaint => ChipFaint,
                _ => null,
            };
        }

        // ---------- Building the kind prefabs ----------

        [MenuItem(MenuBuild)]
        public static void BuildKindPrefabsFromMenu()
        {
            BuildKindPrefabs();
            Debug.Log("UI kinds: kind prefabs are in " + KindFolder + ". Run Apply UI Colours and Apply UI Fonts to finish their colours.");
        }

        /// <summary>Makes the kind prefabs that are missing; a kind that exists is left as it is (it may be hand-tuned).</summary>
        public static void BuildKindPrefabs()
        {
            EditorUiFactory.EnsureFolder(KindFolder);
            MakeIfMissing(Button, BuildButtonBase);
            MakeVariantIfMissing(AccentButton, Button, v =>
            {
                EditorUiFactory.GiveColour(v.GetComponent<Image>(), ColourRole.Accent);
                EditorUiFactory.GiveColour(v.GetComponentInChildren<TMP_Text>(), ColourRole.AccentText);
            });
            MakeVariantIfMissing(IconButton, Button, _ => { });
            MakeVariantIfMissing(TabButton, Button, _ => { });
            MakeVariantIfMissing(SmallButton, Button, v =>
                EditorUiFactory.GiveRole(v.GetComponentInChildren<TMP_Text>(), TextRole.Small));

            MakeIfMissing(Chip, () => BuildChipBase(ColourRole.ChipBackground));
            MakeVariantIfMissing(ChipFaint, Chip, v =>
                EditorUiFactory.GiveColour(v.GetComponent<Image>(), ColourRole.ChipBackgroundFaint));
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildButtonBase()
        {
            var go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            });
            var label = go.GetComponentInChildren<TMP_Text>();
            label.text = "Button";
            EditorUiFactory.GiveRole(label, TextRole.Body);
            EditorUiFactory.GiveColour(label, ColourRole.TextMain);
            EditorUiFactory.GiveColour(go.GetComponent<Button>().targetGraphic, ColourRole.ButtonFace);
            return go;
        }

        private static GameObject BuildChipBase(ColourRole role)
        {
            var go = new GameObject(Chip, typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            EditorUiFactory.GiveColour(image, role);
            return go;
        }

        private static void MakeIfMissing(string kind, System.Func<GameObject> build)
        {
            string path = PathOfKind(kind);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;
            var go = build();
            go.name = kind;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"UI kinds: built {path}.");
        }

        private static void MakeVariantIfMissing(string kind, string baseKind, System.Action<GameObject> tweak)
        {
            string path = PathOfKind(kind);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadKind(baseKind));
            instance.name = kind;
            tweak(instance);
            PrefabUtility.SaveAsPrefabAsset(instance, path); // saving an instance of a prefab makes a variant of it
            Object.DestroyImmediate(instance);
            Debug.Log($"UI kinds: built {path} (a variant of {baseKind}).");
        }
    }
}
