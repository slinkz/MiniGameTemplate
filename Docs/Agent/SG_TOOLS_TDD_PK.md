# SG_TOOLS_TDD PK 评审记录

> **攻方**：软件架构师（10 年系统设计经验，专精 Editor 工具扩展、领域驱动设计）
> **守方**：Unity 编辑器工具开发者（10 年 Unity Editor 扩展经验，专精 CustomEditor/EditorWindow/Gizmo）
> **评审对象**：`SG_TOOLS_TDD_INDEX.md` + 01（波次编辑器增强）+ 02（调试工具+Gizmo）
> **最大轮次**：8
> **PK 状态**：✅ 已收敛（2 轮 / 10 问题 / 100% 回应）

---

## PK Round 1 — 攻方提问（软件架构师）

### ST-001 | 严重度 🔴高 | CustomEditor 冲突：框架已有 EntitySpawnWaveSOEditor，Game 层再注册会覆盖
**涉及章节**：TOOLS_TDD_01 §2.1/§2.2
**质疑**：TDD 设计了 `SG_SpawnWaveSOEditor : UnityEditor.Editor`，用 `[CustomEditor(typeof(EntitySpawnWaveSO))]` 装饰。但框架层已有 `EntitySpawnWaveSOEditor`（同 `[CustomEditor(typeof(EntitySpawnWaveSO))]`）。Unity 对同一类型的 CustomEditor 只会生效一个（后加载的覆盖前者），结果是：
1. 框架层的摘要面板（Wave 0/1/2... 格式化显示）**被完全覆盖**
2. 如果 TDD 中的 `DrawDefaultInspector()` 取代了框架 Editor，框架的格式化摘要消失
3. TDD §2.1 说"如果框架已有 CustomEditor，则继承扩展"——但实际代码继承的是 `UnityEditor.Editor`，不是 `EntitySpawnWaveSOEditor`

**潜在风险**：框架已有的波次摘要功能被静默吞掉
**建议方向**：方案 A：继承 `EntitySpawnWaveSOEditor` 而非 `UnityEditor.Editor`，调用 `base.OnInspectorGUI()` 保留框架功能；方案 B：不另建 CustomEditor，而是用 `[InitializeOnLoad]` + `Editor.finishedDefaultHeaderGUI` 注入额外面板

### ST-002 | 严重度 🔴高 | EntitySpawner.DebugSkipToNextWave() 不存在且实现方案不明
**涉及章节**：TOOLS_TDD_02 §2.3
**质疑**：TDD 引用 `spawner.DebugSkipToNextWave()`，但：
1. `EntitySpawner` 中不存在此方法
2. TDD 的注释说"需确认框架是否已有此方法"——这不是 TDD 应有的态度，TDD 应给出确切设计
3. 实现"跳波"需要：a) DespawnAll 当前波的敌机，b) 推进 ActiveSpawnState.CurrentWaveIndex，c) 重新 InitializeWave。但 ActiveSpawnState 是 Spawner 的 **private struct**，外部无法直接操作
4. 如果要在框架层加 DebugSkipToNextWave，这是框架修改，不是 Game 层工具——跨层耦合

**潜在风险**：编码时发现无法实现，要么改框架要么砍需求
**建议方向**：方案 A：在 EntitySpawner 中正式增加 `DebugSkipToNextWave()` 方法（最干净，但需框架层改动）；方案 B：Game 层跳波改为"秒杀全部敌机"（对 ActiveEntities 全部 TakeDamage(9999)），让框架的 AllCleared 自动推进

### ST-003 | 严重度 🟡中 | BattleController.RetryBattle() / DebugForceVictory / DebugForceDefeat 方法签名未在核心 TDD 定义
**涉及章节**：TOOLS_TDD_02 §2.1/§2.2
**质疑**：MenuItem 引用了 `bc.RetryBattle()`、`bc.DebugForceVictory()`、`bc.DebugForceDefeat()`，但核心 TDD（SG_TDD_02_BATTLE_SYSTEM.md）中：
1. `RetryBattle()` 存在（§5.1 重试流程），但 DebugForceVictory/Defeat 只在工具 TDD 中提及
2. EnterState 是 private 方法还是 public？核心 TDD 写的是 `EnterState(BattleState)` 但未标可见性
3. `#if UNITY_EDITOR` 包裹 public 方法在编辑器中可见但 build 时不存在——如果其他代码引用了这些方法，build 会报错

**潜在风险**：核心 TDD 和工具 TDD 的接口契约不同步
**建议方向**：在核心 TDD_02 的 BattleController 类定义中补充这两个调试方法的声明

### ST-004 | 严重度 🟡中 | SG_BattleStateWindow 中 FindObjectOfType 每帧调用有性能隐患
**涉及章节**：TOOLS_TDD_02 §4.2
**质疑**：`OnGUI()` 中 `var bc = FindObjectOfType<BattleController>()` 会在每帧执行一次 `FindObjectOfType`。加上末尾 `Repaint()` 强制每帧重绘，等于每帧一次全场景查找。虽然编辑器工具不影响游戏帧率，但会导致编辑器卡顿（场景对象多时明显）。

**潜在风险**：编辑器性能下降
**建议方向**：OnEnable 中缓存一次 BattleController 引用（`EditorApplication.playModeStateChanged` 回调中刷新缓存）

### ST-005 | 严重度 🟡中 | 统计面板的预估总时长计算公式不考虑 AllCleared 和 OnCallback 模式
**涉及章节**：TOOLS_TDD_01 §3.1/§3.2
**质疑**：时长计算 `Σ(TriggerDelay + max group duration)` 假设每一波都是 Timer 模式。但：
1. AllCleared 模式的时长取决于玩家击杀速度——无法预估
2. OnCallback 模式的时长取决于外部回调时机——无法预估
3. 如果 5 波中 3 波是 AllCleared，预估时长只包含了 2 波 Timer 的延迟，结果严重偏低

**潜在风险**：显示的时长误导策划
**建议方向**：显示格式改为"预估 XX 秒 (仅 Timer 波)"，或对 AllCleared 波标注"不可预估"

### ST-006 | 严重度 🟡中 | FairyGUI 校验工具的 XML 解析假设了 package.xml 路径结构
**涉及章节**：TOOLS_TDD_02 §5.3
**质疑**：
1. FairyGUI 发布后的文件结构是 `{包名}_fui.bytes` + `{包名}@atlas0.png` 等，不一定有 `package.xml` 在发布目录
2. `package.xml` 是 FairyGUI 编辑器项目文件，通常在 FairyGUI 编辑器的项目目录下，不在 Unity Assets 目录
3. TDD 的 `ValidateButtonStates` 实现只有一个 `Debug.Log` 占位——没有实际校验逻辑
4. 坦白说，V1 做这个校验器的 ROI 很低——FairyGUI 包还没做呢

**潜在风险**：V1 实施时发现文件结构不对，白费功夫
**建议方向**：将 FairyGUI 校验工具降为 P2/Backlog，等 FairyGUI 包实际制作后再编写

### ST-007 | 严重度 🟡中 | BaseLineY Gizmo 硬编码了 X 范围 [-6, 6]
**涉及章节**：TOOLS_TDD_02 §3.1
**质疑**：`Vector3 left = new Vector3(-6f, _baseLineY, 0f)` 硬编码了 X 范围。但 CameraSize=8 时可视宽度约 9 世界单位（-4.5 ~ 4.5），KillBounds 宽度是 12（-6 ~ 6）。应该取 KillBounds 或 Camera 可视范围而非硬编码。

**潜在风险**：CameraSize 或 KillBounds 修改后 Gizmo 线不匹配
**建议方向**：从 `EntitySystemBootstrap.KillBounds` 或 Camera 实际可视范围取 X 值

### ST-008 | 严重度 🟡中 | 一键复制波次的 CalculateNewDelay 逻辑对 AllCleared 波次无意义
**涉及章节**：TOOLS_TDD_01 §4.1/§4.2
**质疑**：`CalculateNewDelay` 计算新波次 TriggerDelay = 源 Delay + 源预估时长 + 3s。但如果源波次是 AllCleared 模式，TriggerDelay 本身就没有被使用（AllCleared 模式忽略 TriggerDelay，等敌机全灭才推进）。复制出来的新波次如果也是 AllCleared，那计算的 TriggerDelay 更没有意义。

**潜在风险**：策划看到自动计算的 Delay 值但不知道它不生效，产生困惑
**建议方向**：如果源波次是 AllCleared 或 OnCallback 模式，新波次 TriggerDelay 默认 0（或保持源值不变），不做自动递增

### ST-009 | 严重度 🟢低 | 摇杆 Gizmo 的 DrawDebugCircle 方法体为空
**涉及章节**：TOOLS_TDD_02 §7.2
**质疑**：`DrawDebugCircle` 方法只有注释 `// GUI.DrawTexture 或 Handles 方式绘制（需要根据实际效果调整）`，没有实际实现。OnGUI 中绘制圆需要手动画 Texture2D 或用 GL.Draw，代码量不小。

**潜在风险**：编码时才发现要补大量代码
**建议方向**：给出具体实现（推荐 GL.PushMatrix + GL.LINES 画圆弧方案），或承认 V1 只做方向线不做圆

### ST-010 | 严重度 🟢低 | asmdef references 缺少 FairyGUI 程序集引用
**涉及章节**：TOOLS_TDD_INDEX §asmdef 配置
**质疑**：asmdef 引用了 `Game.ShooterGame`、`MiniGameTemplate.Entity`、`MiniGameTemplate.Data`，但 FairyGUI 校验器（§5.3）可能不需要 FairyGUI 运行时引用（只解析 XML）。而 `SG_BattleStateWindow` 引用了 `Danmaku.EnumCamp`——这属于 `MiniGameTemplate.Danmaku` 命名空间，可能需要额外引用（取决于 asmdef 结构）。

**潜在风险**：编译时 asmdef 引用不全
**建议方向**：确认 EnumCamp 所在 asmdef 是否已被间接引用，如未被则补上

---

## PK Round 1 — 守方回应（Unity 编辑器工具开发者）

| ID | 判定 | 处理摘要 |
|----|------|---------| 
| ST-001 | ✅ 已修正 | 继承 `EntitySpawnWaveSOEditor` 而非 `UnityEditor.Editor`，`base.OnInspectorGUI()` 保留框架摘要 |
| ST-002 | ✅ 已修正 | 跳波改为秒杀全部敌机（TakeDamage(99999)），利用框架 AllCleared 自动推进，零框架改动 |
| ST-003 | ✅ 已修正 | 在核心 TDD_02 BattleController 类定义中补充 `DebugForceVictory()` / `DebugForceDefeat()` 声明 |
| ST-004 | ✅ 已修正 | BattleStateWindow 缓存 BattleController 引用 + `playModeStateChanged` 回调刷新 |
| ST-005 | ✅ 已修正 | 统计面板只统计 Timer 波时长，显示标注"仅 Timer 波" |
| ST-006 | ✅ 已修正 | FairyGUI 校验降为 P2/Backlog，等 FairyGUI 包制作后再编写 |
| ST-007 | ✅ 已修正 | BaseLineY Gizmo 从 `EntitySystemBootstrap.KillBounds` 动态获取 X 范围 |
| ST-008 | ✅ 已修正 | AllCleared/OnCallback 模式复制时保持源 TriggerDelay，不自动递增 |
| ST-009 | ✅ 已修正 | V1 只实现方向线段（DrawGUILine），死区/最大半径圆降为 P2 |
| ST-010 | ✅ 已修正 | asmdef 新增 `MiniGameTemplate.EditorTools` 引用；确认 EnumCamp 已被间接覆盖 |

**文档版本**：v1.0 → v1.1（10 处修正，FairyGUI 降级 P2）

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

- ST-001: 🟢 满意，继承方案是 Unity Editor 扩展的标准做法
- ST-002: 🟢 满意，秒杀全敌机方案简洁且零框架改动
- ST-003: 🟢 满意，核心 TDD 与工具 TDD 接口契约已同步
- ST-004: 🟢 满意，缓存 + playModeStateChanged 回调是最佳实践
- ST-005: 🟢 满意，标注清晰不误导策划
- ST-006: 🟢 满意，正确的优先级调整
- ST-007: 🟢 满意，动态取值方案抗变更
- ST-008: 🟢 满意，区分 Timer 和非 Timer 模式
- ST-009: 🟢 满意，方向线是最有用的调试信息，圆形边界属于 nice-to-have
- ST-010: 🟢 满意

### 新质疑

无新的 🔴/🟡 问题。所有问题已在 Round 1 解决。

> **PK 收敛意见**：无新问题，PK 可以收敛。

---

## PK 总结报告

| 维度 | 状态 |
|------|------|
| **PK 轮次** | 2 轮完成（Round 1 提问 + Round 2 确认收敛） |
| **总问题数** | 10 个（2🔴 + 6🟡 + 2🟢） |
| **全部回应** | 10/10 ✅ |
| **文档版本** | v1.0 → v1.1 |
| **阻塞编码的问题** | 0 个（全部已修正） |
| **攻方收敛意见** | "无新问题，PK 可以收敛" |

**结论：PK 收敛。文档 v1.1 可以进入编码。**

收敛理由：
1. 2 个 🔴 高优问题全部解决（CustomEditor 冲突 + 跳波方法不存在）
2. 6 个 🟡 中优问题全部解决（接口同步/性能/时长计算/降级/动态取值/Delay 逻辑）
3. Round 2 攻方确认所有回应满意，无新问题

### 最有价值的 Top 3 变更
1. **CustomEditor 继承方案**（ST-001）— 防止框架波次摘要面板被静默覆盖
2. **跳波改为秒杀全敌机**（ST-002）— 零框架改动，利用已有 AllCleared 机制
3. **统计面板时长标注**（ST-005）— 防止 AllCleared 模式下时长误导策划

### 遗留项
- ST-006 FairyGUI 校验：等 FairyGUI 包制作后实施（**P2 backlog**）
- ST-009 摇杆 Gizmo 圆形绘制：V2 用 GL.Draw 实现（**P2 backlog**）


