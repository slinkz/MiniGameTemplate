---
system: role-agent
scope: enemy-design-cards
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SHOOTER_GAME/SG_GAME_DESIGN.md, Docs/Agent/SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md
---

# Enemy Design Cards

> 定位：敌人设计卡片。用于新增敌人、调整威胁组合和关卡投放。

## 卡片模板

```text
Enemy Card
- 名称 / 职责
- 玩家感知：慢、快、肉、会射击、压迫路线
- 核心参数：HP、速度、ContactDamage、碰撞半径
- 攻击方式：无 / 直射 / 散射 / 特殊
- 关卡投放：首次出现、常见组合、上限
- 资产需求：sprite、爆炸、命中、SFX
- 配置入口：EntityConfigSO、BulletPatternSO、Wave SO
- 验收剧本
```

## 当前敌人类型

| 敌人 | 职责 | 设计要点 |
|------|------|----------|
| 普通敌 | 教学和基线压力 | 低 HP、慢速、数量可堆 |
| 快速敌 | 打断安逸站位 | 速度高，数量不宜同时过多 |
| 射手敌 | 引入攻防兼备 | 弹幕清楚，不能淹没安全区 |
| 散射敌 | 制造横向走位 | 子弹角度和间隔要可读 |
| 精英敌 | 小高潮 | 更高 HP、掉落或奖励更明显 |

## 新增敌人检查

- 它解决了什么关卡节奏问题？
- 是否与已有敌人职责重复？
- 第一次出现是否有低压展示？
- 同屏数量上限是多少？
- 是否需要新的 sprite、子弹、爆炸或音效？

