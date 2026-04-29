# PK 评审记录 — Entity-Component TDD v2.1（第 2 轮 PK：策划工作流视角）

> **目标文档**：MiniGameTemplate/Docs/Agent/ENTITY_COMPONENT_TDD.md
> **文档类型**：TDD
> **攻方角色**：游戏设计师（资深策划，10 年手游开发经验，专精角色系统策划工作流、配置管线、编辑器工具链）
> **守方角色**：Unity 架构师（专精 Unity 系统架构、WebGL/小游戏平台限制、性能优化）
> **开始时间**：2026-04-26 18:04
> **最大轮次**：3
> **PK 状态**：✅ 已收敛（Round 2 闭环，2026-04-26 19:10）

---

## PK Round 1 — 攻方提问

### [GD-001] | 严重度 🔴高 | 角色外观/渲染资源配置完全缺失——策划无法指定"敌兵长什么样"

**涉及章节**：§五.1 TbEntityConfig、§三.10 渲染架构预留、§四.3 AnimationComponent
**质疑**：TbEntityConfig 表只有 id、name、faction、maxHp、moveSpeed 等纯逻辑字段，没有任何与角色外观相关的配置项——没有 spriteRef/spineAsset/prefabPath/renderSize/renderScale/deathVFX/spawnVFX/hitVFX。

现有弹幕系统做了很好的示范：BulletTypeSO 同一个 SO 里把 SourceTexture、Tint、Size、Trail、Explosion 全部配好了，策划在一个 Inspector 里就能完成全部视觉配置。而 EntityConfig 表里，策划看到的一个敌兵就是一行数字。

§3.10 说"Phase 1 先不做渲染集成"——对策划来说**一个不可见的敌兵等于不存在**。P1.9 验收场景需要"1 玩家 + 3 敌人 + 弹幕交互"，策划怎么验收看不见的敌人？

**潜在风险**：Phase 1 策划/美术完全无法参与验收；Phase 2 补渲染时必须回头大改配置表。
**建议方向**：TbEntityConfig 至少预留 viewPrefabPath/viewConfigId；Phase 1 提供调试占位符（Debug Sprite/彩色方块 + 碰撞圆框），参考 ObstacleRegistrar。
**状态**：🟡 待回应

---

### [GD-002] | 严重度 🔴高 | `components: list<int>` 对策划完全不可读——数字 ID 配组件是反人类工作流

**涉及章节**：§五.1 TbEntityConfig、§三.2 ComponentType 枚举
**质疑**：TbEntityConfig 定义 `components: list<int>`，策划要在表里填 `[0, 1, 2, 3, 4, 8]` 来表达组件列表。没有人记得住 0=State, 4=Collision, 8=AI。填错一个数字就导致运行时组件缺失，排查困难。

对比 BulletTypeSO：策划通过 bool/enum 字段控制功能显隐，BulletTypeSOEditor 做了条件显示。策划看到的是人话，不是数字。

**潜在风险**：配置出错概率极高，策划对组件选配完全没有直觉——不知道哪些必须配、哪些互斥、哪些依赖。
**建议方向**：Luban 使用 enum 类型列表；或定义"角色模板"概念预置常用组合；添加导表校验规则（如 Control 和 AI 互斥）。
**状态**：🟡 待回应

---

### [GD-003] | 严重度 🔴高 | 刷怪/波次编排工作流完全缺失——策划无法在编辑器中摆放刷怪点

**涉及章节**：§六 实施计划 P1.9、§三.7 EntityManager
**质疑**：天命人在 D-04 已明确要求"刷怪点可放到场景上，有可视化编辑器"。但 TDD 中：没有 SpawnPoint/SpawnZone/WaveConfig 概念，EntityManager.Spawn() 是纯代码调用，没有编辑器工具规划。

对比现有弹幕系统：ObstacleSpawner 在 Inspector 配置障碍物列表+Scene View Gizmo 可视化，ObstacleRegistrar 拖放 GameObject 所见即所得，DanmakuDemoController 在 Inspector 拖入 SO 配好就跑。

**潜在风险**：Phase 1 做出来策划完全无法独立搭建关卡/验收场景，与 D-04 决策直接冲突。
**建议方向**：Phase 1 提供 EntitySpawnPoint MonoBehaviour（类似 ObstacleRegistrar）+Scene View Gizmo+WaveEditorSO。
**状态**：🟡 待回应

---

### [GD-004] | 严重度 🔴高 | 纯逻辑 Entity 无 GameObject——运行时策划/QA 完全无法调试

**涉及章节**：§二 BC-01.1、§三.10、§九 待后续细化
**质疑**：BC-01.1 说"不持有 GameObject"。Phase 1 完成后：策划在 Hierarchy 看不到 Entity，Scene View 看不到位置，QA 无法点击选中查看血量/状态/组件。

现有弹幕系统用 DanmakuCollisionGizmosDrawer 在 Scene View 画碰撞 Gizmo。ObstacleSpawner 有完整的颜色编码 Gizmo（不可摧毁/可摧毁/已摧毁）+HP 标签。Entity 什么都没有。

**潜在风险**：所有人都在"盲调"——运行时完全没有可视化手段。
**建议方向**：Phase 1 必须提供 EntityDebugGizmoDrawer + 可选的 EntityDebugHUD（运行时覆盖层显示 Entity 位置/HP/状态）。
**状态**：🟡 待回应

---

### [GD-005] | 严重度 🟡中 | 策划创建一个新敌兵的端到端工作流没有定义

**涉及章节**：全文档（缺失内容）
**质疑**：整份 TDD 从程序架构角度讲了 Entity 怎么工作，但没有回答一个基本问题：**策划想创建一个新的小兵敌人，从头到尾需要做什么？**

弹幕系统有隐含但清晰的工作流：右键 Create → BulletTypeSO → Inspector 配外观/碰撞/拖尾 → 创建 PatternSO → 拖入 PatternGroupSO → 拖入 SpawnerProfileSO → 场景中 DanmakuDemoController 引用。全程不写代码。

Entity 系统的等价工作流是什么？TDD 没有说。

**潜在风险**：程序做完后策划不知道怎么用，需要程序写文档甚至手把手教——违背"数据驱动"初衷。
**建议方向**：TDD 增加"策划工作流"章节（如§十），用流程图描述端到端创建路径。
**状态**：🟡 待回应

---

### [GD-006] | 严重度 🟡中 | EntityConfigSO vs Luban 二选一未决——Phase 1 不用 Luban 但配置入口没定义

**涉及章节**：§五 Luban 配置表设计、§八 BC-08
**质疑**：TDD 全文假设 Entity 配置走 Luban（TbEntityConfig），但天命人 D-04 说"刷怪点功能先不引用 Luban"。那 Phase 1 的 Entity 配置到底怎么做？

如果 Phase 1 不用 Luban，那就需要一个 `EntityConfigSO`（ScriptableObject）作为临时配置载体。但 TDD §3.6 的 EntityPool 构造函数接收 `int configId`，这个 ID 从哪来？谁来映射 configId → 实际配置数据？

**潜在风险**：Phase 1 配置管线架空——代码写了 Luban 接口但没有 Luban 表，SO 路径也没设计。
**建议方向**：Phase 1 设计 EntityConfigSO（参考 BulletTypeSO 模式），Phase 2 迁移到 Luban 或保留 SO 作为设计时配置+Luban 作为发布时配置。
**状态**：🟡 待回应

---

### [GD-007] | 严重度 🟢低 | AI 行为表（TbAIBehavior）对策划极不友好——条件和动作用数字枚举

**涉及章节**：§五.3 TbAIBehavior
**质疑**：`conditionType: int` + `actionType: int` 意味着策划要记住所有条件和动作的数字编号。比如"血量低于 20% 时逃跑"变成 `conditionType=2, conditionParam=0.2, actionType=3`。可以后续迭代解决，但应该在设计时预留可读性。

**潜在风险**：AI 调参效率低，策划对行为配置心智负担大。
**建议方向**：Luban 中 conditionType/actionType 用 enum 类型而非 int；加注释列。Phase 2+ 考虑可视化行为编辑器。
**状态**：🟡 待回应

---

> **攻方总结**：4 个🔴 + 2 个🟡 + 1 个🟢。核心问题集中在：**TDD 完全缺失策划工作流视角**——没有视觉配置、没有编辑器工具、没有端到端创建路径、没有运行时调试手段。这不是"后续可以补"的问题，而是如果 Phase 1 不解决 GD-001~004，做出来的系统策划完全无法使用。

---

## PK Round 1 — 守方回应

### GD-001 回应：✅ 接受——新增 EntityConfigSO 视觉配置 + Phase 1 调试占位符

攻方说得对。"纯逻辑层不管渲染"是架构分层原则没错，但 Phase 1 验收场景需要**看得见**。解决方案：

**1. 新增 EntityConfigSO**（替代 Phase 1 的 Luban TbEntityConfig）：
```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Entity/Entity Config")]
public class EntityConfigSO : ScriptableObject
{
    [Header("基础属性")]
    public string DisplayName;
    public EnumCamp DefaultCamp;
    public int MaxHp;
    public float MoveSpeed;
    public float CollisionRadius;
    
    [Header("组件配置")]
    public ComponentType[] Components;  // 枚举数组，不是 int 列表
    
    [Header("视觉表现（Phase 1 调试用）")]
    public Color DebugColor = Color.red;
    public Sprite DebugSprite;  // null = 用默认圆形
    
    [Header("视觉表现（Phase 2+ 正式）")]
    public GameObject ViewPrefab;  // Phase 2 桥接用，Phase 1 可 null
    
    [Header("对象池")]
    public int PoolInitial = 5;
    public int PoolMax = 20;
}
```

**2. Phase 1 调试占位符**：EntityManager.Spawn() 时自动创建一个 Debug GameObject（彩色圆 SpriteRenderer + 碰撞圈 Gizmo），和 Entity 逻辑绑定。Phase 2 替换为正式 ViewPrefab。

**文档变更**：§五.1 重写为 EntityConfigSO 设计；§3.10 新增 Phase 1 调试占位符方案。

**状态**：✅ 已回应（Round 1）——新增 EntityConfigSO + DebugView 占位符

---

### GD-002 回应：✅ 接受——components 改为 ComponentType[] 枚举数组

攻方完全正确。`list<int>` 对策划是灾难。

**解决方案**：上面 EntityConfigSO 已经用 `ComponentType[] Components` 替代。Unity Inspector 会自动用 enum 下拉菜单列出所有组件名，策划选的是 `State`, `Health`, `AI` 等人话，不是数字。

**追加**：后续迁移 Luban 时，Luban 表也用 enum 列表类型（Luban 原生支持 `list<ComponentType>`），导表时自动校验互斥规则（Control + AI 不能共存）。

**状态**：✅ 已回应（Round 1）——EntityConfigSO 用 ComponentType[] 枚举数组

---

### GD-003 回应：✅ 接受——新增 EntitySpawner 系统（MonoBehaviour + SO + Gizmo）

攻方完全正确，且与天命人 D-04 决策一致。

**设计方案**（参考 ObstacleSpawner/ObstacleRegistrar 模式）：

**1. EntitySpawnPointSO（刷怪配置 SO）**：
```csharp
[CreateAssetMenu(menuName = "MiniGameTemplate/Entity/Spawn Wave")]
public class EntitySpawnWaveSO : ScriptableObject
{
    public SpawnWaveEntry[] Waves;
    public float DelayBetweenWaves;
    public bool Loop;
}

[System.Serializable]
public struct SpawnWaveEntry
{
    public EntityConfigSO EntityConfig;  // 直接拖 SO，不是 int ID
    public EnumCamp Camp;
    public int Count;
    public float SpawnInterval;
}
```

**2. EntitySpawnPoint（场景中的刷怪点 MonoBehaviour）**：
```csharp
public class EntitySpawnPoint : MonoBehaviour
{
    [Header("刷怪配置")]
    public EntitySpawnWaveSO WaveConfig;
    
    [Header("生成区域")]
    public SpawnAreaMode AreaMode;
    public float AreaRadius = 1f;
    
    // Scene View Gizmo：画刷怪点图标 + 生成区域圈
    private void OnDrawGizmos() { /* 图标 + 圆圈/矩形 */ }
}
```

**3. EntitySpawner（驱动器，纯逻辑）**：由 EntityManager 或 DanmakuDemoController 每帧 Tick，参考 SpawnerDriver 预分配槽位模式。

策划工作流：场景中放一个空 GameObject → 挂 EntitySpawnPoint → Inspector 拖入 EntitySpawnWaveSO → Scene View 看到刷怪点 Gizmo → Play 自动刷怪。

**文档变更**：新增 §3.14 刷怪系统设计；P1.9 验收改为"策划通过 Inspector 配置刷怪"。

**状态**：✅ 已回应（Round 1）——新增完整 EntitySpawner 系统

---

### GD-004 回应：✅ 接受——Phase 1 必须包含调试可视化

攻方正确。"盲调"是不可接受的。

**解决方案**（两层）：

**1. EntityDebugView**（Phase 1 自动创建的 Debug GameObject）：
- Entity Spawn 时，如果 EntityConfigSO.ViewPrefab == null，自动创建简易 Debug GameObject（SpriteRenderer 圆形 + HP 文本）
- 每帧同步 Entity 位置/朝向到 Debug GameObject
- Entity Despawn 时回收 Debug GameObject

**2. EntityGizmoDrawer**（Editor 模式）：
- 类似 DanmakuCollisionGizmosDrawer，[DrawGizmo] 遍历所有活跃 Entity 画碰撞圈
- 颜色编码：Player=绿色, Enemy=红色, Neutral=灰色
- 显示 HP 进度条 + EntityId 标签

**文档变更**：§九 "编辑器工具"从"待后续细化"提升到 Phase 1 必做项。

**状态**：✅ 已回应（Round 1）——Phase 1 必做调试可视化

---

### GD-005 回应：✅ 接受——新增策划工作流章节

攻方正确。缺少端到端工作流描述是 TDD 的重大遗漏。

**新增§十 策划工作流**，描述以下路径：

**创建新敌兵**：
1. Project 面板右键 → Create → MiniGameTemplate/Entity/Entity Config → 命名（如 `EC_Slime`）
2. Inspector 中配置：DisplayName、MaxHp、MoveSpeed、CollisionRadius
3. Components 数组选择：`State, Health, Movement, Collision, AI`
4. DebugColor 选红色（Phase 1 调试用）
5. 创建 EntitySpawnWaveSO → 把 EC_Slime 拖进去 → 设数量/间隔
6. 场景中创建 EntitySpawnPoint → 拖入 WaveSO → 调整位置
7. Play → 看到红色圆点从刷怪点生成 → 被弹幕命中扣血消失

全程零代码。

**状态**：✅ 已回应（Round 1）——新增§十策划工作流

---

### GD-006 回应：✅ 接受——Phase 1 用 EntityConfigSO，Luban 移到 Phase 2

攻方正确。天命人已明确"先不引用 Luban"，TDD 不应假设 Phase 1 有 Luban。

**决策**：
- Phase 1：EntityConfigSO（ScriptableObject）作为唯一配置源
- Phase 2+：可选择保留 SO 或迁移到 Luban（两套可共存，SO 作为设计时配置、Luban 作为发布时配置）
- TDD §五 Luban 表设计降级为"Phase 2+ 预留"，不在 Phase 1 排期中
- P1.8（原"Luban 配置表接入"）改为"EntityConfigSO 配置驱动验证"

**文档变更**：§五 重写；P1.8 重定义。

**状态**：✅ 已回应（Round 1）——Phase 1 用 SO，Luban 移到 Phase 2

---

### GD-007 回应：✅ 接受但不阻塞——AI 行为表后续用 enum

攻方正确但不阻塞 Phase 1（AI 行为表是 Phase 2 的 Luban 工作）。

**标记为遗留项**：迁移 Luban 时，conditionType/actionType 必须用 enum 类型而非 int。TDD 中标注此约束。

**状态**：✅ 已回应（Round 1）——接受，标记为 Phase 2 遗留

---

## PK Round 1 收敛评估

| 维度 | 状态 |
|------|------|
| **本轮问题** | 7 个（4🔴 + 2🟡 + 1🟢） |
| **全部回应** | 7/7 ✅（6 接受修改 + 1 接受遗留） |
| **残余🔴** | 0（全部解决） |
| **严重度趋势** | Round 1: 4🔴→0🔴（大幅收敛） |

---

## PK Round 2 — 攻方二次质疑

> **说明**：以下问题不是重复 Round 1，而是针对守方 Round 1 回应中新方案本身的遗漏和风险。Round 1 共 7 个问题，Round 2 收敛为 5 个。

---

### [GD-101] | 严重度 🔴高 | EntityConfigSO 缺失战斗交互参数——策划只能配"纸面数值"，无法调试受击体验

**涉及章节**：守方 R1 GD-001 回应中的 EntityConfigSO 骨架代码
**质疑**：守方给出的 EntityConfigSO 只有 `MaxHp / MoveSpeed / CollisionRadius / DebugColor` 这几个字段。对比 BulletTypeSO——它不仅有碰撞半径，还有完整的碰撞响应链（OnHitTarget/OnHitObstacle/OnHitScreenEdge）和碰撞反馈（DamageFlashTint/DamageFlashFrames/BounceSFX）。

EntityConfigSO 至少还缺以下策划必须可调的参数：
- **受击反馈**：受击闪烁颜色/帧数、击退距离/曲线、无敌帧（iFrame）
- **死亡表现**：死亡延迟、死亡特效（PoolDefinition）、掉落物配置
- **弹幕交互参数**：命中后弹丸反应（消灭/穿透）

P1.9 验收要求"弹幕命中敌人扣血死亡"，如果策划想调"死后 0.3 秒再消失"或"受击闪白 3 帧"，就得改代码。

**潜在风险**：策划拿到 EntityConfigSO 发现只能填血量和移速，受击体验完全无法调参。
**建议方向**：TDD 中明确 EntityConfigSO 的完整字段清单骨架（至少覆盖 Phase 1 验收所需的受击/死亡参数），用 [Header] 分组，标注哪些 Phase 1 填、哪些 Phase 2 填。
**状态**：🟡 待回应

---

### [GD-102] | 严重度 🔴高 | EntitySpawnWaveSO 方案过于简化——无法表达真实的波次编排需求

**涉及章节**：守方 R1 GD-003 回应中的 EntitySpawnWaveSO + EntitySpawnPoint 设计
**质疑**：`SpawnWaveEntry` 只有 `{ EntityConfig, Camp, Count, SpawnInterval }`，这是单怪种线性时间轴，而实际策划需求更复杂：

1. **同一波多怪种**：一波里 3 个史莱姆 + 1 个精英——当前 SpawnWaveEntry 只能配一种怪
2. **触发条件**：策划需要"玩家进入区域后触发"或"上一波全灭后触发"——当前只有 DelayBetweenWaves
3. **同一刷怪点多配置**：EntitySpawnPoint 只能引用一个 WaveConfig SO
4. **生成分布**：多个怪的排列模式（随机散布 vs 一字横排 vs V 字阵型）

**潜在风险**：Phase 1 做出来策划只能配"每 N 秒刷 M 个同种怪"，稍复杂的关卡设计表达不了，程序返工重构 WaveSO 结构导致已配数据丢失。
**建议方向**：SpawnWaveEntry 改为包含 SpawnGroup[]（每组一个怪种+数量），新增 WaveTriggerMode 枚举 { Timer, AllCleared, OnEnterArea }。
**状态**：🟡 待回应

---

### [GD-103] | 严重度 🟡中 | EntityDebugView 的 GameObject 生命周期管理方案缺失——会成为性能/架构隐患

**涉及章节**：守方 R1 GD-001 + GD-004 回应中的"Phase 1 自动创建 Debug GameObject"方案
**质疑**：守方说"Spawn 时自动创建 Debug GO，Despawn 时回收"，但没回答关键实现问题：

1. 创建方式：new GameObject()（GC 违反 BC-04.2）vs PoolManager（需要 PoolDefinition SO）vs EntityPool 内部预分配
2. Phase 2 切换路径：ViewPrefab != null 时如何切换？接口是什么？
3. BC-01.1 矛盾：Entity 不持有 GO，Debug GO 的引用存在哪？

**潜在风险**：DebugView 是 Phase 1 核心验收依赖，但实现方案在 TDD 中完全未展开，有"做的时候才发现和零 GC/纯逻辑契约冲突"的风险。
**建议方向**：TDD 新增 EntityViewBridge 小节：Debug GO 走 PoolManager 池化；外部 `EntityViewBridge` 持有 EntityId→GO 映射，Entity 本身不持有 GO 引用。
**状态**：🟡 待回应

---

### [GD-104] | 严重度 🟡中 | SO → Luban 迁移路径存在寻址方式断裂——configId（int）vs SO 引用不兼容

**涉及章节**：守方 R1 GD-006 回应 + TDD 原文 §3.6 EntityPool + §3.7 EntityManager
**质疑**：TDD 原文核心 API 全部基于 `int configId` 寻址（EntityPool 构造函数、EntityManager.Spawn、_pools Dictionary）。Phase 1 用 EntityConfigSO 后，configId 从哪来？两条路各有缺陷：
- **方案 A**：SO 上填 int Id → 策划维护全局唯一 ID
- **方案 B**：用 SO 引用为 key → Phase 2 迁 Luban 时改 API 签名

这是影响核心 API 签名的设计决策，不是编码细节。

**潜在风险**：选错寻址方式导致 Phase 2 大面积 API 重构。
**建议方向**：推荐 EntityConfigSO 内置 `[SerializeField] int ConfigId`，策划填 ID，同时保留 SO 引用便利性。Phase 1 内部用 SO 引用 + ConfigId 双路，Phase 2 Luban 用同 ID 无缝切。
**状态**：🟡 待回应

---

### [GD-105] | 严重度 🟡中 | BC-08 行为契约与 Phase 1 实际实现矛盾未解决——TDD 文档自相冲突

**涉及章节**：TDD 原文 §二 BC-08 全部条目 + §3.8 集成矩阵 + §六 P1.8 + 守方 R1 GD-006 回应
**质疑**：守方 R1 说"Phase 1 用 SO，Luban 移到 Phase 2"、"P1.8 改为'EntityConfigSO 配置驱动验证'"，但 TDD 原文中 BC-08 三条全写 Luban、§3.8 写"Luban → Phase 1"、§3.2 注释写"owner.Config 提供 Luban 配置数据"。BC-08 是行为契约层（稳定层），修改需 ADR 审批。守方承诺了不用 Luban 但没说 BC-08 怎么处置：
- 删除 BC-08？
- 改写为抽象配置契约？
- 标记为"Phase 2 生效"？

**潜在风险**：行为契约是团队"宪法"——契约说 Luban 实现做 SO，后续依赖 BC-08 做设计的人会被误导。
**建议方向**：改写 BC-08 为抽象配置契约（同时覆盖 SO 和 Luban），并输出完整变更清单。
**状态**：🟡 待回应

---

> **攻方 Round 2 总结**：2🔴 + 3🟡 = 5 个问题（少于 Round 1 的 7 个，在收敛）。核心聚焦：(1) 方案完整性不足——EntityConfigSO 和 EntitySpawnWaveSO 是骨架级别；(2) 新旧方案衔接断裂——TDD 原文 API/契约仍指向 Luban。

---

## PK Round 2 — 守方回应

### GD-101 回应：⚠️ 部分接受——扩充 EntityConfigSO 字段，但严控 Phase 1 范围

攻方说得有道理，但需要区分"Phase 1 验收必需"和"完整系统需要"。Phase 1 验收是"弹幕命中扣血死亡"，不是"完美受击体验"。

**Phase 1 扩充字段**（验收刚需）：
```csharp
[Header("受击反馈（Phase 1 最小集）")]
public float HitFlashDuration = 0.1f;        // 受击闪白持续时间
public Color HitFlashColor = Color.white;     // 受击闪白颜色

[Header("死亡表现（Phase 1 最小集）")]
public float DeathDelay = 0.3f;               // 死亡延迟（播完表现再回收）
public PoolDefinition DeathEffect;            // 死亡特效（走 PoolManager，可选）

[Header("受击反馈（Phase 2+ 扩展）")]
// public float KnockbackDistance;            // 击退距离
// public int IFrameCount;                    // 无敌帧
// public AnimationCurve KnockbackCurve;      // 击退曲线
```

**决策**：TDD 中用 [Header] 分组展示完整字段骨架，Phase 1 只实现标注为"Phase 1 最小集"的字段，Phase 2 字段注释掉但预留位置。策划可以看到完整蓝图，程序按阶段实现。

**弹幕交互参数不放在 EntityConfigSO**——弹丸碰到目标后的行为（消灭/穿透）由 BulletTypeSO.OnHitTarget 控制，这已经是现有弹幕系统的职责。Entity 层不应重复定义。

**状态**：⚠️ 部分接受——Phase 1 扩充 HitFlash + DeathDelay + DeathEffect，其余预留骨架

---

### GD-102 回应：⚠️ 部分接受——扩展 SpawnWaveEntry 结构，但 Phase 1 不做触发系统

攻方指出了结构性设计缺陷，但需要务实地控制范围。

**接受的结构改动**（Phase 1 必做，因为影响 SO 资产格式）：
```csharp
[System.Serializable]
public struct SpawnWaveEntry
{
    public SpawnGroup[] Groups;              // 支持单波多怪种
    public WaveTriggerMode TriggerMode;      // 触发模式
    public float TriggerDelay;               // Timer 模式的延迟
}

[System.Serializable]
public struct SpawnGroup
{
    public EntityConfigSO EntityConfig;
    public EnumCamp Camp;
    public int Count;
    public float SpawnInterval;
}

public enum WaveTriggerMode
{
    Timer = 0,          // 上一波结束后延迟 N 秒
    AllCleared = 1,     // 上一波全灭后触发
    // OnEnterArea = 2, // Phase 2+ 实现（需要触发区域组件）
}
```

**Phase 1 实现范围**：Timer + AllCleared 两种模式够用。OnEnterArea 需要额外的 TriggerZone 组件，Phase 2 再做。

**阵型排列不放在 WaveEntry**——Phase 1 用随机散布（AreaRadius 内随机点），阵型是锦上添花。如果需要精确位置控制，策划可以放多个 EntitySpawnPoint，每个只刷 1 个怪。

**状态**：⚠️ 部分接受——SpawnGroup[] + WaveTriggerMode（Timer/AllCleared），阵型/OnEnterArea Phase 2

---

### GD-103 回应：✅ 接受——新增 EntityViewBridge 设计

攻方完全正确。这是 Phase 1 的架构关键点，不能留白。

**设计方案**：

```csharp
/// <summary>
/// Entity 逻辑层与视觉层的桥接器。
/// 持有 EntityId → View GO 映射，Entity 本身不持有 GO 引用（BC-01.1 不变）。
/// Phase 1: 使用内置 Debug Prefab（彩色圆 + HP 文本）
/// Phase 2: 使用 EntityConfigSO.ViewPrefab（策划指定的正式 Prefab）
/// </summary>
public class EntityViewBridge
{
    private readonly Dictionary<uint, GameObject> _views;  // EntityId.Value → View GO
    private readonly PoolManager _poolManager;
    private PoolDefinition _debugViewPool;  // Phase 1 内置 Debug Prefab 的池

    /// <summary>Entity 生成时调用——创建/获取对应的 View GO</summary>
    public void OnEntitySpawned(Entity entity, EntityConfigSO config)
    {
        PoolDefinition pool = config.ViewPrefab != null 
            ? config.ViewPoolDef   // Phase 2: 正式 View
            : _debugViewPool;      // Phase 1: Debug View
        
        var go = _poolManager.Get(pool);
        go.transform.position = entity.Position;
        _views[entity.Id.Value] = go;
    }

    /// <summary>每帧同步位置/朝向/HP 显示</summary>
    public void SyncAll(EntityManager manager) { /* ... */ }

    /// <summary>Entity 回收时调用——归还 View GO 到池</summary>
    public void OnEntityDespawned(Entity entity, EntityConfigSO config) { /* ... */ }
}
```

**关键决策**：
1. Debug View Prefab 是项目内置资产（一个带 SpriteRenderer + TextMesh 的最简 Prefab），通过 PoolDefinition 走 PoolManager 池化——零运行时 GC
2. EntityViewBridge 是独立管理器，不在 Entity 内部——BC-01.1 不变
3. Phase 2 切换：策划在 EntityConfigSO 上填 ViewPrefab → EntityViewBridge 自动使用对应 PoolDefinition → 无需改代码
4. EntityViewBridge 由游戏层 MonoBehaviour 持有并驱动（和 EntityManager 同级）

**文档变更**：新增 §3.15 EntityViewBridge 设计。

**状态**：✅ 已回应——新增 EntityViewBridge + Debug Prefab 走 PoolManager 池化

---

### GD-104 回应：✅ 接受——Phase 1 使用 SO 引用为主键 + ConfigId 双路桥接

攻方正确，这是核心 API 签名决策。

**决策**：Phase 1 API 以 EntityConfigSO 为主键，内部用 SO.GetInstanceID() 做 Dictionary key（Unity 保证会话内唯一）。同时 EntityConfigSO 保留 ConfigId 字段为 Phase 2 Luban 迁移铺路。

```csharp
// Phase 1 API 签名
public class EntityManager
{
    private readonly Dictionary<EntityConfigSO, EntityPool> _pools;

    public Entity Spawn(EntityConfigSO config, Vector2 position, float rotation)
    {
        var pool = GetOrCreatePool(config);
        // ...
    }
}

// EntityConfigSO 中
[Header("配置 ID（Phase 2 Luban 迁移用）")]
[Tooltip("全局唯一配置 ID。Phase 1 可不填（用 SO 引用），Phase 2 迁移 Luban 时必填。")]
public int ConfigId;
```

**Phase 2 迁移路径**：
1. EntityConfigSO.ConfigId 填上与 Luban TbEntityConfig.id 相同的值
2. EntityManager 增加 `Spawn(int configId, ...)` 重载，内部查 Luban 表
3. 旧 `Spawn(EntityConfigSO, ...)` 保留（场景 Inspector 直接引用 SO 的路径不变）
4. 两条路径共存，渐进迁移

**TDD 原文变更**：§3.6 EntityPool 构造函数改为接收 `EntityConfigSO config`；§3.7 EntityManager._pools 改为 `Dictionary<EntityConfigSO, EntityPool>`。

**状态**：✅ 已回应——Phase 1 用 SO 引用 + ConfigId 双路

---

### GD-105 回应：✅ 接受——BC-08 改写为抽象配置契约 + 输出完整变更清单

攻方完全正确。行为契约自相矛盾是严重问题。

**BC-08 改写为**：

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-08.1 | Entity 配置（组件列表、属性）通过**配置资产**驱动（Phase 1: EntityConfigSO；Phase 2+: 可选 Luban 导表） | 待实现 |
| BC-08.2 | 配置资产通过 EntityManager 内部解析，外部通过 `EntityConfigSO` 引用或 `int ConfigId` 访问 | 待实现 |
| BC-08.3 | 新增状态标签/AI 行为只加配置，不改代码（Phase 1: 扩展 SO 字段；Phase 2+: 扩展 Luban 表） | 待实现 |

**完整变更清单**（TDD v2.2 需同步修改的位置）：

| 位置 | 变更 |
|------|------|
| §二 BC-08 | 改写为上述抽象配置契约 |
| §三.2 IEntityComponent.Init 注释 | "Luban 配置数据" → "配置数据（SO 或 Luban）" |
| §三.6 EntityPool 构造函数 | `int configId` → `EntityConfigSO config` |
| §三.7 EntityManager | `Dictionary<int, EntityPool>` → `Dictionary<EntityConfigSO, EntityPool>`；`Spawn(int configId,...)` → `Spawn(EntityConfigSO config,...)` |
| §三.8 集成矩阵 | Luban 行：Phase 1 → Phase 2 |
| §五 Luban 配置表设计 | 降级为"Phase 2+ 预留"，新增 §五.0 EntityConfigSO 设计（Phase 1 当前用） |
| §六 P1.8 | "Luban 配置表接入" → "EntityConfigSO 配置驱动验证" |
| 新增 §3.14 | 刷怪系统设计（EntitySpawnWaveSO + EntitySpawnPoint + EntitySpawner） |
| 新增 §3.15 | EntityViewBridge 设计 |
| 新增 §十 | 策划工作流章节 |
| §九 | 编辑器工具从"待后续细化"提升，Phase 1 必做 EntityGizmoDrawer |

**状态**：✅ 已回应——BC-08 改写 + 完整变更清单输出

---

## PK Round 2 收敛评估

| 维度 | 状态 |
|------|------|
| **本轮问题** | 5 个（2🔴 + 3🟡） |
| **全部回应** | 5/5 ✅（2 部分接受 + 3 完全接受） |
| **残余🔴** | 0（GD-101/102 的🔴部分已被部分接受解决，Phase 1 范围明确） |
| **严重度趋势** | Round 1: 7 问题(4🔴) → Round 2: 5 问题(2🔴) → 0 残余🔴 |
| **新增设计产物** | EntityViewBridge、SpawnGroup、WaveTriggerMode、BC-08 改写、完整变更清单 |

## 总收敛判定

| 维度 | Round 1 | Round 2 | 趋势 |
|------|---------|---------|------|
| 问题数 | 7 | 5 | ↓ 收敛 |
| 🔴高 | 4 | 2→0 | ↓ 全部解决 |
| 🟡中 | 2 | 3→0 | ↓ 全部回应 |
| 🟢低 | 1 | 0 | ↓ |
| 残余🔴 | 0 | 0 | 连续 2 轮 0🔴 |

**收敛结论**：✅ **达到收敛标准——连续 2 轮残余🔴 = 0**。Round 2 的 2 个🔴都通过"部分接受 + 明确 Phase 范围"解决。攻守双方在以下核心问题上达成共识：

1. **配置入口**：Phase 1 用 EntityConfigSO（SO 引用为主键 + ConfigId 双路桥接），Phase 2 可选迁移 Luban
2. **策划字段**：Phase 1 包含受击闪白 + 死亡延迟 + 死亡特效，其余预留骨架
3. **刷怪系统**：SpawnGroup[] 支持多怪种 + WaveTriggerMode（Timer/AllCleared），阵型/区域触发 Phase 2
4. **View 桥接**：EntityViewBridge 独立管理 EntityId→GO 映射，Debug Prefab 走 PoolManager 池化
5. **行为契约**：BC-08 改写为抽象配置契约，适用 SO 和 Luban 双路
6. **变更清单**：11 处 TDD 章节需同步更新

**不需要 Round 3**。

> **PK 状态**：✅ 已收敛（Round 2 闭环）

---

## 遗留 Backlog（PK 后可编码期间迭代）

| 编号 | 来源 | 内容 | 优先级 |
|------|------|------|--------|
| BL-01 | GD-101 | EntityConfigSO 扩充 Phase 2 受击参数（击退/无敌帧/击退曲线） | Phase 2 |
| BL-02 | GD-102 | WaveTriggerMode.OnEnterArea + TriggerZone 组件 | Phase 2 |
| BL-03 | GD-102 | 阵型排列模式（随机/一字排/V 字） | Phase 2+ |
| BL-04 | GD-007 | AI 行为表 conditionType/actionType 改 enum | Phase 2 Luban |
| BL-05 | GD-104 | Phase 2 Luban 迁移：添加 Spawn(int configId,...) 重载 | Phase 2 |