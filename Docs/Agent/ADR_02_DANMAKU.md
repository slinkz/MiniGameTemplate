---
system: architecture
scope: adr-danmaku
last_verified: 2026-05-02
depends_on: [ADR_01_FOUNDATION]
related_code: Assets/_Framework/Danmaku/*.cs, Assets/_Framework/VFX/*.cs
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

