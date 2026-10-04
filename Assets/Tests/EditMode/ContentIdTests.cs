using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace HallOfEchoingMirrors.Tests
{
    /// <summary>
    /// Every content asset in the game must have its permanent ID saved in its file (its Unity
    /// GUID), or a built game would give it a new one each time and saves would lose track of it.
    /// </summary>
    public class ContentIdTests
    {
        [Test]
        public void EveryContentAsset_HasItsIdSavedInItsFile()
        {
            // The file on disk, not the loaded asset: the editor fills the ID in memory on load,
            // which would hide a file that never had it saved.
            var missing = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:ContentAsset", new[] { "Assets/Data" });
            Assert.That(guids, Is.Not.Empty, "found no content assets in Assets/Data, so there is nothing to check");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!File.ReadAllText(path).Contains("_id: " + guid))
                    missing.Add(path);
            }

            Assert.That(missing, Is.Empty,
                "These have no saved ID: run Hall of Echoing Mirrors → Tools → Stamp Content IDs.");
        }
    }
}
