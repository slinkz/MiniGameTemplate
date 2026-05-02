# PK 评审记录 — ARCHITECT_DECISION_RECORD.md（ADR-030）

> **目标文档**：c:\workspace\mini-game-template\MiniGameTemplate\Docs\Agent\ARCHITECT_DECISION_RECORD.md
> **文档类型**：ADR
> **攻方角色**：Unity 架构师
> **守方角色**：软件架构师
> **开始时间**：2026-04-20 17:02
> **PK 状态**：✅ 收敛完成（3 轮，6 问题，0 阻塞）

---

## PK Round 1 — 攻方提问

## [UA-001] | 严重度 🔴高 | ADR-030 将 `RuntimeIndex` 视为"纯运行时细节"，但当前代码把它当作跨系统稳定主键使用，迁移范围被明显低估
**涉及章节**：§ADR-030、§ADR-015、§ADR-025
**质疑**：ADR-030 认为把 `DanmakuTypeRegistry` / `VFXTypeRegistrySO` 内化为 runtime-only，并在首次 Spawn 时懒分配 index，是"开发者零感知"的小改动；但当前代码现实里，`RuntimeIndex` 已不是单纯内部缓存，而是多个热路径和系统边界的主键。`AssignRuntimeIndices()` / `RebuildRuntimeIndices()` 会直接写回各类 TypeSO，`BulletRenderer.Rebuild()` 通过 `registry.BulletTypes[core.TypeIndex]` 反查类型，`DanmakuEditorRefreshCoordinator` 和 `DanmakuSystem.EditorWarmupBatches()` 也都显式依赖这条链路。
**潜在风险**：如果只按 ADR-030 表面改法推进，很容易出现双轨 index 并存、不同入口分配顺序不同、旧实例 `TypeIndex` 语义被破坏，最终表现为"能生成，但渲染/命中/特效错类型"的隐性错配。这是阻塞编码的问题。
**建议方向**：在编码前先明确 `RuntimeIndex` 的唯一真相来源：是继续写回 TypeSO，还是完全收敛到 runtime registry；同时列出所有 `RuntimeIndex` 读写点与 `TypeIndex` 消费点，形成完整迁移清单，再重估工期。
**状态**：🟡 待回应

## [UA-002] | 严重度 🔴高 | "internal registry + 懒注册"与当前 Inspector/EditorWarmup/Prefab-Scene 刷新链路直接冲突，ADR-030 把"零感知"说得过头了
**涉及章节**：§ADR-030、§ADR-025
**质疑**：当前工程里，TypeRegistry 不只是运行时查找表，同时还是编辑器刷新工作的输入源、预热桶的枚举源、Prefab/Scene 配置链的一部分。`DanmakuSystem` 当前序列化持有 `_typeRegistry`，`DanmakuEditorRefreshCoordinator` 会扫描 registry 资产并执行重建，`EditorWarmupBatches()` 依赖 `_typeRegistry` 完成预热。ADR-030 说"删掉 Registry.asset 即可"，但没有给出新的显式发现入口。
**潜在风险**：如果没有新的发现机制，Editor 工具无法知道全项目有哪些 TypeSO、哪些 prefab/scene 需要预热，资源校验会从"有入口可枚举"退化为"运行时碰到再说"。这会直接冲撞 ADR-025 原先的受控刷新理念，是阻塞编码的问题。
**建议方向**：ADR-030 需要承认"仍需显式配置入口"，只是入口不一定是 public registry SO。可选方案：保留 editor-only 的 registry/index asset 作为发现清单，或新增明确的扫描约束（目录、label、bootstrap config）。把"开发者零感知"收敛为"运行时零手工注册"，不要扩大到编辑器工具链层面。
**状态**：🟡 待回应

## [UA-003] | 严重度 🟡中 | RBM 懒建桶的实现约束被 ADR-030 说轻了，现有 `RenderBatchManager` 并不支持安全动态扩展
**涉及章节**：§ADR-030、§ADR-017、§ADR-028、§ADR-029
**质疑**：ADR-030 把 RBM 改成 `GetOrCreateBucket`，并认为只要传入 `templateMaterial + sortingOrder`，append 后重排一次即可。但现有 `RenderBatchManager` 的 `_buckets` 是固定长度数组，`Initialize()` 内部负责创建 Mesh/Material/Vertices/IndexBuffer，统计口径只有 `UnknownBucketErrorCount`，`Dispose()` 也只按 `_bucketCount` 销毁；另外 ADR-029 文档要求 BucketKey 降维为纯 Texture，而现代码仍是 `(RenderLayer, Texture)`。
**潜在风险**：动态建桶若不补齐扩容、初始化、排序、回滚、统计、释放等契约，后面很容易出现错桶、漏绘、资源泄漏或监控失真。它不一定阻塞 ADR-030 方向，但会明显低估实现复杂度。
**建议方向**：在开工前补一份 RBM 最小设计：BucketKey 最终形态、数组扩容策略、桶初始化步骤、排序重建时机、失败回滚、Dispose 约束、动态建桶统计指标。并把"懒建桶"与"桶容量扩容"拆开，避免 scope 漂移。
**状态**：🟡 待回应

## [UA-004] | 严重度 🟡中 | ADR-030 对 Laser / Spray / VFX 生命周期安全性论证不足，当前文档主要只证明了 Bullet 路径
**涉及章节**：§ADR-030、§ADR-015、§ADR-028
**质疑**：ADR-030 用 Bullet 例子论证"首次 Spawn 时 GetOrRegister + GetOrCreateBucket 即可"，但当前系统还包括 Laser / Spray / VFX，且生命周期并不等价。Laser/Spray 往往是持续实例，VFX 还受 attached/follow handle 生命周期约束。文档没有说明这些类型在懒注册下如何保证实例创建、持续更新、系统重置、Domain Reload off 等场景下的索引稳定性。
**潜在风险**：如果只在 Bullet 路径验证成功就推广到 Laser/Spray/VFX，后续可能出现 warning 与正式 laser 用不同索引、attached VFX 在 registry reset 后解析错 type 等偶发且难查的问题。
**建议方向**：要么把 ADR-030 第一阶段明确限定为 Bullet 路径，Laser/Spray/VFX 后续单独验收；要么补一节"多类型生命周期约束"，逐项说明 Bullet、Laser、Spray、VFX 在创建、持续更新、销毁、系统重置时的索引稳定性保证。
**状态**：🟡 待回应

---

## PK Round 1 — 守方回应

> **守方角色**：软件架构师（10 年+ 大型系统设计，专精模块化/接口设计/迁移策略）

---

### 对 [UA-001] 的回应 — RuntimeIndex 迁移范围

**承认问题有效，但严重度从 🔴 降为 🟡。**

攻方的核心担忧是"双轨 index 并存导致隐性错配"。经过完整代码搜索，RuntimeIndex 的实际使用模式如下：

**写入点（真相源）**：
- `DanmakuTypeRegistry.AssignRuntimeIndices()` → 写入 `BulletTypeSO.RuntimeIndex`、`LaserTypeSO.RuntimeIndex`、`SprayTypeSO.RuntimeIndex`
- `VFXTypeRegistrySO.RebuildRuntimeIndices()` → 写入 `VFXTypeSO.RuntimeIndex`

**消费链路（均为单次读取后存储到实例数据中）**：
- `BulletSpawner.cs:64` → `core.TypeIndex = type.RuntimeIndex`（写入一次，之后只读 `core.TypeIndex`）
- `DanmakuSystem.API.cs:251` → `laser.LaserTypeIndex = typeIndex`（参数直接传 RuntimeIndex）
- `SpriteSheetVFXSystem.cs:119/199` → `TypeIndex = type.RuntimeIndex`（同上）

**反查链路（从 TypeIndex 反查回 TypeSO）**：
- `registry.BulletTypes[core.TypeIndex]` — 出现在 BulletMover / BulletRenderer / CollisionSolver / DanmakuSystem.API（共 12 处）
- `registry.LaserTypes[laser.LaserTypeIndex]` — LaserUpdater / LaserRenderer / LaserWarningRenderer（4 处）
- `registry.SprayTypes[spray.SprayTypeIndex]` — SprayUpdater / CollisionSolver（3 处）
- `registry.TryGet(instance.TypeIndex, ...)` — VFXBatchRenderer / SpriteSheetVFXSystem（3 处）

**关键观察**：所有反查都是 `registry.XxxTypes[index]` 形式——这恰好是 ADR-030 中 `GetType(index)` 的等价物。只要新 registry 的 `List<T>` 在同一次运行中保持 append-only 且不重排，所有已存储的 TypeIndex 都天然稳定。

**为什么不是 🔴（不阻塞编码）**：
1. 不存在"双轨 index"风险——TypeSO 上的 `RuntimeIndex` 字段会被**删除**，改由 registry 内部 Dictionary 管理。写入实例数据的 TypeIndex 直接来自 `GetOrRegister()` 返回值。
2. 反查从 `registry.BulletTypes[index]` 改为 `registry.GetBulletType(index)` — 纯机械替换。
3. 所有消费者都传递 `DanmakuTypeRegistry` 引用（不是 SO asset），改为传递新的 internal class 引用即可。

**ADR-030 修正**：补充"迁移清单"章节，明确列出 4 类 TypeSO 字段删除 + 12+4+3+3=22 处反查机械替换 + 3 处写入点重定向。工期上调 1h（从 6.5h → 7.5h）。
**状态**：✅ 已回应（严重度修正为 🟡）

---

### 对 [UA-002] 的回应 — Editor 发现入口

**承认问题有效，接受建议方向。"零感知"措辞收敛。**

攻方说得对：ADR-030 把"开发者零感知"说得太绝对了。编辑器工具链仍需一个"发现全部 TypeSO"的入口。

**修正方案**：

ADR-030 的"零感知"严格限定为**运行时 API 层面**（Spawn/Fire/Play 不需要手动注册）。编辑器工具链保留显式发现机制，但**不再是 public SO asset**：

```
Before: DanmakuTypeRegistry.asset（策划手动维护的 SO 数组）
After:  AssetDatabase.FindAssets("t:BulletTypeSO") + Label 约束
```

具体做法：
1. `DanmakuEditorRefreshCoordinator.RebuildRegistries()` 改为直接扫描 `t:BulletTypeSO` / `t:LaserTypeSO` / `t:SprayTypeSO` / `t:VFXTypeSO`，不再依赖中间 Registry SO
2. 资源校验工具同理——直接扫描 TypeSO 资产，检查贴图引用完整性
3. Editor Warmup 通过扫描结果构建临时 registry 来预热桶（效果等价但不依赖持久化 asset）

**这不影响"运行时零手工注册"的核心价值**——策划不再需要手动拖 TypeSO 到任何地方。编辑器扫描是自动的。

**ADR-030 文档修正**：
- §4 "编辑器预热降级为可选优化"中补充"发现机制从 Registry SO 迁移到 AssetDatabase 扫描"
- "开发者零感知"改为"运行时零手工注册"

**状态**：✅ 已回应（接受修正，措辞收敛）

---

### 对 [UA-003] 的回应 — RBM 懒建桶实现约束

**承认有效，接受"补最小设计"建议，但不阻塞 ADR-030 方向决策。**

攻方说的"固定长度数组、Dispose 约束、BucketKey 形态、统计口径"确实是实现层面需要解决的问题。但这些是**编码阶段的详细设计**，不是**架构决策层面的阻塞项**。

ADR-030 的定位是"方向决策"——确定"允许运行时建桶"这个架构约束放松。具体的数组扩容策略属于编码 PR 的设计文档。

**接受的补充**（作为 ADR-030 实施约束附录）：

| 约束 | 设计方向 |
|------|----------|
| BucketKey 最终形态 | ADR-029 已决定：纯 Texture（RenderLayer 已废弃）。代码迁移在 ADR-030 编码中一并执行 |
| 数组扩容 | `List<RenderBucket>` 替代固定数组，初始容量 = 编辑器预热桶数（典型 8~16）|
| 桶初始化步骤 | 与现有 `Initialize()` 内部逻辑相同——new Mesh + Material.Instantiate + 预分配顶点/索引缓冲 |
| 排序重建 | 仅在动态建桶时触发一次 `Sort()`，基于 `sortingOrder` |
| 失败回滚 | 动态建桶失败（例如超过 MaxBuckets 硬上限）→ 返回 false + 计入 `OverflowBucketErrorCount` |
| Dispose | `Dispose()` 遍历 `_buckets.Count`（非固定长度），逐桶释放 Mesh/Material |
| 动态建桶统计 | `DynamicBucketCreatedCount`（累计）+ `DynamicBucketCreatedThisFrame`（帧内峰值） |

**工期**：不额外增加——RBM 改造本身已在实施范围的 1h 中包含。

**状态**：✅ 已回应（接受补充约束，不升级严重度）

---

### 对 [UA-004] 的回应 — Laser / Spray / VFX 生命周期

**承认论证不足，补充多类型生命周期安全性分析。**

攻方的担忧核心是"懒注册下 index 在持续实例中是否稳定"。逐类型分析：

| 类型 | 创建时写入 TypeIndex | 持续更新时读取方式 | 销毁/重置 | 懒注册安全性 |
|------|---------------------|-------------------|-----------|-------------|
| **Bullet** | `core.TypeIndex = registry.GetOrRegister(type)` | `registry.GetBulletType(core.TypeIndex)` 反查 | 标记 Phase=Dead，slot 回收 | ✅ index 在 registry 的 append-only List 中永远有效 |
| **Laser** | `laser.LaserTypeIndex = registry.GetOrRegister(laserType)` | `registry.GetLaserType(laser.LaserTypeIndex)` 反查 | Phase=0 后 slot 回收 | ✅ 同上 |
| **Spray** | `spray.SprayTypeIndex = registry.GetOrRegister(sprayType)` | `registry.GetSprayType(spray.SprayTypeIndex)` 反查 | Phase=0 后 slot 回收 | ✅ 同上 |
| **VFX** | `instance.TypeIndex = registry.GetOrRegister(vfxType)` | `registry.TryGet(instance.TypeIndex, out type)` | Free slot | ✅ 同上 |

**关键保证**：内部 `List<T>` 是 **append-only**，永远不删除/不重排。index 一旦分配就终身有效（在单次运行内）。

**Domain Reload Off 场景**：
- Unity 2021+ "Enter Play Mode Settings: Domain Reload Off" 会导致 static 字段跨 PlayMode 持续存在
- ADR-030 的 registry 由 `DanmakuSystem`（MonoBehaviour）持有，不是 static。`DanmakuSystem.RuntimeInitialize()` 每次 Awake 都会创建新 registry → 旧实例全部回收 → 无残留 index

**系统重置（ClearAllBullets / StopAllLasers）**：
- 只清数据实例，不动 registry → 已注册的类型仍然有效

**Spray 特殊性（持续 VFX 附着）**：
- `SprayUpdater` 中 `typeRegistry.SprayTypes[spray.SprayTypeIndex]` 在 Spray 存活期间每帧读取
- 只要 registry 的 List 是 append-only 且 Spray 存活期间 registry 不被 dispose，就是安全的
- `DanmakuSystem` 的生命周期保证了这一点

**结论**：所有 4 种类型的生命周期都与 Bullet 同构——"创建时写入 index，存活期间用 index 反查"。懒注册对所有类型同样安全，不需要分阶段实施。

**ADR-030 文档修正**：补充"§6. 多类型生命周期安全性保证"章节。

**状态**：✅ 已回应

---

## PK Round 1 总结

| 问题 | 原严重度 | 回应后严重度 | 处置 |
|------|---------|-------------|------|
| UA-001 | 🔴高 | 🟡中 | 补迁移清单，工期+1h |
| UA-002 | 🔴高 | 🟡中 | 措辞收敛+"运行时零手工注册"；编辑器改用 AssetDB 扫描 |
| UA-003 | 🟡中 | 🟡中 | 补 RBM 实施约束附录 |
| UA-004 | 🟡中 | 🟢低 | 补多类型生命周期分析，确认所有类型同构安全 |

**Round 1 结论**：0 个 🔴 阻塞项。ADR-030 方向可行，需要补充 3 处文档修正后可进入编码。

---

## PK Round 2 — 攻方追问

> **攻方角色**：Unity 架构师

Round 1 守方回应质量较高，4 个问题均有实质性回应。以下是基于回应内容的追问，聚焦于守方回应中新引入的假设和遗漏点。

## [UA-005] | 严重度 🟡中 | Laser/Spray 的 API 签名当前接收 `byte typeIndex` 而非 `TypeSO`，ADR-030 的"运行时零手工注册"对这两个类型的 public API 改动被遗漏

**涉及章节**：§ADR-030 §3 开发者新工作流、UA-004 回应
**质疑**：守方在 UA-004 回应中论证了所有类型"创建时写入 index"同构安全，但忽略了一个关键现实：**当前 `DanmakuSystem.FireLaser()` 和 `FireSpray()` 的 public API 签名接收 `byte typeIndex`，不接收 `LaserTypeSO` 引用**。

```csharp
// 现有 API（DanmakuSystem.API.cs:115-154）
public int FireLaser(byte typeIndex, Vector2 origin, float angle, ...)
public int FireSpray(byte typeIndex, Vector2 origin, float direction, ...)
```

这意味着调用方（关卡脚本/Spawner）**必须自己查 typeIndex**——这恰恰要求开发者知道 Registry 的存在，与"运行时零手工注册"直接矛盾。Bullet 路径没有这个问题，因为 `BulletSpawner` 接收 `BulletTypeSO` 引用后内部读取 `RuntimeIndex`。

**潜在风险**：如果 ADR-030 只改了 Bullet 路径的 API 风格（接收 TypeSO），Laser/Spray 仍然暴露 raw index，那"零手工注册"就只覆盖了弹丸，激光和喷雾的开发体验没有改善。

**建议方向**：ADR-030 实施范围应包含 `FireLaser/FireSpray` API 升级——从 `byte typeIndex` 改为接收 `LaserTypeSO/SprayTypeSO` 引用，内部由框架 `GetOrRegister()` 转换。这是 API 表面积变更，需要明确列入实施清单。

**状态**：🟡 待回应

## [UA-006] | 严重度 🟢低 | AssetDatabase 扫描作为编辑器发现机制的性能与可靠性

**涉及章节**：UA-002 回应
**质疑**：守方建议用 `AssetDatabase.FindAssets("t:BulletTypeSO")` 替代 Registry SO 作为编辑器发现入口。这在小项目可行，但存在两个小问题：
1. `FindAssets` 在大型项目中可能产生不必要的全库扫描开销（当前项目规模不大，但作为"通用模板框架"应考虑扩展性）
2. 如果 TypeSO 散落在多个目录（`Assets/_Framework/`、`Assets/Game/`、`Assets/Demo/`），扫描会捞出不应参与当前场景预热的 TypeSO

**潜在风险**：低。当前项目规模下不是问题，但模板框架未来接入大项目时可能需要目录/Label 过滤。

**建议方向**：在 ADR-030 中记录一句"编辑器扫描可通过 AssetLabel 或目录前缀过滤范围"作为未来扩展点即可。不阻塞当前实施。

**状态**：🟢 待回应

---

## PK Round 2 — 守方回应

> **守方角色**：软件架构师

---

### 对 [UA-005] 的回应 — Laser/Spray API 签名升级

**完全接受。这是一个有价值的发现。**

攻方观察非常精准——`FireLaser(byte typeIndex, ...)` 和 `FireSpray(byte typeIndex, ...)` 的 API 设计确实与 ADR-030 的"运行时零手工注册"目标矛盾。当前 Bullet 通过 `BulletSpawner` 接收 `BulletTypeSO` 引用后内部转换 index，但 Laser/Spray 却直接暴露 raw index 给调用方。

**ADR-030 实施范围修正**：

```csharp
// Before
public int FireLaser(byte typeIndex, Vector2 origin, float angle, ...)
public int FireSpray(byte typeIndex, Vector2 origin, float direction, ...)

// After  
public int FireLaser(LaserTypeSO type, Vector2 origin, float angle, ...)
public int FireSpray(SprayTypeSO type, Vector2 origin, float direction, ...)
// 内部：byte typeIndex = _registry.GetOrRegister(type);
```

这属于 breaking API change，需要更新所有调用点。但当前项目中 FireLaser/FireSpray 的调用方数量有限（主要是 PatternScheduler 和 Demo 脚本），迁移量可控。

**新增实施行项**：

| 文件 | 操作 | 工作量 |
|------|------|--------|
| `DanmakuSystem.API.cs` | `FireLaser`/`FireSpray` API 改为接收 TypeSO | 0.5h |
| `PatternScheduler.cs` + 调用方 | 更新调用签名 | 0.5h |

**总工期修正**：从 7.5h → 8.5h（约 1 天）

**状态**：✅ 已回应（接受，纳入实施范围）

---

### 对 [UA-006] 的回应 — AssetDatabase 扫描可扩展性

**接受建议，记录为扩展点。**

攻方说得合理——当前项目规模下 `FindAssets` 无性能问题，但模板框架应预留过滤能力。

**ADR-030 文档补充**：

在"§4. 编辑器预热"中追加一句：

> 编辑器扫描默认使用 `AssetDatabase.FindAssets("t:BulletTypeSO")` 全库搜索。若项目规模增长导致扫描过慢或捞出不相关资产，可通过 `AssetLabel`（如 `l:DanmakuActive`）或目录前缀（`Assets/Game/`）收窄范围。当前阶段不实现过滤，作为未来扩展点保留。

**状态**：✅ 已回应

---

## PK Round 2 总结

| 问题 | 严重度 | 处置 |
|------|--------|------|
| UA-005 | 🟡中 | 接受。FireLaser/FireSpray API 升级纳入实施范围，+1h |
| UA-006 | 🟢低 | 接受。记录为扩展点，不影响当前实施 |

**Round 2 结论**：0 个 🔴 阻塞项，1 个 🟡 已纳入实施。攻方提出的所有实质问题均已回应。

---

## PK Round 3 — 攻方收敛判定

> **攻方角色**：Unity 架构师

经过 2 轮 PK，6 个问题（UA-001 ~ UA-006）已全部得到实质回应。以下是最终收敛评估：

### 收敛状态检查

| 收敛标准 | 达标？ | 说明 |
|---------|-------|------|
| 所有 🔴 问题已降级或解决 | ✅ | UA-001 → 🟡（补迁移清单），UA-002 → 🟡（措辞收敛） |
| 无新增 🔴 问题 | ✅ | Round 2 只有 🟡 和 🟢 |
| 守方回应基于代码事实而非空洞承诺 | ✅ | 完整搜索了 RuntimeIndex 全部 22 个反查点 |
| 文档修正方向明确、可验证 | ✅ | 3 处修正 + 1 处 API 升级，均有具体文件和代码示例 |
| 工期估算合理调整 | ✅ | 从 6.5h → 8.5h，增量 2h 有具体对应 |

### 最终问题状态

| 问题 | 原严重度 | 最终严重度 | 最终处置 |
|------|---------|-----------|---------|
| UA-001 | 🔴高 | 🟡中 → ✅ 已解决 | 补迁移清单（22 处机械替换 + 4 处字段删除） |
| UA-002 | 🔴高 | 🟡中 → ✅ 已解决 | "零感知"收敛为"运行时零手工注册"；编辑器改用 AssetDB 扫描 |
| UA-003 | 🟡中 | 🟡中 → ✅ 已解决 | 补 RBM 实施约束附录（7 项约束） |
| UA-004 | 🟡中 | 🟢低 → ✅ 已解决 | 补多类型生命周期分析，4 种类型同构安全 |
| UA-005 | 🟡中 | 🟡中 → ✅ 已解决 | FireLaser/FireSpray API 升级纳入实施范围 |
| UA-006 | 🟢低 | 🟢低 → ✅ 已解决 | 记录为扩展点 |

### 攻方最终意见

**ADR-030 方向正确，可以进入编码阶段。** 

Round 1 暴露的 2 个 🔴 问题本质是"文档论证不够充分"而非"方向错误"——守方通过完整的代码分析和迁移清单有效回应了。Round 2 的 UA-005 是一个有价值的 API 一致性发现，已纳入实施范围。

**建议 ADR-030 在编码前完成以下文档修正**（守方已承诺）：
1. 补充"迁移清单"章节（22 处反查 + 4 处字段删除 + API 升级）
2. "开发者零感知"改为"运行时零手工注册"
3. 补充"多类型生命周期安全性保证"
4. 补充 RBM 实施约束附录
5. FireLaser/FireSpray API 升级纳入实施范围表
6. 工期修正为 ~8.5h

---

## PK 评审结论

> **PK 状态**：✅ 收敛完成
> **总轮次**：3 轮（Round 1 攻守 + Round 2 攻守 + Round 3 收敛判定）
> **总问题数**：6 个（2 🔴 → 0 🔴，2 🟡，2 🟢）
> **最终结论**：ADR-030 方向可行，补充文档修正后可进入编码
> **完成时间**：2026-04-20

