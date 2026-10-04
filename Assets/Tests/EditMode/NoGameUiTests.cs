using System.Reflection;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>Screens that poll the simulation in Update must stay quiet when GameController failed to start.</summary>
    public class NoGameUiTests
    {
        private GameObject _host;
        private GameObject _panelObject;
        private GameController _game;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("NoGameUiTest");
            // Added in edit mode with nothing assigned: Awake doesn't run, so no simulation is made.
            _game = _host.AddComponent<GameController>();
            Assume.That(_game.Simulation, Is.Null);
            _panelObject = new GameObject("Panel");
            _panelObject.transform.SetParent(_host.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        // Reflection reaches the private Inspector fields; a missing name fails with a clear message.
        private static void Set(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, $"{target.GetType().Name}.{field} not found");
            info.SetValue(target, value);
        }

        private static void RunUpdate(object target)
        {
            var info = target.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, $"{target.GetType().Name}.Update not found");
            info.Invoke(target, null);
        }

        [Test]
        public void RunResultsPanel_WithNoSimulation_HidesPanelAndDoesNotThrow()
        {
            var panel = _host.AddComponent<RunResultsPanel>();
            Set(panel, "_game", _game);
            Set(panel, "_panel", _panelObject);

            Assert.DoesNotThrow(() => RunUpdate(panel));
            Assert.That(_panelObject.activeSelf, Is.False);
        }

        [Test]
        public void StoryPopup_WithNoSimulation_LeavesPanelClosedAndDoesNotThrow()
        {
            var popup = _host.AddComponent<StoryPopup>();
            Set(popup, "_game", _game);
            Set(popup, "_panel", _panelObject);
            _panelObject.SetActive(false);

            Assert.DoesNotThrow(() => RunUpdate(popup));
            Assert.That(_panelObject.activeSelf, Is.False);
        }
    }
}
