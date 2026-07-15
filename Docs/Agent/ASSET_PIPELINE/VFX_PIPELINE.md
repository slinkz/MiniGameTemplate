---
system: role-agent
scope: vfx-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: skills/vfx-creator/SKILL.md, Docs/Agent/MODULE_CARDS/VFXSystem.md
---

# VFX Pipeline

> 定位：Sprite Sheet VFX 的生产、接入和验收。详细操作优先读 `skills/vfx-creator/SKILL.md`。

## 适用

- 爆炸、命中、烟雾、治疗、Buff 光环、短促装饰特效。

不适用：

- 大量持续存在的子弹主体、激光主体、喷雾主体。这些优先走 Danmaku 渲染链。

## 流程

1. 定义验证样本：颜色/轮廓/尺寸/帧节奏必须明显。
2. 生成 sprite sheet。
3. 导入 Unity，设置材质和切帧。
4. 创建 `VFXTypeSO`。
5. 注册到 `VFXTypeRegistrySO`。
6. 接到 `SpriteSheetVFXSystem` 或业务触发链路。
7. 在 VFXDemo、DanmakuDemo 或目标业务链路验证。
8. 更新 Manifest 和必要文档。

## 验收顺序

1. 接线是否完整。
2. 类型选择是否正确。
3. Registry/runtime index 是否正确。
4. 材质、层级、Blend 是否正确。
5. 肉眼是否能看出目标效果。

