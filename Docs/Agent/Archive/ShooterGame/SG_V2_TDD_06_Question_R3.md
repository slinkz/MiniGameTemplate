# TDD-06 PK Round 3：代码评审专家视角

> **PK 轮次**：第 3 轮（独立视角）
> **攻方角色**：代码评审专家（10年+ 经验，专注防御编程/命名一致性/GC 热点/API 契约/边界条件）
> **守方角色**：软件架构师
> **最大轮次**：8
> **开始时间**：2026-05-25 15:53
> **目标文档**：`SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md` v0.3
> **前序 PK**：R1（架构师 2轮14问）、R2（编辑器工具 2轮10问）

---

## Round 1 — 攻方（代码评审专家）提问

### CR-001 | 🔴高 | TickSlot 签名改造不完整——TDD 与现有代码不一致

**涉及章节**：§2.7

**质疑**：

TDD §2.7 中 TickSlot 代码示例写的是：
```csharp
private void TickSlot(int slotIndex, float dt)
{
    ref var slot = ref _slots[slotIndex];
    // ...
}
```

但实际代码中 TickSlot 签名是：
```csharp
private void TickSlot(ref SkillSlot slot, float dt) // 无 slotIndex
```

这意味着：
1. TDD 要求改签名从 `ref SkillSlot` → `int slotIndex`（以便调用 `GetEffectiveCooldown(slotIndex)`）
2. 但 TDD 的 Tick 主循环调用处未给出改造代码——当前调用处传的是 `ref _slots[i]`
3. `GetEffectiveCooldown(int slotIndex)` 需要 index，而 `EnterRecovery(ref SkillSlot)` 也需要 index 才能查 `_runtimeCooldownOverrides[slotIndex]`

**潜在风险**：编码时会发现 TickSlot 改签名后，Tick 主循环、EnterRecovery、Buff 攻速修正三处都需要联动修改。TDD 只给了 TickSlot 本体的示例，遗漏了调用链改造。

**建议方向**：补充 Tick 主循环改造代码 + EnterRecovery 改造代码（传 slotIndex），形成完整的签名迁移链路。

---

### CR-002 | 🔴高 | EnterRecovery 改造代码缺失——§4.2 声明要改但 §二 无实现

**涉及章节**：§2.7 + §4.2

**质疑**：

§4.2 接口变更表明确声明：
> `SkillComponent.EnterRecovery` | 使用 `GetEffectiveCooldown` 替代直接读 `Config.CooldownTime`

但翻遍 §二 设计方案，**找不到 EnterRecovery 的改造代码**。当前 EnterRecovery 代码：
```csharp
private void EnterRecovery(ref SkillSlot slot)
{
    // ...
    slot.CooldownTimer = slot.Config.CooldownTime;  // 直接读 SO，忽略运行时覆盖！
    // ...
}
```

普攻场景：`SK_NormalAttack.CooldownTime = 0.25f`，但 `OverrideSlotCooldown(0, playerConfig.AttackInterval)` 覆盖为不同值（如 0.3f）。如果 EnterRecovery 不改用 `GetEffectiveCooldown`，**运行时覆盖的 CD 值会被忽略**，普攻射速永远是 SO 默认值而非 EntityConfigSO.AttackInterval。

**潜在风险**：G5（零行为回归）不通过——普攻射速与改造前不一致。这是 **阻塞性 bug**。

**建议方向**：补充 EnterRecovery 改造代码——需要改签名接收 slotIndex（或额外传 slotIndex），读 `GetEffectiveCooldown(slotIndex)`。

---

### CR-003 | 🔴高 | BulletCountModifier API 名称错误——TDD 引用了不存在的属性

**涉及章节**：§2.5

**质疑**：

TDD §2.5 代码示例写的是：
```csharp
var buffComp = ctx.Caster.GetComponent(ComponentType.Buff) as BuffComponent;
if (buffComp != null && buffComp.BulletCountModifier != 1f)
    baseCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * buffComp.BulletCountModifier));
```

但实际 BuffComponent 中 **没有 `BulletCountModifier` 属性**。实际 API 是：
```csharp
public float GetBulletCountModifier()  // 方法！不是属性！
```

这是**编译错误**级别的问题——代码写上去就红。

**潜在风险**：编码者可能以为 BuffComponent 有这个属性，浪费时间找 bug。或者编码者自行添加属性（偏离设计）。

**建议方向**：修正为 `buffComp.GetBulletCountModifier()` 或在 TDD 中明确要求新增对应属性。从 API 设计角度，建议统一为属性（与 `AttackIntervalModifier` 风格一致），需要在 §四 接口变更中登记。

---

### CR-004 | 🟡中 | §2.8 SetupPlayerSkills 代码存在逻辑自相矛盾

**涉及章节**：§2.8

**质疑**：

§2.8 中 SetupPlayerSkills 方法体有两段互相矛盾的代码：

**第一段（L404~409）**：
```csharp
if (_battleLevelData.NormalAttackConfig == null)
{
    Debug.LogError("[BattleController] NormalAttackConfig 未配置！普攻将不可用。");
    return;  // ← 直接 return！
}
```

**第二段（L441~453）**：「PK-ET-002 补充」三层兜底逻辑：
```csharp
var normalAttack = _battleLevelData?.NormalAttackConfig   // 可以为 null
                ?? _playerEntityConfig.NormalAttackSkill;  // 兜底
if (normalAttack == null)
{
    normalAttack = Resources.Load<SkillConfigSO>("ShooterGame/SK_NormalAttack"); // 再兜底
}
```

如果第一段代码保留，`NormalAttackConfig == null` 时直接 return 了，根本走不到三层兜底。如果用三层兜底，第一段 null 检查就不应该 return。

**两段代码来自不同 PK 轮次的叠加（UA-014 vs ET-002），没有合并成一个统一逻辑。**

**潜在风险**：编码者不知道该按哪段实现，或两段都实现导致第二段成为死代码。

**建议方向**：合并为一个统一的获取逻辑——删除第一段的 hard return，用三层兜底替代。

---

### CR-005 | 🟡中 | DanmakuSystem.FireBullets 签名不支持 countOverride——调用链断裂

**涉及章节**：§2.5

**质疑**：

TDD §2.5 中 FireBulletsEffect.Execute 改造为：
```csharp
ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId, baseCount);
```

但当前 `DanmakuSystem.FireBullets` 签名是：
```csharp
public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle,
    uint ownerEntityId = 0, int sourceTag = 0)
```

**没有 count 参数**。传入 `baseCount` 会编译报错。

完整调用链：`FireBulletsEffect → DanmakuSystem.FireBullets → PatternScheduler.ScheduleSingle → BulletSpawner.Fire`

TDD 只给了 `BulletSpawner.Fire` 的 `countOverride` 改造，但 **DanmakuSystem.FireBullets** 和 **PatternScheduler.ScheduleSingle** 这两层中间件都没有透传 countOverride 的签名改造。

**潜在风险**：编码时发现中间层需要连续修改 3 个方法签名（Fire → ScheduleSingle → FireBullets），TDD 只覆盖了首尾两端，中间断裂。

**建议方向**：补充 DanmakuSystem.FireBullets + PatternScheduler.ScheduleSingle 的签名变更到 §四 接口变更表中。

---

### CR-006 | 🟡中 | §2.4 Buff 攻速查询每帧 GetComponent——热路径 GC 风险

**涉及章节**：§2.4 + §6.2 P2

**质疑**：

§2.4 代码在 TickSlot 的 Cooldown case 中每帧查询：
```csharp
if (slot.Config.IsNormalAttack)
{
    var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
    if (buff != null && buff.AttackIntervalModifier != 1f)
        cdDt = dt / buff.AttackIntervalModifier;
}
```

§6.2 P2 说：`仅 Slot[0] 查询，≤1 GetComponent/frame，缓存可选`

但代码验证：当前 SkillComponent 已有 `_cachedDecisionMaker` 缓存模式——Init 时缓存一次，后续直接用。对 BuffComponent 却没有缓存，每帧走 `_owner.GetComponent(ComponentType.Buff)`。

Entity.GetComponent 的实现方式决定了 GC 风险——如果是 Dictionary 查询则无 GC，如果是 List.Find 则可能有。无论如何，缓存是零成本优化，与已有缓存模式一致。

**潜在风险**：与 §6.2 P1 `0 Alloc（热路径）`的承诺可能冲突。即便当前实现无 GC，不缓存也是"知道怎么做对却不做"的代码异味。

**建议方向**：在 Init 中缓存 `_cachedBuffComponent`，与 `_cachedDecisionMaker` 保持一致的缓存策略。在 §2.4 代码中使用缓存字段。

---

### CR-007 | 🟡中 | §2.8 firstSlotInitialCD 参数传递但没有使用

**涉及章节**：§2.8

**质疑**：

§2.8 BattleController 调用有两个版本：

**版本 1**（SetupPlayerSkills 方法体 L421）：
```csharp
skillComp.InitWithEquipment(allSkills, staggerOffsetPerSlot: 0.5f);
```
—— **没有传 firstSlotInitialCD**！

**版本 2**（「BattleController 完整调用」L434）：
```csharp
skillComp.InitWithEquipment(allSkills, staggerOffsetPerSlot: 0.5f, firstSlotInitialCD: attackInterval);
```
—— **传了 firstSlotInitialCD**！

同一个方法，文档中出现两个不同的调用方式。编码者不知道该跟哪个。

**潜在风险**：如果用版本 1（不传），Slot[0] 无首发延迟，开场瞬间开火（与改造前 AttackComponent 行为不一致）。如果用版本 2，才能保证 G5 回归。

**建议方向**：删除版本 1，只保留版本 2（完整的 `SetupPlayerSkills` 方法体应该包含 `firstSlotInitialCD: attackInterval`）。

---

### CR-008 | 🟢低 | BulletSpawner.Fire 签名与 TDD 不一致——internal 可见性问题

**涉及章节**：§2.5

**质疑**：

TDD §2.5 给出的 `BulletSpawner.Fire` 改造签名含 `DanmakuTypeRegistry registry` 参数：
```csharp
internal static void Fire(
    BulletPatternSO pattern, Vector2 origin, float baseAngleDeg,
    BulletWorld world, DanmakuTypeRegistry registry,
    DifficultyProfileSO difficulty = null, TrailPool trailPool = null,
    uint ownerEntityId = 0, int sourceTag = 0,
    int? countOverride = null)
```

但实际 `BulletSpawner.Fire` 签名没有 `DanmakuTypeRegistry registry` 参数（至少在 CodeGraph 返回的代码中没有）。这可能是 TDD 对签名的预设偏离了实际代码。

**潜在风险**：编码时对不上号，需要重新核对签名。低风险，但属于文档精度问题。

**建议方向**：校对 BulletSpawner.Fire 的真实签名，TDD 示例代码应与实际代码保持一致。

---

**攻方 Round 1 小结**：
- 🔴高：3 个（CR-001 签名链断裂、CR-002 EnterRecovery 缺失、CR-003 API 名称错误）
- 🟡中：4 个（CR-004 逻辑矛盾、CR-005 调用链断裂、CR-006 热路径缓存、CR-007 参数不一致）
- 🟢低：1 个（CR-008 签名校对）
- 总计 8 个问题

---

## Round 1 — 守方（软件架构师）回应

### 对 CR-001（🔴 TickSlot 签名改造不完整）— ✅ 接受，补充完整改造链

**承认**：TDD 确实只给了 TickSlot 新签名和内部代码，但没有给出 Tick 主循环的调用处改造、以及 EnterRecovery 的联动修改。这对编码者来说是"拼图缺了关键几片"。

**修正方案**：

**1. Tick 主循环改造**：
```csharp
// SkillComponent.Tick — 当前调用方式
for (int i = 0; i < ActiveSlotCount; i++)
    TickSlot(ref _slots[i], dt);

// 改造后
for (int i = 0; i < ActiveSlotCount; i++)
    TickSlot(i, dt);  // 传 index 而非 ref
```

**2. TickSlot 签名变更**（§2.7 已有，保持不变）：
```csharp
private void TickSlot(int slotIndex, float dt)
{
    ref var slot = ref _slots[slotIndex];
    // ... 全部内部逻辑用 ref slot 操作
}
```

**3. EnterRecovery 签名联动**（§2.7 内补充）：
```csharp
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
            slot.CooldownTimer = 0.001f;
            slot.State = SkillState.Cooldown;
            Debug.LogWarning($"[SkillComponent] {slot.Config.DisplayName} CD=0，已强制最小间隔。");
        }
    }
}
```

**4. Recovery case 也要改**（当前在 TickSlot 内）：
```csharp
case SkillState.Recovery:
    slot.CastTimer -= dt;
    if (slot.CastTimer <= 0)
    {
        slot.CooldownTimer = GetEffectiveCooldown(slotIndex); // 替代 slot.Config.CooldownTime
        slot.State = SkillState.Cooldown;
    }
    break;
```

**→ 回写文档 §2.7 补充完整改造链**

---

### 对 CR-002（🔴 EnterRecovery 改造代码缺失）— ✅ 接受，已在 CR-001 回应中一并解决

问题完全正确。EnterRecovery 不改用 `GetEffectiveCooldown` 会导致普攻 CD 永远用 SO 默认值（0.25f），忽略运行时覆盖值——G5 回归直接挂。

已在 CR-001 回应中补充了完整的 EnterRecovery 改造。

**→ 与 CR-001 合并回写**

---

### 对 CR-003（🔴 BulletCountModifier API 名称错误）— ✅ 接受，统一为属性

**承认**：TDD 引用了 `buffComp.BulletCountModifier`（属性），但实际代码只有 `GetBulletCountModifier()`（方法）。编译报错级别问题。

**修正方案**：

两种选择：
- **A**：TDD 改为 `buffComp.GetBulletCountModifier()`——与现有代码一致
- **B**：BuffComponent 新增 `BulletCountModifier` 属性，与 `AttackIntervalModifier` / `MoveSpeedModifier` 风格统一

**决策：方案 B**——API 风格统一更重要。

```csharp
// BuffComponent.cs — RecalcModifiers 中新增 BulletCount 聚合为缓存属性
// 当前 BulletCount 是实时遍历的 GetBulletCountModifier()，改为一致的缓存属性

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

// 旧方法标记废弃
[System.Obsolete("Use BulletCountModifier property instead")]
public float GetBulletCountModifier() => BulletCountModifier;
```

**→ 回写文档 §2.5 + §四 接口变更**

---

### 对 CR-004（🟡 SetupPlayerSkills 逻辑矛盾）— ✅ 接受，合并为统一逻辑

**承认**：两段代码来自不同 PK 轮次（UA-014 的 hard return vs ET-002 的三层兜底），叠加后确实矛盾。

**修正方案**：删除第一段 hard return，保留三层兜底作为唯一路径：

```csharp
private void SetupPlayerSkills()
{
    var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
    if (skillComp == null) return;

    // 三层兜底获取普攻配置（CR-004 统一）
    var normalAttack = _battleLevelData?.NormalAttackConfig       // 1. BattleLevelData 覆盖
                    ?? _playerEntityConfig.NormalAttackSkill;      // 2. EntityConfigSO 自带
    if (normalAttack == null)
        normalAttack = Resources.Load<SkillConfigSO>("ShooterGame/SK_NormalAttack"); // 3. 兜底

    if (normalAttack == null)
    {
        Debug.LogError("[BattleController] 无普攻配置！检查 EntityConfigSO.NormalAttackSkill");
        return;
    }

    // 组装技能数组
    var equipped = _battleLevelData?.EquippedSkills;
    int equipCount = equipped?.Length ?? 0;
    int totalSlots = Mathf.Min(1 + equipCount, SkillComponent.MAX_SLOTS);
    var allSkills = new SkillConfigSO[totalSlots];
    allSkills[0] = normalAttack;
    for (int i = 0; i < equipCount && i + 1 < totalSlots; i++)
        allSkills[i + 1] = equipped[i];

    float attackInterval = _playerEntityConfig.AttackInterval;
    skillComp.InitWithEquipment(allSkills, staggerOffsetPerSlot: 0.5f,
                                firstSlotInitialCD: attackInterval);
    skillComp.OverrideSlotCooldown(0, attackInterval);
}
```

**→ 回写文档 §2.8 替换为统一版本**

---

### 对 CR-005（🟡 调用链断裂）— ✅ 接受，补充中间层签名

**承认**：TDD 只改了 BulletSpawner.Fire（尾端）和 FireBulletsEffect（头端），中间两层没有透传 countOverride。

**修正方案**：

```csharp
// 1. DanmakuSystem.API.cs — FireBullets 新增 countOverride 参数
public void FireBullets(BulletPatternSO pattern, Vector2 origin, float baseAngle,
    uint ownerEntityId = 0, int sourceTag = 0, int? countOverride = null)
{
    _scheduler.ScheduleSingle(pattern, origin, baseAngle, ownerEntityId, sourceTag, countOverride);
}

// 2. PatternScheduler.ScheduleSingle — 新增 countOverride，存入 task
public void ScheduleSingle(BulletPatternSO pattern, Vector2 origin, float baseAngle,
    uint ownerEntityId = 0, int sourceTag = 0, int? countOverride = null)
{
    // ... 现有逻辑 ...
    ref var task = ref _tasks[slot];
    task.CountOverride = countOverride;  // 新增 Nullable<int> 字段
    // ...
}

// 3. PatternScheduler.Tick 中调用 BulletSpawner.Fire 时透传
BulletSpawner.Fire(task.Pattern, task.Origin, task.Angle,
    _world, _registry, _difficulty, _trailPool,
    task.OwnerEntityId, task.SourceTag, task.CountOverride);

// 4. BulletSpawner.Fire — 新增 countOverride（TDD 已有）
internal static void Fire(..., int? countOverride = null)
{
    int count = countOverride ?? pattern.Count;
    if (difficulty != null)
        count = Mathf.RoundToInt(count * difficulty.CountMultiplier);
    // ...
}
```

**→ 回写文档 §2.5 补充完整链路 + §四 接口变更新增 3 个中间层签名**

---

### 对 CR-006（🟡 热路径缓存）— ✅ 接受

**分析**：`Entity.GetComponent(ComponentType.Buff)` 实际是数组直接索引（`_components[(int)type]`），**零 GC、零性能开销**。

但接受攻方建议——代码风格一致性很重要。已有 `_cachedDecisionMaker`，不缓存 BuffComponent 确实是风格不统一。

**修正方案**：
```csharp
// SkillComponent.cs
private BuffComponent _cachedBuffComponent;

public void Init(Entity owner)
{
    _owner = owner;
    _cachedDecisionMaker = ...;
    _cachedBuffComponent = owner.GetComponent(ComponentType.Buff) as BuffComponent;
}
```

§2.4 Cooldown case 改为：
```csharp
if (slot.Config.IsNormalAttack && _cachedBuffComponent != null
    && _cachedBuffComponent.AttackIntervalModifier != 1f)
{
    cdDt = dt / _cachedBuffComponent.AttackIntervalModifier;
}
```

**注意**：Init 时 BuffComponent 可能还没创建完（取决于 Components 数组顺序）。验证：ComponentType.Buff=10 > ComponentType.Skill=6，按顺序初始化时 Buff 在 Skill 后面！

**应对**：改为 lazy 缓存（首次使用时查询并缓存）：
```csharp
private BuffComponent _cachedBuffComponent;
private bool _buffCacheDirty = true;

private BuffComponent GetBuffComponent()
{
    if (_buffCacheDirty)
    {
        _cachedBuffComponent = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
        _buffCacheDirty = false;
    }
    return _cachedBuffComponent;
}
```

或更简单：因为 Entity.GetComponent 就是数组索引（O(1) 无 GC），**直接每帧调用即可**，不额外缓存。风格不一致的代价 < 引入 lazy 缓存的复杂度。

**最终决策**：保持每帧 `_owner.GetComponent(ComponentType.Buff)`——已验证为零 GC 数组索引。在代码中加注释解释为什么不缓存（初始化顺序问题）。

**→ 回写文档 §2.4 加注释**

---

### 对 CR-007（🟡 firstSlotInitialCD 参数不一致）— ✅ 接受

**承认**：§2.8 中存在两个互相矛盾的调用版本。

已在 CR-004 的统一 SetupPlayerSkills 版本中修复——只有一份代码，明确传 `firstSlotInitialCD: attackInterval`。

**→ 已在 CR-004 中一并解决**

---

### 对 CR-008（🟢 BulletSpawner.Fire 签名校对）— ✅ 确认无误

经完整代码验证：`BulletSpawner.Fire` 确实包含 `DanmakuTypeRegistry registry` 参数（L22~23）。TDD 签名与实际代码一致，无需修改。

---

**守方 Round 1 小结**：
- 8 个问题中 7 个接受、1 个确认无误（CR-008）
- 0 个驳回
- 关键新增：TickSlot 完整签名链 + EnterRecovery 联动改造 + BulletCountModifier 属性化 + 弹幕系统全链路透传
- CR-006 最终决策为"不缓存但加注释"（O(1) 数组索引无需缓存）

---

## Round 2 — 攻方（代码评审专家）追问

### Round 1 回应评估

| ID | 判定 | 理由 |
|----|------|------|
| CR-001 | 🟢 满意 | 完整的 4 步改造链清晰可执行 |
| CR-002 | 🟢 满意 | 合并到 CR-001 解决，EnterRecovery 改造代码完整 |
| CR-003 | 🟢 满意 | 方案 B（属性化）与现有 API 风格统一，废弃旧方法保证向后兼容 |
| CR-004 | 🟢 满意 | 统一版本消除矛盾，三层兜底逻辑清晰 |
| CR-005 | 🟢 满意 | 4 步完整链路（FireBulletsEffect → DanmakuSystem → PatternScheduler → BulletSpawner） |
| CR-006 | 🟢 满意 | 分析到位——O(1) 无 GC，不缓存是合理决策，注释解释了初始化顺序问题 |
| CR-007 | 🟢 满意 | 合并到 CR-004 解决 |
| CR-008 | 🟢 满意 | 确认无误 |

### 新问题

### CR-009 | 🟢低 | PatternScheduler task 结构新增 CountOverride 字段——Burst 连射场景的语义

**涉及章节**：守方对 CR-005 的回应

**质疑**：

PatternScheduler.ScheduleSingle 中，当 `pattern.BurstCount > 1` 时，会创建多个 task（每 burst 一个 task）。CountOverride 存入每个 task，意味着 Burst 的每一发都用相同的 countOverride。

对于普攻场景（BurstCount 通常=1）这没问题。但如果未来有 Burst+CountOverride 的组合场景，语义是否清晰？

这不是阻塞问题，只是希望在 TDD 或代码注释中明确：**countOverride 作用于每一次 Burst 的单次 Fire**。

**建议方向**：在 BulletSpawner.Fire 的 countOverride 参数文档注释中明确语义。

---

### CR-010 | 🟢低 | InitWithEquipment 新参数 firstSlotInitialCD 的边界条件

**涉及章节**：§2.7

**质疑**：

`firstSlotInitialCD` 的边界行为：
- 如果传入**负值**怎么办？当前代码 `if (firstSlotInitialCD > 0f)` 会跳过——正确。
- 如果传入值**大于 OverrideSlotCooldown 设置的 CD**（如 firstSlotInitialCD=5f，CD=0.25f）？Slot[0] 会等 5 秒才首发。这是调用者的责任还是 InitWithEquipment 应该 Clamp？

当前设计中 `firstSlotInitialCD = attackInterval`（与 CD 相同），不会越界。但作为防御编程，建议加一行注释说明调用者责任。

**建议方向**：注释说明 `firstSlotInitialCD 通常等于 OverrideSlotCooldown 的值，不做内部 Clamp`。

---

**攻方 Round 2 小结**：
- 8/8 问题 Round 1 全部满意
- 仅新增 2 个 🟢低（文档完善性建议）
- 无 🔴高/🟡中——**明确趋势收敛**

---

## Round 2 — 守方（软件架构师）回应

### 对 CR-009（🟢 Burst+CountOverride 语义）— ✅ 接受

**修正方案**：在 BulletSpawner.Fire 的 `countOverride` 参数添加注释：

```csharp
/// <param name="countOverride">覆盖 Pattern.Count 的弹丸数量（Buff 修正后）。
/// 作用于每一次独立的 Fire 调用（含 Burst 场景下的每一 burst）。
/// null = 使用 Pattern.Count 原始值。
/// 注意：难度乘数（DifficultyProfileSO.CountMultiplier）在此之后继续生效。</param>
```

**→ 回写文档 §2.5 BulletSpawner.Fire 参数注释**

---

### 对 CR-010（🟢 firstSlotInitialCD 边界条件）— ✅ 接受

**修正方案**：在 §2.7 InitWithEquipment 的 `firstSlotInitialCD` 参数注释中补充：

```csharp
/// <param name="firstSlotInitialCD">Slot[0] 首发延迟（秒）。
///   >0 = Slot[0] 初始进入 Cooldown 状态等待此时间后首发。
///   通常等于 OverrideSlotCooldown(0, x) 的同一个 x 值（如 attackInterval）。
///   不做内部 Clamp——调用者负责确保值合理。
///   ≤0 = 立即可用（无首发延迟）。</param>
```

**→ 回写文档 §2.7 参数注释**

---

**守方 Round 2 小结**：
- 2 个 🟢低全部接受
- 均为文档注释补充，不影响逻辑
- 0 个驳回

---

## 收敛判定

| 维度 | 状态 |
|------|------|
| 🔴高 问题全部解决 | ✅（CR-001/002/003 均有完整方案） |
| 🟡中 问题全部解决 | ✅（CR-004~007 均有修正） |
| 🟢低 无分歧 | ✅（CR-008/009/010 全部接受） |
| 攻方无新 🔴高/🟡中 | ✅（Round 2 仅 🟢低） |
| 严重度呈下降趋势 | ✅（R1: 3🔴4🟡1🟢 → R2: 0🔴0🟡2🟢） |
| 连续两轮无 🔴高 | ✅ |

**✅ 收敛达成 — PK 结束（2 轮）**

---

## PK 最终统计

| 指标 | 数值 |
|------|------|
| 总轮次 | 2 |
| 攻方问题总数 | 10（CR-001 ~ CR-010） |
| 🔴高 | 3 → 全部解决 |
| 🟡中 | 4 → 全部解决 |
| 🟢低 | 3 → 全部接受 |
| 守方驳回 | 0 |
| 文档需回写修改点 | 7 处 |

## Top 3 最有价值变更

1. **CR-001/002**：暴露 TickSlot 签名改造的**完整链路遗漏**——Tick 主循环 + EnterRecovery + Recovery case 三处联动未在 TDD 中体现。不修复则 `OverrideSlotCooldown` 整个功能等于废代码。
2. **CR-003**：`BulletCountModifier` 属性名错误会直接导致**编译失败**。顺便推动 BuffComponent API 风格统一（所有 modifier 都是缓存属性，不再有遗留的 Get 方法）。
3. **CR-005**：弹幕系统 countOverride 调用链 3 层中间件断裂——DanmakuSystem + PatternScheduler + BulletSpawner 需要连续修改 4 个方法签名，TDD 只覆盖首尾。

## 需回写到 TDD 文档的修改清单

| # | 章节 | 修改内容 | 来源 |
|---|------|---------|------|
| 1 | §2.7 | 补充 Tick 主循环改造 + EnterRecovery 完整改造代码（含签名变更） | CR-001/002 |
| 2 | §2.5 | 修正 BulletCountModifier 为属性（非 GetBulletCountModifier 方法）；补充 DanmakuSystem + PatternScheduler 中间层透传代码 | CR-003/005 |
| 3 | §2.8 | 替换 SetupPlayerSkills 为统一版本（删除矛盾的两段代码） | CR-004/007 |
| 4 | §2.4 | 加注释说明不缓存 BuffComponent 的原因（O(1) + 初始化顺序） | CR-006 |
| 5 | §四 | 接口变更新增：BuffComponent.BulletCountModifier 属性、DanmakuSystem.FireBullets + PatternScheduler.ScheduleSingle countOverride 参数 | CR-003/005 |
| 6 | §2.5 | BulletSpawner.Fire countOverride 参数注释补充 Burst 语义 | CR-009 |
| 7 | §2.7 | firstSlotInitialCD 参数注释补充边界条件说明 | CR-010 |

> **PK 状态**：✅ 已收敛
> **结束时间**：2026-05-25 16:10
