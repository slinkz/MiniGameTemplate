---
system: runtime-atlas
scope: design-architecture
last_verified: 2026-05-02
related_code: Assets/_Framework/RuntimeAtlas/*.cs
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
