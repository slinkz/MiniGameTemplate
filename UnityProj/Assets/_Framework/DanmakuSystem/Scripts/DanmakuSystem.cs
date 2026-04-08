using MiniGameTemplate.Events;
using MiniGameTemplate.Utils;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹幕系统唯一 MonoBehaviour 入口——初始化所有子系统，驱动每帧更新循环。
    /// DontDestroyOnLoad，关卡切换时调用 ClearAll 清场而非销毁。
    /// </summary>
    public class DanmakuSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private DanmakuWorldConfig _worldConfig;
        [SerializeField] private DanmakuRenderConfig _renderConfig;
        [SerializeField] private DanmakuTypeRegistry _typeRegistry;
        [SerializeField] private DanmakuTimeScaleSO _timeScale;
        [SerializeField] private DifficultyProfileSO _difficulty;

        [Header("事件")]
        [Tooltip("玩家被命中时触发")]
        [SerializeField] private GameEvent _onPlayerHit;

        [Tooltip("造成伤害时触发（传递伤害值）")]
        [SerializeField] private IntGameEvent _onDamageDealt;

        // ──── 子系统 ────
        private BulletWorld _bulletWorld;
        private LaserPool _laserPool;
        private SprayPool _sprayPool;
        private ObstaclePool _obstaclePool;
        private CollisionSolver _collisionSolver;
        private PatternScheduler _scheduler;
        private BulletRenderer _bulletRenderer;
        private DamageNumberSystem _damageNumbers;
        private TrailPool _trailPool;

        // ──── 玩家数据（外部设置） ────
        private Transform _playerTransform;
        private float _playerRadius = 0.2f;
        private BulletFaction _playerFaction = BulletFaction.Player;

        // ──── 无敌帧 ────
        private float _invincibleTimer;

        // ──── 单例 ────
        public static DanmakuSystem Instance { get; private set; }

        /// <summary>弹丸世界容器（外部发射用）</summary>
        public BulletWorld BulletWorld => _bulletWorld;

        /// <summary>激光池</summary>
        public LaserPool LaserPool => _laserPool;

        /// <summary>喷雾池</summary>
        public SprayPool SprayPool => _sprayPool;

        /// <summary>障碍物池</summary>
        public ObstaclePool ObstaclePool => _obstaclePool;

        /// <summary>调度器（外部 Schedule 用）</summary>
        public PatternScheduler Scheduler => _scheduler;

        /// <summary>类型注册表</summary>
        public DanmakuTypeRegistry TypeRegistry => _typeRegistry;

        // ──── 生命周期 ────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSubsystems();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DisposeSubsystems();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // 获取玩家位置
            Vector2 playerPos = _playerTransform != null
                ? (Vector2)_playerTransform.position
                : Vector2.zero;

            // 1. 调度器 Tick（触发到期的发射任务）
            _scheduler.Tick(dt, _bulletWorld, _typeRegistry, _difficulty);

            // 2. 弹丸运动
            BulletMover.UpdateAll(_bulletWorld, _typeRegistry, playerPos, dt, this);

            // 3. 激光更新
            LaserUpdater.UpdateAll(_laserPool, _typeRegistry, dt);

            // 4. 喷雾更新
            SprayUpdater.UpdateAll(_sprayPool, dt);

            // 5. 碰撞检测
            CircleHitbox playerHitbox = new CircleHitbox(playerPos, _playerRadius);

            // 无敌帧检查
            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= Time.unscaledDeltaTime;  // 无敌帧用真实时间
            }

            var result = _collisionSolver.SolveAll(
                _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
                _typeRegistry, in playerHitbox, _playerFaction, dt);

            // 6. 处理碰撞结果
            if (result.HasPlayerHit && _invincibleTimer <= 0)
            {
                _onPlayerHit?.Raise();
                if (result.TotalDamage > 0)
                    _onDamageDealt?.Raise(result.TotalDamage);

                // 伤害飘字
                _damageNumbers.Spawn(playerPos, result.TotalDamage, result.TotalDamage >= 10);

                // 启动无敌帧
                if (_worldConfig.InvincibleDuration > 0)
                    _invincibleTimer = _worldConfig.InvincibleDuration;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // 渲染
            _bulletRenderer.Rebuild(_bulletWorld, _bulletWorld.Trails, _typeRegistry);
            _damageNumbers.UpdateAndRender(dt);
            _trailPool.Render();
        }

        // ──── 公开 API ────

        /// <summary>
        /// 设置玩家碰撞体信息。
        /// </summary>
        public void SetPlayer(Transform playerTransform, float radius)
        {
            _playerTransform = playerTransform;
            _playerRadius = radius;
        }

        /// <summary>
        /// 发射弹幕组合。
        /// </summary>
        public void FireGroup(PatternGroupSO group, Vector2 origin, float baseAngle)
        {
            Vector2 playerPos = _playerTransform != null
                ? (Vector2)_playerTransform.position
                : Vector2.zero;
            _scheduler.Schedule(group, origin, baseAngle, playerPos);
        }

        /// <summary>
        /// 发射单个弹幕。
        /// </summary>
        public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle)
        {
            _scheduler.ScheduleSingle(pattern, origin, baseAngle);
        }

        /// <summary>
        /// 清场——回收所有弹丸/激光/喷雾/障碍物/调度任务。
        /// </summary>
        public void ClearAll()
        {
            _bulletWorld.FreeAll();
            _laserPool.FreeAll();
            _sprayPool.FreeAll();
            _obstaclePool.FreeAll();
            _scheduler.ClearAll();
            _trailPool.FreeAll();
        }

        // ──── 内部 ────

        private void InitializeSubsystems()
        {
            // 类型注册表索引分配
            _typeRegistry.AssignRuntimeIndices();

            // 数据容器
            _bulletWorld = new BulletWorld(_worldConfig.MaxBullets);
            _laserPool = new LaserPool();
            _sprayPool = new SprayPool();
            _obstaclePool = new ObstaclePool();

            // 碰撞
            _collisionSolver = new CollisionSolver();
            _collisionSolver.Initialize(_worldConfig);

            // 调度器
            _scheduler = new PatternScheduler();

            // 渲染
            _bulletRenderer = new BulletRenderer();
            _bulletRenderer.Initialize(_renderConfig, _worldConfig.MaxBullets);

            // 伤害飘字
            _damageNumbers = new DamageNumberSystem();
            _damageNumbers.Initialize(_renderConfig);

            // 重量拖尾
            _trailPool = new TrailPool();
            _trailPool.Initialize(_renderConfig.BulletMaterial);

            GameLog.Log($"[Danmaku] System initialized — bullets: {_worldConfig.MaxBullets}, lasers: {_worldConfig.MaxLasers}, sprays: {_worldConfig.MaxSprays}");
        }

        private void DisposeSubsystems()
        {
            _bulletRenderer?.Dispose();
            _damageNumbers?.Dispose();
            _trailPool?.Dispose();
        }
    }
}
