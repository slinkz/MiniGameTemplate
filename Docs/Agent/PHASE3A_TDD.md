# Entity-Component Phase 3A · TDD 草案

> **版本**：v0.3 (PK Round 2 收敛)  
> **日期**：2026-05-02  
> **状态**：✅ **PK 收敛 — 可实施**  
> **前置文档**：ENTITY_COMPONENT_TDD.md（v2.6）、PHASE3_DESIGN.md（游戏设计师评审版）  
> **决策记录**：待 ADR 编号  
> **适用范围**：MiniGameTemplate Entity-Component 框架 Phase 3A — 战斗能力扩展
>
> **天命人决策**（2026-05-01）：
> - Phase 3B（击杀计分/道具掉落/命数/难度/会话管理器）整体延后
> - 仅"玩家移动边界"（原 P3B.4）归入 Phase 3A 作为前置基建
> - Phase 3A 范围：P3.0 玩家移动边界 + P3.1 空间查询&AutoAim + P3.2 DamageDealer + P3.3 SkillComponent + P3.4 BuffComponent

---

## 一、设计目标

Phase 3A 在 Phase 2 已验收的 Entity-Component 框架基础上，扩展**战斗能力层**：

1. **玩家移动边界**（P3.0）— 基础体验保障，玩家不能飞出屏幕
2. **空间查询 + 自动瞄准**（P3.1）— Entity 能"感知周围"并锁定目标
3. **直接伤害路径**（P3.2）— 不走弹幕的 AOE/光环/陷阱伤害
4. **技能系统**（P3.3）— 最小版：配置驱动的效果槽，不替代 AttackComponent
5. **Buff/Debuff 系统**（P3.4）— 最小版：属性修正 + 持续时间

**设计支柱**（Design Pillars）：

| # | 支柱 | 约束 |
|---|------|------|
| 1 | 零 GC | 所有新增组件/服务禁止运行时分配 |
| 2 | 配置驱动 | 新增行为通过 SO 配置，不改代码 |
| 3 | 最小可用 | 每个子系统只做刚需，不过度设计 |
| 4 | 向下兼容 | 不破坏 Phase 1/2 已有组件的行为契约 |
| 5 | 真机 55fps | 20 Entity + 弹幕 ≥ 55fps（微信小游戏真机） |

---

## 二、行为契约扩展（BC-09 ~ BC-13）

> 以下契约扩展 `ENTITY_COMPONENT_TDD.md` 的行为契约层。

### BC-09 空间查询契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-09.1 | `EntityManager.FindEntitiesInRadius(center, radius, camp, buffer, max)` 返回范围内指定阵营的 Entity 列表，使用调用方传入的预分配 buffer，零 GC | 待实现 |
| BC-09.2 | `EntityManager.FindNearestEntity(center, radius, camp)` 返回最近单个 Entity（内部复用静态 buffer），零 GC | 待实现 |
| BC-09.3 | 空间查询不修改任何 Entity 状态，纯只读操作 | 待实现 |

### BC-10 自动瞄准契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-10.1 | AutoAimComponent 实现 `ITargetProvider` 接口，向 AI/Attack 系统暴露当前锁定目标 | 待实现 |
| BC-10.2 | AutoAimComponent 按可配置间隔（`SearchInterval`）定频搜索，非每帧搜索 | 待实现 |
| BC-10.3 | AutoAimComponent 仅搜索**敌对阵营**（阵营判断规则：Player ↔ Enemy 互为敌对） | 待实现 |
| BC-10.4 | AttackComponent 发射方向优先级：AutoAim 锁定方向 > DecisionCommand.AimDirection > Entity.Rotation | 待实现 |

### BC-11 直接伤害契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-11.1 | `DamageDealer.DealDamageToEntity(target, context)` 直接对单个 Entity 造成伤害，走完整 TakeDamage 管线（IDamageModifier 链） | 待实现 |
| BC-11.2 | `DamageDealer.DealAreaDamage(center, radius, camp, context, max)` 对范围内多个 Entity 造成伤害，返回实际命中数 | 待实现 |
| BC-11.3 | DamageDealer 是无状态静态工具类，不占 ComponentType 槽位 | 待实现 |

### BC-12 技能组件契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-12.1 | SkillComponent 持有**单个** SkillConfigSO 引用，管理 CD/前摇/后摇状态机 | 待实现 |
| BC-12.2 | 技能效果通过 `ISkillEffect` 策略接口实现，`[SerializeReference]` 序列化 | 待实现 |
| BC-12.3 | SkillComponent 与 AttackComponent **共存不替代**——简单 Entity 用 Attack，复杂用 Skill | 待实现 |
| BC-12.4 | SkillComponent 使用 `ComponentType.Skill = 6` 槽位（与 Attack=9 独立） | 待实现 |

### BC-13 Buff 组件契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-13.1 | BuffComponent 持有固定 8 槽 Buff 数组，预分配，零 GC | 待实现 |
| BC-13.2 | Buff 生命周期：挂载（Apply）→ 持续 Tick → 到期移除 → 效果恢复 | 待实现 |
| BC-13.3 | Buff 效果通过属性修正实现（加减乘），不直接修改 EntityConfigSO | 待实现 |
| BC-13.4 | 同 ID Buff 叠加规则：刷新持续时间（不叠层），可配置 | 待实现 |

---

## 三、技术方案

### 3.0 玩家移动边界

> **设计决策**：玩家移动边界是**系统规则**，不是组件行为——与边界击杀（KillOutOfBoundsEntities）同级，在 Bootstrap 层处理。

#### 3.0.1 方案

在 `EntitySystemBootstrap.Update()` 中，`EntityManager.Tick()` 执行后（所有 MovementComponent 已更新位置）、`EntityViewBridge.SyncAll()` 之前，对 `Camp == Player` 的 Entity 做 Position Clamp。

**执行时序**：

```
Bootstrap.Update()
  ├── EntityManager.Tick(dt)           // 所有组件 Tick（含 Movement）
  ├── ClampPlayerPositions()           // ★ 新增：玩家位置约束
  ├── KillOutOfBoundsEntities()        // 已有：越界敌人击杀
  └── EntityViewBridge.SyncAll()       // View 同步
```

#### 3.0.2 实现

```csharp
// EntitySystemBootstrap.cs — 新增字段 + 方法

[Header("玩家移动边界（P3.0）")]
[Tooltip("启用玩家移动边界约束")]
public bool EnablePlayerMoveBounds = true;

[Tooltip("玩家可活动区域（世界坐标 Rect）")]
public Rect PlayerMoveBounds = new Rect(-4.5f, -7f, 9f, 14f);
// 默认值说明：中心(0,-0)，宽 9（-4.5 ~ 4.5），高 14（-7 ~ 7）
// 比 DanmakuSystem.WorldBounds 稍内缩，给视觉留安全边距

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
    var bounds = PlayerMoveBounds;
    
    for (int i = 0; i < entities.Count; i++)
    {
        var entity = entities[i];
        if (entity.Camp != Danmaku.EnumCamp.Player) continue;
        if (entity.IsPendingDespawn) continue;
        
        var pos = entity.Position;
        pos.x = UnityEngine.Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax);
        pos.y = UnityEngine.Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax);
        entity.Position = pos;
    }
}
```

#### 3.0.3 EntityConfigSO 变更

无。移动边界是系统级配置（Bootstrap Inspector），不是单个 Entity 的配置。

#### 3.0.4 默认值设计理由

| 参数 | 默认值 | 理由 |
|------|--------|------|
| `PlayerMoveBounds` | Rect(-4.5, -7, 9, 14) | 弹幕 WorldBounds 通常为 Rect(-6, -10, 12, 20)，内缩约 1.5 单位留安全边距 |
| `EnablePlayerMoveBounds` | true | 飞行射击弹幕品类默认开启 |

#### 3.0.5 Gizmo 可视化

在 `OnDrawGizmos()` 中绘制 PlayerMoveBounds 矩形（蓝色半透明），方便策划在 Scene View 中确认活动区域。

```csharp
// EntitySystemBootstrap.OnDrawGizmos() — 追加
if (EnablePlayerMoveBounds)
{
    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.15f);
    var center = PlayerMoveBounds.center;
    var size = PlayerMoveBounds.size;
    Gizmos.DrawCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 0.01f));
    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);
    Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 0.01f));
}
```

---

### 3.1 空间查询 + AutoAimComponent

#### 3.1.1 FindEntitiesInRadius 实现

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

#### 3.1.2 FindNearestEntity 便捷 API

```csharp
// EntityManager.cs — 新增

private static readonly Entity[] _sharedSearchBuffer = new Entity[64];

/// <summary>
/// 查找指定阵营的最近 Entity（零 GC，内部复用静态 buffer）。
/// 返回 null = 范围内无匹配。
/// 
/// 注意（v0.3 UA-011）：内部使用静态共享 buffer，方法返回后 buffer 内容
/// 可能被后续调用覆盖。调用者应立即使用返回的 Entity 引用，不要保存 buffer 地址。
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

#### 3.1.3 阵营敌对判断

> **v0.2 修正（UA-007）**：提取为独立工具类 `CampUtility`，消除 ApplyBuffEffect → AutoAimComponent 的不合理依赖。

```csharp
// CampUtility.cs — 新增（v0.2 UA-007）
// 路径：_Framework/EntitySystem/Scripts/Core/CampUtility.cs
namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 阵营工具类——提供阵营相关的通用判断方法。
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

#### 3.1.4 AutoAimComponent

```csharp
/// <summary>
/// 自动瞄准组件——定频搜索敌对阵营最近 Entity，暴露锁定目标信息。
/// 实现 ITargetProvider 接口，供 AI Action / AttackComponent 读取。
///
/// ComponentType.AutoAim = 5
/// TickOrder = 120（Attack 之前，Decision 之后）(v0.2 修正：原 200，UA-001)
///
/// 设计决策：
/// - 定频搜索（默认 0.2s），不是每帧——省 CPU，射击品类够用
/// - 只锁定最近目标（最近优先策略），不做优先级/仇恨表
/// - 目标丢失（死亡/出范围）时自动清空，下次搜索重新锁定
/// - Init 时立即执行一次 SearchTarget，避免首帧瞄准方向为默认值 (v0.2 新增)
/// </summary>
public sealed class AutoAimComponent : IEntityComponent, ITickable, ITargetProvider
{
    // ── IEntityComponent ──
    public ComponentType Type => ComponentType.AutoAim;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;

    // ── ITickable ──
    public int TickOrder => TickOrders.AutoAim; // 120 (v0.2 修正)

    // ── ITargetProvider ──
    public bool HasTarget => _currentTarget != null 
                          && _currentTarget.IsAlive 
                          && !_currentTarget.IsPendingDespawn;
    public Vector2 TargetPosition => HasTarget ? _currentTarget.Position : _owner.Position;
    public float DistanceToTarget => HasTarget 
        ? (_currentTarget.Position - _owner.Position).magnitude 
        : float.MaxValue;

    // ── 公开状态 ──
    /// <summary>当前瞄准方向（归一化）。无目标时返回 Entity 朝向的 forward。</summary>
    public Vector2 AimDirection { get; private set; }

    // ── 配置（从 EntityConfigSO 读取）──
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
        AimDirection = Vector2.up; // 默认朝上
        IsActive = _searchRadius > 0f; // 半径为 0 则不激活
        
        // v0.2 新增（UA-001）：Init 时立即搜索，消除首帧瞄准偏差
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
        // 检查当前目标是否仍有效
        if (_currentTarget != null && (!_currentTarget.IsAlive || _currentTarget.IsPendingDespawn))
        {
            _currentTarget = null;
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
            // 无目标时使用 Entity 朝向
            float rad = _owner.Rotation * Mathf.Deg2Rad;
            AimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    private void SearchTarget()
    {
        var mgr = EntityManagerAccessor.Instance;
        if (mgr == null) return;

        var hostileCamp = CampUtility.GetHostileCamp(_owner.Camp); // v0.2 UA-007
        _currentTarget = mgr.FindNearestEntity(_owner.Position, _searchRadius, hostileCamp);
    }
}
```

#### 3.1.5 AttackComponent 集成 AutoAim

修改 `AttackComponent.GetFireAngle()` 增加 AutoAim 优先级：

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

#### 3.1.6 EntityConfigSO 新增字段

```csharp
[Header("自动瞄准（P3.1）")]
[Tooltip("搜索半径（0=不启用 AutoAim）")]
public float AutoAimRadius = 0f;

[Tooltip("搜索间隔（秒）")]
[Min(0.05f)]
public float AutoAimSearchInterval = 0.2f;
```

#### 3.1.7 TickOrders 新增常量

```csharp
// ITickable.cs — TickOrders 类新增/修改
public const int Buff = 50;       // BuffComponent（在 Decision 之前生效）
public const int AutoAim = 120;   // AutoAimComponent (v0.2 修正：原 200，移到 Attack 之前)
public const int Skill = 160;     // SkillComponent（在 Attack 之后）
```

> **TickOrder 时序完整图**（v0.2 修正）：
> ```
> Buff=50 → Decision=100 → AutoAim=120 → Attack=150 → Skill=160 → Health=250 → Movement=300 → Animation=400
> ```
> 
> 设计理由：
> - Buff 在最前（50）：属性修正需在 Decision/Attack 之前生效
> - AutoAim 在 Attack 之前（120）：Attack 需要读取当帧最新瞄准方向 (v0.2 UA-001)
> - Skill 在 Attack 之后（160）：Skill 是增强型攻击，基础攻击先执行

---

### 3.2 直接伤害路径（DamageDealer）

#### 3.2.1 设计决策

DamageDealer 是**无状态静态工具类**：

- 不占 ComponentType 槽位
- 不走弹幕系统（解决 AOE/光环/陷阱/技能直伤场景）
- 复用 HealthComponent.TakeDamage 管线（IDamageModifier 链生效）

#### 3.2.2 实现

```csharp
using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 直接伤害工具类——不走弹幕系统的伤害路径。
    /// 用途：AOE 技能、光环伤害、陷阱、环境伤害、治疗等。
    /// 
    /// 所有方法均为静态，无状态，零 GC（使用预分配 buffer）。
    /// </summary>
    public static class DamageDealer
    {
        private static readonly Entity[] _buffer = new Entity[64];
        private static bool _isProcessingArea; // v0.2 新增（UA-003）：重入保护

        /// <summary>
        /// 对单个 Entity 直接造伤。走完整 TakeDamage 管线。
        /// </summary>
        public static void DealDamageToEntity(Entity target, DamageContext context)
        {
            if (target == null || !target.IsAlive || target.IsPendingDespawn) return;
            
            var health = target.GetComponent(ComponentType.Health) as HealthComponent;
            if (health == null) return;
            
            health.TakeDamage(ref context);
        }

        /// <summary>
        /// 对范围内指定阵营的 Entity 造伤。返回实际命中数。
        /// 注意：不支持嵌套调用（v0.2 UA-003 行为约束）。
        /// </summary>
        public static int DealAreaDamage(
            Vector2 center, float radius, EnumCamp targetCamp,
            DamageContext baseContext, int maxTargets = 16)
        {
            // v0.2 新增（UA-003）：重入检测
            Debug.Assert(!_isProcessingArea, 
                "[DamageDealer] DealAreaDamage 不支持嵌套调用！请检查 OnDeath 回调链。");
            if (_isProcessingArea) return 0; // Release 模式下 fallback 安全退出
            
            var mgr = EntityManagerAccessor.Instance;
            if (mgr == null) return 0;

            _isProcessingArea = true;
            int count = 0;
            try  // v0.3 修正（UA-010）：try/finally 确保异常时 flag 正确 reset
            {
                count = mgr.FindEntitiesInRadius(center, radius, targetCamp, _buffer, 
                    Mathf.Min(maxTargets, _buffer.Length));

                for (int i = 0; i < count; i++)
                {
                    var ctx = baseContext; // struct 值拷贝，每个目标独立 context
                    var health = _buffer[i].GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null)
                    {
                        health.TakeDamage(ref ctx);
                    }
                }
            }
            finally
            {
                _isProcessingArea = false;
            }

            return count;
        }
    }
}
```

---

### 3.3 SkillComponent（最小可用版）

#### 3.3.1 设计哲学

一个 Skill = 一个 SO 配置（SkillConfigSO）+ N 个 SkillEffect（策略接口）。

**与 AttackComponent 的关系**：

- AttackComponent = 持续自动射击（定时器 + BulletPattern）
- SkillComponent = CD 管理的主动/被动技能（前摇 → 效果触发 → 后摇 → CD）
- 两者**共存不替代**：简单 Entity 只配 Attack，Boss 可同时配 Attack + Skill

#### 3.3.2 SkillConfigSO

```csharp
[CreateAssetMenu(menuName = "Entity/SkillConfig")]
public class SkillConfigSO : ScriptableObject
{
    [Header("基础")]
    public string DisplayName;
    
    [Tooltip("触发模式")]
    public SkillTriggerMode TriggerMode = SkillTriggerMode.Auto;
    
    [Header("时间轴")]
    [Tooltip("冷却时间（秒）")]
    [Min(0.1f)]
    public float CooldownTime = 5f;
    
    [Tooltip("前摇时间（秒，0=瞬发）")]
    [Min(0f)]
    public float CastTime = 0f;
    
    [Tooltip("后摇时间（秒）")]
    [Min(0f)]
    public float RecoveryTime = 0.5f;
    
    [Header("效果列表")]
    [SerializeReference]
    public ISkillEffect[] Effects = System.Array.Empty<ISkillEffect>();
}

public enum SkillTriggerMode : byte
{
    Manual = 0,     // 玩家手动触发
    Auto = 1,       // CD 就绪自动触发（= Passive，v0.2 UA-005 语义统一）
    // v0.2 修正（UA-005）：移除 Passive 模式。
    // 原 Passive 与 Auto 行为完全相同（CD 就绪→触发→Recovery→CD→循环），
    // "每帧光环"类效果留给 Phase 4 的 AuraComponent 或 Buff.DOT 机制。
}
```

#### 3.3.3 ISkillEffect 策略接口

```csharp
/// <summary>
/// 技能效果策略接口。通过 [SerializeReference] 序列化到 SkillConfigSO。
/// </summary>
public interface ISkillEffect
{
    /// <summary>技能触发时执行</summary>
    void Execute(SkillContext ctx);
}

/// <summary>技能执行上下文（struct，零 GC）</summary>
public struct SkillContext
{
    public Entity Caster;           // 施法者
    public Vector2 CastPosition;    // 施法位置（= Caster.Position）
    public Vector2 AimDirection;    // 瞄准方向（来自 AutoAim 或 Decision）
    public float DeltaTime;         // 当前帧 dt（供需要时间感知的 ISkillEffect 扩展使用）v0.3 UA-015
}
```

#### 3.3.4 内置 SkillEffect 实现

**FireBulletsEffect**（通过技能发射弹幕）：

```csharp
[System.Serializable]
public class FireBulletsEffect : ISkillEffect
{
    [Tooltip("弹幕 Pattern")]
    public BulletPatternSO Pattern;
    
    [Tooltip("发射偏移")]
    public Vector2 FireOffset;
    
    public void Execute(SkillContext ctx)
    {
        if (Pattern == null) return;
        var ds = DanmakuSystem.Instance;
        if (ds == null) return;
        
        Vector2 pos = ctx.CastPosition + FireOffset;
        float angle = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x) * Mathf.Rad2Deg;
        ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value);
    }
}
```

**AreaDamageEffect**（AOE 直伤）：

```csharp
[System.Serializable]
public class AreaDamageEffect : ISkillEffect
{
    [Tooltip("伤害半径")]
    public float Radius = 3f;
    
    [Tooltip("基础伤害")]
    public int BaseDamage = 50;
    
    [Tooltip("最大目标数")]
    public int MaxTargets = 16;
    
    public void Execute(SkillContext ctx)
    {
        var hostileCamp = CampUtility.GetHostileCamp(ctx.Caster.Camp); // v0.2 UA-007
        var dmgCtx = new DamageContext
        {
            BaseDamage = BaseDamage,
            AttackerId = ctx.Caster.Id,
            SourcePosition = ctx.CastPosition,
            HasSourcePosition = true,
        };
        DamageDealer.DealAreaDamage(ctx.CastPosition, Radius, hostileCamp, dmgCtx, MaxTargets);
    }
}
```

#### 3.3.5 SkillComponent

```csharp
/// <summary>
/// 技能组件——管理单个技能的 CD/前摇/后摇状态机，触发效果列表。
/// 
/// ComponentType.Skill = 6
/// TickOrder = 160（Skill 阶段，Attack 之后、AutoAim 之前）
/// 
/// 状态机：
///   Idle → (触发) → Casting → (前摇结束) → Execute Effects → Recovery → (后摇结束) → Cooldown → Idle
/// 
/// 设计约束：
/// - 单技能槽（不是技能栏）——一个 Entity 一个技能
/// - 前摇期间可被打断（死亡/眩晕），后摇期间不可中断
/// - Phase 3A 不做技能打断机制（留给 Phase 4 FSM）
/// </summary>
public sealed class SkillComponent : IEntityComponent, ITickable
{
    // ── IEntityComponent ──
    public ComponentType Type => ComponentType.Skill;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;

    // ── ITickable ──
    public int TickOrder => TickOrders.Skill; // 160

    // ── 状态 ──
    public SkillState CurrentState { get; private set; }
    public float CooldownRemaining { get; private set; }
    
    // ── 内部 ──
    private Entity _owner;
    private SkillConfigSO _config;
    private float _stateTimer;

    public void Init(Entity owner)
    {
        _owner = owner;
        _config = owner.ConfigSO.SkillConfig;
        CurrentState = SkillState.Idle;
        CooldownRemaining = 0f;
        _stateTimer = 0f;
        IsActive = _config != null;
    }

    public void Reset()
    {
        _owner = null;
        _config = null;
        CurrentState = SkillState.Idle;
        CooldownRemaining = 0f;
        _stateTimer = 0f;
        IsActive = false;
    }

    public void Tick(float dt)
    {
        if (_config == null) return;

        switch (CurrentState)
        {
            case SkillState.Idle:
                if (ShouldTrigger())
                {
                    if (_config.CastTime > 0)
                    {
                        CurrentState = SkillState.Casting;
                        _stateTimer = _config.CastTime;
                    }
                    else
                    {
                        ExecuteEffects(dt); // v0.2 修正（UA-004）
                        EnterRecovery();
                    }
                }
                break;

            case SkillState.Casting:
                _stateTimer -= dt;
                if (_stateTimer <= 0)
                {
                    ExecuteEffects(dt); // v0.2 修正（UA-004）
                    EnterRecovery();
                }
                break;

            case SkillState.Recovery:
                _stateTimer -= dt;
                if (_stateTimer <= 0)
                {
                    CooldownRemaining = _config.CooldownTime;
                    CurrentState = SkillState.Cooldown;
                }
                break;

            case SkillState.Cooldown:
                CooldownRemaining -= dt;
                if (CooldownRemaining <= 0)
                {
                    CooldownRemaining = 0;
                    CurrentState = SkillState.Idle;
                }
                break;
        }
    }

    private bool ShouldTrigger()
    {
        if (CooldownRemaining > 0) return false;

        return _config.TriggerMode switch
        {
            SkillTriggerMode.Auto => true,  // CD 就绪即触发
            SkillTriggerMode.Manual => GetDecisionWantsSkill(),
            _ => false
        };
    }

    private bool GetDecisionWantsSkill()
    {
        // 复用 DecisionCommand 的 WantsAttack（Phase 3A 简化，后续可扩展 WantsSkill）
        var ctrl = _owner.GetComponent(ComponentType.Control) as IDecisionMaker;
        var ai = _owner.GetComponent(ComponentType.AI) as IDecisionMaker;
        var dm = ctrl ?? ai;
        return dm?.GetDecision().WantsAttack ?? false;
    }

    // v0.2 修正（UA-004）：增加 dt 参数，正确赋值 SkillContext.DeltaTime
    private void ExecuteEffects(float dt)
    {
        var ctx = new SkillContext
        {
            Caster = _owner,
            CastPosition = _owner.Position,
            AimDirection = GetAimDirection(),
            DeltaTime = dt,
        };

        for (int i = 0; i < _config.Effects.Length; i++)
        {
            _config.Effects[i]?.Execute(ctx);
        }
    }

    private void EnterRecovery()
    {
        if (_config.RecoveryTime > 0)
        {
            CurrentState = SkillState.Recovery;
            _stateTimer = _config.RecoveryTime;
        }
        else
        {
            CooldownRemaining = _config.CooldownTime;
            CurrentState = SkillState.Cooldown;
        }
    }

    private Vector2 GetAimDirection()
    {
        var autoAim = _owner.GetComponent(ComponentType.AutoAim) as ITargetProvider;
        if (autoAim != null && autoAim.HasTarget)
            return (autoAim.TargetPosition - _owner.Position).normalized;
        
        float rad = _owner.Rotation * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}

public enum SkillState : byte
{
    Idle = 0,
    Casting = 1,    // 前摇
    Recovery = 2,   // 后摇
    Cooldown = 3,
}
```

#### 3.3.6 EntityConfigSO 新增字段

```csharp
[Header("技能（P3.3）")]
[Tooltip("技能配置（null=不启用 Skill 组件）")]
public SkillConfigSO SkillConfig;
```

---

### 3.4 BuffComponent（最小版）

#### 3.4.1 设计哲学

Buff = 持续时间 + 属性修正。最小版只做**属性加成/减成**（乘法修正器），不做 DOT、触发器、层数叠加等复杂机制。

#### 3.4.2 BuffConfigSO

```csharp
[CreateAssetMenu(menuName = "Entity/BuffConfig")]
public class BuffConfigSO : ScriptableObject
{
    [Header("基础")]
    public string DisplayName;
    public int BuffId;  // 唯一标识（同 ID 刷新持续时间）
    
    [Header("持续时间")]
    [Tooltip("持续秒数（0=永久，需手动移除）")]
    [Min(0f)]
    public float Duration = 5f;
    
    [Header("属性修正（乘法：最终值 = 基础值 × Modifier）")]
    [Tooltip("移速倍率（1=不变，0.5=减速50%，2=加速100%）")]
    public float MoveSpeedModifier = 1f;
    
    [Tooltip("攻击间隔倍率（1=不变，0.5=攻速翻倍，2=减速50%）")]
    public float AttackIntervalModifier = 1f;
    
    [Tooltip("受伤倍率（1=不变，0.5=减伤50%，2=受伤翻倍）")]
    public float DamageTakenModifier = 1f;
}
```

#### 3.4.3 BuffComponent

```csharp
/// <summary>
/// Buff/Debuff 组件——管理 Entity 身上的属性修正效果。
/// 
/// ComponentType.Buff = 10（新增枚举值）
/// TickOrder = 50（在 Decision 之前生效，确保属性修正对当帧决策可见）
///
/// 设计约束：
/// - 固定 8 槽位预分配数组，零 GC
/// - 同 ID Buff 刷新持续时间（不叠层）
/// - 属性修正为乘法叠加（多个 Buff 的修正器相乘）
/// </summary>
public sealed class BuffComponent : IEntityComponent, ITickable
{
    // ── IEntityComponent ──
    public ComponentType Type => ComponentType.Buff;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;

    // ── ITickable ──
    public int TickOrder => TickOrders.Buff; // 50

    // ── 常量 ──
    private const int MAX_BUFFS = 8;

    // ── 内部状态 ──
    private Entity _owner;
    private readonly BuffSlot[] _slots = new BuffSlot[MAX_BUFFS];
    private int _activeCount;

    // ── 聚合后的修正值（供其他组件读取）──
    public float MoveSpeedModifier { get; private set; } = 1f;
    public float AttackIntervalModifier { get; private set; } = 1f;
    public float DamageTakenModifier { get; private set; } = 1f;

    // ── 生命周期 ──

    public void Init(Entity owner)
    {
        _owner = owner;
        _activeCount = 0;
        RecalcModifiers();
        // v0.2 注释说明（UA-008）：BuffComponent 挂载即激活，无需额外配置条件。
        // 策划确保只有需要 Buff 系统的 Entity 才在 Components 中配 Buff。
        // 空 Tick 开销可忽略（仅一次 _activeCount==0 检查）。
        IsActive = true;
    }

    public void Reset()
    {
        _owner = null;
        _activeCount = 0;
        for (int i = 0; i < MAX_BUFFS; i++)
            _slots[i] = default;
        RecalcModifiers();
        // v0.3（UA-012）：Reset 不调 SyncMoveSpeedToMovement()。
        // ResetAll 按枚举顺序遍历，Movement(3) 先于 Buff(10) Reset，
        // 此时 Movement._modifierCount 已归零。各组件只管自己的 Reset。
        IsActive = false;
    }

    // ── Tick ──

    public void Tick(float dt)
    {
        bool dirty = false;
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            if (_slots[i].Duration <= 0f) continue; // 永久 Buff
            
            _slots[i].RemainingTime -= dt;
            if (_slots[i].RemainingTime <= 0f)
            {
                RemoveAtIndex(i);
                dirty = true;
            }
        }
        if (dirty) RecalcModifiers();
    }

    // ── 公共 API ──

    /// <summary>施加 Buff。同 ID 刷新持续时间。返回是否成功。</summary>
    public bool ApplyBuff(BuffConfigSO config)
    {
        if (config == null) return false;

        // 同 ID 检查：刷新持续时间
        for (int i = 0; i < _activeCount; i++)
        {
            if (_slots[i].BuffId == config.BuffId)
            {
                _slots[i].RemainingTime = config.Duration;
                return true;
            }
        }

        // 槽位满
        if (_activeCount >= MAX_BUFFS)
        {
            Debug.LogWarning($"[BuffComponent] Buff 槽位已满({MAX_BUFFS})，无法施加: {config.DisplayName}");
            return false;
        }

        // 新增
        _slots[_activeCount] = new BuffSlot
        {
            BuffId = config.BuffId,
            Duration = config.Duration,
            RemainingTime = config.Duration,
            MoveSpeedMod = config.MoveSpeedModifier,
            AttackIntervalMod = config.AttackIntervalModifier,
            DamageTakenMod = config.DamageTakenModifier,
        };
        _activeCount++;
        RecalcModifiers();
        return true;
    }

    /// <summary>按 BuffId 移除指定 Buff</summary>
    public bool RemoveBuff(int buffId)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            if (_slots[i].BuffId == buffId)
            {
                RemoveAtIndex(i);
                RecalcModifiers();
                return true;
            }
        }
        return false;
    }

    /// <summary>当前激活 Buff 数量</summary>
    public int ActiveBuffCount => _activeCount;

    // ── 内部 ──

    private void RemoveAtIndex(int index)
    {
        // swap-remove
        _activeCount--;
        if (index != _activeCount)
            _slots[index] = _slots[_activeCount];
        _slots[_activeCount] = default;
    }

    private void RecalcModifiers()
    {
        float move = 1f, attack = 1f, damage = 1f;
        for (int i = 0; i < _activeCount; i++)
        {
            move *= _slots[i].MoveSpeedMod;
            attack *= _slots[i].AttackIntervalMod;
            damage *= _slots[i].DamageTakenMod;
        }
        MoveSpeedModifier = move;
        AttackIntervalModifier = attack;
        DamageTakenModifier = damage;
    }

    // ── 内部结构 ──

    private struct BuffSlot
    {
        public int BuffId;
        public float Duration;          // 总持续时间（0=永久）
        public float RemainingTime;     // 剩余时间
        public float MoveSpeedMod;
        public float AttackIntervalMod;
        public float DamageTakenMod;
    }
}
```

#### 3.4.4 组件集成：Buff 修正器生效

> **v0.2 修正（UA-006）**：Buff 速度修正**通过已有的 SpeedModifier 系统注入**，而非绕过它。
> **v0.3 修正（UA-009）**：MovementComponent 现有接口为 by-slot（`AddSpeedModifier(float)` → 返回 int slot），
> 需新增 by-ID 重载以支持 Buff 系统。

Buff 的属性修正需要被对应组件读取：

- **MovementComponent**：通过 `AddOrUpdateSpeedModifier(BUFF_MODIFIER_ID, value)` 注入（v0.3 UA-009）
- **AttackComponent**：`effectiveInterval = baseInterval * buffComp.AttackIntervalModifier`
- **HealthComponent（TakeDamage）**：可通过 IDamageModifier 实现 `DamageTakenModifier` 效果

##### MovementComponent by-ID 接口扩展（v0.3 UA-009）

```csharp
// MovementComponent.cs — 新增字段 + by-ID 重载

private readonly int[] _modifierIds = new int[MAX_MODIFIERS]; // 与 _speedModifiers 对应

/// <summary>
/// 按 ID 添加或更新速度修正器。同 ID 覆盖，不同 ID 新增。
/// 返回是否成功（false = 槽位已满且无同 ID 可更新）。
/// </summary>
public bool AddOrUpdateSpeedModifier(int id, float multiplier)
{
    // 先查已有同 ID → 覆盖
    for (int i = 0; i < _modifierCount; i++)
    {
        if (_modifierIds[i] == id)
        {
            _speedModifiers[i] = multiplier;
            return true;
        }
    }
    // 无同 ID → 新增
    if (_modifierCount >= MAX_MODIFIERS) return false;
    _modifierIds[_modifierCount] = id;
    _speedModifiers[_modifierCount] = multiplier;
    _modifierCount++;
    return true;
}

/// <summary>按 ID 移除速度修正器。</summary>
public bool RemoveSpeedModifierById(int id)
{
    for (int i = 0; i < _modifierCount; i++)
    {
        if (_modifierIds[i] == id)
        {
            RemoveSpeedModifier(i); // 复用已有 swap-remove
            return true;
        }
    }
    return false;
}

// 向下兼容：原有 AddSpeedModifier(float) 内部给 id=-1（匿名），不与 by-ID 冲突。
```

##### Buff → Movement 同步

```csharp
// BuffComponent — SyncMoveSpeedToMovement（v0.3 修正 UA-009）
private void SyncMoveSpeedToMovement()
{
    var movement = _owner.GetComponent(ComponentType.Movement) as MovementComponent;
    if (movement == null) return;
    
    const int BUFF_MODIFIER_ID = 99; // 保留 ID，Buff 专用
    if (Mathf.Approximately(MoveSpeedModifier, 1f))
        movement.RemoveSpeedModifierById(BUFF_MODIFIER_ID);
    else
        movement.AddOrUpdateSpeedModifier(BUFF_MODIFIER_ID, MoveSpeedModifier);
}

// RecalcModifiers 末尾调用（仅正常运行时，不在 Reset 路径调用——v0.3 UA-012）：
// SyncMoveSpeedToMovement();

// AttackComponent.Tick — 修改攻击间隔判断
float effectiveInterval = _attackInterval;
var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
if (buff != null)
    effectiveInterval *= buff.AttackIntervalModifier;
```

> **设计决策（UA-006/009/012）**：
> - MovementComponent 保持单一速度修正接口，新增 by-ID 重载与 by-slot 共存
> - Buff 系统作为修正器的一个来源注入，不绕过已有接口
> - `GetFinalSpeed()` 语义始终一致：`finalSpeed = baseSpeed * Π(所有 SpeedModifier)`
> - **Reset 时不调 SyncMoveSpeedToMovement**——ResetAll 按枚举顺序遍历，Movement(3) 先于 Buff(10) Reset，各组件只管自己的 Reset

#### 3.4.5 ComponentType 新增

```csharp
// ComponentType.cs — 新增
Buff = 10,
// 预留 11~15
```

#### 3.4.5b EntityPool.CreateComponent 工厂更新 (v0.2 新增，UA-002)

```csharp
// EntityPool.cs — CreateComponent switch 补充 case
case ComponentType.AutoAim:  return new AutoAimComponent();
case ComponentType.Skill:    return new SkillComponent();
case ComponentType.Buff:     return new BuffComponent();
```

#### 3.4.6 ApplyBuffEffect（Skill→Buff 桥接）

```csharp
[System.Serializable]
public class ApplyBuffEffect : ISkillEffect
{
    [Tooltip("要施加的 Buff 配置")]
    public BuffConfigSO BuffConfig;
    
    [Tooltip("施加给自己还是目标（true=自身，false=敌对范围内最近目标）")]
    public bool ApplyToSelf = true;
    
    public void Execute(SkillContext ctx)
    {
        Entity target = ApplyToSelf 
            ? ctx.Caster 
            : EntityManagerAccessor.Instance?.FindNearestEntity(
                ctx.CastPosition, 5f, CampUtility.GetHostileCamp(ctx.Caster.Camp)); // v0.2 UA-007
        
        if (target == null) return;
        var buffComp = target.GetComponent(ComponentType.Buff) as BuffComponent;
        buffComp?.ApplyBuff(BuffConfig);
    }
}
```

---

## 四、ComponentType 槽位规划（Phase 3A 更新）

```
0  = State       ✅ Phase 1
1  = Health      ✅ Phase 1
2  = Animation   ✅ Phase 1
3  = Movement    ✅ Phase 1
4  = Collision   ✅ Phase 2
5  = AutoAim     🔜 Phase 3A (P3.1)
6  = Skill       🔜 Phase 3A (P3.3)
7  = Control     ✅ Phase 1
8  = AI          ✅ Phase 1
9  = Attack      ✅ Phase 1
10 = Buff        🔜 Phase 3A (P3.4)
11~15 = 预留
```

## 五、TickOrder 时序图（Phase 3A 更新）

```
Buff=50 → Decision=100 → AutoAim=120 → Attack=150 → Skill=160 → Health=250 → Movement=300 → Animation=400
  ↑                                                                                               ↑
 最先生效                                                                                       最后执行
 属性修正                                                                                       视觉更新
```

---

## 六、实施步骤（P3.0 ~ P3.5）

| 步骤 | 内容 | 预估工时 | 依赖 |
|------|------|---------|------|
| **P3.0** | 玩家移动边界（Bootstrap.ClampPlayerPositions + Gizmo） | 0.5h | 无 |
| **P3.1** | 空间查询 + AutoAimComponent + AttackComponent 集成 | 2~3h | P3.0 |
| **P3.2** | DamageDealer 静态工具类 | 1h | P3.1（依赖 FindEntitiesInRadius） |
| **P3.3** | SkillComponent + SkillConfigSO + 内置 Effect（FireBullets/AreaDamage） | 2~3h | P3.1 + P3.2 |
| **P3.4** | BuffComponent + BuffConfigSO + 组件集成 + ApplyBuffEffect | 2~3h | P3.3 |
| **P3.5** | 集成验收 + 真机性能验证 | 1~2h | P3.0~P3.4 全部完成 |

**总计预估**：8~12 小时（1.5~2 天）

---

## 七、验收矩阵（14 项）

| # | 测试项 | 通过条件 | 步骤 |
|---|--------|---------|------|
| 1 | 玩家移动边界 | 玩家 Entity 无法移出 PlayerMoveBounds 矩形 | P3.0 |
| 2 | 边界 Gizmo | Scene View 可见蓝色矩形框标识活动区域 | P3.0 |
| 3 | FindEntitiesInRadius | 正确返回范围内指定阵营 Entity，范围外不返回 | P3.1 |
| 4 | FindNearestEntity | 返回最近的一个，无匹配返回 null | P3.1 |
| 5 | AutoAimComponent 锁定 | 敌方 Entity 自动锁定最近玩家，AimDirection 指向目标 | P3.1 |
| 6 | AutoAim + Attack 联动 | 弹幕朝 AutoAim 锁定方向发射（优先级高于 Entity 朝向） | P3.1 |
| 7 | DamageDealer 单体 | `DealDamageToEntity` 正确扣血，走 IDamageModifier 链 | P3.2 |
| 8 | DamageDealer AOE | `DealAreaDamage` 范围内多个 Entity 同时扣血，返回命中数 | P3.2 |
| 9 | SkillComponent CD | CD 期间不可释放，CD 结束后可再次触发 | P3.3 |
| 10 | SkillComponent 前摇/后摇 | CastTime > 0 时先进入 Casting 状态，时间到后执行 Effects | P3.3 |
| 11 | AreaDamageEffect | 技能触发 AOE 直伤，范围内敌方 Entity 扣血 | P3.3 |
| 12 | BuffComponent 生命周期 | Apply → 持续 → 到期自动移除 → 修正值恢复 | P3.4 |
| 13 | Buff 属性修正 | 减速 Buff 使 MovementComponent 速度降低；攻速 Buff 使攻击间隔缩短 | P3.4 |
| 14 | 真机性能 | 20 Entity + AutoAim + 弹幕 ≥ 55fps（微信小游戏真机） | P3.5 |

---

## 八、架构决策摘要

| 决策 | 选型 | 理由 |
|------|------|------|
| 玩家移动边界 | Bootstrap 层 Clamp | 系统规则，不是组件行为，与 KillBounds 同级 |
| 空间查询算法 | 线性扫描 O(N) | 20 Entity 下 < 0.01ms，无需空间分区 |
| AutoAim 搜索策略 | 定频 + 最近优先 | 0.2s 间隔省 CPU，不做仇恨/优先级 |
| DamageDealer | 静态工具类 | 无状态，不占 ComponentType 槽位 |
| SkillComponent vs AttackComponent | 共存不替代 | 简单 Entity 用 Attack，复杂用 Skill |
| Buff 修正模型 | 乘法叠加 | 多个 Buff 修正器相乘，简单可预测 |
| Buff 叠加规则 | 同 ID 刷新时间 | 最小版不做叠层，够用 |
| Buff 最大数量 | 8 槽固定数组 | 预分配零 GC，射击品类够用 |
| Buff TickOrder | 50（最早） | 属性修正需在 Decision/Attack 之前生效 |
| ComponentType.Buff | 10 | 不侵占现有 0~9 的槽位 |

---

## 九、风险与已知限制

| 风险 | 影响 | 缓解 |
|------|------|------|
| FindEntitiesInRadius 线性扫描在 Entity 数量增长后性能下降 | >100 Entity 时可能超 0.1ms | 后续可引入空间分区（Grid/Quadtree），API 不变 |
| AutoAim 搜索间隔导致目标切换延迟 | 0.2s 内目标可能已移动/死亡 | 每帧检查目标有效性，仅搜索是定频的 |
| SkillComponent 手动触发复用 WantsAttack | 无法区分"要攻击"和"要放技能" | Phase 4 可扩展 DecisionCommand 新增 WantsSkill 字段 |
| BuffComponent 乘法叠加可能导致极端值 | 多个减速 Buff 可能使速度趋近 0 | 在 MovementComponent 中加 MinSpeed Clamp（P3.4 实施时添加） |
| SkillEffect [SerializeReference] 序列化 | WebGL 下 SerializeReference 有反序列化 bug（Unity 特定版本） | 当前 Unity 版本已修复，真机验证确认 |

---

## 十、未决项（Phase 3B / Phase 4）

以下功能已在游戏设计师评审中识别但天命人决策延后：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 1 | 击杀计分（ScoreManager + Combo） | Phase 3 设计评审 | Phase 3B |
| 2 | 道具掉落/拾取（DropTableSO + PickupComponent） | Phase 3 设计评审 | Phase 3B |
| 3 | 玩家命数（LivesManager + 重生无敌 + 广告续命） | Phase 3 设计评审 | Phase 3B |
| 4 | 难度渐进扩展（DifficultyScaler + 敌人数量/频率） | Phase 3 设计评审 | Phase 3B |
| 5 | 游戏会话管理器（ShooterSessionManager） | Phase 3 设计评审 | Phase 3B |
| 6 | FSM 状态机编辑器 | ENTITY_COMPONENT_TDD Phase 3 | Phase 4 |
| 7 | 技能打断机制（眩晕/击飞中断前摇） | Phase 3A BC-12 | Phase 4 |
| 8 | DecisionCommand 扩展 WantsSkill 字段 | Phase 3A 风险项 | Phase 4 |

---

## 十一、文件变更清单

### 新增文件

| 文件 | 目录 | 步骤 |
|------|------|------|
| `AutoAimComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.1 |
| `CampUtility.cs` | `_Framework/EntitySystem/Scripts/Core/` | P3.1 (v0.2 UA-007) |
| `DamageDealer.cs` | `_Framework/EntitySystem/Scripts/Core/` | P3.2 |
| `SkillConfigSO.cs` | `_Framework/EntitySystem/Scripts/Config/` | P3.3 |
| `ISkillEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/` | P3.3 |
| `SkillContext.cs` | `_Framework/EntitySystem/Scripts/Skill/` | P3.3 |
| `FireBulletsEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.3 |
| `AreaDamageEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.3 |
| `ApplyBuffEffect.cs` | `_Framework/EntitySystem/Scripts/Skill/Effects/` | P3.4 |
| `SkillComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.3 |
| `BuffConfigSO.cs` | `_Framework/EntitySystem/Scripts/Config/` | P3.4 |
| `BuffComponent.cs` | `_Framework/EntitySystem/Scripts/Components/` | P3.4 |

### 修改文件

| 文件 | 变更内容 | 步骤 |
|------|---------|------|
| `EntitySystemBootstrap.cs` | 新增 `ClampPlayerPositions()` + PlayerMoveBounds 字段 + Gizmo | P3.0 |
| `EntityManager.cs` | `FindEntitiesInRadius` 补完实现 + `FindNearestEntity` 新增 | P3.1 |
| `ITickable.cs` (TickOrders) | **修改** `AutoAim = 200 → 120`（v0.3 UA-013），新增 `Buff = 50` / `Skill = 160` 常量 | P3.1 |
| `ComponentType.cs` | 新增 `Buff = 10` | P3.4 |
| `EntityConfigSO.cs` | 新增 AutoAimRadius / AutoAimSearchInterval / SkillConfig 字段 | P3.1 + P3.3 |
| `EntityConfigSOEditor.cs` | 补齐新字段绘制（AutoAim / Skill / Buff 分段） | P3.1~P3.4 |
| `EntityPool.cs` | 组件工厂补充 AutoAim / Skill / Buff case | P3.1~P3.4 |
| `MovementComponent.cs` | 新增 `_modifierIds[]` + `AddOrUpdateSpeedModifier(id,mult)` / `RemoveSpeedModifierById(id)` by-ID 重载（v0.3 UA-009） | P3.4 |
| `AttackComponent.cs` | `GetFireAngle` 增加 AutoAim 优先级 + `TickOrder` 改用 `TickOrders.Attack` 常量（v0.3 UA-013） | P3.1 |

---

**文档结束。天命人请审阅，确认后逐步推进实施。** 🎯

---

## 变更日志

### v0.3 (2026-05-02) — PK Round 2 收敛

| 修正 ID | 变更 |
|--------|------|
| UA-009 | MovementComponent 新增 `_modifierIds[]` + `AddOrUpdateSpeedModifier(int id, float)` / `RemoveSpeedModifierById(int id)` by-ID 重载；`SyncMoveSpeedToMovement` 改用 by-ID API |
| UA-010 | DamageDealer.DealAreaDamage 使用 `try/finally` 包裹，确保 `_isProcessingArea` 在异常路径下正确 reset |
| UA-011 | FindNearestEntity XML Doc 补充静态 buffer 原子性声明 |
| UA-012 | BuffComponent.Reset 明确不调用 SyncMoveSpeedToMovement（各组件自管 Reset） |
| UA-013 | 文件变更清单补齐 AutoAim=200→120 修改 + AttackComponent 改用 TickOrders.Attack 常量 |
| UA-014 | 删除 AutoAimComponent 内 private static GetHostileCamp 死代码 |
| UA-015 | SkillContext.DeltaTime 注释移除已废弃的 Passive 模式引用 |

### v0.2 (2026-05-01) — PK Round 1 修正

| 修正 ID | 变更 |
|--------|------|
| UA-001 | AutoAim TickOrder 从 200→120，Init 时立即 SearchTarget |
| UA-002 | EntityPool.CreateComponent 补充 AutoAim/Skill/Buff case |
| UA-003 | DamageDealer 新增 `_isProcessingArea` 重入保护 |
| UA-004 | ExecuteEffects(float dt)，ctx.DeltaTime 正确赋值 |
| UA-005 | 移除 Passive 模式，SkillTriggerMode 简化为 Manual/Auto |
| UA-006 | Buff 速度修正通过 SpeedModifier 系统注入（非旁路） |
| UA-007 | GetHostileCamp 提取为 CampUtility 独立工具类 |
| UA-008 | BuffComponent.Init 增加注释说明 |

### v0.1 (2026-05-01) — 初稿

Phase 3A TDD 完整初稿（P3.0~P3.4 + 验收矩阵 + 变更清单）。
