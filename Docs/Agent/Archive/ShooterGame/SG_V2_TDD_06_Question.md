# PK 评审记录 — SG_V2_TDD_06_ATTACK_SKILL

> **目标文档**：`Docs/Agent/SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md`
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10 年以上 Unity 引擎经验，专精 ECS/组件化架构、WebGL 平台限制、性能优化）
> **守方角色**：软件架构师（10 年以上系统设计经验，专精 DDD、可维护架构、接口设计、权衡分析）
> **开始时间**：2026-05-25 13:42
> **最大轮次**：8
> **PK 状态**：🔄 进行中

---

## Round 1 — 攻方（Unity 架构师）

### UA-001 | 🔴高 | BulletCountModifier 从未在 FireBulletsEffect 中读取

**涉及章节**：§2.5
**质疑**：TDD §2.5 写"需验证 FireBulletsEffect→DanmakuSystem.FireBullets 是否读取 BuffComponent.BulletCountModifier"。经 CodeGraph 验证：`FireBulletsEffect.Execute` 直接调用 `ds.FireBullets(Pattern, pos, angle, ...)`，而 `BulletSpawner.Fire` 只读取 `DifficultyProfileSO.CountMultiplier`，**完全不读 BuffComponent**。TDD 标注为"确认点"但没给出实施方案——这是阻塞性遗漏。

**潜在风险**：G4 目标"Buff 攻速统一影响 Slot[0]"的 BulletCount 部分无法实现。火力全开道具对弹幕数量的影响被断裂。

**建议方向**：在 §2.5 中明确实施方案：`FireBulletsEffect.Execute` 需在调用 `ds.FireBullets` 前查询 `ctx.Caster.GetComponent(BuffComponent).BulletCountModifier`，对 `Pattern.Count` 做运行时乘法覆盖（或通过新增的 `FireBullets` 重载传入 countOverride）。

---

### UA-002 | 🔴高 | 验收章节未区分 Phase 门禁 vs 全局集成验收

**涉及章节**：§六
**质疑**：验收方案分为"功能验收 / 性能验收 / 兼容性验收"三类，但没有按 Phase 拆分。11 个 Phase 的逐 Phase 门禁验收标准未定义。例如 P2 完成后如何在不依赖 P5（SO 资产）的情况下验证 AimMode 分派正确？

**潜在风险**：Phase 推进时无法判断当前 Phase 是否已达标——可能在 P6 才发现 P2 的 bug，导致回溯成本高。

**建议方向**：在 §六 之前新增"Phase 门禁验收矩阵"，为每个 Phase 列出最小可验证条件（只验阻塞项，可在 Editor PlayMode 执行）。全局集成验收（真机+性能+UI）单独成节。

---

### UA-003 | 🟡中 | OverrideSlotCooldown 与 Recovery→Cooldown 赋值冲突

**涉及章节**：§2.7 / §2.4
**质疑**：当前 `SkillComponent.TickSlot` 中 Recovery→Cooldown 转换时（L185）直接 `slot.CooldownTimer = slot.Config.CooldownTime`。TDD §2.7 提出 `GetEffectiveCooldown(slotIndex)` 替代直接读取，但 `TickSlot` 中使用 `ref SkillSlot slot` 参数——**没有 slotIndex 信息**。如何在 `TickSlot` 内部调用 `GetEffectiveCooldown(slotIndex)`？

**潜在风险**：实施时发现方法签名不匹配，需要重构 TickSlot 传参或在 Tick 循环中传递 index。

**建议方向**：明确改造方案——要么 `TickSlot` 增加 `int slotIndex` 参数，要么改为按 index 迭代 `Tick` 方法中直接内联处理。

---

### UA-004 | 🟡中 | InitWithEquipment 的 Stagger 对 Slot[0]（普攻）的影响

**涉及章节**：§2.6 / §2.8
**质疑**：当前 `InitWithEquipment` 中 `_slots[i].CooldownTimer = staggerOffsetPerSlot * i`。如果普攻放在 Slot[0]，则 `stagger = 0.5 * 0 = 0`。TDD §五 R3 提到"Slot[0] stagger=0（第一个槽不错开）"——表面上没问题。但如果传入的 `equippedSkills[0]` 是普攻 SO，那 `equippedSkills[1~5]` 是主动技能，它们的 stagger 变成 `0.5*1, 0.5*2...`。**当前** BattleController 传入的 EquippedSkills 没有包含普攻，改造后需要在数组头部插入普攻 SO——这个数组组装逻辑没有伪代码。

**潜在风险**：BattleController 中数组拼接如果忘记在 index=0 放普攻，Slot[0] 会是第一个主动技能。TDD §2.8 的伪代码写了 `skillArray = [NormalAttackConfig] + equippedSkills`，但实际 C# 中 SkillConfigSO[] 拼接需要分配新数组——应给出具体实现。

**建议方向**：在 §2.8 给出完整的 C# 数组拼接代码（避免 GC 分配：预分配 MAX_SLOTS 大小，或用 stackalloc/临时缓冲区）。

---

### UA-005 | 🟡中 | _runtimeCooldownOverrides 未在 Reset/InitWithEquipment 中清零

**涉及章节**：§2.7
**质疑**：`_runtimeCooldownOverrides` 是 `readonly float[MAX_SLOTS]`（构造时分配）。但 Entity 对象池模式下，Entity Reset 后 SkillComponent 可能被复用。当前 `InitWithEquipment` 已有清零 `_slots[i] = default`，但 `_runtimeCooldownOverrides` 没有被清零。

**潜在风险**：对象池复用时残留上一局的 override 值，导致射速异常。

**建议方向**：在 `InitWithEquipment` 或 `Reset` 方法中显式 `Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS)`。

---

### UA-006 | 🟡中 | EntityConfigSO.Components 数组仍包含 ComponentType.Attack

**涉及章节**：§2.9
**质疑**：`EntityPool.CreateEntityFromConfig` 根据 `config.Components[]` 创建组件。如果 `EntityConfigSO` 的 Components 数组仍然包含 `ComponentType.Attack`，那么 `CreateComponent(ComponentType.Attack)` 仍会创建 AttackComponent 实例。TDD §2.9 说"EntityPool.CreateComponent 中 ComponentType.Attack case → 标记 [Obsolete]，不再创建"，但没说 EntityConfigSO 上的配置怎么处理。

**潜在风险**：如果只标记 Obsolete 而不从 EntityConfigSO.Components 移除 Attack，池化时仍会创建无用组件（浪费内存+Init 调用）。

**建议方向**：明确迁移策略——P7 中需要将所有 EntityConfigSO 的 Components 数组中移除 `ComponentType.Attack`。或者在 `CreateComponent` 中对 Attack case 返回 null + 日志。

---

### UA-007 | 🟡中 | Retry（重试战斗）流程未提及普攻初始化

**涉及章节**：§2.8
**质疑**：BattleController L1059~1064 中 Retry 时重新调用 `skillComp.InitWithEquipment(_battleLevelData.EquippedSkills)`。改造后需要同样将普攻插入 Slot[0] + OverrideSlotCooldown。TDD 只描述了首次初始化流程，没有提及 Retry 路径。

**潜在风险**：Retry 后 Slot[0] 丢失普攻，回退到旧行为。

**建议方向**：§2.8 补充 Retry 流程：抽取公共方法 `SetupPlayerSkills()` 同时被首次初始化和 Retry 调用。

---

### UA-008 | 🟢低 | SkillCDPanel 硬编码 "跳过 Slot[0]" 的脆弱性

**涉及章节**：§2.11
**质疑**：`int slotIndex = i + 1` 假定 Slot[0] 永远是普攻。如果未来允许多普攻或普攻换槽，此硬编码会失效。

**潜在风险**：可维护性风险，但非当前阻塞项。

**建议方向**：可考虑用 `slot.Config.IsNormalAttack` 来 skip 而非硬编码 index——更稳健。

---

### UA-009 | 🟢低 | AimMode.FixedForward 中 Rotation 语义歧义

**涉及章节**：§2.3
**质疑**：代码中 `float rad = _owner.Rotation * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad))`。当前玩家飞机 Rotation=270°（朝上 spawn）。Cos(270°)=0, Sin(270°)=-1 → Vector2(0,-1) = 朝下！但实际玩家应该朝上射击。当前 AttackComponent 似乎不通过 Rotation 来决定方向（它直接用 Pattern 的 baseAngle），这里有行为不一致的风险。

**潜在风险**：FixedForward 实际计算出的方向与当前普攻表现不一致，导致 G5（零行为回归）目标失败。

**建议方向**：验证当前 AttackComponent.Tick 中的方向传递逻辑（baseAngleDeg 传什么值），确保 FixedForward 的计算与之等价。如果当前普攻方向是固定 90°（向上），可能需要 FixedForward = Rotation + 90° 或直接用 Vector2.up。

---

### UA-010 | 🟢低 | FirstAttackDelay（首发延迟）兼容性

**涉及章节**：§2.8 / §三 P6
**质疑**：MEMORY 中记录 `FirstAttackDelay 默认 1.0s`。当前 AttackComponent 似乎有首次攻击延迟机制。改造为 SkillComponent Slot[0] 后，首次进入战斗时 Slot[0] 的 CooldownTimer 初始值是什么？`InitWithEquipment` 中设为 `staggerOffsetPerSlot * 0 = 0`，意味着 Slot[0] 立即进入 Idle → 立即射击。如果当前有 1s 首发延迟，这就是行为回归。

**潜在风险**：改造后玩家进入战斗瞬间就射击（无首发延迟），与当前体验不同。

**建议方向**：确认 AttackComponent 是否有 FirstAttackDelay；若有，需要在 `OverrideSlotCooldown` 后额外设置 `_slots[0].CooldownTimer = firstAttackDelay`。

---

### UA-011 | 🟢低 | SO 资产共用 + 运行时覆盖的序列化安全

**涉及章节**：§2.6
**质疑**：TDD 选择"共用 1 个 SK_NormalAttack SO + 运行时覆盖 CD"。`OverrideSlotCooldown` 写入 `_runtimeCooldownOverrides[]`（实例字段），不修改 SO——安全。但需确认 `EnterRecovery` 中 `slot.CooldownTimer = GetEffectiveCooldown(idx)` 不会意外写回 SO。

**潜在风险**：低风险，但需代码 Review 确认。

**建议方向**：在 §2.7 加一行注释明确"不修改 SO，只读 Config.CooldownTime 作为 fallback"。

---

**攻方小结**：
- 🔴 高（阻塞编码）：2 个（UA-001, UA-002）
- 🟡 中（建议实施前解决）：5 个（UA-003~007）
- 🟢 低（编码期迭代）：4 个（UA-008~011）

---

## Round 1 — 守方（软件架构师）回应

### 对 UA-001（🔴 BulletCountModifier 缺失）— ✅ 接受，补充实施方案

**承认**：代码验证确认 `FireBulletsEffect.Execute` 和 `BulletSpawner.Fire` 均不读取 `BuffComponent.BulletCountModifier`。这是阻塞性遗漏。

**修正方案**：
```csharp
// FireBulletsEffect.Execute 改造（在调用 ds.FireBullets 前）
int countOverride = Pattern.Count;
var buffComp = ctx.Caster.GetComponent(ComponentType.Buff) as BuffComponent;
if (buffComp != null)
    countOverride = Mathf.RoundToInt(countOverride * buffComp.BulletCountModifier);

// 新增 DanmakuSystem.FireBullets 重载
ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId, countOverride);
```
- `BulletSpawner.Fire` 新增可选参数 `int? countOverride = null`，有值时替代 `pattern.Count`。
- **不修改 SO**——运行时覆盖，保持 Pattern 数据干净。
- 此修改归入 Phase 4（Buff 攻速修正迁移）的补充步骤。

**→ 回写文档 §2.5**

---

### 对 UA-002（🔴 验收未分层）— ✅ 接受，补充 Phase 门禁矩阵

**承认**：当前验收章节是"功能/性能/兼容性"三轴，没有按 Phase 拆分。违反天命人的两层验收铁律。

**修正方案**：新增 §六.0 "Phase 门禁验收矩阵"：

| Phase | 门禁验收（当前环境可执行） | 阻塞项 |
|-------|--------------------------|--------|
| P1 | 编译通过 + Inspector 显示 AimMode/IsNormalAttack 字段 | 字段缺失 |
| P2 | 已有技能行为不变（AimMode 默认=AutoAim）；新建临时 SO 设 FixedForward → 方向正确 | AimMode 分派错误 |
| P3 | OverrideSlotCooldown 后 GetEffectiveCooldown 返回 override 值 | API 不生效 |
| P4 | 火力全开 Buff 激活后 Slot[0] CD 消耗加速 + BulletCount 增加 | Buff 路径断裂 |
| P5 | SO Inspector 配置完整，无 Missing Reference | 资产损坏 |
| P6 | PlayTest 射击行为与改造前一致（射速/方向/弹幕数量） | G5 回归 |
| P7 | 零 AttackComponent.Init() 调用（加断点验证） | 残留实例 |
| P8 | SkillCDPanel 只显示 Slot[1~N] | UI 显示普攻 CD |
| P9 | Editor Custom Inspector 正常 | Editor 报错 |
| P10 | 全链路 5 关 PlayTest + Profiler Deep Profile 0 Alloc | 性能回归 |
| P11 | 文档一致性检查 | — |

全局集成验收（真机+微信开发者工具+性能极限）保留为 §6.4。

**→ 回写文档 §六**

---

### 对 UA-003（🟡 TickSlot 无 slotIndex）— ✅ 接受，明确方案

**承认**：`TickSlot(ref SkillSlot slot, float dt)` 确实没有 slotIndex。

**修正方案**：改为 `TickSlot(int slotIndex, float dt)`，内部用 `ref _slots[slotIndex]`：
```csharp
private void TickSlot(int slotIndex, float dt)
{
    ref var slot = ref _slots[slotIndex];
    // ... 原逻辑 ...
    case SkillState.Recovery:
        slot.CastTimer -= dt;
        if (slot.CastTimer <= 0)
        {
            slot.CooldownTimer = GetEffectiveCooldown(slotIndex);
            slot.State = SkillState.Cooldown;
        }
        break;
}
```
**Tick 循环**也相应改为 `for (int i = 0; i < ActiveSlotCount; i++) TickSlot(i, dt);`（当前已是 index 循环）。

**→ 回写文档 §2.7**

---

### 对 UA-004（🟡 数组拼接 GC）— ✅ 接受，补充具体实现

**修正方案**：
```csharp
// BattleController 中
private void SetupPlayerSkills()
{
    var skillComp = _playerEntity.GetComponent(ComponentType.Skill) as SkillComponent;
    if (skillComp == null) return;

    var equipped = _battleLevelData?.EquippedSkills;
    int equipCount = equipped?.Length ?? 0;

    // 预分配固定大小数组（MAX_SLOTS=6），避免 GC
    var allSkills = new SkillConfigSO[Mathf.Min(1 + equipCount, SkillComponent.MAX_SLOTS)];
    allSkills[0] = _battleLevelData.NormalAttackConfig; // Slot[0] = 普攻
    for (int i = 0; i < equipCount && i + 1 < allSkills.Length; i++)
        allSkills[i + 1] = equipped[i];

    skillComp.InitWithEquipment(allSkills, staggerOffsetPerSlot: 0.5f);
    skillComp.OverrideSlotCooldown(0, _playerEntityConfig.AttackInterval);
}
```
- 数组分配发生在初始化阶段（非热路径），GC 可接受。
- 同时被 Retry 流程复用（解决 UA-007）。

**→ 回写文档 §2.8**

---

### 对 UA-005（🟡 Reset 未清零 override）— ✅ 接受

**修正方案**：在 `InitWithEquipment` 开头增加：
```csharp
System.Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS);
```

**→ 回写文档 §2.7**

---

### 对 UA-006（🟡 EntityConfigSO 仍含 Attack）— ✅ 接受，明确迁移策略

**修正方案**：P7 实施时：
1. `CreateComponent(ComponentType.Attack)` 改为返回 `null` + `Debug.LogWarning("[Migration] Attack component skipped")`
2. 所有 EntityConfigSO 的 Components 数组中移除 `ComponentType.Attack`（通过 Editor 脚本批量处理）
3. 不删除 AttackComponent.cs 文件——保留 [Obsolete] 标记

**→ 回写文档 §2.9**

---

### 对 UA-007（🟡 Retry 流程遗漏）— ✅ 接受

已在 UA-004 回应中通过抽取 `SetupPlayerSkills()` 公共方法解决。首次初始化和 Retry 均调用此方法。

**→ 回写文档 §2.8**

---

### 对 UA-008（🟢 SkillCDPanel 硬编码）— 部分接受

**接受建议思路**：改为 `if (slot.Config != null && slot.Config.IsNormalAttack) continue;` 替代硬编码 `i+1`。
**但**：当前只有 1 个普攻且固定在 Slot[0]，硬编码 OK for now。标记为 TODO，编码期迭代。

**→ 回写文档 §2.11 加注释**

---

### 对 UA-009（🟢→🟡 升级！AimMode.FixedForward 方向计算错误）— ✅ 接受，严重度升级

**关键发现**：经验证当前 AttackComponent.GetFireAngle 实际读取的是 `command.AimDirection`（ControlComponent 提供），而 BattleController SpawnPlayer 时设置 `ctrl.SetAimInput(Vector2.up)`。所以当前普攻方向 = Vector2.up = 90° 向上。

但 TDD §2.3 FixedForward 用 `_owner.Rotation` = 270°（Spawn 时的角度），算出 Cos(270°)=0, Sin(270°)=-1 = **朝下**！**这是零回归目标的直接威胁。**

**修正方案**：FixedForward 应该读取 DecisionCommand.AimDirection（与当前 AttackComponent 一致），而非 Entity.Rotation：
```csharp
case AimMode.FixedForward:
    // 从 DecisionMaker 读取方向（ControlComponent 在 Init 时已设为 Vector2.up）
    if (_cachedDecisionMaker != null)
    {
        Vector2 fwdDir = _cachedDecisionMaker.GetDecision().AimDirection;
        if (fwdDir.sqrMagnitude > 0.01f)
            return fwdDir.normalized;
    }
    // 兜底：Entity 朝向（但对纵版飞机不适用，理论上不会走到这里）
    float rad = _owner.Rotation * Mathf.Deg2Rad;
    return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
```

**或者更简洁**：鉴于纵版射击固定向上，FixedForward 直接 = `Vector2.up`？ ——不，这样丧失了通用性。保留读 DecisionMaker 方案。

**严重度升级为 🟡中**——不修会导致 G5 回归。

**→ 回写文档 §2.3**

---

### 对 UA-010（🟢 FirstAttackDelay）— ✅ 接受，确认需要兼容

**验证**：当前 AttackComponent.Init 中 `_timer = 0f`，而 `effectiveInterval` = AttackInterval（默认 0.25s）。第一次 Tick 时 timer=0 < interval → 不射。需要等 0.25s。

MEMORY 中记录 `FirstAttackDelay 默认 1.0s` — 这应该是指 SkillComponent 的 stagger（0.5s * index），但对 AttackComponent 来说首发延迟就是 1 个 interval（0.25s）。

改造后 Slot[0] CooldownTimer = stagger * 0 = 0 → 立即进入 Idle → 立即射击。差异 = 少了 0.25s 的首发等待。

**修正方案**：`OverrideSlotCooldown(0, attackInterval)` 之后，设置 `_slots[0].CooldownTimer = attackInterval`（模拟首个 CD 周期）。或者在 `InitWithEquipment` 中 Slot[0] 初始 state 就是 Cooldown + Timer = attackInterval。

实际上 `InitWithEquipment` 已经将 `slot.State = SkillState.Cooldown; slot.CooldownTimer = stagger * 0 = 0`。CooldownTimer=0 会在下一帧立即转 Idle。需要改为：
```csharp
// SetupPlayerSkills 末尾
_slots[0].CooldownTimer = attackInterval; // 首发延迟 = 1 个 CD 周期
```

**→ 回写文档 §2.8 补充**

---

### 对 UA-011（🟢 SO 序列化安全）— ✅ 确认安全

`GetEffectiveCooldown` 只读取 `_runtimeCooldownOverrides[i]` 或 `Config.CooldownTime`，不写回 SO。已在代码注释中明确。无需额外改动。

---

**守方小结**：
- 全部 11 个问题均接受或部分接受
- UA-009 严重度从 🟢低升级为 🟡中（方向计算错误会导致回归）
- 0 个问题被驳回

---

## Round 2 — 攻方（Unity 架构师）追问

### UA-012 | 🟡中 | FireBullets 重载方案中 countOverride 与 Difficulty 乘数的叠加顺序

**涉及章节**：守方对 UA-001 的修正方案
**质疑**：守方方案在 `FireBulletsEffect.Execute` 中计算 `countOverride = Pattern.Count * BulletCountModifier`，然后传给 `BulletSpawner.Fire`。但 `BulletSpawner.Fire` 内部已有 `count = Mathf.RoundToInt(count * difficulty.CountMultiplier)`。如果传入 countOverride 替代 `pattern.Count`，难度乘数还要不要再应用一次？

**潜在风险**：双重乘法导致弹幕数爆炸，或者遗漏难度乘数导致高难度模式 Buff 失效。

**建议方向**：明确 countOverride 的语义——是"已包含 Buff 修正但不含难度乘数"（Fire 内部继续乘难度），还是"最终值"（Fire 跳过难度乘数）。

---

### UA-013 | 🟡中 | UA-009 修正后 FixedForward 与当前 GetFireAngle 语义差异

**涉及章节**：守方对 UA-009 的修正方案
**质疑**：守方选择让 FixedForward 读 `_cachedDecisionMaker.GetDecision().AimDirection`。但当前 AttackComponent.GetFireAngle 是**每次 Tick 都读最新的 Decision**。而 SkillComponent 的 `_cachedDecisionMaker` 在 Init 时缓存。两者等价吗？

具体问题：如果 ControlComponent 被动态替换（虽然当前不会），缓存的 DecisionMaker 会失效。更重要的是——如果玩家在 Joystick 操控时改变了 AimDirection，FixedForward 就不再是"固定正前方"了，而是跟随玩家瞄准方向。这和名称"FixedForward"语义矛盾。

**建议方向**：考虑 FixedForward 的真正含义：
- 方案 A：永远 `Vector2.up`（纵版专用，最简单，但失去通用性）
- 方案 B：读 Init 时缓存的方向（真正"固定"）
- 方案 C：读每帧 DecisionMaker（和当前行为一致，但名字应该叫 DecisionDir 而非 FixedForward）

---

### UA-014 | 🟢低 | SetupPlayerSkills 中 NormalAttackConfig 为 null 的防御

**涉及章节**：守方对 UA-004 的修正方案
**质疑**：`allSkills[0] = _battleLevelData.NormalAttackConfig` — 如果 `NormalAttackConfig` 未配置（null），后续 `SkillComponent.InitWithEquipment` 中 `if (equippedSkills[i] == null) continue;` 会跳过 Slot[0]，导致没有普攻。

**建议方向**：加 null check + `Debug.LogError`，确保配置缺失时有明确告警而非静默失效。

---

**攻方 Round 2 小结**：
- 新增 1 个 🟡中问题（叠加顺序）
- 1 个 🟡中追问（FixedForward 语义辩论）
- 1 个 🟢低补充
- 其余 Round 1 问题的守方回应**均满意**，不再追问

---

## Round 2 — 守方（软件架构师）回应

### 对 UA-012（countOverride 叠加顺序）— ✅ 接受，明确语义

**修正方案**：`countOverride` 语义定义为"**Buff 修正后的基础数量（不含难度乘数）**"。

```csharp
// FireBulletsEffect.Execute
int baseCount = Pattern.Count;
var buffComp = ctx.Caster.GetComponent(ComponentType.Buff) as BuffComponent;
if (buffComp != null && buffComp.BulletCountModifier != 1f)
    baseCount = Mathf.RoundToInt(baseCount * buffComp.BulletCountModifier);

// 传给 Fire，Fire 内部继续乘 difficulty.CountMultiplier
ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value, ctx.SourceTagId, baseCount);
```

`BulletSpawner.Fire` 中改为：
```csharp
int count = countOverride ?? pattern.Count;
if (difficulty != null)
    count = Mathf.RoundToInt(count * difficulty.CountMultiplier);
```

**叠加顺序**：Buff → 难度 → 最终值。层次清晰，互不干扰。

**→ 回写文档 §2.5**

---

### 对 UA-013（FixedForward 语义）— ✅ 接受，选择方案 A+B 混合

**分析三个方案**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| A. 永远 Vector2.up | 最简单、零 bug | 非纵版游戏不适用 |
| B. Init 时缓存方向 | 真正"固定" | 需新增字段，若 Init 前方向未设置则缓存错误 |
| C. 每帧读 Decision | 与当前行为一致 | 名不副实——跟随玩家操控 ≠ "固定" |

**决策**：选 **方案 A（项目特化）+ 配置化兜底**。

理由：
1. 本项目是纵版射击，普攻方向永远是 Vector2.up（90°）。
2. 当前 AttackComponent 的"固定正前方"本质上也是通过 `SetAimInput(Vector2.up)` 实现的——实质就是方案 A。
3. 如果未来需要不同方向（如横版），在 SkillConfigSO 上新增可选的 `FixedDirection` 字段即可扩展。

```csharp
case AimMode.FixedForward:
    // 纵版射击固定向上（与当前 AttackComponent 行为等价）
    // 未来支持其他方向时可从 SkillConfigSO.FixedDirection 读取
    return Vector2.up;
```

**命名不改**——"FixedForward"的语义是"固定方向射击（不跟踪目标）"，具体方向由配置决定，当前配置 = 向上。

**→ 回写文档 §2.3**

---

### 对 UA-014（NormalAttackConfig null 防御）— ✅ 接受

```csharp
if (_battleLevelData.NormalAttackConfig == null)
{
    Debug.LogError("[BattleController] NormalAttackConfig 未配置！普攻将不可用。");
    return;
}
```

**→ 回写文档 §2.8**

---

**守方 Round 2 小结**：
- 3 个问题全部接受
- 0 个驳回
- FixedForward 采用项目特化方案 A，简洁且与当前行为等价

---

## 收敛判定

| 维度 | 状态 |
|------|------|
| 🔴高 问题全部解决 | ✅（UA-001 补充实施方案，UA-002 新增门禁矩阵） |
| 🟡中 问题全部解决 | ✅（UA-003~007 + UA-009升级 + UA-012~013 均已给出具体方案） |
| 🟢低 问题无分歧 | ✅（接受或标记编码期 TODO） |
| 攻方无新 🔴高 追问 | ✅（Round 2 仅 🟡中和🟢低） |
| 连续两轮无 🔴高 | ✅ |

**✅ 收敛达成 — PK 结束（2 轮）**

---

## PK 最终统计

| 指标 | 数值 |
|------|------|
| 总轮次 | 2 |
| 攻方问题总数 | 14（UA-001 ~ UA-014） |
| 🔴高 | 2 → 全部解决 |
| 🟡中 | 5 + 1升级 + 2追问 = 8 → 全部解决 |
| 🟢低 | 4 → 全部接受/标记 |
| 守方驳回 | 0 |
| 文档需回写修改点 | 9 处 |

---

## 需回写到 TDD 文档的修改清单

| # | 章节 | 修改内容 |
|---|------|---------|
| 1 | §2.3 | FixedForward 改为直接返回 `Vector2.up`，加注释说明项目特化 |
| 2 | §2.5 | 补充完整 BulletCountModifier 实施方案（含 countOverride 语义定义） |
| 3 | §2.7 | TickSlot 增加 slotIndex 参数 + Reset 清零 _runtimeCooldownOverrides |
| 4 | §2.8 | 抽取 SetupPlayerSkills()，含数组拼接代码 + Retry 复用 + null 防御 + 首发延迟 |
| 5 | §2.9 | 明确 EntityConfigSO.Components 移除 Attack + CreateComponent 返回 null |
| 6 | §2.11 | 加注释：未来可用 IsNormalAttack 替代硬编码 index |
| 7 | §五 | 新增 R6: FixedForward=Vector2.up 项目特化 |
| 8 | §六 | 新增 §6.0 Phase 门禁验收矩阵 + 重命名现有验收为全局集成 |
| 9 | §四 | 接口变更补充 BulletSpawner.Fire countOverride 重载 |

> **PK 状态**：✅ 已收敛
> **结束时间**：2026-05-25 14:30





