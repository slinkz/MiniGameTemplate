# VFXSystem

## 模块职责
- 负责 Sprite Sheet 帧动画特效的轻量运行时播放
- 采用 Procedural Mesh + ScriptableObject 配置，不依赖 ParticleSystem
- 面向微信小游戏 / WebGL，目标是低 DrawCall、零 GC、程序员可控

## 目录约定
- `Scripts/Config/` — SO 配置
- `Scripts/Core/` — 运行时核心逻辑
- `Scripts/Data/` — 纯数据结构

## 架构说明
- `SpriteSheetVFXSystem`：唯一 MonoBehaviour 入口，负责生命周期与每帧驱动
- `VFXPool`：预分配实例池，管理槽位分配/回收
- `VFXBatchRenderer`：双 Mesh 合批渲染（Normal + Additive）
- `VFXTypeSO`：设计师可编辑的特效配置资产（阶段 1 基于共享图集 UV）


## 约束
- 不在 ScriptableObject 中保存场景对象引用
- 不使用 `FindObjectOfType` / `GameObject.Find`
- 共享配置全部通过 Inspector 分配 SO / Material / Texture
- 阶段 1 仅实现 Sprite Sheet 播放，不接入弹幕碰撞事件
