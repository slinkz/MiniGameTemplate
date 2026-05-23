---
system: entity-component
scope: components-detail
last_verified: 2026-05-23
depends_on: [EC_TDD_01_OVERVIEW, EC_TDD_02_CORE_ARCH]
related_code: Assets/_Framework/EntitySystem/Components/*.cs
---

## 四、组件详细设计（v2.1 修订版）

### 4.1 StateComponent

**BC 引用**：BC-01.4, BC-02

**v2.1 变更（EC-014）**：
- 状态标签集合封装为 `StateMask` 值类型（内部 uint64，对外不暴露原始位操作）
- 未来如需 > 64 种状态，可将 `StateMask` 内部改为 uint64[] 而不影响外部接口
- 互斥规则表从配置读取（Phase 1: 硬编码或 SO；Phase 2: Luban），启动时预计算互斥掩码矩阵 `uint64[64]`（O(1) 检查）
- 状态变化通过 EntityEventBus 发布 `OnStateChanged`

### 4.2 HealthComponent

**BC 引用**：BC-02, BC-03

**v2.0 变更**：
- 受伤流程中的"来源信息"改为 `EntityId`（而非模糊的"来源"）
- 通过 EntityEventBus 发布 `OnDamaged` / `OnDeath`，不直接操作 StateComponent

**P2.4 扩展**：IDamageModifier 伤害修饰链 + 无敌帧(IFrameCount) + HitStop 顿帧(HitStopFrames)

#### 4.2.1 TakeDamage 完整管线

```
入口: TakeDamage(ref DamageContext context)
│
├─ 1. 前置检查: !IsActive / IsDead / IsInvincible → 直接 return
│
├─ 2. 暴击计算: if (IsCritical) FinalDamage = BaseDamage × CritMultiplier
│               else FinalDamage = BaseDamage
│
├─ 3. IDamageModifier 链（按 Priority 升序遍历）
│     ├─ modifier.ProcessDamage(ref context, target)
│     │   ├─ return true  → 继续下一个 modifier
│     │   └─ return false → 中断链，伤害被完全吸收，不扣血
│     └─ 所有 modifier 处理完毕 → 读取 context.FinalDamage
│
├─ 4. 扣血: CurrentHp -= FinalDamage（最低为 0）
│
├─ 5. 触发无敌帧: if (IFrameCount > 0) → IFrameRemaining = IFrameCount
│
├─ 6. 触发 HitStop: if (HitStopFrames > 0) → Entity.PauseFor(HitStopFrames)
│
├─ 7. 发布事件: OnDamaged { Damage, RemainingHp, Source }
│
└─ 8. 死亡判定: if (CurrentHp <= 0) → OnDeath + StateComponent.ForceAddState(Dead)
```

**关键顺序**：暴击先算 → Modifier 后算。Modifier 修改的是"暴击后的 FinalDamage"，而非 BaseDamage。这意味着减伤是对最终到手伤害做折减。

#### 4.2.2 IDamageModifier 接口

```csharp
public interface IDamageModifier
{
    /// 优先级（升序执行，数字越小越先执行）
    /// 推荐范围：0-100=护盾/免伤，100-200=护甲/减伤，200-300=暴击修正，300+=反弹/吸血
    int Priority { get; }

    /// 处理伤害上下文。返回 true=继续链，false=中断（伤害被完全吸收）
    bool ProcessDamage(ref DamageContext context, Entity target);
}
```

**设计约束**：
- 固定数组 `IDamageModifier[4]`，零 GC（不用 List）
- 插入时按 Priority 升序排序
- `ProcessDamage` 通过 ref 传递 DamageContext struct——零堆分配
- 修正器可以是有状态的（如护盾记录剩余值），但生命周期由调用方管理

**API**：

| 方法 | 说明 |
|------|------|
| `bool AddModifier(IDamageModifier)` | 按 Priority 插入排序注册。返回 false = 已满（上限 4） |
| `void RemoveModifier(IDamageModifier)` | 引用相等移除 + shift-left 保持有序 |
| `void ClearModifiers()` | 清空所有修正器（Entity 回收时由 Reset 调用） |

#### 4.2.3 内置实现：DamageReductionModifier

百分比减伤修正器——IDamageModifier 的首个正式实现，用于护甲/减伤 Buff 场景。

```csharp
public class DamageReductionModifier : IDamageModifier
{
    public int Priority => 150; // 减伤层（在护盾 0-100 之后）
    public float Reduction { get; set; } // 0~1，Clamp01

    public DamageReductionModifier(float reduction)
    {
        Reduction = Mathf.Clamp01(reduction);
    }

    public bool ProcessDamage(ref DamageContext context, Entity target)
    {
        context.FinalDamage = (int)(context.FinalDamage * (1f - Reduction));
        return true; // 继续链
    }
}
```

**用法示例**：

```csharp
// 注册 50% 减伤
var mod = new DamageReductionModifier(0.5f);
healthComponent.AddModifier(mod);

// 此后所有 TakeDamage → FinalDamage 自动减半
// BaseDamage=100 → FinalDamage=50
// BaseDamage=100 + Crit×2=200 → FinalDamage=100

// 移除减伤（Buff 结束时）
healthComponent.RemoveModifier(mod);
```

**验证结果**（2026-05-01 Play Mode 实测）：

| 测试场景 | BaseDamage | FinalDamage | 预期 | 结果 |
|----------|-----------|-------------|------|------|
| 无 Modifier | 100 | 100 | 100 | ✓ PASS |
| 50% 减伤 | 100 | 50 | 50 | ✓ PASS |
| 暴击×2 + 50% 减伤 | 100 | 100 | 200×0.5=100 | ✓ PASS |

#### 4.2.4 自定义 Modifier 实现指南

游戏层可实现自己的 IDamageModifier：

| 场景 | Priority 建议 | ProcessDamage 逻辑 | 返回值 |
|------|--------------|-------------------|--------|
| **护盾** | 50 | 扣除 ShieldHp，溢出伤害写回 FinalDamage | false（全挡时）/ true（溢出时） |
| **减伤 Buff** | 150 | `FinalDamage *= (1 - ratio)` | true |
| **免伤（无敌）** | 10 | `FinalDamage = 0` | false |
| **伤害反弹** | 350 | 对 AttackerId 发起反弹伤害 | true |
| **吸血** | 400 | 对攻击者恢复 HP（需通过 EntityManager 查找） | true |

### 4.3 AnimationComponent

**BC 引用**：BC-02.2（Tickable）

**v2.0 重要变更**：
- **不在 Phase 1 实现视觉渲染**。Phase 1 的 AnimationComponent 只管"动画状态管理"（当前状态→动画 ID 映射），不直接操作 Spine/SpriteRenderer
- 提供 `CurrentAnimId` 只读属性，由游戏层的 View 组件读取并驱动实际渲染
- 这样 Entity 层保持纯逻辑，渲染表现完全解耦

### 4.4 MovementComponent

**BC 引用**：BC-02.2（Tickable）

**v2.0 变更**：无实质变更。
- 速度修正器改用固定数组预分配（最多 4 个 Modifier），避免 List 扩容 GC

**v2.4 新增（GD-R4-004）**：击退（Knockback）支持
```csharp
/// <summary>
/// 施加击退效果。被调用后在 duration 时间内沿 direction 位移 distance 距离。
/// 击退期间正常移速叠加（击退是额外位移，不替代原始运动）。
/// </summary>
public void ApplyKnockback(Vector2 direction, float distance, float duration)
{
    _knockbackDir = direction.normalized;
    _knockbackSpeed = distance / duration;
    _knockbackRemaining = duration;
}
```
- 从 `EntityConfigSO.KnockbackDistance` 读取默认击退距离
- `HealthComponent` 收到 `OnCollisionHit` 后调用 `MovementComponent.ApplyKnockback()`
- 击退曲线（AnimationCurve）Phase 2 扩展

### 4.5 CollisionComponent

**BC 引用**：BC-05

**v2.0 重大变更（vs v1.0）**：
- 实现 `ICollisionTarget` 接口，直接桥接现有弹幕碰撞系统
- 使用 `CircleHitbox`（而非 OBB）作为角色碰撞体——与弹幕系统一致
- OBB 碰撞体（Entity vs 障碍物）通过 `ObstaclePool.AddRect()` 注册，Entity vs 弹幕走 `TargetRegistry`
- 动态注册策略：不是所有 Entity 都常驻 TargetRegistry

### 4.6 AutoAimComponent

**BC 引用**：BC-02.2（Tickable，定频）

**v2.0 变更**：
- 搜索范围用 EntityManager 提供的 `FindEntitiesInRadius()` API，而非碰撞系统
- 阵营过滤复用 `EnumCamp` 枚举的 `ShouldCollide()` 逻辑

### 4.7 ControlComponent / AIComponent

**BC 引用**：BC-07

**v2.0 变更**：无实质变更，设计保持。

**v2.4 新增（GD-R4-002/010）**：AIBehaviorSO 配置资产化 + IAIAction 有状态 Action 接口。

#### AIBehaviorSO（条件-动作表配置资产）

```csharp
/// <summary>
/// AI 行为配置资产。策划在 Inspector 中按优先级配置条件-动作表。
/// 路径：Assets/_Game/Configs/AI/
/// </summary>
[CreateAssetMenu(fileName = "NewAIBehavior", menuName = "Entity/AIBehavior")]
public class AIBehaviorSO : ScriptableObject
{
    [Tooltip("按优先级排列的条件-动作表（索引越小优先级越高）")]
    public AIBehaviorEntry[] Entries;
}

[System.Serializable]
public struct AIBehaviorEntry
{
    public AIConditionType Condition;    // 枚举：Always/HpBelow/TargetInRange/TargetLost/...
    public float ConditionParam;         // 条件参数（如距离阈值、HP 百分比）
    public AIActionType Action;          // 枚举：Idle/MoveToTarget/Attack/Flee/Patrol/...
    public float ActionParam;            // 动作参数（如巡逻半径、逃跑距离）
}

public enum AIConditionType : byte
{
    Always = 0,             // 无条件匹配（兜底）
    HpBelow = 1,            // HP 百分比低于 ConditionParam（0.0~1.0）
    TargetInRange = 2,      // 目标在 ConditionParam 距离内
    TargetLost = 3,         // 无目标 / 目标超出检测范围
    // Phase 2 扩展：HpAbove, AllyCountBelow, WaveIndex, ...
}

public enum AIActionType : byte
{
    Idle = 0,
    MoveToTarget = 1,
    Attack = 2,
    Flee = 3,
    Patrol = 4,
    // Phase 2 扩展：Guard, Retreat, ...
}
```

#### IAIAction 有状态执行器接口

> **v2.4 新增（GD-R4-010）**：条件-动作表决定"什么时候做什么"；IAIAction 决定"怎么做"。
> Action 执行器内部维护多帧状态（如 Patrol 的目标巡逻点、等待计时）。

```csharp
/// <summary>
/// AI Action 执行器接口——支持多帧有状态执行。
/// 每帧由 AIComponent 调用 Execute()，Action 内部维护自身状态。
/// </summary>
public interface IAIAction
{
    void Enter(Entity owner);                           // 进入此 Action 时调用
    DecisionCommand Execute(Entity owner, float dt);    // 每帧执行，返回移动/攻击指令
    void Exit(Entity owner);                            // 退出此 Action 时调用
}
```

**AIComponent 执行流程**：
1. 每帧评估 `AIBehaviorSO.Entries`（按优先级从高到低）→ 匹配第一个满足条件的 Entry → 得到 `AIActionType`
2. **v2.6 安全网（WF-005）**：如果所有条件均未匹配 → **默认执行 IdleAction**（硬编码 fallback，不需要策划配置）
   ```csharp
   // 安全网：所有条件均未匹配时默认 Idle。
   // 建议策划在行为表末尾配置 Always→Idle。
   if (matchedAction == null) matchedAction = _fallbackIdleAction;
   ```
3. 如果 `AIActionType` 与上一帧不同 → 调用旧 Action.Exit() + 新 Action.Enter()
4. 调用当前 Action.Execute(owner, dt) → 得到 `DecisionCommand`

**Phase 1 内置 Action 列表**：

| Action | 说明 | 有状态？ |
|--------|------|---------|
| IdleAction | 原地不动 | 否 |
| MoveToTargetAction | 朝当前锁定目标移动 | 否 |
| PatrolAction | 随机选巡逻点→移向→到达后等待→再选新点 | ✅ 是 |
| AttackAction | 触发 AttackComponent 的攻击逻辑 | 否 |
| FleeAction | 朝远离目标方向移动 | 否 |

**策划视角**：策划只配 AIBehaviorSO（在 Inspector 中拖拽条件-动作），程序负责 IAIAction 实现。

### 4.8 SkillComponent（Phase 3A P3.3）

> CD 管理的主动/被动技能（前摇 → 效果触发 → 后摇 → CD）。与 AttackComponent **共存不替代**。

**BC 引用**：BC-02.2（Tickable） | **TickOrder**：160（Attack 之后）

| 字段 | 类型 | 说明 |
|------|------|------|
| `CurrentState` | `SkillState` | Idle → Casting → Recovery → Cooldown → Idle |
| `CooldownRemaining` | `float` | CD 剩余（秒） |
| `_config` | `SkillConfigSO` | 来自 `owner.ConfigSO.SkillConfig` |
| `_cachedDecisionMaker` | `IDecisionMaker` | Init 时缓存（Control > AI），不支持运行时切换 |

**状态转换矩阵**：
- `Idle` → `Casting`（CastTime>0）或直接执行效果 → `Recovery`（瞬发）
- `Casting` → 前摇结束 → `ExecuteEffects` → `Recovery`
- `Recovery` → `Cooldown`（RecoveryTime>0）或安全网 Cooldown（CD=0+Recovery=0 → 0.001s）
- `Cooldown` → `Idle`
- **死亡/PendingDespawn → 立即中断回 Idle**（ATK-014）

**触发模式**（`SkillTriggerMode`）：
- `Auto`：每帧 CD 就绪即触发
- `Manual`：需 `IDecisionMaker.GetDecision().WantsAttack == true`

**效果执行**：遍历 `_config.Effects[]`（`ISkillEffect`），传入 `SkillContext{Caster, CastPosition, AimDirection, DeltaTime, SkillConfig}`。

### 4.9 AttackComponent（v2.4 → v2.6 更新）

> Phase 1 最小攻击组件——定时发射弹幕。与 SkillComponent **共存**（各占独立槽位）。

**BC 引用**：BC-02.2（Tickable） | **TickOrder**：150（AutoAim=120 之后）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Type` | `ComponentType.Attack` | **独立槽位**（不再复用 Skill） |
| `_bulletPattern` | `BulletPatternSO` | 来自 `owner.ConfigSO.AttackBulletPattern` |
| `_attackInterval` | `float` | 基础攻击间隔 |

**Buff 攻速修正（P3.4 pull 模式）**：
```csharp
float effectiveInterval = _attackInterval;
var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
if (buff != null)
    effectiveInterval *= buff.AttackIntervalModifier;
```

**发射决策链**：DecisionMaker.WantsAttack → CD就绪 → `DanmakuSystem.FireBullets(pattern, pos, angle, ownerId)`

**瞄准优先级**：AutoAim 锁定方向 > DecisionCommand.AimDirection > Entity.Rotation

**近战攻击（GD-R4-009）**：统一走弹幕系统（射程≈0.5、速度=0、存活≈0.1s）。

### 4.10 BuffComponent（V2 扩展 — Sprint 3）

> 管理 Entity 身上的 Buff/DOT 列表和聚合属性修正。

**TickOrder**：50（最先执行，属性修正在 Decision/Attack 之前生效）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Type` | `ComponentType.Buff = 10` | |
| `MAX_BUFFS` | `const 16` | 固定槽位数组，零 GC |
| `_buffSlots[16]` | `BuffSlot` | Buff 槽位（含 Tag/StackMode/VfxInstanceId/BulletCountMod） |
| `_dotSlots[16]` | `DotSlot` | DOT 槽位（DotId/DmgPerTick/Interval/Duration/Timer） |
| `MoveSpeedModifier` | `float`（只读） | 乘法叠加 → Clamp[0.4, 2.5] |
| `AttackIntervalModifier` | `float`（只读） | 乘法叠加 → Clamp[MIN_ATTACK_INTERVAL_RATIO=0.3, 3.0] |
| `DamageTakenModifier` | `float`（只读） | 乘法叠加，不 Clamp |
| `HasActivePierce` | `bool`（只读） | 穿透状态（被动桥接 Buff 设置） |
| `CritRateBonus` | `float`（只读） | 暴击率加成 |
| `PickupRadiusModifier` | `float`（只读） | 拾取半径倍率 |

**API**：
- `ApplyBuff(BuffConfigSO)` → 同 ID Refresh/Stack 模式 | 槽满 LogWarning
- `RemoveBuff(int buffId)` / `RemoveByTag(BuffTag)` — Tag 清除同时遍历 Buff+DOT
- `ApplyDot(DotConfigSO)` → 同 ID 刷新 Duration
- `GetBulletCountModifier()` → 乘法累积 BulletCountMod

**Tick 逻辑**：Buff 倒计时 → 过期移除 → DOT while(timer>=interval) DealDamage → RecalcModifiers → SyncMoveSpeed

### 4.11 PassiveComponent（V2 Sprint 3 新增）

> 3 槽被动技能组件，独立 CD 周期触发。

**TickOrder**：60（在 Buff=50 之后）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Type` | `ComponentType.Passive = 12` | |
| `MAX_PASSIVES` | `const 3` | 固定 3 槽位 |
| `PassiveSlot[3]` | struct | Config/CooldownTimer/IsActive/BuffApplied |

**API**：
- `InitWithPassives(PassiveAbilitySO[])` → 幂等：先 Unsubscribe 旧 OnCollisionHit 再重新订阅（防 Retry 双绑定），初始 CD=1f
- `Reset()` → 取消 OnCollisionHit 订阅 + 清空 slots

**触发模式**：
- `AutoOnReady`：CD≤0 自动激活 → ApplyBuff(LinkedBuff) 或执行 ActivateEffects
- `OnHit`：OnCollisionHit 事件 → CD 就绪时激活（CD 归零等待下一 tick）

---

