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

#if UNITY_EDITOR
        [TextArea(2, 4)]
        [SerializeField] private string _description;
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
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}
