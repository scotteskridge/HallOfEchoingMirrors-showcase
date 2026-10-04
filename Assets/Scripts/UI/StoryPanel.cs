using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The milestones in the Story box: each milestone reached this run becomes a card in the story
    /// feed, in order among the Around her lines. A card shows when it was reached (and how that compares
    /// with last run), the story's opening paragraph, and Read more, which opens the whole passage.
    /// The feed owns the cards and clears them with everything else at a new run.
    /// </summary>
    public class StoryPanel : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("The Story box's feed, where the milestone cards go.")]
        [SerializeField] private StoryFeed _feed;
        [Tooltip("Opens a story's whole passage (Read more).")]
        [SerializeField] private StoryPopup _popup;
        [Tooltip("The opening paragraph is cut to about this many letters.")]
        [SerializeField, Min(40)] private int _paragraphLength = 260;

        // The cards posted so far this run, oldest first: card i is this run's milestone i.
        private readonly List<MilestoneRow> _cards = new List<MilestoneRow>();

        private Simulation Sim => _game.Simulation;

        private void Start()
        {
            if (_popup == null)
            {
                Debug.LogError("StoryPanel: the Popup field is empty, so Read more can't open. Assign the StoryPopup in the Inspector.", this);
                enabled = false;
                return;
            }
            if (_feed == null)
            {
                Debug.LogError("StoryPanel: the Feed field is empty, so milestones won't show. Assign the StoryFeed in the Inspector.", this);
                enabled = false;
                return;
            }
            _feed.Cleared += _cards.Clear;
            GameText.Changed += RefreshAll;
        }

        private void OnDestroy()
        {
            GameText.Changed -= RefreshAll;
            if (_feed != null)
                _feed.Cleared -= _cards.Clear;
        }

        // Posting catches up with the run's list rather than answering MilestoneReached, so the order
        // doesn't matter when a new run or game clears the feed: the next frame posts what's missing.
        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;
            var reached = sim.Loop.Milestones;
            // More cards than milestones: a new run or game began and nothing cleared the box (FeedNotes
            // does). Shouldn't happen; say so, and clear it here so last run's cards don't linger.
            if (_cards.Count > reached.Count)
            {
                Debug.LogError("StoryPanel: a new run began but the Story box wasn't cleared. Is FeedNotes' feed set?", this);
                _feed.Clear();
            }
            while (_cards.Count < reached.Count)
            {
                int index = _cards.Count;
                var card = _feed.AddMilestone();
                card.Setup(() => ReadMore(index), () => GameText.Get("story.times_tip"),
                    hasStory: Simulation.StoryOf(sim.Loop.Milestones[index].milestone) != null);
                _cards.Add(card);
                Refresh(sim, index);
            }
        }

        private void RefreshAll()
        {
            var sim = Sim;
            if (sim == null)
                return;
            for (int i = 0; i < _cards.Count && i < sim.Loop.Milestones.Count; i++)
                Refresh(sim, i);
        }

        private void Refresh(Simulation sim, int index)
        {
            var (milestone, seconds) = sim.Loop.Milestones[index];
            var story = Simulation.StoryOf(milestone);
            _cards[index].Refresh(story != null ? story.Title : Simulation.NameOf(milestone),
                Times(sim, milestone, seconds), Opening(story));
        }

        private void ReadMore(int index)
        {
            var reached = Sim.Loop.Milestones;
            if (index < reached.Count && _popup != null && Simulation.StoryOf(reached[index].milestone) is StoryBeat story)
                _popup.Reread(story);
        }

        /// <summary>"2:31   last run 3:04  (−0:33)", or "0:07   first time".</summary>
        private static string Times(Simulation sim, ContentAsset milestone, float seconds)
        {
            string time = UiText.Clock(seconds);
            if (sim.IsFirstTimeEver(milestone))
                return GameText.Get("story.times_first", ("time", time));

            float? last = sim.LastRunTimeOf(milestone);
            if (last == null)
                return GameText.Get("story.times_no_last", ("time", time));

            float change = seconds - last.Value;
            string delta = change <= 0f
                ? UiStyle.Colour(GameText.Get("story.faster", ("delta", UiText.Clock(-change))), UiStyle.Levelling)
                : UiStyle.Colour(GameText.Get("story.slower", ("delta", UiText.Clock(change))), UiStyle.Warning);
            return GameText.Get("story.times", ("time", time), ("last", UiText.Clock(last.Value)), ("delta", delta));
        }

        /// <summary>The passage's first paragraph, cut short if it's long.</summary>
        private string Opening(StoryBeat story)
        {
            if (story == null)
                return "";
            string body = story.Body.Replace("\r\n", "\n").Trim();
            int end = body.IndexOf("\n\n", System.StringComparison.Ordinal);
            string paragraph = end > 0 ? body.Substring(0, end) : body;
            if (paragraph.Length > _paragraphLength)
                paragraph = paragraph.Substring(0, _paragraphLength).TrimEnd() + "…";
            return UiStyle.Markup(paragraph);
        }
    }
}
