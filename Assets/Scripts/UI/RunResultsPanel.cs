using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Summary page, shown once a run has ended: how it went compared with the runs before, what
    /// Clara gained, and two buttons: Repeat (begin the next run now) and Plan (look at the carried plan first); before planning
    /// is earned, one "Go back in" button with a line above it. It hands each part of the run report to
    /// the piece that draws it.
    /// </summary>
    public class RunResultsPanel : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private GameObject _panel;
        [Tooltip("A small line above the headline: which loop this was.")]
        [SerializeField] private TMP_Text _caption;
        [Tooltip("How the run ended and how long it lasted, in one line at the top.")]
        [SerializeField] private TMP_Text _headline;
        [Tooltip("Under the headline: the run before this one and the difference. Hidden on the first run.")]
        [SerializeField] private TMP_Text _versusLast;
        [Tooltip("Under that: how many actions she finished, and the longest run. The Longest part has the pop-up.")]
        [SerializeField] private TMP_Text _detail;
        [SerializeField] private RunTimeStrip _timeStrip;
        [SerializeField] private SkillGainRow _skillRow;
        [SerializeField] private MilestoneTable _milestones;
        [SerializeField] private ChipColumns _chipColumns;
        [SerializeField] private StatStrip _statStrip;
        [FormerlySerializedAs("_nextRunButton")]
        [FormerlySerializedAs("_planNextButton")]
        [Tooltip("Repeat: begins the next run now, with whatever is carried in the queue.")]
        [SerializeField] private Button _repeatButton;
        [Tooltip("Plan: opens the planning screen, to look at and edit the carried plan first.")]
        [SerializeField] private Button _planButton;
        [Tooltip("Turns the page to the planning screen for Plan.")]
        [SerializeField] private ScreenManager _screens;
        [Tooltip("The Repeat button's label: reads 'Go back in' until planning is earned (Feed your hours), then 'Repeat'.")]
        [SerializeField] private TextKey _repeatLabel;
        [Tooltip("A line in Clara's voice above the button, shown only until planning is earned.")]
        [SerializeField] private GameObject _wakeLine;

        private const string RepeatKey = "results.repeat_button";
        private const string GoBackKey = "results.go_back_button";

        private RunReport _shown;
        private float _repeatX;
        private bool? _planningShown; // null until the page has been drawn once

        private Simulation Sim => _game.Simulation;

        private void Start()
        {
            // Repeat: the next run begins at once, on the main page (the Screen Manager turns there).
            _repeatButton.onClick.AddListener(() => _game.BeginLoop());
            // Plan: the planning screen, where Begin starts the run.
            _planButton.onClick.AddListener(() => _screens.ShowPlan());
            ToolTip.On(_repeatButton, () => GameText.Get(Sim.PlanningUnlocked ? "results.repeat_tip" : "results.go_back_tip"));
            _repeatX = ((RectTransform)_repeatButton.transform).anchoredPosition.x;
            ToolTip.On(_planButton, () => GameText.Get("results.plan_tip"));
            _panel.SetActive(false);
            GameText.Changed += Rewrite;
            // Only explains "Longest", so there is nothing to say on a first run that shows no record.
            ToolTip.On(_detail, () => _shown != null && (_shown.Longest != null || _shown.IsNewLongest)
                ? GameText.Get("results.longest_tip") : null);
        }

        private void OnDestroy() => GameText.Changed -= Rewrite;

        // The text file was saved: write the card again in the new words.
        private void Rewrite() => _shown = null;

        private void Update()
        {
            // GameController didn't start (no Loop Settings; it already logged why): stay quiet rather than throw every frame.
            if (Sim == null)
            {
                if (_panel.activeSelf)
                    _panel.SetActive(false);
                return;
            }
            var report = Sim.LastRun;
            bool show = Sim.Phase == LoopPhase.BetweenRuns && Sim.Loop.IsOver && report != null;
            if (_panel.activeSelf != show)
                _panel.SetActive(show);
            // Planning is earned (Feed your hours to the flames); until then the next run just begins,
            // so the one button says so ("Go back in"), centred, with a line above it.
            if (show && _planningShown != Sim.PlanningUnlocked)
                ShowPlanning(Sim.PlanningUnlocked);

            // The report never changes once made, so the page only needs writing once per run.
            if (show && report != _shown)
            {
                UiText.Set(_caption, GameText.Get("results.run_caption", ("loop", report.LoopNumber)));
                ShowHeadline(report);
                ShowDetail(report);
                _timeStrip.Show(report);
                _skillRow.Show(report, Sim.Settings);
                _milestones.Show(report);
                _chipColumns.Show(report);
                _statStrip.Show(report, _game);
                _shown = report;
            }
        }

        private void ShowPlanning(bool planning)
        {
            _planningShown = planning;
            _planButton.gameObject.SetActive(planning);
            _wakeLine.SetActive(!planning);
            _repeatLabel.SetKey(planning ? RepeatKey : GoBackKey);
            var rect = (RectTransform)_repeatButton.transform;
            rect.anchoredPosition = new Vector2(planning ? _repeatX : 0f, rect.anchoredPosition.y);
        }

        // "Her strength gave out after 4:20." and, under it, "Last run 5:10 (-0:50)".
        private void ShowHeadline(RunReport run)
        {
            string key = run.EndReason switch
            {
                LoopEndReason.EndedByPlayer => "results.headline.ended_early",
                LoopEndReason.WalkedOut => "results.headline.walked_out",
                _ => "results.headline.exhausted",
            };
            UiText.Set(_headline, GameText.Get(key, ("time", UiStyle.Colour(UiText.Clock(run.Seconds), UiStyle.Milestone))));

            var before = run.RunBefore;
            _versusLast.gameObject.SetActive(before != null);
            if (before != null)
            {
                float last = run.SecondsOf(before);
                string delta = UiText.ClockChange(UiText.ClockDifference(run.Seconds, last));
                UiText.Set(_versusLast, GameText.Get("results.vs_last", ("time", UiText.Clock(last)), ("delta", delta)));
            }
        }

        // "12 actions · Longest 6:10", or "Longest yet" when this run beat the record; then
        // " · Known by heart: the junction" when this run made a room known by heart.
        // Placeholder rule: the user has not said whether the action count is wanted (plan 017).
        private void ShowDetail(RunReport run)
        {
            string line;
            if (run.IsNewLongest)
                line = GameText.Get("results.actions_new_longest_line", ("actions", run.ActionsCompleted),
                    ("mark", UiStyle.Colour(GameText.Get("results.new_longest"), UiStyle.Milestone)));
            else if (run.Longest != null)
                line = GameText.Get("results.actions_longest_line", ("actions", run.ActionsCompleted),
                    ("time", UiText.Clock(run.SecondsOf(run.Longest))));
            else
                line = GameText.Get("results.actions_line", ("actions", run.ActionsCompleted));
            if (run.KnownByHeartNow.Count > 0)
            {
                var names = run.KnownByHeartNow.ConvertAll(room => GameText.TitleInSentence(room.DisplayName));
                line += " " + GameText.Get("results.by_heart_suffix", ("rooms", string.Join(", ", names)));
            }
            if (run.Moves > 0)
                line += " " + GameText.Get("results.moves_suffix",
                    ("moves", run.Moves), ("vitality", UiText.Number(run.MoveVitality)));
            line += RunSummaryText.ChargeSuffixes(run);
            if (UiText.Number(run.KeptVitalityGained) != "0") // UiText.Number rounds to one place: don't show "+0"
                line += " " + GameText.Get("results.kept_suffix", ("vitality", UiText.Number(run.KeptVitalityGained)));
            UiText.Set(_detail, line);
        }
    }
}
