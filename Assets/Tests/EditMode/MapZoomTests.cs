using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>How the mouse wheel zooms the map.</summary>
    public class MapZoomTests
    {
        private MapStyle _style;

        [SetUp]
        public void SetUp()
        {
            _style = ScriptableObject.CreateInstance<MapStyle>();
            _style.minZoom = 0.5f;
            _style.maxZoom = 2f;
            _style.zoomStep = 0.25f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_style);

        [Test]
        public void ZoomAfterScroll_Up_ZoomsInOneStep()
        {
            Assert.AreEqual(1.25f, MapWindow.ZoomAfterScroll(1f, 3f, _style), 1e-5f);
        }

        [Test]
        public void ZoomAfterScroll_Down_ZoomsOutOneStep()
        {
            Assert.AreEqual(1f / 1.25f, MapWindow.ZoomAfterScroll(1f, -0.1f, _style), 1e-5f);
        }

        [Test]
        public void ZoomAfterScroll_Sideways_LeavesZoomAlone()
        {
            // A horizontal trackpad swipe arrives as a scroll with no vertical part.
            Assert.AreEqual(1f, MapWindow.ZoomAfterScroll(1f, 0f, _style));
        }

        [Test]
        public void ZoomAfterScroll_NearTheLimits_StaysWithinTheStyleLimits()
        {
            Assert.AreEqual(2f, MapWindow.ZoomAfterScroll(1.9f, 1f, _style), 1e-5f);
            Assert.AreEqual(0.5f, MapWindow.ZoomAfterScroll(0.55f, -1f, _style), 1e-5f);
        }
    }
}
