---
system: role-agent
scope: game-designer-bootstrap
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/DESIGN/README.md, Docs/Agent/SG_GAME_DESIGN.md, Docs/Agent/SG_GDD_INDEX.md, Docs/Agent/SO_WORKFLOWS_INDEX.md
---

# Designer Agent Bootstrap

> 定位：策划 Agent 的上岗入口。用于玩法、关卡、敌人、技能、Buff、道具、经济和数值任务。

## 1. 先读顺序

| 场景 | 必读 |
|------|------|
| 任意策划任务 | `DESIGN/README.md`, `DESIGN/DESIGN_PILLARS.md` |
| 新增/修改关卡 | `DESIGN/LEVEL_DESIGN_GUIDE.md`, `DESIGN/BALANCE_BASELINES.md`, `SO_WORKFLOWS_INDEX.md` |
| 新增敌人 | `DESIGN/ENEMY_DESIGN_CARDS.md`, `SO_WORKFLOWS_02_ENTITY.md` |
| 新增技能 | `DESIGN/SKILL_DESIGN_CARDS.md`, `SG_GDD_INDEX.md`, `SO_WORKFLOWS_02_ENTITY.md` |
| 新增 Buff/DOT/道具 | `DESIGN/ITEM_BUFF_DESIGN_CARDS.md`, `SG_GDD_02_PASSIVE_BUFFS.md`, `SG_GDD_03_ITEMS_CONFIG.md` |
| 影响 UI/资产 | `UI_AGENT_BOOTSTRAP.md`, `ART_ASSET_AGENT_BOOTSTRAP.md` |

## 2. 策划交付物

任何设计变更至少交付：

```text
Design Brief
- 目标体验
- 玩家可感知变化
- 规则/数值/配置改动
- 涉及 SO / Luban / UI / VFX / Audio
- 关卡或战斗验收剧本
- 风险、回滚方式、是否需要程序支持
```

## 3. 判断边界

| 可以直接做 | 需要程序员 Agent |
|------------|------------------|
| 调整 SO 数值、关卡波次、掉落权重、UI 文案草案 | 新增组件、改伤害链路、改 AppFlow、改验证器 |
| 输出技能/敌人/Buff 设计卡 | 新增通用机制或公共接口 |
| 写资产需求和验收剧本 | 接入新渲染管线、云存储、微信平台能力 |

## 4. 核心禁止事项

- 不允许只写“感觉更难/更爽”，必须落到可调参数或验收现象。
- 不允许绕开设计支柱：当前 ShooterGame 是自动战斗、移动走位、火力渐强。
- 不允许引用 Archive 旧方案作为当前事实。
- 不允许新增资源需求却不写尺寸、命名、用途和接入点。
- 不允许改关卡/技能/Buff 后不给 PlayMode 或设备验收剧本。

## 5. 验收口径

策划验收至少回答：

1. 玩家会看到什么变化？
2. 哪个 SO 或表格承载该变化？
3. 关卡/战斗中用什么步骤复现？
4. 数值是否仍满足 `BALANCE_BASELINES.md`？
5. 是否需要 UI、VFX、音频或资产同步？

