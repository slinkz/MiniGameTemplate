---
system: shooter-game
scope: next-phase-guide
last_verified: 2026-05-03
depends_on: [SG_DEV_PLAN, SG_TDD_INDEX, SG_TOOLS_TDD_INDEX]
---

# ShooterGame 下一阶段行动指导

> **版本**：v1.0  
> **日期**：2026-05-03  
> **前置**：SG-P0 PlayMode 验收通过 + P1/P2 验收通过 + 代码评审通过

---

## 一、当前状态总结

| 维度 | 状态 |
|------|------|
| **编译** | ✅ 零错误零警告 |
| **PlayMode** | ✅ 完整战斗循环（Intro→Playing→底线扣血→Victory/Defeat） |
| **SO 资产** | ✅ 14/14 全部创建并绑定 |
| **代码评审** | ✅ 零反模式 + 魔法字符串修复 |
| **键盘输入** | ✅ WASD 永久编辑器调试工具，与摇杆并存 |
| **待验收** | B5(Retry)/B7(Victory确认)/D3(快速Retry) — 需 FairyGUI(P3) |

---

## 二、推荐执行顺序

```
🔧 工具 P0（波次编辑器+Debug工具） → SG-P3（FairyGUI UI 层） → SG-P4（集成验收）
```

---

## 三、🔧 工具 P0 行动计划

### 目标
让关卡设计工作流顺畅，加速后续 5 关配置。

### 子任务清单

| # | 工具 | 内容 | 预估 | 验收标准 |
|---|------|------|------|---------|
| T1 | 波次编辑器：一键复制 | EntitySpawnWaveSO CustomEditor + "复制最后一波" 按钮 | 1h | Inspector 显示按钮 + 点击后新增一波 |
| T2 | 波次编辑器：统计面板 | 总敌机数 / 每波敌机数 / 总时长预估 | 0.75h | Inspector 显示统计 |
| T3 | Debug 字段 + ProfilerMarker | BattleController 核心路径加 ProfilerMarker | 30min | Profiler 可见 SG_Battle.Tick 标记 |
| T4 | 5 个 Debug MenuItem | 强制 Victory / Defeat / 跳波 / 跳关 / 重置存档 | 20min | 菜单栏可触发 |
| T5 | JoystickConfigSO | 已有代码——验证 Inspector 可用 | — | ✅ 已完成 |
| T6 | BaseLineY Gizmo | 已在 BattleController.OnDrawGizmos 中 | — | ✅ 已完成 |

### 执行指导

1. 在 `Assets/_Game/Editor/ShooterGame/` 下创建 `EntitySpawnWaveSOEditor.cs`
2. 参考 TDD：`SHOOTER_GAME/TOOLS_TDD/SG_TOOLS_TDD_01_WAVE_EDITOR.md`
3. Debug MenuItem 放在 `ShooterGame/Debug/` 菜单路径
4. ProfilerMarker 包裹 `TickPlaying` / `UpdateWaveIndex` / `BaseLineDetector.Tick`

---

## 四、SG-P3 FairyGUI 行动计划

### 前置
- FairyGUI 编辑器安装并能打开工程
- 5 个 UI 包结构已在 `SHOOTER_GAME/SG_UI_DESIGN.md` v2.0 中定义

### 子任务清单

| # | 子任务 | 内容 | 预估 | 验收标准 |
|---|--------|------|------|---------|
| P3.0 | FairyGUI 包制作 | 4 包（Loading/LevelSelect/Battle/Popup）+ Common | 4h | 4 个 .fui 导出到 Unity |
| P3.1 | Loading 接入 | LoadingScreenController 绑定 | 30min | 进度条→淡出→Battle场景 |
| P3.2 | LevelSelect 接入 | LevelSelectController 绑定 | 1h | 5 节点三态 + 点击进入 |
| P3.3 | Battle HUD 接入 | BattleHUDController 绑定 | 1h | 血条+预损+波次+飘字+暂停按钮 |
| P3.4 | 弹窗 ×3 接入 | Pause/Victory/Defeat Controller 绑定 | 1.5h | 按钮响应→流程正确 |

### 执行指导

1. **FairyGUI 编辑器**：按 `SHOOTER_GAME/SG_UI_DESIGN.md` §4~§7 创建包和组件
2. **导出路径**：`Assets/_Game/Resources/FairyGUI/` (V1 全量预加载)
3. **代码绑定**：UI Controller 已有完整代码，只需验证 FairyGUI 组件名与代码中 const 一致
4. **关键常量**（已定义在代码中）：
   - `BattleHUDController`: FGUI_PKG="Battle", FGUI_BATTLE_HUD="BattleHUD", FGUI_FLOATING_TEXT="FloatingText"
   - `JoystickController`: FGUI_PKG="Battle", FGUI_JOYSTICK="Joystick"
5. **验收**：B5(Retry) + B7(VictoryConfirm) + D3(快速Retry) 全 PASS

---

## 五、SG-P4 集成验收行动计划

### 前置
- P3 FairyGUI 全部接入
- 5 关 Level SO + 5 关 Wave SO 配置完成

### 子任务清单

| # | 子任务 | 内容 | 预估 | 验收标准 |
|---|--------|------|------|---------|
| P4.1 | 剩余 SO 资产 | SG_Level_02~05 + SG_Wave_02~05 + 子弹/震动调优 | 1h | 21 个 SO 全部存在 |
| P4.2 | 波次编排 | 5 关难度递增（怪数/速度/HP/新怪种） | 1h | 5 关可感知难度变化 |
| P4.3 | 全链路验收 | Boot→选关→1~5关→胜利/失败→回主界面 | 1h | 全流程零错误 |

### 验收标准

| 维度 | 标准 |
|------|------|
| **功能** | 17 测试用例全 PASS（B1~B7 + C1~C4 + D1~D3 + E1~E3 新增） |
| **性能** | Editor Play Mode ≥60fps（无 Profiler 开销） |
| **代码** | 编译零错误 + 零反模式 + 代码评审通过 |
| **包体** | 微信小游戏首包预估 < 10MB（FairyGUI 包 < 500KB） |
| **文档** | DEV_PLAN / ACCEPTANCE_PLAN / BOARD 全部更新 |

---

## 六、风险与注意事项

| 风险 | 影响 | 缓解 |
|------|------|------|
| FairyGUI 组件名与代码 const 不一致 | 运行时 NullRef | P3.0 后立即跑编辑器验证 |
| Wave SO 配置过难/过易 | 玩家体验 | P4.2 手动试玩调参 |
| 微信小游戏包体超标 | 审核不过 | 提前估算 FairyGUI 资源大小 |
| 热启动 Reload 问题 | 数据丢失 | V1 纯本地，V2 再加服务端校验 |

---

_本文档在每个 Phase 完成后更新状态。_
