# ShooterGame · 技术设计文档（TDD）

> **版本**：v1.6 | **日期**：2026-05-07 | **状态**：✅ PK 评审完成 + V2 云存储实施完成（编译0错误，待真机验证）  
> **来源**：`SG_GAME_DESIGN.md` v3.2 + `SG_UI_DESIGN.md` v2.0  
> **命名空间**：`Game.ShooterGame`  
> **目录**：`Assets/_Game/Scripts/ShooterGame/`  
> **配置**：`Assets/_Game/Configs/ShooterGame/`

---

## 子文件目录

| # | 文件 | 内容摘要 | 预估行数 |
|---|------|---------|----------|
| 1 | [SG_TDD_01_ARCHITECTURE.md](SG_TDD_01_ARCHITECTURE.md) | 总体架构 · 目录结构 · 依赖关系 · 生命周期 · 时序图 | ~250 |
| 2 | [SG_TDD_02_BATTLE_SYSTEM.md](SG_TDD_02_BATTLE_SYSTEM.md) | 战斗状态机 · 底线检测 · 屏幕震动 · 通关/失败判定 · 重试流程 | ~350 |
| 3 | [SG_TDD_03_LEVEL_PROGRESS.md](SG_TDD_03_LEVEL_PROGRESS.md) | 关卡配置SO · 存储系统集成 · 关卡解锁 · 数据流 | ~200 |
| 4 | [SG_TDD_04_UI_CONTROLLERS.md](SG_TDD_04_UI_CONTROLLERS.md) | 6个UI Controller · 数据绑定 · 飘字池 · 血条预损 · 转场编排 | ~350 |
| 5 | [SG_TDD_05_INPUT_JOYSTICK.md](SG_TDD_05_INPUT_JOYSTICK.md) | 虚拟摇杆 · Vector2Variable · 输入→移动管线 · JoystickConfigSO | ~200 |
| 6 | [SG_TDD_06_CLOUD_SAVE.md](SG_TDD_06_CLOUD_SAVE.md) | **V2 微信登录 · 云存储 · 跨设备同步 · 离线优先 · 数据迁移** | ~400 |

---

## SO 资产总览（21 个）

| 资产名 | SO 类型 | 路径 | 来源 |
|--------|---------|------|------|
| SG_Player | EntityConfigSO | `Configs/ShooterGame/` | GDD §3.6 |
| SG_Base | EntityConfigSO | `Configs/ShooterGame/` | GDD §3.6 |
| SG_Enemy_Normal | EntityConfigSO | `Configs/ShooterGame/` | GDD §3.4 |
| SG_Enemy_Fast | EntityConfigSO | `Configs/ShooterGame/` | GDD §3.4 |
| SG_Level_01 ~ 05 | SG_LevelConfigSO | `Configs/ShooterGame/Levels/` | GDD §4.4 |
| SG_Wave_01 ~ 05 | EntitySpawnWaveSO | `Configs/ShooterGame/Waves/` | GDD §4.3 |
| SG_PlayerBullet_Straight | BulletPatternSO | `Configs/ShooterGame/` | GDD §3.2 |
| SG_ScreenShake_Default | ScreenShakeConfigSO | `Configs/ShooterGame/` | GDD §3.1 |
| SG_CurrentLevelIndex | IntVariable | `Configs/ShooterGame/Variables/` | GDD §4.4 |
| SG_BaseHP | FloatVariable | `Configs/ShooterGame/Variables/` | GDD §3.4 |
| SG_CurrentWaveIndex | IntVariable | `Configs/ShooterGame/Variables/` | UI §8.3 |
| SG_TotalWaveCount | IntVariable | `Configs/ShooterGame/Variables/` | UI §8.3 |
| SG_KillCount | IntVariable | `Configs/ShooterGame/Variables/` | UI §8.3 |
| SG_TotalEnemyCount | IntVariable | `Configs/ShooterGame/Variables/` | UI §8.3 |
| SG_InputDirection | Vector2Variable | `Configs/ShooterGame/Variables/` | UI §8.4 |

---

## 新增 C# 类型清单

| 类名 | 类型 | 命名空间 | 职责 |
|------|------|---------|------|
| BattleController | MonoBehaviour | Game.ShooterGame | 战斗编排指挥（唯一入口） |
| BattleState | enum | Game.ShooterGame | Intro/Playing/Victory/Defeat |
| BaseLineDetector | 纯 C# | Game.ShooterGame | 底线检测逻辑 |
| CameraShaker | MonoBehaviour | Game.ShooterGame | 屏幕震动 |
| ScreenShakeConfigSO | ScriptableObject | Game.ShooterGame | 震动参数配置 |
| SG_LevelConfigSO | ScriptableObject | Game.ShooterGame | 关卡元数据 |
| SG_ProgressManager | 纯 C# | Game.ShooterGame | 存档读写 |
| Vector2Variable | ScriptableObject | MiniGameTemplate.Data | SO 变量（框架层新增） |
| JoystickConfigSO | ScriptableObject | Game.ShooterGame | 摇杆配置 |
| JoystickController | MonoBehaviour | Game.ShooterGame.UI | 摇杆逻辑 |
| LoadingScreenController | MonoBehaviour | Game.ShooterGame.UI | 加载界面 |
| LevelSelectController | MonoBehaviour | Game.ShooterGame.UI | 选关界面 |
| BattleHUDController | MonoBehaviour | Game.ShooterGame.UI | 战斗HUD |
| PausePanelController | MonoBehaviour | Game.ShooterGame.UI | 暂停面板 |
| VictoryPanelController | MonoBehaviour | Game.ShooterGame.UI | 胜利面板 |
| DefeatPanelController | MonoBehaviour | Game.ShooterGame.UI | 失败面板 |
| SG_PlayerInputBridge | MonoBehaviour | Game.ShooterGame | 摇杆→MovementComponent 桥接 |

---

## 实施优先级

### Phase 总览

| Phase | 内容 | 纯编码 | 含调试 buffer | 依赖 |
|-------|------|--------|-------------|------|
| **SG-P0** | 核心骨架（枚举+变量+BattleController+BaseLineDetector） | 3h | 4.5h | 框架 Entity 系统 |
| **SG-P1** | 支撑系统（CameraShaker+Config+Progress+GameStartupFlow） | 2.5h | 3.5h | SG-P0 |
| **SG-P2** | 输入管线（Joystick+PlayerInputBridge） | 2h | 3h | SG-P0 |
| **SG-P3** | UI 层（FairyGUI 包制作 + 6 个 Controller） | 8h | 11h | SG-P0~P2 |
| **SG-P4** | 集成（SO 资产+波次+集成验收+命名校验） | 3h | 4h | SG-P0~P3 |

**总计**：纯编码 ~18.5h / 含调试 ~26h（单人开发 4~5 天）  
> PM-010 注：首次实现含调试系数约 1.4x，FairyGUI 坐标系 + Entity 集成 debug 是主要时间消耗。

---

### SG-P0 子任务拆分

| 子任务 | 内容 | 预估 | ✅ Done When |
|--------|------|------|-------------|
| P0.0 | 创建 SO 模板资产（SG_Player / SG_Base / SG_Enemy_Normal / SG_BaseHP / SG_CurrentLevelIndex） | 20min | Assets 目录下 5 个 SO 存在、Inspector 字段有默认值 |
| P0.1 | BattleState 枚举 + Vector2Variable | 30min | 编译通过 + Vector2Variable 在 Inspector 可创建 |
| P0.2 | BattleController 骨架 + BaseLineDetector | 1.5h | PlayMode：场景加载→Intro→1.5s 后 Playing→底线突破扣血→HP=0 进 Defeat |
| P0.3 | InitBattle 完整流程 + 击杀计数 + RetryBattle | 1h | PlayMode：Defeat 后点重试→重置血量+波次→重新进入 Intro |

### SG-P1 子任务

| 子任务 | 内容 | 预估 | ✅ Done When |
|--------|------|------|-------------|
| P1.1 | CameraShaker + ScreenShakeConfigSO | 40min | PlayMode：调用 Shake → 相机抖动 → 自然衰减 → StopShake 立即复位 |
| P1.2 | SG_LevelConfigSO + 5 关资产创建 | 30min | 5 个 Level SO 存在、WaveConfig 引用正确 |
| P1.3 | SG_ProgressManager | 40min | Editor 下存档/读档/重置 + IsLevelUnlocked 逻辑验证 |
| P1.4 | GameStartupFlow 骨架 | 40min | Boot 场景加载→创建 ProgressManager→静态字段可访问 |

### SG-P2 子任务

| 子任务 | 内容 | 预估 | ✅ Done When |
|--------|------|------|-------------|
| P2.1 | JoystickConfigSO + JoystickController | 1.5h | PlayMode：触摸区域按下→摇杆显示→拖动输出方向→松手归零 |
| P2.2 | SG_PlayerInputBridge | 30min | PlayMode：拖动摇杆→飞机跟随移动→松手停止→Y 轴方向正确 |

### SG-P3 子任务

| 子任务 | 内容 | 预估 | ✅ Done When |
|--------|------|------|-------------|
| P3.0 | FairyGUI 包制作（Loading/LevelSelect/Battle/Popup 4 个包） | 4h | 4 个 .fui 导出成功 + 在 Unity 中可 CreateObject |
| P3.1 | LoadingScreenController | 30min | Boot 场景→显示进度条→完成后淡出 |
| P3.2 | LevelSelectController | 1h | 5 个节点三态显示正确 + 点击可进入→触发 TransitionToBattle |
| P3.3 | BattleHUDController | 1h | 血条实时响应 SO + 预损动画 + 波次文本更新 + 飘字显示 |
| P3.4 | PausePanelController + VictoryPanelController + DefeatPanelController | 1.5h | 暂停→TimeScale=0 + 恢复→TimeScale=1；胜利/失败面板数据填充正确 |

### SG-P4 子任务

| 子任务 | 内容 | 预估 | ✅ Done When |
|--------|------|------|-------------|
| P4.1 | 剩余 SO 资产创建（变量/子弹/震动/波次） | 1h | 21 个 SO 全部存在 |
| P4.2 | 波次编排（SG_Wave_01~05 配置） | 1h | 5 关波次播放正常、难度递增可感知 |
| P4.3 | SO 命名校验 + 全流程集成验收 | 1h | Editor 脚本校验 21 个 SO 名称一致 + 5 关通关/失败/重试全链路 pass |

---

## 框架依赖确认

| 框架能力 | 状态 | 使用方式 |
|----------|------|---------|
| EntitySystemBootstrap | ✅ 已有 | 战斗场景 Bootstrap GO |
| EntitySpawnWaveSO | ✅ 已有 | 5 关波次配置 |
| HealthComponent | ✅ 已有 | 基地 HP 管理 |
| MovementComponent | ✅ 已有 | 飞机/敌机移动 |
| EntityCollisionSolver | ✅ 已有 | 飞机 vs 敌机碰撞 |
| FloatVariable / IntVariable | ✅ 已有 | SO 变量通信 |
| ISaveSystem | ✅ 已有 | 进度存储 |
| Vector2Variable | ❌ 需新增 | 摇杆方向输出 |
| EntitySpawner.IsAllWavesCleared | ✅ 已确认 | 通关判定 |

---

## 变更日志

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.0 | 2026-05-03 | 初版 TDD，覆盖 GDD v3.2 + UI v2.0 全部 V1 待实现项 |
| v1.1 | 2026-05-03 | PK Round 1 修正：Spawn 签名、RestartAll、碰撞震动方案、击杀计数签名、波次追踪、BaseLineDetector 时序、索引语义、ProgressManager 生命周期、Y 轴翻转确认 |
| v1.2 | 2026-05-03 | PK Round 2 修正（工具开发者视角）：重复属性修复、UI Controller 引用声明、BaseLineDetector SRP、重试 StopShake、MaxUnlockedLevel 参数化、飘字 Tween 竞态、UI 初始化时序、转场 Coroutine |
| v1.3 | 2026-05-03 | PK Round 3 修正（PM 视角）：实施优先级拆分子任务+Done When 验收标准、GameStartupFlow 骨架、Action/IEnumerator 修正、SetInputEnabled 实现、Timer 波 V1 铁律、工时 buffer 校准 |
| v1.4 | 2026-05-03 | 微信真机 PK 修正（WX-001~011）：Save() try-catch+bool、V2 登录升级路径、热启动 Reload、ValidateData 防篡改、totalLevels 参数化、HandleVictoryConfirm 同步更新 |
| v1.5 | 2026-05-07 | 新增 SG_TDD_06_CLOUD_SAVE.md — V2 微信登录+云存储完整 TDD（初稿 v0.1） |
| v1.6 | 2026-05-07 | TDD_06 v0.5 实施完成：jslib+桥接层+WxAuthService+CloudSyncService+CloudSaveSystem+SG_ProgressManager.Reload+云函数模板×3 |
