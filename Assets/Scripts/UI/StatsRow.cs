using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One row of chips under the vitality bar: each stat, then each skill she knows, as its icon and what it's
    /// doing now ("Endurance 120 vitality", "Wayfinding ×2.10"), with two thin bars under it: progress
    /// to the next level (blue) and to the next mastery level (yellow). Hovering shows the levels and XP.
    /// Chips wrap onto more lines as skills are learnt (a FlowLayout on the row). A small toggle at the
    /// vitality bar's end hides and shows the row; the choice is kept (PlayerPrefs, for every save
    /// slot). A chip glows when its level or mastery rises.
    /// </summary>
    public class StatsRow : MonoBehaviour
    {
        private const string ShownKey = "HallOfEchoingMirrors.StatsRow.shown";

        [SerializeField] private GameController _game;
        [Tooltip("What the toggle hides: the chips' row. Keep this object itself outside it.")]
        [SerializeField] private GameObject _chipRow;
        [Tooltip("Copied once per stat and skill. Needs a Label (text) inside it, then two bars (sliders): " +
                 "level first, then mastery; and a TraitIcon before the Label.")]
        [SerializeField] private Button _chipTemplate;
        [Tooltip("Hides and shows the row (at the vitality bar's right end).")]
        [SerializeField] private Button _toggle;
        [Tooltip("Each stat's icon (a skill's is on the skill asset itself).")]
        [SerializeField] private TraitIcons _traitIcons;
        [Tooltip("How often (seconds) it looks for newly known skills. Numbers update every frame.")]
        [SerializeField, Min(0f)] private float _skillCheckSeconds = 0.5f;

        private TemplateList<Button> _chips;
        private readonly List<TMP_Text> _labels = new List<TMP_Text>();
        private readonly List<TraitIcon> _icons = new List<TraitIcon>();
        private readonly List<(Slider level, Slider mastery)> _bars = new List<(Slider, Slider)>();
        private readonly List<SkillDefinition> _skills = new List<SkillDefinition>();
        // Each stat's or skill's level and mastery last frame, to glow on a rise. Kept by stat or skill,
        // not by place: a newly known skill can arrive mid-row and push the others along.
        private readonly Dictionary<object, (int level, int mastery)> _last = new Dictionary<object, (int, int)>();
        private bool _quiet = true; // a new or loaded game (or the row back in view): the numbers jumped, they didn't rise
        private float _nextSkillCheck;
        private bool _shown = true;

        private Simulation Sim => _game.Simulation;
        private static int StatCount => AttributeMath.All.Length;

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= OnSimulationChanged;
        }

        private void OnSimulationChanged(Simulation sim) => _quiet = true;

        private void Start()
        {
            _game.SimulationChanged += OnSimulationChanged;
            _chips = new TemplateList<Button>(_chipTemplate, (chip, index) =>
            {
                _labels.Add(chip.GetComponentInChildren<TMP_Text>());
                var bars = chip.GetComponentsInChildren<Slider>(true);
                if (bars.Length != 2)
                    throw new System.InvalidOperationException(
                        $"StatsRow: the chip template needs two bars (level, then mastery), not {bars.Length}. Run the setup step again.");
                _bars.Add((bars[0], bars[1]));
                var icon = chip.GetComponentInChildren<TraitIcon>(true);
                if (icon == null)
                    throw new System.InvalidOperationException("StatsRow: the chip template has no TraitIcon. Run the setup step again.");
                _icons.Add(icon);
                ToolTip.On(chip, () => TipFor(index));
            });
            _toggle.onClick.AddListener(Toggle);
            ToolTip.On(_toggle, () => GameText.Get("tips.stats_toggle"));
            Show(PlayerPrefs.GetInt(ShownKey, 1) == 1);
        }

        private void Toggle()
        {
            Show(!_shown);
            PlayerPrefs.SetInt(ShownKey, _shown ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Show(bool shown)
        {
            _shown = shown;
            if (_chipRow.activeSelf != shown)
                _chipRow.SetActive(shown);
            UiText.SetCaption(_toggle, GameText.Get(shown ? "main.stats_hide" : "main.stats_show"));
            _quiet = true; // coming back into view: numbers may have moved while hidden, so no glow
        }

        private void Update()
        {
            var sim = Sim;
            if (sim == null || !_shown)
                return;
            bool sameGame = !_quiet;
            _quiet = false;

            // Which skills she knows only changes when a switch unlocks a task: no need to look every frame.
            if (!sameGame || Time.unscaledTime >= _nextSkillCheck)
            {
                _skills.Clear();
                _skills.AddRange(sim.KnownSkills());
                _nextSkillCheck = Time.unscaledTime + _skillCheckSeconds;
            }

            _chips.Show(StatCount + _skills.Count);
            for (int i = 0; i < StatCount; i++)
            {
                var attribute = AttributeMath.All[i];
                Refresh(i, attribute, _traitIcons.IconOf(attribute), ClaraTips.StatBrief(sim, attribute), sim.LevelOf(attribute), sim.MasteryOf(attribute),
                    sim.LevelProgressOf(attribute), sim.MasteryProgressOf(attribute), sameGame, asleep: !sim.IsAwake(attribute));
            }
            for (int i = 0; i < _skills.Count; i++)
            {
                var skill = _skills[i];
                Refresh(StatCount + i, skill, skill.Icon, ClaraTips.SkillBrief(sim, skill), sim.LevelOf(skill), sim.MasteryOf(skill),
                    sim.LevelProgressOf(skill), sim.MasteryProgressOf(skill), sameGame);
            }
        }

        private void Refresh(int index, object trait, Sprite icon, string text, int level, int mastery,
            float levelProgress, float masteryProgress, bool canGlow, bool asleep = false)
        {
            UiText.Set(_labels[index], text);
            _labels[index].alpha = asleep ? UiStyle.UnavailableAlpha : 1f; // a sleeping stat reads dimmed
            _icons[index].Show(icon, null); // no pop-up of its own: hovering it shows the chip's table
            _bars[index].level.value = levelProgress;
            _bars[index].mastery.value = masteryProgress;
            if (canGlow && _last.TryGetValue(trait, out var last) && (level > last.level || mastery > last.mastery))
                Glow.Flash(_labels[index]);
            _last[trait] = (level, mastery);
        }

        private string TipFor(int index)
        {
            var sim = Sim;
            if (sim == null)
                return null;
            // The full pop-ups: a table of the level, mastery and what they add up to.
            return index < StatCount ? ClaraTips.StatTip(sim, AttributeMath.All[index])
                : index - StatCount < _skills.Count ? ClaraTips.SkillTip(sim, _skills[index - StatCount])
                : null;
        }
    }
}
