using System.Collections.Generic;
using UnityEngine;

namespace MiniGameTemplate.Debug
{
    /// <summary>
    /// Simple runtime debug console that captures Debug.Log output.
    /// Toggle visibility with a configurable key or multi-touch.
    /// Disabled in release builds.
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote;
        [SerializeField] private int _maxMessages = 100;
        [SerializeField] private int _fontSize = 14;

        private readonly Queue<LogEntry> _messages = new Queue<LogEntry>();
        private bool _isVisible;
        private Vector2 _scrollPosition;

        // Cached GUIStyles — avoid allocation per OnGUI call
        private GUIStyle _bgStyle;
        private GUIStyle _labelStyle;

        private struct LogEntry
        {
            public string Message;
            public LogType Type;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            _messages.Enqueue(new LogEntry { Message = condition, Type = type });
            while (_messages.Count > _maxMessages)
                _messages.Dequeue(); // O(1)
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _isVisible = !_isVisible;

            // Mobile: 3-finger tap to toggle
            if (Input.touchCount >= 3)
            {
                bool allBegan = true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    if (Input.GetTouch(i).phase != TouchPhase.Began)
                        allBegan = false;
                }
                if (allBegan) _isVisible = !_isVisible;
            }
        }

        private void EnsureStyles()
        {
            if (_bgStyle == null)
                _bgStyle = new GUIStyle(GUI.skin.box);

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _fontSize,
                    wordWrap = true
                };
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            EnsureStyles();

            float w = Screen.width * 0.9f;
            float h = Screen.height * 0.5f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 10;

            GUI.Box(new Rect(x, y, w, h), "Debug Console", _bgStyle);

            var scrollRect = new Rect(x + 5, y + 25, w - 10, h - 35);
            float contentHeight = _messages.Count * (_fontSize + 4);
            var viewRect = new Rect(0, 0, w - 30, contentHeight);

            _scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, viewRect);

            float lineY = 0;
            foreach (var entry in _messages)
            {
                _labelStyle.normal.textColor = entry.Type switch
                {
                    LogType.Error => Color.red,
                    LogType.Warning => Color.yellow,
                    LogType.Exception => Color.magenta,
                    _ => Color.white
                };

                GUI.Label(new Rect(0, lineY, w - 30, _fontSize + 4), entry.Message, _labelStyle);
                lineY += _fontSize + 4;
            }

            GUI.EndScrollView();

            // Clear button
            if (GUI.Button(new Rect(x + w - 65, y + 2, 60, 20), "Clear"))
                _messages.Clear();
        }
#endif
    }
}
