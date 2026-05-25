# SG_TDD PK 评审记录 — 项目经理视角

> **PK 对象**：SG_TDD v1.2（5 子文件）  
> **攻方**：高级项目经理（关注任务可拆分性、验收标准、工时合理性、交付物定义）  
> **守方**：Unity 架构师（设计决策辩护）  
> **PK 状态**：✅ 已收敛（2 轮 / 10 问题 / 100% 回应）

---

## PK Round 1 — 攻方提问

### PM-001 🔴 SG-P0 工时 3h 包含 5 个独立类但缺拆分
**文档位置**：INDEX §实施优先级  
**问题**：SG-P0 阶段包含 Vector2Variable + BattleController + BaseLineDetector + BattleState 枚举，总估 3h。但 BattleController 代码量约 200+ 行（InitBattle + TickPlaying + RetryBattle + 6 个 UI 事件绑定），光编码就不止 1.5h。**缺少子任务拆分和里程碑检查点**——开发者看到"3h 完成 P0"会被吓到，或者把什么都糊到一起。  
**建议**：将 SG-P0 拆为至少 3 个子任务，每个有独立验收标准：  
- P0.1: BattleState 枚举 + Vector2Variable（30 min，编译通过）  
- P0.2: BattleController 骨架 + BaseLineDetector（1.5h，Editor PlayMode 可跑状态机）  
- P0.3: InitBattle + 击杀计数 + 重试流程（1h，重试循环可验证）

---

### PM-002 🔴 验收标准未定义——开发者如何证明"做完了"
**文档位置**：全文档  
**问题**：整个 TDD 没有一处写明"验收标准"。文档只有"行为契约"（SG-BC-01~14），但契约是设计约束，**不是可执行的验收步骤**。开发者做完 BattleController 后如何证明自己做对了？手动测？自动化？看 Inspector？GDD 中有验收描述吗？  
**建议**：每个 Phase（或每个核心类）补充"✅ Done When"列表，例如：  
- BattleController：PlayMode 进入 Battle 场景 → 状态机从 Intro→Playing 自动转换 → 底线突破扣血 → HP=0 进入 Defeat  
- 不需要 Unit Test（过重），但需要手动验收步骤的明确描述

---

### PM-003 🟡 SpawnBase/SpawnPlayer 的 EntityConfigSO 引用来源未明确交付
**文档位置**：TDD_02 §7 InitBattle + TDD_03 §4  
**问题**：InitBattle 引用 `_baseEntityConfig` 和 `_playerEntityConfig`（Inspector 拖拽），但 TDD 没有明确这两个 SO **什么时候创建、由谁创建、用什么参数**。INDEX 中 SO 资产总览列了 SG_Player/SG_Base，但具体字段值（MaxHp? Speed? ContactDamage?）散落在 GDD 而非 TDD。  
**建议**：在 Phase 安排中明确标注"依赖 SO 资产已就绪"，或把 SO 创建纳入 P0 的前置步骤。P4 虽写了"SO 资产创建"但 P0 的 BattleController 就需要用到。**循环依赖**。

---

### PM-004 🟡 GameStartupFlow 完全未定义——Boot 场景开发者从零开始
**文档位置**：TDD_01 §3 核心生命周期 + TDD_03 §2.0  
**问题**：`GameStartupFlow` 被反复引用（创建 ProgressManager、加载 FairyGUI 包、显示 LoadingScreen），但 TDD 没有给它一行代码。它是 MonoBehaviour？哪些字段？Awake/Start 里做什么？与 LoadingScreenController 的调用时序？开发者进入 SG-P3 编码 Loading/LevelSelect 时，会发现 Boot 场景没有入口点。  
**建议**：补充 `GameStartupFlow` 类设计（至少骨架级：字段、Awake 流程、静态 Progress 字段）。或标注为"P3 前置任务"单独给 30min。

---

### PM-005 🟡 SG-P3 工时 4h 包含 6 个 UI Controller + FairyGUI 包制作——严重低估
**文档位置**：INDEX §实施优先级  
**问题**：P3 列出 6 个 UI Controller（HUD→Victory→Defeat→Pause→LevelSelect→Loading），每个平均 40min。但这 **不包含 FairyGUI UI 包制作时间**（画 UI、导出 .fui 文件、配组件绑定）。每个包至少需要 30-60min 的 UI 编辑器工作。实际 P3 需 8-10h 而非 4h。  
**建议**：  
1. 将"FairyGUI 包制作"单独列为 P3.0 前置任务（含原型截图验收）  
2. 或在工时估算中区分"代码工时"和"UI 资产工时"  
3. 4h 只覆盖纯代码编写假设 FairyGUI 包已制作好

---

### PM-006 🟡 "HandleVictoryConfirm/HandleDefeatQuit" 定义为 IEnumerator 但事件注册用 Action
**文档位置**：TDD_02 §7 InitBattle vs TDD_04 §8.1  
**问题**：§7 中事件注册为 `_victoryPanel.OnConfirm += HandleVictoryConfirm;`，但 §8.1 中 HandleVictoryConfirm 定义为 `private IEnumerator HandleVictoryConfirm()`。**C# 不能将 IEnumerator 方法直接赋值给 Action 委托**。开发者会遇到编译错误。  
**建议**：在 §7 中改为 `_victoryPanel.OnConfirm += () => StartCoroutine(HandleVictoryConfirm());`，或把转场逻辑包装成 void 方法内部启动 Coroutine。

---

### PM-007 🟡 PlayerInputBridge.SetEnabled 与 JoystickController.SetEnabled 调用者不明确
**文档位置**：TDD_05 §4.2 + §3.2  
**问题**：两个类都有 `SetEnabled(bool)` 方法用于 Intro/结算禁用输入。但 TDD_02 §1.4 的 `SetInputEnabled(false)` 到底调谁的？调一个就够还是两个都要调？时序是什么？如果只禁 Bridge 不禁 Joystick，摇杆视觉还会显示但移动停了——用户困惑。如果只禁 Joystick 不禁 Bridge，但 Bridge 还在读 SO 旧值——飞机会冻住在最后方向。  
**建议**：在 BattleController.SetInputEnabled 中明确两者的调用顺序和语义。

---

### PM-008 🟡 波次追踪逻辑 §5.1 承认"Timer 模式可能不准"但无 fallback
**文档位置**：TDD_02 §5.1 待确认  
**问题**：文档明确标注"此方案在 Timer 模式时可能不准"，但没有给出确认结论。V1 五关到底用 Timer 还是 AllCleared 模式？这是 SG_Wave_01~05 配置的决定。如果开发者配了 Timer 波次（GDD §4.3 提到"Timer 波"），wave index 就会出错。**这是一个未关闭的设计风险。**  
**建议**：在 TDD 中明确标注"V1 铁律：5 关全部使用 AllCleared 推进模式，不用 Timer 波"，或给出 Timer 兼容方案。

---

### PM-009 🟢 SO 资产命名与路径一致性无校验手段
**文档位置**：INDEX §SO 资产总览 + TOOLS_TDD  
**问题**：TDD 列出了 21 个 SO 资产的名称和路径，但开发者手动创建 SO 时可能打错名（SG_BaseHp 而非 SG_BaseHP）。TOOLS_TDD 中有 FairyGUI 校验但 P2 降级了，SO 命名校验完全没提。低优——不影响功能但影响规范性。  
**建议**：P4 SO 创建步骤中加一条"验收：Editor 脚本列举所有 SG_ 开头 SO 资产，校验名称与 TDD 清单一致"。

---

### PM-010 🟢 "~14h 单人开发 2~3 天"——假设不含调试时间
**文档位置**：INDEX §实施优先级  
**问题**：14h 是纯编码估算。以我的项目经验，首次实现含调试/迭代通常需要 1.5~2x 系数（尤其 FairyGUI 的坐标系 debug + Entity 系统集成 debug）。更现实的估算是 20-28h（约 4-5 天）。这不是要求改 TDD，只是提醒**对外沟通时间线时加 buffer**。  
**建议**：INDEX 中注明"纯编码预估"和"含调试 buffer 预估"两栏。

---

**攻方总结**：2🔴 + 6🟡 + 2🟢 = 10 个问题
- 🔴 PM-001 P0 缺子任务拆分（开发者无着手点）
- 🔴 PM-002 全文缺验收标准（无法判定"完成"）
- 🟡 PM-003~008 是交付前置条件、工时准确性、接口矛盾
- 🟢 PM-009~010 是规范性和估算校准

---

## PK Round 1 — 守方回应（Unity 架构师）

| ID | 判定 | 处理摘要 |
|----|------|---------|
| PM-001 | ✅ 已修正 | INDEX §实施优先级完全重写：5 个 Phase 拆为 15 个子任务（P0.0~P4.3），每个含独立工时预估 |
| PM-002 | ✅ 已修正 | 每个子任务新增"✅ Done When"列，定义具体可验证的完成条件（如"PlayMode：Intro→Playing 自动转换"） |
| PM-003 | ✅ 已修正 | 新增 P0.0 子任务：创建 SO 模板资产（SG_Player/SG_Base/SG_Enemy_Normal/SG_BaseHP/SG_CurrentLevelIndex）作为 P0 前置 |
| PM-004 | ✅ 已修正 | TDD_01 新增 §9 `GameStartupFlow` 完整骨架设计（字段+Awake+Start+LoadFairyGUIPackages）；P1.4 为对应实施子任务 |
| PM-005 | ✅ 已修正 | P3 总工时 4h→8h（含 P3.0 FairyGUI 包制作 4h）；总工时 14h→18.5h 纯编码 / 26h 含 buffer |
| PM-006 | ✅ 已修正 | InitBattle §7 事件注册改为 `() => StartCoroutine(HandleXxx())`，消除 IEnumerator 赋值 Action 的编译错误 |
| PM-007 | ✅ 已修正 | TDD_02 新增 §7.1 `SetInputEnabled` 实现：明确两者调用顺序（禁用先 Bridge 后 Joystick；启用先 Joystick 后 Bridge） |
| PM-008 | ✅ 已修正 | TDD_03 §5.1 "待确认"→"V1 铁律：5 关全部使用 AllCleared 推进模式，不使用 Timer 波" |
| PM-009 | ✅ 已修正 | P4.3 子任务含"Editor 脚本校验 21 个 SO 名称一致"验收条件 |
| PM-010 | ✅ 已修正 | INDEX 工时表新增"含调试 buffer"列（1.4x 系数），总预估 26h / 4~5 天 |

**文档版本**：v1.2 → v1.3（6 处核心修正 + 实施表完全重构）

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

- PM-001: 🟢 满意。15 个子任务拆分粒度合理，每个 20-90min 内可完成。P0.0 的 SO 前置创建解决了循环依赖。
- PM-002: 🟢 满意。"Done When"清单足够具体（含 PlayMode 验证步骤），开发者可自测。
- PM-003: 🟢 满意。P0.0 作为 20min 的前置步骤不会拖慢节奏。
- PM-004: 🟢 满意。GameStartupFlow 骨架设计覆盖了关键决策（静态 Progress 引用、FairyGUI 包加载时序），开发者不再从零猜测。
- PM-005: 🟢 满意。8h（代码）+ 4h（FairyGUI 包制作）的拆分诚实透明。
- PM-006: 🟢 满意。lambda 包装是 C# 事件+协程的标准做法。
- PM-007: 🟢 满意。禁用/启用时序明确（先停移动再隐摇杆 / 先显摇杆再开移动），避免用户看到"摇杆可见但飞机不动"的诡异状态。
- PM-008: 🟢 满意。"V1 铁律"这个表述清晰，开发者不会误用 Timer 波。
- PM-009: 🟢 满意。集成验收时顺手做，不额外增加工作量。
- PM-010: 🟢 满意。buffer 系数 1.4x 对首次实现是合理的。

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
| **文档版本** | v1.2 → v1.3 |
| **阻塞编码的问题** | 0 个（全部已修正） |
| **攻方收敛意见** | "无新问题，PK 可以收敛" |

**结论：PK 收敛。文档 v1.3 可以进入编码。**

收敛理由：
1. 2 个 🔴 高优问题全部解决（任务拆分+验收标准——开发者现在有清晰着手点和完成判定）
2. 6 个 🟡 中优问题全部解决（前置依赖/工时/编译错误/输入时序/Timer 铁律/GameStartupFlow）
3. Round 2 攻方确认所有回应满意，无新问题

### 最有价值的 Top 3 变更
1. **实施优先级完全重构**（PM-001+PM-002）— 从 5 行粗估变为 15 个子任务 + Done When 验收清单
2. **GameStartupFlow 骨架**（PM-004）— Boot 场景入口不再是黑箱
3. **Action/IEnumerator 编译错误修复**（PM-006）— 消除必现的编译阻塞

### 遗留项
无。所有问题已在 V1 文档中修正。
