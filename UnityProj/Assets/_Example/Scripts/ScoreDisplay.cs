using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Utils;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Displays the current score by listening to an IntVariable's change event.
    /// Demonstrates SO Variable → UI binding pattern.
    /// </summary>
    public class ScoreDisplay : MonoBehaviour
    {
        [SerializeField] private IntVariable _score;

        // In a real project, this would update a FairyGUI text component.
        // For demonstration, we use GameLog (stripped in release builds).

        private void OnEnable()
        {
            if (_score != null)
                _score.OnValueChanged += UpdateDisplay;
        }

        private void OnDisable()
        {
            if (_score != null)
                _score.OnValueChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(int value)
        {
            // TODO: Replace with FairyGUI text update
            // _scoreText.text = value.ToString();
            GameLog.Log($"[ScoreDisplay] Score: {value}");
        }
    }
}
