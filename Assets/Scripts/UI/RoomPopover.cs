using System.Collections.Generic;
using HallOfEchoingMirrors.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Everything about one room, opened over the map by clicking it: its name and search bar, the
    /// actions there (those that can't be done left out, or greyed with the reason), what lies on its
    /// floor with Pick up and Put down, and the ways on. Sits beside the room and follows it as the
    /// map moves; once the player drags it (DragToMove, on the same object), it sits at that place
    /// relative to every room it opens for, remembered between sessions; a double-click puts it
    /// back beside the room. Rows are rebuilt only when what's on offer changes; their
    /// details update every frame. It lives inside the map's window (MapView's Area), above the rooms.
    /// </summary>
    public class RoomPopover : MonoBehaviour, IPointerClickHandler, IScrollHandler
    {
        [SerializeField] private GameController _game;
        [SerializeField] private MapView _map;
        [SerializeField] private TMP_Text _title;
        [Tooltip("The search bar's fill: its width follows how much of the room is searched this run.")]
        [SerializeField] private Image _searchFill;
        [SerializeField] private TMP_Text _searchLabel;
        [Tooltip("The search bar's track and label: hidden for rooms with nothing to search.")]
        [SerializeField] private GameObject _searchBar;
        [Tooltip("Copied once per action here.")]
        [SerializeField] private ActionRow _rowTemplate;
        [Tooltip("Each stat's icon, for the start of each action row (a skill's is on the skill itself).")]
        [SerializeField] private TraitIcons _traitIcons;
        [Tooltip("The On the floor heading, chips and rows: hidden when the floor is empty and there's nothing to put down.")]
        [SerializeField] private GameObject _floorSection;
        [Tooltip("Copied once per kind of item on the floor.")]
        [SerializeField] private Image _floorChipTemplate;
        [Tooltip("Copied once per Pick up or Put down action.")]
        [SerializeField] private ActionRow _floorRowTemplate;
        [Tooltip("The Ways on heading and chips: hidden when no way leads on.")]
        [SerializeField] private GameObject _waysSection;
        [Tooltip("Copied once per way on. Clicking one is the same as clicking that room on the map.")]
        [SerializeField] private Button _wayTemplate;
        [SerializeField] private Button _closeButton;
        [Tooltip("Pixels between the room and the popover.")]
        [SerializeField, Min(0f)] private float _gap = 16f;
        [Tooltip("Off: actions that can't be done this run (done already, one of a kind already held, room fully " +
                 "searched) are left out. On: they're listed greyed, with the reason. Actions only missing " +
                 "something they need are always listed.")]
        [SerializeField] private bool _showUnavailable;

        private NodeDefinition _room;
        // Where it sits: beside its room, or where the player dragged it (measured from the room's centre).
        private PopoverDocker _docker;
        private TemplateList<ActionRow> _rows, _floorRows;
        private TemplateList<Image> _floorChips;
        private TemplateList<Button> _ways;

        private readonly List<ActionOffer> _offers = new List<ActionOffer>();
        private readonly List<ActionOffer> _here = new List<ActionOffer>();
        private readonly List<ActionOffer> _floorActions = new List<ActionOffer>();
        private readonly List<TaskDefinition> _shownHere = new List<TaskDefinition>();
        private readonly List<TaskDefinition> _shownFloorActions = new List<TaskDefinition>();
        private readonly List<(ResourceDefinition item, int amount)> _floor = new List<(ResourceDefinition, int)>();
        private readonly List<(ResourceDefinition item, int amount)> _known = new List<(ResourceDefinition, int)>();
        private readonly List<NodeDefinition> _shownWays = new List<NodeDefinition>();
        // Each chip's words, found once when the chip is made (not every frame).
        private readonly List<TMP_Text> _floorChipLabels = new List<TMP_Text>();
        private readonly List<TMP_Text> _wayLabels = new List<TMP_Text>();
        private readonly List<NodeDefinition> _destinations = new List<NodeDefinition>();
        // The strings that need working out (details, chip words), kept between refreshes.
        private readonly List<string> _hereDetails = new List<string>();
        private readonly List<string> _floorActionDetails = new List<string>();
        private float _nextTexts; // text is rebuilt a few times a second (or when what's listed changes), not every frame
        private const float TextSeconds = 0.2f;
        private NodeDefinition _shownRoom;
        // Where Schedule would put an action here, worked out once a frame in LateUpdate.
        private ScheduleTarget _target;

        private Simulation Sim => _game.Simulation;

        /// <summary>The room it's open for; null while closed.</summary>
        public NodeDefinition Room => _room;

        private void Awake()
        {
            _rows = new TemplateList<ActionRow>(_rowTemplate);
            _floorRows = new TemplateList<ActionRow>(_floorRowTemplate);
            _floorChips = new TemplateList<Image>(_floorChipTemplate, (chip, index) =>
            {
                _floorChipLabels.Add(chip.GetComponentInChildren<TMP_Text>());
                ToolTip.On(chip, () => GameText.Get("popover.tip.floor_chip"));
            });
            _ways = new TemplateList<Button>(_wayTemplate, (button, index) =>
            {
                _wayLabels.Add(button.GetComponentInChildren<TMP_Text>());
                button.onClick.AddListener(() => _map.ClickRoom(_shownWays[index]));
            });
            _closeButton.onClick.AddListener(Close);
            // On the track: the bar's frame has no image, so it would never notice the pointer.
            ToolTip.On(_searchFill.transform.parent, () => GameText.Get("popover.tip.searched"));
            // Says something only while the title shows knowledge found here; text must catch the pointer to have a tip.
            _title.raycastTarget = true;
            ToolTip.On(_title, () => (_known.Count > 0 ? GameText.Get("popover.tip.known") + "\n" : "") + GameText.Get("popover.tip.by_heart"));
            GameText.Changed += ForceRebuild; // new words: new rows
            _docker = new PopoverDocker(this, _map, () => _room);
            if (_room == null)
                gameObject.SetActive(false);
        }

        private void OnDestroy() => GameText.Changed -= ForceRebuild;

        private void ForceRebuild() => _shownRoom = null;

        public void Open(NodeDefinition room)
        {
            _room = room;
            _shownRoom = null;
            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // above the rooms and lines
        }

        public void Close()
        {
            _room = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate() // after MapView has moved the map this frame, so it keeps up with the room
        {
            var sim = Sim;
            var roomRect = sim != null && sim.HasPlaces && _room != null ? _map.RoomRect(_room) : null;
            if (roomRect == null)
            {
                Close(); // a new or loaded game, or the room is no longer on the map
                return;
            }

            CollectOffers(sim);
            _target = sim.WhereScheduleLands(_room);
            bool rebuilt = _room != _shownRoom || !SameTasks(_here, _shownHere) || !SameTasks(_floorActions, _shownFloorActions);
            if (rebuilt)
                Rebuild(sim);
            bool words = rebuilt || Time.unscaledTime >= _nextTexts;
            if (words)
                _nextTexts = Time.unscaledTime + TextSeconds;
            // Sections are shown or hidden before their rows are refreshed.
            ShowSearch(sim, words);
            ShowFloor(sim, words);
            ShowWays(sim, words);
            RefreshRows(sim, words);
            _docker.PlaceBeside(roomRect, _gap);
        }

        // ---------- Actions ----------

        private void CollectOffers(Simulation sim)
        {
            sim.OffersAt(_room, _offers);
            _here.Clear();
            _floorActions.Clear();
            foreach (var offer in _offers)
                if (_showUnavailable || !offer.Blocked)
                    (offer.Task.picksUp != null || offer.Task.putsDown != null ? _floorActions : _here).Add(offer);
        }

        private void Rebuild(Simulation sim)
        {
            _shownRoom = _room;
            Fill(sim, _rows, _here, _shownHere);
            Fill(sim, _floorRows, _floorActions, _shownFloorActions);
        }

        private void Fill(Simulation sim, TemplateList<ActionRow> rows, List<ActionOffer> offers, List<TaskDefinition> shown)
        {
            shown.Clear();
            rows.Show(offers.Count);
            for (int i = 0; i < offers.Count; i++)
            {
                var task = offers[i].Task;
                shown.Add(task);
                // Lambdas look the game up on every click, so rows survive loading another game.
                rows[i].Setup(ActionText.NameOf(sim, task, null, _room),
                    () => Sim.PlayIn(task, _room), () => Sim.ScheduleIn(task, _room),
                    () => Sim == null || _room == null ? null : ActionText.Layout(Sim, task, null, _room),
                    () => RoomPopoverText.PlayTip(Sim, _room, _target), () => RoomPopoverText.ScheduleTip(_target),
                    sim.CanCarry(task) ? () => Sim.CarryIn(task, _room) : (System.Action)null,
                    () => RoomPopoverText.CarryTip(Sim, _room, _target));
                ShowTraits(sim, rows[i], task);
            }
        }

        /// <summary>The row's icons: the skill that speeds it up and the stat it trains, each naming itself when hovered.</summary>
        private void ShowTraits(Simulation sim, ActionRow row, TaskDefinition task)
        {
            var skill = task.skill;
            var stat = sim.StatTrainedBy(task);
            row.ShowTraits(
                skill != null ? skill.Icon : null,
                skill != null ? () => Sim == null ? null : ClaraTips.SkillBrief(Sim, skill) : (System.Func<string>)null,
                _traitIcons.IconOf(stat),
                stat != ClaraAttribute.None ? () => Sim == null ? null : ClaraTips.StatBrief(Sim, stat) : (System.Func<string>)null);
        }

        private void RefreshRows(Simulation sim, bool words)
        {
            if (words)
            {
                CollectDetails(sim, _here, _hereDetails);
                CollectDetails(sim, _floorActions, _floorActionDetails);
            }
            // Play, Schedule and Carry work in any room she can reach (Core decides where they land).
            bool canReach = _target.CanSchedule;
            bool canCarry = canReach;
            // Play starts an action now: between runs nothing is running, so the plan is built with Schedule.
            bool canPlay = canReach && sim.RunUnderWay;
            for (int i = 0; i < _here.Count; i++)
                _rows[i].Refresh(_hereDetails[i], canPlay, canReach, _here[i].Reason, _here[i].Blocked, canCarry);
            for (int i = 0; i < _floorActions.Count; i++)
                _floorRows[i].Refresh(_floorActionDetails[i], canPlay, canReach,
                    _floorActions[i].Reason, _floorActions[i].Blocked, canCarry);
        }

        private void CollectDetails(Simulation sim, List<ActionOffer> offers, List<string> details)
        {
            details.Clear();
            foreach (var offer in offers)
                details.Add(ActionText.Details(sim, offer.Task, null, _room));
        }

        // ---------- Header, floor, ways ----------

        private void ShowSearch(Simulation sim, bool words)
        {
            if (words)
                UiText.Set(_title, RoomPopoverText.Title(sim, _room, _target, _known));
            bool searchable = sim.HasSomethingToExplore(_room);
            if (_searchBar.activeSelf != searchable)
                _searchBar.SetActive(searchable);
            if (!searchable)
                return;
            float fraction = sim.ExploredFraction(_room);
            ((RectTransform)_searchFill.transform).anchorMax = new Vector2(fraction, 1f);
            _searchFill.color = _map.Style.ExploreColour(fraction);
            if (words)
                UiText.Set(_searchLabel, GameText.Get("popover.searched",
                    ("done", UiText.Whole(Mathf.Floor(sim.ExploresDoneIn(_room)))), ("total", _room.exploresToFill)));
        }

        private void ShowFloor(Simulation sim, bool words)
        {
            sim.FloorAt(_room, _floor);
            // New chips get their words at once, not at the next refresh.
            words |= _floor.Count != _floorChips.Count;
            _floorChips.Show(_floor.Count);
            for (int i = 0; words && i < _floor.Count; i++)
            {
                var (item, amount) = _floor[i];
                UiText.Set(_floorChipLabels[i], GameText.Get("popover.floor_chip",
                    ("amount", amount), ("item", item.DisplayName), ("max", sim.FloorSpace)));
            }
            bool show = _floor.Count > 0 || _floorActions.Count > 0;
            if (_floorSection.activeSelf != show)
                _floorSection.SetActive(show);
        }

        private void ShowWays(Simulation sim, bool words)
        {
            sim.DestinationsFrom(_room, _destinations);
            bool changed = _destinations.Count != _shownWays.Count;
            for (int i = 0; !changed && i < _destinations.Count; i++)
                changed = _destinations[i] != _shownWays[i];
            if (changed)
            {
                words = true;
                _shownWays.Clear();
                _shownWays.AddRange(_destinations);
                _ways.Show(_shownWays.Count);
                for (int i = 0; i < _shownWays.Count; i++)
                {
                    var to = _shownWays[i];
                    ToolTip.On(_ways[i], () => Sim == null || _room == null ? null : ActionText.Layout(Sim, Sim.TravelVerb, to, _room));
                }
            }
            for (int i = 0; words && i < _shownWays.Count; i++)
                UiText.Set(_wayLabels[i], GameText.Get("popover.way_chip",
                    ("room", _shownWays[i].DisplayName), ("details", ActionText.Details(sim, sim.TravelVerb, _shownWays[i], _room))));
            bool show = _shownWays.Count > 0 && sim.TravelVerb != null;
            if (_waysSection.activeSelf != show)
                _waysSection.SetActive(show);
        }

        // Clicks and the wheel over the popover stay with it: the map underneath would otherwise
        // take them (closing the popover, or zooming the map).
        public void OnPointerClick(PointerEventData eventData) { }
        public void OnScroll(PointerEventData eventData) { }

        private static bool SameTasks(List<ActionOffer> offers, List<TaskDefinition> shown)
        {
            if (offers.Count != shown.Count)
                return false;
            for (int i = 0; i < offers.Count; i++)
                if (offers[i].Task != shown[i])
                    return false;
            return true;
        }
    }
}
