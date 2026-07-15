# FLOATING_TEXT_TDD PK 评审记录（Code Review Checklist 视角）

> **目标文档**：`SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md` v2.1（PK 收敛）  
> **攻方**：代码评审专家（code-review-checklist CL-1~CL-11 + known-pitfalls 视角）  
> **守方**：软件架构师  
> **最大轮次**：8  
> **PK 状态**：✅ 已收敛（3 轮，10 问题全部回应）

---

## PK Round 1 — 攻方提问（代码评审专家）

> 审查视角：code-review-checklist CL-1~CL-11 + known-pitfalls PIT-007~PIT-053

### CR-001 | 严重度 🔴高 | CL-1 跨文件引用完整性：`BattleController.OnBattleEnd` 清场路径仍走 `DanmakuSystem.Instance.ClearAll()`

**涉及章节**：§6.3 DOT 飘字 + §8 Phase 3
**质疑**：TDD §6.3 明确将 DOT 飘字改为走 `_entityBootstrap.FloatingText?.Spawn()`，但代码上下文显示 `BattleController` 第 267 行有另一处 `DanmakuSystem.Instance.ClearAll()` 作为"退场安全网"。`ClearAll()` 内部调用了 `_damageNumbers.ClearAll()`（即飘字清除）。Phase 3 将 `_damageNumbers` 替换为 `_floatingText`，这条路径自然迁移了。
**但问题是**：TDD §11.2 文档更新清单中**没有**提到要验证 `BattleController` 中其他 `DanmakuSystem.Instance` 调用是否仍然正确。代码中有**至少 6 处** `DanmakuSystem.Instance` 散落在 `FireLaserEffect`、`FireBulletsEffect`、`EnemyShootComponent`、`CollisionComponent` 中——这些走单例是合理的（它们属于弹幕业务）。但 §6.4 声明"❌ 不推荐单例访问（违反 coding-standards §3 + §7）"，而 TDD 的修改范围只覆盖了飘字的单例访问，没有声明为什么其他 `DanmakuSystem.Instance` 调用可以例外。
**潜在风险**：实施者读完 TDD §6.4 后可能认为所有 `DanmakuSystem.Instance` 调用都应该重构——但实际只需重构飘字路径。未明确声明例外范围 = 过度重构风险。
**建议方向**：在 §6.4 注入路径表之后，补充一段"范围声明"：本次重构仅迁移飘字相关的 `DanmakuSystem.Instance` 访问。弹幕发射/碰撞注册等保留单例模式（DanmakuSystem 本身是 DontDestroyOnLoad 合法单例）。

### CR-002 | 严重度 🔴高 | CL-9 架构一致性 / CL-1 跨文件引用：`HitReactionHandler` 字段删除后，`EntitySystemBootstrap` 的 `.HitReactionHandler` 访问路径需检查

**涉及章节**：§8 Phase 3 步骤 3.1~3.5
**质疑**：当前 `BattleController.OnEnemyDamaged` 通过 `_entityBootstrap.HitReactionHandler.SpawnDamageNumber(...)` 调用 DOT 飘字。TDD 将此改为 `_entityBootstrap.FloatingText?.Spawn(...)`。但 TDD 步骤表只列出了删除 `SpawnDamageNumber` 方法本身——**未检查 `_entityBootstrap.HitReactionHandler` 属性上是否还有其他外部调用者**在使用这个路径的其他方法。
通过 CodeGraph 我看到 `HitReactionHandler` 上还有 `RegisterEntity` 和 `Tick` 方法被 Bootstrap 内部调用，以及 `IsFlashing` / `GetFlashProgress` 被 ViewBridge 调用——这些不受影响。但 `SpawnDamageNumber` 的第一个外部调用者（BattleController）被迁移后，**是否还有第二个外部调用者**？TDD 声称"调用入口：OnHit（1 处）+ BattleController.OnEnemyDamaged（1 处，DOT）"——**但未用 grep 验证这是否是完整列表**。
**潜在风险**：如果后续有新代码引用了 `SpawnDamageNumber`（如某个 Buff Effect 或测试脚本），Phase 3 删除方法后会编译失败但不在 TDD 预期内。
**建议方向**：在 §8 Phase 3 步骤 3.3 之前，增加一个前置验证步骤："grep 全项目 `SpawnDamageNumber`，确认调用者 = 2（Handler.OnHit + BattleController），无其他外部引用"。

### CR-003 | 严重度 🟡中 | CL-6 渲染管线：`RenderSortingOrder.DamageNumber` 命名不再准确

**涉及章节**：§7.4 渲染排序不变
**质疑**：TDD §7.4 声称"RenderSortingOrder.DamageNumber 仍然有效，FloatingTextSystem 继续使用该排序值"。系统已重命名为 `FloatingTextSystem`，但排序枚举值仍叫 `DamageNumber`。按 CL-1 跨文件引用完整性原则，重命名系统时应同步考虑相关枚举命名是否需要更新（或至少标注为"已知技术债，后续统一处理"）。
**潜在风险**：不阻塞编码。但后续如果 FloatingTextSystem 扩展到治疗飘字、经验值飘字等非"DamageNumber"场景，这个枚举名会产生困惑。
**建议方向**：在 §7.4 补充说明：枚举值保持 `DamageNumber` 不改名（避免 diff 扩散），后续版本可统一重命名为 `FloatingText`。标注为迭代项。

### CR-004 | 严重度 🟡中 | CL-5 生命周期与时序 / PIT-048：`EntitySystemBootstrap.Awake` 中 `FindObjectOfType<DanmakuSystem>()` 的时序依赖

**涉及章节**：§6.4 + §10 R-1
**质疑**：TDD §10 R-1 分析了初始化时序，结论是"DanmakuSystem 是 DontDestroyOnLoad，先于场景级 Bootstrap Awake"。但 PIT-048 明确指出 DontDestroyOnLoad 对象跨场景存活时存在清理风险。更重要的是：**`FindObjectOfType` 在 Unity 中不保证执行顺序**——如果 DanmakuSystem 和 EntitySystemBootstrap 在同一场景中（首次加载），Unity Awake 顺序未定义（取决于加载顺序）。
R-1 分析的前提"DanmakuSystem.Awake() 先于 EntitySystemBootstrap.Awake()"**只在 DanmakuSystem 已经通过上一场景初始化的情况下才成立**。如果游戏从战斗场景直接启动（跳过 Preload 场景），DanmakuSystem 可能尚未 Awake。
**现状代码验证**：当前 `EntitySystemBootstrap.Awake` 中没有 `FindObjectOfType<DanmakuSystem>()`——这是 TDD 计划新增的代码。所以这是一个**新引入的时序风险**。
**潜在风险**：从战斗场景直接启动（Editor 调试）时，`FindObjectOfType<DanmakuSystem>()` 返回非 null 对象但其 `FloatingText` 字段为 null（因为 `InitializeSubsystems` 尚未执行），导致飘字静默失败。
**建议方向**：在 §8 Phase 3 步骤 3.9 中明确：`FindObjectOfType<DanmakuSystem>()?.FloatingText`——通过 `?.` 确保即使 DanmakuSystem 存在但未初始化，也不会 NPE。同时在 §10 R-1 补充场景直启风险说明。

### CR-005 | 严重度 🟡中 | CL-6 渲染管线 / PIT-034：`FloatingTextSystem.Spawn` 位置参数使用 `Vector2` 但 RBM WriteNumber 使用 `Vector3`

**涉及章节**：§4.1 Spawn 签名 + §5.1 FloatingTextData.Position
**质疑**：TDD §4.1 声明 Spawn 签名为 `Spawn(Vector2 position, ...)` 且 §5.1 `FloatingTextData.Position` 类型为 `Vector2`。但实际 `WriteNumber` 方法中生成顶点时使用 `new Vector3(x, y, 0f)`——这是正确的（2D 游戏 z=0）。
然而，§6.2 改动后代码为 `_floatingText.Spawn(entity.Position + new Vector2(0, 0.5f), ...)`。Entity 的 `Position` 类型是什么？通过 CodeGraph 看到 `EntityHitReactionHandler.SpawnDamageNumber` 当前接受 `Vector2 position`，且 Entity.Position 确实是 Vector2——所以类型匹配。
**但 PIT-034 警告**：Entity.Position 如果是来自 `transform.position`（Vector3），隐式转 Vector2 会丢失 z。TDD 示例代码 `entity.Position + new Vector2(0, 0.5f)` 假设 Entity.Position 是 Vector2，如果后续 Entity.Position 改为 Vector3（为支持 3D 功能），这行代码会隐式截断。
**潜在风险**：低。当前系统是纯 2D，Entity.Position 确实是 Vector2。但作为 TDD 应注明此假设。
**建议方向**：在 §5.1 数据结构中注明"Position 为 Vector2，假设纯 2D 游戏。如需支持 z 层，需升级为 Vector3 并同步修改 WriteNumber"。标注为迭代项。

### CR-006 | 严重度 🟡中 | CL-5 生命周期 / PIT-047：`EntityHitReactionHandler.Tick` 中 `TickDamageNumbers` 传入 `Time.unscaledDeltaTime` 而非外部 dt

**涉及章节**：§6.2 + §8 Phase 3 步骤 3.7
**质疑**：当前代码 `Tick(float dt, ...)` 内部调用 `TickDamageNumbers(Time.unscaledDeltaTime)` 而非传入参数 `dt`。旧系统 TextMesh 飘字手动管理 `Time.unscaledDeltaTime` 是因为 PIT-047 要求"纯视觉效果用 unscaledDeltaTime"。
迁移后 `FloatingTextSystem.Rebuild(float dt)` 由 `DanmakuSystem.RunLateUpdatePipeline()` 调用，传入 `Time.unscaledDeltaTime`——这是正确的。
**但问题是**：Phase 3 步骤 3.7 说"修改 Tick：删除 TickDamageNumbers 调用"。删除后飘字更新完全由 DanmakuSystem 的 LateUpdate Rebuild 驱动——**但 Entity 层的 `Tick` 和 DanmakuSystem 的 `LateUpdate` 时机不同**。如果 EntityManager.Tick 在 `Update` 中执行，而 `Rebuild` 在 `LateUpdate` 中执行，则飘字生成和首帧渲染之间有一帧延迟。旧系统在同一个 Tick 内生成+更新位置，新系统分离到两个阶段。
**潜在风险**：一帧延迟通常不可见（60fps）。但如果有极端多飘字同帧生成，第一帧渲染位置可能是初始位置（未经 Velocity 更新）。旧 TextMesh 系统在同帧就应用了初始 position + 0.5f 偏移，新系统第一帧也能正确显示初始位置（Spawn 直接写 Position）。
**建议方向**：非阻塞。建议在 §7.2 补充时序说明："Spawn 在 Update 阶段写入环形缓冲区 → LateUpdate Rebuild 同帧更新位置并渲染。由于 Rebuild 在 Spawn 之后执行（LateUpdate > Update），**同帧生成的飘字会在当帧就被渲染**，无延迟。"

### CR-007 | 严重度 🟢低 | CL-10 方法重载安全 / PIT-039：`Spawn` 新增 `Color32` 参数后旧调用点兼容性

**涉及章节**：§4.2 Spawn 签名变更
**质疑**：旧签名 `Spawn(Vector2, int, bool)` → 新签名 `Spawn(Vector2, int, Color32, bool)`。TDD 移除了旧签名（不保留重载）。这意味着所有调用方必须同步修改——Phase 2 步骤 2.6 修改 UpdatePipeline 调用，Phase 3 步骤 3.6 修改 Handler 调用。
**PIT-039 验证**：由于旧签名被**删除**而非保留为重载，不存在 CS0121 歧义风险。但需确认：是否还有第三方测试代码或 Demo 中调用了 `DamageNumberSystem.Spawn`？
**验证结果**：通过 CodeGraph 上下文，`Spawn` 的调用者只有 `UpdatePipeline`（1 处）——因为 Entity 层走的是 `SpawnDamageNumber`（独立方法）。所以无重载歧义风险。
**潜在风险**：极低。仅建议在 Phase 2 步骤 2.6 明确"grep 全项目 _damageNumbers.Spawn / DamageNumberSystem.*Spawn 确认 = 1 处调用"。
**建议方向**：步骤 2.6 后增加验证 grep。

### CR-008 | 严重度 🟢低 | CL-9 架构一致性 / PIT-051：旧 `DamageNumber.prefab` 删除后需确认无 Missing Prefab 引用

**涉及章节**：§8 Phase 3 步骤 3.14 + §11.1
**质疑**：TDD 步骤 3.14 删除 `DamageNumber.prefab`。PIT-051 教训是"对每个公开字段做 Find All References"。`DamageNumberPool` SO 的 `Prefab` 字段引用了这个 Prefab——TDD 步骤 3.15 删除 Pool SO，步骤 3.10 删除 Bootstrap 字段。但是否有**其他 Pool SO 实例**也引用了同一个 Prefab？（比如 Enemy 变体的 EntityConfig 中配置了相同 Pool）。
**验证**：TDD §1.1 表格显示 EntityHitReactionHandler 容量 = 32，且 `EntitySystemBootstrap` 上只有一个 `DamageNumberPool` 字段——全局只有一个 Pool SO 实例。
**潜在风险**：极低，但完备性要求确认。
**建议方向**：步骤 3.14 前增加"grep 全项目 `Template_DamageNumberPool` 确认引用 = 1（Bootstrap Inspector）"。

---

> **Round 1 严重度分布**：🔴×2 + 🟡×4 + 🟢×2 = 共 8 个问题
> **CL 覆盖**：CL-1(×2), CL-5(×2), CL-6(×2), CL-9(×2), CL-10(×1)
> **PIT 引用**：PIT-034, PIT-039, PIT-047, PIT-048, PIT-051

---

## PK Round 1 — 守方回应（软件架构师）

| ID | 严重度 | 状态 | 回应摘要 |
|----|--------|------|----------|
| CR-001 | 🔴高 | ✅ 已回应 | §6.4 补充范围声明：本次仅迁移飘字的单例访问，弹幕业务 API 保留单例模式（DanmakuSystem 是合法 DontDestroyOnLoad 单例） |
| CR-002 | 🔴高 | ✅ 已回应 | Phase 3 步骤 3.2a 新增前置 grep 验证：`SpawnDamageNumber` 全项目调用者 = 2 |
| CR-003 | 🟡中 | ✅ 已回应 | §7.4 补充说明：`RenderSortingOrder.DamageNumber` 保持不改名（避免 diff 扩散），标注为后续迭代项 |
| CR-004 | 🟡中 | ✅ 已回应 | §8 步骤 3.9 明确使用 `?.FloatingText` 双层安全访问 + §10 R-1 补充场景直启降级说明 |
| CR-005 | 🟡中 | ✅ 已回应 | §5.1 补充 2D 假设注释（Position = Vector2，z=0），标注远期迭代项 |
| CR-006 | 🟡中 | ✅ 已回应 | §7.2 补充时序说明：Spawn(Update) → Rebuild(LateUpdate) 同帧渲染无延迟 |
| CR-007 | 🟢低 | ✅ 已回应 | Phase 2 步骤 2.6 增加 grep 验证（`_damageNumbers.Spawn` = 1 处） |
| CR-008 | 🟢低 | ✅ 已回应 | Phase 3 步骤 3.14 增加 grep 验证（`Template_DamageNumberPool` 引用 = 1） |

**文档版本**：v2.1 → v2.2（8 处修正）

---

## PK Round 2 — 攻方复审（代码评审专家）

### Round 1 回应评估

| ID | 评分 | CL 对照 | 理由 |
|----|------|---------|------|
| CR-001 | 🟢 满意 | CL-1/CL-9 | 范围声明精准——明确了"飘字 only"修改范围，同时说明了 DanmakuSystem 合法单例身份。实施者不会误扩范围 |
| CR-002 | 🟢 满意 | CL-1 | 步骤 3.2a 前置 grep 验证，确保删除方法前已确认完整调用者列表。PIT-051 经验完美落地 |
| CR-003 | 🟢 满意 | CL-1 | 迭代项标注清晰，不扩散 diff |
| CR-004 | 🟢 满意 | CL-5/PIT-048 | 步骤 3.9 使用 `?.FloatingText` 双层安全访问 + R-1 补充了场景直启降级分析。覆盖了 PIT-048 的跨场景生命周期风险 |
| CR-005 | 🟢 满意 | CL-6/PIT-034 | 2D 假设显式注释，后续扩展时可查 |
| CR-006 | 🟢 满意 | CL-5/PIT-047 | 时序说明明确：Update → LateUpdate 同帧渲染无延迟。回答了 PIT-047 的 unscaledDeltaTime 关注 |
| CR-007 | 🟢 满意 | CL-10/PIT-039 | grep 验证嵌入步骤，零成本防重载歧义 |
| CR-008 | 🟢 满意 | CL-9/PIT-051 | grep 验证嵌入步骤，确认 Pool SO 引用唯一性 |

### Round 2 新问题

### CR-009 | 严重度 🟡中 | CL-5 生命周期：步骤 3.9 代码示例中的 `?.` 与 PIT-034 fake-null 冲突

**涉及章节**：§6.4 代码示例 + §8 步骤 3.9
**质疑**：§6.4 代码示例写的是 `danmaku?.FloatingText`。`DanmakuSystem` 继承自 `MonoBehaviour`（`UnityEngine.Object`）。PIT-034 明确指出："Unity Object 的 `?.` 不走 Unity 重载，fake-null 不会短路"。如果 DanmakuSystem 的 GameObject 被 Destroy（战斗退场清理后但 Bootstrap 仍在执行），`danmaku` 是 fake-null 壳对象，C# `?.` 不会短路——`danmaku.FloatingText` 访问一个已 destroyed 的对象可能返回意料之外的值或触发 MissingReferenceException。
**分析**：DanmakuSystem 是 DontDestroyOnLoad，正常流程中不会被 Destroy。但 PIT-034 的教训是："凡是 Unity Object 一律使用 `!= null` 显式判断"。代码示例使用 `?.` 可能被实施者模仿到其他 Unity Object 场景。
**建议方向**：§6.4 代码示例改为显式判断：
```csharp
var danmaku = FindObjectOfType<DanmakuSystem>();
var floatingText = (danmaku != null) ? danmaku.FloatingText : null;
```

### CR-010 | 严重度 🟢低 | CL-6：Phase 1 步骤 1.10 删除 `[Obsolete] GetAtlasStats()` 后需确认无调用者

**涉及章节**：§8 Phase 1 步骤 1.10
**质疑**：步骤 1.10 删除 `GetAtlasStats()` 方法。当前代码中该方法标了 `[Obsolete]`，但未确认是否仍有调用者（可能在 DebugHUD 或测试中调用）。通过 CodeGraph 上下文，`DanmakuDebugHUD` 中有 `RefreshAtlasStatsCache()` 方法——需验证它是否调用了 `_damageNumbers.GetAtlasStats()`。
**潜在风险**：如果 DebugHUD 仍有调用，删除后编译失败但不在 TDD 预期内。
**建议方向**：步骤 1.10 增加"grep `GetAtlasStats` 确认调用者 = 0 或已迁移到 `DanmakuSystem.GetAllAtlasStats()`"。

---

> **Round 2 严重度分布**：🟡×1 + 🟢×1 = 共 2 个新问题
> **趋势**：🔴 2→0（↓），🟡 4→1（↓），总问题数 8→2（↓）
> **PK 收敛评估**：0 个 🔴，严重度单调下降。CR-009 解决后即可收敛。

---

## PK Round 2 — 守方回应（软件架构师）

| ID | 严重度 | 状态 | 回应摘要 |
|----|--------|------|----------|
| CR-009 | 🟡中 | ✅ 已回应 | §6.4 代码示例 + 步骤 3.9 从 `?.` 改为显式 `!= null`（PIT-034 fake-null 规避）。`FloatingText` 是普通 C# 类不是 Unity Object，但取 DanmakuSystem（MonoBehaviour）引用时必须用 `!= null` |
| CR-010 | 🟢低 | ✅ 已回应 | Phase 1 步骤 1.10 增加前置 grep 验证。实际验证结果：`GetAtlasStats` 在 DamageNumberSystem 中只有声明，无外部调用者（DanmakuSystem.GetAllAtlasStats 只查 Shared+VFX） |

**文档版本**：v2.2 → v2.3（2 处修正）

---

## PK Round 3 — 攻方最终复审（代码评审专家）

### Round 2 回应评估

| ID | 评分 | CL 对照 | 理由 |
|----|------|---------|------|
| CR-009 | 🟢 满意 | CL-5/PIT-034 | 代码示例改为显式 `!= null`，步骤 3.9 描述同步更新。PIT-034 fake-null 铁律彻底落地 |
| CR-010 | 🟢 满意 | CL-1 | grep 验证嵌入步骤，实际验证已确认 0 外部调用 |

### Round 3 新问题

**无新问题。**

文档 v2.3 已解决所有 Round 1~2 提出的 10 个问题（2🔴 + 5🟡 + 3🟢），无遗留阻塞项。所有 CL 检查项覆盖完整，PIT 经验引用准确。

> **PK 收敛评估**：无新问题，PK 可以收敛。

---

## PK 总结报告

| 维度 | 状态 |
|------|------|
| **PK 轮次** | 3 轮完成（最大 8 轮内收敛） |
| **总问题数** | 10 个（Round 1: 8 + Round 2: 2） |
| **全部回应** | 10/10 ✅ |
| **文档版本** | v2.1 → v2.2 → v2.3 |
| **阻塞编码的问题** | 0 个 |
| **攻方收敛意见** | "无新问题，PK 可以收敛" |

### CL 覆盖矩阵

| CL | 命中问题 | 结论 |
|----|----------|------|
| CL-1 跨文件引用完整性 | CR-001, CR-002, CR-003, CR-010 | 范围声明 + grep 前置验证 |
| CL-5 生命周期与时序 | CR-004, CR-006, CR-009 | 场景直启降级 + 时序无延迟 + fake-null 规避 |
| CL-6 渲染管线 | CR-003, CR-005 | 枚举迭代标注 + 2D 假设注释 |
| CL-9 架构一致性 | CR-001, CR-008 | 单例范围声明 + Pool SO 唯一性验证 |
| CL-10 方法重载安全 | CR-007 | grep 确认无歧义 |
| CL-2/3/4/7/8/11 | — | 未命中（文档不涉及命名空间冲突/版本API/字符串引用/FairyGUI/三方库/Editor 同步） |

### PIT 引用矩阵

| PIT | 命中问题 | 作用 |
|-----|----------|------|
| PIT-034 (fake-null) | CR-009 | 修正了 `?.` → `!= null` |
| PIT-039 (重载歧义) | CR-007 | 确认无歧义风险 |
| PIT-047 (unscaledDeltaTime) | CR-006 | 补充时序说明 |
| PIT-048 (DontDestroyOnLoad 清理) | CR-004 | 场景直启降级 |
| PIT-051 (Obsolete ≠ 删除) | CR-002, CR-008 | grep 前置验证 |

### 最有价值的 Top 3 变更

1. **CR-001 — 范围声明**：在 §6.4 注入路径表后明确"本次仅迁移飘字"，避免实施者误认为所有 `DanmakuSystem.Instance` 调用都需重构——精确界定修改边界是大型重构的安全阀
2. **CR-009 — PIT-034 fake-null 修正**：将 Unity Object 的 `?.` 运算改为显式 `!= null`，根除了代码示例被模仿到其他场景的隐患——教训库从"知道"变为"执行"
3. **CR-002 — grep 前置验证体系**：在删除方法/文件前嵌入 grep 验证步骤（3.2a / 2.6 / 3.14 / 1.10），将 CL-1 跨文件引用完整性检查从"审查建议"固化为"实施流程"

### 遗留项

| 优先级 | 项目 | 处理方式 |
|--------|------|----------|
| 🟢低 | CR-003 枚举 `RenderSortingOrder.DamageNumber` 改名 | 编码后迭代 |
| 🟢低 | CR-005 Position Vector2→Vector3 升级 | 远期迭代 |

**结论：PK 收敛。文档 v2.3 可以进入编码。**

收敛理由：
1. 🔴 高优问题从 Round 1 的 2 个降至 Round 2 的 0 个，严重度单调下降
2. Round 3 攻方无新问题
3. code-review-checklist CL-1/5/6/9/10 全部审查通过
4. known-pitfalls PIT-034/039/047/048/051 全部引用验证并落地











