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

        // Pre-allocated char buffer to avoid string GC every frame
        private readonly char[] _fpsBuffer = new char[16];
        private string _fpsString = string.Empty;
        private int _lastDisplayedFps = -1;

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

            // Only rebuild string when integer FPS changes (avoids per-frame GC alloc)
            int fps = (int)(1.0f / _deltaTime);
            if (fps != _lastDisplayedFps)
            {
                _lastDisplayedFps = fps;
                _fpsString = FormatFps(fps);
            }

            GUI.Label(_rect, _fpsString, _style);
        }

        /// <summary>
        /// Format FPS into "FPS: NNN" using the pre-allocated char buffer.
        /// Allocates one string per distinct integer FPS value (amortized zero-alloc at steady state).
        /// </summary>
        private string FormatFps(int fps)
        {
            // "FPS: " prefix
            _fpsBuffer[0] = 'F';
            _fpsBuffer[1] = 'P';
            _fpsBuffer[2] = 'S';
            _fpsBuffer[3] = ':';
            _fpsBuffer[4] = ' ';

            // Integer to chars (max 9999)
            if (fps < 0) fps = 0;
            if (fps > 9999) fps = 9999;

            int idx = 5;
            if (fps >= 1000) { _fpsBuffer[idx++] = (char)('0' + fps / 1000); fps %= 1000; _fpsBuffer[idx++] = (char)('0' + fps / 100); fps %= 100; _fpsBuffer[idx++] = (char)('0' + fps / 10); _fpsBuffer[idx++] = (char)('0' + fps % 10); }
            else if (fps >= 100) { _fpsBuffer[idx++] = (char)('0' + fps / 100); fps %= 100; _fpsBuffer[idx++] = (char)('0' + fps / 10); _fpsBuffer[idx++] = (char)('0' + fps % 10); }
            else if (fps >= 10) { _fpsBuffer[idx++] = (char)('0' + fps / 10); _fpsBuffer[idx++] = (char)('0' + fps % 10); }
            else { _fpsBuffer[idx++] = (char)('0' + fps); }

            return new string(_fpsBuffer, 0, idx);
        }
#endif
    }
}
