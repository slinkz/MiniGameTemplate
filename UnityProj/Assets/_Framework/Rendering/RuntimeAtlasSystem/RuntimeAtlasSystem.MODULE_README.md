# RuntimeAtlasSystem — 运行时动态图集基础设施

命名空间：`MiniGameTemplate.Rendering`

## 模块定位

RuntimeAtlasSystem 是**统一渲染管线的核心基础设施**——借鉴 RVT（Runtime Virtual Texture）按需生成思想，运行时将源纹理 Blit 到动态 Atlas RenderTexture，替代此前割裂的 6 路渲染架构。

**核心特性**：
- Shelf Packing 算法，支持混合尺寸纹理（32×32 ~ 256×256）
- Channel 隔离（Bullet / VFX / DamageText / Laser / Trail / Character）
- 缓存命中零 GC、切关清空、RT Lost 自动恢复
- WebGL 2.0 兼容（CommandBuffer.Blit，不依赖 Graphics.CopyTexture）

**设计文档**：`Docs/Agent/RUNTIME_ATLAS_SYSTEM_TDD.md`（v2.10.1）

## 实施状态：Phase R0 ~ R4 全部验收通过 ✅

| Phase | 内容 | 关键产物 |
|-------|------|---------|
| **R0** | 基础设施 | AtlasChannel / AtlasAllocation / AtlasChannelConfig / Shelf / ShelfPacker / AtlasPage / AtlasBlit / RuntimeAtlasBlit.shader |
| **R1** | 配置驱动管理器 | RuntimeAtlasManager.Initialize(Config) / WarmUp / TryGetAllocation / HandleRTLost + RestoreDirtyPages / RuntimeAtlasStats / RuntimeAtlasConfig.Validate |
| **R2** | Renderer 迁移 | RuntimeAtlasBindingResolver（三路解析）/ BulletRenderer + VFXBatchRenderer 优先 RuntimeAtlas / Laser + LaserWarning 保持独立贴图走统一 RBM |
| **R3** | 自管 Mesh 迁移 | DamageNumberSystem → RBM + RuntimeAtlas(DamageText) / TrailPool 方案 A（独立 Mesh + 接入统计）/ DanmakuSystem 管线统一调度 |
| **R4** | VFX 编排层统一 | SpriteSheetVFXSystem 退化为纯 API 入口，DanmakuSystem 管线通过 IDanmakuVFXRuntime.TickVFX/RenderVFX 统一驱动 / Detached Spray 世界空间 VFX 回归修复 |

`RenderBatchManager` 在 R0 阶段提前升级到 TDD v2.3 接口（`BucketKey.Texture : Texture` 基类、`BucketRegistration` 多模板材质、注册时排序）。

## 文件清单

| 文件 | 说明 |
|------|------|
| `AtlasChannel.cs` | Atlas 通道枚举（按业务域隔离） |
| `AtlasAllocation.cs` | 分配结果值类型（PageIndex + UVRect） |
| `AtlasChannelConfig.cs` | 单通道配置（AtlasSize / Padding / MaxPages） |
| `Shelf.cs` | Shelf Packing 行结构 |
| `ShelfPacker.cs` | Best-Fit Shelf 算法（O(N) 搜索，零 GC） |
| `AtlasPage.cs` | 单页 RT 生命周期管理（IDisposable） |
| `AtlasBlit.cs` | WebGL 兼容的 GPU Blit 适配层 |
| `RuntimeAtlasBlit.shader` | `Hidden/RuntimeAtlasBlit` 直通拷贝 Shader（NDC passthrough，不依赖 VP 矩阵） |
| `RuntimeAtlasManager.cs` | 核心入口（Allocate / WarmUp / Reset / HandleRTLost / GetStats） |
| `RuntimeAtlasConfig.cs` | 全局配置 SO（6 个 Channel 独立配置） |
| `RuntimeAtlasStats.cs` | 统计快照（页面数 / 命中率 / 内存 / overflow / pending restore） |
| `RuntimeAtlasBindingResolver.cs` | 统一纹理解析辅助层（RuntimeAtlas → AtlasBinding → SourceTexture 回退链） |

## 已知说明

### 1. 为什么 R0 就出现了 `RuntimeAtlasManager`
严格按 TDD 它属于 R1，但实现时提前落了骨架——`AtlasBlit` / `ShelfPacker` / `AtlasPage` 单独落地后没有可运行闭环，提前形成最小闭环便于编译验证和后续 Phase 直接接入。

### 2. ShelfPacker 单元测试待补
算法实现已完成，但项目当前无 Unity Test Framework 基础设施。后续引入测试框架时需补齐 `ShelfPacker` 边界用例。

### 3. RuntimeAtlasBlit.shader 的 NDC passthrough
**踩坑经验**：在 CommandBuffer + SetRenderTarget 上下文中，`UnityObjectToClipPos` 依赖的 VP 矩阵不可控（可能是上一帧 Camera 的值），会导致全屏 quad 被变换到错误位置。必须直接 passthrough NDC 坐标：`o.vertex = float4(v.vertex.xy, 0, 1)`。

## 后续可选优化

| 项目 | 优先级 | 说明 |
|------|--------|------|
| Editor 预览窗口 | P2 | `RuntimeAtlasDebugWindow.cs` — 可视化 Atlas 占用情况 |
| Debug HUD 接入 | ✅ 已完成 (R4.3) | `DanmakuDebugHUD` 已新增 RuntimeAtlas section（页数/分配/填充率/内存/命中率/overflow），0.5s 刷新间隔 |
| 真机 WebGL 验收 | P0 | 微信小游戏环境下 RT Lost 恢复 + 性能指标 |
| ShelfPacker 单元测试 | P2 | 引入 Unity Test Framework 后补齐 |
