using UnityEngine;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// Displays the countdown timer by listening to a FloatVariable.
    /// Demonstrates SO Variable → UI binding pattern for continuous values.
    /// </summary>
    public class CountdownDisplay : MonoBehaviour
    {
        [SerializeField] private FloatVariable _remainingTime;

        private void OnEnable()
        {
            if (_remainingTime != null)
                _remainingTime.OnValueChanged += UpdateDisplay;
        }

        private void OnDisable()
        {
            if (_remainingTime != null)
                _remainingTime.OnValueChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(float value)
        {
            // TODO: Replace with FairyGUI text update
            // _timerText.text = $"{value:F1}s";
        }
    }
}
