---
system: knowledge-engineering
scope: context-pack-danmaku-rendering
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: Danmaku Rendering

## 适用任务

- 修改弹丸、激光、喷雾、Trail、VFX、飘字、RuntimeAtlas、RenderBatchManager。
- 排查画面不显示、贴图透明、UV 错位、DrawCall 异常、性能异常。
- 新增弹幕花样、BulletType、BulletPattern、VFXType、Atlas 通道。

## 必读文档

| 目的 | 文档 |
|------|------|
| 全局渲染/弹幕架构 | `ARCHITECTURE.md` 中统一渲染管线与 DanmakuSystem |
| RuntimeAtlas | `SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX.md`, `SYSTEMS/ATLAS_TDD/ATLAS_TDD_01_DESIGN.md`, `SYSTEMS/ATLAS_TDD/ATLAS_TDD_02_IMPL.md` |
| 弹幕 SO 工作流 | `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU.md` |
| VFX/Rendering SO | `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_04_VFX_RENDER.md` |
| 飘字系统 | `SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md` |
| OBB/Hitbox | `SYSTEMS/OBB_TDD/OBB_TDD_INDEX.md` |
| Debug 方法 | `DEBUG_PLAYBOOK.md` |
| ADR | `ADR/ADR_INDEX.md` 中 ADR-028、031、032、036 |
| 模块卡 | `MODULE_CARDS/DanmakuSystem.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md`, `MODULE_CARDS/VFXSystem.md` |

## 关键代码入口

```text
UnityProj/Assets/_Framework/DanmakuSystem/
UnityProj/Assets/_Framework/Rendering/
UnityProj/Assets/_Framework/VFXSystem/
UnityProj/Assets/_Framework/**/RuntimeAtlas*/
UnityProj/Assets/_Game/Configs/ShooterGame/
UnityProj/Assets/_Game/Configs/_Template/
```

## 关键 SO / 配置路径

```text
Assets/_Game/Configs/ShooterGame/
Assets/_Game/Configs/_Template/
```

常见 SO：`BulletTypeSO`、`BulletPatternSO`、`PatternGroupSO`、`SpawnerProfileSO`、`VFXTypeSO`、RuntimeAtlas/Rendering 配置。

## 关键 ADR / 约束

- ADR-028：RuntimeAtlasSystem 统一管线。
- ADR-031：RuntimeAtlas 深化，懒建页、Channel、Trail 迁移。
- ADR-032：`new Material()` shaderKeywords 坑。
- ADR-036：飘字系统统一到 RBM 渲染管线。

渲染约束：

- DrawCall 存在不代表画面可见，必须验证顶点、UV、纹理像素和 shader 输出。
- 自定义 Mesh 顶点布局必须与 Unity 标准顺序和 CPU 结构体字段一致。
- CommandBuffer fullscreen blit 不应盲目使用 `UnityObjectToClipPos`。
- RuntimeAtlas 问题最终要读像素或采样 RT 证据。

## 常见坑

- 只看 DrawCall，不看 Atlas 是否真的有非透明像素。
- 修掉一个警告就误判问题结束。
- RuntimeAtlas allocation valid 但 Blit 没写入。
- material/renderQueue 层序依赖代码调用顺序，而不是 GPU 队列。
- 修改弹幕配置后忘记 Atlas 纹理、SO 资源描述和 Validator。

## 修改后必验

- Game View 截图或运行时可见性验证。
- active count、bucket、quad、mesh、material.mainTexture 数据链路验证。
- RuntimeAtlas RT 像素采样或等价证据。
- 自定义 Mesh 修改时检查 VertexAttributeDescriptor 与结构体字段顺序。
- Profiler 检查 DC、GC、RT 内存。
- 微信 WebGL 兼容路径不依赖不可用图形 API。
