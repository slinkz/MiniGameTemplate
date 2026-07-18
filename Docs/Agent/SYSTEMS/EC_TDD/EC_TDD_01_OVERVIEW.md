---
system: entity-component
scope: overview-contracts
last_verified: 2026-05-02
related_code: Assets/_Framework/EntitySystem/Core/Entity.cs, EntityComponent*.cs
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
| BC-01.1 | Entity 是纯数据容器，不继承 MonoBehaviour，不持有 GameObject | ✅ P1.1 |
| BC-01.2 | Entity 通过 `GetComponent(ComponentType)` 按枚举索引 O(1) 查询组件；泛型版 `GetComponent<T>()` 为 O(N)（N≤16，线性扫描 + 类型检查）（见 §3.2） | ✅ P1.1 |
| BC-01.3 | Entity 在生命周期节点（Init/Tick/Reset）统一驱动所有 Tickable 组件 | ✅ P1.1 |
| BC-01.4 | Entity 持有本地事件总线（EntityEventBus），组件只在本 Entity 范围内通信 | ✅ P1.1 |
| BC-01.5 | Entity 持有唯一 ID（EntityId，uint32），用于跨系统引用 | ✅ P1.1 |
| BC-01.6 | Entity 持有 Camp（阵营），Phase 1 统一使用 `EnumCamp` 枚举（天命人决策 D-02，替代原 BulletFaction）；如未来需扩展阵营，引入独立枚举 + 映射层（见 §3.11） | ✅ P1.1 |

### BC-02 组件基类契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-02.1 | 所有组件实现 `IEntityComponent` 接口：`Init(Entity owner)` / `Reset()` / `SetActive(bool)`。组件通过 `owner` 间接访问配置（见 §3.2） | ✅ P1.1 |
| BC-02.2 | Tickable 组件额外实现 `ITickable`：`Tick(float dt)` + `TickOrder` 属性 | ✅ P1.1 |
| BC-02.3 | 组件通过 `SetActive(false)` 休眠——从 TickList 移除，不响应事件，零开销 | ✅ P1.1 |
| BC-02.4 | 组件之间**禁止直接引用**，只通过 EntityEventBus 或 Entity.GetComponent 通信 | ✅ P1.1（接口约束） |

### BC-03 Entity 本地事件总线契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-03.1 | EntityEventBus 为每个 Entity 独立实例，事件不跨 Entity 传播 | ✅ P1.1 |
| BC-03.2 | 支持 `Publish<T>(T evt)` / `Subscribe<T>(Action<T>)` / `Unsubscribe<T>(Action<T>)` | ✅ P1.1 |
| BC-03.3 | 事件类型用 struct（零 GC），通过泛型类型 ID 分发 | ✅ P1.1 |
| BC-03.4 | Reset 时自动清空所有订阅（防池化后事件泄漏） | ✅ P1.1 |

### BC-04 Entity 池管理器契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-04.1 | EntityPool 按配置类型分池（EntityConfigId → 独立池） | ✅ P1.3 |
| BC-04.2 | 池中 Entity + 组件整体预分配，取出/归还零 GC | ✅ P1.3 |
| BC-04.3 | 取出时调用所有组件 `Init()`，归还时调用所有组件 `Reset()` | ✅ P1.3 |
| BC-04.4 | 每池设 InitialCapacity（预热）+ MaxCapacity（硬上限），超限 LogWarning 不崩溃 | ✅ P1.3 |
| BC-04.5 | 池采用预分配数组 + 空闲槽位栈（参考 BulletWorld 模式），非 Queue\<Entity\> | 待实现 |

### BC-05 碰撞集成契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-05.1 | CollisionComponent 实现 `ICollisionTarget` 接口，桥接到现有 TargetRegistry | ✅ P1.5 |
| BC-05.2 | CollisionComponent 在 Init 时向 `DanmakuSystem.Instance.RegisterTarget()` 注册 | ✅ P1.5 |
| BC-05.3 | CollisionComponent 在 Reset 时向 `DanmakuSystem.Instance.UnregisterTarget()` 注销 | ✅ P1.5 |
| BC-05.4 | 碰撞回调（OnBulletHit/OnLaserHit/OnSprayHit）转发到 EntityEventBus | ✅ P1.5 |
| BC-05.5 | CircleHitbox 由 CollisionComponent 每帧从 Entity 位置 + 配置半径更新 | ✅ P1.5 |
| BC-05.6 | OBB 碰撞（Entity vs Entity）走 ObstaclePool 注册，复用 ObstacleCollisionMath | Phase 2 |

### BC-06 Tick 管线契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-06.1 | EntityManager 统一驱动所有活跃 Entity 的 Tick，不依赖 MonoBehaviour.Update | ✅ P1.3 |
| BC-06.2 | Tickable 组件按 TickOrder 升序执行（数字越小越先） | ✅ P1.1+P1.3 |
| BC-06.3 | 定频 Tick 组件内部自行计帧，间隔未到时跳过 | ✅ P1.3（框架层不干预，组件自行实现） |
| BC-06.4 | EntityManager.Tick() 在 DanmakuSystem.Update() 之后、LateUpdate 之前调用。**已知限制**：Entity 位置更新与碰撞检测存在 1 帧延迟，小游戏场景可接受（见 §3.12） | ✅ P1.3（调用时机由 Bootstrap 控制） |

### BC-07 决策接口契约

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-07.1 | ControlComponent 和 AIComponent 均实现 `IDecisionMaker` 接口 | ✅ P1.7 |
| BC-07.2 | 同一 Entity 上 Control 和 AI **互斥挂载** | ✅ P1.7 |
| BC-07.3 | AIComponent 内部策略抽象为 `IDecisionStrategy`，可替换 | ✅ P1.7 |
| BC-07.4 | 默认 AI 策略：ConditionActionTableStrategy（条件-动作表，配置驱动） | ✅ P1.7 |

### BC-08 配置驱动契约

> **v2.2 变更（GD-105）**：从 Luban 硬编码契约改写为抽象配置契约，同时覆盖 SO 和 Luban 双路。

| 编号 | 契约 | 状态 |
|------|------|------|
| BC-08.1 | Entity 配置（组件列表、属性）通过**配置资产**驱动（Phase 1: EntityConfigSO；Phase 2+: 可选 Luban 导表） | ✅ P1.3（最小版） |
| BC-08.2 | 配置资产通过 EntityManager 内部解析，外部通过 `EntityConfigSO` 引用或 `int ConfigId` 访问 | ✅ P1.3 |
| BC-08.3 | 新增状态标签/AI 行为只加配置，不改代码（Phase 1: 扩展 SO 字段；Phase 2+: 扩展 Luban 表） | ✅ P1.3（结构支持） |

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
│   │   ├── EntitySpawner.cs       — 刷怪驱动器
│   │   └── EntityTriggerZone.cs   — P2.5: 触发区域检测器（SpawnPoint 级启动开关）
│   └── Config/
│       ├── EntityConfigSO.cs        — Phase 1 角色配置 SO
│       ├── AIBehaviorSO.cs          — v2.4: AI 行为条件-动作表 SO
│       ├── EntitySpawnWaveSO.cs     — 刷怪波次配置 SO
│       └── (Phase 2: Luban 生成配置)
│
Assets/_Framework/Editor/Entity/       ← Editor 工具（归入 MiniGameFramework.Editor.asmdef）
├── EntityGizmoDrawer.cs               — v2.5: 静态 [DrawGizmo] 碰撞圈/HP Gizmo
├── EntityConfigSOEditor.cs            — v2.5: CustomEditor 条件显示 + HelpBox 校验
├── AIBehaviorSOEditor.cs              — v2.5: 行为条目可读摘要标题
├── EntitySpawnWaveSOEditor.cs         — v2.5: 波次摘要面板
├── EntityConfigValidator.cs           — v2.5: MenuItem 批量校验
└── EntityDebugWindow.cs               — v2.5: Play Mode 概览面板
```

**当前知识入口（替代早期模块 README）**：
- `Docs/Agent/MODULE_CARDS/EntitySystem.md`：职责边界、关键文件、修改前必读与修改后必验
- `Docs/Agent/CONTEXT_PACKS/EntitySystem.md`：任务上下文与相关系统链路
- `Docs/Agent/INDEX.md`：按文件路径和问题类型定位当前说明

**asmdef 隔离方案（v2.5 新增，ET-004）**：
- **Runtime 代码**（`_Framework/EntitySystem/Scripts/`）归入 `MiniGameFramework.Runtime.asmdef`（已有，无需新建 asmdef）
- **Editor 工具**（CustomEditor / Gizmo / Validator / EditorWindow）放在 `_Framework/Editor/Entity/` 目录，归入 `MiniGameFramework.Editor.asmdef`（已有，includePlatforms: Editor）
- **不新建独立 asmdef**——项目规模尚小，复用框架级 asmdef 即可。Phase 2+ 如需拆分模块再评估
- 所有 Editor 代码必须包裹 `#if UNITY_EDITOR` 或放在 Editor asmdef 管辖目录
- EntitySystem/Scripts/ 下**不再有 Editor/ 子目录**——Editor 代码统一收归 `_Framework/Editor/Entity/`
