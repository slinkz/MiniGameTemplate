# PK 评审记录 — SG_SKILL_SYSTEM_GDD.md（编辑器工具开发者视角）

> **目标文档**：`Docs/Agent/SG_SKILL_SYSTEM_GDD.md` v1.4
> **文档类型**：GDD（游戏设计文档）
> **攻方角色**：Unity 编辑器工具开发者（10+ 年经验，专精 EditorWindow、PropertyDrawer、AssetPostprocessor、构建验证管线、策划工具自动化）
> **守方角色**：游戏设计师（GDD 作者，熟悉 ShooterGame 设计意图与技术约束）
> **开始时间**：2026-05-17 23:10
> **PK 状态**：✅ 已完成（2 轮收敛，GDD v1.5 已回写）
> **最大轮次**：8

---

## PK Round 1 — 攻方提问（Unity 编辑器工具开发者）

### TOOL-001 | 严重度 🔴高 | §9.2 策划工具缺乏验证/自动化层——SO 配置错误只能在 PlayMode 才发现

**涉及章节**：§9.2 / §9.4

**质疑**：GDD 列出了 7 个策划工具需求（T1~T7），但完全缺少**编辑态自动验证**层。策划每天创建/修改 SO，当前 SOP（§9.4）的最后一步都是"Play Mode 验证"。问题在于：
1. 一个 `SkillConfigSO` 的 `CooldownTime ≤ 0` 会导致除零崩溃——直到 Play Mode 才暴露
2. `BulletPatternSO` 引用为 null 的 `FireBulletsEffect` 会在技能释放时 NRE
3. `BuffConfigSO` 中 `StackMode=Stack` 但 `MaxStacks=0` 是逻辑错误
4. ~65 个 SO 全靠人工检查一致性不可行

没有 `AssetPostprocessor` 或 `OnValidate` 层级的验证，V2 阶段策划每次改参数的反馈循环 = "改 → Play → 崩溃 → 退出 → 改 → 再 Play"（2~5 分钟/次）。

**潜在风险**：V2 有 65+ SO 资产，独立开发者每天浪费 30~60 分钟在无效 PlayMode 循环上。

**建议方向**：在 §9 中补充"编辑态 SO 验证框架"设计——定义每类 SO 的 `OnValidate` / `IValidatable` 验证规则清单，把错误在 Inspector 修改时即时红框标出，而不是 PlayMode 崩溃。

---

### TOOL-002 | 严重度 🔴高 | §8.1 SO 资产 65+ 个但无命名/路径/引用一致性自动校验

**涉及章节**：§8.1

**质疑**：§8.1 列出了非常详细的 SO 路径约定（`Configs/ShooterGame/Skills/`、`Configs/ShooterGame/Buffs/` 等），但 GDD 没有任何机制保证这些约定被执行。例如：
1. 策划把 `SG_Buff_SpeedUp` 放到了 `Configs/ShooterGame/Skills/` 目录——无人发现
2. SO 命名不遵循 `SG_` 前缀
3. `DropTableSO` 引用了一个 `PickupConfigSO` 但该资产被误删
4. `SkillConfigSO.Effects[]` 中引用的 `BulletPatternSO` 来自错误目录

65 个资产手动审查 = 必然遗漏。TDD §4.3 提到了"命名校验脚本"但 GDD 本身没有定义规则。

**潜在风险**：构建前缺乏约束，有一天 CI/构建通过但运行时找不到资产——微信小游戏环境排查极其困难。

**建议方向**：在 §9 新增 T8"SO 一致性构建验证器"工具规格——定义路径规则、命名规则、引用完整性检查，并集成到 `IPreprocessBuildWithReport`。

---

### TOOL-003 | 严重度 🟡中 | §9.3 技能预览窗口缺少关键交互：无法模拟"全 build"组合效果

**涉及章节**：§9.3

**质疑**：技能预览窗口（T1）规格只支持"选中一个 SkillConfigSO → 预览弹幕"。但 V2 的核心体验是"1 技能 + 3 被动 + Buff"的组合效果。例如：
- 散射弹幕 + 穿透被动 = 弹幕穿透 2 个目标（视觉完全不同）
- 激光 + 暴击被动 = 偶尔出现暴击闪白
- 火力全开 Buff + 基础攻击 = 子弹密度翻倍

如果预览器不支持"模拟被动叠加 + Buff 状态"，策划无法在 Editor 模式下验证组合效果的视觉表现，必须每次都进 PlayMode。

**潜在风险**：组合爆炸问题——6 技能 × 4 被动 = 24 种核心组合，纯 PlayMode 验证耗时巨大。

**建议方向**：在 T1 规格中增加"Build 模拟面板"——可选择勾选 0~3 个被动 + 是否激活某个 Buff，预览器据此调整弹幕行为。

---

### TOOL-004 | 严重度 🟡中 | §5.4 BuffConfigSO + DotConfigSO 共 10 个 SO 但缺少 ID 唯一性全局视图

**涉及章节**：§5.4 / §9.2 T5

**质疑**：T5 提到了"Buff ID 冲突检测"但只说"构建前校验所有 BuffConfigSO 的 BuffId 唯一性"。现在 V1.4 拆了 `DotConfigSO`，但：
1. BuffId 和 DotId 是否共用同一个 ID 空间？还是各自独立？
2. 如果共用，"清除所有减益"按 Tag 扫描时，Buff 和 DOT 的 ID 可能冲突
3. 如果不共用，T5 需要扩展为"Buff ID + DOT ID 各自唯一性校验"

GDD 没有明确定义 ID 分配策略（范围/前缀/自增规则）。

**潜在风险**：运行时两个不同效果共享相同 ID → "刷新"逻辑错误覆盖错误 slot → 隐蔽 bug。

**建议方向**：明确 ID 分配规则（例如 Buff=1000~2999 / Debuff=3000~3999 / DOT=4000~4999），在 GDD 中写死范围约定，工具层验证不越界。

---

### TOOL-005 | 严重度 🟡中 | §7.4 PickupConfigSO 的 union-type 设计对 PropertyDrawer 不友好

**涉及章节**：§7.4

**质疑**：`PickupConfigSO` 是一个 union-type SO：根据 `PickupType` 枚举值，只有部分字段有意义：
- Buff 类型 → `BuffConfig` 有效，其余无效
- Repair → `RepairAmount` 有效
- Ammo → `AmmoBuffConfig` 有效
- Coin → `CoinAmount` 有效

默认 Unity Inspector 会显示所有字段，策划会困惑"我选了 Repair，为什么还有 BuffConfig 字段？填了会怎样？"。

**潜在风险**：策划误填无效字段（如 Repair 类型却填了 BuffConfig）→ 运行时读到非预期数据。

**建议方向**：在 §9.2 中新增工具需求"T9：PickupConfigSO 自定义 Inspector"——根据 `PickupType` 动态显示/隐藏相关字段，或者用 CustomPropertyDrawer + `PropertyAttribute` 实现条件展示。这种 union-type 模式在项目中可能复用（如 AIBehaviorSO 的 ConditionAction vs StateMachine 模式）。

---

### TOOL-006 | 严重度 🟡中 | §9.2 DPS 计算面板（T4）精度不够——不含 DOT/被动/Buff 组合 DPS

**涉及章节**：§9.2 T4 / §8.2

**质疑**：T4"DPS 计算面板"描述为"选中 EntityConfigSO → 自动计算理论 DPS"。但 §8.2 的 DPS 计算表只含基础攻击 + 技能，没有：
1. DOT DPS 贡献（燃烧 10/s、中毒 3/s、电弧 26.7/s）
2. 暴击被动期望 DPS 提升（+20% × 2.5x = +30% 期望 DPS）
3. Buff 期望 DPS（攻速翻倍 3s / 10s CD = 30% uptime → +30% DPS）

策划用当前 T4 面板算出来的 DPS 偏低，会倾向调高基础数值 → 实际战斗中 Buff/DOT/被动全上后 DPS 超标。

**潜在风险**：数值平衡基于不完整的 DPS 模型 → Playtest 阶段大量返工。

**建议方向**：T4 面板扩展为"Build DPS 模拟器"：选 1 技能 + 0~3 被动 + 假设 Buff uptime → 输出"裸 DPS"和"满 Build DPS"两个数字。

---

### TOOL-007 | 严重度 🟡中 | §3.2 AIBehaviorSO 两种模式（ConditionAction / StateMachine）的编辑体验未定义

**涉及章节**：§3.2

**质疑**：v1.4 新增了 AIBehaviorSO 支持两种模式。但 GDD 没有定义策划如何在 Inspector 中配置这两种模式：
1. StateMachine 模式需要定义状态列表 + 转换条件——Unity 默认 Inspector 对数组嵌套数组的展示极其糟糕
2. 策划如何"可视化"状态转换图？能看到哪个状态跳到哪个？
3. 如果策划误配（如 Descending 状态没有设退出条件）→ 敌机永远下落不悬停

**潜在风险**：AIBehaviorSO 配置复杂度 > 简单数值 SO → 策划需要专用编辑器支持，否则会频繁配错。

**建议方向**：在 §9.2 新增"T10：AI 行为编辑器（EditorWindow 或 Custom Inspector）"——至少做到状态列表可视化 + 转换条件的下拉选择 + 配置有效性校验。

---

### TOOL-008 | 严重度 🟢低 | §8.1 SO 资产缺少"批量创建向导"——65 个 SO 手动创建效率低

**涉及章节**：§8.1 / §9.2 T6

**质疑**：T6"技能快速创建向导"只覆盖 SkillConfigSO。但 V2 新增大量同质化 SO（4 PassiveAbilitySO、3 DotConfigSO、4 PickupConfigSO、7 BuffConfigSO）。每个都要：
1. Create → Asset Menu
2. 命名
3. 设置路径
4. 填初始值
5. 挂引用

手动操作 ~40 个新 SO × 5 步 = ~200 次编辑器操作。

**潜在风险**：不是阻塞问题，但初始资产创建阶段浪费 2~3 小时。

**建议方向**：T6 从"技能快速创建向导"升级为"SO 批量创建向导"——输入 SO 类型 + 命名模板 + 数量 → 一键生成到正确目录。适用所有 ShooterGame SO 类型。

---

### TOOL-009 | 严重度 🟢低 | §10.2 美术资源命名/规格约束只靠 SOP 文字——无导入自动检测

**涉及章节**：§10.2 / §10.3

**质疑**：§10.3 定义了美术 SOP（如子弹 12×12 px、道具 24×24 px、不使用 `_N` 后缀），但这些规则只写在文档里。独立开发者可能某天忘了规格导入了 48×48 的子弹 sprite → 运行时看着不对才发现。

如果有 AssetPostprocessor 在导入时自动校验 ShooterGame 特定路径下的 sprite 尺寸，就能在导入时即刻警告。

**潜在风险**：低风险，独立开发者规模小。但如果后续有美术外包参与则变为中风险。

**建议方向**：在 §10.2 中新增 A6"ShooterGame 资源导入校验器"——针对 `Assets/_Game/Sprites/ShooterGame/` 路径下的 sprite 做尺寸/命名规则自动检查。

---

### TOOL-010 | 严重度 🟢低 | §14 风险表没有"工具链"维度——策划工具缺失/延迟的风险未评估

**涉及章节**：§14

**质疑**：风险表 R1~R5 都是运行时风险，没有评估"工具链延迟"风险：
- Sprint 5 安排了全部策划工具（T1~T7，17h）
- 如果 Sprint 5 超时，策划工具推迟——但 Sprint 4 的数值平衡 Playtest 就没有工具辅助
- 无工具的 Playtest = 盲调 → 平衡工作多一倍时间

Sprint 4（数值平衡）在 Sprint 5（策划工具）之前，这意味着最需要工具的时候没有工具。

**潜在风险**：Sprint 4 的 3h Playtest 预估可能膨胀到 6h+。

**建议方向**：考虑将 T4（DPS 计算面板）和 T5（BuffId 冲突检测）提前到 Sprint 3 同期，因为 Sprint 3 正好在做 Buff/DOT/被动——正是最需要验证的时候。在 §14 新增 R6 记录此风险。

---

> **攻方 Round 1 总结**：10 个问题（🔴2 / 🟡5 / 🟢3）。核心关注点是——GDD 对"运行时战斗体验"设计得极其详细，但对"策划日常开发体验"（编辑态验证、工具反馈循环、配置一致性保障）几乎没有投入。V2 有 65+ SO，独立开发者兼任策划，如果没有编辑器工具自动化兜底，每天会在"改配置→PlayMode→崩溃→退出→改→再来"的循环中浪费大量时间。

---

## PK Round 1 — 守方回应（游戏设计师）

| ID | 判定 | 回应摘要 |
|----|------|---------|
| TOOL-001 | ✅ 接受 | 新增 §9.5 编辑态 SO 验证框架——每类 SO 定义 OnValidate 规则 + 构建卡口 |
| TOOL-002 | ✅ 接受 | 新增 T8（SO 一致性构建验证器）——路径/命名/引用完整性 + IPreprocessBuildWithReport |
| TOOL-003 | ⚠️ 部分接受 | T1 增加"被动模拟开关"面板；Buff 模拟留 V3（独立开发者工时约束） |
| TOOL-004 | ✅ 接受 | 新增 §9.6 ID 分配约定：Buff=1000~2999 / Debuff=3000~3999 / DOT=4000~4999 |
| TOOL-005 | ✅ 接受 | 新增 T9（PickupConfigSO 自定义 Inspector），优先级 P1 |
| TOOL-006 | ⚠️ 部分接受 | T4 面板输出"裸 DPS"+"含被动期望 DPS"；完整 Build 模拟器留 V3 |
| TOOL-007 | ✅ 接受 | 新增 T10（AI 行为编辑器），优先级 P2（V2 只有 3 个 AIBehaviorSO） |
| TOOL-008 | ✅ 接受 | T6 范围扩展为"SO 批量创建向导"（所有 ShooterGame SO 类型） |
| TOOL-009 | ⚠️ 部分接受 | 新增 A6（资源导入校验器），优先级 P3（外包参与时再做） |
| TOOL-010 | ✅ 接受 | T5 提前到 Sprint 3 / T4 提前到 Sprint 4 / 新增 R6 风险条目 |

**GDD 更新**：v1.4 → v1.5（编辑器工具开发者 PK 回写）

---

## PK Round 2 — 攻方复审（Round 1 评估 + 新问题）

### Round 1 回应评估

| ID | 评估 | 理由 |
|----|------|------|
| TOOL-001 | 🟢 满意 | §9.5 验证规则表清晰完整可直接编码 |
| TOOL-002 | 🟢 满意 | T8 规格明确，IPreprocessBuildWithReport 集成方案合理 |
| TOOL-003 | 🟢 满意 | 被动模拟开关是合理折中 |
| TOOL-004 | 🟢 满意 | §9.6 ID 范围明确 |
| TOOL-005 | 🟢 满意 | T9 定义清晰 |
| TOOL-006 | 🟢 满意 | 两数字方案务实 |
| TOOL-007 | 🟢 满意 | P2 合理 |
| TOOL-008 | 🟢 满意 | 范围扩展合理 |
| TOOL-009 | 🟢 满意 | P3 合理 |
| TOOL-010 | 🟢 满意 | Sprint 前移 + R6 到位 |

**全部 10/10 🟢 满意。**

---

### 新问题

### TOOL-011 | 严重度 🟡中 | §9.4 "新增 Buff/DOT" SOP 与 v1.4 DotConfigSO 拆分设计脱节

**涉及章节**：§9.4 / §5.4

**质疑**：§9.4 SOP 步骤 2 仍写着"如果是 DOT → 勾选 IsDot → 填 DotDamage/DotInterval"——但 v1.4 已将 DOT 拆为独立 `DotConfigSO`，不再是 BuffConfigSO 的字段。SOP 与实际设计不一致。

**潜在风险**：策划（或未来的自己）照着过时 SOP 操作会卡住。

**建议方向**：更新 §9.4 "新增一个 Buff/DOT" SOP，拆为两个独立流程。

---

### TOOL-012 | 严重度 🟢低 | §9.5 DropTableSO 可增加引用计数信息展示

**涉及章节**：§9.5

**质疑**：DropTableSO 校验规则缺少"引用的 PickupConfigSO 被几个 DropTable 共享"的信息展示。两个 DropTable 引用同一个 PickupConfigSO 可能是设计意图也可能是误操作。

**潜在风险**：极低——独立开发者只有 2 个 DropTable。

**建议方向**：在 Buff 速览面板（T2）或 T8 中加一个引用计数列。纯信息，不阻断。可选做。

---

> **PK Round 2 收敛评估**：Round 1 全部 🟢 满意。Round 2 新问题仅 🟡1 + 🟢1，无 🔴 高优。严重度明显下降趋势（Round 1: 2🔴5🟡3🟢 → Round 2: 0🔴1🟡1🟢）。**攻方建议收敛**——TOOL-011 是 SOP 文字修正，TOOL-012 是可选信息展示。

---

## PK Round 2 — 守方回应（游戏设计师）

| ID | 判定 | 回应摘要 |
|----|------|---------|
| TOOL-011 | ✅ 接受 | §9.4 SOP 已拆分为独立的"新增 Buff"和"新增 DOT"两个流程 |
| TOOL-012 | ✅ 接受（记录为 V3 可选） | 在 T2/T8 中加引用计数为纯信息展示，V2 不做（仅 2 个 DropTable），V3 外包参与后视需求 |

---

## PK 总结报告

| 维度 | 状态 |
|------|------|
| **PK 轮次** | 2 轮完成 |
| **总问题数** | 12 个（Round 1: 10 + Round 2: 2） |
| **严重度分布** | 🔴2 + 🟡6 + 🟢4 |
| **全部回应** | 12/12 ✅（7 接受 + 3 部分接受 + 2 接受但延后） |
| **拒绝** | 0 个 |
| **文档版本** | v1.4 → v1.5 |
| **阻塞编码的问题** | 0 个（Round 1 的 2 个 🔴 已解决） |
| **攻方收敛意见** | "Round 1 全部满意，Round 2 无高优新问题，建议收敛" |

### 最有价值的 Top 3 变更

1. **§9.5 编辑态 SO 验证框架**——65+ SO 配置错误从 PlayMode 崩溃→Inspector 即时标红，每次调参节省 2~5 分钟
2. **§9.6 ID 分配约定 + T5 前移到 Sprint 3**——Buff/DOT/Debuff 数据安全有框架级保障，最需要的时候有工具
3. **T8 SO 一致性构建验证器**——路径/命名/引用完整性在构建时自动卡口，防止运行时资产缺失

### 遗留项

| 项 | 优先级 | 说明 |
|----|--------|------|
| T10 AI 行为编辑器 | P2 | V2 只有 3 个 AIBehaviorSO，用数组配置顶住，V3 敌机类型增多后必做 |
| T6 SO 批量创建向导 | P2 | 便利工具，不影响功能正确性 |
| A6 资源导入校验器 | P3 | 外包参与时提升为 P1 |
| TOOL-012 引用计数展示 | V3 可选 | 纯信息，不阻塞 |

**结论：PK 收敛。文档 v1.5 可以进入编码。**

收敛理由：
1. 🔴 高优问题数：Round 1 = 2 → Round 2 = 0（单调递减）
2. 总问题数：Round 1 = 10 → Round 2 = 2（明显收敛）
3. 攻方明确表态"建议收敛"
4. 所有问题已回应，无阻塞编码的未解决项





