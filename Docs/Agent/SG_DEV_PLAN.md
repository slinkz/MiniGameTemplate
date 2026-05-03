# 🎮 ShooterGame 开发计划总览

> **项目**：纵版飞行弹幕射击 · 微信小游戏  
> **文档版本**：基于 TDD v1.4 / GDD v3.2 / UI Design v2.0  
> **日期**：2026-05-03  
> **状态**：🟢 SG-P0 PlayMode 验收通过 + P1/P2（键盘替代）验收通过 + 代码评审通过（2026-05-03）

---

## 📊 项目总览

| 维度 | 数据 |
|------|------|
| **品类** | 纵版飞行弹幕射击（自动射击 + 虚拟摇杆操控） |
| **核心玩法** | 操控战机消灭敌机 → 保护基地 → 5 关线性解锁 |
| **技术栈** | Unity → 微信小游戏 + Entity-Component 框架 + FairyGUI |
| **新增 C# 类** | 19 个（3 asmdef：Game.ShooterGame / .UI / .Editor） |
| **SO 资产** | 21 个 |
| **FairyGUI 包** | 5 个（Common / Loading / LevelSelect / Battle / Popup） |
| **纯编码工时** | ~18.5h |
| **含调试工时** | ~26h（1.4x buffer 系数） |
| **工具工时** | ~4.25h |
| **合计** | ~30h（单人 4~5 天） |

---

## 📚 设计文档体系

### 文档清单（16 个文档）

| 类别 | 文档 | 版本 | 状态 |
|------|------|------|------|
| **游戏设计** | SG_GAME_DESIGN.md | v3.2 | ✅ PK 通过 |
| **UI 设计** | SG_UI_DESIGN.md | v2.0 | ✅ PK 通过 |
| **核心 TDD** | SG_TDD_INDEX + 5 子文件 | v1.4 | ✅ PK 通过（含微信真机 PK） |
| **工具 TDD** | SG_TOOLS_TDD_INDEX + 2 子文件 | v1.3 | ✅ PK 通过 |
| **PK 记录** | 7 个 PK 文件 | — | ✅ 全部收敛 |

### PK 评审总账（11 轮 / 118 个问题 / 100% 收敛）

| 轮次 | 视角 | 核心 TDD | 工具 TDD | 问题数 |
|------|------|---------|---------|--------|
| 设计 PK | GDD + UI | — | — | 47 |
| TDD 第一轮 | 架构师 / 工具开发者 | 10 | 10 | 20 |
| TDD 第二轮 | 工具开发者 / 架构师 | 10 | 10 | 20 |
| TDD 第三轮 | **PM** / 架构师 & 工具开发者 | 10 | 10 | 20 |
| **微信真机 PK** | **微信小程序开发者 vs Unity 架构师** | 11 | — | 11 |
| **总计** | | | | **118** |

---

## 🗓️ 实施路线图

### 推荐实施顺序（含工具穿插）

```
核心 P0.0~P0.3 → 🔧 工具 P0 → 核心 P1~P2 → 🔧 工具 P1 → 核心 P3~P4 → 🔧 工具 P2(backlog)
```

### Phase 总览

| Phase | 内容 | 纯编码 | 含调试 | 依赖 | 子任务数 |
|-------|------|--------|--------|------|---------|
| **SG-P0** | 核心骨架 | 3h | 4.5h | 框架 Entity 系统 | 4 |
| **🔧 工具 P0** | 波次编辑器 + Debug 工具 | 2.75h | — | 核心 P0.2 | 6 |
| **SG-P1** | 支撑系统 | 2.5h | 3.5h | SG-P0 | 4 |
| **SG-P2** | 输入管线 | 2h | 3h | SG-P0 | 2 |
| **🔧 工具 P1** | 状态监视 + 摇杆 Gizmo | 1.5h | — | 核心 P0.3 / P2.1 | 2 |
| **SG-P3** | UI 层（FairyGUI） | 8h | 11h | SG-P0~P2 | 5 |
| **SG-P4** | 集成验收 | 3h | 4h | SG-P0~P3 | 3 |

**核心总计**：18.5h / 26h | **工具总计**：4.25h | **合计**：~30h（含调试）

---

## 📋 完整子任务清单

### SG-P0：核心骨架（4.5h 含调试）✅ 已完成（2026-05-03）

| # | 子任务 | 内容 | 预估 | ✅ Done When | 状态 |
|---|--------|------|------|-------------|------|
| P0.0 | SO 模板资产 | SG_Player / SG_Base / SG_Enemy_Normal / SG_BaseHP / SG_CurrentLevelIndex | 20min | 5 个 SO 存在 + Inspector 默认值 | ✅ |
| P0.1 | 枚举 + 变量 | BattleState 枚举 + Vector2Variable（框架层新增） | 30min | 编译通过 + Inspector 可创建 | ✅ |
| P0.2 | 战斗骨架 | BattleController + BaseLineDetector | 1.5h | Intro→Playing→底线扣血→HP=0→Defeat | ✅ |
| P0.3 | 完整流程 | InitBattle + 击杀计数 + RetryBattle | 1h | 重试→重置→重新 Intro | ✅ |

**P0 编码产出**（19 个文件）：
- Core：BattleState / BattleController / BaseLineDetector / CameraShaker / IUIControllers / SG_GameBootExtension
- Config：ScreenShakeConfigSO / SG_LevelConfigSO / JoystickConfigSO
- Progress：SG_ProgressManager
- Input：SG_PlayerInputBridge / SG_DebugKeyboardInput（永久编辑器键盘输入）
- UI：LoadingScreenController / LevelSelectController / BattleHUDController / PausePanelController / VictoryPanelController / DefeatPanelController / JoystickController
- 框架层：Vector2Variable
- asmdef：Game.ShooterGame / Game.ShooterGame.UI / Game.ShooterGame.Editor

**P0 编码偏离项**（已修复）：
1. `Entity.Camp` 为 `internal set` → 移除手动赋值，改由 EntityConfigSO.Camp 自动设置
2. `EntitySpawner.SetPaused()` 不存在 → 改为 SpawnPoint 首次 StartWave + _spawnerStarted 布尔
3. `GameStartupFlow.Progress` 命名冲突 → 新增 `SG_Boot` 静态类
4. `GComponent.TweenKillAll()` 不存在 → 改为 `GTween.Kill(ft)`
5. Retry 时未重置 `_spawnerStarted` → 已修复

### 🔧 工具 P0：编辑器工具（2.75h）

| # | 工具 | 预估 | 依赖 |
|---|------|------|------|
| T1 | 波次编辑器：一键复制最后一波 | 1h | 框架 EntitySpawnWaveSO |
| T2 | 波次编辑器：统计面板 | 0.75h | 同上 |
| T3 | Debug 字段 + ProfilerMarker | 30min | 核心 P0.2 |
| T4 | 5 个 Debug MenuItem | 20min | 核心 P0.2 |
| T5 | JoystickConfigSO（核心 TDD_05 覆盖） | 15min | — |
| T6 | BaseLineY Gizmo（含在 BattleController） | — | 核心 P0.2 |

### SG-P1：支撑系统（3.5h 含调试）

| # | 子任务 | 内容 | 预估 | ✅ Done When |
|---|--------|------|------|-------------|
| P1.1 | 屏幕震动 | CameraShaker + ScreenShakeConfigSO | 40min | Shake→抖动→衰减→StopShake 复位 |
| P1.2 | 关卡配置 | SG_LevelConfigSO + 5 关资产 | 30min | 5 个 Level SO + WaveConfig 引用 |
| P1.3 | 进度管理 | SG_ProgressManager | 40min | 存档/读档/重置 + IsLevelUnlocked |
| P1.4 | 启动流程 | GameStartupFlow 骨架 | 40min | Boot 场景→ProgressManager 可访问 |

### SG-P2：输入管线（3h 含调试）

| # | 子任务 | 内容 | 预估 | ✅ Done When |
|---|--------|------|------|-------------|
| P2.1 | 虚拟摇杆 | JoystickConfigSO + JoystickController | 1.5h | 触摸→摇杆显示→方向输出→松手归零 |
| P2.2 | 输入桥接 | SG_PlayerInputBridge | 30min | 摇杆→飞机移动→Y 轴正确 |

### 🔧 工具 P1：监视 + Gizmo（1.5h）

| # | 工具 | 预估 | 依赖 |
|---|------|------|------|
| T7 | 战斗状态监视 EditorWindow | 1h | 核心 P0.3 + SO 资产 |
| T8 | 摇杆 Gizmo 叠加 | 30min | 核心 P2.1 |

### SG-P3：UI 层（11h 含调试）

| # | 子任务 | 内容 | 预估 | ✅ Done When |
|---|--------|------|------|-------------|
| P3.0 | FairyGUI 包 | Loading / LevelSelect / Battle / Popup（4 个包 + Common 共享） | 4h | 4 个 .fui 导出 + Unity CreateObject |
| P3.1 | Loading | LoadingScreenController | 30min | 进度条→淡出 |
| P3.2 | 选关 | LevelSelectController | 1h | 5 节点三态 + 点击进入 |
| P3.3 | 战斗 HUD | BattleHUDController（血条预损+飘字池+波次文字） | 1h | 血条+预损+波次+飘字 |
| P3.4 | 弹窗×3 | Pause + Victory + Defeat Controller | 1.5h | 暂停 TimeScale + 数据填充 |

### SG-P4：集成验收（4h 含调试）

| # | 子任务 | 内容 | 预估 | ✅ Done When |
|---|--------|------|------|-------------|
| P4.1 | SO 资产 | 剩余变量/子弹/震动/波次 SO | 1h | 21 个 SO 全部存在 |
| P4.2 | 波次编排 | SG_Wave_01~05 配置 | 1h | 5 关难度递增可感知 |
| P4.3 | 集成校验 | SO 命名校验 + 全流程验收 | 1h | 21 个 SO 名称一致 + 5 关全链路 pass |

---

## 🏗️ 新增代码架构

### 实际目录结构（P0 完成后）

```
Assets/_Game/
├── Scripts/ShooterGame/
│   ├── Game.ShooterGame.asmdef
│   ├── Core/
│   │   ├── BattleState.cs
│   │   ├── BattleController.cs
│   │   ├── BaseLineDetector.cs
│   │   ├── CameraShaker.cs
│   │   ├── IUIControllers.cs
│   │   └── SG_GameBootExtension.cs
│   ├── Config/
│   │   ├── ScreenShakeConfigSO.cs
│   │   ├── SG_LevelConfigSO.cs
│   │   └── JoystickConfigSO.cs
│   ├── Progress/
│   │   └── SG_ProgressManager.cs
│   ├── Input/
│   │   ├── SG_PlayerInputBridge.cs
│   │   └── SG_DebugKeyboardInput.cs
│   └── UI/
│       ├── Game.ShooterGame.UI.asmdef
│       ├── LoadingScreenController.cs
│       ├── LevelSelectController.cs
│       ├── BattleHUDController.cs
│       ├── PausePanelController.cs
│       ├── VictoryPanelController.cs
│       ├── DefeatPanelController.cs
│       └── JoystickController.cs
├── Editor/ShooterGame/
│   └── Game.ShooterGame.Editor.asmdef
└── _Framework/DataSystem/Scripts/Variables/
    └── Vector2Variable.cs (框架层新增)
```

### asmdef 边界（3 个）

```
Game.ShooterGame          → 核心逻辑（依赖 MiniGameTemplate.Entity + .Data）
Game.ShooterGame.UI       → UI Controller（依赖 Game.ShooterGame + FairyGUI）
Game.ShooterGame.Editor   → 编辑器工具（仅 Editor 平台）
```

### 框架层新增

| 类型 | 命名空间 | 说明 |
|------|---------|------|
| Vector2Variable | MiniGameTemplate.Data | SO 变量（摇杆方向输出） |

---

## ⚙️ 关键设计决策

| 决策 | 内容 | 来源 |
|------|------|------|
| 基地 = Entity + [Health] | 底线检测扣血，不走碰撞系统 | GDD v3.0 |
| 飞机不挂 Health | 碰撞 ContactDamage=9999 一撞即杀敌机 | GDD v3.0 |
| V1 铁律：AllCleared 波次 | 5 关全用 AllCleared 推进，不用 Timer 波 | PM-008 |
| CameraSize=8 | 可视 9×16 世界单位，穿越时间为设计锚点 | GDD v3.0 |
| 通关 = IsAllWavesCleared | EntitySpawner 内置判定 | GDD v3.2 |
| FairyGUI GGraph 摇杆 | 不用 Unity UI，纯 FairyGUI 实现（微信兼容） | TDD_05 |
| GameStartupFlow 静态 Progress | Boot 场景入口，不用 DontDestroyOnLoad | PM-004 |
| SetInputEnabled 时序 | 禁用先 Bridge 后 Joystick，启用反之 | PM-007 |
| FairyGUI V1 全量预加载 | 5 包 < 500KB，占首包 < 2%，零按需加载延迟 | UI §8.7 |
| 飘字池化 8 并发 | FIFO 环形缓冲 + visible=false 回收，零 GC | UI §8.8 |
| 重试不重载场景 | EntitySpawner.Reset() + SO 写回初始值 | UI §8.6 |
| Save() try-catch + bool | 真机存储失败不崩溃，调用方 Toast 通知用户 | WX-001 |
| 热启动 Reload() | Boot 时判断已有实例则从 storage 重载，应对微信后台恢复 | WX-005 |
| V1 纯本地 + V2 路径 | V1 不上服务端（MVP），V2 wx.login→openid→CRUD→并集策略 | WX-002 |
| totalLevels 构造函数注入 | 避免 ValidateData 中硬编码关卡数，V2 加关不踩坑 | WX-010 |

---

## 🎯 下一步

**当前状态**：SG-P0 PlayMode 验收通过 ✅ + P1/P2（键盘替代）验收通过 ✅ + 代码评审通过 ✅

**PlayMode 验收结果**（2026-05-03 22:00）：
- B1 Intro→Playing：✅ 1.5s 自动转换
- B2 敌机生成：✅ 3 波 12 个敌机生成
- B3 底线突破扣血：✅ BaseHP 1.0→0.7（2 个突破）
- B4~B6 Defeat/Retry/Victory：✅ 自动完成（10 击杀→Victory）
- C1~C4 SO 变量绑定：✅ 全部实时反映
- 控制台错误：✅ 零

**紧接任务**：
1. 🔧 工具 P0（波次编辑器 + Debug 工具）
2. SG-P3 UI 层（FairyGUI 包制作 + Controller 接入）
3. SG-P4 集成验收（5 关配置 + 全链路）

**验收里程碑**：下一个 = SG-P3 FairyGUI 包创建 + UI 全链路

---

## 关联文档

| 文档 | 路径 |
|------|------|
| 游戏设计 | `Docs/Agent/SG_GAME_DESIGN.md` |
| UI 设计 | `Docs/Agent/SG_UI_DESIGN.md` |
| 核心 TDD | `Docs/Agent/SG_TDD_INDEX.md` + 5 子文件 |
| 工具 TDD | `Docs/Agent/SG_TOOLS_TDD_INDEX.md` + 2 子文件 |
| PK 记录 | `Docs/Agent/SG_*_PK*.md`（7 个文件） |
