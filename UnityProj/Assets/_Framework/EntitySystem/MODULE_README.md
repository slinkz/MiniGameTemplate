# Entity-Component 框架

> 通用角色组件框架。**核心定位：弹幕射击 + 塔防**。Entity 不绑 GO，纯 C# 对象 + ComponentType 枚举 O(1) GetComponent。支持池化、配置驱动、零 GC 事件总线。

---

## Quick Start（5 步上手）

### 1. 场景中添加 EntitySystemBootstrap

在场景根 GameObject 上添加 `EntitySystemBootstrap` 组件。这是整个 Entity 系统的"引擎启动钥匙"。

- **DebugViewPool**（必填）：指向 `Template_DebugViewPool` 资产（或自建 PoolDefinition）
- **DamageNumberPool**（可选）：指向 `Template_DamageNumberPool` 资产

### 2. 复制模板 SO 并改名

模板路径：`Assets/_Game/Configs/_Template/Entity/Template_Slime.asset`

右键复制 → 改名为你的怪物类型（如 `Goblin.asset`）。修改参数：

| 字段 | 说明 |
|------|------|
| DisplayName | 调试/UI 显示名 |
| Camp | 阵营（Player / Enemy / Neutral） |
| MaxHp | 最大生命值 |
| MoveSpeed | 移动速度 |
| CollisionRadius | 碰撞半径 |
| AttackInterval | 攻击间隔 |
| AIBehavior | 拖入 AI 行为配置 SO |

### 3. 配置 AI 行为

路径：`Assets/_Game/Configs/_Template/AI/Template_SlimeAI.asset`

AIBehaviorSO 是条件-动作表，按优先级从高到低配置：

- **TargetInRange + Attack**：目标进入攻击范围 → 攻击
- **Always + MoveToTarget**：无条件 → 朝目标移动（兜底）

### 4. 创建/引用 EntitySpawnWaveSO

路径：`Assets/_Game/Configs/_Template/SpawnWave/Template_EnemyWave.asset`

配置波次：每波的怪种、数量、间隔、触发模式（Timer / AllCleared / OnCallback）。

在场景中放置 `EntitySpawnPoint` 组件 → 将 WaveConfig 指向你的波次 SO。

> **TriggerZone 触发启动（P2.5 新增）**：给 EntitySpawnPoint 的 TriggerZone 字段拖入一个 `EntityTriggerZone` GO（挂 BoxCollider2D/CircleCollider2D + EntityTriggerZone 脚本）。有 TriggerZone = 等玩家进入区域后才开始刷怪；无 TriggerZone = 按 AutoStartOnEnable 自动开始。详见 TDD §10.2。

### 5. Play → 验证

按 Play，Entity 会按波次生成。场景中有 `EntityDemoInputBridge` 时可用 WASD + Space 操控玩家。

---

## 文件清单

```
Scripts/
├── Core/
│   ├── Entity.cs                 — Entity 容器（纯 C#，不绑 GO）
│   ├── EntityManager.cs          — 全局管理器（池化/Tick/延迟销毁）
│   ├── EntityPool.cs             — 配置驱动的对象池
│   ├── EntitySystemBootstrap.cs  — 胶水层 MonoBehaviour（一拖即用）
│   └── EntityManagerAccessor.cs  — 全局静态访问点
├── Components/
│   ├── StateComponent.cs         — 互斥状态机
│   ├── HealthComponent.cs        — 生命值 + 受伤/死亡流程
│   ├── MovementComponent.cs      — 逻辑位移 + 击退
│   ├── CollisionComponent.cs     — ICollisionTarget 桥接弹幕系统
│   ├── ControlComponent.cs       — 玩家输入 → DecisionCommand
│   ├── AIComponent.cs            — AI 策略 → DecisionCommand
│   └── EnemyShootComponent.cs    — 敌机无条件射击（V2）
├── Decision/
│   ├── IDecisionMaker.cs         — 统一决策接口
│   ├── DecisionCommand.cs        — 决策输出结构体
│   ├── IDecisionStrategy.cs      — AI 策略接口
│   ├── ConditionActionTableStrategy.cs — 默认条件-动作表策略
│   └── Actions/                  — 5 个内置 IAIAction
├── View/
│   ├── EntityViewBridge.cs       — Entity→View GO 映射（位置同步）
│   └── EntityHitReactionHandler.cs — 受击表现管线（闪白/击退/伤害数字/死亡延迟）
├── Spawner/
│   ├── EntitySpawnPoint.cs       — 场景刷怪点组件
│   ├── EntitySpawner.cs          — 刷怪驱动器
│   └── EntityTriggerZone.cs      — 触发区域检测器（SpawnPoint 级启动开关）
├── Config/
│   ├── EntityConfigSO.cs         — 角色配置 SO
│   ├── AIBehaviorSO.cs           — AI 行为配置 SO
│   └── EntitySpawnWaveSO.cs      — 刷怪波次配置 SO
└── Events/
    ├── EntityEventBus.cs         — 零 GC 泛型事件总线
    └── EntityEvents.cs           — 事件定义（OnDamaged/OnDeath/OnCollisionHit...）
```

---

## 架构要点

- **Entity 不持有 GO**：纯逻辑容器，View 由 EntityViewBridge 管理
- **零 GC 设计**：EntityEventBus 预分配 Delegate 数组 + EntityPool 预分配实体数组
- **配置驱动**：所有属性从 EntityConfigSO 读取，策划在 Inspector 中编辑即生效
- **时序铁律**：EntityManager.Tick → Spawner.Tick → ViewBridge.SyncAll → HitReactionHandler.Tick
