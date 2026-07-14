---
system: knowledge-engineering
scope: module-card-rendering-runtimeatlas
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/Danmaku_Rendering.md
---

# Module Card: Rendering_RuntimeAtlas

## 1. 模块职责

Rendering/RuntimeAtlas 提供统一渲染基础设施：RenderBatchManager、RenderVertex、RuntimeAtlasSystem、动态图集通道、材质桶、Mesh 上传、DrawCall 统计，以及 Bullet/Laser/VFX/Trail/DamageNumber 的合批基础。

## 2. 不负责什么

- 不决定业务对象何时生成或销毁。
- 不负责弹幕运动、碰撞、技能逻辑。
- 不负责 FairyGUI UI 渲染。
- 不替业务系统兜底错误配置；资源描述和 SO 仍需正确。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `RenderBatchManager` | 分桶、Mesh 数据、上传与绘制 |
| `RenderVertex` | CPU/GPU 顶点结构契约 |
| `RuntimeAtlasSystem/Manager` | 动态图集、Page、Channel、Allocation |
| `RuntimeAtlasBlit` Shader | 将源纹理写入 Atlas RT |
| `RenderBatchManagerRuntimeStats` | DC/统计 |
| `FloatingTextSystem` | RBM 飘字渲染使用方 |
| `VFXBatchRenderer` | VFX 渲染使用方 |

## 4. 数据流

```text
Resource Descriptor / Texture
  -> RuntimeAtlas Allocate
  -> Blit 写入 Atlas RT
  -> Renderer 获取 UV/Texture
  -> RenderBatchManager 写入 RenderVertex
  -> Upload Mesh
  -> Graphics.DrawMesh
  -> RuntimeStats 统计
```

## 5. 生命周期

```text
Initialize/WarmUp -> Lazy Create Page -> Allocate/Blit -> Per-frame Rebuild -> UploadAndDrawAll -> Cleanup/Release
```

Atlas 采用懒建页策略，避免微信小游戏启动时浪费 RT 内存。

## 6. 依赖关系

Rendering 是渲染基础层，尽量保持零业务依赖。Danmaku、VFX、FloatingText 等系统使用 Rendering，但 Rendering 不反向依赖这些业务系统。

## 7. 关键 SO / 配置路径

```text
Assets/_Game/Configs/ShooterGame/BulletPattern/
Assets/_Game/Configs/ShooterGame/BulletType/
Assets/_Game/Configs/ShooterGame/LaserTypes/
Assets/_Game/Configs/_Template/BulletPattern/
Assets/_Game/Configs/_Template/BulletType/
```

## 8. 关键 ADR

- ADR-002：BatchManager 共享实现不共享实例。
- ADR-028：RuntimeAtlasSystem 统一管线。
- ADR-031：RuntimeAtlas 深化，懒建页、Channel、Trail 入 Atlas。
- ADR-032：`new Material()` shaderKeywords。
- ADR-036：飘字系统统一到 RBM。

## 9. 热路径 / 性能约束

- Mesh 顶点写入、Rebuild、Upload 热路径零 GC。
- VertexAttributeDescriptor 顺序必须符合 Unity 标准顺序，并与 `RenderVertex` 字段顺序一致。
- DrawMesh 跨 RBM 层级控制依赖 `material.renderQueue`，不要依赖代码调用顺序。
- RuntimeAtlas Blit 路径必须兼容 WebGL 2.0。

## 10. 常见错误

- 看到 DrawCall 就认为可见，忽略 UV/纹理/alpha/shader。
- 顶点结构体字段顺序与 GPU 声明不一致。
- CommandBuffer Blit 错用 `UnityObjectToClipPos`。
- Atlas allocation 有效但 RT 区域全透明。
- 新材质 shaderKeywords 丢失导致渲染状态错误。

## 11. 修改前必读

- `CONTEXT_PACKS/Danmaku_Rendering.md`
- `ATLAS_TDD_INDEX.md`
- `DEBUG_PLAYBOOK.md`
- `FLOATING_TEXT_TDD.md`
- `ADR_03_RENDERING.md`, `ADR_05_RECENT.md`, `ADR_06_LIFECYCLE.md`

## 12. 修改后必验

- 读取运行时 bucket、quad、mesh、material、mainTexture。
- 对 RuntimeAtlas RT 做像素采样或等价验证。
- 检查 Game View 截图和实际可见性。
- 检查 `Marshal.OffsetOf` 或等价方式确认顶点布局。
- Profiler 检查 DrawCall、GC、RT 内存。
- 微信 WebGL 真机路径不出现透明/丢贴图/黑屏。
