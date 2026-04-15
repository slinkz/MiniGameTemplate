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
    /// 额外快捷键：L=Attached 激光（跟随 Boss），K=Detached Spray，J=Attached Spray（跟随 Boss），R=ClearAll，D=销毁Boss
    /// 验收辅助：Boss 可自动横向往返移动，用于验证 Attached Laser / Spray / FollowTarget 跟随效果


    /// </para>
    /// </summary>
    public class DanmakuDemoController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("玩家 Transform（用于注册碰撞 + 追踪目标）")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("Boss/发射点 Transform（如果没有则用 _bossPosition 固定点）")]
        [SerializeField] private Transform _bossTransform;
        [Tooltip("是否让 Boss 自动横向往返移动（用于 FollowTarget / Attached Spray 验收）")]
        [SerializeField] private bool _autoMoveBoss = true;
        [Tooltip("Boss 自动横向移动振幅（相对初始位置，世界单位）")]
        [SerializeField] private float _bossMoveAmplitude = 2.5f;
        [Tooltip("Boss 自动横向移动速度")]
        [SerializeField] private float _bossMoveSpeed = 1.5f;
        [Tooltip("Boss 逆时针旋转速度（度/秒），0 = 不旋转")]
        [SerializeField] private float _bossRotateSpeed = 45f;

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

        [Header("激光测试（L 键）")]
        [Tooltip("激光类型在 TypeRegistry 中的索引（通常为 0）")]
        [SerializeField] private byte _laserTypeIndex = 0;
        [Tooltip("激光长度（世界单位）")]
        [SerializeField] private float _laserLength = 12f;
        [Tooltip("激光发射冷却（秒）")]
        [SerializeField] private float _laserCooldown = 3f;

        [Header("喷雾测试（K 键 / J 键）")]
        [Tooltip("喷雾类型在 TypeRegistry 中的索引（需先在 TypeRegistry 注册 SprayTypeSO）")]
        [SerializeField] private byte _sprayTypeIndex = 0;
        [Tooltip("喷雾寿命（秒），<= 0 时回退到 1 秒")]
        [SerializeField] private float _sprayLifetime = 1f;
        [Tooltip("Detached 喷雾射程（世界单位），<= 0 时使用 SprayTypeSO.Range")]
        [SerializeField] private float _sprayRangeOverride = 0f;
        [Tooltip("Detached 喷雾半角（度），<= 0 时使用 SprayTypeSO.ConeAngle")]
        [SerializeField] private float _sprayConeAngleOverride = 0f;
        [Tooltip("喷雾发射冷却（秒）")]
        [SerializeField] private float _sprayCooldown = 1.5f;


        // ──── 运行时状态 ────
        private DanmakuSystem _system;
        private int _spawnerSlot = -1;
        private float _manualCooldownTimer;
        private float _laserCooldownTimer;
        private float _sprayCooldownTimer;
        private bool _initialized;
        private bool _bossDestroyed;
        private Vector3 _bossStartPosition;



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

            if (_bossTransform != null)
                _bossStartPosition = _bossTransform.position;

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

            // ── 防御：Boss 被销毁后自动停止 SpawnerDriver ──
            if (!_bossDestroyed && _bossTransform == null)
            {
                _bossDestroyed = true;
                if (_spawnerSlot >= 0)
                {
                    _system.SpawnerDriver.Stop(_spawnerSlot);
                    _spawnerSlot = -1;
                }
                Debug.Log("[DanmakuDemo] Boss 已销毁，SpawnerDriver 已停止。");
            }

            // X = 销毁 Boss（E-06 验收用）
            if (Input.GetKeyDown(KeyCode.X) && _bossTransform != null)
            {
                Debug.Log("[DanmakuDemo] X 键：销毁 Boss GameObject。");
                Destroy(_bossTransform.gameObject);
                // _bossDestroyed 会在下一帧由上面的检测自动触发
            }

            if (_autoMoveBoss && _bossTransform != null)
            {
                float offsetX = Mathf.Sin(Time.time * _bossMoveSpeed) * _bossMoveAmplitude;
                _bossTransform.position = new Vector3(
                    _bossStartPosition.x + offsetX,
                    _bossStartPosition.y,
                    _bossStartPosition.z);

                // 逆时针旋转（Z 轴正方向）
                if (_bossRotateSpeed > 0f)
                    _bossTransform.Rotate(0f, 0f, _bossRotateSpeed * Time.deltaTime, Space.Self);
            }

            // ── 手动发射（空格键） ──
            if (!_bossDestroyed && (_mode == DemoMode.ManualFire || _mode == DemoMode.Mixed))

            {
                _manualCooldownTimer -= Time.deltaTime;

                if (Input.GetKey(KeyCode.Space) && _manualCooldownTimer <= 0f && _manualPatternGroup != null)
                {
                    Vector2 origin = GetBossOrigin();
                    _system.FireGroup(_manualPatternGroup, origin, 270f);
                    _manualCooldownTimer = _manualCooldown;
                }
            }

            // ── 快捷键（Boss 存活时才可发射） ──

            // L = 发射激光（Attached 模式——跟随 Boss）
            _laserCooldownTimer -= Time.deltaTime;
            if (!_bossDestroyed && Input.GetKeyDown(KeyCode.L) && _laserCooldownTimer <= 0f)
            {
                if (_system.TypeRegistry.LaserTypes != null &&
                    _laserTypeIndex < _system.TypeRegistry.LaserTypes.Length)
                {
                    int slot;
                    if (_bossTransform != null)
                    {
                        // Attached：每帧跟随 Boss 位置
                        slot = _system.FireLaser(_laserTypeIndex, _bossTransform, _laserLength);
                    }
                    else
                    {
                        // Fallback Detached：Boss 不存在时用固定坐标
                        Vector2 origin = GetBossOrigin();
                        float angle = -Mathf.PI * 0.5f;
                        slot = _system.FireLaser(_laserTypeIndex, origin, angle, _laserLength);
                    }

                    if (slot >= 0)
                        _laserCooldownTimer = _laserCooldown;
                    else
                        Debug.LogWarning("[DanmakuDemo] 激光池已满，无法发射。");
                }
                else
                {
                    Debug.LogWarning("[DanmakuDemo] TypeRegistry 中没有激光类型，请先注册 LaserTypeSO。");
                }
            }

            // K = 发射 Detached Spray；J = 发射 Attached Spray（跟随 Boss）
            _sprayCooldownTimer -= Time.deltaTime;
            if (!_bossDestroyed && _sprayCooldownTimer <= 0f && (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.J)))
            {
                if (_system.TypeRegistry.SprayTypes != null &&
                    _sprayTypeIndex < _system.TypeRegistry.SprayTypes.Length)
                {
                    var sprayType = _system.TypeRegistry.SprayTypes[_sprayTypeIndex];
                    float coneAngle = _sprayConeAngleOverride > 0f ? _sprayConeAngleOverride : sprayType.ConeAngle;
                    float range = _sprayRangeOverride > 0f ? _sprayRangeOverride : sprayType.Range;
                    float lifetime = _sprayLifetime > 0f ? _sprayLifetime : 1f;

                    int slot;
                    if (Input.GetKeyDown(KeyCode.J) && _bossTransform != null)
                    {
                        slot = _system.FireSpray(_sprayTypeIndex, _bossTransform, coneAngle, range, lifetime);
                    }
                    else
                    {
                        Vector2 origin = GetBossOrigin();
                        float direction = -Mathf.PI * 0.5f;
                        slot = _system.FireSpray(_sprayTypeIndex, origin, direction, coneAngle, range, lifetime);
                    }

                    if (slot >= 0)
                    {
                        _sprayCooldownTimer = _sprayCooldown;
                    }
                    else
                    {
                        Debug.LogWarning("[DanmakuDemo] 喷雾池已满，无法发射。请检查 SprayPool 容量或等待回收。");
                    }
                }
                else
                {
                    Debug.LogWarning("[DanmakuDemo] TypeRegistry 中没有喷雾类型，请先注册 SprayTypeSO。");
                }
            }

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

            // Esc = 返回主菜单
            if (Input.GetKeyDown(KeyCode.Escape))
                ExampleSceneNavigator.ReturnToMainMenu();
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
