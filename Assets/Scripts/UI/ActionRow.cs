using System;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One action on offer (a task, a trip, or exploring): its name, a short line of details, and
    /// its buttons: Play (to the top of the queue, now), Schedule (to the bottom), and, for actions
    /// that make things she carries (not one-time ones), Carry (on top where she is, low down elsewhere,
    /// only until her pockets and containers are full). Icons at its start show the skill it uses and the stat it trains. Hovering the row
    /// explains the action in full. Its look is the ActionRow prefab.
    /// </summary>
    public class ActionRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _detailsLabel;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _scheduleButton;
        [Tooltip("Optional: shown only for actions that gather things she carries.")]
        [SerializeField] private Button _carryButton;
        [Tooltip("At the far left: the skill that speeds this action up (blank if none).")]
        [SerializeField] private TraitIcon _skillIcon;
        [Tooltip("Beside the skill's icon: the stat this action trains (blank if none).")]
        [SerializeField] private TraitIcon _statIcon;

        private Action _onPlay, _onSchedule, _onCarry;
        private CanvasGroup _group; // dims the whole row when it can't be done

        // Found when first needed, not in Awake: a row inside a section that's switched off (a
        // popover's empty floor) is refreshed before Unity has run its Awake.
        private CanvasGroup Group
        {
            get
            {
                if (_group == null)
                {
                    _group = GetComponent<CanvasGroup>();
                    if (_group == null)
                        _group = gameObject.AddComponent<CanvasGroup>();
                }
                return _group;
            }
        }

        private void Awake()
        {
            // Hooked up once; Setup only swaps what they do, so reused rows never gain extra listeners.
            _playButton.onClick.AddListener(() => _onPlay?.Invoke());
            _scheduleButton.onClick.AddListener(() => _onSchedule?.Invoke());
            if (_carryButton != null)
                _carryButton.onClick.AddListener(() => _onCarry?.Invoke());
        }

        /// <param name="onCarry">Null for actions Carry doesn't suit (the button is hidden).</param>
        public void Setup(string name, Action onPlay, Action onSchedule, Func<TipLayout> tip,
            Func<string> playTip, Func<string> scheduleTip, Action onCarry = null, Func<string> carryTip = null)
        {
            UiText.Set(_nameLabel, name);
            // Here rather than in Awake: by now the text file is loaded (and a reload rebuilds the rows).
            UiText.SetCaption(_playButton, GameText.Get("actions.play_button"));
            UiText.SetCaption(_scheduleButton, GameText.Get("actions.schedule_button"));
            _onPlay = onPlay;
            _onSchedule = onSchedule;
            ToolTip.On(this, tip);
            ToolTip.On(_playButton, playTip);
            ToolTip.On(_scheduleButton, scheduleTip);

            _onCarry = onCarry;
            if (_carryButton != null)
            {
                UiText.SetActive(_carryButton, onCarry != null);
                UiText.SetCaption(_carryButton, GameText.Get("actions.carry_button"));
                ToolTip.On(_carryButton, carryTip);
            }
        }

        /// <summary>
        /// The icons at the row's start: its skill, then the stat it trains (dimmer), each with a short
        /// pop-up of its own. A null icon leaves its space blank, so names stay lined up.
        /// </summary>
        public void ShowTraits(Sprite skill, Func<string> skillTip, Sprite stat, Func<string> statTip)
        {
            if (_skillIcon == null || _statIcon == null)
                throw new InvalidOperationException($"ActionRow {name}: no skill or stat icon set. Run the setup step for the icons.");
            _skillIcon.Show(skill, skillTip);
            _statIcon.Show(stat, statTip, UiStyle.StatIconOnRow);
        }

        /// <summary>The live part: details (time at her current speed) and whether Play can be used here.</summary>
        public void Refresh(string details, bool canPlay)
        {
            UiText.Set(_detailsLabel, details);
            SetInteractable(_playButton, canPlay);
        }

        /// <summary>
        /// As Refresh, for a room's popover: Play, Schedule and Carry are each greyed on their own, and a row that can't be done
        /// at all (<paramref name="blocked"/>) is dimmed with every button off. The reason, if any,
        /// replaces the details.
        /// </summary>
        public void Refresh(string details, bool canPlay, bool canSchedule, string reason, bool blocked, bool canCarry)
        {
            // Blocked: the reason is all there is to say. Otherwise (a need missing) the times still matter.
            UiText.Set(_detailsLabel,
                reason == null ? details :
                blocked ? UiStyle.Colour(reason, UiStyle.Muted) :
                details + " · " + UiStyle.Colour(reason, UiStyle.Warning));
            SetInteractable(_playButton, canPlay && !blocked);
            SetInteractable(_scheduleButton, canSchedule && !blocked);
            // Carry works wherever Play and Schedule do: now, on top, where she is; queued low down elsewhere.
            if (_carryButton != null)
                SetInteractable(_carryButton, canCarry && !blocked);
            float alpha = blocked ? UiStyle.UnavailableAlpha : 1f;
            if (Group.alpha != alpha)
                Group.alpha = alpha;
        }

        private static void SetInteractable(Button button, bool on)
        {
            if (button.interactable != on)
                button.interactable = on;
        }
    }
}
