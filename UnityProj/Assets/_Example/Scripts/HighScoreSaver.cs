using UnityEngine;
using MiniGameTemplate.Core;
using MiniGameTemplate.Data;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Saves high score to local storage when it changes.
    /// Demonstrates ISaveSystem integration with SO Variables.
    /// Uses the shared GameBootstrapper.SaveSystem instance.
    /// </summary>
    public class HighScoreSaver : MonoBehaviour
    {
        [SerializeField] private IntVariable _highScore;

        private const string HIGH_SCORE_KEY = "example_high_score";

        private void Start()
        {
            // Load saved high score on start
            int saved = GameBootstrapper.SaveSystem.LoadInt(HIGH_SCORE_KEY, 0);
            _highScore.SetValue(saved);
        }

        private void OnEnable()
        {
            if (_highScore != null)
                _highScore.OnValueChanged += OnHighScoreChanged;
        }

        private void OnDisable()
        {
            if (_highScore != null)
                _highScore.OnValueChanged -= OnHighScoreChanged;
        }

        private void OnHighScoreChanged(int value)
        {
            GameBootstrapper.SaveSystem.SaveInt(HIGH_SCORE_KEY, value);
            GameBootstrapper.SaveSystem.Save();
            GameLog.Log($"[HighScoreSaver] High score saved: {value}");
        }
    }
}
