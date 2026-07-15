---
system: role-agent
scope: item-buff-design-cards
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_GDD_02_PASSIVE_BUFFS.md, Docs/Agent/SG_GDD_03_ITEMS_CONFIG.md
---

# Item Buff Design Cards

> 定位：Buff、DOT、道具的设计卡片和检查表。

## ID 范围

| 类型 | 范围 |
|------|------|
| Buff 增益 | 1000-2999 |
| Debuff 减益 | 3000-3999 |
| DOT | 4000-4999 |
| 预留 | 5000-5999 |

## 卡片模板

```text
Item/Buff Card
- 名称 / 类型：Buff、Debuff、DOT、Pickup
- 目标体验
- 参数：持续时间、倍率、tick、叠加、刷新规则
- UI 表现：图标、倒计时、折叠 +N、拾取通知
- VFX/SFX：附着、命中、循环、结束
- 配置入口：BuffConfigSO / DotConfigSO / PickupConfigSO / DropTableSO
- 验收剧本
```

## 当前类别

| 类别 | 设计重点 |
|------|----------|
| 攻速/伤害 Buff | 火力明显增强，时间不要太短 |
| 护盾/防御 Buff | 受击反馈清楚，避免误以为无敌常驻 |
| 减速/脆弱 Debuff | 敌人状态需有视觉标记 |
| 燃烧/中毒/电弧 DOT | tick 频率、飘字、VFX 不要过吵 |
| 修复道具 | 低血量时价值明显 |
| 弹药/技能道具 | 强化短期爽感 |

## 验收重点

- 叠加、刷新、结束三种边界。
- Retry/Exit 后没有残留。
- UI 倒计时和真实持续时间一致。
- VFX 附着位置和对象回收正确。
- 掉落概率经过模拟或实测。

