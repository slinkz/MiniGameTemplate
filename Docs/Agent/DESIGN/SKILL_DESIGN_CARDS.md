---
system: role-agent
scope: skill-design-cards
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SHOOTER_GAME/GDD/SG_GDD_01_ACTIVE_SKILLS.md, Docs/Agent/SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md
---

# Skill Design Cards

> 定位：技能设计卡片。详细数值仍以 `SHOOTER_GAME/GDD/SG_GDD_01_ACTIVE_SKILLS.md` 和对应 SO 为准。

## 卡片模板

```text
Skill Card
- 名称 / 类型：主动、被动、普攻升级
- 目标体验
- 触发方式：自动 / 周期 / 条件
- 核心参数：CD、前摇、后摇、伤害、范围、持续时间
- 配置入口：SkillConfigSO / BulletPatternSO / BuffConfigSO
- UI 表现：技能槽、CD、图标、解锁提示
- 资产需求：子弹、VFX、SFX、图标
- 验收剧本
```

## 当前技能簇

| 技能 | 目标体验 | 主要检查 |
|------|----------|----------|
| 直射普攻 | 稳定输出基线 | Slot[0]、CD、子弹速度、命中 |
| 散射弹幕 | 扇形清场 | 角度、数量、同屏压力 |
| 激光 | 高穿透、仪式感 | 持续时间、命中频率、渲染性能 |
| 导弹 | 自动索敌 | 目标选择、追踪、爆炸 VFX |
| 僚机 | 火力成长 | 召唤、跟随、退场清理 |
| 护盾 | 安全窗口 | 持续时间、UI、受击反馈 |
| 轨道炮 | 高爆发 | 前摇提示、伤害、音画同步 |

## 新增技能检查

1. 是否符合自动战斗，不要求玩家手动释放？
2. 是否能用现有 Skill Effect / BulletPattern / Buff 链路表达？
3. 是否给出 UI、VFX、SFX 和图标需求？
4. 是否定义 Retry/Exit 后的清理行为？
5. 是否给出对照基线：比普攻强在哪里，代价是什么？

