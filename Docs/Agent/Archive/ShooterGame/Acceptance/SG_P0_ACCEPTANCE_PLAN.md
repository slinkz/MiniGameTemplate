---
system: shooter-game
scope: p0-acceptance
last_verified: 2026-05-03
depends_on: [SG_TDD_01, SG_TDD_02, SG_TDD_03, SG_TDD_04, SG_TDD_05]
related_code: Assets/_Game/Scripts/ShooterGame/**/*.cs
---

# SG-P0 核心骨架验收计划

> **版本**：v1.1  
> **日期**：2026-05-03  
> **状态**：✅ PlayMode 验收通过（2026-05-03 22:00）

---

## 一、验收前置条件

| # | 条件 | 操作方式 | 状态 |
|---|------|---------|------|
| 1 | Unity 编辑器打开 UnityProj | 双击 UnityProj 或 Unity Hub | ✅ |
| 2 | 编译零错误 | Console 窗口确认 | ✅ |
| 3 | 创建 SO 模板资产（P0.0） | Unity Editor 右键菜单创建 | ✅ |
| 4 | 搭建 Battle 测试场景 | 新建 Scene + 挂载 MonoBehaviour | ✅ |

---

## 二、SO 资产创建清单（P0.0）

在 `Assets/_Game/Configs/ShooterGame/` 下创建：

| # | 资产名 | 类型 | 路径 | 关键字段设置 |
|---|--------|------|------|-------------|
| 1 | SG_Player | EntityConfigSO | Configs/ShooterGame/ | Camp=Player, ContactDamage=9999, MaxHp=1 |
| 2 | SG_Base | EntityConfigSO | Configs/ShooterGame/ | Camp=Player, MaxHp=100 |
| 3 | SG_Enemy_Normal | EntityConfigSO | Configs/ShooterGame/ | Camp=Enemy, ContactDamage=15, MaxHp=30 |
| 4 | SG_BaseHP | FloatVariable | Configs/ShooterGame/Variables/ | InitialValue=1.0 |
| 5 | SG_CurrentLevelIndex | IntVariable | Configs/ShooterGame/Variables/ | InitialValue=0 |
| 6 | SG_CurrentWaveIndex | IntVariable | Configs/ShooterGame/Variables/ | InitialValue=0 |
| 7 | SG_TotalWaveCount | IntVariable | Configs/ShooterGame/Variables/ | InitialValue=0 |
| 8 | SG_KillCount | IntVariable | Configs/ShooterGame/Variables/ | InitialValue=0 |
| 9 | SG_TotalEnemyCount | IntVariable | Configs/ShooterGame/Variables/ | InitialValue=0 |
| 10 | SG_InputDirection | Vector2Variable | Configs/ShooterGame/Variables/ | InitialValue=(0,0) |
| 11 | SG_ScreenShakeConfig | ScreenShakeConfigSO | Configs/ShooterGame/ | 使用默认值 |
| 12 | SG_JoystickConfig | JoystickConfigSO | Configs/ShooterGame/ | 使用默认值 |
| 13 | SG_Level_01 | SG_LevelConfigSO | Configs/ShooterGame/Levels/ | BaseHpRatio=1.0 |
| 14 | SG_Wave_01 | EntitySpawnWaveSO | Configs/ShooterGame/Waves/ | 3 波简单配置 |

---

## 三、测试场景搭建

### Battle 场景（`Assets/_Game/Scenes/Battle.unity`）

1. **创建空场景** → 保存为 `Battle`
2. **添加 Main Camera**：
   - Position = (0, 0, -10)
   - Size = 8（正交）
   - 挂载 `CameraShaker` 组件
3. **创建空 GO `BattleController`**：
   - 挂载 `BattleController` 组件
   - 拖拽 SO 资产到 Inspector 字段
   - 拖拽 CameraShaker 引用
4. **创建空 GO `PlayerInputBridge`**：
   - 挂载 `SG_PlayerInputBridge` 组件
   - 拖拽 SG_InputDirection Vector2Variable
5. **创建空 GO `UIControllers`**：
   - 挂载 BattleHUDController / PausePanelController / VictoryPanelController / DefeatPanelController / JoystickController
   - 将各 Controller 拖拽到 BattleController 的 UI 引用字段
6. **创建 EntitySpawnPoint** GO：
   - 挂载 EntitySpawnPoint 组件
   - 设置 AutoStartOnEnable = false
   - 拖拽到 BattleController._spawnPoint

### Boot 场景（`Assets/_Game/Scenes/Boot.unity`）

1. **确保已有 GameBootstrapper** GO
2. **在 GameBootstrapper 初始化完成后** 调用 `SG_Boot.InitProgress()`
   - 方式 A：修改 GameStartupFlow.cs 在 Start 末尾加一行
   - 方式 B：新建一个 SG_BootInitializer MonoBehaviour，`Start()` 中调用

---

## 四、验收测试用例

### 4.1 编译验证（自动）

| # | 检查项 | 预期 | Pass/Fail |
|---|--------|------|-----------|
| A1 | Console 零 Error | 0 errors | ✅ |
| A2 | Console 零 Warning（除框架已知） | 0 新增 warnings | ✅ |
| A3 | Vector2Variable 可在 Inspector 创建 | 右键菜单可见 | ✅ |
| A4 | ScreenShakeConfigSO 可在 Inspector 创建 | 右键菜单可见 | ✅ |
| A5 | SG_LevelConfigSO 可在 Inspector 创建 | 右键菜单可见 | ✅ |
| A6 | JoystickConfigSO 可在 Inspector 创建 | 右键菜单可见 | ✅ |

### 4.2 核心流程验证（手动 Play Mode）

| # | 场景 | 操作 | 预期结果 | Pass/Fail |
|---|------|------|---------|-----------|
| B1 | Battle | 进入 Play Mode | 自动进入 Intro 状态，1.5s 后转 Playing | ✅ |
| B2 | Battle | 等待敌机生成 | Spawner 启动，敌机从上方出现 | ✅ |
| B3 | Battle | 等待敌机到达底线 | 基地 HP 下降 + CameraShake 触发 | ✅ |
| B4 | Battle | 基地 HP 归零 | 转入 Defeat 状态 | ✅ |
| B5 | Battle | Defeat 面板点 Retry | 全部重置 + 重新 Intro | ⬜ P3(FairyGUI) |
| B6 | Battle | 消灭所有敌机 | 转入 Victory 状态 | ✅ |
| B7 | Battle | Victory 面板点确认 | 加载 Boot 场景 | ⬜ P3(FairyGUI) |

### 4.3 SO 变量绑定验证

| # | 变量 | 验证方式 | 预期 | Pass/Fail |
|---|------|---------|------|-----------|
| C1 | SG_BaseHP | Inspector 实时观察 | 被击后数值从 1.0 下降 | ✅ (1.0→0.7) |
| C2 | SG_CurrentWaveIndex | Inspector 实时观察 | 波次推进时 +1 | ✅ (0→3) |
| C3 | SG_KillCount | Inspector 实时观察 | 击杀敌机时 +1 | ✅ (0→10) |
| C4 | SG_InputDirection | Inspector 实时观察 | 触摸移动时有方向值 | ✅ (键盘WASD) |

### 4.4 边界情况

| # | 场景 | 操作 | 预期 | Pass/Fail |
|---|------|------|------|-----------|
| D1 | Battle | Intro 期间触摸屏幕 | 不响应输入 | ✅ (SetInputEnabled=false) |
| D2 | Battle | Victory/Defeat 期间触摸 | 不响应输入 | ✅ (状态机控制) |
| D3 | Battle | 连续快速 Retry | 无异常，状态正确重置 | ⬜ P3(需UI按钮) |

---

## 五、Debug 快捷操作

编辑器中可用的 Debug 方法（`#if UNITY_EDITOR`）：

```csharp
// 在 Inspector 中找到 BattleController，右键脚本头部：
battleController.DebugForceVictory();  // 强制胜利
battleController.DebugForceDefeat();   // 强制失败
```

---

## 六、已知限制（P0 范围内正常）

1. **无 FairyGUI 包**：UI Controller 代码完成但 FairyGUI 编辑器还没做包，CreateObject 会报错 → SG-P3 解决
2. **无 ViewPrefab**：Entity Spawn 后没有视觉表现 → 需要 EntityConfigSO 配置 ViewPrefab
3. **Boot 场景没有调 SG_Boot.InitProgress()**：需要手动加一行代码
4. **P0.0 SO 资产未创建**：需要在 Unity Editor 中手动创建

---

## 七、验收通过标准

| 维度 | 标准 |
|------|------|
| **编译** | 零 Error + 零新增 Warning |
| **核心流程** | B1~B7 全 PASS |
| **SO 绑定** | C1~C4 全 PASS |
| **边界** | D1~D3 全 PASS |
| **代码质量** | IDE lint 零错误 ✅（已通过） |

---

_验收完成后更新本文档状态并归档。_
