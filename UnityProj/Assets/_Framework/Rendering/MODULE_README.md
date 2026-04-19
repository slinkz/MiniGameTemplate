# Rendering — 共享渲染基础设施层

本目录存放 Danmaku / VFX / DamageNumber 等系统共用的底层渲染类型。

命名空间：`MiniGameTemplate.Rendering`

## 文件清单

| 文件 | 行数 | 说明 |
|------|------|------|
| `RenderVertex.cs` | — | 共享顶点格式（24 bytes，Position:float3 + Color32:4B + UV:float2），`StructLayout.Sequential` |
| `RenderLayer.cs` | — | 渲染层枚举（ADR-029 v2 后当前仅使用 `Normal = 0`） |
| `RenderSortingOrder.cs` | — | 渲染排序常量（Bullet=100, LaserDefault=120, VFX=200, DamageNumber=300） |
| `RenderBatchManager.cs` | 306 | [Phase 1 / R2.1] 渲染批次管理器——按 `(RenderLayer, Texture)` 分桶，支持 `BucketRegistration` 多模板材质与注册时排序 |
| `RenderBatchManagerRuntimeStats.cs` | — | [Phase R3] 渲染统计共享静态类，提供 Last/Peak/Average DrawCall 与 ActiveBatch 统计，TrailPool 已接入 |
| `AtlasMappingSO.cs` | 108 | [Phase 4.1] Atlas 映射 ScriptableObject——记录图集贴图 + 源贴图映射（双键查找），ADR-019 可逆派生产物 |
| `RuntimeAtlasSystem/` | — | [Phase R0/R1/R2/R3] RuntimeAtlas 运行时动态图集基础设施（ShelfPacking / Blit / Page / Manager / Stats / BindingResolver） |

## AtlasMappingSO（Phase 4.1 新增）

Atlas 映射资产——记录打包后的图集贴图与每张源贴图的 UV 映射。

### 核心类型

```
AtlasMappingSO : ScriptableObject
├── AtlasTexture (Texture2D)      — 打包生成的图集贴图
├── Padding (int)                  — 像素间距
├── Entries (AtlasEntry[])         — 子图映射数组
├── SchemaVersion (int)            — 版本号（迁移用）
└── API
    ├── TryFindEntry(source, out entry) → bool  — 双键查找（引用优先 → GUID 兜底）
    └── GetUVRectForSource(source) → Rect       — 快捷查找，未找到返回 (0,0,1,1)

AtlasEntry (struct)
├── SourceTexture (Texture2D)      — 原始独立贴图引用（保持可逆）
├── SourceGUID (string)            — 原始贴图 GUID（引用丢失时兜底）
├── UVRect (Rect)                  — 归一化 UV 区域
└── PixelRect (RectInt)            — 像素区域（调试/预览用）
```

### 设计原则（ADR-019）

1. **Atlas 为可逆派生产物**：源事实仍是原始 `SourceTexture + UVRect`
2. **双键查找**：优先按 Texture2D 引用匹配，引用丢失时按 GUID 兜底
3. **删除即回退**：删除 AtlasMappingSO 后，TypeSO 的 `AtlasBinding = null`，自动回退到独立贴图模式
4. **域分离**：Bullet/VFX/DamageNumber atlas 分域维护，不混打

### 与 TypeSO 的集成

```csharp
// BulletTypeSO / VFXTypeSO 的 Atlas 解析优先级：
// R2 前：AtlasBinding.AtlasTexture > SourceTexture > Renderer fallback
Texture2D tex = bulletType.GetResolvedTexture();     // 旧链路解析实际贴图
Rect baseUV   = bulletType.GetResolvedBaseUV();      // 旧链路解析基础 UV（Atlas 子区域或 UVRect）

// R2 后：RuntimeAtlasBindingResolver 会优先尝试 RuntimeAtlas，失败时再回退旧链路
```

## RenderBatchManager

渲染批次管理器是 Danmaku 和 VFX 共用的渲染基础设施，遵循 **共享实现，不共享实例** 原则。

### 核心类型

```
RenderBatchManager : IDisposable
├── BucketKey (readonly struct)    — (RenderLayer, Texture2D) 二元组
├── RenderBucket (class)           — 单桶：Mesh + Material实例 + RenderVertex[] + QuadCount
└── API
    ├── Initialize(keys, normalMat, additiveMat, maxQuads, sortingOrderProvider?)
    ├── TryGetBucket(key, out bucket) → bool
    ├── ResetAll()                    — 帧头清零 QuadCount
    ├── UploadAndDrawAll()            — 帧尾 SetVertexBufferData + Graphics.DrawMesh
    └── Dispose()                     — 销毁 Mesh/Material 实例
```

### 设计原则（ADR-015）

1. **初始化时预热**：必须在 `Initialize()` 时传入所有 `BucketKey`（来自各自 TypeRegistry），运行时禁止隐式建桶
2. **共享实现，不共享实例**：Danmaku 和 VFX 各自持有独立的 `RenderBatchManager` 实例，生命周期跟随各自系统
3. **未知桶处理**：运行时请求未注册桶时跳过渲染 + 累加 `UnknownBucketErrorCount`（开发期 LogWarning）
4. **零依赖**：本层不引用 Danmaku、VFX 或任何业务命名空间

### 同 asmdef

所有代码在 `MiniGameFramework.Runtime` 内，不新建程序集。

### 使用方式

```csharp
// 1. 构建预热注册表（来自 TypeRegistry / RuntimeAtlas 解析结果）
var registrations = new List<RenderBatchManager.BucketRegistration>();
registrations.Add(new RenderBatchManager.BucketRegistration(
    new RenderBatchManager.BucketKey(RenderLayer.Normal, texture),
    templateMaterial,
    RenderSortingOrder.Bullet));

// 2. 初始化
var batchManager = new RenderBatchManager();
batchManager.Initialize(registrations, maxQuadsPerBucket: 2048);

// 3. 每帧
batchManager.ResetAll();
// ... 逐弹丸/特效 TryGetBucket + AllocateQuad + 写顶点 ...
batchManager.UploadAndDrawAll();

// 4. 销毁
batchManager.Dispose();
```
