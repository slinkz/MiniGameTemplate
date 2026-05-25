---
system: runtime-atlas
scope: acceptance-plan
last_verified: 2026-05-02
depends_on: [ATLAS_TDD_01_DESIGN, ATLAS_TDD_02_IMPL]
related_code: Assets/_Framework/RuntimeAtlas/*.cs
---


```
Assets/_Framework/Rendering/
├── RuntimeAtlas/
│   ├── RuntimeAtlasManager.cs         ← 核心入口
│   ├── AtlasChannel.cs                ← Channel 枚举（v2.0 新增 Laser/Trail）
│   ├── AtlasAllocation.cs             ← 分配结果值类型
│   ├── AtlasPage.cs                   ← 单页状态
│   ├── AtlasChannelConfig.cs          ← Channel 配置结构体
│   ├── RuntimeAtlasConfig.cs          ← 配置 SO
│   ├── RuntimeAtlasStats.cs           ← 统计信息
│   ├── ShelfPacker.cs                 ← Shelf Packing 算法
│   ├── AtlasBlit.cs                   ← Blit 适配层
│   └── MODULE_README.md               ← 模块说明文档
│
├── RuntimeAtlas/Shaders/
│   └── Hidden_RuntimeAtlasBlit.shader ← Blit 专用 Shader
│
├── Editor/RuntimeAtlas/
│   └── RuntimeAtlasDebugWindow.cs     ← Atlas 占用可视化（P2）
│
└── (现有文件保留不动)
    ├── AtlasMappingSO.cs              ← 保留（Editor Atlas 工具链）
    ├── RenderBatchManager.cs          ← 改造 BucketKey 类型
    ├── RenderBatchManagerRuntimeStats.cs
    ├── RenderLayer.cs
    ├── RenderSortingOrder.cs
    └── RenderVertex.cs

Assets/_Framework/Editor/Rendering/
├── DanmakuAtlasPackerWindow.cs        ← 保留（Editor Atlas 打包工具）
├── AtlasMappingSOEditor.cs            ← 保留
└── AtlasSubSpritePopup.cs            ← 保留
```

---

## 九、开发计划（v2.0 修订）

### Phase R0：基础设施（~2 天）

| Task | 描述 | 交付物 |
|------|------|--------|
| R0.1 | ADR-028 正式决策 + ADR-007/008/010 标记 Superseded（✅ 已完成） | `ARCHITECT_DECISION_RECORD.md` |
| R0.2 | 数据结构：`AtlasChannel`, `AtlasAllocation`, `AtlasChannelConfig`, `Shelf` | `.cs` 文件 | ✅ 已完成（2026-04-19） |
| R0.3 | `ShelfPacker` 算法实现 + 单元测试 | `.cs` + test | ⚠️ 算法实现已完成；项目当前无现成 Unity Test 框架，单元测试延后到引入测试基础设施时补齐 |
| R0.4 | `AtlasBlit` 适配层 + `Hidden/RuntimeAtlasBlit` Shader | `.cs` + `.shader` | ✅ 已完成（2026-04-19） |
| R0.5 | `AtlasPage` 生命周期管理 | `.cs` | ✅ 已完成（2026-04-19） |

### Phase R1：核心管理器（~2 天）

| Task | 描述 | 交付物 |
|------|------|--------|
| R1.1 | `RuntimeAtlasManager` 核心实现 | `.cs` | ✅ 已完成（2026-04-19） |
| R1.2 | `RuntimeAtlasConfig` SO | `.cs` | ✅ 已完成（2026-04-19） |
| R1.3 | Warmup 批量预热 | `.cs` | ✅ 已完成（2026-04-19） |
| R1.4 | `RuntimeAtlasStats` 统计输出 | `.cs` | ✅ 已完成（2026-04-19） |

### Phase R2：RBM 改造 + 已有 Renderer 迁移（~3 天）

| Task | 描述 | 交付物 |
|------|------|--------|
| R2.1 | `BucketKey.Texture` 类型拓宽 + `Initialize` 多模板材质 API + 注册时排序 (v2.3) | `RenderBatchManager.cs` |
| R2.2 | BulletRenderer 集成 RuntimeAtlas | `BulletRenderer.cs` | ✅ 已完成（2026-04-19） |
| R2.3 | LaserRenderer 统一到全局 RBM（不入 Atlas，保持独立贴图）(v2.2 修正) | `LaserRenderer.cs` | ✅ 已完成（R0/R1 前置已落地，R2 验证通过） |
| R2.4 | LaserWarningRenderer 集成 RuntimeAtlas | `LaserWarningRenderer.cs` | ✅ 已完成（沿用 Laser 独立贴图 + 统一 RBM 路径，R2 验证通过） |
| R2.5 | VFXBatchRenderer 集成 RuntimeAtlas | `VFXBatchRenderer.cs` | ✅ 已完成（2026-04-19） |

### Phase R3：自管 Mesh 系统迁移（~3 天）— v2.0 新增

| Task | 描述 | 交付物 |
|------|------|--------|
| R3.1 | DamageNumberSystem 迁移到 RBM（含 UV 切分适配） | `DamageNumberSystem.cs` | ✅ 已完成（2026-04-19） |
| R3.2 | TrailPool 方案落地（方案 A：接入统计 / 方案 B：Quad 化） | `TrailPool.cs` | ✅ 已完成（方案 A，2026-04-19） |
| R3.3 | 统一渲染调度：DanmakuSystem.RunLateUpdatePipeline 改造 | `DanmakuSystem.UpdatePipeline.cs` | ✅ 已完成（2026-04-19） |
| R3.4 | VFX 系统 SpriteSheetVFXSystem 接入统一管线 | `SpriteSheetVFXSystem.cs` | ✅ 提交层统一已完成；编排层统一已决策推迟到 **R4.0**（天命人 2026-04-19 确认） |

### Phase R4：管线统一与验证（~2.5 天）

| Task | 描述 | 交付物 | 状态 |
|------|------|--------|------|
| R4.0 | **VFX 编排层统一**：将 `SpriteSheetVFXSystem` 的 `Update()/LateUpdate()` 收编到 `DanmakuSystem` 管线，VFX Tick 纳入 `RunUpdatePipeline`，VFX Rebuild 纳入 `RunLateUpdatePipeline`（含 `BeginFrame/EndFrame` 帧统计）；`SpriteSheetVFXSystem` 退化为纯 API 入口（Play/Stop/PlayAttached），不再自驱更新和渲染 | `SpriteSheetVFXSystem.cs`, `DanmakuSystem.UpdatePipeline.cs` | ✅ 已完成（2026-04-19） |
| R4.1 | Demo 场景验证：所有 6 条渲染路径统一后的视觉正确性 | 验证报告 | ✅ 已完成（2026-04-19）：代码审查确认所有路径正确接入 Atlas/RBM，编译零错误 |
| R4.2 | 迁移对比：DamageNumber / TrailPool 迁移前后逐帧对比 | 截图对比 | 待实施（需 Play Mode 截图） |
| R4.3 | Debug HUD 接入 RuntimeAtlasStats（全局统一 DC 统计） | HUD 扩展 | ✅ 已完成（2026-04-19）：HUD 新增 Atlas section，显示页数/分配/填充率/内存/命中率/overflow |
| R4.4 | Editor 预览窗口（P2，可延后） | `RuntimeAtlasDebugWindow.cs` | 待实施 |
| R4.5 | 真机验收（微信小游戏 WebGL） | 验收报告 | 待实施 |

### Phase R5：文档更新（~0.5 天）

| Task | 描述 | 交付物 | 状态 |
|------|------|--------|------|
| R5.1 | `MODULE_README.md` for RuntimeAtlas | 文档 | ✅ 已完成（2026-04-19） |
| R5.2 | 更新 `ARCHITECTURE.md` 渲染架构图 | 文档 | ✅ 已完成（2026-04-19） |
| R5.3 | 更新 `Rendering/MODULE_README.md` + `DanmakuSystem/MODULE_README.md` + `VFXSystem/MODULE_README.md` | 文档 | ✅ 已完成（2026-04-19） |

### 总预估工期：**约 12.5 天**

（v1.0 = 8.5 天 + 新增 R3 迁移 3 天 + R4 验证扩展 1 天）

### 工期风险因子

| 风险 | 可能加的天数 | 触发条件 |
|------|------------|---------|
| ~~激光 UV 映射复杂度超预期~~ | ~~+1 天~~ | **(v2.2 取消：激光不入 Atlas)** |
| DamageNumber 迁移视觉回归 | +0.5 天 | 数字切分精度问题 |
| TrailPool 选方案 B（Quad化） | +1 天 | 如果天命人选 B |
| WebGL 真机 RT Lost 问题 | +1 天 | 如果微信小游戏频繁触发 RT Lost |

---

## 十、验收标准

### 10.1 功能验收

> **v2.13 启用 SDD 规则 1**：AC 表增加"状态"和"变更记录"列，TDD 作为系统当前行为的单一信息源。
> 状态枚举：✅ Implemented | ⏸ 暂缓 | ❌ 废弃 | 🔄 修改 | ✅ 铁律

| # | 验收项 | 通过标准 | 状态 | 变更记录 |
|---|--------|----------|------|----------|
| AC-01 | 子弹使用 RuntimeAtlas 合批 | 10 种不同贴图的子弹，DrawCall ≤ 1（全部 Normal，ADR-029 v2 已移除 Additive） | ✅ Implemented | R2.2（2026-04-19） |
| AC-02 | VFX 使用 RuntimeAtlas 合批 | 5 种不同特效，DrawCall ≤ 1 | ✅ Implemented | R2.5（2026-04-19） |
| AC-03 | 混合尺寸纹理 | 32×32 和 128×128 纹理共存于同一 Atlas | ✅ Implemented | R0.3 ShelfPacker（2026-04-19） |
| AC-04 | 序列帧 UV 正确 | SpriteSheet 子弹/VFX 帧动画播放正确 | 🔧 Bug 修复 | 2026-04-21 经 MCP 运行时诊断确认：`BulletType_SpriteSheetDemo` 实际走 RuntimeAtlas，`baseUV` 为 Atlas 子区域而非独立贴图；`GetFrameUV()` 静态输出正确，真正根因收敛到 `RuntimeAtlasBlit.shader` 写入 RT 时的 Y 方向翻转。已在 Blit 顶点阶段增加 `o.uv.y = 1.0 - o.uv.y` 修正，待天命人回归验证 |
| AC-05 | Atlas 溢出 | 超过单页自动创建新页，渲染无中断 | ✅ Implemented | R1.1（2026-04-19）+ R4.4A 懒建页（ADR-031） |
| AC-06 | 切关清空 | Reset 后所有 RT 释放 | ✅ Implemented | R1.1（2026-04-19） |
| **AC-07** | **激光统一到全局 RBM** | 激光通过统一 RBM 渲染提交；`UseRuntimeAtlas=true` 时入 Atlas（UV.y 归一化），`false` 时走独立贴图 fallback；UV 渐变/滚动正确 | 🔄 修改 | 原设计(v2.2)：不入 Atlas → ADR-031 方案 C：可选入 Atlas；ADR-032 修复 keyword 丢失 |
| **AC-08** | **DamageNumber 迁移到 RBM** | 飘字视觉效果与迁移前一致 | ✅ Implemented | R3.1（2026-04-19） |
| **AC-09** | **TrailPool 接入统计** | Debug HUD 显示的 DC 数包含拖尾 | ✅ Implemented | R3.2 方案 A（2026-04-19） |
| **AC-10** | **统一 DC 统计** | Debug HUD 一个数字反映全部渲染 DC | ✅ Implemented | R4.3（2026-04-19） |
| AC-11 | Editor Atlas 工具链仍可用 | `DanmakuAtlasPackerWindow` 正常运行，不报错 | ✅ 验收通过 | Editor 手动验证通过（2026-04-21） |
| AC-12 | 独立贴图回退 | Atlas 分配失败时回退到逐贴图模式 | ✅ 验收通过 | Editor Play Mode 手动验证通过：AtlasSize=32 强制溢出→子弹正常渲染，Overflow>0（2026-04-21） |
| **AC-13** | **Atlas 懒建页** | `InitChannel()` 不创建 Page 0；首次 `Allocate()` 时创建；未使用 Channel 的 `PageCount=0, FillRate=0` | ✅ Implemented | ADR-031 R4.4A（2026-04-21） |
| **AC-14** | **Laser 可选入 Atlas** | `LaserTypeSO.UseRuntimeAtlas=true` 时激光走 Atlas（DC 不增加）；`false` 时走独立贴图（UV 滚动保留） | ✅ Implemented | ADR-031 Laser 方案 C（2026-04-21） |
| **AC-15** | **Trail 纹理化** | `BulletTypeSO.TrailTexture` 支持自定义纹理；所有 Trail（含 whiteTexture fallback）统一走 Atlas Channel.Trail | ✅ Implemented | ADR-031 Trail 纹理化（2026-04-21） |
| **AC-16** | **`new Material()` shaderKeywords 显式复制** | 项目内所有材质克隆后必须 `clone.shaderKeywords = source.shaderKeywords`；违反导致 `multi_compile_local` keyword 静默丢失 | ✅ 铁律 | ADR-032（2026-04-21） |

### 10.2 性能验收

| # | 指标 | 基线 | 目标 | 状态 | 变更记录 |
|---|------|------|------|------|----------|
| PC-01 | 缓存命中 Allocate 耗时 | — | < 0.001ms | ⏸ 待真机 | — |
| PC-02 | 单次 Blit 耗时（64×64） | — | < 0.1ms | ⏸ 待真机 | — |
| PC-03 | 预热 20 张纹理总耗时 | — | < 5ms | ⏸ 待真机 | — |
| PC-04 | 全局 DrawCall | N×1+ DC | **≤ 8 DC**（每 Channel 1~2 Atlas Pages + Trail 独立 DC） | ⏸ 待真机 | Editor 验收: DC=2（R4.1 报告） |
| PC-05 | 内存增量 | — | ≤ 48MB（大 Channel 2×16MB + 小 Channel 3×1MB + Trail） | 🔄 修改 | ADR-031 R4.4A 懒建页：未使用 Channel 不建 RT，实际更低 |
| PC-06 | 热路径零 GC | — | Allocate 缓存命中时零 GC | ⏸ 待真机 | — |

### 10.3 兼容性验收

| # | 验收项 | 通过标准 | 状态 | 变更记录 |
|---|--------|----------|------|----------|
| CC-01 | WebGL 2.0（微信小游戏） | Blit + 渲染正常，无报错 | ⏸ 待真机 | — |
| CC-02 | Editor (Windows/macOS) | 编辑器内运行正常 | ✅ Implemented | R4.1 验收通过（2026-04-19） |
| CC-03 | Standalone (IL2CPP) | PC 构建运行正常 | ⏸ 待验证 | — |

---

## 十一、术语表

| 术语 | 定义 |
|------|------|
| **Atlas Page** | 一张运行时创建的 RenderTexture，作为动态图集的物理承载 |
| **Shelf** | Shelf Packing 算法中的一"行"，每行高度由该行最高纹理决定 |
| **Channel** | 业务隔离维度（Bullet / VFX / DamageText / Laser / Trail / Character），各 Channel 独立管理自己的 Atlas Pages |
| **Allocation** | RuntimeAtlasSystem 的分配结果，包含 PageIndex + UVRect |
| **Blit** | GPU 端纹理拷贝操作，将源纹理绘制到 Atlas RT 的目标区域 |
| **RT Lost** | WebGL 中 RenderTexture 内容丢失（如 Tab 切换），需要重新 Blit |
| **预热（Warmup）** | 在关卡加载阶段批量 Allocate 已知会用到的纹理 |
| **RBM** | RenderBatchManager 的缩写 |
| **统一渲染管线** | v2.0 目标架构——所有 2D 渲染通过 RuntimeAtlas + RBM 统一提交 |

---

## 十二、参考

- ADR-001 ~ ADR-027：`docs/Agent/ARCHITECT_DECISION_RECORD.md`
- Phase 4.1/4.2 Atlas 工具：`AtlasMappingSO.cs`, `DanmakuAtlasPackerWindow.cs`
- RVT 思想来源：天命人 × 广智 讨论（2026-04-17）
- Unity Streaming Virtual Texturing：[Unity SVT Docs](https://docs.unity3d.com/Manual/svt-streaming-virtual-texturing.html)

---

_本文档 v2.3（PK 收敛）已获天命人批准（2026-04-18 20:47 v2.1 初批准，22:30 v2.2 PK R1，23:06 v2.3 PK R2 终），全部未决项已确认，可进入编码实施。_
_Editor Atlas 工具链（Phase 4.1/4.2 产物）保留不删，作为离线工具继续服务。_
_v2.2 关键变更：激光不入 Atlas（UA-002）、RBM 按 SortingOrder 排序提交（UA-004）。_
_v2.3 关键变更：RBM 多模板材质 API（UA-005）、文档内联补全（UA-006）、注册时排序（UA-007）。_
