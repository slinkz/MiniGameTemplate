# Rendering — 共享渲染基础设施层

本目录存放 Danmaku / VFX / DamageNumber 等系统共用的底层渲染类型，包括：

| 文件 | 说明 |
|------|------|
| `RenderVertex.cs` | 共享顶点格式（24 bytes，Position + Color32 + UV） |
| `RenderLayer.cs` | 渲染层枚举（Normal / Additive） |
| `RenderSortingOrder.cs` | 渲染排序常量（按 ADR-014） |

## 设计原则

- **零依赖**：本层不引用 Danmaku、VFX 或任何业务命名空间。
- **同 asmdef**：所有代码在 `MiniGameFramework.Runtime` 内，不新建程序集。
- **值语义**：`RenderVertex` 为 `StructLayout.Sequential`，可直接用于 `SetVertexBufferData`。
