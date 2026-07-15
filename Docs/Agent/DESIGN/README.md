---
system: role-agent
scope: design-index
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/DESIGNER_BOOTSTRAP.md, Docs/Agent/SG_GAME_DESIGN.md, Docs/Agent/SG_GDD_INDEX.md
---

# Design Knowledge Index

> 定位：策划 Agent 的设计知识路由。长 GDD 仍是事实源，本目录负责把高频设计任务变成可交付卡片和 SOP。

## 路由

| 我要做什么 | 读什么 |
|------------|--------|
| 确认设计方向 | `DESIGN_PILLARS.md`, `PLAYER_JOURNEY.md` |
| 改关卡/波次 | `LEVEL_DESIGN_GUIDE.md`, `BALANCE_BASELINES.md` |
| 新增技能 | `SKILL_DESIGN_CARDS.md`, `SG_GDD_01_ACTIVE_SKILLS.md` |
| 新增敌人 | `ENEMY_DESIGN_CARDS.md`, `SO_WORKFLOWS_02_ENTITY.md` |
| 新增 Buff/DOT/道具 | `ITEM_BUFF_DESIGN_CARDS.md`, `SG_GDD_02_PASSIVE_BUFFS.md`, `SG_GDD_03_ITEMS_CONFIG.md` |
| 改成长/奖励/广告 | `ECONOMY_AND_PROGRESSION.md`, `WECHAT_INTEGRATION.md` |

## 策划卡片最小格式

```text
- 目标体验
- 规则/数值
- 配置入口
- UI/资产/音频需求
- 验收剧本
- 风险和回滚
```

## 维护规则

- GDD/TDD 改动后，如影响玩家体验或数值基准，同步本目录对应卡片。
- 若设计只是未来方向，标记 `status: proposed`，不要当成当前事实。
- 新增配置字段时，同步 `SO_WORKFLOWS_*` 或在卡片里指向待补项。

