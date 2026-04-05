using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// A runtime set that tracks active scene entities without singletons.
    /// Entities register in OnEnable, unregister in OnDisable.
    /// </summary>
    public abstract class RuntimeSet<T> : ScriptableObject
    {
        [System.NonSerialized]
        private readonly List<T> _items = new List<T>();

        public IReadOnlyList<T> Items => _items;
        public int Count => _items.Count;

        public void Add(T item)
        {
            if (!_items.Contains(item))
                _items.Add(item);
        }

        public void Remove(T item)
        {
            _items.Remove(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// Get the first item, or default if empty.
        /// Useful for "find the player" pattern without FindObjectOfType.
        /// </summary>
        public T GetFirst()
        {
            return _items.Count > 0 ? _items[0] : default;
        }

        private void OnDisable()
        {
            _items.Clear();
        }
    }
}
