using System;
using System.Text;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Building blocks for editor setup commands: making UI pieces, wiring Inspector fields,
    /// and finding or creating assets. Every setup step uses these rather than repeating them.
    /// </summary>
    public static class EditorUiFactory
    {
        public const string ItemsFolder = "Assets/Data/Items"; // not "Resources": Unity treats that name specially
        public const string ContentPath = "Assets/Data/GameContent.asset";
        public const string PrefabFolder = "Assets/Prefabs/UI";

        /// <summary>
        /// The game's one GameContent: the asset at <see cref="ContentPath"/>, else the first one found.
        /// Warns if there is more than one (the tools would otherwise quietly use whichever Unity lists
        /// first), and returns null with a warning if there is none.
        /// </summary>
        public static GameContent LoadContent()
        {
            var main = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameContent));
            if (main != null)
            {
                if (guids.Length > 1)
                    Debug.LogWarning($"There are {guids.Length} GameContent assets; the tools use {ContentPath}.", main);
                return main;
            }
            if (guids.Length == 0)
            {
                Debug.LogWarning($"No GameContent asset found (expected {ContentPath}).");
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Debug.LogWarning($"No GameContent at {ContentPath}; using {path} (of {guids.Length} found).");
            return AssetDatabase.LoadAssetAtPath<GameContent>(path);
        }

        // ---------- UI pieces ----------

        private static Sprite UiSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        private static Sprite UiBackground() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        public static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = UiSprite(),
            background = UiBackground(),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        };

        private static TMP_DefaultControls.Resources TmpResources() => new TMP_DefaultControls.Resources
        {
            standard = UiSprite(),
            background = UiBackground(),
        };

        /// <summary>An empty container that stacks its children top to bottom.</summary>
        public static RectTransform MakeColumn(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(parent, false);
            Place(go.transform, anchor, position, size);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            SetUpStack(layout, controlHeight: false, spacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            return (RectTransform)go.transform;
        }

        /// <summary>A text in its role's font and size (the UiFonts asset) and colour role (the UiColours asset), tagged with both.</summary>
        public static TMP_Text MakeText(string name, Transform parent, TextRole role, float height, ColourRole colourRole = ColourRole.TextMain)
        {
            var go = TMP_DefaultControls.CreateText(TmpResources());
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.name = name;
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(0f, height);

            var text = go.GetComponent<TMP_Text>();
            text.text = "";
            GiveRole(text, role);
            GiveColour(text, colourRole);
            return text;
        }

        /// <summary>Tags a text with its role and gives it that role's font and size now.</summary>
        public static void GiveRole(TMP_Text text, TextRole role, float scale = 1f)
        {
            var fonts = UiTools.LoadUiFonts();
            if (fonts == null)
                throw new InvalidOperationException($"Setup: {UiTools.UiFontsPath} is missing: run Setup Step 89 (book fonts) first.");
            var tag = GetOrAdd<FontRole>(text.gameObject);
            var so = new SerializedObject(tag);
            so.FindProperty("_role").enumValueIndex = (int)role;
            so.FindProperty("_scale").floatValue = scale;
            so.ApplyModifiedProperties();
            Undo.RecordObject(text, "Setup");
            fonts.Apply(text, role, scale);
        }

        /// <summary>Tags a Graphic with its colour role and gives it that role's colour now.</summary>
        public static void GiveColour(Graphic graphic, ColourRole role)
        {
            var colours = UiTools.LoadUiColours();
            if (colours == null)
                throw new InvalidOperationException($"Setup: {UiTools.UiColoursPath} is missing: run Setup Step 90 (UI colours) first.");
            var tag = GetOrAdd<ColourRoleTag>(graphic.gameObject);
            var so = new SerializedObject(tag);
            so.FindProperty("_role").enumValueIndex = (int)role;
            so.ApplyModifiedProperties();
            Undo.RecordObject(graphic, "Setup");
            colours.Apply(graphic, role);
        }

        /// <summary>A button: an instance of the Button kind (AccentButton for the Accent role), so its look comes from the kind prefab.</summary>
        public static Button MakeButton(string name, Transform parent, string label, float height, ColourRole colourRole = ColourRole.ButtonFace)
        {
            string kind = colourRole switch
            {
                ColourRole.ButtonFace => UiKinds.Button,
                ColourRole.Accent => UiKinds.AccentButton,
                _ => throw new ArgumentException($"MakeButton: no button kind for {colourRole}; use ButtonFace or Accent, or add a kind in UiKinds."),
            };
            var go = (GameObject)PrefabUtility.InstantiatePrefab(UiKinds.LoadKind(kind), parent);
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.name = name;
            ((RectTransform)go.transform).sizeDelta = new Vector2(0f, height);
            go.GetComponentInChildren<TMP_Text>().text = label;
            return go.GetComponent<Button>();
        }

        /// <summary>A display-only bar: a slider with no handle that players can't drag.</summary>
        public static Slider MakeDisplayBar(string name, Transform parent, float height, ColourRole fillRole, bool flush = false)
        {
            var go = DefaultControls.CreateSlider(UiResources());
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.name = name;
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(0f, height);

            var handleArea = go.transform.Find("Handle Slide Area");
            if (handleArea != null)
                Object.DestroyImmediate(handleArea.gameObject);

            var slider = go.GetComponent<Slider>();
            slider.handleRect = null;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.value = 0f;

            var fillImage = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fillImage != null)
                GiveColour(fillImage, fillRole);
            var backgroundImage = go.transform.Find("Background")?.GetComponent<Image>();
            if (backgroundImage != null)
                GiveColour(backgroundImage, ColourRole.Track);

            // Flush: background and fill reach all four edges, so bars can sit touching.
            if (flush)
            {
                foreach (var part in new[] { "Background", "Fill Area" })
                {
                    if (go.transform.Find(part) is RectTransform rect)
                    {
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                    }
                }
                if (slider.fillRect != null)
                    slider.fillRect.sizeDelta = Vector2.zero;
            }
            return slider;
        }

        /// <summary>A label stretched across a bar, e.g. "Endurance" on the left or "Lv 2" on the right.</summary>
        public static TMP_Text MakeBarLabel(string name, Transform bar, TextAlignmentOptions alignment)
        {
            var label = MakeText(name, bar, TextRole.Body, 0f, ColourRole.AccentText);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Pins a UI element to an anchor (e.g. top-left) and grows it away from that edge.</summary>
        public static void Place(Transform target, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (target == null)
            {
                Debug.LogWarning("Setup: nothing to place (the target is missing), so it was skipped.");
                return;
            }
            var rect = (RectTransform)target;
            Undo.RecordObject(rect, "Setup");
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Stretches a UI element between two corners of its parent (fractions, 0 to 1), inset by a margin.</summary>
        public static void Stretch(Transform target, Vector2 min, Vector2 max, float margin = 0f)
        {
            var rect = (RectTransform)target;
            Undo.RecordObject(rect, "Setup");
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        /// <summary>
        /// An empty frame stretched between two corners of its parent (fractions, 0 to 1), with a
        /// background in <paramref name="backgroundRole"/> if given (a clear frame catches no clicks).
        /// </summary>
        public static RectTransform MakeFrame(string name, Transform parent, Vector2 min, Vector2 max, ColourRole? backgroundRole = null, float margin = 0f)
        {
            var go = backgroundRole.HasValue
                ? new GameObject(name, typeof(RectTransform), typeof(Image))
                : new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(parent, false);
            if (backgroundRole.HasValue)
            {
                var image = go.GetComponent<Image>();
                image.sprite = UiResources().standard;
                image.type = Image.Type.Sliced;
                GiveColour(image, backgroundRole.Value);
            }
            Stretch(go.transform, min, max, margin);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// Moves an existing UI element into another parent (undoably), stretches it between two
        /// corners of that parent, and logs the move.
        /// </summary>
        public static void MoveInto(Transform target, Transform parent, Vector2 min, Vector2 max, float margin = 0f)
        {
            string from = target.parent != null ? target.parent.name : "(top)";
            Undo.SetTransformParent(target, parent, false, "Setup");
            Stretch(target, min, max, margin);
            Debug.Log($"Setup: moved {target.name} from {from} into {parent.name}.", target);
        }

        /// <summary>
        /// A scrolling list: a mask, and a content column that grows with its rows (each row keeps
        /// its own height). Returns the content, where rows (or a row template) go.
        /// </summary>
        public static RectTransform MakeScrollList(string name, Transform parent, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear; // catches the mouse wheel between rows

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch(viewport.transform, Vector2.zero, Vector2.one);

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = (RectTransform)content.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            SetUpStack(content.GetComponent<VerticalLayoutGroup>(), controlHeight: false, spacing);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = rect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return rect;
        }

        /// <summary>
        /// A list that scrolls left to right, with a scrollbar underneath (shown only when needed):
        /// each item is its preferred width (give it a LayoutElement) and fills the height. Returns the content, where items go.
        /// The mouse wheel scrolls it too, over any part that doesn't scroll by itself.
        /// </summary>
        public static RectTransform MakeSideScroll(string name, Transform parent, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(go, "Setup");
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.clear; // catches the mouse wheel between items

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(go.transform, false);
            Stretch(viewport.transform, Vector2.zero, Vector2.one);

            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rect = (RectTransform)content.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true; // each item as wide as its preferred width (a LayoutElement)
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = DefaultControls.CreateScrollbar(UiResources());
            bar.name = "Scrollbar";
            bar.transform.SetParent(go.transform, false);
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = new Vector2(0f, 14f);
            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.LeftToRight;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = rect;
            scroll.vertical = false;
            scroll.horizontalScrollbar = scrollbar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.horizontalScrollbarSpacing = 4f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return rect;
        }

        /// <summary>
        /// Stacks a GameObject's children top to bottom, full width. With <paramref name="controlHeight"/>
        /// off, children keep their own height (e.g. prefab rows); on, each takes its preferred height.
        /// </summary>
        public static void AddStack(GameObject go, bool controlHeight, float spacing, float padding = 0f)
        {
            var layout = GetOrAdd<VerticalLayoutGroup>(go);
            SetUpStack(layout, controlHeight, spacing, padding);
        }

        /// <summary>The one vertical-stack setup MakeColumn, AddStack and MakeScrollList share.</summary>
        private static void SetUpStack(VerticalLayoutGroup layout, bool controlHeight, float spacing, float padding = 0f)
        {
            int pad = Mathf.RoundToInt(padding);
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandHeight = false;
        }

        /// <summary>Gives a part of a stack (AddStack) a fixed height.</summary>
        public static RectTransform FixHeight(RectTransform part, float height)
        {
            GetOrAdd<LayoutElement>(part.gameObject).preferredHeight = height;
            return part;
        }

        /// <summary>
        /// The component if the object has one (recorded for undo), or a new one. Undo.AddComponent
        /// returns null when a component that allows only one per object is already there, so a
        /// setup step run twice would otherwise throw.
        /// </summary>
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing == null)
                return Undo.AddComponent<T>(go);
            Undo.RecordObject(existing, "Setup");
            return existing;
        }

        /// <summary>A row of chips (MakeChip), left to right, each as wide as its words.</summary>
        public static RectTransform MakeChipRow(Transform parent, string name, float spacing)
        {
            var row = MakeFrame(name, parent, Vector2.zero, Vector2.one);
            var layout = Undo.AddComponent<HorizontalLayoutGroup>(row.gameObject);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return row;
        }

        /// <summary>A chip: an instance of the Chip kind (ChipFaint for the faint role) around one line of words (its Label), optionally a button. Returns its T component.</summary>
        public static T MakeChip<T>(Transform row, string name, ColourRole backgroundRole, bool button) where T : Component
        {
            string kind = backgroundRole switch
            {
                ColourRole.ChipBackground => UiKinds.Chip,
                ColourRole.ChipBackgroundFaint => UiKinds.ChipFaint,
                _ => throw new ArgumentException($"MakeChip: no chip kind for {backgroundRole}; use ChipBackground or ChipBackgroundFaint, or add a kind in UiKinds."),
            };
            var chip = (GameObject)PrefabUtility.InstantiatePrefab(UiKinds.LoadKind(kind), row);
            Undo.RegisterCreatedObjectUndo(chip, "Setup");
            chip.name = name;
            Stretch(chip.transform, Vector2.zero, Vector2.one);
            if (button)
                Undo.AddComponent<Button>(chip).targetGraphic = chip.GetComponent<Image>();
            var layout = Undo.AddComponent<HorizontalLayoutGroup>(chip);
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.childControlWidth = layout.childControlHeight = true;
            var label = MakeText("Label", chip.transform, TextRole.Body, 0f);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return chip.GetComponent<T>();
        }

        /// <summary>Switches a scene object off (undoably), keeping it, and logs it. Warns if it's missing.</summary>
        public static void SwitchOff(Transform target, string what)
        {
            if (target == null)
            {
                Debug.LogWarning($"Setup: {what} wasn't found, so it's still on: switch it off by hand.");
                return;
            }
            Undo.RecordObject(target.gameObject, "Setup");
            target.gameObject.SetActive(false);
            Debug.Log($"Setup: switched off {target.name} (kept, not deleted).", target);
        }

        /// <summary>The main page (MainScreen), found through the Screen Manager. Null if the scene isn't open.</summary>
        public static RectTransform MainPage()
        {
            var screens = Object.FindFirstObjectByType<ScreenManager>();
            if (screens == null)
                return null;
            foreach (var entry in screens.Screens)
                if (entry.id == ScreenId.Main && entry.group != null)
                    return entry.group.transform as RectTransform;
            return null;
        }

        // ---------- Wiring ----------

        /// <summary>Makes a scene label read its words from the game text file (key e.g. "scene.mainscreen_queue").</summary>
        public static void SetTextKey(TMP_Text label, string key)
        {
            var textKey = label.GetComponent<TextKey>();
            if (textKey == null)
                textKey = Undo.AddComponent<TextKey>(label.gameObject);
            var so = new SerializedObject(textKey);
            so.FindProperty("_key").stringValue = key;
            so.ApplyModifiedProperties();
        }

        /// <summary>Gives a UI element a hover pop-up reading a line from the game text file (e.g. "tips.pause").</summary>
        public static void SetTip(Component target, string key)
        {
            var tip = target.GetComponent<ToolTip>();
            if (tip == null)
                tip = Undo.AddComponent<ToolTip>(target.gameObject);
            var so = new SerializedObject(tip);
            so.FindProperty("_textKey").stringValue = key;
            so.ApplyModifiedProperties();
        }

        /// <summary>Fills a component's Inspector fields, as if dragging each value in.</summary>
        public static void Wire(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"Setup: {target.GetType().Name} has no field called {field}.", target);
                    continue;
                }
                property.objectReferenceValue = value;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// As Wire, but only fills fields that are still empty, so running a step again never
        /// overwrites what's been set by hand. Logs each field it fills.
        /// </summary>
        public static void WireEmpty(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"Setup: {target.GetType().Name} has no field called {field}.", target);
                    continue;
                }
                if (property.objectReferenceValue != null)
                    continue;
                property.objectReferenceValue = value;
                Debug.Log($"Setup: set {target.GetType().Name}.{field} to {(value != null ? value.name : "nothing")}.", target);
            }
            so.ApplyModifiedProperties();
        }

        // ---------- Assets ----------

        /// <summary>Loads an asset if it exists (keeping any values you've tuned), otherwise creates it.</summary>
        public static T GetOrCreateAsset<T>(string folder, string fileName, Action<T> initialise) where T : ScriptableObject
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            EnsureFolder(folder);
            var asset = ScriptableObject.CreateInstance<T>();
            initialise(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// Finds a resource anywhere in the project by its display name (or file name), or creates
        /// it in Assets/Data/Items. Never makes a duplicate.
        /// </summary>
        public static ResourceDefinition FindOrCreateResource(string displayName, ResourceLifetime lasts, int max)
        {
            var existing = FindResource(displayName);
            if (existing != null)
                return existing;

            string fileName = FileNameFor(displayName);
            return GetOrCreateAsset<ResourceDefinition>(ItemsFolder, fileName, r =>
            {
                r.displayName = displayName;
                r.lasts = lasts;
                r.startingMax = max;
            });
        }

        /// <summary>A resource anywhere in the project with this display name (or file name), or null.</summary>
        public static ResourceDefinition FindResource(string displayName)
        {
            string fileName = FileNameFor(displayName);
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceDefinition"))
            {
                var existing = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (existing != null &&
                    (string.Equals(existing.DisplayName, displayName, StringComparison.OrdinalIgnoreCase) ||
                     existing.name == fileName))
                    return existing;
            }
            return null;
        }

        /// <summary>Turns a scene object into a prefab in Assets/Prefabs/UI (unless one already exists).</summary>
        public static void MakePrefab(Transform target, string prefabName)
        {
            if (target == null)
            {
                Debug.LogWarning($"Setup: couldn't find the object for the {prefabName} prefab.");
                return;
            }
            if (PrefabUtility.IsPartOfPrefabInstance(target.gameObject))
                return;

            string path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.LogWarning($"Setup: {path} already exists, so {target.name} was not converted.", target);
                return;
            }

            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAssetAndConnect(target.gameObject, path, InteractionMode.UserAction);
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }

        /// <summary>"Tool A" becomes "ToolA": a tidy file name from a display name.</summary>
        public static string FileNameFor(string displayName)
        {
            var sb = new StringBuilder();
            foreach (char c in displayName)
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            return sb.Length > 0 ? sb.ToString() : "Resource";
        }
    }
}
