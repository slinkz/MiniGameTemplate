---
system: knowledge-engineering
scope: module-card-vfx-system
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/Danmaku_Rendering.md
---

# Module Card: VFXSystem

## 1. 模块职责

VFXSystem 负责 Sprite Sheet 帧动画特效的轻量运行时播放。它使用 Procedural Mesh、ScriptableObject 配置和共享 Rendering/RuntimeAtlas 能力，面向微信小游戏 / WebGL 的低 DrawCall、低内存、可控特效管线。

## 2. 不负责什么

- 不决定弹幕、技能、碰撞或战斗规则，只响应外部调用播放/停止特效。
- 不直接拥有 DanmakuSystem 的 Update/LateUpdate 编排；R4.0 后由 DanmakuSystem 管线驱动 `TickVFX()` / `RenderVFX()`。
- 不替代 RuntimeAtlas / RenderBatchManager 的底层合批实现。
- 不在 ScriptableObject 中保存场景对象引用。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `SpriteSheetVFXSystem` | 唯一 MonoBehaviour 入口，纯 API 入口，暴露 Play/Stop/TickVFX/RenderVFX |
| `VFXTypeSO` | 特效类型配置，包含 SourceTexture、UVRect、帧动画参数 |
| `VFXRenderConfig` | 渲染配置，包含模板材质、fallback 贴图、RuntimeAtlasConfig |
| `VFXTypeRegistry` | 运行时类型注册表，懒注册，避免 SO 运行时索引失效 |
| `VFXPool` | 预分配实例池 |
| `VFXBatchRenderer` | 通过 RenderBatchManager 按桶提交渲染 |
| `VFXInstance` | 单个 VFX 实例运行时数据 |
| `IVFXPositionResolver` | 附着目标位置解析接口 |

## 4. 数据流

```text
Danmaku / Skill / Game API
  -> SpriteSheetVFXSystem.Play / PlayAttached
  -> VFXPool 分配 VFXInstance
  -> TickVFX 更新帧、寿命、附着位置
  -> VFXBatchRenderer 提交 RenderBatchManager
  -> RuntimeAtlas / SourceTexture / fallback 纹理链路
  -> RenderVFX 绘制
```

## 5. 生命周期

```text
Initialize -> Play/PlayAttached -> TickVFX -> RenderVFX -> Stop/Expire -> Recycle -> Clear
```

退场、重试或清屏路径必须停止附着 VFX、清空池状态，并避免旧 attachSourceId 在下一局复用时串线。

## 6. 依赖关系

VFXSystem 依赖 Rendering/RuntimeAtlas。DanmakuSystem 通过 `IDanmakuVFXRuntime` 和 `IDanmakuEffectsBridge` 桥接到 VFXSystem；VFXSystem 本身不应反向依赖弹幕业务规则。

## 7. 关键配置 / 资产路径

```text
UnityProj/Assets/_Framework/VFXSystem/
UnityProj/Assets/_Framework/VFXSystem/Scripts/SpriteSheetVFXSystem.cs
UnityProj/Assets/_Framework/VFXSystem/Scripts/Config/*.cs
UnityProj/Assets/_Framework/VFXSystem/Scripts/Core/*.cs
UnityProj/Assets/_Framework/VFXSystem/Scripts/Data/*.cs
UnityProj/Assets/_Game/Configs/VFX/
UnityProj/Assets/_Game/Configs/Rendering/
```

## 8. 关键 ADR / 约束

- ADR-016：Danmaku 与 VFX 通过桥接解耦。
- ADR-028：RuntimeAtlas 统一渲染管线。
- R4.0：VFX 编排层统一到 DanmakuSystem 管线，`SpriteSheetVFXSystem` 不再自驱 Update/LateUpdate。
- WebGL 路径优先：避免 ParticleSystem 和热路径分配。

## 9. 热路径 / 性能约束

- `TickVFX()`、`RenderVFX()`、池分配与回收路径不能引入帧内 GC。
- `PlayAttached()` 去重以 `VFXTypeSO` 引用身份为准，不依赖可能重建的 RuntimeIndex。
- AtlasBinding、RuntimeAtlas、SourceTexture、fallback 的回退链要保持可诊断。
- 不在高频 VFX 播放路径做全量 registry rebuild。

## 10. 常见错误

- 只验证特效实例数量，未验证 Game View 是否可见。
- 修改 RuntimeAtlas 后忘记验证 VFX fallback 纹理链。
- 新增 VFXTypeSO 后未配置 SourceTexture、UVRect 或帧参数。
- 退场时只清弹幕，漏清附着 VFX。
- 把 DanmakuSystem 业务规则写回 VFXSystem。

## 11. 修改前必读

- `CONTEXT_PACKS/Danmaku_Rendering.md`
- `SO_WORKFLOWS_04_VFX_RENDER.md`
- `MODULE_CARDS/Rendering_RuntimeAtlas.md`
- `MODULE_CARDS/DanmakuSystem.md`
- `UnityProj/Assets/_Framework/VFXSystem/MODULE_README.md`

## 12. 修改后必验

- Play/PlayAttached/Stop/Expire 路径可运行。
- `TickVFX()` 和 `RenderVFX()` 由当前管线正确调用。
- Game View 中 VFX 可见，纹理/UV/alpha 正确。
- RuntimeAtlas、AtlasBinding、SourceTexture、fallback 回退链至少覆盖本次改动路径。
- 退场/重试后无 VFX 残留或 attachSourceId 串线。
- Profiler/日志确认热路径无新增 GC。
