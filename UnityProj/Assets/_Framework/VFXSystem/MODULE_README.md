# VFXSystem

## 模块职责
- 负责 Sprite Sheet 帧动画特效的轻量运行时播放
- 采用 Procedural Mesh + ScriptableObject 配置，不依赖 ParticleSystem
- 面向微信小游戏 / WebGL，目标是低 DrawCall、零 GC、程序员可控

命名空间：`MiniGameTemplate.VFX`

## 目录约定
- `Scripts/Config/` — SO 配置（VFXTypeSO 含 SourceTexture/UVRect/SchemaVersion）
- `Scripts/Core/` — 运行时核心逻辑
- `Scripts/Data/` — 纯数据结构

## 架构说明
- `SpriteSheetVFXSystem`：唯一 MonoBehaviour 入口，负责生命周期与每帧驱动
- `VFXPool`：预分配实例池（64 容量），管理槽位分配/回收
- `VFXBatchRenderer`：通过 `RenderBatchManager`（`_Framework/Rendering/`）按 `(RenderLayer, SourceTexture)` 分桶渲染
- `VFXTypeSO`：设计师可编辑的特效配置资产（支持独立贴图 SpriteSheet，atlas 仅为可选优化）

## 与 DanmakuSystem 的关系 [Phase 2]

VFXSystem **不主动依赖** DanmakuSystem。两系统的联动通过桥接接口实现：

```
DanmakuSystem
  └── IDanmakuEffectsBridge (接口，在 Danmaku 命名空间)
        └── DefaultDanmakuEffectsBridge (唯一引用 VFX 命名空间的类)
              └── 调用 SpriteSheetVFXSystem.PlayOneShot / CanPlay
```

- 碰撞命中时，`CollisionSolver` 写入 `CollisionEventBuffer` → `EffectsBridge.OnCollisionEventsReady()` 消费 → 触发 VFX
- 清屏时，`ClearAllBulletsWithEffect()` 逐弹丸调用 `EffectsBridge.OnBulletCleared()` → 触发 VFX
- VFXSystem 本身不感知弹幕逻辑，仅暴露 `Play` / `PlayOneShot` / `CanPlay` 公开 API

## 约束
- 不在 ScriptableObject 中保存场景对象引用
- 不使用 `FindObjectOfType` / `GameObject.Find`
- 共享配置全部通过 Inspector 分配 SO / Material / Texture
- 渲染通过共享 `RenderBatchManager` 实例（VFX 自己持有独立实例）

## 重构进度

| Phase | 与 VFX 相关的改动 | 状态 |
|-------|------------------|------|
| Phase 0 | RenderVertex 迁移到共享层 | ✅ 已完成 |
| Phase 1 | VFXBatchRenderer 迁移到 BatchManager，VFXTypeSO 新增 SourceTexture 支持独立贴图 | ✅ 已完成 |
| Phase 2 | IDanmakuEffectsBridge 桥接解耦，VFX 不再被 Danmaku 直接引用 | ✅ 已完成 |
| Phase 3 | VFX 时间缩放、附着模式（World/FollowTarget）、喷雾可视化 | ⏳ 待开始 |
