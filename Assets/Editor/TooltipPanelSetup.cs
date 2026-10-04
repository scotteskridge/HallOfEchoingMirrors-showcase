using HallOfEchoingMirrors.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// *Hall of Echoing Mirrors → Setup → Rebuild Tooltip Panel* (plan ui-036a): rebuilds ToolTip.prefab
    /// as a frame around two bodies, the plain text box and the layout (kicker, title, rows, footer).
    /// The scene's instance keeps its own settings (the toast it keeps clear of, the delay), because only
    /// the prefab's contents are rebuilt, never the instance.
    /// One-shot: it rebuilds the panel from scratch, so hand tweaks to the prefab's children would be lost.
    /// Run again on an already-built panel, it asks first. Retire it (delete this file and its .meta) once
    /// the rebuilt prefab is committed (editor-tools.md).
    /// </summary>
    public static class TooltipPanelSetup
    {
        private const string PrefabPath = EditorUiFactory.PrefabFolder + "/ToolTip.prefab";
        private const string Menu = "Hall of Echoing Mirrors/Setup/Rebuild Tooltip Panel";

        [MenuItem(Menu)]
        public static void RebuildFromMenu()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null && prefab.transform.Find("Frame/Layout") != null &&
                !EditorUtility.DisplayDialog("Rebuild Tooltip Panel",
                    "The tooltip panel is already built. Rebuilding it loses any changes made by hand to its parts " +
                    "(paddings, sizes, fonts). The scene's own settings are kept.", "Rebuild", "Cancel"))
                return;
            Rebuild();
            Debug.Log($"Setup: rebuilt {PrefabPath}. Hover an action in a room's popover to see it.");
        }

        public static void Rebuild()
        {
            var colours = UiTools.LoadUiColours();
            if (colours == null)
                throw new System.InvalidOperationException($"Setup: {UiTools.UiColoursPath} is missing: run Setup Step 90 (UI colours) first.");

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Build(root, colours);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Build(GameObject root, UiColours colours)
        {
            // The old plain text box is kept (not recreated), so the panel's reference to it, and anything
            // else pointing at it, stays valid.
            var panel = root.GetComponent<ToolTipPanel>();
            if (panel == null)
                throw new System.InvalidOperationException("Setup: ToolTip.prefab has no ToolTipPanel on its root.");
            var panelSo = new SerializedObject(panel);
            var plainText = (panelSo.FindProperty("_text").objectReferenceValue as TMP_Text)?.transform;
            if (plainText != null)
                plainText.SetParent(null, false);
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            // The root is the 1 px frame: its colour shows round the body, which sits one pixel in.
            var border = root.GetComponent<Image>();
            if (border == null)
                border = root.AddComponent<Image>();
            border.sprite = EditorUiFactory.UiResources().standard;
            border.type = Image.Type.Sliced;
            border.raycastTarget = false;
            EditorUiFactory.GiveColour(border, ColourRole.TipBorder);
            var rootStack = root.GetComponent<VerticalLayoutGroup>();
            if (rootStack == null)
                rootStack = root.AddComponent<VerticalLayoutGroup>();
            Stack(rootStack, padding: new RectOffset(1, 1, 1, 1), spacing: 0f);

            var frame = EditorUiFactory.MakeFrame("Frame", root.transform, Vector2.zero, Vector2.one, ColourRole.TipBackground);
            frame.GetComponent<Image>().raycastTarget = false;
            Stack(frame.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(0, 0, 0, 0), 0f);

            // Plain: just words, in the old text box.
            var plain = EditorUiFactory.MakeFrame("Plain", frame, Vector2.zero, Vector2.one);
            // The same padding the panel sizes a plain tip with (its Padding field).
            int pad = Mathf.RoundToInt(panelSo.FindProperty("_padding").floatValue);
            Stack(plain.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(pad, pad, pad, pad), 0f);
            if (plainText != null)
                plainText.SetParent(plain, false);
            var text = plain.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                text = Text("Text", plain, TextRole.Body, ColourRole.TextMain, wrap: true);
            text.raycastTarget = false;

            // Layout: the new shape.
            var layout = EditorUiFactory.MakeFrame("Layout", frame, Vector2.zero, Vector2.one);
            Stack(layout.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(0, 0, 0, 0), 0f);
            var view = layout.gameObject.AddComponent<TipLayoutView>();

            var header = EditorUiFactory.MakeFrame("Header", layout, Vector2.zero, Vector2.one);
            Stack(header.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(16, 16, 12, 10), 2f);
            var kicker = Text("Kicker", header, TextRole.Small, ColourRole.TextSecondary, wrap: false);
            kicker.fontStyle = FontStyles.UpperCase;
            kicker.characterSpacing = 4f;
            var titleLine = EditorUiFactory.MakeFrame("TitleLine", header, Vector2.zero, Vector2.one);
            var titleRow = titleLine.gameObject.AddComponent<HorizontalLayoutGroup>();
            Stack(titleRow, new RectOffset(0, 0, 0, 0), 8f);
            var title = Text("Title", titleLine, TextRole.Heading, ColourRole.TextMain, wrap: true);
            TakeTheRest(title);
            var titleValue = Text("TitleValue", titleLine, TextRole.Heading, ColourRole.TipTime, wrap: false);
            titleValue.alignment = TextAlignmentOptions.TopRight;

            var divider = EditorUiFactory.MakeFrame("Divider", layout, Vector2.zero, Vector2.one, ColourRole.TipBorder);
            divider.GetComponent<Image>().raycastTarget = false;
            EditorUiFactory.FixHeight(divider, 1f);

            var rows = EditorUiFactory.MakeFrame("Rows", layout, Vector2.zero, Vector2.one);
            Stack(rows.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(16, 16, 12, 12), 10f);
            var rowTemplate = BuildRow(rows);
            rowTemplate.gameObject.SetActive(false);
            var description = Text("Description", rows, TextRole.Small, ColourRole.TextSecondary, wrap: true);

            var footer = EditorUiFactory.MakeFrame("Footer", layout, Vector2.zero, Vector2.one, ColourRole.TipFooter);
            footer.GetComponent<Image>().raycastTarget = false;
            Stack(footer.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(16, 16, 9, 11), 7f);
            var footerLine = EditorUiFactory.MakeFrame("FooterLine", footer, Vector2.zero, Vector2.one);
            Stack(footerLine.gameObject.AddComponent<HorizontalLayoutGroup>(), new RectOffset(0, 0, 0, 0), 8f);
            var footerText = Text("FooterText", footerLine, TextRole.Small, ColourRole.TextMain, wrap: true);
            TakeTheRest(footerText);
            var footerCount = Text("FooterCount", footerLine, TextRole.Small, ColourRole.TextSecondary, wrap: false);
            footerCount.alignment = TextAlignmentOptions.TopRight;
            var bar = EditorUiFactory.MakeFrame("Bar", footer, Vector2.zero, Vector2.one, ColourRole.Track);
            bar.GetComponent<Image>().raycastTarget = false;
            EditorUiFactory.FixHeight(bar, 4f);
            var fill = EditorUiFactory.MakeFrame("Fill", bar, Vector2.zero, Vector2.one, ColourRole.MasteryBar);
            fill.GetComponent<Image>().raycastTarget = false;

            EditorUiFactory.Wire(view,
                ("_colours", colours),
                ("_kickerLine", kicker.gameObject),
                ("_kicker", kicker),
                ("_title", title),
                ("_titleValue", titleValue),
                ("_rows", rows),
                ("_rowTemplate", rowTemplate),
                ("_description", description),
                ("_footer", footer.gameObject),
                ("_footerText", footerText),
                ("_footerCount", footerCount),
                ("_bar", bar.gameObject),
                ("_barFill", fill));
            EditorUiFactory.Wire(panel, ("_text", text), ("_plainRoot", plain.gameObject), ("_view", view));
        }

        // A fact row: label | body (value, chips, rule) | note on the right.
        private static TipRowView BuildRow(Transform parent)
        {
            var row = EditorUiFactory.MakeFrame("RowTemplate", parent, Vector2.zero, Vector2.one);
            var line = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            Stack(line, new RectOffset(0, 0, 0, 0), 10f);
            line.childForceExpandWidth = false;
            var view = row.gameObject.AddComponent<TipRowView>();

            var label = Text("Label", row, TextRole.Small, ColourRole.TextSecondary, wrap: true);
            var labelSize = label.gameObject.AddComponent<LayoutElement>();
            labelSize.minWidth = labelSize.preferredWidth = 68f;
            labelSize.flexibleWidth = 0f;

            var body = EditorUiFactory.MakeFrame("Body", row, Vector2.zero, Vector2.one);
            Stack(body.gameObject.AddComponent<VerticalLayoutGroup>(), new RectOffset(0, 0, 0, 0), 4f);
            var bodySize = body.gameObject.AddComponent<LayoutElement>();
            bodySize.minWidth = 0f;
            bodySize.preferredWidth = 0f;
            bodySize.flexibleWidth = 1f;
            var value = Text("Value", body, TextRole.Body, ColourRole.TextMain, wrap: true);

            var chips = EditorUiFactory.MakeFrame("Chips", body, Vector2.zero, Vector2.one);
            var flow = chips.gameObject.AddComponent<FlowLayout>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("_spacing").floatValue = 6f;
            flowSo.ApplyModifiedProperties();
            var chipTemplate = EditorUiFactory.MakeChip<Image>(chips, "ChipTemplate", ColourRole.ChipBackgroundFaint, false);
            chipTemplate.gameObject.SetActive(false);

            var rule = Text("Rule", body, TextRole.Small, ColourRole.TextSecondary, wrap: true);

            var right = Text("Right", row, TextRole.Small, ColourRole.TextSecondary, wrap: true);
            right.alignment = TextAlignmentOptions.TopRight;
            var rightSize = right.gameObject.AddComponent<LayoutElement>();
            rightSize.flexibleWidth = 0f;

            EditorUiFactory.Wire(view,
                ("_label", label),
                ("_value", value),
                ("_rule", rule),
                ("_right", right),
                ("_rightSize", rightSize),
                ("_chips", chips),
                ("_chipTemplate", chipTemplate.gameObject));
            return view;
        }

        // Wrapping text in a row would otherwise ask for its whole one-line width and push its neighbours out:
        // it takes only what the others leave, and wraps.
        private static void TakeTheRest(TMP_Text text)
        {
            var size = text.gameObject.AddComponent<LayoutElement>();
            size.minWidth = 0f;
            size.preferredWidth = 0f;
            size.flexibleWidth = 1f;
        }

        // A text that never catches the mouse (the panel mustn't, or it would flicker).
        private static TMP_Text Text(string name, Transform parent, TextRole role, ColourRole colour, bool wrap)
        {
            var text = EditorUiFactory.MakeText(name, parent, role, 0f, colour);
            text.raycastTarget = false;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.TopLeft;
            return text;
        }

        // Children take their own preferred size, top to bottom (or left to right), the full width.
        private static void Stack(HorizontalOrVerticalLayoutGroup group, RectOffset padding, float spacing)
        {
            group.padding = padding;
            group.spacing = spacing;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = group is VerticalLayoutGroup;
            group.childForceExpandHeight = false;
        }
    }
}
