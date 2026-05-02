---
system: architecture
scope: adr-foundation
last_verified: 2026-05-02
related_code: Assets/_Framework/Rendering/*.cs, Assets/_Framework/Danmaku/*.cs
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

