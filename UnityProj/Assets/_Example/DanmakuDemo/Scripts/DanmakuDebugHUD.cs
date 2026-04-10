using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 弹幕 Demo 调试面板——OnGUI 实现，零依赖。
    /// 显示 FPS、活跃弹丸数、调度任务数、SpawnerDriver 状态、难度标签。
    /// 按 F1 切换显示/隐藏。
    /// </summary>
    public class DanmakuDebugHUD : MonoBehaviour
    {
        [Header("显示")]
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private int _fontSize = 14;

        private bool _visible;
        private DanmakuSystem _system;

        // FPS 平滑
        private float _fpsTimer;
        private int _fpsFrameCount;
        private float _currentFps;

        // 缓存样式（OnGUI 每帧重建会 GC，缓存一次）
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private bool _styleInitialized;

        private void Start()
        {
            _visible = _showOnStart;
            _system = DanmakuSystem.Instance;
        }

        private void Update()
        {
            // F1 切换
            if (Input.GetKeyDown(KeyCode.F1))
                _visible = !_visible;

            // 延迟获取（DanmakuSystem 可能在后续帧初始化）
            if (_system == null)
                _system = DanmakuSystem.Instance;

            // FPS 计算
            _fpsFrameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                _currentFps = _fpsFrameCount / _fpsTimer;
                _fpsFrameCount = 0;
                _fpsTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!_visible || _system == null) return;

            InitStyles();

            float w = 260f;
            float lineH = _fontSize + 4f;
            float padding = 6f;
            int lineCount = 7;
            float h = lineH * lineCount + padding * 2;

            Rect boxRect = new Rect(8, 8, w, h);
            GUI.Box(boxRect, GUIContent.none, _boxStyle);

            float x = boxRect.x + padding;
            float y = boxRect.y + padding;

            // FPS
            Color fpsColor = _currentFps >= 55 ? Color.green
                : _currentFps >= 30 ? Color.yellow
                : Color.red;
            DrawLabel(x, y, $"FPS: <color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{_currentFps:F1}</color>");
            y += lineH;

            // 弹丸
            int activeBullets = CountActiveBullets();
            int maxBullets = _system.BulletWorld?.Capacity ?? 0;
            DrawLabel(x, y, $"Bullets: {activeBullets} / {maxBullets}");
            y += lineH;

            // 调度器
            var scheduler = _system.Scheduler;
            DrawLabel(x, y, $"Scheduler: {scheduler?.ActiveTasks ?? 0} active (peak: {scheduler?.PeakTasks ?? 0})");
            y += lineH;

            // SpawnerDriver
            var driver = _system.SpawnerDriver;
            DrawLabel(x, y, $"Spawners: {driver?.ActiveCount ?? 0} / {SpawnerDriver.MAX_SPAWNERS}");
            y += lineH;

            // 碰撞目标
            var targets = _system.TargetRegistry;
            DrawLabel(x, y, $"Targets: {targets?.Count ?? 0} / {TargetRegistry.MAX_TARGETS}");
            y += lineH;

            // 难度
            var diff = _system.Difficulty;
            string diffLabel = diff != null ? diff.name : "(none)";
            DrawLabel(x, y, $"Difficulty: {diffLabel}");
            y += lineH;

            // 快捷键提示
            DrawLabel(x, y, "<color=#888888>F1=Toggle R=Clear P=Pause 1/2/3=Diff</color>");
        }

        private int CountActiveBullets()
        {
            var world = _system.BulletWorld;
            if (world == null) return 0;

            int count = 0;
            var cores = world.Cores;
            int cap = world.Capacity;
            for (int i = 0; i < cap; i++)
            {
                if ((cores[i].Flags & BulletCore.FLAG_ACTIVE) != 0)
                    count++;
            }
            return count;
        }

        private void DrawLabel(float x, float y, string text)
        {
            GUI.Label(new Rect(x, y, 400, _fontSize + 4), text, _labelStyle);
        }

        private void InitStyles()
        {
            if (_styleInitialized) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0, 0, 0, 0.75f)) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
                richText = true,
                normal = { textColor = Color.white }
            };

            _styleInitialized = true;
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
