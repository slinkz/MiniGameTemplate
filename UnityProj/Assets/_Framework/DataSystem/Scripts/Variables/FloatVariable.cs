using System;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// A float value stored as a ScriptableObject asset.
    /// Fires OnValueChanged when the value is modified.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/Float", order = 0)]
    public class FloatVariable : ScriptableObject
    {
        [SerializeField] private float _initialValue;
        [SerializeField] private float _value;

        public event Action<float> OnValueChanged;

        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void SetValue(float value) => Value = value;
        public void ApplyChange(float amount) => Value += amount;

        /// <summary>
        /// Reset to the initial value defined in the Inspector.
        /// Call this on game start / scene load to ensure clean state.
        /// </summary>
        public void ResetToInitial() => Value = _initialValue;

        private void OnEnable()
        {
            _value = _initialValue;
        }

#if UNITY_EDITOR
        [ContextMenu("Reset to Initial Value")]
        private void EditorReset() => ResetToInitial();
#endif
    }
}
