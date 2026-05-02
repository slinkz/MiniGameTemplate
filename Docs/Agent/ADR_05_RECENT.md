---
system: architecture
scope: adr-recent
last_verified: 2026-05-02
related_code: Assets/_Framework/EntitySystem/*.cs, Assets/_Framework/RuntimeAtlas/*.cs
---


- **日期**：2026-04-21
- **状态**：✅ Implemented（代码已完成 + CR 修复 + 编译通过，待真机验收）
- **Supersedes**：无（增量扩展 ADR-028/029/030 的渲染架构）

### 背景

R0~R5 + R4.1/R4.3/R4.4 完成后，Editor Play Mode 验收通过 8/12 AC。三项深化任务旨在补全 RuntimeAtlas 的完整度并优化内存占用：

1. **R4.4A 懒建页（Lazy Page Creation）**：InitChannel 时不再无条件创建 Page 0，延迟到首次 Allocate。微信小游戏环境可节省最多 32 MB RT 内存。
2. **Laser 接入 RuntimeAtlas（方案 C）**：`LaserTypeSO.UseRuntimeAtlas` 字段控制——禁用 UV 滚动的激光入 Atlas 合并 DC，保留 UV 滚动的走独立贴图 fallback。
3. **Trail 纹理化**：`BulletTypeSO.TrailTexture` 新增纹理支持，所有 Trail（含无纹理的 whiteTexture fallback）统一走 Atlas Channel.Trail，保持 1 DC。

### 关键决策

| 编号 | 决策 | 理由 |
|------|------|------|
| PI-001 | DanmakuSystem 持有唯一 RuntimeAtlasManager 共享实例，通过 Initialize 参数注入各 Renderer | 避免多实例创建 18 个冗余 Channel；Channel 隔离已保证互不干扰 |
| PI-002 | Laser Atlas 模式下 UV.y 归一化到 [0,1]（整条激光映射完整纹理一次） | Atlas RT wrapMode=Clamp 无法支持 UV 环绕；短激光细节清晰，长激光可接受拉伸 |
| PI-003 | GetStats() 的 `Pages.Count` 直接使用（懒建页后 Count=0 时 fillRate=0） | 语义精确，totalPixels=0 时分母保护 |
| PI-004 | Trail RT Lost 恢复路径：Render() 中检测 IsCreated() → 回退 whiteTexture → 下帧 TryGetAllocation 恢复 | 保证 Trail 渲染不中断 |
| PI-005 | ResolveLaser() 签名精简：去掉冗余 fallbackTexture 参数 | 从 LaserTypeSO 直接读取 LaserTexture 即可 |

### 实施顺序

```
R4.4A (懒建页, 2h) → Laser (2.5d) / Trail (3d) 可并行
                      总计 5.75 天
```

### 验收标准

见 `UnityProj/docs/Agent/PHASED_IMPLEMENTATION_PLAN.md` 中各方案的 AC 列表。

### PK 评审

1 轮 5 个问题（PI-001~005）已收敛。详见 `UnityProj/docs/Agent/PHASED_IMPL_PK_Question.md`。

### 代码评审（2026-04-21）

TDD 符合性：100%，零偏离。代码评审发现 6 项，修复 3 项：

| CR | 位置 | 严重度 | 描述 | 处置 |
|---|---|---|---|---|
| CR-01 | RuntimeAtlasManager.HandleRTLost() | ⚠️ 中 | 懒建页后空 Channel 被标记 PendingRestore | ✅ 修复：跳过 Pages=0 且 SourceTextures=0 的 Channel |
| CR-02 | RuntimeAtlasManager.TryAllocateInternal() | ℹ️ 低 | 热路径额外分支（Pages.Count==0 检查） | 接受：branch predictor 学习后开销≈0 |
| CR-03 | LaserRenderer.WriteSegmentQuad() | ⚠️ 中 | `width < 1f` 浮点比较判断 Atlas 模式不健壮 | ✅ 修复：改为显式 bool `usesAtlas` 参数传递 |
| CR-04 | TrailPool.BuildTrailMesh() | ⚠️ 中 | `pointCount==1` 时除零风险 | 接受：上层 guard `PointCount < 2` 已保护 |
| CR-05 | TrailPool.Render() RT Lost 恢复 | ℹ️ 低 | whiteTexture 探针可能永不命中 | 接受：设计意图——Atlas 不可用时走 fallback |
| CR-06 | BulletRenderer/DamageNumberSystem.GetAtlasStats() | ℹ️ 低 | 共享 Atlas 后返回全局统计（遗留 API） | ✅ 修复：标注 `[Obsolete]` |

编译结果：Unity 2021.3.17f1 — **0 errors / 0 warnings**。

### 关联
- **扩展**: ADR-028（RuntimeAtlas 核心决策）, ADR-029（Additive Blend 移除）, ADR-030（TypeRegistry 内化）
- **兼容**: ADR-002, ADR-015

---

## ADR-032：`new Material()` 必须显式复制 shaderKeywords — Laser Atlas 不可见 Bug 修复

- **日期**：2026-04-21
- **状态**：✅ Implemented
- **类型**：Bug Fix + 防御性规范
- **影响范围**：RenderBatchManager、LaserRenderer、LaserWarningRenderer、DanmakuLaser.shader

### 问题现象

`LaserTypeSO.UseRuntimeAtlas = true` 时，激光和预警线在 Editor Play Mode 下完全不可见（alpha=0）。

### 根因分析（三层）

本次 Bug 是三个独立问题叠加的结果，最终表现为同一个症状——"不可见"。

| 层级 | 根因 | 影响 |
|------|------|------|
| **L1（Shader 变体）** | `DanmakuLaser.shader` 缺少 Atlas 模式分支。UV.x 在 Atlas 模式下被压缩到子区域（如 `[0, 0.125]`），`abs(UV.x - 0.5) * 2.0` 计算出的 `distFromCenter ≈ 0.875`，`coreMask=0, glowMask=0` → 完全透明 | 即使 keyword 正确也会透明 |
| **L2（UV 映射）** | LaserRenderer / LaserWarningRenderer 的 UV.x 直接使用 Atlas 子区域坐标，未保持 `[0,1]` 归一化 | 渐变参数语义被破坏 |
| **L3（Keyword 丢失 🔴 真正根因）** | `RenderBatchManager.CreateBucket()` 中 `new Material(templateMaterial)` **不可靠保留 shader keyword**。`_ATLASMODE_ON` 在模板材质上已设置，但克隆后丢失，导致 Shader 走非 Atlas 分支 → UV.x 被当作纹理 UV 而非渐变参数 | L1 和 L2 的修复全部失效 |

**L3 是最隐蔽也最关键的根因**：Unity 的 `new Material(source)` 构造函数在不同版本和不同运行环境下，对 `shaderKeywords` 的复制行为不一致（尤其是 `multi_compile_local` 定义的局部 keyword）。这不是 Unity 文档中明确记载的行为差异，只能通过运行时反射诊断发现。

### 修复方案

#### 1. `RenderBatchManager.CreateBucket()`（核心修复）

```csharp
Material matInstance = new Material(templateMaterial);
// 关键修复：显式复制 shaderKeywords
matInstance.shaderKeywords = templateMaterial.shaderKeywords;
```

**规范升级**：项目内所有 `new Material(source)` 之后，必须紧跟 `shaderKeywords` 显式赋值。这是防御性要求，不依赖 Unity 版本行为。

#### 2. `DanmakuLaser.shader`（Shader 变体）

```hlsl
#pragma multi_compile_local __ _ATLASMODE_ON

#ifdef _ATLASMODE_ON
    fixed4 tex = fixed4(1, 1, 1, 1);  // 跳过纹理采样，程序化渐变驱动
#else
    fixed4 tex = tex2D(_MainTex, i.uv);
#endif
```

**设计权衡**：Atlas 模式跳过纹理采样意味着失去纹理细节叠加，但激光视觉 90%+ 由程序化渐变（CoreColor + GlowColor + smoothstep）驱动，影响极小。如需纹理细节，可后续用 UV2 通道传 Atlas 子区域 x 范围。

#### 3. `LaserRenderer.cs` / `LaserWarningRenderer.cs`（UV 语义分离）

- Atlas 模式：UV.x = `[0, 1]`（渐变参数），UV.y = Atlas 子区域归一化
- 非 Atlas 模式：UV.x = `[0, 1]`（渐变参数），UV.y = 世界空间累积（wrapMode=Repeat）
- 新增 `_laserMaterialAtlas` 材质克隆（`EnableKeyword("_ATLASMODE_ON")`），Dispose 时销毁

#### 4. `RenderBatchManager.TryGetOrCreateBucket()`（重复桶防御）

- 新增线性兜底扫描：字典索引不同步时避免同 key 重复建桶
- 动态建桶后显式 `RebuildBucketIndex()` 再排序，不依赖排序副作用维护索引

### 关键决策

| 编号 | 决策 | 理由 |
|------|------|------|
| FIX-001 | `new Material()` 后必须显式赋值 `shaderKeywords` | Unity 行为不可靠，防御性编程，一行代码零成本 |
| FIX-002 | Atlas Laser 跳过 `tex2D` 采样而非尝试重映射 UV.x | 避免 Atlas 子区域边缘采样溢出；激光以程序化渐变为主，纹理贡献极低 |
| FIX-003 | UV.x 始终保持 `[0,1]` 作为渐变参数语义 | Shader 的 `distFromCenter` 计算硬编码 `abs(x-0.5)*2`，UV.x 必须是归一化的 |
| FIX-004 | 动态建桶增加线性去重兜底 | 冷路径优先正确性；字典索引可能因排序/重建时序不一致而暂时失效 |

### 踩坑经验（项目级规范化）

> **🔴 铁律：Unity `new Material(source)` 不保证复制 `shaderKeywords`。**
>
> 在项目的**任何位置**进行材质克隆后，必须紧跟：
> ```csharp
> clone.shaderKeywords = source.shaderKeywords;
> ```
> 违反此规则的代码在 `multi_compile_local` 变体场景下会静默失败——材质看起来"创建成功"但 keyword 为空，Shader 走错分支，表现为视觉异常或不可见。

### 排查思路备忘

本次 Bug 的诊断路径值得记录，以便未来遇到"材质/Shader 表现不符预期"时快速定位：

1. **确认渲染数据存在**：通过运行时反射检查 pool active count、draw count → 排除"根本没发射"的可能
2. **确认纹理绑定正确**：检查 bucket material 的 `mainTexture` → 排除"纹理没绑上"
3. **检查 shaderKeywords**：反射读取 bucket material 的 `shaderKeywords` 数组 → **发现为空**，定位到 keyword 丢失
4. **逆推克隆链路**：`CreateBucket()` → `new Material()` → 确认是构造函数行为导致

### 变更文件

| 文件 | 修改类型 | 关键变更 |
|------|----------|----------|
| `DanmakuLaser.shader` | Shader 变体 | `_ATLASMODE_ON` keyword 分支 |
| `LaserRenderer.cs` | 功能修复 | Atlas 材质克隆 + UV 语义分离 |
| `LaserWarningRenderer.cs` | 功能修复 | Atlas 材质克隆 + UV 语义分离 |
| `RenderBatchManager.cs` | 核心修复 + 防御 | `shaderKeywords` 显式复制 + 重复桶防御 |

### 验证结果

- Unity 编译：**0 errors / 0 warnings**
- Editor Play Mode：`UseRuntimeAtlas=true` 时激光恢复可见
- 运行时诊断确认：`shaderKeywords = ["_ATLASMODE_ON"]`，Shader 走正确分支

### 关联

- **修复对象**: ADR-031（Laser 接入 RuntimeAtlas）
- **影响**: ADR-028（RuntimeAtlas 核心决策）、ADR-030（TypeRegistry + 懒建桶）
- **新增规范**: 项目级 `new Material()` shaderKeywords 显式复制要求

---

## ADR-033: Entity-Component 通用角色框架

### 状态

已接受（2026-04-25）

### 上下文

MiniGameTemplate 需要一套品类无关的通用角色管理框架，支撑塔防、射击、ARPG、跑酷、放置等不同类型小游戏。当前模板有完善的弹幕系统（DanmakuSystem）、碰撞系统（CollisionSolver + TargetRegistry）、对象池（PoolManager）、事件系统（GameEvent SO）、渲染管线（RBM + RuntimeAtlas），但没有"角色"这一层抽象。

设计草案 v1.0 提出了 Entity + 8 组件的架构，但存在以下未解决问题：
1. 碰撞系统是弹幕专用的，TargetRegistry 限 16 槽位，如何桥接角色？
2. GameEvent SO 是全局广播模式，角色内部通信需要本地事件总线
3. 现有 PoolManager 是 GameObject 池，Entity 需要纯数据池
4. 渲染管线是 instanced quad（弹幕优化），角色渲染走不同管线

### 决策

#### D1: Entity 纯逻辑层，不绑 GameObject

Entity 是纯 C# 对象，不继承 MonoBehaviour，不持有 GameObject。渲染表现由游戏层的 EntityView 桥接。

**放弃了什么**：不能直接拖 Inspector 配置 Entity 组件。
**换来了什么**：可单元测试、零 MonoBehaviour 开销、逻辑/表现完全解耦。

#### D2: CollisionComponent 实现 ICollisionTarget，桥接到现有 TargetRegistry

不新建碰撞系统，复用弹幕碰撞管线。TargetRegistry 16 槽位不扩容，改用动态注册/注销策略（按距离优先级选 16 个注册）。

**放弃了什么**：不是所有 Entity 都能同时被弹幕命中。
**换来了什么**：零碰撞系统重建、与弹幕系统天然联动、性能模型不变。

#### D3: Entity 本地事件总线（EntityEventBus），独立于全局 GameEvent SO

每个 Entity 独立事件总线，用 struct 事件 + 泛型分发，零 GC。跨 Entity 通信仍走全局 GameEvent SO。

**放弃了什么**：Entity 内部事件不能在 Inspector 可视化连线。
**换来了什么**：事件不跨 Entity 污染、零 GC、池化时自动清订阅。

#### D4: EntityPool 采用预分配数组 + 空闲槽位栈（参考 BulletWorld 模式）

不复用 PoolManager（那是 GameObject 池），为 Entity 专建纯数据池。

**放弃了什么**：不能复用已有 PoolManager 代码。
**换来了什么**：零 GC、O(1) 取出/归还、与弹幕 SoA 模式一致。

#### D5: Phase 1 不做渲染集成

Entity 层纯逻辑，渲染集成推迟到 Phase 2。Phase 1 的 AnimationComponent 只管状态→动画 ID 映射，不操作任何渲染对象。

**放弃了什么**：Phase 1 看不到角色视觉。
**换来了什么**：减少 Phase 1 范围、渲染方案可以延迟决策（Spine vs 序列帧）。

#### D6: Luban 配置驱动，新增 4 张配置表

TbEntityConfig / TbStateConfig / TbAIBehavior / TbAnimMapping，通过 ConfigManager.Tables 访问。

### 后果

**变得更容易的**：
- 新增角色类型：只加配置表行 + 美术资源
- 新增组件类型：实现接口 + 注册到工厂
- 单元测试：Entity/组件不依赖 Unity 运行时
- 快速换皮：配置表驱动，代码不改

**变得更难的**：
- Phase 2 需要设计 EntityView 桥接层（逻辑→视觉）
- TargetRegistry 16 槽位限制需要动态注册策略（额外复杂度）
- Entity vs Entity 碰撞需要独立 Solver（Phase 2）
- Luban 配置表数量增加（4 张新表）

### 关联

- **TDD 文档**：Docs/Agent/ENTITY_COMPONENT_TDD.md v2.0
- **设计草案**：MiniGameTemplate-EntityComponent-Design.md v1.0
- **碰撞集成依赖**：ADR-012（多阵营碰撞模型）
- **对象池参考**：BulletWorld（DanmakuSystem/Scripts/Data/BulletWorld.cs）
- **事件系统参考**：EventSystem/Scripts/GameEvent.cs + GameEvent_T.cs

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

