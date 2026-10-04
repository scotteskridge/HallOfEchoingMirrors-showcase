using UnityEditor;

namespace HallOfEchoingMirrors.Tests.Balance
{
    /// <summary>
    /// Every number, true/false and choice on the balance assets (Loop Settings, tasks, items, places,
    /// switches, skills, cost families) as one table: folder, asset, field path, value. Field paths are the
    /// ones scenarios.txt uses. Saved with each pass, so two passes can be compared side by side.
    /// </summary>
    public static class BalanceSnapshot
    {
        private static readonly string[] Folders =
        {
            "Assets/Data/LoopSettings.asset", "Assets/Data/Tasks", "Assets/Data/Items", "Assets/Data/Places",
            "Assets/Data/Switches", "Assets/Data/Skills", "Assets/Data/CostFamilies",
        };

        public static Csv Take()
        {
            var csv = new Csv("folder", "asset", "field", "value");
            foreach (var place in Folders)
            {
                if (place.EndsWith(".asset"))
                {
                    AddAsset(csv, place);
                    continue;
                }
                foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { place }))
                    AddAsset(csv, AssetDatabase.GUIDToAssetPath(guid));
            }
            return csv;
        }

        private static void AddAsset(Csv csv, string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
                return;
            string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var field = new SerializedObject(asset).GetIterator();
            bool enter = true;
            for (bool more = field.Next(enter); more; more = field.Next(enter))
            {
                // Never step inside Unity's own bookkeeping (m_…) or text, which is stored letter by letter.
                bool skip = field.name.StartsWith("m_") || field.propertyType == SerializedPropertyType.String;
                enter = !skip;
                if (skip)
                    continue;
                object value = field.propertyType switch
                {
                    SerializedPropertyType.Float => field.floatValue,
                    SerializedPropertyType.Integer when !field.isArray && field.name != "size" => field.intValue,
                    SerializedPropertyType.Boolean => field.boolValue,
                    SerializedPropertyType.Enum => field.enumValueIndex >= 0 && field.enumValueIndex < field.enumDisplayNames.Length
                        ? field.enumDisplayNames[field.enumValueIndex] : field.enumValueIndex.ToString(),
                    _ => null,
                };
                if (value != null)
                    csv.Row(folder, asset.name, field.propertyPath, value);
            }
        }
    }
}
