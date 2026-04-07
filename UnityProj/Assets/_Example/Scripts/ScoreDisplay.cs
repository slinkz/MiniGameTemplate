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

        // This sample keeps rendering-agnostic behavior: if you later bind to FairyGUI,
        // replace GameLog with concrete text assignment.

        private void OnEnable()
        {
            if (_score != null)
            {
                _score.OnValueChanged += UpdateDisplay;
                UpdateDisplay(_score.Value);
            }
        }


        private void OnDisable()
        {
            if (_score != null)
                _score.OnValueChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(int value)
        {
            GameLog.Log($"[ScoreDisplay] Score: {value}");
        }

    }
}
