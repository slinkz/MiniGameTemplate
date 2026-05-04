# ShooterGame · 编辑器工具 TDD

> **版本**：v1.4 | **日期**：2026-05-04 | **状态**：🟢 工具 P0 实施完成 + 代码评审通过  
> **来源**：`SG_GAME_DESIGN.md` v3.2 §11 + `SG_UI_DESIGN.md` v2.0 §十一  
> **命名空间**：`Game.ShooterGame.Editor`  
> **目录**：`Assets/_Game/Editor/ShooterGame/`  
> **asmdef**：`Game.ShooterGame.Editor`（引用 Game.ShooterGame + UnityEditor）

---

## 子文件目录

| # | 文件 | 内容摘要 | 预估行数 |
|---|------|---------|----------|
| 1 | [SG_TOOLS_TDD_01_WAVE_EDITOR.md](SG_TOOLS_TDD_01_WAVE_EDITOR.md) | EntitySpawnWaveSO CustomEditor（一键复制波次 + 统计面板） | ~200 |
| 2 | [SG_TOOLS_TDD_02_DEBUG_TOOLS.md](SG_TOOLS_TDD_02_DEBUG_TOOLS.md) | Debug MenuItem + 战斗状态监视面板 + Gizmo | ~250 |

---

## 工具总览与优先级

> **PT-001 依赖铁律**：工具 P0 全部依赖核心 SG-P0.2 完成（BattleController 骨架编译通过后方可开始）。
> 推荐实施顺序：核心 P0.0~P0.3 → 工具 P0 → 核心 P1~P2 → 工具 P1 → 核心 P3~P4。

| # | 工具 | 优先级 | 预估工时 | 依赖 | TDD 子文件 | 状态 |
|---|------|--------|----------|------|-----------|------|
| 1 | EntitySpawnWaveSO 一键复制最后一波 | P0 | 1h | 框架 EntitySpawnWaveSO | 01 | ✅ |
| 2 | EntitySpawnWaveSO 总敌机/总时长统计面板 | P0 | 0.75h | 同上 | 01 | ✅ |
| 3 | `#if UNITY_EDITOR` Debug 字段 + ProfilerMarker | P0 | 30min | 核心 SG-P0.2 | 02 | ✅ |
| 4 | 5 个 Debug MenuItem | P0 | 20min | 核心 SG-P0.2 | 02 | ✅ |
| 5 | JoystickConfigSO 定义 | P0 | 15min | — | 核心 TDD_05 已覆盖 | ✅ |
| 6 | BaseLineY Gizmo 红线 | P0 | 含在 BattleController | 核心 SG-P0.2 | 02 | ✅ |
| 7 | 战斗状态监视 EditorWindow | P1 | 1h | 核心 SG-P0.3 + SO 资产 | 02 | ✅ 提前完成 |
| 8 | ~~FairyGUI 包校验 MenuItem~~ | P2 | ~~1h~~ | FairyGUI 包制作后 | 02 | ⬜ backlog |
| 9 | ~~飘字坐标 Debug.DrawLine~~ | ~~P1~~ | ~~15min~~ | — | ~~02~~ 合并到核心 P3.3 | — |
| 10 | 摇杆 Gizmo 叠加 | P1 | 30min | 核心 SG-P2.1 | 02 | ⬜ |

**P0 合计**：~2.75h（依赖核心 SG-P0.2） | **P1 合计**：~1.5h | **P2 合计**：backlog

> PT-010：飘字坐标 Debug.DrawLine（3 行代码）已合并到核心 TDD P3.3 BattleHUDController 实现中，不再单独列为工具任务。

---

## 目录结构

```
Assets/_Game/Editor/ShooterGame/
├── Game.ShooterGame.Editor.asmdef
├── SG_SpawnWaveSOEditor.cs      ← EntitySpawnWaveSO CustomEditor 增强 ✅
├── SG_EditorUtility.cs          ← 公共编辑器工具类（FindBC/EntityCount/SetSOValue）✅
├── SG_DebugMenuItems.cs         ← 5 个 Debug MenuItem ✅
├── SG_BattleStateWindow.cs      ← 战斗状态监视 EditorWindow ✅
├── SG_FairyGUIValidator.cs      ← FairyGUI 包校验 MenuItem（P2 backlog）
└── SG_GizmoDrawer.cs            ← 摇杆 Gizmo（P1 待接入）
```

---

## asmdef 配置

```json
{
    "name": "Game.ShooterGame.Editor",
    "rootNamespace": "Game.ShooterGame.Editor",
    "references": [
        "Game.ShooterGame",
        "MiniGameTemplate.Entity",
        "MiniGameTemplate.Data",
        "MiniGameTemplate.EditorTools"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false
}
```

> **PK ST-010 修正**：新增 `MiniGameTemplate.EditorTools` 引用（SG_SpawnWaveSOEditor 继承所需）。
> `Danmaku.EnumCamp` 位于 `MiniGameTemplate.Entity` 命名空间中（同 asmdef），已被间接引用覆盖。

---

## 变更日志

| 版本 | 日期 | 变更 |
|------|------|------|
| v1.0 | 2026-05-03 | 初版编辑器工具 TDD |
| v1.1 | 2026-05-03 | PK Round 1 修正：CustomEditor 继承、跳波方案、统计面板时长标注、FairyGUI 降 P2、Gizmo 动态取值、asmdef 引用补全 |
| v1.2 | 2026-05-03 | PK Round 2 修正（架构师视角）：DamageDealer 正式管线、定时刷新、字段声明去重、SG_EditorUtility 抽取、base.OnEnable、深拷贝注释、MenuItem const 化、Gizmo 缓存、EnumCamp 别名、Entity 统计拆分 |
| v1.3 | 2026-05-03 | PK Round 3 修正（PM 视角）：依赖铁律+实施顺序标注、验收步骤细化（灰显/跳波/深拷贝）、FindSOByName 假设边界、FairyGUI 草案标注、AllCleared/Timer 验收拆分、统计面板工时校准、飘字 Debug 合并到核心 P3.3 |
| v1.4 | 2026-05-04 | 🟢 工具 P0 实施完成：T1~T6 全部编码 + T7 提前完成；偏离项记录（DamageDealer API/EnumCamp/DebugRetryBattle）；新增 SG_EditorUtility.cs；代码评审通过 |
