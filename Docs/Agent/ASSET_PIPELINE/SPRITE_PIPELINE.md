---
system: role-agent
scope: sprite-pipeline
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/DESIGN/ENEMY_DESIGN_CARDS.md, Docs/Agent/DESIGN/SKILL_DESIGN_CARDS.md
---

# Sprite Pipeline

> 定位：飞机、敌机、子弹、道具和背景 sprite 的生产与接入。

## 规格建议

| 类型 | 尺寸 | 备注 |
|------|------|------|
| 玩家飞机 | 64 x 64 | 可预留倾斜帧 |
| 普通敌 | 48 x 48 | 轮廓清楚 |
| 精英敌 | 64 x 64 | 与普通敌明显不同 |
| 子弹 | 10-16 x 10-20 | 高对比，带发光感 |
| 道具 | 24 x 24 | 颜色编码清楚 |
| 背景 | 750 x 1334 或可循环 | 不抢 HUD 和弹幕 |

## 流程

1. 从设计卡片确认用途和可读性要求。
2. 生成或绘制透明 PNG。
3. 按命名规范放入目录。
4. 设置 Texture Type = Sprite，PPU 与项目一致。
5. 接入 EntityConfigSO / BulletTypeSO / PickupConfigSO。
6. 在 PlayMode 或预览场景验证大小、碰撞、可读性。
7. 更新 Asset Manifest。

## 验收

- 1x 尺寸下可辨认。
- 与背景/敌我阵营有足够对比。
- 碰撞半径和视觉大小一致。
- Atlas/压缩后无明显毛边。

