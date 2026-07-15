# PK 评审记录 — SG_TDD 核心技术设计文档（第二轮）

> **目标文档**：`SHOOTER_GAME/TDD/SG_TDD_INDEX.md` + `SG_TDD_01~05`
> **文档类型**：TDD
> **攻方角色**：Unity 编辑器工具开发者（10 年 Unity Editor 扩展经验，专精 Inspector 工作流、CustomEditor、SerializedProperty、调试工具）
> **守方角色**：Unity 架构师（10 年 Unity 开发经验，专精 Entity 框架、FairyGUI、微信小游戏）
> **开始时间**：2026-05-03 18:15
> **PK 状态**：✅ 已收敛（2 轮 / 10 问题 / 100% 回应）
> **上下文**：基于 v1.1（已经过软件架构师 PK 修正），本轮从编辑器工具/可实施性视角进行补充审查

---

## PK Round 1 — 攻方提问（Unity 编辑器工具开发者）

### ET-001 | 严重度 🔴高 | BattleController 字段声明重复编译错误
**涉及章节**：TDD_02 §1.2 BattleController 类定义
**质疑**：类中 `public BattleState CurrentState { get; private set; }` 出现了**两次**（第 59 行和第 68 行）。这会导致编译错误 CS0102（同名成员重复定义）。按 TDD 原样写代码无法编译。
**潜在风险**：阻塞编码，开发者必须判断哪个是多余的
**建议方向**：删除第二处重复声明，保留第 59 行运行时状态区的声明

### ET-002 | 严重度 🔴高 | BattleController 引用了 6 个 UI Controller 但无 [SerializeField] 声明
**涉及章节**：TDD_02 §1.4 EnterState + §8 转场编排（TDD_04）
**质疑**：`EnterState` 中调用了 `ShowDefeatPanel()`、`ShowVictoryAfterDelay()` 等方法，转场编排中调用了 `BattleHUDController.ForceRefresh()`，但 BattleController 类定义中**没有任何 UI Controller 的 [SerializeField] 字段或获取方式**。实际编码时开发者不知道这些 UI Controller 如何进入 BattleController——是 Inspector 拖拽？FindObjectOfType？还是事件系统？
**潜在风险**：实现时需要自行补全所有 UI 引用方式，不同开发者可能选择不同路径
**建议方向**：在 BattleController §1.2 中显式声明 UI Controller 的引用方式和字段

### ET-003 | 严重度 🟡中 | BaseLineDetector 持有 CameraShaker 引用违反 SRP
**涉及章节**：TDD_02 §2.2 BaseLineDetector.Init()
**质疑**：BaseLineDetector 是纯 C# 底线检测类，但 Init 方法要求传入 `CameraShaker` 和 `ScreenShakeConfigSO`，在检测逻辑内直接调用 `_shaker.Shake()`。这让一个"检测器"承担了"触发视觉反馈"的职责，违反 SRP。若未来需要在底线突破时触发其他效果（音效、粒子），必须修改 BaseLineDetector——这是不应该的。
**潜在风险**：BaseLineDetector 变成上帝类，每加一种反馈就得改 Init 签名
**建议方向**：BaseLineDetector 只负责检测并返回突破信息（哪些敌机、造成多少伤害），由 BattleController 决定触发什么反馈

### ET-004 | 严重度 🟡中 | CameraShaker.Awake() 缓存 _originalPos 在场景加载瞬间可能不准
**涉及章节**：TDD_02 §3.2 CameraShaker.Awake()
**质疑**：`_originalPos = transform.localPosition;` 在 Awake 中执行。如果场景中 Camera 被其他脚本在 Awake 中修改位置（如 CinematicIntro 或其他初始化），缓存的 _originalPos 可能不是"真正的静止位置"。且**重试时**（RetryBattle）如果不调 StopShake() 再重新缓存，_originalPos 可能已经是偏移值。
**潜在风险**：重试后相机漂移、震动基准错误
**建议方向**：1) 确认 RetryBattle 流程中有调 StopShake()（§5.1 未明确写出）；2) 提供 ResetOriginalPosition() 方法或在 StopShake() 中确保复位到初始值

### ET-005 | 严重度 🟡中 | ProgressManager.MaxUnlockedLevel 硬编码 5 关上限
**涉及章节**：TDD_03 §2.2 MaxUnlockedLevel
**质疑**：`for (int i = 1; i <= 5; i++)` 硬编码了 5 关上限。虽然 V1 确实只有 5 关，但这是唯一的硬编码点——BattleController 用 `_levelConfigs.Length` 动态获取关卡数。若 V2 加关，MaxUnlockedLevel 不改就返回错误值。这种**不对称**容易遗漏。
**潜在风险**：V2 扩展时 MaxUnlockedLevel 上限还是 5
**建议方向**：接受总关卡数参数 `MaxUnlockedLevel(int totalLevels)` 或从 SG_LevelConfigSO[] 数组长度获取

### ET-006 | 严重度 🟡中 | 飘字池的 TweenFade OnComplete 在回收时序上有竞态
**涉及章节**：TDD_04 §4.4 ShowFloatingText
**质疑**：飘字使用 FIFO 环形缓冲，`ShowFloatingText` 在 `_floatingTextHead` 位置复用飘字对象。但如果上一个飘字的 TweenFade(0.8s) 还没完成，新飘字就复用了同一个 slot——此时旧 Tween 的 OnComplete 回调会把**新飘字**设为 `visible = false`。这是一个典型的 Tween 竞态问题。
**潜在风险**：高频突破底线时飘字闪烁或消失
**建议方向**：在复用前 Kill 旧 Tween（FairyGUI: `ft.TweenKillAll()` 或 `GTween.Kill(ft)`），然后再重置状态

### ET-007 | 严重度 🟡中 | JoystickController.Init() 需要 battleHUD 参数但 TDD 未说明谁创建 HUD
**涉及章节**：TDD_05 §3.2 JoystickController.Init(GComponent battleHUD)
**质疑**：摇杆的 Init 需要传入 `battleHUD` GComponent，但 TDD_04 中 BattleHUDController 的 `_view` 是私有字段，且在构造中才创建。没有说明 JoystickController 如何拿到 BattleHUDController 的 `_view` 引用。是 BattleController 协调？还是 JoystickController 自己创建 GGraph 挂到 GRoot？
**潜在风险**：两个 Controller 初始化顺序耦合，实现时需要自行发明桥接方式
**建议方向**：在 BattleController 的 InitBattle 流程中补充 UI 初始化时序和 JoystickController.Init 的调用点

### ET-008 | 严重度 🟡中 | LevelSelectController.OnLevelClicked 直接调 SceneManager.LoadScene 跳过过渡
**涉及章节**：TDD_04 §3.2 LevelSelectController.OnLevelClicked
**质疑**：代码中直接 `SceneManager.LoadScene(_battleSceneName);`，但 TDD_04 §8.2 转场时序表定义了"选关→战斗"需要 `LevelNode 缩放 0.2s → 白闪 0.1s → LoadScene + 淡入 0.2s = 0.5s`。代码与转场时序表矛盾——实际应该是 Coroutine 驱动的转场序列，不是同步 LoadScene。
**潜在风险**：开发者按 §3.2 代码实现会跳过转场动画
**建议方向**：修改 OnLevelClicked 为启动转场 Coroutine，或注明 §3.2 是简化伪代码、实际转场逻辑在 §8

### ET-009 | 严重度 🟢低 | BattleController 有 20+ 个 [SerializeField]，Inspector 体验差
**涉及章节**：TDD_02 §1.2 + TDD_01 §8
**质疑**：BattleController 目前有 _levelConfigs / _currentLevelIndex / _baseHP / _currentWaveIndex / _totalWaveCount / _killCount / _totalEnemyCount / _cameraShaker / _shakeConfig / _introDuration / _victoryDelay / _baseLineY / _baseEntityConfig / _playerEntityConfig + 未来补充的 6 个 UI Controller 引用 = **20+ 个 SerializeField**。这在 Inspector 中很难操作，也容易误连引用。
**潜在风险**：配置出错概率高，新手难上手
**建议方向**：使用 `[Header]` 分组（已部分使用）+ 考虑将 SO 变量引用收到一个 `BattleConfigSO` 中减少 Inspector 字段数

### ET-010 | 严重度 🟢低 | Vector2Variable.Value 用 == 比较两个 Vector2 没有容差
**涉及章节**：TDD_05 §1.1 Vector2Variable.Value setter
**质疑**：`if (_value == value) return;` — Unity 的 `Vector2 ==` 内置了 1e-5 容差，这对于摇杆这种高频微小变化的场景可能过于宽松，导致部分微调被吞掉。但反过来如果换成逐分量精确比较又会导致大量冗余事件。这里需要**明确设计意图**。
**潜在风险**：如果摇杆在死区边缘微抖，可能出现事件被吞或过度触发
**建议方向**：在文档中明确"使用 Unity 默认 == 容差"的设计决策，或加注释说明为什么可以接受

---

**攻方总结**：2🔴 + 6🟡 + 2🟢 = 10 个问题
- 🔴 ET-001 属性重复声明是阻塞级编译错误
- 🔴 ET-002 UI Controller 引用缺失会导致开发者编码时"无从下手"
- 🟡 ET-003~008 是实施可靠性和工作流问题
- 🟢 ET-009~010 是体验优化建议

---

## PK Round 1 — 守方回应（Unity 架构师）

| ID | 判定 | 处理摘要 |
|----|------|---------|
| ET-001 | ✅ 已修正 | 删除重复的 `CurrentState` 属性声明，保留运行时状态区的唯一定义 |
| ET-002 | ✅ 已修正 | 在 BattleController 补充 6 个 UI Controller 的 `[SerializeField]` 字段声明（Inspector 拖拽方式） |
| ET-003 | ✅ 已修正 | BaseLineDetector.Init 移除 CameraShaker/ScreenShakeConfigSO 引用；新增 `BreachCountThisFrame` 属性；BattleController.TickPlaying 中由自身触发屏幕震动 |
| ET-004 | ✅ 已修正 | RetryBattle §5.1 时序中新增 step 3b `_cameraShaker.StopShake()` 确保相机复位 |
| ET-005 | ✅ 已修正 | `MaxUnlockedLevel` 改为方法 `MaxUnlockedLevel(int totalLevels)`，参数由调用方从 `_levelConfigs.Length` 获取 |
| ET-006 | ✅ 已修正 | 飘字池复用前调用 `ft.TweenKillAll()` 终止旧 Tween，防止 OnComplete 回调竞态 |
| ET-007 | ✅ 已修正 | 1) InitBattle 末尾补充完整 UI 初始化时序（Show→Init→事件注册）；2) BattleHUDController 新增 `public GComponent View => _view;` getter |
| ET-008 | ✅ 已修正 | LevelSelectController.OnLevelClicked 改为启动 `TransitionToBattle()` Coroutine，与 §8.2 转场时序表一致 |
| ET-009 | ✅ 已标注 | BattleController 字段区补充"V2 考虑收拢到 BattleConfigSO"注释，V1 保持当前结构 |
| ET-010 | ✅ 已标注 | Vector2Variable.Value setter 补充设计意图注释：Unity 默认容差对摇杆场景足够 |

**文档版本**：v1.1 → v1.2（10 处修正 / 2🔴 编译级 + 6🟡 可靠性 + 2🟢 体验）

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

- ET-001: 🟢 满意，编译错误消除
- ET-002: 🟢 满意，Inspector 拖拽方式明确，开发者知道去哪找引用
- ET-003: 🟢 满意，BaseLineDetector 现在是纯检测器，BattleController 负责反馈编排——未来加音效/粒子只改 BattleController
- ET-004: 🟢 满意，重试流程时序现在完整闭环
- ET-005: 🟢 满意，参数化方案简洁且向前兼容
- ET-006: 🟢 满意，`TweenKillAll()` 是 FairyGUI 的标准做法
- ET-007: 🟢 满意，UI 初始化时序清晰（先 Show 创建 View → 再 Init 注入 → 最后注册事件）
- ET-008: 🟢 满意，Coroutine 与转场时序表对齐
- ET-009: 🟢 满意，V1 不过度设计，V2 方向明确
- ET-010: 🟢 满意，设计意图明确有文档注释

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
| **文档版本** | v1.1 → v1.2 |
| **阻塞编码的问题** | 0 个（全部已修正） |
| **攻方收敛意见** | "无新问题，PK 可以收敛" |

**结论：PK 收敛。文档 v1.2 可以进入编码。**

收敛理由：
1. 2 个 🔴 高优问题全部解决（重复声明编译错误 + UI Controller 引用缺失）
2. 6 个 🟡 中优问题全部解决（SRP 修正 / 重试复位 / 参数化 / Tween 竞态 / UI 时序 / 转场矛盾）
3. Round 2 攻方确认所有回应满意，无新问题

### 最有价值的 Top 3 变更
1. **BaseLineDetector SRP 修正**（ET-003）— 检测器不再耦合视觉反馈，未来扩展只改 BattleController
2. **UI Controller 引用方式明确**（ET-002）— 开发者编码时不再困惑引用从哪来
3. **飘字 Tween 竞态修复**（ET-006）— 防止高频底线突破时飘字闪烁消失

### 遗留项
- ET-009 BattleConfigSO 收拢 SO 字段：**V2 backlog**（V1 用 [Header] 分组即可）
</content>
</invoke>