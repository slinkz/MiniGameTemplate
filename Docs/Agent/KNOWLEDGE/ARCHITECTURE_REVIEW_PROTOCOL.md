---
system: knowledge-engineering
scope: architecture-review-protocol
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/AGENT_BOOTSTRAP.md, Docs/Agent/KNOWLEDGE/CODE_KNOWLEDGE_MAP.md, Docs/Agent/ADR/ADR_SCHEMA.md, Docs/Agent/templates/ARCH_REVIEW_TEMPLATE.md
---

# Architecture Review Protocol

> 定位：这是 P5 的架构审查协议。它规定 Agent 在中大型任务编码前如何从全局视角判断模块边界、ADR 约束、数据/资产影响、平台限制和验证闭环。

## 1. 什么时候必须使用

以下任一条件命中时，必须先执行本协议，再编码：

| 触发条件 | 示例 |
|----------|------|
| 跨模块改动 | 同时影响 ShooterGame、EntitySystem、UI、AppFlow、Rendering |
| 触碰框架边界 | 修改 `_Framework/**`、导航、渲染、Entity 核心、生命周期协议 |
| 触碰 ADR 约束 | ADR-028/031/032/033/034/035/036 的 AppliesTo 命中 |
| 触碰热路径 | Update、Tick、渲染、碰撞、对象池、RuntimeAtlas、Entity 组件 |
| 触碰平台约束 | WebGL、微信小游戏、IL2CPP stripping、云存储、CDN、广告 SDK |
| 触碰数据/资产协议 | ScriptableObject、FairyGUI 导出、Luban、Scene、Prefab、link.xml |
| 改变用户流程 | Boot、Main、Battle、Pause、Retry、Return、Victory、Defeat |
| 需要新增通用能力 | 新框架服务、新 Manager、新事件协议、新验证器 |

小型局部改动可以不填写模板，但仍要口头完成 ADR、热路径、平台和验证检查。

## 2. 输入材料

执行审查前按顺序读取：

1. `Docs/Agent/INDEX.md`：确认任务路由。
2. 对应 `CONTEXT_PACKS/*.md`：确认任务上下文和必读文档。
3. 对应 `MODULE_CARDS/*.md`：确认模块职责、边界、入口和必验项。
4. `Docs/Agent/KNOWLEDGE/CODE_KNOWLEDGE_MAP.md`：确认代码路径到文档、ADR、验证项的映射。
5. `Docs/Agent/ADR/ADR_SCHEMA.md`：确认 ADR 状态、AppliesTo、Constraints、Verification、ChangeProtocol。
6. 必要时读取 `SYSTEMS/CONV/CONV_INDEX.md`、`SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md`、`DEBUG_PLAYBOOK.md`、`TOOLS/MCP_INTEGRATION.md`、`PLATFORM/WECHAT_INTEGRATION.md`。

Archive 只用于解释历史背景，不直接作为当前事实源。

## 3. 审查分级

| 等级 | 判定 | 输出 |
|------|------|------|
| Level 0 局部小改 | 单文件、无 ADR、无热路径、无资产/平台影响 | 在回复中简述影响面和验证 |
| Level 1 常规功能 | 单模块或少量文件，涉及 SO/UI/验证 | 使用 `templates/IMPACT_ANALYSIS_TEMPLATE.md` |
| Level 2 架构敏感 | 跨模块、触碰 ADR/框架边界/热路径/平台约束 | 使用 `templates/ARCH_REVIEW_TEMPLATE.md` |
| Level 3 架构决策 | 新通用机制、改变模块依赖、替代既有 ADR、引入长期约束 | 先写/更新 ADR，再实现 |

当等级不确定时，按更高一级处理。

## 4. 审查流程

### 4.1 定位任务

回答：

```text
1. 需求目标是什么？
2. 非目标是什么？
3. 属于哪些 Context Pack？
4. 触碰哪些 Module Card？
5. 是否已有模块可以承载？
```

若已有模块能承载，优先扩展现有模块；不要为了单点需求新增平行体系。

### 4.2 确认代码路径

使用 `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md`、`rg` 或 CodeGraph 找到入口：

```text
任务 -> 代码路径/符号 -> Module Card -> Context Pack -> TDD/Workflow -> ADR -> 验证项
```

如果代码路径未出现在 `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md`，但属于核心模块，P6 前先在审查结论中记录“需要补映射”，实现后同步更新。

### 4.3 检查模块边界

重点判断：

- 是否让 Game 层规则反向污染 Framework 层。
- 是否绕过 Boot / GameBootstrapper / AppFlow。
- 是否把业务流程硬塞进 Manager 或 Singleton。
- 是否让 ScriptableObject 引用场景对象。
- 是否修改 FairyGUI 自动生成代码。
- 是否新增与既有系统并行的重复路径。

命中任何一项都需要调整方案，或升级到 Level 3 写 ADR。

### 4.4 检查 ADR

对每条命中的 ADR 填写：

```text
ADR-ID:
Status:
ImplementationStatus:
AppliesTo 是否命中:
Constraints 是否满足:
Verification 需要执行什么:
是否需要更新或新增 ADR:
```

如果 ADR 为 Superseded，不得作为当前约束；应追到替代 ADR。

### 4.5 检查数据、资产与平台

至少覆盖：

- ScriptableObject 类型、路径、CreateAssetMenu、Validator。
- FairyGUI 包、发布资源、Binder、`.Logic.cs`。
- Scene 引用、Prefab、Texture、Audio、RuntimeAtlas 资源。
- Luban 表、生成脚本、运行时加载。
- WebGL/微信小游戏：线程、阻塞 IO、反射/IL2CPP、网络域名、广告/云能力。
- `link.xml` 与 stripping 风险。

### 4.6 设计验证闭环

验证必须对应影响面，而不是只写“编译通过”。

常用验证入口：

| 影响面 | 验证 |
|--------|------|
| Unity 代码 | 编译 0 error |
| 架构规则 | `Tools -> MiniGame Template -> Validate -> Architecture Check` |
| SO/资产 | SO Validator、Missing Reference 检查 |
| 战斗流程 | Victory/Defeat/Retry/PauseQuit/Return |
| Entity/碰撞 | Spawn/Despawn、Tick 顺序、碰撞/冷却、零 GC |
| RuntimeAtlas/渲染 | DrawCall、RT 像素、UV、Game View 可见、Profiler |
| UI/AppFlow | Push/Pop/Replace、Suspend/Resume、按钮事件、冷启动清栈 |
| 微信平台 | 微信开发者工具、真机、云函数/广告/CDN |

## 5. 停止条件

出现以下情况时，先停下，不直接编码：

| 停止条件 | 下一步 |
|----------|--------|
| 当前方案违反 Accepted ADR | 修改方案，或提出 ADR 更新 |
| 需要改变模块依赖方向 | 写 ADR 或架构说明 |
| 需要新增通用框架能力 | 写 ADR 或模块卡扩展 |
| 现有文档与代码事实冲突 | 先用代码确认事实并更新文档 |
| 验证手段缺失 | 先补 Validator / PlayMode / 手动验收项 |
| 资产数据来源不清 | 先确认 SO/FairyGUI/Luban/Scene 责任边界 |
| 涉及微信真机但无法验证 | 明确本次只到代码级或开发者工具级，记录剩余风险 |

停止不是拖延任务，而是防止把不确定性写进架构。

## 6. 何时新增或更新 ADR

需要新增 ADR：

- 引入新的长期架构约束。
- 替代旧机制或旧 ADR。
- 改变模块依赖方向。
- 新增跨模块协议、生命周期、事件通道或渲染管线。
- 做出会影响后续多个任务的取舍。

需要更新 ADR：

- 实现状态从 Partial/NeedsVerification 变为 Implemented。
- 代码事实与 ADR 原文不一致。
- Verification 变更。
- Supersedes/Extends 关系变化。

不需要 ADR：

- 局部 bugfix。
- 单个 SO/关卡/技能配置新增。
- 不改变模块职责的小 UI 文案或布局修正。
- 既有 ADR 已明确覆盖的普通扩展。

## 7. 何时更新知识资产

实现后按影响面更新：

| 影响 | 需要更新 |
|------|----------|
| 新增核心代码路径 | `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` |
| 改变模块职责/边界 | 对应 Module Card |
| 改变常见任务启动路径 | 对应 Context Pack |
| 改变架构决策 | ADR 原文与 `ADR/ADR_SCHEMA.md` |
| 改变 SO 创建/验证流程 | `SO_WORKFLOWS_*` |
| 新增常见坑或排查步骤 | `DEBUG_PLAYBOOK.md` 或 P6 changes 包 |
| 改变验证入口 | `AGENT_BOOTSTRAP.md`、`INDEX.md` 或模板 |

P6 会把这些规则固化为维护清单；P5 阶段先在审查结论中明确更新项。

## 8. 输出格式

Level 1 使用 `templates/IMPACT_ANALYSIS_TEMPLATE.md`。

Level 2/3 使用 `templates/ARCH_REVIEW_TEMPLATE.md`，并在编码前给出结论：

```text
结论：
- 审查等级：
- 可以直接编码：是/否
- 需要先补 ADR：是/否
- 需要先补验证器/资产：是/否
- 主要风险：
- 本次必跑验证：
- 实现后需更新的知识资产：
```

只有当“可以直接编码”为“是”时，才进入实现。

## 9. 与代码审查的关系

架构审查发生在编码前，代码审查发生在编码后。

- 架构审查回答“该不该这么改、会影响哪里、如何验证”。
- 代码审查回答“实现是否正确、是否有 bug、是否符合约束”。

二者都应引用同一组事实源：Context Pack、Module Card、ADR、Code Knowledge Map、当前代码与验证结果。
