using UnityEngine;
using UnityEngine.Events;

namespace MiniGameTemplate.Events
{
    /// <summary>
    /// Generic event listener base for typed events.
    /// Subclass with concrete types for Inspector support.
    /// </summary>
    public abstract class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] private GameEvent<T> _event;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEnable()
        {
            if (_event != null)
                _event.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (_event != null)
                _event.UnregisterListener(this);
        }

        public void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }
}
