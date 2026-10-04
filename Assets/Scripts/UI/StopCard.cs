using System;
using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// One stop in the queue drawer: its number, the room, a *return* marker if she's been there
    /// earlier in the queue, and a QueueRow for each entry done there (the trip that starts the stop
    /// first). A trip she can't make from where she'd be is greyed, with why. Dragging the card by its
    /// heading moves the whole stop (its rows drag themselves). A stop made only of actions carried from
    /// rooms known by heart is a block: folded by default to one line (its badge and count, or which
    /// action is running), with a fold button, and a remove button that takes out the whole stop.
    /// </summary>
    public class StopCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private TMP_Text _roomLabel;
        [Tooltip("Shown when this room was already a stop earlier in the queue.")]
        [SerializeField] private TMP_Text _returnMarker;
        [Tooltip("Copied once per queue entry at this stop.")]
        [SerializeField] private QueueRow _rowTemplate;
        [Tooltip("Shown while nothing is queued at this stop.")]
        [SerializeField] private TMP_Text _emptyLabel;
        [Tooltip("Under the heading of a block: its by-heart badge and action count, or which action is running. Set up by Step 97.")]
        [SerializeField] private TMP_Text _blockLine;
        [Tooltip("A folded block's running bar, under the block line. Set up by Step 97.")]
        [SerializeField] private Slider _blockBar;
        [Tooltip("Folds and unfolds a block. Set up by Step 97.")]
        [SerializeField] private Button _foldButton;
        [Tooltip("Removes every action of a folded block. Set up by Step 97.")]
        [SerializeField] private Button _removeBlockButton;

        private TemplateList<QueueRow> _rows;
        private int _firstEntry; // this card's first row is this entry in the queue
        private QueueStop _stop;
        private QueueDragController _drag;
        private readonly Vector3[] _corners = new Vector3[4]; // bottom left, top left, top right, bottom right
        private CanvasGroup _group; // fades the card while the whole stop is being dragged
        private bool _folded; // showing as a folded block (its rows hidden)
        private bool _hasText; // the words have been built at least once
        private string _notCarriedTip; // why this stop won't carry into the next run; null when it will
        private bool _warned; // an entry here will be refused when it's reached (Simulation.PlanWarnings)
        private string _foldedWarningTip; // a folded block hides its rows, so its tip lists the reasons; null otherwise

        /// <summary>The list this card's rows scroll in (scrolled by a drag near its top or bottom).</summary>
        public ScrollRect RowsScroll { get; private set; }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            RowsScroll = _rowTemplate.GetComponentInParent<ScrollRect>(true);
        }

        /// <summary>
        /// Hooks up the rows' buttons. Each is given the row's queue entry (not its place in the queue, which can
        /// change in the frame a click lands). <paramref name="drag"/> moves rows and the stop when dragged
        /// (null: they don't drag).
        /// </summary>
        public void Setup(Action<QueueEntry> onToTop, Action<QueueEntry> onRemove, Func<QueueEntry, string> tip, QueueDragController drag,
            Action<QueueStop> onToggleFold, Action<QueueEntry> onRemoveBlock)
        {
            _drag = drag;
            _foldButton.onClick.AddListener(() => onToggleFold(_stop));
            // By entry, like the rows: the stop's place can shift in the frame the click lands.
            _removeBlockButton.onClick.AddListener(() => onRemoveBlock(_rows.Count > 0 ? _rows[_rows.Count - 1].Entry : null));
            UiText.SetCaption(_removeBlockButton, GameText.Get("queue.remove_button"));
            ToolTip.On(_foldButton, () => GameText.Get(_folded ? "queue.block_unfold_tip" : "queue.block_fold_tip"));
            ToolTip.On(_removeBlockButton, () => GameText.Get("queue.remove_block_tip"));
            _rows = new TemplateList<QueueRow>(_rowTemplate, (row, _) =>
            {
                row.Setup(onToTop, onRemove, () => tip(row.Entry));
                row.SetupDrag(drag);
            });
            ToolTip.On(this, () => GameText.Get("queue.stop_tip", ("number", _stop.Number), ("room", RoomName)) +
                                   (_stop.ByHeart ? "\n" + GameText.Get("queue.by_heart_tip") : "") +
                                   (_warned ? "\n" + (_foldedWarningTip ?? GameText.Get("queue.warning_tip")) : "") +
                                   (_notCarriedTip != null ? "\n" + _notCarriedTip : "") +
                                   (_stop.Number > 1 ? "\n" + UiStyle.Aside(GameText.Get("queue.drag_tip")) : ""));
        }

        /// <param name="isHere">She's at this stop now (the first one, during a run).</param>
        /// <param name="unfolded">For a block: the player has opened it.</param>
        /// <param name="blockSize">For a block: its actions done or skipped so far plus those still queued, so "2 of 5" can drop when one is removed.</param>
        /// <param name="tripCharges">Every queue entry's trip charge, by queue index (<see cref="Simulation.PlannedTripCharges"/>).</param>
        /// <param name="rebuildText">Build the words afresh (the drawer says so a few times a second, and at once when the queue or the wording changes). A card whose stop changed does so anyway; one that hasn't moves only its bars.</param>
        public void Refresh(Simulation sim, QueueStop stop, bool isHere, bool unfolded, int blockSize, IReadOnlyList<float> tripCharges, bool rebuildText)
        {
            bool folded = stop.ByHeart && !unfolded;
            rebuildText |= !_hasText || folded != _folded || !SameStop(_stop, stop);
            _stop = stop;
            _folded = folded;
            _firstEntry = stop.FirstEntry;
            _background.color = isHere ? UiStyle.StopCardCurrent : UiStyle.StopCard;
            RowsScroll.gameObject.SetActive(!_folded);
            _rows.Show(stop.EntryCount);
            var entries = sim.Queue.Entries;
            var dragged = _drag != null ? _drag.Dragged : null;
            bool stopDragged = dragged != null && _drag.DraggingStop && stop.Number > 1 && entries[stop.FirstEntry] == dragged;
            _group.alpha = stopDragged ? UiStyle.DraggedAlpha : 1f;
            // The rows' bars and entries move every frame, even folded (they're hidden then, and the next unfold must be right).
            for (int i = 0; i < stop.EntryCount; i++)
            {
                var entry = entries[stop.FirstEntry + i];
                _rows[i].Refresh(entry, QueueEntryText.Progress(sim, entry), stop.FirstEntry + i == 0, dragged == entry && !_drag.DraggingStop);
            }
            if (stop.ByHeart && _folded)
                ShowBlockBar(sim, stop);
            else
                UiText.SetActive(_blockBar, false);
            if (rebuildText)
            {
                RebuildText(sim, stop, isHere, blockSize, tripCharges);
                _hasText = true;
            }
        }

        private static bool SameStop(QueueStop a, QueueStop b) =>
            a.Room == b.Room && a.Number == b.Number && a.IsReturn == b.IsReturn && a.FirstEntry == b.FirstEntry &&
            a.EntryCount == b.EntryCount && a.ByHeart == b.ByHeart && a.CarriesOver == b.CarriesOver;

        // The words: labels, row names and details, the block line. Built as new strings, so not every frame.
        private void RebuildText(Simulation sim, QueueStop stop, bool isHere, int blockSize, IReadOnlyList<float> tripCharges)
        {
            var warnings = sim.PlanWarnings();
            _warned = AnyWarned(warnings, stop);
            _foldedWarningTip = _warned && _folded ? FoldedWarningTip(sim, stop, warnings) : null;
            // Won't-carry is marked the same way (the foot doesn't count it: it still runs this time).
            bool marked = _warned || MarksNotCarried(stop);
            ShowBlock(sim, stop, blockSize, marked);
            UiText.Set(_numberLabel, stop.Number.ToString());
            UiText.Set(_roomLabel, RoomName + (marked ? " " + WarningText.Mark : "") + NotCarriedNote(sim, stop));
            _notCarriedTip = stop.CarriesOver || stop.Room == null
                ? null
                : GameText.Get("queue.not_carried_tip", ("needed", sim.Settings.byHeartRuns));
            WidenRoomLabel(stop);
            UiText.SetActive(_returnMarker, stop.IsReturn);
            if (stop.IsReturn)
                UiText.Set(_returnMarker, UiStyle.Colour(GameText.Get("queue.return"), UiStyle.ReturnMarker));
            // Planning the next run with nothing carried: say why, and when rooms start to be carried.
            if (stop.Number == 1 && stop.EntryCount == 0 && sim.Loop.IsOver)
            {
                UiText.SetActive(_emptyLabel, true);
                UiText.Set(_emptyLabel, GameText.Get("queue.stop_empty_planning", ("runs", sim.Settings.byHeartRuns)));
            }
            else
                UiText.ShowEmpty(_emptyLabel, stop.EntryCount == 0, "queue.stop_empty");

            if (_folded)
                return; // its rows are hidden, so they get their words when it opens (that rebuilds the text)
            var entries = sim.Queue.Entries;
            for (int i = 0; i < stop.EntryCount; i++)
            {
                int index = stop.FirstEntry + i;
                var entry = entries[index];
                string name = ActionText.NameOf(sim, index, entry);
                string details = QueueEntryText.Details(sim, entry, tripCharges[index]);
                // A trip she won't make as planned (Core's WillTravel) is one the run will skip: greyed,
                // with why, so it can be removed.
                if (entry.Destination != null && !sim.WillTravel(index))
                {
                    name = UiStyle.Colour(name, UiStyle.Muted);
                    details = sim.SkippedTripReason(index);
                }
                // Refused when it's reached: the mark after its name, and why in place of its details.
                string warning = index < warnings.Count ? warnings[index] : null;
                if (warning != null)
                {
                    name += " " + WarningText.Mark;
                    details = WarningText.ReasonLine(warning);
                }
                // In a stop that isn't a block, the carried rows wear the mark the block's badge would.
                if (entry.ByHeart && !stop.ByHeart)
                    name += " " + UiStyle.Small(UiStyle.Colour(GameText.Get("queue.by_heart"), UiStyle.ByHeart));
                _rows[i].SetText(name, details);
            }
        }

        /// <summary>
        /// What tells the player this stop won't be queued for the next run: its room isn't known by heart
        /// (with how many of the runs needed she has worked there), or an earlier stop's isn't. Empty when it carries.
        /// </summary>
        private static string NotCarriedNote(Simulation sim, QueueStop stop)
        {
            if (stop.CarriesOver || stop.Room == null)
                return "";
            string note = sim.IsKnownByHeart(stop.Room)
                ? GameText.Get("queue.carry_blocked")
                : GameText.Get("queue.not_by_heart", ("runs", sim.RunsWorkedIn(stop.Room)), ("needed", sim.Settings.byHeartRuns));
            return " " + UiStyle.Aside(note);
        }

        /// <summary>
        /// The room label's right edge, as a share of the header: it stops short of the return marker (from
        /// 0.60) or the fold button (from 0.77) only when those are showing, so the "worked here in 0 runs"
        /// note isn't cut off on an ordinary stop (plan ui-033 step 5).
        /// </summary>
        private void WidenRoomLabel(QueueStop stop)
        {
            float right = stop.IsReturn ? 0.60f : stop.ByHeart ? 0.76f : 0.98f;
            var rect = _roomLabel.rectTransform;
            if (!Mathf.Approximately(rect.anchorMax.x, right))
                rect.anchorMax = new Vector2(right, rect.anchorMax.y);
        }

        /// <summary>
        /// Whether a stop wears the won't-carry ⚠, here and on its room's map tag (MapView): decided here only,
        /// so the two can't disagree. An empty stop (only ever the first: the others hold their trip) has
        /// nothing to lose, so it isn't marked (the user, 2026-10-01); its grey note still says why.
        /// </summary>
        public static bool MarksNotCarried(QueueStop stop) => stop.EntryCount > 0 && !stop.CarriesOver && stop.Room != null;

        /// <summary>A folded block's rows are hidden, so its tip says which action is refused and why.</summary>
        private static string FoldedWarningTip(Simulation sim, QueueStop stop, IReadOnlyList<string> warnings)
        {
            var reasons = new List<string>();
            var entries = sim.Queue.Entries;
            for (int i = stop.FirstEntry; i < stop.FirstEntry + stop.EntryCount && i < warnings.Count; i++)
                if (warnings[i] != null)
                    reasons.Add(WarningText.ActionWithReason(ActionText.NameOf(sim, i, entries[i]), warnings[i]));
            return GameText.Get("queue.warning_tip_folded", ("reasons", string.Join("\n", reasons)));
        }

        private static bool AnyWarned(IReadOnlyList<string> warnings, QueueStop stop)
        {
            for (int i = stop.FirstEntry; i < stop.FirstEntry + stop.EntryCount && i < warnings.Count; i++)
                if (warnings[i] != null)
                    return true;
            return false;
        }

        /// <summary>The block's own words: the badge line (with the ⚠ when folded and <paramref name="marked"/>) and the fold and remove buttons.</summary>
        private void ShowBlock(Simulation sim, QueueStop stop, int blockSize, bool marked)
        {
            bool block = stop.ByHeart;
            UiText.SetActive(_blockLine, block);
            UiText.SetActive(_foldButton, block);
            UiText.SetActive(_removeBlockButton, block && _folded);
            UiText.SetCaption(_foldButton, GameText.Get(_folded ? "queue.unfold_block_button" : "queue.fold_block_button"));
            if (!block)
                return;
            string badge = UiStyle.Colour(GameText.Get("queue.by_heart"), UiStyle.ByHeart);
            string line = RunningEntryOf(sim, stop) != null
                ? GameText.Get("queue.by_heart_running", ("badge", badge), ("done", blockSize - stop.EntryCount + 1),
                    ("total", blockSize), ("action", sim.CurrentActionName))
                : GameText.Get(stop.EntryCount == 1 ? "queue.by_heart_actions.one" : "queue.by_heart_actions.many", ("badge", badge), ("count", stop.EntryCount));
            UiText.Set(_blockLine, _folded && marked ? line + " " + WarningText.Mark : line);
        }

        /// <summary>A folded block's running bar: its running action's progress, shown only while one is running.</summary>
        private void ShowBlockBar(Simulation sim, QueueStop stop)
        {
            var runningEntry = RunningEntryOf(sim, stop);
            UiText.SetActive(_blockBar, runningEntry != null);
            if (runningEntry != null)
                _blockBar.value = QueueEntryText.Progress(sim, runningEntry);
        }

        private static QueueEntry RunningEntryOf(Simulation sim, QueueStop stop)
        {
            if (sim.Loop.IsOver)
                return null;
            var entries = sim.Queue.Entries;
            for (int i = stop.FirstEntry; i < stop.FirstEntry + stop.EntryCount; i++)
                if (entries[i] == sim.Loop.RunningEntry)
                    return entries[i];
            return null;
        }

        /// <summary>
        /// Where one dragged entry would drop in this card: the queue gap nearest the pointer, and the
        /// ends of a line across the card there (world space). Never above a stop's trip: that would put
        /// it in the stop before. False if this card is showing an older <paramref name="stop"/>.
        /// </summary>
        public bool EntryGapAt(QueueStop stop, Vector2 screenPoint, Camera camera, out int gap, out Vector3 lineStart, out Vector3 lineEnd)
        {
            gap = -1;
            lineStart = lineEnd = default;
            var rowsArea = (RectTransform)_rowTemplate.transform.parent;
            // A folded block shows no rows to drop between.
            if (_folded || stop.FirstEntry != _firstEntry || stop.EntryCount != _rows.Count ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(rowsArea, screenPoint, camera, out var pointer))
                return false;

            int local = stop.EntryCount;
            for (int i = stop.Number > 1 ? 1 : 0; i < stop.EntryCount; i++)
            {
                RowCorners(i);
                if (pointer.y > (_corners[0].y + _corners[1].y) / 2f)
                {
                    local = i;
                    break;
                }
            }
            gap = stop.FirstEntry + local;

            // Halfway between the rows either side of the gap (world y grows upwards).
            float y;
            if (stop.EntryCount == 0)
            {
                rowsArea.GetWorldCorners(_corners);
                y = _corners[1].y;
            }
            else if (local == stop.EntryCount)
            {
                RowCorners(local - 1);
                y = _corners[0].y;
            }
            else
            {
                RowCorners(local);
                y = _corners[1].y;
                if (local > 0)
                {
                    float above = y;
                    RowCorners(local - 1);
                    y = (above + _corners[0].y) / 2f;
                }
            }
            // Kept inside the rows' window, for rows scrolled out of sight.
            var window = rowsArea.parent as RectTransform;
            if (window != null)
            {
                window.GetWorldCorners(_corners);
                y = Mathf.Clamp(y, _corners[0].y, _corners[1].y);
            }
            rowsArea.GetWorldCorners(_corners);
            lineStart = new Vector3(_corners[0].x, y, _corners[0].z);
            lineEnd = new Vector3(_corners[2].x, y, _corners[0].z);
            return true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.BeginStopDrag(_stop, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_drag != null)
                _drag.EndDrag(eventData);
        }

        private void RowCorners(int local) => ((RectTransform)_rows[local].transform).GetWorldCorners(_corners);

        private string RoomName => _stop.Room != null ? _stop.Room.DisplayName : "";
    }
}
