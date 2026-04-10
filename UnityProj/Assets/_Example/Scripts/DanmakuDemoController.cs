using MiniGameTemplate.Danmaku;
using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 弹幕 Demo 控制器——一键可跑的弹幕演示场景。
    /// <para>
    /// 用法：
    /// 1. 场景放一个 DanmakuSystem GameObject（挂 DanmakuSystem 组件，配好 SO）
    /// 2. 放一个 Player GameObject（挂 SimplePlayerMover + SpriteRenderer）
    /// 3. 放一个空 GameObject 挂本脚本，Inspector 拖入引用即可
    /// </para>
    /// <para>
    /// 支持三种 Demo 模式：
    /// - SpawnerDriver（自动循环发射，模拟 Boss）
    /// - 手动 FireGroup（按空格发射一组）
    /// - 混合（SpawnerDriver 自动 + 空格手动叠加）
    /// </para>
    /// </summary>
    public class DanmakuDemoController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("玩家 Transform（用于注册碰撞 + 追踪目标）")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("Boss/发射点 Transform（如果没有则用 _bossPosition 固定点）")]
        [SerializeField] private Transform _bossTransform;

        [Header("发射点（无 Boss Transform 时使用）")]
        [SerializeField] private Vector2 _bossPosition = new(0f, 4f);

        [Header("玩家碰撞")]
        [Tooltip("玩家判定圆半径")]
        [SerializeField] private float _playerRadius = 0.15f;

        [Header("Demo 模式")]
        [SerializeField] private DemoMode _mode = DemoMode.SpawnerDriver;

        [Header("SpawnerDriver 模式")]
        [Tooltip("自动发射器配置（拖一个 SpawnerProfileSO）")]
        [SerializeField] private SpawnerProfileSO _spawnerProfile;

        [Header("手动发射模式")]
        [Tooltip("按空格发射的弹幕组（拖一个 PatternGroupSO）")]
        [SerializeField] private PatternGroupSO _manualPatternGroup;

        [Tooltip("手动发射冷却（秒）")]
        [SerializeField] private float _manualCooldown = 0.5f;

        [Header("难度（可选）")]
        [Tooltip("拖入不同的 DifficultyProfileSO 即时切换难度")]
        [SerializeField] private DifficultyProfileSO _difficulty;

        [Header("难度切换（数字键 1/2/3）")]
        [Tooltip("简单难度（数字键 1）")]
        [SerializeField] private DifficultyProfileSO _difficultyEasy;
        [Tooltip("普通难度（数字键 2）")]
        [SerializeField] private DifficultyProfileSO _difficultyNormal;
        [Tooltip("困难难度（数字键 3）")]
        [SerializeField] private DifficultyProfileSO _difficultyHard;

        // ──── 运行时状态 ────
        private DanmakuSystem _system;
        private int _spawnerSlot = -1;
        private float _manualCooldownTimer;
        private bool _initialized;

        public enum DemoMode
        {
            /// <summary>SpawnerDriver 自动循环发射</summary>
            SpawnerDriver,

            /// <summary>按空格手动发射</summary>
            ManualFire,

            /// <summary>SpawnerDriver 自动 + 空格手动叠加</summary>
            Mixed,
        }

        private void Start()
        {
            // 延迟到第一帧，确保 DanmakuSystem.Awake 已执行
            _system = DanmakuSystem.Instance;

            if (_system == null)
            {
                Debug.LogError("[DanmakuDemo] 场景中没有 DanmakuSystem！请先放置并配置。");
                enabled = false;
                return;
            }

            // 注册玩家
            if (_playerTransform != null)
            {
                _system.SetPlayer(_playerTransform, _playerRadius);
            }
            else
            {
                Debug.LogWarning("[DanmakuDemo] 未设置 PlayerTransform，碰撞检测将不触发。");
            }

            // 设置难度
            if (_difficulty != null)
            {
                _system.Difficulty = _difficulty;
            }

            // 启动 SpawnerDriver（如果模式需要）
            if ((_mode == DemoMode.SpawnerDriver || _mode == DemoMode.Mixed) && _spawnerProfile != null)
            {
                _spawnerSlot = _system.SpawnerDriver.Start(
                    _spawnerProfile,
                    GetBossOrigin,
                    baseAngle: 270f  // 朝下发射（竖屏小游戏典型方向）
                );

                if (_spawnerSlot < 0)
                    Debug.LogWarning("[DanmakuDemo] SpawnerDriver 启动失败（可能已满）。");
            }

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            // ── 手动发射（空格键） ──
            if (_mode == DemoMode.ManualFire || _mode == DemoMode.Mixed)
            {
                _manualCooldownTimer -= Time.deltaTime;

                if (Input.GetKey(KeyCode.Space) && _manualCooldownTimer <= 0f && _manualPatternGroup != null)
                {
                    Vector2 origin = GetBossOrigin();
                    _system.FireGroup(_manualPatternGroup, origin, 270f);
                    _manualCooldownTimer = _manualCooldown;
                }
            }

            // ── 快捷键 ──

            // R = 清场
            if (Input.GetKeyDown(KeyCode.R))
            {
                _system.ClearAll();

                // SpawnerDriver 清场后需要重启
                if (_spawnerSlot >= 0 && _spawnerProfile != null)
                {
                    _spawnerSlot = _system.SpawnerDriver.Start(
                        _spawnerProfile, GetBossOrigin, 270f);
                }
            }

            // P = 暂停/恢复 SpawnerDriver
            if (Input.GetKeyDown(KeyCode.P) && _spawnerSlot >= 0)
            {
                // 简单 toggle——由于 SpawnerDriver 没有暴露 IsPaused，用 static 标记
                _paused = !_paused;
                if (_paused)
                    _system.SpawnerDriver.Pause(_spawnerSlot);
                else
                    _system.SpawnerDriver.Resume(_spawnerSlot);
            }

            // 1/2/3 = 切换难度
            if (Input.GetKeyDown(KeyCode.Alpha1) && _difficultyEasy != null)
                _system.Difficulty = _difficultyEasy;
            if (Input.GetKeyDown(KeyCode.Alpha2) && _difficultyNormal != null)
                _system.Difficulty = _difficultyNormal;
            if (Input.GetKeyDown(KeyCode.Alpha3) && _difficultyHard != null)
                _system.Difficulty = _difficultyHard;
        }
        private bool _paused;

        private void OnDestroy()
        {
            // 清理：注销玩家 + 停止发射器
            if (_system != null)
            {
                _system.SetPlayer(null, 0);

                if (_spawnerSlot >= 0)
                    _system.SpawnerDriver.Stop(_spawnerSlot);
            }
        }

        /// <summary>
        /// Boss 发射原点——优先读 Transform，没有则用固定坐标。
        /// </summary>
        private Vector2 GetBossOrigin()
        {
            if (_bossTransform != null)
                return (Vector2)_bossTransform.position;
            return _bossPosition;
        }

        // ──── Editor Gizmos ────

        private void OnDrawGizmosSelected()
        {
            // 画 Boss 发射点
            Vector2 boss = _bossTransform != null
                ? (Vector2)_bossTransform.position
                : _bossPosition;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(boss.x, boss.y, 0), 0.3f);

            // 画发射方向指示（朝下 270°）
            Vector2 dir = new Vector2(0, -1);
            Gizmos.DrawRay(new Vector3(boss.x, boss.y, 0), new Vector3(dir.x, dir.y, 0) * 2f);

            // 画玩家碰撞圈
            if (_playerTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_playerTransform.position, _playerRadius);
            }
        }
    }
}
