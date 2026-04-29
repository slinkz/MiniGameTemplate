# Entity-Component 通用角色框架 · TDD v2.6

> **版本**：v2.6  
> **日期**：2026-04-27  
> **状态**：Phase 1 编码进行中（P1.0 ✅ 完成 2026-04-30）  
> **前置文档**：MiniGameTemplate-EntityComponent-Design.md（v1.0 草案）  
> **决策记录**：ADR-033  
> **PK 评审记录**：ENTITY_COMPONENT_PK.md（R1 技术 PK）、ENTITY_COMPONENT_PK_R2.md（R2 策划工作流 PK）、ENTITY_COMPONENT_PK_R3.md（R3 软件架构师 PK）、ENTITY_COMPONENT_PK_R4.md（R4 游戏设计师 PK）、ENTITY_COMPONENT_PK_R5.md（R5 编辑器工具 PK）、ENTITY_COMPONENT_PK_R6.md（R6 策划落地性 PK）  
> **适用范围**：MiniGameTemplate 通用小游戏模板，面向微信小游戏赛道
>
> **v2.6 变更摘要**（PK R6 策划工作流落地性评审 WF-001~011）：
> - WF-001：新增 EntitySystemBootstrap 胶水层 MonoBehaviour（策划工作流闭环的"最后一公里"）
> - WF-002：EntityConfigSOEditor Play Mode 黄色 HelpBox + EntityDebugWindow Restart All Waves 按钮
> - WF-003：AttackBulletType 类型修正 VFXTypeSO → BulletTypeSO（文档 bug）
> - WF-004：新增 EntityManagerAccessor 全局静态访问点 + Gizmo/DebugWindow null 提示
> - WF-005：AIComponent 默认 fallback Idle + Validator Always 校验升 Error + Editor 红色 HelpBox
> - WF-006：EntityConfigSOEditor 空 Components 红色 HelpBox + SOCreationWizard 预填默认组件
> - WF-007：PoolDefinition 字段 Tooltip + 预览行（深度方案 Phase 2）
> - WF-008：§十 10.1 依赖关系图 + Validator 反向引用输出（Inspector 内嵌 Phase 2）
> - WF-009：P1.11 Demo 模板保留 + MODULE_README Quick Start 定义
> - WF-010：§十 工作流统一推荐右键菜单
> - WF-011：EntityConfigSOEditor 条件显示字段分段标题
>
> **v2.5 变更摘要**（PK R5 编辑器工具评审 ET-001~011）：
> - ET-001/002：§9.2 EntityConfigSOEditor—Checkbox Grid + 条件显示 + HelpBox 校验
> - ET-003：§9.1 EntityGizmoDrawer 重写为静态 [DrawGizmo] + [InitializeOnLoad] 模式
> - ET-004：§3.1 asmdef 隔离方案说明 + Editor 代码统一归入 _Framework/Editor/Entity/
> - ET-005：§9.3 AIBehaviorSOEditor—可读摘要标题
> - ET-006：§9.4 EntityConfigValidator—MenuItem 批量校验
> - ET-007：§9.5 EntitySpawnWaveSOEditor—波次摘要面板
> - ET-008：§9.6 EntityDebugWindow—Play Mode 概览面板
> - ET-009：§3.14 EntitySpawnPoint Gizmo 改 Always 绘制 + Label
> - ET-010：§9.7 SOCreationWizard 新增 Entity 系列 SO 类型
> - ET-011：§9.4/9.8 资产路径软警告（Phase 2）
>
> **v2.4 变更摘要**（PK R4 游戏设计师评审 GD-R4-001~011）：
> - GD-R4-001：新增 DamageContext struct + BulletCore.OwnerEntityId（伤害管线补齐）
> - GD-R4-002/010：新增 AIBehaviorSO 配置资产 + IAIAction 有状态 Action 接口 + 5 个内置 Action
> - GD-R4-003/009：新增 AttackComponent（Phase 1 最小攻击组件）+ 近战走弹幕说明
> - GD-R4-004/011：MovementComponent 新增 Knockback + Entity.PauseFor/IsPaused 预留 + ViewBridge 事件钩子说明
> - GD-R4-005：WaveTriggerMode 新增 OnCallback + EntitySpawnWaveSO 新增 Loop + SpawnGroup 新增 Formation
> - GD-R4-006：§一设计目标修正措辞 + 新增品类适配评估表
> - GD-R4-007：§十 策划工作流 SO 热修改限制措辞修正
> - GD-R4-008：EntityConfigSO 新增 SpawnEffect/HitEffect/ShowDamageNumber
>
> **v2.3 变更摘要**（PK R3 软件架构师评审 SA-001~SA-007）：
> - SA-001：BulletCore.PierceHitMask 从 ushort 改为 ulong（适配 64 槽位扩容）
> - SA-002：新增 P1.0 阵营枚举统一迁移步骤（BulletFaction→EnumCamp 全局替换）
> - SA-004：TypeId<T> 从 static readonly 改为 static int + 懒初始化（Domain Reload 安全）
> - SA-005：EntityViewBridge 内部存储从 Dictionary 改为预分配数组（零 GC 遍历）
> - SA-006：EntityManager 新增 CountAliveByConfig API + Spawner 时序明确
> - SA-007：BC-01.2 措辞修正（区分枚举版 O(1) 与泛型版 O(N)）
> - SA-003：风险表新增 SO Dictionary Key 的 Domain Reload 限制说明
>
> **v2.2 变更摘要**（PK R2 策划工作流评审 + 天命人决策 D-01~D-04）：
> - BC-08 改写为抽象配置契约（覆盖 SO 和 Luban 双路）
> - Phase 1 用 EntityConfigSO（SO 引用为主键 + ConfigId 双路桥接），Luban 移到 Phase 2
> - 新增 §3.14 刷怪系统（SpawnGroup[] + WaveTriggerMode）
> - 新增 §3.15 EntityViewBridge（Entity→View 映射，Debug Prefab 走 PoolManager）
> - 新增 §十 策划工作流章节
> - TargetRegistry 从 16 扩容到 64（天命人决策 D-01）
> - EntityConfigSO 扩充 HitFlash + DeathDelay + DeathEffect（Phase 1 最小集）
> - Phase 1 步骤从 P1.1~P1.9 扩展为 P1.1~P1.11

---

## 一、设计目标

> **v2.4 变更（GD-R4-006）**：修正品类定位措辞，新增品类适配评估表。

构建一套通用角色组件框架。**核心定位：弹幕射击 + 塔防**。通用部分（Entity/组件/池化/配置驱动）可扩展到 ARPG、Roguelike 等品类；跑酷/放置需要额外的领域特定系统层。

核心原则：按需组合 · 单一职责 · 数据驱动 · 微信小游戏友好

**品类适配评估表**：

| 品类 | 适配度 | 框架可直接复用 | 需额外开发 | 评估 |
|------|--------|--------------|-----------|------|
| **弹幕射击** | ⭐⭐⭐⭐⭐ | 全部 | 无 | 完全匹配 |
| **塔防** | ⭐⭐⭐⭐ | Entity/组件/池化/碰撞/刷怪/AI | 塔放置系统、路径规划 | 高度匹配 |
| **ARPG** | ⭐⭐⭐ | Entity/组件/池化/状态 | 技能系统(P3)、装备/属性修改器、完整 FSM | 中度匹配 |
| **Roguelike** | ⭐⭐⭐ | Entity/组件/池化/碰撞 | 运行时属性修改器(P2)、程序生成、道具系统 | 中度匹配 |
| **跑酷** | ⭐⭐ | Entity/组件/池化 | 重力/跳跃/地面检测、矩形碰撞、地形系统 | 低匹配，需重大扩展 |
| **放置** | ⭐⭐ | Entity/组件/池化/配置 | 离线模拟、快进 Tick、倍速战斗 | 低匹配，需重大扩展 |

**架构分层洞察**：Entity/Component/Pool/Config 是品类无关的**基础设施层**；TargetRegistry/CollisionSolver/DanmakuSystem 是**弹幕领域层**。框架的品类适配度 = 基础设施层 + 是否有对应的领域层。

---

## 二、行为契约层（BC-xx，稳定层）

> 行为契约定义"系统对外承诺什么行为"，不涉及实现细节。
> 修改行为契约需要 ADR 审批。

### BC-01 Entity 容器契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-01.1 | Entity 是纯数据容器，不继承 MonoBehaviour，不持有 GameObject | 待实现 |
| BC-01.2 | Entity 通过 `GetComponent(ComponentType)` 按枚举索引 O(1) 查询组件；泛型版 `GetComponent<T>()` 为 O(N)（N≤16，线性扫描 + 类型检查）（见 §3.2） | 待实现 |
| BC-01.3 | Entity 在生命周期节点（Init/Tick/Reset）统一驱动所有 Tickable 组件 | 待实现 |
| BC-01.4 | Entity 持有本地事件总线（EntityEventBus），组件只在本 Entity 范围内通信 | 待实现 |
| BC-01.5 | Entity 持有唯一 ID（EntityId，uint32），用于跨系统引用 | 待实现 |
| BC-01.6 | Entity 持有 Camp（阵营），Phase 1 统一使用 `EnumCamp` 枚举（天命人决策 D-02，替代原 BulletFaction）；如未来需扩展阵营，引入独立枚举 + 映射层（见 §3.11） | 待实现 |

### BC-02 组件基类契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-02.1 | 所有组件实现 `IEntityComponent` 接口：`Init(Entity owner)` / `Reset()` / `SetActive(bool)`。组件通过 `owner` 间接访问配置（见 §3.2） | 待实现 |
| BC-02.2 | Tickable 组件额外实现 `ITickable`：`Tick(float dt)` + `TickOrder` 属性 | 待实现 |
| BC-02.3 | 组件通过 `SetActive(false)` 休眠——从 TickList 移除，不响应事件，零开销 | 待实现 |
| BC-02.4 | 组件之间**禁止直接引用**，只通过 EntityEventBus 或 Entity.GetComponent 通信 | 待实现 |

### BC-03 Entity 本地事件总线契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-03.1 | EntityEventBus 为每个 Entity 独立实例，事件不跨 Entity 传播 | 待实现 |
| BC-03.2 | 支持 `Publish<T>(T evt)` / `Subscribe<T>(Action<T>)` / `Unsubscribe<T>(Action<T>)` | 待实现 |
| BC-03.3 | 事件类型用 struct（零 GC），通过泛型类型 ID 分发 | 待实现 |
| BC-03.4 | Reset 时自动清空所有订阅（防池化后事件泄漏） | 待实现 |

### BC-04 Entity 池管理器契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-04.1 | EntityPool 按配置类型分池（EntityConfigId → 独立池） | 待实现 |
| BC-04.2 | 池中 Entity + 组件整体预分配，取出/归还零 GC | 待实现 |
| BC-04.3 | 取出时调用所有组件 `Init()`，归还时调用所有组件 `Reset()` | 待实现 |
| BC-04.4 | 每池设 InitialCapacity（预热）+ MaxCapacity（硬上限），超限 LogWarning 不崩溃 | 待实现 |
| BC-04.5 | 池采用预分配数组 + 空闲槽位栈（参考 BulletWorld 模式），非 Queue\<Entity\> | 待实现 |

### BC-05 碰撞集成契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-05.1 | CollisionComponent 实现 `ICollisionTarget` 接口，桥接到现有 TargetRegistry | 待实现 |
| BC-05.2 | CollisionComponent 在 Init 时向 `DanmakuSystem.Instance.RegisterTarget()` 注册 | 待实现 |
| BC-05.3 | CollisionComponent 在 Reset 时向 `DanmakuSystem.Instance.UnregisterTarget()` 注销 | 待实现 |
| BC-05.4 | 碰撞回调（OnBulletHit/OnLaserHit/OnSprayHit）转发到 EntityEventBus | 待实现 |
| BC-05.5 | CircleHitbox 由 CollisionComponent 每帧从 Entity 位置 + 配置半径更新 | 待实现 |
| BC-05.6 | OBB 碰撞（Entity vs Entity）走 ObstaclePool 注册，复用 ObstacleCollisionMath | 待实现 |

### BC-06 Tick 管线契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-06.1 | EntityManager 统一驱动所有活跃 Entity 的 Tick，不依赖 MonoBehaviour.Update | 待实现 |
| BC-06.2 | Tickable 组件按 TickOrder 升序执行（数字越小越先） | 待实现 |
| BC-06.3 | 定频 Tick 组件内部自行计帧，间隔未到时跳过 | 待实现 |
| BC-06.4 | EntityManager.Tick() 在 DanmakuSystem.Update() 之后、LateUpdate 之前调用。**已知限制**：Entity 位置更新与碰撞检测存在 1 帧延迟，小游戏场景可接受（见 §3.12） | 待实现 |

### BC-07 决策接口契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-07.1 | ControlComponent 和 AIComponent 均实现 `IDecisionMaker` 接口 | 待实现 |
| BC-07.2 | 同一 Entity 上 Control 和 AI **互斥挂载** | 待实现 |
| BC-07.3 | AIComponent 内部策略抽象为 `IDecisionStrategy`，可替换 | 待实现 |
| BC-07.4 | 默认 AI 策略：ConditionActionTableStrategy（条件-动作表，配置驱动） | 待实现 |

### BC-08 配置驱动契约

> **v2.2 变更（GD-105）**：从 Luban 硬编码契约改写为抽象配置契约，同时覆盖 SO 和 Luban 双路。

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-08.1 | Entity 配置（组件列表、属性）通过**配置资产**驱动（Phase 1: EntityConfigSO；Phase 2+: 可选 Luban 导表） | 待实现 |
| BC-08.2 | 配置资产通过 EntityManager 内部解析，外部通过 `EntityConfigSO` 引用或 `int ConfigId` 访问 | 待实现 |
| BC-08.3 | 新增状态标签/AI 行为只加配置，不改代码（Phase 1: 扩展 SO 字段；Phase 2+: 扩展 Luban 表） | 待实现 |

---

## 三、技术方案层（易变层）

### 3.1 命名空间 & 目录结构

> **v2.5 变更（ET-004）**：补充 asmdef 隔离方案说明。

```
namespace MiniGameTemplate.Entity

Assets/_Framework/EntitySystem/
├── Scripts/
│   ├── Core/
│   │   ├── Entity.cs              — Entity 容器
│   │   ├── EntityManager.cs       — 全局管理 + Tick 驱动
│   │   ├── EntityManagerAccessor.cs — v2.6: 全局静态访问点（WF-004）
│   │   ├── EntitySystemBootstrap.cs — v2.6: 胶水层 MonoBehaviour（WF-001）
│   │   └── EntityPool.cs          — 按类型分池
│   ├── Components/
│   │   ├── IEntityComponent.cs    — 组件接口
│   │   ├── ITickable.cs           — Tickable 接口
│   │   ├── StateComponent.cs
│   │   ├── HealthComponent.cs
│   │   ├── AnimationComponent.cs
│   │   ├── MovementComponent.cs
│   │   ├── CollisionComponent.cs
│   │   ├── AutoAimComponent.cs
│   │   ├── SkillComponent.cs
│   │   ├── AttackComponent.cs    — v2.4: Phase 1 最小攻击组件
│   │   ├── ControlComponent.cs
│   │   └── AIComponent.cs
│   ├── Decision/
│   │   ├── IDecisionMaker.cs
│   │   ├── IDecisionStrategy.cs
│   │   ├── IAIAction.cs           — v2.4: AI Action 有状态执行器接口
│   │   ├── DecisionCommand.cs
│   │   ├── ConditionActionTableStrategy.cs
│   │   └── Actions/               — v2.4: 内置 Action 实现
│   │       ├── IdleAction.cs
│   │       ├── MoveToTargetAction.cs
│   │       ├── PatrolAction.cs
│   │       ├── AttackAction.cs
│   │       └── FleeAction.cs
│   ├── Events/
│   │   ├── EntityEvents.cs        — 所有 Entity 内部事件 struct 定义
│   │   └── EntityEventBus.cs      — 泛型事件分发
│   ├── View/
│   │   └── EntityViewBridge.cs    — Entity→View GO 桥接器
│   ├── Spawner/
│   │   ├── EntitySpawnPoint.cs    — 场景刷怪点组件
│   │   └── EntitySpawner.cs       — 刷怪驱动器
│   └── Config/
│       ├── EntityConfigSO.cs        — Phase 1 角色配置 SO
│       ├── AIBehaviorSO.cs          — v2.4: AI 行为条件-动作表 SO
│       ├── EntitySpawnWaveSO.cs     — 刷怪波次配置 SO
│       └── (Phase 2: Luban 生成配置)
│
├── MODULE_README.md               — v2.6: 含 Quick Start 段落（WF-009）
│
Assets/_Framework/Editor/Entity/       ← Editor 工具（归入 MiniGameFramework.Editor.asmdef）
├── EntityGizmoDrawer.cs               — v2.5: 静态 [DrawGizmo] 碰撞圈/HP Gizmo
├── EntityConfigSOEditor.cs            — v2.5: CustomEditor 条件显示 + HelpBox 校验
├── AIBehaviorSOEditor.cs              — v2.5: 行为条目可读摘要标题
├── EntitySpawnWaveSOEditor.cs         — v2.5: 波次摘要面板
├── EntityConfigValidator.cs           — v2.5: MenuItem 批量校验
└── EntityDebugWindow.cs               — v2.5: Play Mode 概览面板
```

**MODULE_README.md 内容定义（v2.6 新增，WF-009）**：
- **系统概述**（1 段）：Entity-Component 框架定位、核心功能一句话说明
- **Quick Start**（5 步）：
  1. 场景中加 EntitySystemBootstrap 组件
  2. 复制 `Assets/_Game/Configs/_Template/Template_Slime` SO → 改名
  3. 修改参数（HP/速度/弹幕/AI 行为等）
  4. 创建/引用 EntitySpawnWaveSO，在 SpawnPoint 中配置波次
  5. Play → 看效果
- **文件清单**：目录下各文件职责一句话说明

**asmdef 隔离方案（v2.5 新增，ET-004）**：
- **Runtime 代码**（`_Framework/EntitySystem/Scripts/`）归入 `MiniGameFramework.Runtime.asmdef`（已有，无需新建 asmdef）
- **Editor 工具**（CustomEditor / Gizmo / Validator / EditorWindow）放在 `_Framework/Editor/Entity/` 目录，归入 `MiniGameFramework.Editor.asmdef`（已有，includePlatforms: Editor）
- **不新建独立 asmdef**——项目规模尚小，复用框架级 asmdef 即可。Phase 2+ 如需拆分模块再评估
- 所有 Editor 代码必须包裹 `#if UNITY_EDITOR` 或放在 Editor asmdef 管辖目录
- EntitySystem/Scripts/ 下**不再有 Editor/ 子目录**——Editor 代码统一收归 `_Framework/Editor/Entity/`

### 3.2 核心接口定义

> **v2.1 变更（EC-001/EC-004）**：Init 统一为单参数；新增 ComponentType 枚举实现 O(1) GetComponent。

```csharp
namespace MiniGameTemplate.Entity
{
    /// <summary>Entity 唯一标识</summary>
    public readonly struct EntityId : System.IEquatable<EntityId>
    {
        public readonly uint Value;
        public EntityId(uint value) => Value = value;
        public bool Equals(EntityId other) => Value == other.Value;
        public override int GetHashCode() => (int)Value;
        public static readonly EntityId Invalid = new(0);
    }

    /// <summary>
    /// 组件类型枚举——Entity 内部以此为数组索引实现 O(1) GetComponent。
    /// 新增组件类型时在此枚举追加（最大 16 种，预留扩展）。
    /// </summary>
    public enum ComponentType : byte
    {
        State = 0,
        Health = 1,
        Animation = 2,
        Movement = 3,
        Collision = 4,
        AutoAim = 5,
        Skill = 6,
        Control = 7,
        AI = 8,
        // 预留 9~15
        MAX = 16
    }

    /// <summary>组件基接口</summary>
    public interface IEntityComponent
    {
        /// <summary>组件是否激活</summary>
        bool IsActive { get; }

        /// <summary>组件类型枚举（用于 Entity 内部数组索引）</summary>
        ComponentType Type { get; }

        /// <summary>
        /// 初始化（从池取出时调用）。
        /// 组件通过 owner 间接获取配置：owner.Config 提供配置数据（SO 或 Luban）。
        /// </summary>
        void Init(Entity owner);

        /// <summary>重置（归还池时调用，清运行时数据保留对象）</summary>
        void Reset();

        /// <summary>激活/休眠切换</summary>
        void SetActive(bool active);
    }

    /// <summary>需要每帧驱动的组件</summary>
    public interface ITickable
    {
        /// <summary>Tick 排序优先级（升序执行）</summary>
        int TickOrder { get; }

        /// <summary>每帧更新</summary>
        void Tick(float dt);
    }
}
```

**Entity.GetComponent 实现方案**：
```csharp
public class Entity
{
    // 固定长度数组，按 ComponentType 枚举索引
    private readonly IEntityComponent[] _components = new IEntityComponent[(int)ComponentType.MAX];

    // ── v2.4 新增：Pause 支持（GD-R4-011）──
    // Phase 1 预留，Phase 2 用于 HitStop 顿帧。
    // Phase 1 不调用 PauseFor()，IsPaused 永远 false——分支预测器零开销。
    private int _pauseFrames;
    public bool IsPaused => _pauseFrames > 0;
    public void PauseFor(int frames) => _pauseFrames = frames;
    internal void DecrementPauseFrames() { if (_pauseFrames > 0) _pauseFrames--; }

    /// <summary>
    /// 泛型版：O(N) 线性扫描 + is T 类型检查（N≤16，热路径建议用枚举版）。
    /// </summary>
    public T GetComponent<T>() where T : class, IEntityComponent
    {
        for (int i = 0; i < (int)ComponentType.MAX; i++)
        {
            if (_components[i] is T result) return result;
        }
        return null;
    }

    /// <summary>
    /// 枚举版：O(1) 直接数组索引，零类型检查。热路径首选。
    /// </summary>
    public IEntityComponent GetComponent(ComponentType type) => _components[(int)type];
}
```

### 3.3 Tick 优先级常量

```csharp
public static class TickOrders
{
    public const int Decision   = 100;  // ControlComponent / AIComponent
    public const int AutoAim    = 200;  // AutoAimComponent
    public const int Movement   = 300;  // MovementComponent
    public const int Animation  = 400;  // AnimationComponent
}
```

### 3.4 EntityEventBus 设计

> **v2.1 变更（EC-003）**：改为预分配固定长度 Handler 列表，彻底消除 Delegate.Combine GC；补充 TypeId<T> 实现方案。

```csharp
/// <summary>
/// 零 GC 实体本地事件总线。
/// TypeId<T> 通过泛型静态字段递增分配（编译期确定），O(1) 类型分发。
/// Handler 存储用预分配固定数组替代 Delegate.Combine，避免委托链 GC。
/// </summary>
public sealed class EntityEventBus
{
    private const int MAX_EVENT_TYPES = 16;   // 预留 16 种事件类型
    private const int MAX_HANDLERS_PER_TYPE = 4; // 每种事件最多 4 个订阅者

    // 二维预分配数组：[eventTypeId][handlerSlot]
    private readonly System.Delegate[,] _handlers = new System.Delegate[MAX_EVENT_TYPES, MAX_HANDLERS_PER_TYPE];
    private readonly int[] _handlerCounts = new int[MAX_EVENT_TYPES];

    public void Publish<T>(T evt) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        for (int i = 0; i < count; i++)
        {
            ((System.Action<T>)_handlers[typeId, i])?.Invoke(evt);
        }
    }

    public void Subscribe<T>(System.Action<T> handler) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        if (count >= MAX_HANDLERS_PER_TYPE) return; // 静默丢弃，开发期 LogWarning
        _handlers[typeId, count] = handler;
        _handlerCounts[typeId] = count + 1;
    }

    public void Unsubscribe<T>(System.Action<T> handler) where T : struct
    {
        int typeId = TypeId<T>.Get(); // v2.3: 懒初始化，Domain Reload 安全
        if (typeId >= MAX_EVENT_TYPES) return;
        int count = _handlerCounts[typeId];
        for (int i = 0; i < count; i++)
        {
            if (_handlers[typeId, i] == (System.Delegate)handler)
            {
                // swap-remove
                _handlers[typeId, i] = _handlers[typeId, count - 1];
                _handlers[typeId, count - 1] = null;
                _handlerCounts[typeId] = count - 1;
                return;
            }
        }
    }

    public void ClearAll()
    {
        System.Array.Clear(_handlers, 0, _handlers.Length);
        System.Array.Clear(_handlerCounts, 0, _handlerCounts.Length);
    }
}

/// <summary>
/// 泛型事件类型 ID 分配器。利用泛型静态字段实现自动递增。
/// IL2CPP/AOT 安全——每个 T 的静态字段在首次访问时初始化。
/// 
/// v2.3 变更（SA-004）：从 static readonly 改为 static int + 懒初始化，
/// 解决 Domain Reload 后 TypeId 乱序导致 EventBus 事件分发错误的问题。
/// static readonly 字段在 Domain Reload 后不会被重新赋值（CLR 语义），
/// 而 static int + 懒初始化可以在 Reset 后重新分配正确的 ID。
/// </summary>
private static class TypeId<T> where T : struct
{
    public static int Value = -1; // -1 = 未分配

    public static int Get()
    {
        if (Value < 0) Value = TypeIdCounter.Next();
        return Value;
    }
}
private static class TypeIdCounter
{
    private static int _next;
    private static readonly System.Collections.Generic.List<System.Action> _resetCallbacks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _next = 0;
        // 重置所有已分配的 TypeId（通过回调列表）
        for (int i = 0; i < _resetCallbacks.Count; i++)
            _resetCallbacks[i]?.Invoke();
        _resetCallbacks.Clear();
    }

    public static int Next()
    {
        int id = System.Threading.Interlocked.Increment(ref _next) - 1;
        // 注册重置回调（利用闭包无法捕获泛型类型参数，故在调用侧注册）
        return id;
    }

    /// <summary>注册 Domain Reload 时的 TypeId 重置回调</summary>
    public static void RegisterResetCallback(System.Action callback) => _resetCallbacks.Add(callback);
}

// ── 伤害上下文（v2.4 新增，GD-R4-001）──
// 替代裸 int damage，携带攻击者信息 + 命中类型，供伤害管线扩展。
// Phase 1 HealthComponent 直接读 BaseDamage 扣血；
// Phase 2 游戏层可订阅 OnCollisionHit 在 TakeDamage 前拦截处理（护甲/暴击等）。
public struct DamageContext
{
    public int BaseDamage;              // 弹幕配置的原始伤害（TypeSO.Damage）
    public EntityId AttackerId;         // 发射者 EntityId（无发射者时 = Invalid）
    public CollisionEventType HitType;  // Bullet / Laser / Spray
    // Phase 2 扩展预留：DamageType (Physical/Magical)、CritMultiplier 等
}

// 事件 struct 定义
public struct OnStateChanged { public int OldState; public int NewState; }
public struct OnDamaged { public int Damage; public int RemainingHp; public EntityId Source; }
public struct OnDeath { public EntityId Killer; }
public struct OnPositionChanged { public Vector2 OldPos; public Vector2 NewPos; }
public struct OnTargetAcquired { public EntityId Target; }
public struct OnTargetLost { }
public struct OnSkillCast { public int SkillId; public EntityId Target; }
public struct OnAnimEvent { public int EventId; }
// v2.4 变更（GD-R4-001）：OnCollisionHit 改为携带完整 DamageContext
public struct OnCollisionHit { public DamageContext Context; }
```

### 3.5 CollisionComponent → ICollisionTarget 桥接

> **v2.4 变更（GD-R4-001）**：OnBulletHit 等回调改为构造 DamageContext（携带攻击者信息），替代裸 int damage。

```csharp
/// <summary>
/// 将 Entity 桥接到弹幕碰撞系统。
/// 实现 ICollisionTarget，复用 TargetRegistry 的 64 槽位（v2.2 扩容）。
/// v2.4：碰撞回调构造 DamageContext 发布到 EntityEventBus。
/// </summary>
public class CollisionComponent : IEntityComponent, ICollisionTarget
{
    private Entity _owner;
    private float _radius;
    private int _targetSlot = -1;

    // ── ICollisionTarget 实现 ──
    public CircleHitbox Hitbox => new(_owner.Position, _radius);
    public EnumCamp Faction => _owner.Camp;

    // v2.4: 构造 DamageContext，携带 AttackerId（从 BulletCore.OwnerEntityId 获取）
    public void OnBulletHit(int damage, int bulletIndex)
    {
        // CollisionSolver 需将 BulletCore.OwnerEntityId 传入（v2.4 新增参数）
        // Phase 1 暂用 EntityId.Invalid 作为 fallback
        _owner.EventBus.Publish(new OnCollisionHit
        {
            Context = new DamageContext
            {
                BaseDamage = damage,
                AttackerId = EntityId.Invalid, // Phase 1: 由 CollisionSolver 填充
                HitType = CollisionEventType.BulletHit
            }
        });
    }

    public void OnLaserHit(int damage, int laserIndex) { /* 同上模式，HitType=LaserHit */ }
    public void OnSprayHit(int damage, int sprayIndex) { /* 同上模式，HitType=SprayHit */ }

    // ── IEntityComponent 实现 ──
    public void Init(Entity owner)
    {
        _owner = owner;
        _radius = owner.ConfigSO.CollisionRadius;
        // 注册到弹幕碰撞系统（直接调用 TargetRegistry 以获取槽位索引）
        var ds = DanmakuSystem.Instance;
        if (ds != null)
        {
            _targetSlot = ds.TargetRegistry.Register(this);
            if (_targetSlot < 0)
            {
                Debug.LogError($"[CollisionComponent] Entity {_owner.Id} 注册碰撞目标失败：TargetRegistry 已满（64/64），需扩容");
                _isCollisionEnabled = false;
            }
        }
    }

    public void Reset()
    {
        if (_targetSlot >= 0)
        {
            var ds = DanmakuSystem.Instance;
            if (ds != null) ds.TargetRegistry.Unregister(this);
        }
        _targetSlot = -1;
        _isCollisionEnabled = true;
    }
}
```

**集成约束**：
- TargetRegistry 硬上限 64 个目标（v2.2 从 16 扩容，天命人决策 D-01）。超出后 LogError 提示需扩容。
- Entity vs Entity 的碰撞不走 TargetRegistry（那是弹丸 vs 目标），走独立的 EntityCollisionSolver（Phase 2 实现）。

### 3.6 EntityPool 设计（参考 BulletWorld 模式）

> **v2.2 变更（GD-104）**：构造函数从 `int configId` 改为 `EntityConfigSO config`，Phase 1 以 SO 引用为主键。

```csharp
/// <summary>
/// 按 Entity 配置类型分池。
/// 采用预分配数组 + 空闲槽位栈（参考 BulletWorld），零 GC。
/// Phase 1 以 EntityConfigSO 为键；Phase 2 可选 Luban configId 桥接。
/// </summary>
public class EntityPool
{
    private readonly Entity[] _entities;
    private readonly int[] _freeSlots;
    private int _freeTop;
    private readonly EntityConfigSO _config;

    public int ActiveCount { get; private set; }
    public int Capacity { get; }
    public EntityConfigSO Config => _config;

    public EntityPool(EntityConfigSO config)
    {
        _config = config;
        Capacity = config.PoolMax;
        _entities = new Entity[config.PoolMax];
        _freeSlots = new int[config.PoolMax];

        // 预创建 Entity + 组件（根据 config.Components 决定挂哪些组件）
        for (int i = 0; i < config.PoolMax; i++)
        {
            _entities[i] = CreateEntityFromConfig(config);
            _freeSlots[_freeTop++] = i;
        }
    }

    public Entity Acquire(Vector2 position, float rotation)
    {
        if (_freeTop == 0) { Debug.LogWarning($"[EntityPool] 池满：{_config.name}"); return null; }
        int slot = _freeSlots[--_freeTop];
        var entity = _entities[slot];
        entity.InitAll(position, rotation);
        ActiveCount++;
        return entity;
    }

    public void Release(Entity entity)
    {
        entity.ResetAll();
        _freeSlots[_freeTop++] = entity.PoolSlot;
        ActiveCount--;
    }
}
```

### 3.7 EntityManager（全局驱动器）

> **v2.1 变更（EC-005/EC-013）**：Despawn 改为延迟销毁模式 + swap-remove 优化。
> **v2.2 变更（GD-104/GD-105）**：API 签名从 `int configId` 改为 `EntityConfigSO config`；`_pools` 改为以 SO 引用为 key。
> **v2.6 新增（WF-001/WF-004）**：EntitySystemBootstrap 胶水层 + EntityManagerAccessor 全局访问点。

#### EntityManagerAccessor（全局静态访问点，WF-004）

```csharp
/// <summary>
/// EntityManager 全局访问点（Editor 工具 + 游戏层查询用）。
/// 由 EntitySystemBootstrap.Awake() 注册，OnDestroy() 注销。
/// 非 Singleton 模式——不阻止多实例（测试/分屏场景预留）。
/// </summary>
public static class EntityManagerAccessor
{
    public static EntityManager Instance { get; internal set; }
    public static EntityViewBridge ViewBridge { get; internal set; }
    public static EntitySpawner Spawner { get; internal set; }
}
```

#### EntitySystemBootstrap（胶水层 MonoBehaviour，WF-001）

```csharp
/// <summary>
/// Entity 系统启动器——策划拖到场景根 GO 即可激活整个 Entity 系统。
/// 负责创建 EntityManager / EntityViewBridge / EntitySpawner 实例并每帧驱动。
/// 这是策划工作流的"引擎启动钥匙"。
/// </summary>
public class EntitySystemBootstrap : MonoBehaviour
{
    [Header("调试视觉")]
    [Tooltip("Debug View 的 PoolDefinition（Phase 1 必填）")]
    public PoolDefinition DebugViewPool;

    private EntityManager _entityManager;
    private EntityViewBridge _viewBridge;
    private EntitySpawner _spawner;

    void Awake()
    {
        _entityManager = new EntityManager();
        _viewBridge = new EntityViewBridge(PoolManager.Instance, DebugViewPool);
        _spawner = new EntitySpawner();

        // 注册到全局访问点
        EntityManagerAccessor.Instance = _entityManager;
        EntityManagerAccessor.ViewBridge = _viewBridge;
        EntityManagerAccessor.Spawner = _spawner;

        // 自动发现场景中的 EntitySpawnPoint 并启动
        foreach (var point in FindObjectsOfType<EntitySpawnPoint>())
        {
            if (point.AutoStartOnEnable && point.WaveConfig != null)
                _spawner.StartWave(point);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _entityManager.Tick(dt);
        _spawner.Tick(dt, _entityManager);
        _viewBridge.SyncAll(_entityManager);
    }

    void OnDestroy()
    {
        EntityManagerAccessor.Instance = null;
        EntityManagerAccessor.ViewBridge = null;
        EntityManagerAccessor.Spawner = null;
    }
}
```

**关键决策**：
1. **策划拖一次就行**——场景根 GO 上挂 EntitySystemBootstrap，整个系统自动初始化 + 自动发现 SpawnPoint
2. **不是 Singleton**——通过 Accessor 暴露引用但不阻止多实例，测试场景可多 Bootstrap
3. **Update() 中统一驱动**——时序：EntityManager.Tick() → EntitySpawner.Tick() → ViewBridge.SyncAll()，与 §3.12 时序一致

```csharp
/// <summary>
/// Entity 全局管理器——管理所有 EntityPool，统一驱动 Tick。
/// 非 MonoBehaviour，由游戏层 MonoBehaviour 在 Update 中调用。
/// Phase 1 以 EntityConfigSO 为主键；Phase 2 可增加 Spawn(int configId,...) 重载。
/// </summary>
public class EntityManager
{
    private readonly Dictionary<EntityConfigSO, EntityPool> _pools;  // SO 引用 → pool
    private readonly List<Entity> _activeEntities;        // 活跃 Entity 列表
    private readonly List<Entity> _pendingDespawn;        // v2.1: 延迟销毁队列

    /// <summary>每帧驱动所有活跃 Entity</summary>
    public void Tick(float dt)
    {
        _isTicking = true;
        // Phase A: Tick 所有活跃 Entity
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            var entity = _activeEntities[i];
            // v2.4 预留（GD-R4-011）：Phase 1 IsPaused 永远 false（分支预测零开销）
            // Phase 2 HitStop 启用后，暂停的 Entity 跳过 Tick，逐帧递减 pause 计数
            if (entity.IsPaused) { entity.DecrementPauseFrames(); continue; }
            entity.Tick(dt);
        }

        // Phase B: 统一处理延迟销毁（Tick 期间 Despawn 只标记不执行）
        if (_pendingDespawn.Count > 0)
        {
            for (int i = 0; i < _pendingDespawn.Count; i++)
            {
                ExecuteDespawn(_pendingDespawn[i]);
            }
            _pendingDespawn.Clear();
        }
    }

    /// <summary>从指定配置的池取出 Entity（Phase 1 主 API）</summary>
    public Entity Spawn(EntityConfigSO config, Vector2 position, float rotation)
    {
        var pool = GetOrCreatePool(config);
        var entity = pool.Acquire(position, rotation);
        if (entity != null) _activeEntities.Add(entity);
        return entity;
    }

    // Phase 2 预留：Luban 迁移后增加 int configId 重载
    // public Entity Spawn(int configId, Vector2 position, float rotation) { ... }

    /// <summary>
    /// 回收 Entity（延迟模式：Tick 期间调用只加入待销毁队列，帧尾统一执行）。
    /// Tick 外调用则立即执行。
    /// </summary>
    public void Despawn(Entity entity)
    {
        if (_isTicking)
        {
            entity.MarkPendingDespawn(); // 标记脏位，Tick 中后续组件可检查
            _pendingDespawn.Add(entity);
        }
        else
        {
            ExecuteDespawn(entity);
        }
    }

    /// <summary>实际销毁：swap-remove O(1) + 归还池</summary>
    private void ExecuteDespawn(Entity entity)
    {
        // swap-remove: 将最后一个 Entity 移到被删位置
        int idx = entity.ActiveListIndex;
        int last = _activeEntities.Count - 1;
        if (idx != last)
        {
            _activeEntities[idx] = _activeEntities[last];
            _activeEntities[idx].ActiveListIndex = idx;
        }
        _activeEntities.RemoveAt(last);

        var pool = _pools[entity.ConfigSO];  // 通过 SO 引用找到对应池
        pool.Release(entity);
    }

    private EntityPool GetOrCreatePool(EntityConfigSO config)
    {
        if (!_pools.TryGetValue(config, out var pool))
        {
            pool = new EntityPool(config);
            _pools[config] = pool;
        }
        return pool;
    }

    /// <summary>
    /// 查询指定配置类型的存活 Entity 数量（排除 PendingDespawn）。
    /// v2.3 新增（SA-006）：供 EntitySpawner 的 AllCleared 触发模式使用。
    /// </summary>
    public int CountAliveByConfig(EntityConfigSO config)
    {
        int count = 0;
        for (int i = 0; i < _activeEntities.Count; i++)
        {
            var e = _activeEntities[i];
            if (e.ConfigSO == config && !e.IsPendingDespawn)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Phase 3 预留：按半径搜索指定阵营的 Entity（零 GC，使用预分配结果缓冲区）。
    /// </summary>
    public int FindEntitiesInRadius(
        Vector2 center, float radius, EnumCamp camp,
        Entity[] resultBuffer, int maxResults)
    {
        // Phase 3 实现：线性扫描 _activeEntities，如需优化改为空间分区
        throw new System.NotImplementedException("Phase 3");
    }
}
```

### 3.8 与现有系统的集成矩阵

| 现有系统 | 集成方式 | 优先级 |
|----------|----------|--------|
| **碰撞系统（TargetRegistry）** | CollisionComponent 实现 ICollisionTarget，注册到 TargetRegistry | Phase 1 |
| **事件系统（GameEvent SO）** | 跨 Entity 通信用全局 GameEvent SO；Entity 内部用 EntityEventBus | Phase 1 |
| **对象池（PoolManager）** | Entity 池独立实现（EntityPool）；视觉表现的 GameObject 仍走 PoolManager | Phase 1 |
| **配置驱动** | Phase 1: EntityConfigSO（ScriptableObject）；Phase 2+: 可选迁移 Luban 导表 | Phase 1 SO / Phase 2 Luban |
| **渲染系统（RBM/Atlas）** | Phase 2 考虑——角色 Sprite 渲染可通过 RuntimeAtlas 统一，或走 Spine 独立管线 | Phase 2 |
| **弹幕系统（DanmakuSystem）** | Entity 作为弹幕发射源 + 碰撞目标，通过现有 API 交互 | Phase 1 |

### 3.9 TargetRegistry 槽位约束的应对策略

> **v2.1 变更（EC-002/EC-006/EC-010）**：分阶段策略 + 补充伪代码 + PierceHitMask 风险说明 + 池化安全防护。
> **v2.2 变更（天命人决策 D-01）**：TargetRegistry 从 16 扩容到 64，超出后 LogError 提示需扩容。

**问题**：弹幕游戏同屏可能 50+ 敌兵，原 16 槽位不够用。

**方案（v2.2 更新）**：
- **A 方案：扩容 TargetRegistry 到 64** → **选择此方案**（天命人决策 D-01）
- **B 方案：动态注册/注销** → 保留为 Phase 2+ 应急方案（当 Entity > 64 时）

**Phase 1 策略（扩容模式）**：
- TargetRegistry 硬上限 64 个目标，Phase 1 验收场景充分覆盖
- CollisionComponent.Init → RegisterTarget，Reset → UnregisterTarget，无需动态策略
- **Phase 1 硬约束**：如果注册失败（返回 -1），LogError 提示需扩容并将该 Entity 标记为"碰撞不可用"

**Phase 2+ 策略（动态模式，仅在 Entity > 64 时启用）**：
```
CollisionRegistrationPass 伪代码：
1. 计算弹幕活跃区域 = WorldBounds 缩小 10%（或 BulletWorld 中活跃弹丸的包围盒）
2. 对所有持有 CollisionComponent 的活跃 Entity，按以下规则排序：
   - 权重 = (1.0 - 距离/活跃区域半径) × 0.7 + (1.0 - 当前HP/最大HP) × 0.3
   - 权重越高优先注册
3. 取前 63 个注册（保留 1 槽给玩家）
4. 防抖：已注册的 Entity 有 MIN_REGISTERED_FRAMES = 10 帧的最小保持期
   - 未到保持期的 Entity 不被踢出，即使权重低于排队者
5. 注销时清除对应弹丸的 PierceHitMask（见下方风险说明）
```

**PierceHitMask 位宽升级（SA-001，v2.3）**：
- **问题**：TargetRegistry 扩容到 64 后，原 `BulletCore.PierceHitMask`（`ushort`，16 位）只能覆盖 0~15 号槽位。16+ 号槽位的穿透记录溢出，导致同一弹丸对同一目标每帧重复伤害。
- **方案**：`PierceHitMask` 从 `ushort` → `ulong`（64 位），`CollisionSolver` 位操作从 `(ushort)(1 << t)` → `(1UL << t)`
- **权衡**：BulletCore 结构体 48 → 56 bytes（+8 bytes，ulong 对齐），2048 弹丸 × 56 = 112KB，仍在 L2 缓存友好范围（典型 256KB~1MB L2）。接受此开销。
- **影响范围**：BulletCore struct 定义、CollisionSolver.SolveBulletVsTarget()、所有使用 PierceHitMask 的位运算

**PierceHitMask 动态注册/注销冲突风险（EC-006）**：
- 动态注册/注销会导致同一 TargetRegistry 槽位被不同 Entity 复用
- `BulletCore.PierceHitMask`（ulong，按槽位 bit 标记）会误判：旧 Entity 的命中记录被新 Entity 继承
- **缓解方案**：注销 Entity 时，遍历 BulletWorld 活跃弹丸，清除对应槽位 bit（O(n) 但只在注销时执行）
- **替代方案（Phase 2 评估）**：改 PierceHitMask 为 EntityId 数组（每弹丸最多穿透 N 个目标）

**池化安全防护（EC-010/EC-017）**：
- CollisionComponent.Reset() 时，若 `_targetSlot < 0` 或 DanmakuSystem.Instance == null（场景切换时），静默跳过注销
- **注意**：DanmakuSystem.ClearAll() **不会**清除 TargetRegistry（代码注释明确标注"目标生命周期由外部管理"）。CollisionComponent.Reset() 主动注销是唯一清理路径
- 场景切换时，EntityManager 应遍历所有池化 Entity 执行 Reset（确保每个 CollisionComponent 注销自身）
- 增加防护：CollisionComponent 实现 `bool IsAlive => _owner != null && !_owner.IsPendingDespawn`
- CollisionSolver 遍历 TargetRegistry 时检查 target 的有效性（现有代码已有 null 检查，`IsAlive` 是额外保障层）

### 3.10 渲染架构预留（Phase 2）

> 无 v2.1 变更。

角色渲染不走弹幕的 RenderBatchManager 管线（那是 instanced quad 渲染，针对大量同质粒子优化的）。

**Phase 2 选项**：
- **A 方案：Spine + 独立 SpriteBatcher** —— 角色用 Spine 骨骼动画，走 Spine-Unity 渲染管线
- **B 方案：序列帧 + RuntimeAtlas** —— 角色帧动画纹理注册到 RuntimeAtlas，走 instanced quad 渲染
- **当前决策**：Phase 1 先不做渲染集成，Entity 纯逻辑层。渲染表现由游戏层自行桥接。

### 3.11 阵营设计（EC-008 + D-02）

> **v2.1 新增**。
> **v2.2 变更（天命人决策 D-02）**：BulletFaction → EnumCamp 统一，Phase 1 顺手做。

Phase 1 统一使用 `EnumCamp` 枚举（替代原 BulletFaction），在 Phase 1 中完成枚举重命名和全项目替换：
- 小游戏场景 3 个阵营（Enemy/Player/Neutral）足够覆盖 PvE/PvP 基本需求
- Entity 层和弹幕层共用同一枚举，避免映射开销

**扩展触发条件**：当需要 4+ 阵营（如 Team1/Team2/...）时：
1. 扩展 `EnumCamp` 枚举
2. 如果碰撞系统需要更复杂的阵营关系矩阵，引入 `CampRelation` 配置表
3. CollisionSolver 的 ShouldCollide 逻辑从硬编码改为查表

### 3.12 Tick 时序与碰撞延迟说明（EC-009）

> **v2.1 新增**。

**帧内执行顺序**：
```
DanmakuSystem.Update()           ← 弹幕运动 + 碰撞检测（使用 Entity 上一帧位置）
EntityManager.Tick()              ← Phase A: Entity 组件更新（MovementComponent 更新位置）
                                  ← Phase B: 延迟销毁统一执行
EntitySpawner.Tick()              ← 波次推进（AllCleared 判定在延迟销毁后，SA-006 v2.3）
EntityViewBridge.SyncAll()        ← 视觉层位置同步
DanmakuSystem.LateUpdate()       ← 渲染上传
```

**已知限制**：Entity 位置更新在碰撞检测之后，导致碰撞使用上一帧位置（1 帧延迟）。

**影响评估**：
- 30fps 下，1 帧 = 33ms，Entity 移速 5 单位/秒 → 偏移 0.17 单位（~2 像素），不可感知
- 即使 60fps + 冲刺（20 单位/秒），偏移 0.33 单位（~4 像素），仍在碰撞体半径容忍范围内
- **结论**：小游戏场景可接受，不做预测补偿

### 3.13 内存预算估算（EC-011）

> **v2.1 新增**。
> **v2.4 变更（GD-R4-001/003）**：Entity 本体 +4 bytes（PauseFrames），BulletCore +4 bytes（OwnerEntityId）。

单个 Entity 内存估算：
| 组成部分 | 估算大小 |
|----------|----------|
| Entity 本体（ID, Faction, Position, Config ref, 组件数组 16 槽, PauseFrames） | ~132 bytes |
| EntityEventBus（Delegate[16,4] + int[16]） | ~320 bytes |
| 9 个组件（含 AttackComponent，平均每个 ~48 bytes） | ~432 bytes |
| **合计** | **~884 bytes / Entity** |

弹幕系统影响（GD-R4-001）：
- BulletCore 新增 `uint OwnerEntityId`（+4 bytes，56→60 bytes）
- 2048 弹丸 × 60 = ~120 KB，仍在 L2 缓存友好范围

预算场景：
- 10 种配置 × 每种 poolMax=20 = 200 个 Entity = **~163 KB**
- 20 种配置 × 每种 poolMax=20 = 400 个 Entity = **~325 KB**
- **目标上限**：EntitySystem 总内存 < 2MB（含所有池 + Manager 开销）

### 3.14 刷怪系统设计（GD-003/GD-102）

> **v2.2 新增（PK R1 + R2 产物）**。
> **v2.4 变更（GD-R4-005）**：WaveTriggerMode 新增 OnCallback；EntitySpawnWaveSO 新增 Loop/LoopStartWave；SpawnGroup 新增 Formation 枚举。

```csharp
/// <summary>
/// 刷怪波次配置资产。策划在 Inspector 中编排关卡波次。
/// 路径：Assets/_Game/Configs/SpawnWave/
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnWave", menuName = "Entity/SpawnWaveConfig")]
public class EntitySpawnWaveSO : ScriptableObject
{
    public SpawnWaveEntry[] Waves;

    [Header("循环模式（v2.4 新增，GD-R4-005）")]
    [Tooltip("是否在最后一波结束后从 LoopStartWave 重新开始（无限模式）")]
    public bool Loop = false;
    [Tooltip("循环起始波次索引（0-based）")]
    public int LoopStartWave = 0;
}

[System.Serializable]
public struct SpawnWaveEntry
{
    [Tooltip("本波包含的怪物组（支持单波多怪种）")]
    public SpawnGroup[] Groups;

    [Tooltip("触发模式")]
    public WaveTriggerMode TriggerMode;

    [Tooltip("Timer 模式：上一波结束后的延迟秒数")]
    public float TriggerDelay;

    // Phase 2 预留（GD-R4-005，注释掉）：
    // [Tooltip("难度缩放——本波怪物 HP 乘数")]
    // public float HpMultiplier = 1f;
    // [Tooltip("难度缩放——本波怪物数量乘数")]
    // public float CountMultiplier = 1f;
}

[System.Serializable]
public struct SpawnGroup
{
    public EntityConfigSO EntityConfig;     // 怪种配置
    public EnumCamp Camp;                   // 阵营
    public int Count;                       // 数量
    public float SpawnInterval;             // 组内逐个生成间隔

    [Tooltip("生成阵型（v2.4 新增，GD-R4-005）。Phase 1 只实现 Random")]
    public SpawnFormation Formation;        // 阵型
}

/// <summary>
/// 生成阵型枚举（v2.4 新增）。
/// Phase 1 只实现 Random；Line/Circle Phase 2 实现。
/// </summary>
public enum SpawnFormation
{
    Random = 0,     // AreaRadius 内随机散布（Phase 1 默认）
    Line = 1,       // Phase 2：沿指定方向排一列
    Circle = 2,     // Phase 2：围成一圈
}

public enum WaveTriggerMode
{
    Timer = 0,          // 上一波结束后延迟 N 秒
    AllCleared = 1,     // 上一波全灭后触发
    OnCallback = 2,     // v2.4 新增（GD-R4-005）：波次完成后触发事件，等待游戏层调用 Spawner.ContinueNextWave() 才推进
    // OnEnterArea = 3, // Phase 2+ 实现（需要触发区域组件）
}
```

**场景组件**：

```csharp
/// <summary>
/// 放置在场景中的刷怪点。策划通过 Inspector 配置波次 SO 和生成范围。
/// Editor 模式下绘制 Gizmo 可视化生成区域。
/// v2.5 变更（ET-009）：改为 Always 绘制 + Label，多刷怪点场景一目了然。
/// </summary>
public class EntitySpawnPoint : MonoBehaviour
{
    [Header("波次配置")]
    public EntitySpawnWaveSO WaveConfig;    // 引用波次 SO
    public bool AutoStartOnEnable = true;  // 场景加载后自动开始

    [Header("生成区域")]
    public float AreaRadius = 2f;          // 随机散布半径

    // v2.5（ET-009）：始终绘制半透明圆圈 + 名称标签
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // 半透明黄色
        Gizmos.DrawWireSphere(transform.position, AreaRadius);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, gameObject.name);
        #endif
    }

    // v2.5（ET-009）：选中时高亮显示 + 完整波次信息
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AreaRadius);
        #if UNITY_EDITOR
        if (WaveConfig != null)
        {
            int totalWaves = WaveConfig.Waves?.Length ?? 0;
            int totalMonsters = 0;
            string firstEnemy = "N/A";
            if (totalWaves > 0 && WaveConfig.Waves[0].Groups?.Length > 0)
            {
                firstEnemy = WaveConfig.Waves[0].Groups[0].EntityConfig?.DisplayName ?? "?";
                foreach (var wave in WaveConfig.Waves)
                    if (wave.Groups != null)
                        foreach (var g in wave.Groups)
                            totalMonsters += g.Count;
            }
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (AreaRadius + 0.3f),
                $"{gameObject.name}\n{totalWaves} 波 | {totalMonsters} 怪 | 首波: {firstEnemy}");
        }
        #endif
    }
}

/// <summary>
/// 刷怪驱动器——管理 EntitySpawnPoint 的波次推进逻辑。
/// 由游戏层 MonoBehaviour 持有并在 Update 中驱动。
/// </summary>
public class EntitySpawner
{
    public void StartWave(EntitySpawnPoint point) { /* ... */ }
    public void Tick(float dt, EntityManager entityManager) { /* ... */ }
    public bool IsAllWavesCleared { get; }
}
```

**Phase 1 调用时序（SA-006，v2.3 明确）**：
```
游戏层 MonoBehaviour.Update():
    EntityManager.Tick(dt)       ← Phase A: Tick 所有活跃 Entity
                                  ← Phase B: 统一处理延迟销毁（_pendingDespawn）
    EntitySpawner.Tick(dt, mgr)  ← Phase B 之后调用，确保 AllCleared 判定时
                                    已销毁的 Entity 不再被计为活跃
```
- **AllCleared 判定**：调用 `EntityManager.CountAliveByConfig(config)` 查询存活数，排除 `IsPendingDespawn` 的 Entity
- **时序保证**：Spawner 在 EntityManager.Tick() 之后运行，Phase B 延迟销毁已执行完毕，避免 1 帧延迟误触发下一波

**Phase 1 实现范围**：Timer + AllCleared + OnCallback 三种模式 + Loop 循环。OnEnterArea 需要额外 TriggerZone 组件，Phase 2 再做。生成阵型 Phase 1 只实现 Random（AreaRadius 内随机散布），Line/Circle Phase 2+。难度缩放（HpMultiplier/CountMultiplier）Phase 2。

### 3.15 EntityViewBridge 设计（GD-103）

> **v2.2 新增（PK R2 产物）**。
> **v2.3 变更（SA-005）**：内部存储从 `Dictionary<uint, GameObject>` 改为预分配数组，SyncAll() 零 GC 遍历。

```csharp
/// <summary>
/// Entity 逻辑层与视觉层的桥接器。
/// 持有 EntityId → View GO 映射，Entity 本身不持有 GO 引用（BC-01.1 不变）。
/// Phase 1: 使用内置 Debug Prefab（彩色圆 + HP 文本）
/// Phase 2: 使用 EntityConfigSO.ViewPrefab（策划指定的正式 Prefab）
/// 
/// v2.3 变更（SA-005）：内部存储从 Dictionary 改为预分配数组。
/// 原因：Mono 运行时 Dictionary.GetEnumerator() 每次 foreach 产生 ~40 bytes GC Alloc（装箱），
/// 违反零 GC 承诺。改为平铺数组 + for 循环遍历，彻底消除 GC。
/// </summary>
public class EntityViewBridge
{
    private const int MAX_VIEWS = 256; // 预分配上限（远超 Phase 1 需求，可调）

    // 预分配数组——零 GC 遍历
    private readonly GameObject[] _viewGOs = new GameObject[MAX_VIEWS];
    private readonly uint[] _viewEntityIds = new uint[MAX_VIEWS];
    private readonly EntityConfigSO[] _viewConfigs = new EntityConfigSO[MAX_VIEWS]; // 回收时查池用
    private int _activeCount;

    private readonly PoolManager _poolManager;
    private PoolDefinition _debugViewPool;  // Phase 1 内置 Debug Prefab 的池

    /// <summary>Entity 生成时调用——创建/获取对应的 View GO</summary>
    public void OnEntitySpawned(Entity entity, EntityConfigSO config)
    {
        if (_activeCount >= MAX_VIEWS) { Debug.LogWarning("[ViewBridge] 视图数量超限"); return; }

        PoolDefinition pool = config.ViewPrefab != null
            ? config.ViewPoolDef   // Phase 2: 正式 View
            : _debugViewPool;      // Phase 1: Debug View

        var go = _poolManager.Get(pool);
        go.transform.position = entity.Position;

        // append 到数组尾部
        int idx = _activeCount++;
        _viewGOs[idx] = go;
        _viewEntityIds[idx] = entity.Id.Value;
        _viewConfigs[idx] = config;

        // Phase 1: 设置 Debug 颜色
        if (config.ViewPrefab == null)
        {
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = config.DebugColor;
        }
    }

    /// <summary>每帧同步位置/朝向/HP 显示——零 GC for 循环遍历</summary>
    public void SyncAll(EntityManager manager)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            // 从 EntityManager 查 Entity 位置，同步到 View GO transform
            // Phase 1 Debug View: 更新 HP 文本
        }
    }

    /// <summary>Entity 回收时调用——归还 View GO 到池（swap-remove O(1)）</summary>
    public void OnEntityDespawned(Entity entity, EntityConfigSO config)
    {
        uint targetId = entity.Id.Value;
        for (int i = 0; i < _activeCount; i++)
        {
            if (_viewEntityIds[i] == targetId)
            {
                // 归还 GO 到池
                PoolDefinition pool = config.ViewPrefab != null
                    ? config.ViewPoolDef
                    : _debugViewPool;
                _poolManager.Return(pool, _viewGOs[i]);

                // swap-remove
                int last = _activeCount - 1;
                if (i != last)
                {
                    _viewGOs[i] = _viewGOs[last];
                    _viewEntityIds[i] = _viewEntityIds[last];
                    _viewConfigs[i] = _viewConfigs[last];
                }
                _viewGOs[last] = null;
                _viewConfigs[last] = null;
                _activeCount--;
                return;
            }
        }
    }
}
```

**关键决策**：
1. **Debug View Prefab** 是项目内置资产（一个带 SpriteRenderer + TextMesh 的最简 Prefab），通过 PoolDefinition 走 PoolManager 池化——零运行时 GC
2. **EntityViewBridge 是独立管理器**，不在 Entity 内部——BC-01.1"Entity 不持有 GO"不变
3. **Phase 2 自动切换**：策划在 EntityConfigSO 上填 ViewPrefab → EntityViewBridge 自动使用对应 PoolDefinition → 无需改代码
4. **EntityViewBridge 由游戏层 MonoBehaviour 持有并驱动**（和 EntityManager 同级）
5. **v2.3 零 GC 保证**（SA-005）：内部存储用预分配数组 + for 循环遍历，Despawn 用 swap-remove O(1)。避免 Dictionary 遍历的 Enumerator 装箱 GC

**事件钩子说明（v2.4 新增，GD-R4-004/012）**：

EntityViewBridge **只负责位置/朝向同步**。以下表现由游戏层订阅 EntityEventBus 事件自行处理：

| 表现 | 事件源 | 游戏层处理方式 |
|------|--------|--------------|
| 受击闪白 | `OnCollisionHit` | ViewBridge.SyncAll 中检查闪白状态，设置材质属性 |
| 击退位移 | `MovementComponent.Knockback` | 自动生效（位置变化 → SyncAll 同步） |
| 伤害数字 | `OnCollisionHit` | 游戏层订阅事件 → 调用 `DamageNumberSystem.Show(pos, damage)` |
| 音效 | `OnCollisionHit` / `OnDeath` | 游戏层订阅事件 → 播放对应 AudioClip |
| 生成特效 | Entity Spawn | ViewBridge.OnEntitySpawned 中播放 `config.SpawnEffect` |
| 受击特效 | `OnCollisionHit` | 游戏层订阅事件 → 从 `config.HitEffect` PoolManager.Get() |
| 死亡特效 | `OnDeath` | 游戏层订阅事件 → 从 `config.DeathEffect` PoolManager.Get() |

**核心原则**：Entity 框架负责发事件，**不负责做表现**。框架确保事件携带足够信息（DamageContext），表现层的策划友好性由游戏层保证。

---

## 四、组件详细设计（v2.1 修订版）

### 4.1 StateComponent

**BC 引用**：BC-01.4, BC-02

**v2.1 变更（EC-014）**：
- 状态标签集合封装为 `StateMask` 值类型（内部 uint64，对外不暴露原始位操作）
- 未来如需 > 64 种状态，可将 `StateMask` 内部改为 uint64[] 而不影响外部接口
- 互斥规则表从配置读取（Phase 1: 硬编码或 SO；Phase 2: Luban），启动时预计算互斥掩码矩阵 `uint64[64]`（O(1) 检查）
- 状态变化通过 EntityEventBus 发布 `OnStateChanged`

### 4.2 HealthComponent

**BC 引用**：BC-02, BC-03

**v2.0 变更**：
- 受伤流程中的"来源信息"改为 `EntityId`（而非模糊的"来源"）
- 通过 EntityEventBus 发布 `OnDamaged` / `OnDeath`，不直接操作 StateComponent

### 4.3 AnimationComponent

**BC 引用**：BC-02.2（Tickable）

**v2.0 重要变更**：
- **不在 Phase 1 实现视觉渲染**。Phase 1 的 AnimationComponent 只管"动画状态管理"（当前状态→动画 ID 映射），不直接操作 Spine/SpriteRenderer
- 提供 `CurrentAnimId` 只读属性，由游戏层的 View 组件读取并驱动实际渲染
- 这样 Entity 层保持纯逻辑，渲染表现完全解耦

### 4.4 MovementComponent

**BC 引用**：BC-02.2（Tickable）

**v2.0 变更**：无实质变更。
- 速度修正器改用固定数组预分配（最多 4 个 Modifier），避免 List 扩容 GC

**v2.4 新增（GD-R4-004）**：击退（Knockback）支持
```csharp
/// <summary>
/// 施加击退效果。被调用后在 duration 时间内沿 direction 位移 distance 距离。
/// 击退期间正常移速叠加（击退是额外位移，不替代原始运动）。
/// </summary>
public void ApplyKnockback(Vector2 direction, float distance, float duration)
{
    _knockbackDir = direction.normalized;
    _knockbackSpeed = distance / duration;
    _knockbackRemaining = duration;
}
```
- 从 `EntityConfigSO.KnockbackDistance` 读取默认击退距离
- `HealthComponent` 收到 `OnCollisionHit` 后调用 `MovementComponent.ApplyKnockback()`
- 击退曲线（AnimationCurve）Phase 2 扩展

### 4.5 CollisionComponent

**BC 引用**：BC-05

**v2.0 重大变更（vs v1.0）**：
- 实现 `ICollisionTarget` 接口，直接桥接现有弹幕碰撞系统
- 使用 `CircleHitbox`（而非 OBB）作为角色碰撞体——与弹幕系统一致
- OBB 碰撞体（Entity vs 障碍物）通过 `ObstaclePool.AddRect()` 注册，Entity vs 弹幕走 `TargetRegistry`
- 动态注册策略：不是所有 Entity 都常驻 TargetRegistry

### 4.6 AutoAimComponent

**BC 引用**：BC-02.2（Tickable，定频）

**v2.0 变更**：
- 搜索范围用 EntityManager 提供的 `FindEntitiesInRadius()` API，而非碰撞系统
- 阵营过滤复用 `EnumCamp` 枚举的 `ShouldCollide()` 逻辑

### 4.7 ControlComponent / AIComponent

**BC 引用**：BC-07

**v2.0 变更**：无实质变更，设计保持。

**v2.4 新增（GD-R4-002/010）**：AIBehaviorSO 配置资产化 + IAIAction 有状态 Action 接口。

#### AIBehaviorSO（条件-动作表配置资产）

```csharp
/// <summary>
/// AI 行为配置资产。策划在 Inspector 中按优先级配置条件-动作表。
/// 路径：Assets/_Game/Configs/AI/
/// </summary>
[CreateAssetMenu(fileName = "NewAIBehavior", menuName = "Entity/AIBehavior")]
public class AIBehaviorSO : ScriptableObject
{
    [Tooltip("按优先级排列的条件-动作表（索引越小优先级越高）")]
    public AIBehaviorEntry[] Entries;
}

[System.Serializable]
public struct AIBehaviorEntry
{
    public AIConditionType Condition;    // 枚举：Always/HpBelow/TargetInRange/TargetLost/...
    public float ConditionParam;         // 条件参数（如距离阈值、HP 百分比）
    public AIActionType Action;          // 枚举：Idle/MoveToTarget/Attack/Flee/Patrol/...
    public float ActionParam;            // 动作参数（如巡逻半径、逃跑距离）
}

public enum AIConditionType : byte
{
    Always = 0,             // 无条件匹配（兜底）
    HpBelow = 1,            // HP 百分比低于 ConditionParam（0.0~1.0）
    TargetInRange = 2,      // 目标在 ConditionParam 距离内
    TargetLost = 3,         // 无目标 / 目标超出检测范围
    // Phase 2 扩展：HpAbove, AllyCountBelow, WaveIndex, ...
}

public enum AIActionType : byte
{
    Idle = 0,
    MoveToTarget = 1,
    Attack = 2,
    Flee = 3,
    Patrol = 4,
    // Phase 2 扩展：Guard, Retreat, ...
}
```

#### IAIAction 有状态执行器接口

> **v2.4 新增（GD-R4-010）**：条件-动作表决定"什么时候做什么"；IAIAction 决定"怎么做"。
> Action 执行器内部维护多帧状态（如 Patrol 的目标巡逻点、等待计时）。

```csharp
/// <summary>
/// AI Action 执行器接口——支持多帧有状态执行。
/// 每帧由 AIComponent 调用 Execute()，Action 内部维护自身状态。
/// </summary>
public interface IAIAction
{
    void Enter(Entity owner);                           // 进入此 Action 时调用
    DecisionCommand Execute(Entity owner, float dt);    // 每帧执行，返回移动/攻击指令
    void Exit(Entity owner);                            // 退出此 Action 时调用
}
```

**AIComponent 执行流程**：
1. 每帧评估 `AIBehaviorSO.Entries`（按优先级从高到低）→ 匹配第一个满足条件的 Entry → 得到 `AIActionType`
2. **v2.6 安全网（WF-005）**：如果所有条件均未匹配 → **默认执行 IdleAction**（硬编码 fallback，不需要策划配置）
   ```csharp
   // 安全网：所有条件均未匹配时默认 Idle。
   // 建议策划在行为表末尾配置 Always→Idle。
   if (matchedAction == null) matchedAction = _fallbackIdleAction;
   ```
3. 如果 `AIActionType` 与上一帧不同 → 调用旧 Action.Exit() + 新 Action.Enter()
4. 调用当前 Action.Execute(owner, dt) → 得到 `DecisionCommand`

**Phase 1 内置 Action 列表**：

| Action | 说明 | 有状态？ |
|--------|------|---------|
| IdleAction | 原地不动 | 否 |
| MoveToTargetAction | 朝当前锁定目标移动 | 否 |
| PatrolAction | 随机选巡逻点→移向→到达后等待→再选新点 | ✅ 是 |
| AttackAction | 触发 AttackComponent 的攻击逻辑 | 否 |
| FleeAction | 朝远离目标方向移动 | 否 |

**策划视角**：策划只配 AIBehaviorSO（在 Inspector 中拖拽条件-动作），程序负责 IAIAction 实现。

### 4.8 SkillComponent

**v2.0 变更**：技能槽改用固定长度数组（最多 4 个槽位），避免 List GC。

### 4.9 AttackComponent（v2.4 新增，GD-R4-003/009）

> Phase 1 最小攻击组件——定时发射弹幕。Phase 3 SkillComponent 上线后，此组件可作为"默认普攻"保留或被替代。

**BC 引用**：BC-02.2（Tickable）

```csharp
/// <summary>
/// Phase 1 最小攻击组件——定时发射弹幕。
/// 复用 ComponentType.Skill 槽位（Phase 3 SkillComponent 可替代）。
/// </summary>
public class AttackComponent : IEntityComponent, ITickable
{
    public ComponentType Type => ComponentType.Skill;  // 复用 Skill 槽位
    public int TickOrder => TickOrders.Decision + 50;  // Decision 之后、AutoAim 之前

    private Entity _owner;
    private float _attackInterval;
    private float _timer;
    private BulletTypeSO _bulletType;   // v2.6 修正（WF-003）：VFXTypeSO → BulletTypeSO
    private Vector2 _fireOffset;

    public void Init(Entity owner)
    {
        _owner = owner;
        _attackInterval = owner.ConfigSO.AttackInterval;
        _bulletType = owner.ConfigSO.AttackBulletType;
        _fireOffset = owner.ConfigSO.AttackFireOffset;
        _timer = 0f;
    }

    public void Tick(float dt)
    {
        if (_bulletType == null) return; // 未配置攻击弹幕 → 不攻击
        _timer += dt;
        if (_timer >= _attackInterval)
        {
            _timer -= _attackInterval;
            var pos = _owner.Position + _fireOffset;
            // 方向：优先用 AutoAim 锁定目标方向，否则用当前朝向
            var dir = GetAimDirection();
            DanmakuSystem.Instance.Fire(_bulletType, pos, dir, _owner.Id.Value);
        }
    }

    public void Reset() { _timer = 0f; }
}
```

**近战攻击说明（GD-R4-009）**：Phase 1 所有攻击统一走弹幕系统。**近战 = 射程极短的瞬发弹幕**（TypeSO 参数：射程≈0.5、速度=0、存活时间≈0.1s）。好处是整个伤害管线统一，策划只需配弹幕参数。Phase 2 可选路径 B：AttackComponent 直接调用目标 HealthComponent.TakeDamage()（需要 EntityManager.FindEntitiesInRadius()）。

---

## 五、配置设计

> **v2.2 变更（GD-101/104/105）**：Phase 1 使用 EntityConfigSO（ScriptableObject），Luban 配置表降级为 Phase 2+ 预留。

### 5.0 EntityConfigSO（Phase 1 当前用）

> **PK R2 产物**：策划通过 SO Inspector 创建/编辑角色配置，无需 Luban 工具链。

```csharp
/// <summary>
/// Entity 角色配置资产。Phase 1 核心配置入口。
/// 策划在 Inspector 中创建和编辑，路径：Assets/_Game/Configs/Entity/
/// </summary>
[CreateAssetMenu(fileName = "NewEntityConfig", menuName = "Entity/EntityConfig")]
public class EntityConfigSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("全局唯一配置 ID。Phase 1 可不填（用 SO 引用），Phase 2 迁移 Luban 时必填。")]
    public int ConfigId;
    public string DisplayName;                            // 调试/UI 显示名
    public EnumCamp Camp;                                 // 阵营（统一使用 EnumCamp）

    [Header("组件列表")]
    [Tooltip("该 Entity 挂载的组件类型。EntityPool 根据此列表预创建组件。")]
    public ComponentType[] Components;                    // 枚举数组
    // ── v2.5 填写说明（ET-002）──
    // Inspector 中通过 EntityConfigSOEditor 渲染为 Checkbox Grid（非裸数组）。
    // 关键规则：
    //   - 勾选 "Skill" = 启用 AttackComponent（Phase 1 复用 Skill 槽位）
    //     Inspector 标签显示为 "☑ Skill (Attack)"
    //   - Control / AI 互斥：只能选其一，另一个自动灰化
    //   - 依赖建议（Warning）：AI→建议搭配 Movement；Collision→建议搭配 Health
    // Phase 3 SkillComponent 上线后标签改为 "Skill (Attack | Skill)"

    [Header("属性")]
    public int MaxHp = 100;
    public float MoveSpeed = 3f;
    public float TurnSpeed = 360f;
    public float CollisionRadius = 0.5f;

    [Header("攻击（v2.4 新增，GD-R4-003 / v2.6 修正 WF-003）")]
    public float AttackInterval = 1f;                     // 攻击间隔（秒），0 = 不攻击
    public BulletTypeSO AttackBulletType;                 // v2.6 修正（WF-003）：发射的弹幕类型（null = 不攻击）
    public Vector2 AttackFireOffset;                      // 发射点偏移（相对 Entity 位置）

    [Header("AI 行为（v2.4 新增，GD-R4-002）")]
    public AIBehaviorSO AIBehavior;                       // AI 条件-动作表配置（null = 无 AI）

    [Header("受击反馈")]
    public float HitFlashDuration = 0.1f;                 // 受击闪白持续时间
    public Color HitFlashColor = Color.white;             // 受击闪白颜色
    public float KnockbackDistance = 0.5f;                // v2.4：击退距离（GD-R4-004）
    public float KnockbackDuration = 0.15f;               // v2.4：击退持续时间
    public bool ShowDamageNumber = true;                  // v2.4：是否显示伤害数字（GD-R4-008）

    [Header("视觉特效")]
    public PoolDefinition SpawnEffect;                    // v2.4：生成特效（走 PoolManager，可选）（GD-R4-008）
    public PoolDefinition HitEffect;                      // v2.4：受击特效（走 PoolManager，可选）（GD-R4-008）
    public PoolDefinition DeathEffect;                    // 死亡特效（走 PoolManager，可选）
    public float DeathDelay = 0.3f;                       // 死亡延迟（播完表现再回收）

    [Header("受击反馈（Phase 2+ 扩展，暂不实现）")]
    // public int HitStopFrames;                          // v2.4 预留（GD-R4-011）：顿帧
    // public int IFrameCount;                            // 无敌帧
    // public AnimationCurve KnockbackCurve;              // 击退曲线

    [Header("视觉表现")]
    public Color DebugColor = Color.red;                  // Phase 1 Debug View 的颜色
    public GameObject ViewPrefab;                         // Phase 2+: 正式视觉 Prefab（null 时用 DebugView）
    public PoolDefinition ViewPoolDef;                    // Phase 2+: 正式 View 的 PoolDefinition

    [Header("对象池")]
    public int PoolInitial = 5;                           // 预热容量
    public int PoolMax = 20;                              // 硬上限
}
```

**关键设计决策（GD-104）**：
- Phase 1 API 以 `EntityConfigSO` 引用为主键（`EntityManager._pools` 的 Dictionary key）
- `ConfigId` 字段为 Phase 2 Luban 迁移预留——策划填上与 Luban TbEntityConfig.id 相同的值即可无缝切换
- Phase 2 迁移路径：EntityManager 增加 `Spawn(int configId, ...)` 重载，内部查 Luban 表；旧 `Spawn(EntityConfigSO, ...)` 保留

### 5.1~5.4 Luban 配置表设计（Phase 2+ 预留）

> 以下 Luban 表结构仅作为 Phase 2 迁移参考，Phase 1 不实现。

<details>
<summary>点击展开 Phase 2 Luban 表结构</summary>

#### 5.1 TbEntityConfig（角色配置表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 配置 ID（与 EntityConfigSO.ConfigId 对应） |
| name | string | 角色名（调试用） |
| faction | int | 阵营（0=Enemy, 1=Player, 2=Neutral） |
| maxHp | int | 最大生命值 |
| moveSpeed | float | 基础移速 |
| turnSpeed | float | 转向速度 |
| collisionRadius | float | 碰撞半径 |
| components | list\<int\> | 挂载的组件类型 ID 列表 |
| poolInitial | int | 对象池初始容量 |
| poolMax | int | 对象池最大容量 |

#### 5.2 TbStateConfig（状态配置表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 状态标签 ID（对应 BitFlags 的 bit 位） |
| name | string | 状态名 |
| exclusions | list\<int\> | 互斥状态 ID 列表 |

#### 5.3 TbAIBehavior（AI 行为表）

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int | 行为 ID |
| entityConfigId | int | 关联的角色配置 |
| priority | int | 优先级（数字越小越先检查） |
| conditionType | int | 条件类型枚举 |
| conditionParam | float | 条件参数（如血量阈值 0.2 = 20%） |
| actionType | int | 动作类型枚举 |
| actionParam | float | 动作参数 |

#### 5.4 TbAnimMapping（动画映射表）

| 字段 | 类型 | 说明 |
|------|------|------|
| entityConfigId | int | 关联的角色配置 |
| stateId | int | 状态标签 ID |
| animId | string | 动画片段 ID |
| priority | int | 同时多状态时的优先级 |

</details>

---

## 六、实施计划

### Phase 1：核心层 + 碰撞集成（预估 5 天）

> **v2.1 变更（EC-015）**：每步补充功能性 AC，"编译通过"不再作为唯一验收标准。

| 步骤 | 内容 | AC（全部需满足） |
|------|------|-----------------|
| P1.0 | ~~阵营枚举统一迁移（BulletFaction → EnumCamp）~~ ✅ 2026-04-30 | ✅ 全项目零 `BulletFaction` 引用 + 编译通过 + DanmakuDemo 行为不变（SA-002，v2.3 新增） |
| P1.1 | IEntityComponent / ITickable / Entity 容器 | ✅ 编译通过 + GetComponent(ComponentType) O(1) 返回正确组件 + GetComponent 未注册类型返回 null |
| P1.2 | EntityEventBus（零 GC 泛型事件总线） | ✅ 编译通过 + Publish→Subscribe 正确分发 + ClearAll 后无残留 + Profiler 验证 100 次 Pub/Sub 周期 GC = 0 |
| P1.3 | EntityPool + EntityManager | ✅ 编译通过 + Profiler 验证 50 次 Acquire+Release 周期 GC = 0 + 池满 LogWarning 不崩 + 延迟销毁在 Tick 中不崩 |
| P1.4 | StateComponent + HealthComponent | ✅ 编译通过 + 互斥状态冲突时正确阻止 + OnDamaged 事件携带正确来源 + HP=0 触发 OnDeath |
| P1.5 | CollisionComponent（ICollisionTarget 桥接） | ✅ 编译通过 + DanmakuDemo 中弹丸命中 Entity 触发 OnCollisionHit + 注册/注销不泄漏槽位 |
| P1.6 | MovementComponent + AnimationComponent（纯逻辑） | ✅ 编译通过 + Entity 位置按速度更新 + CurrentAnimId 随状态切换 |
| P1.7 | ControlComponent + AIComponent + AttackComponent | ✅ 编译通过 + 同 Entity 互斥挂载校验 + AI 条件-动作表（AIBehaviorSO）驱动行为切换 + IAIAction 有状态 Action（Patrol 多帧上下文保持）+ AttackComponent 定时发射弹幕 (**v2.4 扩展**) |
| P1.8 | EntityConfigSO 配置驱动验证 + Editor 工具 | ✅ 从 EntityConfigSO 创建完整 Entity（含正确组件列表）+ Inspector 可编辑所有 Phase 1 字段（含 AIBehavior/Attack/Effect 新增字段）+ **EntityConfigSOEditor 条件显示 + HelpBox 警告正常工作**（ET-001/002）+ **Components CheckboxGrid 互斥校验正常**（ET-002）+ **EntityConfigValidator MenuItem 批量校验输出正确**（ET-006）+ **AIBehaviorSOEditor 可读摘要标题正常显示**（ET-005）+ **EntitySpawnWaveSOEditor 摘要面板正常显示**（ET-007）+ **EntityDebugWindow Play Mode 概览面板可打开并显示数据**（ET-008）+ **SOCreationWizard 含 Entity 系列 3 种 SO 类型**（ET-010）(**v2.5 扩展**) |
| P1.9 | EntityViewBridge + Debug View | ✅ Entity 生成时自动创建 Debug GO（彩色圆 + HP 文本）+ Despawn 时归还 PoolManager + 每帧位置同步 |
| P1.10 | 刷怪系统（EntitySpawner + EntitySpawnPoint）+ EntitySystemBootstrap | ✅ 场景放置 EntitySpawnPoint + 配置 EntitySpawnWaveSO → 按波次生成 Entity + Timer/AllCleared/OnCallback 三种触发模式 + Loop 循环正常工作 + **场景中放 EntitySystemBootstrap → 自动驱动刷怪系统（v2.6 WF-001）** |
| P1.11 | 集成验收 | ✅ Demo 场景：1 玩家（ControlComponent 手动发射）+ 3 敌人（AIBehaviorSO 驱动追击+AttackComponent 自动射击）+ 双向弹幕交互 + 敌人被命中→DamageContext 传递→受击闪白→击退→伤害数字弹出→死亡延迟→死亡特效→回收 + Entity 总内存 < 2MB + **Demo SO 资产保留为模板（存放 `Assets/_Game/Configs/_Template/`，文件名 `Template_` 前缀）（v2.6 WF-009）** |

### Phase 2：渲染升级 + Entity vs Entity 碰撞 + Luban 迁移（预估 4 天）

> **v2.2 变更**：EntityViewBridge 已提前到 Phase 1（P1.9）；Phase 2 聚焦渲染升级和 Luban。

| 步骤 | 内容 |
|------|------|
| P2.1 | 正式 ViewPrefab 渲染（Spine / 序列帧选型 + EntityViewBridge 自动切换） |
| P2.2 | Entity vs Entity 碰撞（EntityCollisionSolver，圆 vs 圆） |
| P2.3 | Luban 配置迁移（TbEntityConfig + Spawn(int configId,...) 重载） |
| P2.4 | 受击扩展参数（击退/无敌帧/击退曲线） |
| P2.5 | WaveTriggerMode.OnEnterArea + TriggerZone 组件 |
| P2.6 | 集成验收 |

### Phase 3：高级功能（预估 3 天）

| 步骤 | 内容 |
|------|------|
| P3.1 | SkillComponent + 技能效果管理器 |
| P3.2 | AutoAimComponent |
| P3.3 | 伤害管理器（跨 Entity 伤害流水线） |
| P3.4 | 真机性能验证（55fps / 20 Entity + 弹幕） |

---

## 七、质量属性

| 属性 | 目标 | 验证方式 |
|------|------|----------|
| **零 GC** | Entity/组件全池化，事件用 struct，EventBus 预分配 | Profiler GC Alloc = 0 |
| **Tick 性能** | 20 Entity + 2048 弹幕 < 2ms（总 Tick） | Profiler Deep Profile |
| **内存预算** | EntitySystem 总内存 < 2MB（含所有池 + Manager） | Profiler Memory |
| **同屏上限** | 由 EntityPool.MaxCapacity 控制，开发期报警 | LogWarning |
| **可维护性** | 新增组件只需实现接口 + 注册 ComponentType 枚举 | 代码审查 |
| **可测试性** | Entity/组件不依赖 MonoBehaviour，可单元测试 | NUnit |

---

## 八、风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| TargetRegistry 64 槽位不够 | 超大量 Entity 无法被弹幕命中 | 已从 16 扩到 64（D-01）；Phase 2+ 动态注册策略（3.9 节）|
| Entity 纯逻辑层与 View 层桥接复杂 | View 生命周期管理 | Phase 1 EntityViewBridge + Debug Prefab 走 PoolManager（§3.15） |
| SO → Luban 迁移成本 | Phase 2 API 签名变更 | ConfigId 双路桥接（§5.0），渐进迁移不改旧 API |
| Entity vs Entity 碰撞性能 | O(n²) 在 Entity 多时吃力 | Phase 2 用空间分区（Grid/Quadtree） |
| SO 做 Dictionary Key 的 Domain Reload 限制（SA-003） | Enter Play Mode Settings 关闭 Domain Reload 时，SO InstanceID 可能变化导致 `_pools` Dictionary 查询失效 | Phase 1 不使用 Skip Domain Reload；如需支持则改用 int configId 为 Key（Phase 2 Luban 迁移时自然解决） |
| PierceHitMask 扩容内存开销（SA-001） | BulletCore 结构体 +8 bytes（48→56），2048 弹丸多占 16KB | 仍在 L2 缓存范围内，接受此开销 |
| BulletCore.OwnerEntityId 内存开销（GD-R4-001） | BulletCore +4 bytes（56→60），2048 弹丸多占 8KB | 仍在 L2 缓存范围内，接受此开销。必须修改 DanmakuSystem.Fire() API 签名（新增可选 ownerId 参数） |
| AttackComponent 复用 Skill 槽位（GD-R4-003） | Phase 3 SkillComponent 上线时需处理与 AttackComponent 的槽位共存或替代关系 | Phase 3 决策：SkillComponent 可包含 AttackComponent 能力，或两者共存（Skill 槽位拆分为 Attack + Skill） |

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

## 十、策划工作流

> **v2.2 新增（PK R1 GD-003 产物）**。
> **v2.6 更新（WF-001~011）**：新增 10.0 前置条件、依赖关系图、必填标注、Bootstrap 步骤。

### 10.0 前置条件（v2.6 新增，WF-001）

> ⚠️ **场景中必须有一个 EntitySystemBootstrap 组件。** 如缺失，Play Mode 不会有任何 Entity 生成。

```
1. 在场景中创建空 GO（命名建议：_EntitySystem）
2. 挂上 EntitySystemBootstrap 组件
3. 将 Debug View 的 PoolDefinition 拖入 DebugViewPool 字段
4. 完成——Bootstrap 会在 Awake 时自动创建 EntityManager/ViewBridge/Spawner 并发现场景中的 SpawnPoint
```

### 10.1 创建新敌兵（端到端流程）

> **v2.4 更新**：新增 AI 行为 + 攻击 + 特效配置步骤。
> **v2.6 更新（WF-003/008/009/010）**：类型修正 + 依赖图 + 必填标注 + 推荐右键菜单。

**依赖关系图**（v2.6 WF-008，从下到上创建）：
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

```
1. 创建 AI 行为配置（可选，可复用已有的）：
   - 右键 Assets/_Game/Configs/AI/ → Create → Entity/AIBehavior（推荐右键菜单，WF-010）
   - 按优先级配置条件-动作表：
     Entry 0: Condition=TargetInRange(3.0), Action=Attack
     Entry 1: Condition=TargetInRange(8.0), Action=MoveToTarget
     Entry 2: Condition=TargetLost, Action=Patrol, Param=5.0（巡逻半径）
     Entry 3: Condition=Always, Action=Idle  ← ⚠️ 必须有 Always 兜底（WF-005）

2. 右键 Assets/_Game/Configs/Entity/ → Create → Entity/EntityConfig（推荐右键菜单，WF-010）
3. 在 Inspector 中填写：
   - DisplayName: "史莱姆"
   - Camp: Enemy
   - Components: **[必填]** 至少勾选 State（WF-006）。完整示例：[State, Health, Movement, Collision, AI, Skill]
     Skill 槽位由 AttackComponent 使用
   - MaxHp: 50, MoveSpeed: 2, CollisionRadius: 0.4
   - AttackInterval: 1.5, AttackBulletType: (拖入弹幕 **BulletTypeSO**), AttackFireOffset: (0, 0.3)
     ← v2.6 修正（WF-003）：类型为 BulletTypeSO（非 VFXTypeSO）
   - AIBehavior: (拖入步骤 1 创建的 AIBehaviorSO)
   - HitFlashDuration: 0.1, HitFlashColor: white, KnockbackDistance: 0.5
   - ShowDamageNumber: true
   - SpawnEffect: (可选), HitEffect: (可选), DeathEffect: (可选)
   - DeathDelay: 0.3
   - DebugColor: red
   - PoolInitial: 5, PoolMax: 20
4. 保存 SO 资产
```

### 10.2 编排关卡波次

> **v2.6 更新（WF-001）**：步骤 3 明确 Bootstrap 前置。

```
0. [前置] 确认场景中已有 EntitySystemBootstrap（见 §10.0）
1. 右键 Assets/_Game/Configs/SpawnWave/ → Create → Entity/SpawnWaveConfig
2. 在 Inspector 中编排：
   - Wave 0: Groups=[{史莱姆, Enemy, 3, 0.5s}], TriggerMode=Timer, TriggerDelay=2s
   - Wave 1: Groups=[{史莱姆, Enemy, 2}, {精英哥布林, Enemy, 1}], TriggerMode=AllCleared
3. 在场景中创建空 GO → 挂 EntitySpawnPoint → 拖入 WaveConfig SO
   （EntitySystemBootstrap 会在 Awake 时自动发现并启动 AutoStartOnEnable=true 的 SpawnPoint）
4. 调整 AreaRadius（Scene View 中可见黄色圆圈 + 名称标签）
5. Play Mode → 观察波次按配置生成
```

### 10.3 调试与迭代

> **v2.4 措辞修正（GD-R4-007）**：明确 SO 热修改的实际限制。

```
1. Play Mode 中：
   - EntityGizmoDrawer 显示碰撞圈（红=敌、绿=友、灰=中立）
   - EntityViewBridge Debug View 显示彩色圆 + HP 文本
   - 弹幕命中 → DamageContext → 闪白 → 击退 → 伤害数字 → 扣血 → HP 文本更新 → 死亡延迟 → 特效 → 回收
2. 运行时修改 SO 参数对 **已存在的 Entity 不生效**（它们在 Init 时已读取配置快照）。
   新从池中取出的 Entity 会使用新配置。
   v2.6（WF-002）：使用 **Entity Debug Overview** 窗口（Window/Entity/Debug Overview）的
   **"Restart All Waves"** 按钮可快速清除所有 Entity 并重新启动波次，无需退出 Play Mode。
   （Phase 2 可选：EntityManager.HotReloadConfig(EntityConfigSO) 热刷新 API）
3. 批量调整：选中多个 EntityConfigSO → 在 Inspector 中批量修改字段
4. v2.6（WF-002）：EntityConfigSO Inspector 在 Play Mode 下会显示黄色提示，提醒修改仅对新 Entity 生效
```

---

---

## 十一、未决项清单（六轮 PK 汇总）

> **v2.5 新增，v2.6 更新**：汇总 R1~R6 共 69 个问题中所有非 Phase 1 事项。天命人待决策项 = 0（D-01~D-04 全部已决）。

### Phase 2 待办（25 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R1 EC-002 | 碰撞动态注册策略：Entity > 64 时启用 CollisionRegistrationPass |
| 2 | R2 BL-01 / GD-101 | EntityConfigSO 扩充受击参数（击退曲线 KnockbackCurve / 无敌帧 IFrameCount） |
| 3 | R2 BL-02 / GD-102 | WaveTriggerMode.OnEnterArea + TriggerZone 组件 |
| 4 | R2 BL-03 / GD-102 | 刷怪阵型排列模式（Line/Circle，Random 已在 Phase 1） |
| 5 | R2 BL-04 / GD-007 | AI 行为表 conditionType/actionType 迁移 Luban 时改用 enum 类型 |
| 6 | R2 BL-05 / GD-104 | Luban 迁移：添加 `Spawn(int configId,...)` 重载 |
| 7 | R2 GD-006 | Luban 配置表整体迁移（Phase 2 可选保留 SO 或迁移 Luban） |
| 8 | R2 GD-001/103 | EntityViewBridge Phase 2 切换：ViewPrefab 正式渲染（Spine / 序列帧选型） |
| 9 | R4 GD-R4-001 | DamageContext Phase 2 扩展：DamageType / CritMultiplier 等 |
| 10 | R4 GD-R4-001 | HealthComponent 增加 IDamageModifier 接口（减伤/免伤/反弹） |
| 11 | R4 GD-R4-002 | 状态互斥规则 SO 配置化（Phase 1 硬编码 < 5 条） |
| 12 | R4 GD-R4-004 | 受击顿帧（HitStop）实现（Phase 1 已预留 PauseFor/IsPaused） |
| 13 | R4 GD-R4-005 | 刷怪条件分支（HP 判断出精英怪） |
| 14 | R4 GD-R4-005 | 刷怪难度缩放参数（HpMultiplier / CountMultiplier per wave） |
| 15 | R4 GD-R4-007 | EntityManager.HotReloadConfig(EntityConfigSO) 热刷新 API（可选） |
| 16 | R4 GD-R4-008 | 受击变色方案多样化（弹性缩放等）→ ViewBridge 扩展 |
| 17 | R4 GD-R4-008 | Animation 速度倍率 → AnimationComponent 扩展 |
| 18 | R5 ET-005 | AIBehaviorSOEditor 深度：ConditionParam 上下文提示 + 模拟测试按钮 |
| 19 | R5 ET-007 | EntitySpawnWaveSOEditor 深度：拖拽排序、时间线可视化 |
| 20 | R5 ET-008 | EntityDebugWindow 扩展：EventBus 事件追踪 + AI 决策链可视化 |
| 21 | R5 ET-011 | EntityConfigValidator 软警告：SO 资产不在推荐目录下 |
| 22 | R5 ET-004 | asmdef 独立拆分评估（Phase 2+ 如需拆分模块） |
| 23 | R6 WF-007 | PoolDefinition 深度方案：引入 EffectPoolDefinition 子类或 Tag 标记（Phase 1 已有 Tooltip + 预览行） |
| 24 | R6 WF-008 | EntityConfigSOEditor Inspector 内嵌反向引用查询（需缓存机制避免全扫库） |
| 25 | R6 WF-009 | SOCreationWizard "从模板创建" 功能（Phase 1 已有右键 + 复制模板 SO） |

### Phase 3 待办（4 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R4 GD-R4-003 | 完整 SkillComponent 技能系统（Phase 1 AttackComponent 为最小替代） |
| 2 | R4 GD-R4-002 | 完整 FSM 状态机编辑器（可能走 behaviac/行为树） |
| 3 | R4 GD-R4-009 | 直接伤害路径（不走弹幕）→ 需 `EntityManager.FindEntitiesInRadius()` |
| 4 | R5 ET-002 | Phase 3 SkillComponent 上线后 ComponentType Skill 标签改为 `Skill (Attack | Skill)` |

### Backlog（已知限制 / 非阻塞记录，5 项）

| # | 来源 | 描述 |
|---|------|------|
| 1 | R3 SA-003 | SO 做 Dictionary Key 的 Skip Domain Reload 兼容性——已知限制（§八风险表） |
| 2 | R4 GD-R4-006 | 跑酷(⭐⭐)和放置(⭐⭐)品类需重大扩展——§一设计目标已诚实记录 |
| 3 | R4 GD-R4-004 | 镜头抖动（ScreenShake）——框架外职责，游戏层订阅 OnDeath 自行实现 |
| 4 | R4 GD-R4-005 | SpawnFormation 枚举已预留 Line/Circle，Phase 1 只实现 Random |
| 5 | R5 ET-011 | SO 资产命名/目录组织无强制约束——Phase 1 资产少，文档推荐即可 |

### 天命人待决策（0 项）

> D-01~D-04 全部已决。五轮 PK 中无遗留待决策项。

---

> **文档维护说明**：此文档随开发进度迭代更新，每次架构变更需在此文档中同步修改并更新版本号。行为契约（BC-xx）变更需 ADR 审批。
