using UnityEngine;
using UnityEngine.Events;

namespace MiniGameTemplate.Events
{
    /// <summary>
    /// Listens to a GameEvent SO and invokes a UnityEvent response.
    /// Attach to any GameObject, assign the event in Inspector.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("The GameEvent SO to listen to.")]
        [SerializeField] private GameEvent _event;

        [Tooltip("Response to invoke when the event is raised.")]
        [SerializeField] private UnityEvent _response;

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

        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
