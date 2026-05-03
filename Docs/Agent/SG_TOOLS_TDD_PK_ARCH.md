# PK 评审记录 — SG_TOOLS_TDD 编辑器工具 TDD（第二轮）

> **目标文档**：`SG_TOOLS_TDD_INDEX.md` + `SG_TOOLS_TDD_01~02`
> **文档类型**：TDD（编辑器工具）
> **攻方角色**：Unity 架构师（10 年 Unity 架构经验，专精依赖管理、SRP、可维护性、框架/Game 层边界）
> **守方角色**：Unity 编辑器工具开发者（10 年 Unity Editor 扩展经验，专精 CustomEditor/EditorWindow/Gizmo/MenuItem）
> **开始时间**：2026-05-03 18:30
> **PK 状态**：✅ 已收敛（2 轮 / 10 问题 / 100% 回应）
> **上下文**：基于 v1.1（已经过编辑器工具开发者 PK 修正），本轮从架构合理性视角进行补充审查

---

## PK Round 1 — 攻方提问（Unity 架构师）

### AT-001 | 严重度 🔴高 | SkipToNextWave 直接操作 HealthComponent.TakeDamage 耦合了伤害管线细节
**涉及章节**：TOOLS_TDD_02 §2.1 SkipToNextWave
**质疑**：调试命令中直接 `new DamageContext { BaseDamage = 99999, AttackerId = default }; health.TakeDamage(ref ctx);` — 这绕过了 `DamageDealer` 静态工具类。核心 TDD（EC_TDD_05）明确定义 DamageDealer 是伤害处理的唯一入口（重入保护 + PendingDespawn 安全检查）。调试代码绕过它，意味着：
1. 不触发 DamageDealer 的 PendingDespawn 安全检查
2. 如果后续 DamageDealer 新增全局钩子（如全局伤害统计），调试代码不走那条路
**潜在风险**：调试行为与正式行为不一致，可能引发难以复现的 bug
**建议方向**：改用 `DamageDealer.ApplyDamage(entity, 99999, default)` 走正式管线

### AT-002 | 严重度 🔴高 | SG_BattleStateWindow.OnGUI 每帧 Repaint() 在大场景下性能问题
**涉及章节**：TOOLS_TDD_02 §4.2 SG_BattleStateWindow
**质疑**：`OnGUI()` 末尾无条件调用 `Repaint()`，加上 OnGUI 内部每帧遍历 `ActiveEntities` 计数敌方/友方数量——这在 40 Entity + 编辑器 60fps 下每秒执行 60 次全量遍历。虽然 V1 场景小可以接受，但文档 §9 已知限制中自己也标注了"可能影响编辑器性能"。更重要的是：**EditorWindow.OnGUI 不该有 O(n) 的每帧逻辑，这违反了编辑器工具的性能约定。**
**潜在风险**：开发时编辑器卡顿，且新手可能不知道关这个窗口
**建议方向**：V1 直接改为定时刷新（EditorApplication.update + 0.1s 间隔 Repaint），O(n) 遍历只在 Repaint 帧执行

### AT-003 | 严重度 🟡中 | BaseLineY Gizmo 中的 _baseLineY 字段声明位置不明确
**涉及章节**：TOOLS_TDD_02 §3.1
**质疑**：Gizmo 代码段中声明了 `[Header("Gizmo 配置")] [SerializeField] private float _baseLineY = -7f;`，但核心 TDD_02 §1.2 BattleController 中已有 `[SerializeField] private float _baseLineY = -7f;`。如果两者都存在于 BattleController 中，就会有两个同名字段编译冲突。如果是同一个字段，那 Gizmo 代码段应该引用已有字段而非重新声明。
**潜在风险**：开发者可能重复声明 _baseLineY，或者不确定 Gizmo 代码应该放在哪
**建议方向**：明确标注"以下代码在 BattleController §1.2 已声明的 _baseLineY 字段基础上，仅新增 OnDrawGizmos 方法"

### AT-004 | 严重度 🟡中 | FindSOByName 和 FindSO 两个方法实现完全相同
**涉及章节**：TOOLS_TDD_02 §2.1 FindSOByName + §4.2 FindSO
**质疑**：`SG_DebugMenuItems.FindSOByName<T>` 和 `SG_BattleStateWindow.FindSO<T>` 代码完全一样（都是 `AssetDatabase.FindAssets` + `LoadAssetAtPath`）。但它们分别定义在两个不同的类中，是复制粘贴。当搜索逻辑需要修改时（如加缓存、改搜索路径），需要改两处。
**潜在风险**：维护成本翻倍，搜索逻辑不同步
**建议方向**：抽取公共工具方法到 `SG_EditorUtility` 类中，两处引用

### AT-005 | 严重度 🟡中 | SG_SpawnWaveSOEditor.OnEnable 未调用 base.OnEnable
**涉及章节**：TOOLS_TDD_01 §2.2 SG_SpawnWaveSOEditor
**质疑**：继承了 `EntitySpawnWaveSOEditor`，重写了 `OnEnable()` 但没有调用 `base.OnEnable()`。如果基类 `EntitySpawnWaveSOEditor` 在 OnEnable 中做了初始化（如缓存 SerializedProperty、设置默认值），子类不调 base 会导致基类状态不完整，`base.OnInspectorGUI()` 可能 NullReference。
**潜在风险**：框架 Editor 的 OnEnable 初始化被跳过，运行时报错
**建议方向**：在 OnEnable 中首先调用 `base.OnEnable()` 再做自己的初始化

### AT-006 | 严重度 🟡中 | CopyLastWave 的深拷贝遗漏 EntityConfigSO 引用赋值
**涉及章节**：TOOLS_TDD_01 §4.2 DeepCopyGroups
**质疑**：DeepCopyGroups 中 `copy[i] = new SpawnGroup { EntityConfig = source[i].EntityConfig, Camp = ..., Count = ..., SpawnInterval = ..., Formation = ... }`——这里 `EntityConfig` 是 SO 引用，浅拷贝是正确的（SO 是共享资产）。但如果 `SpawnGroup` 未来新增字段（如 `SpawnPosition`, `Rotation`, `Offset`），深拷贝方法不会自动包含新字段——忘记更新 DeepCopyGroups 就会丢数据。
**潜在风险**：框架 SpawnGroup 加字段后，复制功能无声丢失新字段
**建议方向**：1) 添加注释"当 SpawnGroup 字段变更时此处需同步更新"；2) 或用 `JsonUtility.ToJson/FromJson` 序列化拷贝（自动包含所有 Serializable 字段）

### AT-007 | 严重度 🟡中 | MenuItem Validate 方法使用了多个 MenuItem 属性但共享同一个 Validate
**涉及章节**：TOOLS_TDD_02 §2.1 ValidatePlayMode
**质疑**：
```csharp
[MenuItem(MENU_ROOT + "重试当前关卡 %&R", true)]
[MenuItem(MENU_ROOT + "直接胜利", true)]
...
private static bool ValidatePlayMode()
```
Unity 的 MenuItem Validate 规则是：**Validate 方法的 MenuItem path 必须与被校验的执行方法的 MenuItem path 完全相同**。多个 `[MenuItem(..., true)]` 叠加在同一个方法上是可以的，但只要其中任一 path 字符串有误（如多了/少了空格），就会静默失效——不报错但菜单不灰显。这种"一改全错"的模式脆弱。
**潜在风险**：路径字符串修改时漏改 Validate，菜单灰显失效
**建议方向**：将 MENU_ROOT + "xxx" 抽为 const 字段，执行方法和 Validate 使用同一 const，确保路径一致

### AT-008 | 严重度 🟡中 | OnDrawGizmos 中 FindObjectOfType 每帧调用
**涉及章节**：TOOLS_TDD_02 §3.1 BaseLineY Gizmo
**质疑**：`OnDrawGizmos()` 中每帧调用 `FindObjectOfType<EntitySystemBootstrap>()`。虽然 Scene View 不像 Game View 那样高频调用 Gizmo（只在 Scene View 刷新时），但在 Scene View 持续打开时仍然是不必要的开销。
**潜在风险**：多个 Gizmo 绘制类都这样做会累积成性能问题
**建议方向**：缓存 bootstrap 引用，在 OnValidate 或 null 时重新查找

### AT-009 | 严重度 🟢低 | 战斗状态面板的 EnumCamp 判定用了 Danmaku 命名空间前缀
**涉及章节**：TOOLS_TDD_02 §4.2 Entity 统计
**质疑**：`e.Camp == Danmaku.EnumCamp.Enemy` 和 `e.Camp == Danmaku.EnumCamp.Player || e.Camp == Danmaku.EnumCamp.Ally` — 代码中直接使用 `Danmaku.EnumCamp` 前缀。但 Entity 系统命名空间是 `MiniGameTemplate.Entity`，EnumCamp 在 Entity 命名空间下更合理。如果后续 EnumCamp 从 Danmaku 移到 Entity 命名空间（已在 EC_TDD 中讨论过），此处需要同步修改。
**潜在风险**：命名空间迁移时需要改工具代码
**建议方向**：加 `using EnumCamp = Danmaku.EnumCamp;` 别名，集中一处修改

### AT-010 | 严重度 🟢低 | 统计面板未区分 Entity 的子弹和非子弹
**涉及章节**：TOOLS_TDD_02 §4.2 Entity 统计
**质疑**：`活跃 Entity: N` 统计了所有 Entity，包括子弹。ShooterGame 中子弹数量远大于飞机/敌机数量（子弹可能 50+ 而敌机 10+），一个笼统的数字对调试价值有限。
**潜在风险**：开发者看到"活跃 Entity: 62"但不知道是 50 颗子弹 + 12 个敌机
**建议方向**：拆分为"非子弹 Entity"和"活跃子弹"两行，或在敌方/友方计数旁增加"子弹"列

---

**攻方总结**：2🔴 + 6🟡 + 2🟢 = 10 个问题
- 🔴 AT-001 调试代码绕过 DamageDealer 正式管线
- 🔴 AT-002 EditorWindow 每帧 O(n) 遍历 + 无条件 Repaint
- 🟡 AT-003~008 是架构清晰性和可维护性问题
- 🟢 AT-009~010 是代码卫生和信息可读性优化

---

## PK Round 1 — 守方回应（Unity 编辑器工具开发者）

| ID | 判定 | 处理摘要 |
|----|------|---------|
| AT-001 | ✅ 已修正 | SkipToNextWave 改用 `DamageDealer.ApplyDamage(entity, 99999, default)` 走正式伤害管线 |
| AT-002 | ✅ 已修正 | 改为 `EditorApplication.update` + 0.1s 间隔定时 `Repaint()`，OnGUI 末尾不再无条件刷新；已知限制表标注已修复 |
| AT-003 | ✅ 已修正 | Gizmo 代码段标注"引用核心 TDD_02 §1.2 已有 _baseLineY 字段，不重复声明"，仅追加 OnDrawGizmos 方法 |
| AT-004 | ✅ 已修正 | 新增 `SG_EditorUtility` 公共类含 `FindSOByName<T>`，SG_DebugMenuItems 和 SG_BattleStateWindow 均引用 |
| AT-005 | ✅ 已修正 | SG_SpawnWaveSOEditor.OnEnable 首行调用 `base.OnEnable()` |
| AT-006 | ✅ 已修正 | DeepCopyGroups 添加"字段变更同步"注释 + JsonUtility 替代方案说明 |
| AT-007 | ✅ 已修正 | 菜单路径提为 `private const string MENU_*`，执行/Validate 共用同一 const |
| AT-008 | ✅ 已修正 | Gizmo 中缓存 `_cachedBootstrap` 引用，null 时重新查找 |
| AT-009 | ✅ 已修正 | 添加 `using EnumCamp = Danmaku.EnumCamp;` 别名，命名空间迁移只改一处 |
| AT-010 | ✅ 已修正 | Entity 统计拆分为"敌方单位 / 友方单位 / 子弹"三行 |

**文档版本**：v1.1 → v1.2（10 处修正 / 2🔴 管线+性能 + 6🟡 架构 + 2🟢 卫生）

---

## PK Round 2 — 攻方复审

### Round 1 回应评估

- AT-001: 🟢 满意，DamageDealer.ApplyDamage 是框架唯一伤害入口，调试行为与正式行为一致
- AT-002: 🟢 满意，0.1s 定时刷新是 EditorWindow 的标准做法
- AT-003: 🟢 满意，注释明确避免开发者重复声明
- AT-004: 🟢 满意，SG_EditorUtility 作为工具类位置合理
- AT-005: 🟢 满意，base.OnEnable() 是继承 Editor 的基本规范
- AT-006: 🟢 满意，注释 + 替代方案双保险
- AT-007: 🟢 满意，const 化消除路径字符串不同步风险
- AT-008: 🟢 满意，缓存引用 + null 检查是 Gizmo 的标准做法
- AT-009: 🟢 满意，using 别名集中管理
- AT-010: 🟢 满意，拆分后调试信息可读性显著提升

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
1. 2 个 🔴 高优问题全部解决（DamageDealer 管线一致性 + EditorWindow 性能）
2. 6 个 🟡 中优问题全部解决（字段去重/公共工具/base 调用/深拷贝健壮性/路径常量化/Gizmo 缓存）
3. Round 2 攻方确认所有回应满意，无新问题

### 最有价值的 Top 3 变更
1. **DamageDealer 正式管线**（AT-001）— 调试行为与正式行为完全一致，避免隐蔽差异
2. **EditorWindow 定时刷新**（AT-002）— 消除编辑器性能隐患
3. **SG_EditorUtility 公共工具类**（AT-004）— 消除重复代码，集中维护点

### 遗留项
无。所有问题已在 V1 文档中修正。
</content>
<parameter name="explanation">创建 SG_TOOLS_TDD 第二轮 PK 评审记录文件（攻方 = Unity 架构师）