using System;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One milestone card in the Story box: its title, when this run reached it (against last run),
    /// the story's opening paragraph, and Read more for the whole passage. Its look is the
    /// MilestoneRow prefab.
    /// </summary>
    public class MilestoneRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _timesLabel;
        [SerializeField] private TMP_Text _paragraphLabel;
        [SerializeField] private Button _readMoreButton;

        private Action _onReadMore;

        private void Awake()
        {
            _readMoreButton.onClick.AddListener(() => _onReadMore?.Invoke());
        }

        /// <param name="hasStory">False for a room with no first-entry passage: nothing to read, so no Read more.</param>
        public void Setup(Action onReadMore, Func<string> timesTip, bool hasStory = true)
        {
            _onReadMore = onReadMore;
            _readMoreButton.gameObject.SetActive(hasStory);
            SetReadMoreCaption();
            ToolTip.On(_timesLabel, timesTip);
            ToolTip.On(_readMoreButton, () => GameText.Get("story.read_more_tip"));
        }

        // Enabled and disabled, not Awake and OnDestroy: a card made while its box is closed never runs those.
        private void OnEnable()
        {
            GameText.Changed += SetReadMoreCaption;
            SetReadMoreCaption();
        }

        private void OnDisable() => GameText.Changed -= SetReadMoreCaption;

        // Set again when the wording is reloaded (the other texts here are rebuilt by Refresh each time).
        private void SetReadMoreCaption() => UiText.SetCaption(_readMoreButton, GameText.Get("story.read_more"));

        public void Refresh(string title, string times, string paragraph)
        {
            UiText.Set(_titleLabel, title);
            UiText.Set(_timesLabel, times);
            UiText.Set(_paragraphLabel, paragraph);
        }
    }
}
