using UnityEngine;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 系统启动器——策划拖到场景根 GO 即可激活整个 Entity 系统。
    /// 负责创建 EntityManager / EntityViewBridge / EntitySpawner / HitReactionHandler / EntityCollisionSolver 实例并每帧驱动。
    /// 这是策划工作流的"引擎启动钥匙"（v2.6 WF-001）。
    /// 
    /// 时序（§3.12，P2.2 更新）：
    ///   EntityManager.Tick(dt)                ← Phase A: Entity 组件更新 + Phase B: 延迟销毁
    ///   EntitySpawner.Tick(dt, mgr)           ← 波次推进（AllCleared 判定在延迟销毁后，SA-006）
    ///   EntityCollisionSolver.Solve(mgr, dt)  ← Entity vs Entity 碰撞检测+分离+接触伤害（P2.2）
    ///   EntityViewBridge.SyncAll(mgr)         ← 视觉层位置同步（碰撞分离后的修正位置）
    ///   HitReactionHandler.Tick(dt, mgr)      ← 受击表现管线（闪白淡出/伤害数字漂浮/死亡延迟）
    /// </summary>
    public class EntitySystemBootstrap : MonoBehaviour
    {
        [Header("调试视觉")]
        [Tooltip("Debug View 的 PoolDefinition（Phase 1 必填）")]
        public PoolDefinition DebugViewPool;

        [Header("受击表现（P1.11）")]
        [Tooltip("伤害数字 Prefab 的 PoolDefinition（可选，为空则不显示伤害数字）")]
        public PoolDefinition DamageNumberPool;

        [Header("Entity 碰撞（P2.2）")]
        [Tooltip("是否启用 Entity vs Entity 碰撞检测")]
        public bool EnableEntityCollision = true;

        [Header("边界击杀")]
        [Tooltip("是否启用边界击杀（Entity 移出边界后自动死亡回收）")]
        public bool EnableBoundaryKill = true;

        [Tooltip("活动区域边界（超出此范围的 Entity 会被击杀）。默认竖屏 6×10")]
        public Rect KillBounds = new Rect(-6f, -10f, 12f, 20f);

        [Tooltip("超出边界多远后才击杀（缓冲区，避免刚出边缘就死）")]
        public float KillMargin = 1f;

        private EntityManager _entityManager;
        private EntityViewBridge _viewBridge;
        private EntitySpawner _spawner;
        private EntityHitReactionHandler _hitHandler;
        private EntityCollisionSolver _collisionSolver;

        // P2.5: 等待 TriggerZone 触发后才启动的 SpawnPoint
        private EntitySpawnPoint[] _pendingTriggerPoints;
        private int _pendingTriggerCount;

        /// <summary>受击表现管理器（供外部查询闪白状态等）</summary>
        public EntityHitReactionHandler HitReactionHandler => _hitHandler;

        /// <summary>Entity 碰撞求解器（P2.2）</summary>
        public EntityCollisionSolver CollisionSolver => _collisionSolver;

        // ──────────── 生命周期 ────────────

        private void Awake()
        {
            // 创建子系统实例
            _entityManager = new EntityManager();
            _viewBridge = new EntityViewBridge(PoolManager.Instance, DebugViewPool);
            _spawner = new EntitySpawner();
            _hitHandler = new EntityHitReactionHandler(PoolManager.Instance, DamageNumberPool);
            _collisionSolver = new EntityCollisionSolver();

            // ViewBridge ↔ HitHandler 关联（闪白颜色查询）
            _viewBridge.SetHitReactionHandler(_hitHandler);

            // 注册 EntityManager 事件回调（P1.9 解耦设计）
            _entityManager.OnSpawned += _viewBridge.OnEntitySpawned;
            _entityManager.OnDespawned += _viewBridge.OnEntityDespawned;

            // P1.11: 注册受击管线
            _entityManager.OnSpawned += _hitHandler.RegisterEntity;

            // 注册到全局访问点
            EntityManagerAccessor.Instance = _entityManager;
            EntityManagerAccessor.ViewBridge = _viewBridge;
            EntityManagerAccessor.Spawner = _spawner;

            // P2.3: 自动注册场景中 SpawnPoint 引用的 EntityConfigSO 到 ConfigRegistry
            // （确保 Spawn(int configId, ...) 能查到 SO）
            var points = FindObjectsOfType<EntitySpawnPoint>();
            _pendingTriggerPoints = new EntitySpawnPoint[points.Length];
            _pendingTriggerCount = 0;

            foreach (var point in points)
            {
                // 注册波次中引用的所有 EntityConfigSO
                if (point.WaveConfig != null && point.WaveConfig.Waves != null)
                {
                    foreach (var wave in point.WaveConfig.Waves)
                    {
                        if (wave.Groups == null) continue;
                        foreach (var group in wave.Groups)
                        {
                            if (group.EntityConfig != null)
                                EntityConfigRegistry.Register(group.EntityConfig);
                        }
                    }
                }

                // P2.5: 有 TriggerZone 的 SpawnPoint 不立即启动，等待触发
                if (point.TriggerZone != null)
                {
                    _pendingTriggerPoints[_pendingTriggerCount++] = point;
                }
                else if (point.AutoStartOnEnable && point.WaveConfig != null)
                {
                    _spawner.StartWave(point);
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 时序保证（§3.12 / SA-006 / P1.11 / P2.2）：
            // 1. EntityManager.Tick → Phase A + Phase B（延迟销毁执行完毕）
            // 2. EntitySpawner.Tick → 波次推进（此时 CountAliveByConfig 准确）
            // 3. EntityCollisionSolver.Solve → Entity vs Entity 碰撞分离+接触伤害
            // 4. EntityViewBridge.SyncAll → 视觉同步（碰撞修正后的位置）
            // 5. HitReactionHandler.Tick → 受击表现管线（闪白/伤害数字/死亡延迟）
            _entityManager.Tick(dt);
            if (EnableBoundaryKill)
                KillOutOfBoundsEntities();

            // P2.5: 检查待触发 SpawnPoint（TriggerZone 触发后启动刷怪）
            CheckPendingTriggerPoints();

            _spawner.Tick(dt, _entityManager);
            if (EnableEntityCollision)
                _collisionSolver.Solve(_entityManager, dt);
            _viewBridge.SyncAll(_entityManager);
            _hitHandler.Tick(dt, _entityManager);
        }

        // ──────────── P2.5: TriggerZone 启动控制 ────────────

        /// <summary>
        /// 检查挂了 TriggerZone 的 SpawnPoint，触发后启动刷怪并从待触发列表移除。
        /// swap-remove O(1)。
        /// </summary>
        private void CheckPendingTriggerPoints()
        {
            for (int i = _pendingTriggerCount - 1; i >= 0; i--)
            {
                var point = _pendingTriggerPoints[i];
                if (point == null || point.TriggerZone == null)
                {
                    // SpawnPoint 被销毁或 TriggerZone 丢失，移除
                    RemovePendingTrigger(i);
                    continue;
                }

                if (point.TriggerZone.CheckTrigger(_entityManager))
                {
                    // 触发！启动刷怪
                    _spawner.StartWave(point);
                    RemovePendingTrigger(i);
                }
            }
        }

        private void RemovePendingTrigger(int index)
        {
            int last = _pendingTriggerCount - 1;
            if (index != last)
                _pendingTriggerPoints[index] = _pendingTriggerPoints[last];
            _pendingTriggerPoints[last] = null;
            _pendingTriggerCount--;
        }

        // ──────────── 边界击杀 ────────────

        /// <summary>
        /// 扫描所有活跃 Entity，超出 KillBounds + KillMargin 的直接秒杀。
        /// 放在 EntityManager.Tick 之后（延迟销毁已执行），Spawner.Tick 之前。
        /// 不走 TakeDamage（避免触发击退等无意义表现），直接 Despawn。
        /// </summary>
        private void KillOutOfBoundsEntities()
        {
            float xMin = KillBounds.xMin - KillMargin;
            float xMax = KillBounds.xMax + KillMargin;
            float yMin = KillBounds.yMin - KillMargin;
            float yMax = KillBounds.yMax + KillMargin;

            var entities = _entityManager.ActiveEntities;
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                var entity = entities[i];
                if (entity.IsPendingDespawn) continue;

                var pos = entity.Position;
                if (pos.x < xMin || pos.x > xMax || pos.y < yMin || pos.y > yMax)
                {
                    // 直接 Despawn（不走伤害流程，避免无意义的死亡特效/击退/伤害数字）
                    _entityManager.Despawn(entity);
                }
            }
        }

        private void OnDestroy()
        {
            // 注销事件回调
            if (_entityManager != null)
            {
                _entityManager.OnSpawned -= _viewBridge.OnEntitySpawned;
                _entityManager.OnDespawned -= _viewBridge.OnEntityDespawned;
                _entityManager.OnSpawned -= _hitHandler.RegisterEntity;
            }

            // 清理
            _hitHandler?.ClearAll();
            _viewBridge?.ClearAllViews();
            _collisionSolver?.ClearCooldowns();

            // P2.3: 清空 ConfigId 注册表
            EntityConfigRegistry.Clear();

            // 注销全局访问点
            EntityManagerAccessor.Instance = null;
            EntityManagerAccessor.ViewBridge = null;
            EntityManagerAccessor.Spawner = null;
        }
    }
}
