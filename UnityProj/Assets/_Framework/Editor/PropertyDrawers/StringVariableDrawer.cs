#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Data;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Custom property drawer for StringVariable.
    /// Shows the current runtime value inline next to the object reference.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringVariable))]
    public class StringVariableDrawer : PropertyDrawer
    {
        private static GUIStyle _runtimeValueStyle;
        private static GUIStyle RuntimeValueStyle
        {
            get
            {
                if (_runtimeValueStyle == null)
                {
                    _runtimeValueStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        alignment = TextAnchor.MiddleRight,
                        fontStyle = FontStyle.Bold
                    };
                }
                return _runtimeValueStyle;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var obj = property.objectReferenceValue as StringVariable;
            if (obj != null && Application.isPlaying)
            {
                float fieldWidth = position.width * 0.6f;
                float valueWidth = position.width * 0.38f;

                var fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
                var valueRect = new Rect(position.x + fieldWidth + position.width * 0.02f, position.y, valueWidth, position.height);

                EditorGUI.ObjectField(fieldRect, property, label);

                string display = string.IsNullOrEmpty(obj.Value) ? "= \"\"" : $"= \"{obj.Value}\"";
                // Truncate if too long
                if (display.Length > 30) display = display.Substring(0, 27) + "...\"";
                EditorGUI.LabelField(valueRect, display, RuntimeValueStyle);
            }
            else
            {
                EditorGUI.ObjectField(position, property, label);
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
