# Entity-Component 通用角色框架 · TDD v2.6

> **版本**：v2.6 | **日期**：2026-05-01 | **状态**：Phase 2 完成 ✅ / Phase 3A 完成 ✅  
> **决策记录**：ADR-033  
> **命名空间**：`MiniGameTemplate.Entity`  
> **目录**：`Assets/_Framework/EntitySystem/`

---

## 子文件目录

| # | 文件 | 内容摘要 | 行数 |
|---|------|---------|------|
| 1 | [EC_TDD_01_OVERVIEW.md](EC_TDD_01_OVERVIEW.md) | 设计目标 · 品类适配 · 行为契约(BC-01~08) · 命名空间与目录结构 | ~200 |
| 2 | [EC_TDD_02_CORE_ARCH.md](EC_TDD_02_CORE_ARCH.md) | 核心接口 · Tick优先级 · EntityEventBus · CollisionComponent桥接 | ~339 |
| 3 | [EC_TDD_03_ENTITY_POOL.md](EC_TDD_03_ENTITY_POOL.md) | EntityPool · EntityManager · 系统集成矩阵 | ~277 |
| 4 | [EC_TDD_04_SYSTEMS.md](EC_TDD_04_SYSTEMS.md) | TargetRegistry · 阵营 · 碰撞时序 · 内存预算 · 刷怪系统 · ViewBridge | ~461 |
| 5 | [EC_TDD_05_COMPONENTS.md](EC_TDD_05_COMPONENTS.md) | 组件详细设计：State/Health/Anim/Move/Collision/AutoAim/AI/Skill/Attack/Buff | ~365 |
| 6 | [EC_TDD_06_CONFIG.md](EC_TDD_06_CONFIG.md) | EntityConfigSO · Luban预留 · 实施计划 · 质量属性 · 风险缓解 | ~218 |
| 7 | [EC_TDD_07_EDITOR.md](EC_TDD_07_EDITOR.md) | 编辑器工具：Gizmo/ConfigEditor/Validator/DebugWindow/Wizard | ~326 |
| 8 | [EC_TDD_08_APPENDIX.md](EC_TDD_08_APPENDIX.md) | 策划工作流 · 未决项清单(Phase2/3/Backlog) | ~201 |

---

## 变更日志（精简版）

| 版本 | 日期 | 关键变更 |
|------|------|---------|
| v2.6 | 2026-05-01 | PK R6 策划工作流落地 WF-001~011 |
| v2.5 | 2026-04-30 | PK R5 编辑器工具 ET-001~011 |
| v2.4 | 2026-04-30 | PK R4 游戏设计师 GD-R4-001~011 |
| v2.3 | 2026-04-29 | PK R3 软件架构师 SA-001~007 |
| v2.2 | 2026-04-29 | PK R2 策划工作流 + D-01~04 |
| v2.1 | 2026-04-28 | PK R1 技术 11 问题收敛 |
| v2.0 | 2026-04-28 | 初版 TDD（从设计草案升级） |

---

## 相关代码路径

```
Assets/_Framework/EntitySystem/
├── Core/           Entity.cs, EntityPool.cs, EntityManager.cs, EntityEventBus.cs
├── Components/     StateComponent, HealthComponent, MovementComponent, ...
├── Spawner/        EntitySpawnPoint, EntitySpawnWaveSO, EntitySystemBootstrap
├── Config/         EntityConfigSO, EntityConfigId
├── AI/             AIBehaviorSO, IAIAction, ConditionActionTableStrategy
├── View/           EntityViewBridge
└── Skill/          SkillComponent, SkillConfigSO, BuffComponent, BuffConfigSO
Assets/_Framework/Editor/Entity/   EntityConfigSOEditor, EntityGizmoDrawer, ...
```

---

## PK 评审记录（已归档）

6 轮 PK 评审记录已归档至 `Archive/EntityComponent/`。
