# 软件架构师决策记录：弹幕系统 & VFX 系统重构

> 决策日期：2026-04-11
> 角色：软件架构师
> 目的：将落地审计中的未决项收敛为可执行决策，避免实现阶段临时分叉和返工。

---

## 一、执行摘要

本次重构方案**可以落地**，但前提不是"带着未决项直接开工"，而是先把跨模块边界、生命周期归属、事件消费语义定死。

本记录对 Unity 架构审计中提出的 GAP / NEW / MISS 逐项给出正式决策。采纳本记录后：

- Phase 0 可以启动
- Phase 1 的渲染重构边界清晰
- Phase 2 不会因为事件语义和系统拆分方式反复返工
- Phase 3 的 VFX 附着模式有明确前置接口

---

## 二、正式决策总览

| ID | 主题 | 决策 |
|---|---|---|
| ADR-001 | RenderLayer 归属 | 统一上收至 `_Framework/Rendering/RenderLayer.cs` |
| ADR-002 | BatchManager 生命周期 | 共享实现，不共享实例；Danmaku/VFX/其他系统各自持有实例 |
| ADR-003 | CollisionEventBuffer 消费模型 | 单生产者 + 单主消费者 + 可选只读观察者；帧末统一清空 |
| ADR-004 | MotionRegistry 设计 | 受控注册表，不做开放式插件系统 |
| ADR-005 | 容量配置收拢策略 | 分层收拢，只先改主链路容量 |
| ADR-006 | DanmakuSystem 拆分边界 | 保留 Facade 入口，拆职责，不拆成碎系统 |
| ADR-007 | Bullet 资源描述 | 支持独立贴图 + 保留 UV 表达，图集仅为可选优化。**运行时被 ADR-028 Supersede** |
| ADR-008 | VFX 资源策略 | 支持独立贴图 SpriteSheet，图集仅为可选优化。**运行时被 ADR-028 Supersede** |
| ADR-009 | DamageNumber 资源策略 | 默认共用数字图集，独立于 Bullet/VFX 资源自由策略 |
| ADR-010 | Atlas 打包工具定位 | Atlas 是编辑器可选优化工具，不是生产前置条件。**运行时被 ADR-028 Supersede**（工具链保留） |
| ADR-011 | 旧 SO 迁移 | 必须提供自动迁移器，不接受手工迁移 |
| ADR-012 | 多阵营碰撞模型 | 数据结构升级为通用关系模型，本轮只落最小闭环 |
| ADR-013 | VFX 附着模式 | 定义 World / FollowTarget / Socket 三类，本轮至少实现前两类接口能力 |
| ADR-014 | 渲染排序配置 | sortingOrder 独立于 RenderLayer，放共享渲染配置常量 |
| ADR-015 | VFX Registry 重建时机 | 仅初始化/变更时重建，禁止在 Play 热路径重建 |
| ADR-016 | Danmaku 到 VFX 的依赖 | 改为桥接接口，不直接硬引用具体 VFX 组件 |
| ADR-017 | RenderBatchManager 桶生命周期 | ~~初始化按注册表预热，运行时禁止隐式建桶~~ **Superseded by ADR-030** |
| ADR-018 | Bullet/VFX 资源描述统一策略 | 统一资源描述值对象语义，不统一全部行为模型 |
| ADR-019 | Atlas 输出协议 | Atlas 为可逆派生产物，不作为源数据真相 |
| ADR-020 | CollisionEventBuffer 溢出语义 | 仅影响旁路表现，必须可观测，不影响主逻辑 |
| ADR-021 | VFX FollowTarget 句柄模型 | 使用 AttachSourceId 抽象句柄，不直接绑定 Transform |
| ADR-022 | 容量配置化边界表 | 用显式范围表控制本轮收拢边界，防止 scope 漂移 |
| ADR-023 | OnValidate 与热重载边界 | OnValidate 只做校验/标脏，不直接重建运行时对象 |
| ADR-024 | 统一资源描述版本化迁移 | 统一资源描述必须带 SchemaVersion，并通过正式迁移链路升级 |
| ADR-025 | 编辑器刷新工作流 | 资源变更后必须按 Registry 重建 → Batch 预热的固定链路刷新 |
| ADR-028 | RuntimeAtlasSystem（v2.1 已接受） | 统一渲染管线核心：替代割裂的 6 路渲染架构，按需 Blit，Shelf Packing，Channel 隔离，统一 RBM 提交。Supersedes ADR-007/008/010 运行时约束。全部 12 个未决项已确认 |
| ADR-029 | 彻底移除 Additive Blend | v2：彻底移除 Additive 代码/Shader/配置，统一 Normal。BucketKey 降维为纯 Texture。YAGNI 原则 |
| ADR-030 | TypeRegistry 内化 + 懒注册 + 懒建桶 | TypeRegistry 从 public SO 降级为框架内部运行时类；首次 Spawn 时懒注册类型 + 懒建桶；运行时零手工注册。Supersedes ADR-017、修正 ADR-015/ADR-025。PK 3 轮 6 问题已收敛 |



---


## ADR-001: RenderLayer 归属统一到 Rendering 共享层

### 状态
已接受

### 上下文
当前存在两套语义相同的枚举：
- `MiniGameTemplate.Danmaku.RenderLayer`
- `MiniGameTemplate.VFX.VFXRenderLayer`

两者值一致，但定义分裂。重构后共享渲染层需要统一批次键和排序语义。

### 决策
在 `_Framework/Rendering/` 新建统一 `RenderLayer`，Danmaku 和 VFX 全部改用该定义。旧枚举迁移后删除，不保留双轨映射。

### 后果
**收益**：
- 批次键统一
- 文档和调试语义统一
- 后续新增渲染层不会双份维护

**代价**：
- 需要修改 SO 字段类型和引用代码
- 需要做一次序列化迁移验证

---

## ADR-002: BatchManager 共享实现，不共享实例

### 状态
已接受

### 上下文
Danmaku 与 VFX 渲染都需要共享批处理实现，但两者资源、生命周期、初始化时机、排序需求并不相同。

### 决策
`RenderBatchManager` 作为共享实现类存在，但实例归属各系统自身：
- Danmaku 持有自己的实例
- VFX 持有自己的实例
- 其他系统如 DamageNumber 未来如接入，也各自持有实例

### 后果
**收益**：
- 生命周期边界清晰
- 不引入跨系统初始化依赖
- 调试和测试更容易隔离

**代价**：
- 无法跨系统自动合批
- 每个系统各自维护一份批次容器

**放弃的东西**：
- 放弃"全局统一渲染批管理器"的理论最优 DrawCall 方案

---

## ADR-003: CollisionEventBuffer 采用单主消费者模型

### 状态
已接受

### 上下文
`CollisionEventBuffer` 已有良好的零 GC 数据结构设计，但未定义谁消费、何时消费、是否允许多消费者。

### 决策
采用以下规则：
1. 只有 `CollisionSolver` 写入 Buffer
2. 只有 `DanmakuSystem` 在固定帧阶段进行主消费
3. 只允许调试/分析模块做只读观察，不允许业务模块各自抢消费
4. Buffer 在帧末统一 Reset
5. 保留现有 `ICollisionTarget` 回调，不用 Buffer 替代即时命中响应

### 后果
**收益**：
- 事件语义稳定
- 零 GC 容易守住
- 不引入事件总线顺序问题

**代价**：
- 灵活性低于广播总线
- 新扩展点要接入主分发流程

---

## ADR-004: MotionRegistry 做受控注册表

### 状态
已接受

### 上下文
当前 `BulletMover` 依赖 flag/if-else 链，扩展新运动模式会继续膨胀。与此同时，`BulletCore.Flags` 8 位已经全部用完。

### 决策
采用"受控注册表 + 编译期入口"方案：
- `BulletTypeSO` 持有 `MotionType`
- `MotionRegistry` 在初始化时构建有限策略表
- `BulletMover` 通过 `TypeIndex -> BulletTypeSO -> MotionType` 获取策略
- 不做运行时开放注册、反射注册、脚本化任意策略注入

### 后果
**收益**：
- 摆脱 flag 膨胀
- 性能和调试成本可控
- 不增加 `BulletCore` 体积

**代价**：
- 新增 Motion 仍需改代码
- 灵活性不如插件式设计

---

## ADR-005: 容量配置采用分层收拢

### 状态
已接受

### 上下文
当前容量常量散落在多个类中，且部分依赖 `const` 和静态数组初始化。一次性全动态化会显著扩大改造范围。

### 决策
按优先级分层收拢：

### 第一层：本轮必须收拢
- Bullets
- Lasers
- Sprays
- VFX
- CollisionEventBuffer
- 与主链路强耦合的 Trails（若受主容量影响）

### 第二层：建议收拢
- DamageNumbers
- Targets
- Obstacles
- AttachSources

### 第三层：暂不收拢
- PatternScheduler
- SpawnerDriver
- 其他低频辅助模块

### 后果
**收益**：
- 控制 Phase 0 范围
- 先解决主链路问题
- 降低大规模签名改动风险

**代价**：
- 短期存在双轨容量来源
- 文档必须明确哪些已配置化，哪些未配置化

---

## ADR-006: DanmakuSystem 保留 Facade，内部拆职责

### 状态
已接受

### 上下文
`DanmakuSystem.cs` 当前承担初始化、Update 驱动、API 暴露、碰撞后处理、VFX 触发等多重职责，已超过单类合理边界。

### 决策
保留 `DanmakuSystem` 作为 MonoBehaviour Facade 入口，内部拆分职责模块；不拆成多个互相依赖的独立场景系统。

推荐职责拆分：
- `DanmakuRuntime`
- `DanmakuUpdatePipeline`
- `CollisionPipeline`
- `DanmakuEffectsBridge`
- `DanmakuAPI`（可用 partial class 承载对外 API）

### 后果
**收益**：
- 外部入口稳定
- 内部复杂度下降
- 渐进式演进成本低

**代价**：
- 仍保留一个中心编排入口
- 不是"完全去中心化"方案

---

## ADR-007: Bullet 资源策略支持独立贴图，保留 UV 表达

### 状态
已接受 — **运行时约束被 ADR-028 v2.0 Supersede**（Editor 环境仍生效）

### 上下文
用户明确要求框架不要把图集打包作为生产前置条件，也不要因为贴图数量上限限制设计。Bullet 是高频资源，但模板工程首先要承载内容生产自由。

### 决策
Bullet 采用"资源自由优先"策略：
- `BulletTypeSO` 支持直接引用独立贴图资源
- 保留 `UVRect` 表达，允许同一贴图内复用局部区域
- 图集仅作为可选优化结果，不是唯一合法输入
- 渲染按 `(RenderLayer, Texture)` 分桶，相同贴图自动合批，不同贴图接受 DrawCall 增长

### 后果
**收益**：
- 新增子弹无需先打图集
- 不限制贴图数量和设计组合方式
- 同时保留后续做 atlas 优化的空间

**代价**：
- DrawCall 随贴图种类线性增长
- 需要更清晰的调试与批次数监控

---

## ADR-008: VFX 资源策略同样支持独立贴图，图集仅为可选优化

### 状态
已接受 — **运行时约束被 ADR-028 v2.0 Supersede**（Editor 环境仍生效）

### 上下文
旧决策将 VFX 保持为单图集，工程上更省事，但与"资源组织自由优先"的产品原则冲突。VFX 同样不应被强制绑定到 atlas 工作流。

### 决策
VFX 改为与 Bullet 一致的资源原则：
- `VFXTypeSO` 支持独立 `Texture2D` / SpriteSheetTexture 来源
- 保留 `UVRect + Sheet` 表达，既支持整图帧动画，也支持图集子区域帧动画
- 渲染按 `(RenderLayer, Texture)` 分桶
- atlas 只作为编辑器可选优化产物，不是运行前置条件

### 后果
**收益**：
- 特效制作不再依赖统一图集打包
- 美术可按效果独立迭代资源
- Bullet/VFX 资源模型统一，认知成本下降

**代价**：
- VFX DrawCall 可能高于旧方案
- `VFXTypeSO`、迁移器、渲染器都要同步调整

---

## ADR-009: DamageNumber 默认共用数字图集，不追求与 Bullet/VFX 完全同策

### 状态
已接受

### 上下文
飘字的贴图增长速度远慢于子弹和特效，本质上是有限字符集/数字集。为它引入完全自由的独立贴图策略，收益很低，复杂度不值。

### 决策
DamageNumber 采用独立资源策略：
- 默认继续使用共享数字图集
- 不纳入 Bullet/VFX 的"资源自由优先"主策略
- 可以复用共享渲染基础设施和排序约定，但资源组织保持 atlas 友好
- 若未来出现特殊飘字贴图需求，再单独扩展，不提前泛化

### 后果
**收益**：
- 保持飘字系统简单稳定
- 数字/字符资源天然适合图集，批次稳定
- 不把低收益问题拖进主重构链路

**代价**：
- 三类系统的资源策略不完全一致
- 文档必须明确这种"不一致是刻意设计，不是遗漏"

---

## ADR-010: Atlas 打包工具是编辑器可选优化工具，不是生产前置条件

### 状态
已接受 — **运行时约束被 ADR-028 v2.0 Supersede**（Editor Atlas 工具链本身保留不删）

### 上下文
既然 Bullet/VFX 不再强制 atlas，就必须明确 atlas 工具的定位，否则后面很容易又被工具链反向绑架成"必须先打包才能用"。

### 决策
设计一个 Editor Atlas 工具，但明确其定位为"可选优化"：
- 输入：一组 Texture2D / SpriteSheet 源资源 + 打包规则
- 输出：AtlasTexture + 映射清单 + 可选批量回写 SO 的 UV/Texture 引用
- 支持 Bullet/VFX 两类资源
- 不要求项目运行前必须执行
- 未打包资源可直接运行，打包只是减少批次的优化手段

### 后果
**收益**：
- 同时满足资源自由和后期优化需求
- 工具链不会反客为主变成内容生产门槛
- 为后续项目提供可选性能抓手

**代价**：
- 需要额外设计 atlas 描述格式和回写流程
- 必须处理"打包前/打包后"两种资源状态

---

## ADR-011: 旧 SO 迁移必须自动化

### 状态
已接受

### 上下文
本轮会改动枚举归属、资源字段、依赖关系和部分序列化结构。手工迁移风险高且不可审计。

### 决策
必须提供 Editor 迁移器，至少覆盖：
- BulletTypeSO
- VFXTypeSO
- LaserTypeSO
- SprayTypeSO

迁移器职责：
- 补默认值
- 迁移枚举
- 校验缺失引用
- 输出迁移报告

### 后果
**收益**：
- 降低资产损坏风险
- 可重复执行、可审计

**代价**：
- 增加一段编辑器工具工作量

---

## ADR-012: 阵营模型升级为通用关系模型，但本轮只做最小闭环

### 状态
已接受

### 上下文
当前碰撞逻辑偏二元阵营（玩家/非玩家），不利于未来扩展中立、召唤物、多敌对阵营等规则。

### 决策
本轮在数据结构上升级为通用阵营表达：
- `FactionId`
- `SourceFaction/TargetFaction`
- 预留关系判断扩展点

但本轮只实现现有玩家/敌人逻辑，不展开完整阵营矩阵编辑器。

### 后果
**收益**：
- 未来不需要推翻碰撞模型
- 本轮范围可控

**代价**：
- 当前实现会出现"结构先行、规则后补"的轻度超前设计

---

## ADR-013: VFX 附着模式显式建模

### 状态
已接受

### 上下文
当前 VFX API 仅适合一次性世界坐标播放，不足以支撑喷雾、持续附着、挂点特效。

### 决策
定义统一的 `VFXSpawnRequest` / 附着模式模型：
- `World`
- `FollowTarget`
- `Socket`

本轮至少保证：
- `World` 落地
- `FollowTarget` 接口和数据结构落地
- `Socket` 先定义，不要求完整实现

### 后果
**收益**：
- API 不会因新增附着需求反复重载
- Phase 3 的喷雾 VFX 有明确前置模型

**代价**：
- 需要扩展 VFXInstance 或外部附着跟踪结构

---

## ADR-014: sortingOrder 独立配置

### 状态
已接受

### 上下文
`RenderLayer` 表达语义层，`sortingOrder` 表达最终绘制顺序，两者职责不同。

### 决策
架构原则是：**sortingOrder 必须存在单一代码真相来源，且独立于业务语义枚举 `RenderLayer`**。

当前实现建议：新增共享渲染排序常量定义，例如 `RenderSortingOrder`，不把排序数值硬编码进业务枚举。


### 后果
**收益**：
- 语义层与表现层分离
- 后续调序不会污染业务定义

**代价**：
- 需要维护一份共享排序表

---

## ADR-015: VFX Registry 仅在初始化/变更时重建

### 状态
已接受

### 上下文
当前 `SpriteSheetVFXSystem.Play()` 每次调用都执行 `RebuildRuntimeIndices()`，属于把冷路径逻辑放进热路径。

### 决策
改为：
- 初始化时重建一次
- Registry 内容变更时置 dirty
- `Play()` 只读取运行时索引

### 后果
**收益**：
- 热路径开销下降
- 调用语义更合理

**代价**：
- 需要引入 dirty 管理或初始化约束

---

## ADR-016: Danmaku 到 VFX 通过桥接接口解耦

### 状态
已接受

### 上下文
当前 `DanmakuSystem` 通过 `[SerializeField]` 直接依赖 `SpriteSheetVFXSystem` 和 `VFXTypeSO`，边界耦合过深。

### 决策
定义桥接接口，例如：
- `IDanmakuEffectsBridge`

由默认实现内部调用具体 VFX 系统。Danmaku 只表达"命中发生了，需要播放某类效果"，不依赖具体 VFX 组件实现。

### 后果
**收益**：
- 模块边界更清晰
- DanmakuSystem 拆分时不会继续焊死 VFX

**代价**：
- 增加一层桥接抽象
- 需要明确默认实现装配方式


---

## ADR-017: RenderBatchManager 桶在初始化期预热，运行时禁止隐式建桶

### 状态
~~已接受~~ → **Superseded by ADR-030**（2026-04-20）

### 上下文
当前方案已经确认 Bullet/VFX 都支持独立贴图，运行时按 `(RenderLayer, Texture)` 分桶。如果允许运行时遇到未知贴图时临时创建新桶，系统会从"注册期可验证架构"退化为"热路径动态建模"，直接损害性能稳定性、启动期校验能力和问题定位能力。

### 决策
- `RenderBatchManager` 只管理已注册的桶
- 桶在初始化阶段按注册表预热
- 预热覆盖范围以各系统自己的 TypeRegistry 当前已注册的 `(RenderLayer, SourceTexture, MaterialKey/BlendMode)` 组合为准
- Registry 构建负责提供完整预热输入，`RenderBatchManager` 不承担扫描资产或兜底补注册职责
- 运行时禁止因未知贴图隐式创建新桶
- 遇到未知 `(RenderLayer, Texture)`：
  - Editor/Dev：错误日志 + 计数
  - Release：跳过渲染，不自动补建

### 后果
**收益**：
- 渲染热路径稳定
- 启动期可做资源完整性校验
- 资源问题暴露更早

**代价**：
- 失去运行时动态注入贴图的灵活性
- 注册表维护要求更严格

---

## ADR-018: Bullet/VFX 统一资源描述值对象语义，不统一全部行为模型

### 状态
已接受

### 上下文
Bullet 与 VFX 都需要表达源贴图、UV 区域、材质/混合方式等渲染输入，但两者的播放语义不同：Bullet 偏运动体，VFX 偏帧动画与附着。如果完全分裂，会导致 atlas 工具、Inspector、调试语言和迁移器各做一套；如果强行统一成一个超级类型，又会把不同上下文揉成泥球。

### 决策
采用"统一资源入口语义 + 各域保留行为模型"的分层方案：
- 共享资源描述概念：`SourceTexture`、`UVRect`、`MaterialKey/BlendMode`、可选 `AtlasBinding`
- `MaterialKey` 与 `BlendMode` 必须保持单向映射一致性：同一资源描述在 Bullet/VFX 两侧不得被解释为不同混合模式或不同基础材质族
- Laser 必须至少统一到共享渲染契约：`RenderLayer`、`sortingOrder` 单一来源、`MaterialKey/BlendMode` 一致性；若 Laser 不使用 `SourceTexture + UVRect` 语义，必须显式视为"共享渲染基础设施消费者，但不属于统一资源描述值对象覆盖范围"，实现与验收都不得绕过共享材质键和排序约束
- Bullet 保留自身运动/命中相关字段
- VFX 保留自身 Sheet/Playback/Attach 相关字段
- DamageNumber 不强行纳入同一资源策略，只复用共享渲染基础设施

### 后果
**收益**：
- 工具链、调试语义、迁移逻辑可复用
- 保持限界上下文清晰，不做伪统一

**代价**：
- 需要设计共享值对象语言
- Inspector 和迁移器要做一层公共抽象

---

## ADR-019: Atlas 是可逆派生产物，不是源数据真相

### 状态
已接受

### 上下文
既然 Bullet/VFX 已明确"不强制 atlas"，就必须防止 atlas 工具反向绑架生产流程。如果 atlas 结果直接覆盖源事实，系统会在"可选优化"和"单向转换"之间失控，导致回退困难、资产真相混乱、diff 污染严重。

### 决策
- 源事实仍是原始 `SourceTexture + UVRect`
- Atlas 作为编辑器优化产物独立存在
- 工具输出至少包括：`AtlasTexture` + `AtlasMappingSO`（或等价映射资产）
- 运行时可通过映射覆盖采样信息，但不要求强制回写源 SO
- 批量回写只能作为可选能力，不能成为唯一工作流
- Bullet/VFX/DamageNumber atlas 分域维护，不混打

### 后果
**收益**：
- atlas 真正成为可选优化层
- 资产可回退、可审计、可并行维护

**代价**：
- 运行时或编辑器侧需要支持"双态解析"
- 工具设计比"直接改 SO"更复杂

---

## ADR-020: CollisionEventBuffer 是可丢的表现事件通道，溢出不影响主逻辑

### 状态
已接受

### 上下文
系统已确认 `ICollisionTarget` 继续承担即时命中逻辑，`CollisionEventBuffer` 只负责旁路消费、联动和观察。若不进一步定义溢出语义，后续实现很容易把 buffer 误用成第二套业务事实通道，导致"丢事件是否等于丢逻辑"争议持续存在。

### 决策
- `CollisionEventBuffer` 明确定义为表现/联动/观察通道，不承载主业务事实
- Buffer 溢出不影响：伤害、击退、死亡、状态变更
- Buffer 溢出只影响：VFX、飘字、调试统计、非关键联动
- 必须记录 overflow count，接入 profiler/debug HUD
- 若目标压测基线中持续出现 overflow count > 0，则该容量配置验收不通过
- 若做优先级，仅允许轻量分档，不引入复杂业务优先级树

### 后果
**收益**：
- 主逻辑与表现逻辑边界稳定
- 溢出行为可解释、可观测

**代价**：
- 极端负载下会丢失部分表现反馈
- 需要额外调试指标支撑验收

---

## ADR-021: VFX FollowTarget 使用抽象句柄，不直接绑定 Transform 生命周期

### 状态
已接受

### 上下文
VFX 需要支持 `World / FollowTarget / Socket` 三类附着模式。如果直接让 VFXInstance 持有 `Transform`，VFX 上下文会被 Unity 场景对象生命周期绑死，增加耦合、测试成本和生命周期 bug 风险。

### 决策
- `World`：直接存世界坐标
- `FollowTarget`：持有 `AttachSourceId`
- `Socket`：持有 `AttachSourceId + SocketName/SocketIndex`
- VFX 系统通过位置解析接口获取世界坐标，不直接依赖场景对象引用
- attached VFX 的运行时语义定义为"每帧消费解析结果并刷新自身姿态"，而不是"持有 Unity 对象引用等待其驱动"
- `PlayAttached / UpdateAttached / StopAttached` 属于同一组生命周期 API：`PlayAttached` 只负责创建实例与绑定句柄，`UpdateAttached` 只负责刷新派生姿态参数，`StopAttached` 才负责结束策略
- 默认失效语义：目标解析失败时冻结到最后有效位置并播完，不立即销毁
- 一旦进入"冻结到最后有效位置并播完"收尾态，旧 handle 不允许在目标恢复后自动恢复跟随；如需恢复，必须重新 `PlayAttached`
- 只有语义明确要求的特效，才允许配置为目标失效即结束
- Resolver 失败、重复 Stop、无效 handle 都必须是幂等且可观测的，不允许靠异常或静默副作用维持流程

### 后果
**收益**：
- VFX 与场景对象模型解耦
- 生命周期更稳定，后续可扩展到骨骼点、逻辑实体、临时挂点
- attached VFX 的开始、跟随、停止三段语义被正式拆开，后续实现和验收不再各说各话

**代价**：
- 需要维护 AttachSourceRegistry/位置解析接口
- API 设计略复杂于直接传 Transform
- 需要补一套 handle 有效性与失败可观测约束


---

## ADR-022: 容量配置化范围必须显式表格化

### 状态
已接受

### 上下文
当前方案已确认"主链路优先、次级容量后置、不一次性全动态化"。如果不把本轮纳入范围显式写成表格，执行阶段会持续出现"这个要不要顺手一起改"的 scope 漂移，导致工期和风险失控。

### 决策
在计划文档中补充容量配置化范围表，至少包含：
- 模块
- 当前容量来源
- 本轮是否纳入
- 纳入原因
- 计划 Phase
- 是否阻塞其他模块
- 备注

该表作为本轮范围控制依据，未列入项默认不在本轮范围内。

### 后果
**收益**：
- 本轮边界清晰
- 降低执行期 scope 漂移和沟通成本
- 为后续 Phase 演进保留正式入口

**代价**：
- 文档维护成本略增
- 需要在每次范围调整时同步更新表格

---

## ADR-023: OnValidate 与域重载/热重载边界

### 状态
已接受

### 上下文
当前计划已经确认 Phase 4 会引入 SO 配置热重载能力，但如果不把 `OnValidate`、域重载（Domain Reload 开/关）、Play Mode 热路径刷新边界写死，后续实现很容易出现三类问题：
1. 在 `OnValidate` 里直接重建 Registry / Batch，导致编辑器抖动、重复构建、甚至在 Play 中污染热路径
2. 在关闭 Domain Reload 的项目设置下，静态缓存、dirty 标记、运行时索引残留，导致"资源明明改了但系统没刷新"
3. 把编辑器侧的便利逻辑偷渡进运行时路径，破坏已确认的"注册期可验证、运行期只消费"原则

### 决策
统一采用以下边界：
1. `OnValidate` 只允许做：
   - 字段合法性校验
   - 默认值修正
   - 版本字段补齐
   - 设置 `dirty` / `needsRebuild` 标记
   - 记录可观测日志（仅 Editor）
2. `OnValidate` 明确禁止做：
   - 直接重建运行时 Registry
   - 直接创建/销毁 `RenderBatchManager` 桶、Mesh、Material 实例
   - 直接触发运行时对象池重建
   - 在 Play 热路径里做同步全量扫描
3. 域重载开启时：
   - 允许依赖静态字段自然清空
   - 运行时索引在系统初始化阶段统一重建
4. 域重载关闭时：
   - 所有静态缓存必须提供显式 `ResetRuntimeState()` 或等价入口
   - 进入 PlayMode 前执行一次编辑器侧一致性刷新，清理残留 dirty / runtime index / registry cache
5. Play Mode 中资源变更的正式语义：
   - 允许标脏
   - 不允许在当帧热路径里直接全量重建
   - 刷新动作只能走 ADR-025 定义的固定编辑器工作流，由受控入口触发
6. 运行时系统初始化顺序必须保持：
   - Registry 重建先于 Batch 预热
   - Batch 预热先于首帧渲染

### 后果
**更容易的事**：
- Unity 编辑器生命周期边界清晰，不会把 `OnValidate` 变成万能入口
- 支持 Domain Reload 开/关两种模式下的一致行为
- 运行时热路径不被编辑器便利逻辑污染

**更困难的事**：
- 需要补一层显式 dirty 管理与 editor refresh orchestration
- 需要为关闭 Domain Reload 的场景增加额外自检与重置代码

---

## ADR-024: 统一资源描述必须版本化迁移

### 状态
已接受

### 上下文
当前 Bullet/VFX 已确认共享统一资源描述语义（`SourceTexture + UVRect + MaterialKey/BlendMode + 可选 AtlasBinding`），但如果没有 `SchemaVersion` 与正式迁移链路，后续字段演进会出现两个典型失败模式：
1. 新旧 SO 混跑，Inspector 看起来正常，运行时索引或资源解释却不一致
2. 只迁移 SO 资产本体，漏掉 prefab / scene 实例中的旧序列化数据

### 决策
统一资源描述采用显式版本化迁移制度：
1. 所有承载统一资源描述的 SO（至少 `BulletTypeSO`、`VFXTypeSO`，后续含 Laser/Spray 相关配置）必须包含 `SchemaVersion`
2. 版本升级只能通过顺序迁移链路执行：`vN -> vN+1`，禁止跨版本散装修补
3. 每一步迁移必须满足：
   - 幂等：重复执行结果一致
   - 可审计：输出迁移报告
   - 可 dry-run：先预检、后 apply
   - 可分级：必须区分阻断错误与警告，阻断错误禁止 apply，警告允许 apply 但必须进入 report 归档
4. 迁移器职责至少包括：
   - 字段重命名/拆分/合并
   - 默认值补齐
   - 非法组合修复或报错
   - 缺失资源引用报告
   - prefab / scene 实例扫描与验收清单输出
5. migration 边界正式定义为"资产层 schema 升级"，不是"运行时容错补丁系统"：
   - 运行时只消费当前版本数据
   - 遇到旧版本资源，Editor/Dev 报错并阻止进入正式链路
   - `OnValidate` 只允许做轻量补值与标脏，不承担跨版本迁移
   - fallback 只允许作为过渡期读兼容，不得演化成长期双轨运行
6. 兼容退出机制必须显式存在：当 `dry-run/apply/report` 在基线资产集与目标 prefab/scene 实例扫描中达到"阻断错误为 0"后，下一 schema 版本必须移除旧兼容字段与运行时 fallback；兼容读取最多允许保留一个过渡版本周期，禁止长期双轨并存；验收归档必须包含"本轮保留的兼容字段清单"和"下一轮删除清单"
7. AtlasMapping 等派生产物不得作为版本真相源；版本真相始终在原始资源描述资产上

### 后果
**更容易的事**：
- 资源模型可以持续演进，不会每次改字段都变成人工排雷
- 迁移责任明确在编辑器链路，不污染运行时
- prefab / scene 实例级风险被正式纳入验收，而不是靠运气

**更困难的事**：
- 需要维护迁移器与版本表
- 每次资源模型变更都必须同步补迁移步骤和验收口径

---

## ADR-025: 编辑器刷新工作流固定为 Registry 重建 → Batch 预热

### 状态
已接受

### 上下文
当前方案已经确认：
- RenderBatchManager 桶必须按注册表预热
- 运行时禁止隐式建桶
- VFX Registry 只允许在初始化/变更时重建

但如果不把"资源变更后到底如何刷新"写成固定工作流，执行阶段会反复出现：
- Inspector 改了资源，但 Registry 没同步
- Registry 重建了，但 Batch 没预热
- Play 前仍然 dirty，却带着旧缓存进入运行时

### 决策
编辑器侧统一采用固定刷新链路：
1. 资源变更源（Inspector / 迁移器 / 批量工具 / Atlas 工具回写）只负责标脏，不直接碰运行时对象
2. 刷新顺序固定为：
   - Step 1: 收集 dirty 资源
   - Step 2: 重建对应 Registry / RuntimeIndex
   - Step 3: 基于最新注册表执行 Batch 预热
   - Step 4: 输出刷新结果与失败项
3. 若 Step 2 失败，则 Step 3 不执行；系统保持旧运行时状态并报告失败
4. 若 Step 3 失败，则本次刷新整体视为失败，禁止静默部分成功
5. 进入 PlayMode 前若仍有未消费 dirty：
   - Editor/Dev：强制执行一次刷新；失败则报错并阻止继续验收链路
   - Release 构建前：必须通过预检，不能带 dirty 资源进入构建
6. 运行时 `Play()` / `Spawn()` / 渲染热路径不得承担"顺手刷新"职责
7. 刷新工作流必须暴露最小可观测指标：
   - dirty 资源数
   - 重建 Registry 数
   - 预热 Batch 数
   - 失败项列表
   - 最近一次刷新时间

### 后果
**更容易的事**：
- 资源改动后的系统行为可预测，避免"看起来改了但没生效"的鬼故事
- Registry 与 Batch 生命周期正式串起来，符合既有架构约束
- 问题定位更快，失败点能落在固定步骤上

**更困难的事**：
- 需要补 editor orchestration 与失败回滚/报表
- 编辑器工具必须遵守统一链路，不能各写各的捷径

---

## ADR-026: 子弹原生支持序列帧动画，不借道 VFX

### 状态
已接受

### 上下文
用户提出了非常现实的新需求：子弹本体不只是静态贴图，还可能是序列帧动画。如果当前设计只支持 `SourceTexture + UVRect` 的单帧采样，那么它只能覆盖"静态子弹 + 颜色/缩放/透明度动画"，无法覆盖"贴图内容本身逐帧变化"的子弹表现。

把这类需求外包给 VFX 看似省事，但会直接带来三个问题：
1. 子弹本体渲染与碰撞实体分离，生命周期更难对齐
2. 同一颗子弹需要额外 VFX 实例跟随，增加运行时管理复杂度
3. Bullet 与 VFX 的限界上下文被重新揉在一起，破坏既有边界

### 决策
子弹序列帧能力作为 Bullet 主链路原生能力落地：
1. `BulletTypeSO` 的资源描述扩展为两种采样模式：
   - `Static`
   - `SpriteSheet`
2. `SpriteSheet` 模式至少包含：
   - `SheetColumns/Rows` 或等价帧布局描述
   - `FrameCount`
   - `PlaybackMode`（至少 Once / Loop）
   - `TimeSource`（默认 BulletLifetimeNormalized）
   - 可选 `StartFrameOffset` / `FPS` 或等价播放参数
3. `BulletRenderer` 在写顶点时按当前帧计算 UVRect；不创建额外 VFX 实例
4. `BulletMover` 或等价更新阶段只负责产出当前播放时间/归一化进度；渲染器负责把时间解释为具体帧索引
5. 子弹序列帧不改变碰撞、运动、寿命等主逻辑语义；它只是 Bullet 视觉采样策略的扩展
6. 统一资源描述仍成立：序列帧子弹本质上仍是 `SourceTexture + UVRect + Playback` 的 Bullet 域扩展，而不是新的跨域模型

### 后果
**更容易的事**：
- 子弹本体可直接支持贴图逐帧变化，覆盖真实项目常见需求
- 不需要为每颗动画子弹额外维护跟随型 VFX 实例
- Bullet/VFX 边界仍然清晰：子弹负责"实体表现"，VFX 负责"附加效果"

**更困难的事**：
- `BulletTypeSO`、迁移器、Inspector、Renderer 都要同步扩展
- 渲染器需要支持按实例动态 UV 采样，测试面会变宽

**验收口径补充**：
- 新增运动类型的成功标准应解释为"无需修改 `BulletMover` 等核心热路径，只修改受控扩展点"，而不是字面意义上的"任何已有文件都不能改"

**实现约束补记（2026-04-12）**：
- 第一版序列帧子弹不把 `frameIndex` 或播放状态持久写入 `BulletCore`；继续以 `Lifetime/Elapsed + BulletTypeSO.SpriteSheetConfig` 在 `BulletRenderer` 渲染阶段现算
- `BulletRenderer` 负责解释"播放时间 -> frameIndex -> UVRect"，`BulletMover` 不承担帧索引缓存职责
- `BulletTypeMigrationTool` 是 `SchemaVersion` 升级的正式入口；`OnValidate` 只允许做轻量补值与标脏，不承担批量迁移
- 第一版只支持 `Static` / `SpriteSheet`、`StretchToLifetime` / `FixedFpsLoop` / `FixedFpsOnce` 三类播放策略；明确不在本轮引入 `PingPong`、`Reverse`、`RandomStartFrame`
- 验收样例必须至少覆盖 `StretchToLifetime`、`FixedFpsLoop`、`FixedFpsOnce` 各 1 个子弹样本，禁止只用循环样例替代单次播放样例
- 飞行阶段序列帧子弹与 `ExplosionMode.MeshFrame` 继续保持两套模型，避免为"顺手统一"扩大本轮改造面

---


## 三、执行约束



### Phase 0 启动前已决项
- ADR-001
- ADR-002
- ADR-005
- ADR-013

### Phase 1 执行约束
- Bullet 与 VFX 都支持独立贴图输入，运行时按贴图分桶
- UV/图集表达保留，但 atlas 仅为可选优化产物
- DamageNumber 继续使用共享数字图集，不纳入本轮多贴图自由策略
- sortingOrder 走共享常量，不做配置化

### Phase 2 执行约束
- CollisionEventBuffer 不替代 `ICollisionTarget`
- DanmakuSystem 拆分与 VFX 解耦同步推进
- MotionRegistry 不做开放式插件化
- 旧 SO 迁移器必须在资源模型调整后同步提供
- `MotionRegistry` 的验收标准统一为：新增运动类型时不改 `BulletMover` 等核心热路径，只改 `MotionType`、注册表和对应策略实现
- 多阵营能力本阶段只要求把 `SourceFaction/TargetFaction` 与过滤扩展点纳入数据模型，不要求同步交付完整阵营矩阵编辑器
- `CollisionEventBuffer` 默认容量统一建议值为 256；若后续压测上调，必须同步更新 overflow 监控阈值与验收基线

### Phase 3 执行约束
- 先完成 VFX 附着模式接口，再做喷雾 VFX 跟随
- 阵营通用模型不扩展到完整编辑器
- DamageNumber 仅接入共享排序与监控，不强改资源模型
- `AttachSource` 解析职责必须由独立 Resolver 接口承接；VFX 只依赖位置解析契约，不反向依赖 Danmaku 运行时类型
- Spray 附着式 VFX 的验收标准统一为：至少验证 `World` 与 `FollowTarget` 两种模式可用，且 `StopAttached`、目标失效冻结、循环播放停止三类收尾语义一致
- Bullet 视觉动画的性能验收必须包含 `AnimationCurve.Evaluate()` 的 IL2CPP 实测；若出现 GC 或不可接受抖动，允许回退 LUT，但不得改变外部配置语义

### Phase 4 执行约束
- Atlas 工具作为编辑器可选优化工具落地，不得成为资源导入前置步骤
- 工具必须支持 Bullet/VFX 打包与映射回写，DamageNumber atlas 仅做维护增强
- 迁移器工作流统一为 `dry-run -> apply -> report`；`dry-run` 必须输出待迁移资产数、风险项、缺失引用和 prefab/scene 实例扫描结果，`apply` 只处理已通过预检的数据集，`report` 作为验收归档产物
- `OnValidate` / 热重载 / Registry 刷新 / Batch 预热必须统一走固定编辑器工作流；刷新失败时必须保留旧运行时状态并显式报错，不允许"部分成功但静默继续"
- `RenderSortingOrder` 作为 sortingOrder 唯一代码来源；文档、调试 HUD、验收截图和实现代码都必须引用同一套命名


---

## ADR-027: 最终执行契约收口（Unity 最终一次性问题清单闭环）

### 状态
已接受

### 上下文
Unity 架构师在最终一次性评审中提出 25 个执行级问题，核心集中在四类风险：
1. 单一真相是否真的唯一，而不是"文档唯一、代码多源"
2. 运行时是否仍保留隐式补桶、隐式补迁移、隐式补刷新等后门
3. attached VFX、序列帧子弹、CollisionEventBuffer 等关键能力是否具备唯一语义
4. 验收口径、迁移退出机制、范围边界是否足够硬，能够直接判失败而不是靠解释

这些问题不再属于"方向是否正确"，而属于"执行契约是否足够硬"。因此需要以一条总 ADR 统一收口，避免各文档各答各的。

### 决策
以下结论作为最终执行契约，后续实现与验收必须逐条遵守：

1. **单一真相分层**
   - `RenderLayer` 只表达语义分层，不承载排序数值
   - `sortingOrder` 的唯一代码来源为 `RenderSortingOrder`
   - `MaterialKey/BlendMode` 的映射关系必须由共享渲染层统一定义，禁止 Bullet/VFX/Laser 各自解释
   - 业务代码、局部工具、临时调试逻辑均禁止内联新的排序数值或材质解释表

2. **Laser 边界**
   - Laser 至少必须遵守共享渲染契约：`RenderLayer`、`RenderSortingOrder`、`MaterialKey/BlendMode`
   - 若 Laser 不使用 `SourceTexture + UVRect`，则视为"共享渲染基础设施消费者"，而非统一资源描述值对象覆盖对象
   - 后续新增 Laser 视觉能力不得绕开共享材质键和排序规则；若要纳入统一资源描述，必须新增 ADR

3. **RenderBatchManager 运行时边界**
   - 运行时绝对禁止隐式建桶
   - 未注册 `(RenderLayer, SourceTexture, MaterialKey/BlendMode)` 在 Editor/Dev 记错误并计数，在 Release 跳过渲染并计数
   - 动态加载新资源的正式路径固定为：`注册/标脏 -> Registry 重建 -> Batch 预热 -> 结果报告 -> 允许显示`
   - 任何 `Play/Spawn/Render` 热路径都不得承担补注册、补预热、补刷新职责

4. **SchemaVersion 覆盖范围**
   - 第一版必须纳入 `SchemaVersion` 的资产：`BulletTypeSO`、`VFXTypeSO`
   - `LaserTypeSO`、`SprayTypeSO` 本轮不纳入统一资源描述版本链，但若引用共享渲染契约字段，仍必须遵守共享契约
   - 未纳入版本链的资产，不允许偷偷承载统一资源描述的演进责任；需要演进时必须显式升级到版本链

5. **migration 分级规则**
   - 阻断错误：缺失 `SourceTexture`、非法 `Static + PlaybackMode`、非法 `SpriteSheet + Reverse/PingPong/RandomStartFrame`、prefab/scene 实例引用断裂、共享契约字段缺失导致无法生成合法注册项
   - 警告：旧字段仍存在但可自动补齐、atlas 映射缺失但仍可回退到原始 `SourceTexture + UVRect`、可自动修正的默认值补齐
   - `dry-run` 必须输出阻断错误/警告分级；存在任一阻断错误时禁止进入 `apply`

6. **兼容退出机制**
   - "阻断错误为 0"必须同时覆盖：基线资产集 + prefab/scene 实例扫描
   - 必须完成一次正式 `report` 归档，才允许进入下一 schema 版本的兼容删除阶段
   - "最多保留一个过渡版本周期"按 `schema+1` 解释：在下一个 schema 版本中必须删除旧兼容字段与运行时 fallback

7. **序列帧子弹时间源与职责边界**
   - `StretchToLifetime` 只允许使用 `lifetime / maxLifetime`
   - `FixedFpsLoop` 与 `FixedFpsOnce` 只允许使用 `elapsedSeconds`
   - 同一配置禁止混用双时间源
   - `ResolveBulletUV()` 只负责"采样模式 -> frameIndex -> UVRect"解析，不负责颜色、缩放、Alpha、爆炸逻辑等其他视觉职责
   - 残影必须复用同一 UV 解析入口
   - 飞行阶段序列帧子弹与 `ExplosionMode.MeshFrame` 保持两套模型；若未来要统一，必须另立 ADR

8. **attached VFX 三段式语义**
   - `PlayAttached` 只负责创建实例与绑定句柄
   - `UpdateAttached` 只负责显式刷新派生姿态参数，不允许被系统偷偷折叠进 `PlayAttached`
   - `StopAttached` 是唯一合法的主动结束入口
   - 同一 `AttachSourceId + VFXType` 重复 `PlayAttached` 的唯一语义：先停止旧 handle，再创建新 handle；不允许隐式并存，也不允许一帧并存
   - 目标失效默认语义为"冻结到最后有效位置并播完"；"立即结束"只能通过显式配置开启；进入冻结收尾态后绝不自动恢复跟随
   - Resolver 失败、重复 Stop、无效 handle 必须具备：失败计数、最近一次失败原因、验收报告统计入口

9. **CollisionEventBuffer 边界**
   - `CollisionEventBuffer` 永远禁止承载主逻辑事实
   - 伤害、击退、死亡、状态变更不得依赖 Buffer 消费结果
   - `EffectsBridge` 只允许消费旁路事件并驱动 VFX/飘字/调试/统计，不允许反向修改 BulletWorld、CollisionSolver、MotionRegistry 等主状态
   - overflow 统计口径固定为"按事件计数、按性能验收窗口累计"；性能窗口内 `overflow count > 0` 直接判失败，不区分"偶发可放行"

10. **编辑器工作流与 Domain Reload 边界**
   - `OnValidate` 一律禁止：直接重建 Registry、直接预热 Batch、直接改运行时池、直接做跨版本迁移
   - 关闭 Domain Reload 时，所有持有静态缓存的模块都必须提供显式重置入口；缺失即视为验收不通过
   - 唯一合法刷新链路固定为：`标脏 -> Registry 重建 -> Batch 预热 -> 结果报告`
   - Atlas 工具、迁移器、Inspector 修改、批量工具必须全部走同一 orchestration；任一步失败都必须中断后续步骤并保留旧状态

11. **最终验收硬门槛**
   - 30 分钟上手测试起点固定为：模板默认状态 + 已有示例资产 + 指定 Demo 场景
   - 允许复制现有 `BulletTypeSO` / `VFXTypeSO` / Registry 示例资产作为模板；允许使用现有 Editor 工具与 Unity 原生 Inspector 操作；不允许改代码、写临时脚本、依赖隐藏入口
   - 55fps 验收环境必须固定：指定基线机型、固定 Demo 场景、Release/IL2CPP、持续 30 秒、关闭会污染结果的调试开关，并记录 build hash / 配置快照
   - `DrawCall ≤ 50`、`活跃 Batch ≤ 24`、`未知桶错误计数 = 0`、`overflow count = 0` 全部属于最终验收硬失败条件，任一超限即判不通过

12. **范围控制总表**
   - 文档必须显式区分：第一版必须做、第一版允许做但非阻塞、第一版明确不做、未来扩展点
   - `PingPong/Reverse/RandomStartFrame`、`frameIndex` 下沉 `BulletCore`、爆炸帧动画统一、完整阵营矩阵编辑器、Socket 完整实现、运行时动态补桶、长期 fallback 兼容，全部属于"第一版明确不做"或"未来扩展点"，不得在实现中顺手扩 scope

### 后果
**更容易的事**：
- 后续实现、验收、迁移、工具链都有统一硬口径，不再靠口头解释
- Unity 架构师提出的 25 个问题全部有可执行答案，后续不再继续开评审分支
- 文档间可做一致性校验，因为每个关键点都已落到唯一规则

**更困难的事**：
- 实现阶段不能再用"先跑起来再说"的方式偷过关
- 任何想走捷径的运行时后门、编辑器后门、兼容后门都会直接违反正式 ADR

## ADR-028: RuntimeAtlasSystem — 统一渲染管线核心（系统级重构）

### 状态
已接受 v2.1（2026-04-18 天命人确认全部 12 个未决项）

### 关键未决项决策结果

| ID | 决策 |
|----|------|
| UD-01 | BucketKey.Texture 直接拓宽为 `Texture`（方案 A） |
| UD-02 | 受控建桶 + MaxPages 硬上限（选项 2） |
| UD-03 | RT Lost 全量重 Blit |
| UD-04 | 保持源纹理引用不卸载 |
| UD-05 | 第一版不支持热更新，预留接口 |
| UD-06 | 引入 RuntimeAtlasConfigSO |
| UD-07 | 溢出回退到独立贴图 |
| UD-09 | 运行时完全忽略 AtlasBinding |
| UD-10 | 修改 WriteSegmentQuad 适配 Atlas UV |
| UD-11 | TrailPool 方案 A（独立 + 接入统计） |
| UD-12 | 全局单 RBM |

### 上下文

#### 问题 A：DrawCall 线性增长
Phase 4.1/4.2 完成了 Editor-only Atlas 工具链，但静态 Atlas 存在构建耦合、冗余加载、尺寸膨胀三个固有缺陷。

#### 问题 B：渲染系统割裂（v2.0 新增）
当前存在 6 条独立渲染路径（BulletRenderer / LaserRenderer / LaserWarningRenderer / DamageNumberSystem / TrailPool / VFXBatchRenderer），纹理管理、初始化协议、渲染提交、统计方式各自为政。其中 DamageNumberSystem 和 TrailPool 完全自管 Mesh/Material，不经过 RenderBatchManager。

天命人明确指示：RuntimeAtlasSystem **不是可选优化项，而是要替代当前割裂的渲染系统**，这是一次系统级重构。

### 决策

引入 RuntimeAtlasSystem 作为**统一渲染管线的核心基础设施**（v2.0：从"可选优化"升级为"系统必选"）：

1. **业务无关设计**：RuntimeAtlasSystem 只接受 `Texture2D` 返回 `AtlasAllocation(PageIndex, UVRect)`
2. **Channel 隔离**：不同业务域（Bullet / VFX / DamageText / Laser / Trail / Character）各自维护独立的 Atlas Page 池
3. **Shelf Packing 算法**：支持混合尺寸纹理，Best-Fit 策略
4. **无驱逐策略**：切关统一 `Reset()` 清空
5. **缓存去重**：按 InstanceID 只 Blit 一次
6. **自动溢出**：单张 Atlas 放不下时自动创建新 Page，直到 `MaxPages` 上限
7. **graceful degradation**：分配失败时回退到独立贴图模式
8. **WebGL 兼容**：`CommandBuffer.Blit()` + 专用 Shader
9. **Editor Atlas 保留不删**（v2.0）：Editor Atlas 工具链保留作为离线预览/资产管理工具，运行时由 RuntimeAtlasSystem 统一接管
10. **统一渲染管线**（v2.0 新增）：所有消费者通过 RuntimeAtlas + RBM 统一提交，DamageNumberSystem 和 TrailPool 迁移到统一管线
11. **系统必选**（v2.0 新增）：RuntimeAtlasSystem 是渲染前置条件，不是可选优化层。Supersedes ADR-007/008/010 的"可选优化"约束

### 对 ADR-007/008/010 的 Supersede

| 被 Supersede 的 ADR | 原约束 | ADR-028 的替代 |
|---------------------|--------|---------------|
| ADR-007 | 资源自由优先，Atlas 仅为可选优化 | RuntimeAtlas 为系统必选，不再可选 |
| ADR-008 | Atlas 不是生产前置条件 | RuntimeAtlas 是渲染前置条件 |
| ADR-010 | 不配置 Atlas 时系统正常运行 | RuntimeAtlasSystem 是统一管线核心 |

> 注：上述 ADR 在 Editor 环境中仍然成立（Editor Atlas 是可选的），但运行时由 ADR-028 接管。

### 对 ADR-015 的扩展

（与 v1.0 一致）

### BucketKey 类型扩展

（与 v1.0 一致）

### 迁移范围（v2.0 新增）

系统搭建后需将 6 条渲染路径统一迁移：
- BulletRenderer / LaserRenderer / LaserWarningRenderer / VFXBatchRenderer → 改用 RuntimeAtlas 纹理
- DamageNumberSystem → 从自管 Mesh 迁移到 RBM
- TrailPool → 接入统一 DC 统计（保留自管 Mesh 或 Quad 化，待定）

### 后果
**收益**（v2.0 增强）：
- 新增子弹/特效零手动 Atlas 工作
- 内存按需加载
- DrawCall 大幅削减（全局 ≤ 8 DC，ADR-029 v2 移除 Additive 后进一步缩减）
- **消除渲染系统割裂**——单一入口、统一协议、统一统计
- **新增渲染类型有归一化路径**——接入 RuntimeAtlas + RBM 即可

**代价**（v2.0 增加）：
- 运行时 Blit 开销
- 内存增加 32-48MB
- WebGL RT Lost 处理
- BucketKey 类型变更
- **迁移工作量**：DamageNumberSystem 和 TrailPool 需要重构渲染逻辑
- **预估工期增加**：从 8.5 天增加到 12.5 天

**放弃的东西**：
- 放弃 ADR-007/008/010 的"可选优化"定位（运行时）
- 放弃 ADR-015 的绝对纯粹性
- 放弃"源纹理 Blit 后即可卸载"的最优方案

### 关联文档
- 完整技术设计：`docs/Agent/RUNTIME_ATLAS_SYSTEM_TDD.md`（v2.1 已批准）
- 相关 ADR：001, 002, 007(Superseded), 008(Superseded), 010(Superseded), 015, 017, 019

---

## ADR-029: 彻底移除 Additive Blend — 统一 Normal，简化渲染管线

### 状态
已接受（v2 — 升级为彻底移除，Supersedes v1"降级为后门"）

### 上下文

（与 v1 相同——认知负担错配、弹幕过曝、规则不统一三大问题）

当前 `RenderLayer` 枚举（Normal / Additive）作为 `BulletTypeSO.Layer` 和 `VFXTypeSO.Layer` 的公开字段暴露在 Inspector 上，由策划手动选择。激光则硬编码为 Normal。

v1 决策是"降级为后门"——保留 Additive 代码但隐藏。**天命人复审后认为：YAGNI——不需要的代码就别留着，埋着看着费劲。**

### 决策

**彻底移除 Additive Blend 相关的全部代码、配置、Shader。统一使用 Normal（Alpha Blend）。**

#### 具体删除清单

| 类别 | 文件 | 操作 |
|------|------|------|
| 枚举 | `RenderLayer.cs` | 删除 `Additive = 1` 枚举值 |
| 常量 | `RenderSortingOrder.cs` | 删除 `BulletAdditive`、`VFXAdditive` |
| Shader | `DanmakuBulletAdditive.shader` + `.meta` | 整个删除 |
| RBM | `RenderBatchManager.cs` | `Initialize()` 删除 `additiveMaterial` 参数，简化材质选择逻辑 |
| 弹丸配置 | `BulletTypeSO.cs` | 删除 `Layer` 字段及 `[Header("渲染层")]` 区域 |
| VFX 配置 | `VFXTypeSO.cs` | 删除 `Layer` 字段 |
| 弹丸渲染 | `BulletRenderer.cs` | BucketKey 不再传 Layer；fallback 只建 1 个桶 |
| VFX 渲染 | `VFXRenderer.cs` | 同上 |
| 弹幕 Config | `DanmakuRenderConfig.cs` | 删除 `BulletAdditiveMaterial` 字段 |
| VFX Config | `VFXRenderConfig.cs` | 删除 `AdditiveMaterial` 字段 |
| 编辑器 | `VFXAssetBootstrapper.cs` | 删除 Additive 材质创建和赋值 |

#### BucketKey 简化

```csharp
// Before: BucketKey = (RenderLayer, Texture2D) — 二元组
// After:  BucketKey = Texture2D only — 降维

// RBM.Initialize() 签名变化：
// Before: Initialize(keys, normalMat, additiveMat, maxQuads, sortingOrderProvider)
// After:  Initialize(keys, material, maxQuads, sortingOrderProvider)
```

#### "看起来发光"的美术替代

需要发光效果的子弹/VFX，通过贴图美术制作实现（高亮中心 + 半透明光晕边缘），用 Normal Blend 渲染。东方 Project 就是这么做的——密集叠加时互相遮挡而非色彩过曝。

#### 如果将来真的需要 Additive？

重新加——但那是一个 **新的 ADR**，而不是"取消隐藏"。到那时项目对 Additive 的需求会更清晰，设计也会更合理。

### 后果

**收益：**
- 框架干净——没有死代码、没有隐藏后门
- BucketKey 从二元组降为单一 Texture，桶数直接减半
- RBM 签名简化，去掉一个必须但几乎不用的参数
- Shader 从 3 个减为 2 个（子弹 + 激光），少一个 variant
- 策划零认知负担，弹幕场景画面可控
- 所有已有 `.asset` 序列化的 `Layer` 字段（byte 0 = Normal）天然兼容

**代价：**
- 失去 Additive Blend 的 GPU 渲染能力（需重新实现时工作量约 2 天）
- 纯美术手段模拟发光效果，视觉精度不如真正的 Additive

**这是一个故意的不可逆删除——强制未来需求走正式 ADR 流程，而不是悄悄 unhide。**

### 关联
- Supersedes ADR-029 v1
- 影响 ADR-028（BucketKey 降维）
- 影响 TDD §3.0（RenderLayer 维度从设计中移除）

---

## ADR-030: TypeRegistry 内化 + 懒注册 + 懒建桶（Supersedes ADR-017）

### 状态
已接受（2026-04-20）

### 上下文

ADR-017 规定"RBM 桶在初始化期预热，运行时禁止隐式建桶"，其前置假设是 `DanmakuTypeRegistry` / `VFXTypeRegistrySO` 作为 public ScriptableObject 资产，由策划手动将每个 TypeSO 拖入数组。初始化时遍历 Registry 数组预建全部渲染桶。

**实际问题：**

1. **策划流程断裂**：新建 BulletTypeSO 后必须手动拖到 Registry，忘了就静默消失——运行时无报错、无兜底
2. **TypeRegistry 作为 public SO 暴露了框架实现细节**：开发者不应关心 index 分配和桶预热，这是框架内部事务
3. **ADR-017 的"禁止运行时建桶"过于刚性**：预热是性能优化手段，不应该是功能正确性的前提
4. **弹幕数据不存档、不跨会话**：RuntimeIndex 只需单次运行内稳定，不需要跨会话持久化

### 决策

#### 核心原则

**TypeRegistry 是框架内部实现细节，运行时零手工注册。** 开发者只接触两样东西：

```
1. BulletTypeSO / VFXTypeSO — 创建资产、配置参数
2. Spawner.Spawn(typeSO, ...) — 调 API 生成弹丸
```

#### 1. TypeRegistry 从 public SO 降级为 internal 运行时类

```csharp
// Before: [CreateAssetMenu] public class DanmakuTypeRegistry : ScriptableObject
//   → 策划在 Inspector 里手动拖入 BulletTypeSO[]
//   → 编辑器 OnValidate 标脏 → 重建索引 → 预热桶

// After: internal class（无资产文件，纯运行时内存结构）
internal class DanmakuTypeRegistry
{
    private readonly List<BulletTypeSO> _bulletTypes = new();
    private readonly Dictionary<BulletTypeSO, ushort> _bulletIndex = new();

    // 懒注册：首次使用时自动分配 index
    public ushort GetOrRegister(BulletTypeSO type)
    {
        if (_bulletIndex.TryGetValue(type, out ushort idx))
            return idx;
        
        idx = (ushort)_bulletTypes.Count;
        _bulletTypes.Add(type);
        _bulletIndex[type] = idx;
        return idx;
    }

    public BulletTypeSO GetType(ushort index) => _bulletTypes[index];
    public int Count => _bulletTypes.Count;
    
    // LaserType / SprayType 同理...
}
```

**变更清单：**

| 项 | Before | After |
|----|--------|-------|
| 类型 | `public class DanmakuTypeRegistry : ScriptableObject` | `internal class DanmakuTypeRegistry`（非 SO） |
| 持久化 | `.asset` 文件，Inspector 可编辑 | 不持久化，每次运行重建 |
| 可见性 | public，策划可见可编辑 | internal，框架外不可见 |
| 填充方式 | 策划手动拖入 | `GetOrRegister()` 懒注册 |
| index 分配 | 编辑器 `AssignRuntimeIndices()` 预分配 | 运行时首次使用时分配 |
| `[CreateAssetMenu]` | 有 | 删除 |

`VFXTypeRegistrySO` 同理降级为 `internal class VFXTypeRegistry`。

#### 2. RBM 支持运行时懒建桶（放松 ADR-017）

```csharp
// TryGetBucket 改为 GetOrCreateBucket
public bool GetOrCreateBucket(BucketKey key, BucketCreationInfo creationInfo, out RenderBucket bucket)
{
    // 热路径：O(1) 字典命中
    if (_bucketIndex.TryGetValue(key, out int idx))
    {
        bucket = _buckets[idx];
        return true;
    }

    // 冷路径：首次遇到新类型，动态建桶
    return TryCreateBucketDynamic(key, creationInfo, out bucket);
}
```

**约束：**
- 动态建桶每次 Spawn 新类型时只发生一次，后续帧全部命中字典 = 零额外开销
- 动态建桶有日志 + 计数（可观测性保留）
- `BucketCreationInfo` 携带 templateMaterial 和 sortingOrder，由 Renderer 层传入
- 动态桶 append 到桶数组末尾，触发一次重排序（极低频事件）

#### 3. 开发者新工作流

```
Before: 策划创建 TypeSO → 拖到 Registry → OnValidate 标脏 → 重建索引 → 预热桶 → 能用
After:  策划创建 TypeSO → Spawn(typeSO, ...) → 框架自动注册+建桶 → 能用
```

**中间环节全部由框架内部消化，运行时零手工注册。**

#### 4. 编辑器预热降级为可选优化

编辑器工作流（`DanmakuEditorRefreshCoordinator`）变更：

| 项 | Before | After |
|----|--------|-------|
| Registry 重建 | 必须——扫描 SO 资产、调 `AssignRuntimeIndices()` | 可选——作为预热提示，减少首帧 spike |
| Batch 预热 | 必须——遍历 Registry 预建全部桶 | 可选——预建已知类型的桶，未知类型运行时自建 |
| 进 PlayMode 前刷新 | 必须——缺少则运行时异常 | 可选——跳过也不影响功能正确性 |
| 资源完整性校验 | 与 Registry 绑定 | 独立为编辑器工具脚本（检查贴图引用是否丢失） |
| 发现机制 | 通过 Registry SO 枚举 | 改用 `AssetDatabase.FindAssets("t:BulletTypeSO")` 等直接扫描 TypeSO |

> **扩展点**：编辑器扫描默认全库搜索。若项目规模增长导致扫描过慢或捞出不相关资产，可通过 `AssetLabel`（如 `l:DanmakuActive`）或目录前缀收窄范围。当前阶段不实现过滤。

#### 5. 对关联 ADR 的影响

| ADR | 影响 |
|-----|------|
| **ADR-017** | **Superseded** — "运行时禁止隐式建桶"放松为"运行时可安全懒建桶" |
| **ADR-015** | 修正 — "VFX Registry 仅在初始化/变更时重建"改为"Registry 支持运行时懒注册，初始化重建变为可选预热" |
| **ADR-025** | 修正 — "Registry 重建 → Batch 预热"的固定链路降级为可选编辑器优化，不再是功能正确性前提 |
| ADR-002 | 不变 — BatchManager 仍然"共享实现，不共享实例" |
| ADR-028 | 兼容 — RuntimeAtlasSystem 的 Channel 注册同样可采用懒注册模式 |

### 后果

**收益：**
- **策划零摩擦**：创建 TypeSO + Spawn 即可见，不需要知道 Registry 的存在
- **框架内聚性提升**：实现细节不暴露为 public API，减少 API 表面积
- **运行时韧性**：任何 TypeSO 首次使用时自动建桶，不再有"忘记注册→静默消失"的问题
- **为 Lazy RT 铺路**：Atlas RT 不再需要在 init 阶段就存在（桶不依赖预知 Texture 实例）
- **减少资产文件**：删除 DanmakuTypeRegistry.asset 和 VFXTypeRegistrySO.asset

**代价：**
- 首次遇到新类型时有 1~2ms 的建桶开销（new Mesh + new Material），但仅发生一次
- 失去"启动期资源完整性校验"的隐式保证（需要独立编辑器工具显式补偿）
- TypeSO 的 RuntimeIndex 不再跨会话稳定（但弹幕系统无此需求）

**风险评估：**

| 风险 | 等级 | 缓解措施 |
|------|------|---------|
| 首帧 GC spike（密集类型场景） | 低 | 编辑器可选预热仍然可用 |
| 运行时建桶排序开销 | 极低 | 一次性事件，Array.Sort 在 <20 桶规模下 <0.1ms |
| 丢失启动期资源校验 | 中 | 独立编辑器校验工具补偿（不依赖 Registry） |

### 实施范围

| 文件 | 操作 | 工作量 |
|------|------|--------|
| `DanmakuTypeRegistry.cs` | 从 `public SO` 改为 `internal class`，增加 `GetOrRegister()`，删除 `[CreateAssetMenu]` | 0.5h |
| `VFXTypeRegistrySO.cs` | 同上 | 0.5h |
| `RenderBatchManager.cs` | `TryGetBucket` → `GetOrCreateBucket`，增加 `TryCreateBucketDynamic` | 1h |
| `BulletRenderer.cs` | Initialize 改为可选预热；Rebuild 中 Spawn 路径走 `GetOrRegister` + `GetOrCreateBucket` | 1h |
| `LaserRenderer.cs` / `LaserWarningRenderer.cs` | 同上 | 0.5h |
| `VFXBatchRenderer.cs` | 同上 | 0.5h |
| `DanmakuSystem.cs` | 删除 TypeRegistry 的 Inspector 引用，内部创建 internal registry | 0.5h |
| `DanmakuEditorRefreshCoordinator.cs` | 预热降级为可选，删除 Registry 重建的强依赖 | 0.5h |
| `BulletTypeSO.cs` / 其他 TypeSO | 删除 `OnValidate → MarkDirty(registry)` 的 Registry 联动 | 0.5h |
| 删除 `.asset` 文件 | `DanmakuTypeRegistry.asset` + `VFXTypeRegistrySO.asset` | 0.1h |
| 编辑器资源校验工具 | 新建独立脚本，扫描 TypeSO 检查贴图引用完整性 | 1h |
| `DanmakuSystem.API.cs` | `FireLaser`/`FireSpray` API 从 `byte typeIndex` 改为接收 `LaserTypeSO`/`SprayTypeSO` | 0.5h |
| `PatternScheduler.cs` + 调用方 | 更新 FireLaser/FireSpray 调用签名 | 0.5h |

**总预估：~8.5h（1~1.5 天）**

#### RuntimeIndex 迁移清单（PK UA-001 产出）

**TypeSO 字段删除**（4 处）：
- `BulletTypeSO.RuntimeIndex`（ushort）→ 删除，改由 registry 内部 Dictionary 管理
- `LaserTypeSO.RuntimeIndex`（byte）→ 同上
- `SprayTypeSO.RuntimeIndex`（byte）→ 同上
- `VFXTypeSO.RuntimeIndex`（ushort）→ 同上

**写入点重定向**（3 处）：
- `BulletSpawner.cs:64` → `core.TypeIndex = registry.GetOrRegister(type)`（不再读 `type.RuntimeIndex`）
- `DanmakuSystem.API.cs:251` → `laser.LaserTypeIndex = registry.GetOrRegister(laserType)`
- `SpriteSheetVFXSystem.cs:119/199` → `instance.TypeIndex = registry.GetOrRegister(vfxType)`

**反查机械替换**（22 处）：
- `registry.BulletTypes[core.TypeIndex]` → `registry.GetBulletType(core.TypeIndex)` — 12 处（BulletMover ×4, BulletRenderer ×2, CollisionSolver ×5, DanmakuSystem.API ×1）
- `registry.LaserTypes[laser.LaserTypeIndex]` → `registry.GetLaserType(...)` — 4 处（LaserUpdater, LaserRenderer, LaserWarningRenderer ×2）
- `registry.SprayTypes[spray.SprayTypeIndex]` → `registry.GetSprayType(...)` — 3 处（SprayUpdater, CollisionSolver ×2）
- `registry.TryGet(instance.TypeIndex, ...)` → 保持接口兼容 — 3 处（VFXBatchRenderer, SpriteSheetVFXSystem ×2）

#### 多类型生命周期安全性保证（PK UA-004 产出）

内部 `List<T>` 为 **append-only**，index 一旦分配终身有效（单次运行内）：

| 类型 | 创建时写入 | 持续更新时读取 | 销毁/重置 | 安全性 |
|------|-----------|---------------|-----------|--------|
| Bullet | `core.TypeIndex = GetOrRegister(type)` | `GetBulletType(core.TypeIndex)` 反查 | Phase=Dead → slot 回收 | ✅ |
| Laser | `laser.LaserTypeIndex = GetOrRegister(type)` | `GetLaserType(index)` 反查 | Phase=0 → slot 回收 | ✅ |
| Spray | `spray.SprayTypeIndex = GetOrRegister(type)` | `GetSprayType(index)` 反查 | Phase=0 → slot 回收 | ✅ |
| VFX | `instance.TypeIndex = GetOrRegister(type)` | `TryGet(index, out type)` 反查 | Free slot | ✅ |

**Domain Reload Off**：registry 由 `DanmakuSystem`（MonoBehaviour）持有，非 static。每次 Awake 重建 → 无残留。
**系统重置**：`ClearAllBullets` 等只清实例数据，不动 registry → 已注册类型仍有效。

#### RBM 懒建桶实施约束（PK UA-003 产出）

| 约束 | 设计方向 |
|------|----------|
| BucketKey | 纯 Texture（ADR-029 已决定，RenderLayer 废弃） |
| 数组扩容 | `List<RenderBucket>` 替代固定数组，初始容量 = 预热桶数 |
| 桶初始化 | 与现有 `Initialize()` 逻辑相同（new Mesh + Material.Instantiate + 预分配缓冲） |
| 排序重建 | 仅动态建桶时触发一次 `Sort()`，基于 sortingOrder |
| 失败回滚 | 超 MaxBuckets → 返回 false + 计入 `OverflowBucketErrorCount` |
| Dispose | 遍历 `_buckets.Count`（非固定长度），逐桶释放 |
| 统计指标 | `DynamicBucketCreatedCount`（累计）+ `DynamicBucketCreatedThisFrame`（帧内峰值） |

#### PK 评审记录

3 轮 PK（6 问题）已收敛。详见 `docs/Agent/Question.md`。

### 关联
- **Supersedes**: ADR-017
- **修正**: ADR-015, ADR-025
- **兼容**: ADR-002, ADR-028, ADR-029

---

## 四、最终结论


这次重构**不是要推翻重来**，而是要把原本"方向正确但边界没定死"的方案，补成一套真正能持续执行的架构决策包。

采纳本记录后，项目状态从：
- "可以讨论"

变为：
- "可以按阶段执行，并且每个阶段的返工风险已显著下降"

下一步建议：
1. 以本记录 + `REFACTOR_PLAN.md` 作为 Phase 0 启动依据
2. 执行前仅保留文档一致性维护，不再新增架构分叉讨论
3. 后续新增约束统一先回写 ADR，再同步计划与审计文档

