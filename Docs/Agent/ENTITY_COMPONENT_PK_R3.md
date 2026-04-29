# Entity-Component TDD v2.2 PK 评审 Round 3

> **PK 类型**：软件架构师 vs Unity 架构师  
> **目标文档**：`ENTITY_COMPONENT_TDD.md` v2.2  
> **攻方**：软件架构师 Agent（DDD、架构模式、权衡分析、可维护性）  
> **守方**：Unity 架构师 Agent（Unity 引擎、性能、WebGL 平台）  
> **最大轮次**：3  
> **日期**：2026-04-26  
> **前置 PK**：R1 技术 PK（17 问题）、R2 策划工作流 PK（12 问题）

---

## Round 1 — 软件架构师攻方

### SA-001 | 严重度 🔴高 | PierceHitMask 位宽与 TargetRegistry 扩容不兼容

**涉及章节**：§3.9, §3.5

**质疑**：TDD v2.2 将 TargetRegistry 从 16 扩容到 64（D-01），但代码中 `BulletCore.PierceHitMask` 是 `ushort`（16 位），只能覆盖 0~15 号槽位。CollisionSolver.SolveBulletVsTarget() 用 `(ushort)(1 << t)` 做位掩码，t=16+ 时整数溢出。文档虽然提到了 PierceHitMask 冲突风险，但只讨论了"动态注册/注销"场景下的冲突，**完全没提到位宽不够的硬约束**。

**潜在风险**：16~63 号槽位的 Entity 被穿透弹命中后，无法记录 Pierce 冷却，导致同一弹丸对同一目标**每帧重复造成伤害**。这不是边界情况——50 个敌人的场景里大部分敌人都在 16+ 槽位。

**建议方向**：`PierceHitMask` 从 `ushort` 改为 `ulong`（64 位），同步修改 CollisionSolver 的位操作。这会影响 BulletCore 结构体大小（+6 bytes），需要评估对 L2 缓存压力的影响，并在 TDD 中明确记录这个权衡。

**状态**：⏳ 待回应

---

### SA-002 | 严重度 🔴高 | ICollisionTarget.Faction 返回类型与 EnumCamp 不兼容

**涉及章节**：§3.5, §3.11, BC-01.6

**质疑**：TDD 声称 Phase 1 统一使用 `EnumCamp`（D-02，替代 BulletFaction），但现有 `ICollisionTarget` 接口明确定义 `BulletFaction Faction { get; }`。CollisionSolver 全线使用 `BulletFaction` 做阵营过滤。TDD §3.5 示例代码中 `CollisionComponent.Faction => _owner.Camp` 声明返回 `EnumCamp`，但 `ICollisionTarget.Faction` 的返回类型是 `BulletFaction`——**类型不匹配，编译不过**。

**潜在风险**：Phase 1 要么改 `ICollisionTarget` 接口（影响所有现有实现），要么在 CollisionComponent 里做隐式转换（违反类型安全原则）。这个迁移成本在 TDD 中被严重低估了。

**建议方向**：TDD 必须明确迁移方案——是"全局替换 BulletFaction → EnumCamp"（大改），还是"CollisionComponent 内部做类型转换"（小改但有隐患）。建议在 P1.x 步骤中新增一步"阵营枚举统一迁移"，AC 包含"全项目零 BulletFaction 引用"。

**状态**：⏳ 待回应

---

### SA-003 | 严重度 🟡中 | EntityPool 以 EntityConfigSO 引用为 Dictionary Key 的隐式成本

**涉及章节**：§3.6, §3.7

**质疑**：`EntityManager._pools` 是 `Dictionary<EntityConfigSO, EntityPool>`。ScriptableObject 作为 Dictionary Key 时，每次 `TryGetValue` 涉及 UnityEngine.Object 的 operator== 隐式比较（native→managed interop）。更重要的是，SO 在 Domain Reload 后 InstanceID 会变——如果使用了 Enter Play Mode Settings 关闭 Domain Reload（Unity 2021+ 常见优化），预热的 pool Dictionary 可能失效。

**潜在风险**：隐式 interop 成本在 Profiler 中不易发现；Domain Reload 兼容性陷阱。

**建议方向**：用 `int configId` 或 `RuntimeId` 做 Dictionary Key，或明确标注"不兼容 Skip Domain Reload"。

**状态**：⏳ 待回应

---

### SA-004 | 严重度 🟡中 | TypeIdCounter 与 Domain Reload 的 TypeId 乱序

**涉及章节**：§3.4

**质疑**：`TypeId<T>.Value` 是 `static readonly`，只在类型首次访问时初始化。`TypeIdCounter` 的 `[RuntimeInitializeOnLoadMethod]` 重置 `_next=0`，但 `static readonly` 字段在 Domain Reload 后**不会被重新赋值**（CLR 语义）。如果 Domain Reload 前后 `TypeId<T>` 的访问顺序不同，同一 T 会得到不同的 TypeId，EventBus 订阅/发布完全错乱。

**潜在风险**：运行时静默 bug——事件发布到错误 handler 槽位，调试极难。

**建议方向**：改 `static readonly` 为 `static int` + 懒初始化模式，或明确 Domain Reload 兼容策略。

**状态**：⏳ 待回应

---

### SA-005 | 严重度 🟡中 | EntityViewBridge.SyncAll() Dictionary 遍历 GC

**涉及章节**：§3.15

**质疑**：`EntityViewBridge._views` 是 `Dictionary<uint, GameObject>`，`SyncAll()` 每帧遍历产生 ~40 bytes GC Alloc（Mono 运行时 Dictionary Enumerator 装箱）。违反"零 GC"核心承诺。

**潜在风险**：60fps 持续 GC 压力，微信小游戏环境更敏感。

**建议方向**：改用平铺数组同步遍历（for 循环零 GC）。

**状态**：⏳ 待回应

---

### SA-006 | 严重度 🟡中 | EntitySpawner AllCleared 触发与延迟销毁冲突

**涉及章节**：§3.14

**质疑**：EntitySpawner 的 AllCleared 触发模式需要查询存活数，但 (1) EntityManager 无此 API，(2) 延迟销毁 Entity 在 `_pendingDespawn` 中仍被计为活跃，AllCleared 判定会因 1 帧延迟误触发下一波。

**潜在风险**：刷怪逻辑与 Entity 生命周期的边界不清晰。

**建议方向**：补充 EntityManager 查询 API + 明确 AllCleared 判定在延迟销毁执行后。

**状态**：⏳ 待回应

---

### SA-007 | 严重度 🟢低 | GetComponent<T> O(1) 声称与实际 O(16) 实现不一致

**涉及章节**：§3.2, BC-01.2

**质疑**：BC-01.2 承诺"O(1) 查询"，但泛型版 `GetComponent<T>()` 实际是 for 循环 + `is T` 类型检查（O(16)）。只有 `GetComponent(ComponentType)` 是真 O(1)。

**潜在风险**：文档声称和实现不一致，降低文档信任度。

**建议方向**：修正 BC-01.2 措辞，区分"枚举版 O(1)"和"泛型版 O(N)"。

**状态**：⏳ 待回应

---

## Round 1 — Unity 架构师守方回应

### SA-001（🔴高 PierceHitMask 位宽）→ ✅ 接受并修正

**回应**：这是真正的阻塞问题——`ushort` 16 位与 64 槽位确实不兼容。

**方案**：
1. `BulletCore.PierceHitMask` 从 `ushort` → `ulong`（64 位）
2. `CollisionSolver.SolveBulletVsTarget()` 中 `(ushort)(1 << t)` → `(1UL << t)`
3. BulletCore 结构体大小：48 → 56 bytes（+8 bytes 对齐后），2048 × 56 = 112KB，仍在 L2 缓存友好范围内
4. TDD §3.9 新增 PierceHitMask 位宽权衡说明

**TDD 变更**：§3.9 更新 PierceHitMask 说明 + §3.13 更新内存预算

**降级判定**：🔴→✅ 已消除

---

### SA-002（🔴高 BulletFaction vs EnumCamp 不兼容）→ ✅ 接受并修正

**回应**：TDD 确实低估了迁移成本。`ICollisionTarget.Faction` 返回 `BulletFaction`，CollisionComponent 返回 `EnumCamp`，编译不过。

**方案**：
1. Phase 1 新增 **P1.0 阵营枚举统一迁移** 步骤（在所有 Entity 编码之前执行）
2. 全局机械替换 `BulletFaction` → `EnumCamp`（枚举值相同 Enemy=0/Player=1/Neutral=2，纯 rename 不改逻辑）
3. 涉及文件：`ICollisionTarget.cs`、`CollisionSolver.cs`、`BulletFaction.cs`（删除）、所有引用点
4. P1.0 AC："全项目零 `BulletFaction` 引用 + 编译通过 + DanmakuDemo 行为不变"

**TDD 变更**：§六 Phase 1 表格新增 P1.0

**降级判定**：🔴→✅ 已消除

---

### SA-003（🟡中 SO Dictionary Key 隐式成本）→ ✅ 接受但不阻塞

**回应**：澄清一个技术细节——Unity SO 做 Dictionary Key 时底层走 `Object.GetHashCode()` = InstanceID + `Object.Equals()` = reference equality。**不会**触发 `operator==` 的 native interop（那是 `==` 运算符才走的路径，Dictionary 用 `Equals`/`GetHashCode`）。所以性能没问题。

但 Skip Domain Reload 兼容性确实是隐患——Phase 1 SO 数量 <20，不是瓶颈。

**方案**：TDD §八 风险表新增此限制说明，不改数据结构。

**TDD 变更**：§八风险表追加一行

**降级判定**：🟡→⚪ 已知限制，记录在案

---

### SA-004（🟡中 TypeId Domain Reload 乱序）→ ✅ 接受并修正

**回应**：`static readonly` 在 Domain Reload 后确实不会被重新赋值——这是 C# CLR 语义。攻方完全正确。

**方案**：
1. `TypeId<T>.Value` 从 `static readonly int` → `static int`，初始值 `-1`
2. `TypeId<T>.Get()` 方法：`if (Value < 0) Value = TypeIdCounter.Next(); return Value;`
3. `TypeIdCounter.Reset()` 维护已分配类型列表，Domain Reload 时统一重置为 -1
4. EventBus 使用处改为调用 `TypeId<T>.Get()` 而非直接读 `Value`

**TDD 变更**：§3.4 TypeId 实现代码更新

**降级判定**：🟡→✅ 已消除

---

### SA-005（🟡中 ViewBridge GC）→ ✅ 接受并修正

**回应**：Mono 运行时 Dictionary.Enumerator 确实有装箱 GC。

**方案**：
1. `EntityViewBridge._views` 从 `Dictionary<uint, GameObject>` → 预分配数组 `GameObject[] _viewGOs` + `uint[] _viewEntityIds`
2. 容量与 EntityManager 最大活跃数对齐
3. SyncAll() 改为 `for (int i = 0; i < _activeCount; i++)` 零 GC 遍历
4. OnEntitySpawned/Despawned 操作数组（append / swap-remove）

**TDD 变更**：§3.15 代码更新

**降级判定**：🟡→✅ 已消除

---

### SA-006（🟡中 Spawner AllCleared 与延迟销毁冲突）→ ✅ 接受并修正

**回应**：延迟销毁确实导致 AllCleared 时序问题——`_pendingDespawn` 中的 Entity 在 Phase A Tick 时仍被计为活跃。

**方案**：
1. EntityManager 新增 `int CountAliveByConfig(EntityConfigSO config)` API——遍历 `_activeEntities` 计数，排除 `IsPendingDespawn` 的 Entity
2. EntitySpawner.Tick 在 EntityManager.Tick() **之后**调用（即 Phase B 延迟销毁执行后）
3. 调用时序：`EntityManager.Tick(dt)` → `EntitySpawner.Tick(dt, manager)`

**TDD 变更**：§3.7 新增 API + §3.14 补充时序说明

**降级判定**：🟡→✅ 已消除

---

### SA-007（🟢低 GetComponent O(1) 措辞）→ ✅ 接受并修正

**回应**：文档声称与实现确实不一致。

**方案**：
- BC-01.2 措辞修正为："枚举版 `GetComponent(ComponentType)` O(1) 查询；泛型版 `GetComponent<T>()` O(N)，N≤16"
- §3.2 注释同步更新

**TDD 变更**：BC-01.2 + §3.2 注释

**降级判定**：🟢→✅ 已消除

---

## Round 1 收敛评估

| 问题 | 原始严重度 | 回应后状态 | 是否需要 Round 2 追问 |
|------|-----------|-----------|---------------------|
| SA-001 | 🔴高 | ✅ 已消除 | 否 |
| SA-002 | 🔴高 | ✅ 已消除 | 否 |
| SA-003 | 🟡中 | ⚪ 已知限制 | 否 |
| SA-004 | 🟡中 | ✅ 已消除 | 否 |
| SA-005 | 🟡中 | ✅ 已消除 | 否 |
| SA-006 | 🟡中 | ✅ 已消除 | 否 |
| SA-007 | 🟢低 | ✅ 已消除 | 否 |

**收敛判定**：7/7 问题全部解决（6 个消除 + 1 个已知限制记录），0 个残余 🔴，**无需 Round 2**。

PK R3 在 **1 轮**内收敛。

---

## 结论

**PK R3（软件架构师 vs Unity 架构师）1 轮收敛，7 问题全部解决。**

TDD v2.2 → v2.3 核心变更清单：

| 变更 | 涉及章节 | SA 来源 |
|------|---------|---------|
| PierceHitMask ushort→ulong（64 位适配） | §3.9, §八 | SA-001 |
| 新增 P1.0 阵营枚举统一迁移步骤 | §六 | SA-002 |
| 风险表追加 SO Dictionary Key Domain Reload 限制 | §八 | SA-003 |
| TypeId\<T\> static readonly→static int + 懒初始化 | §3.4 | SA-004 |
| EntityViewBridge Dictionary→预分配数组（零 GC） | §3.15 | SA-005 |
| EntityManager 新增 CountAliveByConfig API + Spawner 时序明确 | §3.7, §3.14, §3.12 | SA-006 |
| BC-01.2 措辞修正（枚举版 O(1) / 泛型版 O(N)） | BC-01.2, §3.2 | SA-007 |

**下一步**：天命人审批后，启动 Phase 1 编码（P1.0 → P1.11）。

