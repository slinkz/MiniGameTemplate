using UnityEditor;
using UnityEngine;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Danmaku;

namespace Game.ShooterGame.Editor
{
    /// <summary>
    /// T1 技能预览窗口 + T7 BulletPattern 预览（TDD_05 S5.1）。
    /// 在 Scene View 中模拟弹幕发射，不进入 Play Mode。
    /// </summary>
    public class SG_SkillPreviewWindow : EditorWindow
    {
        // ── 输入 ──
        private SkillConfigSO _skillConfig;
        private BulletPatternSO _bulletPattern; // T7：可直接拖入 Pattern 独立预览
        private bool _isPlaying;
        private double _lastTime;

        // ── 被动模拟 Toggle ──
        private bool _simulatePierce;
        private bool _simulateCrit;
        private bool _simulateHoming;

        // ── 模拟器 ──
        private EditorBulletSimulator _simulator = new();
        private float _fireTimer;

        // ── Scene View 坐标 ──
        private Vector3 _enemyWorldPos = new(0, 5, 0);
        private const float CASTER_RADIUS = 0.3f;
        private const float ENEMY_SIZE = 0.4f;
        private const float BULLET_RADIUS = 0.08f;

        [MenuItem("Tools/ShooterGame/工具/Skill Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<SG_SkillPreviewWindow>("Skill Preview");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            _lastTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            _simulator.Clear();
            SceneView.RepaintAll();
        }

        // ──────────────────── Tick Loop ────────────────────

        private void OnEditorUpdate()
        {
            if (!_isPlaying) return;

            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp((float)(now - _lastTime), 0f, 0.1f);
            _lastTime = now;

            // 自动发射
            var pattern = GetActivePattern();
            if (pattern != null)
            {
                float interval = GetFireInterval();
                _fireTimer += dt;
                if (_fireTimer >= interval)
                {
                    _fireTimer -= interval;
                    _simulator.EnemyPosition = new Vector2(_enemyWorldPos.x, _enemyWorldPos.y);
                    _simulator.Spawn(pattern, Vector2.zero, 90f, _simulatePierce, _simulateHoming);
                }
            }

            _simulator.EnemyPosition = new Vector2(_enemyWorldPos.x, _enemyWorldPos.y);
            _simulator.Tick(dt);
            SceneView.RepaintAll();
        }

        // ──────────────────── GUI ────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("技能预览 (T1/T7)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 拖入区
            _skillConfig = (SkillConfigSO)EditorGUILayout.ObjectField("SkillConfigSO", _skillConfig, typeof(SkillConfigSO), false);
            _bulletPattern = (BulletPatternSO)EditorGUILayout.ObjectField("BulletPatternSO (T7)", _bulletPattern, typeof(BulletPatternSO), false);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("被动模拟", EditorStyles.miniLabel);
            _simulatePierce = EditorGUILayout.Toggle("☐ 穿透", _simulatePierce);
            _simulateCrit = EditorGUILayout.Toggle("☐ 暴击闪白", _simulateCrit);
            _simulateHoming = EditorGUILayout.Toggle("☐ 追踪", _simulateHoming);

            EditorGUILayout.Space(6);

            // 播放控制
            EditorGUILayout.BeginHorizontal();
            if (!_isPlaying)
            {
                if (GUILayout.Button("▶ 预览", GUILayout.Height(28)))
                    StartPreview();
            }
            else
            {
                if (GUILayout.Button("■ 停止", GUILayout.Height(28)))
                    StopPreview();
            }
            EditorGUILayout.EndHorizontal();

            // 警告
            if (_simulator.ReachedCapacity)
                EditorGUILayout.HelpBox($"已达预览上限 ({EditorBulletSimulator.MAX_SIM_BULLETS} 发)", MessageType.Warning);

            // 参数速览
            EditorGUILayout.Space(8);
            DrawParamOverview();
        }

        private void DrawParamOverview()
        {
            EditorGUILayout.LabelField("参数速览", EditorStyles.boldLabel);

            if (_skillConfig != null)
            {
                EditorGUILayout.LabelField($"CD: {_skillConfig.CooldownTime:F2}s");
                EditorGUILayout.LabelField($"前摇: {_skillConfig.CastTime:F2}s");
                EditorGUILayout.LabelField($"后摇: {_skillConfig.RecoveryTime:F2}s");
                int effectCount = _skillConfig.Effects != null ? _skillConfig.Effects.Length : 0;
                EditorGUILayout.LabelField($"Effects: {effectCount} 个");

                // 理论 DPS（简易计算）
                var pattern = GetActivePattern();
                if (pattern != null && pattern.BulletType != null)
                {
                    float interval = _skillConfig.CooldownTime + _skillConfig.RecoveryTime;
                    if (interval > 0)
                    {
                        int dmgPerShot = pattern.BulletType.Damage * pattern.Count;
                        float dps = dmgPerShot / interval;
                        EditorGUILayout.LabelField($"理论 DPS: {dps:F1}");
                    }
                }
            }
            else if (_bulletPattern != null)
            {
                EditorGUILayout.LabelField($"Count: {_bulletPattern.Count}");
                EditorGUILayout.LabelField($"Spread: {_bulletPattern.SpreadAngle:F0}°");
                EditorGUILayout.LabelField($"Speed: {_bulletPattern.Speed:F1}");
                EditorGUILayout.LabelField($"Lifetime: {_bulletPattern.Lifetime:F1}s");
            }
            else
            {
                EditorGUILayout.HelpBox("请拖入 SkillConfigSO 或 BulletPatternSO", MessageType.Info);
            }
        }

        // ──────────────────── Scene View Drawing ────────────────────

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isPlaying && _simulator.Bullets.Count == 0) return;

            // PK-R2 ET-009：防止 SceneView 拦截点击
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // 虚拟施法者
            Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, CASTER_RADIUS);

            // 虚拟敌人（可拖动）
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Handles.DrawSolidDisc(_enemyWorldPos, Vector3.forward, ENEMY_SIZE);

            EditorGUI.BeginChangeCheck();
            Vector3 newEnemyPos = Handles.PositionHandle(_enemyWorldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                _enemyWorldPos = new Vector3(newEnemyPos.x, newEnemyPos.y, 0f);
            }

            // 弹丸
            foreach (var bullet in _simulator.Bullets)
            {
                Color c = bullet.Color;
                // 暴击模拟：偶发闪白
                if (_simulateCrit && Random.value < 0.05f)
                    c = Color.white;

                Handles.color = c;
                Vector3 pos3 = new(bullet.Position.x, bullet.Position.y, 0f);
                Handles.DrawSolidDisc(pos3, Vector3.forward, BULLET_RADIUS);
            }

            // 标签
            Handles.color = Color.white;
            Handles.Label(Vector3.zero + Vector3.down * 0.6f, "Caster");
            Handles.Label(_enemyWorldPos + Vector3.down * 0.6f, "Enemy (drag)");
        }

        // ──────────────────── Helpers ────────────────────

        private void StartPreview()
        {
            _isPlaying = true;
            _lastTime = EditorApplication.timeSinceStartup;
            _fireTimer = 999f; // 立即首发
            _simulator.Clear();
        }

        private void StopPreview()
        {
            _isPlaying = false;
            _simulator.Clear();
            SceneView.RepaintAll();
        }

        private BulletPatternSO GetActivePattern()
        {
            // 优先 SkillConfig 中的第一个 FireBulletsEffect
            if (_skillConfig != null && _skillConfig.Effects != null)
            {
                foreach (var eff in _skillConfig.Effects)
                {
                    if (eff is FireBulletsEffect fire && fire.Pattern != null)
                        return fire.Pattern;
                }
            }

            // T7 独立预览
            return _bulletPattern;
        }

        private float GetFireInterval()
        {
            if (_skillConfig != null)
            {
                float total = _skillConfig.CooldownTime + _skillConfig.RecoveryTime;
                return Mathf.Max(total, 0.5f); // 最小 0.5s 防刷屏
            }
            return 1f; // BulletPattern 独立预览默认每秒一发
        }
    }
}
