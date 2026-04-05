using System;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// An integer value stored as a ScriptableObject asset.
    /// Fires OnValueChanged when the value is modified.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/Int", order = 1)]
    public class IntVariable : ScriptableObject
    {
        [SerializeField] private int _initialValue;
        [SerializeField] private int _value;

        public event Action<int> OnValueChanged;

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void SetValue(int value) => Value = value;
        public void ApplyChange(int amount) => Value += amount;

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
