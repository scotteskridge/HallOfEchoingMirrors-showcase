using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Tags a Graphic with its colour role (panel, row, button face, accent…). Its colour comes from
    /// the UiColours asset (Assets/Data/UI): after changing either, run *Hall of Echoing Mirrors →
    /// UI → Apply UI Colours*. Copies made at runtime keep the tag.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class ColourRoleTag : MonoBehaviour
    {
        [Tooltip("What this colour is for. Its value is set in the UI Colours asset (Assets/Data/UI/UiColours).")]
        [SerializeField] private ColourRole _role;

        public ColourRole Role => _role;
    }
}
