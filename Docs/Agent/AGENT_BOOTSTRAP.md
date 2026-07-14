---
system: knowledge-engineering
scope: agent-bootstrap
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/INDEX.md, Docs/Agent/KNOWLEDGE_INVENTORY.md, Docs/Agent/KNOWLEDGE_ENGINEERING_ROADMAP.md
---

# Agent 上岗入口

> 定位：新会话或新 Agent 进入 MiniGameTemplate 时，先读本文。本文只做启动导航，不替代具体 TDD、ADR、Context Pack 或代码阅读。

## 1. 项目一句话

MiniGameTemplate 是一个 Unity 2022 LTS 微信小游戏开发模板，核心是 ScriptableObject 驱动的模块化框架，并以 ShooterGame 作为当前主要业务样例和验证场。

## 2. 当前主线

| 主线 | 状态 | 入口 |
|------|------|------|
| 框架基础设施 | 已形成体系 | `ARCHITECTURE.md`, `MODULE_CARDS/README.md`, `CONV_INDEX.md` |
| Entity-Component 战斗框架 | 已形成体系 | `EC_TDD_INDEX.md` |
| 弹幕/渲染/RuntimeAtlas | 已形成体系，风险高 | `ATLAS_TDD_INDEX.md`, `DEBUG_PLAYBOOK.md` |
| ShooterGame | 当前业务主线 | `SG_TDD_INDEX.md`, `SG_V2_TDD_INDEX.md` |
| AppFlow 导航 | 已完成多轮设计 | `APPFLOW_TDD_INDEX.md`, `SG_V2_DEVICE_ACCEPTANCE.md` 第六部分 |
| 知识工程 | 当前长期任务 | `KNOWLEDGE_ENGINEERING_ROADMAP.md` |

## 3. 事实源优先级

当文档、代码、历史记录互相冲突时，按以下顺序判断：

1. 当前代码与 Unity 编译/运行结果。
2. 活跃 Agent 文档：`Docs/Agent/INDEX.md` 路由到的当前 TDD、ADR、CONV、SO_WORKFLOWS。
3. `Docs/Agent/KNOWLEDGE_INVENTORY.md` 中列为当前事实源的文档。
4. 仍活跃的操作型 Guide，例如 `Docs/Guide/BUILD_MINIGAME.md`。
5. `Docs/Agent/Archive/**` 与 PK 记录。

Archive 只解释历史原因，不直接作为当前实现事实。

## 4. 会话启动流程

普通任务：

1. 读 `Docs/Agent/INDEX.md`。
2. 根据任务读取对应 Context Pack。
3. 读取 Context Pack 标出的 2-8 个核心文档。
4. 读取 `CODE_KNOWLEDGE_MAP.md`，确认代码路径对应的 Module Card、ADR、TDD 与验证项。
5. 用 `rg` 或 CodeGraph 定位代码入口。
6. 若是常规中型改动，使用 `templates/IMPACT_ANALYSIS_TEMPLATE.md` 先做影响面分析；若是跨模块/架构敏感改动，按 `ARCHITECTURE_REVIEW_PROTOCOL.md` 使用 `templates/ARCH_REVIEW_TEMPLATE.md` 审查后再编码。
7. 修改后按 Context Pack / Code Knowledge Map 的“修改后必验”执行验证。
8. 重要变更后按 `KNOWLEDGE_MAINTENANCE.md` 和 `templates/DOC_UPDATE_CHECKLIST.md` 检查知识资产同步，并运行 `Tools/knowledge-sync-check.ps1` 或确认 CI/pre-commit 会执行。

知识工程任务：

1. 读 `KNOWLEDGE_ENGINEERING_ROADMAP.md`。
2. 读 `KNOWLEDGE_INVENTORY.md`。
3. 查看任务看板当前阶段。
4. 创建或更新本阶段产物。
5. 更新路线图状态和下一步。

## 5. 常用 Context Pack

| 任务类型 | 先读 |
|----------|------|
| 战斗逻辑、关卡、胜负、退场 | `CONTEXT_PACKS/ShooterGame_Battle.md` |
| Entity 组件、技能、Buff、碰撞、刷怪 | `CONTEXT_PACKS/EntitySystem.md` |
| SO 配置、新增敌人/技能/Buff/关卡数据 | `CONTEXT_PACKS/SO_Config_Workflow.md` |
| 弹幕、RuntimeAtlas、VFX、飘字、渲染不显示 | `CONTEXT_PACKS/Danmaku_Rendering.md` |
| FairyGUI 面板、UI Controller、导出代码 | `CONTEXT_PACKS/FairyGUI_UI.md` |
| 微信广告、云存储、CDN、构建真机 | `CONTEXT_PACKS/WeChat_Build_Cloud.md` |

## 6. 常用 Module Card

| 模块 | 先读 |
|------|------|
| ShooterGame | `MODULE_CARDS/ShooterGame.md` |
| EntitySystem | `MODULE_CARDS/EntitySystem.md` |
| DanmakuSystem | `MODULE_CARDS/DanmakuSystem.md` |
| Rendering/RuntimeAtlas | `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| VFXSystem | `MODULE_CARDS/VFXSystem.md` |
| AppFlow | `MODULE_CARDS/AppFlow.md` |
| UISystem/FairyGUI | `MODULE_CARDS/UISystem_FairyGUI.md` |
| WeChatBridge | `MODULE_CARDS/WeChatBridge.md` |
| DataSystem/SO/Luban | `MODULE_CARDS/DataSystem_SO_Luban.md` |
| EditorTools | `MODULE_CARDS/EditorTools.md` |

## 7. 核心架构原则

- 游戏逻辑优先通过 ScriptableObject 资产通信，避免场景对象硬引用。
- 框架层 Manager 可以是 Singleton，游戏层不要到处使用 Singleton 访问业务对象。
- 框架模块只能向下依赖，Game 层可以依赖框架。
- 热路径零 GC：Update/Tick/渲染/碰撞中避免 new、LINQ、string 拼接、装箱。
- WebGL/微信小游戏约束优先：不使用线程、阻塞 IO、未验证的平台 API。
- 数据和流程优先配置化，新增敌人、技能、Buff、关卡等优先走 SO 工作流。
- 修改架构前先查 ADR，修改后补充验证或 ADR。

## 8. 目录速记

| 路径 | 用途 |
|------|------|
| `Docs/Agent/` | Agent 当前事实源和技术文档 |
| `Docs/Guide/` | 人类开发者文档 |
| `Docs/Agent/Archive/` | 历史评审、旧方案、验收记录 |
| `Docs/Agent/changes/` | 变更包和 bugfix 记录 |
| `UnityProj/Assets/_Framework/` | 框架代码 |
| `UnityProj/Assets/_Game/` | 当前游戏业务代码与配置 |
| `UnityProj/Assets/_Example/` | 示例项目 |
| `UIProject/` | FairyGUI 工程 |
| `skills/` | 纳入版本管理的 Skill 源目录 |
| `.workbuddy/skills/` / `.codebuddy/skills/` | Agent 工具自动触发 Skill 的运行时目录，按实际工作区存在情况与 `skills/` 保持一致 |

## 9. 编码前检查

开始改代码前，至少回答：

1. 这次任务属于哪个 Context Pack？
2. 是否触碰 ADR？若触碰，读取 `ADR_INDEX.md` 和 `ADR_SCHEMA.md`。
3. 是否触碰热路径？
4. 是否触碰微信/WebGL 约束？
5. 是否应新增或修改 SO，而不是硬编码？
6. 是否影响 FairyGUI 导出代码、SO 资产、场景或构建配置？
7. 修改后如何验证？

## 10. 禁止事项

- 不要把 Archive 中的旧方案当当前事实。
- 不要直接修改 FairyGUI 自动生成代码，业务逻辑写在 `.Logic.cs`。
- 不要在 ScriptableObject 中引用场景对象。
- 不要在热路径中引入 GC 分配。
- 不要绕过 Boot / GameBootstrapper 初始化流程直接假设服务可用。
- 不要在未查 ADR/CONV 的情况下重构框架边界。
- 不要为了完成单点编码而忽略 SO 配置、UI、验证和真机限制。

## 11. 验证入口

| 验证类型 | 入口 |
|----------|------|
| 架构规则 | `Tools -> MiniGame Template -> Validate -> Architecture Check` |
| 资源预算 | `Tools -> MiniGame Template -> Validate -> Asset Audit` |
| SO 配置 | `SO_WORKFLOWS_INDEX.md`, 编辑器 Validator |
| Unity 编译/运行 | `MCP_INTEGRATION.md` 或 Unity Editor |
| **PlayMode 验证** | **`MCP_INTEGRATION.md` §「PlayMode 快速验证工作流」**（打开场景→PlayMode→截图→Console→退出） |
| 渲染/弹幕排查 | `DEBUG_PLAYBOOK.md` |
| 微信构建 | `Docs/Guide/BUILD_MINIGAME.md`, `WECHAT_INTEGRATION.md` |
| 知识工程 | `KNOWLEDGE_ENGINEERING_ROADMAP.md` |
| 架构审查 | `ARCHITECTURE_REVIEW_PROTOCOL.md`, `templates/ARCH_REVIEW_TEMPLATE.md` |
| 知识维护 | `KNOWLEDGE_MAINTENANCE.md`, `templates/DOC_UPDATE_CHECKLIST.md` |
| 知识评估 | `KNOWLEDGE_EVALS.md` |

## 12. 下一步知识工程

P0-P7 初版已完成。后续知识工程进入持续校准：用 `KNOWLEDGE_EVALS.md` 定期跑标准任务，按评估结果修正 Context Pack、Module Card、ADR_SCHEMA、CODE_KNOWLEDGE_MAP、架构审查和维护规则。
