using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Events
{
    /// <summary>
    /// Generic event channel that carries a payload of type T.
    /// Subclass this to create typed event channels (IntGameEvent, FloatGameEvent, etc.).
    /// </summary>
    public abstract class GameEvent<T> : ScriptableObject
    {
        private readonly List<GameEventListener<T>> _listeners = new List<GameEventListener<T>>();
        private readonly HashSet<GameEventListener<T>> _listenerSet = new HashSet<GameEventListener<T>>();

#if UNITY_EDITOR
        [TextArea(2, 4)]
        [SerializeField] private string _description;
#endif

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(GameEventListener<T> listener)
        {
            if (_listenerSet.Add(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener<T> listener)
        {
            if (!_listenerSet.Remove(listener)) return;

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
