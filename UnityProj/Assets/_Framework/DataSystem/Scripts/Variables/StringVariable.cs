using System;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/String", order = 2)]
    public class StringVariable : ScriptableObject
    {
        [SerializeField] private string _initialValue = "";
        [SerializeField] private string _value = "";

        public event Action<string> OnValueChanged;

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void SetValue(string value) => Value = value;
        public void ResetToInitial() => Value = _initialValue;

        private void OnEnable()
        {
            _value = _initialValue;
        }
    }
}
