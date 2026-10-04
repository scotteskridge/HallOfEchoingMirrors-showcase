using System;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>The map's look: every kind of item on the floor has a dot colour.</summary>
    public class MapStyleTests
    {
        [Test]
        public void EveryKind_HasAColour()
        {
            var style = ScriptableObject.CreateInstance<MapStyle>();
            try
            {
                foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind)))
                    Assert.DoesNotThrow(() => style.KindColour(kind), $"give {kind} a colour in MapStyle.KindColour");
                Assert.Throws<ArgumentOutOfRangeException>(() => style.KindColour((ItemKind)999), "an unknown kind fails loudly");
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }

        [Test]
        public void Backdrop_HasValidDefaults()
        {
            var style = ScriptableObject.CreateInstance<MapStyle>();
            try
            {
                Assert.That(style.vignetteStrength, Is.InRange(0f, 1f));
                Assert.That(style.backdropTint.a, Is.GreaterThan(0f), "an invisible backdrop would be a mistake");
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }
    }
}
