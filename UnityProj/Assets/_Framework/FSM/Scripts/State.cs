using UnityEngine;
using MiniGameTemplate.Events;

namespace MiniGameTemplate.FSM
{
    /// <summary>
    /// A state in the finite state machine, defined as a ScriptableObject.
    /// Each state can optionally fire GameEvents on enter/exit.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/FSM/State", order = 0)]
    public class State : ScriptableObject
    {
#if UNITY_EDITOR
        [TextArea(2, 4)]
        [SerializeField] private string _description;
#endif

        [Tooltip("Event raised when entering this state.")]
        [SerializeField] private GameEvent _onEnterEvent;

        [Tooltip("Event raised when exiting this state.")]
        [SerializeField] private GameEvent _onExitEvent;

        public GameEvent OnEnterEvent => _onEnterEvent;
        public GameEvent OnExitEvent => _onExitEvent;

        /// <summary>
        /// Called by StateMachine when this state is entered.
        /// </summary>
        public virtual void Enter()
        {
            _onEnterEvent?.Raise();
        }

        /// <summary>
        /// Called by StateMachine when this state is exited.
        /// </summary>
        public virtual void Exit()
        {
            _onExitEvent?.Raise();
        }
    }
}
