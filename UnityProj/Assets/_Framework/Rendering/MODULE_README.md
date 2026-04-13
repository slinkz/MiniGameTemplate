# Rendering — 共享渲染基础设施层

本目录存放 Danmaku / VFX / DamageNumber 等系统共用的底层渲染类型。

命名空间：`MiniGameTemplate.Rendering`

## 文件清单

| 文件 | 行数 | 说明 |
|------|------|------|
| `RenderVertex.cs` | — | 共享顶点格式（24 bytes，Position:float3 + Color32:4B + UV:float2），`StructLayout.Sequential` |
| `RenderLayer.cs` | — | 渲染层枚举（`Normal = 0`, `Additive = 1`） |
| `RenderSortingOrder.cs` | — | 渲染排序常量（BulletNormal=100, BulletAdditive=110, LaserDefault=120, VFXNormal=200, VFXAdditive=210, DamageNumber=300） |
| `RenderBatchManager.cs` | 306 | [Phase 1] 渲染批次管理器——按 `(RenderLayer, Texture2D)` 二元组分桶，每桶一个 Mesh + 一个 DrawCall |

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
// 1. 构建预热 Key 列表（来自 TypeRegistry）
var keys = new List<RenderBatchManager.BucketKey>();
foreach (var bulletType in typeRegistry.BulletTypes)
{
    keys.Add(new(RenderLayer.Normal, bulletType.SourceTexture));
    if (bulletType.IsAdditive)
        keys.Add(new(RenderLayer.Additive, bulletType.SourceTexture));
}

// 2. 初始化
var batchManager = new RenderBatchManager();
batchManager.Initialize(keys, normalMaterial, additiveMaterial, maxQuadsPerBucket: 2048);

// 3. 每帧
batchManager.ResetAll();
// ... 逐弹丸/特效 TryGetBucket + AllocateQuad + 写顶点 ...
batchManager.UploadAndDrawAll();

// 4. 销毁
batchManager.Dispose();
```
