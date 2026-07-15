# PK 评审记录 — TDD-07 战斗退场生命周期统一事件通道

> **目标文档**：`Docs/Agent/SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md`
> **文档类型**：TDD
> **攻方角色**：Unity 编辑器工具开发者（10+ 年 Unity Editor 扩展 / EditorWindow / 自动化测试 / SerializedObject 工作流经验）
> **守方角色**：软件架构师（专精系统设计、SO 事件通道、观察者模式、可维护性和关注点分离）
> **开始时间**：2026-05-26 09:30
> **最大轮次**：8
> **PK 状态**：✅ 已收敛（2 轮，2026-05-26 09:30 ~ 10:00）

---

## Round 1：攻方提问

### 代码分析基础（CodeGraph 实证）

| 文件 | 关键发现 |
|------|---------|
| `BattleController.cs` | 4 条退场路径 + OnDestroy 共 5 处手动清理，路径间清理列表**不一致**（OnDestroy 缺 HitReactionHandler.ClearAll，所有路径缺 BattleHUDController.RecycleAllFloatingTexts） |
| `DanmakuSystem.cs` | 单例 + DontDestroyOnLoad，只有 OnDestroy（应用退出）注销 Instance，**无 OnEnable/OnDisable** |
| `EntitySystemBootstrap.cs` | 非 DDOL MonoBehaviour，只有 Awake 初始化，**无 OnEnable/OnDisable/OnDestroy** |
| `BattleHUDController.cs` | MonoBehaviour，有 RecycleAllFloatingTexts 但当前**未被任何退场路径调用** |
| `EntityCollisionSolver.cs` | 纯 C# 类（非 MB），ClearCooldowns 方法正确 |

---

## UT-001 | 严重度 🔴高 | DanmakuSystem 作为 DDOL 单例，OnEnable/OnDisable 不可用于 SO 事件注册

**涉及章节**：§3.2 Phase B - B1，§6 风险

**质疑**：
TDD §4 Phase B 的 B1 步骤写道：
> B1 | DanmakuSystem | 实现 IBattleCleanup, **OnEnable Register, OnDisable Unregister**, `OnBattleCleanup() => ClearAll()`

但代码实证：`DanmakuSystem` 是 `DontDestroyOnLoad` 单例（`Awake` 中 `DontDestroyOnLoad(gameObject)`），生命周期跨场景存在。`OnEnable` 只在首次激活时触发一次，`OnDisable` 只在应用退出或 `Destroy` 时触发——不会在战斗场景切换时触发。

**这意味着**：
1. `OnEnable` 注册只执行一次（第一次进入战斗），如果 SO 资产在场景间被卸载/重建，引用可能失效
2. `OnDisable` 注销永远不会在正常退场时触发，只有应用退出才会执行
3. TDD §6 风险表虽然提到了"OnDisable 不会在场景切换时触发"，但 Phase B 的实施步骤 B1 仍然写了 `OnEnable Register, OnDisable Unregister`——**文档自相矛盾**

**潜在风险**：按 B1 字面实施，DanmakuSystem 的注册时机是正确的（第一次启用即注册），但注销时机是错的——正常退场时不会注销，导致 `_listeners` 中始终持有 DanmakuSystem 引用。如果 Retry 多次调用 Raise，DanmakuSystem 会重复执行但不会造成功能错误（因为 ClearAll 幂等），但如果未来有人看到 B1 以为 `OnDisable` 会在退场时触发就会被误导。

**建议方向**：
1. B1 步骤说明改为"Awake 中 Register（与 DontDestroyOnLoad 一致），不注销（永久监听者）"
2. 或引入 `OnBattleSceneUnloaded` 回调注销（但增加复杂度，不推荐）
3. 在 §6 风险表中明确：DDOL 系统是永久监听者，设计上不注销

---

## UT-002 | 严重度 🔴高 | EntitySystemBootstrap 当前无 OnEnable/OnDisable，需要新增——但 DespawnAll 后是否保留注册？

**涉及章节**：§3.4 Retry 路径，§4 Phase B - B8，§6 风险

**质疑**：
TDD §4 B8 写道 EntitySystemBootstrap 实现 IBattleCleanup 并通过 `OnEnable` Register / `OnDisable` Unregister。代码实证 EntitySystemBootstrap **当前没有 OnEnable/OnDisable**（只有 `Awake`），需要新增。

关键问题在 Retry 路径：
1. `ResetBattleRuntimeState()` → `_onBattleEnd.Raise()` → EntitySystemBootstrap.OnBattleCleanup() → **DespawnAll()**
2. DespawnAll 回收所有 Entity，但 EntitySystemBootstrap 本身是场景级 MB，**不会被 DespawnAll 销毁**
3. 所以 Retry 后 EntitySystemBootstrap 仍然注册在 `_listeners` 中——这是正确的

但 TDD §6 风险表问了一个好问题却没给明确答案：
> 确认 DespawnAll 不触发 OnDisable（EntitySystemBootstrap 本身不被销毁）

这需要**代码级保证**，而不只是"确认"二字。如果未来 EntitySystemBootstrap 被放到 Entity 层级下（虽然不太可能），DespawnAll 可能连带销毁它。

**潜在风险**：Retry 路径依赖 EntitySystemBootstrap 不被销毁的隐含假设，但没有在代码中做防御性保护。

**建议方向**：
1. Phase B8 实施说明中加一条：`OnBattleCleanup()` 内部 DespawnAll 只操作 EntityManager 管理的 Entity，不影响 Bootstrap 自身
2. 加断言：`Debug.Assert(this != null && isActiveAndEnabled, "Bootstrap 被意外销毁！")`
3. 或在 §6 风险表中用一句代码解释清楚，而不是"确认"二字

---

## UT-003 | 严重度 🔴高 | 验收章节不符合两层验收体系

**涉及章节**：§4 Phase E

**质疑**：
TDD 的验收章节（Phase E）将所有验收项混在一起，没有区分：
- **Phase 门禁验收**：阻塞项，不通过则下一 Phase 无法启动
- **全局集成验收**：需要真机/视觉/交互才能验证

当前 Phase E 的 6 项验收：
- E1~E4：需要 PlayMode 运行 + 视觉确认（飘字/弹丸残留）→ 属于**全局集成验收**
- E5：编辑器 Stop PlayMode 无报错 → 可以自动化 → 可作为**门禁验收**
- E6：新增 DummyCleanup 验证自动触发 → 可编辑器内自动化 → 可作为**门禁验收**

而 Phase A~D 各阶段没有门禁验收条款（"编译验证 0E/0W"勉强算，但没有独立成验收项）。

**潜在风险**：实施者不清楚哪些验收必须在该 Phase 完成才能继续，哪些可以延后到全局验收阶段。

**建议方向**：
1. 每个 Phase 末尾加"门禁"子节，至少包含"编译 0E/0W"
2. Phase E 拆分为"门禁验收"（E5, E6 + 自动化脚本验证）和"全局集成验收"（E1~E4）
3. 标注 E1~E4 延后到全局真机验收

---

## UT-004 | 严重度 🟡中 | SO 事件通道 `_listeners` 是运行时 List——Enter Play Mode Settings 下 Domain Reload 关闭时会残留

**涉及章节**：§3.1 BattleLifecycleEvent

**质疑**：
`BattleLifecycleEvent` 的 `_listeners` 声明为：
```csharp
private readonly List<IBattleCleanup> _listeners = new(16);
```

这是**字段初始化器**（非 SerializeField），在 Unity 的 ScriptableObject 生命周期中：
- 如果 **Enter Play Mode Settings → Domain Reload = 关闭**，SO 实例在进出 PlayMode 时不会重新初始化——`_listeners` 会保留上一次 PlayMode 的残留引用
- 这些残留引用指向已被销毁的 MB（如 EntitySystemBootstrap），成为"僵尸监听者"
- 下次 Raise() 时，`_listeners[i].OnBattleCleanup()` 会访问已销毁对象 → `MissingReferenceException`

当前项目可能没开"跳过 Domain Reload"，但这是 Unity 2021+ 的常见优化选项。

**潜在风险**：一旦团队开启 Enter Play Mode Settings 优化，所有 SO 事件通道都会出现僵尸监听者 bug。

**建议方向**：
1. 添加 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 静态方法清空所有 SO 实例的 `_listeners`
2. 或在 `Raise()` 中做空值检查：跳过 `null` 或 `destroyed` 的 MB 监听者
3. 在 TDD 中明确标注此约束

---

## UT-005 | 严重度 🟡中 | OnDestroy 路径清理不完整——当前代码缺 HitReactionHandler.ClearAll

**涉及章节**：§2.1 退场路径清单，§4 Phase C - C5

**质疑**：
代码实证 `BattleController.OnDestroy()` 只调用了 `DanmakuSystem.ClearAll()` 和 `PickupRenderer.Dispose()`，**没有调** `HitReactionHandler.ClearAll()`。但 TDD §2.1 的退场路径清单标注 OnDestroy 只有 `DanmakuSystem.ClearAll + PickupRenderer.Dispose`——这虽然与现状一致，但意味着 OnDestroy 路径可能有飘字残留。

Phase C-C5 写道：`OnDestroy → _onBattleEnd.Raise() + PickupRenderer.Dispose`。走事件通道后 HitReactionHandler 确实会被覆盖。**但问题是**：在 OnDestroy 中调用 `_onBattleEnd.Raise()` 时，EntitySystemBootstrap 可能**已经被销毁**（因为 Unity 不保证 OnDestroy 调用顺序）。

如果 EntitySystemBootstrap 先于 BattleController 被销毁：
1. EntitySystemBootstrap.OnDisable → Unregister（从 `_listeners` 移除）
2. BattleController.OnDestroy → `_onBattleEnd.Raise()` → EntitySystemBootstrap 不在 listener 里了
3. HitReactionHandler.ClearAll 不会被调用

**潜在风险**：场景卸载时清理不完整，飘字残留到下一场景。

**建议方向**：
1. 在 §6 风险表新增条目："OnDestroy 调用顺序不确定"
2. BattleController.OnDestroy 在 Raise 之后额外做兜底清理（直接调 DanmakuSystem.ClearAll，因为它是 DDOL 不会被场景销毁影响）
3. 或明确 OnDestroy 路径的设计意图：是只清 DDOL 资源（因为非 DDOL 会随场景自动销毁），还是要做完整清理

---

## UT-006 | 严重度 🟡中 | Phase D 编辑器验证脚本无法验证"代理注册"模式

**涉及章节**：§4 Phase D

**质疑**：
Phase D 描述的 BattleCleanupValidator 设计：
> D2 | 扫描所有实现 IBattleCleanup 的类型
> D3 | 验证它们都有 `[SerializeField] BattleLifecycleEvent` 字段引用

但按照 §3 的设计，非 MB 系统（HitReactionHandler、CollisionSolver、WaveSpawnerDriver）是由 EntitySystemBootstrap **代理注册**的——它们本身**不实现** IBattleCleanup。Validator 扫描 IBattleCleanup 实现类只能找到 EntitySystemBootstrap，无法发现内部代理的 HitReactionHandler 等是否真的在 OnBattleCleanup 中被调用了。

同样，PickupRenderer 由 BattleController 在 Raise 之后额外调用 Dispose，完全绕过了事件通道——Validator 也检测不到。

**潜在风险**：Validator 给出"0 遗漏"的假阳性，实际上代理模式内部可能有遗漏。

**建议方向**：
1. Validator 增加"白名单检查"：维护一个已知需要清理的系统列表，与 IBattleCleanup 实现类做交叉验证
2. 或在 Validator 中额外扫描 EntitySystemBootstrap.OnBattleCleanup 的方法体，检查是否覆盖了所有注册的子系统
3. 或接受 Validator 的局限性，在文档中注明"Validator 只验证直接注册者，代理注册者需人工审查"

---

## UT-007 | 严重度 🟡中 | `_listeners` 的 Sort 策略每次 Register 都排序——O(n log n) 频繁触发

**涉及章节**：§3.1 BattleLifecycleEvent.Register

**质疑**：
```csharp
public void Register(IBattleCleanup listener)
{
    if (!_listeners.Contains(listener))  // O(n) 线性扫描
    {
        _listeners.Add(listener);
        _listeners.Sort((a, b) => a.CleanupOrder.CompareTo(b.CleanupOrder)); // O(n log n)
    }
}
```

每次 Register 都触发 `List.Sort()`。虽然 n 很小（~8），性能不是问题，但存在 GC 隐患：`List.Sort` 内部会进行 Comparison 委托调用，在 IL2CPP 下可能产生少量 GC。

更重要的是，`Contains` 也是 O(n) 的线性扫描——如果同一个 listener 被多次 Register（比如 OnEnable 触发多次），每次都要遍历。

**潜在风险**：代码层面不是大问题，但考虑到微信小游戏对 GC 的严格要求，值得优化。

**建议方向**：
1. 改为二分插入（InsertSorted），O(n) 移动但无 Sort 调用
2. 或直接在 Raise 前做一次性排序（lazy sort），而不是每次 Register 都排
3. 在 TDD 中标注"当前 n ≤ 10，不做优化"作为有意识决策

---

## UT-008 | 严重度 🟡中 | PickupRenderer.Dispose 不走事件通道——破坏了"统一事件"的设计初衷

**涉及章节**：§3 设计方案尾部，§4 Phase B - B5，Phase C - C5

**质疑**：
TDD 设计的核心目标是"O(1) 的 SO 事件通道"替换"O(路径×系统) 的手动调用"。但 PickupRenderer 被排除在事件通道之外：
> PickupRenderer 的 Dispose 由 BattleController 内部在 Raise 之后额外调用（因为 PickupRenderer 是 BattleController 创建的局部对象，非全局系统）

这意味着 BattleController 的退场代码仍然需要"知道" PickupRenderer 的存在——违反了"新增系统无需修改 BattleController"的成功标准 #2。

如果未来有类似的"局部对象"（比如新增的 ParticleRenderer、TrailRenderer），它们也会被排除在事件通道外，BattleController 又回到了手动调用的模式。

**潜在风险**：设计出现"半事件通道半手动"的混合模式，增加后续维护的认知负担。

**建议方向**：
1. 方案A：PickupRenderer 也走事件通道——在 BattleController 中为它创建一个 wrapper MB（`PickupRendererCleanup`）实现 IBattleCleanup，负责 Dispose
2. 方案B：接受这个例外，但在 §7 "不做的事"中明确说明理由
3. 方案C：PickupRenderer 改为由 BattleController 实现 IBattleCleanup 时在 OnBattleCleanup 中 Dispose（但这让 BattleController 还是要感知它）

---

## UT-009 | 严重度 🟡中 | BattleHUDController.RecycleAllFloatingTexts 当前未被任何退场路径调用

**涉及章节**：§2.1，§2.2，§3.2

**质疑**：
代码实证：当前 BattleController 的 4 条退场路径 + OnDestroy 都**没有调用** `BattleHUDController.RecycleAllFloatingTexts()`。§2.2 列出了它作为需接管清理的系统（#6），§3.2 给了 CleanupOrder=20，但这意味着这是一个**新增的清理行为**——不是"迁移现有调用"而是"修复现有遗漏"。

TDD 应该明确标注这是"修复遗漏"而非"迁移"，否则实施者可能误以为现有代码已经有这个调用。

**潜在风险**：文档模糊性导致 Code Review 时误判。

**建议方向**：在 §2.1 或 §2.2 中加注释"⚠️ 当前退场路径未清理 BattleHUDController 飘字，本 TDD 同时修复此遗漏"

---

## UT-010 | 严重度 🟡中 | Raise() 期间如果监听者内部调 Register/Unregister 会怎样？

**涉及章节**：§3.1 BattleLifecycleEvent.Raise

**质疑**：
`Raise()` 设了 `_isBroadcasting = true` 但没有用它做任何保护：
```csharp
public void Raise()
{
    _isBroadcasting = true;
    for (int i = 0; i < _listeners.Count; i++)
    {
        try { _listeners[i].OnBattleCleanup(); }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }
    _isBroadcasting = false;
}
```

如果某个 listener 的 `OnBattleCleanup()` 内部触发了另一个系统的销毁（比如 DespawnAll 可能导致某个 MB 的 OnDisable → Unregister），那么 `_listeners` 会在遍历过程中被修改——跳过或重复执行某些 listener。

`_isBroadcasting` 字段已经声明但没被利用——这是半成品代码。

**潜在风险**：
- `Unregister` 在广播期间修改了 `_listeners`（`List.Remove` 会移动后续元素），导致跳过部分清理
- 虽然当前场景不太可能发生（退场清理不会触发 OnDisable），但作为框架级代码应当防御

**建议方向**：
1. `Register` 和 `Unregister` 检查 `_isBroadcasting`，广播期间延迟到广播结束后执行
2. 或采用反向遍历（从 Count-1 到 0），Remove 不影响前面的索引
3. 在 TDD 中补充说明 `_isBroadcasting` 的设计意图

---

## UT-011 | 严重度 🟢低 | IBattleCleanup 接口放在 `_Framework/EntitySystem` 命名空间下是否合适？

**涉及章节**：§5 文件变更清单

**质疑**：
路径 `Assets/_Framework/EntitySystem/Scripts/Battle/IBattleCleanup.cs`，命名空间 `MiniGameTemplate.Battle`。
接口定义在 EntitySystem 框架目录下，但命名空间是 Battle——层级归属不一致。IBattleCleanup 是战斗业务层的抽象，不应该放在框架层的 EntitySystem 目录下。

**潜在风险**：框架层反向依赖业务层的概念，破坏分层架构。

**建议方向**：
1. 移到 `Assets/_Game/Scripts/ShooterGame/Core/` 下（与 BattleController 同级）
2. 或移到 `Assets/_Game/Scripts/ShooterGame/Battle/`
3. BattleLifecycleEvent SO 也应该在游戏层而非框架层

---

## UT-012 | 严重度 🟢低 | CleanupOrder 间隔太大（10），且 EntitySystemBootstrap 跳到 100

**涉及章节**：§3.2 CleanupOrder 规约

**质疑**：
间隔 10 留了充足的插入空间，但 EntitySystemBootstrap 直接跳到 100（与之前最高的 60 相差 40）。这暗示"最后执行"的语义，但没有文档化。如果未来插入一个 Order=70 的系统，它不清楚自己是"倒数第二个"还是"随便一个靠后的"。

**潜在风险**：轻微——Order 值的语义不明。

**建议方向**：在 §3.2 表格中加注：`100 = 保留给 EntitySystemBootstrap（必须最后执行），新系统应在 0~90 范围内分配`

---

## UT-013 | 严重度 🟢低 | Phase B 的"添加新路径+保留旧代码"中间状态未说明回退方案

**涉及章节**：§6 风险表

**质疑**：
风险表最后一条提到迁移策略是"添加新路径+保留旧代码"，Phase C 统一删除旧代码。这个策略是对的，但没有说明：
- 如果 Phase B 实施到一半（比如 B4 完成，B5 未开始）需要紧急回滚怎么办？
- 中间状态时哪些系统走事件通道、哪些还是手动——是否有验证手段确认"双路径"不冲突？

**潜在风险**：中间状态下可能出现"双重清理"（事件通道 + 旧手动调用都执行了同一个 ClearAll）。ClearAll 通常幂等所以不会出错，但值得说明。

**建议方向**：在 §6 中加一句："`ClearAll` / `StopShake` 等方法均为幂等操作，双重调用安全"

---

### 攻方整体评价

TDD-07 整体设计思路清晰，问题领域抓得准——4 路径 × 8 系统的维护炸弹确实需要解决。SO 事件通道 + 自注册观察者是 Unity 生态中成熟的模式，选型合理。

但有三个阻塞级问题需要解决：
1. **UT-001**（DDOL 注册矛盾）：文档自相矛盾，实施者会困惑
2. **UT-003**（验收体系不符合两层架构）：项目铁律
3. **UT-005**（OnDestroy 路径的不确定销毁顺序）：可能导致清理不完整

其他中优问题中，**UT-004**（Domain Reload）和 **UT-010**（广播期间修改 listeners）是框架级代码的健壮性问题，建议在 V1 解决。

---

## Round 1：守方回应

| ID | 严重度 | 裁定 | 处置 |
|----|--------|------|------|
| UT-001 | 🔴高 | ✅ 接受 | B1 改为"Awake 中 Register（永久监听者，不注销）"；§6 风险表明确 DDOL 系统不通过 OnDisable 注销 |
| UT-002 | 🔴→🟡 | ⚠️ 部分接受 | 降为🟡。Bootstrap 不在 EntityManager 管理列表中是架构不变量。B8 加注说明 + Debug.Assert 防御 |
| UT-003 | 🔴高 | ✅ 接受 | 项目铁律。Phase A~D 各加门禁验收；Phase E 拆为门禁（E5/E6）+ 全局集成（E1~E4） |
| UT-004 | 🟡中 | ✅ 接受 | §3.1 补充 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 静态清空方法 |
| UT-005 | 🟡→🟡 | ⚠️ 部分接受 | 维持🟡。OnDestroy 非运行时关键路径。C5 改为 `Raise() + DanmakuSystem 兜底 + PickupRenderer.Dispose`；§6 加注 |
| UT-006 | 🟡中 | ⚠️ 部分接受 | Phase D 注明 Validator 局限性——仅验证直接注册者，代理注册需 Code Review 覆盖。V2 考虑 attribute 标注方案 |
| UT-007 | 🟡中 | 📝 记录 | n ≤ 10，不做优化。§3.1 加注释说明有意识决策 |
| UT-008 | 🟡中 | ⚠️ 部分接受 | PickupRenderer 是 BC 私有工具而非全局系统，不纳入事件通道。§7 新增条目明确说明理由 |
| UT-009 | 🟡中 | ✅ 接受 | §2.1/§2.2 加注"⚠️ 新增——当前退场路径未清理 BattleHUDController 飘字，本 TDD 同时修复此遗漏" |
| UT-010 | 🟡中 | ✅ 接受 | 补充 `_isBroadcasting` 保护逻辑——广播期间 Unregister 延迟执行（pending removal list） |
| UT-011 | 🟢低 | ✅ 接受 | 文件路径改为 `Assets/_Game/Scripts/ShooterGame/Battle/`，BattleLifecycleEvent 同移 |
| UT-012 | 🟢低 | ✅ 接受 | §3.2 表格加脚注"100 = 保留给 Bootstrap（必须最后），新系统 0~90" |
| UT-013 | 🟢低 | ✅ 接受 | §6 加注"ClearAll 等方法均为幂等操作，双重调用安全" |

### 守方回应详情

#### UT-001：✅ 接受——DDOL 注册矛盾
攻方说得对。B1 步骤与 §6 风险表自相矛盾。DanmakuSystem 作为 DDOL 单例，生命周期是应用级的，`OnDisable` 在正常游戏流程中不会触发。改为 Awake 注册、永不注销的"永久监听者"模式。

#### UT-002：⚠️ 部分接受（降为🟡）
Bootstrap 不在 EntityManager 管理列表中是**架构层面的不变量**——EntityManager.DespawnAll() 只操作 `_activeEntities` 列表中的 Entity 实例。EntitySystemBootstrap 是场景级管理者 MB，不会出现在 Entity 列表中。这不是"隐含假设"而是清晰的设计边界。

但同意添加断言和注释，防御性更好。

#### UT-003：✅ 接受——项目铁律
两层验收体系是项目规范，必须遵守。

#### UT-004：✅ 接受——Domain Reload 防护
好问题。标准做法是 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`，可以无侵入地解决所有 SO 的状态残留问题。

#### UT-005：⚠️ 部分接受
攻方的技术分析正确——OnDestroy 顺序确实不确定。但需要明确 OnDestroy 的语义定位：
1. **正常退场**（Victory/Defeat/PauseQuit）：走 `_onBattleEnd.Raise()` → 完整清理 → Pop → 场景卸载。此时 OnDestroy 只是善后，不需要完整清理
2. **编辑器 Stop PlayMode / 应用退出**：OnDestroy 是唯一的清理机会，但此时"清理不干净"的影响可以接受

兜底策略：OnDestroy 中 Raise + DanmakuSystem 显式兜底（因为它是 DDOL，一定存活）。

#### UT-006：⚠️ 部分接受
Validator 100% 覆盖需要反射分析方法体，V1 不做，ROI 不高。注明局限性 + Code Review 覆盖是务实选择。

#### UT-007：📝 记录不改
n=8 的 Sort 在 IL2CPP 下的 GC 影响接近 0。如果未来 listener 数显著增长（>50）再优化。

#### UT-008：⚠️ 部分接受
PickupRenderer 是 `new PickupRenderer()`——由 BattleController 创建、BattleController 销毁。它的生命周期等同于 BattleController 的局部变量。
类比：你不会把 `List<T>.Clear()` 放进全局事件通道。PickupRenderer 是工具，不是系统。

但同意在 §7 明确说明，避免后来人困惑。

#### UT-009：✅ 接受
确实是遗漏修复，应该标注清楚。

#### UT-010：✅ 接受
`_isBroadcasting` 已经声明但没用是半成品。补充完整保护逻辑。

#### UT-011~013：✅ 接受
都是合理的改善建议，改动成本极低。

### 守方总结

13 个问题中：
- **10 个接受/部分接受**（含 2 个降级）
- **1 个记录不改**（UT-007）
- **UT-002** 从 🔴 降为 🟡（架构不变量，非隐含假设）
- **UT-005** 原标 🟡，守方认可维持 🟡

残留分歧点：
1. UT-002 的严重度——攻方认为是 🔴 阻塞，守方认为是 🟡（架构保证足够，加断言即可）
2. UT-008 的处理方式——攻方建议纳入事件通道，守方坚持 PickupRenderer 不是全局系统

---

## Round 2：攻方复审

**聚焦残留分歧**

### UT-002 复审
**结论：✅ 接受守方降级为 🟡。收敛。**

守方论据成立——EntityManager.DespawnAll() 只操作 `_activeEntities` 列表中的 Entity 实例。EntitySystemBootstrap 是场景级管理者 MB，不会出现在 Entity 列表中。这是清晰的层级划分，不是隐含假设。加 Debug.Assert 是充分的防御措施。

### UT-008 复审
**结论：⚠️ 条件性接受。**

接受守方的"PickupRenderer 是工具不是系统"论据。类比合理——`new` 创建的局部对象确实不应纳入全局事件通道。

但攻方追加一个小要求：§7 的说明中应给出**判定标准**，帮助未来开发者区分"局部工具"和"全局系统"：
> **判定标准**：由 BC `new` 创建且仅由 BC 引用的对象 = 局部工具，由 BC.OnDestroy 自行清理；由场景注入（`[SerializeField]`）或全局单例的对象 = 全局系统，走事件通道。

如果守方同意补充判定标准，**收敛。**

---

## Round 2：守方回应

### UT-002：✅ 收敛确认
双方一致同意 🟡 + Debug.Assert。

### UT-008：✅ 接受攻方追加条件，收敛
判定标准清晰且务实，同意写入 §7。最终措辞：

> **局部对象例外**：由 BattleController `new` 创建且仅由 BC 引用的私有工具（如 PickupRenderer）不纳入 SO 事件通道——由 BC 自身在 OnDestroy 中清理。
> **判定标准**：`new` 创建 + 仅 BC 引用 = 局部工具 → BC 自管；`[SerializeField]` 注入或全局单例 = 全局系统 → 走事件通道。

**全部 13 个问题收敛。PK 结束。**

---

## PK 最终裁定

| ID | 最终严重度 | 最终裁定 | 修改范围 |
|----|-----------|---------|---------|
| UT-001 | 🔴高 | ✅ 修改 TDD | §4 B1 + §6 风险表 |
| UT-002 | 🟡中（降级） | ✅ 修改 TDD | §4 B8 + §6 风险表 |
| UT-003 | 🔴高 | ✅ 修改 TDD | §4 Phase A~E 验收体系重构 |
| UT-004 | 🟡中 | ✅ 修改 TDD | §3.1 新增 RuntimeInitializeOnLoadMethod |
| UT-005 | 🟡中 | ✅ 修改 TDD | §4 C5 + §6 风险表 |
| UT-006 | 🟡中 | ✅ 修改 TDD | §4 Phase D 注明局限性 |
| UT-007 | 🟡中 | 📝 记录 | §3.1 加注释 |
| UT-008 | 🟡中 | ✅ 修改 TDD | §7 新增条目 + 判定标准 |
| UT-009 | 🟡中 | ✅ 修改 TDD | §2.1/§2.2 加遗漏标注 |
| UT-010 | 🟡中 | ✅ 修改 TDD | §3.1 完善 _isBroadcasting 保护 |
| UT-011 | 🟢低 | ✅ 修改 TDD | §5 文件路径调整 |
| UT-012 | 🟢低 | ✅ 修改 TDD | §3.2 脚注 |
| UT-013 | 🟢低 | ✅ 修改 TDD | §6 幂等性说明 |

**PK 统计**：
- 总问题数：13（2🔴 + 8🟡 + 3🟢）
- 接受/修改：12
- 记录不改：1（UT-007 Sort 优化）
- 收敛轮次：2 轮（高效）

> **PK 状态**：✅ 已收敛

---
---

# 增量 PK 评审 — v0.2 增量复审

> **文档版本**：v0.2（PK Approved）
> **攻方角色**：微信小游戏 WebGL/IL2CPP 性能约束专家（10+ 年 Unity 移动端/WebGL 优化经验，专精 GC 控制、IL2CPP 代码生成、微信小游戏运行时限制）
> **守方角色**：软件架构师（原守方延续）
> **开始时间**：2026-05-26 09:48
> **最大轮次**：8
> **增量 PK 状态**：✅ 已收敛（1 轮，2026-05-26 09:48 ~ 10:10）

---

## Round 1：攻方提问

### 审查视角声明

上一轮 PK 从 Unity 编辑器工具开发者视角审查了生命周期正确性、注册时序、验收体系等问题。本轮从**微信小游戏 WebGL 运行时**视角出发，聚焦以下盲区：
1. **GC/内存分配**——微信小游戏对每帧 GC 极度敏感（目标 0 GC/frame）
2. **IL2CPP 代码生成约束**——接口默认实现、泛型虚调用、反射等的 AOT 兼容性
3. **WebGL 单线程模型**——无 async/await 的协程降级、阻塞 API 的规避
4. **包体/冷启动**——新增文件和 SO 资产对包体的影响
5. **微信小游戏特有限制**——DontDestroyOnLoad 在微信环境下的行为差异

### 代码分析基础（CodeGraph 实证）

| 文件 | 关键发现 |
|------|---------|
| `Singleton<T>` | 泛型单例 + SingletonResetRegistry 静态列表，已有 `[RuntimeInitializeOnLoadMethod]` 清空机制 |
| `DanmakuSystem.cs` | 手写单例（非 Singleton<T>），Awake 中 `DontDestroyOnLoad` + `Instance = this` |
| `BattleController.cs` | `_pickupRenderer` 由 `new PickupRenderer()` 创建；`FindObjectOfType<EntitySystemBootstrap>()` 在 Awake 中调用 |
| `PickupRenderer.cs` | IDisposable + RenderBatchManager，Dispose 释放 _batchManager |
| `BattleLifecycleEvent` (TDD 设计) | SO + `List<IBattleCleanup>` + `_isBroadcasting` + `_pendingRemoval` |

---

### WX-001 | 严重度 🟡中 | IBattleCleanup.CleanupOrder 接口默认实现在 IL2CPP/WebGL 下的 AOT 风险

**涉及章节**：§3.1 IBattleCleanup 接口

**质疑**：
```csharp
public interface IBattleCleanup
{
    int CleanupOrder => 0; // C# 8 接口默认实现
    void OnBattleCleanup();
}
```

C# 8 的接口默认实现（Default Interface Methods, DIM）在 Unity IL2CPP 下的支持情况：
1. Unity 2021 LTS 开始**基本支持** DIM，但 IL2CPP 对 DIM 的代码生成在某些边界场景存在已知 bug（特别是涉及结构体实现、泛型接口等）
2. 更关键的是——DIM 的调用走的是**虚分派**（virtual dispatch），在 IL2CPP 下比普通虚方法调用多一层间接（需要查找接口方法表中的默认实现 slot）
3. 微信小游戏的 WebGL/WASM 目标对虚分派特别敏感——每次 `_listeners[i].CleanupOrder` 都是一次接口属性虚调用

虽然 n ≤ 10 时性能影响微不足道，但作为**框架级代码**的设计选择，使用 DIM 而非显式实现带来的架构收益需要与 IL2CPP 兼容性风险做权衡。

**潜在风险**：
- IL2CPP 对 DIM 的 bug 可能导致 `CleanupOrder` 返回错误值
- 未来如果某个实现者是 struct，DIM 在值类型上的行为更加不确定
- 增加编译产物体积（DIM 需要额外的接口方法表条目）

**建议方向**：
1. 方案A：去掉 DIM，改为普通接口方法 `int CleanupOrder { get; }`，每个实现者显式返回值
2. 方案B：改为抽象基类 `abstract class BattleCleanupBase : MonoBehaviour`（但限制了继承链）
3. 方案C：保留 DIM 但在 TDD 中标注"已验证 Unity 2022+ IL2CPP 对 DIM 的支持"

---

### WX-002 | 严重度 🟡中 | Register 的 Contains + Sort 的 GC 细节——接口类型 List 的隐式开销

**涉及章节**：§3.1 BattleLifecycleEvent.Register

**质疑**：
```csharp
if (!_listeners.Contains(listener))  // O(n) 线性扫描
{
    _listeners.Add(listener);
    _listeners.Sort((a, b) => a.CleanupOrder.CompareTo(b.CleanupOrder)); // O(n log n)
}
```

上一轮 UT-007 讨论了 Sort 的性能，结论是"n ≤ 10 可忽略"。但遗漏了**微信小游戏关键细节**：

1. `_listeners.Contains(listener)` 使用 `EqualityComparer<IBattleCleanup>.Default`——对于接口类型，Default 会走 `Object.Equals` → 对于 MB 实现者会调用 Unity 的重载 `==` → 涉及 native interop 开销
2. 每次 Register 都要遍历——如果 Register 在 OnEnable 中调用（场景加载时多个系统同帧 Enable），开销在同一帧累积
3. Sort 的 lambda `(a, b) => ...` 不捕获外部变量——IL2CPP 会生成静态委托缓存——✅ 安全
4. 但 `List<IBattleCleanup>.Sort` 内部的 ArraySortHelper 在接口类型上的某些路径可能产生临时 GC

**结论**：Register 发生在场景加载期间（非战斗帧），GC 峰值不影响战斗 FPS。低风险但优化成本也低。

**建议方向**：
1. Contains 改为手动 `ReferenceEquals` 遍历（绕过 Unity 重载 `==`，对于注册场景 `ReferenceEquals` 就足够了）
2. Sort 的 lambda 提取为 `private static readonly Comparison<IBattleCleanup>` 字段
3. 在 TDD 中标注"Register 在场景加载期间执行，少量 GC 可接受"

---

### WX-003 | 严重度 🟡中 | C5 的 `DanmakuSystem.Instance?.ClearAll()`——`?.` 在已销毁 Unity Object 上的语义陷阱

**涉及章节**：§4 Phase C - C5

**质疑**：
TDD C5：
```
OnDestroy → _onBattleEnd.Raise() + DanmakuSystem.Instance?.ClearAll()（DDOL 兜底）
```

`?.`（null-conditional operator）在 C# 中检查的是 `ReferenceEquals(obj, null)`。但 Unity Object 的"null"有两层含义：
1. **C# 引用为 null**：`ReferenceEquals(obj, null) == true` → `?.` 正确跳过
2. **Unity Object 已销毁**：`ReferenceEquals(obj, null) == false` 但 `obj == null` 为 true（Unity 重载 `==`）

如果 DanmakuSystem 在 BattleController 之前被销毁（应用退出时 DDOL 对象的销毁顺序不确定），`DanmakuSystem.Instance` 的静态字段可能仍持有已销毁对象的引用（因为 DanmakuSystem.OnDestroy 会清空 Instance——但如果两者**同帧** OnDestroy，顺序不确定）。

**场景分析**：
- 正常退场：不走 OnDestroy → ❌ 不触发此问题
- 应用退出：DanmakuSystem 是 DDOL，BattleController 不是 → BC.OnDestroy 先执行 → `DanmakuSystem.Instance` 此时还存活 → ✅ 安全
- 编辑器 Stop Play：销毁顺序不确定 → ⚠️ 可能触发

**实际风险评估**：低——编辑器 Stop Play 的 MissingReferenceException 不影响运行时。但这是**经典 Unity 陷阱**，TDD 中应标注。

**建议方向**：
1. C5 代码示例改为 `if (DanmakuSystem.Instance != null) DanmakuSystem.Instance.ClearAll();`——使用 Unity 重载 `==`
2. §6 风险表加注"注意 Unity Object 的 `?.` vs `!= null` 语义差异"

---

### WX-004 | 严重度 🟢低 | _pendingRemoval List 容量残留

**涉及章节**：§3.1 BattleLifecycleEvent.Raise

**质疑**：`List.Clear()` 不释放内部数组。但 `_pendingRemoval` 初始容量 4，即使扩容也只会占用极少内存（n ≤ 10 的场景下最多 40 字节）。

**建议方向**：📝 记录即可——不需要修改。

---

### WX-005 | 严重度 🟢低 | BattleCleanupValidator 路径与 UT-011 不一致

**涉及章节**：§5 文件变更清单

**质疑**：UT-011 把 IBattleCleanup 和 BattleLifecycleEvent 移到了 `Assets/_Game/Scripts/ShooterGame/Battle/`，但 BattleCleanupValidator 仍在 `Assets/_Framework/EntitySystem/Editor/`。Validator 依赖游戏层接口——框架 Editor 程序集反向引用游戏层。如果项目使用 asmdef 隔离，会编译失败。

**建议方向**：移到 `Assets/_Game/Scripts/ShooterGame/Editor/BattleCleanupValidator.cs`

---

### WX-006 | 严重度 🟡中 | Retry 路径 Raise → SO 重置的顺序——OnBattleCleanup 中不应依赖 SO 变量值

**涉及章节**：§3.4 Retry 特殊路径

**质疑**：
```csharp
private void ResetBattleRuntimeState()
{
    _onBattleEnd.Raise();       // ← 此时 SO 变量仍为旧值
    _currentWaveIndex.SetValue(1); // ← Raise 之后才重置
    _killCount.SetValue(0);
}
```

当前代码安全——所有 OnBattleCleanup 实现都不读 SO 变量。但 TDD 没有明确约束这一点。未来新增的 IBattleCleanup 实现者可能在 OnBattleCleanup 中读 SO 变量，读到不一致的值。

**建议方向**：§3.4 加注"⚠️ OnBattleCleanup 方法不应依赖 SO 变量状态——Retry 路径中 Raise 先于 SO 重置执行"

---

### WX-007 | 严重度 🟡中 | DanmakuSystem Awake Register 的 null 防御——SerializeField 漏拖导致静默失败

**涉及章节**：§4 Phase B - B1

**质疑**：
B1 设计 DanmakuSystem 在 Awake 中 `_onBattleEnd.Register(this)`。`_onBattleEnd` 是 `[SerializeField]`——需要在 Inspector 手动拖拽赋值。

如果忘记拖拽：`_onBattleEnd` 为 null → `Register` 调用 NullReferenceException → Awake 中断 → DanmakuSystem 部分初始化。

代码实证：现有的 `_onBattleEnd?.Register(this)` 用了 `?.` → 静默跳过 → DanmakuSystem 看起来正常但退场时不被清理 → 弹丸残留。

Phase D Validator 能否检测到？取决于实现——如果 Validator 扫描场景中的 MB 实例并检查 SerializeField 引用，可以。但当前 Validator 设计（D2~D3）只扫描**类型**是否有 SerializeField 字段，不检查**实例**的赋值。

**建议方向**：
1. Awake 中加 `if (_onBattleEnd == null) Debug.LogError("[DanmakuSystem] _onBattleEnd SO 未赋值！退场时弹丸不会被自动清理。");`
2. Phase D Validator 增加实例级检查：扫描场景中所有含 BattleLifecycleEvent 引用的 MB，检查引用是否为 null

---

### WX-008 | 严重度 🟢低 | 包体影响评估

**涉及章节**：§5

**质疑**：新增 ~100 行有效 C# + 1KB SO 资产，IL2CPP/WASM 增量 ≤ 5KB。首包限制 20MB 下可忽略。

**建议方向**：📝 记录即可。

---

### 攻方整体评价

TDD v0.2 经过上一轮 PK 后质量很高。从微信小游戏 WebGL/IL2CPP 视角看：

**阻塞项**：0 个——无 🔴 高严重度问题。

**值得修改的**（5 个 🟡）：
1. **WX-001**（DIM 在 IL2CPP 下的风险）——低成本高防御
2. **WX-002**（Contains/Sort 的接口类型隐式 GC）——优化成本低
3. **WX-003**（`?.` vs `!= null` Unity 对象陷阱）——经典坑，必须文档化
4. **WX-006**（Retry 路径 SO 顺序约束）——文档化约束即可
5. **WX-007**（SerializeField 漏拖防御）——运行时安全网

**记录不改的**（3 个 🟢）：WX-004、WX-005、WX-008

---

## Round 1：守方回应

| ID | 严重度 | 裁定 | 处置 |
|----|--------|------|------|
| WX-001 | 🟡中 | ✅ 接受 | 去掉 DIM，改为显式接口属性 `int CleanupOrder { get; }`，所有实现者显式返回值 |
| WX-002 | 🟡中 | ⚠️ 部分接受 | Contains 改为 ReferenceEquals 遍历；Sort lambda 提取为 static readonly 字段；加注释说明场景加载期间 GC 可接受 |
| WX-003 | 🟡中 | ✅ 接受 | C5 改为 `if (DanmakuSystem.Instance != null) DanmakuSystem.Instance.ClearAll()`；§6 风险表加注 Unity Object `?.` 陷阱 |
| WX-004 | 🟢低 | 📝 记录 | 不修改。_pendingRemoval 容量残留 ≤ 40 字节，可忽略 |
| WX-005 | 🟢低 | ✅ 接受 | Validator 路径改为 `Assets/_Game/Scripts/ShooterGame/Editor/BattleCleanupValidator.cs` |
| WX-006 | 🟡中 | ✅ 接受 | §3.4 加注"OnBattleCleanup 不应依赖 SO 变量状态"约束 |
| WX-007 | 🟡中 | ✅ 接受 | B1 加 null 检查 + Debug.LogError；Phase D Validator 增加实例级 SerializeField 检查 |
| WX-008 | 🟢低 | 📝 记录 | 不修改。包体增量 ≤ 5KB |

### 守方回应详情

#### WX-001：✅ 接受——去掉 DIM
攻方分析到位。虽然 Unity 2022+ 对 DIM 的支持已经改善，但微信小游戏目标平台（WebGL/WASM + IL2CPP）是我们最保守的编译目标。去掉 DIM 的成本极低——每个实现者多写一行 `public int CleanupOrder => X;`，换取零 IL2CPP 兼容性风险。

**修改**：§3.1 IBattleCleanup 改为：
```csharp
public interface IBattleCleanup
{
    int CleanupOrder { get; }
    void OnBattleCleanup();
}
```
每个实现者显式实现 `CleanupOrder` 属性。

#### WX-002：⚠️ 部分接受
攻方说的 Contains 走 `EqualityComparer<T>.Default` → Unity `==` 重载的分析是正确的。但需要明确：Register 只在场景加载时调用（OnEnable/Awake），不在战斗帧中执行——所以这不是性能关键路径。

**同意优化**的部分：
1. Contains → 手动 ReferenceEquals 遍历——成本为零，收益明确
2. Sort lambda → static readonly 字段——IL2CPP 下确保无重复委托分配

**不同意优化**的部分：
- 不需要重写 ArraySortHelper 路径——n ≤ 10 的 Sort 本身不是瓶颈

**修改**：§3.1 BattleLifecycleEvent.Register 改为：
```csharp
private static readonly Comparison<IBattleCleanup> s_orderComparer =
    (a, b) => a.CleanupOrder.CompareTo(b.CleanupOrder);

public void Register(IBattleCleanup listener)
{
    // WX-002: 用 ReferenceEquals 绕过 Unity 重载 ==
    for (int i = 0; i < _listeners.Count; i++)
    {
        if (ReferenceEquals(_listeners[i], listener))
            return;
    }
    _listeners.Add(listener);
    _listeners.Sort(s_orderComparer);
}
```

#### WX-003：✅ 接受——Unity Object 的 `?.` 陷阱
这是 Unity 开发者的经典坑。虽然在 DDOL 场景下实际触发概率极低，但 TDD 作为实施指导文档必须给出正确的代码示例。

**修改**：
- C5 代码改为 `if (DanmakuSystem.Instance != null) DanmakuSystem.Instance.ClearAll();`
- §6 风险表新增条目："Unity Object 的 `?.` 与 `!= null` 语义不同——`?.` 使用 C# 原生 null 检查（ReferenceEquals），无法检测已销毁的 Unity Object。所有 Unity Object 引用的 null 检查必须使用 `!= null`（Unity 重载 `==`）"

#### WX-004：📝 记录
同意不修改。_pendingRemoval 初始容量 4 × 引用大小（WASM32=4字节）= 16 字节。即使极端扩容也微不足道。

#### WX-005：✅ 接受
与 UT-011 同理——保持依赖方向干净。编辑器脚本也应该放在游戏层。

**修改**：§5 Validator 路径改为 `Assets/_Game/Scripts/ShooterGame/Editor/BattleCleanupValidator.cs`

#### WX-006：✅ 接受——Retry 顺序约束文档化
攻方分析正确——当前安全但缺乏防护。文档化约束是最低成本的防御措施。

**修改**：§3.4 Retry 代码注释加注：
```csharp
// ⚠️ 约束：OnBattleCleanup 实现中不应依赖 SO 变量状态。
// Retry 路径中 Raise() 先于 SO 变量重置执行，
// 此时 SO 变量仍为旧值。
_onBattleEnd.Raise();
```

#### WX-007：✅ 接受——SerializeField 漏拖防御
这是运行时最容易踩的坑之一。DanmakuSystem 是 DDOL 单例——如果首次进入战斗时 SO 引用为 null，整场游戏的退场清理都会静默失败。

**修改**：
1. B1 实施代码加 null 检查：
```csharp
private void Awake()
{
    // ... 现有单例初始化 ...
    if (_onBattleEnd != null)
        _onBattleEnd.Register(this);
    else
        Debug.LogError("[DanmakuSystem] _onBattleEnd SO 未赋值！退场时弹丸不会被自动清理。");
}
```
2. Phase D Validator 增加**实例级检查**：扫描场景中所有含 `[SerializeField] BattleLifecycleEvent` 的 MB 实例，检查引用是否为 null → 输出到 Console

#### WX-008：📝 记录
新增代码 ≤ 5KB WASM 增量，对 20MB 首包限制无影响。

### 守方总结

8 个问题中：
- **6 个接受/部分接受**（WX-001~003, 005~007）
- **2 个记录不改**（WX-004, WX-008）
- **0 个分歧**——全部收敛

**本轮增量 PK 一轮收敛。**

---

## 增量 PK 最终裁定

| ID | 最终严重度 | 最终裁定 | 修改范围 |
|----|-----------|---------|---------:|
| WX-001 | 🟡中 | ✅ 修改 TDD | §3.1 IBattleCleanup 去掉 DIM |
| WX-002 | 🟡中 | ✅ 修改 TDD | §3.1 Register 改用 ReferenceEquals + static readonly 委托 |
| WX-003 | 🟡中 | ✅ 修改 TDD | §4 C5 + §6 风险表 |
| WX-004 | 🟢低 | 📝 记录 | — |
| WX-005 | 🟢低 | ✅ 修改 TDD | §5 Validator 路径 |
| WX-006 | 🟡中 | ✅ 修改 TDD | §3.4 加注释约束 |
| WX-007 | 🟡中 | ✅ 修改 TDD | §4 B1 + Phase D Validator |
| WX-008 | 🟢低 | 📝 记录 | — |

**增量 PK 统计**：
- 总问题数：8（0🔴 + 5🟡 + 3🟢）
- 修改：6
- 记录不改：2
- 收敛轮次：1 轮（高效——v0.2 基础扎实）

> **增量 PK 状态**：✅ 已收敛（1 轮，2026-05-26 09:48 ~ 10:10）