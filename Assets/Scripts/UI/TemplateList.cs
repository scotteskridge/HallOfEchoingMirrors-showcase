using System;
using System.Collections.Generic;
using UnityEngine;

namespace HallOfEchoingMirrors.UI
{
    /// <summary>
    /// Keeps a list of on-screen copies of a template (a bar, a row, a button): hides the template
    /// itself, makes copies as needed, and shows exactly as many as are wanted. Copies are reused,
    /// never thrown away, so nothing is re-created every frame.
    /// </summary>
    public class TemplateList<T> where T : Component
    {
        private readonly T _template;
        private readonly Action<T, int> _onCreate;
        private readonly List<T> _items = new List<T>();

        /// <param name="template">The template in the scene. Copies go next to it, in order.</param>
        /// <param name="onCreate">Optional: runs once per new copy, with its index (e.g. to hook up a click).</param>
        public TemplateList(T template, Action<T, int> onCreate = null)
        {
            _template = template;
            _onCreate = onCreate;
            _template.gameObject.SetActive(false);
        }

        public int Count { get; private set; }

        public T this[int index] => _items[index];

        /// <summary>Shows exactly <paramref name="count"/> copies, making more if needed.</summary>
        public void Show(int count)
        {
            while (_items.Count < count)
                _items.Add(Create(_items.Count));

            for (int i = 0; i < _items.Count; i++)
            {
                bool wanted = i < count;
                if (_items[i].gameObject.activeSelf != wanted)
                    _items[i].gameObject.SetActive(wanted);
            }
            Count = count;
        }

        private T Create(int index)
        {
            var copy = UnityEngine.Object.Instantiate(_template, _template.transform.parent);
            copy.transform.SetSiblingIndex(_template.transform.GetSiblingIndex() + 1 + index);
            copy.gameObject.SetActive(true);
            _onCreate?.Invoke(copy, index);
            return copy;
        }
    }
}
