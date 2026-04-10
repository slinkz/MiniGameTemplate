using UnityEngine;
using MiniGameTemplate.Data;
using MiniGameTemplate.Utils;


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
            {
                _remainingTime.OnValueChanged += UpdateDisplay;
                UpdateDisplay(_remainingTime.Value);
            }
        }


        private void OnDisable()
        {
            if (_remainingTime != null)
                _remainingTime.OnValueChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(float value)
        {
            GameLog.Log($"[CountdownDisplay] Remaining: {value:F1}s");
        }

    }
}
