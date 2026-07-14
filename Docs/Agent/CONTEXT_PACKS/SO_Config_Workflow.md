---
system: knowledge-engineering
scope: context-pack-so-config-workflow
status: active
created: 2026-07-14
last_updated: 2026-07-14
---

# Context Pack: SO Config Workflow

## 适用任务

- 新增或修改敌人、技能、Buff、DOT、被动、道具、波次、关卡。
- 新增弹幕花样、VFX、Audio、变量、事件、Pool、FSM、SceneDefinition。
- 修改配置资产路径、命名、模板资产和正式资产。
- 操作 Luban 配置表。

## 必读文档

| 目的 | 文档 |
|------|------|
| SO 总入口 | `SO_WORKFLOWS_INDEX.md` |
| 核心配置 | `SO_WORKFLOWS_01_CORE.md` |
| Entity/Skill/Buff/DOT/Passive/Pickup/Wave | `SO_WORKFLOWS_02_ENTITY.md` |
| 弹幕 | `SO_WORKFLOWS_03_DANMAKU.md` |
| VFX/Rendering | `SO_WORKFLOWS_04_VFX_RENDER.md` |
| 变量/事件/Pool/FSM/Audio | `SO_WORKFLOWS_05_INFRA.md` |
| 命名规则 | `CONV_01_NAMING.md` |
| Luban Skill 源 | `skills/luban-config/SKILL.md` |

## 关键代码入口

```text
UnityProj/Assets/_Framework/**/**SO.cs
UnityProj/Assets/_Framework/**/Scripts/Config/*.cs
UnityProj/Assets/_Game/Configs/**
UnityProj/DataTables/**
skills/luban-config/**
```

## 关键 SO / 配置路径

| 类型 | 路径 |
|------|------|
| Core | `Assets/_Game/Configs/Core/` |
| Entity | `Assets/_Game/Configs/Entity/` |
| ShooterGame | `Assets/_Game/Configs/ShooterGame/` |
| Skill | `Assets/_Game/Configs/ShooterGame/Skills/` |
| Buff | `Assets/_Game/Configs/ShooterGame/Buffs/` |
| DOT | `Assets/_Game/Configs/ShooterGame/Dots/` |
| Passive | `Assets/_Game/Configs/ShooterGame/Passives/` |
| SpawnWave | `Assets/_Game/Configs/ShooterGame/Waves/` |
| Danmaku | `Assets/_Game/Configs/Danmaku/` |
| VFX | `Assets/_Game/Configs/VFX/` |
| Variables | `Assets/_Game/Configs/Variables/` 或 SG 专用 Variables |
| Events | `Assets/_Game/Configs/Events/` |

## 关键 ADR / 约束

- SO 是项目级资产，不引用场景对象。
- 模板资产使用 `Template_` 前缀，正式资产使用业务前缀或语义名。
- 新增配置应优先走已有 SO 类型和工作流，不直接硬编码在 MonoBehaviour 中。
- Luban 适用于表格规模较大或需要策划协作的配置，ShooterGame V1/V2 很多内容优先使用 SO。

## 常见坑

- 创建了代码类型但没有创建对应 SO 资产。
- 修改 SO 字段后没有同步自定义 Inspector、Validator、模板资产。
- 新增技能/Buff/DOT 时 ID 范围冲突。
- 在 Play Mode 下改了临时值却以为已经持久化。
- `skills/` 与运行时 Skill 目录（`.workbuddy/skills/` 或 `.codebuddy/skills/`）未同步导致 Agent 自动触发知识过期。

## 修改后必验

- Asset 引用无 Missing。
- 命名、路径、前缀符合 `SO_WORKFLOWS` 与 `CONV_01`。
- 相关 Validator 通过。
- 运行时读取配置正常。
- 若涉及 Luban，执行生成脚本并确认生成代码、bytes、预览 JSON。
- 若改 Skill，确认 `skills/` 与实际运行时 Skill 目录的同步策略没有被破坏。
