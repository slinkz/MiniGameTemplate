---
system: knowledge-engineering
scope: project-wide-agent-context
status: active
created: 2026-07-14
last_updated: 2026-07-14
owner: Agent + 天命人
related_docs: Docs/Agent/INDEX.md, Docs/Agent/ARCHITECTURE.md, Docs/Agent/ADR_INDEX.md, Docs/Agent/CONV_INDEX.md, Docs/Agent/CODEGRAPH_INTEGRATION.md
---

# MiniGameTemplate 知识工程路线图

> 定位：这是 MiniGameTemplate 知识工程建设的主任务文档。后续跨会话推进时，Agent 应先阅读本文，再根据当前阶段读取对应产物。

## 1. 背景

MiniGameTemplate 的代码、文档、架构决策、工具链和业务样例持续增长。Agent 在有限上下文窗口内，如果仍依赖临时阅读大量文件，很容易只理解当前编码点，而忘记系统边界、历史 ADR、平台限制、踩坑记录和验收规则。

本任务的目标不是“多写文档”，而是建立一套可持续演进的项目认知基础设施，让 Agent 能快速、准确、可验证地获得当前任务所需上下文。

## 2. 总目标

让任意 Agent 在有限上下文内，能快速获得“当前任务所需的全局视角”：

- 知道项目为什么这样设计。
- 知道当前系统边界在哪里。
- 知道一次改动会影响哪些模块、SO、UI、平台约束和验证流程。
- 知道应该读哪些文档，而不是盲目 grep 全目录。
- 知道如何判断自己的设计和实现没有违反项目约束。

## 3. 核心原则

### 3.1 知识工程不是大文档库

本项目需要的是“Agent 可路由、可查询、可验证、可演进的项目认知系统”，不是把所有内容堆进一个更大的文档。

### 3.2 分层存储知识

| 层级 | 类型 | 例子 | 目标 |
|------|------|------|------|
| L0 | 战略记忆 | 项目目标、架构哲学、长期方向 | 让 Agent 理解为什么 |
| L1 | 架构知识 | 系统边界、ADR、生命周期、依赖关系 | 让 Agent 站在全局设计 |
| L2 | 工作流知识 | 新增敌人、技能、UI、SO、构建 | 让 Agent 知道怎么做 |
| L3 | 代码知识 | 类、方法、调用链、符号图谱 | 让 Agent 快速定位实现 |
| L4 | 事故知识 | Debug Playbook、踩坑、回归清单 | 让 Agent 不重复犯错 |
| L5 | 验证工具 | Validator、测试、Unity MCP、CodeGraph | 让 Agent 闭环验证 |

### 3.3 文档必须可路由

每份核心文档都应回答：适用什么任务、对应哪些代码路径、关联哪些 ADR、修改后要验证什么、何时需要更新本文档。

### 3.4 架构知识必须可执行

ADR、规范和模块卡不应只描述历史背景，还应沉淀为可检查的约束：禁止事项、必须事项、影响范围、验收方法、相关工具。

### 3.5 先建立恢复能力，再追求完整度

本任务优先保证“下一次会话能恢复主线”，再逐步补齐模块卡、上下文包、代码映射和自动化检查。

## 4. 目标知识系统形态

```text
Docs/Agent/INDEX.md
  -> AGENT_BOOTSTRAP.md
  -> KNOWLEDGE_ENGINEERING_ROADMAP.md
  -> CONTEXT_PACKS/*.md
  -> MODULE_CARDS/*.md
  -> ADR_INDEX.md / ADR_*.md
  -> CODE_KNOWLEDGE_MAP.md
  -> ARCHITECTURE_REVIEW_PROTOCOL.md
  -> KNOWLEDGE_MAINTENANCE.md
  -> KNOWLEDGE_EVALS.md

CodeGraph / rg / Unity MCP / Validators
  -> 为文档结论提供代码证据与验证闭环
```

## 5. 阶段路线

### P0. 知识资产盘点

**目标**：先知道已有知识在哪里，哪些可信，哪些过期。

**产物**：

- `Docs/Agent/KNOWLEDGE_INVENTORY.md`
- 更新本文的任务状态区。

**要盘点的知识资产**：

| 类型 | 目录/文件 | 判断重点 |
|------|-----------|----------|
| 入口索引 | `Docs/Agent/INDEX.md` | 路由是否覆盖当前任务类型 |
| 架构总览 | `Docs/Agent/ARCHITECTURE.md` | 是否仍匹配当前代码 |
| ADR | `ADR_INDEX.md`, `ADR_*.md` | 状态、适用范围、废弃关系 |
| 规范 | `CONV_INDEX.md`, `CONV_*.md` | 是否可执行、是否重复 |
| TDD/GDD | `SG_*`, `EC_*`, `APPFLOW_*`, `ATLAS_*` | 已实现 vs 计划 |
| 工作流 | `SO_WORKFLOWS_*`, `EDITOR_TOOLS_*` | 是否能指导实际操作 |
| Debug | `DEBUG_PLAYBOOK.md` | 是否覆盖高频问题 |
| 工具 | `CODEGRAPH_INTEGRATION.md`, `MCP_INTEGRATION.md` | 当前可用性 |
| 归档 | `Archive/**`, `changes/**` | 哪些仍有参考价值 |
| Skills | `skills/**` + 实际运行时目录（如父目录 `.workbuddy/skills/**` 或 `.codebuddy/skills/**`） | 与 Docs/Agent 的职责边界 |

**完成标准**：

- 有一份“活跃知识 / 归档知识 / 待校验知识”清单。
- Agent 能判断哪些文档可作为当前事实，哪些只能作为历史背景。
- 明确下一阶段优先补哪些知识缺口。

### P1. Agent 上岗入口

**目标**：解决“新 Agent 进来先读什么”的问题。

**产物**：

- `Docs/Agent/AGENT_BOOTSTRAP.md`
- `Docs/Agent/CONTEXT_PACKS/`
- 更新 `Docs/Agent/INDEX.md`

**AGENT_BOOTSTRAP 内容要求**：

- 项目一句话定位。
- 当前主线。
- 核心架构原则。
- 目录结构。
- 任务路由方式。
- 编码前必读规则。
- 验证方式。
- 禁止事项。

**首批 Context Pack**：

```text
Docs/Agent/CONTEXT_PACKS/
├── ShooterGame_Battle.md
├── EntitySystem.md
├── Danmaku_Rendering.md
├── FairyGUI_UI.md
├── SO_Config_Workflow.md
└── WeChat_Build_Cloud.md
```

**Context Pack 模板**：

```text
# Context Pack: <Name>

## 适用任务
## 必读文档
## 关键代码入口
## 关键 SO / 配置路径
## 关键 ADR / 约束
## 常见坑
## 修改后必验
```

**完成标准**：Agent 接到常见任务时，可以通过 Context Pack 将阅读范围压到 3-8 个文件。

### P2. 模块知识卡片化

**目标**：让 Agent 通过固定结构理解模块边界，而不是靠读大量散文拼图。

**产物**：

```text
Docs/Agent/MODULE_CARDS/
├── ShooterGame.md
├── EntitySystem.md
├── DanmakuSystem.md
├── Rendering_RuntimeAtlas.md
├── VFXSystem.md
├── AppFlow.md
├── UISystem_FairyGUI.md
├── DataSystem_SO_Luban.md
├── WeChatBridge.md
└── EditorTools.md
```

**模块卡模板**：

```text
# Module Card: <Name>

## 1. 模块职责
## 2. 不负责什么
## 3. 入口类 / 核心类型
## 4. 数据流
## 5. 生命周期
## 6. 依赖关系
## 7. 关键 SO / 配置路径
## 8. 关键 ADR
## 9. 热路径 / 性能约束
## 10. 常见错误
## 11. 修改前必读
## 12. 修改后必验
```

**完成标准**：核心模块都有一页结构化总览，并能连接到代码路径、文档、ADR、验证方式。

### P3. ADR 可执行化

**目标**：让 ADR 从历史说明升级为设计约束数据库。

**产物**：

- `Docs/Agent/ADR_SCHEMA.md`
- 更新 `Docs/Agent/ADR_INDEX.md`
- 逐步为关键 ADR 增加适用范围、约束和验证字段。

**建议 ADR 扩展字段**：

```text
Status:
AppliesTo:
Decision:
Constraints:
Supersedes:
Verification:
RelatedDocs:
RelatedTests:
RelatedPitfalls:
```

**优先处理 ADR**：

| ADR | 主题 | 优先级 | 原因 |
|-----|------|--------|------|
| ADR-028 | RuntimeAtlasSystem 统一管线 | 高 | 渲染系统影响面大 |
| ADR-031 | RuntimeAtlas 深化 | 高 | 微信小游戏内存与 DC 关键 |
| ADR-033 | Entity-Component 框架 | 高 | 战斗系统核心 |
| ADR-034 | AppFlow 栈式导航系统 | 高 | UI/场景流转核心 |
| ADR-035 | 战斗退场生命周期 | 高 | 退场清理容易回归 |
| ADR-036 | 飘字系统统一 | 中 | 渲染与反馈链路 |

**完成标准**：Agent 能根据 ADR 判断设计边界，关键架构改动能明确是否需要新增 ADR。

### P4. 代码图谱与文档映射闭环

**目标**：打通“任务 -> 文档 -> 代码 -> 验证”。

**产物**：

- `Docs/Agent/CODE_KNOWLEDGE_MAP.md`
- `Docs/Agent/templates/IMPACT_ANALYSIS_TEMPLATE.md`
- 更新 `Docs/Agent/INDEX.md` 的代码路径映射。

**映射格式**：

```text
代码路径/符号
  -> 所属模块卡
  -> 对应 TDD/GDD
  -> 相关 ADR
  -> 相关 SO 工作流
  -> 必跑验证
  -> 常见坑/Debug 文档
```

**完成标准**：常见代码路径能反查文档，常见任务能正查代码入口，Agent 编码前能输出影响面分析。

### P5. 架构审查流程

**目标**：让 Agent 在中大型任务编码前先站到全局视角。

**产物**：

- `Docs/Agent/ARCHITECTURE_REVIEW_PROTOCOL.md`
- `Docs/Agent/templates/ARCH_REVIEW_TEMPLATE.md`
- 可选：后续沉淀为 `architecture-review` Skill。

**架构审查问题清单**：

```text
1. 这次需求属于哪个系统？
2. 是否已有模块能承载？
3. 是否应该扩展 SO，而不是硬编码？
4. 是否触碰热路径？
5. 是否触碰 WebGL/微信约束？
6. 是否违反模块依赖方向？
7. 是否需要新增 ADR？
8. 是否需要新增 Validator？
9. 修改后怎么验收？
```

**完成标准**：中大型任务编码前有影响面分析，架构变更能沉淀为 ADR 或模块卡更新。

### P6. 知识维护机制

**目标**：防止知识库变成漂亮但过期的陈列品。

**产物**：

- `Docs/Agent/KNOWLEDGE_MAINTENANCE.md`
- `Docs/Agent/templates/DOC_UPDATE_CHECKLIST.md`
- `Docs/Agent/changes/README.md`

**每次重要变更应检查**：

```text
- 是否影响模块卡？
- 是否影响 ADR？
- 是否影响 Context Pack？
- 是否影响 SO 工作流？
- 是否产生新坑？
- 是否需要更新 Debug Playbook？
- 是否需要新增验证项？
```

**建议变更包结构**：

```text
Docs/Agent/changes/YYYY-MM-DD-topic/
├── SUMMARY.md
├── IMPACT.md
├── VALIDATION.md
└── DOC_UPDATES.md
```

**完成标准**：代码变更后相关知识不会长期漂移，Agent 能找到最近一次相关变更。

### P7. 知识工程评估体系

**目标**：判断知识工程是否真的提升 Agent 工作质量。

**产物**：

- `Docs/Agent/KNOWLEDGE_EVALS.md`
- 一组标准评估任务。

**评估维度**：

| 维度 | 问题 |
|------|------|
| 路由准确率 | Agent 是否读到了正确文档 |
| 上下文效率 | 是否避免读大量无关内容 |
| 设计一致性 | 是否遵守 ADR / CONV / WebGL 约束 |
| 影响面完整度 | 是否识别相关模块、SO、UI、测试 |
| 踩坑复发率 | 是否重复犯已记录问题 |
| 验证闭环率 | 是否给出可执行验收方案 |

**首批评估任务**：

```text
1. 新增一种敌人
2. 新增一个技能
3. 修改碰撞逻辑
4. 修改 FairyGUI 面板
5. 修改微信云存储
6. 调整 RuntimeAtlas
7. 新增关卡
8. 新增 Buff
9. 调试渲染不显示
10. 做一次架构重构评审
```

**完成标准**：每个任务都有推荐上下文包，Agent 能给出影响面和验证方案。

## 6. 当前任务看板

| 阶段 | 状态 | 当前目标 | 下一步 |
|------|------|----------|--------|
| P0 知识资产盘点 | 初版完成 | 已建立知识清单与事实源边界 | P1 创建 `AGENT_BOOTSTRAP.md` |
| P1 Agent 上岗入口 | 初版完成 | 已建立上岗入口与首批 Context Pack | P2 创建 `MODULE_CARDS/` |
| P2 模块知识卡 | 初版完成 | 已建立 10 张核心模块卡 | P3 创建 `ADR_SCHEMA.md` |
| P3 ADR 可执行化 | 初版完成 | 已建立 ADR Schema 与 7 条优先 ADR 可执行摘要 | P4 创建 `CODE_KNOWLEDGE_MAP.md` |
| P4 代码映射闭环 | 初版完成 | 已建立代码路径到知识资产与验证项映射 | P5 创建 `ARCHITECTURE_REVIEW_PROTOCOL.md` |
| P5 架构审查流程 | 初版完成 | 已建立中大型改动前的架构审查协议与模板 | P6 创建 `KNOWLEDGE_MAINTENANCE.md` |
| P6 知识维护机制 | 初版完成 | 已建立知识维护协议、更新清单与 changes 规范 | P7 创建 `KNOWLEDGE_EVALS.md` |
| P7 评估体系 | 初版完成 | 已建立 10 个标准评估任务与评分规则 | 后续按评估结果持续校准知识资产 |
| P8 文档收敛与归档 | P8.2 完成 | 已归档旧微信手机导出指南、Danmaku Demo 决策、AppFlow 独立验收、S5.4~S5.6 独立验收、Guide Danmaku 系列、Framework Modules 系列 | 后续仅保留操作型 Guide；架构/模块长文按需由 Agent 基于当前知识工程生成 |

## 7. 推荐推进顺序

### 第一批：入口和恢复能力

1. 创建 `KNOWLEDGE_INVENTORY.md`。
2. 创建 `AGENT_BOOTSTRAP.md`。
3. 创建 `CONTEXT_PACKS/` 和 2-3 个高频上下文包。
4. 更新 `Docs/Agent/INDEX.md`，把知识工程入口加入任务路由。

### 第二批：模块地图

1. 创建 `MODULE_CARDS/`。
2. 先完成 `ShooterGame.md`、`EntitySystem.md`、`DanmakuSystem.md`。
3. 再完成 `Rendering_RuntimeAtlas.md`、`AppFlow.md`、`UISystem_FairyGUI.md`。

### 第三批：约束和影响面

1. 创建 `ADR_SCHEMA.md`。
2. 可执行化 ADR-033、ADR-034、ADR-035。
3. 创建 `CODE_KNOWLEDGE_MAP.md`。
4. 创建影响面分析模板。

### 第四批：维护和评估

1. 创建 `ARCHITECTURE_REVIEW_PROTOCOL.md`。
2. 创建 `KNOWLEDGE_MAINTENANCE.md`。
3. 创建 `KNOWLEDGE_EVALS.md`。
4. 用 10 个标准任务验证知识系统。

## 8. 跨会话恢复协议

后续任意会话继续知识工程任务时，Agent 应按以下顺序恢复：

1. 阅读本文。
2. 查看“当前任务看板”。
3. 查看最近创建或修改的知识工程文档。
4. 若任务涉及具体模块，读取对应 Context Pack 或 Module Card。
5. 开始前说明本次推进的阶段、目标和预计产物。
6. 完成后更新本文的任务看板，或在对应文档中写明下一步。

## 9. Definition of Done

本知识工程任务整体完成时，应满足：

- Agent 有统一上岗入口。
- 常见任务有 Context Pack。
- 核心模块有 Module Card。
- 关键 ADR 可执行化。
- 常见代码路径可反查文档、ADR 和验证项。
- 中大型任务有架构审查协议。
- 文档维护有检查清单。
- 有一组评估任务能验证 Agent 是否真正获得全局视角。

## 10. 风险与对策

| 风险 | 表现 | 对策 |
|------|------|------|
| 文档膨胀 | 新文档越来越多，入口反而变乱 | 所有新文档必须挂到 INDEX 或本文 |
| 内容过期 | 文档与代码不一致 | P6 建立维护清单和变更包 |
| 过度形式化 | Agent 花太多时间填模板 | 模板只服务路由、边界和验证，不写空话 |
| 只重文档不重验证 | 文档写得好但不可证明 | 每份核心知识都绑定验证方式 |
| 上下文包太大 | Agent 仍然读不动 | Context Pack 只做导航，不重复长文 |
| ADR 变成历史堆积 | 新旧决策冲突 | ADR_SCHEMA 增加 Supersedes 和 Status |

## 11. 下一步

P0-P8.2 初版已完成。后续进入持续校准：

1. 使用 `Docs/Agent/KNOWLEDGE_EVALS.md` 定期运行 10 个标准评估任务。
2. 记录每次评估的分数、漏项和需修正文档。
3. 按评估结果反向修正 Context Pack、Module Card、Code Knowledge Map、ADR_SCHEMA、架构审查和维护规则。
4. 当项目新增高频任务或核心模块时，补充新的评估任务。
5. 继续控制文档膨胀：Guide 只保留操作型入口；架构、模块、Danmaku、渲染等长期事实统一沉淀到 Agent 知识工程。
