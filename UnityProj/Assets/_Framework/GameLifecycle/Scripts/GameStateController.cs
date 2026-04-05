using UnityEngine;
using MiniGameTemplate.FSM;
using MiniGameTemplate.Events;

namespace MiniGameTemplate.Core
{
    /// <summary>
    /// Bridges the FSM with game lifecycle events.
    /// Listens to GameEvents and triggers state transitions accordingly.
    /// </summary>
    public class GameStateController : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private StateMachine _stateMachine;

        [Header("States")]
        [SerializeField] private State _menuState;
        [SerializeField] private State _playingState;
        [SerializeField] private State _pausedState;
        [SerializeField] private State _gameOverState;

        /// <summary>
        /// Transition to playing state. Wire to OnGameStart event in Inspector.
        /// </summary>
        public void StartGame()
        {
            _stateMachine.TransitionTo(_playingState);
        }

        /// <summary>
        /// Transition to paused state. Wire to OnGamePause event.
        /// </summary>
        public void PauseGame()
        {
            _stateMachine.TransitionTo(_pausedState);
        }

        /// <summary>
        /// Resume from paused → playing. Wire to OnGameResume event.
        /// </summary>
        public void ResumeGame()
        {
            _stateMachine.TransitionTo(_playingState);
        }

        /// <summary>
        /// Transition to game over state. Wire to OnGameOver event.
        /// </summary>
        public void GameOver()
        {
            _stateMachine.TransitionTo(_gameOverState);
        }

        /// <summary>
        /// Return to menu. Resets game state.
        /// </summary>
        public void ReturnToMenu()
        {
            _stateMachine.ForceTransitionTo(_menuState);
        }
    }
}
