---
system: knowledge-engineering
scope: context-pack-entity-system
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: EntitySystem

## 适用任务

- 修改 Entity 生命周期、对象池、组件系统、Tick 顺序。
- 新增或修改敌人、技能、Buff、DOT、被动、碰撞、刷怪。
- 排查战斗实体不生成、不回收、不受伤、碰撞异常。
- 评估战斗框架架构边界。

## 必读文档

| 目的 | 文档 |
|------|------|
| 总入口 | `SYSTEMS/EC_TDD/EC_TDD_INDEX.md` |
| 核心架构、TickOrder、EventBus | `SYSTEMS/EC_TDD/EC_TDD_02_CORE_ARCH.md` |
| EntityPool / EntityManager | `SYSTEMS/EC_TDD/EC_TDD_03_ENTITY_POOL.md` |
| 系统集成、TargetRegistry、碰撞、Spawner | `SYSTEMS/EC_TDD/EC_TDD_04_SYSTEMS.md` |
| 组件详细设计 | `SYSTEMS/EC_TDD/EC_TDD_05_COMPONENTS.md` |
| SO 配置 | `SYSTEMS/EC_TDD/EC_TDD_06_CONFIG.md`, `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md` |
| 编辑器工具 | `SYSTEMS/EC_TDD/EC_TDD_07_EDITOR.md`, `SYSTEMS/EDITOR_TOOLS_MANUAL/EDITOR_TOOLS_MANUAL_04_INSPECTORS.md` |
| 架构决策 | `ADR/ADR_INDEX.md` 中 ADR-033 |

## 关键代码入口

```text
UnityProj/Assets/_Framework/EntitySystem/
├── Core/
├── Components/
├── Systems/
├── Spawner/
├── Config/
├── AI/
├── View/
└── Skill/
```

常见映射：

| 代码 | 先读 |
|------|------|
| `Core/*.cs` | `SYSTEMS/EC_TDD/EC_TDD_02_CORE_ARCH.md`, `SYSTEMS/EC_TDD/EC_TDD_03_ENTITY_POOL.md` |
| `Components/Skill*` | `SYSTEMS/EC_TDD/EC_TDD_05_COMPONENTS.md`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_06_ATTACK_SKILL.md` |
| `Components/Buff*` | `SYSTEMS/EC_TDD/EC_TDD_05_COMPONENTS.md`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_03_BUFF_DOT_PASSIVE.md` |
| `Collision/*.cs` | `SYSTEMS/EC_TDD/EC_TDD_04_SYSTEMS.md`, `SYSTEMS/OBB_TDD/OBB_TDD_INDEX.md` |
| `EntitySystemBootstrap.cs` | `SYSTEMS/EC_TDD/EC_TDD_04_SYSTEMS.md`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_07_LIFECYCLE.md` |
| `Config/*SO.cs` | `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_02_ENTITY.md` |

## 关键 SO / 配置路径

```text
UnityProj/Assets/_Game/Configs/Entity/
UnityProj/Assets/_Game/Configs/AI/
UnityProj/Assets/_Game/Configs/ShooterGame/
UnityProj/Assets/_Game/Configs/ShooterGame/Skills/
UnityProj/Assets/_Game/Configs/ShooterGame/Buffs/
UnityProj/Assets/_Game/Configs/ShooterGame/Dots/
UnityProj/Assets/_Game/Configs/ShooterGame/Passives/
UnityProj/Assets/_Game/Configs/ShooterGame/Waves/
```

## 关键 ADR / 约束

- ADR-033：Entity-Component 框架。
- Entity 是纯 C# 逻辑容器，不直接绑定 GameObject。
- View 同步走 EntityViewBridge 或游戏层 View Bridge。
- 热路径零 GC。
- ComponentType 使用固定数组索引，不随意扩容或改变含义。
- Tick 顺序是行为契约，修改前必须查文档和影响面。

## 常见坑

- 在 Tick 中 new 对象、LINQ、字符串拼接。
- 直接让 Entity 持有场景 GameObject。
- 改 Skill/Buff 后忘记同步 SO 配置和自定义 Inspector。
- 修改碰撞后忘记 TargetRegistry、CampUtility、底线检测的关系。
- 只改代码不改 Template/正式 SO 资产。

## 修改后必验

- Unity 编译零错误。
- 相关 PlayMode 或手动战斗流程可跑通。
- 对热路径改动做 GC 检查。
- 新增/修改 SO 后跑相关 Validator。
- 修改 Skill/Buff/DOT 后至少验证：装备、生效、持续时间、叠加、清理。
- 修改生命周期后验证战斗退出、重试、返回主菜单无残留。