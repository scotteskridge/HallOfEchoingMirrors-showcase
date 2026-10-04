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
using UnityEngine.UI;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Button and chip kinds (plan ui-035): nothing is styled on its own, and every kind is tagged with its roles.</summary>
    public class UiKindsTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private static readonly string[] AllKinds =
            { UiKinds.Button, UiKinds.AccentButton, UiKinds.IconButton, UiKinds.TabButton, UiKinds.SmallButton, UiKinds.Chip, UiKinds.ChipFaint };

        [Test]
        public void EverySceneButtonIsAKind()
        {
            var problems = new List<string>();
            WithScene(scene =>
            {
                foreach (var root in scene.GetRootGameObjects())
                    CheckButtons(root, problems);
            });
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryPrefabButtonIsAKind()
        {
            var problems = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(UiKinds.KindFolder + "/"))
                    continue;
                CheckButtons(AssetDatabase.LoadAssetAtPath<GameObject>(path), problems);
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void ChipTemplatesAreKinds()
        {
            var problems = new List<string>();
            WithScene(scene =>
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tag in root.GetComponentsInChildren<ColourRoleTag>(true))
                        if (UiKinds.ChipKindFor(tag.gameObject) != null && !UiKinds.IsKind(tag.gameObject))
                            problems.Add($"{UiTools.PathOf(tag.transform)} is a chip but not a kind");
            });
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void KindsHaveRoles()
        {
            foreach (string kind in AllKinds)
            {
                var prefab = UiKinds.LoadKind(kind);
                var face = prefab.GetComponent<Image>();
                Assert.That(face, Is.Not.Null, $"{kind} has no face Image");
                Assert.That(face.GetComponent<ColourRoleTag>(), Is.Not.Null, $"{kind}'s face has no Colour Role");
                if (prefab.GetComponent<Button>() == null)
                    continue;
                var label = prefab.GetComponentInChildren<TMP_Text>(true);
                Assert.That(label, Is.Not.Null, $"{kind} has no label");
                Assert.That(label.GetComponent<FontRole>(), Is.Not.Null, $"{kind}'s label has no Font Role");
                Assert.That(label.GetComponent<ColourRoleTag>(), Is.Not.Null, $"{kind}'s label has no Colour Role");
            }
        }

        private static void CheckButtons(GameObject root, List<string> problems)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (UiKinds.ButtonKindFor(button) != null && !UiKinds.IsKind(button.gameObject))
                    problems.Add($"{UiTools.PathOf(button.transform)} is a button but not a kind");
        }

        /// <summary>The scene as it is on disk (or as it's open, if it is).</summary>
        private static void WithScene(System.Action<Scene> check)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                check(scene);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }
}
