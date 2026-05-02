# Phase 3A TDD v0.3 — PK 评审（Round 3）

> **攻方角色**：🎮 游戏设计师 Agent（关注：玩家体验、游戏循环完整性、风险/回报设计、手感调优杠杆、可扩展玩法空间）
> **守方角色**：🏗️ Unity 架构师 Agent（关注：架构一致性、性能约束、扩展路径、设计原则、零 GC）
> **最大轮次**：6 轮
> **PK 状态**：🔄 进行中
> **TDD 版本**：v0.3（基于 PK Round 1+2 收敛版本）

---

## Round 1 · 攻方发难

> 🎮 **攻方（游戏设计师）**：

我从**玩家体验**和**游戏循环设计**的角度审读了整个 TDD。技术实现看起来稳固，但我关注的是：这些系统组合起来，能否产生**有趣的玩家决策**和**令人满意的游戏手感**。

---

### GD-001：玩家移动边界触边无反馈——硬夹 vs 柔性约束

**设计问题**：当前方案是纯粹的 `Mathf.Clamp`——玩家推向边界时，角色**突然停住**，没有任何反馈信号。

**玩家感受分析**：
- 在弹幕射击中，玩家操作频率极高（手指持续滑动），**突然被无声截停**会产生"我的操作断了/游戏卡了"的错觉
- 对比经典弹幕射击（雷电、东方 Project）：要么有屏幕边缘闪烁/粒子效果，要么有轻微减速缓冲区
- 无反馈 = 玩家不知道边界在哪 → 会反复撞边，降低操控信心

**设计建议**（三选一，或组合）：

| 方案 | 描述 | 实现复杂度 |
|------|------|-----------|
| A. 柔性边缘 | 边界外 0.5 单位软区域，移速衰减到 0 | 中（需额外参数） |
| B. 视觉反馈 | 触边时屏幕边缘短暂闪光/Entity 闪白 | 低（View 层事件） |
| C. 振动反馈 | 触边时手机振动（微信小游戏 wx.vibrateShort） | 低（一行 jslib） |

**关键问题**：TDD 的设计支柱 3 是"最小可用"，但触边反馈是**基础手感**，不是花哨特性。是否应该至少在 TDD 中**预留接口/事件点**（`OnPlayerHitBounds`），即使 Phase 3A 不实现反馈效果？

---

### GD-002：AutoAim 搜索间隔 0.2s 在弹幕射击中可能导致"追不上弹幕"

**设计问题**：AutoAim 每 0.2s 搜索一次目标。但在典型弹幕射击中：

- 弹幕发射间隔（`AttackInterval`）通常 0.1~0.3s
- 如果 AutoAim 搜索间隔 = 0.2s，Attack 发射间隔 = 0.15s——那么在两次搜索之间，弹幕可能连续发射 1~2 发**朝向旧目标方向**
- 如果旧目标已死亡（被上一发弹幕击杀），下一发弹幕朝着尸体/空位发射 → 浪费

**手感分析**：这不是 bug，但影响"满意感"：
- 弹幕射击的满意感来自**精准打击** + **每发有效**
- "朝死目标发射"是"白打了"的感受
- 但过于灵敏的瞄准（每帧更新）又会导致弹幕扫射而非锁定感

**设计问题**：
1. AutoAimSearchInterval 的默认值 0.2s 是否经过**手感预判**？还是随便填的？
2. 更重要的是：**AutoAim 在目标死亡时是否需要立即清除 + 下帧搜索**？TDD 说"每帧检查目标有效性"（`_currentTarget.IsAlive`），但清除后要等下一次定频搜索才重新锁定——这中间有最多 0.2s 的"无目标"窗口。
3. **设计杠杆**：是否应新增一个 `InstantRetargetOnKill` 布尔配置——当锁定目标死亡时立即触发一次额外搜索？

---

### GD-003：SkillComponent 只有一个技能槽——未来多技能扩展的设计约束

**设计问题**：当前 SkillComponent 是"单技能槽"设计——一个 Entity 只能配一个 Skill。

**从游戏设计角度看**：
- 对小兵/杂兵：单技能足够
- 对 Boss/精英怪：几乎必然需要**多技能轮转**（Phase 1 攻击 → Phase 2 切技能 → 怒气技）
- 对玩家角色：如果未来支持"捡装备获得新技能"，也需要多槽

**我不要求 Phase 3A 做多技能**，但想确认架构层面的扩展路径是否清晰：
1. 方案 A：一个 Entity 挂多个 SkillComponent 实例（ComponentType 不允许——每种只能有一个）
2. 方案 B：SkillComponent 内部管理 `SkillConfigSO[]` 数组 + 激活索引
3. 方案 C：高级 Entity 不用 SkillComponent，用 FSM 状态机（Phase 4）

**问题**：TDD §3.3.1 说"单技能槽"，§十 说 Phase 4 有 FSM。但**中间状态呢**？如果 Phase 3B 需要一个 Boss 有两个技能（比如近身 AOE + 远程弹幕切换），当前架构是否支撑？还是必须等到 Phase 4 FSM？

---

### GD-004：Buff 系统的乘法叠加在极端情况下会"杀死手感"

**设计问题**：多个 Buff 的修正器**相乘**：`finalSpeed = baseSpeed × mod1 × mod2 × mod3`

**风险场景**：
- 3 个减速 Buff（0.7 × 0.7 × 0.7 = 0.343）→ 玩家速度降到原来的 34%
- 这在弹幕射击中 = **判死刑**（无法躲避弹幕 → 玩家愤怒退出）
- TDD §九"风险"中提到了"加 MinSpeed Clamp"，但没有给出**具体数值**

**游戏设计必答题**：
1. **MinSpeed 应该是多少？** 对弹幕射击来说，低于 40% 基础速度的移动是"不可操控"的。建议 MinSpeedRatio = 0.4（至少能走）
2. **是否需要 MaxSpeed 上限？** 3 个加速 Buff（1.5 × 1.5 × 1.5 = 3.375）→ 移速变 3 倍 → 无法精确控制角色，同样影响手感
3. **Clamp 在哪一层做？** 在 BuffComponent.RecalcModifiers 中 Clamp（影响所有消费者），还是在 MovementComponent.GetFinalSpeed 中 Clamp（只影响移动）？

**设计建议**：
```
MinSpeedRatio = 0.4f  // 永远不低于 40% 基础速度
MaxSpeedRatio = 2.5f  // 永远不超过 250% 基础速度
// 在 RecalcModifiers 中：
MoveSpeedModifier = Mathf.Clamp(move, MinSpeedRatio, MaxSpeedRatio);
```

---

### GD-005：SkillComponent 的 CooldownTime 最小值 0.1s——无法做"无 CD"技能

**设计问题**：`SkillConfigSO.CooldownTime` 标注了 `[Min(0.1f)]`，最小值 0.1 秒。

**但有些合法设计需要 CD = 0**：
- "蓄力攻击"：长前摇 + 无 CD（前摇本身就是限制，不需要额外 CD）
- "弹药制"：通过弹药数量限制而非冷却限制
- "连招"：技能 A 释放后立即解锁技能 B，B 的 CD = 0

**如果 CD = 0，当前代码会发生什么？**
```csharp
case SkillState.Cooldown:
    CooldownRemaining -= dt;  // 0 - dt = 负数
    if (CooldownRemaining <= 0)  // 立即满足
    {
        CooldownRemaining = 0;
        CurrentState = SkillState.Idle;
    }
    // 下一帧又 ShouldTrigger() = true → 立即再次触发
```

CD = 0 + Auto 模式 = **每帧都触发**。如果有 RecoveryTime > 0 还好（Recovery 限制了频率），但如果 Recovery 也 = 0，就是**每帧触发效果**——可能导致性能爆炸（每帧 AOE？每帧发射弹幕？）

**设计建议**：
1. 允许 `[Min(0f)]`（解除 0.1f 下限）
2. 但增加**安全网**：如果 CD=0 且 Recovery=0，至少间隔 1 帧（或发出 LogWarning）
3. 或者：在 TDD 文档中明确"CD=0 + Recovery=0 是非法配置，由策划避免"

---

### GD-006：Buff 持续时间为 0 = "永久" 的设计语义是否清晰？

**设计问题**：`BuffConfigSO.Duration` 标注 `[Min(0f)]` + Tooltip "0=永久，需手动移除"。

**作为游戏设计师，我担心两个问题**：

1. **策划认知负担**：在大多数系统中，"0"意味着"无效果"或"不做"。让 0 = 永久是**反直觉**的。是否应改为 `-1 = 永久`（更常见的约定），`0 = 瞬时（施加后立即移除 = 单帧效果）`？

2. **永久 Buff 不被 Tick 减时间**（`if (_slots[i].Duration <= 0f) continue;`），这很好。但如果 Entity 回池 → Reset 清除所有 Buff → Entity 被复用 → 永久 Buff 消失了。这对"复用的 Entity"来说是正确行为（新一轮新状态），但如果是**玩家角色**（不会被回池），永久 Buff 是安全的。

**确认**：文档中是否需要明确"永久 Buff 仅适用于不会被回池的 Entity（如玩家角色）"的约束？

---

### GD-007：整个 Phase 3A 缺少"伤害数字/打击反馈"的设计接口

**设计问题**：Phase 3A 增加了 DamageDealer + SkillComponent + BuffComponent——Entity 现在能相互造成伤害了。但没有任何关于**伤害发生后的反馈系统**接口。

**弹幕射击的核心满意感 = 打击反馈**：
- 伤害数字弹出（FloatingText）
- 受击闪白/帧冻结（HitStop）
- 击杀特效/音效
- 屏幕震动（ScreenShake）

我理解这些是 View 层的事情，不属于 Entity-Component 的逻辑层。但当前 TDD 的 `HealthComponent.TakeDamage` 管线中是否有**事件/回调点**供 View 层注入？

**查看 Phase 1 TDD 的记忆**：Phase 2 验收中提到了 `EntityHitReactionHandler 受击管线（闪白+击退+伤害数字+死亡延迟）`——这是 Phase 2 已完成的？

**问题**：如果 Phase 2 已有受击反馈管线，那 Phase 3A 新增的 `DamageDealer.DealDamageToEntity/DealAreaDamage` 路径是否自动走同一管线？还是绕过了？

TDD §3.2.2 说"走完整 TakeDamage 管线（IDamageModifier 链）"——这是否包含受击反馈事件？**请明确确认**，因为如果 DamageDealer 绕过反馈系统，技能伤害就会是"无声的伤害"——没有数字、没有闪白、没有满足感。

---

### GD-008：SkillTriggerMode 只有 Manual 和 Auto——缺少"条件触发"维度

**设计问题**：Phase 3A 的技能只有两种触发模式：
- **Auto**：CD 就绪即触发
- **Manual**：玩家按键/AI 决策触发

**但弹幕射击 + 生存类游戏中，更常见的触发模式是"条件触发"**：
- "HP 低于 30% 时自动触发护盾技能"
- "范围内敌人 ≥ 5 时自动触发 AOE"
- "连续击杀 3 次后自动触发终极技"

Phase 3A 需要做这个吗？不需要。但设计层面，**扩展路径是否清晰？**

**可能的方案**：
- Phase 4 新增 `SkillTriggerMode.Conditional`
- `SkillConfigSO` 新增 `ISkillTriggerCondition[] Conditions`（类似 ISkillEffect 的策略模式）
- `ShouldTrigger()` 检查所有 Condition 是否满足

**我不要求 Phase 3A 实现这个**，但 TDD §十"未决项"中是否应新增一条记录这个设计方向？当前只列了 Phase 4 的 FSM 和 WantsSkill，没有提到条件触发。

---

以上 8 个问题，请守方逐一回应。

---

## Round 1 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

从架构层面逐一回应，优先确认哪些是"基础手感"必须做、哪些是"有价值但延后"：

---

### RE: GD-001 触边无反馈 — ⚠️ 部分接受（预留事件点，不实现效果）

**结论**：攻方说得对——硬夹无反馈是手感缺陷。但三个方案的实现成本和 Phase 3A 的"最小可用"原则有冲突。

**架构分析**：

触边反馈是**View 层**职责，不属于 Entity 逻辑层。正确的做法是在逻辑层产出事件，由 View 层消费：

```csharp
// ClampPlayerPositions() 修正
private void ClampPlayerPositions()
{
    // ... existing code ...
    var clampedPos = new Vector2(
        Mathf.Clamp(pos.x, bounds.xMin, bounds.xMax),
        Mathf.Clamp(pos.y, bounds.yMin, bounds.yMax));
    
    // GD-001：触边事件（预留接口）
    if (clampedPos != pos)
    {
        entity.Position = clampedPos;
        OnPlayerHitBounds?.Invoke(entity, pos, clampedPos); // 静态事件
    }
    else
    {
        entity.Position = pos; // 无变化，不触发
    }
}

// 事件定义（Bootstrap 或独立事件类）
public static event System.Action<Entity, Vector2, Vector2> OnPlayerHitBounds;
```

**Phase 3A 范围**：
- ✅ 预留 `OnPlayerHitBounds` 事件点（零成本，一行代码）
- ❌ 不实现柔性边缘（需修改 Movement 管线，成本中等）
- ❌ 不实现视觉/振动反馈（View 层，Phase 3B 的打磨项）

**影响**：§3.0.2 代码修改（+5 行），§十 未决项新增"边界触碰反馈效果"。

**设计师期望管理**：Phase 3A 能做到的是"架构上不挡路"——策划在 Phase 3B 订阅 `OnPlayerHitBounds` 即可加入任何反馈效果，零重构。

---

### RE: GD-002 AutoAim 搜索间隔 0.2s 与目标死亡重瞄延迟 — ✅ 接受（InstantRetarget 配置）

**结论**：这是一个真正的手感问题。攻方的分析准确——"朝死目标发射"破坏满意感。

**设计分析**：
- 0.2s 搜索间隔是基于"省 CPU"的工程考量——20 个 Entity 每帧搜索 = 20 × O(N) ≈ 400 次比较/帧
- 但"目标死亡后立即重瞄"的成本 = **一次额外搜索/事件**（不是每帧），完全在预算内

**修正方案**：

```csharp
// AutoAimComponent.Tick — 目标失效时立即重搜
if (_currentTarget != null && (!_currentTarget.IsAlive || _currentTarget.IsPendingDespawn))
{
    _currentTarget = null;
    // GD-002：目标死亡时立即重搜（不等下一次定频搜索）
    SearchTarget();
}
```

**不新增配置项**（`InstantRetargetOnKill`）：这个行为应该是**默认行为**而非可选项。没有任何设计场景下"锁定目标死了还继续朝它射击"是合理的。简化为：**目标失效 = 立即重搜**，不需要策划开关。

**关于 0.2s 默认值**：
- 这是一个**调整杠杆**，TDD 已通过 `AutoAimSearchInterval` 暴露给策划
- 0.2s 是"可接受起点"（每秒 5 次搜索），策划可根据手感调到 0.1s（更灵敏）或 0.3s（更锁定感）
- **真正的手感验证需要 gameplay 测试**——TDD 只能给出"合理默认值"
- 在 TDD 默认值注释中标注：`[占位符] 0.2s — 需 gameplay 测试调整`

**影响**：§3.1.4 Tick 代码修改（目标死亡时立即搜索），变更量 +3 行。

---

### RE: GD-003 单技能槽 vs 多技能扩展路径 — ✅ 接受（明确扩展路径文档）

**结论**：攻方问的不是"现在做多技能"而是"路怎么走"。这个问题值得明确回答。

**架构层面扩展路径分析**：

| 方案 | 可行性 | 适用场景 | Phase 3A 影响 |
|------|--------|---------|--------------|
| A. 多 SkillComponent 实例 | ❌ 不可行 | — | ComponentType 机制限制每种一个实例 |
| B. SkillComponent 内部数组 | ✅ 可行 | Boss 2~3 技能轮转 | Phase 3B 可平滑扩展 |
| C. FSM 替代 | ✅ 可行 | 复杂 Boss 多阶段 | Phase 4 |

**Phase 3B 扩展路径（方案 B 设计预告）**：
```csharp
// Phase 3B 可选扩展（不影响 Phase 3A 接口）
public class SkillComponent
{
    // Phase 3A：单技能
    private SkillConfigSO _config;
    
    // Phase 3B 扩展方向：
    // private SkillConfigSO[] _configs;  // 技能列表
    // private int _activeIndex;          // 当前激活索引
    // public void SwitchSkill(int index) // 切换接口
}
```

**关键保证**：Phase 3A 的 `SkillComponent` 外部接口（Init/Reset/Tick/CurrentState/CooldownRemaining）在方案 B 扩展后**完全不变**。消费者（AI Action / 触发系统）不需要知道内部是单技能还是多技能。

**修正**：TDD §十"未决项"新增：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 9 | SkillComponent 多技能槽扩展（configs[] + activeIndex + SwitchSkill） | GD-003 | Phase 3B |

---

### RE: GD-004 Buff 乘法叠加极端值 — ✅ 接受（MinSpeed/MaxSpeed Clamp）

**结论**：攻方识别了一个真正的"手感杀手"。乘法叠加无上下界 = 失控。

**修正**：在 BuffComponent.RecalcModifiers 中添加 Clamp：

```csharp
// BuffComponent.RecalcModifiers() — GD-004 修正
private const float MIN_MOVE_SPEED_RATIO = 0.4f;  // 最低 40% 速度
private const float MAX_MOVE_SPEED_RATIO = 2.5f;  // 最高 250% 速度
private const float MIN_ATTACK_INTERVAL_RATIO = 0.3f;  // 最快 3.3 倍攻速
private const float MAX_ATTACK_INTERVAL_RATIO = 3.0f;  // 最慢 3 倍攻击间隔

private void RecalcModifiers()
{
    float move = 1f, attack = 1f, damage = 1f;
    for (int i = 0; i < _activeCount; i++)
    {
        move *= _slots[i].MoveSpeedMod;
        attack *= _slots[i].AttackIntervalMod;
        damage *= _slots[i].DamageTakenMod;
    }
    // GD-004：Clamp 极端值，保证手感底线
    MoveSpeedModifier = Mathf.Clamp(move, MIN_MOVE_SPEED_RATIO, MAX_MOVE_SPEED_RATIO);
    AttackIntervalModifier = Mathf.Clamp(attack, MIN_ATTACK_INTERVAL_RATIO, MAX_ATTACK_INTERVAL_RATIO);
    DamageTakenModifier = damage; // 伤害倍率不 Clamp——设计上允许"无敌(0)"和"脆弱(×5)"
}
```

**Clamp 在 BuffComponent 层的理由**：
- 如果在 MovementComponent 中 Clamp，Buff 系统无法知道"我的修正被截断了"→ 调试困难
- 在 Buff 层 Clamp = "产出的修正值已经是安全的"→ 所有消费者直接使用，无需各自防御

**DamageTakenModifier 不 Clamp 的理由**：
- 无敌效果（DamageTaken = 0）是合法设计需求（重生无敌、Boss 免疫阶段）
- 脆弱效果（DamageTaken × 5）是合法 Debuff
- 如果未来需要 Clamp 伤害倍率，策划通过 Buff 配置本身控制（不给超过 ×3 的 Buff）

**影响**：§3.4.3 RecalcModifiers 代码修改，新增 4 个常量。标注为 `[占位符]` 待 gameplay 测试调整。

---

### RE: GD-005 CooldownTime [Min(0.1f)] 无法做无 CD 技能 — ✅ 接受（放开为 [Min(0f)] + 安全网）

**结论**：攻方的"蓄力攻击"和"弹药制"场景是合理的。0.1f 下限过于武断。

**修正**：

```csharp
[Tooltip("冷却时间（秒，0=无冷却，受 Recovery 限制最小间隔）")]
[Min(0f)]  // GD-005：放开 0.1f 下限
public float CooldownTime = 5f;
```

**安全网设计**：

```csharp
// SkillComponent.EnterRecovery — GD-005 安全网
private void EnterRecovery()
{
    if (_config.RecoveryTime > 0)
    {
        CurrentState = SkillState.Recovery;
        _stateTimer = _config.RecoveryTime;
    }
    else if (_config.CooldownTime > 0)
    {
        // Recovery=0 但有 CD → 直接进 CD
        CooldownRemaining = _config.CooldownTime;
        CurrentState = SkillState.Cooldown;
    }
    else
    {
        // GD-005 安全网：Recovery=0 + CD=0 → 强制 1 帧间隔
        // 不使用 Idle（否则 Auto 模式同帧再触发），改为最短 Cooldown
        CooldownRemaining = 0.001f; // 小于任何合理 dt，下帧必定满足 <=0
        CurrentState = SkillState.Cooldown;
        // 效果：相当于"下一帧才能再次触发"
        Debug.LogWarning($"[SkillComponent] {_config.DisplayName} CD=0 + Recovery=0，已强制最小间隔。建议至少配一个 > 0。");
    }
}
```

**这个安全网保证**：即使策划配错（CD=0 + Recovery=0 + Auto），最坏情况也只是每帧触发一次（而非无限循环/卡死），并且有 LogWarning 提醒。

**影响**：§3.3.2 `[Min(0.1f)]` → `[Min(0f)]`，§3.3.5 EnterRecovery 修正。

---

### RE: GD-006 Duration=0 语义不直觉 — ⚠️ 部分接受（保持 0=永久，补文档约束）

**结论**：这是个"约定"问题，两种约定各有利弊。

**保持 `0 = 永久` 的理由**：
- Unity 生态中"0=无限/永久"是常见约定（ParticleSystem.duration=0=loop、AudioSource.pitch=0 等）
- 如果改为 `-1 = 永久`，需要在 Inspector 中允许负数 → `[Min(-1f)]` → 策划可能误输入 -0.5f 等奇怪值
- `0 = 永久` 在 Buff 系统的代码路径中更简洁（`Duration <= 0f` 一个条件判断搞定）

**但攻方说对了一点**：文档不够清晰。

**修正**：
1. **Tooltip 改进**：`[Tooltip("持续秒数。0=永久Buff（不会自动过期，需通过 RemoveBuff 手动移除）")]`
2. **TDD 文档约束新增**：

> **Buff 持续时间约定（GD-006）**：
> - `Duration > 0`：持续 N 秒后自动移除
> - `Duration = 0`：永久 Buff，仅通过代码 `RemoveBuff(id)` 移除
> - 永久 Buff 在 Entity 回池时通过 Reset 清除（正确行为——Entity 复用 = 全新生命周期）
> - 适用于玩家角色的装备加成、被动光环等不应过期的效果
> - ⚠️ 对会被回池的 Entity（敌人/弹幕），永久 Buff 的意义有限——Entity 生命周期通常短于 Buff 意图

**不改为 -1 的原因**：运行时代码变更成本大于收益，且改后不一定更直觉（策划同样需要学习"-1 是什么意思"）。

**影响**：纯文档变更 + Tooltip 改善。

---

### RE: GD-007 DamageDealer 是否经过受击反馈管线 — ✅ 接受（确认并明确文档化）

**结论**：好问题。让我明确回答——

**Phase 2 已有的受击管线**：`EntityHitReactionHandler` 注册了 `HealthComponent.OnTakeDamage` 事件，提供闪白+击退+伤害数字+死亡延迟。

**Phase 3A 的 DamageDealer 路径分析**：

```
DamageDealer.DealDamageToEntity(target, context)
  → target.GetComponent(Health) → HealthComponent
  → health.TakeDamage(ref context)
    → IDamageModifier 链处理
    → 扣血
    → 触发 OnTakeDamage 事件  ← ★ 这里
      → EntityHitReactionHandler 收到事件
        → 闪白 / 击退 / 伤害数字 / 死亡延迟
```

**确认**：DamageDealer **自动走完整管线**——因为它最终调用的是 `HealthComponent.TakeDamage`，而所有反馈都注册在 TakeDamage 的事件系统上。无论伤害来自弹幕碰撞还是 DamageDealer 直伤还是技能 AOE，都走同一管线。

**这不是巧合，是设计意图**：TakeDamage 是**唯一的伤害入口**，所有伤害源最终都汇聚到这一点。反馈系统只需订阅一个事件点。

**TDD 文档补充**：在 §3.2.1 设计决策新增一条明确说明：

> DamageDealer 的所有路径最终调用 `HealthComponent.TakeDamage`——Phase 2 已有的受击反馈管线（闪白/击退/伤害数字/死亡延迟）**自动生效**。无需额外集成。

**影响**：纯文档补充，确认设计意图。

---

### RE: GD-008 条件触发模式扩展路径 — ✅ 接受（记录到未决项）

**结论**：攻方识别了一个明确的 Phase 4 需求——"条件触发"是 Roguelike/生存类的核心机制。

**架构层面兼容性分析**：

当前 `ShouldTrigger()` 的 switch 结构**天然支持扩展**：

```csharp
// Phase 4 扩展预告
private bool ShouldTrigger()
{
    if (CooldownRemaining > 0) return false;
    return _config.TriggerMode switch
    {
        SkillTriggerMode.Auto => true,
        SkillTriggerMode.Manual => GetDecisionWantsSkill(),
        // Phase 4 新增：
        // SkillTriggerMode.Conditional => EvaluateConditions(),
        _ => false
    };
}
```

`ISkillTriggerCondition` 策略接口与 `ISkillEffect` 对称——设计模式一致，扩展路径清晰。

**修正**：TDD §十"未决项"新增：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 10 | SkillTriggerMode.Conditional + ISkillTriggerCondition 策略接口（HP阈值/范围内敌人数/连杀触发等） | GD-008 | Phase 4 |

**影响**：§十 未决项表新增一行。

---

## Round 1 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| GD-001 | ⚠️ 部分接受 | 预留 OnPlayerHitBounds 事件点，不实现反馈效果 |
| GD-002 | ✅ 接受 | 目标死亡立即重搜（默认行为），SearchInterval 标注 [占位符] |
| GD-003 | ✅ 接受 | §十 新增多技能槽扩展方向（Phase 3B 方案 B） |
| GD-004 | ✅ 接受 | RecalcModifiers 添加 Min/Max Clamp（速度/攻速） |
| GD-005 | ✅ 接受 | CooldownTime [Min(0f)] + CD=0&Recovery=0 安全网 |
| GD-006 | ⚠️ 部分接受 | 保持 0=永久约定，补文档+Tooltip |
| GD-007 | ✅ 接受 | 确认 DamageDealer 走完整 TakeDamage 管线（含反馈），补文档 |
| GD-008 | ✅ 接受 | §十 新增条件触发扩展方向（Phase 4） |

**8 个问题：6 个完全接受，2 个部分接受（无拒绝）。攻方是否有后续追问？**

---

## Round 2 · 攻方追问

> 🎮 **攻方（游戏设计师）**：

守方回应质量很高。我有以下追问和新发现：

---

### GD-009：GD-001 OnPlayerHitBounds 事件的触发频率——玩家贴边滑动时每帧触发？

**追问 GD-001**：守方预留了 `OnPlayerHitBounds` 事件。但考虑实际操作场景：

玩家贴着屏幕边缘横向滑动时，每帧 `clampedPos.y != pos.y`（被 Clamp 了 Y 轴），但 X 轴正常。这意味着事件**每帧都在触发**。

如果 View 层在事件中做振动/闪光/声音——**每帧触发 = 灾难**（连续振动 / 声音叠加 / 闪烁不停）。

**设计建议**：事件应有**冷却机制**或改为**状态变化**通知：
- 方案 A：事件改为 `OnPlayerEnterBounds` / `OnPlayerExitBounds`（状态边沿触发）
- 方案 B：事件带冷却（上次触发后 0.3s 内不重复触发）
- 方案 C：让 View 层自己做冷却（事件保持"纯数据推送"，不管频率）

哪个是架构上更干净的选择？

---

### GD-010：GD-004 中 DamageTakenModifier 不 Clamp 的风险——"无敌 + AOE 技能"循环

**追问 GD-004**：守方说 DamageTakenModifier 不 Clamp，允许 0（无敌）。

**但从游戏设计角度有一个经典陷阱**：如果玩家有"无敌 Buff"（DamageTaken=0）+ "反弹 AOE 技能"（受到伤害时对周围造伤），会出现什么？

回答自己：Phase 3A 没有"受伤反击"机制，这个问题暂不存在。

**但更实际的问题是**：如果 `DamageTakenModifier = 0`，`HealthComponent.TakeDamage` 计算的最终伤害 = `baseDamage × 0 = 0`。TakeDamage 是否会在**伤害为 0 时仍然触发 OnTakeDamage 事件**？

如果触发 → 闪白/击退/伤害数字"0" 每次受击都出现 → 合理的设计是"无敌时不触发受击反馈"
如果不触发 → 无敌时玩家没有"我被打了但没掉血"的视觉确认 → 可能也不对

**设计决策**：无敌状态下的受击反馈行为是什么？这不是 Phase 3A 要实现的，但 `DamageTakenModifier = 0` 通过 `IDamageModifier` 链路实现后，TakeDamage 的事件触发条件需要明确：
- `finalDamage > 0` 才触发 OnTakeDamage？
- 还是**始终触发**（让 View 层判断是显示"0"还是"IMMUNE"）？

---

### GD-011：SkillComponent 前摇（Casting）期间玩家能否移动/攻击？

**新发现**：TDD 的 SkillComponent 状态机在 Casting 期间只做 `_stateTimer -= dt`，不对 Entity 施加任何约束。这意味着：

- 前摇期间 Entity **可以正常移动**
- 前摇期间 AttackComponent **可以正常发射弹幕**
- 前摇期间 **另一个 Skill**（如果未来有多技能）可以被触发？（当前单槽不存在）

**从设计角度**：
- 对于弹幕射击的"Boss 蓄力 AOE"：通常蓄力时 Boss **停止移动 + 停止普攻**（视觉告知玩家"要放大招了"）
- 对于玩家"蓄力技能"：通常蓄力时**移速降低**但不完全禁止移动（保留躲避能力）

**设计问题**：Phase 3A 是否需要支持"Casting 期间的行为约束"？

**我的判断**：Phase 3A **不需要**。原因：
- "最小可用"原则
- 行为约束应该通过 Buff 实现（前摇开始时施加减速 Buff，前摇结束时移除）
- 更复杂的"施法中禁止移动"应该在 Phase 4 FSM 中做

**但 TDD 应明确记录这个设计选择**："Casting 期间不限制 Entity 其他行为。如需限制（Boss 蓄力停步），通过配合施加减速 Buff 或 Phase 4 FSM 状态约束实现。"

---

### GD-012：AutoAim 的"最近优先"策略在特定场景下可能反直觉

**新发现**：AutoAim 永远锁定最近目标。但在弹幕射击中有这样的场景：

场景：玩家在屏幕底部，前方有一排 5 个小兵。最近的小兵在正前方 2 单位处，另一个在左前方 2.1 单位处（几乎一样近）。

当正前方小兵被击杀后，AutoAim 重搜 → 锁定左前方小兵 → 弹幕偏转到左边 → 正前方第二排的小兵安全通过。

**玩家心智模型**："我在对付正前方的敌人"，但系统选了"最近的"（恰好在侧面）。

**我不要求改算法**（最近优先在大多数情况下是对的），但想确认：

1. Phase 4 的扩展方向是否包含**瞄准策略可配置**？（最近/血最少/距轨迹最近/朝向权重）
2. 当前是否有 `TargetPriority` 的架构预留？

**如果没有，建议在§十未决项中记录**。

---

### GD-013：ApplyBuffEffect 的 "施加给敌方最近目标" 搜索半径硬编码 5f

**新发现**：`ApplyBuffEffect` 中 `ApplyToSelf = false` 时：

```csharp
: EntityManagerAccessor.Instance?.FindNearestEntity(
    ctx.CastPosition, 5f, CampUtility.GetHostileCamp(ctx.Caster.Camp));
```

**5f 是硬编码的**。这意味着：
- 所有"给敌人上 Debuff"的技能搜索范围都是 5 单位
- 如果技能设计意图是"远程诅咒"（搜索范围 10）或"近身冰冻"（搜索范围 2），无法配置

**建议**：新增可配置字段：

```csharp
[System.Serializable]
public class ApplyBuffEffect : ISkillEffect
{
    public BuffConfigSO BuffConfig;
    public bool ApplyToSelf = true;
    
    [Tooltip("搜索半径（仅 ApplyToSelf=false 时生效）")]
    [Min(0.1f)]
    public float SearchRadius = 5f;  // GD-013：提取为配置
    
    public bool Execute(SkillContext ctx)
    {
        Entity target = ApplyToSelf 
            ? ctx.Caster 
            : EntityManagerAccessor.Instance?.FindNearestEntity(
                ctx.CastPosition, SearchRadius, CampUtility.GetHostileCamp(ctx.Caster.Camp));
        // ...
    }
}
```

---

以上 5 个追问，请守方回应。

---

## Round 2 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: GD-009 OnPlayerHitBounds 触发频率 — ✅ 接受方案 C（事件纯数据，View 层自行冷却）

**设计分析**：

三个方案对比：
- A（状态边沿）：需要 Bootstrap 维护"上帧是否在边界内"的状态 → 增加了逻辑层复杂度，且不够灵活（View 可能想做"持续贴边时渐变效果"）
- B（事件内置冷却）：逻辑层不应该关心"视觉反馈冷却"——这是 View 的职责，冷却时间也取决于具体反馈类型
- C（View 层冷却）：**事件层保持"纯信号"**，View 层按需消费——最灵活

**选择方案 C**。事件签名保持原样，TDD 中添加使用指南注释：

```csharp
/// <summary>
/// 玩家 Entity 触碰移动边界时触发。可能每帧触发（贴边滑动时）。
/// ⚠️ 消费者应自行做节流/冷却——不要在此回调中每帧做重开销操作。
/// 推荐：View 层订阅后用 cooldown timer（如 0.3s）去频。
/// </summary>
public static event System.Action<Entity, Vector2, Vector2> OnPlayerHitBounds;
```

**影响**：纯注释/文档变更。

---

### RE: GD-010 DamageTakenModifier=0 时的受击事件触发行为 — ✅ 接受（明确文档化设计选择）

**结论**：好问题。答案取决于我们想要什么样的"无敌"体验。

**Phase 2 已有的 HealthComponent.TakeDamage 行为**：

```csharp
public void TakeDamage(ref DamageContext ctx)
{
    // IDamageModifier 链处理（含 Buff 的 DamageTakenModifier）
    int finalDamage = ApplyModifiers(ctx.BaseDamage);
    
    if (finalDamage <= 0)
    {
        // 无伤害 → 不扣血，但仍触发事件（传入 finalDamage=0）
        OnTakeDamage?.Invoke(ref ctx, 0);
        return;
    }
    
    _currentHp -= finalDamage;
    OnTakeDamage?.Invoke(ref ctx, finalDamage);
    // ...
}
```

**设计选择**：`finalDamage = 0` 时**仍然触发事件**（传入 0）。

**理由**：
- View 层可以根据 finalDamage 显示不同效果：0 → 显示 "IMMUNE" 或无敌护盾特效；>0 → 正常伤害数字
- 如果不触发事件，View 层无法知道"有攻击打到我了但被挡住了"→ 无法做无敌反馈
- **"无敌"的好设计 = 玩家感知到"我在被攻击但我无敌"**，而非"什么都没发生"

**TDD 文档补充**：§3.2.1 新增：

> **伤害为 0 的事件行为（GD-010）**：`HealthComponent.TakeDamage` 在 `finalDamage = 0` 时仍触发 `OnTakeDamage` 事件（传入 0）。View 层可据此显示"IMMUNE"文字或无敌特效。如需完全静默（不触发任何事件），应在 TakeDamage 入口检查 Entity 的"完全免疫"标记（Phase 4 需求）。

**影响**：纯文档确认。当前 Phase 2 实现已符合此行为（需实施时验证）。

---

### RE: GD-011 Casting 期间行为约束 — ✅ 接受（文档明确化 + 不实现约束）

**确认**：Phase 3A 的设计选择是 **"Casting 期间不限制任何其他行为"**。

**设计理由**：
1. 弹幕射击品类中，大部分"技能"是瞬发或极短前摇（0.1~0.3s），限制意义不大
2. Boss 蓄力 AOE 的"停步"效果应该通过 **SkillEffect 链** 实现——在 Effects 列表第一个放一个 `ApplyBuffEffect(自身, 减速100%)`，最后一个放 `RemoveBuffEffect(自身, 减速BuffId)`
3. Phase 4 FSM 提供真正的"状态互斥"（施法状态禁止移动/攻击）

**TDD 修正**：§3.3.1 新增设计约束注释：

> **Casting 行为约束（GD-011）**：Phase 3A 的 SkillComponent Casting 状态**不限制 Entity 其他行为**（移动/攻击均正常）。如需 Boss 蓄力时停步，通过 Effects 列表在前摇开始时施加减速/禁足 Buff 实现。Phase 4 FSM 提供状态级互斥约束。

**但这引出一个问题**：当前 Effects 只在前摇**结束**时执行。如果要在前摇**开始**时施加 Buff——

等等，攻方没有要求这个。Phase 3A 保持"Effects 在前摇后执行"的简单模型。如果需要"前摇开始时施加 Buff"，那是 Phase 4 的 `OnCastStart` 事件。

**影响**：§3.3.1 纯文档变更。

---

### RE: GD-012 AutoAim 瞄准策略扩展 — ✅ 接受（记录到未决项）

**确认**：Phase 3A 的"最近优先"是正确的默认策略（80% 场景够用），高级策略是 Phase 4 的事。

**架构兼容性**：当前 `AutoAimComponent.SearchTarget()` 调用 `FindNearestEntity`——如果要做策略可配置，改为：

```csharp
// Phase 4 扩展预告
// private IAimStrategy _strategy; // 策略接口
// _currentTarget = _strategy.SelectTarget(_owner, searchResults);
```

`ITargetProvider` 接口不变，内部搜索策略可替换。

**修正**：§十 新增未决项：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 11 | AutoAim 瞄准策略可配置（IAimStrategy：最近/血最少/朝向权重/轨迹预判） | GD-012 | Phase 4 |

---

### RE: GD-013 ApplyBuffEffect 搜索半径硬编码 5f — ✅ 接受

**结论**：明显是"偷懒的硬编码"，应该提取为配置字段。攻方说得对。

**修正**：

```csharp
[System.Serializable]
public class ApplyBuffEffect : ISkillEffect
{
    [Tooltip("要施加的 Buff 配置")]
    public BuffConfigSO BuffConfig;
    
    [Tooltip("施加给自己还是目标")]
    public bool ApplyToSelf = true;
    
    [Tooltip("搜索半径（仅 ApplyToSelf=false 时生效）")]
    [Min(0.1f)]
    public float SearchRadius = 5f;  // GD-013：从硬编码提取
    
    public bool Execute(SkillContext ctx)
    {
        Entity target = ApplyToSelf 
            ? ctx.Caster 
            : EntityManagerAccessor.Instance?.FindNearestEntity(
                ctx.CastPosition, SearchRadius, CampUtility.GetHostileCamp(ctx.Caster.Camp));
        
        if (target == null) return false;  // ATK-008：无目标 = 效果未执行
        var buffComp = target.GetComponent(ComponentType.Buff) as BuffComponent;
        if (buffComp == null) return false;
        return buffComp.ApplyBuff(BuffConfig);
    }
}
```

**注意**：这里同时修正了 `bool` 返回值（ATK-008），并且 `target == null` 时 `return false`——这是"施放失败"的正确语义（Phase 4 可用于"失败不进 CD"）。

**影响**：§3.4.6 代码修改。

---

## Round 2 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| GD-009 | ✅ 接受方案 C | 事件纯数据推送 + 文档注明消费者需自行节流 |
| GD-010 | ✅ 接受 | 明确 finalDamage=0 仍触发事件，View 层自行判断显示 |
| GD-011 | ✅ 接受 | 文档明确"Casting 不限制其他行为"的设计选择 |
| GD-012 | ✅ 接受 | §十 新增 AutoAim 策略可配置（Phase 4） |
| GD-013 | ✅ 接受 | ApplyBuffEffect.SearchRadius 提取为配置字段 |

**Round 2 全部接受。攻方是否满意收敛？**

---

## Round 3 · 攻方最终审查

> 🎮 **攻方（游戏设计师）**：

Round 1-2 的回应非常充分。做最后一轮扫描——从"验收矩阵"和"调整杠杆"维度做收尾检查：

---

### GD-014：验收矩阵（§七）缺少"Buff 修正 Clamp 验证"测试项

**新发现**：GD-004 接受后，RecalcModifiers 添加了 Min/Max Clamp。但验收矩阵（14 项）中没有覆盖：

- 测试：施加 3 个 0.5x 减速 Buff → 最终速度不低于 40% 基础速度
- 测试：施加 3 个 2.0x 加速 Buff → 最终速度不超过 250% 基础速度

**建议**：新增验收项 #15：

| # | 测试项 | 通过条件 | 步骤 |
|---|--------|---------|------|
| 15 | Buff 修正 Clamp | 多 Buff 叠加后 MoveSpeedModifier ∈ [0.4, 2.5]，AttackIntervalModifier ∈ [0.3, 3.0] | P3.4 |

---

### GD-015：所有数值参数缺少"调整杠杆清单"

**设计师核心关切**：Phase 3A 引入了大量**影响手感的数值**，但 TDD 中没有一个集中的"调整杠杆"参考表。实施完成后，策划做 gameplay 调整时需要知道"改哪个参数影响什么"。

**建议新增"调整杠杆速查表"**（§ 九后或独立附录）：

| 杠杆 | 位置 | 默认值 | 影响 | 状态 |
|------|------|--------|------|------|
| PlayerBoundsCenter/Size | Bootstrap Inspector | (0,0)/(9,14) | 玩家活动范围 | `[占位符]` |
| AutoAimRadius | EntityConfigSO | 0 (不启用) | 锁定范围 | `[占位符]` |
| AutoAimSearchInterval | EntityConfigSO | 0.2s | 瞄准灵敏度 | `[占位符]` |
| SkillConfigSO.CooldownTime | SkillConfigSO | 5s | 技能使用频率 | `[占位符]` |
| SkillConfigSO.CastTime | SkillConfigSO | 0s | 施法前摇（手感 vs 策略） | `[占位符]` |
| SkillConfigSO.RecoveryTime | SkillConfigSO | 0.5s | 施法后硬直 | `[占位符]` |
| BuffDuration | BuffConfigSO | 5s | Buff 持续长度 | `[占位符]` |
| MoveSpeedModifier | BuffConfigSO | 1.0 | 速度加减成 | `[占位符]` |
| AttackIntervalModifier | BuffConfigSO | 1.0 | 攻速加减成 | `[占位符]` |
| DamageTakenModifier | BuffConfigSO | 1.0 | 受伤加减成 | `[占位符]` |
| MIN_MOVE_SPEED_RATIO | BuffComponent | 0.4 | 减速下限 | `[占位符]` |
| MAX_MOVE_SPEED_RATIO | BuffComponent | 2.5 | 加速上限 | `[占位符]` |
| DamageDealer._buffer size | DamageDealer | 64 | AOE 最大目标数上限 | 固定 |
| MAX_BUFFS | BuffComponent | 8 | 同时最大 Buff 数 | 固定 |

这不需要实际代码改动——纯文档价值。但对**后续 gameplay 调优**极为重要。

---

### GD-016：AreaDamageEffect 缺少"伤害衰减"设计预留

**新发现**：`AreaDamageEffect` 对范围内所有目标造成**等量伤害**。但多数 AOE 设计会有**距离衰减**（中心全额 → 边缘 50%）。

Phase 3A 不需要实现衰减，但当前 `DamageDealer.DealAreaDamage` 的签名不支持传入衰减函数。

**分析后撤回**：距离衰减需要知道每个目标的距离，这是 `DealAreaDamage` 内部循环的事情。如果未来要做，改动在 `DealAreaDamage` 内部加一个可选 `Func<float, float>` 参数（距离→伤害倍率）即可，不影响当前签名（可选参数向下兼容）。

✅ 不追问，但建议在 §十 未决项记录。

---

### GD-017：SkillContext 中缺少 SkillConfigSO 引用——Effect 无法读取技能等级/技能名

**新发现**：`SkillContext` 是：
```csharp
public struct SkillContext
{
    public Entity Caster;
    public Vector2 CastPosition;
    public Vector2 AimDirection;
    public float DeltaTime;
}
```

**缺少技能配置引用**。如果一个 ISkillEffect 需要知道"我属于哪个技能"（用于日志、调试、或 Phase 4 的技能等级系统读取配置中的自定义字段），当前 SkillContext 不提供这个信息。

**Phase 3A 是否需要？** 看一遍所有内置 Effect：
- FireBulletsEffect：不需要知道技能名
- AreaDamageEffect：不需要
- ApplyBuffEffect：不需要

**Phase 4 是否会需要？** 几乎必然——"技能等级影响 Effect 参数"是标准需求。

**建议**：在 SkillContext 中添加一个引用字段，成本为零（struct 多一个引用 = +8 bytes）：

```csharp
public struct SkillContext
{
    public Entity Caster;
    public Vector2 CastPosition;
    public Vector2 AimDirection;
    public float DeltaTime;
    public SkillConfigSO SkillConfig;  // GD-017：技能配置引用
}
```

---

以上 4 个点，请守方回应。

---

## Round 3 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: GD-014 验收矩阵补充 Buff Clamp 测试项 — ✅ 接受

**修正**：验收矩阵新增 #15 和 #16（拆分速度和攻速两项）：

| # | 测试项 | 通过条件 | 步骤 |
|---|--------|---------|------|
| 15 | Buff 速度 Clamp | 多减速 Buff 叠加 → MoveSpeedModifier 不低于 0.4；多加速 Buff 叠加 → 不超过 2.5 | P3.4 |
| 16 | Buff 攻速 Clamp | 多攻速 Buff 叠加 → AttackIntervalModifier ∈ [0.3, 3.0] | P3.4 |

**影响**：验收矩阵从 14 项 → 16 项。

---

### RE: GD-015 调整杠杆速查表 — ✅ 接受

**结论**：这是**游戏设计师的核心工具**——每个系统的"旋钮"在哪里、默认值是什么、改了影响什么。TDD 中确实缺少这个集中视图。

**修正**：在 TDD §八（架构决策摘要）之后、§九（风险）之前，新增 **§八b 调整杠杆速查表**，内容按攻方建议的表格。所有默认数值标注 `[占位符]`，提醒策划必须在 gameplay 测试中确认。

**影响**：纯文档新增，对代码无影响。

---

### RE: GD-016 AOE 伤害衰减 — ✅ 确认撤回 + 记录未决项

**认同攻方分析**：距离衰减通过可选参数向下兼容，不影响 Phase 3A 签名。

**修正**：§十 新增：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 12 | DealAreaDamage 距离衰减（可选参数 `Func<float, float> falloff`） | GD-016 | Phase 4 |

---

### RE: GD-017 SkillContext 添加 SkillConfigSO 引用 — ✅ 接受

**结论**：成本为零（struct +8 bytes），收益明确（Phase 4 技能等级、调试日志）。"现在免费做 vs 以后 struct 布局变更"——明显现在做。

**修正**：

```csharp
public struct SkillContext
{
    public Entity Caster;
    public Vector2 CastPosition;
    public Vector2 AimDirection;
    public float DeltaTime;
    public SkillConfigSO SkillConfig;  // GD-017：技能配置引用
}

// SkillComponent.ExecuteEffects 中赋值：
var ctx = new SkillContext
{
    Caster = _owner,
    CastPosition = _owner.Position,
    AimDirection = GetAimDirection(),
    DeltaTime = dt,
    SkillConfig = _config,  // GD-017
};
```

**影响**：§3.3.3 SkillContext 新增字段，§3.3.5 ExecuteEffects 赋值修改。

---

## Round 3 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| GD-014 | ✅ 接受 | 验收矩阵新增 #15/#16（Buff Clamp 测试） |
| GD-015 | ✅ 接受 | 新增 §八b 调整杠杆速查表 |
| GD-016 | ✅ 撤回 | §十 记录 AOE 距离衰减未决项 |
| GD-017 | ✅ 接受 | SkillContext 新增 SkillConfigSO 引用 |

---

## 🏁 PK 收敛声明

> **攻方（游戏设计师）**：3 轮审查，17 个问题（15 个实质 + 2 个自行撤回/确认），全部收敛。满意。
> **守方（Unity 架构师）**：所有接受项将与 Round 2（工具开发者）的修正一并回写 TDD v0.4。

### PK 统计

| 维度 | 数据 |
|------|------|
| 总轮次 | 3 轮（最大 6 轮） |
| 总问题数 | 17（实质 15 + 撤回/确认 2） |
| 接受 | 13 |
| 部分接受 | 2（GD-001 预留事件不实现效果、GD-006 保持 0=永久） |
| 拒绝 | 0 |
| 自行撤回/确认 | 2（GD-016 距离衰减 → 记录未决项） |
| 收敛率 | 100% |

### 需回写 TDD 的修正项清单

| ID | 修正内容 | 影响范围 |
|----|---------|---------| 
| GD-001 | ClampPlayerPositions 新增 OnPlayerHitBounds 事件点 | §3.0.2 + §十 |
| GD-002 | AutoAim 目标死亡立即重搜 + SearchInterval 标注 [占位符] | §3.1.4 |
| GD-003 | §十 新增多技能槽扩展方向（Phase 3B） | §十 |
| GD-004 | BuffComponent.RecalcModifiers 添加 Min/Max Speed/AttackInterval Clamp | §3.4.3 |
| GD-005 | CooldownTime [Min(0f)] + CD=0 & Recovery=0 安全网 | §3.3.2 + §3.3.5 |
| GD-006 | Duration=0 永久 Buff 文档约束 + Tooltip 改善 | §3.4.2 |
| GD-007 | DamageDealer 走完整 TakeDamage 管线确认文档 | §3.2.1 |
| GD-008 | §十 新增条件触发扩展方向（Phase 4） | §十 |
| GD-009 | OnPlayerHitBounds 事件文档注明消费者需自行节流 | §3.0.2 |
| GD-010 | finalDamage=0 仍触发 OnTakeDamage 事件确认文档 | §3.2.1 |
| GD-011 | Casting 不限制 Entity 其他行为——文档明确化 | §3.3.1 |
| GD-012 | §十 新增 AutoAim 策略可配置（Phase 4） | §十 |
| GD-013 | ApplyBuffEffect.SearchRadius 提取为配置字段 | §3.4.6 |
| GD-014 | 验收矩阵新增 #15/#16（Buff Clamp 测试） | §七 |
| GD-015 | 新增 §八b 调整杠杆速查表 | §八b（新增） |
| GD-016 | §十 新增 AOE 距离衰减（Phase 4） | §十 |
| GD-017 | SkillContext 新增 SkillConfigSO 引用 | §3.3.3 + §3.3.5 |

**PK 状态**：✅ **已收敛 — 待与 Round 2 修正合并回写 TDD v0.4**
