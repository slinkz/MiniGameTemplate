# RuntimeAtlasSystem — 运行时动态图集基础设施

命名空间：`MiniGameTemplate.Rendering`

## 当前阶段
- Phase R0 已落地：
  - `AtlasChannel`
  - `AtlasAllocation`
  - `AtlasChannelConfig`
  - `Shelf`
  - `ShelfPacker`
  - `AtlasPage`
  - `AtlasBlit`
  - `RuntimeAtlasBlit.shader`
- Phase R1 已落地：
  - `RuntimeAtlasManager.Initialize(RuntimeAtlasConfig)` 配置驱动初始化
  - `WarmUp()` 返回本次预热新增 Blit 数
  - `TryGetAllocation()` / `GetPageCount()` 查询接口
  - `HandleRTLost()` + `RestoreDirtyPages()` 两阶段恢复
  - `RuntimeAtlasStats` 扩展到请求数 / 命中率 / overflow / pending restore
  - `RuntimeAtlasConfig.Validate()` 统一配置校验
- `RenderBatchManager` 已提前升级到 TDD v2.3 接口：
  - `BucketKey.Texture : Texture`
  - `BucketRegistration`
  - 注册时排序

## 文件说明

| 文件 | 说明 |
|------|------|
| `AtlasChannel.cs` | Atlas 通道枚举（当前按业务域隔离） |
| `AtlasAllocation.cs` | 运行时分配结果（PageIndex + UVRect） |
| `AtlasChannelConfig.cs` | 单通道配置（AtlasSize / Padding / MaxPages） |
| `Shelf.cs` | Shelf Packing 行结构 |
| `ShelfPacker.cs` | Best-Fit Shelf 算法 |
| `AtlasPage.cs` | 单页 RT 生命周期管理 |
| `AtlasBlit.cs` | WebGL 兼容的运行时拷贝层 |
| `RuntimeAtlasBlit.shader` | `Hidden/RuntimeAtlasBlit` 直通拷贝 Shader |
| `RuntimeAtlasManager.cs` | RuntimeAtlas 核心入口（已提前落地 R1 骨架） |
| `RuntimeAtlasConfig.cs` | 全局配置 SO |
| `RuntimeAtlasStats.cs` | 统计快照结构 |

## 已知说明

### 1. 为什么 R0 就出现了 `RuntimeAtlasManager`
严格按计划它属于 R1，但这次实现里提前把骨架一起落了。
原因很简单：
- `AtlasBlit` / `ShelfPacker` / `AtlasPage` 单独落地后没有可运行闭环
- 提前形成最小闭环，便于编译验证和后续 Phase R1/R2 直接接入

### 2. 为什么 R0.3 的单元测试还没补
TDD 原计划包含单元测试，但当前项目里没有现成的 Unity Test Framework 目录/惯例。
本次先保证：
- 算法实现完成
- Unity 编译通过
- 后续在引入测试基础设施时补齐 `ShelfPacker` 的边界用例

## 下一步
- 待天命人验收通过后进入 Phase R2
- Phase R2 重点：
  - BulletRenderer 接入 RuntimeAtlas
  - LaserRenderer / LaserWarningRenderer 统一到全局 RBM（保持独立贴图）
  - VFXBatchRenderer 接入 RuntimeAtlas
  - 验证统一 RBM 初始化链路
