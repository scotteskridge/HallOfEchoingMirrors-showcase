using HallOfEchoingMirrors.Core;
using UnityEditor;
using UnityEngine;

namespace HallOfEchoingMirrors.EditorTools
{
    /// <summary>
    /// Makes sure every content asset (task, switch, skill, story...) has its permanent ID saved in
    /// its file: when Unity starts, and whenever content is created or imported. Without the saved
    /// ID a built game would give the asset a new one each time, and saves would lose track of it.
    /// </summary>
    [InitializeOnLoad]
    public class ContentIds : AssetPostprocessor
    {
        static ContentIds()
        {
            // After the editor has finished loading, not during it.
            EditorApplication.delayCall += StampAll;
        }

        [MenuItem("Hall of Echoing Mirrors/Tools/Stamp Content IDs")]
        public static void StampAll()
        {
            int stamped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ContentAsset"))
                if (Stamp(AssetDatabase.LoadAssetAtPath<ContentAsset>(AssetDatabase.GUIDToAssetPath(guid))))
                    stamped++;
            if (stamped > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Content IDs: saved the permanent ID of {stamped} content asset(s).");
            }
        }

        // Newly created or imported content gets its ID straight away.
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool any = false;
            foreach (string path in imported)
                if (path.EndsWith(".asset") && Stamp(AssetDatabase.LoadAssetAtPath<ContentAsset>(path)))
                    any = true;
            if (any)
                AssetDatabase.SaveAssets(); // re-imports them once; they're stamped by then, so it stops there
        }

        /// <summary>
        /// Makes sure the asset's file holds its GUID as its ID. True if it had to be written. (The
        /// file is checked, not the loaded asset: the editor fills the ID in memory on load, which
        /// would hide a file that never had it saved.)
        /// </summary>
        private static bool Stamp(ContentAsset asset)
        {
            if (asset == null)
                return false;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || System.IO.File.ReadAllText(path).Contains("_id: " + guid))
                return false;
            asset.StampId();
            EditorUtility.SetDirty(asset); // even if the ID was already right in memory, the file needs it
            return true;
        }
    }
}
