using MiniGameTemplate.Events;
using MiniGameTemplate.Utils;
using MiniGameTemplate.VFX;
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

        [Header("命中特效")]
        [Tooltip("命中非玩家目标时播放的 Sprite Sheet VFX 系统")]
        [SerializeField] private SpriteSheetVFXSystem _hitVfxSystem;

        [Tooltip("命中非玩家目标时播放的特效类型")]
        [SerializeField] private VFXTypeSO _hitVfxType;

        [Tooltip("命中特效的统一缩放")]
        [SerializeField, Min(0.01f)] private float _hitVfxScale = 1f;

        // ──── 子系统 ────
        private BulletWorld _bulletWorld;
        private LaserPool _laserPool;
        private SprayPool _sprayPool;
        private ObstaclePool _obstaclePool;
        private AttachSourceRegistry _attachRegistry;
        private TargetRegistry _targetRegistry;
        private CollisionSolver _collisionSolver;
        private PatternScheduler _scheduler;
        private SpawnerDriver _spawnerDriver;
        private BulletRenderer _bulletRenderer;
        private LaserRenderer _laserRenderer;
        private DamageNumberSystem _damageNumbers;
        private TrailPool _trailPool;

        // ──── 内置 Player 目标适配器 ────
        private PlayerCollisionTarget _builtinPlayerTarget;

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

        /// <summary>碰撞目标注册表</summary>
        public TargetRegistry TargetRegistry => _targetRegistry;

        /// <summary>调度器（外部 Schedule 用）</summary>
        public PatternScheduler Scheduler => _scheduler;

        /// <summary>发射器驱动器</summary>
        public SpawnerDriver SpawnerDriver => _spawnerDriver;

        /// <summary>类型注册表</summary>
        public DanmakuTypeRegistry TypeRegistry => _typeRegistry;

        /// <summary>当前难度配置</summary>
        public DifficultyProfileSO Difficulty
        {
            get => _difficulty;
            set => _difficulty = value;
        }

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

            // 1. 发射器驱动 Tick（SpawnerDriver 自动发射）
            _spawnerDriver.Tick(dt, this);

            // 2. 调度器 Tick（触发到期的发射任务）
            _scheduler.Tick(dt, _bulletWorld, _typeRegistry, _difficulty, _trailPool);

            // 3. 弹丸运动
            Vector2 playerPos = _builtinPlayerTarget != null
                ? _builtinPlayerTarget.Hitbox.Center
                : Vector2.zero;
            BulletMover.UpdateAll(_bulletWorld, _typeRegistry, playerPos, dt, this, _trailPool);

            // 4. 激光更新（含挂载同步 + 折射段解算）
            LaserUpdater.UpdateAll(_laserPool, _typeRegistry, _obstaclePool,
                _attachRegistry, _worldConfig.WorldBounds, dt);

            // 5. 喷雾更新（含挂载同步）
            SprayUpdater.UpdateAll(_sprayPool, _attachRegistry, dt);

            // 6. 碰撞检测
            // 无敌帧递减
            if (_invincibleTimer > 0)
            {
                _invincibleTimer -= Time.unscaledDeltaTime;  // 无敌帧用真实时间
            }

            var result = _collisionSolver.SolveAll(
                _bulletWorld, _laserPool, _sprayPool, _obstaclePool,
                _typeRegistry, _attachRegistry, _targetRegistry, dt);

            // 7. 处理碰撞结果——仅在 Player 阵营目标被命中时触发事件 / 无敌帧 / 飘字
            if (result.PlayerHit && _invincibleTimer <= 0)
            {
                _onPlayerHit?.Raise();
                if (result.PlayerDamage > 0)
                    _onDamageDealt?.Raise(result.PlayerDamage);

                // 伤害飘字——显示在被命中的玩家位置
                _damageNumbers.Spawn(result.PlayerHitPosition, result.PlayerDamage, result.PlayerDamage >= 10);
                PlayHitVFX(result.PlayerHitPosition);

                // 启动无敌帧
                if (_worldConfig.InvincibleDuration > 0)
                    _invincibleTimer = _worldConfig.InvincibleDuration;
            }

            if (result.NonPlayerHit)
            {
                PlayHitVFX(result.NonPlayerHitPosition);
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_timeScale != null)
                dt *= _timeScale.TimeScale;

            // 渲染
            _bulletRenderer.Rebuild(_bulletWorld, _bulletWorld.Trails, _typeRegistry);
            _laserRenderer.Rebuild(_laserPool, _typeRegistry);
            _damageNumbers.UpdateAndRender(dt);
            _trailPool.Render();
        }

        // ──── 公开 API ────

        /// <summary>
        /// 设置玩家碰撞体信息（向后兼容便捷方法）。
        /// 内部通过 PlayerCollisionTarget 适配器注册到 TargetRegistry。
        /// </summary>
        public void SetPlayer(Transform playerTransform, float radius)
        {
            if (_builtinPlayerTarget != null)
            {
                _targetRegistry.Unregister(_builtinPlayerTarget);
            }

            if (playerTransform != null)
            {
                _builtinPlayerTarget = new PlayerCollisionTarget(playerTransform, radius, _onPlayerHit, _onDamageDealt);
                _targetRegistry.Register(_builtinPlayerTarget);
            }
            else
            {
                _builtinPlayerTarget = null;
            }
        }

        /// <summary>
        /// 注册一个碰撞目标到弹幕系统。
        /// </summary>
        /// <returns>注册是否成功</returns>
        public bool RegisterTarget(ICollisionTarget target)
        {
            return _targetRegistry.Register(target) >= 0;
        }

        /// <summary>
        /// 注销一个碰撞目标。
        /// </summary>
        public void UnregisterTarget(ICollisionTarget target)
        {
            _targetRegistry.Unregister(target);
        }

        /// <summary>
        /// 发射弹幕组合。
        /// </summary>
        public void FireGroup(PatternGroupSO group, Vector2 origin, float baseAngle)
        {
            // 难度替换查表
            group = ResolvePatternOverride(group);

            Vector2 playerPos = _builtinPlayerTarget != null
                ? _builtinPlayerTarget.Hitbox.Center
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
            laser.Faction = (byte)type.Faction;
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
            spray.Faction = (byte)type.Faction;
            spray.Elapsed = 0f;
            spray.TickTimer = 0f;

            return index;
        }

        /// <summary>
        /// 清场——回收所有弹丸/激光/喷雾/障碍物/挂载源/调度任务。
        /// 注意：不清除 TargetRegistry 的注册（目标对象的生命周期由外部管理）。
        /// </summary>
        public void ClearAll()
        {
            _bulletWorld.FreeAll();
            _laserPool.FreeAll();
            _sprayPool.FreeAll();
            _obstaclePool.FreeAll();
            _attachRegistry.FreeAll();
            _scheduler.ClearAll();
            _spawnerDriver.ClearAll();
            _trailPool.FreeAll();
        }

        // ──── 内部 ────

        /// <summary>
        /// 根据当前难度配置查找 PatternOverride 替换。
        /// 如果有匹配的 Override 则返回替换后的 group，否则返回原 group。
        /// </summary>
        private PatternGroupSO ResolvePatternOverride(PatternGroupSO group)
        {
            if (_difficulty == null || _difficulty.PatternOverrides == null) return group;

            var overrides = _difficulty.PatternOverrides;
            for (int i = 0; i < overrides.Length; i++)
            {
                if (overrides[i].Original == group && overrides[i].Replacement != null)
                    return overrides[i].Replacement;
            }
            return group;
        }

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
            _targetRegistry = new TargetRegistry();

            // 碰撞
            _collisionSolver = new CollisionSolver();
            _collisionSolver.Initialize(_worldConfig);

            // 调度器
            _scheduler = new PatternScheduler();

            // 发射器驱动
            _spawnerDriver = new SpawnerDriver();

            // 渲染
            _bulletRenderer = new BulletRenderer();
            _bulletRenderer.Initialize(_renderConfig, _worldConfig.MaxBullets);

            _laserRenderer = new LaserRenderer();
            _laserRenderer.Initialize(_renderConfig);

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
            _laserRenderer?.Dispose();
            _damageNumbers?.Dispose();
            _trailPool?.Dispose();
        }

        private void PlayHitVFX(Vector2 position)
        {
            if (_hitVfxSystem == null || _hitVfxType == null)
                return;

            if (!_hitVfxSystem.CanPlay(_hitVfxType))
                return;

            _hitVfxSystem.PlayOneShot(_hitVfxType, new Vector3(position.x, position.y, 0f), _hitVfxScale);
        }
    }

    /// <summary>
    /// 内置 Player 碰撞目标适配器——将旧 SetPlayer API 适配到 ICollisionTarget 接口。
    /// </summary>
    internal class PlayerCollisionTarget : ICollisionTarget
    {
        private readonly Transform _transform;
        private readonly float _radius;
        private readonly GameEvent _onPlayerHit;
        private readonly IntGameEvent _onDamageDealt;

        public PlayerCollisionTarget(Transform transform, float radius,
            GameEvent onPlayerHit, IntGameEvent onDamageDealt)
        {
            _transform = transform;
            _radius = radius;
            _onPlayerHit = onPlayerHit;
            _onDamageDealt = onDamageDealt;
        }

        public CircleHitbox Hitbox
        {
            get
            {
                // Transform 被销毁后返回零大小 hitbox（不可被命中）
                if (_transform == null) return new CircleHitbox(Vector2.zero, 0f);
                return new CircleHitbox(_transform.position, _radius);
            }
        }
        public BulletFaction Faction => BulletFaction.Player;

        public void OnBulletHit(int damage, int bulletIndex)
        {
            // 事件通知由 DanmakuSystem.Update 统一处理
        }

        public void OnLaserHit(int damage, int laserIndex)
        {
            // 事件通知由 DanmakuSystem.Update 统一处理
        }

        public void OnSprayHit(int damage, int sprayIndex)
        {
            // 事件通知由 DanmakuSystem.Update 统一处理
        }
    }
}
