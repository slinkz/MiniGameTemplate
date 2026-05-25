# 🔧 工具 P0 验收手册

> **版本**：v1.0 | **日期**：2026-05-04  
> **前置条件**：Unity 编辑器已打开 MiniGameTemplate 项目，编译零错误  
> **验收时长**：~15 分钟

---

## 验收概览

| # | 验收项 | 工具 | 预估 |
|---|--------|------|------|
| A1 | 波次编辑器 — 统计面板 | SG_SpawnWaveSOEditor | 2min |
| A2 | 波次编辑器 — 一键复制最后一波 | SG_SpawnWaveSOEditor | 3min |
| A3 | Debug MenuItem — 5 个菜单灰显 | SG_DebugMenuItems | 1min |
| A4 | Debug MenuItem — Play Mode 功能 | SG_DebugMenuItems | 5min |
| A5 | BattleController — ProfilerMarker | BattleController | 2min |
| A6 | BattleController — Gizmo 升级 | BattleController | 1min |
| A7 | 战斗状态监视面板 | SG_BattleStateWindow | 2min |

---

## A1：波次编辑器 — 统计面板

### 步骤

1. 在 Project 窗口找到任意 `EntitySpawnWaveSO` 资产（如 `Assets/_Game/Configs/ShooterGame/SG_Wave_01`）
2. 选中它，在 Inspector 中查看

### 预期结果

- ✅ Inspector 顶部显示**统计面板**区域（灰色 HelpBox 样式）
- ✅ 显示内容：
  - 总波次数：N 波
  - 总敌机数：M 个
  - 预估时长：仅对 `WaveTriggerMode.Timer` 类型的波次累加时间
- ✅ 如果 Waves 数组为空，显示提示："暂无波次数据"

### 失败处理

- 如果看不到统计面板 → 检查 `SG_SpawnWaveSOEditor` 的 `[CustomEditor]` 注解是否生效（可能需要重启 Unity）

---

## A2：波次编辑器 — 一键复制最后一波

### 步骤

1. 选中一个**至少有 1 波数据**的 `EntitySpawnWaveSO`
2. 在统计面板下方找到 **"复制最后一波"** 按钮
3. 记住当前波次数
4. 点击按钮

### 预期结果

- ✅ Waves 数组末尾新增一项，内容 = 最后一波的**深拷贝**
- ✅ 新波的 `Groups` 数组中每个 SpawnGroup 也是独立拷贝（修改新波不影响原波）
- ✅ Inspector 中可以 Ctrl+Z 撤销
- ✅ 资产被标记为 dirty（标题栏出现 `*` 号）

### 深拷贝验证（可选详细验证）

1. 复制后，展开新波的第一个 Group
2. 修改其 `Count` 值
3. 检查**原波对应 Group 的 Count 是否不变** → 如果不变则深拷贝正确

### 失败处理

- 按钮不出现 → Waves 可能为空（应该不显示按钮，这是正确行为）
- 修改新波影响了旧波 → 深拷贝失败，报 BUG

---

## A3：Debug MenuItem — 灰显检查

### 步骤

1. **不要进入 Play Mode**（保持编辑器模式）
2. 顶部菜单 → `ShooterGame/Debug/`
3. 观察 5 个菜单项

### 预期结果

- ✅ 5 个菜单项全部**灰显（不可点击）**：
  - Force Retry
  - Force Victory
  - Force Defeat
  - Skip Current Wave
  - Set Base HP to 50%

---

## A4：Debug MenuItem — Play Mode 功能

### 步骤

1. 打开 `Game` 场景（或含 BattleController 的场景）
2. 进入 **Play Mode**
3. 等待战斗进入 `Playing` 状态（约 1.5s Intro 后）

#### A4.1 Force Victory
4. 菜单 → `ShooterGame/Debug/Force Victory`
5. **预期**：立即进入 Victory 状态（Console 输出 `[SG] EnterState: Victory`）

#### A4.2 Force Retry
6. 菜单 → `ShooterGame/Debug/Force Retry`
7. **预期**：重置战斗，重新 Intro（`[SG] EnterState: Intro`）

#### A4.3 Force Defeat
8. 等到 Playing 状态后 → `ShooterGame/Debug/Force Defeat`
9. **预期**：进入 Defeat 状态

#### A4.4 Set Base HP to 50%
10. Retry 后回到 Playing → `ShooterGame/Debug/Set Base HP to 50%`
11. **预期**：Console 输出 `[SG Debug] BaseHP 设为 50%`，SG_BaseHP SO 的 Value 变为 0.5

#### A4.5 Skip Current Wave
12. Retry 后回到 Playing → `ShooterGame/Debug/Skip Current Wave`
13. **预期**：Console 输出 `[SG Debug] 跳过当前波次：击杀 N 个存活敌机`，所有当前存活敌机被秒杀

### 失败处理

- 菜单项仍然灰显 → 没有进入 Play Mode，或场景中没有 BattleController
- 找不到 BattleController → 检查场景中是否有 BattleController 组件

---

## A5：ProfilerMarker 验证

### 步骤

1. 进入 Play Mode（战斗进行中）
2. 打开 **Window → Analysis → Profiler**
3. 选择 **CPU** 模块，选择 **Hierarchy** 视图
4. 在搜索栏输入 `SG.`

### 预期结果

- ✅ 能看到 `SG.BattleController.TickPlaying` 条目
- ✅ 能看到 `SG.BaseLineDetector.Tick` 条目（嵌套在 TickPlaying 内部）

---

## A6：Gizmo 升级验证

### 步骤

1. 选中场景中的 **BattleController** 所在 GameObject
2. 在 **Scene 视图** 中查看

### 预期结果

- ✅ 底线位置显示**红色横线**
- ✅ 横线 X 范围 = EntitySystemBootstrap 的 KillBounds X 范围（不再是硬编码 ±20）
- ✅ 红线左上角有文字标签：`BaseLine Y=X.X`（X.X 为实际 Y 值）

---

## A7：战斗状态监视面板

### 步骤

1. 菜单 → `Window/ShooterGame/Battle State`（或 `ShooterGame/Debug/Battle State Window`）
2. 面板应该打开

### 非 Play Mode 预期

- ✅ 显示 "⚠️ 仅在 Play Mode 下显示实时数据" 或类似提示
- ✅ 显示 SO 引用搜索结果（即使不在 Play Mode 也尝试查找 SO）

### Play Mode 预期（进入 Play Mode 后）

1. 进入 Play Mode
2. 观察面板内容

- ✅ **SO 变量区**：
  - BaseHP：显示当前值（如 1.0）
  - CurrentLevel：显示当前关卡索引
- ✅ **Entity 统计区**：
  - 总存活 Entity 数
  - 敌方单位数
  - 友方单位数
  - 子弹数
- ✅ 面板**每 0.1 秒自动刷新**（数据实时变化）
- ✅ 击杀敌机后，敌方单位数实时减少

---

## 总结清单

| # | 验收项 | PASS/FAIL |
|---|--------|-----------|
| A1 | 波次统计面板显示 | ⬜ |
| A2 | 一键复制最后一波（含深拷贝） | ⬜ |
| A3 | 5 个菜单 Edit Mode 灰显 | ⬜ |
| A4.1 | Force Victory | ⬜ |
| A4.2 | Force Retry | ⬜ |
| A4.3 | Force Defeat | ⬜ |
| A4.4 | Set Base HP to 50% | ⬜ |
| A4.5 | Skip Current Wave | ⬜ |
| A5 | ProfilerMarker 可见 | ⬜ |
| A6 | Gizmo 红线 + 标签 | ⬜ |
| A7 | 战斗状态监视面板 | ⬜ |

**全部 PASS → 工具 P0 验收通过，可推进 SG-P3（FairyGUI）**
