# PK 评审记录 — Entity-Component TDD v2.5（Round 6：策划工作流落地性）

> **目标文档**：`Docs/Agent/ENTITY_COMPONENT_TDD.md` v2.5
> **文档类型**：TDD
> **攻方角色**：游戏设计师（10+ 年经验，专精策划工作流、功能可落地性验证、Editor UX）
> **守方角色**：Unity 编辑器开发者（10+ 年经验，专精 CustomEditor / EditorWindow / SerializedProperty / Gizmo）
> **开始时间**：2026-04-27 00:19
> **最大轮次**：3
> **PK 状态**：🔄 进行中
> **评审重点**：策划工作流程、功能实现后是否可落地可验证

---

## PK Round 1 — 攻方提问

### WF-001 | 严重度 🔴高 | 策划首次配置闭环缺少"胶水层"引导——从 SO 到"看到 Entity 在场景中跑起来"之间存在断层

**涉及章节**：§十 10.1、§十 10.2、§3.14、§3.15
**质疑**：§十 描述了"端到端流程"，但策划按照 10.1 和 10.2 的步骤操作后，会卡在"Play Mode → 观察波次按配置生成"。原因：EntityManager、EntitySpawner、EntityViewBridge 全部是纯 C# 对象（非 MonoBehaviour），需要一个"游戏层 MonoBehaviour"来初始化和驱动。但 TDD 没有定义这个"胶水层"是什么。策划做完所有配置后点 Play → 什么都不会发生。
**潜在风险**：策划工作流的第一个完整闭环的最后一环断裂——策划做了所有配置，点 Play 什么都没有，完全不知道哪里出了问题。
**建议方向**：Phase 1 定义一个 `EntitySystemBootstrap`（MonoBehaviour），策划拖到场景中即可激活整个 Entity 系统；§十 策划工作流中明确写出这一步。

---

### WF-002 | 严重度 🔴高 | Play Mode 修改 SO 对已有 Entity 不生效的反馈完全缺失——策划会反复改参数却看不到变化

**涉及章节**：§十 10.3
**质疑**：§10.3 写道"运行时修改 SO 参数对已存在的 Entity 不生效"。Unity 策划的核心工作习惯是"Play Mode 里调参数看效果"。Entity 框架的 SO 快照模式完全违反这个直觉，且 EntityConfigSOEditor 中没有任何 Play Mode 警告。策划改参数 → 没反应 → 困惑 → 找程序。
**潜在风险**：策划迭代效率严重受阻。HotReloadConfig 被标为 Phase 2 可选，但 Play Mode 提示是 Phase 1 刚需。
**建议方向**：(1) EntityConfigSOEditor 在 Play Mode 下显示黄色 HelpBox 提示；(2) EntityDebugWindow 增加"Restart All Waves"按钮；(3) 或在 Play Mode 下 Inspector 底部显示"Apply to All"按钮（遍历活跃 Entity 重新读 SO）。

---

### WF-003 | 严重度 🔴高 | AttackBulletType 字段类型为 VFXTypeSO 而非弹幕系统的 BulletTypeSO——策划拖错资产无法发现

**涉及章节**：§5.0、§4.9
**质疑**：§5.0 声明 `public VFXTypeSO AttackBulletType`，但 AttackComponent 调用 `DanmakuSystem.Instance.Fire(_bulletType, ...)`。VFXTypeSO（特效系统）和 BulletTypeSO（弹幕系统）是完全不同的类。策划拖入 VFX 特效到 AttackBulletType 字段，类型检查通过但运行时弹幕系统不认。
**潜在风险**：字段类型写错导致编译报错或运行时静默失败。
**建议方向**：明确 AttackBulletType 应为 BulletTypeSO 类型，TDD §5.0 修正。

---

### WF-004 | 严重度 🟡中 | EntityManagerAccessor 是未定义的"静态访问点"——Gizmo 和 DebugWindow 的运行前提不明

**涉及章节**：§9.1、§9.6
**质疑**：EntityGizmoDrawer 依赖 `EntityManagerAccessor.Instance`，但 TDD 中没有定义这个类。如果 WF-001 的胶水层没配对，Gizmo 和 DebugWindow 静默失效——策划看到空白 Scene View 和 Debug 窗口，没有任何提示。
**潜在风险**：两个核心调试工具悬挂在未定义的访问点上。
**建议方向**：TDD 明确 EntityManagerAccessor 的设计；DebugWindow 和 Gizmo 在 Instance == null 时显示明确提示。

---

### WF-005 | 严重度 🟡中 | AIBehaviorSO 条件-动作表缺少"兜底条件必须存在"的强制校验——策划漏配 Always 导致 AI 卡死

**涉及章节**：§4.7、§9.4
**质疑**：如果策划只配了 TargetInRange→Attack 和 HpBelow→Flee，没有 Always→Idle 兜底，所有条件不匹配时 AI 行为未定义。§9.4 将"至少有一个 Always 兜底条件"标记为 Warning 而非 Error。
**潜在风险**：策划配 AI 时容易忘加兜底，运行时 AI 间歇性"发呆"。
**建议方向**：(1) AIComponent 代码中明确 fallback 行为（默认 Idle）；(2) Validator 将缺少 Always 提升为 Error；(3) AIBehaviorSOEditor 显示红色 HelpBox。

---

### WF-006 | 严重度 🟡中 | EntityConfigSO 新建时所有字段为默认值，策划不知道哪些是"必须改"的——缺少必填项指引

**涉及章节**：§5.0、§9.2、§十 10.1
**质疑**：策划新建 SO 后 Components = 空数组。EntityConfigSOEditor 的条件显示基于"Components 中有什么"来联动——如果 Components 空，所有联动不触发，Inspector 看起来很"干净"，策划反而觉得"配好了"。
**潜在风险**：新建 SO 后首次体验是空白，策划不知道第一步该做什么。
**建议方向**：(1) Components 为空时显示大红色 HelpBox；(2) SOCreationWizard 创建时预填默认组件；(3) §10.1 明确标注必填字段。

---

### WF-007 | 严重度 🟡中 | 特效/弹幕引用字段（PoolDefinition / VFXTypeSO）对策划不友好——Inspector 中看不出该拖什么资产

**涉及章节**：§5.0
**质疑**：PoolDefinition 字段对策划认知负担大——策划想配"死亡爆炸特效"，但字段类型是 PoolDefinition 不是"特效 Prefab"。Object Picker 中无法区分用途。拖错不会报错但行为完全错误。
**潜在风险**：间接引用增加理解成本和出错概率。
**建议方向**：(1) EntityConfigSOEditor 为 PoolDefinition 字段增加只读预览行（显示 Prefab 名称）；(2) Tooltip 明确说明用途。

---

### WF-008 | 严重度 🟡中 | 策划工作流§10.1 到§10.2 的"串联"依赖关系不清——跨 SO 引用链没有可视化

**涉及章节**：§十 10.1、§十 10.2
**质疑**：完整配置涉及 SO 引用链 SpawnWaveSO→EntityConfigSO→AIBehaviorSO→BulletTypeSO→PoolDefinition。策划修改共享的 AIBehaviorSO 不知道会影响多少怪物类型。
**潜在风险**：共享资产修改影响范围不透明。
**建议方向**：(1) Validator 增加反向引用查询输出；(2) §10.1 增加依赖图示意。

---

### WF-009 | 严重度 🟡中 | 新手策划的"30 分钟上手路径"不存在——缺少 Quick Start 或 Demo 场景的引导文档

**涉及章节**：§十、§六 P1.11
**质疑**：§十 读起来是"功能规格说明"而非"新手引导"。前置知识不明（BulletTypeSO / PoolDefinition / EnumCamp 是外部系统概念）。没有可复制的模板资产。MODULE_README.md 内容不明。
**潜在风险**：策划上手时间远超 30 分钟。
**建议方向**：(1) P1.11 AC 增加"Demo SO 保留为模板"；(2) MODULE_README.md 含 Quick Start 段落；(3) 或 SOCreationWizard 增加"从模板创建"。

---

### WF-010 | 严重度 🟢低 | SOCreationWizard 的 savePath 需要策划手动输入路径字符串——不如右键菜单直观

**涉及章节**：§9.7
**质疑**：SOCreationWizard 的 savePath 是纯文本输入框，策划可能拼错路径。而所有 SO 都已有 [CreateAssetMenu]，右键菜单更直观。
**潜在风险**：两套创建入口体验不一致，但右键菜单已可用，不阻塞。
**建议方向**：§十 工作流统一推荐右键菜单方式，Wizard 作为备选。

---

### WF-011 | 严重度 🟢低 | Components CheckboxGrid 与 Inspector 下方字段的联动关系没有视觉引导

**涉及章节**：§9.2
**质疑**：策划勾选组件后下方突然多出字段，没有视觉线索说明"这些字段是因为勾了什么而出现的"。同时勾 3 个组件时十几个字段分不清归属。
**潜在风险**：体验困惑，但多用几次就能习惯。
**建议方向**：条件显示段落前加分段标题（如"── AI 组件配置 ──"）。

---

### Round 1 攻方汇总

| ID | 严重度 | 核心问题 |
|----|--------|----------|
| WF-001 | 🔴高 | 缺少"胶水层"Bootstrap，策划配完点 Play 什么都不会发生 |
| WF-002 | 🔴高 | Play Mode 改 SO 不生效无提示，策划迭代效率受阻 |
| WF-003 | 🔴高 | AttackBulletType 类型写错（VFXTypeSO vs BulletTypeSO） |
| WF-004 | 🟡中 | EntityManagerAccessor 未定义，Gizmo/DebugWindow 可能静默失效 |
| WF-005 | 🟡中 | AI 缺少 Always 兜底的校验级别不够 |
| WF-006 | 🟡中 | 新建 SO 时 Components 为空，无必填项指引 |
| WF-007 | 🟡中 | PoolDefinition 间接引用对策划不友好 |
| WF-008 | 🟡中 | 跨 SO 引用链无反向查询 |
| WF-009 | 🟡中 | 缺少新手快速上手路径和模板资产 |
| WF-010 | 🟢低 | SOCreationWizard 路径手输不够友好 |
| WF-011 | 🟢低 | CheckboxGrid 联动字段缺少分段标题 |

**🔴 高严重度：3 个** — 均为阻塞策划独立工作的闭环断裂点
**🟡 中严重度：6 个** — 体验痛点
**🟢 低严重度：2 个** — 改善建议

---

## PK Round 1 — 守方回应

### WF-001 回应 | ✅ 接受 — 新增 EntitySystemBootstrap MonoBehaviour

**判定**：一针见血。这是策划工作流的致命断点——做了所有配置但没有"引擎启动钥匙"。

**定案行动**：
1. §3.1 目录结构新增 `EntitySystemBootstrap.cs`（放在 `_Framework/EntitySystem/Scripts/Core/`）
2. EntitySystemBootstrap 是 **MonoBehaviour**，策划拖到场景根 GO 即可：
   - `Awake()`：创建 EntityManager + EntityViewBridge + EntitySpawner 实例
   - `Update()`：驱动 EntityManager.Tick() → EntitySpawner.Tick() → EntityViewBridge.SyncAll()
   - 同时将 EntityManager 实例注册到 `EntityManagerAccessor.Instance`（解决 WF-004）
   - Inspector 暴露 `PoolDefinition debugViewPool` 字段（策划拖入 Debug View 的 PoolDefinition）
3. §十 策划工作流 10.2 步骤 3 修正为：「在场景中创建空 GO → 挂 `EntitySystemBootstrap` + `EntitySpawnPoint`」
4. §十 新增 **10.0 前置条件**段落：「场景中必须有一个 EntitySystemBootstrap 组件。如缺失，Play Mode 不会有任何 Entity 生成。」
5. P1.10 AC 扩充：「场景中放 EntitySystemBootstrap → 自动驱动刷怪系统」
6. **不引入 Singleton 模式**——EntitySystemBootstrap 持有实例引用并通过 EntityManagerAccessor 暴露，但不阻止多实例（测试/分屏场景预留）

**回写位置**：§3.1 + §3.7（Bootstrap 说明）+ §3.15 + §十 10.0/10.2 + §六 P1.10 AC

---

### WF-002 回应 | ✅ 接受 — Play Mode HelpBox + Restart Waves 按钮

**判定**：正确。SO 快照模式违反 Unity 直觉，不提示就是坑。

**定案行动**：
1. **EntityConfigSOEditor Play Mode 黄色 HelpBox**（Phase 1 必做）：
   ```
   ⚠️ Play Mode：修改此配置仅对新生成的 Entity 生效，已存在的 Entity 不受影响。
   如需验证所有 Entity，请退出并重新进入 Play Mode。
   ```
2. **EntityDebugWindow 增加"Restart All Waves"按钮**（Phase 1 必做）：
   - 功能：清除所有活跃 Entity（EntityManager.DespawnAll()）+ 重置所有 Spawner 状态 + 重新启动波次
   - 策划点一下就能"从头来"验证新参数，无需退出 Play Mode
3. **HotReloadConfig 保持 Phase 2**——"Restart All Waves"已经解决了 80% 的调参场景（策划关心的是"整体效果"而非"单个 Entity 热刷新"）
4. §十 10.3 措辞补充：增加"使用 Entity Debug Overview 窗口的 Restart All Waves 按钮可快速重新验证"

**回写位置**：§9.2（Play Mode HelpBox）+ §9.6（Restart 按钮）+ §十 10.3

---

### WF-003 回应 | ✅ 接受 — AttackBulletType 类型修正为 BulletTypeSO

**判定**：文档 bug，毫无疑问。这是 v2.4 新增字段时的笔误——AttackComponent 调用 DanmakuSystem.Fire()，参数类型显然应该是 BulletTypeSO。

**定案行动**：
1. §5.0 EntityConfigSO：`public VFXTypeSO AttackBulletType` → `public BulletTypeSO AttackBulletType`
2. §4.9 AttackComponent：`private VFXTypeSO _bulletType` → `private BulletTypeSO _bulletType`
3. §9.2 EntityConfigSOEditor 校验增加：AttackBulletType 非空时验证类型（虽然 Inspector 强制类型了，但双保险）
4. §十 10.1 步骤 3 修正：「AttackBulletType: (拖入弹幕 BulletTypeSO)」

**回写位置**：§5.0 + §4.9 + §十 10.1

---

### WF-004 回应 | ✅ 接受 — EntityManagerAccessor 由 Bootstrap 统一管理

**判定**：WF-001 的 Bootstrap 方案已经自然解决了这个问题。

**定案行动**：
1. §3.7 EntityManager 下方新增 **EntityManagerAccessor** 简要设计：
   ```csharp
   /// <summary>
   /// EntityManager 全局访问点（Editor 工具用）。
   /// 由 EntitySystemBootstrap.Awake() 注册，OnDestroy() 注销。
   /// </summary>
   public static class EntityManagerAccessor
   {
       public static EntityManager Instance { get; internal set; }
       public static EntityViewBridge ViewBridge { get; internal set; }
       public static EntitySpawner Spawner { get; internal set; }
   }
   ```
2. EntityGizmoDrawer（§9.1）在 `Instance == null` 时：在 Scene View 中央用 `Handles.Label` 显示提示「Entity System 未初始化 — 请在场景中添加 EntitySystemBootstrap」
3. EntityDebugWindow（§9.6）在 `Instance == null` 时：显示 HelpBox「Entity System 未初始化。请确认场景中有 EntitySystemBootstrap 组件。」（替代当前的"仅在 Play Mode 下可用"）

**回写位置**：§3.7（Accessor 定义）+ §9.1 + §9.6

---

### WF-005 回应 | ✅ 接受 — AI 代码 fallback + Validator 升级

**判定**：正确。"Warning"对缺少兜底来说太温和了，这在运行时一定会出问题。

**定案行动**：
1. **AIComponent 代码**（§4.7）：所有条件都不匹配时，**默认执行 IdleAction**（硬编码 fallback，不需要配置）
   - 代码注释：`// 安全网：所有条件均未匹配时默认 Idle。建议策划在行为表末尾配置 Always→Idle。`
2. **EntityConfigValidator**（§9.4）：「AIBehaviorSO 缺少 Always 兜底条件」从 **Warning 提升为 Error**
3. **AIBehaviorSOEditor**（§9.3）：当 Entries 最后一条不是 `AIConditionType.Always` 时，在 Inspector 底部显示**红色 HelpBox**：「警告：条件表缺少 Always 兜底条目。运行时将默认 Idle，建议显式配置。」

**回写位置**：§4.7 + §9.3 + §9.4

---

### WF-006 回应 | ✅ 接受 — 空 Components HelpBox + 预填默认值

**判定**：精准。空 Components 是"最大的隐性地雷"——Inspector 越干净策划越觉得没问题。

**定案行动**：
1. **EntityConfigSOEditor**（§9.2）：Components 为空时，Inspector 最顶部显示 **红色 HelpBox**：「⚠️ 组件列表为空！Entity 将没有任何能力。请至少勾选 State 组件。」
2. **SOCreationWizard**（§9.7）：创建 EntityConfigSO 时**预填默认 Components**：`[State, Health, Movement, Collision]`——策划"改默认"比"从零开始"更直观
3. **§十 10.1** 步骤 3 明确标注必填字段：「Components: **[必填]** 至少勾选 State」

**回写位置**：§9.2 + §9.7 + §十 10.1

---

### WF-007 回应 | 🔄 部分接受 — Phase 1 加 Tooltip + 预览行，深度方案 Phase 2

**判定**：问题真实，但 Phase 1 特效资产数量 < 5，Object Picker 中不至于混淆。Tooltip 和预览行足够了。

**定案行动**：
1. **Phase 1**：EntityConfigSOEditor 中，PoolDefinition 字段（SpawnEffect/HitEffect/DeathEffect）下方显示一行**只读灰色文字**：`→ Prefab: [PoolDefinition.Prefab.name]`（如果非 null）
2. **Phase 1**：每个 PoolDefinition 字段的 Tooltip 改为具体说明：如 `SpawnEffect` → `"生成时播放的特效——拖入特效类的 PoolDefinition 资产"`
3. **Phase 2**：引入 `EffectPoolDefinition` 子类或 Tag 标记 → 不在 Phase 1 范围内

**回写位置**：§9.2 补充（PoolDefinition 预览行）

---

### WF-008 回应 | 🔄 部分接受 — Phase 1 加依赖图 + Validator 输出引用链

**判定**：反向引用查询是好功能，但 Inspector 内嵌需要 `AssetDatabase.FindAssets` 扫全库——每次打开 SO Inspector 都扫一遍，性能不佳。Phase 1 放在 Validator 批量输出更合适。

**定案行动**：
1. **§十 10.1** 新增**依赖关系图**（只读示意，非代码）：
   ```
   创建顺序（从下到上）：
   ┌─────────────────────────────────────────────┐
   │ EntitySpawnWaveSO                            │ ← 步骤 4：编排关卡波次
   │   └→ SpawnGroup.EntityConfigSO              │
   ├─────────────────────────────────────────────┤
   │ EntityConfigSO                              │ ← 步骤 2-3：创建 Entity 配置
   │   ├→ AIBehaviorSO                           │
   │   ├→ BulletTypeSO (AttackBulletType)        │
   │   └→ PoolDefinition (SpawnEffect/HitEffect) │
   ├─────────────────────────────────────────────┤
   │ AIBehaviorSO / BulletTypeSO / PoolDefinition│ ← 步骤 1：底层资产（可复用已有）
   └─────────────────────────────────────────────┘
   ```
2. **EntityConfigValidator**（§9.4）输出增加**反向引用摘要**：每个 AIBehaviorSO 列出所有引用它的 EntityConfigSO 名称
3. Inspector 内嵌反向查询 → **Phase 2**（需要缓存机制避免每次 OnInspectorGUI 全扫库）

**回写位置**：§十 10.1 + §9.4

---

### WF-009 回应 | ✅ 接受 — Demo 模板 + Quick Start

**判定**：完全正确。策划上手最快的方式是"复制一个已有的配置改改"，不是看文档。

**定案行动**：
1. **P1.11 AC 扩充**：「Demo 场景中的 SO 资产（EntityConfigSO + AIBehaviorSO + EntitySpawnWaveSO）保留为策划参考模板，存放在 `Assets/_Game/Configs/_Template/` 目录下，文件命名以 `Template_` 前缀」
2. **MODULE_README.md 内容定义**（§3.1 补充）：
   - 系统概述（1 段）
   - Quick Start（5 步）：① 场景加 EntitySystemBootstrap → ② 复制 Template_Slime SO → 改名 → ③ 修改参数 → ④ 创建/引用 SpawnWaveSO → ⑤ Play 看效果
   - 文件清单（目录下各文件职责一句话说明）
3. SOCreationWizard "从模板创建" → **Phase 2**（当前右键 Create + 复制模板 SO 已足够）

**回写位置**：§六 P1.11 AC + §3.1（MODULE_README 内容定义）

---

### WF-010 回应 | ✅ 接受 — 统一推荐右键菜单

**判定**：正确。右键菜单更直观，Wizard 是历史遗留的统一入口。

**定案行动**：
1. §十 10.1 统一推荐右键菜单方式（「右键 → Create → Entity/EntityConfig」），不再引导策划用 Wizard
2. SOCreationWizard 仍然加 Entity SO 类型（ET-010 已定案），但作为**备选入口**，不是首选
3. Wizard 的 savePath 不做改动（Phase 1 策划用右键菜单就行）

**回写位置**：§十 10.1（措辞微调）

---

### WF-011 回应 | ✅ 接受 — 分段标题

**判定**：成本极低，20+ 字段的 SO 确实需要分段。

**定案行动**：
EntityConfigSOEditor（§9.2）条件显示的字段段落前增加**分段标题 Label**：
```
─── AI 组件配置（因勾选了 AI 而显示）───
[AIBehavior 字段]
─── 攻击组件配置（因勾选了 Skill 而显示）───
[AttackInterval / BulletType / FireOffset 字段]
─── 碰撞组件配置（因勾选了 Collision 而显示）───
[CollisionRadius 字段]
```

**回写位置**：§9.2

---

## PK Round 1 收敛统计

| ID | 严重度 | 判定 | 状态 |
|----|--------|------|------|
| WF-001 | 🔴高 | ✅ 接受 | 定案——新增 EntitySystemBootstrap |
| WF-002 | 🔴高 | ✅ 接受 | 定案——Play Mode HelpBox + Restart Waves 按钮 |
| WF-003 | 🔴高 | ✅ 接受 | 定案——AttackBulletType 类型修正为 BulletTypeSO |
| WF-004 | 🟡中 | ✅ 接受 | 定案——EntityManagerAccessor 由 Bootstrap 管理 + null 提示 |
| WF-005 | 🟡中 | ✅ 接受 | 定案——AI fallback Idle + Validator 升级 Error + Editor 红色 HelpBox |
| WF-006 | 🟡中 | ✅ 接受 | 定案——空 Components 红色 HelpBox + 预填默认组件 |
| WF-007 | 🟡中 | 🔄 部分接受 | 定案——Phase 1 Tooltip + 预览行；深度方案 Phase 2 |
| WF-008 | 🟡中 | 🔄 部分接受 | 定案——Phase 1 依赖图 + Validator 反向引用；Inspector 内嵌 Phase 2 |
| WF-009 | 🟡中 | ✅ 接受 | 定案——Demo 模板 + Quick Start |
| WF-010 | 🟢低 | ✅ 接受 | 定案——统一推荐右键菜单 |
| WF-011 | 🟢低 | ✅ 接受 | 定案——分段标题 |

**🔴 高严重度：3 / 3 已解决**
**🟡 中严重度：6 / 6 已解决（4 全接受 + 2 部分接受但有定案）**
**🟢 低严重度：2 / 2 已解决**

**残余 🔴 = 0 | 所有 🔴🟡 有定案行动 | 严重度呈下降趋势 → 达到收敛标准**

---

## PK R6 结论

**Round 数**：1 轮收敛（无需 Round 2/3）
**问题总数**：11 个（WF-001~011）
**定案变更**：11 项回写 TDD（9 全接受 + 2 部分接受）

### 需回写到 TDD v2.6 的变更清单

| # | 变更内容 | 涉及章节 |
|---|----------|----------|
| 1 | 新增 EntitySystemBootstrap MonoBehaviour（胶水层） | §3.1 + §3.7 + §十 10.0/10.2 + §六 P1.10 AC |
| 2 | 新增 EntityManagerAccessor 静态访问点定义 | §3.7 |
| 3 | EntityConfigSOEditor Play Mode 黄色 HelpBox 提示 | §9.2 |
| 4 | EntityDebugWindow 新增 Restart All Waves 按钮 | §9.6 |
| 5 | AttackBulletType 类型修正：VFXTypeSO → BulletTypeSO | §5.0 + §4.9 + §十 10.1 |
| 6 | EntityGizmoDrawer / DebugWindow null 时显示提示 | §9.1 + §9.6 |
| 7 | AIComponent 默认 fallback Idle + Validator Always 校验升 Error + Editor HelpBox | §4.7 + §9.3 + §9.4 |
| 8 | EntityConfigSOEditor 空 Components 红色 HelpBox + SOCreationWizard 预填默认组件 | §9.2 + §9.7 |
| 9 | EntityConfigSOEditor PoolDefinition 预览行 + 分段标题 | §9.2 |
| 10 | §十 10.1 新增依赖关系图 + Validator 反向引用输出 + 推荐右键菜单 + 必填标注 | §十 + §9.4 |
| 11 | P1.11 AC 扩充 Demo 模板 + MODULE_README Quick Start 定义 | §六 P1.11 + §3.1 |

### 最有价值的 Top 3 变更

1. **WF-001 EntitySystemBootstrap**——策划工作流闭环的"最后一公里"，没有它整个配置流程是断的
2. **WF-003 BulletTypeSO 类型修正**——文档 bug，如不修正编码阶段一定踩坑
3. **WF-002 Play Mode HelpBox + Restart Waves**——直接解决策划调参效率的最大痛点

### 六轮 PK 总量

| 轮次 | 视角 | 问题数 | 回合 |
|------|------|--------|------|
| R1 | 技术 | 17 | 2 |
| R2 | 策划工作流 | 12 | 2 |
| R3 | 软件架构 | 7 | 1 |
| R4 | 游戏设计 | 11 | 2 |
| R5 | 编辑器工具 | 11 | 1 |
| R6 | 策划落地性 | 11 | 1 |
| **总计** | | **69** | **9** |

> **PK 状态**：✅ 已收敛
> **完成时间**：2026-04-27 07:10
> **下一步**：✅ 已回写到 TDD v2.6，待天命人审批后启动 Phase 1 编码
