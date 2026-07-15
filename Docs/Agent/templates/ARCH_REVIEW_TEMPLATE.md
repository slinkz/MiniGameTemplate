---
system: knowledge-engineering
scope: architecture-review-template
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md, Docs/Agent/KNOWLEDGE/CODE_KNOWLEDGE_MAP.md, Docs/Agent/ADR/ADR_SCHEMA.md
---

# Architecture Review Template

> 用途：Level 2/3 任务编码前使用。先完成本模板，再决定是否能进入实现。

## 1. 任务定义

- 需求：
- 目标行为：
- 非目标：
- 审查等级：Level 2 / Level 3
- 判断依据：

## 2. 上下文路由

| 类型 | 文档 | 本次读取结论 |
|------|------|--------------|
| Context Pack | | |
| Module Card | | |
| Code Knowledge Map | | |
| TDD/GDD/Workflow | | |
| ADR Schema / ADR 原文 | | |
| CONV / 平台规则 | | |
| Debug / MCP / WeChat | | |

## 3. 模块边界

| 模块 | 本次角色 | 是否已有承载点 | 边界风险 |
|------|----------|----------------|----------|
| | 主改/被影响/验证 | 是/否 | 低/中/高 |

检查项：

- 是否让 Game 层规则进入 Framework 层：是/否
- 是否绕过 Boot / GameBootstrapper / AppFlow：是/否
- 是否新增与既有系统并行的重复路径：是/否
- 是否把业务流程硬塞进 Manager / Singleton：是/否
- 是否修改 FairyGUI 自动生成代码：是/否
- 是否让 ScriptableObject 引用场景对象：是/否

说明：

```text

```

## 4. 代码路径影响

| 路径/符号 | 改动类型 | 映射来源 | 风险 | 验证项 |
|-----------|----------|----------|------|--------|
| | 新增/修改/删除 | `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` / 新发现 | 低/中/高 | |

未在 `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` 中覆盖但应补充的路径：

```text

```

## 5. ADR 约束检查

| ADR | Status | ImplementationStatus | AppliesTo 命中 | Constraints 满足 | Verification |
|-----|--------|----------------------|----------------|------------------|--------------|
| | | | 是/否 | 是/否 | |

需要新增或更新 ADR：

- 是/否：
- 原因：
- 预计 ADR 主题：

若任一 Accepted ADR 的 Constraints 不满足，结论必须是“不可直接编码”。

## 6. 数据、资产与平台

| 类型 | 是否影响 | 路径/对象 | 风险 | 验证 |
|------|----------|-----------|------|------|
| ScriptableObject | 是/否 | | | |
| FairyGUI | 是/否 | | | |
| Scene | 是/否 | | | |
| Prefab/Texture/Audio | 是/否 | | | |
| Luban | 是/否 | | | |
| link.xml / IL2CPP | 是/否 | | | |
| WebGL/微信小游戏 | 是/否 | | | |
| 云存储/网络/CDN/广告 | 是/否 | | | |

## 7. 热路径与性能

- 是否触碰 Update/Tick/渲染/碰撞：是/否
- 是否可能新增 GC：是/否
- 是否改变对象池/生命周期：是/否
- 是否改变 DrawCall/RuntimeAtlas/材质关键字：是/否
- 是否需要 Profiler 或帧调试验证：是/否

风险说明：

```text

```

## 8. 方案选择

| 方案 | 优点 | 缺点 | 是否采用 |
|------|------|------|----------|
| A | | | 是/否 |
| B | | | 是/否 |

选择理由：

```text

```

## 9. 停止条件检查

| 停止条件 | 是否命中 | 处理 |
|----------|----------|------|
| 违反 Accepted ADR | 是/否 | |
| 改变模块依赖方向 | 是/否 | |
| 需要新增通用框架能力 | 是/否 | |
| 文档与代码事实冲突 | 是/否 | |
| 验证手段缺失 | 是/否 | |
| 资产数据来源不清 | 是/否 | |
| 涉及微信真机但无法验证 | 是/否 | |

## 10. 验证计划

| 验证项 | 工具/入口 | 通过标准 | 本次是否必须 |
|--------|-----------|----------|--------------|
| 编译 | Unity/MCP | 0 error | 是/否 |
| 架构检查 | Architecture Check | 0 blocker | 是/否 |
| SO 校验 | Validator | 无 Missing/非法配置 | 是/否 |
| PlayMode/手动流程 | | 核心流程通过 | 是/否 |
| Profiler/GC | | 热路径无新增分配 | 是/否 |
| 渲染可见性 | Game View / Frame Debugger | 可见且状态正确 | 是/否 |
| 微信开发者工具 | | 无平台异常 | 是/否 |
| 真机 | | 关键路径通过 | 是/否 |

## 11. 知识资产更新计划

| 文档 | 是否更新 | 原因 |
|------|----------|------|
| `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` | 是/否 | |
| Module Card | 是/否 | |
| Context Pack | 是/否 | |
| ADR 原文 / `ADR/ADR_SCHEMA.md` | 是/否 | |
| `SO_WORKFLOWS_*` | 是/否 | |
| `DEBUG_PLAYBOOK.md` | 是/否 | |
| `AGENT_BOOTSTRAP.md` / `INDEX.md` | 是/否 | |
| P6 changes 包 | 是/否 | |

## 12. 结论

- 可以直接编码：是/否
- 需要先补 ADR：是/否
- 需要先补验证器/资产：是/否
- 主要风险：
- 本次必跑验证：
- 实现后必须更新的知识资产：
- 需要用户确认的问题：
