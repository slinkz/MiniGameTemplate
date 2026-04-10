using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;
using MiniGameTemplate.Events;
using MiniGameTemplate.FSM;
using MiniGameTemplate.Timing;
using MiniGameTemplate.Platform;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Example game manager for the ClickCounter demo.
    /// Orchestrates game flow by connecting FSM states with game events.
    /// </summary>
    public class ClickGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private float _gameDuration = 10f;

        [Header("State Machine")]
        [SerializeField] private StateMachine _stateMachine;
        [SerializeField] private State _menuState;
        [SerializeField] private State _playingState;
        [SerializeField] private State _gameOverState;

        [Header("Data")]
        [SerializeField] private IntVariable _playerScore;
        [SerializeField] private IntVariable _highScore;
        [SerializeField] private FloatVariable _remainingTime;

        [Header("Events")]
        [SerializeField] private GameEvent _onGameStart;
        [SerializeField] private GameEvent _onGameOver;

        private const string HIGH_SCORE_KEY = "example_high_score";

        private TimerHandle _countdownTimer = TimerHandle.Invalid;

        private void Start()
        {
            // Load high score from local storage via shared SaveSystem
            _highScore.SetValue(GameBootstrapper.SaveSystem.LoadInt(HIGH_SCORE_KEY, 0));
        }

        /// <summary>
        /// Called when the player presses "Start Game".
        /// Wire to a UI button or GameEventListener.
        /// </summary>
        public void StartGame()
        {
            _playerScore.ResetToInitial();
            _remainingTime.SetValue(_gameDuration);

            _stateMachine.TransitionTo(_playingState);
            _onGameStart?.Raise();

            // Start countdown
            _countdownTimer = TimerService.Instance.Repeat(0.1f, () =>
            {
                _remainingTime.ApplyChange(-0.1f);

                if (_remainingTime.Value <= 0f)
                {
                    _remainingTime.SetValue(0f);
                    EndGame();
                }
            });
        }

        /// <summary>
        /// Called when a click/tap is registered during gameplay.
        /// </summary>
        public void OnClick()
        {
            if (_stateMachine.CurrentState != _playingState) return;
            _playerScore.ApplyChange(1);
        }

        /// <summary>
        /// End the game and check for high score.
        /// </summary>
        private void EndGame()
        {
            TimerService.Instance.Cancel(_countdownTimer);
            _stateMachine.TransitionTo(_gameOverState);
            _onGameOver?.Raise();

            // Check and save high score
            if (_playerScore.Value > _highScore.Value)
            {
                _highScore.SetValue(_playerScore.Value);
                GameBootstrapper.SaveSystem.SaveInt(HIGH_SCORE_KEY, _highScore.Value);
                GameBootstrapper.SaveSystem.Save();
            }
        }

        /// <summary>
        /// Restart the game. Wire to "Play Again" button.
        /// </summary>
        public void RestartGame()
        {
            StartGame();
        }

        /// <summary>
        /// Return to menu. Wire to "Back to Menu" button.
        /// </summary>
        public void ReturnToMenu()
        {
            TimerService.Instance.Cancel(_countdownTimer);
            _stateMachine.ForceTransitionTo(_menuState);
        }

        /// <summary>
        /// Share score to WeChat. Wire to "Share" button.
        /// </summary>
        public void ShareScore()
        {
            var wx = WeChatBridgeFactory.Create();
            wx.Share($"I scored {_playerScore.Value} points!", "", $"score={_playerScore.Value}");
        }
    }
}
