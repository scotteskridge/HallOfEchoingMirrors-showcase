using System;
using HallOfEchoingMirrors.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Put this on any UI element to give it a hover pop-up. The text is either a line from
    /// game_text.txt (set in the Inspector) or live text from code (<see cref="Text"/>), which is
    /// asked again every frame while the pop-up is showing, so numbers stay current.
    /// The element (or one of its children) needs something the mouse can hit, e.g. an Image or text.
    /// </summary>
    public class ToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("A key in game_text.txt, e.g. tips.vitality. Ignored when code supplies the text.")]
        [SerializeField] private string _textKey;

        /// <summary>Live text from code. Takes priority over the text key.</summary>
        public Func<string> Text { get; set; }

        /// <summary>
        /// A pop-up in the new shape (kicker, title, rows, footer), asked again every frame like <see cref="Text"/>.
        /// Takes priority over the text. It sits beside this element, never over it.
        /// </summary>
        public Func<TipLayout> Layout { get; set; }

        /// <summary>The layout the pop-up should show right now, or null if this tip is plain text.</summary>
        public TipLayout CurrentLayout => Layout?.Invoke();

        /// <summary>What the pop-up should say right now, or null for nothing.</summary>
        public string Current =>
            Text != null ? Text() :
            !string.IsNullOrEmpty(_textKey) ? GameText.Get(_textKey) :
            null;

        /// <summary>Gives a UI element a hover pop-up with live text (adding the component if needed).</summary>
        public static ToolTip On(Component target, Func<string> text)
        {
            var tip = TipOn(target);
            tip.Text = text;
            tip.Layout = null; // a reused row may have had a layout, which would win
            return tip;
        }

        /// <summary>Gives a UI element a hover pop-up in the new shape (adding the component if needed).</summary>
        public static ToolTip On(Component target, Func<TipLayout> layout)
        {
            var tip = TipOn(target);
            tip.Layout = layout;
            tip.Text = null;
            return tip;
        }

        private static ToolTip TipOn(Component target)
        {
            var tip = target.GetComponent<ToolTip>();
            return tip != null ? tip : target.gameObject.AddComponent<ToolTip>();
        }

        // No pop-ups while dragging (e.g. a queue row): they'd cover where it's being dropped.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!eventData.dragging)
                ToolTipPanel.Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) => ToolTipPanel.Hide(this);
        private void OnDisable() => ToolTipPanel.Hide(this);
    }
}
