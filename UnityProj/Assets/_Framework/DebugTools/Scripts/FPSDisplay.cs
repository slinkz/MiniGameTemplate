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
        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            int w = Screen.width, h = Screen.height;
            var style = new GUIStyle();
            var rect = new Rect(10, 10, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = _fontSize;
            style.normal.textColor = _textColor;

            float fps = 1.0f / _deltaTime;
            string text = $"FPS: {fps:0.}";
            GUI.Label(rect, text, style);
        }
#endif
    }
}
