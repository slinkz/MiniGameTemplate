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
        private AttachSourceRegistry _attachRegistry;
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

        /// <summary>挂载源注册表（激光/喷雾跟随旋转物体）</summary>
        public AttachSourceRegistry AttachRegistry => _attachRegistry;

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

            // 3. 激光更新（含挂载同步 + 折射段解算）
            LaserUpdater.UpdateAll(_laserPool, _typeRegistry, _obstaclePool,
                _attachRegistry, _worldConfig.WorldBounds, dt);

            // 4. 喷雾更新（含挂载同步）
            SprayUpdater.UpdateAll(_sprayPool, _attachRegistry, dt);

            // 5. 碰撞检测
            CircleHitbox playerHitbox = new CircleHitbox(playerPos, _playerRadius);

            // 无敌帧检查
            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= Time.unscaledDeltaTime;  // 无敌帧用真实时间
            }

            var result = _collisionSolver.SolveAll(
                _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
                _typeRegistry, _attachRegistry, in playerHitbox, _playerFaction, dt);

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
        /// 发射激光（Detached 模式——发射后固定不动）。
        /// </summary>
        /// <param name="typeIndex">LaserTypeSO 在 TypeRegistry 中的运行时索引</param>
        /// <param name="origin">发射点（世界坐标）</param>
        /// <param name="angle">角度（弧度）</param>
        /// <param name="length">激光长度（世界单位）</param>
        /// <param name="lifetime">持续时间（秒），0 = 使用 SO 的 TotalDuration</param>
        /// <returns>池索引，-1 表示池满</returns>
        public int FireLaser(byte typeIndex, Vector2 origin, float angle,
            float length, float lifetime = 0f)
        {
            return FireLaserInternal(typeIndex, origin, angle, length, lifetime, 0);
        }

        /// <summary>
        /// 发射激光（Attached 模式——每帧跟随挂载 Transform 的位置和朝向）。
        /// </summary>
        /// <param name="typeIndex">LaserTypeSO 在 TypeRegistry 中的运行时索引</param>
        /// <param name="source">挂载的 Transform</param>
        /// <param name="length">激光长度（世界单位）</param>
        /// <param name="lifetime">持续时间（秒），0 = 使用 SO 的 TotalDuration</param>
        /// <param name="localOffset">相对 Transform 的局部位置偏移</param>
        /// <param name="angleOffset">角度偏移（弧度）</param>
        /// <returns>池索引，-1 表示池满或挂载源注册失败</returns>
        public int FireLaser(byte typeIndex, Transform source, float length,
            float lifetime = 0f, Vector2 localOffset = default, float angleOffset = 0f)
        {
            byte attachId = _attachRegistry.Register(source, localOffset, angleOffset);
            if (attachId == 0) return -1;

            Vector2 origin = _attachRegistry.GetWorldPosition(attachId, (Vector2)source.position);
            float angle = _attachRegistry.GetWorldAngle(attachId, source.eulerAngles.z * Mathf.Deg2Rad);

            return FireLaserInternal(typeIndex, origin, angle, length, lifetime, attachId);
        }

        /// <summary>
        /// 发射喷雾（Detached 模式——发射后固定不动）。
        /// </summary>
        /// <param name="lifetime">持续时间（秒），必须 &gt; 0</param>
        public int FireSpray(byte typeIndex, Vector2 origin, float direction,
            float coneAngle, float range, float lifetime)
        {
            return FireSprayInternal(typeIndex, origin, direction, coneAngle, range, lifetime, 0);
        }

        /// <summary>
        /// 发射喷雾（Attached 模式——每帧跟随挂载 Transform）。
        /// </summary>
        /// <param name="lifetime">持续时间（秒），必须 &gt; 0</param>
        public int FireSpray(byte typeIndex, Transform source,
            float coneAngle, float range, float lifetime,
            Vector2 localOffset = default, float angleOffset = 0f)
        {
            byte attachId = _attachRegistry.Register(source, localOffset, angleOffset);
            if (attachId == 0) return -1;

            Vector2 origin = _attachRegistry.GetWorldPosition(attachId, (Vector2)source.position);
            float direction = _attachRegistry.GetWorldAngle(attachId, source.eulerAngles.z * Mathf.Deg2Rad);

            return FireSprayInternal(typeIndex, origin, direction, coneAngle, range, lifetime, attachId);
        }

        private int FireLaserInternal(byte typeIndex, Vector2 origin, float angle,
            float length, float lifetime, byte attachId)
        {
            int index = _laserPool.Allocate();
            if (index < 0)
            {
                // 池满，释放挂载源
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            var type = _typeRegistry.LaserTypes[typeIndex];
            ref var laser = ref _laserPool.Data[index];
            laser.Origin = origin;
            laser.Angle = angle;
            laser.Length = length;
            laser.MaxWidth = type.MaxWidth;
            laser.Width = type.MaxWidth * 0.05f; // 初始 Charging 宽度
            laser.Lifetime = lifetime > 0f ? lifetime : type.TotalDuration;
            laser.TickInterval = type.TickInterval;
            laser.DamagePerTick = type.DamagePerTick;
            laser.Phase = 1; // Charging
            laser.LaserTypeIndex = typeIndex;
            laser.MaxReflections = type.MaxReflections;
            laser.AttachId = attachId;
            laser.Elapsed = 0f;
            laser.TickTimer = 0f;
            laser.SegmentCount = 0;
            laser.VisualLength = 0f;

            return index;
        }

        private int FireSprayInternal(byte typeIndex, Vector2 origin, float direction,
            float coneAngle, float range, float lifetime, byte attachId)
        {
            int index = _sprayPool.Allocate();
            if (index < 0)
            {
                if (attachId != 0) _attachRegistry.Release(attachId);
                return -1;
            }

            var type = _typeRegistry.SprayTypes[typeIndex];
            ref var spray = ref _sprayPool.Data[index];
            spray.Origin = origin;
            spray.Direction = direction;
            spray.ConeAngle = coneAngle;
            spray.Range = range;
            spray.Lifetime = lifetime;
            spray.TickInterval = type.TickInterval;
            spray.DamagePerTick = type.DamagePerTick;
            spray.Phase = 1; // Active
            spray.SprayTypeIndex = typeIndex;
            spray.AttachId = attachId;
            spray.Elapsed = 0f;
            spray.TickTimer = 0f;

            return index;
        }

        /// <summary>
        /// 清场——回收所有弹丸/激光/喷雾/障碍物/挂载源/调度任务。
        /// </summary>
        public void ClearAll()
        {
            _bulletWorld.FreeAll();
            _laserPool.FreeAll();
            _sprayPool.FreeAll();
            _obstaclePool.FreeAll();
            _attachRegistry.FreeAll();
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
            _attachRegistry = new AttachSourceRegistry();

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
