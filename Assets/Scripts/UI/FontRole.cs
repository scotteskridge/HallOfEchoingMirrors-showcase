using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Tags a text with its role (heading, story, small print…). Its font and size come from the
    /// UiFonts asset (Assets/Data/UI): after changing either, run *Hall of Echoing Mirrors → UI →
    /// Apply UI Fonts*. Copies made at runtime keep the tag.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class FontRole : MonoBehaviour
    {
        [Tooltip("What kind of text this is. Its font and size are set in the UI Fonts asset (Assets/Data/UI/UiFonts).")]
        [SerializeField] private TextRole _role;
        [Tooltip("This text's size as a share of its role's (1 = the role's size), for the odd text that needs to be " +
                 "smaller or bigger, like room names on the map.")]
        [SerializeField, Range(0.5f, 2f)] private float _scale = 1f;

        public TextRole Role => _role;
        public float Scale => _scale;
    }
}
