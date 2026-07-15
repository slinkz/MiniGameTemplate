---
system: role-agent
scope: screen-cards
status: active
created: 2026-07-15
last_updated: 2026-07-15
related_docs: Docs/Agent/SG_UI_DESIGN.md, Docs/Agent/APPFLOW_TDD_INDEX.md
---

# Screen Cards

> 定位：界面卡片。新增界面或改流程时先更新这里。

## LoadingScreen

- 类型：UIScreen。
- 目标：品牌露出 + 加载进度。
- 数据：加载进度。
- 验收：最短展示、自动跳转、进度条不倒退。

## LevelSelectScreen

- 类型：UIScreen。
- 目标：显示关卡进度和下一目标。
- 状态：cleared、available、locked。
- 数据：关卡解锁、星级、当前选择。
- 验收：锁定不可进入，可进入可点击，胜利后解锁动画。

## BattleHUD

- 类型：UILayer。
- 目标：少量信息，不干扰战斗。
- 数据：HP、Wave、Skill CD、Buff、KillCount、InputDirection。
- 验收：触摸穿透、暂停按钮优先、血条/波次/飘字正常。

## SortieBottomSheet

- 类型：UIPanel 或 BottomSheet。
- 目标：出战前选择技能/被动。
- 数据：已解锁技能、选中组合、战力提示。
- 验收：打开/关闭、选择、确认、返回、未解锁提示。

## PausePanel

- 类型：UIPanel。
- 目标：继续优先，退出需确认。
- 数据：无或当前关卡。
- 验收：TimeScale 冻结、继续恢复、退出返回选关。

## VictoryPanel

- 类型：UIPanel。
- 目标：成就反馈，推进下一关。
- 数据：击杀数、剩余 HP、星级、奖励。
- 验收：延迟弹出、确定返回、进度保存、解锁下一关。

## DefeatPanel

- 类型：UIPanel。
- 目标：降低挫败，强化重试。
- 数据：击杀/总数、失败原因、复活机会。
- 验收：再试一次不重载场景、返回选关、数据重置。

