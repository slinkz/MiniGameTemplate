using UnityEngine;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Saves high score to local storage when it changes.
    /// Demonstrates ISaveSystem integration with SO Variables.
    /// </summary>
    public class HighScoreSaver : MonoBehaviour
    {
        [SerializeField] private IntVariable _highScore;

        private ISaveSystem _saveSystem;
        private const string HIGH_SCORE_KEY = "example_high_score";

        private void Awake()
        {
            _saveSystem = new PlayerPrefsSaveSystem();
        }

        private void Start()
        {
            // Load saved high score on start
            int saved = _saveSystem.LoadInt(HIGH_SCORE_KEY, 0);
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
            _saveSystem.SaveInt(HIGH_SCORE_KEY, value);
            _saveSystem.Save();
            UnityEngine.Debug.Log($"[HighScoreSaver] High score saved: {value}");
        }
    }
}
