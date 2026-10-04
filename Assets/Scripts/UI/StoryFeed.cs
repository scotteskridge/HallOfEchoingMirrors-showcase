using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Story box: the *Around her* lines (mostly story in Clara's voice, with small notes about the
    /// game state) and the milestone cards (StoryPanel posts them), in one list, oldest at the top.
    /// It follows new entries unless the player has scrolled up to reread. Older ambient lines fade
    /// and story lines can type themselves out; milestone cards never fade and are never thrown away
    /// (FeedTrim). Cleared at the start of each run.
    /// </summary>
    public class StoryFeed : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scroll;
        [Tooltip("Copied once per line. Lives inside the scroll view's content, which stacks its children.")]
        [SerializeField] private TMP_Text _lineTemplate;
        [Tooltip("Copied once per milestone reached. Lives beside the line template.")]
        [SerializeField] private MilestoneRow _milestoneTemplate;

        [Header("Look")]
        [Tooltip("The UI Fonts asset: story lines use its Story role, notes (game state) its Small role.")]
        [SerializeField] private UiFonts _fonts;
        [Tooltip("How many of the newest Around her lines stay at full strength before the rest start to fade. " +
                 "Milestones never fade.")]
        [SerializeField, Min(1)] private int _fullStrengthLines = 2;
        [Tooltip("Around her lines fade out between Full Strength Lines and this many, then are removed: so only " +
                 "this many are ever visible at once. Milestones stay for the whole run regardless.")]
        [SerializeField, Min(1)] private int _maxAmbientVisible = 5;
        [Tooltip("A long-run memory guard: the oldest Around her lines are thrown away past this many entries " +
                 "even if Max Ambient Visible somehow didn't already drop them. Milestones are always kept.")]
        [SerializeField, Min(10)] private int _maxLines = 150;

        [Header("Typewriter")]
        [Tooltip("Story lines appear letter by letter. (Will move to the Settings page, so players can turn it off.)")]
        [SerializeField] private bool _typewriter = true;
        [SerializeField, Min(1f)] private float _lettersPerSecond = 65f;

        /// <summary>Everything was cleared (a new run or another game): milestone cards handed out are gone.</summary>
        public event Action Cleared;

        /// <summary>One entry: a line of text, or a milestone card.</summary>
        private class Entry
        {
            public TMP_Text Text;      // a line; null for a card
            public MilestoneRow Card;  // a card; null for a line
            public bool IsStory;
            public float ShownAt;
            public int Length;
            public bool IsMilestone => Card != null;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<bool> _kinds = new List<bool>(); // scratch for FeedTrim: true = milestone
        private readonly Queue<TMP_Text> _spareLines = new Queue<TMP_Text>();
        private readonly Queue<MilestoneRow> _spareCards = new Queue<MilestoneRow>();
        private bool _snapToBottom;

        private void Awake()
        {
            if (_fonts == null)
                throw new InvalidOperationException("StoryFeed: no UI Fonts asset set, so lines can't be shown. Assign the Fonts field (the UiFonts asset) in the Inspector.");
            _lineTemplate.gameObject.SetActive(false);
            if (_milestoneTemplate != null)
                _milestoneTemplate.gameObject.SetActive(false);
        }

        /// <summary>A line in Clara's voice. *Asterisks* become italics.</summary>
        public void AddStory(string line) => AddLine(UiStyle.Markup(line), isStory: true);

        /// <summary>A short, quiet note about the game state, e.g. "A new way: the junction".</summary>
        public void AddNote(string line) => AddLine(UiStyle.Colour(line, UiStyle.Muted), isStory: false);

        /// <summary>
        /// A new milestone card at the bottom, for the caller to fill in (MilestoneRow.Setup and Refresh).
        /// It's the caller's until Cleared.
        /// </summary>
        public MilestoneRow AddMilestone()
        {
            if (_milestoneTemplate == null)
                throw new InvalidOperationException("StoryFeed: no milestone template set, so milestones can't be shown. Assign the Milestone Template field in the Inspector.");
            BeforeAdding();
            var card = _spareCards.Count > 0 ? _spareCards.Dequeue() : Instantiate(_milestoneTemplate, _milestoneTemplate.transform.parent);
            card.transform.SetAsLastSibling();
            card.gameObject.SetActive(true);
            _entries.Add(new Entry { Card = card });
            AfterAdding();
            return card;
        }

        public void Clear()
        {
            foreach (var entry in _entries)
                Recycle(entry);
            _entries.Clear();
            Cleared?.Invoke();
        }

        private void AddLine(string text, bool isStory)
        {
            BeforeAdding();
            var label = _spareLines.Count > 0 ? _spareLines.Dequeue() : Instantiate(_lineTemplate, _lineTemplate.transform.parent);
            label.transform.SetAsLastSibling();
            label.gameObject.SetActive(true);
            label.text = text;
            _fonts.Apply(label, isStory ? TextRole.Story : TextRole.Small);
            label.ForceMeshUpdate();

            label.maxVisibleCharacters = _typewriter && isStory ? 0 : int.MaxValue;
            _entries.Add(new Entry { Text = label, IsStory = isStory, ShownAt = Time.unscaledTime, Length = label.textInfo.characterCount });
            AfterAdding();
        }

        // Only follow new entries if the player was already at the bottom, not rereading further up.
        private void BeforeAdding() => _snapToBottom = _entries.Count == 0 || IsAtBottom();

        private void AfterAdding()
        {
            var kinds = Kinds();
            var gone = new SortedSet<int>(FeedTrim.Expired(kinds, _maxAmbientVisible));
            foreach (int i in FeedTrim.Dropped(kinds, _maxLines))
                gone.Add(i);
            foreach (int i in gone.Reverse()) // from the end, so the earlier indexes still hold
            {
                Recycle(_entries[i]);
                _entries.RemoveAt(i);
            }
            Fade();
        }

        /// <summary>Which entries are milestones, oldest first, for FeedTrim (a reused list).</summary>
        private List<bool> Kinds()
        {
            _kinds.Clear();
            foreach (var entry in _entries)
                _kinds.Add(entry.IsMilestone);
            return _kinds;
        }

        private void Recycle(Entry entry)
        {
            if (entry.IsMilestone)
            {
                entry.Card.gameObject.SetActive(false);
                _spareCards.Enqueue(entry.Card);
            }
            else
            {
                entry.Text.gameObject.SetActive(false);
                _spareLines.Enqueue(entry.Text);
            }
        }

        private bool IsAtBottom() => _scroll == null || _scroll.verticalNormalizedPosition <= 0.02f;

        private void LateUpdate()
        {
            if (_typewriter)
                TypeOut();

            // After the layout has made room for the new entry.
            if (_snapToBottom && _scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = 0f;
                _snapToBottom = false;
            }
        }

        private void TypeOut()
        {
            foreach (var entry in _entries)
            {
                if (!entry.IsStory || entry.Text.maxVisibleCharacters >= entry.Length)
                    continue;
                int shown = Mathf.FloorToInt((Time.unscaledTime - entry.ShownAt) * _lettersPerSecond);
                entry.Text.maxVisibleCharacters = Mathf.Min(shown, entry.Length);
            }
        }

        /// <summary>The newest ambient lines at full strength; older ones fade to nothing by Max Ambient Visible. Cards stay as they are.</summary>
        private void Fade()
        {
            var ages = FeedTrim.AmbientAges(Kinds());
            float fadeSpan = Mathf.Max(1, _maxAmbientVisible - _fullStrengthLines + 1);
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].IsMilestone)
                    continue;
                float alpha = 1f;
                if (FeedTrim.Fades(ages[i], _fullStrengthLines))
                {
                    float t = Mathf.Clamp01((ages[i] - _fullStrengthLines + 1) / fadeSpan);
                    alpha = Mathf.Lerp(1f, 0f, t);
                }
                _entries[i].Text.alpha = alpha;
            }
        }
    }
}
