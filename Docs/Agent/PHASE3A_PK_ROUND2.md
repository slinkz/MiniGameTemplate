# Phase 3A TDD v0.3 — PK 评审（Round 2）

> **攻方角色**：🔧 Unity 工具开发者 Agent（关注：Editor 体验、调试便利、序列化陷阱、Inspector 集成、日常工作流）
> **守方角色**：🏗️ Unity 架构师 Agent（关注：架构一致性、性能约束、扩展路径、设计原则、零 GC）
> **最大轮次**：6 轮
> **PK 状态**：✅ 已收敛（3 轮 / 最大 6 轮）
> **TDD 版本**：v0.3（基于 PK Round 1+2 收敛版本）

---

## Round 1 · 攻方发难

> 🔧 **攻方（Unity 工具开发者）**：

我仔细审读了 Phase 3A TDD v0.3 全文。作为一个日常在 Unity Editor 中工作、需要调试这些系统的开发者，我有以下问题和顾虑：

---

### ATK-001：SkillConfigSO.Effects 使用 [SerializeReference] 但无自定义 Inspector

**问题**：`SkillConfigSO` 的 `Effects` 字段使用了 `[SerializeReference] ISkillEffect[]`。Unity 默认 Inspector 对 SerializeReference 的支持非常糟糕——字段显示为一堆灰色的 Managed Reference，无法直观添加/删除/重排序具体实现类型。

**影响**：
- 策划无法通过 Inspector 直观配置技能效果
- 调试时无法快速看到每个 Effect 的具体类型和参数
- 无自定义 Drawer 的情况下，几乎不可用

**追问**：TDD 中 `EntityConfigSOEditor.cs` 被列入修改文件清单（"补齐新字段绘制"），但没有给 SkillConfigSO 本身的 Editor 任何设计。是否缺少一个 `SkillConfigSOEditor.cs` 的规格？还是计划使用第三方库（如 Odin Inspector）？

---

### ATK-002：BuffComponent 的 RecalcModifiers 末尾调 SyncMoveSpeedToMovement 的时机问题

**问题**：TDD §3.4.4 说"RecalcModifiers 末尾调用 SyncMoveSpeedToMovement()"，但代码中 `RecalcModifiers()` 在三个地方被调用：
1. `Init()` → `RecalcModifiers()`
2. `Reset()` → `RecalcModifiers()`
3. `ApplyBuff()` → `RecalcModifiers()`
4. `RemoveBuff()` → `RecalcModifiers()`
5. `Tick()` → `RecalcModifiers()`（dirty 时）

UA-012 说"Reset 时不调 Sync"，但实现上 RecalcModifiers 内部如何区分"我在 Reset 路径"还是"正常运行时路径"？

**建议方案**：
- A：RecalcModifiers 不调 Sync，在 ApplyBuff/RemoveBuff/Tick 的 dirty 路径后**显式调用** SyncMoveSpeedToMovement
- B：RecalcModifiers 加一个 bool 参数 `syncMovement = true`，Reset 路径传 false

哪个方案是 TDD 的意图？需要明确写入 TDD 以消除实施歧义。

---

### ATK-003：DamageDealer._buffer 是 static readonly，但 FindEntitiesInRadius 使用调用方传入的 buffer

**问题**：`DamageDealer` 内部有 `private static readonly Entity[] _buffer = new Entity[64]`，DealAreaDamage 将此 buffer 传给 `EntityManager.FindEntitiesInRadius(center, radius, camp, _buffer, ...)`。

但 `FindNearestEntity` 也有自己的 `_sharedSearchBuffer`。如果 `DealAreaDamage` 的 for 循环中、某个 `TakeDamage` 触发 OnDeath 回调、回调中又调用了 `FindNearestEntity`——`_sharedSearchBuffer` 会被覆盖，但因为用的是**不同的 buffer**（DamageDealer 用自己的 `_buffer`），所以不会冲突。

**但反过来**：如果未来某个 OnDeath 回调调用了 `DealAreaDamage`（嵌套 AOE），现在 UA-003 的重入保护会拒绝并返回 0，`Debug.Assert` 会触发。这是正确的。

**真正的问题**：UA-003 的 `Debug.Assert` 在 Release 构建中被编译移除。TDD 说"Release 模式下 fallback 安全退出"，但 `_isProcessingArea` 检查在 `Debug.Assert` **之后**的 `if` 语句中——这没问题。

OK，重读后我撤回这个问题的核心——逻辑是对的。但我仍然想确认：**`DamageDealer._buffer` 大小为 64，与 `maxTargets` 默认值 16 之间有不匹配的文档说明**。为什么 buffer 开 64 但默认只用 16？这需要一个注释或设计理由说明。

---

### ATK-004：AutoAimComponent.SearchTarget 中的 FindNearestEntity 与 EntityManager._sharedSearchBuffer 的生命周期

**问题**：`AutoAimComponent.SearchTarget()` 调用 `mgr.FindNearestEntity()` 获取一个 Entity 引用并存入 `_currentTarget`。UA-011 说"调用者应立即使用返回的 Entity 引用，不要保存 buffer 地址"。

但 AutoAimComponent **确实保存了返回的 Entity 引用**（`_currentTarget = mgr.FindNearestEntity(...)`）。这和 UA-011 的文档声明是否矛盾？

**我的理解**：UA-011 说的是"不要保存 **buffer 地址**"（即不要持有 `_sharedSearchBuffer` 的指针），保存**返回值**（Entity 引用）是完全安全的，因为返回的是 Entity 对象本身的引用而非 buffer 中的索引。

请确认这个理解是否正确，并考虑将 UA-011 的 XML Doc 措辞从"不要保存 buffer 地址"改为更精确的表述，避免实施者误读。

---

### ATK-005：SkillComponent.GetDecisionWantsSkill() 的组件依赖链

**问题**：
```csharp
var ctrl = _owner.GetComponent(ComponentType.Control) as IDecisionMaker;
var ai = _owner.GetComponent(ComponentType.AI) as IDecisionMaker;
var dm = ctrl ?? ai;
return dm?.GetDecision().WantsAttack ?? false;
```

这里有两个 `GetComponent` 调用 + 两个 `as` 转换 + 一个 `??` + 一个 `?.` 链。考虑到 SkillComponent.Tick 是每帧执行（Auto 模式下 CD 为 0.1s~几秒，但 Tick 本身每帧走 switch），这条热路径上每帧都在做两次字典查找。

**建议**：在 `Init()` 时缓存 `IDecisionMaker`（与 AttackComponent 的做法一致——如果 AttackComponent 也缓存的话）。

另外：SkillTriggerMode.Auto 根本不走 `GetDecisionWantsSkill()`（直接 return true），所以这个性能问题**只在 Manual 模式下存在**。但 TDD 的设计支柱 1 是"零 GC"——`as` 转换不产生 GC，两次 O(1) GetComponent 也在预算内。

**重新评估**：这可能不是真正的问题，但从代码可读性角度，Init 缓存 IDecisionMaker 更清晰。是否值得在 TDD 中推荐？

---

### ATK-006：BuffConfigSO.BuffId 是手动填写的 int，无防重复机制

**问题**：`BuffConfigSO` 的 `BuffId` 是一个 `public int BuffId`，策划在 Inspector 中手动填写。没有任何机制防止两个 BuffConfigSO 使用相同的 BuffId。

**影响**：
- 如果两个不同 Buff 碰巧使用相同 ID，`ApplyBuff` 的"同 ID 刷新持续时间"逻辑会导致第二个 Buff 无法正确生效——它会被当作同一个 Buff 刷新
- 调试时很难发现这种错误

**建议**：
- A：改用 ScriptableObject 实例的 `GetInstanceID()` 或 `name.GetHashCode()` 自动生成 ID
- B：添加一个 Editor Validation 脚本，扫描项目内所有 BuffConfigSO 检查 ID 冲突
- C：不改运行时代码，但在 TDD 中明确记录"BuffId 唯一性由策划保证"的约束 + 推荐命名规范

---

### ATK-007：PlayerMoveBounds 的 Rect 参数含义不直观

**问题**：`Rect(-4.5f, -7f, 9f, 14f)` 对策划来说不够直观。Rect 的构造参数是 `(x, y, width, height)`，其中 (x, y) 是**左下角**坐标。策划可能误以为 (x, y) 是中心点。

**建议**：
- 在 Inspector 中改为 `Vector2 BoundsCenter` + `Vector2 BoundsSize` 两个字段，内部转成 Rect
- 或者加上自定义 PropertyDrawer 显示为 "Min: (-4.5, -7) / Max: (4.5, 7)"
- 至少在 Tooltip 或 Header 中强调 "(x,y) = 左下角，不是中心点"

---

### ATK-008：ISkillEffect 的 Execute 方法没有返回值或执行结果反馈

**问题**：`ISkillEffect.Execute(SkillContext ctx)` 返回 void。如果效果执行失败（比如目标已死亡、资源不足、范围内无敌人），调用方（SkillComponent.ExecuteEffects）无法知道。

**影响**：
- 无法实现"技能施放失败不进入 CD"的逻辑（常见设计需求）
- 无法在 Editor 中追踪"这次技能释放命中了几个敌人"用于调试
- 无法做效果执行日志

**建议**：改为 `bool Execute(SkillContext ctx)` 或返回一个 `SkillEffectResult` 结构体。但这可能是"过度设计" vs "最小可用"的取舍。

**追问**：Phase 4 是否会需要这个返回值？如果会，现在加上 bool 返回值的成本为零（不用就忽略）。

---

以上 8 个问题，请守方逐一回应。

---

## Round 1 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

逐一回应，标注接受/拒绝/部分接受：

---

### RE: ATK-001 SkillConfigSO [SerializeReference] 无自定义 Inspector — ✅ 接受

**结论**：攻方说得对。`[SerializeReference]` 在 Unity 默认 Inspector 下的体验确实很差，策划几乎无法操作。

**修正方案**：

TDD 新增 `SkillConfigSOEditor.cs` 规格：
- 新增 `SkillConfigSOEditor.cs` 到文件变更清单（`_Framework/EntitySystem/Editor/`）
- 使用 `SerializedProperty` + 自定义下拉菜单列出所有 `ISkillEffect` 实现类（通过反射收集带 `[Serializable]` 的实现）
- 提供 "+" 按钮添加效果、"-" 按钮删除、拖拽重排序
- 每个 Effect 条目展开显示其具体属性
- **不依赖 Odin**——模板框架要求零第三方 Editor 依赖

**影响**：实施步骤 P3.3 工时 +0.5h，新增 `SkillConfigSOEditor.cs` 到变更清单。

---

### RE: ATK-002 RecalcModifiers 与 SyncMoveSpeedToMovement 的调用时机 — ✅ 接受方案 A

**结论**：攻方准确识别了歧义。TDD 中注释写的是"RecalcModifiers 末尾调用"，但 Reset 路径也调了 RecalcModifiers，导致实施者不确定要不要 Sync。

**选择方案 A**：**RecalcModifiers 不包含 Sync 调用**，在以下路径**显式**调用 SyncMoveSpeedToMovement：

```csharp
// ApplyBuff() 成功后：
_activeCount++;
RecalcModifiers();
SyncMoveSpeedToMovement();  // 显式

// RemoveBuff() 成功后：
RemoveAtIndex(i);
RecalcModifiers();
SyncMoveSpeedToMovement();  // 显式

// Tick() dirty 后：
if (dirty) 
{
    RecalcModifiers();
    SyncMoveSpeedToMovement();  // 显式
}

// Init() 和 Reset() 不调 Sync
```

**理由**：
- 方案 A 比 B 更清晰——bool 参数是"布尔参数代码异味"，调用者需要知道内部行为
- 显式调用让每个路径的意图一目了然
- Init 不调 Sync 因为 Init 时 Movement 可能还未 Init（取决于 ComponentType 枚举顺序：Movement=3，Buff=10，Init 按枚举顺序，Movement 先于 Buff Init）
- Reset 不调 Sync 原因已在 UA-012 说明

**影响**：修正 §3.4.4 中的注释和代码，消除歧义。

---

### RE: ATK-003 DamageDealer._buffer 大小 64 vs maxTargets 默认 16 — ✅ 接受（补注释）

**结论**：逻辑无 bug，但缺少设计理由文档。

**修正**：在 DamageDealer 代码中添加注释：

```csharp
// _buffer 开 64 而非 16 的理由：
// - maxTargets=16 是默认值，调用方可传更大值（如全屏 AOE 需要命中更多 Entity）
// - buffer 大小为 64 = EntityPool 默认最大容量的一半（预留裕量）
// - buffer 只有 Entity 引用（64 × 8 bytes = 512B），内存开销可忽略
private static readonly Entity[] _buffer = new Entity[64];
```

**影响**：纯注释变更，无运行时影响。

---

### RE: ATK-004 UA-011 文档措辞精确性 — ✅ 接受

**结论**：攻方的理解完全正确。`FindNearestEntity` 返回的是 Entity 对象引用，不是 buffer 中的索引或指针。保存 Entity 引用是安全且正确的用法。

当前 UA-011 的措辞"调用者应立即使用返回的 Entity 引用，不要保存 buffer 地址"确实可能被误读为"不能保存返回值"。

**修正**：更新 XML Doc：

```csharp
/// 注意（v0.3 UA-011，v0.4 ATK-004 修正）：
/// 返回的 Entity 引用可安全持有（如赋值给成员变量做瞄准目标）。
/// 但内部静态 _sharedSearchBuffer 的*内容*会被后续调用覆盖——
/// 不要缓存 buffer 本身的引用或遍历 buffer（它不是公开 API）。
```

**影响**：纯文档变更。

---

### RE: ATK-005 SkillComponent.GetDecisionWantsSkill() 缓存 IDecisionMaker — ⚠️ 部分接受

**结论**：

性能分析：
- ComponentType 枚举 → O(1) 数组索引查找，不是字典
- `as` 转换不产生 GC（引用类型 cast 检查）
- Auto 模式（主要使用场景）根本不走这条路径
- Manual 模式每帧的开销 < 10ns

**但攻方说对了一点**：从代码可读性角度，Init 缓存确实更清晰。

**折中方案**：TDD 中**推荐但不强制**在 Init 缓存：

```csharp
// SkillComponent — 推荐做法（ATK-005）
private IDecisionMaker _cachedDecisionMaker;

public void Init(Entity owner)
{
    // ... existing code ...
    _cachedDecisionMaker = (owner.GetComponent(ComponentType.Control) as IDecisionMaker)
                         ?? (owner.GetComponent(ComponentType.AI) as IDecisionMaker);
}

private bool GetDecisionWantsSkill()
{
    return _cachedDecisionMaker?.GetDecision().WantsAttack ?? false;
}
```

**影响**：改进可读性，性能增益可忽略但不为零。写入 TDD 为推荐实践。

---

### RE: ATK-006 BuffConfigSO.BuffId 无防重复机制 — ✅ 接受方案 C + 轻量 Editor Validation

**结论**：运行时不改（保持 int BuffId 简单性），但需要安全网。

**修正**：
1. **TDD 文档**中明确约束："BuffId 唯一性由策划保证，推荐命名规范：`{类型}{三位数字}`，如 buff_speed_001 = 1001，buff_atk_002 = 2002"
2. **新增 Editor Validation**（低优先级，P3.4 可选子任务）：在 `EntityConfigSOEditor` 或独立的 `BuffIdValidator` 中，OnValidate 或 Menu Item 触发扫描所有 BuffConfigSO 检查 ID 冲突

**不采用方案 A**（`GetInstanceID`/`GetHashCode`）的理由：
- `GetInstanceID()` 在不同 session 间不稳定
- `name.GetHashCode()` 有碰撞风险且不可预测
- 手动 int ID 配合命名规范是 SO 配置驱动系统的标准做法（与 AnimationHash 等一致）

**影响**：P3.4 工时 +0.5h（Editor Validation 脚本）。

---

### RE: ATK-007 PlayerMoveBounds Rect 参数不直观 — ✅ 接受（改为 Center+Size）

**结论**：攻方说得对。`Rect(x, y, width, height)` 对策划不友好。

**修正**：将 `PlayerMoveBounds` 拆为两个字段：

```csharp
[Header("玩家移动边界（P3.0）")]
[Tooltip("启用玩家移动边界约束")]
public bool EnablePlayerMoveBounds = true;

[Tooltip("活动区域中心（世界坐标）")]
public Vector2 PlayerBoundsCenter = Vector2.zero;

[Tooltip("活动区域尺寸（宽, 高）")]
public Vector2 PlayerBoundsSize = new Vector2(9f, 14f);

// 内部使用时转换：
private Rect GetPlayerBoundsRect()
{
    return new Rect(
        PlayerBoundsCenter.x - PlayerBoundsSize.x * 0.5f,
        PlayerBoundsCenter.y - PlayerBoundsSize.y * 0.5f,
        PlayerBoundsSize.x, PlayerBoundsSize.y);
}
```

**理由**：Center+Size 是策划最直观的心智模型，与 BoxCollider2D 的 offset+size 一致。

**影响**：§3.0.2 和 §3.0.5 需更新代码。

---

### RE: ATK-008 ISkillEffect.Execute 无返回值 — ⚠️ 部分接受（改为 bool 返回）

**结论**：这是一个"现在免费做 vs 以后破坏性改"的问题。

**分析**：
- 当前 Phase 3A 不需要这个返回值——"最小可用"原则说不加
- 但 Phase 4 几乎必然需要（技能施放失败不进 CD = 标准设计需求）
- `bool Execute(SkillContext ctx)` 的改动成本为零（不用就忽略返回值）
- 如果 Phase 4 再改 `void → bool`，所有 ISkillEffect 实现都要改签名——这是破坏性变更

**修正**：接受改为 `bool Execute(SkillContext ctx)`：

```csharp
public interface ISkillEffect
{
    /// <summary>
    /// 技能触发时执行。返回 true 表示效果成功执行，false 表示执行失败。
    /// Phase 3A 中 SkillComponent 不消费返回值；Phase 4 可用于"失败不进 CD"逻辑。
    /// </summary>
    bool Execute(SkillContext ctx);
}
```

所有内置 Effect 实现默认 `return true`，AreaDamageEffect 返回 `hitCount > 0`。

**SkillComponent.ExecuteEffects 暂不消费返回值**（Phase 3A 保持最小可用），但接口已就绪。

**影响**：接口签名变更 + 3 个内置 Effect 改返回值。

---

## Round 1 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| ATK-001 | ✅ 接受 | 新增 SkillConfigSOEditor.cs 规格 |
| ATK-002 | ✅ 接受方案 A | RecalcModifiers 不含 Sync，显式调用 |
| ATK-003 | ✅ 接受 | 补 buffer 大小设计理由注释 |
| ATK-004 | ✅ 接受 | 修正 UA-011 文档措辞 |
| ATK-005 | ⚠️ 部分接受 | 推荐但不强制 Init 缓存 IDecisionMaker |
| ATK-006 | ✅ 接受 C+验证 | 命名规范约束 + 可选 Editor Validation |
| ATK-007 | ✅ 接受 | 改为 Center+Size 两字段 |
| ATK-008 | ⚠️ 部分接受 | ISkillEffect.Execute 改为 bool 返回 |

**8 个问题：6 个完全接受，2 个部分接受（无拒绝）。攻方是否有后续追问？**

---

## Round 2 · 攻方追问

> 🔧 **攻方（Unity 工具开发者）**：

守方回应得很全面，大部分接受。我有以下追问和新发现：

---

### ATK-009：SkillConfigSOEditor 的 SerializeReference 类型发现机制需要明确

**追问 ATK-001**：守方接受了新增 SkillConfigSOEditor，提到"通过反射收集带 [Serializable] 的实现"。

但在 Unity 中，`TypeCache.GetTypesDerivedFrom<ISkillEffect>()` 是标准做法（Editor only，零运行时开销）。TDD 是否应明确指定使用 `TypeCache` 而非手动反射？这影响：
- 热重载兼容性（TypeCache 自动更新，手动反射需要 cache invalidation）
- Domain Reload 后的行为

另外：如果用户在 `_Game/` 目录下新增了自定义 ISkillEffect 实现（框架外扩展），Editor 是否能自动发现？答案应该是"是"（TypeCache 搜索所有程序集），但 TDD 应明确这一点。

---

### ATK-010：ATK-005 中 _cachedDecisionMaker 在 Entity 运行中组件动态切换的安全性

**追问 ATK-005**：守方建议 Init 时缓存 `IDecisionMaker`。但有一个边界情况：

如果 Entity 在运行时**从 AI 控制切换为玩家控制**（或反之）——比如"被控制"Buff 或者"AI 接管"机制——缓存的 IDecisionMaker 就过时了。

Phase 3A 有这个场景吗？如果没有，缓存安全。但这应该**明确记录为约束**："_cachedDecisionMaker 在 Init 后固定，不支持运行时切换控制源。如需支持，改为每帧查询。"

---

### ATK-011：BuffComponent.Init 中 RecalcModifiers() 在 _activeCount=0 时的行为

**新发现**：`Init()` 中调用 `RecalcModifiers()`，此时 `_activeCount = 0`，循环不执行，三个 Modifier 都保持 1f。

这是正确的。但结合 ATK-002 的修正（Init 不调 Sync），如果一个 Entity 被回池再复用时，假设上一次使用时 Movement 上还残留着 Buff 的 SpeedModifier（ID=99）——

等等，回池时 `Reset()` 会被调用。Movement.Reset() 应该清零 `_modifierCount`（从而清除所有 modifier 包括 ID=99）。Buff.Reset() 清零 `_activeCount`。

**确认点**：MovementComponent.Reset() **是否**将 `_modifierCount` 归零？TDD v0.3 中没有显示 MovementComponent.Reset() 的实现。Phase 1 的 ENTITY_COMPONENT_TDD.md 中应该有，但请确认 by-ID 新增的 `_modifierIds[]` 在 Reset 时也被正确处理。

---

### ATK-012：ISkillEffect.Execute 返回 bool 后，内置 AreaDamageEffect 返回 hitCount > 0 的语义

**追问 ATK-008**：守方说"AreaDamageEffect 返回 `hitCount > 0`"。

但考虑这个场景：一个 AOE 技能，设计意图是"在指定位置放置一个伤害区域"，即使当前范围内没有敌人也算"成功施放"（区域已经放下了，只是暂时没命中）。

**问题**：`hitCount > 0` 作为"成功"的语义对 AreaDamageEffect 来说可能过于严格。Phase 4 的"失败不进 CD"逻辑如果消费这个返回值，可能导致"AOE 放空就不进 CD"——这不符合大多数游戏的设计。

**建议**：AreaDamageEffect 应该 `return true`（表示"效果已执行"），而非 `return hitCount > 0`（表示"效果有命中"）。如果 Phase 4 需要区分"施放成功"和"命中敌人"，应该通过 SkillContext 回写或事件系统，而非 bool 返回值承载两种语义。

---

### ATK-013：PlayerBoundsCenter + PlayerBoundsSize 在 Gizmo 中需要区分编辑时和运行时

**追问 ATK-007**：守方改为 Center+Size，很好。

补充一个 Editor 体验点：Gizmo 应该在**编辑模式**和**运行模式**下都可见。但如果用 `OnDrawGizmos()`，它只在选中 GameObject 时显示（除非用 `OnDrawGizmosSelected` → 只选中时）或 `OnDrawGizmos` → 总是显示。

**确认**：TDD 应明确使用 `OnDrawGizmos`（非 Selected 版本），因为玩家边界是**全局系统配置**，需要始终可见，而非只在选中 Bootstrap 时才看到。

---

以上 5 个追问，请守方回应。

---

## Round 2 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: ATK-009 TypeCache 与框架外扩展自动发现 — ✅ 接受

**修正**：明确指定 SkillConfigSOEditor 使用 `TypeCache.GetTypesDerivedFrom<ISkillEffect>()`：

```csharp
// SkillConfigSOEditor.cs — 类型发现
private static Type[] GetEffectTypes()
{
    return TypeCache.GetTypesDerivedFrom<ISkillEffect>()
        .Where(t => !t.IsAbstract && !t.IsInterface)
        .OrderBy(t => t.Name)
        .ToArray();
}
```

**关于框架外扩展**：是的，`TypeCache` 搜索所有已加载程序集（包括 `_Game/` 下的 Assembly Definition）。用户在 `_Game/Scripts/` 下新增 `[Serializable] class MyCustomEffect : ISkillEffect` 会自动出现在下拉菜单中。

**TDD 记录**：在 §3.3 新增"扩展点"注释——"ISkillEffect 为开放扩展接口，`_Game/` 目录下新增实现类会被 Editor 自动发现（TypeCache）。"

---

### RE: ATK-010 _cachedDecisionMaker 运行时切换安全性 — ✅ 接受（加约束注释）

**确认**：Phase 3A **没有**运行时切换控制源的场景。"被控制"Buff 或 AI 接管是 Phase 4+ 的功能。

**修正**：在 SkillComponent 代码中添加约束注释：

```csharp
// v0.4（ATK-010）：_cachedDecisionMaker 在 Init 后固定，不支持运行时切换控制源。
// 如 Phase 4 需要"被控制/AI 接管"，改为 property 每帧查询或注册切换回调。
private IDecisionMaker _cachedDecisionMaker;
```

---

### RE: ATK-011 MovementComponent.Reset() 是否清理 _modifierIds — ✅ 接受（补充明确描述）

**确认**：MovementComponent.Reset() 在 Phase 1 TDD 中的契约是"重置所有内部状态到初始值"。`_modifierCount = 0` 即可（因为所有槽位通过 count 控制有效范围，无需逐个清零 _modifierIds 数组内容）。

**但 v0.3 新增 _modifierIds[] 后需要在 TDD 中明确**：

```csharp
// MovementComponent.Reset() — v0.4 补充
public void Reset()
{
    // ... existing resets ...
    _modifierCount = 0;
    // _modifierIds[] 和 _speedModifiers[] 无需逐个清零——
    // count=0 意味着所有槽位无效，新增时直接覆盖。
}
```

**确认回池安全性**：
1. Entity 回池 → `ResetAll()` 按枚举顺序调用组件 Reset
2. MovementComponent(3) Reset → `_modifierCount = 0`（所有 modifier 失效，包括 buff ID=99）
3. BuffComponent(10) Reset → `_activeCount = 0`（不调 Sync，因为 Movement 已经 Reset 了）
4. 下次复用时 Init 从干净状态开始 ✅

无泄漏、无残留。

---

### RE: ATK-012 AreaDamageEffect 返回值语义 — ✅ 接受

**结论**：攻方分析得非常到位。`hitCount > 0` 承载了"命中"语义而非"施放成功"语义。这两个概念应该分开。

**修正**：

```csharp
// AreaDamageEffect.Execute
public bool Execute(SkillContext ctx)
{
    var hostileCamp = CampUtility.GetHostileCamp(ctx.Caster.Camp);
    var dmgCtx = new DamageContext { ... };
    DamageDealer.DealAreaDamage(ctx.CastPosition, Radius, hostileCamp, dmgCtx, MaxTargets);
    return true;  // "效果已执行" = 施放成功，不管命中几个
    // Phase 4 如需命中数，通过 DamageDealer 返回值 + SkillContext 回写
}
```

**设计原则明确化**：
- `bool Execute` 语义 = "效果执行逻辑是否**启动**"（true = 执行了，false = 前置条件不满足未执行）
- 不表示"效果是否有效果/命中"——那是另一个维度的信息

所有 3 个内置 Effect 在 Phase 3A 都 return true。

---

### RE: ATK-013 Gizmo 使用 OnDrawGizmos 非 Selected — ✅ 接受

**确认**：使用 `OnDrawGizmos()`（非 Selected 版本）。玩家边界作为全局系统配置，应始终可见，不依赖 Bootstrap GameObject 的选中状态。

TDD §3.0.5 原文就是写的 `OnDrawGizmos()`，未使用 Selected 版本。确认意图正确。

**补充优化**：仅在 `EnablePlayerMoveBounds == true` 时绘制（已有），且添加 `#if UNITY_EDITOR` 包围以确保 Release 构建零开销：

```csharp
#if UNITY_EDITOR
private void OnDrawGizmos()
{
    if (!EnablePlayerMoveBounds) return;
    // ... draw bounds ...
}
#endif
```

---

## Round 2 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| ATK-009 | ✅ 接受 | 明确 TypeCache + 框架外扩展自动发现 |
| ATK-010 | ✅ 接受 | 加约束注释：Init 后固定，不支持运行时切换 |
| ATK-011 | ✅ 接受 | 明确 MovementComponent.Reset 清零 _modifierCount 即可 |
| ATK-012 | ✅ 接受 | AreaDamageEffect return true（施放成功语义） |
| ATK-013 | ✅ 接受 | 确认 OnDrawGizmos 非 Selected + #if UNITY_EDITOR |

**Round 2 全部接受。攻方是否满意收敛？**

---

## Round 3 · 攻方最终审查

> 🔧 **攻方（Unity 工具开发者）**：

Round 1-2 的所有回应我都满意。做最后一轮细节扫描：

---

### ATK-014：SkillComponent 的 Casting 状态下如果 Entity 死亡，前摇无法中断

**问题**：TDD 说"前摇期间可被打断（死亡/眩晕）"，但代码中 SkillComponent.Tick 的 Casting 分支只做 `_stateTimer -= dt`，没有检查 Entity 存活状态。

如果 Entity 在 Casting 阶段死亡：
1. HealthComponent 设 `IsAlive = false`
2. Entity 进入 PendingDespawn
3. 但 SkillComponent.Tick 还会继续执行（因为 EntityManager.Tick 遍历所有 Active Entity 的所有 Active 组件）
4. 前摇结束后 ExecuteEffects 会在死亡 Entity 上触发——发射弹幕？AOE？

**建议**：在 Tick 入口添加存活检查：

```csharp
public void Tick(float dt)
{
    if (_config == null) return;
    if (!_owner.IsAlive || _owner.IsPendingDespawn)  // ATK-014：死亡中断
    {
        CurrentState = SkillState.Idle;
        return;
    }
    // ... rest of state machine ...
}
```

---

### ATK-015：FireBulletsEffect 中 ds.FireBullets 的 ownerId 参数

**问题**：`ds.FireBullets(Pattern, pos, angle, ctx.Caster.Id.Value)` 使用 `ctx.Caster.Id.Value`。

如果 Caster 已死亡但 Effect 在同帧延迟执行（比如在 Tick 排序后），`ctx.Caster.Id` 是否仍然有效？

回答自己：SkillContext 是 struct 值拷贝，`ctx.Caster` 是 Entity 引用。即使 Entity 标记 PendingDespawn，`Id` 仍然有效（直到回池时 Reset）。所以 FireBullets 用旧 ID 发射弹幕是安全的——弹幕不关心发射者是否存活。

**但结合 ATK-014**：如果加了死亡中断，这个问题就不存在了。所以 ATK-014 是前置修正。

✅ 撤回此问题。

---

### ATK-016：BuffSlot 是 struct，但 Tick 中 `_slots[i].RemainingTime -= dt` 是否安全？

**问题**：`BuffSlot` 是 struct。在 `Tick()` 中：

```csharp
_slots[i].RemainingTime -= dt;
```

因为 `_slots` 是数组（不是 List），`_slots[i]` 返回的是**引用**（数组元素是 in-place 的），所以 `-= dt` 直接修改数组中的值。这是安全的。

如果用的是 `List<BuffSlot>`，这就不安全了（List 的索引器返回值拷贝）。

**确认**：TDD 使用 `BuffSlot[] _slots = new BuffSlot[MAX_BUFFS]`，数组是正确的。这不是问题。

✅ 撤回此问题。

---

### ATK-017：OnDrawGizmos 中的 #if UNITY_EDITOR 是否必要？

**问题**：守方在 RE:ATK-013 中建议 `#if UNITY_EDITOR` 包围 `OnDrawGizmos`。

但 `OnDrawGizmos` 本身就是 Unity Editor 专用的回调——Runtime 构建中 Unity 不会调用它，即使方法存在也不会执行。

`#if UNITY_EDITOR` 的作用仅是**编译移除方法体**减少 IL 大小（微乎其微）。在微信小游戏的包体敏感场景下有一定意义，但对 MonoBehaviour 的 Gizmo 方法来说是标准实践。

**结论**：认同加上 `#if UNITY_EDITOR` 是好习惯，这不是真正的问题。

✅ 确认，不追问。

---

### ATK-018：变更清单中缺少 SkillConfigSOEditor.cs

**问题**：Round 1 ATK-001 守方接受了新增 `SkillConfigSOEditor.cs`，但还没有更新到文件变更清单中。

**请确认**：回写 TDD 时需要在§十一新增文件表中补充：

| `SkillConfigSOEditor.cs` | `_Framework/EntitySystem/Editor/` | P3.3 |

以及修改文件中补充 `ISkillEffect.cs` 的签名变更（void → bool）。

---

**总结**：Round 3 提出 5 个点，其中 3 个自行撤回/确认，1 个实质性修正（ATK-014 死亡中断），1 个清单补全（ATK-018）。

**攻方声明**：如果守方接受 ATK-014 和 ATK-018，我满意收敛。

---

## Round 3 · 守方回应

> 🏗️ **守方（Unity 架构师）**：

---

### RE: ATK-014 Casting 状态死亡中断 — ✅ 接受

**好眼力！** 这是一个真正的 bug。死亡 Entity 不应该继续执行技能。

**修正**：

```csharp
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
    // ... rest of state machine ...
}
```

补充：重置 `_stateTimer` 确保回池后复用不会残留旧值。

---

### RE: ATK-015 — ✅ 确认撤回，无需修正

### RE: ATK-016 — ✅ 确认撤回，struct 数组元素 in-place 修改是安全的

### RE: ATK-017 — ✅ 确认，#if UNITY_EDITOR 为好习惯，保留

---

### RE: ATK-018 变更清单补全 — ✅ 接受

**修正**：TDD §十一 需补充：

新增文件追加：
| `SkillConfigSOEditor.cs` | `_Framework/EntitySystem/Editor/` | P3.3 |

修改文件追加/更新：
| `ISkillEffect.cs` | `Execute` 签名从 `void` 改为 `bool`（v0.4 ATK-008） | P3.3 |

---

## Round 3 小结

| 问题 ID | 结论 | 动作 |
|---------|------|------|
| ATK-014 | ✅ 接受 | SkillComponent.Tick 入口添加死亡中断 |
| ATK-015 | ✅ 撤回 | — |
| ATK-016 | ✅ 撤回 | — |
| ATK-017 | ✅ 确认 | — |
| ATK-018 | ✅ 接受 | 变更清单补全 SkillConfigSOEditor + ISkillEffect 签名 |

---

## 🏁 PK 收敛声明

> **攻方**：3 轮审查，18 个问题（13 个实质 + 5 个自行撤回/确认），全部收敛。满意。
> **守方**：所有接受项将回写 TDD v0.4。

### PK 统计

| 维度 | 数据 |
|------|------|
| 总轮次 | 3 轮（最大 6 轮） |
| 总问题数 | 18（实质 13 + 撤回/确认 5） |
| 接受 | 10 |
| 部分接受 | 2（ATK-005 推荐不强制、ATK-008 改 bool 返回） |
| 拒绝 | 0 |
| 自行撤回/确认 | 5（ATK-015/016/017 + 部分 ATK-003） |
| 收敛率 | 100% |

### 需回写 TDD 的修正项清单

| ID | 修正内容 | 影响范围 |
|----|---------|---------|
| ATK-001 | 新增 SkillConfigSOEditor.cs 规格（TypeCache 类型发现） | §3.3 + §十一 |
| ATK-002 | RecalcModifiers 不含 Sync，ApplyBuff/RemoveBuff/Tick-dirty 路径显式调 Sync | §3.4.3 + §3.4.4 |
| ATK-003 | DamageDealer._buffer 大小 64 的设计理由注释 | §3.2.2 |
| ATK-004 | FindNearestEntity XML Doc 措辞修正（区分 Entity 引用 vs buffer 地址） | §3.1.2 |
| ATK-005 | SkillComponent 推荐 Init 缓存 IDecisionMaker（带约束注释 ATK-010） | §3.3.5 |
| ATK-006 | BuffId 唯一性约束文档 + 可选 Editor Validation | §3.4.2 + §十一 |
| ATK-007 | PlayerMoveBounds 改为 Center+Size 两字段 | §3.0.2 + §3.0.5 |
| ATK-008 | ISkillEffect.Execute 改为 bool 返回（施放成功语义） | §3.3.3 + 内置 Effect |
| ATK-012 | AreaDamageEffect return true（施放语义，非命中语义） | §3.3.4 |
| ATK-013 | OnDrawGizmos 加 #if UNITY_EDITOR | §3.0.5 |
| ATK-014 | SkillComponent.Tick 入口死亡中断检查 | §3.3.5 |
| ATK-018 | 文件变更清单补全 SkillConfigSOEditor + ISkillEffect 签名 | §十一 |

**PK 状态**：✅ **已收敛 — 待回写 TDD v0.4**

