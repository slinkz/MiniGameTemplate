using System;
using UnityEngine;

namespace MiniGameTemplate.FSM
{
    /// <summary>
    /// SO-driven finite state machine.
    /// States and transitions are ScriptableObject assets configured in Inspector.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private State _initialState;
        [SerializeField] private StateTransition[] _validTransitions;

        private State _currentState;

        public State CurrentState => _currentState;

        /// <summary>
        /// Fired when state changes. Args: (previousState, newState).
        /// </summary>
        public event Action<State, State> OnStateChanged;

        private void Start()
        {
            if (_initialState != null)
            {
                _currentState = _initialState;
                _currentState.Enter();
            }
        }

        /// <summary>
        /// Attempt to transition to a new state.
        /// Only succeeds if a valid StateTransition exists for the current → target pair.
        /// </summary>
        public bool TransitionTo(State targetState)
        {
            if (targetState == null || targetState == _currentState)
                return false;

            if (!IsTransitionValid(targetState))
            {
                Debug.LogWarning($"[FSM] Invalid transition: {_currentState?.name} → {targetState.name}");
                return false;
            }

            var previous = _currentState;
            _currentState?.Exit();
            _currentState = targetState;
            _currentState.Enter();

            OnStateChanged?.Invoke(previous, _currentState);
            Debug.Log($"[FSM] Transition: {previous?.name} → {_currentState.name}");
            return true;
        }

        /// <summary>
        /// Force transition without validation. Use sparingly (e.g., for reset/restart).
        /// </summary>
        public void ForceTransitionTo(State targetState)
        {
            if (targetState == null) return;

            var previous = _currentState;
            _currentState?.Exit();
            _currentState = targetState;
            _currentState.Enter();

            OnStateChanged?.Invoke(previous, _currentState);
        }

        private bool IsTransitionValid(State targetState)
        {
            if (_validTransitions == null || _validTransitions.Length == 0)
                return true; // No restrictions defined — allow all transitions

            foreach (var transition in _validTransitions)
            {
                if (transition.ToState == targetState && transition.IsValid(_currentState))
                    return true;
            }

            return false;
        }
    }
}
