using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Events
{
    /// <summary>
    /// Parameterless event channel implemented as a ScriptableObject.
    /// Create instances via: Create → Events → Game Event.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Events/Game Event", order = 0)]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new List<GameEventListener>();
        private readonly HashSet<GameEventListener> _listenerSet = new HashSet<GameEventListener>();

#if UNITY_EDITOR
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        /// <summary>Editor-only: number of active listeners (for debug tools).</summary>
        public int ListenerCount => _listeners.Count;
#endif

        /// <summary>
        /// Raise this event, notifying all registered listeners.
        /// Iterates in reverse to safely handle listener removal during invocation.
        /// </summary>
        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (_listenerSet.Add(listener)) // O(1) duplicate check
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener listener)
        {
            if (!_listenerSet.Remove(listener)) return; // O(1)

            // Swap-remove: find index, swap with last, RemoveAt end → O(1) total
            int idx = _listeners.IndexOf(listener);
            if (idx >= 0)
            {
                int last = _listeners.Count - 1;
                if (idx != last)
                    _listeners[idx] = _listeners[last];
                _listeners.RemoveAt(last);
            }
        }
    }
}
