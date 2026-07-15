---
system: entity-component
scope: entity-pool-manager
last_verified: 2026-05-02
depends_on: [EC_TDD_01_OVERVIEW, EC_TDD_02_CORE_ARCH]
related_code: Assets/_Framework/EntitySystem/Core/EntityPool.cs, EntityManager.cs
---

### 3.6 EntityPool 设计（参考 BulletWorld 模式）

> **v2.2 变更（GD-104）**：构造函数从 `int configId` 改为 `EntityConfigSO config`，Phase 1 以 SO 引用为主键。

```csharp
/// <summary>
/// 按 Entity 配置类型分池。
/// 采用预分配数组 + 空闲槽位栈（参考 BulletWorld），零 GC。
/// Phase 1 以 EntityConfigSO 为键；Phase 2 可选 Luban configId 桥接。
/// </summary>
public class EntityPool
{
    private readonly Entity[] _entities;
    private readonly int[] _freeSlots;
    private int _freeTop;
    private readonly EntityConfigSO _config;

    public int ActiveCount { get; private set; }
    public int Capacity { get; }
    public EntityConfigSO Config => _config;

    public EntityPool(EntityConfigSO config)
    {
        _config = config;
        Capacity = config.PoolMax;
        _entities = new Entity[config.PoolMax];
        _freeSlots = new int[config.PoolMax];

        // 预创建 Entity + 组件（根据 config.Components 决定挂哪些组件）
        for (int i = 0; i < config.PoolMax; i++)
        {
            _entities[i] = CreateEntityFromConfig(config);
            _freeSlots[_freeTop++] = i;
        }
    }

    public Entity Acquire(Vector2 position, float rotation)
    {
        if (_freeTop == 0) { Debug.LogWarning($"[EntityPool] 池满：{_config.name}"); return null; }
        int slot = _freeSlots[--_freeTop];
        var entity = _entities[slot];
        entity.InitAll(position, rotation);
        ActiveCount++;
        return entity;
    }

    public void Release(Entity entity)
    {
        entity.ResetAll();
        _freeSlots[_freeTop++] = entity.PoolSlot;
        ActiveCount--;
    }
}
```

### 3.7 EntityManager（全局驱动器）

> **v2.1 变更（EC-005/EC-013）**：Despawn 改为延迟销毁模式 + swap-remove 优化。
> **v2.2 变更（GD-104/GD-105）**：API 签名从 `int configId` 改为 `EntityConfigSO config`；`_pools` 改为以 SO 引用为 key。
> **v2.6 新增（WF-001/WF-004）**：EntitySystemBootstrap 胶水层 + EntityManagerAccessor 全局访问点。

#### EntityManagerAccessor（全局静态访问点，WF-004）

```csharp
/// <summary>
/// EntityManager 全局访问点（Editor 工具 + 游戏层查询用）。
/// 由 EntitySystemBootstrap.Awake() 注册，OnDestroy() 注销。
/// 非 Singleton 模式——不阻止多实例（测试/分屏场景预留）。
/// </summary>
public static class EntityManagerAccessor
{
    public static EntityManager Instance { get; internal set; }
    public static EntityViewBridge ViewBridge { get; internal set; }
    public static EntitySpawner Spawner { get; internal set; }
}
```

#### EntitySystemBootstrap（胶水层 MonoBehaviour，WF-001）

```csharp
/// <summary>
/// Entity 系统启动器——策划拖到场景根 GO 即可激活整个 Entity 系统。
/// 负责创建 EntityManager / EntityViewBridge / EntitySpawner 实例并每帧驱动。
/// 这是策划工作流的"引擎启动钥匙"。
/// </summary>
public class EntitySystemBootstrap : MonoBehaviour
{
    [Header("调试视觉")]
    [Tooltip("Debug View 的 PoolDefinition（Phase 1 必填）")]
    public PoolDefinition DebugViewPool;

    private EntityManager _entityManager;
    private EntityViewBridge _viewBridge;
    private EntitySpawner _spawner;

    void Awake()
    {
        _entityManager = new EntityManager();
        _viewBridge = new EntityViewBridge(PoolManager.Instance, DebugViewPool);
        _spawner = new EntitySpawner();

        // 注册到全局访问点
        EntityManagerAccessor.Instance = _entityManager;
        EntityManagerAccessor.ViewBridge = _viewBridge;
        EntityManagerAccessor.Spawner = _spawner;

        // 自动发现场景中的 EntitySpawnPoint 并启动
        foreach (var point in FindObjectsOfType<EntitySpawnPoint>())
        {
            if (point.AutoStartOnEnable && point.WaveConfig != null)
                _spawner.StartWave(point);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _entityManager.Tick(dt);
        _spawner.Tick(dt, _entityManager);
        _viewBridge.SyncAll(_entityManager);
    }

    void OnDestroy()
    {
        EntityManagerAccessor.Instance = null;
        EntityManagerAccessor.ViewBridge = null;
        EntityManagerAccessor.Spawner = null;
    }
}
```

**关键决策**：
1. **策划拖一次就行**——场景根 GO 上挂 EntitySystemBootstrap，整个系统自动初始化 + 自动发现 SpawnPoint
2. **不是 Singleton**——通过 Accessor 暴露引用但不阻止多实例，测试场景可多 Bootstrap
3. **Update() 中统一驱动**——时序：EntityManager.Tick() → EntitySpawner.Tick() → ViewBridge.SyncAll()，与 §3.12 时序一致

```csharp
/// <summary>
/// Entity 全局管理器——管理所有 EntityPool，统一驱动 Tick。
/// 非 MonoBehaviour，由游戏层 MonoBehaviour 在 Update 中调用。
/// Phase 1 以 EntityConfigSO 为主键；Phase 2 可增加 Spawn(int configId,...) 重载。
/// </summary>
public class EntityManager
{
    private readonly Dictionary<EntityConfigSO, EntityPool> _pools;  // SO 引用 → pool
    private readonly List<Entity> _activeEntities;        // 活跃 Entity 列表
    private readonly List<Entity> _pendingDespawn;        // v2.1: 延迟销毁队列

    /// <summary>每帧驱动所有活跃 Entity</summary>
    public void Tick(float dt)
    {
        _isTicking = true;
        // Phase A: Tick 所有活跃 Entity
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            var entity = _activeEntities[i];
            // v2.4 预留（GD-R4-011）：Phase 1 IsPaused 永远 false（分支预测零开销）
            // Phase 2 HitStop 启用后，暂停的 Entity 跳过 Tick，逐帧递减 pause 计数
            if (entity.IsPaused) { entity.DecrementPauseFrames(); continue; }
            entity.Tick(dt);
        }

        // Phase B: 统一处理延迟销毁（Tick 期间 Despawn 只标记不执行）
        if (_pendingDespawn.Count > 0)
        {
            for (int i = 0; i < _pendingDespawn.Count; i++)
            {
                ExecuteDespawn(_pendingDespawn[i]);
            }
            _pendingDespawn.Clear();
        }
    }

    /// <summary>从指定配置的池取出 Entity（Phase 1 主 API）</summary>
    public Entity Spawn(EntityConfigSO config, Vector2 position, float rotation)
    {
        var pool = GetOrCreatePool(config);
        var entity = pool.Acquire(position, rotation);
        if (entity != null) _activeEntities.Add(entity);
        return entity;
    }

    // Phase 2 预留：Luban 迁移后增加 int configId 重载
    // public Entity Spawn(int configId, Vector2 position, float rotation) { ... }

    /// <summary>
    /// 回收 Entity（延迟模式：Tick 期间调用只加入待销毁队列，帧尾统一执行）。
    /// Tick 外调用则立即执行。
    /// </summary>
    public void Despawn(Entity entity)
    {
        if (_isTicking)
        {
            entity.MarkPendingDespawn(); // 标记脏位，Tick 中后续组件可检查
            _pendingDespawn.Add(entity);
        }
        else
        {
            ExecuteDespawn(entity);
        }
    }

    /// <summary>实际销毁：swap-remove O(1) + 归还池</summary>
    private void ExecuteDespawn(Entity entity)
    {
        // swap-remove: 将最后一个 Entity 移到被删位置
        int idx = entity.ActiveListIndex;
        int last = _activeEntities.Count - 1;
        if (idx != last)
        {
            _activeEntities[idx] = _activeEntities[last];
            _activeEntities[idx].ActiveListIndex = idx;
        }
        _activeEntities.RemoveAt(last);

        var pool = _pools[entity.ConfigSO];  // 通过 SO 引用找到对应池
        pool.Release(entity);
    }

    private EntityPool GetOrCreatePool(EntityConfigSO config)
    {
        if (!_pools.TryGetValue(config, out var pool))
        {
            pool = new EntityPool(config);
            _pools[config] = pool;
        }
        return pool;
    }

    /// <summary>
    /// 查询指定配置类型的存活 Entity 数量（排除 PendingDespawn）。
    /// v2.3 新增（SA-006）：供 EntitySpawner 的 AllCleared 触发模式使用。
    /// </summary>
    public int CountAliveByConfig(EntityConfigSO config)
    {
        int count = 0;
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            var e = _activeEntities[i];
            if (e.ConfigSO == config && !e.IsPendingDespawn)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Phase 3 预留：按半径搜索指定阵营的 Entity（零 GC，使用预分配结果缓冲区）。
    /// </summary>
    public int FindEntitiesInRadius(
        Vector2 center, float radius, EnumCamp camp,
        Entity[] resultBuffer, int maxResults)
    {
        // Phase 3 实现：线性扫描 _activeEntities，如需优化改为空间分区
        throw new System.NotImplementedException("Phase 3");
    }
}
```

### 3.8 与现有系统的集成矩阵

| 现有系统 | 集成方式 | 优先级 |
|----------|----------|--------|
| **碰撞系统（TargetRegistry）** | CollisionComponent 实现 ICollisionTarget，注册到 TargetRegistry | Phase 1 |
| **事件系统（GameEvent SO）** | 跨 Entity 通信用全局 GameEvent SO；Entity 内部用 EntityEventBus | Phase 1 |
| **对象池（PoolManager）** | Entity 池独立实现（EntityPool）；视觉表现的 GameObject 仍走 PoolManager | Phase 1 |
| **配置驱动** | Phase 1: EntityConfigSO（ScriptableObject）；Phase 2+: 可选迁移 Luban 导表 | Phase 1 SO / Phase 2 Luban |
| **渲染系统（RBM/Atlas）** | Phase 2 考虑——角色 Sprite 渲染可通过 RuntimeAtlas 统一，或走 Spine 独立管线 | Phase 2 |
| **弹幕系统（DanmakuSystem）** | Entity 作为弹幕发射源 + 碰撞目标，通过现有 API 交互 | Phase 1 |

