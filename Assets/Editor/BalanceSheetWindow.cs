using System.Collections.Generic;
using System.Text;
using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Every balance number in one window (menu: Hall of Echoing Mirrors → Balance Sheet): the
    /// rules in LoopSettings, then a row per task, switch and resource. The numbers still live in
    /// their own assets; this only shows them side by side and edits them in place.
    /// </summary>
    public partial class BalanceSheetWindow : EditorWindow
    {
        [SerializeField] private LoopSettings _settings;
        [SerializeField] private GameContent _content;
        [SerializeField] private bool _showRules = true;
        [SerializeField] private bool _showTasks = true;
        [SerializeField] private bool _showSwitches = true;
        [SerializeField] private bool _showResources = true;
        [SerializeField] private bool _showSkills = true;
        [SerializeField] private bool _showPlaces = true;
        [SerializeField] private bool _showFamilies = true;
        [SerializeField] private bool _showWays = true;
        [SerializeField] private bool _showFinds = true;
        [SerializeField] private bool _showBlurbs = true;

        private Vector2 _scroll;
        private readonly List<ResourceDefinition> _resources = new List<ResourceDefinition>();
        private readonly StringBuilder _text = new StringBuilder();

        [MenuItem("Hall of Echoing Mirrors/Balance Sheet", false, 0)]
        public static void Open() => GetWindow<BalanceSheetWindow>("Balance Sheet");

        private void OnEnable()
        {
            if (_settings == null)
                _settings = FindFirst<LoopSettings>();
            if (_content == null)
                _content = EditorUiFactory.LoadContent();
            FindResources();
        }

        // New resource assets may have been made while the window was in the background.
        private void OnFocus() => FindResources();

        private static T FindFirst<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }
            return null;
        }

        private void FindResources()
        {
            _resources.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ResourceDefinition)))
            {
                var resource = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (resource != null)
                    _resources.Add(resource);
            }
            _resources.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Every balance number in one place. Edits change the assets directly: Ctrl+Z undoes them, " +
                "and changes made in Play mode are kept afterwards. \"One run of it\" is rough: that one task " +
                "on repeat until the passive drain (with its own Drain × and extra drain), its cost (if charged) and its escalating charge spend her, " +
                "before skills, stats, restoration or anything she holds help.", MessageType.Info);

            _settings = (LoopSettings)EditorGUILayout.ObjectField("Loop Settings", _settings, typeof(LoopSettings), false);
            _content = (GameContent)EditorGUILayout.ObjectField("Game Content", _content, typeof(GameContent), false);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawRules();
            DrawPlaces();
            DrawFamilies();
            DrawWays();
            DrawFinds();
            DrawTasks();
            DrawSwitches();
            DrawResources();
            DrawSkills();
            DrawBlurbs();
            EditorGUILayout.EndScrollView();
        }

        // ---------- Rules ----------

        private void DrawRules()
        {
            if (!Section(ref _showRules, "Rules (Loop Settings)") || _settings == null)
                return;

            // Every field in LoopSettings, as the Inspector would draw it, so new settings appear
            // here without changing this window.
            var so = new SerializedObject(_settings);
            so.Update();
            var property = so.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name != "m_Script")
                    EditorGUILayout.PropertyField(property, true);
            }
            so.ApplyModifiedProperties();
        }
    }
}
