# SG_V2_TDD_06: 普攻升格为技能系统（AimMode 数据驱动）

---
system: phase3a-skill-buff
scope: attack-to-skill-unification
last_verified: 2026-05-25
related_code:
  - Assets/_Framework/EntitySystem/Scripts/Components/AttackComponent.cs
  - Assets/_Framework/EntitySystem/Scripts/Components/SkillComponent.cs
  - Assets/_Framework/EntitySystem/Scripts/Config/SkillConfigSO.cs
  - Assets/_Framework/EntitySystem/Scripts/Config/EntityConfigSO.cs
  - Assets/_Framework/EntitySystem/Scripts/Skill/Effects/FireBulletsEffect.cs
  - Assets/_Framework/EntitySystem/Scripts/Components/BuffComponent.cs
  - Assets/_Game/Scripts/ShooterGame/Core/BattleController.cs
---

> **版本**：v0.4（代码评审 PK 后）  
> **日期**：2026-05-25  
> **作者**：广智 × 天命人  
> **PK 状态**：✅ 已收敛（R1: 架构师 2轮14问 + R2: Editor工具 2轮10问 + R3: 代码评审 2轮10问）  
> **编码状态**：🔨 P1~P10 完成，待 Unity Editor 验收（SO 创建+迁移工具执行）  
> **前置**：SG_V2_TDD_02（技能装备）、SG_V2_TDD_03（Buff/DOT）  
> **预估**：~20.5h（4~5 天）

---

## 一、目标与动机

### 1.1 问题陈述

当前攻击系统存在**双轨架构债务**：

| 系统 | ComponentType | TickOrder | 瞄准策略 | Buff 影响路径 |
|------|---------------|-----------|----------|--------------|
| AttackComponent | Attack (5) | 150 | 正前方（硬编码） | Pull: `buff.AttackIntervalModifier` |
| SkillComponent | Skill (6) | 160 | AutoAim > Decision > Rotation | 无直接 CD 修正 |

**痛点**：
1. 两套射击系统共存——新增火力功能需改两处代码
2. 瞄准策略硬编码——普攻永远朝前，无法配置跟踪
3. Buff 攻速修正走 Pull 模式——只影响 `AttackComponent`，技能 CD 不受攻速 Buff 影响
4. TickOrder 存在两个"攻击阶段"（150/160）——概念模糊

### 1.2 目标

| # | 目标 | 验收标准 |
|---|------|---------|
| G1 | 消灭 `AttackComponent` | ComponentType.Attack 废弃 / 无代码引用 |
| G2 | 普攻 = SkillComponent Slot[0] | Slot[0] 由 `NormalAttackConfig` SO 驱动，表现与当前一致 |
| G3 | AimMode 数据驱动 | 每个 SkillConfigSO 独立配置瞄准策略 |
| G4 | Buff 攻速统一影响 Slot[0] | 火力全开 Buff 影响 Slot[0] CD + BulletCount |
| G5 | 零行为回归 | PlayTest 普攻射速/方向/Buff 响应与改造前一致 |

### 1.3 非目标

- ❌ 不改 EnemyShootComponent（敌机射击独立，保持简单）
- ❌ 不改现有技能 Slot[1~5] 的行为
- ❌ 不实现运行时切换 AimMode（仅配置时决定）

---

## 二、设计方案

### 2.1 AimMode 枚举

```csharp
// 新文件：Assets/_Framework/EntitySystem/Scripts/Skill/AimMode.cs
namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 瞄准策略枚举——决定技能释放时的方向来源。
    /// </summary>
    public enum AimMode : byte
    {
        /// <summary>永远沿 Entity 朝向射击（当前普攻行为）</summary>
        FixedForward = 0,

        /// <summary>有锁定目标→跟踪，无目标→Entity 朝向（当前技能行为）</summary>
        AutoAim = 1,

        /// <summary>完全由 DecisionCommand.AimDirection 决定（预留手动操控）</summary>
        CommandDir = 2,
    }
}
```

### 2.2 SkillConfigSO 新增字段

```csharp
// SkillConfigSO.cs 新增 —— 插入到 [Header("时间轴")] 之前
[Header("瞄准")]
[Tooltip("瞄准策略：决定技能释放方向")]
public AimMode AimMode = AimMode.AutoAim;

[Header("普攻标记")]
[Tooltip("标记此技能为普攻（Slot[0]）。影响：Buff 攻速修正作用于此技能的 CD")]
public bool IsNormalAttack;
```

**设计决策**：用 `IsNormalAttack` 布尔标记而非依赖"是否在 Slot[0]"，原因：

| 方案 | 优点 | 缺点 |
|------|------|------|
| A. 依赖 Slot 索引 | 零字段 | 如果策划把普攻放到别的槽位就挂 |
| B. `IsNormalAttack` 标记 | 解耦槽位 / 语义明确 | 多一个 bool 字段 |

**选择 B**：明确语义优于隐式约定。

**【PK-ET-001 补充】SkillConfigSOEditor 同步更新**：

当前 `SkillConfigSOEditor` 是完全自定义 Inspector（不调用 `DrawDefaultInspector`），需在 `OnEnable` 中新增 `FindProperty("AimMode")`、`FindProperty("IsNormalAttack")`、`FindProperty("AttachedDotConfig")`、`FindProperty("SourceTagId")`。`OnInspectorGUI` 中新增"瞄准"区绘制。同时修复 `AttachedDotConfig`/`SourceTagId` 的现存未绘制遗漏。

`IsNormalAttack=true` 时的条件 UI：
- `CooldownTime` 字段用 `EditorGUI.DisabledScope(true)` 置灰
- 显示 HelpBox：`"⚠ 普攻标记已启用。CooldownTime 运行时由 EntityConfigSO.AttackInterval 覆盖。"`

**【PK-ET-004 补充】OnValidate 增强**：
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    if (Effects == null || Effects.Length == 0)
        Debug.LogWarning($"[SkillConfigSO] '{name}' Effects 为空——技能无实际效果", this);

    if (IsNormalAttack)
    {
        if (TriggerMode != SkillTriggerMode.Auto)
            Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 但 TriggerMode!=Auto", this);
        if (AimMode != AimMode.FixedForward)
            Debug.LogWarning($"[SkillConfigSO] '{name}' IsNormalAttack=true 建议 AimMode=FixedForward", this);
    }
}
#endif
```

### 2.3 SkillComponent.GetAimDirection 改造

**当前**：统一走 AutoAim > Decision > Rotation 硬编码优先级链。

**改造后**：每槽按 `Config.AimMode` 分派。

```csharp
private Vector2 GetAimDirection(SkillConfigSO config, ITargetProvider autoAim)
{
    switch (config.AimMode)
    {
        case AimMode.FixedForward:
            // 【PK-UA-009/013 修正】纵版射击固定向上。
            // 当前 AttackComponent 等价行为 = ControlComponent.SetAimInput(Vector2.up)。
            // 直接返回 Vector2.up，避免 Entity.Rotation(270°) 计算出朝下的 bug。
            // 未来支持非纵版时可从 SkillConfigSO 新增 FixedDirection 字段扩展。
            return Vector2.up;

        case AimMode.AutoAim:
            // 有目标→跟踪，无目标→Decision→Rotation（原有逻辑）
            if (autoAim != null && autoAim.HasTarget)
                return (autoAim.TargetPosition - _owner.Position).normalized;
            goto case AimMode.CommandDir;

        case AimMode.CommandDir:
            if (_cachedDecisionMaker != null)
            {
                Vector2 aimDir = _cachedDecisionMaker.GetDecision().AimDirection;
                if (aimDir.sqrMagnitude > 0.01f)
                    return aimDir.normalized;
            }
            // 兜底：Entity 朝向
            float fallbackRad = _owner.Rotation * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(fallbackRad), Mathf.Sin(fallbackRad));

        default:
            return Vector2.up;
    }
}
```

**破坏性变更**：方法签名从 `GetAimDirection(ITargetProvider)` → `GetAimDirection(SkillConfigSO, ITargetProvider)`。仅内部调用，无外部 API 影响。

### 2.4 Buff 攻速修正迁移

**当前机制**（AttackComponent.Tick L64~68）：
```csharp
float effectiveInterval = _attackInterval;
var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
if (buff != null)
    effectiveInterval *= buff.AttackIntervalModifier;
```

**迁移方案**：在 `SkillComponent.TickSlot` 的 Cooldown 阶段，对标记为 `IsNormalAttack` 的槽位应用攻速修正。

```csharp
case SkillState.Cooldown:
    float cdDt = dt;
    // 普攻槽：Buff 攻速修正影响 CD 消耗速率
    // AttackIntervalModifier < 1 → 攻速更快 → CD 消耗更快
    if (slot.Config.IsNormalAttack)
    {
        // 【CR-006】不缓存 BuffComponent 的原因：
        //   1. Entity.GetComponent(ComponentType.Buff) = _components[10]，O(1) 数组索引，零 GC
        //   2. ComponentType.Buff=10 > Skill=6，Init 时 Buff 可能还未创建，lazy 缓存复杂度不划算
        //   3. 每帧仅 Slot[0] 执行此分支（≤1 次/帧），性能可忽略
        var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
        if (buff != null && buff.AttackIntervalModifier != 1f)
        {
            // 修正值 = 1/modifier（modifier=0.5 → cd消耗2倍速）
            cdDt = dt / buff.AttackIntervalModifier;
        }
    }
    slot.CooldownTimer -= cdDt;
    if (slot.CooldownTimer <= 0)
    {
        slot.CooldownTimer = 0;
        slot.State = SkillState.Idle;
    }
    break;
```

**等效性证明**：
- 原始：`timer >= interval * modifier` → 修正后间隔变长/变短
- 新方案：CD 时间固定 = `CooldownTime`，消耗速率 = `1/modifier`
- 效果等价：modifier=0.5 → 原始间隔半减 → 新方案 CD 2倍速消耗 → 实际攻击频率翻倍 ✅

### 2.5 BulletCount Buff 迁移

**当前状态**（经 CodeGraph 验证）：`FireBulletsEffect.Execute` 直接调用 `ds.FireBullets(Pattern, pos, angle, ...)`。`BulletSpawner.Fire` 只读取 `DifficultyProfileSO.CountMultiplier`，**不读取 BuffComponent.BulletCountModifier**。

**【CR-003 修正】BuffComponent API 统一**：当前代码只有 `GetBulletCountModifier()` 方法（实时遍历），需改造为与 `AttackIntervalModifier` 一致的缓存属性：

```csharp
// BuffComponent.cs — 新增缓存属性（RecalcModifiers 中聚合）
/// <summary>子弹数修正倍率（乘法叠加，无 Clamp）</summary>
public float BulletCountModifier { get; private set; } = 1f;

// RecalcModifiers 中新增：
float bulletCount = 1f;
for (int i = 0; i < _activeBuffCount; i++)
{
    ref var s = ref _buffSlots[i];
    int stacks = s.CurrentStacks;
    if (s.StackMode == StackMode.Stack && stacks > 1)
        bulletCount *= Mathf.Pow(s.BulletCountMod, stacks);
    else
        bulletCount *= s.BulletCountMod;
}
BulletCountModifier = bulletCount;

// 旧方法保留向后兼容
[System.Obsolete("Use BulletCountModifier property instead")]
public float GetBulletCountModifier() => BulletCountModifier;
```

**实施方案**（PK UA-001/UA-012/CR-003 修正）：

```csharp
// FireBulletsEffect.Execute 改造
public bool Execute(SkillContext ctx)
{
    if (Pattern == null) return false;
    var ds = DanmakuSystem.Instance;
    if (ds == null) return false;

    Vector2 pos = ctx.CastPosition + FireOffset;
    Vector2 dir = UseForwardDirection && ctx.CasterTransform != null
        ? (Vector2)ctx.CasterTransform.up
        : ctx.AimDirection;
    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    // 【PK-UA-001 新增】Buff 弹幕数修正
    int baseCount = Pattern.Count;
    var buffComp = ctx.Caster.GetComponent(ComponentType.Buff) as BuffComponent;
    if (buffComp != null && buffComp.BulletCountModifier != 1f) // 【CR-003】属性，非方法
        baseCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * buffComp.BulletCountModifier));

    ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId, baseCount);
    return true;
}
```

**countOverride 语义定义**（PK UA-012）：
- `countOverride` = Buff 修正后的基础数量（**不含**难度乘数）
- `BulletSpawner.Fire` 内部接收后继续乘 `difficulty.CountMultiplier`
- **叠加顺序**：Pattern.Count × BulletCountModifier × DifficultyCountMultiplier = 最终值

**【CR-005 补充】完整调用链透传（4 层）**：

```csharp
// 1. DanmakuSystem.API.cs — 新增 countOverride 可选参数
public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle,
    uint ownerEntityId = 0, int sourceTag = 0, int? countOverride = null)
{
    _scheduler.ScheduleSingle(pattern, origin, baseAngle, ownerEntityId, sourceTag, countOverride);
}

// 2. PatternScheduler.ScheduleSingle — 新增 countOverride，存入 ScheduledTask
public void ScheduleSingle(BulletPatternSO pattern, Vector2 origin, float baseAngle,
    uint ownerEntityId = 0, int sourceTag = 0, int? countOverride = null)
{
    // ... 现有逻辑 ...
    ref var task = ref _tasks[slot];
    task.CountOverride = countOverride;  // 新增 int? 字段
    // ...
}

// ScheduledTask 结构新增字段
public int? CountOverride;  // Buff 修正后的弹丸数，null=使用 Pattern.Count

// 3. PatternScheduler.Tick 中调用 BulletSpawner.Fire 时透传
BulletSpawner.Fire(task.Pattern, task.Origin, task.Angle,
    _world, _registry, _difficulty, _trailPool,
    task.OwnerEntityId, task.SourceTag, task.CountOverride);

// 4. BulletSpawner.Fire — 新增 countOverride 可选参数
internal static void Fire(
    BulletPatternSO pattern, Vector2 origin, float baseAngleDeg,
    BulletWorld world, DanmakuTypeRegistry registry,
    DifficultyProfileSO difficulty = null, TrailPool trailPool = null,
    uint ownerEntityId = 0, int sourceTag = 0,
    int? countOverride = null)  // 【新增】
{
    int count = countOverride ?? pattern.Count;
    // 难度乘数照常应用
    if (difficulty != null)
        count = Mathf.RoundToInt(count * difficulty.CountMultiplier);
    // ... 余下逻辑不变
}
```

**【CR-009 补充】countOverride Burst 语义**：countOverride 作用于每一次独立的 Fire 调用。Burst 场景（BurstCount>1）下，每一 burst 均使用相同的 countOverride 值。

### 2.6 NormalAttack SkillConfigSO 资产

创建 SO 资产：`Assets/_Game/Configs/ShooterGame/Skills/SK_NormalAttack.asset`

| 字段 | 值 | 来源 |
|------|---|------|
| DisplayName | "基础射击" | — |
| TriggerMode | Auto | 全自动战斗设计 |
| AimMode | FixedForward | 当前普攻行为 |
| IsNormalAttack | true | 标记 |
| CooldownTime | 0.25f | 从 EntityConfigSO.AttackInterval 迁移 |
| CastTime | 0f | 瞬发 |
| RecoveryTime | 0f | 无后摇 |
| Effects[0] | FireBulletsEffect | Pattern=PlayerBullet, Offset=EntityConfigSO.AttackFireOffset |
| SourceTagId | 0 | 基础攻击标记 |

**注意**：不同飞机可能有不同攻击间隔——需要为每种 EntityConfigSO 创建对应的 NormalAttack SO，或在 BattleController 初始化时动态覆盖 CD。

**决策**：采用**运行时覆盖 CD**方案。

理由：
- 当前 5 种飞机可能有不同 AttackInterval
- 为每种飞机创建独立 NormalAttack SO = 配置爆炸
- 运行时覆盖 = 共用 1 个 SO + `InitWithEquipment` 时覆盖 CooldownTime

```csharp
// BattleController 初始化时
var normalAttackConfig = _battleLevelData.NormalAttackConfig; // 共用 SO
// 运行时覆盖 CD = EntityConfigSO.AttackInterval
float attackInterval = playerConfig.AttackInterval;
skillComp.InitWithEquipment(allSkills, staggerOffset: 0.5f);
skillComp.OverrideSlotCooldown(0, attackInterval); // 新增 API
```

### 2.7 SkillComponent 新增 API

```csharp
/// <summary>
/// 运行时覆盖指定槽位的 CooldownTime。
/// 用于普攻从 EntityConfigSO.AttackInterval 读取实际 CD。
/// 注意：不修改 SO 资产——只读 Config.CooldownTime 作为 fallback。
/// </summary>
public void OverrideSlotCooldown(int slotIndex, float cooldownTime)
{
    if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;
    if (_slots[slotIndex].Config == null) return;
    _runtimeCooldownOverrides[slotIndex] = cooldownTime;
}

// 新增字段
private readonly float[] _runtimeCooldownOverrides = new float[MAX_SLOTS];

// GetEffectiveCooldown：优先使用运行时覆盖值
private float GetEffectiveCooldown(int slotIndex)
{
    float over = _runtimeCooldownOverrides[slotIndex];
    return over > 0f ? over : _slots[slotIndex].Config.CooldownTime;
}
```

**【CR-001/002 补充】TickSlot + EnterRecovery 完整签名改造链**：

改造涉及 3 处联动修改，缺一不可：

**1. Tick 主循环**（调用处改造）：
```csharp
// 当前：TickSlot(ref _slots[i], dt)
// 改造后：传 index 以便内部访问 _runtimeCooldownOverrides
public void Tick(float dt)
{
    if (!IsActive) return;
    for (int i = 0; i < ActiveSlotCount; i++)
        TickSlot(i, dt);  // 【CR-001】传 index，不传 ref
}
```

**2. TickSlot 签名变更**：
```csharp
// 【PK-UA-003 + CR-001 修正】改为传 slotIndex
private void TickSlot(int slotIndex, float dt)
{
    ref var slot = ref _slots[slotIndex];
    switch (slot.State)
    {
        case SkillState.Idle:
            if (ShouldTrigger(slot.Config))
            {
                if (slot.Config.CastTime > 0)
                {
                    slot.State = SkillState.Casting;
                    slot.CastTimer = slot.Config.CastTime;
                }
                else
                {
                    if (ExecuteEffects(slot.Config, dt))
                        EnterRecovery(slotIndex);  // 【CR-001】传 index
                }
            }
            break;

        case SkillState.Casting:
            slot.CastTimer -= dt;
            if (slot.CastTimer <= 0)
            {
                if (ExecuteEffects(slot.Config, dt))
                    EnterRecovery(slotIndex);  // 【CR-001】传 index
                else
                    slot.State = SkillState.Idle;
            }
            break;

        case SkillState.Recovery:
            slot.CastTimer -= dt;
            if (slot.CastTimer <= 0)
            {
                // 【CR-002 核心修正】使用 GetEffectiveCooldown 替代 Config.CooldownTime
                slot.CooldownTimer = GetEffectiveCooldown(slotIndex);
                slot.State = SkillState.Cooldown;
            }
            break;

        case SkillState.Cooldown:
            // ... Buff 攻速修正逻辑（见 §2.4）...
            break;
    }
}
```

**3. EnterRecovery 签名联动改造**：
```csharp
// 【CR-001/002】改为接收 slotIndex，以便读取 GetEffectiveCooldown
private void EnterRecovery(int slotIndex)
{
    ref var slot = ref _slots[slotIndex];
    if (slot.Config.RecoveryTime > 0)
    {
        slot.State = SkillState.Recovery;
        slot.CastTimer = slot.Config.RecoveryTime;
    }
    else
    {
        float cd = GetEffectiveCooldown(slotIndex); // 使用运行时覆盖
        if (cd > 0)
        {
            slot.CooldownTimer = cd;
            slot.State = SkillState.Cooldown;
        }
        else
        {
            // 安全网：CD=0 + Recovery=0 → 强制最短 Cooldown
            slot.CooldownTimer = 0.001f;
            slot.State = SkillState.Cooldown;
            Debug.LogWarning($"[SkillComponent] {slot.Config.DisplayName} CD=0，已强制最小间隔。");
        }
    }
}
```

**【PK-UA-005 修正】**：`InitWithEquipment` 开头新增清零：
```csharp
/// <param name="firstSlotInitialCD">Slot[0] 首发延迟（秒）。
///   >0 = Slot[0] 初始进入 Cooldown 状态等待此时间后首发。
///   通常等于 OverrideSlotCooldown(0, x) 的同一个 x 值（如 attackInterval）。
///   不做内部 Clamp——调用者负责确保值合理。【CR-010 补充】
///   ≤0 = 立即可用（无首发延迟）。</param>
public void InitWithEquipment(
    SkillConfigSO[] equippedSkills,
    float staggerOffsetPerSlot = 0.5f,
    float firstSlotInitialCD = 0f)  // 【PK-ET-009 新增】
{
    // 清零运行时覆盖（对象池复用安全）
    System.Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS);

    // 清空所有槽位
    for (int i = 0; i < MAX_SLOTS; i++)
        _slots[i] = default;

    // ... 装备注入逻辑 ...

    // 【PK-ET-009】Slot[0] 首发延迟（合并原 SetSlotCooldownTimer 功能）
    if (firstSlotInitialCD > 0f && _slots[0].Config != null)
    {
        _slots[0].CooldownTimer = firstSlotInitialCD;
        _slots[0].State = SkillState.Cooldown;
    }
}
```

### 2.8 BattleController 初始化流程改造

**当前流程**（`BattleController.SetupPlayer`）：
1. 创建 Player Entity（含 AttackComponent + SkillComponent）
2. SkillComponent.InitWithEquipment(equippedSkills) — 装备技能注入 Slot[1~5]

**改造后**：
1. 创建 Player Entity（**不含 AttackComponent**）
2. 调用 `SetupPlayerSkills()`（抽取公共方法，Retry 复用）
3. Retry 流程同样调用 `SetupPlayerSkills()` 重新初始化

**【CR-004 统一版本】SetupPlayerSkills 完整实现**：
```csharp
/// <summary>
/// 抽取公共方法：初始化/Retry 均调用。
/// 三层兜底获取普攻配置 + 组装技能数组 + 首发延迟。
/// </summary>
private void SetupPlayerSkills()
{
    var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
    if (skillComp == null) return;

    // 三层兜底获取普攻配置（PK-ET-002/003 + CR-004 统一）
    var normalAttack = _battleLevelData?.NormalAttackConfig       // 1. BattleLevelData 覆盖（调试/特殊关卡）
                    ?? _playerEntityConfig.NormalAttackSkill;      // 2. EntityConfigSO 自带（正式流程主数据源）
    if (normalAttack == null)
        normalAttack = Resources.Load<SkillConfigSO>("ShooterGame/SK_NormalAttack"); // 3. Resources 兜底（直跑模式）

    if (normalAttack == null)
    {
        Debug.LogError("[BattleController] 无普攻配置！检查 EntityConfigSO.NormalAttackSkill 或 Resources/ShooterGame/SK_NormalAttack");
        return;
    }

    // 组装技能数组：[普攻, 技能1, ..., 技能N]
    var equipped = _battleLevelData?.EquippedSkills;
    int equipCount = equipped?.Length ?? 0;
    int totalSlots = Mathf.Min(1 + equipCount, SkillComponent.MAX_SLOTS);
    var allSkills = new SkillConfigSO[totalSlots];
    allSkills[0] = normalAttack; // Slot[0] = 普攻
    for (int i = 0; i < equipCount && i + 1 < totalSlots; i++)
        allSkills[i + 1] = equipped[i];

    // 初始化 + 首发延迟 + 运行时 CD 覆盖
    float attackInterval = _playerEntityConfig.AttackInterval;
    skillComp.InitWithEquipment(allSkills, staggerOffsetPerSlot: 0.5f,
                                firstSlotInitialCD: attackInterval);
    skillComp.OverrideSlotCooldown(0, attackInterval);
}
```

### 2.9 AttackComponent 废弃

| 步骤 | 操作 |
|------|------|
| 1 | EntityPool.CreateComponent 中 `ComponentType.Attack` case → 返回 `null` + `Debug.LogWarning("[Migration] Attack component skipped — use SkillComponent Slot[0]")` |
| 2 | EntityConfigSO 中 `AttackInterval/AttackBulletPattern/AttackFireOffset` 保留（向后兼容 + 迁移读取）|
| 3 | BattleController 中移除 AttackComponent 相关初始化代码 |
| 4 | `TickOrders.Attack (150)` 保留但注释为 legacy |
| 5 | `AttackComponent.cs` 添加 `[Obsolete("Use SkillComponent Slot[0]")]` + #if 条件编译 |
| 6 | 【PK-ET-006 补充】所有 EntityConfigSO 的 Components 数组中移除 `ComponentType.Attack`（Editor 脚本批量处理，或 P7 手动逐个 Inspector 操作） |

**不在本轮删除文件**——标记 Obsolete 即可，下一轮清理统一删除。

**CreateComponent 改造示例**：
```csharp
case ComponentType.Attack:
    Debug.LogWarning("[Migration] Attack component creation skipped. Use SkillComponent Slot[0].");
    return null; // 不再创建 AttackComponent 实例
```

**【PK-ET-005 补充】EntityConfigSO 僵尸字段标记**：
```csharp
// EntityConfigSO.cs
[Header("⚠ Legacy —— 以下字段已被 SkillComponent Slot[0] 取代")]
[Obsolete("Use NormalAttackSkill.FireBulletsEffect.Pattern instead")]
public BulletPatternSO AttackBulletPattern;

[Obsolete("Use NormalAttackSkill.FireBulletsEffect.FireOffset instead")]
public Vector2 AttackFireOffset;

[Header("普攻射速（运行时覆盖 Slot[0] CD）")]
[Tooltip("此值被 BattleController 读取，覆盖 NormalAttackSkill.CooldownTime")]
public float AttackInterval = 0.25f;

[Header("V2: 普攻技能")]
[Tooltip("此实体的普攻配置。运行时注入 SkillComponent Slot[0]。")]
public SkillConfigSO NormalAttackSkill;  // 【PK-ET-003 新增】
```

**【PK-ET-008 补充】迁移验证工具**：

P7 scope 内提供两个 MenuItem 脚本（放在 `Assets/_Framework/EntitySystem/Editor/Migration/`）：
1. `[MenuItem("Tools/Migration/Remove Attack From All EntityConfigs")]` — 扫描所有 EntityConfigSO，从 Components 数组中移除 ComponentType.Attack
2. `[MenuItem("Tools/Migration/Verify No Attack Components")]` — 扫描验证 + DisplayDialog 报告结果

### 2.10 FireBulletsEffect.UseForwardDirection 处理

当前 `FireBulletsEffect` 已有 `UseForwardDirection` 字段——当 `AimMode.FixedForward` 时，可直接设置此字段为 true。

**但**：AimMode 已经在 SkillComponent 层面决定了传给 Effect 的 `ctx.AimDirection`。所以：
- `AimMode.FixedForward` → `ctx.AimDirection = Entity 正前方` → `UseForwardDirection` 可保持 false
- 结论：**无需修改 FireBulletsEffect**——AimMode 在上游已解决。

### 2.11 SkillCDPanel UI 适配

Slot[0] = 普攻，CD 极短（0.25s），频繁闪烁体验差。

**方案**：SkillCDPanel 从 index=1 开始显示（跳过 Slot[0]）。

```csharp
// SkillCDPanel 修改：起始索引从 0 → 1
public void Update(SkillComponent skillComp)
{
    for (int i = 0; i < MAX_SKILLS; i++)
    {
        int slotIndex = i + 1; // 跳过 Slot[0]（普攻）
        // 【PK-UA-008 TODO】当前硬编码 i+1 跳过 Slot[0]。
        // 未来如果普攻不固定在 Slot[0]，应改为：
        //   if (slot.Config != null && slot.Config.IsNormalAttack) continue;
        // 当前只有 1 个普攻且固定 Slot[0]，硬编码 OK，编码期可迭代。
        // ...
    }
}
```

---

### 2.12 BattleDebugLauncher 改造（PK-ET-002）

调试直跑模式需同步支持普攻注入，否则非 Flow 启动时玩家无普攻。

```csharp
// BattleDebugLauncher.cs 新增
[Header("V2: 普攻配置")]
[SerializeField] private SkillConfigSO _debugNormalAttack;

public BattleLevelData BuildDebugLevelData()
{
    if (!_enabled) return null;

    var data = new BattleLevelData();
    data.LevelIndex = _debugLevelIndex >= 0 ? _debugLevelIndex : 0;

    // 技能...（现有逻辑不变）
    // 被动...（现有逻辑不变）

    // 普攻（V2 TDD-06）
    if (_debugNormalAttack != null)
        data.NormalAttackConfig = _debugNormalAttack;
    else
        Debug.LogWarning("[BattleDebugLauncher] _debugNormalAttack 未配置，将从 EntityConfigSO 兜底");

    return data;
}
```

**注意**：即使 `_debugNormalAttack` 为 null，BattleController 的三层兜底逻辑（§2.8）仍能从 EntityConfigSO.NormalAttackSkill 或 Resources 加载。

---

## 三、实施步骤

| Phase | 任务 | 预估 | 验收标准 |
|-------|------|------|---------|
| **P1** | 新增 `AimMode` 枚举 + SkillConfigSO 字段（AimMode, IsNormalAttack） | 1h | 编译通过 + Editor Inspector 显示新字段 |
| **P2** | 改造 `SkillComponent.GetAimDirection` 支持 AimMode 分派 | 2h | 已有技能行为不变（AimMode=AutoAim 默认值） |
| **P3** | 新增 `OverrideSlotCooldown` API + `GetEffectiveCooldown` 内部方法 | 1h | 单元测试覆盖 |
| **P4** | Buff 攻速修正迁移：TickSlot Cooldown 阶段 `IsNormalAttack` 加速 | 2.5h | 火力全开 Buff 影响 Slot[0] CD 消耗速率 |
| **P5** | 创建 `SK_NormalAttack.asset` SO 资产 + EntityConfigSO 新增 `NormalAttackSkill` 引用 | 1h | Inspector 配置完整 |
| **P6** | BattleController 初始化改造：普攻作为 Slot[0] 注入 + 三层兜底 + BattleDebugLauncher 同步 | 2.5h | PlayTest 玩家射击行为与改造前一致（含直跑模式） |
| **P7** | AttackComponent 标记 Obsolete + EntityPool 跳过创建 + 迁移 MenuItem 工具 + 验证脚本 | 2.5h | 零 AttackComponent 实例化 + MenuItem 验证通过 |
| **P8** | SkillCDPanel UI 适配：跳过 Slot[0] 显示 | 1h | UI 只显示 Slot[1~5] 技能 CD |
| **P9a** | SkillConfigSOEditor 字段绘制：AimMode/IsNormalAttack/AttachedDotConfig/SourceTagId + 条件 UI | 1.5h | 所有字段 Inspector 可见 + IsNormalAttack 条件 UI |
| **P9b** | OnValidate 增强 + IsNormalAttack 配置互斥校验 | 1h | 配置矛盾时 Console 有 LogWarning |
| **P10** | PlayTest 全链路回归 | 3h | G1~G5 全部达标 |
| **P11** | 文档更新：TDD_01/TDD_02 相关章节 + INDEX | 1h | 文档一致 |

**总计**：~20.5h（PK 后增加 Editor 工具+调试兜底 scope，+3h）

---

## 四、接口变更汇总

### 4.1 新增

| 符号 | 类型 | 位置 |
|------|------|------|
| `AimMode` | enum | `Scripts/Skill/AimMode.cs` |
| `SkillConfigSO.AimMode` | field | `Scripts/Config/SkillConfigSO.cs` |
| `SkillConfigSO.IsNormalAttack` | field | `Scripts/Config/SkillConfigSO.cs` |
| `SkillComponent.OverrideSlotCooldown(int, float)` | method | `Scripts/Components/SkillComponent.cs` |
| `SkillComponent._runtimeCooldownOverrides` | private field | `Scripts/Components/SkillComponent.cs` |
| `SkillComponent.GetEffectiveCooldown(int)` | private method | `Scripts/Components/SkillComponent.cs` |
| `BuffComponent.BulletCountModifier` | property | `Scripts/Components/BuffComponent.cs`【CR-003 新增】|
| `EntityConfigSO.NormalAttackSkill` | field | `Scripts/Config/EntityConfigSO.cs`【PK-ET-003 新增】|
| `BattleLevelData.NormalAttackConfig` | field（可选覆盖） | `ShooterGame/Core/BattleLevelData.cs` |
| `BattleController.SetupPlayerSkills()` | private method | `ShooterGame/Core/BattleController.cs`【PK-UA-004/007 新增】|
| `BulletSpawner.Fire` `countOverride` 参数 | optional param | `DanmakuSystem/Scripts/Core/BulletSpawner.cs`【PK-UA-012 新增】|
| `DanmakuSystem.FireBullets` `countOverride` 参数 | optional param | `DanmakuSystem/Scripts/DanmakuSystem.API.cs`【CR-005 新增】|
| `PatternScheduler.ScheduleSingle` `countOverride` 参数 | optional param | `DanmakuSystem/Scripts/Core/PatternScheduler.cs`【CR-005 新增】|
| `PatternScheduler.ScheduledTask.CountOverride` | field | `DanmakuSystem/Scripts/Core/PatternScheduler.cs`【CR-005 新增】|
| `BattleDebugLauncher._debugNormalAttack` | serialized field | `ShooterGame/Core/BattleDebugLauncher.cs`【PK-ET-002 新增】|

### 4.2 修改

| 符号 | 变更 |
|------|------|
| `SkillComponent.GetAimDirection` | 签名新增 `SkillConfigSO config` 参数 |
| `SkillComponent.TickSlot` | 签名从 `(ref SkillSlot, float)` → `(int slotIndex, float dt)`【CR-001】|
| `SkillComponent.EnterRecovery` | 签名从 `(ref SkillSlot)` → `(int slotIndex)` + 使用 GetEffectiveCooldown【CR-001/002】|
| `SkillComponent.Tick` 主循环 | 调用 `TickSlot(i, dt)` 替代 `TickSlot(ref _slots[i], dt)`【CR-001】|
| `SkillComponent.TickSlot` (Cooldown case) | 普攻槽 Buff 攻速加速逻辑 |
| `SkillComponent.TickSlot` (Recovery case) | 使用 `GetEffectiveCooldown(slotIndex)` 替代 `slot.Config.CooldownTime`【CR-002】|
| `SkillComponent.InitWithEquipment` | 新增 `firstSlotInitialCD` 可选参数【PK-ET-009】|
| `BattleController.SetupPlayer` | 普攻作为 Slot[0] 注入 + 三层兜底逻辑【CR-004 统一】|
| `BattleDebugLauncher.BuildDebugLevelData` | 新增 NormalAttackConfig 注入【PK-ET-002】|
| `SkillCDPanel.Update` | 起始索引 0 → 1 |

### 4.3 废弃

| 符号 | 处理 |
|------|------|
| `AttackComponent` | `[Obsolete]` 标记，不删文件 |
| `ComponentType.Attack` | 保留枚举值，标记注释 |
| `TickOrders.Attack (150)` | 保留常量，注释为 legacy |
| `EntityConfigSO.AttackInterval/AttackBulletPattern/AttackFireOffset` | **保留**（运行时读取 + 向后兼容） |
| `BuffComponent.GetBulletCountModifier()` | `[Obsolete]` → 使用 `BulletCountModifier` 属性【CR-003】|

---

## 五、风险与缓解

| # | 风险 | 影响 | 缓解 |
|---|------|------|------|
| R1 | Buff 攻速 `1/modifier` 导致 modifier→0 时 CD 消耗无限快 | Slot[0] 每帧触发 | `MIN_ATTACK_INTERVAL_RATIO = 0.1f`（BuffComponent 已有 Clamp） |
| R2 | SK-P04 火力全开 Buff 同时影响 BulletCount — 是否作用于 Slot[0] 的 FireBulletsEffect | 多发弹幕 | 验证 FireBulletsEffect→DanmakuSystem.FireBullets 是否读取 BuffComponent.BulletCountModifier；若否需注入 |
| R3 | SkillComponent.InitWithEquipment 错开 CD 对 Slot[0] 的影响 | 开场 0.5s 延迟 | Slot[0] stagger=0（第一个槽不错开） |
| R4 | EntityConfigSO.AttackInterval 语义变化——策划可能误改 | 射速异常 | Editor 中 AttackInterval 字段加 `[Header("⚠ 此值运行时覆盖到 Slot[0] CD")]` 提示 |
| R5 | 已有 SkillConfigSO 的 AimMode 默认值 = AutoAim，与当前行为一致 | 无回归 | ✅ 默认值安全 |
| R6 | 【PK-UA-009/013】FixedForward 硬编码 `Vector2.up`（项目特化） | 非纵版游戏复用时方向错误 | 文档注释 + 未来扩展时从 `SkillConfigSO.FixedDirection` 读取 |
| R7 | 【PK-ET-003】NormalAttackConfig 三层兜底（BattleLevelData→EntityConfigSO→Resources） | 兜底链过长可能掩盖配置问题 | 第三层（Resources）仅在直跑模式使用 + LogWarning 提示 |

---

## 六、验收方案

### 6.0 Phase 门禁验收矩阵（PK-UA-002 修正）

> **铁律**：每个 Phase 完成时只验"不通过会阻塞下一 Phase"的项，且必须在当前环境可执行（无需额外 UI/美术/真机）。

| Phase | 门禁验收（当前环境可执行） | 阻塞判定 |
|-------|--------------------------|----------|
| P1 | 编译通过 + Inspector 显示 AimMode / IsNormalAttack 字段 | 字段缺失 → 阻塞 P2 |
| P2 | 已有技能行为不变（AimMode 默认=AutoAim）；新建临时 SO 设 FixedForward → 方向=Vector2.up | AimMode 分派错误 → 阻塞 P6 |
| P3 | OverrideSlotCooldown(0, 0.5f) 后 GetEffectiveCooldown(0) == 0.5f；EnterRecovery 使用覆盖值 | API 不生效 → 阻塞 P4/P6 |
| P4 | 火力全开 Buff 激活 → Slot[0] CD 消耗速率加倍 + BulletCount 增加 | Buff 路径断裂 → 阻塞 G4 |
| P5 | SO Inspector 配置完整（SK_NormalAttack.asset），BattleLevelData 引用无 Missing | 资产损坏 → 阻塞 P6 |
| P6 | PlayTest 射击行为与改造前一致（射速/方向/弹幕数量/首发延迟） | G5 回归 → 阻塞 P7 |
| P7 | 零 AttackComponent.Init() 调用（断点验证）；CreateComponent 返回 null + LogWarning | 残留实例 → 阻塞 G1 |
| P8 | SkillCDPanel 只显示 Slot[1~N]，Slot[0] 无 CD 条 | UI 显示普攻 CD → 用户体验问题 |
| P9 | Editor Custom Inspector 正常显示新字段，无 Console 错误 | Editor 报错 → 策划不可用 |
| P10 | 全链路 5 关 PlayTest + Profiler Deep Profile 0 Alloc（热路径） | 性能回归 → 阻塞发版 |
| P11 | 文档一致性检查（TDD_01/02/06 无矛盾） | — |

### 6.1 功能验收（全局集成）

| # | 用例 | 预期结果 | 验证方式 |
|---|------|---------|---------|
| T1 | 玩家进入战斗，无额外技能装备 | Slot[0] 普攻正常射击，射速 = EntityConfigSO.AttackInterval | PlayMode |
| T2 | 装备 3 技能，进入战斗 | Slot[0]=普攻 + Slot[1~3]=技能，各自独立 CD | PlayMode |
| T3 | 拾取火力全开道具 | 普攻射速翻倍（CD 消耗 2x） + 弹幕数翻倍 | PlayMode + Profiler |
| T4 | 火力全开到期 | 普攻恢复原始射速 | PlayMode |
| T5 | 普攻方向 | 永远朝正前方（AimMode=FixedForward），不跟 AutoAim | PlayMode |
| T6 | 技能方向 | 跟踪 AutoAim 目标（AimMode=AutoAim） | PlayMode |
| T7 | SkillCDPanel | 只显示 Slot[1~5]，Slot[0] 不显示 | UI 检查 |
| T8 | Entity 死亡 | Slot[0] 立即中断（与现有死亡逻辑一致） | PlayMode |

### 6.2 性能验收

| # | 指标 | 标准 |
|---|------|------|
| P1 | 战斗热路径 GC | 0 Alloc（Profiler Deep Profile） |
| P2 | TickSlot 新增 Buff 查询 | 仅 Slot[0] 查询，≤1 GetComponent/frame，缓存可选 |
| P3 | 总帧时间 | 不超过改造前 +0.1ms |

### 6.3 兼容性验收

| # | 检查项 |
|---|--------|
| C1 | 编译零错误零警告（排除 Obsolete 警告） |
| C2 | 现有 5 关 PlayTest 无回归 |
| C3 | 微信开发者工具编译通过（无 WebGL 不兼容） |
| C4 | EntityConfigSO Inspector 无 Missing Reference |

---

## 七、ADR

### ADR-036: 普攻统一为 SkillComponent Slot[0]

**状态**：提议

**上下文**：
Phase 3 设计决策 GD-R4-003 确立了"AttackComponent 与 SkillComponent 共存不替代"的原则。
5 个 Sprint 全部完成后，事实证明两套系统并存导致 Buff 影响路径分裂、瞄准逻辑重复、新功能需双份适配。

**决策**：
将 AttackComponent 废弃，普攻统一收编为 SkillComponent Slot[0]，通过 `AimMode` 枚举实现瞄准策略数据驱动。

**后果**：
- ✅ Buff 统一影响所有攻击（CD + BulletCount）
- ✅ 瞄准策略可配置（普攻=Forward，技能=AutoAim）
- ✅ 减少 1 个 ComponentType + 1 个 TickOrder 阶段
- ⚠️ Slot[0] 语义变为"普攻专用"——策划需知晓
- ⚠️ `EntityConfigSO.AttackInterval` 语义从"组件配置"变为"运行时覆盖源"

---

_创建于 2026-05-25 | 普攻升格 TDD v0.1_
