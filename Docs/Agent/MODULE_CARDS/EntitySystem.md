---
system: knowledge-engineering
scope: module-card-entity-system
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_context: Docs/Agent/CONTEXT_PACKS/EntitySystem.md
---

# Module Card: EntitySystem

## 1. 模块职责

EntitySystem 是纯 C# Entity-Component 战斗框架，负责实体生命周期、组件容器、Tick 调度、对象池、事件总线、碰撞桥接、刷怪系统、技能/Buff/DOT/被动等战斗能力。

## 2. 不负责什么

- 不直接持有或驱动 GameObject 渲染表现，View 同步走 Bridge。
- 不负责 FairyGUI UI 展示。
- 不负责具体游戏规则的最终编排，ShooterGame 的胜负和关卡流程由 Game 层控制。
- 不直接处理微信平台、存档、场景导航。

## 3. 入口类 / 核心类型

| 类型 | 职责 |
|------|------|
| `Entity` | 纯 C# 逻辑容器，组件数组访问 |
| `EntityManager` | 生命周期与 Tick 调度 |
| `EntityPool` | 预分配数组与空闲栈 |
| `EntityEventBus` | 零 GC 事件总线 |
| `EntitySystemBootstrap` | 场景胶水层入口 |
| `EntitySpawnWaveSO` / Spawner | 波次刷怪 |
| `HealthComponent`, `MovementComponent`, `CollisionComponent` | 基础战斗组件 |
| `SkillComponent`, `BuffComponent`, `PassiveComponent` | V2 战斗能力 |

## 4. 数据流

```text
EntityConfigSO / Wave SO
  -> EntitySystemBootstrap
  -> EntityManager.Spawn
  -> EntityPool 取 Entity
  -> 添加组件并初始化
  -> EntityManager Tick 按 TickOrders 执行
  -> EntityEventBus / Collision / Skill / Buff 产生状态变化
  -> EntityViewBridge 同步表现
  -> Despawn 标记 PendingDespawn
  -> 帧末回收进 EntityPool
```

## 5. 生命周期

```text
Create/Spawn -> Initialize Components -> Tick -> Event/Collision/Skill/Buff -> PendingDespawn -> Cleanup -> Pool Return
```

战斗退场时，系统必须响应统一清理流程，清空活跃实体、组件状态、事件订阅和刷怪状态。

## 6. 依赖关系

EntitySystem 位于独立战斗层，依赖基础 Utils/Data/Event 等低层能力。Game 层可以使用 EntitySystem，但 EntitySystem 不应反向依赖 ShooterGame 业务。

## 7. 关键 SO / 配置路径

```text
Assets/_Game/Configs/Entity/
Assets/_Game/Configs/AI/
Assets/_Game/Configs/ShooterGame/
Assets/_Game/Configs/ShooterGame/Skills/
Assets/_Game/Configs/ShooterGame/Buffs/
Assets/_Game/Configs/ShooterGame/Dots/
Assets/_Game/Configs/ShooterGame/Passives/
Assets/_Game/Configs/ShooterGame/Waves/
```

## 8. 关键 ADR

- ADR-033：Entity-Component 框架。
- ADR-035：战斗退场生命周期统一事件通道。
- 与技能/普攻相关：`SG_V2_TDD_06_ATTACK_SKILL.md`。

## 9. 热路径 / 性能约束

- Tick、碰撞、技能触发、Buff 更新中禁止 GC 分配。
- 避免 LINQ、foreach 装箱、字符串拼接、临时 List/数组。
- ComponentType 和 TickOrders 是性能与行为契约，不能随意改。
- EntityPool 与 EventBus 应保持预分配策略。

## 10. 常见错误

- 在组件中直接引用场景 GameObject。
- 修改组件顺序或 ComponentType 后未同步文档和所有访问点。
- 新增组件后忘记初始化、重置、回收清理。
- 改 Skill/Buff 只改代码不改 SO、Inspector、Validator。
- 碰撞逻辑改动未考虑 CampUtility、TargetRegistry、底线检测。

## 11. 修改前必读

- `CONTEXT_PACKS/EntitySystem.md`
- `EC_TDD_INDEX.md`
- `EC_TDD_02_CORE_ARCH.md`
- `EC_TDD_05_COMPONENTS.md`
- `SO_WORKFLOWS_02_ENTITY.md`
- `ADR_INDEX.md` 中 ADR-033/035

## 12. 修改后必验

- Unity 编译零错误。
- Spawn/Despawn/Retry/退出战斗不残留。
- 相关组件初始化和 Reset 正确。
- 热路径 GC 检查。
- 新增/修改 SO 后 Validator 通过。
- 触碰技能/Buff/DOT 时验证装备、生效、叠加、结束、清理。