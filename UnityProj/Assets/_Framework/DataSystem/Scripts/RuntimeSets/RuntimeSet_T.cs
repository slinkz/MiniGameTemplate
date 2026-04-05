using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// A runtime set that tracks active scene entities without singletons.
    /// Entities register in OnEnable, unregister in OnDisable.
    ///
    /// Performance: Uses HashSet for O(1) Add/Remove duplicate checks while
    /// maintaining a List for ordered iteration (required by IReadOnlyList).
    /// </summary>
    public abstract class RuntimeSet<T> : ScriptableObject
    {
        [System.NonSerialized]
        private readonly List<T> _items = new List<T>();

        [System.NonSerialized]
        private readonly HashSet<T> _itemSet = new HashSet<T>();

        public IReadOnlyList<T> Items => _items;
        public int Count => _items.Count;

        public void Add(T item)
        {
            if (_itemSet.Add(item)) // O(1) duplicate check
                _items.Add(item);
        }

        public void Remove(T item)
        {
            if (_itemSet.Remove(item)) // O(1)
                _items.Remove(item);   // O(n) but only on actual removal
        }

        public void Clear()
        {
            _items.Clear();
            _itemSet.Clear();
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
            _itemSet.Clear();
        }
    }
}
