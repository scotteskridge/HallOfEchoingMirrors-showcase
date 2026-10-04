using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Queue column: the whole queue top to bottom as stop cards (one per visit to a room),
    /// with To top and Remove on each entry, and how many actions are queued underneath.
    /// Between runs, the queue planned for the next run, from the start room.
    /// </summary>
    public class QueueDrawer : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("Copied once per stop, top to bottom.")]
        [SerializeField] private StopCard _cardTemplate;
        [Tooltip("How many actions are queued.")]
        [SerializeField] private TMP_Text _footer;
        [Tooltip("Moves rows and stops when they're dragged. Set up by setup Step 80.")]
        [SerializeField] private QueueDragController _drag;
        [Tooltip("Folds every by-heart block, or opens them all when none is open. Hidden with no blocks. Set up by Step 98.")]
        [SerializeField] private Button _foldAllButton;

        /// <summary>The PlayerPrefs key: the Ids of the rooms whose by-heart blocks the player last left open, joined by commas.</summary>
        public const string OpenBlocksKey = "HallOfEchoingMirrors.QueueBlocks.open";

        private TemplateList<StopCard> _cards;
        private readonly List<QueueStop> _stops = new List<QueueStop>();
        private readonly List<float> _tripCharges = new List<float>();
        // Which blocks the player has opened: by room, so it survives the queue changing from run to run
        // (kept in PlayerPrefs like the column's own fold, not in the run save).
        private readonly HashSet<string> _openRooms = new HashSet<string>();
        private readonly List<QueueStop> _blocks = new List<QueueStop>();
        // The block she is working through (the first stop, when it's a block): its room, how many of its actions
        // are done or skipped, and how many were queued last frame. Only this block ever has any done; the others
        // count what's queued. Counted from events, so removing an action lowers "2 of 5" instead of leaving it.
        private NodeDefinition _activeRoom;
        private int _activeDone, _activeCount, _finishedSinceLastFrame;
        // The words are rebuilt a few times a second, and at once when the queue or the wording changes; bars move every frame.
        private readonly TextThrottle _text = new TextThrottle();
        private Simulation _listeningTo;
        private readonly List<float> _middles = new List<float>();
        private readonly Vector3[] _corners = new Vector3[4]; // bottom left, top left, top right, bottom right

        private Simulation Sim => _game.Simulation;

        /// <summary>The list the stop cards scroll in, up and down (scrolled by a drag near its edge).</summary>
        public ScrollRect CardsScroll { get; private set; }

        private void Start()
        {
            CardsScroll = _cardTemplate.transform.parent.GetComponentInParent<ScrollRect>(true);
            foreach (string id in PlayerPrefs.GetString(OpenBlocksKey, "").Split(','))
                if (id.Length > 0)
                    _openRooms.Add(id);
            _foldAllButton.onClick.AddListener(ToggleAll);
            ToolTip.On(_foldAllButton, () => GameText.Get(AnyOpen() ? "queue.fold_all_tip" : "queue.open_all_tip"));
            _cards = new TemplateList<StopCard>(_cardTemplate, (card, _) =>
                card.Setup(entry => QueueEntryText.ToTop(Sim, entry), RemoveEntry,
                    RowTip, _drag,
                    ToggleFold, entry => Sim.RemoveStopOf(entry)));
            _game.SimulationChanged += ListenTo;
            GameText.Changed += MarkTextDirty;
            ListenTo(_game.Simulation);
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= ListenTo;
            GameText.Changed -= MarkTextDirty;
            ListenTo(null);
        }

        private void ListenTo(Simulation sim)
        {
            if (_listeningTo != null)
            {
                _listeningTo.QueueChanged -= MarkTextDirty;
                _listeningTo.LoopStarted -= ForgetActiveBlock;
                _listeningTo.TaskCompleted -= CountFinished;
                _listeningTo.TaskSkipped -= CountSkipped;
            }
            _listeningTo = sim;
            if (sim != null)
            {
                sim.QueueChanged += MarkTextDirty;
                sim.LoopStarted += ForgetActiveBlock;
                sim.TaskCompleted += CountFinished;
                sim.TaskSkipped += CountSkipped;
            }
            ForgetActiveBlock();
            MarkTextDirty();
        }

        private void MarkTextDirty() => _text.MarkDirty();
        private void ForgetActiveBlock() => _activeRoom = null;
        private void CountFinished(TaskDefinition _) => _finishedSinceLastFrame++;
        private void CountSkipped(TaskDefinition _, string __) => _finishedSinceLastFrame++;

        // ---------- Where a drag would drop ----------

        /// <summary>The rows' list of the card under the pointer, or null if it's over none.</summary>
        public ScrollRect RowsScrollAt(Vector2 screenPoint, Camera camera)
        {
            if (_cards == null)
                return null;
            for (int i = 0; i < _cards.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_cards[i].transform, screenPoint, camera))
                    return _cards[i].RowsScroll;
            return null;
        }

        /// <summary>
        /// The gap in the queue under the pointer for dropping one entry (0 is above the first entry),
        /// and the ends of a line across it (world space). False if the pointer isn't over a card, or
        /// the cards on screen don't yet match <paramref name="stops"/> (worked out afresh by the caller).
        /// </summary>
        public bool EntryGapAt(List<QueueStop> stops, Vector2 screenPoint, Camera camera, out int gap, out Vector3 lineStart, out Vector3 lineEnd)
        {
            gap = -1;
            lineStart = lineEnd = default;
            if (_cards == null || _cards.Count != stops.Count)
                return false;
            for (int i = 0; i < stops.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_cards[i].transform, screenPoint, camera))
                    return _cards[i].EntryGapAt(stops[i], screenPoint, camera, out gap, out lineStart, out lineEnd);
            return false;
        }

        /// <summary>
        /// The stop a dropped stop would go before, from the heights of the middles of the stop cards
        /// (world y, top card first) and the pointer's height: the first card after the first whose
        /// middle is below the pointer, or the card count for after the last. Never before the first
        /// stop (where the queue starts).
        /// </summary>
        public static int StopGapBefore(IReadOnlyList<float> cardMiddles, float pointerY)
        {
            for (int i = 1; i < cardMiddles.Count; i++)
                if (pointerY > cardMiddles[i])
                    return i;
            return cardMiddles.Count;
        }

        /// <summary>
        /// The gap between stop cards under the pointer, as the stop a dropped stop would go before
        /// (stops.Count for after the last), and the ends of a line across it (world space). Never
        /// before the first stop (where the queue starts). False if the pointer is outside the cards' list.
        /// </summary>
        public bool StopGapAt(List<QueueStop> stops, Vector2 screenPoint, Camera camera, out int beforeStop, out Vector3 lineStart, out Vector3 lineEnd)
        {
            beforeStop = -1;
            lineStart = lineEnd = default;
            var window = CardsScroll == null ? null : CardsScroll.viewport != null ? CardsScroll.viewport : (RectTransform)CardsScroll.transform;
            if (_cards == null || _cards.Count != stops.Count || stops.Count < 2 || window == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(window, screenPoint, camera) ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(window, screenPoint, camera, out var pointer))
                return false;

            _middles.Clear();
            for (int i = 0; i < stops.Count; i++)
            {
                ((RectTransform)_cards[i].transform).GetWorldCorners(_corners);
                _middles.Add((_corners[0].y + _corners[1].y) / 2f);
            }
            beforeStop = StopGapBefore(_middles, pointer.y);

            // The line goes halfway between the card it lands before and the one above it, or under the last.
            float y;
            if (beforeStop == stops.Count)
            {
                ((RectTransform)_cards[beforeStop - 1].transform).GetWorldCorners(_corners);
                y = _corners[0].y;
            }
            else
            {
                ((RectTransform)_cards[beforeStop - 1].transform).GetWorldCorners(_corners);
                float above = _corners[0].y;
                ((RectTransform)_cards[beforeStop].transform).GetWorldCorners(_corners);
                y = (above + _corners[1].y) / 2f;
            }
            lineStart = new Vector3(_corners[0].x, y, _corners[0].z);
            lineEnd = new Vector3(_corners[3].x, y, _corners[0].z);
            return true;
        }

        // A click can land after the queue changed under it: the row names its entry, and one that has left does nothing.
        private void RemoveEntry(QueueEntry entry) => Sim.RemoveFromQueue(entry);

        private void ToggleFold(QueueStop stop)
        {
            if (!_openRooms.Remove(stop.Room.Id))
                _openRooms.Add(stop.Room.Id);
            SaveOpenBlocks();
        }

        private bool AnyOpen()
        {
            foreach (var block in _blocks)
                if (_openRooms.Contains(block.Room.Id))
                    return true;
            return false;
        }

        // Folds every block showing now, or opens them all if none is open.
        private void ToggleAll()
        {
            bool fold = AnyOpen();
            foreach (var block in _blocks)
            {
                if (fold)
                    _openRooms.Remove(block.Room.Id);
                else
                    _openRooms.Add(block.Room.Id);
            }
            SaveOpenBlocks();
        }

        private void SaveOpenBlocks()
        {
            PlayerPrefs.SetString(OpenBlocksKey, string.Join(",", _openRooms));
            PlayerPrefs.Save();
            MarkTextDirty(); // the Fold all caption and the cards' fold buttons follow at once
        }

        // Every frame, as the rows' bars move; the words only now and then (TextThrottle). The stops are worked
        // out afresh too: they change when she arrives, a way opens or a run begins, not only when the queue is edited.
        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;

            bool rebuildText = _text.Due();
            sim.QueueStops(_stops);
            sim.PlannedTripCharges(_tripCharges);
            _cards.Show(_stops.Count);
            TrackActiveBlock();
            _blocks.Clear();
            for (int i = 0; i < _stops.Count; i++)
            {
                var stop = _stops[i];
                bool unfolded = false;
                int size = stop.EntryCount;
                if (stop.ByHeart)
                {
                    _blocks.Add(stop);
                    unfolded = _openRooms.Contains(stop.Room.Id);
                    if (i == 0)
                        size += _activeDone;
                }
                _cards[i].Refresh(sim, stop, i == 0 && !sim.Loop.IsOver, unfolded, size, _tripCharges, rebuildText);
            }
            if (!rebuildText)
                return;
            UiText.SetActive(_foldAllButton, _blocks.Count > 0);
            UiText.SetCaption(_foldAllButton, GameText.Get(AnyOpen() ? "queue.fold_all_button" : "queue.open_all_button"));
            UiText.Set(_footer, GameText.Get("queue.actions_count", ("count", sim.Queue.Count)) + WarningsNote(sim.PlanWarningCount));
        }

        // Counts the first block's finished actions: how far its queued count dropped, as far as actions finished
        // or were skipped since last frame (an action repeating finishes without leaving, and a removal isn't a finish).
        private void TrackActiveBlock()
        {
            bool block = _stops.Count > 0 && _stops[0].ByHeart;
            if (!block || _stops[0].Room != _activeRoom)
            {
                _activeRoom = block ? _stops[0].Room : null;
                _activeDone = 0;
                _activeCount = block ? _stops[0].EntryCount : 0;
            }
            else
            {
                int finished = Mathf.Min(_activeCount - _stops[0].EntryCount, _finishedSinceLastFrame);
                if (finished > 0)
                    _activeDone += finished;
                _activeCount = _stops[0].EntryCount;
            }
            _finishedSinceLastFrame = 0;
        }

        /// <summary>" · 2 warnings" after the count, in the warning colour; empty with none. Stops that won't carry over aren't counted: they still run this time.</summary>
        private static string WarningsNote(int warnings) =>
            warnings == 0
                ? ""
                : " " + UiStyle.Colour(warnings == 1
                    ? GameText.Get("queue.warnings.one")
                    : GameText.Get("queue.warnings.many", ("count", warnings)), UiStyle.Warning);

        // A row's tooltip: how it reads, why it will be refused (if it will), and how to drag it.
        private string RowTip(QueueEntry entry)
        {
            var sim = Sim;
            int index = sim.Queue.Entries.IndexOf(entry);
            if (index < 0)
                return null; // gone since the row was drawn
            var warnings = sim.PlanWarnings();
            string warning = index < warnings.Count ? warnings[index] : null;
            return QueueEntryText.Tip(sim, index, index < _tripCharges.Count ? _tripCharges[index] : 0f) +
                   (warning != null ? "\n" + WarningText.MarkedReason(warning) : "") +
                   "\n" + UiStyle.Aside(GameText.Get("queue.drag_tip"));
        }
    }
}
