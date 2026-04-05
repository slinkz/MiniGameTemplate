using System;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/Bool", order = 3)]
    public class BoolVariable : ScriptableObject
    {
        [SerializeField] private bool _initialValue;
        [SerializeField] private bool _value;

        public event Action<bool> OnValueChanged;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void SetValue(bool value) => Value = value;
        public void Toggle() => Value = !_value;
        public void ResetToInitial() => Value = _initialValue;

        private void OnEnable()
        {
            _value = _initialValue;
        }
    }
}
