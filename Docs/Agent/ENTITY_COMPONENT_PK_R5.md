# PK 评审记录 — Entity-Component TDD v2.4（R5：编辑器工具开发者 vs Unity 架构师）

> **目标文档**：`Docs/Agent/ENTITY_COMPONENT_TDD.md`（v2.4）
> **文档类型**：TDD
> **攻方角色**：Unity 编辑器工具开发者（10 年经验，专精 CustomEditor/CustomPropertyDrawer/EditorWindow/SceneView 工具链、Inspector UX 设计、SerializedObject/SerializedProperty 工作流、MenuItem/Validation/Gizmo 扩展）
> **守方角色**：Unity 架构师（专精于 Entity-Component 框架设计、零 GC 运行时、微信小游戏平台约束）
> **开始时间**：2026-04-26 21:00
> **PK 状态**：🔄 进行中
> **前置 PK**：R1（技术 17 题）、R2（策划 12 题）、R3（架构 7 题）、R4（设计 11 题）

---

## PK Round 1 — 攻方提问

### ET-001 | 严重度 🔴高 | EntityConfigSO 字段数量膨胀，缺少 CustomEditor 分组与条件显示
**涉及章节**：§5.0、§九
**质疑**：EntityConfigSO 目前有 20+ 个字段（基础信息、组件列表、属性、攻击、AI 行为、受击反馈、视觉特效、视觉表现、对象池），全部平铺在默认 Inspector 中。虽然用了 `[Header]` 分组，但存在以下问题：
1. **缺乏条件显示**：当 `Components[]` 数组中没有 AI 类型时，AIBehavior 字段仍然显示，策划可能误填；当没有 Collision 时 CollisionRadius 仍然可编辑；当没有 Skill/Attack 组件时攻击相关 3 个字段仍然暴露。
2. **缺乏即时反馈**：策划填了 `AttackBulletType` 但忘了在 Components 中加 Skill（AttackComponent 复用 Skill 槽位），运行时静默失败，Inspector 不会给出任何警告。
3. **Phase 2 注释字段干扰**：`HitStopFrames`、`IFrameCount`、`KnockbackCurve` 等被注释掉的字段出现在代码中，虽然 Inspector 不显示，但对后续维护者有干扰。

项目已有先例：BulletTypeSOEditor 用 CustomEditor 实现了条件显示（根据 `UseVisualAnimation` 控制动画字段可见性）。EntityConfigSO 的复杂度远超 BulletTypeSO，更需要 CustomEditor。

**潜在风险**：策划在 20+ 字段的 Inspector 中迷路，且组件列表与具体字段之间缺乏联动会导致大量"配了但没生效"的隐性 bug，排查代价极高。
**建议方向**：TDD §九 新增 EntityConfigSOEditor 为 Phase 1 必做项，核心功能：(1) 根据 `Components[]` 内容动态显示/隐藏关联字段段落；(2) 配置不一致时在 Inspector 顶部显示 HelpBox 警告（如"组件列表包含 AI 但未指定 AIBehaviorSO"）。

---

### ET-002 | 严重度 🔴高 | ComponentType[] 数组填写极易出错且无校验机制
**涉及章节**：§5.0、§4.9
**质疑**：`EntityConfigSO.Components` 是一个 `ComponentType[]` 枚举数组，策划需要手动选择要挂载的组件类型。存在以下严重问题：
1. **重复填写**：Unity 默认 Inspector 对枚举数组没有去重检查，策划可以填两个 `Health`，运行时第二个会覆盖第一个。
2. **互斥检查缺失**：BC-07.2 明确"Control 和 AI 互斥挂载"，但 Inspector 不阻止策划同时勾选这两个。
3. **隐式依赖不可见**：AttackComponent 的 `ComponentType Type => ComponentType.Skill`（复用 Skill 槽位），策划在 Components 中应该填 `Skill` 还是填一个不存在的 `Attack`？TDD 中说"Skill 槽位由 AttackComponent 使用"，但策划面对 Inspector 时完全不知道这个规则。
4. **遗漏关键组件**：如果策划填了 `Collision` 但忘了填 `Health`，Entity 注册到碰撞系统后被弹幕命中但无法扣血，静默失败。

**潜在风险**：ComponentType[] 是整个配置驱动系统的"骨架"，填错直接导致 Entity 行为异常，且排查需要理解框架内部实现——策划无法自行排查。
**建议方向**：(1) 用 CustomPropertyDrawer 或 CustomEditor 替代裸枚举数组，实现 CheckboxGroup 形式（复选框列表而非数组）+ 去重 + 互斥校验 + HelpBox 警告；(2) 在 EntityConfigSOEditor.OnInspectorGUI 中增加依赖检查（如 AI→需要 Movement，Collision→建议搭配 Health）；(3) 明确 AttackComponent 对应填哪个 ComponentType 枚举值。

---

### ET-003 | 严重度 🟡中 | EntityGizmoDrawer 使用 [ExecuteAlways] + MonoBehaviour 模式不当
**涉及章节**：§9.1
**质疑**：TDD 设计 `EntityGizmoDrawer` 为 `[ExecuteAlways] MonoBehaviour`，但存在多个问题：
1. **[ExecuteAlways] 运行时开销**：这个特性会让组件在 Play Mode 和 Edit Mode 都执行生命周期函数。Entity 逻辑全在纯 C# 层（非 MonoBehaviour），EntityManager 在 Edit Mode 不存在——此时 OnDrawGizmos 遍历活跃 Entity 列表会访问空引用。
2. **与参考实现不一致**：文档说"参考 DanmakuCollisionGizmosDrawer 模式"，但实际看代码，DanmakuCollisionGizmosDrawer 使用的是 **静态 `[DrawGizmo]` 特性 + `#if UNITY_EDITOR`** 模式——不需要 MonoBehaviour，不用 [ExecuteAlways]，完全是纯 Editor 代码。
3. **Editor 代码泄漏风险**：EntityGizmoDrawer.cs 放在 `Scripts/Editor/` 目录下但使用 MonoBehaviour + [ExecuteAlways]，如果 Editor 目录不在 Editor asmdef 中（目前 EntitySystem 还没有 asmdef），这个 MonoBehaviour 会被打包到 Runtime——在小游戏包里产生无用代码。

**潜在风险**：Play Mode 下可能触发空引用异常；打包后 MonoBehaviour 残留在 Runtime；与项目已有模式不一致增加维护成本。
**建议方向**：(1) 改用 DanmakuCollisionGizmosDrawer 的模式：静态类 + `[DrawGizmo]` 特性 + `#if UNITY_EDITOR`，以某个场景中的 MonoBehaviour（如 EntitySpawnPoint 或一个轻量 EntityDebugRoot）作为 DrawGizmo 的 target；(2) 或在 Play Mode 下用 `[InitializeOnLoad]` + SceneView.duringSceneGui 注册绘制回调，不需要场景中放任何 GO；(3) 明确 EntitySystem 的 asmdef 隔离策略。

---

### ET-004 | 严重度 🟡中 | EntitySystem 缺少 Editor asmdef 隔离规划
**涉及章节**：§3.1 目录结构
**质疑**：目录结构中 `EntitySystem/Scripts/Editor/EntityGizmoDrawer.cs` 放在 Scripts/Editor 子目录下。项目已有 `MiniGameFramework.Editor.asmdef`（includePlatforms: Editor），但 EntitySystem 目前没有独立的 asmdef。这意味着：
1. 如果 EntitySystem 代码放在 `_Framework/EntitySystem/` 下，它会被 `MiniGameFramework.Runtime.asmdef` 管辖——Editor 子目录虽然 Unity 约定不打包，但如果代码中有 `using UnityEditor` 没被 `#if UNITY_EDITOR` 包裹，CI 打包会报错。
2. 文档没有说明 EntitySystem 是否需要独立的 Runtime + Editor asmdef 对，还是复用框架级别的 asmdef。
3. 9.1 节的 EntityGizmoDrawer 如果是 MonoBehaviour（当前设计），放在特殊的 Editor 文件夹会导致它无法被场景引用（Editor-only 脚本不能作为组件挂到 GO 上）。

**潜在风险**：Editor/Runtime 代码边界不清导致打包失败或运行时包含不必要的 Editor 代码。
**建议方向**：TDD §3.1 补充 asmdef 隔离方案说明，至少明确：(1) EntitySystem Runtime 代码属于哪个 asmdef；(2) EntitySystem Editor 工具用独立的 `EntitySystem.Editor.asmdef` 还是复用 `MiniGameFramework.Editor.asmdef`；(3) EntityGizmoDrawer 不应是 MonoBehaviour（参见 ET-003）。

---

### ET-005 | 严重度 🟡中 | AIBehaviorSO Inspector 体验对策划不友好——缺少优先级可视化和条件预览
**涉及章节**：§4.7、§十 10.1
**质疑**：AIBehaviorSO.Entries 是一个 `AIBehaviorEntry[]` 数组，每个元素有 4 个字段（Condition/ConditionParam/Action/ActionParam）。默认 Inspector 中：
1. **优先级不直观**：数组索引 = 优先级（"索引越小优先级越高"），但 Unity 默认的 ReorderableList 只显示 `Element 0/1/2/...`，策划看不到语义化的优先级标签。
2. **缺少行内预览**：策划需要在脑中拼接"Condition=HpBelow, Param=0.3"→"HP 低于 30% 时"，没有即时可读的描述。
3. **ConditionParam/ActionParam 含义随枚举变化**：HpBelow 的 Param 是百分比（0~1），TargetInRange 的 Param 是距离（float），但 Inspector 中没有任何提示当前 Param 的含义和有效范围。
4. **缺少"测试/预览"入口**：策划配完后无法在 Inspector 中验证"如果 HP=30%、目标距离=5，AI 会做什么？"。

**潜在风险**：策划配错 ConditionParam 含义（把距离填成百分比）导致 AI 行为异常，且肉眼审查数组很难发现问题。
**建议方向**：Phase 1 至少实现一个 `AIBehaviorSOEditor`：(1) 每行显示可读描述，如"当 HP < 30% → 逃跑（距离 5.0）"；(2) ConditionParam 根据 ConditionType 显示不同的 label 和 Range（HpBelow → [0,1] Slider；TargetInRange → float+单位标签"米"）；(3) 数组元素支持拖拽排序（Unity 自带 ReorderableList）。可迭代：Phase 2 加"模拟测试"按钮。

---

### ET-006 | 严重度 🟡中 | SO 校验和引用完整性检查工具完全缺失
**涉及章节**：§九 9.2
**质疑**：§9.2"待后续细化"只列了 4 条，且全是 Phase 2+ 的功能性内容，**没有任何 SO 资产校验工具**。当前 3 种 SO 资产（EntityConfigSO、AIBehaviorSO、EntitySpawnWaveSO）形成引用链：SpawnWaveSO → EntityConfigSO → AIBehaviorSO / VFXTypeSO / PoolDefinition。风险点：
1. **断链检测**：策划删除或重命名一个 EntityConfigSO，所有引用它的 SpawnWaveSO 静默断链（null reference），运行时才崩。
2. **批量校验**：10+ 个 EntityConfigSO 资产，逐个打开 Inspector 检查太慢。没有"一键校验所有 Entity 配置"的 MenuItem 工具。
3. **PoolMax=0 等边界值**：EntityPool 构造时 `new Entity[config.PoolMax]`，PoolMax=0 会创建长度 0 的数组，Acquire 永远返回 null 且 LogWarning——但策划不知道为什么 Entity 没生成。
4. **SpawnWaveSO.LoopStartWave 越界**：如果 LoopStartWave >= Waves.Length，运行时数组越界。

项目已有 SOCreationWizard，说明项目对 Editor 工具有投入意愿，但缺少 Validation 类工具。

**潜在风险**：随着 SO 资产数量增长，人工检查不可持续，隐性配置错误会在运行时才暴露，浪费大量调试时间。
**建议方向**：TDD §九 新增 Phase 1 必做的 `EntityConfigValidator` MenuItem 工具（`Tools/Entity/Validate All Configs`），核心检查项：(1) Components 数组去重 + Control/AI 互斥；(2) PoolMax > 0 && PoolMax >= PoolInitial；(3) 引用完整性（AIBehavior/AttackBulletType/SpawnEffect 等非空引用检查）；(4) SpawnWaveSO 引用的 EntityConfigSO 非空 + LoopStartWave 范围检查。这个工具的实现量 < 1 小时，收益极高。

---

### ET-007 | 严重度 🟡中 | EntitySpawnWaveSO Inspector 嵌套数组体验差——Wave → Groups 是数组套数组
**涉及章节**：§3.14、§十 10.2
**质疑**：`EntitySpawnWaveSO` 结构为 `SpawnWaveEntry[] Waves`，每个 Wave 内含 `SpawnGroup[] Groups`。这是**数组嵌套数组**（Array of Array），Unity 默认 Inspector 渲染为：
```
Waves
  ├─ Element 0
  │   ├─ Groups
  │   │   ├─ Element 0 (EntityConfig, Camp, Count, SpawnInterval, Formation)
  │   │   └─ Element 1 (...)
  │   ├─ TriggerMode
  │   └─ TriggerDelay
  └─ Element 1 ...
```
策划需要展开 3 层才能编辑一个怪物组的数量，且 Wave 之间的关系（触发条件、前后依赖）完全没有可视化。10 波以上的关卡配置在默认 Inspector 中几乎不可用。

**潜在风险**：策划编排关卡波次效率极低，容易在嵌套折叠中迷路、选错层级编辑。
**建议方向**：Phase 1 实现 `EntitySpawnWaveSOEditor`，核心功能：(1) 每波显示单行摘要（如"Wave 0 [Timer 2s]: 史莱姆×3, 哥布林×1"）；(2) 展开后显示详情；(3) 支持拖拽排序。这是策划高频操作场景，值得投入。如果 Phase 1 时间紧张，至少在 TDD 中标注为 P1.8 的 AC 要求。

---

### ET-008 | 严重度 🟡中 | Play Mode 调试工具链严重不足——无 Entity 状态查看器、无事件追踪
**涉及章节**：§9.2、§十 10.3
**质疑**：§9.2 提到"Entity Inspector 自定义面板（运行时状态查看）"但标注为"待后续细化"。Play Mode 调试手段仅有：
1. EntityGizmoDrawer 的碰撞圈 + HP 标签（视觉层）
2. Debug View 的彩色圆 + HP 文本（视觉层）

缺少的关键调试能力：
1. **单个 Entity 运行时状态检查**：当前 StateMask 值、当前 AI Action、组件激活状态、EventBus 订阅数——Entity 是纯 C# 对象，没有 GO 可以在 Inspector 中查看。
2. **EntityEventBus 事件追踪**：OnDamaged/OnDeath 事件是否正确发布？Subscribe 是否遗漏？目前完全是黑盒。
3. **EntityManager 全局概览**：当前活跃 Entity 数、各池使用率、待销毁队列长度。
4. **AI 行为调试**：当前匹配了哪条 AIBehaviorEntry？为什么没匹配预期的那条？

Entity 是纯逻辑层不挂 GO，意味着 Unity 默认的 Inspector 窗口完全无法查看 Entity 状态，必须有自定义工具。

**潜在风险**：P1.11 集成验收场景中出现 bug 时，开发者缺少定位问题的手段，只能靠 Debug.Log 满天飞，效率极低。
**建议方向**：TDD §九 新增 Phase 1 最小调试工具（EditorWindow）：(1) EntityManager 概览面板（活跃数/池使用率/待销毁数）；(2) 点击某个 Entity 可展开查看其组件列表 + 关键运行时数据（HP、位置、AI 当前 Action）。事件追踪和 AI 行为可视化可以 Phase 2 做，但至少概览面板在 Phase 1 是调试的刚需。

---

### ET-009 | 严重度 🟢低 | EntitySpawnPoint Gizmo 仅在 Selected 时绘制，多刷怪点场景不便
**涉及章节**：§3.14
**质疑**：`EntitySpawnPoint.OnDrawGizmosSelected()` 只在选中时绘制黄色圆圈。当场景中有多个刷怪点时，策划无法一次性看到所有生成区域的分布——需要逐个选中才能看到。

此外，Gizmo 只画了 AreaRadius 圆圈，没有显示引用的 WaveConfig 信息（如波次数、总怪物数），策划无法在 Scene View 中快速判断"这个刷怪点会生什么怪、多少只"。

**潜在风险**：多刷怪点的关卡布局设计体验不佳，但不阻塞实施。
**建议方向**：(1) 增加 `OnDrawGizmos()`（非 Selected）绘制半透明圆圈，选中时高亮显示详细信息；(2) 在圆心位置用 `Handles.Label` 显示刷怪点名称 + 波次数 + 首波怪种。

---

### ET-010 | 严重度 🟢低 | SOCreationWizard 未包含 Entity 系统的 3 种 SO 类型
**涉及章节**：§九
**质疑**：项目已有 `SOCreationWizard`（Tools → MiniGame Template → SO Creation Wizard），支持创建 Danmaku 系列 SO。但 TDD 没有提及将 EntityConfigSO / AIBehaviorSO / EntitySpawnWaveSO 加入此向导。虽然这 3 种 SO 都有 `[CreateAssetMenu]`（可以通过右键菜单创建），但不更新向导会导致工具入口不一致——策划习惯了用 Wizard 的，找不到 Entity 相关类型。

**潜在风险**：工具入口不一致，影响策划体验一致性，但不阻塞功能。
**建议方向**：实施期间顺手将 3 种 SO 加入 SOCreationWizard 的枚举列表，并设定默认 savePath 为 `Assets/_Game/Configs/Entity/` 等。

---

### ET-011 | 严重度 🟢低 | SO 资产命名与目录组织规范仅在工作流文本中提及，缺少强制约束
**涉及章节**：§十 10.1、10.2
**质疑**：策划工作流中提到 SO 存放路径（`Assets/_Game/Configs/Entity/`、`Assets/_Game/Configs/AI/`、`Assets/_Game/Configs/SpawnWave/`），但这只是文字说明，没有任何强制约束。策划可以在任意目录创建 SO 资产，导致：
1. 后续批量校验工具的搜索范围不明确（搜全项目？还是只搜约定目录？）
2. ConfigId 唯一性无法跨目录校验（Phase 2 迁移 Luban 时才暴露）

**潜在风险**：资产散落各处，长期维护成本上升，但 Phase 1 资产数量少时影响不大。
**建议方向**：在 Validation 工具中加一条软警告"EntityConfigSO 资产不在推荐目录下"；或在 CreateAssetMenu 的 fileName 中加路径前缀提示（效果有限，仅作为辅助）。

---

**攻方整体评价**：TDD 的编辑器工具章节（§九）是整份文档中**最薄弱的环节**。Phase 1 只规划了一个 EntityGizmoDrawer，且实现方案与项目已有模式（DanmakuCollisionGizmosDrawer 的静态 [DrawGizmo] 模式）不一致。更关键的问题是：EntityConfigSO 的 20+ 字段 Inspector 缺少 CustomEditor 条件显示和校验，ComponentType[] 数组是策划高频踩坑点但无防错机制，AIBehaviorSO 和 EntitySpawnWaveSO 的嵌套数组结构在默认 Inspector 中体验很差——这些直接影响策划的日常工作效率和配置正确性。建议将 EntityConfigSOEditor（含组件联动校验）和 EntityConfigValidator（MenuItem 批量校验）提升为 Phase 1 必做项，其余编辑器体验优化可在实施中迭代。

---

## PK Round 1 — 守方回应

### ET-001 回应 | ✅ 接受 — EntityConfigSOEditor 提升为 Phase 1 必做

**判定**：完全认同。BulletTypeSOEditor 就是先例——EntityConfigSO 的复杂度远超 BulletTypeSO，不做 CustomEditor 是失策。

**定案行动**：
1. §九 新增 **§9.2 EntityConfigSOEditor（Phase 1 必做）**
2. 核心功能：
   - 根据 `Components[]` 内容动态显示/隐藏字段段落（无 AI → 隐藏 AIBehavior 区；无 Skill → 隐藏攻击区；无 Collision → CollisionRadius 灰化）
   - Inspector 顶部 HelpBox 警告层：组件-字段不一致时即时提示（如"Components 含 AI 但 AIBehavior 未填"）
   - 参考 BulletTypeSOEditor 的 SerializedProperty 遍历模式
3. Phase 2 注释字段（HitStopFrames 等）用 `#if false` 替代 `//` 注释，避免代码层面的视觉噪音
4. Phase 1 步骤表 P1.8 AC 更新：增加"EntityConfigSOEditor 条件显示 + HelpBox 警告 正常工作"

**回写位置**：§九 + §六 P1.8 AC

---

### ET-002 回应 | ✅ 接受 — ComponentType[] 改为 CheckboxGroup + 校验

**判定**：精准打击。裸枚举数组对策划来说就是地雷阵。

**定案行动**：
1. EntityConfigSOEditor 中将 `Components[]` 渲染为 **Checkbox Grid**（每个 ComponentType 一个复选框），替代默认数组 Inspector
   - 去重自动保证（CheckboxGroup 不可能选两次）
   - Control / AI 互斥：选一个自动灰化另一个 + HelpBox 说明
2. 新增**依赖建议**（非硬阻塞，用 Warning 提示）：
   - AI → 建议搭配 Movement
   - Collision → 建议搭配 Health
   - Skill（AttackComponent）→ 明确标注为"攻击（Skill 槽位）"，Inspector 中 label 显示为 `☑ Skill (Attack)`
3. 明确 TDD 说明：**策划在 Components 中勾选 `Skill`** 来启用 AttackComponent（Phase 1）。Phase 3 SkillComponent 上线后此标签改为 `Skill (Attack | Skill)`。

**回写位置**：§5.0 EntityConfigSO 代码注释 + §九 EntityConfigSOEditor + §六 P1.8 AC

---

### ET-003 回应 | ✅ 接受 — 改用静态 [DrawGizmo] 模式

**判定**：完全正确。TDD 说"参考 DanmakuCollisionGizmosDrawer 模式"但写出来的代码却不一致——这是文档 bug。

**定案行动**：
1. §9.1 **重写**：EntityGizmoDrawer 改为静态类 + `[DrawGizmo]` + `#if UNITY_EDITOR`
   - DrawGizmo target 选择方案：挂在 `EntitySpawnPoint` 上（场景中必有的 MonoBehaviour）
   - Play Mode 时额外通过 `[InitializeOnLoad]` + `SceneView.duringSceneGui` 绘制运行时 Entity 碰撞圈（EntitySpawnPoint 不在场景时的 fallback）
2. 删除 `[ExecuteAlways]` MonoBehaviour 方案
3. 代码放在 `_Framework/Editor/Entity/` 目录（复用 MiniGameFramework.Editor.asmdef），不放在 EntitySystem/Scripts/Editor/ 下

**回写位置**：§9.1 全部重写

---

### ET-004 回应 | ✅ 接受 — 补充 asmdef 隔离说明

**判定**：正确。TDD 遗漏了 asmdef 规划。

**定案行动**：
§3.1 目录结构补充 **asmdef 隔离方案**：
- EntitySystem Runtime 代码（`_Framework/EntitySystem/Scripts/`）归入 `MiniGameFramework.Runtime.asmdef`（已有，无需新建 asmdef）
- EntitySystem Editor 工具（CustomEditor/Gizmo/Validator）放在 `_Framework/Editor/Entity/` 目录，归入 `MiniGameFramework.Editor.asmdef`（已有，includePlatforms: Editor）
- **不新建独立 asmdef**——项目规模尚小，复用框架级 asmdef 即可。Phase 2+ 如需拆分模块再评估
- 所有 Editor 代码必须包裹 `#if UNITY_EDITOR` 或放在 Editor asmdef 管辖目录

**回写位置**：§3.1

---

### ET-005 回应 | 🔄 部分接受 — Phase 1 做最小 AIBehaviorSOEditor，深度功能 Phase 2

**判定**：问题真实存在，但 Phase 1 策划是天命人自己（程序出身），对 AI 配置的理解门槛较低。全功能 Editor 投入产出比不划算。

**定案行动**：
- **Phase 1 最小版**：AIBehaviorSOEditor 只做一件事——每个 Entry 的列表元素标题显示**可读摘要**（如 `[0] HP < 30% → Flee (5.0)`），替代默认的 `Element 0`
- ConditionParam 上下文提示（label 变化 + Range 属性）和"模拟测试"按钮 → **Phase 2**
- ReorderableList 拖拽排序 → Unity 2021+ 默认 Inspector 已支持数组元素拖拽，**无需额外代码**

**回写位置**：§九 新增 §9.3 AIBehaviorSOEditor（Phase 1 最小版）

---

### ET-006 回应 | ✅ 接受 — 新增 EntityConfigValidator MenuItem

**判定**：高性价比工具，< 1 小时实现，收益极高。完全接受。

**定案行动**：
§九 新增 **§9.4 EntityConfigValidator（Phase 1 必做）**：
- MenuItem 路径：`Tools/Entity/Validate All Configs`
- 校验项：
  1. ComponentType[] 去重 + Control/AI 互斥
  2. PoolMax > 0 且 PoolMax >= PoolInitial
  3. 引用完整性：有 AI 组件时 AIBehavior ≠ null；有 Skill 组件时 AttackBulletType ≠ null（或 AttackInterval = 0）
  4. SpawnWaveSO.Waves 非空 + 每个 Group.EntityConfig ≠ null
  5. SpawnWaveSO.LoopStartWave < Waves.Length
  6. EntityConfigSO.CollisionRadius > 0（有 Collision 组件时）
- 输出格式：Console 中按 SO 资产分组输出 Error/Warning，点击可定位到资产
- Phase 1 步骤表新增 **P1.8b**（Validator 作为 P1.8 的子步骤与 Editor 一起实现）

**回写位置**：§九 + §六 P1.8 AC

---

### ET-007 回应 | 🔄 部分接受 — Phase 1 最小摘要显示，深度 Editor Phase 2

**判定**：嵌套数组确实体验差，但 Phase 1 波次数量少（Demo 验收场景 3~5 波），全功能 Editor 过度投资。

**定案行动**：
- **Phase 1 最小版**：EntitySpawnWaveSOEditor 的 `OnInspectorGUI` 在 Waves 数组上方显示**只读摘要面板**（每波一行："Wave 0 [Timer 2.0s]: 史莱姆×3, 哥布林×1"），不替换默认数组编辑器
- 深度 Editor（拖拽排序、时间线可视化、折叠详情）→ **Phase 2**
- TDD §六 P1.10 AC 增加"EntitySpawnWaveSOEditor 摘要面板正常显示"

**回写位置**：§九 新增 §9.5 EntitySpawnWaveSOEditor（Phase 1 最小版）

---

### ET-008 回应 | 🔄 部分接受 — Phase 1 做 EntityDebugWindow 概览面板，深度调试 Phase 2

**判定**：Entity 是纯 C# 对象没有 GO，确实缺调试入口。但 P1.11 集成验收的规模（1 玩家 + 3 敌人）靠 Debug View + Gizmo 基本够用。全功能 EditorWindow 性价比不高。

**定案行动**：
- **Phase 1 最小版**：新增 `EntityDebugWindow`（EditorWindow），功能极简：
  1. EntityManager 概览：活跃 Entity 总数、各 Pool 使用率（ActiveCount / Capacity）、PendingDespawn 队列长度
  2. Entity 列表：只显示 Id / ConfigName / HP / Position / AI 当前 Action
  3. 不做详细展开、不做事件追踪
- **Phase 2 扩展**：EventBus 事件追踪、AI 行为决策链可视化、单 Entity 详细 Inspector
- 步骤表不新增独立步骤——并入 P1.8 Editor 阶段

**回写位置**：§九 新增 §9.6 EntityDebugWindow（Phase 1 最小版）

---

### ET-009 回应 | ✅ 接受 — 改为 Always 绘制 + 标签

**判定**：成本极低，体验提升显著。

**定案行动**：
- §3.14 EntitySpawnPoint 代码修改：
  - `OnDrawGizmos()`（always）：半透明黄色圆圈 + `Handles.Label` 显示名称
  - `OnDrawGizmosSelected()`：高亮实线圆圈 + 完整波次信息（波数 / 首波怪种 / 总怪物数）
- 实施期间直接做，不影响 Phase 1 时间线

**回写位置**：§3.14 EntitySpawnPoint 代码更新

---

### ET-010 回应 | ✅ 接受 — 实施期间顺手做

**判定**：10 分钟的事，不做反而奇怪。

**定案行动**：
- SOCreationWizard 枚举新增 `EntityConfig`、`AIBehavior`、`EntitySpawnWave` 三项
- 默认 savePath 分别为 `Assets/_Game/Configs/Entity/`、`Assets/_Game/Configs/AI/`、`Assets/_Game/Configs/SpawnWave/`
- 实施期间 P1.8 阶段顺手做

**回写位置**：§九 补充说明

---

### ET-011 回应 | 📋 记录 — Phase 2 Validator 中加软警告

**判定**：Phase 1 资产数量 < 10，散落风险极低。不值得现在投入约束机制。

**定案行动**：
- Phase 1 不做强制约束
- Phase 2 EntityConfigValidator 中增加一条**软警告**："EntityConfigSO 资产不在 `Assets/_Game/Configs/Entity/` 目录下"
- Phase 1 文档（§十 策划工作流）已有路径推荐，足够了

**回写位置**：无（Phase 2 待办记录）

---

## PK Round 1 收敛统计

| ID | 严重度 | 判定 | 状态 |
|----|--------|------|------|
| ET-001 | 🔴高 | ✅ 接受 | 定案——新增 EntityConfigSOEditor |
| ET-002 | 🔴高 | ✅ 接受 | 定案——ComponentType 改 CheckboxGroup |
| ET-003 | 🟡中 | ✅ 接受 | 定案——改用静态 [DrawGizmo] 模式 |
| ET-004 | 🟡中 | ✅ 接受 | 定案——补充 asmdef 隔离说明 |
| ET-005 | 🟡中 | 🔄 部分接受 | 定案——Phase 1 最小摘要 Editor |
| ET-006 | 🟡中 | ✅ 接受 | 定案——新增 EntityConfigValidator |
| ET-007 | 🟡中 | 🔄 部分接受 | 定案——Phase 1 最小摘要面板 |
| ET-008 | 🟡中 | 🔄 部分接受 | 定案——Phase 1 概览面板 |
| ET-009 | 🟢低 | ✅ 接受 | 定案——改为 Always 绘制 |
| ET-010 | 🟢低 | ✅ 接受 | 定案——顺手加入 SOWizard |
| ET-011 | 🟢低 | 📋 记录 | Phase 2 待办 |

**🔴 高严重度：2 / 2 已解决**
**🟡 中严重度：5 / 5 已解决（3 全接受 + 2 部分接受但有定案）**
**🟢 低严重度：3 / 2 已解决 + 1 记录**

**残余 🔴 = 0 | 所有 🔴🟡 有定案行动 → 达到收敛标准 ✅**

---

## PK R5 结论

**Round 数**：1 轮收敛（无需 Round 2/3）
**问题总数**：11 个（ET-001~011）
**定案变更**：10 项回写 TDD

### 需回写到 TDD v2.5 的变更清单

| # | 变更内容 | 涉及章节 |
|---|----------|----------|
| 1 | §9.1 重写——EntityGizmoDrawer 改为静态 [DrawGizmo] + `#if UNITY_EDITOR` | §9.1 |
| 2 | §9.2 新增 EntityConfigSOEditor（Components CheckboxGrid + 条件显示 + HelpBox 警告） | §九 新增 |
| 3 | §9.3 新增 AIBehaviorSOEditor（Phase 1 最小摘要标题） | §九 新增 |
| 4 | §9.4 新增 EntityConfigValidator（MenuItem 批量校验） | §九 新增 |
| 5 | §9.5 新增 EntitySpawnWaveSOEditor（Phase 1 最小摘要面板） | §九 新增 |
| 6 | §9.6 新增 EntityDebugWindow（Phase 1 概览面板） | §九 新增 |
| 7 | §3.1 补充 asmdef 隔离方案说明 | §3.1 |
| 8 | §3.14 EntitySpawnPoint Gizmo 改为 Always 绘制 + Label | §3.14 |
| 9 | §5.0 补充 Components 填写说明（Skill 标签 = AttackComponent） | §5.0 |
| 10 | §六 P1.8 AC 更新（Editor 工具验收标准扩充） | §六 |
| 11 | SOCreationWizard 新增 Entity 系列 SO 类型（实施期间做，TDD 记录） | §九 补充 |

> **PK 状态**：✅ 已收敛
> **完成时间**：2026-04-26 21:XX
> **下一步**：天命人审批后，将 10 项变更回写到 TDD v2.5

---

