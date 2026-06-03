---
system: entity-component
scope: systems-spawner-view
last_verified: 2026-05-02
depends_on: [EC_TDD_01_OVERVIEW, EC_TDD_03_ENTITY_POOL]
related_code: Assets/_Framework/EntitySystem/Core/EntitySystemBootstrap.cs, EntityViewBridge.cs, Assets/_Framework/EntitySystem/Spawner/*.cs
---

### 3.9 TargetRegistry 槽位约束的应对策略

> **v2.1 变更（EC-002/EC-006/EC-010）**：分阶段策略 + 补充伪代码 + PierceHitMask 风险说明 + 池化安全防护。
> **v2.2 变更（天命人决策 D-01）**：TargetRegistry 从 16 扩容到 64，超出后 LogError 提示需扩容。

**问题**：弹幕游戏同屏可能 50+ 敌兵，原 16 槽位不够用。

**方案（v2.2 更新）**：
- **A 方案：扩容 TargetRegistry 到 64** → **选择此方案**（天命人决策 D-01）
- **B 方案：动态注册/注销** → 保留为 Phase 2+ 应急方案（当 Entity > 64 时）

**Phase 1 策略（扩容模式）**：
- TargetRegistry 硬上限 64 个目标，Phase 1 验收场景充分覆盖
- CollisionComponent.Init → RegisterTarget，Reset → UnregisterTarget，无需动态策略
- **Phase 1 硬约束**：如果注册失败（返回 -1），LogError 提示需扩容并将该 Entity 标记为"碰撞不可用"

**Phase 2+ 策略（动态模式，仅在 Entity > 64 时启用）**：
```
CollisionRegistrationPass 伪代码：
1. 计算弹幕活跃区域 = WorldBounds 缩小 10%（或 BulletWorld 中活跃弹丸的包围盒）
2. 对所有持有 CollisionComponent 的活跃 Entity，按以下规则排序：
   - 权重 = (1.0 - 距离/活跃区域半径) × 0.7 + (1.0 - 当前HP/最大HP) × 0.3
   - 权重越高优先注册
3. 取前 63 个注册（保留 1 槽给玩家）
4. 防抖：已注册的 Entity 有 MIN_REGISTERED_FRAMES = 10 帧的最小保持期
   - 未到保持期的 Entity 不被踢出，即使权重低于排队者
5. 注销时清除对应弹丸的 PierceHitMask（见下方风险说明）
```

**PierceHitMask 位宽升级（SA-001，v2.3）**：
- **问题**：TargetRegistry 扩容到 64 后，原 `BulletCore.PierceHitMask`（`ushort`，16 位）只能覆盖 0~15 号槽位。16+ 号槽位的穿透记录溢出，导致同一弹丸对同一目标每帧重复伤害。
- **方案**：`PierceHitMask` 从 `ushort` → `ulong`（64 位），`CollisionSolver` 位操作从 `(ushort)(1 << t)` → `(1UL << t)`
- **权衡**：BulletCore 结构体 48 → 56 bytes（+8 bytes，ulong 对齐），2048 弹丸 × 56 = 112KB，仍在 L2 缓存友好范围（典型 256KB~1MB L2）。接受此开销。
- **影响范围**：BulletCore struct 定义、CollisionSolver.SolveBulletVsTarget()、所有使用 PierceHitMask 的位运算

**PierceHitMask 动态注册/注销冲突风险（EC-006）**：
- 动态注册/注销会导致同一 TargetRegistry 槽位被不同 Entity 复用
- `BulletCore.PierceHitMask`（ulong，按槽位 bit 标记）会误判：旧 Entity 的命中记录被新 Entity 继承
- **缓解方案**：注销 Entity 时，遍历 BulletWorld 活跃弹丸，清除对应槽位 bit（O(n) 但只在注销时执行）
- **替代方案（Phase 2 评估）**：改 PierceHitMask 为 EntityId 数组（每弹丸最多穿透 N 个目标）

**池化安全防护（EC-010/EC-017）**：
- CollisionComponent.Reset() 时，若 `_targetSlot < 0` 或 DanmakuSystem.Instance == null（场景切换时），静默跳过注销
- **注意**：DanmakuSystem.ClearAll() **不会**清除 TargetRegistry（代码注释明确标注"目标生命周期由外部管理"）。CollisionComponent.Reset() 主动注销是唯一清理路径
- 场景切换时，EntityManager 应遍历所有池化 Entity 执行 Reset（确保每个 CollisionComponent 注销自身）
- 增加防护：CollisionComponent 实现 `bool IsAlive => _owner != null && !_owner.IsPendingDespawn`
- CollisionSolver 遍历 TargetRegistry 时检查 target 的有效性（现有代码已有 null 检查，`IsAlive` 是额外保障层）

### 3.10 渲染架构预留（Phase 2）

> 无 v2.1 变更。

角色渲染不走弹幕的 RenderBatchManager 管线（那是 instanced quad 渲染，针对大量同质粒子优化的）。

**Phase 2 选项**：
- **A 方案：Spine + 独立 SpriteBatcher** —— 角色用 Spine 骨骼动画，走 Spine-Unity 渲染管线
- **B 方案：序列帧 + RuntimeAtlas** —— 角色帧动画纹理注册到 RuntimeAtlas，走 instanced quad 渲染
- **当前决策**：Phase 1 先不做渲染集成，Entity 纯逻辑层。渲染表现由游戏层自行桥接。

### 3.11 阵营设计（EC-008 + D-02）

> **v2.1 新增**。
> **v2.2 变更（天命人决策 D-02）**：BulletFaction → EnumCamp 统一，Phase 1 顺手做。

Phase 1 统一使用 `EnumCamp` 枚举（替代原 BulletFaction），在 Phase 1 中完成枚举重命名和全项目替换：
- 小游戏场景 3 个阵营（Enemy/Player/Neutral）足够覆盖 PvE/PvP 基本需求
- Entity 层和弹幕层共用同一枚举，避免映射开销

**扩展触发条件**：当需要 4+ 阵营（如 Team1/Team2/...）时：
1. 扩展 `EnumCamp` 枚举
2. 如果碰撞系统需要更复杂的阵营关系矩阵，引入 `CampRelation` 配置表
3. CollisionSolver 的 ShouldCollide 逻辑从硬编码改为查表

### 3.12 Tick 时序与碰撞延迟说明（EC-009）

> **v2.1 新增**。

**帧内执行顺序**：
```
DanmakuSystem.Update()           ← 弹幕运动 + 碰撞检测（使用 Entity 上一帧位置）
EntityManager.Tick()              ← Phase A: Entity 组件更新（MovementComponent 更新位置）
                                  ← Phase B: 延迟销毁统一执行
EntitySpawner.Tick()              ← 波次推进（AllCleared 判定在延迟销毁后，SA-006 v2.3）
EntityViewBridge.SyncAll()        ← 视觉层位置同步
DanmakuSystem.LateUpdate()       ← 渲染上传
```

**已知限制**：Entity 位置更新在碰撞检测之后，导致碰撞使用上一帧位置（1 帧延迟）。

**影响评估**：
- 30fps 下，1 帧 = 33ms，Entity 移速 5 单位/秒 → 偏移 0.17 单位（~2 像素），不可感知
- 即使 60fps + 冲刺（20 单位/秒），偏移 0.33 单位（~4 像素），仍在碰撞体半径容忍范围内
- **结论**：小游戏场景可接受，不做预测补偿

### 3.13 内存预算估算（EC-011）

> **v2.1 新增**。
> **v2.4 变更（GD-R4-001/003）**：Entity 本体 +4 bytes（PauseFrames），BulletCore +4 bytes（OwnerEntityId）。

单个 Entity 内存估算：
| 组成部分 | 估算大小 |
|----------|----------|
| Entity 本体（ID, Faction, Position, Config ref, 组件数组 16 槽, PauseFrames） | ~132 bytes |
| EntityEventBus（Delegate[16,4] + int[16]） | ~320 bytes |
| 9 个组件（含 AttackComponent，平均每个 ~48 bytes） | ~432 bytes |
| **合计** | **~884 bytes / Entity** |

弹幕系统影响（GD-R4-001）：
- BulletCore 新增 `uint OwnerEntityId`（+4 bytes，56→60 bytes）
- 2048 弹丸 × 60 = ~120 KB，仍在 L2 缓存友好范围

预算场景：
- 10 种配置 × 每种 poolMax=20 = 200 个 Entity = **~163 KB**
- 20 种配置 × 每种 poolMax=20 = 400 个 Entity = **~325 KB**
- **目标上限**：EntitySystem 总内存 < 2MB（含所有池 + Manager 开销）

### 3.14 刷怪系统设计（GD-003/GD-102）

> **v2.2 新增（PK R1 + R2 产物）**。
> **v2.4 变更（GD-R4-005）**：WaveTriggerMode 新增 OnCallback；EntitySpawnWaveSO 新增 Loop/LoopStartWave；SpawnGroup 新增 Formation 枚举。

```csharp
/// <summary>
/// 刷怪波次配置资产。策划在 Inspector 中编排关卡波次。
/// 路径：Assets/_Game/Configs/SpawnWave/
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnWave", menuName = "Entity/SpawnWaveConfig")]
public class EntitySpawnWaveSO : ScriptableObject
{
    public SpawnWaveEntry[] Waves;

    [Header("循环模式（v2.4 新增，GD-R4-005）")]
    [Tooltip("是否在最后一波结束后从 LoopStartWave 重新开始（无限模式）")]
    public bool Loop = false;
    [Tooltip("循环起始波次索引（0-based）")]
    public int LoopStartWave = 0;
}

[System.Serializable]
public struct SpawnWaveEntry
{
    [Tooltip("本波包含的怪物组（支持单波多怪种）")]
    public SpawnGroup[] Groups;

    [Tooltip("触发模式")]
    public WaveTriggerMode TriggerMode;

    [Tooltip("Timer 模式：上一波结束后的延迟秒数")]
    public float TriggerDelay;

    // Phase 2 预留（GD-R4-005，注释掉）：
    // [Tooltip("难度缩放——本波怪物 HP 乘数")]
    // public float HpMultiplier = 1f;
    // [Tooltip("难度缩放——本波怪物数量乘数")]
    // public float CountMultiplier = 1f;
}

[System.Serializable]
public struct SpawnGroup
{
    public EntityConfigSO EntityConfig;     // 怪种配置
    public EnumCamp Camp;                   // 阵营
    public int Count;                       // 数量
    public float SpawnInterval;             // 组内逐个生成间隔

    [Tooltip("生成阵型（v2.4 新增，GD-R4-005）。Phase 1 只实现 Random")]
    public SpawnFormation Formation;        // 阵型
}

/// <summary>
/// 生成阵型枚举（v2.4 新增）。
/// Phase 1 只实现 Random；Line/Circle Phase 2 实现。
/// </summary>
public enum SpawnFormation
{
    Random = 0,     // AreaRadius 内随机散布（Phase 1 默认）
    Line = 1,       // Phase 2：沿指定方向排一列
    Circle = 2,     // Phase 2：围成一圈
}

public enum WaveTriggerMode
{
    Timer = 0,          // 上一波结束后延迟 N 秒
    AllCleared = 1,     // 上一波全灭后触发
    OnCallback = 2,     // v2.4 新增（GD-R4-005）：波次完成后触发事件，等待游戏层调用 Spawner.ContinueNextWave() 才推进
}
```

**场景组件**：

```csharp
/// <summary>
/// 放置在场景中的刷怪点。策划通过 Inspector 配置波次 SO 和生成范围。
/// Editor 模式下绘制 Gizmo 可视化生成区域。
/// v2.5 变更（ET-009）：改为 Always 绘制 + Label，多刷怪点场景一目了然。
/// </summary>
public class EntitySpawnPoint : MonoBehaviour
{
    [Header("波次配置")]
    public EntitySpawnWaveSO WaveConfig;    // 引用波次 SO
    public bool AutoStartOnEnable = true;  // 场景加载后自动开始

    [Header("生成区域")]
    public float AreaRadius = 2f;          // 随机散布半径

    // v2.5（ET-009）：始终绘制半透明圆圈 + 名称标签
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // 半透明黄色
        Gizmos.DrawWireSphere(transform.position, AreaRadius);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, gameObject.name);
        #endif
    }

    // v2.5（ET-009）：选中时高亮显示 + 完整波次信息
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AreaRadius);
        #if UNITY_EDITOR
        if (WaveConfig != null)
        {
            int totalWaves = WaveConfig.Waves?.Length ?? 0;
            int totalMonsters = 0;
            string firstEnemy = "N/A";
            if (totalWaves > 0 && WaveConfig.Waves[0].Groups?.Length > 0)
            {
                firstEnemy = WaveConfig.Waves[0].Groups[0].EntityConfig?.DisplayName ?? "?";
                foreach (var wave in WaveConfig.Waves)
                    if (wave.Groups != null)
                        foreach (var g in wave.Groups)
                            totalMonsters += g.Count;
            }
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (AreaRadius + 0.3f),
                $"{gameObject.name}\n{totalWaves} 波 | {totalMonsters} 怪 | 首波: {firstEnemy}");
        }
        #endif
    }
}

/// <summary>
/// 刷怪驱动器——管理 EntitySpawnPoint 的波次推进逻辑。
/// 由游戏层 MonoBehaviour 持有并在 Update 中驱动。
/// </summary>
public class EntitySpawner
{
    public void StartWave(EntitySpawnPoint point) { /* ... */ }
    public void Tick(float dt, EntityManager entityManager) { /* ... */ }
    public bool IsAllWavesCleared { get; }
}
```

**触发区域组件（P2.5 新增）**：

```csharp
/// <summary>
/// 触发区域检测器（P2.5）——放置在场景中，作为 EntitySpawnPoint 的启动开关。
/// 
/// 使用方式：
/// 1. 在 GO 上挂 BoxCollider2D 或 CircleCollider2D（自动设为 IsTrigger=true）
/// 2. 挂本脚本，配置 TargetCamp / OneShot
/// 3. 在 EntitySpawnPoint 的 TriggerZone 字段中引用此 GO
///    → SpawnPoint.TriggerZone != null：等玩家进入区域后才开始刷怪
///    → SpawnPoint.TriggerZone == null：按 AutoStartOnEnable 自动开始
/// 
/// 设计决策：
/// - TriggerZone 是 SpawnPoint 级开关，不是波次级（SO 不能引用场景对象）
/// - 区域形状由 Collider2D 定义（策划在 Inspector 中拖拽编辑大小）
/// - 检测仍为主动轮询 EntityManager（Entity 纯逻辑无 GO Collider，不走 Physics2D）
/// - Collider2D.OverlapPoint(entity.Position) 判断是否在区域内——支持任意形状
/// - 零 GC：无事件、无回调、纯状态查询
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EntityTriggerZone : MonoBehaviour
{
    public EnumCamp TargetCamp = EnumCamp.Player;   // 检测的目标阵营
    public bool OneShot = true;                      // 进入后永久激活

    public bool IsTriggered { get; private set; }
    public void ResetTrigger() => IsTriggered = false;

    /// <summary>
    /// 由 Bootstrap.CheckPendingTriggerPoints() 每帧调用。
    /// Collider2D.OverlapPoint 判断 Entity.Position 是否在区域内。
    /// </summary>
    public bool CheckTrigger(EntityManager entityManager) { /* ... */ }
}
```

**EntitySystemBootstrap — TriggerZone 启动控制（P2.5）**：

```csharp
// Awake 中：
if (point.TriggerZone != null)
    _pendingTriggerPoints[_pendingTriggerCount++] = point;   // 等触发
else if (point.AutoStartOnEnable && point.WaveConfig != null)
    _spawner.StartWave(point);                                // 立即启动

// Update 中（Spawner.Tick 之前）：
CheckPendingTriggerPoints();

/// <summary>swap-remove O(1)，触发后 StartWave + 移除</summary>
private void CheckPendingTriggerPoints()
{
    for (int i = _pendingTriggerCount - 1; i >= 0; i--)
    {
        if (point.TriggerZone.CheckTrigger(_entityManager))
        {
            _spawner.StartWave(point);
            RemovePendingTrigger(i);
        }
    }
}
```

**Phase 1 调用时序（SA-006，v2.3 明确）**：
```
游戏层 MonoBehaviour.Update():
    EntityManager.Tick(dt)       ← Phase A: Tick 所有活跃 Entity
                                  ← Phase B: 统一处理延迟销毁（_pendingDespawn）
    CheckPendingTriggerPoints()  ← P2.5: 检查 TriggerZone 触发状态
    EntitySpawner.Tick(dt, mgr)  ← Phase B 之后调用，确保 AllCleared 判定时
                                    已销毁的 Entity 不再被计为活跃
```
- **AllCleared 判定**：调用 `EntityManager.CountAliveByConfig(config)` 查询存活数，排除 `IsPendingDespawn` 的 Entity
- **时序保证**：Spawner 在 EntityManager.Tick() 之后运行，Phase B 延迟销毁已执行完毕，避免 1 帧延迟误触发下一波

**Phase 1 实现范围**：Timer + AllCleared + OnCallback 三种模式 + Loop 循环。TriggerZone 启动控制在 P2.5 实现。生成阵型 Phase 1 只实现 Random（AreaRadius 内随机散布），Line/Circle Phase 2+。难度缩放（HpMultiplier/CountMultiplier）Phase 2。

### 3.15 EntityViewBridge 设计（GD-103）

> **v2.2 新增（PK R2 产物）**。
> **v2.3 变更（SA-005）**：内部存储从 `Dictionary<uint, GameObject>` 改为预分配数组，SyncAll() 零 GC 遍历。

```csharp
/// <summary>
/// Entity 逻辑层与视觉层的桥接器。
/// 持有 EntityId → View GO 映射，Entity 本身不持有 GO 引用（BC-01.1 不变）。
/// Phase 1: 使用内置 Debug Prefab（彩色圆 + HP 文本）
/// Phase 2: 使用 EntityConfigSO.ViewPrefab（策划指定的正式 Prefab）
/// 
/// v2.3 变更（SA-005）：内部存储从 Dictionary 改为预分配数组。
/// 原因：Mono 运行时 Dictionary.GetEnumerator() 每次 foreach 产生 ~40 bytes GC Alloc（装箱），
/// 违反零 GC 承诺。改为平铺数组 + for 循环遍历，彻底消除 GC。
/// </summary>
public class EntityViewBridge
{
    private const int MAX_VIEWS = 256; // 预分配上限（远超 Phase 1 需求，可调）

    // 预分配数组——零 GC 遍历
    private readonly GameObject[] _viewGOs = new GameObject[MAX_VIEWS];
    private readonly uint[] _viewEntityIds = new uint[MAX_VIEWS];
    private readonly EntityConfigSO[] _viewConfigs = new EntityConfigSO[MAX_VIEWS]; // 回收时查池用
    private int _activeCount;

    private readonly PoolManager _poolManager;
    private PoolDefinition _debugViewPool;  // Phase 1 内置 Debug Prefab 的池

    /// <summary>Entity 生成时调用——创建/获取对应的 View GO</summary>
    public void OnEntitySpawned(Entity entity, EntityConfigSO config)
    {
        if (_activeCount >= MAX_VIEWS) { Debug.LogWarning("[ViewBridge] 视图数量超限"); return; }

        PoolDefinition pool = config.ViewPrefab != null
            ? config.ViewPoolDef   // Phase 2: 正式 View
            : _debugViewPool;      // Phase 1: Debug View

        var go = _poolManager.Get(pool);
        go.transform.position = entity.Position;

        // append 到数组尾部
        int idx = _activeCount++;
        _viewGOs[idx] = go;
        _viewEntityIds[idx] = entity.Id.Value;
        _viewConfigs[idx] = config;

        // Phase 1: 设置 Debug 颜色
        if (config.ViewPrefab == null)
        {
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = config.DebugColor;
        }
    }

    /// <summary>每帧同步位置/朝向/HP 显示——零 GC for 循环遍历</summary>
    public void SyncAll(EntityManager manager)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            // 从 EntityManager 查 Entity 位置，同步到 View GO transform
            // Phase 1 Debug View: 更新 HP 文本
        }
    }

    /// <summary>Entity 回收时调用——归还 View GO 到池（swap-remove O(1)）</summary>
    public void OnEntityDespawned(Entity entity, EntityConfigSO config)
    {
        uint targetId = entity.Id.Value;
        for (int i = 0; i < _activeCount; i++)
        {
            if (_viewEntityIds[i] == targetId)
            {
                // 归还 GO 到池
                PoolDefinition pool = config.ViewPrefab != null
                    ? config.ViewPoolDef
                    : _debugViewPool;
                _poolManager.Return(pool, _viewGOs[i]);

                // swap-remove
                int last = _activeCount - 1;
                if (i != last)
                {
                    _viewGOs[i] = _viewGOs[last];
                    _viewEntityIds[i] = _viewEntityIds[last];
                    _viewConfigs[i] = _viewConfigs[last];
                }
                _viewGOs[last] = null;
                _viewConfigs[last] = null;
                _activeCount--;
                return;
            }
        }
    }
}
```

**关键决策**：
1. **Debug View Prefab** 是项目内置资产（一个带 SpriteRenderer + TextMesh 的最简 Prefab），通过 PoolDefinition 走 PoolManager 池化——零运行时 GC
2. **EntityViewBridge 是独立管理器**，不在 Entity 内部——BC-01.1"Entity 不持有 GO"不变
3. **Phase 2 自动切换**：策划在 EntityConfigSO 上填 ViewPrefab → EntityViewBridge 自动使用对应 PoolDefinition → 无需改代码
4. **EntityViewBridge 由游戏层 MonoBehaviour 持有并驱动**（和 EntityManager 同级）
5. **v2.3 零 GC 保证**（SA-005）：内部存储用预分配数组 + for 循环遍历，Despawn 用 swap-remove O(1)。避免 Dictionary 遍历的 Enumerator 装箱 GC

**事件钩子说明（v2.4 新增，GD-R4-004/012）**：

EntityViewBridge **只负责位置/朝向同步**。以下表现由游戏层订阅 EntityEventBus 事件自行处理：

| 表现 | 事件源 | 游戏层处理方式 |
|------|--------|--------------|
| 受击闪白 | `OnCollisionHit` | ViewBridge.SyncAll 中检查闪白状态，设置材质属性 |
| 击退位移 | `MovementComponent.Knockback` | 自动生效（位置变化 → SyncAll 同步） |
| 伤害数字 | `OnCollisionHit` | `EntityHitReactionHandler.OnHit` → `FloatingTextSystem.Spawn(pos, damage, color)` |
| 音效 | `OnCollisionHit` / `OnDeath` | 游戏层订阅事件 → 播放对应 AudioClip |
| 生成特效 | Entity Spawn | ViewBridge.OnEntitySpawned 中播放 `config.SpawnEffect` |
| 受击特效 | `OnCollisionHit` | 游戏层订阅事件 → 从 `config.HitEffect` PoolManager.Get() |
| 死亡特效 | `OnDeath` | 游戏层订阅事件 → 从 `config.DeathEffect` PoolManager.Get() |

**核心原则**：Entity 框架负责发事件，**不负责做表现**。框架确保事件携带足够信息（DamageContext），表现层的策划友好性由游戏层保证。

---

