# PK 评审记录 — PHASE3A_TDD.md

> **目标文档**：`Docs/Agent/PHASE3A_TDD.md`
> **文档类型**：TDD
> **攻方角色**：Unity 架构师（10年+经验，专精 Unity 运行时性能、WebGL 平台限制、弹幕系统实现）
> **守方角色**：软件架构师（专精系统设计、API 设计、可维护性和关注点分离）
> **开始时间**：2026-05-01 23:55
> **最大轮次**：6
> **PK 状态**：✅ 已收敛（2 轮 / 最大 6 轮）  
> **TDD 版本**：v0.3（所有修正已回写到 TDD 文档本体）

---

## PK Round 1 — 攻方提问

### UA-001 | 严重度 🔴高 | AutoAim TickOrder=200 在 Attack=150 之后，导致瞄准方向滞后一帧

**涉及章节**：§3.1.4、§3.1.5、§五
**质疑**：AutoAim 的 TickOrder=200 执行在 Attack TickOrder=150 之后。但 AttackComponent.GetFireAngle() 需要读取 AutoAimComponent.AimDirection 来确定发射方向。这意味着 AttackComponent 在帧 N 发射弹幕时，读取的是帧 N-1 的 AutoAim 锁定方向（因为帧 N 的 AutoAim 还没 Tick）。文档在 §3.1.7 的设计理由中提到"AutoAim 在 Attack 之后（200）：已有预留，下帧生效不影响体验"，但这与 BC-10.4 行为契约（"AttackComponent 发射方向优先级：AutoAim 锁定方向 > DecisionCommand.AimDirection"）的语义期望存在矛盾——既然 AutoAim 是最高优先级瞄准源，逻辑上应该在 Attack 之前更新。
**潜在风险**：
1. 在目标高速移动场景下，一帧延迟导致瞄准偏差
2. 首帧 Entity 生成后 AutoAim 还没执行过 Tick（AimDirection=Vector2.up），Attack 可能在第一帧发射朝上的弹幕
3. 与"不影响体验"的说法缺少论据
**建议方向**：将 AutoAim 的 TickOrder 改为 Attack 之前（如 120 或 140），使 Attack 每帧拿到最新瞄准方向。或在 Init 阶段立即执行一次 SearchTarget 避免首帧朝向错误。
**状态**：🟡 待回应

---

### UA-002 | 严重度 🔴高 | EntityPool.CreateComponent 工厂缺失 AutoAim/Skill/Buff case 的具体代码

**涉及章节**：§3.4.5、§四、§十一
**质疑**：现有 `EntityPool.CreateComponent` 工厂方法的 switch 只覆盖 State/Health/Movement/Collision/Control/AI/Attack/Animation。文档在"文件变更清单"中列了 `EntityPool.cs | 组件工厂补充 AutoAim / Skill / Buff case`，但未给出具体实现代码片段。特别是 ComponentType.Buff=10 需要新增枚举值，且 `_components` 数组长度为 `ComponentType.MAX=16`，Buff=10 在范围内但代码中未体现。
**潜在风险**：实施时如果遗漏 EntityPool 工厂更新，EntityConfigSO.Components 配置了 AutoAim/Skill/Buff 的 Entity 将触发 `[EntityPool] 未知组件类型：XXX，跳过创建`，组件为 null 静默失败。
**建议方向**：在 TDD 中补充完整的 EntityPool.CreateComponent switch 变更代码。
**状态**：🟡 待回应

---

### UA-003 | 严重度 🟡中 | DamageDealer 静态 buffer 嵌套调用安全隐患（重入风险）

**涉及章节**：§3.2.2
**质疑**：`DamageDealer._buffer` 是 `private static readonly Entity[64]`。如果 `DealAreaDamage` 遍历 _buffer 时，TakeDamage → OnDeath 事件 → 某回调再次调用 `DealAreaDamage`（如死亡触发爆炸技能 → 再 AOE），则静态 buffer 被覆盖，正在遍历的数据损坏。当前 Phase 3A 的 SkillEffect 设计支持在 OnDeath 挂载 ISkillEffect，这条路径完全可能发生。
**潜在风险**：未定义行为（UB）——buffer 数据被覆盖导致错误的 Entity 被伤害或崩溃。
**建议方向**：加入重入保护（bool _isProcessing flag + Debug.Assert），并在行为契约中明确标注"DamageDealer 不支持嵌套调用"。或者在遍历前将 count + Entity ID 拷贝到栈上 Span。
**状态**：🟡 待回应

---

### UA-004 | 严重度 🟡中 | SkillContext.DeltaTime 字段未在 ExecuteEffects 中赋值

**涉及章节**：§3.3.5
**质疑**：`SkillContext` 定义了 `DeltaTime` 字段用于 Passive 模式，但 `SkillComponent.ExecuteEffects()` 构造 ctx 时没有赋值 `DeltaTime = dt`——因为 ExecuteEffects 没有接收 dt 参数。Passive 模式技能在 Idle 状态每帧触发 ShouldTrigger()=true → 执行 ExecuteEffects，但 DeltaTime 永远是 0。
**潜在风险**：Passive 类型的 ISkillEffect（如光环持续伤害）读取 ctx.DeltaTime 恒为 0，行为错误。
**建议方向**：`ExecuteEffects` 改为 `ExecuteEffects(float dt)`，在 ctx 中正确赋值。同时 Passive 模式的触发逻辑需要调整——当前实现 Passive 触发后进入 Recovery → CD → 再触发，并非"每帧执行"。需要澄清 Passive 语义。
**状态**：🟡 待回应

---

### UA-005 | 严重度 🟡中 | SkillComponent Passive 模式语义与实现不匹配

**涉及章节**：§3.3.2、§3.3.5
**质疑**：`SkillTriggerMode.Passive` 注释说"始终生效（Tick 每帧驱动）"，但实际实现中 Passive 和 Auto 行为完全相同——都是 `ShouldTrigger() → return true`。触发后执行效果 → 进入 Recovery → Cooldown → 回到 Idle 再触发。这不是"每帧驱动"，而是"CD 就绪就触发"，与 Auto 模式无区别。
**潜在风险**：如果真要实现"每帧光环"类效果（如 DOT 领域），当前状态机完全不支持——它总要经过 Recovery/CD 循环。
**建议方向**：明确 Passive 的设计意图。如果 Passive="CD 就绪就自动触发"则与 Auto 合并；如果 Passive="每帧执行不走 CD"则需要特殊分支（跳过状态机，直接 Tick 中 Execute）。
**状态**：🟡 待回应

---

### UA-006 | 严重度 🟡中 | MovementComponent 已有 SpeedModifier 系统，Buff 修正的集成路径不明确

**涉及章节**：§3.4.4
**质疑**：现有 `MovementComponent` 已经有一套完整的 SpeedModifier 系统（AddSpeedModifier/RemoveSpeedModifier，最多 4 个）。TDD 中 BuffComponent 的 `MoveSpeedModifier` 建议在 MovementComponent.Tick 中直接读取：`effectiveSpeed = baseSpeed * buffComp.MoveSpeedModifier`。但这**绕过了**已有的 Modifier 数组！最终速度应该是 `GetFinalSpeed() * buffComp.MoveSpeedModifier` 还是只用其中一个？两套修正系统并存会造成混乱。
**潜在风险**：策划配了 SpeedModifier 又配了 Buff，效果叠加顺序不可预测，调试困难。
**建议方向**：明确二者关系——要么 Buff 通过已有的 `AddSpeedModifier` 系统注入（Buff Apply 时 Add、Remove 时 Remove），要么废弃旧系统统一用 BuffComponent。建议采用前者（Buff 通过 Modifier 系统注入），保持 MovementComponent 接口不变。
**状态**：🟡 待回应

---

### UA-007 | 严重度 🟢低 | GetHostileCamp 工具方法定义位置不一致

**涉及章节**：§3.1.3、§3.1.4、§3.4.6
**质疑**：`GetHostileCamp` 在 §3.1.3 说"可放在 EnumCamp 扩展或 EntityManager 中"，在 §3.1.4 AutoAimComponent 中以 `private static` 方式定义了一份，在 §3.4.6 ApplyBuffEffect 中以 `AutoAimComponent.GetHostileCamp` 方式调用。一个工具方法出现在具体组件中会产生不合理的依赖（ApplyBuffEffect → AutoAimComponent）。
**潜在风险**：未来如果没有配 AutoAim 的 Entity 要用 ApplyBuffEffect，编译不会报错但语义上产生"为什么 BuffEffect 依赖 AutoAim？"的困惑。
**建议方向**：将 `GetHostileCamp` 提取为独立工具类 `CampUtility` 或 `EnumCampExtensions`，所有需要的地方统一调用。
**状态**：🟡 待回应

---

### UA-008 | 严重度 🟢低 | BuffComponent.Init 无条件设 IsActive=true，但未关联 EntityConfigSO 条件

**涉及章节**：§3.4.3
**质疑**：AutoAimComponent 的 Init 中 `IsActive = _searchRadius > 0f`（配置为 0 则不激活），SkillComponent 的 Init 中 `IsActive = _config != null`（无配置则不激活）。但 BuffComponent 的 Init 中无条件 `IsActive = true`。如果 Entity 的 Components 数组配了 Buff 但实际不需要 Buff 系统（纯装饰物），BuffComponent 仍然会 Tick（虽然空循环代价极小）。
**潜在风险**：极低——空循环几乎没有性能开销。但与其他组件的 Init 模式不一致。
**建议方向**：保持现状即可（Buff 无"配置依赖"，挂了就说明需要），但建议在注释中说明"BuffComponent 挂载即激活，策划确保只有需要 Buff 的 Entity 才配"。
**状态**：🟡 待回应

---

> **攻方整体评估**：
> 
> Phase 3A TDD 整体质量高，设计支柱清晰、契约定义完整、代码细节丰富。核心阻塞项有 2 个（UA-001 TickOrder 时序矛盾、UA-002 工厂代码缺失），3 个中度问题需要在实施前澄清（DamageDealer 重入、Passive 语义、Modifier 集成），2 个低优可编码期间修正。文档"足够好可以开始实施"，但需先解决 TickOrder 时序这个根本性设计决策。

---

## PK Round 1 — 守方回应

> **守方角色**：软件架构师（专精系统设计、API 设计、可维护性和关注点分离）

### UA-001 回应 | ✅ 接受并修正

**处置**：完全采纳攻方建议。
1. AutoAim TickOrder 从 200 → **120**（在 Attack=150 之前）
2. Init 时立即执行一次 `SearchTarget()` + 更新 AimDirection，消除首帧偏差
3. 时序图全文更新

**修正位置**：TDD v0.2 §3.1.4、§3.1.7、§五
**状态**：✅ 收敛

---

### UA-002 回应 | ✅ 接受并补充

**处置**：在 §3.4.5b 补充了完整的 EntityPool.CreateComponent switch case 代码。三个 case 逐一列出。
**状态**：✅ 收敛

---

### UA-003 回应 | ✅ 接受并修正

**处置**：采纳 Debug.Assert + bool flag 方案。
- 新增 `_isProcessingArea` 重入保护字段
- Assert 给出明确的错误信息（提示检查 OnDeath 回调链）
- Release 模式下 fallback 安全退出（return 0）
- 行为约束写入 XML Doc：「不支持嵌套调用」

**设计取舍**：未采用 Span 拷贝方案——WebGL/IL2CPP 对 stackalloc > 64 元素的支持不稳定，且 flag 方案已足够安全。

**修正位置**：TDD v0.2 §3.2.2
**状态**：✅ 收敛

---

### UA-004 回应 | ✅ 接受并修正

**处置**：`ExecuteEffects()` → `ExecuteEffects(float dt)`，ctx.DeltaTime 正确赋值。
所有调用点（Idle→Execute、Casting→Execute）均传入 dt。
**状态**：✅ 收敛

---

### UA-005 回应 | ✅ 接受并简化

**处置**：**移除 Passive 模式**。理由：
1. Passive 与 Auto 行为完全相同（CD 就绪 → 触发 → Recovery → CD → 循环）
2. 真正的"每帧光环"效果（DOT/Aura）不适合用 SkillComponent 状态机实现
3. 留给 Phase 4 的 AuraComponent 或 Buff.DOT 扩展

**SkillTriggerMode** 简化为 Manual(0) / Auto(1) 两个值。

**修正位置**：TDD v0.2 §3.3.2、§3.3.5
**状态**：✅ 收敛

---

### UA-006 回应 | ✅ 接受并重设计

**处置**：Buff 速度修正**不再绕过**已有的 SpeedModifier 系统。改为：
- BuffComponent.RecalcModifiers() 末尾调用 `SyncMoveSpeedToMovement()`
- 通过 `movement.AddSpeedModifier(BUFF_MODIFIER_ID=99, value)` 注入保留槽位
- MoveSpeedModifier == 1f 时自动 Remove

**设计原则**：MovementComponent 保持单一速度修正接口，Buff 作为修正器的一个来源注入而非旁路。`GetFinalSpeed()` 语义始终一致。

**修正位置**：TDD v0.2 §3.4.4
**状态**：✅ 收敛

---

### UA-007 回应 | ✅ 接受并重构

**处置**：`GetHostileCamp` 提取为独立工具类 `CampUtility`（路径：`_Framework/EntitySystem/Scripts/Core/CampUtility.cs`）。
- AutoAimComponent.SearchTarget → `CampUtility.GetHostileCamp()`
- AreaDamageEffect.Execute → `CampUtility.GetHostileCamp()`
- ApplyBuffEffect.Execute → `CampUtility.GetHostileCamp()`
- AutoAimComponent 中保留 private static 版本作为临时兼容，实施时可直接删除

**修正位置**：TDD v0.2 §3.1.3、§3.1.4、§3.3.4、§3.4.6、文件变更清单
**状态**：✅ 收敛

---

### UA-008 回应 | 📝 接受（仅注释）

**处置**：在 Init 中添加注释说明设计意图：
> "BuffComponent 挂载即激活，策划确保只有需要 Buff 系统的 Entity 才在 Components 中配 Buff。空 Tick 开销可忽略。"

不做代码逻辑变更。

**修正位置**：TDD v0.2 §3.4.3
**状态**：✅ 收敛

---

> **守方 Round 1 总结**：
> 
> 8 个问题全部接受（含 2 个仅注释/说明级）。TDD v0.2 已完成所有修正。
> 主要设计变更：
> 1. TickOrder 时序修正（AutoAim 120，Attack 之前）
> 2. Passive 模式废弃，简化为 Manual/Auto 二选一
> 3. Buff 速度修正通过已有 SpeedModifier 系统注入
> 4. GetHostileCamp 提取为 CampUtility 独立工具类
> 5. DamageDealer 重入保护
> 6. ExecuteEffects 增加 dt 参数

---

## PK Round 2 — 攻方复审

### Round 1 修正验证

| ID | 修正状态 | 验证说明 |
|----|---------|---------|
| UA-001 | ⚠️ | TDD 内部一致（§3.1.4/§3.1.7/§五），但文件变更清单漏标「AutoAim 从 200→120」（现有代码 ITickable.cs 中仍是 200） |
| UA-002 | ✅ | §3.4.5b 完整补充三个 case |
| UA-003 | ✅ | 重入保护逻辑正确 |
| UA-004 | ✅ | ExecuteEffects(dt) 签名+调用点全部修正 |
| UA-005 | ✅ | Passive 移除，Manual(0)/Auto(1) 无空洞 |
| UA-006 | ⚠️ | 设计思路正确，但 TDD 代码使用了不存在的 by-ID API |
| UA-007 | ✅ | CampUtility 统一引用 |
| UA-008 | ✅ | 注释说明到位 |

---

### UA-009 | 严重度 🔴高 | BuffComponent.SyncMoveSpeedToMovement 使用了不存在的 API 签名

**涉及章节**：§3.4.4
**质疑**：`SyncMoveSpeedToMovement` 中使用了 `movement.AddSpeedModifier(BUFF_MODIFIER_ID, MoveSpeedModifier)` 和 `movement.RemoveSpeedModifier(BUFF_MODIFIER_ID)` 两个调用（by-ID 语义）。但现有 `MovementComponent` 真实接口是：
- `AddSpeedModifier(float multiplier)` → 返回 `int` slot 索引
- `RemoveSpeedModifier(int slot)` → 按 slot 索引 swap-remove

TDD 假设的 by-ID 语义与现有 by-slot 语义完全不同。
**潜在风险**：实施时直接粘贴 TDD 代码会编译失败。
**建议方向**：明确声明需要新增 `AddOrUpdateSpeedModifier(int id, float multiplier)` + `RemoveSpeedModifierById(int id)` 重载，或改为适配现有 by-slot 接口。
**状态**：🟡 待回应

---

### UA-010 | 严重度 🟡中 | DamageDealer._isProcessingArea 在异常路径下不会 reset

**涉及章节**：§3.2.2
**质疑**：如果循环中 `health.TakeDamage(ref ctx)` 抛异常（IDamageModifier 自定义修正器 bug），`_isProcessingArea` 永远为 true，后续所有 AOE 伤害永久失效。
**建议方向**：try/finally 确保 reset，或循环体内捕获单个 TakeDamage 异常。
**状态**：🟡 待回应

---

### UA-011 | 严重度 🟡中 | FindNearestEntity._sharedSearchBuffer 缺少文档化原子性声明

**涉及章节**：§3.1.2
**质疑**：`FindNearestEntity` 是原子性操作（调用→遍历→返回最近），不会在中间触发用户回调，因此当前安全。但文档未声明此保证。
**建议方向**：在 XML Doc 中明确标注「此方法使用静态共享 buffer，返回后 buffer 内容可能被覆盖——不要缓存 buffer 引用」。
**状态**：🟡 待回应

---

### UA-012 | 严重度 🟡中 | BuffComponent.Reset 中 RecalcModifiers → SyncMoveSpeedToMovement 的时序问题

**涉及章节**：§3.4.3、§3.4.4
**质疑**：Entity.ResetAll() 按 ComponentType 枚举顺序（0→15）遍历调用 Reset()。Movement=3 在 Buff=10 之前 Reset。当 BuffComponent.Reset() 调用时 MovementComponent 已经 Reset（_modifierCount=0），SyncMoveSpeedToMovement 中的 RemoveSpeedModifier 变成空操作。
**建议方向**：BuffComponent.Reset() 不调用 SyncMoveSpeedToMovement——ResetAll 会清理所有组件状态。明确标注：「Reset 场景下 MovementComponent 由其自身 Reset 清理，BuffComponent 不需要主动撤销」。
**状态**：🟡 待回应

---

### UA-013 | 严重度 🟡中 | 文件变更清单对 ITickable.cs 的 AutoAim 修改遗漏

**涉及章节**：§十一
**质疑**：变更清单写「新增 Buff=50 / Skill=160」，漏掉「修改 AutoAim = 200 → 120」。AttackComponent.cs 也硬编码了 `TickOrder => 150`（未用常量）。
**建议方向**：变更清单补充 AutoAim 修改说明，建议 AttackComponent 改用 `TickOrders.Attack`。
**状态**：🟡 待回应

---

### UA-014 | 严重度 🟢低 | AutoAimComponent 保留无用的 private static GetHostileCamp（死代码）

**涉及章节**：§3.1.4
**质疑**：SearchTarget() 已改用 CampUtility.GetHostileCamp，private static 版本完全是死代码。「向下兼容 fallback」不成立——CampUtility 是 P3.1 同步新建的文件。
**建议方向**：TDD 中直接删除该方法。
**状态**：🟡 待回应

---

### UA-015 | 严重度 🟢低 | SkillContext.DeltaTime 注释引用已废弃的 Passive 模式

**涉及章节**：§3.3.3
**质疑**：SkillContext.DeltaTime 注释写「Passive 模式用」，但 Passive 已被 UA-005 移除。
**建议方向**：更新注释为「供需要时间感知的 ISkillEffect 扩展使用」。
**状态**：🟡 待回应

---

> **攻方 Round 2 总结**：
> 
> Round 1 修正质量高，8 项中 6 项完全到位。新发现 7 个问题（1🔴 + 4🟡 + 2🟢）。
> **核心阻塞**：UA-009 MovementComponent API 签名不匹配，需在实施前明确接口变更方案。
> 其他问题可在编码阶段处理。建议解决 UA-009 + UA-013 后收敛。

---

## PK Round 2 — 守方回应

### UA-009 回应 | ✅ 接受并补充 MovementComponent 接口扩展

**处置**：攻方正确——现有 `MovementComponent` 只有 by-slot 接口。需要新增 by-ID 重载以支持 Buff 系统。

**新增 API 设计**：
```csharp
// MovementComponent.cs — 新增 by-ID 重载（P3.4 实施时添加）

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

// 新增字段：
private readonly int[] _modifierIds = new int[MAX_MODIFIERS]; // 与 _speedModifiers 对应
```

**SyncMoveSpeedToMovement 修正**：
```csharp
private void SyncMoveSpeedToMovement()
{
    var movement = _owner.GetComponent(ComponentType.Movement) as MovementComponent;
    if (movement == null) return;
    
    const int BUFF_MODIFIER_ID = 99;
    if (Mathf.Approximately(MoveSpeedModifier, 1f))
        movement.RemoveSpeedModifierById(BUFF_MODIFIER_ID);
    else
        movement.AddOrUpdateSpeedModifier(BUFF_MODIFIER_ID, MoveSpeedModifier);
}
```

**向下兼容**：原有 `AddSpeedModifier(float)` / `RemoveSpeedModifier(int slot)` 保留不变——by-slot API 内部给 id=-1（匿名），不与 by-ID 冲突。现有测试无需修改。

**文件变更清单更新**：`MovementComponent.cs` 变更内容改为「新增 _modifierIds 字段 + AddOrUpdateSpeedModifier / RemoveSpeedModifierById 重载 + Buff 速度集成」。

**状态**：✅ 收敛

---

### UA-010 回应 | ✅ 接受，采用 try/finally

**处置**：
```csharp
_isProcessingArea = true;
try
{
    int count = mgr.FindEntitiesInRadius(...);
    for (int i = 0; i < count; i++) { ... }
}
finally
{
    _isProcessingArea = false;
}
```
WebGL/IL2CPP 下 try/finally 无 GC 开销（只有 try/catch + Exception 对象才产生 GC），安全采用。

**状态**：✅ 收敛

---

### UA-011 回应 | ✅ 接受，补充文档声明

**处置**：在 `FindNearestEntity` XML Doc 中追加：
> 「注意：内部使用静态共享 buffer，方法返回后 buffer 内容可能被后续调用覆盖。调用者应立即使用返回的 Entity 引用，不要保存 buffer 地址。」

**状态**：✅ 收敛

---

### UA-012 回应 | ✅ 接受，Reset 不调用 Sync

**处置**：明确设计约定——`BuffComponent.Reset()` 只清自身状态，不调用 `SyncMoveSpeedToMovement()`。原因：
1. ResetAll 按枚举顺序遍历，Movement 先 Reset（_modifierCount=0）
2. BuffComponent.Reset 时 Movement 已清空，调 Sync 是空操作
3. 「谁分配谁释放」原则应用到 Reset 语境：每个组件只管自己的 Reset

RecalcModifiers 末尾的 `SyncMoveSpeedToMovement()` 只在正常运行时调用（ApplyBuff / RemoveBuff / Tick 到期时），不在 Reset 路径调用。

**状态**：✅ 收敛

---

### UA-013 回应 | ✅ 接受并修正变更清单

**处置**：文件变更清单更新为：
`ITickable.cs (TickOrders) | **修改** AutoAim = 200 → 120，新增 Buff = 50 / Skill = 160`

AttackComponent 硬编码 150 的问题：建议在 P3.1 实施时改为 `TickOrders.Attack`（一行改动，低风险）。写入变更清单。

**状态**：✅ 收敛

---

### UA-014 回应 | ✅ 接受，删除死代码

**处置**：TDD 中直接删除 AutoAimComponent 内的 `private static GetHostileCamp` 方法。SearchTarget 已用 `CampUtility.GetHostileCamp`，无需兼容 fallback。

**状态**：✅ 收敛

---

### UA-015 回应 | ✅ 接受，更新注释

**处置**：SkillContext.DeltaTime 注释改为：
```csharp
public float DeltaTime;  // 当前帧 dt（供需要时间感知的 ISkillEffect 扩展使用）
```
移除 Passive 模式引用。

**状态**：✅ 收敛

---

> **守方 Round 2 总结**：
> 
> 7 个问题全部接受。主要设计变更：
> 1. MovementComponent 新增 by-ID 接口（AddOrUpdateSpeedModifier / RemoveSpeedModifierById）
> 2. DamageDealer 使用 try/finally 保护重入 flag
> 3. BuffComponent.Reset 不调用 SyncMoveSpeedToMovement
> 4. 文件变更清单补齐 AutoAim 常量修改
> 5. 删除 AutoAimComponent 死代码
> 6. SkillContext.DeltaTime 注释更新
>
> 至此所有 15 个问题已收敛。TDD v0.3 可以开始实施。

---

## PK 收敛裁决

| 轮次 | 攻方问题数 | 守方全接受 | 新增阻塞 |
|------|-----------|-----------|---------|
| Round 1 | 8 (2🔴+4🟡+2🟢) | ✅ 8/8 | 0 |
| Round 2 | 7 (1🔴+4🟡+2🟢) | ✅ 7/7 | 0 |

**总计**：15 个问题，全部收敛。无未解决分歧。

**PK 结论**：文档质量已达到「足够好可以开始实施」标准。TDD 升级至 v0.3。

> **PK 状态**：✅ 已收敛（2 轮 / 最大 6 轮）




