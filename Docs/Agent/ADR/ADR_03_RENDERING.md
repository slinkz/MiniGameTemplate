---
system: architecture
scope: adr-rendering
last_verified: 2026-05-02
depends_on: [ADR_01_FOUNDATION, ADR_02_DANMAKU]
related_code: Assets/_Framework/Rendering/*.cs, Assets/_Framework/RuntimeAtlas/*.cs
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

