using System;
using UnityEngine;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// A Vector2 value stored as a ScriptableObject asset.
    /// Fires OnValueChanged when the value is modified.
    /// Uses Unity default Vector2 == tolerance (~1e-5).
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Variables/Vector2", order = 2)]
    public class Vector2Variable : ScriptableObject
    {
        [SerializeField] private Vector2 _initialValue;
        [SerializeField] private Vector2 _value;

        public event Action<Vector2> OnValueChanged;

        public Vector2 Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void SetValue(Vector2 value) => Value = value;

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
