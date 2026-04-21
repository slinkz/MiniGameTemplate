# 分阶段实施方案：三项 RuntimeAtlas 深化任务

> **文档版本**：v1.1  
> **日期**：2026-04-21  
> **前置条件**：R0~R5 + R4.1/R4.3/R4.4 已完成，Editor Play Mode 验收 8/12 AC 通过  
> **目标**：在现有 RuntimeAtlas 基础设施上，完成三项增量任务——懒建页优化、Laser 渲染路径统一、Trail 纹理化接入
>
> **变更记录**：
> | 版本 | 日期 | 变更说明 |
> |---|---|---|
> | v1.0 | 2026-04-21 | 初始版本 |
> | v1.1 | 2026-04-21 | PK Round 1 修正：PI-001~005 回应 |

---

## 目录

1. [方案一：R4.4A — Atlas 懒建页（Lazy Page Creation）](#方案一r44a--atlas-懒建页lazy-page-creation)
2. [方案二：Laser 接入 RuntimeAtlas](#方案二laser-接入-runtimeatlas)
3. [方案三：Trail 纹理化接入 RuntimeAtlas](#方案三trail-纹理化接入-runtimeatlas)
4. [跨方案依赖图](#跨方案依赖图)
5. [风险登记与缓解](#风险登记与缓解)

---

## 方案一：R4.4A — Atlas 懒建页（Lazy Page Creation）

### 1.1 背景与动机

当前 `RuntimeAtlasManager.InitChannel()` 在初始化时 **无条件创建第一张 AtlasPage**（`CreatePage()`），即使该 Channel 在当前关卡可能完全没有被使用。

```csharp
// 当前代码 — RuntimeAtlasManager.cs L61-67
AtlasChannelState state = new AtlasChannelState { Config = config };
state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));  // ← 无条件创建 RT
_channels[channel] = state;
```

每张 RT 的内存开销：

| Channel Config | AtlasSize | 内存（ARGB32） |
|---|---|---|
| Default | 2048×2048 | **16 MB** |
| Small | 1024×1024 | **4 MB** |

6 个 Channel 全部初始化 = 2×16 + 4×4 = **48 MB**。若关卡只用到 Bullet + VFX 两个 Channel，白白浪费了 Laser/Trail/DamageText/Character 的 RT 内存。

**在微信小游戏环境下，48 MB 的 RT 常驻内存是不可接受的。**

### 1.2 目标

- 将 Page 0 的创建时机从 `InitChannel()` 延迟到 `TryAllocateInternal()` **首次分配**时
- 零行为变更：所有现有测试和消费方代码无需修改
- 最坏情况节省 **32 MB** RT 内存（4 个未使用的 Channel）

### 1.3 设计约束

| 约束 | 说明 |
|---|---|
| **API 不变** | `InitChannel()`, `Allocate()`, `WarmUp()`, `GetStats()` 签名和语义不变 |
| **Stats 兼容** | 未创建 Page 的 Channel 在 Stats 中 `PageCount=0, FillRate=0` |
| **WarmUp 触发建页** | `WarmUp()` 内调 `Allocate()` → 首次分配自然触发 Page 0 创建 |
| **RT Lost 安全** | `HandleRTLost()` 仅遍历已存在的 Pages，空列表 = no-op |

### 1.4 分步实施

#### Step 1：移除 `InitChannel()` 中的 `CreatePage()` 调用

**文件**：`RuntimeAtlasManager.cs`

```csharp
// Before (L61-67)
AtlasChannelState state = new AtlasChannelState { Config = config };
state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));
_channels[channel] = state;

// After
AtlasChannelState state = new AtlasChannelState { Config = config };
// Page 0 延迟到首次 Allocate 时创建（R4.4A 懒建页）
_channels[channel] = state;
```

#### Step 2：在 `TryAllocateInternal()` 开头加入 Page 0 懒建逻辑

**文件**：`RuntimeAtlasManager.cs`

```csharp
private AtlasAllocation TryAllocateInternal(AtlasChannel channel, AtlasChannelState state, Texture2D source)
{
    int width = source.width;
    int height = source.height;
    AtlasChannelConfig config = state.Config;

    // R4.4A：懒建页——首次分配时创建 Page 0
    if (state.Pages.Count == 0)
    {
        state.Pages.Add(CreatePage(config.AtlasSize, channel, 0));
    }

    // ... 后续逻辑不变
    for (int pageIndex = 0; pageIndex < state.Pages.Count; pageIndex++)
    {
        // ...
    }
}
```

#### Step 3：修复 `GetAtlasTexture()` 对空 Pages 的处理

当前逻辑已正确处理（`pageIndex < 0 || pageIndex >= state.Pages.Count` 返回 null），**无需修改**。

#### Step 4：修复 `GetStats()` 对空 Pages 的处理

~~当前逻辑已正确处理~~（v1.1 修正，PI-003）：`Math.Max(1, state.Pages.Count)` 在 `Pages.Count=0` 时将 `totalPixels` 设为一页像素量，语义不合理。需要顺手修复：

```csharp
// Before (L228)
long totalPixels = (long)state.Config.AtlasSize * state.Config.AtlasSize * Math.Max(1, state.Pages.Count);

// After (R4.4A 修正)
long totalPixels = (long)state.Config.AtlasSize * state.Config.AtlasSize * state.Pages.Count;
fillRate[index] = totalPixels > 0 ? (float)usedPixels / totalPixels : 0f;
```

当 `Pages.Count=0`：`totalPixels=0`，`fillRate=0`，`totalMemoryBytes` 贡献为 0。**语义精确。**

#### Step 5：验证 `RebuildChannel()` 对空 Pages 的处理

`RebuildChannel()` 中 `ReleasePages()` 清空 Pages 后，后续 `TryAllocateInternal()` 会自动触发懒建——**无需修改**。

### 1.5 验收标准

| AC | 验收内容 | 方法 |
|---|---|---|
| AC-1 | 初始化后未使用的 Channel `PageCount=0` | DebugHUD 观察 Stats |
| AC-2 | 首次 `Allocate()` 后 Channel `PageCount=1` | DebugHUD 观察 Stats |
| AC-3 | `WarmUp()` 触发 Page 创建 | Editor 预热验证 |
| AC-4 | RT Lost → Restore 后功能正常 | 模拟 `HandleRTLost()` + `RestoreDirtyPages()` |
| AC-5 | 内存对比：6 Channel 初始化前后 RT 内存差值 | Profiler Memory 快照 |

### 1.6 预估工时

| 步骤 | 耗时 |
|---|---|
| Step 1-2：代码改动 | 0.5h |
| Step 3-5：审查确认无需改动 | 0.5h |
| 验收 | 1h |
| **合计** | **2h** |

---

## 方案二：Laser 接入 RuntimeAtlas

### 2.1 背景与动机

#### 当前 Laser 渲染数据流

```
LaserTypeSO.LaserTexture (Texture2D)
    → LaserRenderer.Rebuild() 每帧遍历活跃激光
        → BucketKey = (RenderLayer.Normal, type.LaserTexture)  // 独立贴图直接做 Key
        → TryGetOrCreateBucket(key, _laserMaterial, ...)
        → WriteSegmentQuad() 写入顶点
    → UploadAndDrawAll() → Graphics.DrawMesh
```

**问题**：每种 LaserTypeSO 的 `LaserTexture` 独立做桶 Key，如果有 5 种激光类型 = 5 个独立材质实例 = **5 个 DrawCall**。

#### PK 评审决策 (UA-002)

> **激光不入 Atlas，保持独立贴图注册统一 RBM。**

原因：激光纹理通常 **UV.y 方向连续滚动**，截断到 Atlas 子区域会导致 **UV 环绕边缘采样到相邻子图**。

#### 但现在情况变了

R4.4 引入了 **Channel 隔离**，每个 Channel 独立 Atlas。如果 Laser Channel 只有 3-5 张纹理、每张 128×128 或 256×256，一页 1024×1024 的 Atlas 绑绑有余。关键是 Shader 端需要处理 UV 环绕到 Atlas 子区域内。

### 2.2 可选方案对比

| 方案 | DC 收益 | Shader 改动 | UV 风险 | 推荐 |
|---|---|---|---|---|
| **A：保持现状（独立贴图）** | 0 | 无 | 无 | ⬜ 最安全 |
| **B：Laser 入 Atlas + Shader UV 子区域环绕** | ≤5 DC → 1 DC | 需要 | 需验证 | ⬜ 最大收益 |
| **C：Laser 入 Atlas + 禁用 UV 滚动** | ≤5 DC → 1 DC | 小 | 低 | ✅ **推荐** |

**方案 C 理由**：
1. 激光视觉效果主要靠 WidthProfile 曲线 + CoreColor/EdgeColor 渐变 + Phase alpha 控制，UV 滚动在多数设计中是 **锦上添花而非必须**
2. 如果设计师需要 UV 滚动效果，可以在配置中用 `UVScrollSpeed=0` 关闭入 Atlas，走独立贴图 fallback
3. 避免了 Shader 改动的复杂度和 WebGL 兼容风险

### 2.3 分步实施（方案 C）

#### Phase L1：基础设施准备（0.5 天）

##### L1.1：LaserTypeSO 新增字段

**文件**：`LaserTypeSO.cs`

```csharp
[Header("RuntimeAtlas")]
[Tooltip("是否允许将激光纹理 Blit 到 RuntimeAtlas（禁用 UV 滚动时推荐开启）")]
public bool UseRuntimeAtlas = false;
```

##### L1.2：RuntimeAtlasBindingResolver 新增 `ResolveLaser()` 方法（v1.1 修正，PI-005）

**文件**：`RuntimeAtlasBindingResolver.cs`

```csharp
// v1.1：去掉冗余的 fallbackTexture 参数（PI-005），直接从 type 读取 LaserTexture
public static ResolvedTextureBinding ResolveLaser(
    RuntimeAtlasManager atlasManager,
    MiniGameTemplate.Danmaku.LaserTypeSO type)
{
    if (type == null || type.LaserTexture == null)
        return default;

    // 策略：UseRuntimeAtlas=true 时走 Atlas
    if (type.UseRuntimeAtlas && atlasManager != null && atlasManager.IsInitialized)
    {
        AtlasAllocation allocation = atlasManager.Allocate(AtlasChannel.Laser, type.LaserTexture);
        if (allocation.Valid)
        {
            RenderTexture atlasTexture = atlasManager.GetAtlasTexture(AtlasChannel.Laser, allocation.PageIndex);
            if (atlasTexture != null)
                return new ResolvedTextureBinding(atlasTexture, allocation.UVRect, true);
        }
    }

    // Fallback：独立贴图
    return new ResolvedTextureBinding(type.LaserTexture, new Rect(0, 0, 1, 1), false);
}
```

#### Phase L2：LaserRenderer 集成（1 天）

##### L2.1：LaserRenderer 持有 RuntimeAtlasManager 引用

**文件**：`LaserRenderer.cs`

```csharp
private RenderBatchManager _batchManager;
private Material _laserMaterial;
private RuntimeAtlasManager _runtimeAtlas;  // 新增
private int _quadCount;
```

##### L2.2：Initialize 中接收共享 RuntimeAtlas（v1.1 修正，PI-001）

```csharp
internal void Initialize(DanmakuRenderConfig renderConfig, DanmakuTypeRegistry registry,
    int maxQuadsPerBucket, RuntimeAtlasManager sharedAtlas = null)
{
    _batchManager = new RenderBatchManager();
    _laserMaterial = renderConfig.LaserMaterial;

    // v1.1：接收 DanmakuSystem 持有的共享 RuntimeAtlasManager
    // Channel 隔离保证 Laser Channel 不会和 Bullet Channel 冲突
    _runtimeAtlas = sharedAtlas;

    _batchManager.Initialize(System.Array.Empty<RenderBatchManager.BucketRegistration>(), maxQuadsPerBucket);
}
```

> ⚠️ **架构决策点**（v1.1 修正，PI-001）：
>
> **结论：共享实例**。DanmakuSystem 持有唯一 RuntimeAtlasManager，通过 Initialize 参数注入到 BulletRenderer/LaserRenderer/LaserWarningRenderer/TrailPool。
>
> 理由：
> - `RuntimeAtlasManager.Initialize()` 遍历全部 6 个 Channel 枚举值，独立实例会创建 18 个冗余 Channel 状态
> - Channel 隔离已保证不同 Renderer 使用不同 Channel 时互不干扰
> - 共享实例在微信小游戏环境下减少对象分配开销

##### L2.3：Rebuild 中使用 Resolver

```csharp
internal void Rebuild(LaserPool pool, DanmakuTypeRegistry registry)
{
    _batchManager.ResetAll();
    _quadCount = 0;

    if (pool.ActiveCount == 0)
    {
        _batchManager.UploadAndDrawAll();
        return;
    }

    for (int i = 0; i < pool.Capacity; i++)
    {
        ref var laser = ref pool.Data[i];
        if (laser.Phase == 0) continue;
        if (laser.SegmentCount == 0) continue;

        var type = registry.GetLaserType(laser.LaserTypeIndex);
        if (type.LaserTexture == null) continue;

        // L2.3：通过 Resolver 解算纹理（v1.1：签名简化，PI-005）
        var binding = RuntimeAtlasBindingResolver.ResolveLaser(
            _runtimeAtlas, type);

        if (!binding.IsValid) continue;

        var bucketKey = new RenderBatchManager.BucketKey(RenderLayer.Normal, binding.Texture);
        if (!_batchManager.TryGetOrCreateBucket(bucketKey, _laserMaterial, RenderSortingOrder.LaserDefault, out var bucket))
            continue;

        // ... UV 需要 Remap
        // 如果走了 Atlas，UV.y 的滚动计算需要映射到 Atlas 子区域
        // 方案 C 下 UseRuntimeAtlas=true 意味着禁用了滚动，UV 直接用 binding.UVRect

        float alpha = GetPhaseAlpha(ref laser, type);
        if (alpha <= 0f) continue;

        float uvYAccum = 0f;
        float lengthAccum = 0f;
        float totalLength = laser.VisualLength > 0f ? laser.VisualLength : laser.Length;

        for (int s = 0; s < laser.SegmentCount; s++)
        {
            ref var seg = ref laser.Segments[s];
            if (seg.Length <= 0.0001f) continue;

            WriteSegmentQuad(bucket, ref seg, type, laser.Width, alpha,
                ref uvYAccum, ref lengthAccum, totalLength,
                binding.UsesRuntimeAtlas ? binding.UVRect : new Rect(0,0,1,1));
        }
    }

    _batchManager.UploadAndDrawAll();
}
```

##### L2.4：WriteSegmentQuad UV Remap（v1.1 修正，PI-002）

> **UV 映射语义变更说明**（v1.1 新增）：
>
> - **非 Atlas 模式**（`UseRuntimeAtlas=false`）：UV.y = 世界空间累积长度值，`wrapMode=Repeat` 自然环绕，纹理按世界单位 1:1 平铺。
> - **Atlas 模式**（`UseRuntimeAtlas=true`）：UV.y 归一化到 [0,1]，整条激光映射完整纹理一次。这是**有意的语义变更**——因为 Atlas RT 的 `wrapMode=Clamp`，无法支持 UV 环绕。
>
> 实际影响：Atlas 模式下，短激光纹理密度高（细节清晰），长激光纹理密度低（略有拉伸）。对于多数弹幕游戏场景（激光长度 2~10 世界单位），视觉差异可接受。
>
> 如果需要"按世界单位平铺"效果，应设置 `UseRuntimeAtlas=false` 走独立贴图。

```csharp
private void WriteSegmentQuad(
    RenderBatchManager.RenderBucket bucket,
    ref LaserSegment seg,
    LaserTypeSO type,
    float width,
    float alpha,
    ref float uvYAccum,
    ref float lengthAccum,
    float totalLength,
    Rect atlasUVRect)  // 新增参数
{
    // ... 顶点计算不变 ...

    float uvYEnd = uvYAccum + seg.Length;

    float u0, u1, v0, v1;
    if (atlasUVRect.width < 1f) // Atlas 模式：归一化到子区域
    {
        // PI-002：明确语义——整条激光映射完整纹理一次
        u0 = atlasUVRect.x;
        u1 = atlasUVRect.x + atlasUVRect.width;
        v0 = atlasUVRect.y + (uvYAccum / totalLength) * atlasUVRect.height;
        v1 = atlasUVRect.y + (uvYEnd / totalLength) * atlasUVRect.height;
    }
    else // 非 Atlas 模式：保留原始世界空间 UV
    {
        u0 = 0f;
        u1 = 1f;
        v0 = uvYAccum;
        v1 = uvYEnd;
    }

    // ... 写入顶点时使用计算后的 UV ...
}
```

##### L2.5：LaserWarningRenderer 同步改造

LaserWarningRenderer 的结构与 LaserRenderer 几乎完全一致，按相同模式改造。预警线本身不需要 UV 滚动，**天然适合入 Atlas**。

##### L2.6：Dispose 清理

```csharp
public void Dispose()
{
    _batchManager?.Dispose();
    // v1.1：共享实例，不在此处 Dispose RuntimeAtlasManager（PI-001）
    _runtimeAtlas = null;
}
```

#### Phase L3：LaserWarningRenderer 集成（0.5 天）

与 L2 相同模式，不再赘述。LaserWarningRenderer 可以默认 `UseRuntimeAtlas=true`（预警线无 UV 滚动需求）。

#### Phase L4：验收（0.5 天）

### 2.4 验收标准

| AC | 验收内容 | 方法 |
|---|---|---|
| LC-1 | `UseRuntimeAtlas=false` 的激光行为完全不变 | 对照 baseline 截图 |
| LC-2 | `UseRuntimeAtlas=true` 的激光正确渲染（无 UV 撕裂） | 视觉对比 |
| LC-2a | 不同长度激光（2/5/10 单位）的纹理密度视觉可接受（v1.1，PI-002） | 视觉对比 |
| LC-3 | 多种激光类型共用 Atlas 时 DC 合并 | DebugHUD DC 计数 |
| LC-4 | Laser Channel 在 Stats 中正确显示页数/分配/填充率 | DebugHUD Atlas section |
| LC-5 | RT Lost → Restore 后激光渲染恢复 | 模拟测试 |
| LC-6 | LaserWarningRenderer 同步工作 | 视觉验证 |

### 2.5 预估工时

| Phase | 耗时 |
|---|---|
| L1：基础设施 | 0.5 天 |
| L2：LaserRenderer | 1 天 |
| L3：LaserWarningRenderer | 0.5 天 |
| L4：验收 | 0.5 天 |
| **合计** | **2.5 天** |

---

## 方案三：Trail 纹理化接入 RuntimeAtlas

### 3.1 背景与动机

#### 当前 Trail 渲染数据流

```
BulletTypeSO.Trail = TrailMode.HeavyTrail
    → TrailPool.Allocate(type) 读取 TrailPointCount / TrailWidth / TrailWidthCurve / TrailColor
    → TrailPool.AddPoint() 每帧追加弹丸位置
    → TrailPool.Render() 每帧重建 Mesh
        → BuildTrailMesh() 生成三角带（每段 2 顶点 / 6 索引）
        → _material.mainTexture = Texture2D.whiteTexture  // ← 纯色！
        → Graphics.DrawMesh(_mesh, ..., _material, 0)
```

**核心问题**：Trail 当前是 **纯色渲染**，使用 `Texture2D.whiteTexture` 作为主纹理，全部视觉效果依赖顶点色 Gradient。

这意味着：
1. 无法实现"火焰尾巴"、"电弧尾巴"等纹理化 Trail 效果
2. 即使只有颜色渐变，也缺乏纹理带来的细节层次
3. 与弹幕系统其他渲染路径（Bullet/Laser）的视觉丰富度不匹配

#### 目标

- 在 `BulletTypeSO` 中新增 `TrailTexture` 字段
- Trail 渲染支持纹理采样（UV 沿宽度 x 轴、长度 y 轴映射）
- 有纹理的 Trail 走 RuntimeAtlas，无纹理的 Trail 保持 whiteTexture 行为
- **不改变现有 Trail 的单 DrawCall 渲染架构**（保持 `Graphics.DrawMesh`）

### 3.2 设计分析

#### 3.2.1 为什么不让 Trail 走 RBM？

当前 TrailPool 用独立 Mesh + `Graphics.DrawMesh`，好处是：
- 所有 Trail 共享一个 Mesh 对象，只有 **1 个 DrawCall**
- 顶点布局和 RBM 相同（RenderVertex），但 Mesh 是三角带而非 Quad

如果改为走 RBM，每种不同纹理的 Trail 会变成独立的 Bucket = 多个 DC。**不如让所有 Trail 共享同一张 Atlas 纹理。**

#### 3.2.2 纹理化策略

```
所有 TrailTexture → RuntimeAtlas.Allocate(AtlasChannel.Trail, texture)
    → 获得 Atlas RenderTexture + UV 子区域
    → _material.mainTexture = Atlas RT (替代 whiteTexture)
    → BuildTrailMesh() 中 UV 映射到 Atlas 子区域内
```

**关键约束**：所有活跃 Trail 必须共享同一张 Atlas 页面（因为只有一个 Material 实例）。

- Trail Channel 配置 `MaxPages=1`（当前默认）
- 如果有 Trail 溢出，那条 Trail fallback 到纯色模式

#### 3.2.3 混合渲染：有纹理 Trail + 无纹理 Trail

有两种策略：

| 策略 | 优点 | 缺点 |
|---|---|---|
| **A：全部入 Atlas** | 统一材质，1 DC | 无纹理 Trail 也要 Blit whiteTexture 到 Atlas |
| **B：分离渲染** | 精确 | 有纹理 Trail 一个 DC + 无纹理 Trail 一个 DC = 2 DC |

**推荐策略 A**：把 `Texture2D.whiteTexture` 也 Blit 到 Atlas 作为 fallback UV 子区域，所有 Trail 统一走 Atlas 纹理。成本是 Atlas 中浪费一小块 4×4 区域，收益是保持 1 DC。

### 3.3 分步实施

#### Phase T1：配置层扩展（0.5 天）

##### T1.1：BulletTypeSO 新增 TrailTexture 字段

**文件**：`BulletTypeSO.cs`

```csharp
[Header("拖尾")]
public TrailMode Trail = TrailMode.None;

[Tooltip("拖尾纹理（null=纯色拖尾，使用顶点色）")]
public Texture2D TrailTexture;  // 新增

// ... 现有字段 ...
```

##### T1.2：TrailPool 新增 RuntimeAtlasManager 支持

**文件**：`TrailPool.cs`

```csharp
private RuntimeAtlasManager _runtimeAtlas;
private Rect _whiteTextureUV;  // whiteTexture 在 Atlas 中的 UV
private RenderTexture _atlasRT; // Atlas 贴图引用（替代 whiteTexture）
```

#### Phase T2：TrailPool 集成 RuntimeAtlas（1.5 天）

##### T2.1：Initialize 改造

```csharp
public void Initialize(Material material, RuntimeAtlasManager runtimeAtlas = null)
{
    _runtimeAtlas = runtimeAtlas;

    if (material != null)
    {
        _material = new Material(material) { name = "Danmaku Trail (Instance)" };

        if (_runtimeAtlas != null && _runtimeAtlas.IsInitialized)
        {
            // 分配 whiteTexture 到 Atlas 作为无纹理 Trail 的 fallback
            var whiteAlloc = _runtimeAtlas.Allocate(AtlasChannel.Trail, Texture2D.whiteTexture);
            if (whiteAlloc.Valid)
            {
                _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, whiteAlloc.PageIndex);
                _whiteTextureUV = whiteAlloc.UVRect;
                _material.mainTexture = _atlasRT;
            }
            else
            {
                _material.mainTexture = Texture2D.whiteTexture;
                _whiteTextureUV = new Rect(0, 0, 1, 1);
            }
        }
        else
        {
            _material.mainTexture = Texture2D.whiteTexture;
            _whiteTextureUV = new Rect(0, 0, 1, 1);
        }
    }

    // ... 后续 Mesh 初始化不变 ...
}
```

##### T2.2：Allocate 时预分配纹理到 Atlas

```csharp
public int Allocate(BulletTypeSO type)
{
    if (_freeTop == 0) return -1;

    int slot = _freeSlots[--_freeTop];
    ActiveCount++;

    ref var trail = ref _trails[slot];
    trail.Active = true;
    trail.PointCount = 0;
    trail.MaxPoints = Mathf.Min(type.TrailPointCount, MAX_POINTS_PER_TRAIL);
    trail.Width = type.TrailWidth;
    trail.WidthCurve = type.TrailWidthCurve;
    trail.ColorGradient = type.TrailColor;

    // T2.2：解算纹理 UV
    if (type.TrailTexture != null && _runtimeAtlas != null && _runtimeAtlas.IsInitialized)
    {
        var alloc = _runtimeAtlas.Allocate(AtlasChannel.Trail, type.TrailTexture);
        if (alloc.Valid)
        {
            trail.TextureUVRect = alloc.UVRect;
            // 确保 material 纹理指向 Atlas RT
            if (_atlasRT == null)
            {
                _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, alloc.PageIndex);
                _material.mainTexture = _atlasRT;
            }
        }
        else
        {
            trail.TextureUVRect = _whiteTextureUV; // 溢出 fallback
        }
    }
    else
    {
        trail.TextureUVRect = _whiteTextureUV; // 无纹理 fallback
    }

    return slot;
}
```

##### T2.3：TrailInstance 新增 UV 字段

```csharp
private class TrailInstance
{
    public bool Active;
    public int PointCount;
    public int MaxPoints;
    public float Width;
    public AnimationCurve WidthCurve;
    public Gradient ColorGradient;
    public Rect TextureUVRect;  // 新增：Atlas 中的 UV 子区域
    public readonly Vector2[] Points = new Vector2[MAX_POINTS_PER_TRAIL];
}
```

##### T2.4：BuildTrailMesh UV Remap

```csharp
private void BuildTrailMesh(TrailInstance trail)
{
    int pointCount = trail.PointCount;
    Rect uvRect = trail.TextureUVRect;  // Atlas UV 子区域

    for (int p = 0; p < pointCount; p++)
    {
        float t = (float)p / (pointCount - 1);
        // ... 宽度计算不变 ...

        // UV Remap 到 Atlas 子区域
        float u0 = uvRect.x;                          // 左边缘
        float u1 = uvRect.x + uvRect.width;            // 右边缘
        float v = uvRect.y + t * uvRect.height;         // 沿长度方向

        _vertices[_vertexCount] = new RenderVertex
        {
            Position = ...,
            Color = color,
            UV = new Vector2(u0, v),  // 替代原来的 (0f, t)
        };
        _vertices[_vertexCount + 1] = new RenderVertex
        {
            Position = ...,
            Color = color,
            UV = new Vector2(u1, v),  // 替代原来的 (1f, t)
        };

        // ... 索引构造不变 ...
    }
}
```

##### T2.5：Render 和 Dispose 更新（v1.1 修正，PI-004）

```csharp
public void Render()
{
    // ... 现有逻辑 ...

    // v1.1 修正（PI-004）：RT Lost 检测 + 恢复尝试
    if (_atlasRT != null && !_atlasRT.IsCreated())
    {
        // RT Lost：先回退到 whiteTexture 保证本帧可渲染
        _material.mainTexture = Texture2D.whiteTexture;
        _atlasRT = null;
    }
    else if (_atlasRT == null && _runtimeAtlas != null && _runtimeAtlas.IsInitialized)
    {
        // 尝试恢复：检查 Atlas 是否已被 RestoreDirtyPages 重建
        var testAlloc = _runtimeAtlas.TryGetAllocation(AtlasChannel.Trail, Texture2D.whiteTexture);
        if (testAlloc.Valid)
        {
            _atlasRT = _runtimeAtlas.GetAtlasTexture(AtlasChannel.Trail, testAlloc.PageIndex);
            if (_atlasRT != null && _atlasRT.IsCreated())
            {
                _material.mainTexture = _atlasRT;
                // 注意：各 Trail 的 TextureUVRect 在 Rebuild 后已更新，这里只需恢复 Material 纹理指向
            }
            else
            {
                _atlasRT = null; // 仍未恢复
            }
        }
    }

    // ... 后续 DrawMesh 不变 ...
}

public void Dispose()
{
    if (_mesh != null) Object.Destroy(_mesh);
    if (_material != null) Object.Destroy(_material);
    // RuntimeAtlasManager 生命周期由 DanmakuSystem 管理，这里不 Dispose（v1.1：共享实例）
    _runtimeAtlas = null;
    _atlasRT = null;
}
```

#### Phase T3：DanmakuSystem 集成（0.5 天）

##### T3.1：DanmakuSystem 传递共享 RuntimeAtlasManager 给 TrailPool（v1.1 修正，PI-001）

**结论：共享实例注入**。DanmakuSystem 持有唯一 RuntimeAtlasManager（已在 BulletRenderer.Initialize 时创建），通过 `TrailPool.Initialize(material, sharedAtlas)` 传入。

```csharp
// DanmakuSystem.Runtime.cs — InitializeSubsystems (v1.1 新增)
// 唯一 RuntimeAtlasManager 由 BulletRenderer 创建逻辑改为 DanmakuSystem 持有
RuntimeAtlasManager sharedAtlas = null;
if (_renderConfig.RuntimeAtlasConfig != null)
{
    sharedAtlas = new RuntimeAtlasManager();
    sharedAtlas.Initialize(_renderConfig.RuntimeAtlasConfig);
}

_bulletRenderer.Initialize(_renderConfig, _typeRegistry, _worldConfig.MaxBullets * 4, sharedAtlas);
_laserRenderer.Initialize(_renderConfig, _typeRegistry, ..., sharedAtlas);
_laserWarningRenderer.Initialize(_renderConfig, _typeRegistry, ..., sharedAtlas);
_trailPool.Initialize(_renderConfig.BulletMaterial, sharedAtlas);

// DanmakuSystem.DisposeSubsystems 中统一 Dispose
sharedAtlas?.Dispose();
```

#### Phase T4：验收（0.5 天）

### 3.4 验收标准

| AC | 验收内容 | 方法 |
|---|---|---|
| TC-1 | `TrailTexture=null` 的 Trail 行为完全不变（纯色渐变） | 对照 baseline 截图 |
| TC-2 | `TrailTexture` 赋值后 Trail 正确显示纹理 | 视觉验证 |
| TC-3 | 多种 TrailTexture 共享 Atlas 仍为 1 DC | DebugHUD DC 计数 |
| TC-4 | Trail Channel 在 Stats 中正确显示 | DebugHUD Atlas section |
| TC-5 | RT Lost 后 Trail 回退到纯色并恢复 | 模拟测试 |
| TC-6 | 无 TrailTexture 的 Trail + 有 TrailTexture 的 Trail 同屏混合 | 视觉验证 |

### 3.5 预估工时

| Phase | 耗时 |
|---|---|
| T1：配置层 | 0.5 天 |
| T2：TrailPool 集成 | 1.5 天 |
| T3：DanmakuSystem 集成 | 0.5 天 |
| T4：验收 | 0.5 天 |
| **合计** | **3 天** |

---

## 跨方案依赖图

```
R4.4A (懒建页)
  │
  ├─── 方案二 (Laser) 依赖 R4.4A
  │    └── Laser Channel 初始化后 Page=0，首次发射激光时才建页
  │
  └─── 方案三 (Trail) 依赖 R4.4A
       └── Trail Channel 初始化后 Page=0，首次分配 Trail 时才建页
```

**实施顺序**：`R4.4A` → `Laser` / `Trail`（后两者可并行）

## 总工时汇总

| 方案 | 工时 |
|---|---|
| R4.4A 懒建页 | 2h（0.25 天） |
| Laser 接入 | 2.5 天 |
| Trail 纹理化 | 3 天 |
| **合计** | **5.75 天** |

---

## 风险登记与缓解

| ID | 风险 | 严重度 | 概率 | 缓解措施 |
|---|---|---|---|---|
| R-01 | 懒建页导致首次 Allocate 帧耗突增 | 🟡 中 | 中 | WarmUp 在 loading 阶段预热；首帧 spike 已在 BulletRenderer 中被 WarmUp 覆盖 |
| R-02 | Laser UV Remap 导致 Atlas 边缘采样错误 | 🟡 中 | 低 | 方案 C 禁用滚动避开；Padding=1 提供 1px 安全边距 |
| R-03 | Trail 纹理混合场景 Atlas 溢出 | 🟡 中 | 低 | Trail Channel MaxPages=1 + 溢出 fallback 到 whiteTexture |
| R-04 | RT Lost 后 Trail Material 纹理引用失效 | 🔴 高 | 中 | `Render()` 中检测 `_atlasRT.IsCreated()` 并回退 |
| R-05 | ~~多个 RuntimeAtlasManager 实例增加内存~~ | ~~🟢 低~~ | ~~确定~~ | v1.1：改为共享实例，风险消除（PI-001） |
| R-06 | TrailPool 的 `Allocate()` 中 Atlas 分配非零开销 | 🟢 低 | 确定 | 首次分配后走缓存命中（AllocationCache），热路径成本≈1 次 Dict 查找 |

---

> **下一步**：天命人确认方案后，按 `R4.4A → Laser → Trail` 顺序逐步实施，每步闭环验收。
