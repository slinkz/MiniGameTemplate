using System;
using System.Collections.Generic;
using UnityEngine;
using MiniGameTemplate.Utils;

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

        // Pre-built lookup for O(1) transition validation.
        // Key: (fromState, toState). null fromState = wildcard ("any state").
        private HashSet<(State, State)> _transitionLookup;
        private bool _hasWildcardTransitions;

        public State CurrentState => _currentState;

        /// <summary>
        /// Fired when state changes. Args: (previousState, newState).
        /// </summary>
        public event Action<State, State> OnStateChanged;

        private void Start()
        {
            BuildTransitionLookup();

            if (_initialState != null)
            {
                _currentState = _initialState;
                _currentState.Enter();
            }
        }

        /// <summary>
        /// Build a HashSet of valid (from, to) pairs for O(1) lookup.
        /// </summary>
        private void BuildTransitionLookup()
        {
            if (_validTransitions == null || _validTransitions.Length == 0)
            {
                _transitionLookup = null; // null means "allow all"
                return;
            }

            _transitionLookup = new HashSet<(State, State)>(_validTransitions.Length);
            _hasWildcardTransitions = false;

            foreach (var t in _validTransitions)
            {
                if (t == null) continue;
                _transitionLookup.Add((t.FromState, t.ToState));
                if (t.FromState == null)
                    _hasWildcardTransitions = true;
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
                GameLog.LogWarning($"[FSM] Invalid transition: {_currentState?.name} → {targetState.name}");
                return false;
            }

            var previous = _currentState;
            _currentState?.Exit();
            _currentState = targetState;
            _currentState.Enter();

            OnStateChanged?.Invoke(previous, _currentState);
            GameLog.Log($"[FSM] Transition: {previous?.name} → {_currentState.name}");
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
            // No restrictions defined — allow all transitions
            if (_transitionLookup == null)
                return true;

            // O(1) exact match: (currentState → targetState)
            if (_transitionLookup.Contains((_currentState, targetState)))
                return true;

            // O(1) wildcard match: (null → targetState) means "any state → target"
            if (_hasWildcardTransitions && _transitionLookup.Contains((null, targetState)))
                return true;

            return false;
        }
    }
}
