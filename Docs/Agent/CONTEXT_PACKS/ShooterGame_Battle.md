---
system: knowledge-engineering
scope: context-pack-shootergame-battle
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: ShooterGame Battle

## 适用任务

- 修改 ShooterGame 战斗流程、关卡、胜负、重试、退场清理。
- 修改玩家输入、基地血量、底线检测、波次、击杀统计。
- 修改技能系统、道具、Buff/DOT、被动、战果结算。
- 做 ShooterGame 相关架构评审或验收。

## 必读文档

| 目的 | 文档 |
|------|------|
| 游戏设计 | `SHOOTER_GAME/SG_GAME_DESIGN.md` |
| UI/交互设计 | `SHOOTER_GAME/SG_UI_DESIGN.md` |
| V1 技术入口 | `SHOOTER_GAME/TDD/SG_TDD_INDEX.md` |
| 战斗系统 | `SHOOTER_GAME/TDD/SG_TDD_02_BATTLE_SYSTEM.md` |
| 关卡进度 | `SHOOTER_GAME/TDD/SG_TDD_03_LEVEL_PROGRESS.md` |
| UI Controller | `SHOOTER_GAME/TDD/SG_TDD_04_UI_CONTROLLERS.md` |
| 输入摇杆 | `SHOOTER_GAME/TDD/SG_TDD_05_INPUT_JOYSTICK.md` |
| 云存储 | `SHOOTER_GAME/TDD/SG_TDD_06_CLOUD_SAVE.md` |
| V2 技能系统入口 | `SHOOTER_GAME/V2_TDD/SG_V2_TDD_INDEX.md` |
| 退场生命周期 | `SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md` |
| 统一设备验收 | `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` |
| AppFlow | `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md`, `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分 |

## 关键代码入口

```text
UnityProj/Assets/_Game/Scripts/ShooterGame/
UnityProj/Assets/_Game/Scripts/GameStartupFlow.cs
UnityProj/Assets/_Game/Scenes/Main.unity
UnityProj/Assets/_Game/Configs/ShooterGame/
UIProject/assets/SG_*/
```

常见映射：

| 代码/资产 | 先读 |
|-----------|------|
| `Core/BattleController.cs` | `SHOOTER_GAME/TDD/SG_TDD_02_BATTLE_SYSTEM.md`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md` |
| `Progress/SG_ProgressManager.cs` | `SHOOTER_GAME/TDD/SG_TDD_03_LEVEL_PROGRESS.md`, `SHOOTER_GAME/TDD/SG_TDD_06_CLOUD_SAVE.md` |
| `Config/SG_LevelConfigSO.cs` | `SHOOTER_GAME/TDD/SG_TDD_03_LEVEL_PROGRESS.md`, `SHOOTER_GAME/SG_GAME_DESIGN.md` |
| UI Controllers | `SHOOTER_GAME/TDD/SG_TDD_04_UI_CONTROLLERS.md`, `SHOOTER_GAME/SG_UI_DESIGN.md` |
| Joystick/Input Bridge | `SHOOTER_GAME/TDD/SG_TDD_05_INPUT_JOYSTICK.md` |
| Skills/Pickups/Buffs | `SG_V2_TDD_02~03`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md` |
| `Configs/ShooterGame/Levels/**`, `Configs/ShooterGame/Waves/**` | `SHOOTER_GAME/SG_GAME_DESIGN.md`, `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md` |

## 关键 SO / 配置路径

```text
UnityProj/Assets/_Game/Configs/ShooterGame/
UnityProj/Assets/_Game/Configs/ShooterGame/Levels/
UnityProj/Assets/_Game/Configs/ShooterGame/Waves/
UnityProj/Assets/_Game/Configs/ShooterGame/Variables/
UnityProj/Assets/_Game/Resources/ShooterGame/
```

核心资产包括：`SG_Player`、`SG_Base`、`SG_Enemy_Normal`、`SG_Enemy_Fast`、`SG_Level_01~05`、`SG_Wave_01~05`、`SG_CurrentLevelIndex`、`SG_BaseHP`、技能/Buff/DOT/Passive/Pickup 相关 SO。

## 关键 ADR / 约束

- ADR-033：Entity-Component 框架。
- ADR-034：AppFlow 栈式导航。
- ADR-035：战斗退场生命周期统一事件通道。
- ADR-036：飘字系统统一到 RBM 渲染管线。

业务约束：

- 玩家飞机不可被摧毁，基地扣血来源是敌机突破底线。
- 战斗状态机为 Intro -> Playing -> Victory/Defeat。
- 关卡流转应通过 AppFlow/SceneLoader/UIManager 约定流程。
- 退出、重试、返回主菜单必须清理战斗残留。

## 常见坑

- 只重置 UI，没有重置 Entity、Spawner、Buff、输入、弹幕或退场事件。
- 改关卡配置后忘记更新解锁、统计、UI 显示。
- 胜利/失败流程绕过 BattleController 导致状态错乱。
- 修改 Skill/Buff 后忘记出战准备、掉落、存档共享数据。
- 将 Archive 的旧验收或旧问题当当前事实。

## 修改后必验

- 从 Boot 进入主界面，再进入战斗，再胜利/失败/重试/返回。
- 战斗退出后无 Entity、弹幕、VFX、飘字、输入残留。
- 关卡选择、进度保存、云同步逻辑不回退。
- UI 面板 Suspend/Resume、Pop/Push 行为符合 AppFlow。
- 若触碰 V2 技能，至少验技能装备、拾取、Buff/DOT、被动、普攻 Slot[0]。
