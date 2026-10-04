using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The main page's top bar: Clara's vitality (a large bar with "current / max"), how fast it's
    /// draining, the run's clock (game time, so pauses don't count), and the run controls: Pause,
    /// the speed tiers, and End run. Every part explains itself when hovered. (Between runs the planning
    /// screen's Begin, or the Summary's Repeat, starts the next one.)
    /// </summary>
    public class RunHeader : MonoBehaviour
    {
        [SerializeField] private GameController _game;

        [Header("Vitality")]
        [SerializeField] private Slider _vitalityBar;
        [SerializeField] private TMP_Text _vitalityLabel;
        [SerializeField] private TMP_Text _drainLabel;
        [SerializeField] private TMP_Text _clockLabel;

        [Header("Controls")]
        [SerializeField] private Button _pauseButton;
        [Tooltip("The row holding the speed buttons: hidden until a speed above ×1 is earned.")]
        [SerializeField] private RectTransform _speedTierRow;
        [Tooltip("Copied once per speed shown (see SpeedTiers): the earned speeds, then the next one greyed out.")]
        [SerializeField] private Button _speedTierTemplate;
        [SerializeField] private Button _endRunButton;
        [Tooltip("The speeds offered as buttons, slowest first, once earned: each needs something held with that " +
                 "Unlocks Speed (or faster). Until one above ×1 is earned, the buttons are hidden.")]
        [SerializeField] private float[] _speeds = { 1f, 2f };
        [Tooltip("Ending a run throws the rest of it away, so the first click only asks. Seconds to confirm.")]
        [SerializeField, Min(0.5f)] private float _confirmSeconds = 3f;

        private TMP_Text _pauseLabel, _endRunLabel;
        private float _confirmUntil = -1f;

        private TemplateList<Button> _tierButtons;
        private readonly List<SpeedTiers.Tier> _tiers = new List<SpeedTiers.Tier>();
        private Color _tierColour; // the template's own colour: a tier that's neither current nor locked

        private Simulation Sim => _game.Simulation;
        private bool Running => Sim != null && Sim.RunUnderWay;

        private void Start()
        {
            _pauseLabel = _pauseButton.GetComponentInChildren<TMP_Text>();
            _endRunLabel = _endRunButton.GetComponentInChildren<TMP_Text>();

            // Lambdas look the game up on every click, so they still work after loading another.
            _pauseButton.onClick.AddListener(() => { if (_game.IsPaused) _game.Resume(); else _game.Pause(); });
            _endRunButton.onClick.AddListener(OnEndRunClicked);

            ToolTip.On(_vitalityBar, VitalityTip);
            ToolTip.On(_drainLabel, DrainTip);
            ToolTip.On(_clockLabel, () => GameText.Get("tips.clock") + "\n" + GameText.Get("tips.clock_moves", ("count", Sim.MovesThisRun)));
            ToolTip.On(_endRunButton, () => GameText.Get("tips.end_run"));
            ToolTip.On(_pauseButton, () => _game.IsPaused && _game.PauseReason != null ? _game.PauseReason : GameText.Get("tips.pause"));

            _tierColour = _speedTierTemplate.image.color;
            _tierButtons = new TemplateList<Button>(_speedTierTemplate, (button, index) =>
            {
                button.onClick.AddListener(() => ChooseTier(index));
                ToolTip.On(button, () => TierTip(index));
            });
        }

        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;

            var vitality = sim.Loop.Vitality;
            _vitalityBar.value = vitality.Fraction;
            UiText.Set(_vitalityLabel, GameText.Get("main.vitality", ("name", vitality.Name),
                ("current", UiText.Whole(vitality.Current)), ("max", UiText.Whole(vitality.Max))));
            UiText.Set(_drainLabel, GameText.Get("main.drain", ("rate", UiText.Rate(sim.VitalityDrainPerSecond))));
            int moves = Running ? sim.MovesThisRun : 0; // between runs the clock shows no moves
            UiText.Set(_clockLabel, GameText.Get("main.clock", ("time", UiText.Clock(sim.SecondsThisRun)))
                + (moves > 0 ? " " + GameText.Get("main.moves", ("count", moves)) : ""));

            bool running = Running;
            UiText.SetActive(_pauseButton, running);
            SpeedTiers.Fill(_speeds, sim.FastestSpeedUnlocked, _tiers);
            bool showTiers = running && _tiers.Count > 0;
            UiText.SetActive(_speedTierRow, showTiers);
            if (showTiers)
                ShowTiers();
            UiText.SetActive(_endRunButton, running);

            if (!running)
                _confirmUntil = -1f;
            UiText.Set(_pauseLabel, GameText.Get(_game.IsPaused ? "run.resume" : "run.pause"));
            UiText.Set(_endRunLabel, GameText.Get(Time.unscaledTime < _confirmUntil ? "common.sure" : "run.end_run"));
        }

        /// <summary>One button per tier: the current speed highlighted, the locked one greyed out.</summary>
        private void ShowTiers()
        {
            _tierButtons.Show(_tiers.Count);
            float current = _game.TickEngine.Speed;
            for (int i = 0; i < _tiers.Count; i++)
            {
                var tier = _tiers[i];
                var button = _tierButtons[i];
                UiText.SetCaption(button, GameText.Get("main.speed", ("speed", UiText.Number(tier.Speed))));
                if (button.interactable == tier.Locked)
                    button.interactable = !tier.Locked;

                // A dev-panel speed that isn't in the list highlights none of them.
                bool isCurrent = !tier.Locked && Mathf.Abs(tier.Speed - current) < 0.001f;
                button.image.color = tier.Locked ? UiStyle.SpeedTierLocked
                    : isCurrent ? UiStyle.SpeedTierCurrent
                    : _tierColour;
            }
        }

        private void ChooseTier(int index)
        {
            if (index < _tiers.Count && !_tiers[index].Locked)
                _game.TickEngine.Speed = _tiers[index].Speed;
        }

        private string TierTip(int index)
        {
            if (index >= _tiers.Count)
                return null;
            var tier = _tiers[index];
            return tier.Locked
                ? GameText.Get("tips.speed_locked", ("speed", UiText.Number(tier.Speed)))
                : GameText.Get("tips.speed");
        }

        private void OnEndRunClicked()
        {
            if (Time.unscaledTime < _confirmUntil)
            {
                _confirmUntil = -1f;
                _game.EndRunEarly();
            }
            else
            {
                _confirmUntil = Time.unscaledTime + _confirmSeconds;
            }
        }

        private string VitalityTip()
        {
            var sim = Sim;
            var vitality = sim.Loop.Vitality;
            string tip = GameText.Get("tips.vitality", ("name", vitality.Name),
                ("current", UiText.Number(vitality.Current)), ("max", UiText.Whole(vitality.Max)));
            if (sim.RestoringPerSecond > 0f)
                tip += "\n" + UiStyle.Colour(GameText.Get("tips.restoring",
                    ("rate", UiText.Number(sim.RestoringPerSecond)), ("left", UiText.Number(sim.RestoringStillToCome))), UiStyle.Levelling);
            return tip;
        }

        private string DrainTip()
        {
            var sim = Sim;
            var settings = sim.Settings;
            return GameText.Get("tips.drain",
                ("rate", UiText.Rate(sim.VitalityDrainPerSecond)),
                ("base", UiText.Setting(settings.vitalityDrainPerSecond)),
                ("growth", UiText.Whole(settings.drainGrowthPerMinute * 100f)),
                ("grown", UiText.Rate(sim.DrainGrowthMultiplier)),
                ("composure", UiText.Rate(sim.ComposureGrowthMultiplier)))
                + (sim.DrainHeldOffNow < 1f
                    ? "\n" + GameText.Get("tips.drain_held_off", ("times", UiText.Rate(sim.DrainHeldOffNow)))
                    : "")
                + (sim.ExtraDrainNow > 0f
                    ? "\n" + GameText.Get("tips.drain_extra",
                        ("action", sim.CurrentActionName), ("rate", UiText.Rate(sim.ExtraDrainNow)))
                    : "");
        }
    }
}
