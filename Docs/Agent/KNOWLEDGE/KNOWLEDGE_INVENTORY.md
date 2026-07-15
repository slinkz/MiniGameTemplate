---
system: knowledge-engineering
scope: knowledge-inventory
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md, Docs/Agent/INDEX.md
---

# MiniGameTemplate 知识资产盘点

> 定位：这是知识工程 P0 的盘点文档，用来区分“当前事实”“历史参考”“待校验知识”，并为后续 Agent Bootstrap、Context Pack、Module Card 提供输入。

## 1. 使用规则

Agent 在使用本盘点时应遵循：

1. 优先信任“当前事实源”。
2. 归档与 PK 记录只作为历史推理材料，不直接当作当前实现事实。
3. 当文档之间状态冲突时，以当前代码、最新活跃 TDD/ADR、Unity 编译/运行验证为准。
4. 涉及具体任务时，先走 `Docs/Agent/INDEX.md` 路由，再读对应专题文档。
5. 本文只做知识资产地图，不替代具体 TDD、ADR、规范和实现文档。

## 2. 盘点摘要

| 类别 | 当前观察 | 使用结论 |
|------|----------|----------|
| Agent 活跃文档 | `Docs/Agent/` 顶层按 ADR/EC/SG/SO/APPFLOW/CONV 等主题组织，数量较多 | 当前项目知识主入口 |
| Guide 文档 | `Docs/Guide/` 面向人类开发者，保留少量上手/构建/FAQ 等操作型文档 | 不承载架构事实；Agent 应优先读 Agent 文档 |
| Archive 文档 | `Docs/Agent/Archive/` 中 ShooterGame、PK、验收、历史设计材料最多 | 历史参考，不直接作为当前事实 |
| changes 文档 | `Docs/Agent/changes/` 有少量变更包和 bugfix 记录 | 用于追溯近期改动和踩坑 |
| Skills | 仓库实际路径为 `skills/`，包含 8 个技能包 | 操作 SOP 和专家经验源 |
| Skills 双路径 | `skills/` 是纳入版本管理的源目录；运行时目录按 Agent 工具可能是父目录 `.workbuddy/skills/` 或 `.codebuddy/skills/` | 后续需建立同步/校验规则，确保两处内容一致 |
| 索引新鲜度 | `Docs/Agent/INDEX.md` 已更新至 2026-07-14，统计为 118 活跃 + 90 归档 | P6 已建立统计规则，后续新增/归档文档时维护 |
| 父目录工作台 | `C:\workspace\mini-game-template` 下存在 `.workbuddy/`, `.tasks/`, `.codegraph/`, `output/` 等非仓库材料 | 可作历史和本机工具背景；当前事实源仍以仓库 `Docs/Agent/**` 为准 |

## 3. 当前事实源

这些文档可作为 Agent 理解当前项目的第一可信来源，但仍需结合代码和验证工具确认实现状态。

| 文档/目录 | 可信范围 | 使用方式 |
|-----------|----------|----------|
| `Docs/Agent/INDEX.md` | 任务路由、代码到文档映射、概念速查 | 每次任务优先读取 |
| `Docs/Agent/ARCHITECTURE.md` | 全局架构、启动流程、模块依赖、渲染/弹幕架构 | 做全局设计前读取 |
| `Docs/Agent/ADR/ADR_INDEX.md` + `ADR_*.md` | 架构决策、取舍、废弃关系 | 判断设计边界；需 P3 可执行化 |
| `Docs/Agent/SYSTEMS/CONV/CONV_INDEX.md` + `CONV_*.md` | 命名、编码、平台、工作流约束 | 编码和审查前读取 |
| `Docs/Agent/SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md` + 子文档 | SO 类型、路径、创建流程 | 新增配置资产时读取 |
| `Docs/Agent/SYSTEMS/EC_TDD/EC_TDD_INDEX.md` + 子文档 | Entity-Component 框架设计 | 修改战斗框架/组件时读取 |
| `Docs/Agent/SHOOTER_GAME/TDD/SG_TDD_INDEX.md` + 子文档 | ShooterGame V1 技术设计 | 修改 SG 核心流程时读取 |
| `Docs/Agent/SHOOTER_GAME/V2_TDD/SG_V2_TDD_INDEX.md` + 子文档 | ShooterGame V2 技能系统与架构升级 | 修改技能/Buff/DOT/道具/生命周期时读取 |
| `Docs/Agent/SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md` + 子文档 | 栈式导航、面板 Suspend/Resume、场景策略 | 修改 UI 流/场景流时读取 |
| `Docs/Agent/DEBUG_PLAYBOOK.md` | 渲染、RuntimeAtlas、弹幕排查方法 | 调试“不显示/不对/性能异常”时读取 |
| `Docs/Agent/PLATFORM/WECHAT_INTEGRATION.md` | 微信广告、云开发、CDN、Dev Server 等 | 修改微信平台集成时读取 |
| `Docs/Agent/TOOLS/CODEGRAPH_INTEGRATION.md` | 代码图谱检索策略 | 大范围代码理解时读取 |
| `Docs/Agent/TOOLS/MCP_INTEGRATION.md` | Unity MCP 操作约束 | 需要 Unity 编译/截图/PlayMode 时读取 |
| `Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md` | 知识工程主线任务 | 推进本知识工程时读取 |
| `Docs/Agent/KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md` | 中大型改动前的架构审查流程 | 跨模块、框架边界、ADR、热路径或平台敏感任务前读取 |
| `Docs/Agent/KNOWLEDGE/KNOWLEDGE_MAINTENANCE.md` | 重要变更后的知识资产同步规则 | 代码、资产、ADR、Skill、changes 或索引变更后读取 |
| `Docs/Agent/KNOWLEDGE/KNOWLEDGE_EVALS.md` | 知识工程评估体系 | 定期检查 Agent 路由、影响面、ADR、验证和维护闭环 |

## 4. 专题文档族清单

| 文档族 | 文件范围 | 当前用途 | 后续知识工程动作 |
|--------|----------|----------|------------------|
| ADR | `ADR/ADR_INDEX.md`, `ADR_01~06_*.md` | 决策记录 | P3 增加 Schema、AppliesTo、Constraints、Verification |
| CONV | `SYSTEMS/CONV/CONV_INDEX.md`, `CONV_01~04_*.md` | 项目规范 | P1 纳入 Bootstrap 必读；P5 作为架构审查约束 |
| EC_TDD | `SYSTEMS/EC_TDD/EC_TDD_INDEX.md`, `EC_TDD_01~08_*.md` | Entity 框架设计 | P2 生成 `MODULE_CARDS/EntitySystem.md` |
| SO_WORKFLOWS | `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_INDEX.md`, `SO_WORKFLOWS_01~05_*.md` | SO 配置流程 | P1 生成 `CONTEXT_PACKS/SO_Config_Workflow.md` |
| APPFLOW | `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md`, `APPFLOW_TDD_01~05_*.md`；验收统一见 `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分 | 导航系统 | P2 生成 `MODULE_CARDS/AppFlow.md` |
| ATLAS | `SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX.md`, `ATLAS_TDD_01~02_*.md` | RuntimeAtlas 设计 | P2 生成 Rendering/RuntimeAtlas 模块卡 |
| OBB | `SYSTEMS/OBB_TDD/OBB_TDD_INDEX.md`, `OBB_TDD_01~02_*.md` | OBB 碰撞 | P4 加入碰撞代码映射 |
| SG GDD/TDD | `SHOOTER_GAME/SG_GAME_DESIGN.md`, `SHOOTER_GAME/SG_UI_DESIGN.md`, `SG_TDD_*` | ShooterGame V1 | P2 生成 `MODULE_CARDS/ShooterGame.md` |
| SG_V2 | `SG_GDD_*`, `SG_V2_TDD_*`, `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` | 技能系统、验收 | P1/P2 纳入 ShooterGame Context Pack |
| SG_TOOLS | `SG_TOOLS_TDD_*` | 波次编辑器、Debug 工具 | P4 纳入编辑器工具映射 |
| EDITOR_TOOLS | `EDITOR_TOOLS_MANUAL_*` | 编辑器工具使用手册 | P1/P4 纳入工具 Context Pack |
| Debug/Integration | `DEBUG_PLAYBOOK.md`, `TOOLS/MCP_INTEGRATION.md`, `TOOLS/CODEGRAPH_INTEGRATION.md`, `PLATFORM/WECHAT_INTEGRATION.md` | 工具与排查 | P1 纳入 Bootstrap 工具入口 |

## 5. Guide 文档边界

`Docs/Guide/` 面向人类开发者，只保留少量上手、构建、FAQ 等操作型材料。架构、模块、弹幕、渲染等长期事实不再由 Guide 承载。

| 文档 | 当前用途 | 使用边界 |
|------|----------|----------|
| `README.md` | 人类操作型文档入口、项目定位、技术栈 | 可读；Agent 任务仍应转向 `Docs/Agent/INDEX.md` |
| `GETTING_STARTED.md` | 环境搭建、首次运行 | 用于上手和环境问题 |
| `BUILD_MINIGAME.md` | 微信小游戏构建流程 | 构建任务可读；平台细节还需看 `PLATFORM/WECHAT_INTEGRATION.md` |
| `ARCHITECTURE_OVERVIEW.md` | 人类版架构概览 | 只作轻量背景，细节以 `ARCHITECTURE.md` 为准 |
| `FRAMEWORK_MODULES*.md` | 早期框架模块 API 和说明 | 已归档为历史参考；当前入口见 `MODULE_CARDS/README.md` 与 `ARCHITECTURE.md` |
| `DANMAKU_*.md` | 早期弹幕系统专题 | 已归档为历史参考；当前入口见 `CONTEXT_PACKS/Danmaku_Rendering.md` 与 `MODULE_CARDS/DanmakuSystem.md` |
| `FAQ.md` | 常见问题 | 辅助排错 |
| `微信小游戏导出到手机完整指南.md` | 旧版真机导出流程 | 已归档为历史参考；当前构建入口以 `BUILD_MINIGAME.md` 与 `PLATFORM/WECHAT_INTEGRATION.md` 为准 |

## 6. Skills 盘点

仓库实际存在 `skills/` 目录，这是纳入版本管理的 Skill 源目录。Agent 工具自动触发 Skill 时，可能读取工作区运行时目录：本机历史 WorkBuddy 环境为父目录 `.workbuddy/skills/`，部分旧文档/工具可能使用 `.codebuddy/skills/`。因此它们不是互斥路径，而是“仓库源目录 + 运行时触发目录”的双路径约定，内容应保持一致。

| Skill | 路径 | 职责 | 知识工程定位 |
|-------|------|------|--------------|
| `coding-standards` | `skills/coding-standards/` | 编码规则、平台规则、Bug 修复 SOP | P1 Bootstrap 必读规则候选 |
| `code-review-checklist` | `skills/code-review-checklist/` | 审查清单、已知坑、维护指南 | P5/P6 审查与维护输入 |
| `doc-maintenance` | `skills/doc-maintenance/` | 文档 frontmatter、索引模板、维护规则 | P6 的基础材料 |
| `fairygui-tools` | `skills/fairygui-tools/` | FairyGUI XML、工作流、C# 强类型模板、XML 校验、业务绑定回流检查 | P1 Context Pack 输入 |
| `luban-config` | `skills/luban-config/` | Luban 表结构、脚本、项目布局 | P1 SO/Config Context Pack 输入 |
| `task-tracker` | `skills/task-tracker/` | 跨会话任务模板 | 可与本路线图协同 |
| `tdd-pk-review` | `skills/tdd-pk-review/tdd-pk-review/` | PK 对抗评审模板与收敛标准 | P5 架构审查流程输入 |
| `vfx-creator` | `skills/vfx-creator/` | VFX 工作流、验证清单、提示模板 | VFX 模块卡输入 |
| `game-designer` | `skills/game-designer/` | 玩法、关卡、敌人、技能、Buff、道具、经济和数值设计工作流 | P9 策划 Agent 入口 |
| `ui-designer` | `skills/ui-designer/` | UI/UX、界面、组件、状态矩阵、动效、文案和 FairyGUI handoff 工作流 | P9 UI Agent 入口 |
| `asset-pipeline` | `skills/asset-pipeline/` | sprite、VFX、UI icon、audio、font 等资产生产、接入和验收工作流 | P9 资产 Agent 入口 |
| `wechat-minigame-plugin-update` | `skills/wechat-minigame-plugin-update/` | WeChat Mini Game Unity SDK update SOP: official version endpoint, embedded package sync, DLL lock handling, duplicate runtime recovery, MCP compile gate | Platform/build maintenance Skill |

## 7. Archive 与 changes 边界

### 7.1 Archive

`Docs/Agent/Archive/` 是历史知识库，主要包含 PK、验收、旧方案、问题复盘。它的价值在于解释“为什么后来变成这样”，但不能直接作为当前实现事实。

高价值归档主题：

| 归档主题 | 价值 | 使用方式 |
|----------|------|----------|
| `Archive/ShooterGame/**` | SG 各阶段验收、PK、旧问题 | 当前文档不够解释原因时再读 |
| `Archive/EntityComponent/**` | Entity 框架多轮 PK | P3/P5 做 ADR/架构审查时可参考 |
| `Archive/AppFlow/**` | AppFlow PK 评审 | 导航系统权衡背景 |
| `Archive/General/**` | 审查、重构、问题记录 | 需要追溯历史设计时使用 |
| `Archive/VFX/**` | VFX 研究 | VFX 模块卡补充材料 |
| `Archive/OBB/**` | OBB 问题 | 碰撞专题参考 |
| `Archive/Guide/**` | 从 Guide 降级归档的旧教程/旧决策 | 只作历史参考；当前入口以 `Docs/Guide/README.md` 和 Agent 知识工程路由为准 |

### 7.2 changes

`Docs/Agent/changes/` 当前已有少量变更包和 bugfix 记录：

- `2026-04-21-laser-atlas-bugfix`
- `2026-04-21-phase4-deep-integration`
- `2026-04-23-obb-obstacle`
- `2026-04-29-fairygui-click-fix`
- `2026-04-30-p1.0-enumcamp-migration`
- `P2.4_P2.5_ACCEPTANCE_CHECKLIST.md`

这些是知识维护机制的既有样例。P6 已新增 `Docs/Agent/changes/README.md`：旧包保留原状，后续新包按统一结构创建。

### 7.3 P8 归档记录

2026-07-14 已完成首批文档收敛归档：

| 原路径 | 归档路径 | 当前事实源 |
|--------|----------|------------|
| `Docs/Guide/微信小游戏导出到手机完整指南.md` | `Docs/Agent/Archive/Guide/微信小游戏导出到手机完整指南.md` | `Docs/Guide/BUILD_MINIGAME.md`, `PLATFORM/WECHAT_INTEGRATION.md`, `CONTEXT_PACKS/WeChat_Build_Cloud.md` |
| `Docs/Guide/DANMAKU_DEMO_DECISIONS.md` | `Docs/Agent/Archive/Guide/DANMAKU_DEMO_DECISIONS.md` | `CONTEXT_PACKS/Danmaku_Rendering.md`, `MODULE_CARDS/DanmakuSystem.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| `Docs/Agent/APPFLOW_ACCEPTANCE_PLAN.md` | `Docs/Agent/Archive/AppFlow/APPFLOW_ACCEPTANCE_PLAN.md` | `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 第六部分, `SYSTEMS/APPFLOW_TDD/APPFLOW_TDD_INDEX.md` |
| `Docs/Agent/TDD05_S54_S56_ACCEPTANCE_GUIDE.md` | `Docs/Agent/Archive/ShooterGame/Acceptance/TDD05_S54_S56_ACCEPTANCE_GUIDE.md` | `SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md`, `SHOOTER_GAME/V2_TDD/SG_V2_TDD_05_TOOLS_UI_POLISH.md` |

### 7.4 P8.2 归档记录

2026-07-14 已完成 Guide 深文档收敛：人类架构/模块长文不再作为活跃事实源，必要的人类阅读材料由 Agent 基于当前知识工程按需生成。

| 原路径 | 归档路径 | 当前事实源 |
|--------|----------|------------|
| `Docs/Guide/DANMAKU_SYSTEM.md` | `Docs/Agent/Archive/Guide/Danmaku/DANMAKU_SYSTEM.md` | `CONTEXT_PACKS/Danmaku_Rendering.md`, `MODULE_CARDS/DanmakuSystem.md` |
| `Docs/Guide/DANMAKU_RENDERING.md` | `Docs/Agent/Archive/Guide/Danmaku/DANMAKU_RENDERING.md` | `SYSTEMS/ATLAS_TDD/ATLAS_TDD_INDEX.md`, `SYSTEMS/FLOATING_TEXT/FLOATING_TEXT_TDD.md`, `MODULE_CARDS/Rendering_RuntimeAtlas.md` |
| `Docs/Guide/DANMAKU_DATA.md` | `Docs/Agent/Archive/Guide/Danmaku/DANMAKU_DATA.md` | `MODULE_CARDS/DanmakuSystem.md`, `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU.md` |
| `Docs/Guide/DANMAKU_CONFIG.md` | `Docs/Agent/Archive/Guide/Danmaku/DANMAKU_CONFIG.md` | `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_03_DANMAKU.md`, `SYSTEMS/SO_WORKFLOWS/SO_WORKFLOWS_04_VFX_RENDER.md` |
| `Docs/Guide/DANMAKU_COLLISION.md` | `Docs/Agent/Archive/Guide/Danmaku/DANMAKU_COLLISION.md` | `MODULE_CARDS/DanmakuSystem.md`, `SYSTEMS/OBB_TDD/OBB_TDD_INDEX.md` |
| `Docs/Guide/FRAMEWORK_MODULES*.md` | `Docs/Agent/Archive/Guide/FrameworkModules/` | `MODULE_CARDS/README.md`, `ARCHITECTURE.md`, `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` |
| `C:\workspace\mini-game-template\ShooterGame-Design.md` | `Docs/Agent/Archive/ShooterGame/Design/ShooterGame-Design.md` | `SHOOTER_GAME/SG_GAME_DESIGN.md`, `SHOOTER_GAME/GDD/SG_GDD_INDEX.md`, `SHOOTER_GAME/TDD/SG_TDD_INDEX.md` |
| `C:\workspace\mini-game-template\ShooterGame-UI-Design.md` | `Docs/Agent/Archive/ShooterGame/Design/ShooterGame-UI-Design.md` | `SHOOTER_GAME/SG_UI_DESIGN.md`, `CONTEXT_PACKS/FairyGUI_UI.md`, `MODULE_CARDS/UISystem_FairyGUI.md` |

## 8. 父目录工作台边界

`C:\workspace\mini-game-template` 是本机工作区外壳，不是 Git 仓库。真正纳入版本管理的仓库为 `C:\workspace\mini-game-template\MiniGameTemplate`。

父目录材料的使用规则：

| 路径 | 当前判断 | 使用边界 |
|------|----------|----------|
| `.workbuddy/memory/**` | WorkBuddy 历史长期记忆 | 只作历史背景；若与 `Docs/Agent/**` 冲突，以 `Docs/Agent/**` 和当前代码为准 |
| `.workbuddy/skills/**` | 本机 WorkBuddy 运行时 Skill 目录 | 应与仓库 `skills/**` 保持同步；不直接作为版本化事实源 |
| `.tasks/**` | 早期 task-tracker 本地任务系统 | 不建议整体纳入版本管理；其中有价值结论应迁入 `Docs/Agent/changes/**`、Roadmap 或 Archive |
| `.codegraph/**` | CodeGraph 本地索引数据库 | 工具缓存，不入版本管理；事实以代码和 `TOOLS/CODEGRAPH_INTEGRATION.md` 为准 |
| `output/**` | WebGL/微信小游戏构建产物 | 构建输出，不入版本管理；发布/部署流程见 `PLATFORM/WECHAT_INTEGRATION.md` |

`.tasks` 的定位：保留为本地历史工作台即可，不应作为新 Agent 的当前任务入口。后续任务管理优先使用：

1. `Docs/Agent/KNOWLEDGE/KNOWLEDGE_ENGINEERING_ROADMAP.md`：长期知识工程路线。
2. `Docs/Agent/changes/YYYY-MM-DD-topic/`：重要变更的可追溯记录。
3. `Docs/Agent/SHOOTER_GAME/SG_V2_DEVICE_ACCEPTANCE.md` 等活跃验收/路线文档：项目主线状态。
4. Git commits / GitHub Issues 或 Projects：需要跨人协作、分配、筛选和关闭状态时使用。

## 9. 待校验知识

以下内容需要后续通过 P7 评估持续观察，或在日常维护中执行。

| 项 | 现象 | 风险 | 建议处理 |
|----|------|------|----------|
| Skills 双路径同步 | `skills/` 参与版本管理；父目录存在 `.workbuddy/skills/`，当前未发现 `.codebuddy/skills/` | 两处内容不一致会导致 Agent 行为与仓库知识不同步 | P6 已明确同步检查规则；修改 Skill 后同步实际存在的运行时目录 |
| `INDEX.md` 文件统计 | 索引已更新为 118 活跃 + 90 归档 | 后续新增/归档文档后可能再次过期 | P6 已写入统计命令，日常维护执行 |
| `.workbuddy/skills` 同步 | 2026-07-14 已发现 `code-review-checklist/known-pitfalls.md` 运行时版本领先仓库，已将 PIT-055~057 同步回 `skills/` | 后续仍可能漂移 | 修改 Skill 后使用哈希或 diff 校验仓库源目录与实际运行时目录 |
| `.tasks` 历史状态 | `.tasks/active/ADR-035-IMPL.md` 等文件已滞后于当前 `Docs/Agent` 事实 | 新 Agent 若优先读取 `.tasks` 会误判任务状态 | `.tasks` 不作为事实源；必要信息迁入 `Docs/Agent` 后再归档/忽略 |
| ADR-035 状态已清理 | 原 ADR 标注“待实施”，已于 2026-07-14 代码级确认实现并更新 ADR/Schema | 后续仍需在真机/统一验收中验证退场清理表现 | P4/P5 引用 ADR_SCHEMA 的实现状态与验证项 |
| Guide 与 Agent 双轨 | Guide 已收敛为操作型文档，架构/模块深文档已归档 | 同一概念的长期事实源减少为 Agent 知识工程 | P8.2 已处理；后续避免新增长期人类事实文档 |
| Archive 引用边界 | Archive 内容多且有旧方案 | Agent 可能引用废弃方案 | P0 已规定 Archive 只作历史参考 |
| 变更包结构 | changes 目录已有旧格式，P6 已新增 `changes/README.md` 规范后续结构 | 旧包不强制迁移，新包需统一 | P7 评估是否便于检索 |
| 代码映射闭环初版完成 | 已创建 `KNOWLEDGE/CODE_KNOWLEDGE_MAP.md` 和影响面分析模板 | 后续新增路径需持续维护映射 | P6 已纳入文档维护检查 |
| 架构审查流程初版完成 | 已创建 `KNOWLEDGE/ARCHITECTURE_REVIEW_PROTOCOL.md` 和架构审查模板 | 后续需在实际编码任务中检验是否过重或遗漏 | P7 纳入评估 |
| 知识评估体系初版完成 | 已创建 `KNOWLEDGE/KNOWLEDGE_EVALS.md` 与 10 个标准任务 | 需要定期实际运行评估，发现薄弱环节 | 按评分反向修正知识资产 |

## 10. 当前阶段结论

截至 P8.2，MiniGameTemplate 的知识工程已补齐：

1. Agent 上岗入口与事实源优先级。
2. 高频 Context Pack。
3. 核心 Module Card。
4. 可执行 ADR Schema。
5. 代码知识映射与影响面模板。
6. 架构审查协议。
7. 知识维护协议、文档更新清单与 changes 规范。
8. 首批文档收敛与归档记录。
9. Guide 深文档归档，架构/模块/Danmaku 长期事实源收敛到 Agent 知识工程。

P0-P8.2 初版已完成。后续重点从“补齐文档”转向“跑评估、看漏项、反向修正、控制文档膨胀”，验证这些知识资产是否真正提升 Agent 的路由、设计、影响面判断和验证闭环能力。
