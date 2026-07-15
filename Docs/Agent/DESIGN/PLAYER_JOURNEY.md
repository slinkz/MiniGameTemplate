---
system: role-agent
scope: player-journey
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_GAME_DESIGN.md, Docs/Agent/SG_UI_DESIGN.md, Docs/Agent/APPFLOW_TDD_INDEX.md
---

# Player Journey

> 定位：统一策划、UI 和程序对玩家动线的理解。

## 主路径

```text
Loading
  -> LevelSelect
  -> Battle Intro
  -> Playing
  -> Victory / Defeat / Pause
  -> LevelSelect / Retry
```

## 情绪目标

| 阶段 | 目标情绪 | 设计抓手 |
|------|----------|----------|
| Loading | 稳定、可信 | 简短品牌露出，不卡住 |
| LevelSelect | 有目标 | 当前可进入关卡高亮，已通关有进度感 |
| Battle Intro | 准备好了 | 飞机进场、血条出现、短暂停顿 |
| Playing | 专注又爽 | UI 少，反馈强，波次松紧明确 |
| Victory | 成就感 | 数据、星级、解锁下一关 |
| Defeat | 不气馁 | 强化重试，显示差一点的进度 |
| Pause | 可控 | 继续高优先级，退出需确认 |

## 设计变更影响

任何改变下面内容的任务，都要同时检查 UI 和 AppFlow：

- 进入战斗前是否新增选择或确认。
- 胜利后是否新增奖励、广告、翻倍、下一关。
- 失败后是否新增复活广告、重试成本、返回策略。
- 关卡解锁是否改变。
- 战斗中是否新增常驻 HUD 信息。

