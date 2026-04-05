using UnityEngine;

namespace MiniGameTemplate.Debug
{
    /// <summary>
    /// Displays current FPS in the top-left corner using OnGUI.
    /// Automatically disabled in non-development builds.
    /// </summary>
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] private int _fontSize = 24;
        [SerializeField] private Color _textColor = Color.green;

        private float _deltaTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GUIStyle _style;
        private Rect _rect;

        private void Start()
        {
            _style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = _fontSize,
            };
            _style.normal.textColor = _textColor;
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (_style == null) return;

            int w = Screen.width, h = Screen.height;
            _rect.Set(10, 10, w, h * 2 / 100);

            float fps = 1.0f / _deltaTime;
            GUI.Label(_rect, $"FPS: {fps:0.}", _style);
        }
#endif
    }
}
