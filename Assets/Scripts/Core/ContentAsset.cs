using System;
using UnityEngine;

namespace HallOfEchoingMirrors.Core
{
    /// <summary>
    /// The parent of every piece of game content (tasks, switches, story beats, resources).
    /// Gives each one a permanent ID that never changes, even if the asset is renamed or moved,
    /// so save files can refer to content safely.
    /// </summary>
    public abstract class ContentAsset : ScriptableObject
    {
        [Tooltip("Permanent ID used by save files. Set automatically; never edit it.")]
        [SerializeField] private string _id;

        /// <summary>Permanent ID for save files. Matches the asset's Unity GUID.</summary>
        public string Id
        {
            get
            {
                // Content made in code (e.g. in tests) has no asset file, so give it a one-off ID.
                if (string.IsNullOrEmpty(_id))
                    _id = Guid.NewGuid().ToString("N");
                return _id;
            }
        }

        protected virtual void OnValidate() => StampId();

        /// <summary>
        /// Editor only: copies the asset file's GUID into the ID. Unity gives every asset file a
        /// unique GUID that survives renames and moves, and a duplicated asset gets a new one, so
        /// IDs stay unique automatically.
        /// </summary>
        public void StampId()
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path))
                return;
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid) && guid != _id)
            {
                _id = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
