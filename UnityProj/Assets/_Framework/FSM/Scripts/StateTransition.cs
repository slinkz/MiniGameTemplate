using UnityEngine;

namespace MiniGameTemplate.FSM
{
    /// <summary>
    /// Defines a valid transition between two states.
    /// Used to enforce transition rules — StateMachine only allows transitions
    /// that have a matching StateTransition asset.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/FSM/State Transition", order = 1)]
    public class StateTransition : ScriptableObject
    {
        [SerializeField] private State _fromState;
        [SerializeField] private State _toState;

        public State FromState => _fromState;
        public State ToState => _toState;

        public bool IsValid(State currentState)
        {
            // null fromState means "any state" — wildcard transition
            return _fromState == null || _fromState == currentState;
        }
    }
}
