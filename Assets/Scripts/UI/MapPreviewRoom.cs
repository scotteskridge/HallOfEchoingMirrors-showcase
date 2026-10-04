using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Marks a room in the editor's map layout preview with the room it stands for, so dragging it
    /// can move that room and selecting it can show the room's settings. Only ever on preview
    /// objects, which are never saved into the scene.
    /// </summary>
    public class MapPreviewRoom : MonoBehaviour
    {
        public NodeDefinition room;
    }
}
