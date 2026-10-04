using System.Collections.Generic;
using HallOfEchoingMirrors.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// A RectTransform records only one layout driver. A child with its own ContentSizeFitter inside a
    /// layout group has two, so Unity saves the group's values for it as real numbers that change from save
    /// to save (the SampleScene save churn, fixed in commit 635888d). The parent stack should size it instead.
    /// </summary>
    public class LayoutDriverTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void NoContentSizeFitter_InsideALayoutGroup()
        {
            var problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab, path, problems);
            }

            // The scene as it is on disk (or as it's open, if it is).
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    Check(root, ScenePath, problems);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Assert.That(problems, Is.Empty,
                "These have a Content Size Fitter inside a layout group: remove the fitter and let the parent " +
                "group control its children's size instead.\n" + string.Join("\n", problems));
        }

        private static void Check(GameObject root, string where, List<string> problems)
        {
            foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true))
            {
                var parent = fitter.transform.parent;
                if (parent == null || parent.GetComponent<LayoutGroup>() == null)
                    continue;
                // A child the group skips has only the one driver.
                var element = fitter.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                    continue;
                problems.Add($"{where}: {PathOf(fitter.transform)}");
            }
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (var p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
