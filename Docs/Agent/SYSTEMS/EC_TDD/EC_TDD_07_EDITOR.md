---
system: entity-component
scope: editor-tools
last_verified: 2026-05-02
depends_on: [EC_TDD_05_COMPONENTS, EC_TDD_06_CONFIG]
related_code: Assets/_Framework/Editor/Entity/*.cs
---

## 九、编辑器工具

> **v2.2 变更（GD-004）**：从"待后续细化"提升，Phase 1 必做 EntityGizmoDrawer。

### 9.1 EntityGizmoDrawer（Phase 1 必做）

> **v2.5 重写（ET-003）**：从 `[ExecuteAlways] MonoBehaviour` 改为**静态类 + `[DrawGizmo]` + `#if UNITY_EDITOR`**，与项目已有 DanmakuCollisionGizmosDrawer 模式一致。

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Entity 碰撞圈 + HP 标签 Gizmo 绘制器。
/// 
/// Edit Mode：以 EntitySpawnPoint 为 DrawGizmo target，绘制生成区域
///   （已内置在 EntitySpawnPoint.OnDrawGizmos 中）。
/// Play Mode：通过 [InitializeOnLoad] + SceneView.duringSceneGui 注册回调，
///   遍历 EntityManager 活跃 Entity 绘制碰撞圈和 HP。
/// 
/// 零运行时开销——全部代码在 Editor asmdef 中，不打包。
/// </summary>
[InitializeOnLoad]
public static class EntityGizmoDrawer
{
    static EntityGizmoDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!Application.isPlaying) return;

        // 获取 EntityManager 实例（由 EntitySystemBootstrap 注册到静态访问点）
        var mgr = EntityManagerAccessor.Instance;
        if (mgr == null)
        {
            // v2.6（WF-004）：null 时在 Scene View 中央显示提示
            Handles.BeginGUI();
            GUILayout.Label("Entity System 未初始化 — 请在场景中添加 EntitySystemBootstrap",
                EditorStyles.helpBox);
            Handles.EndGUI();
            return;
        }

        // 遍历所有活跃 Entity
        foreach (var entity in mgr.ActiveEntities)
        {
            if (entity.IsPendingDespawn) continue;

            // 阵营颜色：Enemy=红, Player=绿, Neutral=灰
            Color color = entity.Camp switch
            {
                EnumCamp.Enemy => Color.red,
                EnumCamp.Player => Color.green,
                _ => Color.gray
            };

            // 绘制碰撞圈
            Handles.color = color;
            Handles.DrawWireDisc(
                (Vector3)entity.Position,
                Vector3.forward,
                entity.ConfigSO.CollisionRadius);

            // HP 标签
            Handles.Label(
                (Vector3)entity.Position + Vector3.up * (entity.ConfigSO.CollisionRadius + 0.2f),
                $"HP: {entity.CurrentHp}/{entity.ConfigSO.MaxHp}",
                EditorStyles.boldLabel);
        }
    }
}
#endif
```

**关键决策**：
1. **不是 MonoBehaviour**——纯 Editor 静态类，不需要场景中挂任何 GO
2. **Play Mode 才绘制 Entity**——Edit Mode 只有 EntitySpawnPoint 的生成区域 Gizmo（ET-009）
3. **EntityManagerAccessor**：由 EntitySystemBootstrap.Awake() 注册，Editor 工具（Gizmo/DebugWindow）通过它获取 EntityManager 实例（v2.6 WF-001/WF-004）
4. **文件位置**：`_Framework/Editor/Entity/EntityGizmoDrawer.cs`（归入 MiniGameFramework.Editor.asmdef）

### 9.2 EntityConfigSOEditor（Phase 1 必做）

> **v2.5 新增（ET-001/ET-002）**：EntityConfigSO 的 20+ 字段需要 CustomEditor 实现条件显示和校验，参考 BulletTypeSOEditor 先例。

```csharp
#if UNITY_EDITOR
/// <summary>
/// EntityConfigSO 自定义 Inspector。
/// 核心功能：
/// 1. Components[] 渲染为 Checkbox Grid（替代裸枚举数组）
///    - 去重自动保证（CheckboxGroup 不可能选两次）
///    - Control / AI 互斥：选一个自动灰化另一个 + HelpBox 说明
///    - Skill 标签显示为 "☑ Skill (Attack)"（Phase 1 AttackComponent 复用 Skill 槽位）
/// 2. 根据 Components[] 内容动态显示/隐藏字段段落
///    - 无 AI 组件 → 隐藏 AIBehavior 区
///    - 无 Skill 组件 → 隐藏攻击参数区（AttackInterval/AttackBulletType/AttackFireOffset）
///    - 无 Collision 组件 → CollisionRadius 灰化
///    v2.6（WF-011）：条件显示段落前加分段标题
///    ─── AI 组件配置（因勾选了 AI 而显示）───
///    ─── 攻击组件配置（因勾选了 Skill 而显示）───
///    ─── 碰撞组件配置（因勾选了 Collision 而显示）───
/// 3. Inspector 顶部 HelpBox 警告层
///    - v2.6（WF-006）：Components 为空时红色 HelpBox「⚠️ 组件列表为空！Entity 将没有任何能力。请至少勾选 State 组件。」
///    - "Components 含 AI 但 AIBehavior 未填"
///    - "Components 含 Skill 但 AttackBulletType 未填且 AttackInterval > 0"
///    - Control / AI 同时存在的互斥警告
///    - v2.6（WF-002）：Play Mode 下黄色 HelpBox「⚠️ Play Mode：修改此配置仅对新生成的 Entity 生效，已存在的 Entity 不受影响。如需验证所有 Entity，请使用 Entity Debug Overview 窗口的 Restart All Waves 按钮，或退出并重新进入 Play Mode。」
/// 4. 依赖建议（Warning，非硬阻塞）
///    - AI → 建议搭配 Movement
///    - Collision → 建议搭配 Health
/// 5. v2.6（WF-007）：PoolDefinition 字段（SpawnEffect/HitEffect/DeathEffect）预览行
///    - 每个 PoolDefinition 字段下方显示只读灰色文字：→ Prefab: [PoolDefinition.Prefab.name]
///    - Tooltip 改为具体说明：SpawnEffect → "生成时播放的特效——拖入特效类的 PoolDefinition 资产"
/// </summary>
[CustomEditor(typeof(EntityConfigSO))]
public class EntityConfigSOEditor : Editor
{
    // SerializedProperty 缓存 + CheckboxGrid 绘制逻辑
    // 参考 BulletTypeSOEditor 的 SerializedProperty 遍历模式
}
#endif
```

**策划视角**：打开 EntityConfigSO Inspector → 顶部显示健康状态（绿/黄/红）→ 中部 Checkbox Grid 勾选组件 → 下方只显示已勾选组件相关的字段 → 配置不一致时 HelpBox 即时提醒。

### 9.3 AIBehaviorSOEditor（Phase 1 最小版）

> **v2.5 新增（ET-005）**：Phase 1 只做可读摘要标题，ConditionParam 上下文提示和模拟测试按钮 Phase 2。

```csharp
#if UNITY_EDITOR
/// <summary>
/// AIBehaviorSO 自定义 Inspector——Phase 1 最小版。
/// 每个 AIBehaviorEntry 列表元素标题显示可读摘要，替代默认的 "Element 0"。
/// 示例：[0] HP < 30% → Flee (5.0)
///        [1] TargetInRange (8.0) → MoveToTarget
///        [2] Always → Idle
///
/// v2.6（WF-005）：当 Entries 最后一条不是 AIConditionType.Always 时，
/// Inspector 底部显示红色 HelpBox：
/// 「警告：条件表缺少 Always 兜底条目。运行时将默认 Idle，建议显式配置。」
/// </summary>
[CustomEditor(typeof(AIBehaviorSO))]
public class AIBehaviorSOEditor : Editor
{
    // ReorderableList + 自定义 elementHeightCallback/drawElementCallback
    // 生成可读摘要：$"[{i}] {FormatCondition(entry)} → {entry.Action} ({entry.ActionParam})"
    // v2.6: 底部增加 Always 兜底检查 HelpBox
}
#endif
```

**Phase 2 扩展方向**：ConditionParam 根据 ConditionType 显示不同 label + Range（HpBelow→[0,1] Slider；TargetInRange→float+"米"）；模拟测试按钮（输入 HP%+距离→显示匹配结果）。

### 9.4 EntityConfigValidator（Phase 1 必做）

> **v2.5 新增（ET-006）**：高性价比 MenuItem 批量校验工具，< 1 小时实现。

```csharp
#if UNITY_EDITOR
/// <summary>
/// Entity 配置资产批量校验工具。
/// MenuItem: Tools/Entity/Validate All Configs
/// 
/// 校验项：
/// 1. EntityConfigSO:
///    - ComponentType[] 去重 + Control/AI 互斥
///    - PoolMax > 0 且 PoolMax >= PoolInitial
///    - 有 AI 组件时 AIBehavior ≠ null
///    - 有 Skill 组件时 AttackBulletType ≠ null（或 AttackInterval ≤ 0）
///    - 有 Collision 组件时 CollisionRadius > 0
///    - v2.6（WF-006）：Components[] 为空时 Error
/// 2. AIBehaviorSO:
///    - Entries 非空
///    - v2.6（WF-005）：至少有一个 Always 兜底条件——从 Warning 提升为 **Error**
/// 3. EntitySpawnWaveSO:
///    - Waves 非空
///    - 每个 Group.EntityConfig ≠ null
///    - LoopStartWave < Waves.Length（Loop=true 时）
///    - SpawnGroup.Count > 0
/// 
/// 输出：Console 中按 SO 资产分组输出 Error/Warning，点击可 Ping 定位到资产。
/// v2.6（WF-008）：输出末尾新增**反向引用摘要**——每个 AIBehaviorSO 列出所有引用它的 EntityConfigSO 名称。
/// </summary>
public static class EntityConfigValidator
{
    [MenuItem("Tools/Entity/Validate All Configs")]
    public static void ValidateAll()
    {
        // AssetDatabase.FindAssets("t:EntityConfigSO") + "t:AIBehaviorSO" + "t:EntitySpawnWaveSO"
        // 逐个校验，结果输出到 Console
    }
}
#endif
```

### 9.5 EntitySpawnWaveSOEditor（Phase 1 最小版）

> **v2.5 新增（ET-007）**：在嵌套数组上方显示只读摘要面板，不替换默认数组编辑器。

```csharp
#if UNITY_EDITOR
/// <summary>
/// EntitySpawnWaveSO 自定义 Inspector——Phase 1 最小版。
/// 在 Waves[] 数组上方显示只读摘要面板（每波一行）。
/// 
/// 示例摘要：
///   Wave 0 [Timer 2.0s]: 史莱姆×3, 哥布林×1
///   Wave 1 [AllCleared]: 精英哥布林×2
///   Wave 2 [OnCallback]: Boss×1
///   ──── Loop → Wave 0 ────
/// 
/// 下方保留默认 Inspector 用于实际编辑。
/// </summary>
[CustomEditor(typeof(EntitySpawnWaveSO))]
public class EntitySpawnWaveSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制摘要面板（只读 HelpBox 风格）
        // DrawDefaultInspector() — 保留原始编辑器
    }
}
#endif
```

**Phase 2 扩展方向**：拖拽排序、时间线可视化、折叠详情面板。

### 9.6 EntityDebugWindow（Phase 1 最小版）

> **v2.5 新增（ET-008）**：Play Mode 下的 Entity 系统概览面板。

```csharp
#if UNITY_EDITOR
/// <summary>
/// Entity 系统 Play Mode 调试窗口。
/// MenuItem: Window/Entity/Debug Overview
/// 
/// Phase 1 功能（极简）：
/// 1. EntityManager 概览：活跃 Entity 总数 / 各 Pool 使用率 / PendingDespawn 队列长度
/// 2. Entity 列表表格：Id | ConfigName | HP | Position | AI 当前 Action
///    - 支持按 ConfigName 筛选
///    - 点击行可在 Scene View 中高亮对应 Entity（通过 EntityViewBridge 获取 GO）
/// 3. v2.6（WF-002）：**"Restart All Waves" 按钮**
///    - 功能：清除所有活跃 Entity（EntityManager.DespawnAll()）+ 重置所有 Spawner 状态 + 重新启动波次
///    - 策划修改 SO 参数后点一下即可"从头来"验证新配置，无需退出 Play Mode
/// 4. v2.6（WF-004）：EntityManagerAccessor.Instance == null 时显示 HelpBox
///    「Entity System 未初始化。请确认场景中有 EntitySystemBootstrap 组件。」
///    （替代当前的"仅在 Play Mode 下可用"——区分"未初始化"和"非 Play Mode"两种状态）
/// 
/// Phase 2 扩展方向：
/// - EventBus 事件追踪面板（记录最近 N 条事件 + 时间戳）
/// - AI 行为决策链可视化（当前匹配的 Entry 高亮）
/// - 单 Entity 详细 Inspector（StateMask 展开、组件激活状态）
/// </summary>
public class EntityDebugWindow : EditorWindow
{
    [MenuItem("Window/Entity/Debug Overview")]
    public static void ShowWindow() => GetWindow<EntityDebugWindow>("Entity Debug");

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("仅在 Play Mode 下可用", MessageType.Info);
            return;
        }

        // v2.6（WF-004）：区分"未初始化"和"Play Mode 正常"
        if (EntityManagerAccessor.Instance == null)
        {
            EditorGUILayout.HelpBox(
                "Entity System 未初始化。请确认场景中有 EntitySystemBootstrap 组件。",
                MessageType.Warning);
            return;
        }

        // EntityManager 概览 + Entity 列表

        // v2.6（WF-002）：Restart All Waves 按钮
        EditorGUILayout.Space();
        if (GUILayout.Button("🔄 Restart All Waves", GUILayout.Height(30)))
        {
            // EntityManager.DespawnAll() + Spawner.ResetAll() + Spawner.RestartAll()
        }
    }
}
#endif
```

### 9.7 SOCreationWizard 扩展（实施期间顺手做）

> **v2.5 补充（ET-010）**：SOCreationWizard 新增 Entity 系列 SO 类型。

实施 P1.8 阶段时，在已有的 SOCreationWizard 枚举中新增：
- `EntityConfig` → 默认 savePath: `Assets/_Game/Configs/Entity/`
  - **v2.6（WF-006）**：创建时**预填默认 Components**：`[State, Health, Movement, Collision]`——策划"改默认"比"从零开始"更直观
- `AIBehavior` → 默认 savePath: `Assets/_Game/Configs/AI/`
- `EntitySpawnWave` → 默认 savePath: `Assets/_Game/Configs/SpawnWave/`

**v2.6 备注**：推荐策划使用**右键菜单方式**创建 SO（WF-010），Wizard 作为备选统一入口。

### 9.8 待后续细化（Phase 2+）

- [ ] Entity vs Entity 碰撞的空间分区方案
- [ ] 技能效果管理器架构
- [ ] 网络同步预留（帧同步 / 状态同步接口）
- [ ] EntityDebugWindow 事件追踪 + AI 决策链可视化
- [ ] AIBehaviorSOEditor ConditionParam 上下文提示 + 模拟测试按钮
- [ ] EntitySpawnWaveSOEditor 拖拽排序 + 时间线可视化
- [ ] EntityConfigValidator 资产路径软警告（ET-011）

---

