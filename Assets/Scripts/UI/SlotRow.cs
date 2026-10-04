using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>One save slot on the menu: what's in it, and New game / Load / Delete buttons.</summary>
    public class SlotRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _deleteButton;

        private TMP_Text _newLabel;
        private TMP_Text _deleteLabel;

        public void Hook(int slot, Action<int> onNew, Action<int> onLoad, Action<int> onDelete)
        {
            _newLabel = _newButton.GetComponentInChildren<TMP_Text>();
            _deleteLabel = _deleteButton.GetComponentInChildren<TMP_Text>();
            _newButton.onClick.AddListener(() => onNew(slot));
            _loadButton.onClick.AddListener(() => onLoad(slot));
            _deleteButton.onClick.AddListener(() => onDelete(slot));
        }

        public void Show(string description, bool canLoad, bool canDelete, string newLabel, string deleteLabel)
        {
            UiText.Set(_label, description);
            _loadButton.interactable = canLoad;
            _deleteButton.interactable = canDelete;
            UiText.Set(_newLabel, newLabel);
            UiText.Set(_deleteLabel, deleteLabel);
        }
    }
}
