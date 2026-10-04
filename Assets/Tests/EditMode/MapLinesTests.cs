using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>How a way between two rooms is drawn: one stretched, turned image.</summary>
    public class MapLinesTests
    {
        private GameObject _line;

        [SetUp]
        public void SetUp() => _line = new GameObject("Line", typeof(RectTransform));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_line);

        [Test]
        public void Place_CentresRotatesAndSizes()
        {
            var rect = (RectTransform)_line.transform;

            MapLines.Place(rect, new Vector2(0f, 0f), new Vector2(30f, 40f), 4f);

            Assert.AreEqual(new Vector2(15f, 20f), rect.anchoredPosition);
            Assert.AreEqual(50f, rect.sizeDelta.x, 1e-4f); // a 3-4-5 triangle
            Assert.AreEqual(4f, rect.sizeDelta.y, 1e-4f);
            Assert.AreEqual(Mathf.Atan2(40f, 30f) * Mathf.Rad2Deg, rect.localEulerAngles.z, 1e-3f);
        }

        [Test]
        public void Place_PointingLeft_TurnsHalfWay()
        {
            var rect = (RectTransform)_line.transform;

            MapLines.Place(rect, new Vector2(10f, 0f), new Vector2(-10f, 0f), 2f);

            Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
            Assert.AreEqual(20f, rect.sizeDelta.x, 1e-4f);
            Assert.AreEqual(180f, rect.localEulerAngles.z, 1e-3f);
        }
    }
}
