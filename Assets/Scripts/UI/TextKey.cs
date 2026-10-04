using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Makes a label in the scene (a button caption, a heading) show a line from the game text
    /// file instead of text typed into the scene, so every word is edited in one place. Set up by
    /// Hall of Echoing Mirrors → Text → Scene Labels. In the editor the label keeps showing its
    /// last text; in the game it shows the file's, and follows the file when it's saved.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TextKey : MonoBehaviour
    {
        [Tooltip("The line in Assets/Text/game_text.txt, e.g. scene.results_plan_next.")]
        [SerializeField] private string _key;

        public string Key => _key;

        /// <summary>Switches to another line of the text file, e.g. a button whose caption depends on what she has earned.</summary>
        public void SetKey(string key)
        {
            if (_key == key)
                return;
            _key = key;
            Apply();
        }

        private void OnEnable()
        {
            GameText.Changed += Apply;
            Apply();
        }

        private void OnDisable() => GameText.Changed -= Apply;

        private void Apply()
        {
            // Before the file is read (very early in start-up), keep the scene's text: Changed
            // fires as soon as it's loaded.
            if (!GameText.IsLoaded || string.IsNullOrEmpty(_key))
                return;
            GetComponent<TMP_Text>().text = GameText.Get(_key);
        }
    }
}
