---
name: game-designer
description: "MiniGameTemplate 策划 Agent 工作流。用于新增或修改关卡、敌人、技能、Buff、DOT、道具、掉落、经济、成长、广告入口、数值平衡、玩家动线和验收剧本；当任务需要从玩法体验出发产出 Design Brief、配置影响面、UI/资产/音频需求或交付给程序员的实现说明时触发。"
---

# Game Designer

## 使用流程

1. 先读 `Docs/Agent/ROLES/DESIGNER_BOOTSTRAP.md`。
2. 按任务读 `Docs/Agent/DESIGN/README.md` 中的专题文档。
3. 若涉及配置，读 `Docs/Agent/SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md` 和对应子文档。
4. 若涉及 UI 或资产，同步切到 `ROLES/UI_AGENT_BOOTSTRAP.md` 或 `ROLES/ART_ASSET_AGENT_BOOTSTRAP.md`。
5. 输出 Design Brief，不只输出想法。

## 任务路由

| 任务 | 必读 |
|------|------|
| 新增/调关卡 | `DESIGN/LEVEL_DESIGN_GUIDE.md`, `DESIGN/BALANCE_BASELINES.md` |
| 新敌人 | `DESIGN/ENEMY_DESIGN_CARDS.md`, `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md` |
| 新技能 | `DESIGN/SKILL_DESIGN_CARDS.md`, `SHOOTER_GAME/GDD/SG_GDD_INDEX.md` |
| Buff/DOT/道具 | `DESIGN/ITEM_BUFF_DESIGN_CARDS.md`, `SHOOTER_GAME/GDD/SG_GDD_02_PASSIVE_BUFFS.md`, `SHOOTER_GAME/GDD/SG_GDD_03_ITEMS_CONFIG.md` |
| 经济/广告/成长 | `DESIGN/ECONOMY_AND_PROGRESSION.md`, `PLATFORM/WECHAT_INTEGRATION.md` |

## Design Brief 模板

```text
Design Brief
- 目标体验：
- 玩家可感知变化：
- 规则/数值/配置改动：
- 涉及 SO / Luban / UI / VFX / Audio：
- 程序实现需求：
- 验收剧本：
- 风险与回滚：
```

## 必须检查

- 是否符合 `DESIGN/DESIGN_PILLARS.md`。
- 是否有配置入口，而不是只写抽象体验。
- 是否标出 UI、资产、音频需求。
- 是否给出 PlayMode/设备验收步骤。
- 是否需要更新 DESIGN、SO Workflow、Evals 或 changes。

