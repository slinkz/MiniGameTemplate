---
system: knowledge-engineering
scope: module-card-shootergame
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/ShooterGame_Battle.md
---

# Module Card: ShooterGame

## 1. 模块职责

ShooterGame 是当前业务主线：纵版飞行弹幕射击小游戏。它负责将框架能力组合成完整游戏体验：主界面、选关、战斗、输入、基地血量、胜负结算、进度存储、技能/道具/Buff/DOT、退场清理和 UI 编排。

## 2. 不负责什么

- 不实现底层 Entity 框架、弹幕渲染、RuntimeAtlas、FairyGUI 管理器。
- 不直接管理框架 Singleton 生命周期。
- 不绕过 AppFlow/SceneLoader/UIManager 自行维护全局导航。
- 不把配置硬编码进战斗 MonoBehaviour；敌人、技能、波次、关卡优先走 SO。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `BattleController` | 战斗状态机、初始化、胜负、重试、退场编排 |
| `BaseLineDetector` | 敌机突破底线检测与基地扣血 |
| `CameraShaker` / `ScreenShakeConfigSO` | 屏幕震动反馈 |
| `SG_LevelConfigSO` | 关卡元数据，关联 Wave、基地 HP 等 |
| `SG_ProgressManager` | 关卡进度、云存储/本地存储集成 |
| `JoystickController` / `SG_PlayerInputBridge` | 输入到 Movement 的桥接 |
| UI Controllers | 选关、HUD、胜利、失败、暂停、出战准备等 |

## 4. 数据流

```text
LevelSelect UI
  -> SG_CurrentLevelIndex(IntVariable)
  -> AppFlow / SceneLoader 进入 Battle
  -> BattleController 读取 SG_LevelConfigSO
  -> EntitySystemBootstrap + Spawner 生成实体
  -> DanmakuSystem 处理子弹/碰撞
  -> SO Variables 更新 HP/波次/击杀
  -> UI Controllers 监听变量刷新
  -> Victory/Defeat 写入 SG_ProgressManager
```

## 5. 生命周期

```text
Boot -> GameStartupFlow -> Main/AppFlow
  -> LevelSelect
  -> Battle Intro
  -> Playing
  -> Victory / Defeat
  -> 退场清理
  -> 返回 Main/LevelSelect 或 Retry
```

退场清理必须覆盖 Entity、Spawner、弹幕、VFX、飘字、输入、UI 状态和战斗事件通道。

## 6. 依赖关系

ShooterGame 可依赖框架全部模块，但应保持 Game 层胶水角色：

- EntitySystem：战斗实体、技能、Buff、刷怪。
- DanmakuSystem：弹幕发射、碰撞。
- UISystem/FairyGUI：面板显示与 Controller。
- AppFlow：场景/面板导航。
- DataSystem：SO 变量、存档、云同步。
- WeChatBridge：微信平台能力。

## 7. 关键 SO / 配置路径

```text
UnityProj/Assets/_Game/Configs/ShooterGame/
UnityProj/Assets/_Game/Configs/ShooterGame/Levels/
UnityProj/Assets/_Game/Configs/ShooterGame/Waves/
UnityProj/Assets/_Game/Configs/ShooterGame/Variables/
UnityProj/Assets/_Game/Configs/ShooterGame/Skills/
UnityProj/Assets/_Game/Configs/ShooterGame/Buffs/
UnityProj/Assets/_Game/Configs/ShooterGame/Dots/
UnityProj/Assets/_Game/Configs/ShooterGame/Passives/
UnityProj/Assets/_Game/Resources/ShooterGame/
```

## 8. 关键 ADR

- ADR-033：Entity-Component 框架。
- ADR-034：AppFlow 栈式导航系统。
- ADR-035：战斗退场生命周期统一事件通道。
- ADR-036：飘字系统统一到 RBM 渲染管线。

## 9. 热路径 / 性能约束

- 战斗 Tick 中避免 GC 分配。
- UI 动画、飘字、弹幕、技能触发不要频繁分配临时对象。
- 微信小游戏真机性能优先，避免复杂后处理和阻塞操作。

## 10. 常见错误

- 只重置 UI，没有清理 Entity/弹幕/VFX/Buff/输入。
- 胜利/失败绕过 BattleController，导致状态机分叉。
- 改关卡或技能后忘记同步 SO 资产和 UI 展示。
- 把 Archive 中旧验收当当前实现事实。
- 云存储和本地进度读写顺序混乱。

## 11. 修改前必读

- `CONTEXT_PACKS/ShooterGame_Battle.md`
- `SHOOTER_GAME/TDD/SG_TDD_INDEX.md`
- `SHOOTER_GAME/V2_TDD/SG_V2_TDD_INDEX.md`
- `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md`
- 触碰 UI 时读 `CONTEXT_PACKS/FairyGUI_UI.md`
- 触碰导航时读 `MODULE_CARDS/AppFlow.md`

## 12. 修改后必验

- Boot -> Main -> LevelSelect -> Battle 全链路。
- Victory、Defeat、Retry、Return 都可重复执行。
- 退出战斗后无 Entity/弹幕/VFX/飘字/输入残留。
- 进度保存、云同步、解锁状态不回退。
- UI 面板层级、Suspend/Resume、按钮事件无重复绑定。