using System;
using System.Collections.Generic;
using System.Linq;
using HallOfEchoingMirrors.EditorTools;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The UI's colour roles (plan 012): every role set, every tagged Graphic in step, every tagged button derived.</summary>
    public class UiColoursTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static UiColours Colours()
        {
            var colours = UiTools.LoadUiColours();
            Assert.That(colours, Is.Not.Null, $"{UiTools.UiColoursPath} is missing: run Setup Step 90.");
            return colours;
        }

        [Test]
        public void Colours_EveryRole_HasExactlyOneColour()
        {
            var colours = Colours();
            foreach (ColourRole role in Enum.GetValues(typeof(ColourRole)))
            {
                var entry = colours.Find(role);
                Assert.That(entry, Is.Not.Null, $"{role} has no entry in UiColours");
                Assert.That(colours.Colours.Count(e => e.role == role), Is.EqualTo(1), $"{role} is in UiColours more than once");
            }
        }

        [Test]
        public void TaggedGraphics_EachUsesItsRoleColour()
        {
            var colours = Colours();
            var problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var graphic in prefab.GetComponentsInChildren<Graphic>(true))
                    CheckColour(graphic, colours, problems);
            }

            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                        CheckColour(graphic, colours, problems);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static void CheckColour(Graphic graphic, UiColours colours, List<string> problems)
        {
            var tag = graphic.GetComponent<ColourRoleTag>();
            if (tag == null)
                return;
            if (graphic.color != colours.For(tag.Role))
                problems.Add($"{UiTools.PathOf(graphic.transform)} is {tag.Role} but doesn't match UiColours: " +
                             "run Hall of Echoing Mirrors → UI → Apply UI Colours");
        }

        [Test]
        public void TaggedButtons_HaveTheDerivedColorBlock()
        {
            var problems = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorUiFactory.PrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var button in prefab.GetComponentsInChildren<Selectable>(true))
                    CheckDerived(button, problems);
            }

            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var button in root.GetComponentsInChildren<Selectable>(true))
                        CheckDerived(button, problems);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static void CheckDerived(Selectable button, List<string> problems)
        {
            if (button.targetGraphic == null || button.targetGraphic.GetComponent<ColourRoleTag>() == null)
                return;
            // Apply UI Colours only derives a ColorBlock when the Selectable sits on the same GameObject as its
            // targetGraphic (Step 88's own limitation): a Scrollbar's Selectable is on its root, not its Handle.
            if (button.gameObject != button.targetGraphic.gameObject)
                return;
            if (button.colors.Equals(ColorBlock.defaultColorBlock))
                problems.Add($"{UiTools.PathOf(button.transform)} is tagged but still has the default ColorBlock: " +
                             "run Hall of Echoing Mirrors → UI → Apply UI Colours");
        }
    }
}
