# 弹幕系统 & VFX 系统重构计划总纲

> 制定日期：2026-04-11 | 角色：高级项目经理
> 输入文档：
> - `REVIEW_GAME_DESIGNER.md` — 20 项需求盲区
> - `REVIEW_SOFTWARE_ARCHITECT.md` — 9 项架构缺陷
> - `REVIEW_UNITY_ARCHITECT.md` — 6 项决策点 + 8 项技术风险

---

## 一、重构目标

> 将弹幕系统和 VFX 系统从「Demo 原型」升级为「可支撑真实项目开发的生产级框架」。

**成功标准**：
1. 一个从未用过此模板的开发者，在**允许查阅 Guide 文档、基于模板现有 demo、且不修改代码只新增/修改 SO 资产**的前提下，能在 30 分钟内配置出一个包含 3 种独立贴图子弹 + 3 种独立贴图特效并可跑通指定示例场景的关卡；该目标的验收口径限定为：允许查阅 Guide、允许复制现有 `BulletTypeSO` / `VFXTypeSO` / Registry 等示例资产作为模板并修改其 Inspector 字段、允许使用 Inspector/已有 Editor 工具，但**不允许**修改运行时代码、临时写脚本、手工改 prefab/scene 序列化文本、依赖未文档化的场景内临时对象或隐藏入口、或引入额外构建步骤；验收完成定义为：基于公开文档描述的标准挂接步骤即可在指定 Demo 场景中运行
2. 新增一种弹丸运动类型无需修改 `BulletMover` 等核心热路径，只修改受控扩展点（如 `MotionRegistry` / `MotionType` / 对应策略实现）
3. 弹幕碰撞产生的爆炸特效可以通过 SO 配置一行搞定，且不要求先打 atlas
4. 微信小游戏中 1000 颗子弹 + 10 个特效同屏时，在**固定 Demo 场景、Release/IL2CPP 构建、指定基线机型、持续 30 秒**的测试条件下，平均帧率 ≥ 55fps；性能统计口径按"预热完成后进入稳定运行窗口再计时"执行，不把编辑器刷新、Registry 重建、Batch 预热或首次资源冷启动成本计入该 30 秒平均帧率；同一窗口内还必须同时记录平均 FPS、平均/峰值 DrawCall、活跃 Batch 数、未知桶错误计数、`CollisionEventBuffer overflow count`，并以"未知桶错误计数 = 0、overflow count = 0、DrawCall ≤ 50、活跃 Batch ≤ 24"作为共同通过条件
5. 飘字系统默认使用共享数字图集，且 atlas 工具仅作为可选优化入口存在
6. 所有 Phase 的验收统一按"**边界是否守住、契约是否单一、失败是否可判定且可回退**"执行，而不是按"功能是否先堆出来"执行



---

## 二、前置决策清单（阻塞重构启动）

以下 6 个决策必须在开始编码前确认：

| ID | 决策 | 用户选择 | 状态 |
|----|------|----------|------|
| DEC-001 | 多贴图方案 | **B. 按贴图分桶**（不打 Atlas，每种贴图独立批次） | ✅ 已确认 |
| DEC-002 | 共享渲染层位置 | **B. 框架内目录** `_Framework/Rendering/` | ✅ 已确认 |
| DEC-003 | 渲染调度方式 | **Graphics.DrawMesh + sortingOrder** | ✅ 已确认 |
| DEC-004 | 碰撞事件实现 | **预分配 CollisionEvent[]**（零 GC） | ✅ 已确认 |
| DEC-005 | 视觉动画数据 | **C. BulletCore 存储当前值**（+12B → 48B 对齐） | ✅ 已确认 |
| DEC-006 | 喷雾渲染方案 | **VFX Sprite Sheet**（复用 VFX 渲染器） | ✅ 已确认 |

---

## 三、重构阶段规划

## 2.5 执行前置约束（2026-04-11 已决）

以下约束已由软件架构决策确认，执行阶段不再作为开放问题讨论：

1. **RenderLayer 统一归属**：上收至 `_Framework/Rendering/RenderLayer.cs`，Danmaku/VFX 共用一套枚举
2. **BatchManager 生命周期**：共享实现，不共享实例；Danmaku/VFX 各自持有实例，且实例生命周期跟随各自系统初始化/销毁，不引入全局渲染单例
3. **CollisionEventBuffer 语义**：保留 `ICollisionTarget` 即时回调；Buffer 仅用于旁路消费、联动和观察，帧末统一清空，不承载伤害/击退/死亡等主事实
4. **MotionRegistry 设计**：采用受控注册表，从 `BulletTypeSO.MotionType` 驱动，不做开放式插件系统；成功标准是"不改 BulletMover 等核心热路径，只改受控扩展点"
5. **容量配置策略**：分层收拢，只先处理主链路容量，不做一次性全动态化
6. **DanmakuSystem 拆分边界**：保留 MonoBehaviour Facade 入口，内部拆职责，不拆成多个碎系统；Facade 继续承担生命周期编排与对外 API 单一入口
7. **Bullet 资源策略**：支持独立贴图输入并保留 UV 表达，atlas 仅为可选优化
8. **VFX 资源策略**：支持独立贴图 SpriteSheet，atlas 仅为可选优化，不再强制单图集
9. **DamageNumber 资源策略**：默认继续使用共享数字图集，不纳入本轮资源自由化主链路
10. **Atlas 工具定位**：作为 Editor 可选优化工具设计，不得成为生产前置条件
11. **sortingOrder 策略**：独立于 RenderLayer，使用共享渲染常量定义；架构原则是"sortingOrder 单一真相"，当前建议实现是 `RenderSortingOrder`
12. **VFX 索引重建时机**：仅初始化或 Registry 内容变化时重建，禁止在 Play 热路径重建
13. **Danmaku × VFX 解耦**：通过桥接接口实现，不保留 `DanmakuSystem -> SpriteSheetVFXSystem` 的直接硬引用
14. **旧 SO 迁移**：必须提供 Editor 迁移器，不接受手工迁移；迁移验收必须覆盖 prefab/scene 实例，不只看 SO 资产本体
15. **RenderBatchManager 桶生命周期**：初始化按注册表预热，运行时禁止隐式建桶；未知贴图在开发期报错计数、运行时跳过渲染；预热覆盖范围以各自 TypeRegistry 当前已注册的 `(RenderLayer, SourceTexture, MaterialKey/BlendMode)` 组合为准，责任边界归属各系统自己的 Registry 构建链路，不允许由 `RenderBatchManager` 自行扫描资产兜底
16. **资源描述统一策略**：Bullet/VFX 统一 `SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding` 语义，不统一全部行为字段；统一的是共享值对象语言，不是强行揉成超级基类；其中 `MaterialKey` 与 `BlendMode` 必须保持单向映射一致性，不允许出现同一资源描述在不同系统被解释为不同混合模式的情况
17. **Atlas 输出协议**：Atlas 为可逆派生产物，输出 `AtlasTexture + AtlasMapping`，不作为源数据真相
18. **CollisionEventBuffer 溢出语义**：仅影响旁路表现，不影响伤害/击退/死亡等主逻辑，且必须记录 overflow 统计；验收时若在目标压测场景中持续出现 overflow count > 0，则视为该容量基线不通过，必须调大容量或下调旁路事件产出
19. **VFX FollowTarget 句柄模型**：使用 `AttachSourceId` 抽象句柄，不直接绑定 `Transform`
20. **容量配置化边界**：以显式范围表控制本轮纳入项，未列入项默认不在本轮范围内
21. **OnValidate / 域重载边界**：`OnValidate` 只做校验、补默认值、版本补齐与标脏，不直接重建 Registry/Batch；关闭 Domain Reload 时必须显式重置静态运行时状态
22. **统一资源描述版本化迁移**：`BulletTypeSO` / `VFXTypeSO` 等统一资源描述资产必须带 `SchemaVersion`，并通过正式迁移链路升级，运行时不承担迁移责任；迁移报告必须区分阻断错误与警告，阻断错误禁止进入 apply，警告允许 apply 但必须进入 report 归档
23. **编辑器刷新工作流**：资源变更后统一走"标脏 → Registry 重建 → Batch 预热 → 结果报告"的固定链路，禁止在 `Play()`/渲染热路径顺手刷新
24. **子弹序列帧能力**：Bullet 资源描述必须支持 `Static` 与 `SpriteSheet` 两种采样模式；序列帧子弹属于 Bullet 主链路能力，不外包给 VFX 替代
25. **Attached VFX 语义**：attached VFX 不是"把 Transform 塞进 VFX 实例"，而是"VFX 消费 AttachSource 解析结果"；默认失效语义为冻结到最后有效位置并播完，只有显式配置才允许目标失效即结束；一旦进入"冻结到最后有效位置并播完"收尾态，旧 handle 不允许在目标恢复后自动恢复跟随，必须重新 `PlayAttached`



---


### Phase 0: 基础设施层（无功能变化，纯重构）


**目标**：解耦 VFX 与 Danmaku 的编译依赖，建立共享渲染基础设施。

| 任务 | 描述 | 改动范围 | Agent 可独立完成 |
|------|------|----------|-----------------|
| 任务 | 描述 | 改动范围 | Agent 可独立完成 | 状态 |
|------|------|----------|-----------------|------|
| 0.1 | 新建 `_Framework/Rendering/` + MODULE_README.md | 新建 | ✅ | ✅ 已完成 |
| 0.2 | `DanmakuVertex` → `RenderVertex` 迁移到 Rendering/ | 改名+移动 | ✅ | ✅ 已完成 |
| 0.3 | 更新 BulletRenderer/LaserRenderer/VFXBatchRenderer 的 using | 8 文件 | ✅ | ✅ 已完成 |
| 0.4 | 容量硬编码按主链路优先分层收拢到 WorldConfig / VFXConfig | 5 文件 | ✅ | ✅ 已完成 |
| 0.5 | batchmode 编译验证 + 现有 Demo 回归 | — | ✅ | ✅ 已完成（用户验收通过 2026-04-12） |

**Phase 0 执行记录（2026-04-12）**：
- 0.1~0.4 由 Unity 架构师 agent 完成编码，软件架构师 agent 评审通过，代码评审专家 agent 确认 Linter 零错误
- 0.3 实际涉及 8 个文件（BulletRenderer、LaserRenderer、DamageNumberSystem、TrailPool、VFXRenderer、VFXTypeSO、VFXAssetBootstrapper、BulletTypeSO）+ DanmakuEnums.cs 中删除旧 RenderLayer 枚举
- 0.4 涉及 LaserPool、SprayPool、TrailPool（新增 Capacity 属性+构造函数参数化）、DanmakuWorldConfig（新增 MaxTrails）、DanmakuSystem（传参）
- 已知限制：LaserRenderer 缓冲区容量仍使用 `LaserPool.MAX_LASERS` 静态常量，未与 `pool.Capacity` 联动（Phase 1 渲染重构时一并处理）
- 关键验收项：已有 `WorldConfig.asset` 中 `MaxTrails` 字段缺失，需用户在 Inspector 中手动设为 64

**Phase 0 验收通过（2026-04-12）**：用户确认编译通过、Demo 回归正常、WorldConfig.asset MaxTrails 已手动修复为 64。Phase 0 正式关闭。

**预估**：0.5~1 天 | **依赖决策**：DEC-002 | **用户参与**：打开 Unity 编辑器验证编译 + 修复 WorldConfig.asset

**Phase 0 执行约束补充**：
- `RenderBatchManager` 若在本阶段落骨架，必须按注册表预热桶，禁止预留运行时隐式建桶后门
- 共享渲染层只统一基础设施与契约，不要求 Trail/DamageNumber 在本阶段并入同一渲染主链路


---

### Phase 1: 渲染管线重构（核心改动）

**目标**：支持多贴图分桶渲染、Z 排序。喷雾可视化延后到 Phase 3 用 VFX Sprite Sheet 实现。

> **DEC-001 = B**（按贴图分桶）：不打 Atlas，每个 SO 直接引用自己的 Texture2D。渲染器按 (RenderLayer, Texture) 二元组分桶，每桶一个 DrawCall。
> **DEC-006 = VFX Sprite Sheet**：喷雾不写独立 SprayRenderer，改为复用 VFX 渲染管线播放预制帧动画。

| 任务 | 描述 | 改动范围 | Agent 可独立完成 | 状态 |
|------|------|----------|-----------------|------|
| 1.1 | 实现 `RenderBatchManager`（Layer × Texture 分桶） | 新建 ~300 行 | ✅ | ✅ 已完成 |
| 1.2 | 重构 `BulletRenderer` 使用 BatchManager | 大改 | ✅ | ✅ 已完成 |
| 1.3 | 重构 `VFXBatchRenderer` 使用 BatchManager，支持按贴图分桶 | 大改 | ✅ | ✅ 已完成 |
| 1.4 | `BulletTypeSO` 新增统一资源描述字段，支持 `Static/SpriteSheet` 采样模式并保留 UV 表达 + `[FormerlySerializedAs]` + `BulletTypeMigrationTool` | 中改 | ✅ | ✅ 已完成 |
| 1.5 | `BulletRenderer` 支持子弹序列帧采样（按 Bullet 自身生命周期驱动，不借道 VFX） | 中改 | ✅ | ✅ 已完成 |
| 1.6 | `VFXTypeSO` 新增源贴图字段并保留 UV/Sheet 表达 + 迁移策略 | 中改 | ✅ | ✅ 已完成 |
| 1.7 | 重构 `LaserRenderer` 使用 BatchManager | 中改 | ✅ | ✅ 已完成 |
| 1.8 | 编译验证 + Demo 回归 | — | ✅ | 🔄 编译通过，待用户 Demo 回归 |


**预估**：2~4 天 | **依赖决策**：DEC-001 ✅, DEC-003 ✅ | **用户参与**：打开 Demo 验证渲染正确

**Phase 1 执行记录（2026-04-12）**：
- 1.1 `RenderBatchManager`：306 行，`(RenderLayer, Texture2D, MaterialKey)` 三元组分桶，初始化时按 TypeRegistry 预热，运行时禁止隐式建桶，未知桶报错计数
- 1.2 `BulletRenderer` 迁移：移除旧双 Mesh 直接管理，改为通过 BatchManager 查桶提交四边形；新增 `ResolveUV()` 统一入口支持 Static/SpriteSheet
- 1.3 `VFXBatchRenderer` 迁移：移除旧双 Mesh，改为 BatchManager 分桶；支持按 VFXTypeSO.SourceTexture 独立分桶
- 1.4 `BulletTypeSO` 扩展：新增 `SourceTexture/UVRect/SamplingMode/PlaybackMode/SheetColumns/SheetRows/SheetTotalFrames/SheetFPS/SchemaVersion` + `[FormerlySerializedAs]`；新建 `BulletTypeMigrationTool`（dry-run/apply/report）
- 1.5 `BulletRenderer` 序列帧：`ResolveUV()` 内根据 `SamplingMode` 分流，SpriteSheet 支持 `StretchToLifetime/FixedFpsLoop/FixedFpsOnce` 三种 PlaybackMode
- 1.6 `VFXTypeSO` 扩展：新增 `SourceTexture/UVRect/SchemaVersion` + `[FormerlySerializedAs]`
- 1.7 `LaserRenderer` 迁移：移除旧直接 Mesh 管理，改为 BatchManager 查桶
- 1.8 编译验证通过（0 个 `error CS`），修复了 `core.MaxLifetime → core.Lifetime` 笔误
- **已知问题**：VFXRenderer.cs 与 Unity 内建组件同名（`AddComponent`/`GetComponent` 不可用），非阻断但建议后续改名
- **新增文件**：`_Framework/Rendering/RenderBatchManager.cs`、`_Framework/Editor/Danmaku/BulletTypeMigrationTool.cs`
- **新增枚举**：`BulletSamplingMode`、`BulletPlaybackMode`（在 `DanmakuEnums.cs`）

**Phase 1 执行约束补充**：
- Bullet/VFX 必须共享同一套资源描述语义：`SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding`
- `BulletTypeSO` 与 `VFXTypeSO` 统一资源入口语义，但保留各自行为字段，不做超级基类硬统一
- Bullet 必须原生支持 `Static` 与 `SpriteSheet` 两种采样模式：
  - `Static`：单帧子弹，沿用当前 UVRect 采样；`Static` 模式下不存在 `PlaybackMode`
  - `SpriteSheet`：同一子弹实例在生命周期内按帧序列更新 UV，不新增 VFX 实例、不改碰撞语义；第一版 `PlaybackMode` 仅允许 `StretchToLifetime`、`FixedFpsLoop`、`FixedFpsOnce`
- 第一版非法组合必须显式报错或在迁移/校验阶段阻断：`Static + 任意 PlaybackMode`、以及 `SpriteSheet + PingPong/Reverse/RandomStartFrame`
- 子弹序列帧的时间源默认绑定子弹自身生命周期归一化时间；如后续需要独立 FPS/循环策略，可在 Bullet 播放配置中扩展，但不得反向污染 VFX 播放模型
- Atlas 相关实现只能产出映射资产与可选回写能力，不得把 atlas 结果写成唯一真相

#### Phase 1.5 序列帧子弹项目内落地清单（实现级补充）

- **目标文件**：
  - `UnityProj/Assets/_Framework/DanmakuSystem/Scripts/Config/BulletTypeSO.cs`
  - `UnityProj/Assets/_Framework/DanmakuSystem/Scripts/Core/BulletRenderer.cs`
  - `UnityProj/Assets/_Framework/DanmakuSystem/Scripts/Config/DanmakuTypeRegistry.cs`
  - `UnityProj/Assets/_Framework/Editor/Danmaku/BulletTypeMigrationTool.cs`（新建）
- **字段模型**：`BulletTypeSO` 扩展为 `SchemaVersion + BulletVisualResource + BulletVisualSamplingMode(Static/SpriteSheet) + BulletSpriteSheetConfig`，旧 `AtlasUV` 先保留为迁移兼容字段，不在第一版立即硬删
- **渲染实现**：`BulletRenderer` 新增统一入口 `ResolveBulletUV()`，由其内部根据 `Static/SpriteSheet` 分流；SpriteSheet 通过 `lifetime/maxLifetime` 或 `elapsedSeconds` 解析 `frameIndex` 与 UV；主弹丸与残影统一走该入口
- **第一版明确不做**：不把 `frameIndex` 下沉到 `BulletCore`；不支持 `PingPong/Reverse/RandomStartFrame`；不把飞行阶段序列帧与 `ExplosionMode.MeshFrame` 爆炸帧动画强行统一为同一模型
- **迁移顺序**：先落 `BulletTypeSO` 新字段与 `BulletTypeMigrationTool`（dry-run/apply + 报告），再切 `BulletRenderer` 到新采样入口；禁止跳过迁移器直接手改资产
- **验收口径**：验收样例固定为 4 个：1 个 `Static` 样本、1 个 `SpriteSheet + StretchToLifetime` 样本、1 个 `SpriteSheet + FixedFpsLoop` 样本、1 个 `SpriteSheet + FixedFpsOnce` 样本；同时确认旧静态子弹迁移后视觉不变、无新增脚本编译错误、无 prefab/scene 实例引用损坏

> 注：Atlas 打包 Editor 工具和 SprayRenderer 已从本阶段移除。



---

### Phase 2: 事件与扩展性 ✅ 已完成（2026-04-12 用户验收通过）

**目标**：碰撞回调事件化、运动模式可扩展、系统入口精简化。

| 任务 | 描述 | 改动范围 | Agent 可独立完成 | 状态 |
|------|------|----------|-----------------|------|
| 2.1 | 实现 `CollisionEventBuffer` | 新建 97 行 | ✅ | ✅ 已完成 |
| 2.2 | 改造 `CollisionSolver` 写入事件 Buffer（不替代 `ICollisionTarget` 回调） | 中改 715 行 | ✅ | ✅ 已完成 |
| 2.3 | 实现 `MotionRegistry` 受控运动策略表 | 新建 62 行 | ✅ | ✅ 已完成 |
| 2.4 | 改造 `BulletMover` 使用策略表 | 中改 153 行 | ✅ | ✅ 已完成 |
| 2.5 | 新增运动模式：正弦波、螺旋 | 新建 76+73 行 | ✅ | ✅ 已完成 |
| 2.6 | 拆分 `DanmakuSystem`（保留 Facade 入口，内部拆职责） | 大改 4 文件 545 行 | ✅ | ✅ 已完成 |
| 2.7 | 弹幕×VFX 联动：通过桥接接口消费碰撞事件并触发特效 | 新建 27+59 行 | ✅ | ✅ 已完成 |
| 2.8 | 清屏 API（FreeAll + 转化为特效/得分） | 新建（集成在 API.cs） | ✅ | ✅ 已完成 |
| 2.9 | 编译验证 + Demo 回归 | — | ✅ | ✅ 已完成（batchmode 0 errors 0 warnings） |

**预估**：2~3 天 | **依赖决策**：DEC-004 | **用户参与**：无

**Phase 2 执行记录（2026-04-12）**：
- 多智能体协作：unity-architect 编码 16 文件（10 新建 + 6 修改），software-architect 评审"条件通过"（4 条偏差，0 阻塞项）
- 新增文件（10 个）：DanmakuSystem.Runtime.cs / DanmakuSystem.API.cs / DanmakuSystem.UpdatePipeline.cs / CollisionEventBuffer.cs / MotionRegistry.cs / DefaultMotionStrategy.cs / SineWaveMotionStrategy.cs / SpiralMotionStrategy.cs / IDanmakuEffectsBridge.cs / DefaultDanmakuEffectsBridge.cs
- 修改文件（6 个）：DanmakuSystem.cs / CollisionSolver.cs / BulletMover.cs / DanmakuEnums.cs / DanmakuWorldConfig.cs / BulletTypeSO.cs
- 总计新增 ~2,412 行代码
- DEV-001 已修复（删除旧 `CollisionSolver.Initialize(config)` 单参数重载）
- DEV-002 标记 Phase 3 待办（VFX 序列化字段仍在 Facade）
- DEV-003/004 加入 Backlog（Buffer overflow 累计计数 / CalculateModifierSpeed 重复）
- Unity batchmode 编译通过：0 errors, 0 warnings

**Phase 2 执行约束补充**：
- `CollisionEventBuffer` 明确定义为表现/联动/观察通道，不承载主逻辑事实
- Buffer 溢出只允许影响 VFX/飘字/调试统计等旁路消费，且必须记录 overflow count
- 若实现事件优先级，仅允许轻量分档，不引入复杂业务优先级树
- 2.6 与 2.7 继续视为同步执行项，避免 Danmaku × VFX 解耦过程中出现中间态硬耦合
- `MotionRegistry` 的验收标准统一为：新增运动类型时无需修改 `BulletMover` 等核心热路径，只允许修改 `MotionType`、`MotionRegistry` 注册表和对应策略实现
- 多阵营能力本阶段只要求把 `SourceFaction/TargetFaction` 与过滤扩展点纳入数据模型，不要求同时交付完整阵营矩阵编辑器
- `CollisionEventBuffer` 的默认容量在本阶段正式固化为 256；若压测证明不足，可调大，但必须同步更新 overflow 监控阈值与验收基线
- `DanmakuSystem` 的拆分类级边界统一为：Facade（生命周期与对外 API） / RuntimeContext（持有世界、池、注册表） / UpdatePipeline（逐帧推进） / EffectsBridge（消费旁路事件并桥接 VFX）；不允许拆成多个互相回调的场景级小单例


---

### Phase 3: 视觉增强 ✅ 已完成（2026-04-13 用户验收通过）

**目标**：弹丸视觉动画、Shader 增强、预警线、喷雾可视化。

> **DEC-005 = C**（BulletCore 存储动画值）：BulletCore 新增 Scale/Alpha/Color 字段（+12B → 48B，2 的幂对齐），每帧由 Mover 写入，Renderer 直接读取，零查找。
> **DEC-006 = VFX Sprite Sheet**：喷雾可视化在本阶段实现，通过 VFXTypeSO 配置喷雾帧动画；SprayUpdater 通过附着式 VFX API（如 `PlayAttached / UpdateAttached / StopAttached`）驱动持续跟随表现，而不是一次性 `VFX.Play()`。

| 任务 | 描述 | 改动范围 | Agent 可独立完成 | 状态 |
|------|------|----------|-----------------|------|
| 3.1 | BulletCore 扩展 Scale/Alpha/Color 字段（48B 对齐） | 改 BulletCore + BulletWorld | ✅ | ✅ 已完成 |
| 3.2 | BulletMover 写入动画值（从 SO 曲线采样） | 改 BulletMover + BulletTypeSO | ✅ | ✅ 已完成 |
| 3.3 | BulletRenderer 读取 Core 动画值应用到顶点 | 改 BulletRenderer | ✅ | ✅ 已完成 |
| 3.4 | Shader 增强：溶解效果（dissolve） | 改 2 Shader | ✅ | ✅ 已完成 |
| 3.5 | Shader 增强：发光参数（glow intensity） | 改 2 Shader | ✅ | ✅ 已完成 |
| 3.6 | 激光预警线渲染器 | 新建 146 行 | ✅ | ✅ 已完成 |
| 3.7 | 喷雾可视化：基于 VFX 附着模式（World / FollowTarget）实现 SprayUpdater 联动 | 改 SprayUpdater + 新建 7 文件 | ✅ | ✅ 已完成 |
| 3.8 | VFX 时间缩放联动 | 改 SpriteSheetVFXSystem | ✅ | ✅ 已完成 |
| 3.9 | 编译验证 + Demo 全功能验证 | — | ✅ | ✅ 已完成（用户验收通过 2026-04-15） |

**预估**：3~4 天 | **依赖决策**：DEC-005 ✅, DEC-006 ✅ | **用户参与**：打开 Demo 验证视觉效果

**Phase 3 执行记录（2026-04-12~13）**：
- 多智能体协作：5 阶段流水线——Unity 架构师编码 → 软件架构师评审（有条件通过）→ 代码评审专家（通过 + DEV-005/006 修复）→ 技术文档 → 验收清单
- **新建文件（8 个）**：
  - `LaserWarningRenderer.cs` — 激光预警线渲染器，Phase==1 闪烁细线
  - `VFXAttachMode.cs` — VFX 附着模式枚举（World/FollowTarget/Socket 占位）
  - `IVFXPositionResolver.cs` — VFX 位置解析契约接口
  - `DanmakuAttachSourceResolver.cs` — 弹幕→VFX 位置桥接实现
  - `DanmakuEffectsBridgeConfig.cs` — VFX 序列化字段组件（DEV-002 迁移）
  - Shader 修改 2 个（DanmakuBullet.shader / DanmakuBulletAdditive.shader）
- **修改文件（14 个）**：
  - BulletCore.cs（36B→48B）、BulletWorld.cs（Allocate 初始化动画默认值 DEV-005）
  - BulletTypeSO.cs（AnimationCurve/Gradient 配置）、BulletMover.cs（曲线采样写入）
  - BulletRenderer.cs（读取动画值，DEV-005 移除防御分支）
  - VFXInstance.cs（+AttachSourceId/FLAG_FROZEN）、VFXTypeSO.cs（+AttachMode）
  - SpriteSheetVFXSystem.cs（+PlayAttached/StopAttached/SetPositionResolver/SetTimeScale/附着位置同步）
  - SprayTypeSO.cs（+SprayVFXType）、SprayData.cs（+VfxSlot）、SprayUpdater.cs（VFX 生命周期联动）
  - DanmakuSystem.cs（移除 VFX 字段 DEV-002）、DanmakuSystem.Runtime.cs（+LaserWarning+BridgeConfig+Resolver 注入）
  - DanmakuSystem.UpdatePipeline.cs（+VFX 时间缩放+SprayUpdater 新签名）、DanmakuSystem.API.cs（清屏 VFX 停止）
  - VFXAttachMode.cs（+Socket=2 占位 DEV-006）
- **架构评审结论**：有条件通过（10/11 项通过，4 个建议/偏差项）
  - DEV-005 ✅ 已修复：BulletWorld.Allocate() 初始化动画默认值，Renderer 移除运行时防御分支
  - DEV-006 ✅ 已修复：VFXAttachMode 添加 Socket=2 占位符
  - DEV-007 [轻微] PlayAttached 未实现同源同类型去重（SprayUpdater 用 VfxSlot guard 绕过，Phase 4 前文档化）
  - DEV-008 [中等] Runtime.cs 直接全限定引用 SpriteSheetVFXSystem（DanmakuEffectsBridgeConfig 已缓解，完整桥接化留 Phase 4）
- IDE lint 全部 0 错误

**Phase 3 执行约束补充**：
- `FollowTarget` 默认使用 `AttachSourceId` 解析位置，不直接把 `Transform` 作为 VFX 主句柄
- 目标失效默认冻结到最后有效位置并播完；只有语义明确要求的特效才允许配置为立即结束
- `Socket` 先定义为 `AttachSourceId + SocketName/SocketIndex` 契约，本阶段不强求完整实现
- `AttachSource` 解析职责必须由独立 Resolver 接口承接；VFX 只依赖位置解析契约，不反向依赖 Danmaku 运行时具体类型
- Spray 附着式 VFX 的验收标准统一为：至少验证 `World` 与 `FollowTarget` 两种模式可用，且 `StopAttached`、目标失效冻结、循环播放停止三类收尾语义一致
- Spray 附着式 VFX 还必须覆盖：Resolver 失败、重复 `StopAttached`、无效 handle、目标失效冻结后目标恢复但旧 handle 不自动恢复跟随
- Bullet 视觉动画的性能验收必须包含 `AnimationCurve.Evaluate()` 的 IL2CPP 实测；若出现 GC 或不可接受抖动，允许回退到 LUT，但不得改变外部配置语义


---

### Phase 4: 工作流与工具 ✅ 已完成（2026-04-15）

**目标**：策划配置效率、编辑器体验、文档完善。

| 任务 | 描述 | 改动范围 | Agent 可独立完成 | 状态 |
|------|------|----------|-----------------|------|
| 4.1 | Bullet/VFX Atlas 打包工具（可选优化，不是前置） | 新建 Editor 窗口 + 资产格式 | ✅ | ✅ 已完成 |
| 4.2 | 弹丸/VFX 子图选择器与映射回写工具 | 新建 Editor 窗口 | ✅ | ✅ 已完成 |
| 4.3 | SO 配置热重载（OnValidate + 运行时刷新） | 改多个 SO | ✅ | ✅ 已完成 |
| 4.4 | 碰撞可视化 Gizmos | 改 DanmakuSystem Editor | ✅ | ✅ 已完成 |
| 4.5 | ProfilerMarker 性能标记 + Batch/DC 监控 | 改多个核心文件 | ✅ | ✅ 已完成 |
| 4.6 | 更新 MODULE_README / Guide 文档 | 多文件 | ✅ | ✅ 已完成 |
| 4.7 | CHANGELOG 更新 | 1 文件 | ✅ | ✅ 已完成 |


**预估**：1~2 天 | **依赖决策**：无 | **用户参与**：评审文档

**Phase 4 执行记录（2026-04-15）**：
- 多智能体流水线：开发者 → 架构师评审 → 代码评审专家 → 文档工程师
- **4.3 SO 配置热重载**：
  - 新建 `DanmakuEditorRefreshCoordinator`（9.2 KB），固化 `标脏 → Registry 重建 → Batch 预热 → 结果报告` 四阶段链路
  - `BulletTypeSO/LaserTypeSO/SprayTypeSO/DanmakuTypeRegistry/VFXTypeRegistry` 的 OnValidate 统一接入 `MarkDirty()`
  - 新建 `DanmakuSystemEditor` Inspector 面板，提供 "Run Controlled Refresh" 按钮 + 刷新报告面板
  - `DanmakuSystem.EditorWarmupBatches()` 提供受控预热入口
- **4.4 碰撞可视化 Gizmos**：
  - 新建 `DanmakuCollisionGizmosDrawer`，使用 `[DrawGizmo(GizmoType.Selected)]`
  - 仅在 DanmakuSystem 被选中时绘制：弹丸半径线框球 + 激光段线
- **4.5 Batch/DC 监控**：
  - 新建 `RenderBatchManagerRuntimeStats`（Rendering/ 共享层），提供 Last/Peak/Average DrawCall 与 ActiveBatch 统计
  - `DanmakuDebugHUD` 扩展：显示 DrawCalls（当前/avg/peak）、Active Batches（当前/avg/peak）、Unknown Buckets、Collision Overflow
- **DEV-008 VFX 桥接化**：
  - 新建 `IDanmakuVFXRuntime` 接口（SetTimeScale/StopAttached/PlayAttached）
  - 新建 `DanmakuVFXRuntimeBridge` 实现，委托 SpriteSheetVFXSystem
  - `UpdatePipeline/API/CollisionSolver/SprayUpdater` 全部改走 `_vfxRuntime` 桥接，不再直接持有 `SpriteSheetVFXSystem`
- **架构评审**：有条件通过后修复 5 个阻塞项
  1. UpdatePipeline 残留 `_sprayVfxSystem` → 统一改走 `_vfxRuntime` ✅
  2. API.ClearAll() 未走桥接 → 改走 `_vfxRuntime.StopAttached` ✅
  3. Warmup 旁路按钮 → 删除，仅保留 Controlled Refresh ✅
  4. Gizmos 范围过宽 → 缩为 `GizmoType.Selected` ✅
  5. DebugHUD 缺平均/峰值 → 补齐 avg/peak DrawCall 与 ActiveBatch ✅
- **代码评审**：P0/P1 修复
  - 5 个 SO/Registry 文件顶层 `using MiniGameTemplate.Danmaku.Editor` 泄漏 → 移除，OnValidate 内改命名空间限定调用
  - 7 处 Unity Object `?.` 误用（DanmakuVFXRuntimeBridge 3处 + CollisionSolver 2处 + UpdatePipeline 2处）→ 显式 null 检查
  - IDE lint 全部 0 错误
- **新建文件（5 个）**：
  - `DanmakuEditorRefreshCoordinator.cs`
  - `DanmakuSystemEditor.cs`
  - `DanmakuCollisionGizmosDrawer.cs`
  - `IDanmakuVFXRuntime.cs`
  - `DanmakuVFXRuntimeBridge.cs`
- **新增共享文件（1 个）**：`RenderBatchManagerRuntimeStats.cs`（Rendering/）
- **修改文件（12 个）**：DanmakuSystem.cs、DanmakuSystem.Runtime.cs、DanmakuSystem.UpdatePipeline.cs、DanmakuSystem.API.cs、CollisionSolver.cs、SprayUpdater.cs、BulletTypeSO.cs、LaserTypeSO.cs、SprayTypeSO.cs、DanmakuTypeRegistry.cs、VFXTypeRegistry.cs、DanmakuDebugHUD.cs
- **4.1/4.2 Atlas 打包工具与子图选择器**：已完成（Phase 4 子任务，2026-04-15）
  - 新建 `AtlasMappingSO.cs`（`_Framework/Rendering/`）：Atlas 映射数据模型，双键查找（引用+GUID），ADR-019 可逆派生产物
  - 新建 `DanmakuAtlasPackerWindow.cs`（`_Framework/Editor/Rendering/`）：Atlas 打包 Editor 窗口，域分离（Bullet/VFX），拖拽/文件夹导入，利用率预览
  - 新建 `AtlasSubSpritePopup.cs`（`_Framework/Editor/Rendering/`）：子图选择器弹出窗口，Atlas 条目模式 + Grid 模式
  - 新建 `AtlasMappingSOEditor.cs`（`_Framework/Editor/Rendering/`）：回写工具自定义 Inspector，dry-run → 确认 → apply → report 四阶段
  - 修改 `BulletTypeSO.cs`：新增 `AtlasBinding` 字段 + `GetResolvedTexture()` / `GetResolvedBaseUV()` 方法
  - 修改 `VFXTypeSO.cs`：新增 `AtlasBinding` 字段 + `GetResolvedTexture()` / `GetResolvedBaseUV()` 方法
  - 修改 `BulletRenderer.cs`：桶预热和渲染改用 `GetResolvedTexture()` / `GetResolvedBaseUV()`
  - 修改 `VFXRenderer.cs`：桶预热和渲染改用 `GetResolvedTexture()` / `GetResolvedBaseUV()`
  - 修改 `BulletTypeSOEditor.cs`：新增 "🔍 选择子图" / "🔍 选择爆炸子图" 按钮
  - 架构评审：修复 ARCH-001（ReferenceEquals→Unity ==）、ARCH-002（冗余 #if）、ARCH-003（VFXRenderer 死代码）
  - Unity batchmode 编译通过：0 errors, 0 warnings

**Phase 4 执行约束补充**：
- Atlas 工具必须输出显式映射资产，并保留回退到原始 `SourceTexture + UVRect` 的能力
- Bullet/VFX/DamageNumber atlas 分域维护，不混打
- 迁移器必须具备预检、缺失引用报告、迁移统计和人工处理清单，不能只提供直接改资产模式
- 迁移器工作流统一为 `dry-run -> apply -> report`；其中 `dry-run` 必须输出待迁移资产数、风险项、缺失引用和 prefab/scene 实例扫描结果，`apply` 只处理已通过预检的数据集，`report` 作为验收归档产物
- `OnValidate` / 热重载 / Registry 刷新 / Batch 预热必须统一走固定编辑器工作流，不允许任何工具各写各的捷径；刷新失败时必须保留旧运行时状态并显式报错，不允许"部分成功但静默继续"
- `RenderSortingOrder` 作为 sortingOrder 唯一代码来源；文档、调试 HUD、验收截图和实现代码都必须引用同一套命名，不再允许各系统私自定义排序常量；新增层位只能通过更新 `RenderSortingOrder` 进入系统，禁止在业务类中内联新的 sortingOrder 数值


---

## 四、总览甘特图

```
Week 1                          Week 2
Mon Tue Wed Thu Fri    Mon Tue Wed Thu Fri
─────────────────────────────────────────
P0  P0  P1  P1  P1    P1  P2  P2  P3  P3
                P1              P2  P3  P3
                                        P4
                                        P4
─────────────────────────────────────────
 ▲                     ▲               ▲
 决策确认              中期检查点       最终验收
```

**总预估**：8~14 天（Agent 工时），用户参与约 2-3 小时（主要是决策 + 验证运行效果）

---

## 五、需求覆盖追踪表

| 需求 ID | 需求 | 覆盖阶段 | 状态 |
|---------|------|----------|------|
| GD-001 | 多贴图共存 | Phase 1 | ✅ BulletTypeSO.SourceTexture + RBM 按贴图分桶 |
| GD-002 | 弹丸视觉动画（含缩放/透明/颜色与序列帧子弹） | Phase 1 + Phase 3 | ✅ BulletCore 48B + AnimationCurve/Gradient 采样 + Renderer 读取 |

| GD-003 | VFX×弹幕联动 | Phase 2 | ✅ IDanmakuEffectsBridge + DefaultDanmakuEffectsBridge |
| GD-004 | 喷雾可视化 | Phase 3 | ✅ SprayUpdater + PlayAttached/StopAttached + VFXAttachMode |
| GD-005 | 运动多样性 | Phase 2 | ✅ MotionRegistry + SineWave + Spiral |
| GD-006 | 多阵营系统 | Phase 2（扩展碰撞过滤）| ✅ CollisionEventBuffer 含 SourceFaction/TargetFaction |
| GD-007 | 编排工具 | Phase 4（基础版）| ✅ DanmakuEditorRefreshCoordinator + DanmakuSystemEditor（Controlled Refresh + 报告面板）|
| GD-008 | 清屏炸弹 | Phase 2 | ✅ ClearAllBulletsWithEffect API |
| GD-009 | 弹丸×弹丸碰撞 | Phase 2（碰撞事件 Buffer 预留扩展点）| ✅ Buffer 已落地，扩展点已预留 |
| GD-010 | 道具/收集物 | Phase 2（非伤害弹丸通过 Faction 区分）| ✅ Faction 过滤已在 CollisionSolver 中实现 |
| GD-011 | 预警线 | Phase 3 | ✅ LaserWarningRenderer（Charging 闪烁细线） |
| GD-012 | Z 轴渲染层级 | Phase 1 | ✅ RenderSortingOrder + RenderBatchManager |
| GD-013 | VFX 时间缩放 | Phase 3 | ✅ SpriteSheetVFXSystem.SetTimeScale() |
| GD-014 | 相机震动 | 不在本轮范围（可独立模块）| ➖ |
| GD-015 | 音效分层 | 不在本轮范围（AudioManager 侧）| ➖ |
| GD-016 | 粒子拖尾 | Phase 3（可选）| ✅ TrailPool（Mesh Trail）+ Ghost 残影（BulletRenderer 内置）— 不使用 ParticleSystem |
| GD-017 | 统计与调试 | Phase 4 | ✅ RenderBatchManagerRuntimeStats + DanmakuDebugHUD 扩展（DrawCall/Batch avg/peak + Gizmos） |
| GD-018 | 配置热重载 | Phase 4 | ✅ DanmakuEditorRefreshCoordinator（标脏→Registry重建→Batch预热→报告） |
| GD-019 | 容量可配置 | Phase 0 | ✅ 主链路已完成 |
| GD-020 | VFX 类型丰富度 | Phase 3（附着特效）| ✅ PlayAttached/StopAttached + IVFXPositionResolver + ADR-021 冻结语义 |

**覆盖率**：本轮重构覆盖 18/20 需求（GD-014 相机震动、GD-015 音效分层为独立模块，不在弹幕/VFX 重构范围内）。

---

## 5.5 容量配置化范围表（新增边界控制）

| 模块 | 当前容量来源 | 本轮是否纳入 | 纳入原因 | 计划 Phase | 是否阻塞其他模块 | 备注 |
|---|---|---|---|---|---|---|
| BulletWorld | Config + 默认值 | 是 | 主链路核心容量 | Phase 0 | 是 | 已有配置入口，需收口到统一约束 |
| LaserPool | const | 是 | 主链路核心容量 | Phase 0 | 是 | 与渲染/碰撞主链路耦合 |
| SprayPool | const | 是 | 主链路核心容量 | Phase 0 | 是 | 影响碰撞静态数组与主流程 |
| VFXPool | ctor/config | 是 | 主链路核心容量 | Phase 0 | 否 | 与特效播放上限直接相关 |
| CollisionEventBuffer | 新增容量项 | 是 | 主链路事件旁路容量 | Phase 2 | 否 | 需和 overflow 统计一起落地 |
| TrailPool | const | 条件性纳入 | 仅当受主链路容量牵引 | Phase 0/1 | 否 | 保持独立渲染，不强行并主链路 |
| DamageNumberSystem | const | 否 | 非主链路，独立资源策略 | Phase 3/4 后 | 否 | 仅接共享排序/监控 |
| TargetRegistry | const | 否 | 次级容量，后置处理 | 后续 Phase | 否 | 不阻塞本轮主链路 |
| ObstaclePool | const | 否 | 次级容量，后置处理 | 后续 Phase | 否 | 不阻塞本轮主链路 |
| AttachSourceRegistry | const | 否 | 跟附着模型联动，后置收口 | Phase 2/3 后 | 否 | 先保证接口契约 |
| PatternScheduler | const | 否 | 低频辅助模块 | 不在本轮 | 否 | 显式排除，防止顺手扩 scope |
| SpawnerDriver | const | 否 | 低频辅助模块 | 不在本轮 | 否 | 显式排除，防止顺手扩 scope |

## 六、风险缓解计划


| 风险 | 缓解措施 | 负责方 |
|------|----------|--------|
| RISK-001 DC 预算（DEC-001=B 按贴图分桶，DC 线性增长）| DebugHUD DC 计数器 + 预警阈值 + 尽量合并同贴图弹丸 | Agent |
| RISK-002 BulletCore 48B 对齐（DEC-005=C）| 2048 × 48B = 96KB，仍在 L2 缓存友好范围内 | Agent |
| RISK-003 SO 序列化迁移 | `[FormerlySerializedAs]` + 迁移脚本 | Agent |
| RISK-004 Shader WebGL 兼容 | WebGL 编译验证检查 | Agent |
| RISK-005 BatchManager 桶数预测 | 初始化时从 TypeRegistry 预创建桶，不运行时动态创建 | Agent |
| RISK-006 喷雾 VFX 精度（DEC-006=VFX Sheet）| 帧动画无法完美匹配逻辑扇形→文档说明视觉仅为近似表现 | Agent |
| RISK-007 碰撞 Buffer 溢出 | 固定容量 + 优先级丢弃 | Agent |
| RISK-008 AnimationCurve.Evaluate GC（DEC-005=C）| 微信 IL2CPP 下实测无 GC；若有则回退 LUT | Agent |

---

## 七、下一步行动

**重构计划执行状态（2026-04-15 最终更新）**：

1. ✅ Phase 0 基础设施层 — 2026-04-12 用户验收通过
2. ✅ Phase 1 渲染管线重构 — 2026-04-12 编译通过，1.8 待用户 Demo 回归（已在 Phase 3 验收中覆盖）
3. ✅ Phase 2 事件与扩展性 — 2026-04-12 用户验收通过
4. ✅ Phase 3 视觉增强 — 2026-04-15 用户验收通过（含 E-05/E-06 修复 + git commit `3d78267`）
5. ✅ Phase 4 工作流与工具 — 2026-04-15 全部完成（含 4.1/4.2 Atlas 工具子任务）

**遗留 Backlog**：
- **DEV-003** ✅ 已完成（2026-04-20）：提取 `MotionUtility.CalculateModifierSpeed()`，消除 Default/SineWave/Spiral 三处重复实现
- **DEV-004** ✅ 已完成（2026-04-20）：新增 `CollisionEventBufferTests`，覆盖 overflow 计数、0 容量、Reset 清零、Span 内容、Reset 后复用
- **DEV-007** ✅ 已完成（2026-04-20）：`SpriteSheetVFXSystem.PlayAttached()` 增加“同源 + 同类型”去重；去重键采用 `VFXTypeSO` 引用身份，避免 `RuntimeIndex` 重建导致映射失效

> 本轮弹幕 & VFX 系统重构（Phase 0~4）已全部完成，包括 4.1/4.2 Atlas 工具；原遗留 Backlog（DEV-003/004/007）已于 2026-04-20 清零。后续仅剩 RuntimeAtlas 真机验收与按需补充项。




---

## 附录：文档索引

| 文档 | 路径 | 内容 |
|------|------|------|
| 游戏设计师评审 | `Docs/Agent/REVIEW_GAME_DESIGNER.md` | 20 项需求盲区 |
| 软件架构师评审 | `Docs/Agent/REVIEW_SOFTWARE_ARCHITECT.md` | 9 项架构缺陷 + 重构路线 |
| Unity架构师评审 | `Docs/Agent/REVIEW_UNITY_ARCHITECT.md` | 6 项决策点 + 8 项技术风险 |
| 本文档 | `Docs/Agent/REFACTOR_PLAN.md` | 重构计划总纲 |
