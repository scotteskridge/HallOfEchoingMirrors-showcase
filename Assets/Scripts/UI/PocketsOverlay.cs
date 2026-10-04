using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The overlay on the map (its left side, until the player drags it elsewhere): everything
    /// Clara has. A "3/5 pockets" heading; one chip per kind of item she carries ("Candle 6", edged
    /// in the item kind's map colour), a satchel among them; one line per container ("Pouch: Phial
    /// 4/10"); what's on the floor where she is; then knowledge and progress (what isn't an object,
    /// this run's and kept). An item in use shows its countdown. Read-only: Put down and Pick up
    /// stay in the room popover.
    /// Only the chips and headings catch the mouse, so clicks between them reach the rooms beneath;
    /// dragging any of them moves the overlay (DragToMove, on the same object: it stays where it
    /// was left, and a double-click puts it back). A chip of something she has glows when its
    /// count rises.
    /// </summary>
    public class PocketsOverlay : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [Tooltip("For the item kinds' colours (the map's floor-dot colours).")]
        [SerializeField] private MapView _map;
        [Tooltip("\"3/5 pockets\": carries the overlay's tooltip.")]
        [SerializeField] private TMP_Text _heading;
        [Tooltip("Copied once per thing carried. Needs a Label (text) and an Edge (thin image) inside it.")]
        [SerializeField] private Image _chipTemplate;
        [Tooltip("\"On the floor here\": shown while there's anything on the floor where she is.")]
        [SerializeField] private TMP_Text _floorHeading;
        [Tooltip("Copied once per kind of item on the floor here. Needs a Label (text) and an Edge (thin image) inside it.")]
        [SerializeField] private Image _floorTemplate;
        [Tooltip("\"Knowledge and progress\": shown while there's any.")]
        [SerializeField] private TMP_Text _knowledgeHeading;
        [Tooltip("Copied once per thing known or kept. Needs a Label (text) and an Edge (thin image) inside it.")]
        [SerializeField] private Image _knowledgeTemplate;
        [Tooltip("The stats chips above the overlay. Until the player drags the overlay somewhere, it hangs just below these, " +
                 "however many lines they wrap to. Empty: it stays where the scene puts it.")]
        [SerializeField] private RectTransform _below;
        [Tooltip("The gap between the chips' bottom edge and the overlay, in pixels.")]
        [SerializeField, Min(0f)] private float _gapBelow = 16f;

        /// <summary>One chip on screen: something carried or known, or a container with what's in it.</summary>
        private struct Line
        {
            public ResourceDefinition Item;      // for a container's line: the container
            public int Amount;                   // for a container's line: how many it holds now
            public bool IsContainer;
        }

        /// <summary>A group of chips (carried, on the floor, or known) and each chip's parts, found once when it's made.</summary>
        private class Group
        {
            public readonly List<Line> Lines = new List<Line>();
            public readonly List<Line> Shown = new List<Line>(); // what the chips' words were last built from
            public readonly List<TMP_Text> Labels = new List<TMP_Text>();
            public readonly List<Image> Edges = new List<Image>();
            public TemplateList<Image> Chips;
        }

        private readonly Group _carriedGroup = new Group();
        private readonly Group _floorGroup = new Group();
        private readonly Group _knownGroup = new Group();
        private readonly List<(ResourceDefinition item, int amount)> _floor = new List<(ResourceDefinition, int)>();
        private readonly List<CarriedEntry> _carried = new List<CarriedEntry>();
        private readonly List<(ResourceDefinition item, int amount)> _known = new List<(ResourceDefinition, int)>();
        private readonly List<string> _contents = new List<string>();
        // Each line's count last frame, by item (or container): a count that rises glows.
        private Dictionary<ResourceDefinition, int> _lastAmounts = new Dictionary<ResourceDefinition, int>();
        private Dictionary<ResourceDefinition, int> _nowAmounts = new Dictionary<ResourceDefinition, int>();
        private bool _quiet = true; // a new or loaded game: counts jumped, they didn't rise
        // Chip words (with their timers) are rebuilt a few times a second; a chip whose count changes is rebuilt at once, and glows.
        private readonly TextThrottle _text = new TextThrottle();

        private Simulation Sim => _game.Simulation;

        private DragToMove _drag;
        private readonly Vector3[] _corners = new Vector3[4];

        // Where the overlay hangs from, until the player moves it: the bottom of the stats chips, which grow as skills are learnt
        // and wrap sooner on a narrower screen (plan ui-033 step 6).
        private void LateUpdate()
        {
            if (_below == null)
                return;
            if (_drag.Placed == null)
            {
                var self = (RectTransform)transform;
                _below.GetWorldCorners(_corners);
                float bottom = self.parent.InverseTransformPoint(_corners[0]).y;
                float top = bottom - _gapBelow;
                // Pivot is the top-left corner (set up in the scene), so its local y is its top edge.
                if (!Mathf.Approximately(self.localPosition.y, top))
                    self.localPosition = new Vector3(self.localPosition.x, top, self.localPosition.z);
            }
            _drag.KeepPlaced();
        }

        private void OnDestroy()
        {
            if (_game != null)
                _game.SimulationChanged -= OnSimulationChanged;
            GameText.Changed -= _text.MarkDirty;
        }

        private void OnSimulationChanged(Simulation sim)
        {
            _quiet = true;
            _text.MarkDirty();
        }

        private void Start()
        {
            _drag = GetComponent<DragToMove>();
            if (_below != null)
                _drag.OwnerPlaces = true; // LateUpdate places it under the chips, then lets DragToMove keep it in view
            _game.SimulationChanged += OnSimulationChanged;
            GameText.Changed += _text.MarkDirty;
            Setup(_carriedGroup, _chipTemplate);
            Setup(_floorGroup, _floorTemplate);
            Setup(_knownGroup, _knowledgeTemplate);
            ToolTip.On(_heading, () =>
            {
                var sim = Sim;
                return sim == null ? null
                    : GameText.Get("tips.pockets_overlay", ("used", sim.PocketsUsed), ("slots", sim.PocketSlots));
            });
            ToolTip.On(_floorHeading, () =>
            {
                var sim = Sim;
                return sim == null ? null : GameText.Get("tips.pockets_floor", ("space", sim.FloorSpace));
            });
            ToolTip.On(_knowledgeHeading, () => GameText.Get("tips.pockets_knowledge"));
        }

        private void Setup(Group group, Image template)
        {
            group.Chips = new TemplateList<Image>(template, (chip, index) =>
            {
                group.Labels.Add(chip.transform.Find("Label").GetComponent<TMP_Text>());
                group.Edges.Add(chip.transform.Find("Edge").GetComponent<Image>());
                ToolTip.On(chip, () => TipFor(group, index));
            });
        }

        private void Update()
        {
            var sim = Sim;
            if (sim == null)
                return;
            bool sameGame = !_quiet;
            _quiet = false;

            CollectCarried(sim);
            CollectFloor(sim);
            CollectKnown(sim);
            bool rebuildText = _text.Due();
            bool changed = Show(sim, _carriedGroup, sameGame, rebuildText);
            changed |= Show(sim, _floorGroup, sameGame, rebuildText);
            changed |= Show(sim, _knownGroup, sameGame, rebuildText);
            UiText.SetActive(_floorHeading, _floorGroup.Lines.Count > 0);
            UiText.SetActive(_knowledgeHeading, _knownGroup.Lines.Count > 0);
            if (rebuildText || changed)
            {
                UiText.Set(_heading, GameText.Get("pockets.heading", ("used", sim.PocketsUsed), ("slots", sim.PocketSlots)));
                if (_floorGroup.Lines.Count > 0)
                    UiText.Set(_floorHeading, GameText.Get("pockets.floor",
                        ("limit", UiStyle.Aside(GameText.Get("pockets.floor_limit", ("space", sim.FloorSpace))))));
                if (_knownGroup.Lines.Count > 0)
                    UiText.Set(_knowledgeHeading, GameText.Get("pockets.knowledge"));
            }
            (_lastAmounts, _nowAmounts) = (_nowAmounts, _lastAmounts);
            _nowAmounts.Clear();
        }

        /// <summary>Shows a group's chips; returns whether a chip appeared, went or changed its count.</summary>
        private bool Show(Simulation sim, Group group, bool sameGame, bool rebuildText)
        {
            group.Chips.Show(group.Lines.Count);
            // Such a chip is worded at once, not at the next beat.
            bool changed = !SameAsShown(group);
            if (changed)
            {
                rebuildText = true;
                group.Shown.Clear();
                group.Shown.AddRange(group.Lines);
            }
            for (int i = 0; i < group.Lines.Count; i++)
            {
                var line = group.Lines[i];
                if (rebuildText)
                {
                    UiText.Set(group.Labels[i], line.IsContainer ? ContainerText(sim, line)
                        : group == _floorGroup ? FloorText(line) : ItemText(sim, line));
                    // Knowledge isn't an object: it has no kind to show.
                    bool edged = group != _knownGroup;
                    if (group.Edges[i].enabled != edged)
                        group.Edges[i].enabled = edged;
                    if (edged)
                        group.Edges[i].color = _map.Style.KindColour(line.IsContainer ? KindInside(line.Item) : line.Item.kind);
                }
                // The floor doesn't glow (as on the map): only what she has.
                if (group == _floorGroup)
                    continue;

                int before = _lastAmounts.TryGetValue(line.Item, out int shown) ? shown : 0;
                if (sameGame && line.Amount > before)
                    Glow.Flash(group.Labels[i]);
                _nowAmounts[line.Item] = line.Amount;
            }
            return changed;
        }

        private static bool SameAsShown(Group group)
        {
            if (group.Lines.Count != group.Shown.Count)
                return false;
            for (int i = 0; i < group.Lines.Count; i++)
            {
                var now = group.Lines[i];
                var then = group.Shown[i];
                if (now.Item != then.Item || now.Amount != then.Amount || now.IsContainer != then.IsContainer)
                    return false;
            }
            return true;
        }

        // ---------- What's shown ----------

        /// <summary>
        /// What she carries (Simulation.CarriedItems), one line per kind and one per container (its
        /// contents go in its words). One in use with none left still shows while its timer runs.
        /// </summary>
        private void CollectCarried(Simulation sim)
        {
            var lines = _carriedGroup.Lines;
            sim.CarriedItems(_carried);
            lines.Clear();
            foreach (var entry in _carried)
            {
                if (entry.IsContainer)
                    lines.Add(new Line { Item = entry.Item, Amount = sim.ContainerUsed(entry.Item), IsContainer = true });
                else if (entry.Container == null)
                    lines.Add(new Line { Item = entry.Item, Amount = entry.Amount });
            }
            foreach (var restoring in sim.Loop.Restorings)
                if (restoring.Item.IsPocketed && sim.ContainerFor(restoring.Item) == null && !Lists(lines, restoring.Item))
                    lines.Add(new Line { Item = restoring.Item });
        }

        /// <summary>What lies on the floor where she is, one line per kind.</summary>
        private void CollectFloor(Simulation sim)
        {
            var lines = _floorGroup.Lines;
            lines.Clear();
            sim.FloorAt(sim.Loop.CurrentNode, _floor);
            foreach (var (item, amount) in _floor)
                lines.Add(new Line { Item = item, Amount = amount });
        }

        /// <summary>Knowledge and progress (Simulation.KnowledgeItems), one line per kind.</summary>
        private void CollectKnown(Simulation sim)
        {
            var lines = _knownGroup.Lines;
            lines.Clear();
            sim.KnowledgeItems(_known);
            foreach (var (item, amount) in _known)
                lines.Add(new Line { Item = item, Amount = amount });
        }

        private static bool Lists(List<Line> lines, ResourceDefinition item)
        {
            foreach (var line in lines)
                if (line.Item == item)
                    return true;
            return false;
        }

        /// <summary>"Candle 6", noting a carried or kept one, and the countdown of one in use from here.</summary>
        private static string ItemText(Simulation sim, Line line)
        {
            var item = line.Item;
            string text = GameText.Get("pockets.chip", ("item", item.DisplayName), ("amount", line.Amount));
            if (item.IsCarried || item.IsKept)
                text = GameText.Get("pockets.chip_lasts", ("chip", text), ("lasts", UiStyle.Aside(ItemTips.LastsName(item))));
            return sim.ContainerFor(item) == null ? WithTimer(sim, text, item) : text;
        }

        private static string FloorText(Line line) =>
            GameText.Get("pockets.chip", ("item", line.Item.DisplayName), ("amount", line.Amount));

        private string ContainerText(Simulation sim, Line line)
        {
            int capacity = sim.ContainerCapacity(line.Item);
            var contents = sim.ContentsOf(line.Item).contents;
            string text;
            if (contents.Count == 0)
                text = GameText.Get("pockets.pouch_empty", ("container", line.Item.DisplayName), ("capacity", capacity));
            // One kind (the usual): its count is how full the container is, so it isn't said twice.
            else if (contents.Count == 1)
                text = GameText.Get("pockets.pouch_line_one", ("container", line.Item.DisplayName),
                    ("item", contents[0].item.DisplayName), ("used", line.Amount), ("capacity", capacity));
            else
            {
                _contents.Clear();
                foreach (var (item, amount) in contents)
                    _contents.Add(GameText.Get("pockets.chip", ("item", item.DisplayName), ("amount", amount)));
                text = GameText.Get("pockets.pouch_line", ("container", line.Item.DisplayName), ("items", UiText.List(_contents)),
                    ("used", line.Amount), ("capacity", capacity));
            }
            // One in use came from here: its countdown shows on the container's line.
            foreach (var held in line.Item.holds)
                if (held != null && sim.ContainerFor(held) == line.Item && sim.RestoringSecondsLeft(held) > 0f)
                    return WithTimer(sim, text, held);
            return text;
        }

        private static string WithTimer(Simulation sim, string text, ResourceDefinition item)
        {
            float usingFor = sim.RestoringSecondsLeft(item);
            if (usingFor <= 0f)
                return text;
            string timer = GameText.Get("pockets.timer", ("seconds", UiText.Countdown(usingFor)));
            return GameText.Get("pockets.in_use", ("chip", text), ("timer", UiStyle.Colour(timer, UiStyle.Levelling)));
        }

        /// <summary>A container is edged in the colour of what it's for (a pouch of phials: restorative).</summary>
        private static ItemKind KindInside(ResourceDefinition container)
        {
            foreach (var held in container.holds)
                if (held != null)
                    return held.kind;
            return container.kind;
        }

        private string TipFor(Group group, int index)
        {
            var sim = Sim;
            if (sim == null || index >= group.Lines.Count)
                return null;
            var line = group.Lines[index];
            if (group == _floorGroup)
                return GameText.Get("tips.pockets_floor_chip", ("item", line.Item.DisplayName),
                    ("amount", line.Amount), ("space", sim.FloorSpace));
            return line.IsContainer ? ItemTips.ContainerTip(line.Item) : ItemTips.ItemTip(sim, line.Item, line.Amount);
        }
    }
}
