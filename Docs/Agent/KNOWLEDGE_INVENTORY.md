---
system: knowledge-engineering
scope: knowledge-inventory
status: active
created: 2026-07-14
last_updated: 2026-07-14
related_docs: Docs/Agent/KNOWLEDGE_ENGINEERING_ROADMAP.md, Docs/Agent/INDEX.md
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
| Guide 文档 | `Docs/Guide/` 面向人类开发者，覆盖上手、构建、框架模块、弹幕等 | 可辅助理解，但 Agent 应优先读 Agent 文档 |
| Archive 文档 | `Docs/Agent/Archive/` 中 ShooterGame、PK、验收、历史设计材料最多 | 历史参考，不直接作为当前事实 |
| changes 文档 | `Docs/Agent/changes/` 有少量变更包和 bugfix 记录 | 用于追溯近期改动和踩坑 |
| Skills | 仓库实际路径为 `skills/`，包含 8 个技能包 | 操作 SOP 和专家经验源 |
| Skills 双路径 | `skills/` 是纳入版本管理的源目录；WorkBuddy 自动触发 Skill 需要同步到 `.codebuddy/skills/` | 后续需建立同步/校验规则，确保两处内容一致 |
| 索引新鲜度 | `Docs/Agent/INDEX.md` 已更新至 2026-07-14，统计为 114 活跃 + 75 归档 | P6 已建立统计规则，后续新增/归档文档时维护 |

## 3. 当前事实源

这些文档可作为 Agent 理解当前项目的第一可信来源，但仍需结合代码和验证工具确认实现状态。

| 文档/目录 | 可信范围 | 使用方式 |
|-----------|----------|----------|
| `Docs/Agent/INDEX.md` | 任务路由、代码到文档映射、概念速查 | 每次任务优先读取 |
| `Docs/Agent/ARCHITECTURE.md` | 全局架构、启动流程、模块依赖、渲染/弹幕架构 | 做全局设计前读取 |
| `Docs/Agent/ADR_INDEX.md` + `ADR_*.md` | 架构决策、取舍、废弃关系 | 判断设计边界；需 P3 可执行化 |
| `Docs/Agent/CONV_INDEX.md` + `CONV_*.md` | 命名、编码、平台、工作流约束 | 编码和审查前读取 |
| `Docs/Agent/SO_WORKFLOWS_INDEX.md` + 子文档 | SO 类型、路径、创建流程 | 新增配置资产时读取 |
| `Docs/Agent/EC_TDD_INDEX.md` + 子文档 | Entity-Component 框架设计 | 修改战斗框架/组件时读取 |
| `Docs/Agent/SG_TDD_INDEX.md` + 子文档 | ShooterGame V1 技术设计 | 修改 SG 核心流程时读取 |
| `Docs/Agent/SG_V2_TDD_INDEX.md` + 子文档 | ShooterGame V2 技能系统与架构升级 | 修改技能/Buff/DOT/道具/生命周期时读取 |
| `Docs/Agent/APPFLOW_TDD_INDEX.md` + 子文档 | 栈式导航、面板 Suspend/Resume、场景策略 | 修改 UI 流/场景流时读取 |
| `Docs/Agent/DEBUG_PLAYBOOK.md` | 渲染、RuntimeAtlas、弹幕排查方法 | 调试“不显示/不对/性能异常”时读取 |
| `Docs/Agent/WECHAT_INTEGRATION.md` | 微信广告、云开发、CDN、Dev Server 等 | 修改微信平台集成时读取 |
| `Docs/Agent/CODEGRAPH_INTEGRATION.md` | 代码图谱检索策略 | 大范围代码理解时读取 |
| `Docs/Agent/MCP_INTEGRATION.md` | Unity MCP 操作约束 | 需要 Unity 编译/截图/PlayMode 时读取 |
| `Docs/Agent/KNOWLEDGE_ENGINEERING_ROADMAP.md` | 知识工程主线任务 | 推进本知识工程时读取 |
| `Docs/Agent/ARCHITECTURE_REVIEW_PROTOCOL.md` | 中大型改动前的架构审查流程 | 跨模块、框架边界、ADR、热路径或平台敏感任务前读取 |
| `Docs/Agent/KNOWLEDGE_MAINTENANCE.md` | 重要变更后的知识资产同步规则 | 代码、资产、ADR、Skill、changes 或索引变更后读取 |
| `Docs/Agent/KNOWLEDGE_EVALS.md` | 知识工程评估体系 | 定期检查 Agent 路由、影响面、ADR、验证和维护闭环 |

## 4. 专题文档族清单

| 文档族 | 文件范围 | 当前用途 | 后续知识工程动作 |
|--------|----------|----------|------------------|
| ADR | `ADR_INDEX.md`, `ADR_01~06_*.md` | 决策记录 | P3 增加 Schema、AppliesTo、Constraints、Verification |
| CONV | `CONV_INDEX.md`, `CONV_01~04_*.md` | 项目规范 | P1 纳入 Bootstrap 必读；P5 作为架构审查约束 |
| EC_TDD | `EC_TDD_INDEX.md`, `EC_TDD_01~08_*.md` | Entity 框架设计 | P2 生成 `MODULE_CARDS/EntitySystem.md` |
| SO_WORKFLOWS | `SO_WORKFLOWS_INDEX.md`, `SO_WORKFLOWS_01~05_*.md` | SO 配置流程 | P1 生成 `CONTEXT_PACKS/SO_Config_Workflow.md` |
| APPFLOW | `APPFLOW_TDD_INDEX.md`, `APPFLOW_TDD_01~05_*.md`, `APPFLOW_ACCEPTANCE_PLAN.md` | 导航系统 | P2 生成 `MODULE_CARDS/AppFlow.md` |
| ATLAS | `ATLAS_TDD_INDEX.md`, `ATLAS_TDD_01~02_*.md` | RuntimeAtlas 设计 | P2 生成 Rendering/RuntimeAtlas 模块卡 |
| OBB | `OBB_TDD_INDEX.md`, `OBB_TDD_01~02_*.md` | OBB 碰撞 | P4 加入碰撞代码映射 |
| SG GDD/TDD | `SG_GAME_DESIGN.md`, `SG_UI_DESIGN.md`, `SG_TDD_*` | ShooterGame V1 | P2 生成 `MODULE_CARDS/ShooterGame.md` |
| SG_V2 | `SG_GDD_*`, `SG_V2_TDD_*`, `SG_V2_DEVICE_ACCEPTANCE.md` | 技能系统、验收 | P1/P2 纳入 ShooterGame Context Pack |
| SG_TOOLS | `SG_TOOLS_TDD_*` | 波次编辑器、Debug 工具 | P4 纳入编辑器工具映射 |
| EDITOR_TOOLS | `EDITOR_TOOLS_MANUAL_*` | 编辑器工具使用手册 | P1/P4 纳入工具 Context Pack |
| Debug/Integration | `DEBUG_PLAYBOOK.md`, `MCP_INTEGRATION.md`, `CODEGRAPH_INTEGRATION.md`, `WECHAT_INTEGRATION.md` | 工具与排查 | P1 纳入 Bootstrap 工具入口 |

## 5. Guide 文档边界

`Docs/Guide/` 面向人类开发者，适合快速理解和操作，但不是 Agent 的唯一事实源。

| 文档 | 当前用途 | 使用边界 |
|------|----------|----------|
| `README.md` | 人类文档入口、项目定位、技术栈 | 可读；Agent 任务仍应转向 `Docs/Agent/INDEX.md` |
| `GETTING_STARTED.md` | 环境搭建、首次运行 | 用于上手和环境问题 |
| `BUILD_MINIGAME.md` | 微信小游戏构建流程 | 构建任务可读；平台细节还需看 `WECHAT_INTEGRATION.md` |
| `ARCHITECTURE_OVERVIEW.md` | 人类版架构概览 | 可作为快速背景，细节以 `ARCHITECTURE.md` 为准 |
| `FRAMEWORK_MODULES*.md` | 框架模块 API 和说明 | 可作为模块背景；Entity/VFX/Rendering 仍在 Agent 文档更完整 |
| `DANMAKU_*.md` | 弹幕系统专题 | 弹幕修改可读；实现细节结合 Agent/ATLAS/DEBUG |
| `FAQ.md` | 常见问题 | 辅助排错 |
| `微信小游戏导出到手机完整指南.md` | 真机导出流程 | 平台操作参考 |

## 6. Skills 盘点

仓库实际存在 `skills/` 目录，这是纳入版本管理的 Skill 源目录。WorkBuddy 这类 Agent 工具自动触发 Skill 时，需要将同一套内容放置到 `.codebuddy/skills/`。因此两者不是互斥路径，而是“仓库源目录 + 运行时触发目录”的双路径约定，内容应保持一致。

| Skill | 路径 | 职责 | 知识工程定位 |
|-------|------|------|--------------|
| `coding-standards` | `skills/coding-standards/` | 编码规则、平台规则、Bug 修复 SOP | P1 Bootstrap 必读规则候选 |
| `code-review-checklist` | `skills/code-review-checklist/` | 审查清单、已知坑、维护指南 | P5/P6 审查与维护输入 |
| `doc-maintenance` | `skills/doc-maintenance/` | 文档 frontmatter、索引模板、维护规则 | P6 的基础材料 |
| `fairygui-tools` | `skills/fairygui-tools/` | FairyGUI XML、工作流、C# 模板、校验脚本 | P1 Context Pack 输入 |
| `luban-config` | `skills/luban-config/` | Luban 表结构、脚本、项目布局 | P1 SO/Config Context Pack 输入 |
| `task-tracker` | `skills/task-tracker/` | 跨会话任务模板 | 可与本路线图协同 |
| `tdd-pk-review` | `skills/tdd-pk-review/tdd-pk-review/` | PK 对抗评审模板与收敛标准 | P5 架构审查流程输入 |
| `vfx-creator` | `skills/vfx-creator/` | VFX 工作流、验证清单、提示模板 | VFX 模块卡输入 |

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

### 7.2 changes

`Docs/Agent/changes/` 当前已有少量变更包和 bugfix 记录：

- `2026-04-21-laser-atlas-bugfix`
- `2026-04-21-phase4-deep-integration`
- `2026-04-23-obb-obstacle`
- `2026-04-29-fairygui-click-fix`
- `2026-04-30-p1.0-enumcamp-migration`
- `P2.4_P2.5_ACCEPTANCE_CHECKLIST.md`

这些是知识维护机制的既有样例。P6 已新增 `Docs/Agent/changes/README.md`：旧包保留原状，后续新包按统一结构创建。

## 8. 待校验知识

以下内容需要后续通过 P7 评估持续观察，或在日常维护中执行。

| 项 | 现象 | 风险 | 建议处理 |
|----|------|------|----------|
| Skills 双路径同步 | `skills/` 参与版本管理，`.codebuddy/skills/` 用于 WorkBuddy 自动触发；当前工作区未发现 `.codebuddy/` 目录 | 两处内容不一致会导致 Agent 行为与仓库知识不同步 | P6 已明确同步检查规则；若恢复 WorkBuddy 目录，按维护清单同步 |
| `INDEX.md` 文件统计 | 索引已更新为 114 活跃 + 75 归档 | 后续新增/归档文档后可能再次过期 | P6 已写入统计命令，日常维护执行 |
| ADR-035 状态已清理 | 原 ADR 标注“待实施”，已于 2026-07-14 代码级确认实现并更新 ADR/Schema | 后续仍需在真机/统一验收中验证退场清理表现 | P4/P5 引用 ADR_SCHEMA 的实现状态与验证项 |
| Guide 与 Agent 双轨 | Guide 仍保留人类文档，Agent 文档更完整 | 同一概念可能有两处说法 | P1 Bootstrap 明确优先级 |
| Archive 引用边界 | Archive 内容多且有旧方案 | Agent 可能引用废弃方案 | P0 已规定 Archive 只作历史参考 |
| 变更包结构 | changes 目录已有旧格式，P6 已新增 `changes/README.md` 规范后续结构 | 旧包不强制迁移，新包需统一 | P7 评估是否便于检索 |
| 代码映射闭环初版完成 | 已创建 `CODE_KNOWLEDGE_MAP.md` 和影响面分析模板 | 后续新增路径需持续维护映射 | P6 已纳入文档维护检查 |
| 架构审查流程初版完成 | 已创建 `ARCHITECTURE_REVIEW_PROTOCOL.md` 和架构审查模板 | 后续需在实际编码任务中检验是否过重或遗漏 | P7 纳入评估 |
| 知识评估体系初版完成 | 已创建 `KNOWLEDGE_EVALS.md` 与 10 个标准任务 | 需要定期实际运行评估，发现薄弱环节 | 按评分反向修正知识资产 |

## 9. 当前阶段结论

截至 P6，MiniGameTemplate 的知识工程已补齐：

1. Agent 上岗入口与事实源优先级。
2. 高频 Context Pack。
3. 核心 Module Card。
4. 可执行 ADR Schema。
5. 代码知识映射与影响面模板。
6. 架构审查协议。
7. 知识维护协议、文档更新清单与 changes 规范。

P0-P7 初版已完成。后续重点从“补齐文档”转向“跑评估、看漏项、反向修正”，验证这些知识资产是否真正提升 Agent 的路由、设计、影响面判断和验证闭环能力。
