using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// For a list scrolling up and down inside another scrolling list (a stop card's rows inside the
    /// queue drawer): while this one has nothing to scroll, the mouse wheel moves the outer one
    /// instead of being swallowed here.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class NestedScroll : MonoBehaviour, IScrollHandler
    {
        private ScrollRect _inner, _outer;

        private void Awake()
        {
            _inner = GetComponent<ScrollRect>();
            _outer = transform.parent != null ? transform.parent.GetComponentInParent<ScrollRect>(true) : null;
            if (_outer == null)
                Debug.LogError("NestedScroll: no scrolling list above this one to pass the mouse wheel to.", this);
        }

        // Unity gives the wheel to every scroll handler on this object, so the inner list still scrolls itself when it can.
        public void OnScroll(PointerEventData eventData)
        {
            if (_outer != null && !CanScroll)
                _outer.OnScroll(eventData);
        }

        private bool CanScroll => _inner.content != null && _inner.viewport != null &&
                                  _inner.content.rect.height > _inner.viewport.rect.height;
    }
}
