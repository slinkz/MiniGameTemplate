# RuntimeAtlasSystem 技术设计文档（TDD）

> 文档版本：v2.10.1
> 创建日期：2026-04-18
> 修订日期：2026-04-19（v2.10.1 — R4.1/R4.3 评审收口）
> 作者：广智 × 天命人
> 状态：**R0~R5 已验收通过，R4.1/R4.3 已完成** — R4.2/R4.4/R4.5 待按需执行

---

## 〇、版本变更记录

| 版本 | 日期 | 变更内容 |
|------|------|---------|
| v1.0 | 2026-04-18 | 初始设计（RuntimeAtlasSystem 作为可选优化层） |
| **v2.0** | **2026-04-18** | **重大定位调整**：从"可选优化"升级为"系统级重构"。新增：统一渲染管线设计、6 条渲染路径迁移计划、Editor Atlas 保留策略。删除：ADR-007/008/010 的"可选优化"约束（不再适用）。 |
| **v2.1** | **2026-04-18** | **全部 12 个未决项（UD-01~12）已确认**。适配 ADR-029 v2（移除 Additive），修正验收标准 AC-01/AC-02。文档状态从"设计评审中"转为"已批准"。 |
| **v2.2** | **2026-04-18** | **PK Round 1 回应**：修正 BucketKey 描述矛盾（UA-001）、激光改为独立贴图不入 Atlas（UA-002）、补充分帧重建策略（UA-003）、RBM 按 SortingOrder 排序提交（UA-004） |
| **v2.3** | **2026-04-18** | **PK Round 2 回应（终）**：RBM.Initialize 改为多模板材质 API（UA-005）、§3.3/§3.4/§7.1/§7.3 内联补全（UA-006）、排序策略明确为注册时排序（UA-007） |
| **v2.4** | **2026-04-19** | **Phase R0 落地**：完成 R0.2/R0.4/R0.5，补齐 RuntimeAtlas 基础设施代码；R0.3 算法实现完成，单元测试因项目缺少现成测试基础设施延后补齐。另提前落地 R2.1 的 RBM v2.3 接口改造（Texture 基类、多模板材质、注册时排序），避免后续迁移返工。 |
| **v2.5** | **2026-04-19** | **Phase R1 落地**：`RuntimeAtlasManager` 从可编译骨架补齐为配置驱动核心管理器；新增 `Initialize(RuntimeAtlasConfig)`、`TryGetAllocation()`、`GetPageCount()`、`RestoreDirtyPages()` 分批恢复能力；`RuntimeAtlasConfig` 增加统一 `Validate()`；`RuntimeAtlasStats` 扩展为请求数 / 命中率 / overflow / pending restore 统计。 |
| **v2.6** | **2026-04-19** | **Phase R2 落地**：新增 `RuntimeAtlasBindingResolver` 统一 `SourceTexture / AtlasBinding / RuntimeAtlas` 三路解析；`BulletRenderer` 与 `VFXBatchRenderer` 已优先接入 `RuntimeAtlas`，Laser / LaserWarning 保持独立贴图但继续走统一 RBM；`DanmakuRenderConfig` / `VFXRenderConfig` 新增 `RuntimeAtlasConfig` 配置入口。 |
| **v2.7** | **2026-04-19** | **Phase R3 落地**：`DamageNumberSystem` 已迁移到 `RenderBatchManager + RuntimeAtlas(DamageText)`，数字 UV 改为基于 Atlas 子区间重映射；`TrailPool` 采用方案 A，保持独立 Mesh 但已接入 `RenderBatchManagerRuntimeStats`；`DanmakuSystem.RunLateUpdatePipeline()` 已切换到新的 `DamageNumberSystem.Rebuild(dt)` 统一提交流程。`SpriteSheetVFXSystem` 在提交层面已通过 `VFXBatchRenderer` 统一到 RBM，但编排层面仍保持独立 `LateUpdate`，该边界在 R3 文档中显式保留。 |
| **v2.8** | **2026-04-19** | **Phase R4.0 落地**：VFX 编排层统一——`SpriteSheetVFXSystem` 的 `Update()/LateUpdate()` 已删除，新增 `TickVFX()/RenderVFX()` 供 DanmakuSystem 管线调用；`IDanmakuVFXRuntime` 扩展 `TickVFX/RenderVFX` 方法；`DanmakuVFXRuntimeBridge` 实现转发；`RunUpdatePipeline` 步骤 6 调 VFX Tick，`RunLateUpdatePipeline` 在 BeginFrame/EndFrame 区间内调 VFX Render。修复 TimeScale 双重缩放问题（`TickVFX` 接收已缩放的 dt，不再内部二次乘 `_timeScale`）。
| **v2.8.1** | **2026-04-19** | **R4.0 回归修复**：补齐 Detached Spray 的 VFX 启动路径。此前 `SprayUpdater` 仅在 `FollowTarget + AttachId!=0` 时调用 `PlayAttached`，导致 Demo 中 `J`（Attached Spray）可见而 `K`（Detached Spray）不可见。现 `IDanmakuVFXRuntime` 新增世界空间 `Play(...)` 接口，`DanmakuVFXRuntimeBridge` 实现转发，`SprayUpdater` 按 AttachMode 分流：Attached 走 `PlayAttached`，Detached/World 走 `Play`。 |
| **v2.9** | **2026-04-19** | **Phase R5 文档更新完成**：RuntimeAtlasSystem.MODULE_README.md 全面重写（补 R0~R4 落地记录 + 后续可选优化表）；ARCHITECTURE.md 新增"统一渲染管线"章节（渲染架构图 + renderQueue 层序表 + 每帧管线调度 + 关键决策摘要）+ 模块依赖表补充 Rendering/VFXSystem 层级；Rendering/MODULE_README.md 更新 RBM 核心类型为 BucketRegistration 多模板材质 API；DanmakuSystem/MODULE_README.md 大幅更新（管线描述同步 R4 10 步 Update + 统一 LateUpdate、重构进度补齐 R0~R4、Shader 目录修正 ADR-029 移除 Additive、新增 VFX 桥接文件）；VFXSystem/MODULE_README.md 补充 RuntimeAtlas 集成说明。 |
| **v2.10** | **2026-04-19** | **R4.1/R4.3 落地**：R4.1 代码审查确认 BulletRenderer/VFXBatchRenderer/DamageNumberSystem 已正确接入 RuntimeAtlas（Laser/Trail 按 UA-002 设计不入 Atlas）；分布式 RBM 架构功能正确（全局单 RBM 统一为 P2 优化）。R4.3 DanmakuDebugHUD 扩展 RuntimeAtlas 统计：每个子系统暴露 `GetAtlasStats()` 方法，`DanmakuSystem.API.cs` 新增 `GetAllAtlasStats()` 聚合接口，HUD 新增 Atlas section（页数/分配数/填充率/内存/命中率/overflow），0.5s 刷新间隔避免 GC。`IDanmakuVFXRuntime` 接口扩展 `GetAtlasStats()` 方法，通过 `DanmakuVFXRuntimeBridge` 透传到 `SpriteSheetVFXSystem`。 |
| **v2.10.1** | **2026-04-19** | **评审收口**：软件架构师复核确认本轮实现未偏离 TDD，仅对 `DanmakuDebugHUD` 做两处收口修正——(1) `Start()` 首帧主动刷新 `_atlasStatsCache`，避免进入 Play / 首次显示 HUD 时 Atlas section 需要等待 0.5s 才出现；(2) 统一 Atlas section 的显示条件与行数计算逻辑，仅在存在至少 1 条有效 stats 时显示 section header，避免 HUD 高度计算与实际显示分叉。随后代码评审复核通过，Unity MCP 编译检查 0 errors。 |

### v2.0 核心变更（天命人反馈驱动）

1. **RuntimeAtlasSystem 不是可选优化项，而是要替代当前割裂的渲染系统** — 这是一次系统级重构
2. **搭建后需要将之前的渲染功能迁移到新系统之上** — 迁移工作量纳入开发计划
3. **原来打包 Atlas 的功能要保留** — Editor Atlas 工具链不删除，作为离线预览/资产管理工具继续存在

---

## 一、问题陈述

### 1.1 当前渲染系统的"割裂"问题

当前项目存在 **6 条独立的渲染路径**，各自为政：

| # | 渲染器 | 所属系统 | 是否使用 RenderBatchManager | 纹理管理方式 | Mesh 管理 |
|---|--------|---------|---------------------------|------------|----------|
| 1 | **BulletRenderer** | Danmaku | ✅ | TypeSO.GetResolvedTexture() + fallback | 共享 RBM |
| 2 | **LaserRenderer** | Danmaku | ✅ | LaserTypeSO.LaserTexture 直接引用 | 共享 RBM |
| 3 | **LaserWarningRenderer** | Danmaku | ✅ | 同 LaserRenderer | 共享 RBM |
| 4 | **DamageNumberSystem** | Danmaku | ❌ 自管 Mesh | DanmakuRenderConfig.NumberAtlas 独立材质 | 自管 Mesh + 自管材质 |
| 5 | **TrailPool** | Danmaku | ❌ 自管 Mesh | Texture2D.whiteTexture（纯顶点色） | 自管 Mesh + 自管材质 |
| 6 | **VFXBatchRenderer** | VFX | ✅ | TypeSO.GetResolvedTexture() + fallback | 独立 RBM 实例 |

**割裂体现在四个维度：**

#### A. 纹理管理各自为政
- BulletRenderer 和 VFXBatchRenderer：TypeSO → AtlasBinding → SourceTexture → fallback，三级解析链
- LaserRenderer：直接引用 `LaserTypeSO.LaserTexture`，无 Atlas 解析
- DamageNumberSystem：独立绑定 `NumberAtlas`，用 `BulletMaterial` 克隆材质
- TrailPool：固定使用 `Texture2D.whiteTexture`

#### B. 初始化协议不统一
- 使用 RBM 的渲染器（1/2/3/6）：各自收集 BucketKey → 各自调用 `RBM.Initialize()`
- DamageNumberSystem（4）：自己创建 Mesh + Material，预填索引
- TrailPool（5）：自己创建 Mesh + Material，用 `_material.mainTexture = Texture2D.whiteTexture`

#### C. 渲染提交路径分散
- `DanmakuSystem.RunLateUpdatePipeline()` 中的渲染调用链：
  ```
  RenderBatchManagerRuntimeStats.BeginFrame();
  _bulletRenderer.Rebuild();          // → RBM.UploadAndDrawAll()
  _laserRenderer.Rebuild();           // → RBM.UploadAndDrawAll()
  _laserWarningRenderer.Rebuild();    // → RBM.UploadAndDrawAll()
  _damageNumbers.UpdateAndRender();   // → 自管 Graphics.DrawMesh()
  _trailPool.Render();                // → 自管 Graphics.DrawMesh()
  RenderBatchManagerRuntimeStats.EndFrame();
  ```
- VFX 系统在 `SpriteSheetVFXSystem.LateUpdate()` 中独立渲染，不经过 DanmakuSystem

#### D. 统计和调试各自为政
- DamageNumberSystem 和 TrailPool 的 DrawCall 不计入 `RenderBatchManagerRuntimeStats`
- 无法从单一入口获得"整个游戏画面的 DC 全貌"

### 1.2 现状痛点（优先级排序）

| # | 痛点 | 影响 | 严重度 |
|---|------|------|--------|
| P1 | **DrawCall 线性增长** | N 种贴图 × 2 层 = 2N DC，随内容增长不可控 | 🔴 高 |
| P2 | **渲染系统割裂** | 6 条独立路径，维护成本高，新增渲染类型要重复造轮子 | 🔴 高 |
| P3 | **构建耦合** | Editor Atlas 改贴图 → 重建 → 重打包 | 🟡 中 |
| P4 | **冗余加载** | 静态 Atlas 包含全部贴图，单关卡只用一部分 | 🟡 中 |
| P5 | **新增类型无归一化路径** | 想加飘字类型/角色序列帧/弹幕预警等，没有统一的渲染接入方式 | 🟡 中 |

### 1.3 核心思路

借鉴 **Runtime Virtual Texture（RVT）** 的"按需生成"思想：

> 构建一个**统一的渲染管线**，以 RuntimeAtlasSystem 为纹理合并核心，所有视觉元素（子弹、激光、特效、飘字、拖尾）通过统一协议接入，彻底消除割裂。

### 1.4 系统定位（v2.0 修正）

> **RuntimeAtlasSystem 不是可选优化项，而是新渲染管线的核心基础设施。**
> 它将替代当前割裂的多路径渲染架构，成为所有 2D Quad 型渲染的统一出口。

```
┌───────────────────────────────────────────────────────────────┐
│                     统一渲染管线（目标架构）                      │
│                                                               │
│   BulletRenderer  LaserRenderer  VFXRenderer  DmgText  Trail  │
│        │              │              │           │       │     │
│        └──────┬───────┴──────┬───────┴─────┬─────┘       │     │
│               ▼              ▼             ▼             ▼     │
│            统一纹理解析层（RuntimeAtlasSystem）                  │
│               │                                               │
│               ▼                                               │
│            RenderBatchManager（统一分桶 + 提交）                │
│               │                                               │
│               ▼                                               │
│            Graphics.DrawMesh（统一 DC 提交）                   │
│               │                                               │
│               ▼                                               │
│            RenderBatchManagerRuntimeStats（统一统计）           │
│                                                               │
│         资源加载层                                              │
│           └─ YooAsset (加载源 Texture2D)                       │
└───────────────────────────────────────────────────────────────┘
```

对比当前架构：
```
当前（6 条割裂路径）：
  BulletRenderer ──┐
  LaserRenderer  ──┤── 各自持有 RBM 实例 ──→ Graphics.DrawMesh
  LaserWarning   ──┘
  DamageNumber   ────── 自管 Mesh ──────────→ Graphics.DrawMesh  （不统计）
  TrailPool      ────── 自管 Mesh ──────────→ Graphics.DrawMesh  （不统计）
  VFXBatchRenderer ── 独立 RBM ────────────→ Graphics.DrawMesh  （独立统计）
```

---

## 二、需求与约束

### 2.1 功能需求

| ID | 需求 | 优先级 |
|----|------|--------|
| FR-01 | 运行时按需将 Texture2D Blit 到 Atlas RenderTexture 上 | P0 |
| FR-02 | 支持混合尺寸纹理（32×32 ~ 256×256），不要求统一 Cell 尺寸 | P0 |
| FR-03 | 同一张源纹理只 Blit 一次，后续直接返回缓存的 UV Rect | P0 |
| FR-04 | 单张 Atlas 放不下时自动创建新 AtlasPage | P0 |
| FR-05 | 业务消费者通过 Channel 隔离（Bullet / VFX / DamageText / Laser / Trail） | P0 |
| FR-06 | 切关时统一 Reset，释放所有 Atlas RT | P0 |
| FR-07 | 关卡加载时可预热（批量 Allocate），减少战斗中首帧卡顿 | P1 |
| FR-08 | **统一渲染管线**：所有消费者通过 RBM 提交，统一 DC 统计 | P0 |
| FR-09 | **迁移兼容**：DamageNumberSystem / TrailPool 迁移到 RBM 后行为一致 | P0 |
| FR-10 | **Editor Atlas 保留**：原有 Editor Atlas 工具链保留不删，可继续使用 | P0 |
| FR-11 | Editor 预览工具：可视化 Atlas 占用情况 | P2 |

### 2.2 非功能约束

| 约束 | 说明 |
|------|------|
| **平台** | 微信小游戏 WebGL，需兼容 WebGL 2.0 |
| **API 限制** | `Graphics.CopyTexture()` 在 WebGL 上不可靠，必须用 `CommandBuffer.Blit()` |
| **RT 格式** | `RenderTextureFormat.ARGB32`，最安全 |
| **Atlas 尺寸** | 默认 2048×2048，WebGL 最大支持 4096×4096 |
| **内存预算** | 单张 2048×2048 ARGB32 = 16MB；全部 Channel 合计 ≤ 64MB |
| **Padding** | 至少 1px，防止线性采样纹理出血 |
| **零 GC** | 热路径（Allocate 命中缓存）不允许 GC Alloc |
| **不引入 Addressables** | 项目已有 YooAsset，源纹理加载/卸载走 YooAsset |

### 2.3 架构约束（来自已有 ADR — v2.0 修订）

| ADR | 约束 | RuntimeAtlasSystem 的应对 |
|-----|------|--------------------------|
| ADR-002 | 共享实现不共享实例 | RuntimeAtlasSystem 是共享实现；各消费者通过 Channel 隔离 |
| ~~ADR-007/008~~ | ~~资源自由优先，Atlas 仅为可选优化~~ | **v2.0 取消此约束**：RuntimeAtlasSystem 是必选项，不是可选优化层 |
| ~~ADR-010~~ | ~~Atlas 不是生产前置条件~~ | **v2.0 取消此约束**：RuntimeAtlasSystem 是渲染前置条件 |
| ADR-015 | 初始化预热桶，运行时禁止隐式建桶 | **核心变化**：BucketKey 的 Texture 从独立贴图变为 Atlas RT |
| ADR-017 | 桶预热 | 预热时 Key 由 RuntimeAtlasSystem 提供的 Atlas RT 替代 |
| ADR-019 | Atlas 为可逆派生产物 | **修正**：Editor Atlas 保留可逆性；Runtime Atlas 是运行时必要基础设施 |

> ~~⚠️ 未决项 UD-08~~：✅ **已确认**。ADR-007/008/010 正式标记为 Superseded by ADR-028（运行时约束）。Editor 环境中原决策仍然生效。

---

## 三、架构设计

### 3.0 核心设计原则（Channel 隔离策略 — UD-08 深度分析结论 + ADR-029 v2 更新）

> **Atlas 层只做 Channel（业务类型）隔离。**
>
> ~~Layer 维度由下游 RenderBatchManager 的 `BucketKey = (RenderLayer, AtlasRT)` 处理。~~
> **ADR-029 v2 后，RenderLayer 只剩 Normal 一个值。BucketKey 在代码层面仍保留 `(RenderLayer, Texture)` 结构体以保持兼容，但运行时等价于纯 Texture 分桶。(v2.2 修正：明确代码现状与逻辑语义的关系，消除 UA-001 矛盾)**
>
> ```
> Atlas 层  → Channel 隔离（内存预算 / 配置差异化 / 故障隔离 / 可观测性）
> RBM 层   → 只按 Texture 分桶（Blend 统一 Normal / Alpha Blend）
> ```
>
> **ADR-029 v2 决策**：彻底删除 Additive Blend 代码、Shader、配置。
> 原因：弹幕游戏 90%+ 场景不需要 Additive，密集叠加会过曝，且 Blend 模式不应暴露给策划。
> 如果将来需要 Additive，走新 ADR 流程重新加——而不是"取消隐藏"。
>
> **Channel 枚举何时不够用？**
> 当 Channel 数量超过 8 个或需要运行时动态注册时，可演进为 `AtlasChannelRegistry` 动态注册制。
> 当前项目阶段，硬编码枚举是正确的简化。

### 3.1 核心组件

```
RuntimeAtlasSystem（系统级重构）
│
├── RuntimeAtlasManager           ← 全局入口（非 Singleton，由初始化器持有）
│   ├── Dictionary<AtlasChannel, AtlasChannelState>
│   └── API: Allocate / GetAtlasTexture / GetMaterial / Reset / GetStats
│
├── AtlasChannelState             ← 单 Channel 的状态
│   ├── List<AtlasPage>           ← 该 Channel 的所有 Atlas 页
│   ├── Dictionary<int, AtlasAllocation>  ← 缓存（InstanceID → Allocation）
│   └── AtlasChannelConfig        ← 该 Channel 的配置（尺寸、padding 等）
│
├── AtlasPage                     ← 单张 Atlas RT 的状态
│   ├── RenderTexture Texture
│   ├── List<Shelf> Shelves       ← Shelf Packing 行列表
│   └── int NextShelfY
│
├── ShelfPacker                   ← Shelf Packing 算法（Best-Fit Shelf）
│   └── bool TryAllocate(page, w, h, padding, out pixelRect)
│
├── AtlasBlit                     ← Blit 适配层（WebGL 兼容）
│   └── void Blit(source, atlasRT, destRect)
│
└── UnifiedRenderPipeline         ← 统一渲染调度（新增）
    ├── RegisterRenderer(IQuadRenderer)
    ├── RenderAll()                ← 统一入口
    └── GetGlobalStats()           ← 全局 DC 统计
```

### 3.2 数据结构

```csharp
/// <summary>
/// Atlas 通道枚举——不同业务域的 Atlas 物理隔离
/// v2.0：新增 Laser / Trail / DamageText 通道
/// </summary>
public enum AtlasChannel : byte
{
    Bullet = 0,       // 子弹（数量最多，合批收益最大）
    VFX = 1,          // 特效帧
    DamageText = 2,   // 飘字数字精灵
    Laser = 3,        // 激光纹理
    Trail = 4,        // 拖尾（如果需要纹理化拖尾）
    Character = 5,    // 角色序列帧（预留）
}

/// <summary>
/// 分配结果——业务层拿到这个就够了
/// </summary>
public readonly struct AtlasAllocation
{
    public readonly int PageIndex;      // 第几张 Atlas Page
    public readonly Rect UVRect;        // 归一化 UV 区域
    public readonly bool Valid;         // 分配是否成功

    public AtlasAllocation(int pageIndex, Rect uvRect)
    {
        PageIndex = pageIndex;
        UVRect = uvRect;
        Valid = true;
    }

    public static readonly AtlasAllocation Invalid = default;
}

/// <summary>
/// Shelf Packing 中的一"行"
/// </summary>
internal struct Shelf
{
    public int Y;          // 该行在 Atlas 中的 Y 起点（像素）
    public int Height;     // 该行高度（含 padding）
    public int UsedWidth;  // 该行已使用的宽度（像素，含 padding）
}

/// <summary>
/// 单张 Atlas 页面的状态
/// </summary>
internal class AtlasPage : System.IDisposable
{
    public RenderTexture Texture;
    public List<Shelf> Shelves;
    public int NextShelfY;

    public void Dispose()
    {
        if (Texture != null)
        {
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }
        Shelves?.Clear();
    }
}

/// <summary>
/// 单个 Channel 的配置
/// </summary>
[System.Serializable]
public struct AtlasChannelConfig
{
    [Tooltip("Atlas 页面尺寸（像素，正方形）")]
    public int AtlasSize;

    [Tooltip("子图之间的 Padding（像素）")]
    public int Padding;

    [Tooltip("最大页面数（超过则拒绝分配并报警告）")]
    public int MaxPages;

    public static AtlasChannelConfig Default => new AtlasChannelConfig
    {
        AtlasSize = 2048,
        Padding = 1,
        MaxPages = 4,
    };

    /// <summary>小型 Channel 配置（DamageText / Laser / Trail）</summary>
    public static AtlasChannelConfig Small => new AtlasChannelConfig
    {
        AtlasSize = 1024,
        Padding = 1,
        MaxPages = 1,
    };
}
```

### 3.3 核心算法：Shelf Packing（Best-Fit Shelf）

**(v2.3 内联补全——原 v1.0 §3.3 内容)**

算法概述：将 Atlas 页面视为从上到下的一组"行（Shelf）"，每行高度由该行中最高的纹理决定。

```
TryAllocate(page, width, height, padding):
  paddedW = width + padding * 2
  paddedH = height + padding * 2

  // 1. 在已有 Shelf 中找 Best-Fit（剩余宽度最小但足够放下的行）
  bestShelf = null
  bestWaste = MAX_INT
  for each shelf in page.Shelves:
    if shelf.Height >= paddedH AND (atlasSize - shelf.UsedWidth) >= paddedW:
      waste = shelf.Height - paddedH  // 高度浪费
      if waste < bestWaste:
        bestWaste = waste
        bestShelf = shelf

  // 2. 找到合适行 → 直接放入
  if bestShelf != null:
    x = bestShelf.UsedWidth + padding
    y = bestShelf.Y + padding
    bestShelf.UsedWidth += paddedW
    return Rect(x, y, width, height)

  // 3. 没有合适行 → 尝试新建行
  if page.NextShelfY + paddedH <= atlasSize:
    newShelf = Shelf(Y=page.NextShelfY, Height=paddedH, UsedWidth=paddedW)
    page.Shelves.Add(newShelf)
    x = padding
    y = page.NextShelfY + padding
    page.NextShelfY += paddedH
    return Rect(x, y, width, height)

  // 4. 本页放不下
  return null
```

**关键特性**：零 GC（纯值类型运算）、O(N) 搜索（N = Shelf 数，通常 < 20）、Best-Fit 策略减少高度浪费。

### 3.4 Blit 策略（WebGL 兼容）

**(v2.3 内联补全——原 v1.0 §3.4 内容)**

```csharp
/// <summary>
/// 将源纹理 Blit 到 Atlas RT 的指定像素区域。
/// 使用 CommandBuffer + 全屏 Quad 方式，兼容 WebGL 2.0。
/// </summary>
public static class AtlasBlit
{
    private static Material _blitMat;  // Hidden/RuntimeAtlasBlit Shader
    private static CommandBuffer _cmd;

    public static void Blit(Texture source, RenderTexture atlasRT, Rect destPixelRect)
    {
        EnsureResources();

        // 计算目标 UV Rect（归一化）
        float atlasW = atlasRT.width;
        float atlasH = atlasRT.height;
        Rect viewport = new Rect(
            destPixelRect.x / atlasW,
            destPixelRect.y / atlasH,
            destPixelRect.width / atlasW,
            destPixelRect.height / atlasH);

        _cmd.Clear();
        _cmd.SetRenderTarget(atlasRT);
        _cmd.SetViewport(new Rect(destPixelRect.x, destPixelRect.y,
                                   destPixelRect.width, destPixelRect.height));
        _cmd.DrawMesh(fullscreenQuad, Matrix4x4.identity, _blitMat, 0, 0);
        Graphics.ExecuteCommandBuffer(_cmd);
    }
}
```

**关键约束**：
- 不使用 `Graphics.CopyTexture()`（WebGL 不可靠）
- `Hidden/RuntimeAtlasBlit` Shader 仅执行 `tex2D(_MainTex, uv)` 直通拷贝
- 每次 Blit 设置 `SetViewport` 限制写入区域，避免影响其他子图

### 3.5 集成方案：与 RenderBatchManager 的对接

**核心变化（v2.0 增强 + v2.3 修正）**：

1. `BucketKey.Texture` 从 `Texture2D` 拓宽为 `Texture`（基类），以同时支持独立 `Texture2D` 和 `RenderTexture`
2. **所有渲染消费者统一通过 RBM 提交**，包括之前自管 Mesh 的 DamageNumberSystem 和 TrailPool
3. **(v2.3 新增) `RBM.Initialize()` API 改造为多模板材质**：从 `Initialize(IReadOnlyList<BucketKey> keys, Material material, ...)` 改为 `Initialize(IReadOnlyList<BucketRegistration> registrations, ...)`，其中 `BucketRegistration = (BucketKey key, Material templateMat, int sortingOrder)`。每个桶独立绑定自己的模板材质和排序值。

> **v2.3 新增：多模板材质设计理由（UA-005 回应）**
>
> 当前系统存在至少两种不同的 Shader/Blend 模式：
> - `DanmakuBullet.shader`：`Blend SrcAlpha OneMinusSrcAlpha`（Alpha Blend），用于子弹/VFX/飘字
> - `DanmakuLaser.shader`：`Blend SrcAlpha One`（Additive），用于激光（含 CoreColor/GlowColor 参数）
>
> 全局单 RBM 必须能在同一实例内为不同桶绑定不同模板材质，否则激光桶和子弹桶无法共存。
> 注意：ADR-029 v2 移除的是"暴露给策划的 Additive Layer 选项"，Laser Shader 自身的硬编码 Additive Blend 不受影响。

> **⚠️ 未决项 UD-01**：BucketKey 类型变更的影响面评估。推荐方案 A（改基类类型），需确认。

#### 统一后的初始化流程

```
v2.0 统一渲染管线初始化（v2.3 修订）：
  1. RuntimeAtlasManager 创建
  2. 各 Channel 预热：
     - Bullet: 遍历 BulletTypeSO → Allocate → 收集 (Normal, AtlasRT) BucketKey + BulletMaterial
     - Laser:  遍历 LaserTypeSO → 不入 Atlas，直接以独立贴图注册 (Normal, LaserTexture) BucketKey + LaserMaterial  // (v2.2+v2.3)
     - VFX:    遍历 VFXTypeSO → Allocate → 收集 (Normal, AtlasRT) BucketKey + BulletMaterial
     - DmgText: 将 NumberAtlas Allocate → 收集 BucketKey + BulletMaterial
     - Trail:  特殊处理（纯顶点色 → whiteTexture 桶 + BulletMaterial）
  3. 组装 BucketRegistration[] 列表（key + templateMat + sortingOrder）
  4. 统一 RBM.Initialize(registrations)  // (v2.3：多模板材质 API)
  5. 各 Renderer 拿到统一 RBM 引用
```

### 3.6 生命周期

```
┌─────────────────────────────────────────────────────┐
│ 关卡加载                                             │
│   1. RuntimeAtlasManager 创建（或重用）               │
│   2. 预热：批量 Allocate 本关卡所有已知贴图            │
│   3. 统一 RBM 桶预热                                 │
│                                                     │
│ 战斗中                                               │
│   4. 新贴图首次出现                                   │
│      → Allocate（缓存命中：O(1)，无 GC）              │
│      → 未命中：Blit 到 Atlas（一次性开销）             │
│   5. 统一渲染提交：所有 Renderer → RBM → DrawMesh     │
│   6. 统一统计：RenderBatchManagerRuntimeStats          │
│                                                     │
│ 关卡结束                                             │
│   7. RuntimeAtlasManager.Reset()                     │
│      → Release 所有 RT                               │
│      → Clear 缓存                                    │
│      → RBM Dispose + 重建                            │
└─────────────────────────────────────────────────────┘
```

---

## 四、与现有系统的关系

### 4.1 与 Editor Atlas 工具链的关系（v2.0 重要修正）

**Editor Atlas 工具链保留，不删除。**

| 维度 | Editor Atlas (Phase 4.1/4.2) | RuntimeAtlasSystem |
|------|------------------------------|---------------------|
| 执行时机 | 编辑器手动操作 | 运行时自动 |
| 产物 | `AtlasMappingSO` + `Texture2D` (资产) | `RenderTexture` (运行时临时) |
| 持久化 | 是（持久化到项目中） | 否（切关即销毁） |
| **地位** | 离线工具——预览、资产管理、导出验证 | **运行时渲染核心基础设施** |
| **删除风险** | **不删除** | — |

保留 Editor Atlas 的理由：
1. **资产预览**：策划在 Inspector 中需要看到"打包后的效果"
2. **导出验证**：可以对比 Editor Atlas 和 Runtime Atlas 的结果是否一致
3. **兼容回退**：极端情况下可降级为静态 Atlas 方案
4. **零成本保留**：不删代码 = 零风险

**共存策略变化（v2.0 vs v1.0）：**

| 场景 | v1.0 行为 | v2.0 行为 |
|------|----------|----------|
| TypeSO 有 AtlasBinding | RuntimeAtlas 跳过，用 Editor Atlas | **RuntimeAtlas 接管**，忽略 AtlasBinding（运行时统一走 RuntimeAtlas） |
| TypeSO 无 AtlasBinding | RuntimeAtlas 接管 | RuntimeAtlas 接管 |
| Editor 预览模式 | — | Editor Atlas 仍可用于 Inspector 预览 |

> **⚠️ 未决项 UD-09**：运行时是否完全忽略 AtlasBinding（v2.0 推荐），还是保留"如果有 AtlasBinding 就跳过 RuntimeAtlas"的兼容路径？
>
> **推荐**：运行时完全由 RuntimeAtlasSystem 接管，AtlasBinding 仅用于 Editor 预览。理由：统一管线不应有两条纹理解析路径。

### 4.2 纹理解析链变更

当前解析优先级（v1.0 / 旧系统）：
```
AtlasBinding.AtlasTexture > SourceTexture > Renderer fallback
```

v2.0 新链：
```
RuntimeAtlas(SourceTexture) > SourceTexture(fallback，仅 Atlas 分配失败时)
```

AtlasBinding 在运行时不再参与解析链，仅在 Editor 预览中使用。

### 4.3 与 ADR-015/017 的关系

（与 v1.0 一致。ADR-015 的扩展点：Atlas 溢出时受控建桶。）

---

## 五、迁移计划 — 6 条渲染路径统一

### 5.1 迁移总览

| # | 渲染器 | 当前状态 | 迁移目标 | 迁移难度 | 关键变更 |
|---|--------|---------|---------|---------|---------|
| 1 | BulletRenderer | 已用 RBM | 改用 RuntimeAtlas 纹理 | 🟢 低 | GetResolvedTexture → RuntimeAtlas |
| 2 | LaserRenderer | 已用 RBM | 统一到全局 RBM（纹理保持独立） | 🟡 中 | 不入 Atlas，独立贴图注册统一 RBM（v2.2 修正） |
| 3 | LaserWarningRenderer | 已用 RBM | 统一到全局 RBM（纹理保持独立） | 🟢 低 | 同 LaserRenderer（v2.2） |
| 4 | **DamageNumberSystem** | **自管 Mesh** | **迁移到 RBM** | 🟡 中 | 拆出渲染逻辑，改用 RBM.WriteQuad |
| 5 | **TrailPool** | **自管 Mesh** | **迁移到 RBM** | 🟡 中 | TriangleStrip → Quad 化，或保持独立但接入统计 |
| 6 | VFXBatchRenderer | 独立 RBM 实例 | 改用统一 RBM + RuntimeAtlas | 🟢 低 | 同 BulletRenderer |

### 5.2 BulletRenderer 迁移（难度：🟢 低）

**当前逻辑**：
```csharp
// Initialize: 遍历 BulletTypeSO → bt.GetResolvedTexture() → 收集 BucketKey
// Rebuild:    bt.GetResolvedTexture() → _batchManager.TryGetBucket(key) → WriteQuad
```

**迁移后**：
```csharp
// Initialize: 遍历 BulletTypeSO → runtimeAtlas.Allocate(channel, bt.SourceTexture)
//             → 收集 (Layer, AtlasRT) BucketKey
// Rebuild:    runtimeAtlas.GetAllocation(bt.SourceTexture) → 用 allocation.UVRect 替换 baseUV
//             → _batchManager.TryGetBucket((Layer, AtlasRT)) → WriteQuad
```

**关键变更点**：
1. `GetResolvedTexture()` 不再调用 — 纹理统一由 RuntimeAtlas 管理
2. `GetResolvedBaseUV()` 不再调用 — baseUV 由 `AtlasAllocation.UVRect` 提供
3. BucketKey 的 Texture 从 `Texture2D` 变为 `RenderTexture`（Atlas Page）
4. fallback 逻辑简化：RuntimeAtlas 分配失败 → 用原始 SourceTexture 作为 BucketKey

### 5.3 LaserRenderer / LaserWarningRenderer 迁移（难度：🟡 中 — v2.2 修正）

**当前逻辑**：
```csharp
// Initialize: 遍历 LaserTypeSO → lt.LaserTexture → 收集 BucketKey
// Rebuild:    type.LaserTexture → TryGetBucket → WriteSegmentQuad
```

**迁移后（v2.2 修正——激光不入 Atlas，保持独立贴图）**：
```csharp
// Initialize: 遍历 LaserTypeSO → 不走 RuntimeAtlas.Allocate
//             直接以 (Normal, lt.LaserTexture) 注册桶到统一 RBM
// Rebuild:    type.LaserTexture → TryGetBucket → WriteSegmentQuad（UV 逻辑不变）
```

**不入 Atlas 的技术理由（v2.2 新增）**：
1. 激光 `UV.y` 是 world-space 累积长度（`uvYAccum`），不归一化到 0→1，依赖 Shader 端 `repeat/wrap` 采样模式实现纹理滚动
2. Blit 到 Atlas 子区域后，`frac(uvYAccum)` 的 0→1 范围对应整张 Atlas 而非子区域——纹理滚动效果彻底错乱
3. 强行支持需要 Shader variant + 额外 atlas_uvRect uniform，破坏"统一材质"目标
4. 激光贴图种类极少（通常 1-3 种），独立贴图仅增加 1-3 个 DC，合批收益微乎其微

**迁移关键变更**：
1. LaserRenderer 改为使用统一 RBM（而非自建 RBM 实例），但桶的 Texture 仍为独立 `Texture2D`
2. `WriteSegmentQuad` 的 UV 逻辑完全不变
3. 激光桶与子弹/VFX 的 Atlas RT 桶共存于同一 RBM，由 `UploadAndDrawAll` 统一提交

> ~~⚠️ 未决项 UD-10~~：~~修改 WriteSegmentQuad~~。**v2.2 修正**：激光不入 Atlas，UV 映射无需修改。UD-10 不再适用。

### 5.4 DamageNumberSystem 迁移（难度：🟡 中）

**当前逻辑**：
```csharp
// 自管 Mesh + Material
// _numberAtlas 绑定到独立 Material
// WriteNumber() 直接写 _vertices 数组
// UpdateAndRender() 直接调 Graphics.DrawMesh
```

**迁移策略**：DamageNumberSystem 的 NumberAtlas Blit 到 RuntimeAtlas 的 DamageText Channel。

```csharp
// 迁移后：
// Initialize: runtimeAtlas.Allocate(DamageText, numberAtlas) → 获得 UVRect
//             但 NumberAtlas 是 10 个数字水平排列，需要在 UVRect 内再切分
// Rebuild:    通过 RBM.TryGetBucket → WriteQuad
```

**关键挑战**：
1. DamageNumberSystem 当前的 `DIGIT_UV_WIDTH = 0.1f` 假设贴图宽度的 10% = 一个数字。Blit 到 Atlas 后，这个比例要相对于 `AtlasAllocation.UVRect` 重新计算
2. DamageNumberSystem 当前持有自己的 `_mesh`、`_material`、`_indices`，迁移后这些由 RBM 管理
3. 渲染排序：当前由独立 `Graphics.DrawMesh` 的 sortingOrder 控制，迁移后需要在 RBM 中注册 `RenderSortingOrder.DamageNumber` 对应的桶

### 5.5 TrailPool 迁移（难度：🟡 中，方案待定）

**当前逻辑**：
```csharp
// TriangleStrip 展开为 TriangleList
// 使用 whiteTexture，纯靠 Vertex Color 着色
// 每条拖尾 N 个点 → 2N 顶点 → (N-1)*6 索引
```

**迁移方案 A：保持独立但接入统计**
- TrailPool 保持自管 Mesh（因为它不是 Quad 化的，是 TriangleStrip）
- 但接入 `RenderBatchManagerRuntimeStats`，贡献 DC 统计
- 最小侵入性

**迁移方案 B：Quad 化迁移到 RBM**
- 将拖尾改为 Quad 链（每段一个 Quad）
- 好处：完全统一
- 坏处：视觉质量可能下降（Quad 拐角处不连续），且 Quad 化后顶点数增加

> **⚠️ 未决项 UD-11**：TrailPool 迁移方案 A（保持独立+接入统计）还是方案 B（Quad 化）？推荐 A，因为拖尾的 TriangleStrip 拓扑与 RBM 的 Quad 拓扑不匹配。

### 5.6 VFXBatchRenderer 迁移（难度：🟢 低）

与 BulletRenderer 完全对称的迁移方式。

### 5.7 统一渲染调度（新增）

迁移完成后，`DanmakuSystem.RunLateUpdatePipeline()` 中的渲染调用变为：

```csharp
private void RunLateUpdatePipeline()
{
    float dt = Time.deltaTime * (_timeScale != null ? _timeScale.TimeScale : 1f);

    RenderBatchManagerRuntimeStats.BeginFrame();

    // 所有通过 RBM 的渲染器
    _bulletRenderer.Rebuild(...);           // → 统一 RBM
    _laserRenderer.Rebuild(...);            // → 统一 RBM
    _laserWarningRenderer.Rebuild(...);     // → 统一 RBM
    _damageNumbers.Rebuild(dt);             // → 统一 RBM（迁移后）

    // 统一提交（v2.2+v2.3：桶已在 Initialize 时按 SortingOrder 排序，UploadAndDrawAll 顺序遍历即可）
    _renderBatchManager.UploadAndDrawAll(); // 一次提交全部，桶已按 SortingOrder 排好序

    // TrailPool 独立提交（方案 A）
    _trailPool.Render();                    // 独立 Mesh，但接入统计
    RenderBatchManagerRuntimeStats.AccumulateBatch(trailDC, trailBatches, 0);

    RenderBatchManagerRuntimeStats.EndFrame();
}
```

> **⚠️ 未决项 UD-12**：是否所有 Renderer 共用一个 RBM 实例（全局统一），还是按系统域分多个 RBM 实例但共享 RuntimeAtlas？
>
> **推荐**：全局单 RBM 实例。理由：1 个 RBM = 所有 DC 在一次 UploadAndDrawAll 中提交，减少 `Graphics.DrawMesh` 调用次数。如果多个 RBM，每个 RBM 都要独立 UploadAndDrawAll。
>
> **风险**：单 RBM 的 BucketKey 空间变大（Bullet + Laser + VFX + DmgText 的所有 (Normal, Texture) 组合），但实际桶数仍然可控（每 Channel 1~2 张 Atlas Pages + Laser 独立贴图 1-3 个 = 10-15 桶）。
>
> **v2.2 新增 + v2.3 修正**：统一到单 RBM 后，桶的渲染顺序由 `SortingOrder` 决定。**排序时机：Initialize 阶段**——`_buckets` 数组在初始化末尾按 SortingOrder 升序排列并重建 `_bucketIndex` 映射。运行时 `UploadAndDrawAll()` 顺序遍历即可，零排序开销。（v2.3 UA-007：采纳方案 B — 注册时排序）

---

## 六、边界条件与风险

### 6.1 边界条件

| # | 场景 | 处理策略 |
|---|------|----------|
| BC-01 | 单张纹理超过 Atlas 尺寸 | 拒绝分配，返回 `AtlasAllocation.Invalid`，回退到独立贴图 |
| BC-02 | Channel 达到 MaxPages 上限 | 拒绝分配，返回 Invalid，LogWarning，回退到独立贴图 |
| BC-03 | 预热阶段 Atlas 已满 | 自动创建新 Page（仍在初始化期） |
| BC-04 | 战斗中首次出现新贴图种类 | Allocate + Blit 一次性开销 ~0.1ms |
| BC-05 | 一帧内大量新贴图涌入 | 可选分帧加载策略（预热机制可缓解） |
| BC-06 | 源纹理 Read/Write = false | `CommandBuffer.Blit()` 不要求 CPU 可读，无问题 |
| BC-07 | 源纹理使用压缩格式 | GPU Blit 处理格式转换，输出到 ARGB32 RT |
| BC-08 | RT Lost（WebGL Tab 切换） | 标记 dirty，重新 Blit 所有缓存条目。源纹理引用已保持（UD-04），不存在源纹理丢失风险。当缓存条目 >50 时启用分帧重建（每帧最多 20 张，P1 优化项）(v2.2 补充) |
| BC-09 | 序列帧 UV 计算 | AtlasAllocation.UVRect 替换 baseUV，GetFrameUV 在 UVRect 内再分帧 |
| BC-10 | 多个 TypeSO 引用同一 SourceTexture | 缓存去重：按 InstanceID 只 Blit 一次 |
| **BC-11** | ~~激光 UV 从全贴图变为子区域~~ | ~~LaserRenderer 的 UV 计算需要适配 Atlas 子区域映射~~ **(v2.2 取消：激光不入 Atlas，UV 不变)** |
| **BC-12** | **DamageNumber 数字切分从绝对 UV 变为相对 UV** | 数字宽度从 `0.1 * 全贴图` 变为 `0.1 * allocation.UVRect.width` |

### 6.2 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| WebGL RT Lost | 中 | 高 | dirty flag + 重建机制 |
| Blit Shader 兼容性 | 低 | 高 | fallback 降级 |
| 内存超预算 | 低 | 中 | MaxPages 硬上限 + Debug HUD |
| 首帧 Blit 卡顿 | 中 | 中 | 预热机制 |
| BucketKey 类型变更引发回归 | 低 | 中 | 编译期可发现 |
| **DamageNumber 迁移后视觉差异** | 中 | 中 | 迁移后逐帧对比验证 |
| ~~LaserRenderer UV 映射错误~~ | ~~中~~ | ~~高~~ | **(v2.2 取消：激光不入 Atlas，无 UV 映射变更风险)** |
| **迁移期间两套系统并存的混乱** | 低 | 中 | 分 Phase 迁移，每 Phase 保持可运行 |

### 6.3 未决项清单（UD - Undecided） — 全部已确认 ✅

> **2026-04-18 20:47 天命人逐一确认完毕，全部 12 项已定。**

| ID | 问题 | **最终决策** | 决策影响 |
|----|------|-------------|----------|
| ~~UD-01~~ | BucketKey.Texture 拓宽 | ✅ **方案 A：直接从 `Texture2D` 拓宽为 `Texture`** | RBM + 所有 Renderer |
| ~~UD-02~~ | Atlas 溢出时运行时创建新桶 | ✅ **选项 2：受控建桶 + MaxPages 硬上限** | ADR-015 + RBM |
| ~~UD-03~~ | WebGL RT Lost 重建策略 | ✅ **全量重 Blit（dirty flag → 遍历缓存全部重新 Blit）** | 缓存数据结构 |
| ~~UD-04~~ | 源纹理 Blit 后是否可卸载 | ✅ **保持引用（不卸载）**。省 <2MB 内存但加载逻辑复杂度翻倍，不值得 | 内存策略 |
| ~~UD-05~~ | 是否支持热更新纹理 | ✅ **第一版不支持，预留接口**。后续如需重构再加 | API 设计 |
| ~~UD-06~~ | 是否引入 RuntimeAtlasConfigSO | ✅ **引入**。参数未定需要反复调优，ConfigSO 省掉改常量→编译→测试循环 | 配置方式 |
| ~~UD-07~~ | 溢出时行为 | ✅ **回退到独立贴图**。宁可多 DC 也不丢画面 | Renderer 逻辑 |
| ~~UD-08~~ | ADR-007/008/010 Superseded | ✅ **已确认**：标记 Superseded by ADR-028（运行时），Editor 环境仍生效 | ADR 体系 |
| ~~UD-09~~ | 运行时忽略 AtlasBinding | ✅ **是**。统一管线不走两条路径 | 纹理解析链 |
| ~~UD-10~~ | 激光 UV 映射 | ✅ ~~修改 WriteSegmentQuad~~ → **(v2.2 废弃：激光不入 Atlas，UV 不变，UD-10 不再适用)** | LaserRenderer |
| ~~UD-11~~ | TrailPool 迁移方案 | ✅ **方案 A：保持独立 Mesh + 接入 RenderBatchManagerRuntimeStats 统计** | TrailPool |
| ~~UD-12~~ | 全局单 RBM vs 多 RBM | ✅ **全局单 RBM**。一次 UploadAndDrawAll 提交全部 DC | 渲染调度 |

---

## 七、API 设计草案

### 7.1 RuntimeAtlasManager（核心入口）

**(v2.3 内联补全——原 v1.0 §6.1 内容)**

```csharp
public class RuntimeAtlasManager : System.IDisposable
{
    /// <summary>初始化指定 Channel（创建首页 Atlas RT）</summary>
    public void InitChannel(AtlasChannel channel, AtlasChannelConfig config);

    /// <summary>
    /// 分配源纹理到指定 Channel 的 Atlas。
    /// 缓存命中：O(1)，零 GC。
    /// 未命中：执行 Blit，返回新分配。
    /// 分配失败（Atlas 满 + 超 MaxPages）：返回 AtlasAllocation.Invalid。
    /// </summary>
    public AtlasAllocation Allocate(AtlasChannel channel, Texture2D source);

    /// <summary>获取指定 Channel 指定 Page 的 Atlas RenderTexture</summary>
    public RenderTexture GetAtlasTexture(AtlasChannel channel, int pageIndex);

    /// <summary>批量预热——关卡加载时调用</summary>
    public void WarmUp(AtlasChannel channel, IReadOnlyList<Texture2D> sources);

    /// <summary>切关清空——释放所有 RT + 清除缓存</summary>
    public void Reset();

    /// <summary>RT Lost 恢复——标记 dirty 后全量/分帧重 Blit</summary>
    public void HandleRTLost();

    /// <summary>获取统计信息</summary>
    public RuntimeAtlasStats GetStats();

    public void Dispose();
}
```

### 7.2 RuntimeAtlasConfig（配置 SO）

```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Rendering/Runtime Atlas Config")]
public class RuntimeAtlasConfig : ScriptableObject
{
    [Header("Bullet Channel")]
    public AtlasChannelConfig Bullet = AtlasChannelConfig.Default;

    [Header("VFX Channel")]
    public AtlasChannelConfig VFX = AtlasChannelConfig.Default;

    [Header("DamageText Channel")]
    public AtlasChannelConfig DamageText = AtlasChannelConfig.Small;

    [Header("Laser Channel")]
    public AtlasChannelConfig Laser = AtlasChannelConfig.Small;

    [Header("Trail Channel (Reserved)")]
    public AtlasChannelConfig Trail = AtlasChannelConfig.Small;

    [Header("Character Channel (Reserved)")]
    public AtlasChannelConfig Character = AtlasChannelConfig.Default;
}
```

### 7.3 RuntimeAtlasStats

**(v2.3 内联补全——原 v1.0 §6.3 内容)**

```csharp
public readonly struct RuntimeAtlasStats
{
    /// <summary>各 Channel 的页面数</summary>
    public readonly int[] PageCountPerChannel;

    /// <summary>各 Channel 已分配的纹理数</summary>
    public readonly int[] AllocationCountPerChannel;

    /// <summary>各 Channel 的填充率（已用像素 / 总像素）</summary>
    public readonly float[] FillRatePerChannel;

    /// <summary>总 RT 内存占用（字节）</summary>
    public readonly long TotalMemoryBytes;

    /// <summary>总分配次数（含缓存命中）</summary>
    public readonly int TotalAllocations;

    /// <summary>缓存命中次数</summary>
    public readonly int CacheHits;

    /// <summary>Blit 次数（未命中时执行的实际 Blit）</summary>
    public readonly int BlitCount;
}
```

---

## 八、文件结构规划

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

| # | 验收项 | 通过标准 |
|---|--------|----------|
| AC-01 | 子弹使用 RuntimeAtlas 合批 | 10 种不同贴图的子弹，DrawCall ≤ 1（全部 Normal，ADR-029 v2 已移除 Additive） |
| AC-02 | VFX 使用 RuntimeAtlas 合批 | 5 种不同特效，DrawCall ≤ 1 |
| AC-03 | 混合尺寸纹理 | 32×32 和 128×128 纹理共存于同一 Atlas |
| AC-04 | 序列帧 UV 正确 | SpriteSheet 子弹/VFX 帧动画播放正确 |
| AC-05 | Atlas 溢出 | 超过单页自动创建新页，渲染无中断 |
| AC-06 | 切关清空 | Reset 后所有 RT 释放 |
| **AC-07** | **激光统一到全局 RBM** | 激光通过统一 RBM 渲染提交（纹理保持独立，不入 Atlas），UV 滚动正确 (v2.2 修正) |
| **AC-08** | **DamageNumber 迁移到 RBM** | 飘字视觉效果与迁移前一致 |
| **AC-09** | **TrailPool 接入统计** | Debug HUD 显示的 DC 数包含拖尾 |
| **AC-10** | **统一 DC 统计** | Debug HUD 一个数字反映全部渲染 DC |
| AC-11 | Editor Atlas 工具链仍可用 | `DanmakuAtlasPackerWindow` 正常运行，不报错 |
| AC-12 | 独立贴图回退 | Atlas 分配失败时回退到逐贴图模式 |

### 10.2 性能验收

| # | 指标 | 基线 | 目标 |
|---|------|------|------|
| PC-01 | 缓存命中 Allocate 耗时 | — | < 0.001ms |
| PC-02 | 单次 Blit 耗时（64×64） | — | < 0.1ms |
| PC-03 | 预热 20 张纹理总耗时 | — | < 5ms |
| PC-04 | 全局 DrawCall | N×1+ DC | **≤ 8 DC**（每 Channel 1~2 Atlas Pages + Trail 独立 DC） |
| PC-05 | 内存增量 | — | ≤ 48MB（大 Channel 2×16MB + 小 Channel 3×1MB + Trail） |
| PC-06 | 热路径零 GC | — | Allocate 缓存命中时零 GC |

### 10.3 兼容性验收

| # | 验收项 | 通过标准 |
|---|--------|----------|
| CC-01 | WebGL 2.0（微信小游戏） | Blit + 渲染正常，无报错 |
| CC-02 | Editor (Windows/macOS) | 编辑器内运行正常 |
| CC-03 | Standalone (IL2CPP) | PC 构建运行正常 |

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
