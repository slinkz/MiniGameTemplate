# Phase 3A TDD v0.3 — PK 评审（Round 4）

> **攻方角色**：🧱 软件架构师 Agent（关注：SOLID 原则、依赖管理、耦合度、可测试性、模块边界、设计模式适用性、接口契约完整性）
> **守方角色**：🏗️ Unity 架构师 Agent（关注：架构一致性、性能约束、扩展路径、设计原则、零 GC、Unity 生态约束）
> **最大轮次**：6 轮
> **PK 状态**：🔄 进行中
> **TDD 版本**：v0.3（基于 PK Round 1~3 收敛版本，含 Round 2/3 待回写修正）

---

## Round 1 · 攻方发难

> 🧱 **攻方（软件架构师）**：

我从经典软件工程原则（SOLID、耦合/内聚、依赖方向、可测试性、契约完整性）角度审读了整个 TDD。Unity 特有约束我理解（MonoBehaviour 生态、零 GC 要求、WebGL 限制），但这些不应成为架构债务的借口。以下是我识别的问题：

---

### SA-001：DamageDealer 静态类 + 静态 buffer = 全局可变状态 + 不可测试

**违反原则**：单一职责、依赖反转、可测试性

**问题分析**：

`DamageDealer` 是静态类，持有 `private static readonly Entity[] _buffer` 和 `private static bool _isProcessingArea`。这意味着：

1. **全局可变状态**：`_isProcessingArea` 是全局的 boolean flag，任何地方调用 `DealAreaDamage` 都会修改它
2. **隐式依赖**：`DealAreaDamage` 内部直接访问 `EntityManagerAccessor.Instance`（Service Locator 模式），调用方看签名完全不知道它依赖 EntityManager
3. **不可测试**：想要单元测试 DamageDealer，必须 mock 全局的 `EntityManagerAccessor.Instance` + 所有 Entity 的 HealthComponent。静态方法 + 静态状态 = 测试噩梦

**对比**：如果 DamageDealer 是实例类（注入 IEntityQuery 依赖），可以：
- 在测试中替换为 FakeEntityQuery
- 无全局状态污染
- 方法签名暴露真实依赖

**但我理解 Unity 约束**：
- 实例类意味着需要一个持有者（Bootstrap？）→ 增加生命周期管理复杂度
- Service Locator（EntityManagerAccessor）在 Unity 中是被广泛接受的折衷

**追问**：
1. 这个框架是否有任何层级的自动化测试计划？（如果完全不做单元测试，可测试性问题的权重降为 0）
2. 如果不做测试，是否至少有 **Debug 验证路径**（如 DamageDealer 的结果可在 Inspector 中观测）？

---

### SA-002：ISkillEffect 接口契约不完整——无法保证 Effect 是否无状态

**违反原则**：里氏替换、契约完整性

**问题分析**：

`ISkillEffect` 定义为：
```csharp
public interface ISkillEffect
{
    bool Execute(SkillContext ctx);
}
```

接口没有声明实现类应该是**有状态还是无状态**。这很关键：

- `[SerializeReference]` 意味着 Effect 实例**嵌入在 SkillConfigSO 中**（ScriptableObject 是项目级资产）
- 如果 Effect 实现是有状态的（比如一个 "ChargeUpEffect" 累积充能值），**所有引用同一个 SkillConfigSO 的 Entity 共享同一个 Effect 实例**
- 共享实例 + 可变状态 = 经典并发 bug（虽然 Unity 单线程，但多 Entity 同帧依次 Execute 相当于"逻辑并发"）

**场景**：
```csharp
[Serializable]
public class AccumulateEffect : ISkillEffect
{
    private int _chargeCount; // ❌ 有状态！共享实例 = 所有 Entity 共享计数器
    
    public bool Execute(SkillContext ctx)
    {
        _chargeCount++;
        if (_chargeCount >= 3) { DoSuperAttack(ctx); _chargeCount = 0; }
        return true;
    }
}
```

**设计问题**：TDD 是否强制要求 ISkillEffect 实现为**无状态**？如果是：
- 应在接口文档中显式声明约束
- 应在代码审查/Editor 工具中提供验证手段

如果允许有状态 Effect（Phase 4 可能需要）：
- 需要每个 Entity 持有独立的 Effect 实例副本（深拷贝 / 运行时 Clone）
- 当前"SkillConfigSO 共享 Effect 数组"的设计就不够了

---

### SA-003：BuffComponent 与 MovementComponent 的双向耦合

**违反原则**：依赖反转、模块边界

**问题分析**：

依赖链：
```
BuffComponent → (knows) MovementComponent
BuffComponent → (calls) movement.AddOrUpdateSpeedModifier(99, value)
BuffComponent → (calls) movement.RemoveSpeedModifierById(99)
```

BuffComponent **直接知道** MovementComponent 的具体 API（方法名、参数语义、魔法数字 99）。这是**跨组件的具体依赖**。

**问题**：
1. 如果 MovementComponent 的 API 变了（比如 Phase 4 重构为 `IMovable` 接口），BuffComponent 必须跟着改
2. `BUFF_MODIFIER_ID = 99` 是魔法数字——如果其他系统也想通过 by-ID 注入修正器，怎么保证 ID 不冲突？
3. 类似的：BuffComponent 需要影响 AttackComponent 的 interval——但代码在 **AttackComponent.Tick** 中（`buff.AttackIntervalModifier`）。方向反了一半：
   - Speed：Buff → push → Movement（主动注入）
   - Attack：Attack → pull → Buff（被动查询）
   - 不一致的依赖方向 = 理解成本增加

**替代方案**（仅供讨论）：
- 统一为 **pull 模式**：所有消费者（Movement/Attack/Health）主动从 BuffComponent 拉取修正值。BuffComponent 不 push 任何东西。
- 优势：BuffComponent 只做"聚合 Modifier"，不知道消费者是谁
- 劣势：Movement 的 by-ID SpeedModifier 系统就用不上了（Buff 不是唯一修正来源）

**追问**：当前设计中 Speed 用 push + Attack 用 pull 的"混合模式"是否是有意为之？如果是，架构理由是什么？

---

### SA-004：EntityManagerAccessor 单例模式的隐式契约——Init 顺序依赖

**违反原则**：显式依赖、失败模式清晰化

**问题分析**：

多个地方使用 `EntityManagerAccessor.Instance`：
- `DamageDealer.DealAreaDamage`
- `ApplyBuffEffect.Execute`
- `AutoAimComponent.SearchTarget`

全部以 `if (mgr == null) return` 静默失败。

**问题**：
1. **静默失败 = 隐藏 bug**：如果 EntityManager 没初始化（Init 顺序错误），所有 AutoAim 不工作、所有 AOE 不造伤——但不报错。策划/开发者看到的是"技能没效果"但不知道为什么
2. Phase 2 已有的 `EntityManagerAccessor` 的生命周期契约没有在 Phase 3A TDD 中重申。新加入的开发者读 Phase 3A 时不知道这个前置条件
3. 没有**诊断措施**：`null` 时应该至少 `Debug.LogError`，让开发者知道"系统未初始化"

**建议**：
- 第一次 `EntityManagerAccessor.Instance == null` 时输出 `Debug.LogError("[SystemName] EntityManager not initialized! Ensure EntitySystemBootstrap runs first.")`
- 或者：Assert + conditional compilation（Debug 模式下 Assert，Release 下静默）

---

### SA-005：SkillComponent 状态机缺少状态转换约束表

**违反原则**：契约完整性、防御式设计

**问题分析**：

SkillComponent 的状态机有 4 个状态：`Idle → Casting → Recovery → Cooldown → Idle`。

TDD 中有**代码级别**的状态转换（switch-case），但没有**显式的合法转换表**。这意味着：

- 如果 Phase 4 新增状态（如 `Channeling`、`Interrupted`），开发者不知道哪些转换是合法的
- 如果外部代码（如 Buff 的"沉默"效果）想强制中断技能，不知道应该从哪些状态转换到哪个状态

**建议**：新增显式状态转换矩阵：

| 当前状态 | → Idle | → Casting | → Recovery | → Cooldown |
|---------|--------|-----------|------------|------------|
| Idle | — | ✅ ShouldTrigger | ❌ | ❌ |
| Casting | ✅ 死亡中断(ATK-014) | — | ✅ 前摇结束 | ❌ |
| Recovery | ❌ | ❌ | — | ✅ 后摇结束 |
| Cooldown | ✅ CD归零 | ❌ | ❌ | — |

此表在 Phase 4 扩展新状态时价值极高——新增状态时必须更新此表，强制设计者思考所有转换路径。

---

### SA-006：FindEntitiesInRadius 的 resultBuffer 所有权语义不明

**违反原则**：所有权清晰、内存安全契约

**问题分析**：

```csharp
public int FindEntitiesInRadius(
    Vector2 center, float radius, EnumCamp camp,
    Entity[] resultBuffer, int maxResults)
```

调用方传入 `resultBuffer`。方法写入结果后返回 count。

**所有权问题**：
1. 方法返回后，`resultBuffer[0..count-1]` 中的引用仍然指向活跃 Entity。但如果调用方在**之后的代码**中触发了 Entity 回池（比如 DealAreaDamage 中 TakeDamage 杀死了一个 Entity），buffer 中的引用变成了"指向已回池 Entity"的悬挂引用
2. 当前 DamageDealer 的 for 循环中，`_buffer[i]` 可能在循环过程中变成 `IsPendingDespawn = true`（前一个目标死亡触发连锁反应杀死后一个）

**TDD 中 DealAreaDamage 已有 `IsPendingDespawn` 检查吗？**

看代码——没有！循环中直接 `TakeDamage`，不检查 `_buffer[i].IsPendingDespawn`：

```csharp
for (int i = 0; i < count; i++)
{
    var ctx = baseContext;
    var health = _buffer[i].GetComponent(ComponentType.Health) as HealthComponent;
    if (health != null)
    {
        health.TakeDamage(ref ctx);
    }
}
```

如果第 1 个目标的 OnDeath 回调导致第 3 个目标被标记 PendingDespawn，循环到第 3 个时仍会对已标记回收的 Entity 造伤。

**这是 bug 还是 by design？**

---

### SA-007：SkillContext 是 struct 但包含引用类型字段——语义不清晰

**违反原则**：值类型语义一致性

**问题分析**：

```csharp
public struct SkillContext
{
    public Entity Caster;           // 引用类型
    public Vector2 CastPosition;    // 值类型
    public Vector2 AimDirection;    // 值类型
    public float DeltaTime;         // 值类型
    public SkillConfigSO SkillConfig; // 引用类型（GD-017 新增）
}
```

Struct 中包含引用类型字段。这在 C# 中完全合法，但有**语义陷阱**：

1. **值拷贝的"假象"**：`var ctx2 = ctx1` 拷贝了 struct，但 `ctx2.Caster` 和 `ctx1.Caster` 指向**同一个 Entity 对象**。修改 `ctx2.Caster.Position` 会影响 `ctx1.Caster.Position`
2. 在 DealAreaDamage 中 `var ctx = baseContext;` 就利用了这一点（每个目标独立 context，但 Caster 引用共享）——这是正确的
3. 但如果有人写 `ctx.Caster = null;`——只影响该 copy 的 Caster 字段，不影响原始

**这不是 bug**，但属于"容易被误解的设计"。

**建议**：在 SkillContext 的 XML Doc 中明确声明：

> "SkillContext 是 struct（值拷贝语义），但内部包含引用类型字段（Caster、SkillConfig）。  
> 值拷贝后，引用字段仍指向同一对象。这是 by-design——允许 DealAreaDamage 等场景中每个目标有独立 context 但共享 Caster 引用。"

---

### SA-008：CampUtility.GetHostileCamp 的扩展性——三方阵营怎么办？

**违反原则**：开放-封闭原则

**问题分析**：

```csharp
public static EnumCamp GetHostileCamp(EnumCamp self)
{
    return self switch
    {
        EnumCamp.Player => EnumCamp.Enemy,
        EnumCamp.Enemy  => EnumCamp.Player,
        _ => EnumCamp.Neutral
    };
}
```

硬编码的二元对立（Player ↔ Enemy）。如果品类扩展（例如 PvP 模式、三方乱斗、中立可攻击 NPC）：

- 新增 `EnumCamp.Ally`、`EnumCamp.NeutralHostile` 等值时，switch 默认返回 Neutral → 新阵营永远找不到敌人
- 每次新增阵营都要改这个 switch = 违反 OCP

**替代方案**：
- A：阵营关系矩阵（`bool[,] hostileMatrix`），数据驱动
- B：`EnumCamp` 改为 `[Flags]` bitmask，`IsHostile = (camp & enemyMask) != 0`

**但我理解**：Phase 3A 的设计支柱 3 是"最小可用"，弹幕射击品类只有 Player/Enemy 两方。

**追问**：§十（未决项）是否应记录"多阵营支持"？还是明确声明"框架定位 = 二元阵营，不支持多阵营"？

---

以上 8 个问题，请守方逐一回应。

---

## Round 1 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

从 Unity 生态实际约束 + 项目定位（独立开发者微信小游戏模板）出发逐一回应：

---

### RE: SA-001 DamageDealer 静态类不可测试 — ⚠️ 部分接受（加诊断，不改静态设计）

**承认的点**：
- 攻方对"全局可变状态"和"隐式依赖"的分析是准确的
- 在企业级软件中，这确实是 anti-pattern

**不改为实例类的理由**：

1. **项目定位约束**：这是独立开发者模板——团队规模 = 1 人。"可测试性"的 ROI 需要衡量：
   - 完整单元测试框架（Test Runner + Mock）→ 维护成本 > 1 人团队能承受的
   - 验证路径已有：Editor 验收矩阵（14→16 项）+ 真机验证
   - 不做自动化单元测试，**做 PlayMode 集成测试**（真实场景验证）

2. **Unity 生态中静态工具类是标准做法**：`Physics2D.Raycast`、`Physics.OverlapSphere` 都是静态方法 + 内部 buffer。DamageDealer 刻意模仿这种模式——对 Unity 开发者来说是"熟悉的心智模型"。

3. **实例化 DamageDealer 的成本分析**：
   - 需要一个 host（Bootstrap？ScriptableObject？纯 C# singleton？）
   - 需要注入 IEntityQuery 接口 → 所有调用点从 `DamageDealer.DealAreaDamage(...)` 变为 `_damageDealer.DealAreaDamage(...)` → ISkillEffect 也需要持有 DamageDealer 引用 → SkillContext 膨胀
   - 收益：仅当需要 mock 测试时

**接受的修正**：

1. **加诊断日志**：`EntityManagerAccessor.Instance == null` 时输出 `Debug.LogError`（与 SA-004 合并处理）
2. **文档声明设计决策**：在 §3.2.1 补充——"DamageDealer 是静态工具类（模仿 Unity Physics API 风格）。不支持 mock/DI，通过 PlayMode 集成测试验证。如需单元测试，封装为 `IDamageService` 接口是 Phase 5 的可选重构路径。"

**回答攻方追问**：
1. **测试计划**：无自动化单元测试。验证通过：Editor 验收矩阵 + PlayMode 手动测试 + 真机验收。
2. **Debug 验证路径**：
   - DamageDealer 的每次伤害通过 `HealthComponent.OnTakeDamage` 事件传播 → EntityHitReactionHandler 显示伤害数字 → 视觉可观测
   - `DealAreaDamage` 返回 int hitCount → 调用方（AreaDamageEffect）可 Log

**影响**：§3.2.1 设计决策补充文档。

---

### RE: SA-002 ISkillEffect 有状态/无状态约束 — ✅ 接受（强制无状态 + 文档声明）

**结论**：攻方精准命中了一个真正的设计陷阱。`[SerializeReference]` + ScriptableObject 共享实例 = 有状态 Effect 必然 bug。

**设计选择**：Phase 3A 的 ISkillEffect **强制无状态**。

**理由**：
- Phase 3A 是"最小可用"——三个内置 Effect 全部无状态
- 有状态 Effect（充能/连续施法/蓄力）需要 per-Entity 实例 = 需要深拷贝或运行时 Clone = 开销 + 复杂度
- Phase 4 如果需要有状态 Effect，扩展方案：
  - A：SkillComponent.Init 时 Clone Effects 数组（每个 Entity 独立副本）
  - B：有状态行为由 SkillComponent 本身管理（状态在 Component 中，Effect 只做"一次性执行"）
  - C：新增 `IStatefulSkillEffect` 子接口，带 `Init/Reset/Clone` 方法

**修正**：

1. ISkillEffect 接口文档新增约束：

```csharp
/// <summary>
/// 技能效果策略接口。通过 [SerializeReference] 序列化到 SkillConfigSO。
/// 
/// ⚠️ 实现约束（SA-002）：
/// - ISkillEffect 实现必须是【无状态】的——不得持有随 Execute 调用变化的字段
/// - 原因：SkillConfigSO 是共享资产，多个 Entity 引用同一 SO = 共享同一 Effect 实例
/// - 如需有状态行为（充能/蓄力/连续施法），使用 SkillComponent 的内部状态或 Phase 4 扩展
/// - 所有序列化字段应为【配置参数】（只读），不应在 Execute 中修改
/// </summary>
public interface ISkillEffect
{
    bool Execute(SkillContext ctx);
}
```

2. §3.3.1 设计哲学新增约束说明

**影响**：纯文档约束，无代码变更。但这是**架构级约束**——必须在 Code Review 中严格执行。

---

### RE: SA-003 BuffComponent 与 MovementComponent 双向耦合 — ⚠️ 部分接受（保留 push+pull 混合，文档化理由）

**承认的点**：
- Speed 用 push、Attack 用 pull——方向不一致是事实
- 魔法数字 `BUFF_MODIFIER_ID = 99` 有冲突风险

**保留混合模式的理由**：

**为什么 Speed 用 push 而不是 pull**：

MovementComponent 的 SpeedModifier 系统是 Phase 1 已有设计（非 Buff 专属）：
- 其他系统也可能注入 Speed Modifier（地形减速、技能增速、装备加成等）
- MovementComponent 不应该"知道 BuffComponent 存在"——它只知道"有人给我加了一个修正器"
- 如果改为 pull（Movement 主动查询 Buff），Movement 就需要 `using Buff` → **下层依赖上层**，这更糟

**所以 push 是正确的依赖方向**：
```
BuffComponent (上层，知道 MovementComponent API) → push → MovementComponent (下层，不知道 Buff)
```

**为什么 Attack 用 pull**：
- AttackComponent 的攻击间隔只有 Buff 一个修正来源（Phase 3A）
- 如果也 push，需要 AttackComponent 也提供 by-ID Modifier 系统——过度设计
- Pull 更简单：Attack.Tick 中 `if (buff != null) interval *= buff.AttackIntervalModifier`

**混合模式的架构理由总结**：
- **已有 Modifier 系统的组件（Movement）**：用 push（Buff 是多个修正来源之一）
- **没有 Modifier 系统的组件（Attack）**：用 pull（Buff 是唯一修正来源，不值得建 Modifier 系统）
- **统一为 pull 的代价**：Movement 的 by-ID 系统就废了——其他修正来源（地形/装备）也得找地方汇总

**接受的修正**：

1. **魔法数字修正**：`BUFF_MODIFIER_ID = 99` → 提取为公共常量，并明确 ID 分配规范：

```csharp
/// <summary>
/// SpeedModifier ID 分配规范（SA-003）：
/// - 0~49：系统保留（Buff=99 改为 1、地形=2、装备=3 ...）
/// - 50~99：Phase 4+ 扩展预留
/// - 100+：自定义/临时修正器
/// </summary>
public static class SpeedModifierIds
{
    public const int Buff = 1;       // BuffComponent 速度修正
    public const int Terrain = 2;    // 预留：地形减速
    public const int Equipment = 3;  // 预留：装备加成
}
```

2. **文档化混合模式设计理由**（写入 §3.4.4 设计决策）

**影响**：常量提取 + 文档补充。

---

### RE: SA-004 EntityManagerAccessor.Instance null 时静默失败 — ✅ 接受

**结论**：攻方说得对。静默 `return` = 隐藏 bug。对于独立开发者来说更致命——没有团队帮你排查"为什么技能没效果"。

**修正**：所有 `EntityManagerAccessor.Instance == null` 检查点添加一次性 LogError：

```csharp
// 提取为统一辅助方法
private static EntityManager GetManager([System.Runtime.CompilerServices.CallerMemberName] string caller = "")
{
    var mgr = EntityManagerAccessor.Instance;
    if (mgr == null)
    {
        Debug.LogError($"[{caller}] EntityManager not initialized! Ensure EntitySystemBootstrap.Awake() runs first.");
    }
    return mgr;
}
```

应用到：
- `DamageDealer.DealAreaDamage` / `DealDamageToEntity`
- `ApplyBuffEffect.Execute`
- `AutoAimComponent.SearchTarget`

**注意**：LogError 不应每帧触发（AutoAim 定频搜索中如果 Manager 为 null 会每 0.2s 打一次 error）。优化：首次 null 后设标志位，只报一次。

**更好的方案**：`Debug.Assert(mgr != null, ...)` — Debug 模式下断言，Release 模式下编译移除。结合 `if (mgr == null) return` 做安全退出：

```csharp
var mgr = EntityManagerAccessor.Instance;
Debug.Assert(mgr != null, "[DamageDealer] EntityManager not initialized!");
if (mgr == null) return 0; // Release 安全退出
```

**选择 Debug.Assert 方案**——开发阶段必然会触发断言弹窗（引起注意），Release 构建零开销。

**影响**：所有调用点添加 Debug.Assert + null 检查。

---

### RE: SA-005 SkillComponent 状态转换矩阵 — ✅ 接受

**结论**：攻方要求的"显式状态转换表"是状态机设计的标准实践。当前只有代码级别的隐式转换规则，对 Phase 4 扩展者不够友好。

**修正**：在 §3.3.5 之前或之后新增显式转换矩阵：

| 当前 \ 目标 | Idle | Casting | Recovery | Cooldown |
|------------|------|---------|----------|----------|
| **Idle** | — | ✅ `ShouldTrigger() && CastTime>0` | ❌ | ❌ |
| **Idle** | — | ❌ | ✅ `ShouldTrigger() && CastTime==0`（瞬发→直接 Recovery） | ❌ |
| **Casting** | ✅ 死亡中断(ATK-014) | — | ✅ 前摇 timer≤0 → ExecuteEffects → Recovery | ❌ |
| **Recovery** | ❌ | ❌ | — | ✅ 后摇 timer≤0 |
| **Cooldown** | ✅ CD≤0 | ❌ | ❌ | — |

**补充 Phase 4 扩展预告**：
- 新增 `Interrupted` 状态：Casting → Interrupted（被眩晕/沉默）→ Cooldown（中断不免 CD）或 Idle（中断免 CD）
- 新增 `Channeling` 状态：Recovery 替换为 Channeling（持续施法）

**影响**：§3.3.5 新增状态转换矩阵（纯文档）。

---

### RE: SA-006 DealAreaDamage 循环中未检查 PendingDespawn — ✅ 接受（这是 bug）

**结论**：**这是真正的 bug**。攻方找到了一个 Round 1~3 都没发现的缺陷。

**场景复现**：
1. AOE 命中 Entity A 和 Entity B
2. Entity A 的 TakeDamage → 死亡 → OnDeath 回调 → 回调中 Kill Entity B → B.IsPendingDespawn = true
3. 循环继续到 Entity B → 对已标记回收的 Entity 造伤 → 可能导致 HP 变负 / 二次死亡 / 状态异常

**修正**：

```csharp
for (int i = 0; i < count; i++)
{
    // SA-006：循环中检查——前序目标的 OnDeath 可能导致后序目标被标记回收
    if (_buffer[i].IsPendingDespawn || !_buffer[i].IsAlive) continue;
    
    var ctx = baseContext;
    var health = _buffer[i].GetComponent(ComponentType.Health) as HealthComponent;
    if (health != null)
    {
        health.TakeDamage(ref ctx);
    }
}
```

**附加**：同样的检查对 `DealDamageToEntity` 其实已有（入口检查 `IsPendingDespawn`），但 AOE 循环中的 buffer 元素在循环过程中可能状态变化——必须每次迭代重新检查。

**影响**：§3.2.2 代码修改（+1 行 if 检查）。新增验收项 #17："AOE 中连锁击杀不对已死亡 Entity 二次造伤"。

---

### RE: SA-007 SkillContext struct 含引用类型——语义文档化 — ✅ 接受

**结论**：这不是 bug，但属于"容易踩坑的设计"。文档声明是正确的做法。

**修正**：SkillContext XML Doc 新增：

```csharp
/// <summary>
/// 技能执行上下文（struct，零 GC）。
/// 
/// 设计说明（SA-007）：
/// SkillContext 是值类型（struct），但包含引用类型字段（Caster、SkillConfig）。
/// 值拷贝后，引用字段仍指向同一对象实例。
/// 这是 by-design：允许 DealAreaDamage 等场景中对 baseContext 做值拷贝，
/// 每个目标获得独立的 context 副本但共享 Caster 引用（不重复分配 Entity 对象）。
/// 
/// 不要：在 Execute 中修改 ctx.Caster 的引用指向（如 ctx.Caster = null）——
/// 这只影响当前副本，不影响其他副本或原始值。
/// </summary>
public struct SkillContext { ... }
```

**影响**：纯文档变更。

---

### RE: SA-008 CampUtility 二元阵营扩展性 — ⚠️ 部分接受（记录未决项 + 明确定位）

**结论**：攻方的分析正确——硬编码 switch 确实违反 OCP。但需要在"最小可用"和"未来扩展"之间画线。

**明确定位声明**：

> **Phase 3A~3B 的阵营模型 = 二元对立（Player ↔ Enemy）**。这是弹幕射击核心品类的标准模型。
> 不支持三方乱斗/PvP/中立可攻击 NPC。如需多阵营，属于 Phase 5 品类扩展。

**不改为矩阵/bitmask 的理由**：
- 当前框架品类定位 = 弹幕射击 + 塔防 → 都是严格二元阵营
- Bitmask 方案增加理解成本（策划需要理解位运算），收益为 0（没有多阵营场景）
- 数据驱动矩阵更重——需要额外配置资产、初始化流程、内存分配

**接受的修正**：

1. §十 未决项新增：

| # | 功能 | 来源 | 目标阶段 |
|---|------|------|---------|
| 13 | CampUtility 多阵营支持（关系矩阵或 bitmask） | SA-008 | Phase 5 |

2. CampUtility 注释中明确框架定位：

```csharp
/// <summary>
/// 阵营工具类——提供阵营相关的通用判断方法。
/// 
/// 当前版本仅支持二元阵营（Player ↔ Enemy）。
/// 框架品类定位：弹幕射击 + 塔防核心，均为严格二元对立。
/// 多阵营支持（PvP/三方/中立可攻击）属于 Phase 5 品类扩展范畴。
/// 扩展方向：关系矩阵（bool[,]）或 [Flags] bitmask（SA-008）。
/// </summary>
```

**影响**：§十 新增未决项 + CampUtility 注释补充。

---

## Round 1 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| SA-001 | ⚠️ 部分接受 | 保持静态设计 + 文档声明理由 + Debug.Assert |
| SA-002 | ✅ 接受 | ISkillEffect 强制无状态约束文档化 |
| SA-003 | ⚠️ 部分接受 | 保留 push+pull 混合 + 魔法数字→常量类 + 文档化理由 |
| SA-004 | ✅ 接受 | 所有 null 检查点添加 Debug.Assert |
| SA-005 | ✅ 接受 | 新增显式状态转换矩阵 |
| SA-006 | ✅ 接受 | AOE 循环中添加 PendingDespawn 检查（修 bug） |
| SA-007 | ✅ 接受 | SkillContext XML Doc 声明值拷贝+引用共享语义 |
| SA-008 | ⚠️ 部分接受 | 明确二元阵营定位 + 多阵营记录到 Phase 5 未决项 |

**8 个问题：5 个完全接受（含 1 个 bug 修复），3 个部分接受（架构决策保留但加文档/诊断）。攻方是否有后续追问？**

---

## Round 2 · 攻方追问

> 🧱 **攻方（软件架构师）**：

守方的回应体现了对 Unity 生态约束的深刻理解。3 个"部分接受"的理由我认可。以下追问和新发现：

---

### SA-009：追问 SA-001——DamageDealer 的 `_buffer` 和 `_isProcessingArea` 在 async/coroutine 场景下的安全性

**追问**：守方保留了静态设计。我接受。但有一个边界场景需要确认：

Unity 中 `MonoBehaviour.StartCoroutine` + `yield return` 可以在帧间暂停执行。如果未来 Phase 4+ 的某个系统在 coroutine 中调用 `DealAreaDamage`：

```csharp
IEnumerator DelayedAOE()
{
    DamageDealer.DealAreaDamage(pos1, 5f, camp, ctx);
    yield return new WaitForSeconds(0.5f);
    DamageDealer.DealAreaDamage(pos2, 5f, camp, ctx); // 0.5s 后第二次
}
```

这没问题——两次调用在不同帧，`_isProcessingArea` 在第一次调用结束时已经 false。

**但如果**：

```csharp
IEnumerator ParallelAOE()
{
    StartCoroutine(AOESequence(pos1)); // 协程 A
    StartCoroutine(AOESequence(pos2)); // 协程 B
    yield return null;
}

IEnumerator AOESequence(Vector2 pos)
{
    DamageDealer.DealAreaDamage(pos, 5f, camp, ctx); // ❓ 协程 A 和 B 同帧执行？
    yield return null;
}
```

Unity coroutine 是**协作式**的（同一帧按启动顺序依次执行到 yield），所以协程 A 的 DealAreaDamage 在同一帧**完整执行完**才轮到 B。`_isProcessingArea` 在 A 结束后 reset，B 不会被重入保护拦截。

**结论**：协程场景下当前设计是安全的。**撤回此追问**——但建议在 UA-003 注释中补充一句："Unity 协程是协作式调度（非抢占），同帧内的多个协程不会真正并行，重入保护对协程场景有效。"

✅ 自行确认安全，补充注释。

---

### SA-010：追问 SA-003——SpeedModifierIds 常量类的作用域和命名空间

**追问**：守方提出 `SpeedModifierIds` 常量类。两个细节问题：

1. **放在哪里？** 是嵌套在 `MovementComponent` 内部（`MovementComponent.ModifierIds.Buff`）还是独立顶层类？
   - 如果嵌套在 MovementComponent → Buff 引用 `MovementComponent.ModifierIds.Buff` → 依赖方向正确但引用路径冗长
   - 如果独立顶层 → 更短更清晰，但增加了命名空间内的公共类型数量

2. **Phase 4+ 有没有冲突检查？** 如果两个独立开发的系统（如 Terrain + Equipment）碰巧用了相同 ID，会悄悄覆盖对方的 modifier，无报错。
   - 是否需要在 `AddOrUpdateSpeedModifier` 中检测"新 ID 首次注册时如果覆盖了已有的不同来源"？

**我的判断**：问题 1 选独立顶层更好（与 `CampUtility` 同级别的"框架公共工具"）。问题 2 Phase 3A 不需要（只有 Buff 一个来源），记录到注释即可。

---

### SA-011：Entity 组件初始化顺序依赖——Init 按 ComponentType 枚举值顺序执行，但新组件的 Init 可能依赖其他组件

**新发现**：`ResetAll` / `InitAll` 按 `ComponentType` 枚举值的升序遍历：

```
State(0) → Health(1) → Animation(2) → Movement(3) → Collision(4) → AutoAim(5) → Skill(6) → Control(7) → AI(8) → Attack(9) → Buff(10)
```

**Phase 3A 的潜在问题**：

- `AutoAim(5).Init()` 内部调用 `SearchTarget()` → 调用 `EntityManagerAccessor.Instance.FindNearestEntity()` → **但 Entity 自身可能还没加入 EntityManager 的 active list**（取决于 Spawn 流程：先 Init 还是先加入列表？）
- 如果 Spawn 流程是"先 Init 所有组件 → 然后加入 active list"，那 AutoAim.Init 时的 SearchTarget **搜不到自己**（正确行为——不应该瞄准自己）但可能**搜不到同帧 Spawn 的友军**（如果多个 Entity 在同一帧 Spawn）

**追问**：Entity 的 Spawn 流程是"Init → 加入 active list"还是"加入 active list → Init"？这影响 AutoAim.Init 时首次 SearchTarget 的搜索结果。

如果是前者，AutoAim.Init 的 SearchTarget 可能搜不到本帧刚 Spawn 但还没加入列表的 Entity。这不是 bug（下次定频搜索就能找到），但值得文档化。

---

### SA-012：ISkillEffect 的 `[SerializeReference]` 与 WebGL/IL2CPP 的兼容性——已知 Unity 版本限制

**新发现**：TDD §九 风险表提到"SkillEffect [SerializeReference] 序列化——WebGL 下 SerializeReference 有反序列化 bug（Unity 特定版本），当前 Unity 版本已修复，真机验证确认"。

**架构层面的追问**：

`[SerializeReference]` 依赖完整的类型名（包括命名空间和 Assembly）进行反序列化。如果：
- 重命名 `FireBulletsEffect` → `FireProjectileEffect`
- 或移动命名空间 `MiniGameTemplate.Entity` → `MiniGameTemplate.Skill`
- 或重构程序集（Assembly Definition 变更）

所有已序列化的 SkillConfigSO **丢失 Effects 数据**（类型名不匹配 → 反序列化回落为 null）。

**缓解措施**：Unity 提供 `[MovedFrom]` attribute 处理类型迁移，但需要**手动标注**。

**建议**：在 §九 风险表新增：

| 风险 | 影响 | 缓解 |
|------|------|------|
| ISkillEffect 实现类重命名/移动命名空间导致 SO 数据丢失 | 已配置的 SkillConfigSO 的 Effects 反序列化为 null | 变更时使用 `[MovedFrom]` attribute；CI 中可选加 SerializationIntegrity 验证 |

---

### SA-013：BuffSlot.Duration 和 RemainingTime 的冗余——Duration 信息在 BuffConfigSO 中已有

**新发现**：`BuffSlot` 存储了：
```csharp
public float Duration;          // 总持续时间
public float RemainingTime;     // 剩余时间
```

`Duration` 仅用于 Tick 中判断"永久 Buff"（`if (_slots[i].Duration <= 0f) continue;`）。这个值在 `ApplyBuff` 时从 `BuffConfigSO.Duration` 拷贝。

**问题**：
- 如果一个 Buff 被刷新（同 ID），`RemainingTime` 被重置但 `Duration` 不会变（因为 ApplyBuff 刷新路径只改 `RemainingTime`）
- 如果**同 ID 但不同配置**的 Buff 刷新（比如"减速 Lv1"和"减速 Lv2"共用 BuffId，但 Duration 不同）——Duration 保持旧值
- 这是 bug 还是设计意图？

**代码分析**：

```csharp
// 同 ID 刷新路径
if (_slots[i].BuffId == config.BuffId)
{
    _slots[i].RemainingTime = config.Duration; // 刷新剩余时间
    return true;
    // ❌ 没有更新 _slots[i].Duration
    // ❌ 也没有更新 MoveSpeedMod/AttackIntervalMod/DamageTakenMod
}
```

**这意味着**：同 ID Buff 刷新时**只重置倒计时**，不更新属性修正值。如果"减速 Lv1"（0.5x 速度，5s）被"减速 Lv2"（0.3x 速度，8s）覆盖——持续时间变成 8s，但速度修正值**仍然是 Lv1 的 0.5x**。

**这可能是 design 也可能是 bug**，取决于"同 ID 刷新"的设计意图：
- 如果"同 ID = 完全相同的 Buff"→ 不需要更新属性（一样的）→ OK
- 如果"同 ID = 同类 Buff 可升级"→ 需要更新属性 → 当前代码是 bug

**追问**：Phase 3A 的设计意图是哪个？

---

以上 5 个追问，请守方回应。

---

## Round 2 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: SA-009 协程场景安全性 — ✅ 确认撤回 + 补充注释

攻方自行分析并得出了正确结论。在 UA-003 注释中补充协程安全说明。

**影响**：纯注释变更。

---

### RE: SA-010 SpeedModifierIds 作用域和冲突检查 — ✅ 接受

**Q1 放在哪里**：采用攻方建议——独立顶层类，放在 `_Framework/EntitySystem/Scripts/Core/SpeedModifierIds.cs`，与 `CampUtility.cs` 同级。

理由：
- ID 常量是**跨组件的公共约定**（Buff 定义 ID、Movement 消费 ID），不属于任何单一组件
- 独立文件 = 新增修正器来源时只改一个文件

**Q2 冲突检查**：Phase 3A 不做运行时检查。注释中记录：

```csharp
/// <summary>
/// MovementComponent SpeedModifier ID 注册表。
/// ⚠️ ID 唯一性由开发者保证——如果两个系统用了相同 ID，后注册的会覆盖前一个。
/// Phase 3A 只有 Buff 一个来源，冲突风险为零。
/// Phase 4+ 如增加来源，考虑在 Debug 模式下添加 ID 冲突检测（AddOrUpdate 时 Assert）。
/// </summary>
public static class SpeedModifierIds
{
    public const int Buff = 1;
    public const int Terrain = 2;      // 预留
    public const int Equipment = 3;    // 预留
}
```

**影响**：新增 `SpeedModifierIds.cs` 到文件变更清单。

---

### RE: SA-011 Init 顺序与 AutoAim 首次搜索 — ✅ 接受（文档化已有行为）

**回答追问**：Entity 的 Spawn 流程（Phase 1 TDD 已定义）是：

```
EntityPool.Get(configSO)
  → 从池中取出 Entity
  → entity.InitAll(configSO)     // 按 ComponentType 枚举顺序 Init 所有组件
  → EntityManager.Register(entity)  // 加入 active list
  → 返回 entity
```

所以：**先 Init → 后加入 active list**。

**影响分析**：
- AutoAim.Init 的 SearchTarget 搜索**当前 active list 中的 Entity**
- 本帧尚未 Register 的 Entity（包括自身和同帧 Spawn 的其他 Entity）搜不到
- 这是**正确行为**（不应瞄准自己，同帧 Spawn 的 Entity 下次搜索周期找到）

**修正**：在 AutoAim.Init 的代码注释中补充：

```csharp
// v0.4（SA-011）：Init 时 SearchTarget 搜索当前 active list。
// 此时 Entity 自身尚未加入 active list（先 Init → 后 Register），
// 因此不会瞄准自己。同帧 Spawn 的其他 Entity 也搜不到——
// 这是正确行为，下次定频搜索（0.2s 后）会找到它们。
if (IsActive) SearchTarget();
```

**影响**：纯注释变更。

---

### RE: SA-012 SerializeReference 类型重命名导致数据丢失 — ✅ 接受（风险表新增）

**结论**：这是 Unity `[SerializeReference]` 的已知陷阱，攻方的缓解措施建议完全正确。

**修正**：§九 风险表新增：

| 风险 | 影响 | 缓解 |
|------|------|------|
| ISkillEffect 实现类重命名/移动命名空间 | 已配置的 SkillConfigSO.Effects 反序列化为 null，数据丢失 | 1) 变更时标注 `[UnityEngine.Scripting.APIUpdating.MovedFrom]`；2) 重命名后检查所有 SkillConfigSO 资产；3) 命名空间确定后不轻易变更 |

**补充约束**：在 §3.3.3 ISkillEffect 接口文档中新增：

> ⚠️ ISkillEffect 实现类一经发布（有 SkillConfigSO 引用），**不得重命名类名或移动命名空间**。
> Unity `[SerializeReference]` 使用全限定类型名做序列化键。如必须重命名，使用 `[MovedFrom]` 属性做兼容映射。

**影响**：§九 风险表 + §3.3.3 约束注释。

---

### RE: SA-013 同 ID Buff 刷新时不更新属性值 — ✅ 接受（这是 design gap，需明确）

**好问题！** 让我明确设计意图：

**Phase 3A 的设计意图**：**同 ID = 完全相同的 BuffConfigSO 实例**。

理由：
- `ApplyBuff(BuffConfigSO config)` 的调用者传入的就是 SO 引用
- 正常使用场景：同一个 ApplyBuffEffect 反复对同一个 Entity 施加同一个 BuffConfigSO
- "减速 Lv1"和"减速 Lv2"**应该有不同的 BuffId**——它们是不同的 Buff

**但攻方找到了一个合法的边界情况**：如果 `_Game/` 层的自定义代码用**不同 BuffConfigSO 但相同 BuffId** 调用 ApplyBuff——这是"策划配错了"，但代码应该有**防御行为**。

**修正方案（二选一）**：

A. **严格方案**：刷新时同时更新所有字段（Duration/属性修正），"后配置覆盖前配置"
B. **保守方案**：保持只刷新 RemainingTime，但加 Debug.Assert 检查 config 是否一致

**选择方案 A**——成本很低（多赋值几个字段），语义更清晰（"刷新 = 用最新配置重置"）：

```csharp
// 同 ID 刷新路径 — v0.4（SA-013）完整刷新
if (_slots[i].BuffId == config.BuffId)
{
    _slots[i].Duration = config.Duration;                    // SA-013
    _slots[i].RemainingTime = config.Duration;               // 原有
    _slots[i].MoveSpeedMod = config.MoveSpeedModifier;       // SA-013
    _slots[i].AttackIntervalMod = config.AttackIntervalModifier; // SA-013
    _slots[i].DamageTakenMod = config.DamageTakenModifier;   // SA-013
    RecalcModifiers();                                        // SA-013：属性变了需重算
    SyncMoveSpeedToMovement();                                // SA-013：同步
    return true;
}
```

**影响**：§3.4.3 ApplyBuff 刷新路径代码修改。

---

## Round 2 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| SA-009 | ✅ 撤回 | 补充协程安全注释 |
| SA-010 | ✅ 接受 | SpeedModifierIds 独立顶层类 + 冲突注释 |
| SA-011 | ✅ 接受 | AutoAim.Init 搜索行为文档化 |
| SA-012 | ✅ 接受 | 风险表新增 SerializeReference 重命名风险 + ISkillEffect 命名约束 |
| SA-013 | ✅ 接受 | 同 ID 刷新时完整更新所有字段（方案 A） |

**Round 2 全部接受/确认。攻方是否满意收敛？**

---

## Round 3 · 攻方最终审查

> 🧱 **攻方（软件架构师）**：

Round 1-2 的回应质量极高，特别是 SA-006（AOE 循环 PendingDespawn 检查）和 SA-013（Buff 刷新完整更新）两处实质性 bug/gap 修复让我印象深刻。做最后一轮扫描：

---

### SA-014：SA-006 修正后的验收项 #17 与现有 DamageDealer 重入保护（UA-003）的关系

**确认性问题**：SA-006 新增的"AOE 循环中检查 PendingDespawn"和 UA-003 的"重入保护"覆盖了不同场景：

- UA-003：防止 OnDeath 回调**嵌套调用** DealAreaDamage（重入）→ Assert + 安全退出
- SA-006：防止循环过程中 Entity **状态变化**（非重入，而是同一次 AOE 内的连锁效应）→ 每次迭代 re-check

两者互补，不冲突。✅ 确认。

但验收矩阵新增 #17 如何测试？建议测试步骤：

> 配置一个"死亡时对周围造伤（非 AOE，走 DealDamageToEntity 单体）"的 OnDeath 回调。
> 在一堆聚集的 Enemy 上使用 AOE。验证：
> 1. AOE 正常命中所有 Enemy
> 2. 死亡的 Enemy 不再被后续迭代造伤（无 double-kill log 或 HP 变为负数）
> 3. OnDeath 单体伤害对其他 Enemy 正常生效（不被 AOE 的重入保护拦截——因为单体伤害走 DealDamageToEntity 不走 DealAreaDamage）

---

### SA-015：SA-013 修正后，刷新路径中新增 RecalcModifiers + SyncMoveSpeedToMovement 的调用——是否与 ATK-002 一致？

**确认性问题**：ATK-002（Round 2 PK）确立了"RecalcModifiers 不含 Sync，显式调用"的规范。SA-013 修正后的刷新路径：

```csharp
RecalcModifiers();
SyncMoveSpeedToMovement();
return true;
```

与 `ApplyBuff` 新增路径的模式一致（新增 Buff 时也是 `RecalcModifiers(); SyncMoveSpeedToMovement();`）。✅ 确认一致。

---

### SA-016：整体依赖方向审计——Phase 3A 新增组件的依赖图是否有环

**最终审查**：列出 Phase 3A 所有新增组件/类的依赖方向：

```
依赖方向图（箭头 = "知道/依赖"）：

SkillComponent → ISkillEffect（接口）
SkillComponent → SkillConfigSO（配置）
SkillComponent → AutoAimComponent（via ITargetProvider 接口）
SkillComponent → IDecisionMaker（via 接口）

FireBulletsEffect → DanmakuSystem（发射弹幕）
AreaDamageEffect → DamageDealer（AOE 直伤）
AreaDamageEffect → CampUtility（阵营查询）
ApplyBuffEffect → BuffComponent（施加 Buff）
ApplyBuffEffect → CampUtility
ApplyBuffEffect → EntityManagerAccessor（空间查询）

DamageDealer → EntityManagerAccessor
DamageDealer → HealthComponent（造伤）

BuffComponent → MovementComponent（push SpeedModifier）
BuffComponent → SpeedModifierIds（常量）

AttackComponent → AutoAimComponent（瞄准方向）
AttackComponent → BuffComponent（pull AttackIntervalModifier）

AutoAimComponent → EntityManagerAccessor
AutoAimComponent → CampUtility

CampUtility → （无依赖）
SpeedModifierIds → （无依赖）
EntityManagerAccessor → EntityManager（单例包装）
```

**环检测**：没有环 ✅

**层级分析**：
```
底层（无依赖）：CampUtility, SpeedModifierIds
基础设施层：EntityManagerAccessor → EntityManager
组件层：MovementComponent, HealthComponent, AutoAimComponent, BuffComponent, AttackComponent, SkillComponent
效果层：FireBulletsEffect, AreaDamageEffect, ApplyBuffEffect
工具层：DamageDealer
配置层：SkillConfigSO, BuffConfigSO, EntityConfigSO
```

依赖方向：效果层 → 工具层 → 基础设施层 → 底层。**没有逆向依赖**。✅ 合格。

**唯一的"横向依赖"**：
- BuffComponent → MovementComponent（push）
- AttackComponent → BuffComponent（pull）

这两个都是"组件层内部的横向协作"，方向一致（上层组件知道下层组件），可接受。

---

### SA-017：最终建议——TDD 应新增"依赖方向约束"章节

**建议**：将上面的依赖方向图写入 TDD，作为**架构约束**（不是可选建议）。Phase 4+ 新增组件时，开发者必须检查不引入环依赖。

这是一个纯文档建议，不影响 Phase 3A 实施。

---

**总结**：Round 3 提出 4 个点，全部是确认性/文档性的。无新的实质性缺陷。

**攻方声明**：如果守方接受 SA-014 测试步骤补充和 SA-017 依赖方向图建议，我满意收敛。

---

## Round 3 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: SA-014 验收项 #17 测试步骤 — ✅ 接受

验收矩阵 #17 补充测试步骤如攻方所述。三个验证点都准确。

**修正**：验收矩阵 #17 完整描述：

| # | 测试项 | 通过条件 | 步骤 |
|---|--------|---------|------|
| 17 | AOE 连锁击杀安全性 | 1) AOE 正常命中范围内 Entity；2) 循环中已死亡 Entity 不被二次造伤（无 HP 变负/double-kill）；3) OnDeath 单体伤害回调正常生效（不被 AOE 重入保护拦截） | P3.2 |

---

### RE: SA-015 刷新路径与 ATK-002 一致性 — ✅ 确认

攻方的一致性审计正确。SA-013 修正后的刷新路径与 ATK-002 规范完全一致。无需额外修改。

---

### RE: SA-016 依赖方向审计 — ✅ 确认无环

攻方的依赖图分析完整准确。确认 Phase 3A 新增的所有组件/类之间无环依赖。

---

### RE: SA-017 TDD 新增"依赖方向约束"章节 — ✅ 接受

在 TDD §八（架构决策摘要）之后新增 **§八c 依赖方向约束**：

将攻方整理的依赖方向图和层级分析写入 TDD，标注为**架构约束**：

> **Phase 3A 依赖方向约束**
> 
> 依赖方向必须遵循：效果层 → 工具层 → 组件层 → 基础设施层 → 底层。
> 不允许逆向依赖。组件层内部允许横向协作（如 Buff→Movement push、Attack→Buff pull），
> 但必须在 TDD 中文档化。
> 
> Phase 4+ 新增组件/效果时，必须更新此依赖图并验证无环。

**影响**：纯文档新增。

---

## Round 3 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| SA-014 | ✅ 接受 | 验收 #17 补充完整测试步骤 |
| SA-015 | ✅ 确认 | 一致性审计通过 |
| SA-016 | ✅ 确认 | 无环依赖 |
| SA-017 | ✅ 接受 | 新增 §八c 依赖方向约束 |

---

## 🏁 PK 收敛声明

> **攻方（软件架构师）**：3 轮审查，17 个问题（14 个实质 + 3 个自行撤回/确认），全部收敛。满意。
> **守方（Unity 架构师）**：所有接受项将与 Round 2/3 的修正一并回写 TDD v0.4。

### PK 统计

| 维度 | 数据 |
|------|------|
| 总轮次 | 3 轮（最大 6 轮） |
| 总问题数 | 17（实质 14 + 撤回/确认 3） |
| 接受 | 10 |
| 部分接受 | 3（SA-001 保持静态 + 诊断、SA-003 保留混合 + 常量类、SA-008 二元阵营 + 未决项） |
| 拒绝 | 0 |
| 自行撤回/确认 | 3（SA-009 协程安全、SA-015 一致性确认、SA-016 无环确认） |
| Bug 发现 | 1（SA-006：AOE 循环中未检查 PendingDespawn） |
| Design Gap 发现 | 1（SA-013：Buff 刷新时属性未更新） |
| 收敛率 | 100% |

### 需回写 TDD 的修正项清单

| ID | 修正内容 | 影响范围 |
|----|---------|---------|
| SA-001 | DamageDealer 设计决策文档化（静态工具类 + 不支持 mock + Debug.Assert） | §3.2.1 |
| SA-002 | ISkillEffect 强制无状态约束（共享实例不允许可变状态） | §3.3.1 + §3.3.3 |
| SA-003 | SpeedModifierIds 常量类 + push/pull 混合模式理由文档化 | §3.4.4 + 新增 SpeedModifierIds.cs |
| SA-004 | 所有 EntityManagerAccessor.Instance null 检查点添加 Debug.Assert | §3.1.4 + §3.2.2 + §3.4.6 |
| SA-005 | SkillComponent 显式状态转换矩阵 | §3.3.5（新增小节） |
| SA-006 | DealAreaDamage 循环中添加 PendingDespawn/IsAlive 检查（**bug fix**） | §3.2.2 |
| SA-007 | SkillContext XML Doc 声明 struct + 引用类型混合语义 | §3.3.3 |
| SA-008 | CampUtility 二元阵营定位声明 + §十 新增 Phase 5 多阵营未决项 | §3.1.3 + §十 |
| SA-009 | UA-003 注释补充协程安全说明 | §3.2.2 |
| SA-010 | 新增 SpeedModifierIds.cs + 文件变更清单更新 | §十一 |
| SA-011 | AutoAim.Init SearchTarget 搜索行为文档化（先 Init 后 Register） | §3.1.4 |
| SA-012 | §九 风险表新增 SerializeReference 重命名风险 + ISkillEffect 命名约束 | §九 + §3.3.3 |
| SA-013 | Buff 同 ID 刷新时完整更新所有字段（Duration + 属性修正）（**design gap fix**） | §3.4.3 |
| SA-014 | 验收矩阵 #17 补充完整测试步骤（AOE 连锁击杀安全性） | §七 |
| SA-017 | 新增 §八c 依赖方向约束（依赖图 + 层级约束） | §八c（新增） |

### 最具价值发现 TOP 5

1. **SA-006**（⭐ Bug）：DealAreaDamage 循环中未检查 PendingDespawn → OnDeath 连锁反应可导致对已死亡 Entity 二次造伤
2. **SA-013**（⭐ Design Gap）：Buff 同 ID 刷新路径只更新倒计时不更新属性值 → "同类 Buff 升级"场景下属性不变
3. **SA-002**（架构约束）：ISkillEffect 共享实例的有状态陷阱 → 强制无状态约束
4. **SA-005**（文档完整性）：SkillComponent 缺少显式状态转换矩阵 → Phase 4 扩展必需
5. **SA-012**（风险识别）：SerializeReference 类型重命名导致数据丢失 → 命名锁定约束

**PK 状态**：✅ **已收敛 — 待与 Round 2/3 修正合并回写 TDD v0.4**
