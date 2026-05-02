# Phase 3A TDD — §3.0 玩家移动边界 & §3.1 空间查询 + AutoAim

> **所属文档**：[PHASE3A_TDD_INDEX.md](PHASE3A_TDD_INDEX.md) · v0.4  
> **本文件范围**：技术方案 P3.0 + P3.1

---

## 3.0 玩家移动边界

> **设计决策**：玩家移动边界是**系统规则**，不是组件行为——与边界击杀（KillOutOfBoundsEntities）同级，在 Bootstrap 层处理。

### 3.0.1 方案

在 `EntitySystemBootstrap.Update()` 中，`EntityManager.Tick()` 执行后（所有 MovementComponent 已更新位置）、`EntityViewBridge.SyncAll()` 之前，对 `Camp == Player` 的 Entity 做 Position Clamp。

**执行时序**：

```
Bootstrap.Update()
  ├── EntityManager.Tick(dt)           // 所有组件 Tick（含 Movement）
  ├── ClampPlayerPositions()           // ★ 新增：玩家位置约束
  ├── KillOutOfBoundsEntities()        // 已有：越界敌人击杀
  └── EntityViewBridge.SyncAll()       // View 同步
```

### 3.0.2 实现

> **v0.4 修正（ATK-007）**：PlayerMoveBounds 改为 Center + Size 两字段，对策划更直观。  
> **v0.4 修正（GD-001）**：新增 `OnPlayerHitBounds` 静态事件，预留触边反馈接口。  
> **v0.4 修正（GD-009）**：事件可能每帧触发（贴边滑动），消费者需自行节流。

```csharp
// EntitySystemBootstrap.cs — 新增字段 + 方法

[Header("玩家移动边界（P3.0）")]
[Tooltip("启用玩家移动边界约束")]
public bool EnablePlayerMoveBounds = true;

[Tooltip("活动区域中心（世界坐标）")]
public Vector2 PlayerBoundsCenter = Vector2.zero; // v0.4 ATK-007

[Tooltip("活动区域尺寸（宽, 高）")]
public Vector2 PlayerBoundsSize = new Vector2(9f, 14f); // v0.4 ATK-007
// 默认值说明：中心(0,0)，宽 9（-4.5 ~ 4.5），高 14（-7 ~ 7）
// 比 DanmakuSystem.WorldBounds 稍内缩，给视觉留安全边距

/// <summary>
/// 玩家 Entity 触碰移动边界时触发。可能每帧触发（贴边滑动时）。
/// ⚠️ 消费者应自行做节流/冷却——不要在此回调中每帧做重开销操作。
/// 推荐：View 层订阅后用 cooldown timer（如 0.3s）去频。(v0.4 GD-001/GD-009)
/// 参数：Entity, 原始位置, Clamp 后位置
/// </summary>
public static event System.Action<Entity, Vector2, Vector2> OnPlayerHitBounds;

// 内部辅助
private Rect GetPlayerBoundsRect() // v0.4 ATK-007
{
    return new Rect(
        PlayerBoundsCenter.x - PlayerBoundsSize.x * 0.5f,
        PlayerBoundsCenter.y - PlayerBoundsSize.y * 0.5f,
        PlayerBoundsSize.x, PlayerBoundsSize.y);
}

/// <summary>
/// 将所有玩家阵营 Entity 的位置 Clamp 到 PlayerMoveBounds 内。
/// 在 EntityManager.Tick 之后、ViewBridge.SyncAll 之前调用。
/// </summary>
private void ClampPlayerPositions()
{
    if (!EnablePlayerMoveBounds) return;
    
    var mgr = EntityManagerAccessor.Instance;
    if (mgr == null) return;
    
    var entities = mgr.ActiveEntities;
    var bounds = GetPlayerBoundsRect();
    
    for (int i = 0; i < entities.Count; i++)
    {
        var entity = entities[i];
        if (entity.Camp != Danmaku.EnumCamp.Player) continue;
        if (entity.IsPendingDespawn) continue;
        
        var pos = entity.Position;
        var clampedPos = new Vector2(
            UnityEngine.Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax),
            UnityEngine.Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax));
        
        // GD-001：触边事件
        if (clampedPos != pos)
        {
            entity.Position = clampedPos;
            OnPlayerHitBounds?.Invoke(entity, pos, clampedPos);
        }
    }
}
```

### 3.0.3 EntityConfigSO 变更

无。移动边界是系统级配置（Bootstrap Inspector），不是单个 Entity 的配置。

### 3.0.4 默认值设计理由

| 参数 | 默认值 | 理由 | 状态 |
|------|--------|------|------|
| `PlayerBoundsCenter` | (0, 0) | 以屏幕中心为对称 | `[占位符]` |
| `PlayerBoundsSize` | (9, 14) | 弹幕 WorldBounds 通常 (12, 20)，内缩约 1.5 单位留安全边距 | `[占位符]` |
| `EnablePlayerMoveBounds` | true | 飞行射击弹幕品类默认开启 | 固定 |

### 3.0.5 Gizmo 可视化

> **v0.4 确认（ATK-013/ATK-017）**：使用 `OnDrawGizmos`（非 Selected 版本），加 `#if UNITY_EDITOR` 包围。

```csharp
#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (!EnablePlayerMoveBounds) return;
    var center = new Vector3(PlayerBoundsCenter.x, PlayerBoundsCenter.y, 0);
    var size = new Vector3(PlayerBoundsSize.x, PlayerBoundsSize.y, 0.01f);
    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
    Gizmos.DrawCube(center, size);
    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
    Gizmos.DrawWireCube(center, size);
}
#endif
```

---

## 3.1 空间查询 + AutoAimComponent

### 3.1.1 FindEntitiesInRadius 实现

补完 `EntityManager` 中已预留的 stub：

```csharp
// EntityManager.cs — 替换 NotImplementedException

/// <summary>
/// 按半径搜索指定阵营的 Entity（零 GC，使用调用方预分配 buffer）。
/// 线性扫描 O(N)，20 Entity 下 < 0.01ms。
/// </summary>
public int FindEntitiesInRadius(
    Vector2 center, float radius, Danmaku.EnumCamp camp,
    Entity[] resultBuffer, int maxResults)
{
    int count = 0;
    float radiusSq = radius * radius;
    
    for (int i = 0; i < _activeEntities.Count && count < maxResults; i++)
    {
        var e = _activeEntities[i];
        if (e.IsPendingDespawn) continue;
        if (e.Camp != camp) continue;
        
        float distSq = (e.Position - center).sqrMagnitude;
        if (distSq <= radiusSq)
        {
            resultBuffer[count++] = e;
        }
    }
    return count;
}
```

### 3.1.2 FindNearestEntity 便捷 API

> **v0.4 修正（ATK-004）**：XML Doc 措辞修正——区分"Entity 引用安全持有"和"buffer 内容不缓存"。

```csharp
// EntityManager.cs — 新增

private static readonly Entity[] _sharedSearchBuffer = new Entity[64];

/// <summary>
/// 查找指定阵营的最近 Entity（零 GC，内部复用静态 buffer）。
/// 返回 null = 范围内无匹配。
/// 
/// 注意（v0.4 ATK-004 修正，原 UA-011）：
/// 返回的 Entity 引用可安全持有（如赋值给成员变量做瞄准目标）。
/// 但内部静态 _sharedSearchBuffer 的*内容*会被后续调用覆盖——
/// 不要缓存 buffer 本身的引用或遍历 buffer（它不是公开 API）。
/// 此方法为原子性操作（调用→遍历→返回最近），不会在中间触发用户回调。
/// </summary>
public Entity FindNearestEntity(Vector2 center, float radius, Danmaku.EnumCamp camp)
{
    int count = FindEntitiesInRadius(center, radius, camp, _sharedSearchBuffer, _sharedSearchBuffer.Length);
    if (count == 0) return null;
    
    Entity nearest = null;
    float nearestDistSq = float.MaxValue;
    for (int i = 0; i < count; i++)
    {
        float dSq = (_sharedSearchBuffer[i].Position - center).sqrMagnitude;
        if (dSq < nearestDistSq)
        {
            nearestDistSq = dSq;
            nearest = _sharedSearchBuffer[i];
        }
    }
    return nearest;
}
```

**线程安全说明**：`_sharedSearchBuffer` 是静态共享的。Unity 主线程单线程执行，无竞态。如未来需多线程查询，改为 ThreadLocal 或实例 buffer。

### 3.1.3 阵营敌对判断

> **v0.2 修正（UA-007）**：提取为独立工具类 `CampUtility`。  
> **v0.4 修正（SA-008）**：明确二元阵营定位声明。

```csharp
// CampUtility.cs — 路径：_Framework/EntitySystem/Scripts/Core/CampUtility.cs
namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 阵营工具类——提供阵营相关的通用判断方法。
    /// 
    /// 当前版本仅支持二元阵营（Player ↔ Enemy）。(v0.4 SA-008)
    /// 框架品类定位：弹幕射击 + 塔防核心，均为严格二元对立。
    /// 多阵营支持（PvP/三方/中立可攻击）属于 Phase 5 品类扩展范畴。
    /// 扩展方向：关系矩阵（bool[,]）或 [Flags] bitmask。
    /// </summary>
    public static class CampUtility
    {
        public static Danmaku.EnumCamp GetHostileCamp(Danmaku.EnumCamp self)
        {
            return self switch
            {
                Danmaku.EnumCamp.Player => Danmaku.EnumCamp.Enemy,
                Danmaku.EnumCamp.Enemy  => Danmaku.EnumCamp.Player,
                _ => Danmaku.EnumCamp.Neutral
            };
        }
    }
}
```

### 3.1.4 AutoAimComponent

> **v0.4 修正（GD-002）**：目标死亡时立即重搜（默认行为）。  
> **v0.4 修正（SA-004）**：SearchTarget 中添加 Debug.Assert。  
> **v0.4 修正（SA-011）**：Init 时 SearchTarget 搜索行为文档化。

```csharp
/// <summary>
/// 自动瞄准组件——定频搜索敌对阵营最近 Entity，暴露锁定目标信息。
/// 实现 ITargetProvider 接口，供 AI Action / AttackComponent 读取。
///
/// ComponentType.AutoAim = 5
/// TickOrder = 120（Attack 之前，Decision 之后）(v0.2 修正)
///
/// 设计决策：
/// - 定频搜索（默认 0.2s [占位符]），不是每帧——省 CPU
/// - 只锁定最近目标（最近优先策略），不做优先级/仇恨表
/// - 目标失效时立即重搜（v0.4 GD-002，默认行为，非可选配置）
/// - Init 时立即执行一次 SearchTarget
/// </summary>
public sealed class AutoAimComponent : IEntityComponent, ITickable, ITargetProvider
{
    // ── IEntityComponent ──
    public ComponentType Type => ComponentType.AutoAim;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;

    // ── ITickable ──
    public int TickOrder => TickOrders.AutoAim; // 120

    // ── ITargetProvider ──
    public bool HasTarget => _currentTarget != null 
                          && _currentTarget.IsAlive 
                          && !_currentTarget.IsPendingDespawn;
    public Vector2 TargetPosition => HasTarget ? _currentTarget.Position : _owner.Position;
    public float DistanceToTarget => HasTarget 
        ? (_currentTarget.Position - _owner.Position).magnitude 
        : float.MaxValue;

    // ── 公开状态 ──
    public Vector2 AimDirection { get; private set; }

    // ── 配置 ──
    private Entity _owner;
    private float _searchRadius;
    private float _searchInterval;
    private float _timer;
    private Entity _currentTarget;

    // ── 生命周期 ──

    public void Init(Entity owner)
    {
        _owner = owner;
        _searchRadius = owner.ConfigSO.AutoAimRadius;
        _searchInterval = owner.ConfigSO.AutoAimSearchInterval;
        _timer = 0f;
        _currentTarget = null;
        AimDirection = Vector2.up;
        IsActive = _searchRadius > 0f;
        
        // v0.4（SA-011）：Init 时 SearchTarget 搜索当前 active list。
        // 此时 Entity 自身尚未加入 active list（先 Init → 后 Register），
        // 因此不会瞄准自己。同帧 Spawn 的其他 Entity 也搜不到——
        // 这是正确行为，下次定频搜索会找到它们。
        if (IsActive) SearchTarget();
        if (HasTarget)
        {
            Vector2 dir = _currentTarget.Position - _owner.Position;
            if (dir.sqrMagnitude > 0.001f)
                AimDirection = dir.normalized;
        }
    }

    public void Reset()
    {
        _currentTarget = null;
        _owner = null;
        _timer = 0f;
        AimDirection = Vector2.up;
        IsActive = false;
    }

    // ── Tick ──

    public void Tick(float dt)
    {
        // v0.4（GD-002）：目标失效时立即重搜（不等下次定频搜索）
        if (_currentTarget != null && (!_currentTarget.IsAlive || _currentTarget.IsPendingDespawn))
        {
            _currentTarget = null;
            SearchTarget(); // 立即重搜
        }

        // 定频搜索
        _timer += dt;
        if (_timer >= _searchInterval)
        {
            _timer -= _searchInterval;
            SearchTarget();
        }

        // 更新瞄准方向
        if (HasTarget)
        {
            Vector2 dir = _currentTarget.Position - _owner.Position;
            if (dir.sqrMagnitude > 0.001f)
                AimDirection = dir.normalized;
        }
        else
        {
            float rad = _owner.Rotation * Mathf.Deg2Rad;
            AimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    private void SearchTarget()
    {
        var mgr = EntityManagerAccessor.Instance;
        Debug.Assert(mgr != null, "[AutoAimComponent] EntityManager not initialized!"); // v0.4 SA-004
        if (mgr == null) return;

        var hostileCamp = CampUtility.GetHostileCamp(_owner.Camp);
        _currentTarget = mgr.FindNearestEntity(_owner.Position, _searchRadius, hostileCamp);
    }
}
```

### 3.1.5 AttackComponent 集成 AutoAim

```csharp
// AttackComponent.cs — 替换 GetFireAngle 方法

private float GetFireAngle(Vector2 aimDir)
{
    // 优先级 1：AutoAim 锁定方向
    var autoAim = _owner.GetComponent(ComponentType.AutoAim);
    if (autoAim is ITargetProvider tp && tp.HasTarget)
    {
        var dir = ((AutoAimComponent)autoAim).AimDirection;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    // 优先级 2：DecisionCommand 瞄准方向
    if (aimDir.sqrMagnitude > 0.01f)
    {
        return Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
    }

    // 优先级 3：Entity 朝向
    return _owner.Rotation;
}
```

### 3.1.6 EntityConfigSO 新增字段

```csharp
[Header("自动瞄准（P3.1）")]
[Tooltip("搜索半径（0=不启用 AutoAim）")]
public float AutoAimRadius = 0f;

[Tooltip("搜索间隔（秒）[占位符]——需 gameplay 测试调整")]
[Min(0.05f)]
public float AutoAimSearchInterval = 0.2f;
```

### 3.1.7 TickOrders 新增常量

```csharp
// ITickable.cs — TickOrders 类新增/修改
public const int Buff = 50;       // BuffComponent（在 Decision 之前生效）
public const int AutoAim = 120;   // AutoAimComponent (Attack 之前)
public const int Skill = 160;     // SkillComponent（在 Attack 之后）
```

> **TickOrder 时序完整图**：
> ```
> Buff=50 → Decision=100 → AutoAim=120 → Attack=150 → Skill=160 → Health=250 → Movement=300 → Animation=400
> ```
