using System;
using System.Globalization;
using HallOfEchoingMirrors.Core;
using HallOfEchoingMirrors.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// The Menu page: three save slots (New game / Load / Delete), Save now, and room for
    /// Settings and Achievements. Anything that throws progress away asks for a second click.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private ScreenManager _screens;
        [Tooltip("Copied once per save slot.")]
        [SerializeField] private SlotRow _slotTemplate;
        [SerializeField] private Button _saveNowButton;
        [SerializeField] private TMP_Text _status;
        [Header("Coming later")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _achievementsButton;

        [Tooltip("New game over a save, and Delete, ask to be clicked again within this many seconds.")]
        [SerializeField, Min(0.5f)] private float _confirmSeconds = 3f;
        private static string ConfirmText => GameText.Get("common.sure");

        private enum PendingAction { None, NewGame, Delete }

        private TemplateList<SlotRow> _rows;

        /// <summary>What a slot's file held when last read: its save, or why it couldn't be read.</summary>
        private struct SlotInfo
        {
            public SaveData Save;
            public string ReadError; // null for an empty slot or one that read fine
            // A save that can't be read (damaged, or from a newer version) still fills its slot: it may
            // load again after an update, so it's asked about before being overwritten and can be deleted.
            public bool HasFile => Save != null || ReadError != null;
        }

        private readonly SlotInfo[] _slots = new SlotInfo[SaveSlots.SlotCount];

        private PendingAction _pending;
        private int _pendingSlot = -1;
        private float _pendingUntil;

        // The slot lines only change when the saves, a pending confirmation, the game being played
        // or the words do.
        private bool _dirty = true;
        // Saves are written often (every action); the files are only read while the Menu is open.
        private bool _slotsStale = true;
        private int _shownActiveSlot = -2;

        private void Start()
        {
            _rows = new TemplateList<SlotRow>(_slotTemplate,
                (row, slot) => row.Hook(slot, OnNewGame, OnLoad, OnDelete));
            _rows.Show(SaveSlots.SlotCount);

            _saveNowButton.onClick.AddListener(() =>
            {
                // A failed save sets its own status, through SaveFailed.
                if (_game.SaveNow())
                    SetStatus(GameText.Get("menu.saved", ("slot", _game.ActiveSlot + 1)));
            });

            // Placeholders until settings and achievements exist.
            if (_settingsButton != null)
                _settingsButton.interactable = false;
            if (_achievementsButton != null)
                _achievementsButton.interactable = false;

            _game.SlotsChanged += MarkSlotsStale;
            _game.SaveFailed += SetStatus; // an autosave that fails shows here too, for when the player next looks
            GameText.Changed += MarkDirty;
            ReadSlots();
            SetStatus(GameText.Get("menu.choose_slot"));
        }

        private void OnDestroy()
        {
            if (_game != null)
            {
                _game.SlotsChanged -= MarkSlotsStale;
                _game.SaveFailed -= SetStatus;
            }
            GameText.Changed -= MarkDirty;
        }

        private void MarkDirty() => _dirty = true;

        private void MarkSlotsStale() => _slotsStale = true;

        private void Update()
        {
            if (_pending != PendingAction.None && Time.unscaledTime > _pendingUntil)
                ClearPending();

            _saveNowButton.interactable = _game.HasActiveGame;
            if (_slotsStale && _screens.Current == ScreenId.Menu)
                ReadSlots();
            if (!_dirty && _shownActiveSlot == _game.ActiveSlot)
                return;
            _dirty = false;
            _shownActiveSlot = _game.ActiveSlot;
            for (int slot = 0; slot < SaveSlots.SlotCount; slot++)
            {
                _rows[slot].Show(Describe(slot), _slots[slot].Save != null, _slots[slot].HasFile,
                    IsPending(PendingAction.NewGame, slot) ? ConfirmText : GameText.Get("menu.new_game"),
                    IsPending(PendingAction.Delete, slot) ? ConfirmText : GameText.Get("menu.delete"));
            }
        }

        // ---------- Buttons ----------

        // A new game goes straight into its first run (paused until something is queued). A loaded game
        // is shown where it was: a run under way on the main page, one between runs on the planning
        // screen, so its saved plan can be looked at before Begin. Before she has earned planning there
        // is no planning screen to show, so a game loaded between runs begins its next run as well.
        private void Show(bool beginFirstRun)
        {
            bool begin = beginFirstRun || _game.Simulation.NextRunBeginsAtOnce;
            if (begin && _game.Simulation.Phase != LoopPhase.Running)
                _game.BeginLoop();
            _screens.ShowGame();
        }

        private void OnNewGame(int slot)
        {
            // Starting over a saved game throws it away, so ask first.
            if (_slots[slot].HasFile && !IsPending(PendingAction.NewGame, slot))
            {
                AskToConfirm(PendingAction.NewGame, slot);
                return;
            }

            ClearPending();
            _game.NewGame(slot);
            SetStatus(GameText.Get("menu.started", ("slot", slot + 1)));
            Show(beginFirstRun: true);
        }

        private void OnLoad(int slot)
        {
            ClearPending();
            if (_game.LoadGame(slot, out string error))
            {
                SetStatus(GameText.Get("menu.loaded", ("slot", slot + 1)));
                Show(beginFirstRun: false);
            }
            else
            {
                SetStatus(GameText.Get("menu.couldnt_load", ("slot", slot + 1), ("error", error)));
            }
        }

        private void OnDelete(int slot)
        {
            if (!IsPending(PendingAction.Delete, slot))
            {
                AskToConfirm(PendingAction.Delete, slot);
                return;
            }

            ClearPending();
            if (_game.DeleteSlot(slot, out string error))
                SetStatus(GameText.Get("menu.deleted", ("slot", slot + 1)));
            else
                SetStatus(GameText.Get("menu.couldnt_delete", ("slot", slot + 1), ("error", error)));
        }

        // ---------- Helpers ----------

        private void ReadSlots()
        {
            _dirty = true;
            _slotsStale = false;
            for (int slot = 0; slot < SaveSlots.SlotCount; slot++)
            {
                var save = SaveSlots.Read(slot, out string error);
                _slots[slot] = new SlotInfo { Save = save, ReadError = SaveSlots.Exists(slot) ? error : null };
            }
        }

        private string Describe(int slot)
        {
            string name = GameText.Get("menu.slot", ("slot", slot + 1));
            if (slot == _game.ActiveSlot)
                name += " " + UiStyle.Colour(GameText.Get("menu.playing"), UiStyle.Milestone);

            var save = _slots[slot].Save;
            if (save == null)
                return GameText.Get("menu.slot_line", ("name", name), ("details", _slots[slot].ReadError != null
                    ? UiStyle.Colour(GameText.Get("menu.unreadable", ("error", _slots[slot].ReadError)), UiStyle.Warning)
                    : UiStyle.Colour(GameText.Get("menu.empty"), UiStyle.Muted)));

            string loops = save.loopsCompleted == 1 ? GameText.Get("menu.loops.one") : GameText.Get("menu.loops.many", ("count", save.loopsCompleted));
            string passages = save.journal.Count == 1 ? GameText.Get("menu.passages.one") : GameText.Get("menu.passages.many", ("count", save.journal.Count));
            return GameText.Get("menu.slot_line", ("name", name), ("details", GameText.Get("menu.slot_details", ("loops", loops), ("passages", passages))))
                + "\n" + UiStyle.Aside(GameText.Get("menu.saved_at", ("when", SavedAt(save))));
        }

        private static string SavedAt(SaveData save) =>
            DateTime.TryParse(save.savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var when)
                ? when.ToLocalTime().ToString(GameText.Get("menu.date_format"), CultureInfo.CurrentCulture)
                : GameText.Get("menu.unknown_time");

        private bool IsPending(PendingAction action, int slot) => _pending == action && _pendingSlot == slot;

        private void AskToConfirm(PendingAction action, int slot)
        {
            _pending = action;
            _pendingSlot = slot;
            _pendingUntil = Time.unscaledTime + _confirmSeconds;
            _dirty = true;
            SetStatus(action == PendingAction.NewGame
                ? GameText.Get("menu.confirm_new", ("slot", slot + 1))
                : GameText.Get("menu.confirm_delete", ("slot", slot + 1)));
        }

        private void ClearPending()
        {
            _pending = PendingAction.None;
            _pendingSlot = -1;
            _dirty = true;
        }

        private void SetStatus(string message)
        {
            if (_status != null)
                _status.text = message;
        }
    }
}
