# Phase 3A TDD — §3.2 DamageDealer & §3.3 SkillComponent

> **所属文档**：[PHASE3A_TDD_INDEX.md](PHASE3A_TDD_INDEX.md) · v0.4  
> **本文件范围**：技术方案 P3.2 + P3.3

---

## 3.2 直接伤害路径（DamageDealer）

### 3.2.1 设计决策

DamageDealer 是**无状态静态工具类**：

- 不占 ComponentType 槽位
- 不走弹幕系统（解决 AOE/光环/陷阱/技能直伤场景）
- 复用 HealthComponent.TakeDamage 管线（IDamageModifier 链生效）

> **v0.4（GD-007）确认**：DamageDealer 的所有路径最终调用 `HealthComponent.TakeDamage`——Phase 2 已有的受击反馈管线（闪白/击退/伤害数字/死亡延迟）**自动生效**。无需额外集成。

> **v0.4（GD-010）**：`finalDamage = 0` 时仍触发 `OnTakeDamage` 事件（传入 0）。View 层可据此显示 "IMMUNE" 文字或无敌特效。

> **v0.4（SA-001）设计声明**：DamageDealer 是静态工具类（模仿 Unity `Physics2D` API 风格）。不支持 mock/DI，通过 PlayMode 集成测试验证。如需单元测试，封装为 `IDamageService` 接口是 Phase 5 的可选重构路径。

### 3.2.2 实现

> **v0.4 修正（SA-006）**：循环中每次迭代检查 PendingDespawn（bug fix）。  
> **v0.4 修正（SA-004）**：EntityManagerAccessor null 时 Debug.Assert。  
> **v0.4 修正（ATK-003）**：补充 _buffer 大小设计理由注释。  
> **v0.4 修正（SA-009）**：补充协程安全说明。

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
        // ATK-003：buffer 开 64 而非 16 的理由：
        // - maxTargets=16 是默认值，调用方可传更大值（如全屏 AOE）
        // - buffer 大小 64 = EntityPool 默认最大容量的一半（预留裕量）
        // - 只有 Entity 引用（64 × 8 bytes = 512B），内存开销可忽略
        private static readonly Entity[] _buffer = new Entity[64];
        
        // UA-003：重入保护。
        // SA-009：Unity 协程是协作式调度（非抢占），同帧内多个协程不会真正并行，
        // 重入保护对协程场景有效。
        private static bool _isProcessingArea;

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
        /// 注意：不支持嵌套调用（UA-003 行为约束）。
        /// </summary>
        public static int DealAreaDamage(
            Vector2 center, float radius, EnumCamp targetCamp,
            DamageContext baseContext, int maxTargets = 16)
        {
            // UA-003：重入检测
            Debug.Assert(!_isProcessingArea, 
                "[DamageDealer] DealAreaDamage 不支持嵌套调用！请检查 OnDeath 回调链。");
            if (_isProcessingArea) return 0;
            
            var mgr = EntityManagerAccessor.Instance;
            Debug.Assert(mgr != null, "[DamageDealer] EntityManager not initialized!"); // v0.4 SA-004
            if (mgr == null) return 0;

            _isProcessingArea = true;
            int hitCount = 0;
            try  // UA-010：try/finally 确保异常时 flag 正确 reset
            {
                int count = mgr.FindEntitiesInRadius(center, radius, targetCamp, _buffer, 
                    Mathf.Min(maxTargets, _buffer.Length));

                for (int i = 0; i < count; i++)
                {
                    // v0.4 SA-006：循环中检查——前序目标的 OnDeath 可能导致后序目标被标记回收
                    if (_buffer[i].IsPendingDespawn || !_buffer[i].IsAlive) continue;
                    
                    var ctx = baseContext; // struct 值拷贝，每个目标独立 context
                    var health = _buffer[i].GetComponent(ComponentType.Health) as HealthComponent;
                    if (health != null)
                    {
                        health.TakeDamage(ref ctx);
                        hitCount++;
                    }
                }
            }
            finally
            {
                _isProcessingArea = false;
            }

            return hitCount;
        }
    }
}
```

---

## 3.3 SkillComponent（最小可用版）

### 3.3.1 设计哲学

一个 Skill = 一个 SO 配置（SkillConfigSO）+ N 个 SkillEffect（策略接口）。

**与 AttackComponent 的关系**：
- AttackComponent = 持续自动射击（定时器 + BulletPattern）
- SkillComponent = CD 管理的主动/被动技能（前摇 → 效果触发 → 后摇 → CD）
- 两者**共存不替代**：简单 Entity 只配 Attack，Boss 可同时配 Attack + Skill

> **v0.4（SA-002）约束**：ISkillEffect 实现必须**无状态**——SkillConfigSO 是共享资产，多个 Entity 引用同一 SO = 共享同一 Effect 实例。有状态行为由 SkillComponent 管理或等 Phase 4 扩展。

> **v0.4（GD-011）约束**：Casting 期间**不限制 Entity 其他行为**（移动/攻击正常）。如需 Boss 蓄力时停步，通过 Effects 列表施加减速 Buff 实现。Phase 4 FSM 提供状态级互斥约束。

> **v0.4（ATK-001）扩展点**：`_Game/` 目录下新增 `[Serializable] class MyEffect : ISkillEffect` 会被 Editor 自动发现（TypeCache）。

### 3.3.2 SkillConfigSO

> **v0.4 修正（GD-005）**：CooldownTime `[Min(0f)]`（放开 0.1f 下限）。

```csharp
[CreateAssetMenu(menuName = "Entity/SkillConfig")]
public class SkillConfigSO : ScriptableObject
{
    [Header("基础")]
    public string DisplayName;
    
    [Tooltip("触发模式")]
    public SkillTriggerMode TriggerMode = SkillTriggerMode.Auto;
    
    [Header("时间轴")]
    [Tooltip("冷却时间（秒，0=无冷却，受 Recovery 限制最小间隔）")]
    [Min(0f)]  // v0.4 GD-005：放开 0.1f 下限
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
    Auto = 1,       // CD 就绪自动触发
}
```

### 3.3.3 ISkillEffect 策略接口

> **v0.4 修正（ATK-008）**：Execute 返回 bool（施放成功语义）。  
> **v0.4 修正（SA-002）**：强制无状态约束。  
> **v0.4 修正（SA-012）**：命名锁定约束。

```csharp
/// <summary>
/// 技能效果策略接口。通过 [SerializeReference] 序列化到 SkillConfigSO。
/// 
/// ⚠️ 实现约束（SA-002）：
/// - ISkillEffect 实现必须是【无状态】的——不得持有随 Execute 调用变化的字段
/// - 原因：SkillConfigSO 是共享资产，多个 Entity 引用同一 SO = 共享同一 Effect 实例
/// - 如需有状态行为（充能/蓄力），使用 SkillComponent 内部状态或 Phase 4 扩展
/// - 所有序列化字段应为【配置参数】（只读），不应在 Execute 中修改
/// 
/// ⚠️ 命名约束（SA-012）：
/// - ISkillEffect 实现类一经发布（有 SkillConfigSO 引用），不得重命名类名或移动命名空间
/// - Unity [SerializeReference] 使用全限定类型名做序列化键
/// - 如必须重命名，使用 [MovedFrom] 属性做兼容映射
/// </summary>
public interface ISkillEffect
{
    /// <summary>
    /// 技能触发时执行。返回 true 表示效果成功执行（施放语义），false 表示未执行。
    /// Phase 3A 中 SkillComponent 不消费返回值；Phase 4 可用于"失败不进 CD"逻辑。
    /// (v0.4 ATK-008)
    /// </summary>
    bool Execute(SkillContext ctx);
}

/// <summary>
/// 技能执行上下文（struct，零 GC）。
/// 
/// 设计说明（SA-007）：
/// SkillContext 是值类型（struct），但包含引用类型字段（Caster、SkillConfig）。
/// 值拷贝后，引用字段仍指向同一对象实例。
/// 这是 by-design：允许 DealAreaDamage 等场景中对 baseContext 做值拷贝，
/// 每个目标获得独立 context 但共享 Caster 引用。
/// </summary>
public struct SkillContext
{
    public Entity Caster;           // 施法者
    public Vector2 CastPosition;    // 施法位置
    public Vector2 AimDirection;    // 瞄准方向
    public float DeltaTime;         // 当前帧 dt（供扩展使用）
    public SkillConfigSO SkillConfig; // v0.4 GD-017：技能配置引用
}
```

### 3.3.4 内置 SkillEffect 实现

> **v0.4 修正（ATK-008/ATK-012）**：所有 Effect 返回 bool，AreaDamageEffect return true（施放语义）。

**FireBulletsEffect**：

```csharp
[System.Serializable]
public class FireBulletsEffect : ISkillEffect
{
    [Tooltip("弹幕 Pattern")]
    public BulletPatternSO Pattern;
    
    [Tooltip("发射偏移")]
    public Vector2 FireOffset;
    
    public bool Execute(SkillContext ctx)
    {
        if (Pattern == null) return false;
        var ds = DanmakuSystem.Instance;
        if (ds == null) return false;
        
        Vector2 pos = ctx.CastPosition + FireOffset;
        float angle = Mathf.Atan2(ctx.AimDirection.y, ctx.AimDirection.x) * Mathf.Rad2Deg;
        ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value);
        return true;
    }
}
```

**AreaDamageEffect**：

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
    
    public bool Execute(SkillContext ctx)
    {
        var hostileCamp = CampUtility.GetHostileCamp(ctx.Caster.Camp);
        var dmgCtx = new DamageContext
        {
            BaseDamage = BaseDamage,
            AttackerId = ctx.Caster.Id,
            SourcePosition = ctx.CastPosition,
            HasSourcePosition = true,
        };
        DamageDealer.DealAreaDamage(ctx.CastPosition, Radius, hostileCamp, dmgCtx, MaxTargets);
        return true; // v0.4 ATK-012：施放成功语义（不管命中几个）
    }
}
```

### 3.3.5 SkillComponent

> **v0.4 修正（ATK-005/ATK-010）**：Init 缓存 IDecisionMaker + 运行时不切换约束注释。  
> **v0.4 修正（ATK-014）**：Tick 入口死亡中断。  
> **v0.4 修正（GD-005）**：CD=0 + Recovery=0 安全网。  
> **v0.4 修正（GD-017）**：ExecuteEffects 赋值 SkillConfig 引用。  
> **v0.4 新增（SA-005）**：显式状态转换矩阵。

**状态转换矩阵**（SA-005）：

| 当前 \ 目标 | Idle | Casting | Recovery | Cooldown |
|------------|------|---------|----------|----------|
| **Idle** | — | ✅ `ShouldTrigger() && CastTime>0` | ✅ `ShouldTrigger() && CastTime==0`（瞬发） | ❌ |
| **Casting** | ✅ 死亡中断(ATK-014) | — | ✅ 前摇 timer≤0 → ExecuteEffects | ❌ |
| **Recovery** | ❌ | ❌ | — | ✅ 后摇 timer≤0 |
| **Cooldown** | ✅ CD≤0 | ❌ | ❌ | — |

```csharp
public sealed class SkillComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.Skill;
    public bool IsActive { get; private set; }
    public void SetActive(bool active) => IsActive = active;
    public int TickOrder => TickOrders.Skill; // 160

    public SkillState CurrentState { get; private set; }
    public float CooldownRemaining { get; private set; }
    
    private Entity _owner;
    private SkillConfigSO _config;
    private float _stateTimer;
    
    // v0.4（ATK-005/ATK-010）：Init 后固定，不支持运行时切换控制源。
    // 如 Phase 4 需要"被控制/AI 接管"，改为每帧查询或注册切换回调。
    private IDecisionMaker _cachedDecisionMaker;

    public void Init(Entity owner)
    {
        _owner = owner;
        _config = owner.ConfigSO.SkillConfig;
        CurrentState = SkillState.Idle;
        CooldownRemaining = 0f;
        _stateTimer = 0f;
        IsActive = _config != null;
        
        _cachedDecisionMaker = (owner.GetComponent(ComponentType.Control) as IDecisionMaker)
                             ?? (owner.GetComponent(ComponentType.AI) as IDecisionMaker);
    }

    public void Reset()
    {
        _owner = null;
        _config = null;
        _cachedDecisionMaker = null;
        CurrentState = SkillState.Idle;
        CooldownRemaining = 0f;
        _stateTimer = 0f;
        IsActive = false;
    }

    public void Tick(float dt)
    {
        if (_config == null) return;
        
        // v0.4（ATK-014）：死亡/待回收时中断技能
        if (!_owner.IsAlive || _owner.IsPendingDespawn)
        {
            if (CurrentState != SkillState.Idle)
            {
                CurrentState = SkillState.Idle;
                _stateTimer = 0f;
            }
            return;
        }

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
                        ExecuteEffects(dt);
                        EnterRecovery();
                    }
                }
                break;

            case SkillState.Casting:
                _stateTimer -= dt;
                if (_stateTimer <= 0)
                {
                    ExecuteEffects(dt);
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
            SkillTriggerMode.Auto => true,
            SkillTriggerMode.Manual => _cachedDecisionMaker?.GetDecision().WantsAttack ?? false,
            _ => false
        };
    }

    private void ExecuteEffects(float dt)
    {
        var ctx = new SkillContext
        {
            Caster = _owner,
            CastPosition = _owner.Position,
            AimDirection = GetAimDirection(),
            DeltaTime = dt,
            SkillConfig = _config, // v0.4 GD-017
        };

        for (int i = 0; i < _config.Effects.Length; i++)
        {
            _config.Effects[i]?.Execute(ctx);
        }
    }

    // v0.4（GD-005）：CD=0 + Recovery=0 安全网
    private void EnterRecovery()
    {
        if (_config.RecoveryTime > 0)
        {
            CurrentState = SkillState.Recovery;
            _stateTimer = _config.RecoveryTime;
        }
        else if (_config.CooldownTime > 0)
        {
            CooldownRemaining = _config.CooldownTime;
            CurrentState = SkillState.Cooldown;
        }
        else
        {
            // 安全网：CD=0 + Recovery=0 → 强制最短 Cooldown（下帧再触发）
            CooldownRemaining = 0.001f;
            CurrentState = SkillState.Cooldown;
            Debug.LogWarning($"[SkillComponent] {_config.DisplayName} CD=0 + Recovery=0，已强制最小间隔。");
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
    Casting = 1,
    Recovery = 2,
    Cooldown = 3,
}
```

### 3.3.6 EntityConfigSO 新增字段

```csharp
[Header("技能（P3.3）")]
[Tooltip("技能配置（null=不启用 Skill 组件）")]
public SkillConfigSO SkillConfig;
```

### 3.3.7 SkillConfigSOEditor（v0.4 ATK-001 新增）

```csharp
// SkillConfigSOEditor.cs — 路径：_Framework/EntitySystem/Editor/
// 使用 TypeCache.GetTypesDerivedFrom<ISkillEffect>() 实现类型发现
// 提供 "+"添加、"-"删除、拖拽重排序
// 每个 Effect 条目展开显示其具体属性
// 不依赖 Odin——零第三方 Editor 依赖

private static Type[] GetEffectTypes()
{
    return TypeCache.GetTypesDerivedFrom<ISkillEffect>()
        .Where(t => !t.IsAbstract && !t.IsInterface)
        .OrderBy(t => t.Name)
        .ToArray();
}
// 搜索所有已加载程序集（包含 _Game/ 下的 Assembly Definition），
// 框架外扩展的 ISkillEffect 实现会自动出现在下拉菜单中。
```
